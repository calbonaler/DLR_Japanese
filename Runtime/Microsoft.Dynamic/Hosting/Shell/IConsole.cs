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

using System.IO;

namespace Microsoft.Scripting.Hosting.Shell
{
	/// <summary>
	/// コンソールの入出力を制御します。
	/// このインターフェイスは <see cref="System.IO.TextReader"/>、<see cref="System.IO.TextWriter"/>、<see cref="System.Console"/> などに値します。
	/// </summary>
	public interface IConsole
	{
		/// <summary>
		/// 単一行の対話入力あるいは複数行のステートメントセットのブロックを読み取ります。
		/// イベント駆動型の GUI コンソールでは入力が利用可能であることを示すイベントをブロックおよび待機するスレッドを作成することで、このメソッドを実装することができます。
		/// </summary>
		/// <param name="autoIndentSize">
		/// 現在のステートメントセットに使用されるインデントレベルを指定します。
		/// コンソールは自動インデントをサポートしない場合、この引数を無視することができます。
		/// </param>
		/// <returns>
		/// 入力ストリームが閉じていた場合は <c>null</c>。それ以外の場合は実行するコマンドを表す文字列。
		/// 結果はステートメントのブロックとして処理されるような複数行の文字列なることもあります。
		/// </returns>
		string ReadLine(int autoIndentSize);

		/// <summary>指定された文字列を指定されたスタイルでコンソールに出力します。</summary>
		/// <param name="text">出力する文字列を指定します。</param>
		/// <param name="style">文字列を出力するスタイルを指定します。</param>
		void Write(string text, Style style);

		/// <summary>指定された文字列を指定されたスタイルでコンソールに出力し、続けて改行文字を出力します。</summary>
		/// <param name="text">出力する文字列を指定します。</param>
		/// <param name="style">文字列を出力するスタイルを指定します。</param>
		void WriteLine(string text, Style style);

		/// <summary>コンソールに改行文字を出力します。</summary>
		void WriteLine();

		/// <summary>コンソールの標準出力を表す <see cref="TextWriter"/> を取得または設定します。</summary>
		TextWriter Output { get; set; }

		/// <summary>コンソールの標準エラー出力を表す <see cref="TextWriter"/> を取得または設定します。</summary>
		TextWriter ErrorOutput { get; set; }
	}
}
