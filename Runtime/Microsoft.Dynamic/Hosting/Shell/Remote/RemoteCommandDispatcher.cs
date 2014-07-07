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
using System.Security.Permissions;
using System.Threading;

namespace Microsoft.Scripting.Hosting.Shell.Remote
{
	/// <summary>
	/// <see cref="RemoteConsoleHost"/> が長時間実行されている操作を中止できるようにします。
	/// <see cref="RemoteConsoleHost"/> 自体はどのスレッドプールのスレッドがリモート呼び出しを処理しているのかを知らないため、これにはリモートランタイムサーバーからの協調動作を必要とします。
	/// </summary>
	public class RemoteCommandDispatcher : MarshalByRefObject, ICommandDispatcher
	{
		/// <summary>リモート コンソールが現在のコマンドからのすべての出力が到着したことを確認できるような出力の終わりを示すマーカーを表します。</summary>
		internal const string OutputCompleteMarker = "{7FF032BB-DB03-4255-89DE-641CA195E5FA}";
		Thread _executingThread;

		/// <summary>指定されたスコープを使用して、<see cref="Microsoft.Scripting.Hosting.Shell.Remote.RemoteCommandDispatcher"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="scope">このディスパッチャに関連付けるスコープを指定します。</param>
		public RemoteCommandDispatcher(ScriptScope scope) { ScriptScope = scope; }

		/// <summary>このディスパッチャに関連付けられたスコープを取得します。</summary>
		public ScriptScope ScriptScope { get; private set; }

		/// <summary>指定された長時間実行される可能性のあるコードを指定されたスコープで実行し、結果を返します。</summary>
		/// <param name="compiledCode">実行するコードを指定します。</param>
		/// <param name="scope">コードを実行するスコープを指定します。</param>
		/// <returns>コード実行の結果。</returns>
		public object Execute(CompiledCode compiledCode, ScriptScope scope)
		{
			Debug.Assert(_executingThread == null);
			_executingThread = Thread.CurrentThread;
			try
			{
				object result = compiledCode.Execute(scope);
				Console.WriteLine(RemoteCommandDispatcher.OutputCompleteMarker);
				return result;
			}
			catch (ThreadAbortException tae)
			{
				var pki = tae.ExceptionState as KeyboardInterruptException;
				if (pki != null)
				{
					// ほとんどの例外はクライアントに逆伝播されます。
					// しかし、ThreadAbortException はリモーティング インフラストラクチャによって異なって処理され、RemotingException にラップされます。
					// ("サーバーでの要求の処理中にエラーが発生しました。")
					// そのため、フィルタして代わりに KeyboardInterruptException を発生させます。
					Thread.ResetAbort();
					throw pki;
				}
				else
					throw;
			}
			finally { _executingThread = null; }
		}

		/// <summary>現在実行中の <see cref="Execute"/> への呼び出しを <see cref="Thread.Abort(object)"/> により中止します。</summary>
		/// <returns>実際に <see cref="Thread.Abort(object)"/> が呼ばれた場合は <c>true</c>。実行中の <see cref="Execute"/> への呼び出しが存在しない場合は <c>false</c>。</returns>
		public bool AbortCommand()
		{
			var executingThread = _executingThread;
			if (executingThread == null)
				return false;
			executingThread.Abort(new KeyboardInterruptException(""));
			return true;
		}

		// TODO: どれが正しい生存期間なのかを計算する
		/// <summary>対象のインスタンスの有効期間ポリシーを制御する、有効期間サービス オブジェクトを取得します。</summary>
		/// <returns>
		/// 対象のインスタンスの有効期間ポリシーを制御するときに使用する、<see cref="System.Runtime.Remoting.Lifetime.ILease"/> 型のオブジェクト。
		/// 存在する場合は、このインスタンスの現在の有効期間サービス オブジェクトです。それ以外の場合は、<see cref="System.Runtime.Remoting.Lifetime.LifetimeServices.LeaseManagerPollTime"/>
		/// プロパティの値に初期化された新しい有効期間サービス オブジェクトです。
		/// </returns>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
		public override object InitializeLifetimeService() { return null; }
	}
}
