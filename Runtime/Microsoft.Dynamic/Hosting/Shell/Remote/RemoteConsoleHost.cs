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
using System.Diagnostics;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization;
using System.Threading;

namespace Microsoft.Scripting.Hosting.Shell.Remote
{
	/// <summary><see cref="ScriptRuntime"/> �� (�����[�g�����^�C���T�[�o�[�ƌĂ΂��) �����v���Z�X�Ńz�X�g����Ă��� <see cref="ConsoleHost"/> ��\���܂��B</summary>
	/// <remarks>
	/// ���̃N���X�̓����[�g�����^�C���T�[�o�[�𐶐����A���ݒʐM�Ɏg�p���� IPC �`�����l���������肵�܂��B
	/// �����[�g�����^�C���T�[�o�[�� <see cref="ScriptRuntime"/> ����� <see cref="ScriptEngine"/> ���쐬�A���������āA�E�F���m�E�� URI �̎w�肳�ꂽ IPC �`�����l����Ō��J���܂��B
	/// ����: <see cref="RemoteConsoleHost"/> �� <see cref="Microsoft.Scripting.Runtime.LanguageContext"/> �̂悤�ȃN���X�������[�g�\�łȂ����߁A<see cref="ScriptEngine"/> �̏������ɊȒP�ɎQ���ł��܂���B
	/// <see cref="RemoteConsoleHost"/> �͎��Ƀ����[�e�B���O�`�����l����� <see cref="ScriptEngine"/> �őΘb���[�v���J�n���A�R�}���h�����s���܂��B
	/// �܂��A�����[�g�����^�C���T�[�o�[�̕W���o�͂��Ď����邱�ƂŁA���[�J���ł̃��[�U�[�ւ̕\�����s���܂��B
	/// </remarks>
	public abstract class RemoteConsoleHost : ConsoleHost, IDisposable
	{
		internal RemoteCommandDispatcher _remoteCommandDispatcher;
		string _channelName = GetChannelName();
		IpcChannel _clientChannel;
		AutoResetEvent _remoteOutputReceived = new AutoResetEvent(false);
		ScriptScope _scriptScope;

		static string GetChannelName() { return "RemoteRuntime-" + Guid.NewGuid().ToString(); }

		ProcessStartInfo GetProcessStartInfo()
		{
			var processInfo = new ProcessStartInfo()
			{
				Arguments = RemoteRuntimeServer.RemoteRuntimeArg + " " + _channelName,
				CreateNoWindow = true,
				// ���_�C���N�g��L���ɂ��邽�߂� UseShellExecute �� false �ɐݒ�
				UseShellExecute = false,
				// �W���X�g���[�������_�C���N�g����B�o�̓X�g���[���̓C�x���g�n���h�����g�p���Ĕ񓯊��ɓǂݎ��
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				// ���̓X�g���[���̓����[�g�T�[�o�[�v���Z�X����������͂�ǂݎ��K�v���Ȃ��悤�ɖ����ł���B
				RedirectStandardInput = true,
			};
			CustomizeRemoteRuntimeStartInfo(processInfo);
			Debug.Assert(processInfo.FileName != null);
			return processInfo;
		}

		void StartRemoteRuntimeProcess()
		{
			var process = new Process();
			process.StartInfo = GetProcessStartInfo();
			process.OutputDataReceived += OnOutputDataReceived;
			process.ErrorDataReceived += OnErrorDataReceived;
			process.Exited += OnRemoteRuntimeExited;
			RemoteRuntimeProcess = process;
			process.Start();
			// �o�̓X�g���[���̔񓯊��ł̓ǂݎ����J�n����
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			// Exited ����������悤�ɂ���
			process.EnableRaisingEvents = true;
			// �N���̏o�͂���������������m�邽�߂ɏo�̓}�[�J�[��ҋ@
			_remoteOutputReceived.WaitOne();
			if (process.HasExited)
				throw new RemoteRuntimeStartupException("Remote runtime terminated during startup with exitcode " + process.ExitCode);
		}

