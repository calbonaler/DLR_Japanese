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
using System;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using System.Reflection;

namespace Microsoft.Scripting.Ast
{
	public static partial class Utils
	{
		/// <summary>指定された式を <see cref="System.Void"/> 型に変換します。</summary>
		/// <param name="expression"><see cref="System.Void"/> 型に変換する <see cref="Expression"/> を指定します。</param>
		/// <returns><see cref="System.Void"/> 型に変換された <see cref="Expression"/>。</returns>
		public static Expression Void(Expression expression)
		{
			ContractUtils.RequiresNotNull(expression, "expression");
			return expression.Type == typeof(void) ? expression : Expression.Block(expression, Empty());
		}

		/// <summary>
		/// 指定された式を指定された型に変換します。
		/// このメソッドは <see cref="System.Void"/> 型に関する変換もサポートします。
		/// </summary>
		/// <param name="expression">指定された型に変換する式を指定します。</param>
		/// <param name="type">式の変換先の型を指定します。</param>
		/// <returns>指定された型に変換された式。</returns>
		public static Expression Convert(Expression expression, Type type)
		{
			ContractUtils.RequiresNotNull(expression, "expression");
			if (expression.Type == type)
				return expression;
			if (expression.Type == typeof(void))
				return Expression.Block(expression, Default(type));
			if (type == typeof(void))
				return Void(expression);
			// TODO: これは正しいレベルではありません。言語が本当にこの動作が必要な場合は言語に追い出すべきです。
			if (type == typeof(object))
				return Box(expression);
			return Expression.Convert(expression, type);
		}

		/// <summary>
		/// 指定された式をボックス化した式を返します。
		/// <see cref="System.Int32"/> および <see cref="System.Boolean"/> 型に対してはキャッシュが適用されます。
		/// </summary>
		/// <param name="expression">ボックス化する式を指定します。</param>
		/// <returns>ボックス化された式。</returns>
		public static Expression Box(Expression expression)
		{
			MethodInfo m;
			if (expression.Type == typeof(int))
				m = ScriptingRuntimeHelpers.Int32ToObjectMethod;
			else if (expression.Type == typeof(bool))
				m = ScriptingRuntimeHelpers.BooleanToObjectMethod;
			else
				m = null;
			return Expression.Convert(expression, typeof(object), m);
		}
	}
}
