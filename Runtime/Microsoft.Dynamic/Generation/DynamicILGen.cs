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
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Generation
{
	/// <summary>動的なメソッドの本体の構築を支援し、メソッドまたはデリゲートを取得できるようにします。</summary>
	public abstract class DynamicILGen
	{
		/// <summary>動的メソッドの本体を構築できる <see cref="ILGenerator"/> を使用して、<see cref="Microsoft.Scripting.Generation.DynamicILGen"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="il">動的メソッドの本体を構築できる <see cref="ILGenerator"/> を指定します。</param>
		internal DynamicILGen(ILGenerator il) { Generator = il; }

		/// <summary>この動的メソッドを構築する <see cref="ILGenerator"/> を取得します。</summary>
		public ILGenerator Generator { get; private set; }

		/// <summary>動的メソッドを完了して、作成されたメソッドを表す指定された型のデリゲートを作成します。</summary>
		/// <typeparam name="TDelegate">取得するデリゲートの型を指定します。</typeparam>
		/// <returns>作成されたメソッドを表すデリゲート。</returns>
		public TDelegate CreateDelegate<TDelegate>() where TDelegate : class
		{
			MethodInfo mi;
			return CreateDelegate<TDelegate>(out mi);
		}

		/// <summary>動的メソッドを完了して、作成されたメソッドを表す指定された型のデリゲートおよびメソッドの情報を格納する <see cref="MethodInfo"/> を取得します。</summary>
		/// <typeparam name="TDelegate">取得するデリゲートの型を指定します。</typeparam>
		/// <param name="mi">作成されたメソッドの情報が格納されます。</param>
		/// <returns>作成されたメソッドを表すデリゲート。</returns>
		public TDelegate CreateDelegate<TDelegate>(out MethodInfo mi) where TDelegate : class
		{
			ContractUtils.Requires(typeof(TDelegate).IsSubclassOf(typeof(Delegate)), "T");
			return (TDelegate)(object)(mi = Finish()).CreateDelegate(typeof(TDelegate));
		}

		/// <summary>動的メソッドを完了して、作成されたメソッドの情報を格納する <see cref="MethodInfo"/> を取得します。</summary>
		/// <returns>作成されたメソッドの情報を表す <see cref="MethodInfo"/>。</returns>
		public abstract MethodInfo Finish();
	}

	class DynamicILGenMethod : DynamicILGen
	{
		readonly DynamicMethod _dm;

		internal DynamicILGenMethod(DynamicMethod dm) : base(dm.GetILGenerator()) { _dm = dm; }

		public override MethodInfo Finish() { return _dm; }
	}

	class DynamicILGenType : DynamicILGen
	{
		readonly TypeBuilder _tb;
		readonly MethodBuilder _mb;

		internal DynamicILGenType(TypeBuilder tb, MethodBuilder mb) : base(mb.GetILGenerator())
		{
			_tb = tb;
			_mb = mb;
		}

		public override MethodInfo Finish() { return _tb.CreateType().GetMethod(_mb.Name); }
	}
}
