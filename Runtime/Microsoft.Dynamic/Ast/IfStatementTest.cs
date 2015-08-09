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
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Ast
{
	/// <summary>If 文で条件とその条件が真の場合に実行される式の組を表します。</summary>
	public sealed class IfStatementTest
	{
		/// <summary>条件および式を使用して、<see cref="Microsoft.Scripting.Ast.IfStatementTest"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="test">成立または不成立を判断する条件を指定します。</param>
		/// <param name="body">条件が真の場合に実行される式を指定します。</param>
		internal IfStatementTest(Expression test, Expression body)
		{
			Test = test;
			Body = body;
		}

		/// <summary>成立または不成立を判断する条件を取得します。</summary>
		public Expression Test { get; private set; }

		/// <summary>条件が真の場合に実行される式を取得します。</summary>
		public Expression Body { get; private set; }
	}

	public partial class Utils
	{
		/// <summary>条件および式を使用して、新しい <see cref="IfStatementTest"/> を作成します。</summary>
		/// <param name="test">成立または不成立を判断する条件を指定します。</param>
		/// <param name="body">条件が真の場合に実行される式を指定します。</param>
		/// <returns>新しく作成された <see cref="IfStatementTest"/>。</returns>
		public static IfStatementTest IfCondition(Expression test, Expression body)
		{
			ContractUtils.RequiresNotNull(test, "test");
			ContractUtils.RequiresNotNull(body, "body");
			ContractUtils.Requires(test.Type == typeof(bool), "test", "条件は真偽値である必要があります。");
			return new IfStatementTest(test, body);
		}
	}
}
