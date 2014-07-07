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

using System.Linq.Expressions;

namespace Microsoft.Scripting.ComInterop
{
	/// <summary>
	/// メソッドバインダーによって使用される引数を提供します。
	/// メソッドに定義されているそれぞれの物理仮引数に対して 1 つの <see cref="ArgBuilder"/> が存在します。
	/// メソッドに渡される論理実引数を表す <see cref="Microsoft.Scripting.Actions.Calls.ParameterWrapper"/> とは対照的です。
	/// </summary>
	abstract class ArgBuilder
	{
		/// <summary>引数に渡される値を提供する <see cref="Expression"/> を返します。</summary>
		internal abstract Expression Marshal(Expression parameter);

		/// <summary>
		/// 引数に渡される値を提供する <see cref="Expression"/> を返します。
		/// このメソッドは結果が参照渡しに利用されると想定される場合に呼ばれます。
		/// </summary>
		internal virtual Expression MarshalToRef(Expression parameter) { return Marshal(parameter); }

		/// <summary>
		/// メソッド呼び出しの後で指定された値を更新する <see cref="Expression"/> を返します。
		/// 更新が必要ない場合は <c>null</c> を返す可能性があります。
		/// </summary>
		internal virtual Expression UnmarshalFromRef(Expression newValue) { return newValue; }
	}
}