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
		/// <summary>�w�肳�ꂽ�������A�{�́AElse ����g�p���� While ���[�v��\�� <see cref="Expression"/> �쐬���܂��B</summary>
		/// <param name="test">�����������{�̂����s�����������w�肵�܂��B<c>null</c> ���w�肷��Ɩ������[�v�ɂȂ�܂��B</param>
		/// <param name="body">���[�v�̖{�̂��w�肵�܂��B</param>
		/// <param name="else">�������s�����ɂȂ����Ƃ��� 1 �񂾂����s����鎮���w�肵�܂��B���̈����ɂ� <c>null</c> ���w��ł��܂��B</param>
		/// <returns>While ���[�v��\�� <see cref="Expression"/>�B</returns>
		public static LoopExpression While(Expression test, Expression body, Expression @else) { return Loop(test, null, body, @else, null, null); }

		/// <summary>�w�肳�ꂽ�������A�{�́AElse ����g�p���� While ���[�v��\�� <see cref="Expression"/> �쐬���܂��B</summary>
		/// <param name="test">�����������{�̂����s�����������w�肵�܂��B<c>null</c> ���w�肷��Ɩ������[�v�ɂȂ�܂��B</param>
		/// <param name="body">���[�v�̖{�̂��w�肵�܂��B</param>
		/// <param name="else">�������s�����ɂȂ����Ƃ��� 1 �񂾂����s����鎮���w�肵�܂��B���̈����ɂ� <c>null</c> ���w��ł��܂��B</param>
		/// <param name="break">���[�v�̖{�̂ɂ���Ďg�p����� break �̈ړ�����w�肵�܂��B</param>
		/// <param name="continue">���[�v�̖{�̂ɂ���Ďg�p����� continue �̈ړ�����w�肵�܂��B</param>
		/// <returns>While ���[�v��\�� <see cref="Expression"/>�B</returns>
		public static LoopExpression While(Expression test, Expression body, Expression @else, LabelTarget @break, LabelTarget @continue) { return Loop(test, null, body, @else, @break, @continue); }

		/// <summary>�w�肳�ꂽ�{�̂��g�p���āA�������[�v��\�� <see cref="Expression"/> ���쐬���܂��B</summary>
		/// <param name="body">���[�v�̖{�̂��w�肵�܂��B</param>
		/// <returns>�������[�v��\�� <see cref="Expression"/>�B</returns>
		public static LoopExpression Infinite(Expression body) { return Expression.Loop(body, null, null); }

		/// <summary>�w�肳�ꂽ�{�̂��g�p���āA�������[�v��\�� <see cref="Expression"/> ���쐬���܂��B</summary>
		/// <param name="body">���[�v�̖{�̂��w�肵�܂��B</param>
		/// <param name="break">���[�v�̖{�̂ɂ���Ďg�p����� break �̈ړ�����w�肵�܂��B</param>
		/// <param name="continue">���[�v�̖{�̂ɂ���Ďg�p����� continue �̈ړ�����w�肵�܂��B</param>
		/// <returns>�������[�v��\�� <see cref="Expression"/>�B</returns>
		public static LoopExpression Infinite(Expression body, LabelTarget @break, LabelTarget @continue) { return Expression.Loop(body, @break, @continue); }

		/// <summary>�w�肳�ꂽ�����A�X�V���A�{�́Aelse ����g�p���āA���[�v��\�� <see cref="Expression"/> ���쐬���܂��B</summary>
		/// <param name="test">�����������{�̂����s�����������w�肵�܂��B<c>null</c> ���w�肷��Ɩ������[�v�ɂȂ�܂��B</param>
		/// <param name="update">���[�v�̍Ō�Ɏ��s�����X�V�����w�肵�܂��B���̈����ɂ� <c>null</c> ���w��ł��܂��B</param>
		/// <param name="body">���[�v�̖{�̂��w�肵�܂��B</param>
		/// <param name="else">�������s�����ɂȂ����Ƃ��� 1 �񂾂����s����鎮���w�肵�܂��B���̈����ɂ� <c>null</c> ���w��ł��܂��B</param>
		/// <returns>���[�v��\�� <see cref="Expression"/>�B</returns>
		public static LoopExpression Loop(Expression test, Expression update, Expression body, Expression @else) { return Loop(test, update, body, @else, null, null); }

		/// <summary>�w�肳�ꂽ�����A�X�V���A�{�́Aelse ����g�p���āA���[�v��\�� <see cref="Expression"/> ���쐬���܂��B</summary>
		/// <param name="test">�����������{�̂����s�����������w�肵�܂��B<c>null</c> ���w�肷��Ɩ������[�v�ɂȂ�܂��B</param>
		/// <param name="update">���[�v�̍Ō�Ɏ��s�����X�V�����w�肵�܂��B���̈����ɂ� <c>null</c> ���w��ł��܂��B</param>
		/// <param name="body">���[�v�̖{�̂��w�肵�܂��B</param>
		/// <param name="else">�������s�����ɂȂ����Ƃ��� 1 �񂾂����s����鎮���w�肵�܂��B���̈����ɂ� <c>null</c> ���w��ł��܂��B</param>
		/// <param name="break">���[�v�̖{�̂ɂ���Ďg�p����� break �̈ړ�����w�肵�܂��B</param>
		/// <param name="continue">���[�v�̖{�̂ɂ���Ďg�p����� continue �̈ړ�����w�肵�܂��B</param>
		/// <returns>���[�v��\�� <see cref="Expression"/>�B</returns>
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
				ContractUtils.Requires(test.Type == typeof(bool), "test", "�����͐^�U�l�ł���K�v������܂��B");
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
