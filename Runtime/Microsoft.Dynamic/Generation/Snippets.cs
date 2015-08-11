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
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Generation
{
	// TODO: simplify initialization logic & state
	/// <summary>動的言語ランタイム全体で使用する型を定義し、アセンブリを作成できるようにします。</summary>
	public static class Snippets
	{
		static AssemblyGen _assembly;
		static AssemblyGen _debugAssembly;

		/// <summary><see cref="SaveSnippets"/> が設定されたいるときに、アセンブリが保存されるディレクトリを取得します。</summary>
		public static string SnippetsDirectory { get; private set; }

		/// <summary>アセンブリを保存するかどうかを示す値を取得します。</summary>
		public static bool SaveSnippets { get; private set; }

		static AssemblyGen GetAssembly(bool emitSymbols)
		{
			var outDirectory = SaveSnippets ? SnippetsDirectory ?? Directory.GetCurrentDirectory() : null;
			if (emitSymbols)
			{
				if (_debugAssembly == null)
					Interlocked.CompareExchange(ref _debugAssembly, new AssemblyGen(new AssemblyName("Snippets.debug.scripting"), outDirectory, ".dll", true), null);
				return _debugAssembly;
			}
			else
			{
				if (_assembly == null)
					Interlocked.CompareExchange(ref _assembly, new AssemblyGen(new AssemblyName("Snippets.scripting"), outDirectory, ".dll", false), null);
				return _assembly;
			}
		}

		/// <summary>アセンブリの保存に関する情報を設定します。</summary>
		/// <param name="enable">アセンブリの保存を可能にするかどうかを示す値を指定します。</param>
		/// <param name="directory">アセンブリが保存されるディレクトリを指定します。</param>
		public static void SetSaveAssemblies(bool enable, string directory)
		{
			// リフレクションを通じて SetSaveAssemblies を呼び出すことによって内部リングに対して SaveAssemblies を設定する。
			var assemblyGen = typeof(Expression).Assembly.GetType(typeof(Expression).Namespace + ".Compiler.AssemblyGen");
			// 型が存在しないかもしれない
			if (assemblyGen != null)
			{
				var configSaveAssemblies = assemblyGen.GetMethod("SetSaveAssemblies", BindingFlags.NonPublic | BindingFlags.Static);
				// メソッドが存在しないかもしれない
				if (configSaveAssemblies != null)
					configSaveAssemblies.Invoke(null, new object[] { enable, directory });
			}
			SaveSnippets = enable;
			SnippetsDirectory = directory;
		}

		/// <summary>アセンブリを保存して検証を行います。</summary>
		public static void SaveAndVerifyAssemblies()
		{
			if (!SaveSnippets)
				return;
			// 検証されるアセンブリの場所を取得するために、リフレクションを使用してコアの AssemblyGen.SaveAssembliesToDisk を呼び出す
			// アセンブリを PEVerify.exe を使用して検証する。
			// 外側のリングアセンブリはコアのリングアセンブリに依存しているので、これらは外側のリングアセンブリを検証する前に行う必要がある。
			// すなわち順番は次のようである必要がある。
			// 1) 内部リングのアセンブリを保存する。
			// 2) 外部リングのアセンブリを保存する。内部リングのアセンブリは生成される IL を通して外部リングのアセンブリに依存しているので、
			//    これは内部リングのアセンブリを検証する前に実行される必要がある。
			// 3) 内部リングのアセンブリを検証する。
			// 4) 外部リングのアセンブリを検証する。
			var assemblyGen = typeof(Expression).Assembly.GetType(typeof(Expression).Namespace + ".Compiler.AssemblyGen");
			// 型は存在しないかもしれない
			string[] coreAssemblyLocations = null;
			if (assemblyGen != null)
			{
				var saveAssemblies = assemblyGen.GetMethod("SaveAssembliesToDisk", BindingFlags.NonPublic | BindingFlags.Static);
				// メソッドは存在しないかもしれない
				if (saveAssemblies != null)
					coreAssemblyLocations = (string[])saveAssemblies.Invoke(null, null);
			}
			var outerAssemblyLocations = SaveAssemblies();
			if (coreAssemblyLocations != null)
			{
				foreach (var file in coreAssemblyLocations)
					AssemblyGen.PeVerifyAssemblyFile(file);
			}
			// 外部リングのアセンブリを検証
			foreach (var file in outerAssemblyLocations)
				AssemblyGen.PeVerifyAssemblyFile(file);
		}

		/// <summary>アセンブリを保存して、検証される必要があるアセンブリの場所を返します。</summary>
		/// <returns>検証される必要があるアセンブリの場所。</returns>
		static string[] SaveAssemblies()
		{
			if (!SaveSnippets)
				return ArrayUtils.EmptyStrings;
			List<string> assemlyLocations = new List<string>();
			// 最初にすべてのアセンブリをディスクに保存する
			if (_assembly != null)
			{
				var assemblyLocation = _assembly.SaveAssembly();
				if (assemblyLocation != null)
					assemlyLocations.Add(assemblyLocation);
				_assembly = null;
			}
			if (_debugAssembly != null)
			{
				var debugAssemblyLocation = _debugAssembly.SaveAssembly();
				if (debugAssemblyLocation != null)
					assemlyLocations.Add(debugAssemblyLocation);
				_debugAssembly = null;
			}
			return assemlyLocations.ToArray();
		}

		/// <summary>新しい動的メソッドを作成して、メソッド本体を構築する <see cref="DynamicILGen"/> を返します。</summary>
		/// <param name="methodName">動的メソッドの名前を指定します。</param>
		/// <param name="returnType">動的メソッドの戻り値の型を指定します。</param>
		/// <param name="parameterTypes">動的メソッドの仮引数の型を指定します。</param>
		/// <param name="isDebuggable">動的メソッドがデバッグ可能かどうかを示す値を指定します。</param>
		/// <returns>動的メソッドの本体を構築する <see cref="DynamicILGen"/>。</returns>
		public static DynamicILGen CreateDynamicMethod(string methodName, Type returnType, Type[] parameterTypes, bool isDebuggable)
		{
			ContractUtils.RequiresNotEmpty(methodName, "methodName");
			ContractUtils.RequiresNotNull(returnType, "returnType");
			ContractUtils.RequiresNotNullItems(parameterTypes, "parameterTypes");
			if (SaveSnippets)
			{
				var tb = GetAssembly(isDebuggable).DefinePublicType(methodName, typeof(object), false);
				return new DynamicILGenType(tb, tb.DefineMethod(methodName, CompilerHelpers.PublicStatic, returnType, parameterTypes));
			}
			else
				return new DynamicILGenMethod(new DynamicMethod(methodName, returnType, parameterTypes, true));
		}

		/// <summary>指定された名前をもつ新しいパブリック型を定義して、型を構築する <see cref="TypeBuilder"/> を返します。</summary>
		/// <param name="name">定義する型の名前を指定します。</param>
		/// <param name="parent">定義する型が拡張する型を指定します。</param>
		/// <returns>定義された型を構築する <see cref="TypeBuilder"/>。</returns>
		public static TypeBuilder DefinePublicType(string name, Type parent) { return GetAssembly(false).DefinePublicType(name, parent, false); }

		/// <summary>指定された名前をもつ新しいパブリック型を定義して、型を構築する <see cref="TypeBuilder"/> を返します。</summary>
		/// <param name="name">定義する型の名前を指定します。</param>
		/// <param name="parent">定義する型が拡張する型を指定します。</param>
		/// <param name="preserveName">指定された名前を保持するかどうかを示す値を指定します。</param>
		/// <param name="emitDebugSymbols">デバッグシンボルを出力するかどうかを示す値を指定します。</param>
		/// <returns>定義された型を構築する <see cref="TypeBuilder"/>。</returns>
		public static TypeBuilder DefinePublicType(string name, Type parent, bool preserveName, bool emitDebugSymbols) { return GetAssembly(emitDebugSymbols).DefinePublicType(name, parent, preserveName); }

		/// <summary>指定された名前、パラメータ型、戻り値の型をもつ新しいデリゲート型を定義して、型を構築をする <see cref="TypeBuilder"/> を返します。</summary>
		/// <param name="name">アセンブリに定義する型の名前を指定します。</param>
		/// <param name="parameters">作成するデリゲート型のパラメータ型を指定します。</param>
		/// <param name="returnType">作成するデリゲート型の戻り値の型を指定します。</param>
		/// <returns>定義された型を構築する <see cref="Type"/>。</returns>
		public static TypeBuilder DefineDelegateType(string name, Type[] parameters, Type returnType) { return GetAssembly(false).DefineDelegateType(name, parameters, returnType); }

		/// <summary>指定されたアセンブリがこのスニペットで構築しているアセンブリかどうかを判断します。</summary>
		/// <param name="asm">調べるアセンブリを指定します。</param>
		/// <returns>指定されたアセンブリがこのスニペットで構築しているアセンブリである場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool IsSnippetsAssembly(Assembly asm) { return _assembly != null && asm == _assembly.AssemblyBuilder || _debugAssembly != null && asm == _debugAssembly.AssemblyBuilder; }
	}
}
