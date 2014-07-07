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
		/// <summary>�w�肳�ꂽ���A�ϐ��A�ϐ��������q���܂� <see cref="BlockExpression"/> ���쐬���܂��B<paramref name="body"/> �����łɃu���b�N�̏ꍇ�͂܂Ƃ߂��܂��B</summary>
		/// <param name="body">�ϐ�����ѕϐ��������q��ǉ����鎮���w�肵�܂��B</param>
		/// <param name="variable">�ϐ����w�肵�܂��B</param>
		/// <param name="variableInit">�ϐ��������q���w�肵�܂��B</param>
		/// <returns>�ϐ������������鎮���܂� <see cref="BlockExpression"/>�B</returns>
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

		/// <summary>�w�肳�ꂽ�����܂� <c>void</c> �^�̃u���b�N���쐬���܂��B</summary>
		/// <param name="expressions">�u���b�N�Ɋ܂߂鎮���w�肵�܂��B</param>
		/// <returns><c>void</c> �^�̃u���b�N��\�� <see cref="BlockExpression"/>�B</returns>
		internal static BlockExpression BlockVoid(Expression[] expressions)
		{
			if (expressions.Length == 0 || expressions[expressions.Length - 1].Type != typeof(void))
				expressions = ArrayUtils.Append(expressions, Utils.Empty());
			return Expression.Block(expressions);
		}

		/// <summary>�w�肳�ꂽ�����܂ރu���b�N���쐬���܂��B������̏ꍇ�� <c>void</c> �^�̎���z�u���܂��B</summary>
		/// <param name="expressions">�u���b�N�Ɋ܂߂鎮���w�肵�܂��B</param>
		/// <returns>�u���b�N��\�� <see cref="BlockExpression"/>�B</returns>
		internal static BlockExpression Block(Expression[] expressions)
		{
			if (expressions.Length == 0)
				expressions = new[] { Utils.Empty() };
			return Expression.Block(expressions);
		}
	}
}
