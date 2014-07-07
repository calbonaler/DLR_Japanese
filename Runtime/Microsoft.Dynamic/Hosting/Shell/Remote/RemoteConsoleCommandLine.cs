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
using System.Runtime.Remoting;
using System.Threading;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting.Shell.Remote
{
	/// <summary>リモートシナリオにカスタマイズされたコマンドライン ホスティング サービスを表します。</summary>
	public class RemoteConsoleCommandLine : CommandLine
	{
		RemoteConsoleCommandDispatcher _remoteConsoleCommandDispatcher;

		/// <summary>指定されたスコープ、ディスパッチャ、イベントを使用して、<see cref="Microsoft.Scripting.Hosting.Shell.Remote.RemoteConsoleCommandLine"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="scope">コマンドラインに関連付けられたスコープを指定します。</param>
		/// <param name="remoteCommandDispatcher">コマンドを実際にディスパッチする <see cref="RemoteCommandDispatcher"/> を指定します。</param>
		/// <param name="remoteOutputReceived">出力を受信した際にシグナル状態になる <see cref="AutoResetEvent"/> を指定します。</param>
		public RemoteConsoleCommandLine(ScriptScope scope, RemoteCommandDispatcher remoteCommandDispatcher, AutoResetEvent remoteOutputReceived)
		{
			ContractUtils.RequiresNotNull(scope, "scope");
			_remoteConsoleCommandDispatcher = new RemoteConsoleCommandDispatcher(remoteCommandDispatcher, remoteOutputReceived);
			ScriptScope = scope;
		}

		/// <summary>単一のコマンドをディスパッチする新しい <see cref="ICommandDispatcher"/> を作成します。</summary>
		/// <returns>新しく作成された <see cref="ICommandDispatcher"/>。</returns>
		protected override ICommandDispatcher CreateCommandDispatcher() { return _remoteConsoleCommandDispatcher; }

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal void UnhandledExceptionWorker(Exception ex)
		{
			try { base.UnhandledException(ex); }
			catch (Exception exceptionDuringHandling)
			{
				// リモートランタイムのアクセス中に白紙に戻るので、すべての例外を捕捉します。
				// しかし、多くの場合、ここでは RemotingException が得られることを予測しています。
				if (!(exceptionDuringHandling is RemotingException))
					Console.WriteLine(string.Format("({0} thrown while trying to display unhandled exception)", exceptionDuringHandling.GetType()), Style.Error);
				// リモートサーバーがシャットダウンしている可能性があります。そのため、単純に行います。
				Console.WriteLine(ex.ToString(), Style.Error);
			}
		}

		/// <summary>対話ループ中でハンドルされない例外が発生した際に実行されます。</summary>
		/// <param name="ex">ハンドルされなかった例外。</param>
		protected override void UnhandledException(Exception ex) { UnhandledExceptionWorker(ex); }

		/// <summary>リモートランタイムからの出力を確実に同期させる <see cref="ICommandDispatcher"/> を表します。</summary>
		class RemoteConsoleCommandDispatcher : ICommandDispatcher
		{
			RemoteCommandDispatcher _remoteCommandDispatcher;
			AutoResetEvent _remoteOutputReceived;

			internal RemoteConsoleCommandDispatcher(RemoteCommandDispatcher remoteCommandDispatcher, AutoResetEvent remoteOutputReceived)
			{
				_remoteCommandDispatcher = remoteCommandDispatcher;
				_remoteOutputReceived = remoteOutputReceived;
			}

			public object Execute(CompiledCode compiledCode, ScriptScope scope)
			{
				// リモートランタイムでコードを実行する RemoteCommandDispatcher に処理を委譲します。
				var result = _remoteCommandDispatcher.Execute(compiledCode, scope);
				// 出力は非同期的に受信されるので、リモートコンソールでの明示的な同期が必要です。
				_remoteOutputReceived.WaitOne();
				return result;
			}
		}
	}
}