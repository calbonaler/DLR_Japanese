/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Microsoft Public License. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Microsoft Public License, please send an email to 
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Microsoft Public License.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Contracts;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting
{
	/// <summary>ディスクに保存することができる <see cref="ScriptCode"/> を表します。</summary>
	public abstract class SavableScriptCode : ScriptCode
	{
		/// <summary>翻訳単位を使用して、<see cref="Microsoft.Scripting.SavableScriptCode"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="sourceUnit">このクラスに関連づけられる<see cref="LanguageContext"/> を保持している <see cref="SourceUnit"/> オブジェクトを指定します。</param>
		protected SavableScriptCode(SourceUnit sourceUnit) : base(sourceUnit) { }

		class CodeInfo
		{
			public readonly MethodBuilder Builder;
			public readonly ScriptCode Code;
			public readonly Type DelegateType;

			public CodeInfo(MethodBuilder builder, ScriptCode code, Type delegateType)
			{
				Builder = builder;
				Code = code;
				DelegateType = delegateType;
			}
		}

		/// <summary>
		/// 指定された <see cref="SavableScriptCode"/> オブジェクトをアセンブリの保存場所を指定して保存します。
		/// 指定されたスクリプトのコードは複数の言語のコードとすることができます。
		/// ファイルが既に存在した場合は例外がスローされます。
		/// </summary>
		/// <param name="assemblyName">保存するアセンブリの場所を表す拡張子を含んだアセンブリ名を指定します。完全修飾名あるいは相対パスのどちらかである必要があります。</param>
		/// <param name="codes">指定されたアセンブリに保存する <see cref="SavableScriptCode"/> オブジェクトを指定します。</param>
		public static void SaveToAssembly(string assemblyName, params SavableScriptCode[] codes)
		{
			ContractUtils.RequiresNotNull(assemblyName, "assemblyName");
			ContractUtils.RequiresNotNullItems(codes, "codes");
			// break the assemblyName into it's dir/name/extension
			var dir = Path.GetDirectoryName(assemblyName);
			if (string.IsNullOrEmpty(dir))
				dir = Environment.CurrentDirectory;
			// build the assembly & type gen that all the script codes will live in...
			var ag = new AssemblyGen(new AssemblyName(Path.GetFileNameWithoutExtension(assemblyName)), dir, Path.GetExtension(assemblyName), /*emitSymbols*/false);
			var tb = ag.DefinePublicType("DLRCachedCode", typeof(object), true);
			var tg = new TypeGen(ag, tb);
			// then compile all of the code
			var langCtxBuilders = new Dictionary<Type, List<CodeInfo>>();
			foreach (var sc in codes)
			{
				List<CodeInfo> builders;
				if (!langCtxBuilders.TryGetValue(sc.LanguageContext.GetType(), out builders))
					langCtxBuilders[sc.LanguageContext.GetType()] = builders = new List<CodeInfo>();
				var compInfo = sc.CompileForSave(tg);
				builders.Add(new CodeInfo(compInfo.Key, sc, compInfo.Value));
			}
			var mb = tb.DefineMethod(
				"GetScriptCodeInfo",
				MethodAttributes.SpecialName | MethodAttributes.Public | MethodAttributes.Static,
				typeof(MutableTuple<Type[], Delegate[][], string[][], string[][]>),
				Type.EmptyTypes);
			var ilgen = mb.GetILGenerator();
			var langsWithBuilders = langCtxBuilders.ToArray();
			// lang ctx array
			ilgen.EmitArray(typeof(Type), langsWithBuilders.Length, i =>
			{
				ilgen.Emit(OpCodes.Ldtoken, langsWithBuilders[i].Key);
				ilgen.EmitCall(typeof(Type).GetMethod("GetTypeFromHandle", new[] { typeof(RuntimeTypeHandle) }));
			});
			// builders array of array
			ilgen.EmitArray(typeof(Delegate[]), langsWithBuilders.Length, i =>
				ilgen.EmitArray(typeof(Delegate), langsWithBuilders[i].Value.Count, j =>
				{
					ilgen.EmitNull();
					ilgen.Emit(OpCodes.Ldftn, langsWithBuilders[i].Value[j].Builder);
					ilgen.EmitNew(langsWithBuilders[i].Value[j].DelegateType, new[] { typeof(object), typeof(IntPtr) });
				})
			);
			// paths array of array
			ilgen.EmitArray(typeof(string[]), langsWithBuilders.Length, i =>
				ilgen.EmitArray(typeof(string), langsWithBuilders[i].Value.Count,
					j => ilgen.EmitString(langsWithBuilders[i].Value[j].Code.SourceUnit.Path)
				)
			);
			// 4th element in tuple - custom per-language data
			ilgen.EmitArray(typeof(string[]), langsWithBuilders.Length, i =>
				ilgen.EmitArray(typeof(string), langsWithBuilders[i].Value.Count, j =>
				{
					var data = langsWithBuilders[i].Value[j].Code as ICustomScriptCodeData;
					if (data != null)
						ilgen.EmitString(data.CustomScriptCodeData);
					else
						ilgen.EmitNull();
				})
			);
			ilgen.EmitNew(typeof(MutableTuple<Type[], Delegate[][], string[][], string[][]>), new[] { typeof(Type[]), typeof(Delegate[][]), typeof(string[][]), typeof(string[][]) });
			ilgen.Emit(OpCodes.Ret);
			mb.SetCustomAttribute(new CustomAttributeBuilder(typeof(DlrCachedCodeAttribute).GetConstructor(Type.EmptyTypes), ArrayUtils.EmptyObjects));
			tg.FinishType();
			ag.SaveAssembly();
		}

		/// <summary>
		/// 指定されたアセンブリから <see cref="ScriptDomainManager"/> にロードされた新しい <see cref="ScriptCode"/> のセットを返します。
		/// <see cref="ScriptCode"/> に関連付けられた言語が <see cref="ScriptDomainManager"/> にロードされていない場合、保存された型情報に基づいて <see cref="LanguageContext"/> をロードします。
		/// 言語のコンパイル対象の <see cref="LanguageContext"/> や DLR のバージョンが利用できない場合、管理者によってバインディングをリダイレクトするポリシーが適用されない限り <see cref="TypeLoadException"/> がスローされます。
		/// </summary>
		/// <param name="runtime">読み込まれた <see cref="ScriptCode"/> または言語がロードされる <see cref="ScriptDomainManager"/> を指定します。</param>
		/// <param name="assembly">ロードする <see cref="ScriptCode"/> を含むユーザーがすでにロードしたアセンブリを指定します。</param>
		/// <returns><see cref="ScriptDomainManager"/> にロードされた新しい <see cref="ScriptCode"/> の配列。</returns>
		public static ScriptCode[] LoadFromAssembly(ScriptDomainManager runtime, Assembly assembly)
		{
			ContractUtils.RequiresNotNull(runtime, "runtime");
			ContractUtils.RequiresNotNull(assembly, "assembly");
			// get the type which has our cached code...
			var t = assembly.GetType("DLRCachedCode");
			if (t == null)
				return new ScriptCode[0];
			var codes = new List<ScriptCode>();
			var mi = t.GetMethod("GetScriptCodeInfo");
			if (mi.IsSpecialName && mi.IsDefined(typeof(DlrCachedCodeAttribute), false))
			{
				var infos = (MutableTuple<Type[], Delegate[][], string[][], string[][]>)mi.Invoke(null, ArrayUtils.EmptyObjects);
				for (int i = 0; i < infos.Item000.Length; i++)
				{
					var lc = runtime.GetLanguage(infos.Item000[i]);
					Debug.Assert(infos.Item001[i].Length == infos.Item002[i].Length);
					for (int j = 0; j < infos.Item001[i].Length; j++)
						codes.Add(lc.LoadCompiledCode(infos.Item001[i][j], infos.Item002[i][j], infos.Item003[i][j]));
				}
			}
			return codes.ToArray();
		}

		/// <summary>指定されたラムダ式をディスクへ保存するためにリライトします。</summary>
		/// <param name="typeGen">リライトでフィールドなどが付加される型を構築する <see cref="TypeGen"/> を指定します。</param>
		/// <param name="code">ディスクへ保存するためにリライトするラムダ式を指定します。</param>
		/// <returns>リライトされたラムダ式</returns>
		protected LambdaExpression RewriteForSave(TypeGen typeGen, LambdaExpression code) { return new ToDiskRewriter(typeGen).RewriteLambda(code); }

		/// <summary>このオブジェクトが表すコードを保存用にコンパイルします。</summary>
		/// <param name="typeGen">このコードと等価なメソッドが追加される型を表す <see cref="TypeGen"/> を指定します。</param>
		/// <returns>コードがコンパイルされたメソッドとメソッドを表すデリゲートの型を示す <see cref="KeyValuePair&lt;MethodBuilder, Type&gt;"/>。</returns>
		protected virtual KeyValuePair<MethodBuilder, Type> CompileForSave(TypeGen typeGen) { throw new NotSupportedException(); }

		/// <summary>このオブジェクトの文字列表現を返します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		[Confined]
		public override string ToString() { return string.Format("ScriptCode '{0}' from {1}", SourceUnit.Path, LanguageContext.GetType().Name); }
	}
}
