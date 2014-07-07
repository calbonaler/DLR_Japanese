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
	/// <summary>If �X�e�[�g�����g�����R�ȍ\���ō\�z�ł���r���_�[��񋟂��܂��B</summary>
	public sealed class IfStatementBuilder
	{
		readonly List<IfStatementTest> _clauses = new List<IfStatementTest>();

		/// <summary><see cref="Microsoft.Scripting.Ast.IfStatementBuilder"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		internal IfStatementBuilder() { }

		/// <summary>���̃r���_�[�ɐV���� ElseIf ���ǉ����܂��B</summary>
		/// <param name="test"><paramref name="body"/> �����s�����������w�肵�܂��B</param>
		/// <param name="body">���s����鎮���w�肵�܂��B</param>
		/// <returns>�V���� ElseIf �傪�ǉ����ꂽ���̃r���_�[�B</returns>
		public IfStatementBuilder ElseIf(Expression test, params Expression[] body)
		{
			ContractUtils.RequiresNotNull(test, "test");
			ContractUtils.Requires(test.Type == typeof(bool), "test");
			ContractUtils.RequiresNotNullItems(body, "body");
			_clauses.Add(Utils.IfCondition(test, body.Length == 1 ? body[0] : Utils.Block(body)));
			return this;
		}

		/// <summary>���̃r���_�[�� Else ���ǉ����āA������ <see cref="Expression"/> ��Ԃ��܂��B</summary>
		/// <param name="body">�ǂ̏����ɂ���v���Ȃ������ꍇ�Ɏ��s����鎮���w�肵�܂��B</param>
		/// <returns>Else �傪�ǉ����ꂽ���_�̃r���_�[�̏�ԂƓ����� <see cref="Expression"/>�B</returns>
		public Expression Else(params Expression[] body)
		{
			ContractUtils.RequiresNotNullItems(body, "body");
			return BuildConditions(_clauses, body.Length == 1 ? body[0] : Utils.Block(body));
		}

		/// <summary>�w�肳�ꂽ��A�� <see cref="IfStatementTest"/> ����� Else �傩�� If-Then-Else ��\�� <see cref="Expression"/> ���쐬���܂��B</summary>
		/// <param name="clauses">��������ю��s����鎮��\����A�� <see cref="IfStatementTest"/> ���w�肵�܂��B</param>
		/// <param name="else">�ǂ̏����ɂ���v���Ȃ������ꍇ�Ɏ��s����鎮���w�肵�܂��B</param>
		/// <returns>If-Then-Else ��\�� <see cref="Expression"/>�B</returns>
		internal static Expression BuildConditions(IEnumerable<IfStatementTest> clauses, Expression @else)
		{
			// ������ "else" ������ꍇ�̓X�^�b�N�I�[�o�[�t���[������邽�߂� SwitchExpression ���g�p����ׂ���������Ȃ�
			return clauses.Reverse().Aggregate(@else ?? Utils.Empty(), (x, y) => Expression.IfThenElse(y.Test, y.Body, x));
		}

		/// <summary>���݂̃r���_�[�̏�ԂƓ����� <see cref="Expression"/> ��Ԃ��܂��B</summary>
		/// <returns>���݂̃r���_�[�̏�ԂƓ����� <see cref="Expression"/>�B</returns>
		public Expression ToStatement() { return BuildConditions(_clauses, null); }

		/// <summary>�r���_�[�����݂̏�ԂƓ����� <see cref="Expression"/> �ɕϊ����܂��B</summary>
		/// <param name="builder">�ϊ����̃r���_�[�B</param>
		/// <returns>�ϊ����̃r���_�[�̏�ԂƓ����� <see cref="Expression"/>�B</returns>
		public static implicit operator Expression(IfStatementBuilder builder)
		{
			ContractUtils.RequiresNotNull(builder, "builder");
			return builder.ToStatement();
		}
	}

	public partial class Utils
	{
		/// <summary>�V������� <see cref="IfStatementBuilder"/> ��Ԃ��܂��B</summary>
		/// <returns>�V������� <see cref="IfStatementBuilder"/>�B</returns>
		public static IfStatementBuilder If() { return new IfStatementBuilder(); }

		/// <summary>�w�肳�ꂽ��������і{�̂�ǉ����ꂽ�V���� <see cref="IfStatementBuilder"/> ��Ԃ��܂��B</summary>
		/// <param name="test">�ǉ�����������w�肵�܂��B</param>
		/// <param name="body">�������^�̏ꍇ�Ɏ��s����鎮���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ��������і{�̂�ǉ����ꂽ�V���� <see cref="IfStatementBuilder"/>�B</returns>
		public static IfStatementBuilder If(Expression test, params Expression[] body) { return new IfStatementBuilder().ElseIf(test, body); }

		/// <summary>�w�肳�ꂽ��A�� <see cref="IfStatementTest"/> ����� Else �傩�� If-Then-Else ��\�� <see cref="Expression"/> ���쐬���܂��B</summary>
		/// <param name="tests">��������ю��s����鎮��\����A�� <see cref="IfStatementTest"/> ���w�肵�܂��B</param>
		/// <param name="else">�ǂ̏����ɂ���v���Ȃ������ꍇ�Ɏ��s����鎮���w�肵�܂��B</param>
		/// <returns>If-Then-Else ��\�� <see cref="Expression"/>�B</returns>
		public static Expression If(IEnumerable<IfStatementTest> tests, Expression @else)
		{
			ContractUtils.RequiresNotNullItems(tests, "tests");
			return IfStatementBuilder.BuildConditions(tests, @else);
		}

		/// <summary>�w�肳�ꂽ�������Ɩ{�̂��g�p���āAIf-Then ��\�� <see cref="Expression"/> ���쐬���܂��B</summary>
		/// <param name="test">�{�̂����s����������w�肵�܂��B</param>
		/// <param name="body">�������^�̏ꍇ�Ɏ��s����鎮���w�肵�܂��B</param>
		/// <returns>If-Then ��\�� <see cref="Expression"/>�B</returns>
		public static Expression IfThen(Expression test, params Expression[] body) { return IfThenElse(test, body.Length == 1 ? body[0] : Utils.Block(body), null); }

		/// <summary>�w�肳�ꂽ�������Ɩ{�́AElse ����g�p���āAIf-Then-Else ��\�� <see cref="Expression"/> ���쐬���܂��B</summary>
		/// <param name="test">�{�̂����s����������w�肵�܂��B</param>
		/// <param name="body">�������^�̏ꍇ�Ɏ��s����鎮���w�肵�܂��B</param>
		/// <param name="else">�������U�̏ꍇ�Ɏ��s����鎮���w�肵�܂��B</param>
		/// <returns>If-Then-Else ��\�� <see cref="Expression"/>�B</returns>
		public static Expression IfThenElse(Expression test, Expression body, Expression @else) { return If(new[] { IfCondition(test, body) }, @else); }

		/// <summary>�w�肳�ꂽ�������������Ȃ��ꍇ�Ɏ������s����� <see cref="Expression"/> ���쐬���܂��B</summary>
		/// <param name="test">���f����������w�肵�܂��B</param>
		/// <param name="body">�������U�̏ꍇ�Ɏ��s����鎮���w�肵�܂��B</param>
		/// <returns>�������������Ȃ��ꍇ�Ɏ������s����� <see cref="Expression"/>�B</returns>
		public static Expression Unless(Expression test, Expression body) { return IfThenElse(test, Utils.Empty(), body); }
	}
}
