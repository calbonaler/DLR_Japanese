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
	/// <summary>無効な形式のオプションに遭遇した場合にスローされる例外を表します。</summary>
	[Serializable]
	public class InvalidOptionException : Exception
	{
		/// <summary><see cref="Microsoft.Scripting.Hosting.Shell.InvalidOptionException"/> クラスの新しいインスタンスを初期化します。</summary>
		public InvalidOptionException() { }

		/// <summary>指定されたメッセージを使用して、<see cref="Microsoft.Scripting.Hosting.Shell.InvalidOptionException"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="message">例外の詳細を表すメッセージを指定します。</param>
		public InvalidOptionException(string message) : base(message) { }

		/// <summary>指定されたメッセージと内部例外を使用して、<see cref="Microsoft.Scripting.Hosting.Shell.InvalidOptionException"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="message">例外の詳細を表すメッセージを指定します。</param>
		/// <param name="innerException">この例外の原因となった例外を指定します。</param>
		public InvalidOptionException(string message, Exception innerException) : base(message, innerException) { }

		/// <summary>シリアル化したデータを使用して、<see cref="Microsoft.Scripting.Hosting.Shell.InvalidOptionException"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="info">スローされている例外に関するシリアル化済みオブジェクト データを保持している <see cref="System.Runtime.Serialization.SerializationInfo"/>。</param>
		/// <param name="context">転送元または転送先に関するコンテキスト情報を含んでいる <see cref="System.Runtime.Serialization.StreamingContext"/>。</param>
		/// <exception cref="System.ArgumentNullException"><paramref name="info"/> パラメーターが <c>null</c> です。</exception>
		/// <exception cref="System.Runtime.Serialization.SerializationException">クラス名が <c>null</c> であるか、または <see cref="System.Exception.HResult"/> が 0 です。</exception>
		protected InvalidOptionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}

	/// <summary>起動時の引数から <see cref="ConsoleOptions"/> を解析します。このクラスは抽象クラスです。</summary>
	public abstract class OptionsParser
	{
		string[] _args;
		int _current = -1;

		/// <summary><see cref="Microsoft.Scripting.Hosting.Shell.OptionsParser"/> クラスの新しいインスタンスを初期化します。</summary>
		protected OptionsParser() { IgnoredArgs = new List<string>(); }

		/// <summary>引数の解析によって変更される可能性のある <see cref="ScriptRuntimeSetup"/> を取得します。</summary>
		public ScriptRuntimeSetup RuntimeSetup { get; private set; }

		/// <summary>引数の解析によって変更される可能性のある <see cref="LanguageSetup"/> を取得します。</summary>
		public LanguageSetup LanguageSetup { get; private set; }

		/// <summary>解析で使用される <see cref="PlatformAdaptationLayer"/> を取得します。</summary>
		public PlatformAdaptationLayer Platform { get; private set; }

		/// <summary>引数の解析によって変更される <see cref="ConsoleOptions"/> を取得します。</summary>
		public abstract ConsoleOptions CommonConsoleOptions { get; }

		/// <summary>解析がスキップされた引数のリストを取得します。</summary>
		public IList<string> IgnoredArgs { get; private set; }

		/// <summary>指定された起動時の引数を解析することで、セットアップ情報を更新します。</summary>
		/// <param name="args">解析する引数を指定します。</param>
		/// <param name="setup">解析によって変更される可能性のある <see cref="ScriptRuntimeSetup"/> を指定します。</param>
		/// <param name="languageSetup">解析によって変更される可能性のある <see cref="LanguageSetup"/> を指定します。</param>
		/// <param name="platform">解析に使用される <see cref="PlatformAdaptationLayer"/> を指定します。</param>
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

		/// <summary>引数の解析の直前に呼ばれます。</summary>
		protected virtual void BeforeParse() { }

		/// <summary>引数の解析の直後に呼ばれます。</summary>
		protected virtual void AfterParse() { }

		/// <summary>指定された単一の引数を解析します。</summary>
		/// <param name="arg">解析する引数の値を指定します。</param>
		protected abstract void ParseArgument(string arg);

		/// <summary>解析されなかった残りの引数を無視します。</summary>
		protected void IgnoreRemainingArgs()
		{
			while (_current < _args.Length)
				IgnoredArgs.Add(_args[_current++]);
		}

		/// <summary>引数の読み取り位置を最後尾に移動させて、読み取ったすべての引数を返します。</summary>
		/// <returns>読み取ったすべての引数を含む配列。</returns>
		protected string[] PopRemainingArgs()
		{
			var result = ArrayUtils.ShiftLeft(_args, _current);
			_current = _args.Length;
			return result;
		}

		/// <summary>次の引数の値を取得します。</summary>
		/// <returns>次の引数の値。</returns>
		/// <exception cref="InvalidOptionException">現在読み取っている引数は最後の引数です。</exception>
		protected string PeekNextArg()
		{
			if (_current < _args.Length)
				return _args[_current];
			else
				throw new InvalidOptionException(string.Format(CultureInfo.CurrentCulture, "オプション '{0}' には値が必要です。", _current > 0 ? _args[_current - 1] : ""));
		}

		/// <summary>次の引数を読み取り、読み取り位置を 1 つ先に進めます。</summary>
		/// <returns>次の引数の値。</returns>
		/// <exception cref="InvalidOptionException">現在読み取っている引数は最後の引数です。</exception>
		protected string PopNextArg()
		{
			var result = PeekNextArg();
			_current++;
			return result;
		}

		/// <summary>読み取り位置を 1 つ前に戻します。</summary>
		protected void PushArgBack() { _current--; }

		/// <summary>指定されたオプション名において指定された値が無効であることを示す <see cref="InvalidOptionException"/> を返します。</summary>
		/// <param name="option">オプション名を指定します。</param>
		/// <param name="value">オプションの値を指定します。</param>
		/// <returns>値が無効であることを示す <see cref="InvalidOptionException"/>。</returns>
		protected static Exception InvalidOptionValue(string option, string value) { return new InvalidOptionException(string.Format("'{0}' はオプション '{1}' に対する有効な値ではありません。", value, option)); }

		/// <summary>コマンドライン オプションのヘルプを取得します。</summary>
		/// <returns>ヘルプを格納する <see cref="OptionsHelp"/>。</returns>
		public abstract OptionsHelp GetHelp();
	}

	/// <summary><see cref="OptionsParser.GetHelp"/> メソッドによって返されるコマンドライン オプションのヘルプを表します。</summary>
	public class OptionsHelp
	{
		/// <summary><see cref="Microsoft.Scripting.Hosting.Shell.OptionsHelp"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="commandLine">コマンドラインのフォーマットを示す文字列を指定します。</param>
		/// <param name="options">オプション名および説明を格納するシーケンスを指定します。</param>
		/// <param name="environmentVariables">環境変数の名前および説明を格納するシーケンスを指定します。</param>
		/// <param name="comments">コメントを示す文字列を指定します。</param>
		public OptionsHelp(string commandLine, IEnumerable<KeyValuePair<string, string>> options, IEnumerable<KeyValuePair<string, string>> environmentVariables, string comments)
		{
			CommandLine = commandLine;
			Options = options.ToReadOnly();
			EnvironmentVariables = environmentVariables.ToReadOnly();
			Comments = comments;
		}

		/// <summary>コマンドラインのフォーマットを示す文字列を取得します。</summary>
		public string CommandLine { get; private set; }

		/// <summary>オプション名および説明を格納するコレクションを取得します。</summary>
		public ReadOnlyCollection<KeyValuePair<string, string>> Options { get; private set; }

		/// <summary>環境変数の名前および説明を格納するコレクションを取得します。</summary>
		public ReadOnlyCollection<KeyValuePair<string, string>> EnvironmentVariables { get; private set; }

		/// <summary>コメントを示す文字列を取得します。</summary>
		public string Comments { get; private set; }
	}

	/// <summary>指定された言語固有のオプションを解析する <see cref="OptionsParser"/> を表します。</summary>
	/// <typeparam name="TConsoleOptions">言語固有のオプションを指定します。</typeparam>
	public class OptionsParser<TConsoleOptions> : OptionsParser where TConsoleOptions : ConsoleOptions, new()
	{
		TConsoleOptions _consoleOptions;
		bool _saveAssemblies = false;
		string _assembliesDir = null;

		/// <summary><see cref="Microsoft.Scripting.Hosting.Shell.OptionsParser&lt;TConsoleOptions&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		public OptionsParser() { }

		/// <summary>引数の解析によって変更される言語固有のオプションを取得または設定します。</summary>
		public TConsoleOptions ConsoleOptions
		{
			get { return _consoleOptions ?? (_consoleOptions = new TConsoleOptions()); }
			set
			{
				ContractUtils.RequiresNotNull(value, "value");
				_consoleOptions = value;
			}
		}

		/// <summary>引数の解析によって変更される <see cref="ConsoleOptions"/> を取得します。</summary>
		public sealed override ConsoleOptions CommonConsoleOptions { get { return ConsoleOptions; } }

		/// <summary>指定された単一の引数を解析します。</summary>
		/// <param name="arg">解析する引数の値を指定します。</param>
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

		/// <summary>環境変数に動的言語ランタイムに関するオプションを設定します。</summary>
		/// <param name="option">設定するオプションを指定します。</param>
		internal static void SetDlrOption(string option) { SetDlrOption(option, "true"); }

		// Note: this works because it runs before the compiler picks up the environment variable
		/// <summary>環境変数に動的言語ランタイムに関するオプションを設定します。</summary>
		/// <param name="option">設定するオプションの名前を指定します。</param>
		/// <param name="value">設定するオプションの値を指定します。</param>
		internal static void SetDlrOption(string option, string value) { Environment.SetEnvironmentVariable("DLR_" + option, value); }

		/// <summary>コマンドライン オプションのヘルプを取得します。</summary>
		/// <returns>ヘルプを格納する <see cref="OptionsHelp"/>。</returns>
		public override OptionsHelp GetHelp()
		{
			var options = new [] {
				new KeyValuePair<string, string>("-c cmd",                          "プログラムが文字列として渡されます (オプションリストの最後に指定します)。"),
				new KeyValuePair<string, string>("-h",                              "使用法を表示します。"),
#if !IRONPYTHON_WINDOW															    
				new KeyValuePair<string, string>("-i",                              "スクリプト実行後に対話的に検査します。"),
#endif																			    
				new KeyValuePair<string, string>("-V",                              "バージョン番号を表示して終了します。"),
				new KeyValuePair<string, string>("-D",                              "アプリケーションのデバッグを有効にします。"),
				new KeyValuePair<string, string>("-X:AutoIndent",                   "REPL で自動インデントを有効にします。"),
				new KeyValuePair<string, string>("-X:ExceptionDetail",              "例外詳細モードを有効にします。"),
				new KeyValuePair<string, string>("-X:NoAdaptiveCompilation",        "適応的コンパイルを無効にします。"),
				new KeyValuePair<string, string>("-X:CompilationThreshold",         "インタプリタがコンパイルを開始する前の繰り返し回数です。"),
				new KeyValuePair<string, string>("-X:PassExceptions",               "スクリプトコードによってハンドルされない例外をキャッチしません。"),
				new KeyValuePair<string, string>("-X:PrivateBinding",               "プライベートなメンバへのバインディングを有効にします。"),
				new KeyValuePair<string, string>("-X:ShowClrExceptions",            "CLS 例外情報を表示します。"),
				new KeyValuePair<string, string>("-X:TabCompletion",                "タブ補完モードを有効にします。"),
				new KeyValuePair<string, string>("-X:ColorfulConsole",              "色つきのコンソールを有効にします。"),
#if DEBUG
				new KeyValuePair<string, string>("-X:AssembliesDir <ディレクトリ>", "生成されたアセンブリの保存のためのディレクトリを設定します。[デバッグのみ]"),
				new KeyValuePair<string, string>("-X:SaveAssemblies",               "生成されたアセンブリを保存します。[デバッグのみ]"),
				new KeyValuePair<string, string>("-X:TrackPerformance",             "パフォーマンスに敏感な領域を追跡します。[デバッグのみ]"),
				new KeyValuePair<string, string>("-X:PerfStats",                    "プロセス終了時にパフォーマンス統計を表示します。[デバッグのみ]"),
				new KeyValuePair<string, string>(Remote.RemoteRuntimeServer.RemoteRuntimeArg + " <チャンネル名>", "遠隔コンソールセッションに対する遠隔サーバを開始します。"),
#endif
			};
			return new OptionsHelp("[オプション] [ファイル|- [引数]]", options, null, null);
		}
	}
}
