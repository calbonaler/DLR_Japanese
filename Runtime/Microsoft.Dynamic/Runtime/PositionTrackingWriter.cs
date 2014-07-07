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
using System.CodeDom;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>テキストが追加された場合に効率的に (行, 列) の情報を追跡し、元のソースコードの生成されたソースコードの間の行マッピングを収集することで、正しいデバッグ情報を生成できるようにします。</summary>
	public class PositionTrackingWriter : StringWriter
	{
		List<KeyValuePair<int, int>> _lineMap = new List<KeyValuePair<int, int>>();
		List<KeyValuePair<int, string>> _fileMap = new List<KeyValuePair<int, string>>();

		int _line = 1;
		int _column = 1;

		/// <summary><see cref="Microsoft.Scripting.Runtime.PositionTrackingWriter"/> クラスの新しいインスタンスを初期化します。</summary>
		public PositionTrackingWriter() { }

		/// <summary>ライターの現在の位置を指定された元のソースコードの位置と対応するとマークします。</summary>
		/// <param name="linePragma">生成されたコードで現在の位置と対応する行プラグマを指定します。</param>
		public void MapLocation(CodeLinePragma linePragma)
		{
			_lineMap.Add(new KeyValuePair<int, int>(_line, linePragma.LineNumber));
			_fileMap.Add(new KeyValuePair<int, string>(_line, linePragma.FileName));
		}

		/// <summary>このライターの生成されたソースコードから元のソースコードへの行マッピングを取得します。</summary>
		/// <returns>取得された行マッピング。</returns>
		public KeyValuePair<int, int>[] GetLineMap() { return _lineMap.ToArray(); }

		/// <summary>このライターの生成されたソースコードの行番号から元のソースコードのファイル名へのマッピングを取得します。</summary>
		/// <returns>行番号からファイル名へのマッピング。</returns>
		public KeyValuePair<int, string>[] GetFileMap() { return _fileMap.ToArray(); }

		/// <summary>文字列に文字を書き込みます。</summary>
		/// <param name="value">書き込む文字。</param>
		/// <exception cref="ObjectDisposedException">ライターが閉じられました。</exception>
		public override void Write(char value)
		{
			if (value != '\n')
				++_column;
			else
			{
				_column = 1;
				++_line;
			}
			base.Write(value);
		}

		/// <summary>現在の文字列に文字列を書き込みます。</summary>
		/// <param name="value">書き込む文字列。</param>
		/// <exception cref="ObjectDisposedException">ライターが閉じられました。</exception>
		public override void Write(string value)
		{
			UpdateLineColumn(0, value.Length, (a, b, c) => value.IndexOf(a, b, c));
			base.Write(value);
		}

		/// <summary>文字配列の一部を文字列に書き込みます。</summary>
		/// <param name="buffer">データの書き込み元の文字配列。</param>
		/// <param name="index">データの読み取りを開始する、バッファー内の位置。</param>
		/// <param name="count">書き込む文字の最大数。</param>
		/// <exception cref="ArgumentNullException"><paramref name="buffer"/> は <c>null</c> です。</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> または <paramref name="count"/> が負の値です。</exception>
		/// <exception cref="ArgumentException">(<paramref name="index"/> + <paramref name="count"/>) の値が <paramref name="buffer"/>.Length よりも大きな値です。</exception>
		/// <exception cref="ObjectDisposedException">ライターが閉じられました。</exception>
		public override void Write(char[] buffer, int index, int count)
		{
			UpdateLineColumn(index, count, (a, b, c) => Array.IndexOf(buffer, a, b, c));
			base.Write(buffer, index, count);
		}

		void UpdateLineColumn(int index, int count, Func<char, int, int, int> indexOf)
		{
			int lastPos = index, pos;
			while ((pos = 1 + indexOf('\n', lastPos, index + count - lastPos)) > 0)
			{
				++_line;
				lastPos = pos;
			}
			if (lastPos > 0)
				_column = count - lastPos + 1;
			else
				_column += count;
		}
	}
}