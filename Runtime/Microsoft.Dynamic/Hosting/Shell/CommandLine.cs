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
	/// <summary>コマンドライン ホスティング サービスを表します。</summary>
	public class CommandLine
	{
		/// <summary><see cref="Microsoft.Scripting.Hosting.Shell.CommandLine"/> クラスの新しいインスタンスを初期化します。</summary>
		public CommandLine() { ExitCode = 1; }

		LanguageContext _language;
		ICommandDispatcher _commandDispatcher;
		int? _terminatingExitCode;

		/// <summary>このサービスの入出力を行っているコンソールを取得します。</summary>
		protected IConsole Console { get; private set; }

		/// <summary>このサービスのコンソールに対するオプションを取得します。</summary>
		protected ConsoleOptions Options { get; private set; }

		/// <summary>このサービスを現在実行中の言語を表す <see cref="ScriptEngine"/> を取得します。</summary>
		protected ScriptEngine Engine { get; private set; }

		/// <summary>
		/// このサービスによってコマンドを実行するスコープを取得 (または設定) します。
		/// このプロパティの設定は派生クラスのみ可能です。
		/// </summary>
		public ScriptScope ScriptScope { get; protected set; }

		/// <summary>このコマンドの終了コードを取得 (または設定) します。
		/// このプロパティの設定は派生クラスのみ可能です。</summary>
		public int ExitCode { get; protected set; }

		/// <summary><see cref="ScriptScope"/> に対する同じアプリケーションドメイン内でのみ動作するリモート可能ではない <see cref="Microsoft.Scripting.Runtime.Scope"/> オブジェクトを取得します。</summary>
		protected Scope Scope
		{
			get { return ScriptScope == null ? null : HostingHelpers.GetScope(ScriptScope); }
			set { ScriptScope = HostingHelpers.CreateScriptScope(Engine, value); }
		}

		/// <summary><see cref="Engine"/> に対する同じアプリケーションドメイン内でのみ動作するリモート可能ではない <see cref="LanguageContext"/> オブジェクトを取得します。</summary>
		protected LanguageContext Language { get { return _language ?? (_language = HostingHelpers.GetLanguageContext(Engine)); } }

		/// <summary>このコマンドラインによるプロンプトを取得します。</summary>
		protected virtual string Prompt { get { return ">>> "; } }

		/// <summary>このコマンドラインによる複数行入力の 2 行目以降のプロンプトを取得します。</summary>
		public virtual string PromptContinuation { get { return "... "; } }

		/// <summary>通常対話ループの開始時に表示されるロゴを取得します。</summary>
		protected virtual string Logo { get { return null; } }

		/// <summary>このサービスの初期化処理を行います。</summary>
		protected virtual void Initialize()
		{
			if (_commandDispatcher == null)
				_commandDispatcher = CreateCommandDispatcher();
		}

		/// <summary>このコマンドライン ホスティング サービスが使用する新しい <see cref="Scope"/> を作成します。</summary>
		/// <returns>作成された新しい <see cref="Scope"/>。</returns>
		protected virtual Scope CreateScope() { return new Scope(); }

		/// <summary>このコマンドライン ホスティング サービスが使用する新しい <see cref="ICommandDispatcher"/> を作成します。</summary>
		/// <returns>作成された新しい <see cref="ICommandDispatcher"/>。</returns>
		protected virtual ICommandDispatcher CreateCommandDispatcher() { return new SimpleCommandDispatcher(); }

		// 既定の実装はフラグを設定するだけです。派生クラスではよりよい終了をサポートできます。
		/// <summary>終了コードを指定して、このサービスを終了します。</summary>
		/// <param name="exitCode">終了に関する終了コードを指定します。</param>
		public virtual void Terminate(int exitCode) { _terminatingExitCode = exitCode; }

		/// <summary>コマンドラインを実行します。</summary>
		/// <param name="engine">サービスを実行する言語を表す <see cref="ScriptEngine"/> を指定します。</param>
		/// <param name="console">サービスの入出力を行うコンソールを指定します。</param>
		/// <param name="options">サービスのコンソールに対するオプションを指定します。</param>
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

		/// <summary>コマンドラインを実行します。言語はこのメソッドをオーバーライドすることで、単一のコマンドあるいはファイルの実行、対話ループの開始以外の動作を提供できます。</summary>
		/// <returns>終了コード。</returns>
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

		/// <summary>このコマンドラインの終了時に呼ばれます。既定の実装では基になる動的言語ランタイムをシャットダウンします。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		protected virtual void Shutdown()
		{
			try { Engine.Runtime.Shutdown(); }
			catch (Exception ex) { UnhandledException(ex); }
		}

		/// <summary>指定されたファイル名のファイルを実行して、終了コードを返します。</summary>
		/// <param name="fileName">ファイル名を指定します。</param>
		/// <returns>終了コード。</returns>
		protected virtual int RunFile(string fileName) { return RunFile(Engine.CreateScriptSourceFromFile(fileName)); }

		/// <summary>指定されたコマンドを実行して、終了コードを返します。</summary>
		/// <param name="command">コマンドを表す文字列を指定します。</param>
		/// <returns>終了コード。</returns>
		protected virtual int RunCommand(string command) { return RunFile(Engine.CreateScriptSourceFromString(command, SourceCodeKind.Statements)); }

		/// <summary>指定された <see cref="ScriptSource"/> を実行して、終了コードを返します。</summary>
		/// <param name="source">実行するソースコードを表す <see cref="ScriptSource"/> を指定します。</param>
		/// <returns>終了コード。</returns>
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

		/// <summary>ロゴをコンソールに表示します。</summary>
		protected void PrintLogo()
		{
			if (Logo != null)
				Console.Write(Logo, Style.Out);
		}

		/// <summary>対話ループを開始します。ループの開始前にあらゆる必要な初期化を実行し、ループを開始します。対話ループが完了したら、終了コードを返します。</summary>
		/// <returns>終了コード。</returns>
		protected virtual int RunInteractive()
		{
			PrintLogo();
			return RunInteractiveLoop();
		}

		/// <summary>
		/// 対話ループを実行します。
		/// 終了コードが到着するまで繰り返し対話動作を解析、実行します。
		/// ハンドルされないあらゆる例外はコンソールを通してユーザーに表示されます。
		/// </summary>
		/// <returns>終了コード。</returns>
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

		/// <summary>指定された例外が致命的であるかどうかを判断します。</summary>
		/// <param name="ex">調べる例外を指定します。</param>
		/// <returns>例外が致命的であった場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		internal static bool IsFatalException(Exception ex)
		{
			ThreadAbortException tae = ex as ThreadAbortException;
			return tae != null && !(tae.ExceptionState is KeyboardInterruptException);
		}

		/// <summary>ハンドルされない例外発生した場合に呼ばれます。</summary>
		/// <param name="ex">ハンドルされなかった例外。</param>
		protected virtual void UnhandledException(Exception ex) { Console.WriteLine(Engine.GetService<ExceptionOperations>().FormatException(ex), Style.Error); }

		/// <summary>
		/// 単一の対話動作の実行およびあらゆる言語固有の例外のハンドルを試みます。
		/// 派生クラスはこのメソッドをオーバーライドして、個別の例外ハンドリングを追加できます。
		/// </summary>
		/// <returns>成功して実行を継続できる場合は <c>null</c>。それ以外の場合は終了コードを指定します。</returns>
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
		/// 単一の対話コマンドまたはステートメントセットを解析および実行します。
		/// 読み取られたコードが対話コマンドかステートメントであるかは改行によって判断します。
		/// コードが改行を含んでいる場合、それは (おそらく SendToConsole から来た) ステートメントセットです。
		/// コードが改行を含まない場合、それはプロンプトでユーザーが入力した対話コマンドです。
		/// </summary>
		/// <returns>成功して実行を継続できる場合は <c>null</c>。それ以外の場合は適切な終了コード。</returns>
		int? RunOneInteraction()
		{
			bool continueInteraction;
			var s = ReadStatement(out continueInteraction);
			if (continueInteraction == false)
				return _terminatingExitCode == null ? 0 : _terminatingExitCode;
			if (string.IsNullOrEmpty(s))
			{
				// 空行?
				Console.Write(string.Empty, Style.Out);
				return null;
			}
			ExecuteCommand(s);
			return null;
		}

		/// <summary>対話ループで指定されたコマンドを実行します。</summary>
		/// <param name="command">実行するコマンドを指定します。</param>
		protected virtual void ExecuteCommand(string command) { ExecuteCommand(Engine.CreateScriptSourceFromString(command, SourceCodeKind.InteractiveCode)); }

		/// <summary>対話ループで指定された <see cref="ScriptSource"/> を実行します。</summary>
		/// <param name="source">実行する <see cref="ScriptSource"/></param>
		/// <returns>実行の結果。</returns>
		protected object ExecuteCommand(ScriptSource source) { return _commandDispatcher.Execute(source.Compile(Engine.GetCompilerOptions(ScriptScope), new ErrorSinkProxyListener(ErrorSink)), ScriptScope); }

		/// <summary>エラーを処理する <see cref="Microsoft.Scripting.ErrorSink"/> を取得します。</summary>
		protected virtual ErrorSink ErrorSink { get { return ErrorSink.Default; } }

		/// <summary>指定された入力を空行として扱うかどうかを判断します。自動インデントされたテキストについてのみこれを実行します。</summary>
		static bool TreatAsBlankLine(string line, int autoIndentSize) { return line.Length == 0 || autoIndentSize != 0 && line.Trim().Length == 0 && line.Length == autoIndentSize; }

		/// <summary>ステートメントを読み取ります。ステートメントは (クラス宣言のような) 潜在的に複数行のステートメントセットになる可能性があるものです。</summary>
		/// <param name="continueInteraction">コンソールセッションを継続させるかどうかを示す値。</param>
		/// <returns>評価された式。空の入力については <c>null</c>。</returns>
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

		/// <summary>指定されたソースコードについての解析結果を取得します。</summary>
		/// <param name="code">解析するコードを指定します。</param>
		/// <returns>解析結果を表す <see cref="ScriptCodeParseResult"/>。</returns>
		protected virtual ScriptCodeParseResult GetCommandProperties(string code) { return Engine.CreateScriptSourceFromString(code, SourceCodeKind.InteractiveCode).FetchCodeProperties(Engine.GetCompilerOptions(ScriptScope)); }

		/// <summary>次行の自動インデントサイズを取得します。</summary>
		/// <param name="text">現在のコードを指定します。</param>
		/// <returns>次行の自動インデントサイズ。</returns>
		protected virtual int GetNextAutoIndentSize(string text) { return 0; }

		/// <summary>指定された自動インデントサイズを使用して行を読み取ります。</summary>
		/// <param name="autoIndentSize">行に適用される自動インデントサイズを指定します。</param>
		/// <returns>読み取られた行。</returns>
		protected virtual string ReadLine(int autoIndentSize) { return Console.ReadLine(autoIndentSize); }

		//private static DynamicSite<object, IList<string>>  _memberCompletionSite = new DynamicSite<object, IList<string>>(OldDoOperationAction.Make(Operators.GetMemberNames));

		/// <summary>指定されたコードに関するメンバ名の一覧を取得します。</summary>
		/// <param name="code">メンバを取得する対象のコードを指定します。</param>
		/// <returns>取得されたメンバの名前。</returns>
		public IList<string> GetMemberNames(string code)
		{
			return Engine.Operations.GetMemberNames((object)Engine.CreateScriptSourceFromString(code, SourceCodeKind.Expression).Execute(ScriptScope));
			// TODO: why doesn't this work ???
			//return _memberCompletionSite.Invoke(new CodeContext(_scope, _engine), value);
		}

		/// <summary>指定された名前のグローバル変数名の一覧を取得します。</summary>
		/// <param name="name">検索する名前を指定します。</param>
		/// <returns>指定された名前のグローバル変数名。</returns>
		public virtual IList<string> GetGlobals(string name) { return ScriptScope.GetVariableNames().Where(x => x.StartsWith(name)).ToArray(); }

		class SimpleCommandDispatcher : ICommandDispatcher
		{
			public object Execute(CompiledCode compiledCode, ScriptScope scope) { return compiledCode.Execute(scope); }
		}
	}
}
