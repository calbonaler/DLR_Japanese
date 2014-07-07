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
using System.Collections.ObjectModel;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting.Shell
{
	/// <summary>コンソール全体に対するオプションを表します。</summary>
	[Serializable]
	public class ConsoleOptions
	{
		/// <summary>コンソールで自動的にインデントを行うかどうかを示す値を取得または設定します。</summary>
		public bool AutoIndent { get; set; }

		/// <summary>例外をハンドルするかどうかを示す値を取得または設定します。</summary>
		public bool HandleExceptions { get; set; }

		/// <summary>コンソールでタブ補完を行うかどうかを示す値を取得または設定します。</summary>
		public bool TabCompletion { get; set; }

		/// <summary>色つきのコンソールを使用するかどうかを示す値を取得または設定します。</summary>
		public bool ColorfulConsole { get; set; }

		/// <summary>使用方法を表示するかどうかを示す値を取得または設定します。</summary>
		public bool PrintUsage { get; set; }

		/// <summary>起動時オプションで与えられたリテラルのスクリプトコマンドを取得または設定します。</summary>
		public string Command { get; set; }

		/// <summary>実行するファイル名を取得または設定します。</summary>
		public string FileName { get; set; }

		/// <summary>バージョンを表示するかどうかを示す値を取得または設定します。</summary>
		public bool PrintVersion { get; set; }

		/// <summary>プロンプトを出すことなく即座に実行を終了させるかどうかを示す値を取得または設定します。</summary>
		public bool Exit { get; set; }

		/// <summary>コンソールの自動インデントサイズを取得または設定します。</summary>
		public int AutoIndentSize { get; set; }

		/// <summary>解析されなかった残りの引数を取得または設定します。</summary>
		public ReadOnlyCollection<string> RemainingArgs { get; set; }

		/// <summary>内部調査を行うかどうかを示す値を取得または設定します。</summary>
		public bool Introspection { get; set; }

		/// <summary>実際の実行をマルチスレッドアパートメントとしてマークされたスレッドで行うかどうかを示す値を取得または設定します。</summary>
		public bool IsMta { get; set; }

		/// <summary>リモートコンソールが <see cref="ScriptEngine"/> との相互通信に使用すると予期される IPC チャンネルを取得または設定します。</summary>
		public string RemoteRuntimeChannel { get; set; }

		/// <summary><see cref="Microsoft.Scripting.Hosting.Shell.ConsoleOptions"/> クラスの新しいインスタンスを初期化します。</summary>
		public ConsoleOptions()
		{
			HandleExceptions = true;
			AutoIndentSize = 4;
		}

		/// <summary>基になるオプションを使用して、<see cref="Microsoft.Scripting.Hosting.Shell.ConsoleOptions"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="options">設定をコピーするオプションを指定します。</param>
		protected ConsoleOptions(ConsoleOptions options)
		{
			ContractUtils.RequiresNotNull(options, "options");
			Command = options.Command;
			FileName = options.FileName;
			PrintVersion = options.PrintVersion;
			Exit = options.Exit;
			AutoIndentSize = options.AutoIndentSize;
			RemainingArgs = options.RemainingArgs.ToReadOnly();
			Introspection = options.Introspection;
			AutoIndent = options.AutoIndent;
			HandleExceptions = options.HandleExceptions;
			TabCompletion = options.TabCompletion;
			ColorfulConsole = options.ColorfulConsole;
			PrintUsage = options.PrintUsage;
			IsMta = options.IsMta;
			RemoteRuntimeChannel = options.RemoteRuntimeChannel;
		}
	}
}
