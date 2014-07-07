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
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional")] // TODO: fix
		public KeyValuePair<string, string>[] GetHelp()
		{
			return new[] {
                new KeyValuePair<string, string>("/help",                     "Displays this help."),
                new KeyValuePair<string, string>("/lang:<extension>",         "Specify language by the associated extension (py, js, vb, rb). Determined by an extension of the first file. Defaults to IronPython."),
                new KeyValuePair<string, string>("/paths:<file-path-list>",   "Semicolon separated list of import paths (/run only)."),
                new KeyValuePair<string, string>("/setenv:<var1=value1;...>", "Sets specified environment variables for the console process. Not available on Silverlight."),
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
