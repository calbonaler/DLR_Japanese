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
using System.Runtime.Remoting;
using System.Threading;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting.Shell.Remote
{
	/// <summary>�����[�g�V�i���I�ɃJ�X�^�}�C�Y���ꂽ�R�}���h���C�� �z�X�e�B���O �T�[�r�X��\���܂��B</summary>
	public class RemoteConsoleCommandLine : CommandLine
	{
		RemoteConsoleCommandDispatcher _remoteConsoleCommandDispatcher;

		/// <summary>�w�肳�ꂽ�X�R�[�v�A�f�B�X�p�b�`���A�C�x���g���g�p���āA<see cref="Microsoft.Scripting.Hosting.Shell.Remote.RemoteConsoleCommandLine"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="scope">�R�}���h���C���Ɋ֘A�t����ꂽ�X�R�[�v���w�肵�܂��B</param>
		/// <param name="remoteCommandDispatcher">�R�}���h�����ۂɃf�B�X�p�b�`���� <see cref="RemoteCommandDispatcher"/> ���w�肵�܂��B</param>
		/// <param name="remoteOutputReceived">�o�͂���M�����ۂɃV�O�i����ԂɂȂ� <see cref="AutoResetEvent"/> ���w�肵�܂��B</param>
		public RemoteConsoleCommandLine(ScriptScope scope, RemoteCommandDispatcher remoteCommandDispatcher, AutoResetEvent remoteOutputReceived)
		{
			ContractUtils.RequiresNotNull(scope, "scope");
			_remoteConsoleCommandDispatcher = new RemoteConsoleCommandDispatcher(remoteCommandDispatcher, remoteOutputReceived);
			ScriptScope = scope;
		}

		/// <summary>�P��̃R�}���h���f�B�X�p�b�`����V���� <see cref="ICommandDispatcher"/> ���쐬���܂��B</summary>
		/// <returns>�V�����쐬���ꂽ <see cref="ICommandDispatcher"/>�B</returns>
		protected override ICommandDispatcher CreateCommandDispatcher() { return _remoteConsoleCommandDispatcher; }

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal void UnhandledExceptionWorker(Exception ex)
		{
			try { base.UnhandledException(ex); }
			catch (Exception exceptionDuringHandling)
			{
				// �����[�g�����^�C���̃A�N�Z�X���ɔ����ɖ߂�̂ŁA���ׂĂ̗�O��ߑ����܂��B
				// �������A�����̏ꍇ�A�����ł� RemotingException �������邱�Ƃ�\�����Ă��܂��B
				if (!(exceptionDuringHandling is RemotingException))
					Console.WriteLine(string.Format("({0} thrown while trying to display unhandled exception)", exceptionDuringHandling.GetType()), Style.Error);
				// �����[�g�T�[�o�[���V���b�g�_�E�����Ă���\��������܂��B���̂��߁A�P���ɍs���܂��B
				Console.WriteLine(ex.ToString(), Style.Error);
			}
		}

		/// <summary>�Θb���[�v���Ńn���h������Ȃ���O�����������ۂɎ��s����܂��B</summary>
		/// <param name="ex">�n���h������Ȃ�������O�B</param>
		protected override void UnhandledException(Exception ex) { UnhandledExceptionWorker(ex); }

		/// <summary>�����[�g�����^�C������̏o�͂��m���ɓ��������� <see cref="ICommandDispatcher"/> ��\���܂��B</summary>
		class RemoteConsoleCommandDispatcher : ICommandDispatcher
		{
			RemoteCommandDispatcher _remoteCommandDispatcher;
			AutoResetEvent _remoteOutputReceived;

			internal RemoteConsoleCommandDispatcher(RemoteCommandDispatcher remoteCommandDispatcher, AutoResetEvent remoteOutputReceived)
			{
				_remoteCommandDispatcher = remoteCommandDispatcher;
				_remoteOutputReceived = remoteOutputReceived;
			}

			public object Execute(CompiledCode compiledCode, ScriptScope scope)
			{
				// �����[�g�����^�C���ŃR�[�h�����s���� RemoteCommandDispatcher �ɏ������Ϗ����܂��B
				var result = _remoteCommandDispatcher.Execute(compiledCode, scope);
				// �o�͔͂񓯊��I�Ɏ�M�����̂ŁA�����[�g�R���\�[���ł̖����I�ȓ������K�v�ł��B
				_remoteOutputReceived.WaitOne();
				return result;
			}
		}
	}
}