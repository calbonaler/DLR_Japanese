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
using Microsoft.Scripting.Interpreter;

namespace Microsoft.Scripting.Ast
{
	/// <summary>
	/// �c���[���̐���t���[���W�b�N�𐶐����邱�ƂŁA���b�v���ꂽ�m�[�h���̃c���[�ɂ����� finally �u���b�N����̃W�����v���\�ɂ��܂��B
	/// ���̃m�[�h�̏k�ނɂ� (�l�X�g���ꂽ�����_�ł͂Ȃ�) �{�̂̎��c���[�̒T�����K�v�ɂȂ�܂��B
	/// ������s���ȃW�����v���O���̃X�R�[�v�ւ̃W�����v�Ɖ��肷��̂ŁA���̃m�[�h�ɂ̓u���b�N�����f����W�����v���܂߂邱�Ƃ��ł��܂���B
	/// </summary>
	public sealed class FinallyFlowControlExpression : Expression, IInstructionProvider
	{
		Expression _reduced;

		/// <summary>�w�肳�ꂽ�{�̂��g�p���āA<see cref="Microsoft.Scripting.Ast.FinallyFlowControlExpression"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="body">finally �u���b�N����̃W�����v���\�ɂ���{�̂��w�肵�܂��B</param>
		internal FinallyFlowControlExpression(Expression body) { Body = body; }

		/// <summary>�m�[�h�����P���ȃm�[�h�ɕό`�ł��邱�Ƃ������܂��B</summary>
		public override bool CanReduce { get { return true; } }

		/// <summary>���� <see cref="System.Linq.Expressions.Expression"/> ���\�����̐ÓI�Ȍ^���擾���܂��B</summary>
		public sealed override Type Type { get { return Body.Type; } }

		/// <summary>���� <see cref="System.Linq.Expressions.Expression"/> �̃m�[�h�^���擾���܂��B</summary>
		public sealed override ExpressionType NodeType { get { return ExpressionType.Extension; } }

		/// <summary>finally �u���b�N����̃W�����v��L���ɂ���{�̂��擾���܂��B</summary>
		public Expression Body { get; private set; }

		/// <summary>���̃m�[�h�����P���Ȏ��ɕό`���܂��B</summary>
		/// <returns>�P�������ꂽ���B</returns>
		public override Expression Reduce() { return _reduced ?? (_reduced = new FlowControlRewriter().Reduce(Body)); }

		/// <summary>�m�[�h��P�������A�P�������ꂽ���� visitor �f���Q�[�g���Ăяo���܂��B</summary>
		/// <param name="visitor"><see cref="System.Func&lt;T,TResult&gt;"/> �̃C���X�^���X�B</param>
		/// <returns>�������̎��A�܂��̓c���[���ő������̎��ƒu�������鎮</returns>
		protected override Expression VisitChildren(ExpressionVisitor visitor)
		{
			var b = visitor.Visit(Body);
			if (b == Body)
				return this;
			return new FinallyFlowControlExpression(b);
		}

		void IInstructionProvider.AddInstructions(LightCompiler compiler) { compiler.Compile(Body); } // �C���^�v���^�� finally �u���b�N����̃W�����v�������̂ł��̂܂�
	}

	public partial class Utils
	{
		/// <summary>�w�肳�ꂽ���c���[���ɂ����� finally �u���b�N����̃W�����v���\�ɂ��܂��B</summary>
		/// <param name="body">finally �u���b�N����̃W�����v���\�ɂ��鎮�c���[���w�肵�܂��B</param>
		/// <returns>finally �u���b�N����̃W�����v���\�Ȏ��B</returns>
		public static Expression FinallyFlowControl(Expression body) { return new FinallyFlowControlExpression(body); }
	}
}
