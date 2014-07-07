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
using System;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using System.Reflection;

namespace Microsoft.Scripting.Ast
{
	public static partial class Utils
	{
		/// <summary>�w�肳�ꂽ���� <see cref="System.Void"/> �^�ɕϊ����܂��B</summary>
		/// <param name="expression"><see cref="System.Void"/> �^�ɕϊ����� <see cref="Expression"/> ���w�肵�܂��B</param>
		/// <returns><see cref="System.Void"/> �^�ɕϊ����ꂽ <see cref="Expression"/>�B</returns>
		public static Expression Void(Expression expression)
		{
			ContractUtils.RequiresNotNull(expression, "expression");
			return expression.Type == typeof(void) ? expression : Expression.Block(expression, Empty());
		}

		/// <summary>
		/// �w�肳�ꂽ�����w�肳�ꂽ�^�ɕϊ����܂��B
		/// ���̃��\�b�h�� <see cref="System.Void"/> �^�Ɋւ���ϊ����T�|�[�g���܂��B
		/// </summary>
		/// <param name="expression">�w�肳�ꂽ�^�ɕϊ����鎮���w�肵�܂��B</param>
		/// <param name="type">���̕ϊ���̌^���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�^�ɕϊ����ꂽ���B</returns>
		public static Expression Convert(Expression expression, Type type)
		{
			ContractUtils.RequiresNotNull(expression, "expression");
			if (expression.Type == type)
				return expression;
			if (expression.Type == typeof(void))
				return Expression.Block(expression, Default(type));
			if (type == typeof(void))
				return Void(expression);
			// TODO: ����͐��������x���ł͂���܂���B���ꂪ�{���ɂ��̓��삪�K�v�ȏꍇ�͌���ɒǂ��o���ׂ��ł��B
			if (type == typeof(object))
				return Box(expression);
			return Expression.Convert(expression, type);
		}

		/// <summary>
		/// �w�肳�ꂽ�����{�b�N�X����������Ԃ��܂��B
		/// <see cref="System.Int32"/> ����� <see cref="System.Boolean"/> �^�ɑ΂��Ă̓L���b�V�����K�p����܂��B
		/// </summary>
		/// <param name="expression">�{�b�N�X�����鎮���w�肵�܂��B</param>
		/// <returns>�{�b�N�X�����ꂽ���B</returns>
		public static Expression Box(Expression expression)
		{
			MethodInfo m;
			if (expression.Type == typeof(int))
				m = ScriptingRuntimeHelpers.Int32ToObjectMethod;
			else if (expression.Type == typeof(bool))
				m = ScriptingRuntimeHelpers.BooleanToObjectMethod;
			else
				m = null;
			return Expression.Convert(expression, typeof(object), m);
		}
	}
}
