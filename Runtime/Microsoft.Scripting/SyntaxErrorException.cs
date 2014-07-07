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
	/// <summary>�\����͂����s�����ꍇ�ɃX���[������O�B</summary>
	[Serializable]
	public class SyntaxErrorException : Exception
	{
		/// <summary><see cref="Microsoft.Scripting.SyntaxErrorException"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		public SyntaxErrorException() : base() { }

		/// <summary>�w�肵�����b�Z�[�W���g�p���āA<see cref="Microsoft.Scripting.SyntaxErrorException"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="message">�G���[��������郁�b�Z�[�W�B</param>
		public SyntaxErrorException(string message) : base(message) { }

		/// <summary>�w�肵���G���[ ���b�Z�[�W�ƁA���̗�O�̌����ł��������O�ւ̎Q�Ƃ��g�p���āA<see cref="Microsoft.Scripting.SyntaxErrorException"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="message">��O�̌������������G���[ ���b�Z�[�W�B</param>
		/// <param name="innerException">���݂̗�O�̌����ł����O�B������O���w�肳��Ă��Ȃ��ꍇ�� <c>null</c> �Q�� (Visual Basic �ł́ANothing)�B</param>
		public SyntaxErrorException(string message, Exception innerException) : base(message, innerException) { }

		/// <summary>�w�肵�����b�Z�[�W���g�p���āA<see cref="Microsoft.Scripting.SyntaxErrorException"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="message">�G���[��������郁�b�Z�[�W�B</param>
		/// <param name="sourceUnit">�G���[�����������|����͒P�ʁB</param>
		/// <param name="span">�G���[�����������\�[�X�R�[�h��͈̔́B</param>
		/// <param name="errorCode">�G���[�̎�ނ��������l�B</param>
		/// <param name="severity">�G���[�̐[�����������l�B</param>
		public SyntaxErrorException(string message, SourceUnit sourceUnit, SourceSpan span, int errorCode, Severity severity) : base(message)
		{
			ContractUtils.RequiresNotNull(message, "message");
			RawSpan = span;
			Severity = severity;
			ErrorCode = errorCode;
			if (sourceUnit != null)
			{
				SourcePath = sourceUnit.Path;
				try
				{
					SourceCode = sourceUnit.GetCode();
					CodeLine = sourceUnit.GetCodeLine(Line);
				}
				catch (System.IO.IOException) { } // could not get source code.
			}
		}

		/// <summary>�w�肵�����b�Z�[�W���g�p���āA<see cref="Microsoft.Scripting.SyntaxErrorException"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="message">�G���[��������郁�b�Z�[�W�B</param>
		/// <param name="path">�G���[�����������t�@�C���������p�X�B</param>
		/// <param name="code">�G���[�����������\�[�X�R�[�h�B</param>
		/// <param name="line">�G���[�����������s�̃\�[�X�R�[�h�B</param>
		/// <param name="span">�G���[�����������\�[�X�R�[�h��͈̔́B</param>
		/// <param name="errorCode">�G���[�̎�ނ��������l�B</param>
		/// <param name="severity">�G���[�̐[�����������l�B</param>
		public SyntaxErrorException(string message, string path, string code, string line, SourceSpan span, int errorCode, Severity severity) : base(message)
		{
			ContractUtils.RequiresNotNull(message, "message");
			RawSpan = span;
			Severity = severity;
			ErrorCode = errorCode;
			SourcePath = path;
			SourceCode = code;
			CodeLine = line;
		}

		/// <summary>�V���A���������f�[�^���g�p���āA<see cref="Microsoft.Scripting.SyntaxErrorException"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="info">�X���[����Ă����O�Ɋւ���V���A�����ς݃I�u�W�F�N�g �f�[�^��ێ����Ă��� <see cref="System.Runtime.Serialization.SerializationInfo"/>�B</param>
		/// <param name="context">�]�����܂��͓]����Ɋւ���R���e�L�X�g�����܂�ł��� <see cref="System.Runtime.Serialization.StreamingContext"/>�B</param>
		protected SyntaxErrorException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			RawSpan = (SourceSpan)info.GetValue("Span", typeof(SourceSpan));
			SourceCode = info.GetString("SourceCode");
			SourcePath = info.GetString("SourcePath");
			Severity = (Severity)info.GetValue("Severity", typeof(Severity));
			ErrorCode = info.GetInt32("ErrorCode");
		}

		/// <summary>���̗�O�Ɋւ�������g�p���� <see cref="System.Runtime.Serialization.SerializationInfo"/> ��ݒ肵�܂��B</summary>
		/// <param name="info">�X���[����Ă����O�Ɋւ���V���A�����ς݃I�u�W�F�N�g �f�[�^��ێ����Ă��� <see cref="System.Runtime.Serialization.SerializationInfo"/>�B</param>
		/// <param name="context">�]�����܂��͓]����Ɋւ���R���e�L�X�g�����܂�ł��� <see cref="System.Runtime.Serialization.StreamingContext"/>�B</param>
		/// <exception cref="System.ArgumentNullException"><paramref name="info"/> �p�����[�^�[�� <c>null</c> �Q�� (Visual Basic �̏ꍇ�� Nothing) �ł��B</exception>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			ContractUtils.RequiresNotNull(info, "info");
			base.GetObjectData(info, context);
			info.AddValue("Span", RawSpan);
			info.AddValue("SourceCode", SourceCode);
			info.AddValue("SourcePath", SourcePath);
			info.AddValue("Severity", Severity);
			info.AddValue("ErrorCode", ErrorCode);
		}

		/// <summary>�G���[�����������}�b�s���O����Ă��Ȃ��\�[�X�R�[�h��͈̔͂��擾���܂��B</summary>
		public SourceSpan RawSpan { get; private set; }

		/// <summary>�G���[�����������\�[�X�R�[�h���擾���܂��B</summary>
		public string SourceCode { get; private set; }

		/// <summary>�G���[�����������t�@�C���������p�X���擾���܂��B</summary>
		public string SourcePath { get; private set; }

		/// <summary>���������G���[�̐[�����������l���擾���܂��B</summary>
		public Severity Severity { get; private set; }

		/// <summary>�G���[�����������\�[�X�R�[�h��� 1 ����n�܂�s�ԍ����擾���܂��B</summary>
		public int Line { get { return RawSpan.Start.Line; } }

		/// <summary>�G���[�����������\�[�X�R�[�h��� 1 ����n�܂錅�ԍ����擾���܂��B</summary>
		public int Column { get { return RawSpan.Start.Column; } }

		/// <summary>���������G���[�̎�ނ��������l���擾���܂��B</summary>
		public int ErrorCode { get; private set; }

		/// <summary>�G���[�����������V���{���h�L�������g�����擾���܂��B</summary>
		public string SymbolDocumentName { get { return SourcePath; } }

		/// <summary>�G���[�����������\�[�X�R�[�h�̍s���擾���܂��B</summary>
		public string CodeLine { get; private set; }
	}
}
