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
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.Scripting.Ast
{
	/// <summary><see cref="Expression"/> �^�̃R���N�V�������\�z������@��񋟂��܂��B</summary>
	/// <typeparam name="TExpression">�r���_�[�ɒǉ��ł���v�f�̌^���w�肵�܂��B</typeparam>
	public class ExpressionCollectionBuilder<TExpression> : IEnumerable<TExpression> where TExpression : Expression
	{
		/// <summary>���̃r���_�[�� 1 �Ԗڂ̗v�f���擾���܂��B</summary>
		public TExpression Expression0 { get; private set; }

		/// <summary>���̃r���_�[�� 2 �Ԗڂ̗v�f���擾���܂��B</summary>
		public TExpression Expression1 { get; private set; }

		/// <summary>���̃r���_�[�� 3 �Ԗڂ̗v�f���擾���܂��B</summary>
		public TExpression Expression2 { get; private set; }

		/// <summary>���̃r���_�[�� 4 �Ԗڂ̗v�f���擾���܂��B</summary>
		public TExpression Expression3 { get; private set; }

		/// <summary><see cref="Microsoft.Scripting.Ast.ExpressionCollectionBuilder&lt;TExpression&gt;"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		public ExpressionCollectionBuilder() { }

		/// <summary>���̃r���_�[�ɒǉ����ꂽ�v�f�̐����擾���܂��B</summary>
		public int Count { get; private set; }

		/// <summary>
		/// �r���_�[�ɒǉ����ꂽ�v�f�̐��� 5 �ȏ�ł���΂��ׂĂ̗v�f���܂� <see cref="ReadOnlyCollectionBuilder&lt;TExpression&gt;"/> ���擾���܂��B
		/// ����ȊO�̏ꍇ�� <c>null</c> ��Ԃ��܂��B
		/// </summary>
		public ReadOnlyCollectionBuilder<TExpression> Expressions { get; private set; }

		/// <summary>���̃r���_�[�Ɏw�肳�ꂽ����ǉ����܂��B</summary>
		/// <param name="expressions">���̃r���_�[�ɒǉ����鎮���w�肵�܂��B</param>
		public void Add(IEnumerable<TExpression> expressions)
		{
			if (expressions != null)
			{
				foreach (var expression in expressions)
					Add(expression);
			}
		}

		/// <summary>���̃r���_�[�Ɏw�肳�ꂽ����ǉ����܂��B</summary>
		/// <param name="expression">���̃r���_�[�ɒǉ����鎮���w�肵�܂��B</param>
		public void Add(TExpression expression)
		{
			if (expression == null)
				return;
			switch (Count)
			{
				case 0: Expression0 = expression; break;
				case 1: Expression1 = expression; break;
				case 2: Expression2 = expression; break;
				case 3: Expression3 = expression; break;
				case 4:
					Expressions = new ReadOnlyCollectionBuilder<TExpression> { Expression0, Expression1, Expression2, Expression3, expression };
					break;
				default:
					Expressions.Add(expression);
					break;
			}
			Count++;
		}

		IEnumerator<TExpression>/*!*/ GetItemEnumerator()
		{
			if (Count > 0)
				yield return Expression0;
			if (Count > 1)
				yield return Expression1;
			if (Count > 2)
				yield return Expression2;
			if (Count > 3)
				yield return Expression3;
		}

		/// <summary>���̃R���N�V�����𔽕���������񋓎q���擾���܂��B</summary>
		/// <returns>�R���N�V�����𔽕���������񋓎q�B</returns>
		public IEnumerator<TExpression>/*!*/ GetEnumerator() { return Expressions != null ? Expressions.GetEnumerator() : GetItemEnumerator(); }

		/// <summary>���̃R���N�V�����𔽕���������񋓎q���擾���܂��B</summary>
		/// <returns>�R���N�V�����𔽕���������񋓎q�B</returns>
		System.Collections.IEnumerator/*!*/ System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}

	/// <summary>���\�b�h�Ăяo���̈������\�z������@��񋟂��܂��B</summary>
	public class ExpressionCollectionBuilder : ExpressionCollectionBuilder<Expression>
	{
		/// <summary>���̃r���_�[�Ɋ܂܂�Ă���������g�p���āA�w�肳�ꂽ�C���X�^���X�Ń��\�b�h���Ăяo������Ԃ��܂��B</summary>
		/// <param name="instance">�w�肳�ꂽ���\�b�h���Ăяo���C���X�^���X���w�肵�܂��B<c>null</c> ���w�肷��ƐÓI���\�b�h�̌Ăяo���ɂȂ�܂��B</param>
		/// <param name="method">�Ăяo�����\�b�h���w�肵�܂��B</param>
		/// <returns>���̃r���_�[�Ɋ܂܂�Ă���������g�p���ă��\�b�h���Ăяo�����B</returns>
		public Expression/*!*/ ToMethodCall(Expression instance, MethodInfo/*!*/ method)
		{
			switch (Count)
			{
				case 0:
					return Expression.Call(instance, method);
				case 1:
					// we have no specialized subclass for instance method call expression with 1 arg:
					return instance != null ?
						Expression.Call(instance, method, new[] { Expression0 }) :
						Expression.Call(method, Expression0);
				case 2:
					return Expression.Call(instance, method, Expression0, Expression1);
				case 3:
					return Expression.Call(instance, method, Expression0, Expression1, Expression2);
				case 4:
					// we have no specialized subclass for instance method call expression with 4 args:
					return instance != null ?
						Expression.Call(instance, method, new[] { Expression0, Expression1, Expression2, Expression3 }) :
						Expression.Call(method, Expression0, Expression1, Expression2, Expression3);
				default:
					return Expression.Call(instance, method, Expressions);
			}
		}
	}
}
