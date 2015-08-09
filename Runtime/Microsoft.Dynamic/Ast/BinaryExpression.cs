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
using System.Reflection;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Ast
{
	public static partial class Utils
	{
		/// <summary>
		/// <c>null</c> ��������\�� <see cref="ConditionalExpression"/> ���쐬���܂��BC# �ɂ����� "??" ���Z�q�Ɠ����ł��B
		/// <code>return (temp = left) == null ? right : temp;</code>
		/// </summary>
		/// <param name="left">null �������̍��ӂ��w�肵�܂��B</param>
		/// <param name="right">null �������̉E�ӂ��w�肵�܂��B</param>
		/// <param name="temp">�ꎞ�ϐ���\�� <see cref="ParameterExpression"/> ���i�[�����ϐ����w�肵�܂��B</param>
		/// <returns><c>null</c> ��������\�� <see cref="ConditionalExpression"/>�B</returns>
		public static ConditionalExpression Coalesce(Expression left, Expression right, out ParameterExpression temp) { return CoalesceInternal(left, right, null, false, out temp); }

		/// <summary>
		/// <c>true</c> ��������\�� <see cref="ConditionalExpression"/> ���쐬���܂��B����͒Z���]�������_���ς���ʉ��������̂ł��B
		/// <code>return isTrue(temp = left) ? right : temp;</code>
		/// </summary>
		/// <param name="left"><c>true</c> �������̍��ӂ��w�肵�܂��B</param>
		/// <param name="right"><c>true</c> �������̉E�ӂ��w�肵�܂��B</param>
		/// <param name="isTrue">���ӂ��^�ł���Ɣ��f�����Ƃ��� <c>true</c> ��Ԃ� public static ���\�b�h���w�肵�܂��B</param>
		/// <param name="temp">�ꎞ�ϐ���\�� <see cref="ParameterExpression"/> ���i�[�����ϐ����w�肵�܂��B</param>
		/// <returns><c>true</c> ��������\�� <see cref="ConditionalExpression"/>�B</returns>
		public static ConditionalExpression CoalesceTrue(Expression left, Expression right, MethodInfo isTrue, out ParameterExpression temp)
		{
			ContractUtils.RequiresNotNull(isTrue, "isTrue");
			return CoalesceInternal(left, right, isTrue, false, out temp);
		}

		/// <summary>
		/// <c>false</c> ��������\�� <see cref="ConditionalExpression"/> ���쐬���܂��B����͒Z���]�������_���a����ʉ��������̂ł��B
		/// <code>return isTrue(temp = left) ? temp : right;</code>
		/// </summary>
		/// <param name="left"><c>false</c> �������̍��ӂ��w�肵�܂��B</param>
		/// <param name="right"><c>false</c> �������̉E�ӂ��w�肵�܂��B</param>
		/// <param name="isTrue">���ӂ��^�ł���Ɣ��f�����Ƃ��� <c>true</c> ��Ԃ� public static ���\�b�h���w�肵�܂��B</param>
		/// <param name="temp">�ꎞ�ϐ���\�� <see cref="ParameterExpression"/> ���i�[�����ϐ����w�肵�܂��B</param>
		/// <returns><c>false</c> ��������\�� <see cref="ConditionalExpression"/>�B</returns>
		public static ConditionalExpression CoalesceFalse(Expression left, Expression right, MethodInfo isTrue, out ParameterExpression temp)
		{
			ContractUtils.RequiresNotNull(isTrue, "isTrue");
			return CoalesceInternal(left, right, isTrue, true, out temp);
		}

		static ConditionalExpression CoalesceInternal(Expression left, Expression right, MethodInfo isTrue, bool isReverse, out ParameterExpression temp)
		{
			ContractUtils.RequiresNotNull(left, "left");
			ContractUtils.RequiresNotNull(right, "right");
			// A bit too strict, but on a safe side.
			ContractUtils.Requires(left.Type == right.Type, "���̌^�͈�v����K�v������܂��B");
			temp = Expression.Variable(left.Type, "tmp_left");
			Expression condition;
			if (isTrue != null)
			{
				ContractUtils.Requires(isTrue.ReturnType == typeof(bool), "isTrue", "�q��͐^�U�l��Ԃ��K�v������܂��B");
				ParameterInfo[] parameters = isTrue.GetParameters();
				ContractUtils.Requires(parameters.Length == 1, "isTrue", "�q��� 1 �̈������Ƃ�K�v������܂��B");
				ContractUtils.Requires(isTrue.IsStatic && isTrue.IsPublic, "isTrue", "�q��͌��J����Ă���ÓI���\�b�h�ł���K�v������܂��B");
				ContractUtils.Requires(TypeUtils.CanAssign(parameters[0].ParameterType, left.Type), "left", "���ӂ̌^������������܂���B");
				condition = Expression.Call(isTrue, Expression.Assign(temp, left));
			}
			else
			{
				ContractUtils.Requires(!left.Type.IsValueType, "left", "���ӂ̌^������������܂���B");
				condition = Expression.Equal(Expression.Assign(temp, left), AstUtils.Constant(null, left.Type));
			}
			if (isReverse)
				return Expression.Condition(condition, temp, right);
			else
				return Expression.Condition(condition, right, temp);
		}

		/// <summary>
		/// <c>null</c> ��������\�� <see cref="ConditionalExpression"/> ���쐬���܂��BC# �ɂ����� "??" ���Z�q�Ɠ����ł��B
		/// <code>return (temp = left) == null ? right : temp;</code>
		/// </summary>
		/// <param name="builder">�ꎞ�ϐ��̃X�R�[�v���܂� <see cref="LambdaBuilder"/> ���w�肵�܂��B</param>
		/// <param name="left">null �������̍��ӂ��w�肵�܂��B</param>
		/// <param name="right">null �������̉E�ӂ��w�肵�܂��B</param>
		/// <returns><c>null</c> ��������\�� <see cref="ConditionalExpression"/>�B</returns>
		public static ConditionalExpression Coalesce(LambdaBuilder builder, Expression left, Expression right)
		{
			ParameterExpression temp;
			var result = Coalesce(left, right, out temp);
			builder.AddHiddenVariable(temp);
			return result;
		}

		/// <summary>
		/// <c>true</c> ��������\�� <see cref="ConditionalExpression"/> ���쐬���܂��B����͒Z���]�������_���ς���ʉ��������̂ł��B
		/// <code>return isTrue(temp = left) ? right : temp;</code>
		/// </summary>
		/// <param name="builder">�ꎞ�ϐ��̃X�R�[�v���܂� <see cref="LambdaBuilder"/> ���w�肵�܂��B</param>
		/// <param name="left"><c>true</c> �������̍��ӂ��w�肵�܂��B</param>
		/// <param name="right"><c>true</c> �������̉E�ӂ��w�肵�܂��B</param>
		/// <param name="isTrue">���ӂ��^�ł���Ɣ��f�����Ƃ��� <c>true</c> ��Ԃ� public static ���\�b�h���w�肵�܂��B</param>
		/// <returns><c>true</c> ��������\�� <see cref="ConditionalExpression"/>�B</returns>
		public static ConditionalExpression CoalesceTrue(LambdaBuilder builder, Expression left, Expression right, MethodInfo isTrue)
		{
			ContractUtils.RequiresNotNull(isTrue, "isTrue");
			ParameterExpression temp;
			var result = CoalesceTrue(left, right, isTrue, out temp);
			builder.AddHiddenVariable(temp);
			return result;
		}

		/// <summary>
		/// <c>false</c> ��������\�� <see cref="ConditionalExpression"/> ���쐬���܂��B����͒Z���]�������_���a����ʉ��������̂ł��B
		/// <code>return isTrue(temp = left) ? temp : right;</code>
		/// </summary>
		/// <param name="builder">�ꎞ�ϐ��̃X�R�[�v���܂� <see cref="LambdaBuilder"/> ���w�肵�܂��B</param>
		/// <param name="left"><c>false</c> �������̍��ӂ��w�肵�܂��B</param>
		/// <param name="right"><c>false</c> �������̉E�ӂ��w�肵�܂��B</param>
		/// <param name="isTrue">���ӂ��^�ł���Ɣ��f�����Ƃ��� <c>true</c> ��Ԃ� public static ���\�b�h���w�肵�܂��B</param>
		/// <returns><c>false</c> ��������\�� <see cref="ConditionalExpression"/>�B</returns>
		public static ConditionalExpression CoalesceFalse(LambdaBuilder builder, Expression left, Expression right, MethodInfo isTrue)
		{
			ContractUtils.RequiresNotNull(isTrue, "isTrue");
			ParameterExpression temp;
			var result = CoalesceFalse(left, right, isTrue, out temp);
			builder.AddHiddenVariable(temp);
			return result;
		}

		/// <summary>�w�肳�ꂽ <see cref="BinaryExpression"/> �Ɏ����V���������쐬���܂����A���ӂ���щE�ӂ̂ݎw�肳�ꂽ�����g�p���܂��B</summary>
		/// <param name="expression">���̍쐬���� <see cref="BinaryExpression"/> ���w�肵�܂��B</param>
		/// <param name="left">�쐬����鎮�̍��ӂ��w�肵�܂��B</param>
		/// <param name="right">�쐬����鎮�̉E�ӂ��w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�q������ <see cref="BinaryExpression"/>�B���ׂĂ̎q�������ꍇ�͂��̎w�肳�ꂽ�����Ԃ���܂��B</returns>
		public static BinaryExpression Update(this BinaryExpression expression, Expression left, Expression right) { return expression.Update(left, expression.Conversion, right); }
	}
}
