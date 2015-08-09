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
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.Serialization;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting.Shell
{
	/// <summary>�����Ȍ`���̃I�v�V�����ɑ��������ꍇ�ɃX���[������O��\���܂��B</summary>
	[Serializable]
	public class InvalidOptionException : Exception
	{
		/// <summary><see cref="Microsoft.Scripting.Hosting.Shell.InvalidOptionException"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		public InvalidOptionException() { }

		/// <summary>�w�肳�ꂽ���b�Z�[�W���g�p���āA<see cref="Microsoft.Scripting.Hosting.Shell.InvalidOptionException"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="message">��O�̏ڍׂ�\�����b�Z�[�W���w�肵�܂��B</param>
		public InvalidOptionException(string message) : base(message) { }

		/// <summary>�w�肳�ꂽ���b�Z�[�W�Ɠ�����O���g�p���āA<see cref="Microsoft.Scripting.Hosting.Shell.InvalidOptionException"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="message">��O�̏ڍׂ�\�����b�Z�[�W���w�肵�܂��B</param>
		/// <param name="innerException">���̗�O�̌����ƂȂ�����O���w�肵�܂��B</param>
		public InvalidOptionException(string message, Exception innerException) : base(message, innerException) { }

		/// <summary>�V���A���������f�[�^���g�p���āA<see cref="Microsoft.Scripting.Hosting.Shell.InvalidOptionException"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="info">�X���[����Ă����O�Ɋւ���V���A�����ς݃I�u�W�F�N�g �f�[�^��ێ����Ă��� <see cref="System.Runtime.Serialization.SerializationInfo"/>�B</param>
		/// <param name="context">�]�����܂��͓]����Ɋւ���R���e�L�X�g�����܂�ł��� <see cref="System.Runtime.Serialization.StreamingContext"/>�B</param>
		/// <exception cref="System.ArgumentNullException"><paramref name="info"/> �p�����[�^�[�� <c>null</c> �ł��B</exception>
		/// <exception cref="System.Runtime.Serialization.SerializationException">�N���X���� <c>null</c> �ł��邩�A�܂��� <see cref="System.Exception.HResult"/> �� 0 �ł��B</exception>
		protected InvalidOptionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}

	/// <summary>�N�����̈������� <see cref="ConsoleOptions"/> ����͂��܂��B���̃N���X�͒��ۃN���X�ł��B</summary>
	public abstract class OptionsParser
	{
		string[] _args;
		int _current = -1;

		/// <summary><see cref="Microsoft.Scripting.Hosting.Shell.OptionsParser"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		protected OptionsParser() { IgnoredArgs = new List<string>(); }

		/// <summary>�����̉�͂ɂ���ĕύX�����\���̂��� <see cref="ScriptRuntimeSetup"/> ���擾���܂��B</summary>
		public ScriptRuntimeSetup RuntimeSetup { get; private set; }

		/// <summary>�����̉�͂ɂ���ĕύX�����\���̂��� <see cref="LanguageSetup"/> ���擾���܂��B</summary>
		public LanguageSetup LanguageSetup { get; private set; }

		/// <summary>��͂Ŏg�p����� <see cref="PlatformAdaptationLayer"/> ���擾���܂��B</summary>
		public PlatformAdaptationLayer Platform { get; private set; }

		/// <summary>�����̉�͂ɂ���ĕύX����� <see cref="ConsoleOptions"/> ���擾���܂��B</summary>
		public abstract ConsoleOptions CommonConsoleOptions { get; }

		/// <summary>��͂��X�L�b�v���ꂽ�����̃��X�g���擾���܂��B</summary>
		public IList<string> IgnoredArgs { get; private set; }

		/// <summary>�w�肳�ꂽ�N�����̈�������͂��邱�ƂŁA�Z�b�g�A�b�v�����X�V���܂��B</summary>
		/// <param name="args">��͂���������w�肵�܂��B</param>
		/// <param name="setup">��͂ɂ���ĕύX�����\���̂��� <see cref="ScriptRuntimeSetup"/> ���w�肵�܂��B</param>
		/// <param name="languageSetup">��͂ɂ���ĕύX�����\���̂��� <see cref="LanguageSetup"/> ���w�肵�܂��B</param>
		/// <param name="platform">��͂Ɏg�p����� <see cref="PlatformAdaptationLayer"/> ���w�肵�܂��B</param>
		/// <exception cref="InvalidOptionException">On error.</exception>
		public void Parse(string[] args, ScriptRuntimeSetup setup, LanguageSetup languageSetup, PlatformAdaptationLayer platform)
		{
			ContractUtils.RequiresNotNull(args, "args");
			ContractUtils.RequiresNotNull(setup, "setup");
			ContractUtils.RequiresNotNull(languageSetup, "languageSetup");
			ContractUtils.RequiresNotNull(platform, "platform");
			_args = args;
			RuntimeSetup = setup;
			LanguageSetup = languageSetup;
			Platform = platform;
			_current = 0;
			try
			{
				BeforeParse();
				while (_current < args.Length)
					ParseArgument(args[_current++]);
				AfterParse();
			}
			finally
			{
				_args = null;
				RuntimeSetup = null;
				LanguageSetup = null;
				Platform = null;
				_current = -1;
			}
		}

		/// <summary>�����̉�͂̒��O�ɌĂ΂�܂��B</summary>
		protected virtual void BeforeParse() { }

		/// <summary>�����̉�͂̒���ɌĂ΂�܂��B</summary>
		protected virtual void AfterParse() { }

		/// <summary>�w�肳�ꂽ�P��̈�������͂��܂��B</summary>
		/// <param name="arg">��͂�������̒l���w�肵�܂��B</param>
		protected abstract void ParseArgument(string arg);

		/// <summary>��͂���Ȃ������c��̈����𖳎����܂��B</summary>
		protected void IgnoreRemainingArgs()
		{
			while (_current < _args.Length)
				IgnoredArgs.Add(_args[_current++]);
		}

		/// <summary>�����̓ǂݎ��ʒu���Ō���Ɉړ������āA�ǂݎ�������ׂĂ̈�����Ԃ��܂��B</summary>
		/// <returns>�ǂݎ�������ׂĂ̈������܂ޔz��B</returns>
		protected string[] PopRemainingArgs()
		{
			var result = ArrayUtils.ShiftLeft(_args, _current);
			_current = _args.Length;
			return result;
		}

		/// <summary>���̈����̒l���擾���܂��B</summary>
		/// <returns>���̈����̒l�B</returns>
		/// <exception cref="InvalidOptionException">���ݓǂݎ���Ă�������͍Ō�̈����ł��B</exception>
		protected string PeekNextArg()
		{
			if (_current < _args.Length)
				return _args[_current];
			else
				throw new InvalidOptionException(string.Format(CultureInfo.CurrentCulture, "�I�v�V���� '{0}' �ɂ͒l���K�v�ł��B", _current > 0 ? _args[_current - 1] : ""));
		}

		/// <summary>���̈�����ǂݎ��A�ǂݎ��ʒu�� 1 ��ɐi�߂܂��B</summary>
		/// <returns>���̈����̒l�B</returns>
		/// <exception cref="InvalidOptionException">���ݓǂݎ���Ă�������͍Ō�̈����ł��B</exception>
		protected string PopNextArg()
		{
			var result = PeekNextArg();
			_current++;
			return result;
		}

		/// <summary>�ǂݎ��ʒu�� 1 �O�ɖ߂��܂��B</summary>
		protected void PushArgBack() { _current--; }

		/// <summary>�w�肳�ꂽ�I�v�V�������ɂ����Ďw�肳�ꂽ�l�������ł��邱�Ƃ����� <see cref="InvalidOptionException"/> ��Ԃ��܂��B</summary>
		/// <param name="option">�I�v�V���������w�肵�܂��B</param>
		/// <param name="value">�I�v�V�����̒l���w�肵�܂��B</param>
		/// <returns>�l�������ł��邱�Ƃ����� <see cref="InvalidOptionException"/>�B</returns>
		protected static Exception InvalidOptionValue(string option, string value) { return new InvalidOptionException(string.Format("'{0}' �̓I�v�V���� '{1}' �ɑ΂���L���Ȓl�ł͂���܂���B", value, option)); }

		/// <summary>�R�}���h���C�� �I�v�V�����̃w���v���擾���܂��B</summary>
		/// <returns>�w���v���i�[���� <see cref="OptionsHelp"/>�B</returns>
		public abstract OptionsHelp GetHelp();
	}

	/// <summary><see cref="OptionsParser.GetHelp"/> ���\�b�h�ɂ���ĕԂ����R�}���h���C�� �I�v�V�����̃w���v��\���܂��B</summary>
	public class OptionsHelp
	{
		/// <summary><see cref="Microsoft.Scripting.Hosting.Shell.OptionsHelp"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="commandLine">�R�}���h���C���̃t�H�[�}�b�g��������������w�肵�܂��B</param>
		/// <param name="options">�I�v�V����������ѐ������i�[����V�[�P���X���w�肵�܂��B</param>
		/// <param name="environmentVariables">���ϐ��̖��O����ѐ������i�[����V�[�P���X���w�肵�܂��B</param>
		/// <param name="comments">�R�����g��������������w�肵�܂��B</param>
		public OptionsHelp(string commandLine, IEnumerable<KeyValuePair<string, string>> options, IEnumerable<KeyValuePair<string, string>> environmentVariables, string comments)
		{
			CommandLine = commandLine;
			Options = options.ToReadOnly();
			EnvironmentVariables = environmentVariables.ToReadOnly();
			Comments = comments;
		}

		/// <summary>�R�}���h���C���̃t�H�[�}�b�g��������������擾���܂��B</summary>
		public string CommandLine { get; private set; }

		/// <summary>�I�v�V����������ѐ������i�[����R���N�V�������擾���܂��B</summary>
		public ReadOnlyCollection<KeyValuePair<string, string>> Options { get; private set; }

		/// <summary>���ϐ��̖��O����ѐ������i�[����R���N�V�������擾���܂��B</summary>
		public ReadOnlyCollection<KeyValuePair<string, string>> EnvironmentVariables { get; private set; }

		/// <summary>�R�����g��������������擾���܂��B</summary>
		public string Comments { get; private set; }
	}

	/// <summary>�w�肳�ꂽ����ŗL�̃I�v�V��������͂��� <see cref="OptionsParser"/> ��\���܂��B</summary>
	/// <typeparam name="TConsoleOptions">����ŗL�̃I�v�V�������w�肵�܂��B</typeparam>
	public class OptionsParser<TConsoleOptions> : OptionsParser where TConsoleOptions : ConsoleOptions, new()
	{
		TConsoleOptions _consoleOptions;
		bool _saveAssemblies = false;
		string _assembliesDir = null;

		/// <summary><see cref="Microsoft.Scripting.Hosting.Shell.OptionsParser&lt;TConsoleOptions&gt;"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		public OptionsParser() { }

		/// <summary>�����̉�͂ɂ���ĕύX����錾��ŗL�̃I�v�V�������擾�܂��͐ݒ肵�܂��B</summary>
		public TConsoleOptions ConsoleOptions
		{
			get { return _consoleOptions ?? (_consoleOptions = new TConsoleOptions()); }
			set
			{
				ContractUtils.RequiresNotNull(value, "value");
				_consoleOptions = value;
			}
		}

		/// <summary>�����̉�͂ɂ���ĕύX����� <see cref="ConsoleOptions"/> ���擾���܂��B</summary>
		public sealed override ConsoleOptions CommonConsoleOptions { get { return ConsoleOptions; } }

		/// <summary>�w�肳�ꂽ�P��̈�������͂��܂��B</summary>
		/// <param name="arg">��͂�������̒l���w�肵�܂��B</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		protected override void ParseArgument(string arg)
		{
			ContractUtils.RequiresNotNull(arg, "arg");
			// the following extension switches are in alphabetic order
			switch (arg)
			{
				case "-h":
				case "-help":
				case "-?":
				case "/?":
					ConsoleOptions.PrintUsage = true;
					ConsoleOptions.Exit = true;
					IgnoreRemainingArgs();
					break;
				case "-D":
					RuntimeSetup.DebugMode = true;
					break;
				case "-X:PrivateBinding":
					RuntimeSetup.PrivateBinding = true;
					break;
				case "-X:PassExceptions": ConsoleOptions.HandleExceptions = false; break;
				// TODO: #if !IRONPYTHON_WINDOW
				case "-X:ColorfulConsole": ConsoleOptions.ColorfulConsole = true; break;
				case "-X:TabCompletion": ConsoleOptions.TabCompletion = true; break;
				case "-X:AutoIndent": ConsoleOptions.AutoIndent = true; break;
				//#endif
#if DEBUG
				case "-X:AssembliesDir":
					_assembliesDir = PopNextArg();
					break;
				case "-X:SaveAssemblies":
					_saveAssemblies = true;
					break;
				case "-X:TrackPerformance":
					SetDlrOption(arg.Substring(3));
					break;
#endif
				// TODO: remove
				case "-X:Interpret":
					LanguageSetup.Options["InterpretedMode"] = ScriptingRuntimeHelpers.True;
					break;
				case "-X:NoAdaptiveCompilation":
					LanguageSetup.Options["NoAdaptiveCompilation"] = true;
					break;
				case "-X:CompilationThreshold":
					LanguageSetup.Options["CompilationThreshold"] = Int32.Parse(PopNextArg());
					break;
				case "-X:ExceptionDetail":
				case "-X:ShowClrExceptions":
#if DEBUG
				case "-X:PerfStats":
#endif
					// TODO: separate options dictionary?
					LanguageSetup.Options[arg.Substring(3)] = ScriptingRuntimeHelpers.True;
					break;
				case Remote.RemoteRuntimeServer.RemoteRuntimeArg:
					ConsoleOptions.RemoteRuntimeChannel = PopNextArg();
					break;
				default:
					ConsoleOptions.FileName = arg.Trim();
					break;
			}
			if (_saveAssemblies)
				Snippets.SetSaveAssemblies(true, _assembliesDir);
		}

		/// <summary>���ϐ��ɓ��I���ꃉ���^�C���Ɋւ���I�v�V������ݒ肵�܂��B</summary>
		/// <param name="option">�ݒ肷��I�v�V�������w�肵�܂��B</param>
		internal static void SetDlrOption(string option) { SetDlrOption(option, "true"); }

		// Note: this works because it runs before the compiler picks up the environment variable
		/// <summary>���ϐ��ɓ��I���ꃉ���^�C���Ɋւ���I�v�V������ݒ肵�܂��B</summary>
		/// <param name="option">�ݒ肷��I�v�V�����̖��O���w�肵�܂��B</param>
		/// <param name="value">�ݒ肷��I�v�V�����̒l���w�肵�܂��B</param>
		internal static void SetDlrOption(string option, string value) { Environment.SetEnvironmentVariable("DLR_" + option, value); }

		/// <summary>�R�}���h���C�� �I�v�V�����̃w���v���擾���܂��B</summary>
		/// <returns>�w���v���i�[���� <see cref="OptionsHelp"/>�B</returns>
		public override OptionsHelp GetHelp()
		{
			var options = new [] {
				new KeyValuePair<string, string>("-c cmd",                          "�v���O������������Ƃ��ēn����܂� (�I�v�V�������X�g�̍Ō�Ɏw�肵�܂�)�B"),
				new KeyValuePair<string, string>("-h",                              "�g�p�@��\�����܂��B"),
#if !IRONPYTHON_WINDOW															    
				new KeyValuePair<string, string>("-i",                              "�X�N���v�g���s��ɑΘb�I�Ɍ������܂��B"),
#endif																			    
				new KeyValuePair<string, string>("-V",                              "�o�[�W�����ԍ���\�����ďI�����܂��B"),
				new KeyValuePair<string, string>("-D",                              "�A�v���P�[�V�����̃f�o�b�O��L���ɂ��܂��B"),
				new KeyValuePair<string, string>("-X:AutoIndent",                   "REPL �Ŏ����C���f���g��L���ɂ��܂��B"),
				new KeyValuePair<string, string>("-X:ExceptionDetail",              "��O�ڍ׃��[�h��L���ɂ��܂��B"),
				new KeyValuePair<string, string>("-X:NoAdaptiveCompilation",        "�K���I�R���p�C���𖳌��ɂ��܂��B"),
				new KeyValuePair<string, string>("-X:CompilationThreshold",         "�C���^�v���^���R���p�C�����J�n����O�̌J��Ԃ��񐔂ł��B"),
				new KeyValuePair<string, string>("-X:PassExceptions",               "�X�N���v�g�R�[�h�ɂ���ăn���h������Ȃ���O���L���b�`���܂���B"),
				new KeyValuePair<string, string>("-X:PrivateBinding",               "�v���C�x�[�g�ȃ����o�ւ̃o�C���f�B���O��L���ɂ��܂��B"),
				new KeyValuePair<string, string>("-X:ShowClrExceptions",            "CLS ��O����\�����܂��B"),
				new KeyValuePair<string, string>("-X:TabCompletion",                "�^�u�⊮���[�h��L���ɂ��܂��B"),
				new KeyValuePair<string, string>("-X:ColorfulConsole",              "�F���̃R���\�[����L���ɂ��܂��B"),
#if DEBUG
				new KeyValuePair<string, string>("-X:AssembliesDir <�f�B���N�g��>", "�������ꂽ�A�Z���u���̕ۑ��̂��߂̃f�B���N�g����ݒ肵�܂��B[�f�o�b�O�̂�]"),
				new KeyValuePair<string, string>("-X:SaveAssemblies",               "�������ꂽ�A�Z���u����ۑ����܂��B[�f�o�b�O�̂�]"),
				new KeyValuePair<string, string>("-X:TrackPerformance",             "�p�t�H�[�}���X�ɕq���ȗ̈��ǐՂ��܂��B[�f�o�b�O�̂�]"),
				new KeyValuePair<string, string>("-X:PerfStats",                    "�v���Z�X�I�����Ƀp�t�H�[�}���X���v��\�����܂��B[�f�o�b�O�̂�]"),
				new KeyValuePair<string, string>(Remote.RemoteRuntimeServer.RemoteRuntimeArg + " <�`�����l����>", "���u�R���\�[���Z�b�V�����ɑ΂��鉓�u�T�[�o���J�n���܂��B"),
#endif
			};
			return new OptionsHelp("[�I�v�V����] [�t�@�C��|- [����]]", options, null, null);
		}
	}
}