		T GetRemoteObject<T>(string uri)
		{
			T result = (T)Activator.GetObject(typeof(T), "ipc://" + _channelName + "/" + uri);
			// �����[�g�I�u�W�F�N�g�� (�����[�g�Ɏ��s�����) ���z���\�b�h�̌Ăяo���ɉ����\�ł��邱�Ƃ�ۏ؂���B
			Debug.Assert(result.ToString() != null);
			return result;
		}

		void InitializeRemoteScriptEngine()
		{
			StartRemoteRuntimeProcess();
			Engine = (_scriptScope = (_remoteCommandDispatcher = GetRemoteObject<RemoteCommandDispatcher>(RemoteRuntimeServer.CommandDispatcherUri)).ScriptScope).Engine;
			// �����[�g�����^�C���v���Z�X���C�x���g�𔭍s������A��O���X���[�������ꍇ�́A�t�����ɑ΂���`�����l����o�^����B
			var clientChannelName = _channelName.Replace("RemoteRuntime", "RemoteConsole");
			ChannelServices.RegisterChannel(RemoteRuntimeServer.CreateChannel(clientChannelName, clientChannelName), false);
		}

		/// <summary>�����[�g�����^�C���T�[�o�[���I�������ۂɌĂ΂�܂��B</summary>
		/// <param name="sender">�I�����������[�g�����^�C���T�[�o�[��\�� <see cref="Process"/> �I�u�W�F�N�g�B</param>
		/// <param name="e">�����[�g�����^�C���T�[�o�[�̏I���Ɋ֘A�t�����Ă���C�x���g�I�u�W�F�N�g�B</param>
		protected virtual void OnRemoteRuntimeExited(object sender, EventArgs e)
		{
			Debug.Assert(((Process)sender).HasExited);
			Debug.Assert(sender == RemoteRuntimeProcess || RemoteRuntimeProcess == null);
			var remoteRuntimeExited = RemoteRuntimeExited;
			if (remoteRuntimeExited != null)
				remoteRuntimeExited(sender, e);
			// StartRemoteRuntimeProcess �͂��̃C�x���g���u���b�N����B�����[�g�����^�C�������̋N�����ɏI�������ꍇ�ɁA�V�O�i����Ԃɂ���B
			_remoteOutputReceived.Set();
			// ConsoleHost �� REPL ���[�v���I���ł���悤�ɒ�������
			Terminate(RemoteRuntimeProcess.ExitCode);
		}

		/// <summary>�����[�g�����^�C���T�[�o�[����o�̓f�[�^�����������ۂɌĂ΂�܂��B</summary>
		/// <param name="sender">�f�[�^�̑��M���̃����[�g�����^�C���T�[�o�[��\�� <see cref="Process"/> �I�u�W�F�N�g�B</param>
		/// <param name="e">�����[�g�����^�C���T�[�o�[���瓞�������f�[�^���i�[���Ă���C�x���g�I�u�W�F�N�g�B</param>
		protected virtual void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (string.IsNullOrEmpty(e.Data))
				return;
			if (e.Data.Contains(RemoteCommandDispatcher.OutputCompleteMarker))
			{
				Debug.Assert(e.Data == RemoteCommandDispatcher.OutputCompleteMarker);
				_remoteOutputReceived.Set();
			}
			else
				ConsoleIO.WriteLine(e.Data, Style.Out);
		}

