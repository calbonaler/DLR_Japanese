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
	/// <summary>���\�b�h�̎������������Ȃ��ꍇ�ɃX���[������O�B</summary>
	[Serializable]
	public class InvalidImplementationException : Exception
	{
		/// <summary><see cref="Microsoft.Scripting.InvalidImplementationException"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		public InvalidImplementationException() : base() { }

		/// <summary>�w�肵�����b�Z�[�W���g�p���āA<see cref="Microsoft.Scripting.InvalidImplementationException"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="message">�G���[��������郁�b�Z�[�W�B</param>
		public InvalidImplementationException(string message) : base(message) { }

		/// <summary>�w�肵���G���[ ���b�Z�[�W�ƁA���̗�O�̌����ł��������O�ւ̎Q�Ƃ��g�p���āA<see cref="Microsoft.Scripting.InvalidImplementationException"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="message">��O�̌������������G���[ ���b�Z�[�W�B</param>
		/// <param name="e">���݂̗�O�̌����ł����O�B������O���w�肳��Ă��Ȃ��ꍇ�� <c>null</c> �Q�� (Visual Basic �ł́ANothing)�B</param>
		public InvalidImplementationException(string message, Exception e) : base(message, e) { }

		/// <summary>�V���A���������f�[�^���g�p���āA<see cref="Microsoft.Scripting.InvalidImplementationException"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="info">�X���[����Ă����O�Ɋւ���V���A�����ς݃I�u�W�F�N�g �f�[�^��ێ����Ă��� <see cref="System.Runtime.Serialization.SerializationInfo"/>�B</param>
		/// <param name="context">�]�����܂��͓]����Ɋւ���R���e�L�X�g�����܂�ł��� <see cref="System.Runtime.Serialization.StreamingContext"/>�B</param>
		protected InvalidImplementationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}
