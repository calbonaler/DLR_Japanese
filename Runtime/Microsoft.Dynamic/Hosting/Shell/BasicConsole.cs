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
using System.IO;
using System.Threading;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting.Shell
{
	/// <summary><see cref="IConsole"/> を実装する基本的なコンソールを表します。</summary>
	public class BasicConsole : IConsole, IDisposable
	{
		TextWriter _output;
		TextWriter _errorOutput;

		/// <summary>このコンソールの出力を書き込む <see cref="TextWriter"/> を取得または設定します。</summary>
		public TextWriter Output
		{
			get { return _output; }
			set
			{
				ContractUtils.RequiresNotNull(value, "value");
				_output = value;
			}
		}

		/// <summary>このコンソールのエラー出力を書き込む <see cref="TextWriter"/> を取得または設定します。</summary>
		public TextWriter ErrorOutput
		{
			get { return _errorOutput; }
			set
			{
				ContractUtils.RequiresNotNull(value, "value");
				_errorOutput = value;
			}
		}

		/// <summary>このコンソールが Ctrl-C を入力された際に終了できるようにする <see cref="AutoResetEvent"/> を取得または設定します。</summary>
		protected AutoResetEvent CtrlCEvent { get; set; }

		/// <summary>このコンソールが作成されたスレッドを取得または設定します。</summary>
		protected Thread CreatingThread { get; set; }

		/// <summary>基になるコンソールでキャンセルキーが押された時に呼ばれるデリゲートを取得または設定します。</summary>
		public ConsoleCancelEventHandler ConsoleCancelEventHandler { get; set; }

		ConsoleColor _promptColor;
		ConsoleColor _outColor;
		ConsoleColor _errorColor;
		ConsoleColor _warningColor;

		/// <summary>各スタイルで異なる色を使用するかどうかを示す値を使用して、<see cref="Microsoft.Scripting.Hosting.Shell.BasicConsole"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="colorful">このコンソールが <see cref="Style"/> 列挙体の各値に応じて異なる色を使用するかどうかを示す値を指定します。</param>
		public BasicConsole(bool colorful)
		{
			_output = System.Console.Out;
			_errorOutput = System.Console.Error;
			SetupColors(colorful);
			CreatingThread = Thread.CurrentThread;
			// 既定のハンドラを作成
			ConsoleCancelEventHandler = (sender, e) =>
			{
				if (e.SpecialKey == ConsoleSpecialKey.ControlC)
				{
					e.Cancel = true;
					CtrlCEvent.Set();
					CreatingThread.Abort(new KeyboardInterruptException(""));
				}
			};
			Console.CancelKeyPress += (sender, e) =>
			{
				// 登録されたハンドラにディスパッチ
				if (ConsoleCancelEventHandler != null)
					ConsoleCancelEventHandler(sender, e);
			};
			CtrlCEvent = new AutoResetEvent(false);
		}

		void SetupColors(bool colorful)
		{
			if (colorful)
			{
				_promptColor = PickColor(ConsoleColor.Gray, ConsoleColor.White);
				_outColor = PickColor(ConsoleColor.Cyan, ConsoleColor.White);
				_errorColor = PickColor(ConsoleColor.Red, ConsoleColor.White);
				_warningColor = PickColor(ConsoleColor.Yellow, ConsoleColor.White);
			}
			else
				_promptColor = _outColor = _errorColor = _warningColor = Console.ForegroundColor;
		}

		static ConsoleColor PickColor(ConsoleColor best, ConsoleColor other)
		{
			if (Console.BackgroundColor != best)
				return best;
			return other;
		}

		/// <summary>指定された <see cref="TextWriter"/> に指定された文字列を指定された色で書き込みます。</summary>
		/// <param name="output">文字列が書き込まれる <see cref="TextWriter"/> を指定します。</param>
		/// <param name="str">書き込まれる文字列を指定します。</param>
		/// <param name="c">文字列が表示される色を指定します。</param>
		protected void WriteColor(TextWriter output, string str, ConsoleColor c)
		{
			var origColor = Console.ForegroundColor;
			Console.ForegroundColor = c;
			output.Write(str);
			output.Flush();
			Console.ForegroundColor = origColor;
		}

		/// <summary>指定されたインデント幅でコンソールから 1 行を読み取ります。</summary>
		/// <param name="autoIndentSize">行の左端に挿入されるインデントの幅を指定します。</param>
		/// <returns>読み取られた文字列。文字列には自動インデントの幅も含まれます。</returns>
		public virtual string ReadLine(int autoIndentSize)
		{
			Write("".PadLeft(autoIndentSize), Style.Prompt);
			var res = Console.In.ReadLine();
			if (res == null)
			{
				// 競合があります - Ctrl-C イベントは ReadLine から制御が戻った後に到着します。
				// どちらが発生したかどうかを知るにはすこし待つ必要があります。
				// これは Ctrl-Z を通したシャットダウン処理にわずかな遅延を生じさせますが、本当に気づくほどではありません。
				// Ctrl-C の場合イベントがシグナル状態になれば、すぐに戻ります。
				if (CtrlCEvent != null && CtrlCEvent.WaitOne(100, false))
					return ""; // ctrl-C
				else
					return null; // ctrl-Z
			}
			return "".PadLeft(autoIndentSize) + res;
		}

		/// <summary>指定された文字列を指定されたスタイルで適切なストリームに書き込みます。</summary>
		/// <param name="text">書き込まれる文字列を指定します。</param>
		/// <param name="style">書き込まれる文字列のスタイルを指定します。</param>
		public virtual void Write(string text, Style style)
		{
			switch (style)
			{
				case Style.Prompt: WriteColor(_output, text, _promptColor); break;
				case Style.Out: WriteColor(_output, text, _outColor); break;
				case Style.Error: WriteColor(_errorOutput, text, _errorColor); break;
				case Style.Warning: WriteColor(_errorOutput, text, _warningColor); break;
			}
		}

		/// <summary>指定された文字列を指定されたスタイルで適切なストリームに書き込み、続けて行終端記号を出力します。</summary>
		/// <param name="text">書き込まれる文字列を指定します。</param>
		/// <param name="style">書き込まれる文字列のスタイルを指定します。</param>
		public void WriteLine(string text, Style style) { Write(text + Environment.NewLine, style); }

		/// <summary>標準出力に行終端記号を出力します。</summary>
		public void WriteLine() { Write(Environment.NewLine, Style.Out); }

		/// <summary>このクラスで使用されているすべてのリソースを破棄します。</summary>
		public void Dispose()
		{
			if (CtrlCEvent != null)
				CtrlCEvent.Close();
			GC.SuppressFinalize(this);
		}
	}
}

