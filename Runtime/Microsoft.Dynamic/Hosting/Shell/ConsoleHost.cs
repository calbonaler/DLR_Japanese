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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting.Shell
{
	/// <summary>�Θb�R���\�[�����������邽�߂̊�{�I�ȋ@�\��񋟂��܂��B���̃N���X�͋�̓I�Ȏ����̂��߂Ɍp�������K�v������܂��B</summary>
	public abstract class ConsoleHost
	{
		internal OptionsParser _languageOptionsParser;

		/// <summary>�J�n���ɗ^����ꂽ�R���\�[�� �z�X�g�ɑ΂���I�v�V�������擾���܂��B</summary>
		public ConsoleHostOptions Options { get { return ConsoleHostOptionsParser.Options; } }

		/// <summary>�J�n���ɍ쐬���ꂽ <see cref="ScriptRuntime"/> �̃Z�b�g�A�b�v�����擾���܂��B</summary>
		public ScriptRuntimeSetup RuntimeSetup { get { return ConsoleHostOptionsParser.RuntimeSetup; } }

		/// <summary>�R���\�[���̃R�}���h�����s���錾���\�� <see cref="ScriptEngine"/> ���擾 (�܂��͐ݒ�) ���܂��B�ݒ�͔h���N���X�̂݉\�ł��B</summary>
		public ScriptEngine Engine { get; protected set; }

		/// <summary>�Z�b�g�A�b�v���ꂽ�����^�C����\�� <see cref="ScriptRuntime"/> ���擾 (�܂��͐ݒ�) ���܂��B�ݒ�͔h���N���X�̂݉\�ł��B</summary>
		public ScriptRuntime Runtime { get; protected set; }

		/// <summary>�R���\�[���̏I���R�[�h���擾�܂��͐ݒ肵�܂��B</summary>
		protected int ExitCode { get; set; }

		/// <summary>�J�n���Ɉ�������͂��� <see cref="Microsoft.Scripting.Hosting.Shell.ConsoleHostOptionsParser"/> ���擾�܂��͐ݒ肵�܂��B</summary>
		protected ConsoleHostOptionsParser ConsoleHostOptionsParser { get; set; }

		/// <summary>�R�}���h���C���ȂǂŎg�p�������o�̓R���\�[�����擾�܂��͐ݒ肵�܂��B</summary>
		protected IConsole ConsoleIO { get; set; }

		/// <summary>���̃z�X�g�����ݕێ����Ă���R�}���h���C�����擾���܂��B</summary>
		protected CommandLine CommandLine { get; private set; }

		/// <summary><see cref="Microsoft.Scripting.Hosting.Shell.ConsoleHost"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		protected ConsoleHost() { }

		/// <summary>�w���v�Ȃǂŕ\�������R���\�[���z�X�g�̃G���g���[�|�C���g���܂�ł�����s�t�@�C���̖��O���擾���܂��B</summary>
		protected virtual string ExeName
		{
			get
			{
				var entryAssembly = Assembly.GetEntryAssembly();
				//Can be null if called from unmanaged code (VS integration scenario)
				return entryAssembly != null ? entryAssembly.GetName().Name : "ConsoleHost";
			}
		}

		/// <summary>�w�肳�ꂽ������ <see cref="ConsoleHostOptionsParser"/> ���g�p���ĉ�͂��܂��B</summary>
		/// <param name="args">��͂���������w�肵�܂��B</param>
		protected virtual void ParseHostOptions(string[] args) { ConsoleHostOptionsParser.Parse(args); }

		/// <summary>�g�p���� <see cref="ScriptRuntime"/> �̃Z�b�g�A�b�v�ɕK�v�� <see cref="ScriptRuntimeSetup"/> ���쐬���܂��B</summary>
		/// <returns>�쐬����я��������ꂽ <see cref="ScriptRuntimeSetup"/>�B</returns>
		protected virtual ScriptRuntimeSetup CreateRuntimeSetup()
		{
			var setup = ScriptRuntimeSetup.ReadConfiguration();
			if (!setup.LanguageSetups.Any(s => s.TypeName == Provider.AssemblyQualifiedName))
			{
				var languageSetup = CreateLanguageSetup();
				if (languageSetup != null)
					setup.LanguageSetups.Add(languageSetup);
			}
			return setup;
		}

		/// <summary>�����\�� <see cref="LanguageSetup"/> ���쐬����я��������܂��B</summary>
		/// <returns>�쐬����я��������ꂽ <see cref="LanguageSetup"/>�B</returns>
		protected virtual LanguageSetup CreateLanguageSetup() { return null; }

		/// <summary>���̃z�X�g�ɂ���Ďg�p����� <see cref="Microsoft.Scripting.PlatformAdaptationLayer"/> ���擾���܂��B</summary>
		protected virtual PlatformAdaptationLayer PlatformAdaptationLayer { get { return PlatformAdaptationLayer.Default; } }

		/// <summary>���̃z�X�g�ɂ���Ďg�p����錾��v���o�C�_�̌^���擾���܂��B</summary>
		protected virtual Type Provider { get { return null; } }

		string GetLanguageProvider(ScriptRuntimeSetup setup)
		{
			if (Provider != null)
				return Provider.AssemblyQualifiedName;
			if (Options.HasLanguageProvider)
				return Options.LanguageProvider;
			if (Options.RunFile != null)
			{
				var lang = setup.LanguageSetups.FirstOrDefault(x => x.FileExtensions.Any(e => DlrConfiguration.FileExtensionComparer.Equals(e, Path.GetExtension(Options.RunFile))));
				if (lang != null)
					return lang.TypeName;
			}
			throw new InvalidOptionException("No language specified.");
		}

		/// <summary>���̃z�X�g�ŃR�}���h���C�����T�|�[�g���� <see cref="CommandLine"/> ���쐬���܂��B</summary>
		/// <returns>�쐬���ꂽ <see cref="CommandLine"/>�B</returns>
		protected virtual CommandLine CreateCommandLine() { return new CommandLine(); }

		/// <summary>��������͂��ăR���\�[���̃I�v�V������ݒ肷�� <see cref="OptionsParser"/> ���쐬���܂��B</summary>
		/// <returns>�쐬���ꂽ <see cref="OptionsParser"/>�B</returns>
		protected virtual OptionsParser CreateOptionsParser() { return new OptionsParser<ConsoleOptions>(); }

		/// <summary>����A�R�}���h���C���A�I�v�V�������g�p���āA<see cref="IConsole"/> �C���^�[�t�F�C�X����������I�u�W�F�N�g���쐬���܂��B</summary>
		/// <param name="engine">�R���\�[���Ŏg�p���錾���\�� <see cref="ScriptEngine"/> ���w�肵�܂��B</param>
		/// <param name="commandLine">�R���\�[���Ɋ֘A�t����R�}���h���C�����w�肵�܂��B</param>
		/// <param name="options">�R���\�[���̃I�v�V������\�� <see cref="ConsoleOptions"/> ���w�肵�܂��B</param>
		/// <returns>�쐬���ꂽ <see cref="IConsole"/> �C���^�[�t�F�C�X�����s����I�u�W�F�N�g�B</returns>
		protected virtual IConsole CreateConsole(ScriptEngine engine, CommandLine commandLine, ConsoleOptions options)
		{
			ContractUtils.RequiresNotNull(options, "options");
			if (options.TabCompletion)
				return CreateSuperConsole(commandLine, options.ColorfulConsole);
			else
				return new BasicConsole(options.ColorfulConsole);
		}

		// The advanced console functions are in a special non-inlined function so that dependencies are pulled in only if necessary.
		[MethodImplAttribute(MethodImplOptions.NoInlining)]
		static IConsole CreateSuperConsole(CommandLine commandLine, bool isColorful) { return new SuperConsole(commandLine, isColorful); }

		/// <summary>(���̃X���b�h����) �R���\�[���� REPL ���[�v���I������悤�ɗv�����܂��B</summary>
		/// <param name="exitCode">�I�����g���K�[����C�x���g�ɑΉ�����I���R�[�h���w�肵�܂��B����� <see cref="M:CommandLine.Run(ScriptEngine, IConsole, ConsoleOptions)"/> ����Ԃ���܂��B</param>
		public virtual void Terminate(int exitCode) { CommandLine.Terminate(exitCode); }

		/// <summary>�v���O�����̃G���g���|�C���g����Ă΂�A�w�肳�ꂽ�����ł̃R���\�[���̎��s���J�n���܂��B</summary>
		/// <param name="args">�J�n���Ƀv���O�����ɗ^����ꂽ�������w�肵�܂��B</param>
		/// <returns>�I���R�[�h�B</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public virtual int Run(string[] args)
		{
			var runtimeSetup = CreateRuntimeSetup();
			ConsoleHostOptionsParser = new ConsoleHostOptionsParser(new ConsoleHostOptions(), runtimeSetup);
			try { ParseHostOptions(args); }
			catch (InvalidOptionException ex)
			{
				Console.Error.WriteLine("Invalid argument: " + ex.Message);
				return ExitCode = 1;
			}
			SetEnvironment();
			var provider = GetLanguageProvider(runtimeSetup);
			LanguageSetup languageSetup = null;
			languageSetup = runtimeSetup.LanguageSetups.Aggregate(languageSetup, (x, y) => y.TypeName == provider ? y : x);
			if (languageSetup == null)
				// the language doesn't have a setup -> create a default one:
				runtimeSetup.LanguageSetups.Add(languageSetup = new LanguageSetup(Provider.AssemblyQualifiedName, Provider.Name));
			// inserts search paths for all languages (/paths option):
			InsertSearchPaths(runtimeSetup.Options, Options.SourceUnitSearchPaths);
			_languageOptionsParser = CreateOptionsParser();
			try { _languageOptionsParser.Parse(Options.IgnoredArgs.ToArray(), runtimeSetup, languageSetup, PlatformAdaptationLayer); }
			catch (InvalidOptionException ex)
			{
				Console.Error.WriteLine(ex.Message);
				return ExitCode = -1;
			}
			if (typeof(DynamicMethod).GetConstructor(new[] { typeof(string), typeof(Type), typeof(Type[]), typeof(bool) }) == null)
			{
				Console.WriteLine(string.Format("{0} requires .NET 2.0 SP1 or later to run.", languageSetup.DisplayName));
				Environment.Exit(1);
			}
			Runtime = new ScriptRuntime(runtimeSetup);
			try { Engine = Runtime.GetEngineByTypeName(provider); }
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex.Message);
				return ExitCode = 1;
			}
			Execute();
			return ExitCode;
		}

		static void InsertSearchPaths(IDictionary<string, object> options, ICollection<string> paths)
		{
			if (options != null && paths != null && paths.Count > 0)
			{
				var existingPaths = new List<string>(LanguageOptions.GetSearchPathsOption(options) ?? (IEnumerable<string>)ArrayUtils.EmptyStrings);
				existingPaths.InsertRange(0, paths);
				options["SearchPaths"] = existingPaths;
			}
		}

		/// <summary>�w���v���R���\�[���ɕ\�����܂��B</summary>
		protected virtual void PrintHelp() { Console.WriteLine(GetHelp()); }

		static void PrintTable(StringBuilder output, IEnumerable<KeyValuePair<string, string>> table)
		{
			Assert.NotNull(output, table);
			var max_width = table.Aggregate(0, (x, y) => System.Math.Max(x, y.Key.Length));
			foreach (var row in table)
			{
				output.Append(" ");
				output.Append(row.Key);
				output.Append(' ', max_width - row.Key.Length + 1);
				output.AppendLine(row.Value);
			}
		}

		/// <summary>�w���v��l�Ԃ��ǂ߂�`���Ŏ擾���܂��B</summary>
		/// <returns></returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		protected virtual string GetHelp()
		{
			StringBuilder sb = new StringBuilder();
			var optionsHelp = Options.GetHelp();
			sb.AppendLine(string.Format("Usage: {0}.exe [<dlr-options>] [--] [<language-specific-command-line>]", ExeName));
			sb.AppendLine();
			sb.AppendLine("DLR options (both slash or dash could be used to prefix options):");
			PrintTable(sb, optionsHelp);
			sb.AppendLine();
			sb.AppendLine("Language specific command line:");
			PrintLanguageHelp(sb);
			sb.AppendLine();
			return sb.ToString();
		}

		/// <summary>����Ɋւ���w���v���w�肳�ꂽ <see cref="StringBuilder"/> �ɒǉ����܂��B</summary>
		/// <param name="output">�w���v��\���������ǉ����� <see cref="StringBuilder"/> ���w�肵�܂��B</param>
		public void PrintLanguageHelp(StringBuilder output)
		{
			ContractUtils.RequiresNotNull(output, "output");
			var help = CreateOptionsParser().GetHelp();
			// only display language specific options if one or more optinos exists.
			if (help.CommandLine != null || help.Options.Count > 0 || help.EnvironmentVariables.Count > 0 || help.Comments != null)
			{
				if (help.CommandLine != null)
				{
					output.AppendLine(help.CommandLine);
					output.AppendLine();
				}
				if (help.Options.Count > 0)
				{
					output.AppendLine("Options:");
					PrintTable(output, help.Options);
					output.AppendLine();
				}
				if (help.EnvironmentVariables.Count > 0)
				{
					output.AppendLine("Environment variables:");
					PrintTable(output, help.EnvironmentVariables);
					output.AppendLine();
				}
				if (help.Comments != null)
				{
					output.Append(help.Comments);
					output.AppendLine();
				}
				output.AppendLine();
			}
		}

		void Execute()
		{
			if (_languageOptionsParser.CommonConsoleOptions.IsMta)
			{
				var thread = new Thread(ExecuteInternal);
				thread.SetApartmentState(ApartmentState.MTA);
				thread.Start();
				thread.Join();
				return;
			}
			ExecuteInternal();
		}

		/// <summary>��͂��ꂽ�I�v�V�����Ɋ�Â��āA�g�p�@��o�[�W������\��������A�R�}���h���C����t�@�C�������s�����肵�܂��B</summary>
		protected virtual void ExecuteInternal()
		{
			Debug.Assert(Engine != null);
			if (_languageOptionsParser.CommonConsoleOptions.PrintVersion)
				PrintVersion();
			if (_languageOptionsParser.CommonConsoleOptions.PrintUsage)
				PrintUsage();
			if (_languageOptionsParser.CommonConsoleOptions.Exit)
			{
				ExitCode = 0;
				return;
			}
			switch (Options.RunAction)
			{
				case ConsoleHostAction.None:
				case ConsoleHostAction.RunConsole:
					ExitCode = RunCommandLine();
					break;
				case ConsoleHostAction.RunFile:
					ExitCode = RunFile();
					break;
				default:
					throw Assert.Unreachable;
			}
		}

		void SetEnvironment()
		{
			Debug.Assert(Options.EnvironmentVars != null);
			foreach (var env in Options.EnvironmentVars)
			{
				if (!string.IsNullOrEmpty(env))
				{
					var var_def = env.Split('=');
					Environment.SetEnvironmentVariable(var_def[0], (var_def.Length > 1) ? var_def[1] : "");
				}
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		int RunFile()
		{
			Debug.Assert(Engine != null);
			int result = 0;
			try { return Engine.CreateScriptSourceFromFile(Options.RunFile).ExecuteProgram(); }
			catch (Exception ex)
			{
				UnhandledException(Engine, ex);
				result = 1;
			}
			finally
			{
				try { Snippets.SaveAndVerifyAssemblies(); }
				catch { result = 1; }
			}
			return result;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
		int RunCommandLine()
		{
			Debug.Assert(Engine != null);
			CommandLine = CreateCommandLine();
			if (ConsoleIO == null)
				ConsoleIO = CreateConsole(Engine, CommandLine, _languageOptionsParser.CommonConsoleOptions);
			int? exitCodeOverride = null;
			try
			{
				if (_languageOptionsParser.CommonConsoleOptions.HandleExceptions)
				{
					try { CommandLine.Run(Engine, ConsoleIO, _languageOptionsParser.CommonConsoleOptions); }
					catch (Exception ex)
					{
						if (CommandLine.IsFatalException(ex))
							throw; // Some exceptions are too dangerous to try to catch
						UnhandledException(Engine, ex);
					}
				}
				else
					CommandLine.Run(Engine, ConsoleIO, _languageOptionsParser.CommonConsoleOptions);
			}
			finally
			{
				try { Snippets.SaveAndVerifyAssemblies(); }
				catch { exitCodeOverride = 1; }
			}
			if (exitCodeOverride == null)
				return CommandLine.ExitCode;
			else
				return exitCodeOverride.Value;
		}

		void PrintUsage()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat("Usage: {0}.exe ", ExeName);
			PrintLanguageHelp(sb);
			Console.Write(sb.ToString());
		}

		/// <summary>���̌���̃o�[�W�������R���\�[���ɕ\�����܂��B</summary>
		protected void PrintVersion() { Console.WriteLine("{0} {1} on {2}", Engine.Setup.DisplayName, Engine.LanguageVersion, GetRuntime()); }

		static string GetRuntime()
		{
			var mono = typeof(object).Assembly.GetType("Mono.Runtime");
			return mono != null ? (string)mono.GetMethod("GetDisplayName", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null) : string.Format(CultureInfo.InvariantCulture, ".NET {0}", Environment.Version);
		}

		/// <summary>�n���h������Ȃ���O�����������ꍇ�ɌĂ΂�܂��B</summary>
		/// <param name="engine">��O�������������ۂɓ��삵�Ă����G���W�����w�肵�܂��B</param>
		/// <param name="ex">����������O���w�肵�܂��B</param>
		protected virtual void UnhandledException(ScriptEngine engine, Exception ex)
		{
			Console.Error.Write("Unhandled exception");
			Console.Error.WriteLine(':');
			Console.Error.WriteLine(engine.GetService<ExceptionOperations>().FormatException(ex));
		}

		/// <summary>�w�肳�ꂽ <see cref="TextWriter"/> �Ɏw�肳�ꂽ��O��\��������������o���܂��B</summary>
		/// <param name="output">��O��\��������������o�� <see cref="TextWriter"/> ���w�肵�܂��B</param>
		/// <param name="ex">�����o����O���w�肵�܂��B</param>
		protected static void PrintException(TextWriter output, Exception ex)
		{
			Debug.Assert(output != null);
			ContractUtils.RequiresNotNull(ex, "e");
			while (ex != null)
			{
				output.WriteLine(ex);
				ex = ex.InnerException;
			}
		}
	}
}

