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
using System.Linq.Expressions;

namespace Microsoft.Scripting.Ast
{
	//�C���^�v���^���s���̂ŁA�K�����[�h�ōs��ǐՂ���R�[�h��}������K�v�͂Ȃ��B
	// TODO: �K���R���p�C�������コ���āA������s���K�v���Ȃ��悤�ɂ��A����Ɍ��ꂩ��s�ǐՂ���菜����悤�ɂ���B
	/// <summary>�C���^�v���^�Ŏw�肳�ꂽ�R�[�h�����s����Ȃ��悤�Ƀ}�[�N���܂��B</summary>
	public sealed class SkipInterpretExpression : Expression
	{
		/// <summary>�w�肳�ꂽ�{�̂��g�p���āA<see cref="Microsoft.Scripting.Ast.SkipInterpretExpression"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="body">�C���^�v���^���������鎮���w�肵�܂��B</param>
		internal SkipInterpretExpression(Expression body)
		{
			if (body.Type != typeof(void))
				body = Expression.Block(body, Utils.Empty());
			Body = body;
		}

		/// <summary>�C���^�v���^���������鎮���擾���܂��B</summary>
		public Expression Body { get; private set; }

		/// <summary>���� <see cref="System.Linq.Expressions.Expression"/> ���\�����̐ÓI�Ȍ^���擾���܂��B</summary>
		public sealed override Type Type { get { return typeof(void); } }

		/// <summary>
		/// ���̎��̃m�[�h�^��Ԃ��܂��B
		/// �g���m�[�h�́A���̃��\�b�h���I�[�o�[���C�h����Ƃ��A<see cref="System.Linq.Expressions.ExpressionType.Extension"/> ��Ԃ��K�v������܂��B
		/// </summary>
		public sealed override ExpressionType NodeType { get { return ExpressionType.Extension; } }

		/// <summary>
		/// �m�[�h�����P���ȃm�[�h�ɕό`�ł��邱�Ƃ������܂��B
		/// ���ꂪ <c>true</c> ��Ԃ��ꍇ�A<see cref="Reduce"/> ���Ăяo���ĒP�������ꂽ�`���𐶐��ł��܂��B
		/// </summary>
		public override bool CanReduce { get { return true; } }

		/// <summary>
		/// ���̃m�[�h�����P���Ȏ��ɕό`���܂��B
		/// <see cref="CanReduce"/> �� <c>true</c> ��Ԃ��ꍇ�A����͗L���Ȏ���Ԃ��܂��B
		/// ���̃��\�b�h�́A���ꎩ�̂��P��������K�v������ʂ̃m�[�h��Ԃ��ꍇ������܂��B
		/// </summary>
		/// <returns>�P�������ꂽ���B</returns>
		public override Expression Reduce() { return Body; }

		/// <summary>
		/// �m�[�h��P�������A�P�������ꂽ���� <paramref name="visitor"/> �f���Q�[�g���Ăяo���܂��B
		/// �m�[�h��P�����ł��Ȃ��ꍇ�A���̃��\�b�h�͗�O���X���[���܂��B
		/// </summary>
		/// <param name="visitor"><see cref="System.Func&lt;T,TResult&gt;"/> �̃C���X�^���X�B</param>
		/// <returns>�������̎��A�܂��̓c���[���ő������̎��ƒu�������鎮</returns>
		protected override Expression VisitChildren(ExpressionVisitor visitor)
		{
			var body = visitor.Visit(Body);
			return body == Body ? this : new SkipInterpretExpression(body);
		}
	}

	public static partial class Utils
	{
		/// <summary>�w�肳�ꂽ�����C���^�v���^�ɂ���Ď��s����Ȃ��Ƃ��ă}�[�N���܂��B</summary>
		/// <param name="body">�C���^�v���^���������鎮���w�肵�܂��B</param>
		/// <returns>�C���^�v���^�Ŏw�肳�ꂽ�R�[�h�����s����Ȃ����Ƃ�\�� <see cref="SkipInterpretExpression"/>�B</returns>
		public static SkipInterpretExpression SkipInterpret(Expression body)
		{
			var skip = body as SkipInterpretExpression;
			return skip != null ? skip : new SkipInterpretExpression(body);
		}
	}
}
