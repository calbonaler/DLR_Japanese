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
using System.Reflection;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Ast
{
	public static partial class Utils
	{
		/// <summary>
		/// 指定した引数を使用して指定されたコンストラクタを呼び出します。
		/// このメソッドは必要であれば <see cref="Convert"/> を使用する変換を引数に対して行います。
		/// </summary>
		/// <param name="constructor">呼び出すコンストラクタを指定します。</param>
		/// <param name="arguments">コンストラクタの呼び出しに必要な引数を指定します。</param>
		/// <returns>コンストラクタの呼び出しを表す <see cref="NewExpression"/>。</returns>
		public static NewExpression SimpleNewHelper(ConstructorInfo constructor, params Expression[] arguments)
		{
			ContractUtils.RequiresNotNull(constructor, "constructor");
			ContractUtils.RequiresNotNullItems(arguments, "arguments");
			ParameterInfo[] parameters = constructor.GetParameters();
			ContractUtils.Requires(arguments.Length == parameters.Length, "arguments", "実引数の数が正しくありません。");
			return Expression.New(constructor, ArgumentConvertHelper(arguments, parameters));
		}
	}
}
