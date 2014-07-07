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
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Scripting.Hosting.Providers;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting.Shell
{
	/// <summary>�R�}���h���C�� �z�X�e�B���O �T�[�r�X��\���܂��B</summary>
	public class CommandLine
	{
		/// <summary><see cref="Microsoft.Scripting.Hosting.Shell.CommandLine"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		public CommandLine() { ExitCode = 1; }

		LanguageContext _language;
		ICommandDispatcher _commandDispatcher;
		int? _terminatingExitCode;

		/// <summary>���̃T�[�r�X�̓��o�͂��s���Ă���R���\�[�����擾���܂��B</summary>
		protected IConsole Console { get; private set; }

		/// <summary>���̃T�[�r�X�̃R���\�[���ɑ΂���I�v�V�������擾���܂��B</summary>
		protected ConsoleOptions Options { get; private set; }

		/// <summary>���̃T�[�r�X�����ݎ��s���̌����\�� <see cref="ScriptEngine"/> ���擾���܂��B</summary>
		protected ScriptEngine Engine { get; private set; }

		/// <summary>
		/// ���̃T�[�r�X�ɂ���ăR�}���h�����s����X�R�[�v���擾 (�܂��͐ݒ�) ���܂��B
		/// ���̃v���p�e�B�̐ݒ�͔h���N���X�̂݉\�ł��B
		/// </summary>
		public ScriptScope ScriptScope { get; protected set; }

		/// <summary>���̃R�}���h�̏I���R�[�h���擾 (�܂��͐ݒ�) ���܂��B
		/// ���̃v���p�e�B�̐ݒ�͔h���N���X�̂݉\�ł��B</summary>
		public int ExitCode { get; protected set; }

		/// <summary><see cref="ScriptScope"/> �ɑ΂��铯���A�v���P�[�V�����h���C�����ł̂ݓ��삷�郊���[�g�\�ł͂Ȃ� <see cref="Microsoft.Scripting.Runtime.Scope"/> �I�u�W�F�N�g���擾���܂��B</summary>
		protected Scope Scope
		{
			get { return ScriptScope == null ? null : HostingHelpers.GetScope(ScriptScope); }
			set { ScriptScope = HostingHelpers.CreateScriptScope(Engine, value); }
		}

		/// <summary><see cref="Engine"/> �ɑ΂��铯���A�v���P�[�V�����h���C�����ł̂ݓ��삷�郊���[�g�\�ł͂Ȃ� <see cref="LanguageContext"/> �I�u�W�F�N�g���擾���܂��B</summary>
		protected LanguageContext Language { get { return _language ?? (_language = HostingHelpers.GetLanguageContext(Engine)); } }

		/// <summary>���̃R�}���h���C���ɂ��v�����v�g���擾���܂��B</summary>
		protected virtual string Prompt { get { return ">>> "; } }

		/// <summary>���̃R�}���h���C���ɂ�镡���s���͂� 2 �s�ڈȍ~�̃v�����v�g���擾���܂��B</summary>
		public virtual string PromptContinuation { get { return "... "; } }

		/// <summary>�ʏ�Θb���[�v�̊J�n���ɕ\������郍�S���擾���܂��B</summary>
		protected virtual string Logo { get { return null; } }

		/// <summary>���̃T�[�r�X�̏������������s���܂��B</summary>
		protected virtual void Initialize()
		{
			if (_commandDispatcher == null)
				_commandDispatcher = CreateCommandDispatcher();
		}

		/// <summary>���̃R�}���h���C�� �z�X�e�B���O �T�[�r�X���g�p����V���� <see cref="Scope"/> ���쐬���܂��B</summary>
		/// <returns>�쐬���ꂽ�V���� <see cref="Scope"/>�B</returns>
		protected virtual Scope CreateScope() { return new Scope(); }

		/// <summary>���̃R�}���h���C�� �z�X�e�B���O �T�[�r�X���g�p����V���� <see cref="ICommandDispatcher"/> ���쐬���܂��B</summary>
		/// <returns>�쐬���ꂽ�V���� <see cref="ICommandDispatcher"/>�B</returns>
		protected virtual ICommandDispatcher CreateCommandDispatcher() { return new SimpleCommandDispatcher(); }

		// ����̎����̓t���O��ݒ肷�邾���ł��B�h���N���X�ł͂��悢�I�����T�|�[�g�ł��܂��B
		/// <summary>�I���R�[�h���w�肵�āA���̃T�[�r�X���I�����܂��B</summary>
		/// <param name="exitCode">�I���Ɋւ���I���R�[�h���w�肵�܂��B</param>
		public virtual void Terminate(int exitCode) { _terminatingExitCode = exitCode; }

		/// <summary>�R�}���h���C�������s���܂��B</summary>
		/// <param name="engine">�T�[�r�X�����s���錾���\�� <see cref="ScriptEngine"/> ���w�肵�܂��B</param>
		/// <param name="console">�T�[�r�X�̓��o�͂��s���R���\�[�����w�肵�܂��B</param>
		/// <param name="options">�T�[�r�X�̃R���\�[���ɑ΂���I�v�V�������w�肵�܂��B</param>
		public void Run(ScriptEngine engine, IConsole console, ConsoleOptions options)
		{
			ContractUtils.RequiresNotNull(engine, "engine");
			ContractUtils.RequiresNotNull(console, "console");
			ContractUtils.RequiresNotNull(options, "options");
			Engine = engine;
			Options = options;
			Console = console;
			Initialize();
			try
			{
				ExitCode = Run();
			}
			catch (ThreadAbortException tae)
			{
				if (tae.ExceptionState is KeyboardInterruptException)
				{
					Thread.ResetAbort();
					ExitCode = -1;
				}
				else
					throw;
			}
			finally
			{
				Shutdown();
				Engine = null;
				Options = null;
				Console = null;
			}
		}

		/// <summary>�R�}���h���C�������s���܂��B����͂��̃��\�b�h���I�[�o�[���C�h���邱�ƂŁA�P��̃R�}���h���邢�̓t�@�C���̎��s�A�Θb���[�v�̊J�n�ȊO�̓����񋟂ł��܂��B</summary>
		/// <returns>�I���R�[�h�B</returns>
		protected virtual int Run()
		{
			int result;
			if (Options.Command != null)
				result = RunCommand(Options.Command);
			else if (Options.FileName != null)
				result = RunFile(Options.FileName);
			else
				return RunInteractive();
			if (Options.Introspection)
				return RunInteractiveLoop();
			return result;
		}

		/// <summary>���̃R�}���h���C���̏I�����ɌĂ΂�܂��B����̎����ł͊�ɂȂ铮�I���ꃉ���^�C�����V���b�g�_�E�����܂��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		protected virtual void Shutdown()
		{
			try { Engine.Runtime.Shutdown(); }
			catch (Exception ex) { UnhandledException(ex); }
		}

		/// <summary>�w�肳�ꂽ�t�@�C�����̃t�@�C�������s���āA�I���R�[�h��Ԃ��܂��B</summary>
		/// <param name="fileName">�t�@�C�������w�肵�܂��B</param>
		/// <returns>�I���R�[�h�B</returns>
		protected virtual int RunFile(string fileName) { return RunFile(Engine.CreateScriptSourceFromFile(fileName)); }

		/// <summary>�w�肳�ꂽ�R�}���h�����s���āA�I���R�[�h��Ԃ��܂��B</summary>
		/// <param name="command">�R�}���h��\����������w�肵�܂��B</param>
		/// <returns>�I���R�[�h�B</returns>
		protected virtual int RunCommand(string command) { return RunFile(Engine.CreateScriptSourceFromString(command, SourceCodeKind.Statements)); }

		/// <summary>�w�肳�ꂽ <see cref="ScriptSource"/> �����s���āA�I���R�[�h��Ԃ��܂��B</summary>
		/// <param name="source">���s����\�[�X�R�[�h��\�� <see cref="ScriptSource"/> ���w�肵�܂��B</param>
		/// <returns>�I���R�[�h�B</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		protected virtual int RunFile(ScriptSource source)
		{
			int result = 1;
			if (Options.HandleExceptions)
			{
				try { result = source.ExecuteProgram(); }
				catch (Exception ex) { UnhandledException(ex); }
			}
			else
				result = source.ExecuteProgram();
			return result;
		}

		/// <summary>���S���R���\�[���ɕ\�����܂��B</summary>
		protected void PrintLogo()
		{
			if (Logo != null)
				Console.Write(Logo, Style.Out);
		}

		/// <summary>�Θb���[�v���J�n���܂��B���[�v�̊J�n�O�ɂ�����K�v�ȏ����������s���A���[�v���J�n���܂��B�Θb���[�v������������A�I���R�[�h��Ԃ��܂��B</summary>
		/// <returns>�I���R�[�h�B</returns>
		protected virtual int RunInteractive()
		{
			PrintLogo();
			return RunInteractiveLoop();
		}

		/// <summary>
		/// �Θb���[�v�����s���܂��B
		/// �I���R�[�h����������܂ŌJ��Ԃ��Θb�������́A���s���܂��B
		/// �n���h������Ȃ��������O�̓R���\�[����ʂ��ă��[�U�[�ɕ\������܂��B
		/// </summary>
		/// <returns>�I���R�[�h�B</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		protected int RunInteractiveLoop()
		{
			if (ScriptScope == null)
				ScriptScope = Engine.CreateScope();
			if (Options.RemoteRuntimeChannel != null)
			{
				// Publish the ScriptScope so that the host can use it
				Remote.RemoteRuntimeServer.StartServer(Options.RemoteRuntimeChannel, ScriptScope);
				return 0;
			}
			int? res = null;
			do
			{
				if (Options.HandleExceptions)
				{
					try { res = TryInteractiveAction(); }
					catch (Exception ex)
					{
						if (IsFatalException(ex))
							// Some exceptions are too dangerous to try to catch
							throw;
						// There should be no unhandled exceptions in the interactive session
						// We catch all (most) exceptions here, and just display it, and keep on going
						UnhandledException(ex);
					}
				}
				else
					res = TryInteractiveAction();
			} while (res == null);
			return (int)res;
		}

		/// <summary>�w�肳�ꂽ��O���v���I�ł��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="ex">���ׂ��O���w�肵�܂��B</param>
		/// <returns>��O���v���I�ł������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		internal static bool IsFatalException(Exception ex)
		{
			ThreadAbortException tae = ex as ThreadAbortException;
			return tae != null && !(tae.ExceptionState is KeyboardInterruptException);
		}

		/// <summary>�n���h������Ȃ���O���������ꍇ�ɌĂ΂�܂��B</summary>
		/// <param name="ex">�n���h������Ȃ�������O�B</param>
		protected virtual void UnhandledException(Exception ex) { Console.WriteLine(Engine.GetService<ExceptionOperations>().FormatException(ex), Style.Error); }

		/// <summary>
		/// �P��̑Θb����̎��s����т����錾��ŗL�̗�O�̃n���h�������݂܂��B
		/// �h���N���X�͂��̃��\�b�h���I�[�o�[���C�h���āA�ʂ̗�O�n���h�����O��ǉ��ł��܂��B
		/// </summary>
		/// <returns>�������Ď��s���p���ł���ꍇ�� <c>null</c>�B����ȊO�̏ꍇ�͏I���R�[�h���w�肵�܂��B</returns>
		protected virtual int? TryInteractiveAction()
		{
			int? result = null;
			try { result = RunOneInteraction(); }
			catch (ThreadAbortException tae)
			{
				if (tae.ExceptionState is KeyboardInterruptException)
				{
					UnhandledException(tae);
					Thread.ResetAbort();
				}
				else
					throw;
			}
			return result;
		}

		/// <summary>
		/// �P��̑Θb�R�}���h�܂��̓X�e�[�g�����g�Z�b�g����͂���ю��s���܂��B
		/// �ǂݎ��ꂽ�R�[�h���Θb�R�}���h���X�e�[�g�����g�ł��邩�͉��s�ɂ���Ĕ��f���܂��B
		/// �R�[�h�����s���܂�ł���ꍇ�A����� (�����炭 SendToConsole ���痈��) �X�e�[�g�����g�Z�b�g�ł��B
		/// �R�[�h�����s���܂܂Ȃ��ꍇ�A����̓v�����v�g�Ń��[�U�[�����͂����Θb�R�}���h�ł��B
		/// </summary>
		/// <returns>�������Ď��s���p���ł���ꍇ�� <c>null</c>�B����ȊO�̏ꍇ�͓K�؂ȏI���R�[�h�B</returns>
		int? RunOneInteraction()
		{
			bool continueInteraction;
			var s = ReadStatement(out continueInteraction);
			if (continueInteraction == false)
				return _terminatingExitCode == null ? 0 : _terminatingExitCode;
			if (string.IsNullOrEmpty(s))
			{
				// ��s?
				Console.Write(string.Empty, Style.Out);
				return null;
			}
			ExecuteCommand(s);
			return null;
		}

		/// <summary>�Θb���[�v�Ŏw�肳�ꂽ�R�}���h�����s���܂��B</summary>
		/// <param name="command">���s����R�}���h���w�肵�܂��B</param>
		protected virtual void ExecuteCommand(string command) { ExecuteCommand(Engine.CreateScriptSourceFromString(command, SourceCodeKind.InteractiveCode)); }

		/// <summary>�Θb���[�v�Ŏw�肳�ꂽ <see cref="ScriptSource"/> �����s���܂��B</summary>
		/// <param name="source">���s���� <see cref="ScriptSource"/></param>
		/// <returns>���s�̌��ʁB</returns>
		protected object ExecuteCommand(ScriptSource source) { return _commandDispatcher.Execute(source.Compile(Engine.GetCompilerOptions(ScriptScope), new ErrorSinkProxyListener(ErrorSink)), ScriptScope); }

		/// <summary>�G���[���������� <see cref="Microsoft.Scripting.ErrorSink"/> ���擾���܂��B</summary>
		protected virtual ErrorSink ErrorSink { get { return ErrorSink.Default; } }

		/// <summary>�w�肳�ꂽ���͂���s�Ƃ��Ĉ������ǂ����𔻒f���܂��B�����C���f���g���ꂽ�e�L�X�g�ɂ��Ă݂̂�������s���܂��B</summary>
		static bool TreatAsBlankLine(string line, int autoIndentSize) { return line.Length == 0 || autoIndentSize != 0 && line.Trim().Length == 0 && line.Length == autoIndentSize; }

		/// <summary>�X�e�[�g�����g��ǂݎ��܂��B�X�e�[�g�����g�� (�N���X�錾�̂悤��) ���ݓI�ɕ����s�̃X�e�[�g�����g�Z�b�g�ɂȂ�\����������̂ł��B</summary>
		/// <param name="continueInteraction">�R���\�[���Z�b�V�������p�������邩�ǂ����������l�B</param>
		/// <returns>�]�����ꂽ���B��̓��͂ɂ��Ă� <c>null</c>�B</returns>
		protected string ReadStatement(out bool continueInteraction)
		{
			StringBuilder b = new StringBuilder();
			int autoIndentSize = 0;
			Console.Write(Prompt, Style.Prompt);
			while (true)
			{
				var line = ReadLine(autoIndentSize);
				continueInteraction = true;
				if (line == null || _terminatingExitCode != null)
				{
					continueInteraction = false;
					return null;
				}
				var allowIncompleteStatement = TreatAsBlankLine(line, autoIndentSize);
				b.Append(line);
				// Note that this does not use Environment.NewLine because some languages (eg. Python) only recognize \n as a line terminator.
				b.Append("\n");
				var code = b.ToString();
				var props = GetCommandProperties(code);
				if (ScriptCodeParseResultUtils.IsCompleteOrInvalid(props, allowIncompleteStatement))
					return props != ScriptCodeParseResult.Empty ? code : null;
				if (Options.AutoIndent && Options.AutoIndentSize != 0)
					autoIndentSize = GetNextAutoIndentSize(code);
				// Keep on reading input
				Console.Write(PromptContinuation, Style.Prompt);
			}
		}

		/// <summary>�w�肳�ꂽ�\�[�X�R�[�h�ɂ��Ẳ�͌��ʂ��擾���܂��B</summary>
		/// <param name="code">��͂���R�[�h���w�肵�܂��B</param>
		/// <returns>��͌��ʂ�\�� <see cref="ScriptCodeParseResult"/>�B</returns>
		protected virtual ScriptCodeParseResult GetCommandProperties(string code) { return Engine.CreateScriptSourceFromString(code, SourceCodeKind.InteractiveCode).FetchCodeProperties(Engine.GetCompilerOptions(ScriptScope)); }

		/// <summary>���s�̎����C���f���g�T�C�Y���擾���܂��B</summary>
		/// <param name="text">���݂̃R�[�h���w�肵�܂��B</param>
		/// <returns>���s�̎����C���f���g�T�C�Y�B</returns>
		protected virtual int GetNextAutoIndentSize(string text) { return 0; }

		/// <summary>�w�肳�ꂽ�����C���f���g�T�C�Y���g�p���čs��ǂݎ��܂��B</summary>
		/// <param name="autoIndentSize">�s�ɓK�p����鎩���C���f���g�T�C�Y���w�肵�܂��B</param>
		/// <returns>�ǂݎ��ꂽ�s�B</returns>
		protected virtual string ReadLine(int autoIndentSize) { return Console.ReadLine(autoIndentSize); }

		//private static DynamicSite<object, IList<string>>  _memberCompletionSite = new DynamicSite<object, IList<string>>(OldDoOperationAction.Make(Operators.GetMemberNames));

		/// <summary>�w�肳�ꂽ�R�[�h�Ɋւ��郁���o���̈ꗗ���擾���܂��B</summary>
		/// <param name="code">�����o���擾����Ώۂ̃R�[�h���w�肵�܂��B</param>
		/// <returns>�擾���ꂽ�����o�̖��O�B</returns>
		public IList<string> GetMemberNames(string code)
		{
			return Engine.Operations.GetMemberNames((object)Engine.CreateScriptSourceFromString(code, SourceCodeKind.Expression).Execute(ScriptScope));
			// TODO: why doesn't this work ???
			//return _memberCompletionSite.Invoke(new CodeContext(_scope, _engine), value);
		}

		/// <summary>�w�肳�ꂽ���O�̃O���[�o���ϐ����̈ꗗ���擾���܂��B</summary>
		/// <param name="name">�������閼�O���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ���O�̃O���[�o���ϐ����B</returns>
		public virtual IList<string> GetGlobals(string name) { return ScriptScope.GetVariableNames().Where(x => x.StartsWith(name)).ToArray(); }

		class SimpleCommandDispatcher : ICommandDispatcher
		{
			public object Execute(CompiledCode compiledCode, ScriptScope scope) { return compiledCode.Execute(scope); }
		}
	}
}
