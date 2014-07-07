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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Scripting.Hosting.Shell.Remote
{
	/// <summary>�����[�g�����^�C���������I�����ꂽ���Ƃ̌��o����ѐV���������[�g�����^�C���̋N�����T�|�[�g���܂��B</summary>
	/// <remarks>
	/// �X���b�f�B���O���f��:
	/// 
	/// <see cref="ConsoleRestartManager"/> �̓R���\�[�����쐬�����s���镪���X���b�h���쐬���܂��B
	/// ����ɂ͏��Ȃ��Ƃ� 3 �̃X���b�h���֌W���Ă��܂�:
	/// 
	/// 1. ���C�� �A�v���P�[�V���� �X���b�h: <see cref="ConsoleRestartManager"/> ���C���X�^���X�����A���� API �ɃA�N�Z�X���܂��B
	///    ���̃X���b�h�̓��[�U�[���͂ɉ����\�ł���K�v������A<see cref="ConsoleRestartManager"/> �� API �𒷊��Ԏ��s������A�u���b�N�����肷�邱�Ƃ͂ł��܂���B
	///    �����[�g�����^�C���v���Z�X�͔񓯊��I�ɏI���ł���̂ŁA���݂� <see cref="RemoteConsoleHost"/> �� (�����ċN�����L���ł����) ���ł��ύX�ł��܂��B
	///    �A�v���P�[�V�����͒ʏ�ǂ� <see cref="RemoteConsoleHost"/> �̃C���X�^���X�����ݎg�p����Ă��邩���C�ɂ���K�v�͂���܂���B
	///    ���̃X���b�h�̃t���[�`���[�g�͎��̂悤�ɂȂ�܂�:
	///        <see cref="ConsoleRestartManager"/> ���쐬
	///        <see cref="ConsoleRestartManager.Start"/>
	///        ���[�v:
	///            ���[�U�[���͂ɉ��� | ���[�U�[���͂����s���̃R���\�[���ɑ��M | <see cref="BreakExecution"/> | <see cref="RestartConsole"/> | <see cref="GetMemberNames"/>
	///        <see cref="ConsoleRestartManager.Terminate"/>
	///    TODO: ���݁A<see cref="BreakExecution"/> �� <see cref="GetMemberNames"/> �̓��C���X���b�h���瓯���I�ɌĂяo����܂��B
	///    �����̓����[�g�����^�C���ɂ���R�[�h�����s���܂����A�C�ӂ̎��Ԃ�������\��������܂��B
	///    ���C���A�v���P�[�V�����X���b�h���������Ƀu���b�N����邱�Ƃ��Ȃ��悤�ɂ��̓����ύX����K�v������܂��B
	///
	/// 2. �R���\�[�� �X���b�h: <see cref="RemoteConsoleHost"/> ���쐬������A(���s�Ɏ��Ԃ�v���邩�A�������Ƀu���b�N����\���̂���) �R�[�h�����s�����肷�邽�߂̐�p�̃X���b�h�ł��B
	///        <see cref="ConsoleRestartManager.Start"/> �̎��s��ҋ@
	///        ���[�v:
	///            <see cref="RemoteConsoleHost"/> �̍쐬
	///            ���̃V�O�i���̑ҋ@:
	///                 �R�[�h�̎��s | <see cref="RestartConsole"/> | <see cref="Process.Exited"/>
	///
	/// 3. �����|�[�g�̔񓯊��R�[���o�b�N
	///        <see cref="Process.Exited"/> | <see cref="Process.OutputDataReceived"/> | <see cref="Process.ErrorDataReceived"/>
	/// 
	/// 4. �t�@�C�i���C�U �X���b�h
	///    (Dispose ���Ăяo���\���̂���) Finalize ���\�b�h������I�u�W�F�N�g������܂��B
	///    ����قǑ����͂Ȃ��^�ł� Finalize ���\�b�h�����K�v��������̂�����܂��B
	/// </remarks>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1012:AbstractTypesShouldNotHaveConstructors")] // TODO: This is public only because the test (RemoteConsole.py) needs it to be so. The test should be rewritten
	public abstract class ConsoleRestartManager
	{
		bool _exitOnNormalExit;
		bool _terminating;

		/// <summary>REPL ���[�v�̐������[�h���g�p���āA<see cref="Microsoft.Scripting.Hosting.Shell.Remote.ConsoleRestartManager"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="exitOnNormalExit"><see cref="RemoteConsoleHost"/> �̃C���X�^���X������ɏI�������ꍇ�� REPL ���[�v���I�����邩�ǂ����������l���w�肵�܂��B</param>
		public ConsoleRestartManager(bool exitOnNormalExit)
		{
			AccessLock = new object();
			_exitOnNormalExit = exitOnNormalExit;
			ConsoleThread = new Thread(Run);
			ConsoleThread.Name = "Console thread";
		}

		/// <summary><see cref="CurrentConsoleHost"/> �ɃR���\�[�� �X���b�h�ȊO����A�N�Z�X���ꂽ�ꍇ�� <c>null</c> ���j������Ă��Ȃ���Ԃł��邱�Ƃ�ۏ؂��郍�b�N���擾���܂��B</summary>
		protected object AccessLock { get; private set; }

		/// <summary><see cref="RemoteConsoleHost"/> ���쐬������A���s�Ɏ��Ԃ̂�����\���̂���R�[�h�����s�����肷���p�̃X���b�h���擾���܂��B</summary>
		public Thread ConsoleThread { get; private set; }

		/// <summary>���݂� <see cref="RemoteConsoleHost"/> ���擾���܂��B</summary>
		protected RemoteConsoleHost CurrentConsoleHost { get; private set; }

		/// <summary>�V���� <see cref="RemoteConsoleHost"/> ���쐬���܂��B���̃��\�b�h�̓R���\�[�� �X���b�h�Ŏ��s����܂��B</summary>
		/// <returns>�V�����쐬���ꂽ <see cref="RemoteConsoleHost"/>�B</returns>
		public abstract RemoteConsoleHost CreateRemoteConsoleHost();

		/// <summary>�R���\�[�� REPL ���[�v���J�n���܂��B���̃��\�b�h���Ăяo���ɂ̓A�N�e�B�x�[�V�������������Ă���K�v������܂��B</summary>
		public void Start()
		{
			Debug.Assert(Thread.CurrentThread != ConsoleThread);
			if (ConsoleThread.IsAlive)
				throw new InvalidOperationException("Console thread is already running.");
			ConsoleThread.Start();
		}

		void Run()
		{
#if DEBUG
			try { RunWorker(); }
			catch (Exception e) { Debug.Fail("Unhandled exception on console thread:\n\n" + e.ToString()); }
#else
            RunWorker();
#endif
		}

		void RunWorker()
		{
			Debug.Assert(Thread.CurrentThread == ConsoleThread);
			while (true)
			{
				var remoteConsoleHost = CreateRemoteConsoleHost();
				// Reading _terminating and setting of _remoteConsoleHost should be done atomically. 
				// Terminate() does the reverse operation (setting _terminating reading _remoteConsoleHost) atomically
				lock (AccessLock)
				{
					if (_terminating)
						return;
					CurrentConsoleHost = remoteConsoleHost;
				}
				try
				{
					var exitCode = remoteConsoleHost.Run(new string[0]);
					if (_exitOnNormalExit && exitCode == 0)
						return;
				}
				catch (RemoteRuntimeStartupException) { }
				finally
				{
					lock (AccessLock)
					{
						remoteConsoleHost.Dispose();
						CurrentConsoleHost = null;
					}
				}
			}
		}

		// TODO: �����[�g�����^�C���Ń��[�U�[�R�[�h�����s���Ă��āA�ǂ�ȗ�O���X���[����邩�𐧌�ł��Ȃ����߁A���ׂĂ̗�O��ߑ�����K�v������܂��B
		// ����̓����[�g�����^�C�������O���t�`�d�������ɁA�G���[�R�[�h��Ԃ������[�g�`�����l����݂��邱�ƂŏC�����܂��B
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public IList<string> GetMemberNames(string expression)
		{
			Debug.Assert(Thread.CurrentThread != ConsoleThread);
			lock (AccessLock)
			{
				if (CurrentConsoleHost == null)
					return null;
				try
				{
					var operations = CurrentConsoleHost.Engine.CreateOperations(CurrentConsoleHost.ScriptScope);
					var src = CurrentConsoleHost.Engine.CreateScriptSourceFromString(expression, SourceCodeKind.Expression);
					return operations.GetMemberNames(src.ExecuteAndWrap(CurrentConsoleHost.ScriptScope));
				}
				catch { return null; }
			}
		}

		/// <summary>�R���\�[�� REPL ���[�v�̎��s�𒆒f���܂��B</summary>
		public void BreakExecution()
		{
			Debug.Assert(Thread.CurrentThread != ConsoleThread);
			lock (AccessLock)
			{
				if (CurrentConsoleHost == null)
					return;
				try { CurrentConsoleHost.AbortCommand(); }
				catch (System.Runtime.Remoting.RemotingException) { } // �����[�g�����^�C���͏I�����ꂽ���A����������܂���B
			}
		}

		/// <summary>�R���\�[�� REPL ���[�v�̏I����v�����āAREPL ���[�v���ċN�����܂��B</summary>
		public void RestartConsole()
		{
			Debug.Assert(Thread.CurrentThread != ConsoleThread);
			lock (AccessLock)
			{
				if (CurrentConsoleHost == null)
					return;
				CurrentConsoleHost.Terminate(0);
			}
		}

		/// <summary>�R���\�[�� REPL ���[�v�̏I����v�����܂��B</summary>
		public void Terminate()
		{
			Debug.Assert(Thread.CurrentThread != ConsoleThread);
			lock (AccessLock)
			{
				_terminating = true;
				CurrentConsoleHost.Terminate(0);
			}
			ConsoleThread.Join();
		}
	}
}