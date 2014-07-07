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
	/// <summary>対話コンソールを実装するための基本的な機能を提供します。このクラスは具体的な実装のために継承される必要があります。</summary>
	public abstract class ConsoleHost
	{
		internal OptionsParser _languageOptionsParser;

		/// <summary>開始時に与えられたコンソール ホストに対するオプションを取得します。</summary>
		public ConsoleHostOptions Options { get { return ConsoleHostOptionsParser.Options; } }

		/// <summary>開始時に作成された <see cref="ScriptRuntime"/> のセットアップ情報を取得します。</summary>
		public ScriptRuntimeSetup RuntimeSetup { get { return ConsoleHostOptionsParser.RuntimeSetup; } }

		/// <summary>コンソールのコマンドを実行する言語を表す <see cref="ScriptEngine"/> を取得 (または設定) します。設定は派生クラスのみ可能です。</summary>
		public ScriptEngine Engine { get; protected set; }

		/// <summary>セットアップされたランタイムを表す <see cref="ScriptRuntime"/> を取得 (または設定) します。設定は派生クラスのみ可能です。</summary>
		public ScriptRuntime Runtime { get; protected set; }

		/// <summary>コンソールの終了コードを取得または設定します。</summary>
		protected int ExitCode { get; set; }

		/// <summary>開始時に引数を解析する <see cref="Microsoft.Scripting.Hosting.Shell.ConsoleHostOptionsParser"/> を取得または設定します。</summary>
		protected ConsoleHostOptionsParser ConsoleHostOptionsParser { get; set; }

		/// <summary>コマンドラインなどで使用される入出力コンソールを取得または設定します。</summary>
		protected IConsole ConsoleIO { get; set; }

		/// <summary>このホストが現在保持しているコマンドラインを取得します。</summary>
		protected CommandLine CommandLine { get; private set; }

		/// <summary><see cref="Microsoft.Scripting.Hosting.Shell.ConsoleHost"/> クラスの新しいインスタンスを初期化します。</summary>
		protected ConsoleHost() { }

		/// <summary>ヘルプなどで表示されるコンソールホストのエントリーポイントを含んでいる実行ファイルの名前を取得します。</summary>
		protected virtual string ExeName
		{
			get
			{
				var entryAssembly = Assembly.GetEntryAssembly();
				//Can be null if called from unmanaged code (VS integration scenario)
				return entryAssembly != null ? entryAssembly.GetName().Name : "ConsoleHost";
			}
		}

		/// <summary>指定された引数を <see cref="ConsoleHostOptionsParser"/> を使用して解析します。</summary>
		/// <param name="args">解析する引数を指定します。</param>
		protected virtual void ParseHostOptions(string[] args) { ConsoleHostOptionsParser.Parse(args); }

		/// <summary>使用する <see cref="ScriptRuntime"/> のセットアップに必要な <see cref="ScriptRuntimeSetup"/> を作成します。</summary>
		/// <returns>作成および初期化された <see cref="ScriptRuntimeSetup"/>。</returns>
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

		/// <summary>言語を表す <see cref="LanguageSetup"/> を作成および初期化します。</summary>
		/// <returns>作成および初期化された <see cref="LanguageSetup"/>。</returns>
		protected virtual LanguageSetup CreateLanguageSetup() { return null; }

		/// <summary>このホストによって使用される <see cref="Microsoft.Scripting.PlatformAdaptationLayer"/> を取得します。</summary>
		protected virtual PlatformAdaptationLayer PlatformAdaptationLayer { get { return PlatformAdaptationLayer.Default; } }

		/// <summary>このホストによって使用される言語プロバイダの型を取得します。</summary>
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

		/// <summary>このホストでコマンドラインをサポートする <see cref="CommandLine"/> を作成します。</summary>
		/// <returns>作成された <see cref="CommandLine"/>。</returns>
		protected virtual CommandLine CreateCommandLine() { return new CommandLine(); }

		/// <summary>引数を解析してコンソールのオプションを設定する <see cref="OptionsParser"/> を作成します。</summary>
		/// <returns>作成された <see cref="OptionsParser"/>。</returns>
		protected virtual OptionsParser CreateOptionsParser() { return new OptionsParser<ConsoleOptions>(); }

		/// <summary>言語、コマンドライン、オプションを使用して、<see cref="IConsole"/> インターフェイスを実装するオブジェクトを作成します。</summary>
		/// <param name="engine">コンソールで使用する言語を表す <see cref="ScriptEngine"/> を指定します。</param>
		/// <param name="commandLine">コンソールに関連付けるコマンドラインを指定します。</param>
		/// <param name="options">コンソールのオプションを表す <see cref="ConsoleOptions"/> を指定します。</param>
		/// <returns>作成された <see cref="IConsole"/> インターフェイスを実行するオブジェクト。</returns>
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

		/// <summary>(他のスレッドから) コンソールの REPL ループを終了するように要求します。</summary>
		/// <param name="exitCode">終了をトリガーするイベントに対応する終了コードを指定します。これは <see cref="M:CommandLine.Run(ScriptEngine, IConsole, ConsoleOptions)"/> から返されます。</param>
		public virtual void Terminate(int exitCode) { CommandLine.Terminate(exitCode); }

		/// <summary>プログラムのエントリポイントから呼ばれ、指定された引数でのコンソールの実行を開始します。</summary>
		/// <param name="args">開始時にプログラムに与えられた引数を指定します。</param>
		/// <returns>終了コード。</returns>
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

		/// <summary>ヘルプをコンソールに表示します。</summary>
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

		/// <summary>ヘルプを人間が読める形式で取得します。</summary>
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

		/// <summary>言語に関するヘルプを指定された <see cref="StringBuilder"/> に追加します。</summary>
		/// <param name="output">ヘルプを表す文字列を追加する <see cref="StringBuilder"/> を指定します。</param>
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

		/// <summary>解析されたオプションに基づいて、使用法やバージョンを表示したり、コマンドラインやファイルを実行したりします。</summary>
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

		/// <summary>この言語のバージョンをコンソールに表示します。</summary>
		protected void PrintVersion() { Console.WriteLine("{0} {1} on {2}", Engine.Setup.DisplayName, Engine.LanguageVersion, GetRuntime()); }

		static string GetRuntime()
		{
			var mono = typeof(object).Assembly.GetType("Mono.Runtime");
			return mono != null ? (string)mono.GetMethod("GetDisplayName", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null) : string.Format(CultureInfo.InvariantCulture, ".NET {0}", Environment.Version);
		}

		/// <summary>ハンドルされない例外が発生した場合に呼ばれます。</summary>
		/// <param name="engine">例外が発生したい際に動作していたエンジンを指定します。</param>
		/// <param name="ex">発生した例外を指定します。</param>
		protected virtual void UnhandledException(ScriptEngine engine, Exception ex)
		{
			Console.Error.Write("Unhandled exception");
			Console.Error.WriteLine(':');
			Console.Error.WriteLine(engine.GetService<ExceptionOperations>().FormatException(ex));
		}

		/// <summary>指定された <see cref="TextWriter"/> に指定された例外を表す文字列を書き出します。</summary>
		/// <param name="output">例外を表す文字列を書き出す <see cref="TextWriter"/> を指定します。</param>
		/// <param name="ex">書き出す例外を指定します。</param>
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

