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

namespace Microsoft.Scripting.Ast
{
	/// <summary>ブロックを作成する <see cref="Microsoft.Scripting.Ast.ExpressionCollectionBuilder"/> を表します。</summary>
	public sealed class BlockBuilder : ExpressionCollectionBuilder<Expression>
	{
		/// <summary><see cref="Microsoft.Scripting.Ast.BlockBuilder"/> クラスの新しいインスタンスを初期化します。</summary>
		public BlockBuilder() { }

		/// <summary>このオブジェクトを式に変換します。式が追加されていない場合は <c>null</c>、1 個追加されている場合はその式、それ以外の場合はブロックを返します。</summary>
		/// <returns>このオブジェクトに対する <see cref="System.Linq.Expressions.Expression"/> オブジェクト。</returns>
		public Expression ToExpression()
		{
			switch (Count)
			{
				case 0: return null;
				case 1: return Expression0;
				case 2: return Expression.Block(Expression0, Expression1);
				case 3: return Expression.Block(Expression0, Expression1, Expression2);
				case 4: return Expression.Block(Expression0, Expression1, Expression2, Expression3);
				default: return Expression.Block(Expressions);
			}
		}

		/// <summary>指定された <see cref="BlockBuilder"/> を式に変換します。</summary>
		/// <param name="block">変換する <see cref="BlockBuilder"/> を指定します。</param>
		/// <returns>指定された <see cref="BlockBuilder"/> に対する <see cref="System.Linq.Expressions.Expression"/> オブジェクト。</returns>
		public static implicit operator Expression(BlockBuilder/*!*/ block) { return block.ToExpression(); }
	}
}
