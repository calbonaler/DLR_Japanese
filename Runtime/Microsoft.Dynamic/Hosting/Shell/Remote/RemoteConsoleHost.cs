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
	/// <summary><see cref="ScriptRuntime"/> が (リモートランタイムサーバーと呼ばれる) 分離プロセスでホストされている <see cref="ConsoleHost"/> を表します。</summary>
	/// <remarks>
	/// このクラスはリモートランタイムサーバーを生成し、相互通信に使用する IPC チャンネル名を決定します。
	/// リモートランタイムサーバーは <see cref="ScriptRuntime"/> および <see cref="ScriptEngine"/> を作成、初期化して、ウェルノウン URI の指定された IPC チャンネル上で公開します。
	/// 注釈: <see cref="RemoteConsoleHost"/> は <see cref="Microsoft.Scripting.Runtime.LanguageContext"/> のようなクラスがリモート可能でないため、<see cref="ScriptEngine"/> の初期化に簡単に参加できません。
	/// <see cref="RemoteConsoleHost"/> は次にリモーティングチャンネル上の <see cref="ScriptEngine"/> で対話ループを開始し、コマンドを実行します。
	/// また、リモートランタイムサーバーの標準出力を監視することで、ローカルでのユーザーへの表示も行います。
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
				// リダイレクトを有効にするために UseShellExecute を false に設定
				UseShellExecute = false,
				// 標準ストリームをリダイレクトする。出力ストリームはイベントハンドラを使用して非同期に読み取る
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				// 入力ストリームはリモートサーバープロセスがあらゆる入力を読み取る必要がないように無視できる。
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
			// 出力ストリームの非同期での読み取りを開始する
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			// Exited が発生するようにする
			process.EnableRaisingEvents = true;
			// 起動の出力がいつ完了したかを知るために出力マーカーを待機
			_remoteOutputReceived.WaitOne();
			if (process.HasExited)
				throw new RemoteRuntimeStartupException("Remote runtime terminated during startup with exitcode " + process.ExitCode);
		}

		T GetRemoteObject<T>(string uri)
		{
			T result = (T)Activator.GetObject(typeof(T), "ipc://" + _channelName + "/" + uri);
			// リモートオブジェクトが (リモートに実行される) 仮想メソッドの呼び出しに応答可能であることを保証する。
			Debug.Assert(result.ToString() != null);
			return result;
		}

		void InitializeRemoteScriptEngine()
		{
			StartRemoteRuntimeProcess();
			Engine = (_scriptScope = (_remoteCommandDispatcher = GetRemoteObject<RemoteCommandDispatcher>(RemoteRuntimeServer.CommandDispatcherUri)).ScriptScope).Engine;
			// リモートランタイムプロセスがイベントを発行したり、例外をスローしたい場合の、逆方向に対するチャンネルを登録する。
			var clientChannelName = _channelName.Replace("RemoteRuntime", "RemoteConsole");
			ChannelServices.RegisterChannel(RemoteRuntimeServer.CreateChannel(clientChannelName, clientChannelName), false);
		}

		/// <summary>リモートランタイムサーバーが終了した際に呼ばれます。</summary>
		/// <param name="sender">終了したリモートランタイムサーバーを表す <see cref="Process"/> オブジェクト。</param>
		/// <param name="e">リモートランタイムサーバーの終了に関連付けられているイベントオブジェクト。</param>
		protected virtual void OnRemoteRuntimeExited(object sender, EventArgs e)
		{
			Debug.Assert(((Process)sender).HasExited);
			Debug.Assert(sender == RemoteRuntimeProcess || RemoteRuntimeProcess == null);
			var remoteRuntimeExited = RemoteRuntimeExited;
			if (remoteRuntimeExited != null)
				remoteRuntimeExited(sender, e);
			// StartRemoteRuntimeProcess はこのイベントもブロックする。リモートランタイムがその起動中に終了した場合に、シグナル状態にする。
			_remoteOutputReceived.Set();
			// ConsoleHost が REPL ループを終了できるように調整する
			Terminate(RemoteRuntimeProcess.ExitCode);
		}

		/// <summary>リモートランタイムサーバーから出力データが到着した際に呼ばれます。</summary>
		/// <param name="sender">データの送信元のリモートランタイムサーバーを表す <see cref="Process"/> オブジェクト。</param>
		/// <param name="e">リモートランタイムサーバーから到着したデータを格納しているイベントオブジェクト。</param>
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

		/// <summary>リモートコンソールの REPL ループに終了を要求します。</summary>
		/// <param name="exitCode">コンソールの終了コードを指定します。</param>
		public override void Terminate(int exitCode)
		{
			if (CommandLine == null)
				// CommandLine が初期化されなかった場合は、起動中に呼ばれる可能性があります。
				// これはリモートランタイムプロセスの起動前に CommandLine を初期化することによって修正できる可能性があります
				return;
			base.Terminate(exitCode);
		}

		/// <summary>新しい <see cref="CommandLine"/> オブジェクトを作成します。</summary>
		/// <returns>新しく作成された <see cref="CommandLine"/> オブジェクト。</returns>
		protected override CommandLine CreateCommandLine() { return new RemoteConsoleCommandLine(_scriptScope, _remoteCommandDispatcher, _remoteOutputReceived); }

		/// <summary>このコンソール ホストに関連付けられたスコープを表す <see cref="ScriptScope"/> を取得します。</summary>
		public ScriptScope ScriptScope { get { return CommandLine.ScriptScope; } }

		/// <summary>このリモートコンソールのリモートランタイムサーバーを表す <see cref="Process"/> を取得します。</summary>
		public Process RemoteRuntimeProcess { get; private set; }

		/// <summary>コマンドの実行中にハンドルされない例外が発生した際に呼ばれます。</summary>
		/// <param name="engine">コマンドを実行しているエンジン。</param>
		/// <param name="ex">ハンドルされなかった例外。</param>
		protected override void UnhandledException(ScriptEngine engine, Exception ex) { ((RemoteConsoleCommandLine)CommandLine).UnhandledExceptionWorker(ex); }

		/// <summary>リモートランタイムサーバーがそれ自体によって終了した場合に発生します。</summary>
		internal event EventHandler RemoteRuntimeExited;

		/// <summary>
		/// コンソールが環境変数や作業ディレクトリなどを変更できる機会を与えます。
		/// このメソッドで少なくとも <see cref="ProcessStartInfo.FileName"/> は初期化される必要があります。
		/// </summary>
		/// <param name="processInfo">リモートランタイムサーバープロセスの起動情報を表す <see cref="ProcessStartInfo"/>。</param>
		public abstract void CustomizeRemoteRuntimeStartInfo(ProcessStartInfo processInfo);

		/// <summary>現在に実行されているコマンドを中止します。</summary>
		/// <returns>実際にコマンドが中止された場合は <c>true</c>。コマンドが実行されていなかったか、すでに完了した場合は場合は <c>false</c>。</returns>
		public bool AbortCommand() { return _remoteCommandDispatcher.AbortCommand(); }

		/// <summary>指定された引数を使用して、リモートコンソールの実行を開始します。</summary>
		/// <param name="args">プログラムの引数を指定します。</param>
		/// <returns>リモートコンソールの終了コード。</returns>
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
			// 起動時の出力を表示できるようにするために、(既定の設定で) 早めに IConsole を作成します。
			ConsoleIO = CreateConsole(null, null, new ConsoleOptions());
			InitializeRemoteScriptEngine();
			Runtime = Engine.Runtime;
			ExecuteInternal();
			return ExitCode;
		}

		/// <summary>このリモートコンソールホストを破棄します。</summary>
		/// <param name="disposing">すべてのリソースを破棄する場合は <c>true</c>。アンマネージリソースのみを破棄する場合は <c>false</c>。</param>
		public virtual void Dispose(bool disposing)
		{
			if (!disposing)
				// ファイナライズ中はマネージフィールドはすでにファイナライズされている可能性があるため、信頼してアクセスすることができません。
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
				// 標準入力のクローズはリモートランタイムに対してプロセスを終了するシグナルです。
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

		/// <summary>このリモートコンソールホストを破棄します。</summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}

	/// <summary>リモートランタイムが起動中に予期せず終了した場合にスローされる例外を表します。</summary>
	[Serializable]
	public class RemoteRuntimeStartupException : Exception
	{
		/// <summary><see cref="Microsoft.Scripting.Hosting.Shell.Remote.RemoteRuntimeStartupException"/> クラスの新しいインスタンスを初期化します。</summary>
		public RemoteRuntimeStartupException() { }

		/// <summary>指定されたメッセージを使用して、<see cref="Microsoft.Scripting.Hosting.Shell.Remote.RemoteRuntimeStartupException"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="message">例外の詳細を表すメッセージを指定します。</param>
		public RemoteRuntimeStartupException(string message) : base(message) { }

		/// <summary>指定されたメッセージと内部例外を使用して、<see cref="Microsoft.Scripting.Hosting.Shell.Remote.RemoteRuntimeStartupException"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="message">例外の詳細を表すメッセージを指定します。</param>
		/// <param name="innerException">この例外の原因となった例外を指定します。</param>
		public RemoteRuntimeStartupException(string message, Exception innerException) : base(message, innerException) { }

		/// <summary>シリアル化したデータを使用して、<see cref="Microsoft.Scripting.Hosting.Shell.Remote.RemoteRuntimeStartupException"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="info">スローされている例外に関するシリアル化済みオブジェクト データを保持している <see cref="System.Runtime.Serialization.SerializationInfo"/>。</param>
		/// <param name="context">転送元または転送先に関するコンテキスト情報を含んでいる <see cref="System.Runtime.Serialization.StreamingContext"/>。</param>
		/// <exception cref="System.ArgumentNullException"><paramref name="info"/> パラメーターが <c>null</c> です。</exception>
		/// <exception cref="System.Runtime.Serialization.SerializationException">クラス名が <c>null</c> であるか、または <see cref="System.Exception.HResult"/> が 0 です。</exception>
		protected RemoteRuntimeStartupException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}