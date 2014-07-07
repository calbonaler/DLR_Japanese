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
using System.Linq.Expressions;

namespace Microsoft.Scripting.Utils
{
	/// <summary>式木に対するユーティリティメソッドを提供します。</summary>
	sealed class ExpressionUtils
	{
		/// <summary>指定された <see cref="System.Linq.Expressions.Expression"/> が指定された型でない場合のみ型変換ノードでラップする変換操作を返します。</summary>
		/// <param name="expression">変換の対象となる <see cref="System.Linq.Expressions.Expression"/> を指定します。</param>
		/// <param name="type">変換先の型を指定します。</param>
		internal static Expression Convert(Expression expression, Type type) { return expression.Type != type ? Expression.Convert(expression, type) : expression; }
	}
}
