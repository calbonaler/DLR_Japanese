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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Contracts;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Generation
{
	/// <summary>型の構築を支援します。</summary>
	public sealed class TypeGen
	{
		ILGenerator _initGen; // The IL generator for the .cctor()

		/// <summary>型初期化子 (cctor) の本体を構築する <see cref="ILGenerator"/> を取得します。</summary>
		public ILGenerator TypeInitializer
		{
			get
			{
				if (_initGen == null)
					_initGen = TypeBuilder.DefineTypeInitializer().GetILGenerator();
				return _initGen;
			}
		}

		/// <summary>この型が属しているアセンブリを構築している <see cref="AssemblyGen"/> を取得します。</summary>
		internal AssemblyGen AssemblyGen { get; private set; }

		/// <summary>型をより詳細に定義できる <see cref="TypeBuilder"/> を取得します。</summary>
		public TypeBuilder TypeBuilder { get; private set; }

		/// <summary>指定された <see cref="AssemblyGen"/> および <see cref="TypeBuilder"/> を使用して、<see cref="Microsoft.Scripting.Generation.TypeGen"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="myAssembly">この型が属しているアセンブリを構築している <see cref="AssemblyGen"/> を指定します。</param>
		/// <param name="myType">この型を構築する <see cref="TypeBuilder"/> を指定します。</param>
		public TypeGen(AssemblyGen myAssembly, TypeBuilder myType)
		{
			Assert.NotNull(myAssembly, myType);
			AssemblyGen = myAssembly;
			TypeBuilder = myType;
		}

		/// <summary>このオブジェクトの文字列表現を取得します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		[Confined]
		public override string ToString() { return TypeBuilder.ToString(); }

		/// <summary>型の構築を完了して、この型の <see cref="System.Type"/> オブジェクトを作成します。</summary>
		/// <returns>この型を表す <see cref="System.Type"/> オブジェクト。</returns>
		public Type FinishType()
		{
			if (_initGen != null)
				_initGen.Emit(OpCodes.Ret);
			var ret = TypeBuilder.CreateType();
			Debug.WriteLine("finished: " + ret.FullName);
			return ret;
		}

		/// <summary>この型に指定された型および名前をもつパブリックな静的フィールドを追加します。</summary>
		/// <param name="fieldType">静的フィールドの型を指定します。</param>
		/// <param name="name">静的フィールドの名前を指定します。</param>
		/// <returns>追加された静的フィールドを構築する <see cref="FieldBuilder"/>。</returns>
		public FieldBuilder AddStaticField(Type fieldType, string name) { return TypeBuilder.DefineField(name, fieldType, FieldAttributes.Public | FieldAttributes.Static); }

		/// <summary>この型に指定された型および名前をもつ静的フィールドを追加します。</summary>
		/// <param name="fieldType">静的フィールドの型を指定します。</param>
		/// <param name="attributes">静的フィールドの名前を指定します。</param>
		/// <param name="name">静的フィールドの属性を表す <see cref="FieldAttributes"/> を指定します。</param>
		/// <returns>追加された静的フィールドを構築する <see cref="FieldBuilder"/>。</returns>
		public FieldBuilder AddStaticField(Type fieldType, FieldAttributes attributes, string name) { return TypeBuilder.DefineField(name, fieldType, attributes | FieldAttributes.Static); }

		/// <summary>この型に指定されたインターフェイス メソッドの明示実装を定義します。</summary>
		/// <param name="baseMethod">実装するインターフェイスのメソッドを表す <see cref="MethodInfo"/> を指定します。</param>
		/// <returns>インターフェイスの明示実装メソッド本体を構築できる <see cref="ILGenerator"/>。</returns>
		public ILGenerator DefineExplicitInterfaceImplementation(MethodInfo baseMethod)
		{
			ContractUtils.RequiresNotNull(baseMethod, "baseMethod");
			var mb = TypeBuilder.DefineMethod(
				baseMethod.DeclaringType.Name + "." + baseMethod.Name,
				baseMethod.Attributes & ~(MethodAttributes.Abstract | MethodAttributes.Public) | MethodAttributes.NewSlot | MethodAttributes.Final,
				baseMethod.ReturnType,
				baseMethod.GetParameters().Select(p => p.ParameterType).ToArray()
			);
			TypeBuilder.DefineMethodOverride(mb, baseMethod);
			return mb.GetILGenerator();
		}

		/// <summary>この型で指定された基底クラスのメソッドをオーバーライドします。</summary>
		/// <param name="baseMethod">オーバーライドする基底クラスのメソッドを表す <see cref="MethodInfo"/> を指定します。</param>
		/// <returns>メソッド オーバーライドの本体を構築できる <see cref="ILGenerator"/>。</returns>
		public ILGenerator DefineMethodOverride(MethodInfo baseMethod)
		{
			var mb = TypeBuilder.DefineMethod(baseMethod.Name, baseMethod.Attributes & ~(MethodAttributes.Abstract | MethodAttributes.ReservedMask), baseMethod.ReturnType, baseMethod.GetParameters().Select(p => p.ParameterType).ToArray());
			TypeBuilder.DefineMethodOverride(mb, baseMethod);
			return mb.GetILGenerator();
		}
	}
}
