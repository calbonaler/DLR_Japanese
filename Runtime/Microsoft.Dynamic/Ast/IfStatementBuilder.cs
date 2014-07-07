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

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Ast
{
	/// <summary>If ステートメントを自然な構文で構築できるビルダーを提供します。</summary>
	public sealed class IfStatementBuilder
	{
		readonly List<IfStatementTest> _clauses = new List<IfStatementTest>();

		/// <summary><see cref="Microsoft.Scripting.Ast.IfStatementBuilder"/> クラスの新しいインスタンスを初期化します。</summary>
		internal IfStatementBuilder() { }

		/// <summary>このビルダーに新しい ElseIf 句を追加します。</summary>
		/// <param name="test"><paramref name="body"/> が実行される条件を指定します。</param>
		/// <param name="body">実行される式を指定します。</param>
		/// <returns>新しい ElseIf 句が追加されたこのビルダー。</returns>
		public IfStatementBuilder ElseIf(Expression test, params Expression[] body)
		{
			ContractUtils.RequiresNotNull(test, "test");
			ContractUtils.Requires(test.Type == typeof(bool), "test");
			ContractUtils.RequiresNotNullItems(body, "body");
			_clauses.Add(Utils.IfCondition(test, body.Length == 1 ? body[0] : Utils.Block(body)));
			return this;
		}

		/// <summary>このビルダーに Else 句を追加して、等価な <see cref="Expression"/> を返します。</summary>
		/// <param name="body">どの条件にも一致しなかった場合に実行される式を指定します。</param>
		/// <returns>Else 句が追加された時点のビルダーの状態と等価な <see cref="Expression"/>。</returns>
		public Expression Else(params Expression[] body)
		{
			ContractUtils.RequiresNotNullItems(body, "body");
			return BuildConditions(_clauses, body.Length == 1 ? body[0] : Utils.Block(body));
		}

		/// <summary>指定された一連の <see cref="IfStatementTest"/> および Else 句から If-Then-Else を表す <see cref="Expression"/> を作成します。</summary>
		/// <param name="clauses">条件および実行される式を表す一連の <see cref="IfStatementTest"/> を指定します。</param>
		/// <param name="else">どの条件にも一致しなかった場合に実行される式を指定します。</param>
		/// <returns>If-Then-Else を表す <see cref="Expression"/>。</returns>
		internal static Expression BuildConditions(IEnumerable<IfStatementTest> clauses, Expression @else)
		{
			// 多くの "else" がある場合はスタックオーバーフローを避けるために SwitchExpression を使用するべきかもしれない
			return clauses.Reverse().Aggregate(@else ?? Utils.Empty(), (x, y) => Expression.IfThenElse(y.Test, y.Body, x));
		}

		/// <summary>現在のビルダーの状態と等価な <see cref="Expression"/> を返します。</summary>
		/// <returns>現在のビルダーの状態と等価な <see cref="Expression"/>。</returns>
		public Expression ToStatement() { return BuildConditions(_clauses, null); }

		/// <summary>ビルダーを現在の状態と等価な <see cref="Expression"/> に変換します。</summary>
		/// <param name="builder">変換元のビルダー。</param>
		/// <returns>変換元のビルダーの状態と等価な <see cref="Expression"/>。</returns>
		public static implicit operator Expression(IfStatementBuilder builder)
		{
			ContractUtils.RequiresNotNull(builder, "builder");
			return builder.ToStatement();
		}
	}

	public partial class Utils
	{
		/// <summary>新しい空の <see cref="IfStatementBuilder"/> を返します。</summary>
		/// <returns>新しい空の <see cref="IfStatementBuilder"/>。</returns>
		public static IfStatementBuilder If() { return new IfStatementBuilder(); }

		/// <summary>指定された条件および本体を追加された新しい <see cref="IfStatementBuilder"/> を返します。</summary>
		/// <param name="test">追加する条件を指定します。</param>
		/// <param name="body">条件が真の場合に実行される式を指定します。</param>
		/// <returns>指定された条件および本体を追加された新しい <see cref="IfStatementBuilder"/>。</returns>
		public static IfStatementBuilder If(Expression test, params Expression[] body) { return new IfStatementBuilder().ElseIf(test, body); }

		/// <summary>指定された一連の <see cref="IfStatementTest"/> および Else 句から If-Then-Else を表す <see cref="Expression"/> を作成します。</summary>
		/// <param name="tests">条件および実行される式を表す一連の <see cref="IfStatementTest"/> を指定します。</param>
		/// <param name="else">どの条件にも一致しなかった場合に実行される式を指定します。</param>
		/// <returns>If-Then-Else を表す <see cref="Expression"/>。</returns>
		public static Expression If(IEnumerable<IfStatementTest> tests, Expression @else)
		{
			ContractUtils.RequiresNotNullItems(tests, "tests");
			return IfStatementBuilder.BuildConditions(tests, @else);
		}

		/// <summary>指定された条件式と本体を使用して、If-Then を表す <see cref="Expression"/> を作成します。</summary>
		/// <param name="test">本体を実行する条件を指定します。</param>
		/// <param name="body">条件が真の場合に実行される式を指定します。</param>
		/// <returns>If-Then を表す <see cref="Expression"/>。</returns>
		public static Expression IfThen(Expression test, params Expression[] body) { return IfThenElse(test, body.Length == 1 ? body[0] : Utils.Block(body), null); }

		/// <summary>指定された条件式と本体、Else 句を使用して、If-Then-Else を表す <see cref="Expression"/> を作成します。</summary>
		/// <param name="test">本体を実行する条件を指定します。</param>
		/// <param name="body">条件が真の場合に実行される式を指定します。</param>
		/// <param name="else">条件が偽の場合に実行される式を指定します。</param>
		/// <returns>If-Then-Else を表す <see cref="Expression"/>。</returns>
		public static Expression IfThenElse(Expression test, Expression body, Expression @else) { return If(new[] { IfCondition(test, body) }, @else); }

		/// <summary>指定された条件が成立しない場合に式が実行される <see cref="Expression"/> を作成します。</summary>
		/// <param name="test">判断する条件を指定します。</param>
		/// <param name="body">条件が偽の場合に実行される式を指定します。</param>
		/// <returns>条件が成立しない場合に式が実行される <see cref="Expression"/>。</returns>
		public static Expression Unless(Expression test, Expression body) { return IfThenElse(test, Utils.Empty(), body); }
	}
}
