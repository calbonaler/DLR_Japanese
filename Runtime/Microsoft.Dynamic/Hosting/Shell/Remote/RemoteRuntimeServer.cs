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
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting.Lifetime;

namespace Microsoft.Scripting.Hosting.Shell.Remote
{
	/// <summary>リモートランタイムサーバーが初期化された <see cref="ScriptEngine"/> や <see cref="ScriptRuntime"/> をリモーティングチャンネルを通して公開する際に使用されます。</summary>
	static class RemoteRuntimeServer
	{
		internal const string CommandDispatcherUri = "CommandDispatcherUri";
		internal const string RemoteRuntimeArg = "-X:RemoteRuntimeChannel";

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2116:AptcaMethodsShouldOnlyCallAptcaMethods")] // TODO: Microsoft.Scripting does not need to be APTCA
		internal static IpcChannel CreateChannel(string channelName, string portName)
		{
			// チャンネルを作成する
			return new IpcChannel(
				new System.Collections.Hashtable()
				{
					{ "name", channelName },
					{ "portName", portName },
					// exclusiveAddressUse は CreateNamedPipe の FILE_FLAG_FIRST_PIPE_INSTANCE フラグに対応しています
					// これを true に設定すると "IPC ポートを作成できません: アクセスが拒否されました" というエラーがときどき発生します
					// TODO: これを false に設定することは ACL も使用する限り安全です
					{ "exclusiveAddressUse", false },
				},
				null,
				// The Hosting API classes require TypeFilterLevel.Full to be remoted 
				new BinaryServerFormatterSinkProvider() { TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full }
			);
		}

		/// <summary>ホストが使用できるようにオブジェクトを公開して、(入力ストリームがオープンされるまで) 無期限にブロックします。</summary>
		/// <param name="remoteRuntimeChannelName">リモートコンソールが <see cref="ScriptEngine"/> との相互通信に使用すると予測される IPC チャンネルを指定します。</param>
		/// <param name="scope">スクリプトコマンド処理の開始準備ができている初期化済みの <see cref="ScriptScope"/> を指定します。</param>
		/// <remarks>
		/// 注釈: 1 つのオブジェクトのみを公開するようにして、他のオブジェクトはそれからアクセスできるようにしてください。
		/// リモーティングは両方のプロキシに対するサーバーオブジェクトが同じサーバーにあるかどうかを知る方法がないため、
		/// 複数のオブジェクトの発行はクライアントが "remoteProxy1(remoteProxy2)" のような呼び出しをする場合に、問題を発生させる恐れがあります。
		/// </remarks>
		internal static void StartServer(string remoteRuntimeChannelName, ScriptScope scope)
		{
			Debug.Assert(ChannelServices.GetChannel(remoteRuntimeChannelName) == null);
			var channel = CreateChannel("ipc", remoteRuntimeChannelName);
			LifetimeServices.LeaseTime = TimeSpan.FromDays(7);
			LifetimeServices.LeaseManagerPollTime = TimeSpan.FromDays(7);
			LifetimeServices.RenewOnCallTime = TimeSpan.FromDays(7);
			LifetimeServices.SponsorshipTimeout = TimeSpan.FromDays(7);
			ChannelServices.RegisterChannel(channel, false);
			try
			{
				RemotingServices.Marshal(new RemoteCommandDispatcher(scope), CommandDispatcherUri);
				// リモートコンソールに (存在すれば) 起動時出力が完了したことを知らせます。
				// すべての起動時出力が続行前にリモートコンソールに到着することを必要としているので、名前付きイベントの代わりにこれを使用します。
				Console.WriteLine(RemoteCommandDispatcher.OutputCompleteMarker);
				// Console.In でブロックします。
				// これはホストが終了を処理したときに ReadLine が null を返すので、終了を判断するために使用されます。
				var input = Console.ReadLine();
				Debug.Assert(input == null);
			}
			finally { ChannelServices.UnregisterChannel(channel); }
		}
	}
}