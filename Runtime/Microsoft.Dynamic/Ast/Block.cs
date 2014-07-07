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
	public static partial class Utils
	{
		/// <summary>指定された式、変数、変数初期化子を含む <see cref="BlockExpression"/> を作成します。<paramref name="body"/> がすでにブロックの場合はまとめられます。</summary>
		/// <param name="body">変数および変数初期化子を追加する式を指定します。</param>
		/// <param name="variable">変数を指定します。</param>
		/// <param name="variableInit">変数初期化子を指定します。</param>
		/// <returns>変数を初期化する式を含む <see cref="BlockExpression"/>。</returns>
		internal static BlockExpression AddScopedVariable(Expression body, ParameterExpression variable, Expression variableInit)
		{
			List<ParameterExpression> vars = new List<ParameterExpression>();
			IReadOnlyList<Expression> exprs = new[] { body };
			var parentType = body.Type;
			// Merge blocks if the current block has only one child that is another block, the blocks to merge must have the same type.
			for (BlockExpression scope; exprs.Count == 1 && (scope = exprs[0] as BlockExpression) != null && parentType == scope.Type; exprs = scope.Expressions, parentType = scope.Type)
				vars.AddRange(scope.Variables);
			vars.Add(variable);
			return Expression.Block(vars, Enumerable.Repeat(Expression.Assign(variable, variableInit), 1).Concat(exprs));
		}

		/// <summary>指定された式を含む <c>void</c> 型のブロックを作成します。</summary>
		/// <param name="expressions">ブロックに含める式を指定します。</param>
		/// <returns><c>void</c> 型のブロックを表す <see cref="BlockExpression"/>。</returns>
		internal static BlockExpression BlockVoid(Expression[] expressions)
		{
			if (expressions.Length == 0 || expressions[expressions.Length - 1].Type != typeof(void))
				expressions = ArrayUtils.Append(expressions, Utils.Empty());
			return Expression.Block(expressions);
		}

		/// <summary>指定された式を含むブロックを作成します。式が空の場合は <c>void</c> 型の式を配置します。</summary>
		/// <param name="expressions">ブロックに含める式を指定します。</param>
		/// <returns>ブロックを表す <see cref="BlockExpression"/>。</returns>
		internal static BlockExpression Block(Expression[] expressions)
		{
			if (expressions.Length == 0)
				expressions = new[] { Utils.Empty() };
			return Expression.Block(expressions);
		}
	}
}
