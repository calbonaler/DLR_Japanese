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
using System.Security.Permissions;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting
{
	/// <summary>�����̃t�@�C�����������܂��ł���ꍇ�ɃX���[������O��\���܂��B</summary>
	[Serializable]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
	public class AmbiguousFileNameException : Exception
	{
		/// <summary>�����܂��ȃt�@�C�����ɑ΂��� 1 �Ԗڂ̃t�@�C���p�X���擾���܂��B</summary>
		public string FirstPath { get; private set; }

		/// <summary>�����܂��ȃt�@�C�����ɑ΂��� 2 �Ԗڂ̃t�@�C���p�X���擾���܂��B</summary>
		public string SecondPath { get; private set; }

		/// <summary>�����܂��ł���p�X���g�p���āA<see cref="Microsoft.Scripting.AmbiguousFileNameException"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="firstPath">�����܂��ł��� 1 �Ԗڂ̃p�X���w�肵�܂��B</param>
		/// <param name="secondPath">�����܂��ł��� 2 �Ԗڂ̃p�X���w�肵�܂��B</param>
		public AmbiguousFileNameException(string firstPath, string secondPath) : this(firstPath, secondPath, null, null) { }

		/// <summary>�����܂��ł���p�X�Ɨ�O��������郁�b�Z�[�W���g�p���āA<see cref="Microsoft.Scripting.AmbiguousFileNameException"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="firstPath">�����܂��ł��� 1 �Ԗڂ̃p�X���w�肵�܂��B</param>
		/// <param name="secondPath">�����܂��ł��� 2 �Ԗڂ̃p�X���w�肵�܂��B</param>
		/// <param name="message">��O��������郁�b�Z�[�W���w�肵�܂��B</param>
		public AmbiguousFileNameException(string firstPath, string secondPath, string message) : this(firstPath, secondPath, message, null) { }

		/// <summary>�����܂��ł���p�X�A��O��������郁�b�Z�[�W�A���̗�O�̌����ƂȂ�����O���g�p���āA<see cref="Microsoft.Scripting.AmbiguousFileNameException"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="firstPath">�����܂��ł��� 1 �Ԗڂ̃p�X���w�肵�܂��B</param>
		/// <param name="secondPath">�����܂��ł��� 2 �Ԗڂ̃p�X���w�肵�܂��B</param>
		/// <param name="message">��O��������郁�b�Z�[�W���w�肵�܂��B</param>
		/// <param name="innerException">���̗�O�̌����ƂȂ�����O���w�肵�܂��B</param>
		public AmbiguousFileNameException(string firstPath, string secondPath, string message, Exception innerException) : base(message ?? string.Format("�t�@�C�����������܂��ł��B2 �ȏ�̃t�@�C�����������O�Ƀ}�b�`���܂��� ('{0}', '{1}')", firstPath, secondPath), innerException)
		{
			ContractUtils.RequiresNotNull(firstPath, "firstPath");
			ContractUtils.RequiresNotNull(secondPath, "secondPath");
			FirstPath = firstPath;
			SecondPath = secondPath;
		}

		/// <summary>�V���A���������f�[�^���g�p���āA<see cref="Microsoft.Scripting.AmbiguousFileNameException"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="info">�X���[����Ă����O�Ɋւ���V���A�����ς݃I�u�W�F�N�g �f�[�^��ێ����Ă��� <see cref="SerializationInfo"/>�B</param>
		/// <param name="context">�]�����܂��͓]����Ɋւ���R���e�L�X�g�����܂�ł��� <see cref="StreamingContext"/>�B</param>
		/// <exception cref="ArgumentNullException"><paramref name="info"/> �p�����[�^�[�� <c>null</c> �ł��B</exception>
		/// <exception cref="SerializationException">�N���X���� <c>null</c> �ł��邩�A�܂��� <see cref="P:Microsoft.Scripting.AmbiguousFileNameException.HResult"/> �� 0 �ł��B</exception>
		protected AmbiguousFileNameException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			FirstPath = info.GetString("firstPath");
			SecondPath = info.GetString("secondPath");
		}

		/// <summary>��O�Ɋւ�������g�p���� <see cref="SerializationInfo"/> ��ݒ肵�܂��B</summary>
		/// <param name="info">�X���[����Ă����O�Ɋւ���V���A�����ς݃I�u�W�F�N�g �f�[�^��ێ����Ă��� <see cref="SerializationInfo"/>�B</param>
		/// <param name="context">�]�����܂��͓]����Ɋւ���R���e�L�X�g�����܂�ł��� <see cref="StreamingContext"/>�B</param>
		/// <exception cref="ArgumentNullException"><paramref name="info"/> �p�����[�^�[�� <c>null</c> �Q�� (Visual Basic �̏ꍇ�� <c>Nothing</c>) �ł��B</exception>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("firstPath", FirstPath);
			info.AddValue("secondPath", SecondPath);
			base.GetObjectData(info, context);
		}
	}
}
