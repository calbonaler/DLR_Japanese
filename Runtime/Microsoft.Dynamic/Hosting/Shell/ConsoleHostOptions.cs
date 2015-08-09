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

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Scripting.Hosting.Shell
{
	/// <summary><see cref="ConsoleHost"/> が参照するオプションを表します。</summary>
	public class ConsoleHostOptions
	{
		/// <summary><see cref="ConsoleHostOptionsParser"/> によって解析がスキップされた引数を取得します。</summary>
		public List<string> IgnoredArgs { get; private set; }

		/// <summary><see cref="ConsoleHost"/> が実行するファイル名を取得または設定します。指定されていない場合は <c>null</c> になります。</summary>
		public string RunFile { get; set; }

		/// <summary>引数で指定された追加の検索パスを取得または設定します。指定されていない場合は <c>null</c> になります。</summary>
		public ReadOnlyCollection<string> SourceUnitSearchPaths { get; set; }

		/// <summary><see cref="ConsoleHost"/> によって実行される動作を取得または設定します。</summary>
		public ConsoleHostAction RunAction { get; set; }

		/// <summary>引数で作成または上書きされた環境変数を取得します。</summary>
		public List<string> EnvironmentVars { get; private set; }

		/// <summary><see cref="ConsoleHost"/> で使用される言語プロバイダの型名を取得または設定します。</summary>
		public string LanguageProvider { get; set; }

		/// <summary>言語プロバイダが引数で指定されたかどうかを示す値を取得または設定します。</summary>
		public bool HasLanguageProvider { get; set; }

		/// <summary><see cref="Microsoft.Scripting.Hosting.Shell.ConsoleHostOptions"/> クラスの新しいインスタンスを初期化します。</summary>
		public ConsoleHostOptions()
		{
			IgnoredArgs = new List<string>();
			EnvironmentVars = new List<string>();
		}

		/// <summary><see cref="ConsoleHost"/> の起動時オプションに関するヘルプを取得します。</summary>
		/// <returns>オプション書式と説明が格納された 2 次元配列。</returns>
		public KeyValuePair<string, string>[] GetHelp()
		{
			return new[] {
                new KeyValuePair<string, string>("/help",                       "このヘルプを表示します。"),
                new KeyValuePair<string, string>("/lang:<拡張子>",              "関連付けられた拡張子 (py, js, vb, rb) から言語を指定します。最初のファイルの拡張子から判断されます。既定値は IronPython です。"),
                new KeyValuePair<string, string>("/paths:<ファイルパスリスト>", "インポートパスのセミコロン区切りのリスト (/run のみ)."),
                new KeyValuePair<string, string>("/setenv:<変数1=値1;...>",   "指定された環境変数をコンソールプロセスに対して設定します。Silverlight では利用できません。"),
            };
		}
	}

	/// <summary><see cref="ConsoleHost"/> によって実行される動作を示します。</summary>
	public enum ConsoleHostAction
	{
		/// <summary><see cref="ConsoleHost"/> は何もしません。</summary>
		None,
		/// <summary><see cref="ConsoleHost"/> はコンソールを実行します。</summary>
		RunConsole,
		/// <summary><see cref="ConsoleHost"/> は指定されたファイルを実行します。</summary>
		RunFile,
		/// <summary><see cref="ConsoleHost"/> はヘルプを表示します。</summary>
		DisplayHelp
	}
}
