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


namespace Microsoft.Scripting.Runtime
{
	/// <summary>�\����͊�̏�Ԃ��ʒm�����I�u�W�F�N�g�ł��B</summary>
	public class ParserSink
	{
		/// <summary>��Ԃ��ʒm����Ă��������Ȃ� <see cref="ParserSink"/> �I�u�W�F�N�g�ł��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly ParserSink Null = new ParserSink();

		/// <summary>��g�Ƃ��Ă̈�v�����̃I�u�W�F�N�g�ɒʒm���܂��B</summary>
		/// <param name="opening">�J�n�g�[�N���̃\�[�X�R�[�h��ł͈̔͂��w�肵�܂��B</param>
		/// <param name="closing">�I���g�[�N���̃\�[�X�R�[�h��ł͈̔͂��w�肵�܂��B</param>
		/// <param name="priority">�D��x���w�肵�܂��B</param>
		public virtual void MatchPair(SourceSpan opening, SourceSpan closing, int priority) { }

		/// <summary>�O�g�Ƃ��Ă̈�v�����̃I�u�W�F�N�g�ɒʒm���܂��B</summary>
		/// <param name="opening">�J�n�g�[�N���̃\�[�X�R�[�h��ł͈̔͂��w�肵�܂��B</param>
		/// <param name="middle">�����g�[�N���̃\�[�X�R�[�h��ł͈̔͂��w�肵�܂��B</param>
		/// <param name="closing">�I���g�[�N���̃\�[�X�R�[�h��ł͈̔͂��w�肵�܂��B</param>
		/// <param name="priority">�D��x���w�肵�܂��B</param>
		public virtual void MatchTriple(SourceSpan opening, SourceSpan middle, SourceSpan closing, int priority) { }

		/// <summary>�������X�g�̏I�������̃I�u�W�F�N�g�ɒʒm���܂��B</summary>
		/// <param name="span">�������X�g�̏I���g�[�N���̃\�[�X�R�[�h��ł͈̔͂��w�肵�܂��B</param>
		public virtual void EndParameters(SourceSpan span) { }

		/// <summary>���̈����̉�͂����̃I�u�W�F�N�g�ɒʒm���܂��B</summary>
		/// <param name="span">�����g�[�N���̃\�[�X�R�[�h��ł͈̔͂��w�肵�܂��B</param>
		public virtual void NextParameter(SourceSpan span) { }

		/// <summary>���O�̏C�������̃I�u�W�F�N�g�ɒʒm���܂��B</summary>
		/// <param name="selector">�Z���N�^�̃\�[�X�R�[�h��ł͈̔͂��w�肵�܂��B</param>
		/// <param name="span">�C������閼�O�̃\�[�X�R�[�h��ł͈̔͂��w�肵�܂��B</param>
		/// <param name="name">���O���w�肵�܂��B</param>
		public virtual void QualifyName(SourceSpan selector, SourceSpan span, string name) { }

		/// <summary>���O�̊J�n�����̃I�u�W�F�N�g�ɒʒm���܂��B</summary>
		/// <param name="span">���O�̃\�[�X�R�[�h��ł͈̔͂��w�肵�܂��B</param>
		/// <param name="name">���O���w�肵�܂��B</param>
		public virtual void StartName(SourceSpan span, string name) { }

		/// <summary>�������X�g�̊J�n�����̃I�u�W�F�N�g�ɒʒm���܂��B</summary>
		/// <param name="context">�������X�g�̊J�n�g�[�N���̃\�[�X�R�[�h��ł͈̔͂��w�肵�܂��B</param>
		public virtual void StartParameters(SourceSpan context) { }
	}
}
