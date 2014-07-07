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
using System.Runtime.Serialization;

namespace Microsoft.Scripting
{
	/// <summary>�C���^�v���^�Ŗ��߂̎��s���Ƀ��[�U�[�����荞�݃L�[���������ꍇ�ɃX���[������O�B</summary>
	[Serializable]
	public class KeyboardInterruptException : Exception
	{
		/// <summary><see cref="Microsoft.Scripting.KeyboardInterruptException"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		public KeyboardInterruptException() : base() { }

		/// <summary>�w�肵���G���[ ���b�Z�[�W���g�p���āA<see cref="Microsoft.Scripting.KeyboardInterruptException"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="msg">�G���[��������郁�b�Z�[�W�B</param>
		public KeyboardInterruptException(string msg) : base(msg) { }

		/// <summary>�w�肵���G���[ ���b�Z�[�W�ƁA���̗�O�̌����ł��������O�ւ̎Q�Ƃ��g�p���āA<see cref="Microsoft.Scripting.KeyboardInterruptException"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="message">��O�̌������������G���[ ���b�Z�[�W�B</param>
		/// <param name="innerException">���݂̗�O�̌����ł����O�B������O���w�肳��Ă��Ȃ��ꍇ�� <c>null</c> �Q�� (Visual Basic �ł́A<c>Nothing</c>)�B</param>
		public KeyboardInterruptException(string message, Exception innerException) : base(message, innerException) { }

		/// <summary>�V���A���������f�[�^���g�p���āA<see cref="Microsoft.Scripting.KeyboardInterruptException"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="info">�X���[����Ă����O�Ɋւ���V���A�����ς݃I�u�W�F�N�g �f�[�^��ێ����Ă��� <see cref="SerializationInfo"/>�B</param>
		/// <param name="context">�]�����܂��͓]����Ɋւ���R���e�L�X�g�����܂�ł��� <see cref="StreamingContext"/>�B</param>
		/// <exception cref="ArgumentNullException"><paramref name="info"/> �p�����[�^�[�� <c>null</c> �ł��B</exception>
		/// <exception cref="SerializationException">�N���X���� <c>null</c> �ł��邩�A�܂��� <see cref="P:KeyboardInterruptException.HResult"/> �� 0 �ł��B</exception>
		protected KeyboardInterruptException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}
