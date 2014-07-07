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
	/// <summary>リモートランタイムが強制終了されたことの検出および新しいリモートランタイムの起動をサポートします。</summary>
	/// <remarks>
	/// スレッディングモデル:
	/// 
	/// <see cref="ConsoleRestartManager"/> はコンソールを作成し実行する分離スレッドを作成します。
	/// これには少なくとも 3 つのスレッドが関係しています:
	/// 
	/// 1. メイン アプリケーション スレッド: <see cref="ConsoleRestartManager"/> をインスタンス化し、その API にアクセスします。
	///    このスレッドはユーザー入力に応答可能である必要があり、<see cref="ConsoleRestartManager"/> の API を長期間実行したり、ブロックしたりすることはできません。
	///    リモートランタイムプロセスは非同期的に終了できるので、現在の <see cref="RemoteConsoleHost"/> は (自動再起動が有効であれば) いつでも変更できます。
	///    アプリケーションは通常どの <see cref="RemoteConsoleHost"/> のインスタンスが現在使用されているかを気にする必要はありません。
	///    このスレッドのフローチャートは次のようになります:
	///        <see cref="ConsoleRestartManager"/> を作成
	///        <see cref="ConsoleRestartManager.Start"/>
	///        ループ:
	///            ユーザー入力に応答 | ユーザー入力を実行中のコンソールに送信 | <see cref="BreakExecution"/> | <see cref="RestartConsole"/> | <see cref="GetMemberNames"/>
	///        <see cref="ConsoleRestartManager.Terminate"/>
	///    TODO: 現在、<see cref="BreakExecution"/> と <see cref="GetMemberNames"/> はメインスレッドから同期的に呼び出されます。
	///    それらはリモートランタイムにあるコードを実行しますが、任意の時間がかかる可能性があります。
	///    メインアプリケーションスレッドが無期限にブロックされることがないようにこの動作を変更する必要があります。
	///
	/// 2. コンソール スレッド: <see cref="RemoteConsoleHost"/> を作成したり、(実行に時間を要するか、無期限にブロックする可能性のある) コードを実行したりするための専用のスレッドです。
	///        <see cref="ConsoleRestartManager.Start"/> の実行を待機
	///        ループ:
	///            <see cref="RemoteConsoleHost"/> の作成
	///            次のシグナルの待機:
	///                 コードの実行 | <see cref="RestartConsole"/> | <see cref="Process.Exited"/>
	///
	/// 3. 完了ポートの非同期コールバック
	///        <see cref="Process.Exited"/> | <see cref="Process.OutputDataReceived"/> | <see cref="Process.ErrorDataReceived"/>
	/// 
	/// 4. ファイナライザ スレッド
	///    (Dispose を呼び出す可能性のある) Finalize メソッドがあるオブジェクトがあります。
	///    それほど多くはない型では Finalize メソッドをもつ必要があるものもあります。
	/// </remarks>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1012:AbstractTypesShouldNotHaveConstructors")] // TODO: This is public only because the test (RemoteConsole.py) needs it to be so. The test should be rewritten
	public abstract class ConsoleRestartManager
	{
		bool _exitOnNormalExit;
		bool _terminating;

		/// <summary>REPL ループの生存モードを使用して、<see cref="Microsoft.Scripting.Hosting.Shell.Remote.ConsoleRestartManager"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="exitOnNormalExit"><see cref="RemoteConsoleHost"/> のインスタンスが正常に終了した場合は REPL ループも終了するかどうかを示す値を指定します。</param>
		public ConsoleRestartManager(bool exitOnNormalExit)
		{
			AccessLock = new object();
			_exitOnNormalExit = exitOnNormalExit;
			ConsoleThread = new Thread(Run);
			ConsoleThread.Name = "Console thread";
		}

		/// <summary><see cref="CurrentConsoleHost"/> にコンソール スレッド以外からアクセスされた場合に <c>null</c> か破棄されていない状態であることを保証するロックを取得します。</summary>
		protected object AccessLock { get; private set; }

		/// <summary><see cref="RemoteConsoleHost"/> を作成したり、実行に時間のかかる可能性のあるコードを実行したりする専用のスレッドを取得します。</summary>
		public Thread ConsoleThread { get; private set; }

		/// <summary>現在の <see cref="RemoteConsoleHost"/> を取得します。</summary>
		protected RemoteConsoleHost CurrentConsoleHost { get; private set; }

		/// <summary>新しい <see cref="RemoteConsoleHost"/> を作成します。このメソッドはコンソール スレッドで実行されます。</summary>
		/// <returns>新しく作成された <see cref="RemoteConsoleHost"/>。</returns>
		public abstract RemoteConsoleHost CreateRemoteConsoleHost();

		/// <summary>コンソール REPL ループを開始します。このメソッドを呼び出すにはアクティベーションが完了している必要があります。</summary>
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

		// TODO: リモートランタイムでユーザーコードを実行していて、どんな例外がスローされるかを制御できないため、すべての例外を捕捉する必要があります。
		// これはリモートランタイムから例外を逆伝播する代わりに、エラーコードを返すリモートチャンネルを設けることで修正します。
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

		/// <summary>コンソール REPL ループの実行を中断します。</summary>
		public void BreakExecution()
		{
			Debug.Assert(Thread.CurrentThread != ConsoleThread);
			lock (AccessLock)
			{
				if (CurrentConsoleHost == null)
					return;
				try { CurrentConsoleHost.AbortCommand(); }
				catch (System.Runtime.Remoting.RemotingException) { } // リモートランタイムは終了されたか、応答がありません。
			}
		}

		/// <summary>コンソール REPL ループの終了を要求して、REPL ループを再起動します。</summary>
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

		/// <summary>コンソール REPL ループの終了を要求します。</summary>
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