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
using System.Security.Permissions;

namespace Microsoft.Scripting.Hosting
{
	/// <summary>
	/// �z�X�g�͂��̃N���X���g�p���ăX�N���v�g�̉�͂�R���p�C�����ɕ񍐂��ꂽ�G���[��ǐՂ��邱�Ƃ��ł��܂��B
	/// <see cref="Microsoft.Scripting.ErrorSink"/> �ɑ΂������ 1 �̃z�X�e�B���O API �ł��B
	/// </summary>
	public abstract class ErrorListener : MarshalByRefObject
	{
		/// <summary><see cref="Microsoft.Scripting.Hosting.ErrorListener"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		protected ErrorListener() { }

		/// <summary>�G���[���񍐂��ꂽ�Ƃ��ɌĂяo����܂��B</summary>
		/// <param name="source">�G���[���������� <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �ł��B</param>
		/// <param name="message">�G���[�ɑ΂��郁�b�Z�[�W�ł��B</param>
		/// <param name="span">�G���[�����������ꏊ������ <see cref="Microsoft.Scripting.SourceSpan"/> �ł��B</param>
		/// <param name="errorCode">�G���[�R�[�h�����������l�ł��B</param>
		/// <param name="severity">�G���[�̐[�����������l�ł��B</param>
		public abstract void ErrorReported(ScriptSource source, string message, SourceSpan span, int errorCode, Severity severity);

		// TODO: Figure out what is the right lifetime
		/// <summary>�Ώۂ̃C���X�^���X�̗L�����ԃ|���V�[�𐧌䂷��A�L�����ԃT�[�r�X �I�u�W�F�N�g���擾���܂��B</summary>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
		public override object InitializeLifetimeService() { return null; }
	}
}