		void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (!string.IsNullOrEmpty(e.Data))
				ConsoleIO.WriteLine(e.Data, Style.Error);
		}

		/// <summary>�����[�g�R���\�[���� REPL ���[�v�ɏI����v�����܂��B</summary>
		/// <param name="exitCode">�R���\�[���̏I���R�[�h���w�肵�܂��B</param>
		public override void Terminate(int exitCode)
		{
			if (CommandLine == null)
				// CommandLine ������������Ȃ������ꍇ�́A�N�����ɌĂ΂��\��������܂��B
				// ����̓����[�g�����^�C���v���Z�X�̋N���O�� CommandLine �����������邱�Ƃɂ���ďC���ł���\��������܂�
				return;
			base.Terminate(exitCode);
		}

		/// <summary>�V���� <see cref="CommandLine"/> �I�u�W�F�N�g���쐬���܂��B</summary>
		/// <returns>�V�����쐬���ꂽ <see cref="CommandLine"/> �I�u�W�F�N�g�B</returns>
		protected override CommandLine CreateCommandLine() { return new RemoteConsoleCommandLine(_scriptScope, _remoteCommandDispatcher, _remoteOutputReceived); }

		/// <summary>���̃R���\�[�� �z�X�g�Ɋ֘A�t����ꂽ�X�R�[�v��\�� <see cref="ScriptScope"/> ���擾���܂��B</summary>
		public ScriptScope ScriptScope { get { return CommandLine.ScriptScope; } }

		/// <summary>���̃����[�g�R���\�[���̃����[�g�����^�C���T�[�o�[��\�� <see cref="Process"/> ���擾���܂��B</summary>
		public Process RemoteRuntimeProcess { get; private set; }

		/// <summary>�R�}���h�̎��s���Ƀn���h������Ȃ���O�����������ۂɌĂ΂�܂��B</summary>
		/// <param name="engine">�R�}���h�����s���Ă���G���W���B</param>
		/// <param name="ex">�n���h������Ȃ�������O�B</param>
		protected override void UnhandledException(ScriptEngine engine, Exception ex) { ((RemoteConsoleCommandLine)CommandLine).UnhandledExceptionWorker(ex); }

		/// <summary>�����[�g�����^�C���T�[�o�[�����ꎩ�̂ɂ���ďI�������ꍇ�ɔ������܂��B</summary>
		internal event EventHandler RemoteRuntimeExited;

		/// <summary>
		/// �R���\�[�������ϐ����ƃf�B���N�g���Ȃǂ�ύX�ł���@���^���܂��B
		/// ���̃��\�b�h�ŏ��Ȃ��Ƃ� <see cref="ProcessStartInfo.FileName"/> �͏����������K�v������܂��B
		/// </summary>
		/// <param name="processInfo">�����[�g�����^�C���T�[�o�[�v���Z�X�̋N������\�� <see cref="ProcessStartInfo"/>�B</param>
		public abstract void CustomizeRemoteRuntimeStartInfo(ProcessStartInfo processInfo);

		/// <summary>���݂Ɏ��s����Ă���R�}���h�𒆎~���܂��B</summary>
		/// <returns>���ۂɃR�}���h�����~���ꂽ�ꍇ�� <c>true</c>�B�R�}���h�����s����Ă��Ȃ��������A���łɊ��������ꍇ�͏ꍇ�� <c>false</c>�B</returns>
		public bool AbortCommand() { return _remoteCommandDispatcher.AbortCommand(); }

		/// <summary>�w�肳�ꂽ�������g�p���āA�����[�g�R���\�[���̎��s���J�n���܂��B</summary>
		/// <param name="args">�v���O�����̈������w�肵�܂��B</param>
		/// <returns>�����[�g�R���\�[���̏I���R�[�h�B</returns>
		public override int Run(string[] args)
		{
			ConsoleHostOptionsParser = new ConsoleHostOptionsParser(new ConsoleHostOptions(), CreateRuntimeSetup());
			try { ParseHostOptions(args); }
			catch (InvalidOptionException ex)
			{
				Console.Error.WriteLine("Invalid argument: " + ex.Message);
				return ExitCode = 1;
			}
			_languageOptionsParser = CreateOptionsParser();
			// �N�����̏o�͂�\���ł���悤�ɂ��邽�߂ɁA(����̐ݒ��) ���߂� IConsole ���쐬���܂��B
			ConsoleIO = CreateConsole(null, null, new ConsoleOptions());
			InitializeRemoteScriptEngine();
			Runtime = Engine.Runtime;
			ExecuteInternal();
			return ExitCode;
		}

		/// <summary>���̃����[�g�R���\�[���z�X�g��j�����܂��B</summary>
		/// <param name="disposing">���ׂẴ��\�[�X��j������ꍇ�� <c>true</c>�B�A���}�l�[�W���\�[�X�݂̂�j������ꍇ�� <c>false</c>�B</param>
		public virtual void Dispose(bool disposing)
		{
			if (!disposing)
				// �t�@�C�i���C�Y���̓}�l�[�W�t�B�[���h�͂��łɃt�@�C�i���C�Y����Ă���\�������邽�߁A�M�����ăA�N�Z�X���邱�Ƃ��ł��܂���B
				return;
			_remoteOutputReceived.Close();
			if (_clientChannel != null)
			{
				ChannelServices.UnregisterChannel(_clientChannel);
				_clientChannel = null;
			}
			if (RemoteRuntimeProcess != null)
			{
				RemoteRuntimeProcess.Exited -= OnRemoteRuntimeExited;
				// �W�����͂̃N���[�Y�̓����[�g�����^�C���ɑ΂��ăv���Z�X���I������V�O�i���ł��B
				RemoteRuntimeProcess.StandardInput.Close();
				RemoteRuntimeProcess.WaitForExit(5000);
				if (!RemoteRuntimeProcess.HasExited)
				{
					RemoteRuntimeProcess.Kill();
					RemoteRuntimeProcess.WaitForExit();
				}
				RemoteRuntimeProcess = null;
			}
		}

		/// <summary>���̃����[�g�R���\�[���z�X�g��j�����܂��B</summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}

	/// <summary>�����[�g�����^�C�����N�����ɗ\�������I�������ꍇ�ɃX���[������O��\���܂��B</summary>
	[Serializable]
	public class RemoteRuntimeStartupException : Exception
	{
		/// <summary><see cref="Microsoft.Scripting.Hosting.Shell.Remote.RemoteRuntimeStartupException"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		public RemoteRuntimeStartupException() { }

		/// <summary>�w�肳�ꂽ���b�Z�[�W���g�p���āA<see cref="Microsoft.Scripting.Hosting.Shell.Remote.RemoteRuntimeStartupException"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="message">��O�̏ڍׂ�\�����b�Z�[�W���w�肵�܂��B</param>
		public RemoteRuntimeStartupException(string message) : base(message) { }

		/// <summary>�w�肳�ꂽ���b�Z�[�W�Ɠ�����O���g�p���āA<see cref="Microsoft.Scripting.Hosting.Shell.Remote.RemoteRuntimeStartupException"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="message">��O�̏ڍׂ�\�����b�Z�[�W���w�肵�܂��B</param>
		/// <param name="innerException">���̗�O�̌����ƂȂ�����O���w�肵�܂��B</param>
		public RemoteRuntimeStartupException(string message, Exception innerException) : base(message, innerException) { }

		/// <summary>�V���A���������f�[�^���g�p���āA<see cref="Microsoft.Scripting.Hosting.Shell.Remote.RemoteRuntimeStartupException"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="info">�X���[����Ă����O�Ɋւ���V���A�����ς݃I�u�W�F�N�g �f�[�^��ێ����Ă��� <see cref="System.Runtime.Serialization.SerializationInfo"/>�B</param>
		/// <param name="context">�]�����܂��͓]����Ɋւ���R���e�L�X�g�����܂�ł��� <see cref="System.Runtime.Serialization.StreamingContext"/>�B</param>
		/// <exception cref="System.ArgumentNullException"><paramref name="info"/> �p�����[�^�[�� <c>null</c> �ł��B</exception>
		/// <exception cref="System.Runtime.Serialization.SerializationException">�N���X���� <c>null</c> �ł��邩�A�܂��� <see cref="System.Exception.HResult"/> �� 0 �ł��B</exception>
		protected RemoteRuntimeStartupException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}