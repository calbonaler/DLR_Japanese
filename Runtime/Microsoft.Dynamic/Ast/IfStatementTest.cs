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
	/// <summary>If ���ŏ����Ƃ��̏������^�̏ꍇ�Ɏ��s����鎮�̑g��\���܂��B</summary>
	public sealed class IfStatementTest
	{
		/// <summary>��������ю����g�p���āA<see cref="Microsoft.Scripting.Ast.IfStatementTest"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="test">�����܂��͕s�����𔻒f����������w�肵�܂��B</param>
		/// <param name="body">�������^�̏ꍇ�Ɏ��s����鎮���w�肵�܂��B</param>
		internal IfStatementTest(Expression test, Expression body)
		{
			Test = test;
			Body = body;
		}

		/// <summary>�����܂��͕s�����𔻒f����������擾���܂��B</summary>
		public Expression Test { get; private set; }

		/// <summary>�������^�̏ꍇ�Ɏ��s����鎮���擾���܂��B</summary>
		public Expression Body { get; private set; }
	}

	public partial class Utils
	{
		/// <summary>��������ю����g�p���āA�V���� <see cref="IfStatementTest"/> ���쐬���܂��B</summary>
		/// <param name="test">�����܂��͕s�����𔻒f����������w�肵�܂��B</param>
		/// <param name="body">�������^�̏ꍇ�Ɏ��s����鎮���w�肵�܂��B</param>
		/// <returns>�V�����쐬���ꂽ <see cref="IfStatementTest"/>�B</returns>
		public static IfStatementTest IfCondition(Expression test, Expression body)
		{
			ContractUtils.RequiresNotNull(test, "test");
			ContractUtils.RequiresNotNull(body, "body");
			ContractUtils.Requires(test.Type == typeof(bool), "test", "�����͐^�U�l�ł���K�v������܂��B");
			return new IfStatementTest(test, body);
		}
	}
}
