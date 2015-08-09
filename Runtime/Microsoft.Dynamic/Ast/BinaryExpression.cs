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
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Ast
{
	public static partial class Utils
	{
		/// <summary>
		/// <c>null</c> 結合式を表す <see cref="ConditionalExpression"/> を作成します。C# における "??" 演算子と等価です。
		/// <code>return (temp = left) == null ? right : temp;</code>
		/// </summary>
		/// <param name="left">null 結合式の左辺を指定します。</param>
		/// <param name="right">null 結合式の右辺を指定します。</param>
		/// <param name="temp">一時変数を表す <see cref="ParameterExpression"/> が格納される変数を指定します。</param>
		/// <returns><c>null</c> 結合式を表す <see cref="ConditionalExpression"/>。</returns>
		public static ConditionalExpression Coalesce(Expression left, Expression right, out ParameterExpression temp) { return CoalesceInternal(left, right, null, false, out temp); }

		/// <summary>
		/// <c>true</c> 結合式を表す <see cref="ConditionalExpression"/> を作成します。これは短絡評価される論理積を一般化したものです。
		/// <code>return isTrue(temp = left) ? right : temp;</code>
		/// </summary>
		/// <param name="left"><c>true</c> 結合式の左辺を指定します。</param>
		/// <param name="right"><c>true</c> 結合式の右辺を指定します。</param>
		/// <param name="isTrue">左辺が真であると判断されるときに <c>true</c> を返す public static メソッドを指定します。</param>
		/// <param name="temp">一時変数を表す <see cref="ParameterExpression"/> が格納される変数を指定します。</param>
		/// <returns><c>true</c> 結合式を表す <see cref="ConditionalExpression"/>。</returns>
		public static ConditionalExpression CoalesceTrue(Expression left, Expression right, MethodInfo isTrue, out ParameterExpression temp)
		{
			ContractUtils.RequiresNotNull(isTrue, "isTrue");
			return CoalesceInternal(left, right, isTrue, false, out temp);
		}

		/// <summary>
		/// <c>false</c> 結合式を表す <see cref="ConditionalExpression"/> を作成します。これは短絡評価される論理和を一般化したものです。
		/// <code>return isTrue(temp = left) ? temp : right;</code>
		/// </summary>
		/// <param name="left"><c>false</c> 結合式の左辺を指定します。</param>
		/// <param name="right"><c>false</c> 結合式の右辺を指定します。</param>
		/// <param name="isTrue">左辺が真であると判断されるときに <c>true</c> を返す public static メソッドを指定します。</param>
		/// <param name="temp">一時変数を表す <see cref="ParameterExpression"/> が格納される変数を指定します。</param>
		/// <returns><c>false</c> 結合式を表す <see cref="ConditionalExpression"/>。</returns>
		public static ConditionalExpression CoalesceFalse(Expression left, Expression right, MethodInfo isTrue, out ParameterExpression temp)
		{
			ContractUtils.RequiresNotNull(isTrue, "isTrue");
			return CoalesceInternal(left, right, isTrue, true, out temp);
		}

		static ConditionalExpression CoalesceInternal(Expression left, Expression right, MethodInfo isTrue, bool isReverse, out ParameterExpression temp)
		{
			ContractUtils.RequiresNotNull(left, "left");
			ContractUtils.RequiresNotNull(right, "right");
			// A bit too strict, but on a safe side.
			ContractUtils.Requires(left.Type == right.Type, "式の型は一致する必要があります。");
			temp = Expression.Variable(left.Type, "tmp_left");
			Expression condition;
			if (isTrue != null)
			{
				ContractUtils.Requires(isTrue.ReturnType == typeof(bool), "isTrue", "述語は真偽値を返す必要があります。");
				ParameterInfo[] parameters = isTrue.GetParameters();
				ContractUtils.Requires(parameters.Length == 1, "isTrue", "述語は 1 つの引数をとる必要があります。");
				ContractUtils.Requires(isTrue.IsStatic && isTrue.IsPublic, "isTrue", "述語は公開されている静的メソッドである必要があります。");
				ContractUtils.Requires(TypeUtils.CanAssign(parameters[0].ParameterType, left.Type), "left", "左辺の型が正しくありません。");
				condition = Expression.Call(isTrue, Expression.Assign(temp, left));
			}
			else
			{
				ContractUtils.Requires(!left.Type.IsValueType, "left", "左辺の型が正しくありません。");
				condition = Expression.Equal(Expression.Assign(temp, left), AstUtils.Constant(null, left.Type));
			}
			if (isReverse)
				return Expression.Condition(condition, temp, right);
			else
				return Expression.Condition(condition, right, temp);
		}

		/// <summary>
		/// <c>null</c> 結合式を表す <see cref="ConditionalExpression"/> を作成します。C# における "??" 演算子と等価です。
		/// <code>return (temp = left) == null ? right : temp;</code>
		/// </summary>
		/// <param name="builder">一時変数のスコープを含む <see cref="LambdaBuilder"/> を指定します。</param>
		/// <param name="left">null 結合式の左辺を指定します。</param>
		/// <param name="right">null 結合式の右辺を指定します。</param>
		/// <returns><c>null</c> 結合式を表す <see cref="ConditionalExpression"/>。</returns>
		public static ConditionalExpression Coalesce(LambdaBuilder builder, Expression left, Expression right)
		{
			ParameterExpression temp;
			var result = Coalesce(left, right, out temp);
			builder.AddHiddenVariable(temp);
			return result;
		}

		/// <summary>
		/// <c>true</c> 結合式を表す <see cref="ConditionalExpression"/> を作成します。これは短絡評価される論理積を一般化したものです。
		/// <code>return isTrue(temp = left) ? right : temp;</code>
		/// </summary>
		/// <param name="builder">一時変数のスコープを含む <see cref="LambdaBuilder"/> を指定します。</param>
		/// <param name="left"><c>true</c> 結合式の左辺を指定します。</param>
		/// <param name="right"><c>true</c> 結合式の右辺を指定します。</param>
		/// <param name="isTrue">左辺が真であると判断されるときに <c>true</c> を返す public static メソッドを指定します。</param>
		/// <returns><c>true</c> 結合式を表す <see cref="ConditionalExpression"/>。</returns>
		public static ConditionalExpression CoalesceTrue(LambdaBuilder builder, Expression left, Expression right, MethodInfo isTrue)
		{
			ContractUtils.RequiresNotNull(isTrue, "isTrue");
			ParameterExpression temp;
			var result = CoalesceTrue(left, right, isTrue, out temp);
			builder.AddHiddenVariable(temp);
			return result;
		}

		/// <summary>
		/// <c>false</c> 結合式を表す <see cref="ConditionalExpression"/> を作成します。これは短絡評価される論理和を一般化したものです。
		/// <code>return isTrue(temp = left) ? temp : right;</code>
		/// </summary>
		/// <param name="builder">一時変数のスコープを含む <see cref="LambdaBuilder"/> を指定します。</param>
		/// <param name="left"><c>false</c> 結合式の左辺を指定します。</param>
		/// <param name="right"><c>false</c> 結合式の右辺を指定します。</param>
		/// <param name="isTrue">左辺が真であると判断されるときに <c>true</c> を返す public static メソッドを指定します。</param>
		/// <returns><c>false</c> 結合式を表す <see cref="ConditionalExpression"/>。</returns>
		public static ConditionalExpression CoalesceFalse(LambdaBuilder builder, Expression left, Expression right, MethodInfo isTrue)
		{
			ContractUtils.RequiresNotNull(isTrue, "isTrue");
			ParameterExpression temp;
			var result = CoalesceFalse(left, right, isTrue, out temp);
			builder.AddHiddenVariable(temp);
			return result;
		}

		/// <summary>指定された <see cref="BinaryExpression"/> に似た新しい式を作成しますが、左辺および右辺のみ指定された式を使用します。</summary>
		/// <param name="expression">式の作成元の <see cref="BinaryExpression"/> を指定します。</param>
		/// <param name="left">作成される式の左辺を指定します。</param>
		/// <param name="right">作成される式の右辺を指定します。</param>
		/// <returns>指定された子を持つ <see cref="BinaryExpression"/>。すべての子が同じ場合はこの指定された式が返されます。</returns>
		public static BinaryExpression Update(this BinaryExpression expression, Expression left, Expression right) { return expression.Update(left, expression.Conversion, right); }
	}
}
