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
	/// <summary>�u���b�N���쐬���� <see cref="Microsoft.Scripting.Ast.ExpressionCollectionBuilder"/> ��\���܂��B</summary>
	public sealed class BlockBuilder : ExpressionCollectionBuilder<Expression>
	{
		/// <summary><see cref="Microsoft.Scripting.Ast.BlockBuilder"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		public BlockBuilder() { }

		/// <summary>���̃I�u�W�F�N�g�����ɕϊ����܂��B�����ǉ�����Ă��Ȃ��ꍇ�� <c>null</c>�A1 �ǉ�����Ă���ꍇ�͂��̎��A����ȊO�̏ꍇ�̓u���b�N��Ԃ��܂��B</summary>
		/// <returns>���̃I�u�W�F�N�g�ɑ΂��� <see cref="System.Linq.Expressions.Expression"/> �I�u�W�F�N�g�B</returns>
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

		/// <summary>�w�肳�ꂽ <see cref="BlockBuilder"/> �����ɕϊ����܂��B</summary>
		/// <param name="block">�ϊ����� <see cref="BlockBuilder"/> ���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ <see cref="BlockBuilder"/> �ɑ΂��� <see cref="System.Linq.Expressions.Expression"/> �I�u�W�F�N�g�B</returns>
		public static implicit operator Expression(BlockBuilder/*!*/ block) { return block.ToExpression(); }
	}
}
