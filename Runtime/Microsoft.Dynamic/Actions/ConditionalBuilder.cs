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

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions
{
	using Ast = Expression;

	/// <summary>
	/// false �������܂��s���ȏꍇ�Ɉ�A�̏��������\�z���܂��B
	/// ��������т��̏����ɑ΂��� true ���͒ǉ��������邱�Ƃ��ł��܂��B
	/// ���ꂼ��̌㑱�̏������͈ȑO�̏����� false ���ɂȂ�܂��B
	/// �Ō�ɏ������ł͂Ȃ��I�[�m�[�h��ǉ�����K�v������܂��B
	/// </summary>
	class ConditionalBuilder
	{
		readonly List<Expression> _conditions = new List<Expression>();
		readonly List<Expression> _bodies = new List<Expression>();
		readonly List<ParameterExpression> _variables = new List<ParameterExpression>();
		Expression _body;
		BindingRestrictions _restrictions = BindingRestrictions.Empty;

		/// <summary>�V�����������Ɩ{�̂�ǉ����܂��B�ŏ��̌Ăяo���͍ŏ�ʂ̏������ɁA�㑱�̌Ăяo���͈ȑO�̏������� false ���Ƃ��Ēǉ�����܂��B</summary>
		/// <param name="condition"><see cref="System.Boolean"/> �^�̌��ʌ^�������������w�肵�܂��B</param>
		/// <param name="body"><paramref name="condition"/> ���^�̏ꍇ�Ɏ��s����鎮���w�肵�܂��B</param>
		public void AddCondition(Expression condition, Expression body)
		{
			Assert.NotNull(condition, body);
			_conditions.Add(condition);
			_bodies.Add(body);
		}

		/// <summary>��s���邷�ׂĂ̏�������������Ȃ��ꍇ�Ɏ��s����鎮��\�� <see cref="DynamicMetaObject"/> ��ǉ����܂��B</summary>
		/// <param name="body">��s���邷�ׂĂ̏�������������Ȃ��ꍇ�Ɏ��s����鎮��ێ����� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		public void FinishCondition(DynamicMetaObject body)
		{
			_restrictions = _restrictions.Merge(body.Restrictions);
			FinishCondition(body.Expression);
		}

		/// <summary>��s���邷�ׂĂ̏�������������Ȃ��ꍇ�Ɏ��s����鎮��ǉ����܂��B</summary>
		/// <param name="body">��s���邷�ׂĂ̏�������������Ȃ��ꍇ�Ɏ��s����鎮���w�肵�܂��B</param>
		public void FinishCondition(Expression body)
		{
			if (_body != null)
				throw new InvalidOperationException();
			for (int i = _bodies.Count - 1; i >= 0; i--)
			{
				var t = _bodies[i].Type;
				if (t != body.Type)
				{
					if (t.IsSubclassOf(body.Type)) // �T�u�N���X
						t = body.Type;
					else if (!body.Type.IsSubclassOf(t)) // �݊��ł͂Ȃ����� object ��
						t = typeof(object);
				}
				body = Ast.Condition(_conditions[i], AstUtils.Convert(_bodies[i], t), AstUtils.Convert(body, t));
			}
			_body = Ast.Block(_variables, body);
		}

		/// <summary>���ʂƂ��Đ�������� <see cref="DynamicMetaObject"/> �ɑ΂��ēK�p�����o�C���f�B���O������擾�܂��͐ݒ肵�܂��B</summary>
		public BindingRestrictions Restrictions
		{
			get { return _restrictions; }
			set
			{
				ContractUtils.RequiresNotNull(value, "value");
				_restrictions = value;
			}
		}

		/// <summary>
		/// ���̏�������\�����ʂ̃��^�I�u�W�F�N�g���擾���܂��B
		/// FinishCondition ���Ăяo����Ă���K�v������܂��B
		/// </summary>
		/// <param name="types">���ʂ� <see cref="DynamicMetaObject"/> �ւ̒ǉ��̐����ێ����� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <returns>���̏�������\�� <see cref="DynamicMetaObject"/>�B</returns>
		public DynamicMetaObject GetMetaObject(params DynamicMetaObject[] types)
		{
			if (_body == null)
				throw new InvalidOperationException("FinishCondition ���Ăяo����Ă���K�v������܂��B");
			return new DynamicMetaObject(_body, BindingRestrictions.Combine(types).Merge(_restrictions));
		}

		/// <summary>�ŏI���̃��x���ɃX�R�[�v���ꂽ�ϐ���ǉ����܂��B</summary>
		/// <param name="var">���̏������ɒǉ�����ϐ����w�肵�܂��B</param>
		public void AddVariable(ParameterExpression var) { _variables.Add(var); }
	}
}
