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
	public static partial class Utils
	{
		/// <summary>指定された条件式、本体、Else 句を使用して While ループを表す <see cref="Expression"/> 作成します。</summary>
		/// <param name="test">成立する限り本体が実行される条件を指定します。<c>null</c> を指定すると無限ループになります。</param>
		/// <param name="body">ループの本体を指定します。</param>
		/// <param name="else">条件が不成立になったときに 1 回だけ実行される式を指定します。この引数には <c>null</c> を指定できます。</param>
		/// <returns>While ループを表す <see cref="Expression"/>。</returns>
		public static LoopExpression While(Expression test, Expression body, Expression @else) { return Loop(test, null, body, @else, null, null); }

		/// <summary>指定された条件式、本体、Else 句を使用して While ループを表す <see cref="Expression"/> 作成します。</summary>
		/// <param name="test">成立する限り本体が実行される条件を指定します。<c>null</c> を指定すると無限ループになります。</param>
		/// <param name="body">ループの本体を指定します。</param>
		/// <param name="else">条件が不成立になったときに 1 回だけ実行される式を指定します。この引数には <c>null</c> を指定できます。</param>
		/// <param name="break">ループの本体によって使用される break の移動先を指定します。</param>
		/// <param name="continue">ループの本体によって使用される continue の移動先を指定します。</param>
		/// <returns>While ループを表す <see cref="Expression"/>。</returns>
		public static LoopExpression While(Expression test, Expression body, Expression @else, LabelTarget @break, LabelTarget @continue) { return Loop(test, null, body, @else, @break, @continue); }

		/// <summary>指定された本体を使用して、無限ループを表す <see cref="Expression"/> を作成します。</summary>
		/// <param name="body">ループの本体を指定します。</param>
		/// <returns>無限ループを表す <see cref="Expression"/>。</returns>
		public static LoopExpression Infinite(Expression body) { return Expression.Loop(body, null, null); }

		/// <summary>指定された本体を使用して、無限ループを表す <see cref="Expression"/> を作成します。</summary>
		/// <param name="body">ループの本体を指定します。</param>
		/// <param name="break">ループの本体によって使用される break の移動先を指定します。</param>
		/// <param name="continue">ループの本体によって使用される continue の移動先を指定します。</param>
		/// <returns>無限ループを表す <see cref="Expression"/>。</returns>
		public static LoopExpression Infinite(Expression body, LabelTarget @break, LabelTarget @continue) { return Expression.Loop(body, @break, @continue); }

		/// <summary>指定された条件、更新式、本体、else 句を使用して、ループを表す <see cref="Expression"/> を作成します。</summary>
		/// <param name="test">成立する限り本体が実行される条件を指定します。<c>null</c> を指定すると無限ループになります。</param>
		/// <param name="update">ループの最後に実行される更新式を指定します。この引数には <c>null</c> を指定できます。</param>
		/// <param name="body">ループの本体を指定します。</param>
		/// <param name="else">条件が不成立になったときに 1 回だけ実行される式を指定します。この引数には <c>null</c> を指定できます。</param>
		/// <returns>ループを表す <see cref="Expression"/>。</returns>
		public static LoopExpression Loop(Expression test, Expression update, Expression body, Expression @else) { return Loop(test, update, body, @else, null, null); }

		/// <summary>指定された条件、更新式、本体、else 句を使用して、ループを表す <see cref="Expression"/> を作成します。</summary>
		/// <param name="test">成立する限り本体が実行される条件を指定します。<c>null</c> を指定すると無限ループになります。</param>
		/// <param name="update">ループの最後に実行される更新式を指定します。この引数には <c>null</c> を指定できます。</param>
		/// <param name="body">ループの本体を指定します。</param>
		/// <param name="else">条件が不成立になったときに 1 回だけ実行される式を指定します。この引数には <c>null</c> を指定できます。</param>
		/// <param name="break">ループの本体によって使用される break の移動先を指定します。</param>
		/// <param name="continue">ループの本体によって使用される continue の移動先を指定します。</param>
		/// <returns>ループを表す <see cref="Expression"/>。</returns>
		public static LoopExpression Loop(Expression test, Expression update, Expression body, Expression @else, LabelTarget @break, LabelTarget @continue)
		{
			// loop {
			//     if (test) {
			//         body
			//     } else {
			//         else;
			//         break;
			//     }
			// continue:
			//     update;
			// }
			ContractUtils.RequiresNotNull(body, "body");
			if (test != null)
			{
				ContractUtils.Requires(test.Type == typeof(bool), "test", "条件は真偽値である必要があります。");
				@break = @break ?? Expression.Label();
				body = Expression.IfThenElse(test,
					body,
					@else == null ? (Expression)Expression.Break(@break) : Expression.Block(@else, Expression.Break(@break))
				);
			}
			return update != null ?
				Expression.Loop(@continue != null ? Expression.Block(body, Expression.Label(@continue), update) : Expression.Block(body, update), @break) :
				Expression.Loop(body, @break, @continue);
		}
	}
}
