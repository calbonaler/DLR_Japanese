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

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>�ŋߎ��s�Ɏ��s�����s�ԍ��̎��c���[�m�[�h��\���܂��B</summary>
	public class LastFaultingLineExpression : Expression
	{
		readonly Expression _lineNumberExpression;

		/// <summary>�w�肳�ꂽ�s�ԍ���\�������g�p���āA<see cref="Microsoft.Scripting.Interpreter.LastFaultingLineExpression"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="lineNumberExpression">�s�ԍ���\�����c���[�m�[�h���w�肵�܂��B</param>
		public LastFaultingLineExpression(Expression lineNumberExpression) { _lineNumberExpression = lineNumberExpression; }

		/// <summary>
		/// ���̎��̃m�[�h�^��Ԃ��܂��B
		/// �g���m�[�h�́A���̃��\�b�h���I�[�o�[���C�h����Ƃ��A<see cref="System.Linq.Expressions.ExpressionType.Extension"/> ��Ԃ��K�v������܂��B
		/// </summary>
		public sealed override ExpressionType NodeType { get { return ExpressionType.Extension; } }

		/// <summary>���� <see cref="System.Linq.Expressions.Expression"/> ���\�����̐ÓI�Ȍ^���擾���܂��B</summary>
		public sealed override Type Type { get { return typeof(int); } }

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
		public override Expression Reduce() { return _lineNumberExpression; }

		/// <summary>
		/// �m�[�h��P�������A�P�������ꂽ���� <paramref name="visitor"/> �f���Q�[�g���Ăяo���܂��B
		/// �m�[�h��P�����ł��Ȃ��ꍇ�A���̃��\�b�h�͗�O���X���[���܂��B
		/// </summary>
		/// <param name="visitor"><see cref="System.Func&lt;T,TResult&gt;"/> �̃C���X�^���X�B</param>
		/// <returns>�������̎��A�܂��̓c���[���ő������̎��ƒu�������鎮</returns>
		protected override Expression VisitChildren(ExpressionVisitor visitor)
		{
			var lineNo = visitor.Visit(_lineNumberExpression);
			if (lineNo != _lineNumberExpression)
				return new LastFaultingLineExpression(lineNo);
			return this;
		}
	}

	/// <summary>�t���[���̍ŋߎ��s�������߂�\���s�ԍ����v�b�V�����閽�߂�\���܂��B</summary>
	sealed class UpdateStackTraceInstruction : Instruction
	{
		/// <summary>�s�ԍ�����������f�o�b�O����\���܂��B</summary>
		internal DebugInfo[] _debugInfos;

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			var info = DebugInfo.GetMatchingDebugInfo(_debugInfos, frame.FaultingInstruction);
			frame.Push(info != null && !info.IsClear ? info.StartLine : -1);
			return +1;
		}
	}
}
