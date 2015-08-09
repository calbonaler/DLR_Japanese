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
//#define DUMP_TOKENS

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>トークナイザが使用できるバッファを提供します。</summary>
	public sealed class TokenizerBuffer
	{
		const int FirstColumn = 1;
		/// <summary>ファイルの末尾に達したことを示す文字を表します。</summary>
		public const int EndOfFile = -1;
		/// <summary>バッファ内で無効な文字を表します。</summary>
		public const int InvalidCharacter = -2;

		/// <summary>
		/// 複数形式の改行文字を認めるかどうかを示します。
		/// <c>false</c> の場合 \n のみが改行文字として扱われます。
		/// それ以外の場合は \n, \r\n, \r が改行文字として扱われます。
		/// </summary>
		bool _multiEols;
		char[] _buffer;
		/// <summary>
		/// バッファがリサイズされたかどうかを示します。
		/// バッファの内容はその開始位置にシフトされ、もはや使用されていないバッファ内のデータは破棄されます。
		/// </summary>
		bool _bufferResized;
		/// <summary>次に読み取られる文字を示すバッファ内の位置を示します。</summary>
		int _position;
		/// <summary>トークンの終了位置を示します。これは後で計算されます。</summary>
		SourceLocation _tokenEndLocation;
		/// <summary>バッファ内の有効な文字の最初を示すインデックスを示します。</summary>
		int _start;
		/// <summary>バッファ内の有効な文字の最後の直後の文字を示すインデックスを示します。</summary>
		int _end;
		/// <summary>現在のトークンが終了する文字の次の文字を示すインデックスを示します。(トークンの開始位置は <see cref="_start"/> になります。)</summary>
		int _tokenEnd;

		/// <summary>このバッファがデータを取得する <see cref="TextReader"/> を取得します。</summary>
		public TextReader Reader { get; private set; }

		/// <summary>次に読み取られる文字が基になる <see cref="TextReader"/> の最初の文字であるかどうかを示す値を取得します。</summary>
		public bool AtBeginning { get { return _position == 0 && !_bufferResized; } }

		/// <summary>現在のトークンに含まれている文字数を取得します。</summary>
		public int TokenLength
		{
			get
			{
				if (_tokenEnd == -1)
					throw new InvalidOperationException("トークンの終端が記録されていません。");
				return _tokenEnd - _start;
			}
		}

		/// <summary>現在のトークンの次の読みとられる文字位置に相対的な位置を取得します。</summary>
		public int TokenRelativePosition { get { CheckInvariants(); return _position - _start; } }

		/// <summary>次に読み取られる文字を示すバッファ内の位置を取得します。</summary>
		public int Position { get { CheckInvariants(); return _position; } }

		/// <summary>現在のトークンのソースコード内での範囲を取得します。</summary>
		public SourceSpan TokenSpan { get { return new SourceSpan(TokenStart, TokenEnd); } }

		/// <summary>現在のトークンの最初の文字に対応する位置を取得します。</summary>
		public SourceLocation TokenStart { get; private set; }

		/// <summary>現在のトークンの最後の文字の次の文字に対応する位置を取得します。</summary>
		public SourceLocation TokenEnd
		{
			get
			{
				if (_tokenEnd == -1)
					throw new InvalidOperationException("トークンの終端が記録されていません。");
				return _tokenEndLocation;
			}
		}

		/// <summary>指定された引数を使用して、<see cref="Microsoft.Scripting.Runtime.TokenizerBuffer"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="reader">データの取得元の <see cref="TextReader"/> を指定します。</param>
		/// <param name="initialLocation"><see cref="TextReader"/> の現在の位置に対応する <see cref="SourceLocation"/> を指定します。</param>
		/// <param name="initialCapacity">バッファの初期容量を <see cref="System.Char"/> の個数で指定します。</param>
		/// <param name="multiEols">'\n' 以外の改行記号を改行と判断するかどうかを示す値を指定します。</param>
		public TokenizerBuffer(TextReader reader, SourceLocation initialLocation, int initialCapacity, bool multiEols) { Initialize(reader, initialLocation, initialCapacity, multiEols); }

		/// <summary>指定された引数を使用して、この <see cref="TokenizerBuffer"/> を初期化します。</summary>
		/// <param name="reader">データの取得元の <see cref="TextReader"/> を指定します。</param>
		/// <param name="initialLocation"><see cref="TextReader"/> の現在の位置に対応する <see cref="SourceLocation"/> を指定します。</param>
		/// <param name="initialCapacity">バッファの初期容量を <see cref="System.Char"/> の個数で指定します。</param>
		/// <param name="multiEols">'\n' 以外の改行記号を改行と判断するかどうかを示す値を指定します。</param>
		public void Initialize(TextReader reader, SourceLocation initialLocation, int initialCapacity, bool multiEols)
		{
			ContractUtils.RequiresNotNull(reader, "reader");
			ContractUtils.Requires(initialCapacity > 0, "initialCapacity");
			Reader = reader;
			if (_buffer == null || _buffer.Length < initialCapacity)
				_buffer = new char[initialCapacity];
			_tokenEnd = -1;
			_multiEols = multiEols;
			_tokenEndLocation = SourceLocation.Invalid;
			TokenStart = initialLocation;
			_start = _end = 0;
			_position = 0;
			CheckInvariants();
		}

		/// <summary>基になる <see cref="TextReader"/> から次の文字を読み取り、文字位置を 1 文字進めます。</summary>
		/// <returns>読み取られた文字を表す整数値。末尾を表す <see cref="EndOfFile"/> が返される可能性があります。</returns>
		public int Read()
		{
			var result = Peek();
			_position++;
			return result;
		}

		/// <summary>次の文字が指定された文字であれば、文字位置を 1 文字進めます。</summary>
		/// <param name="expectedChar">文字位置を進める文字を指定します。この値には <see cref="EndOfFile"/> なども指定することができます。</param>
		/// <returns>次の文字が指定された文字の場合には <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool Read(int expectedChar)
		{
			CheckInvariants();
			if (Peek() == expectedChar)
			{
				_position++;
				CheckInvariants();
				return true;
			}
			else
				return false;
		}

		/// <summary>バッファ内の現在の文字以降が指定された文字列に等しい場合はその文字数分文字位置を進めます。</summary>
		/// <param name="expectedString">文字位置を進める文字列を指定します。</param>
		/// <returns>バッファ内の現在の文字以降が指定された文字列に等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool Read(string expectedString)
		{
			ContractUtils.RequiresNotEmpty(expectedString, "expectedString");
			CheckInvariants();
			var oldPos = _position;
			// ensure sufficient data loaded:
			SeekRelative(expectedString.Length - 1);
			if (Read() == EndOfFile)
			{
				Seek(oldPos);
				CheckInvariants();
				return false;
			}
			if (_position + expectedString.Length > _buffer.Length)
				throw new ArgumentOutOfRangeException("expectedString");
			for (int i = 0; i < expectedString.Length; i++)
			{
				if (_buffer[i] != expectedString[i])
				{
					Seek(oldPos);
					CheckInvariants();
					return false;
				}
			}
			CheckInvariants();
			return true;
		}

		/// <summary>基になる <see cref="TextReader"/> から次の文字を読み取り、バッファに格納してから返します。</summary>
		/// <returns>文字が有効な場合はその文字。それ以外の場合は <see cref="EndOfFile"/> などの負数値。</returns>
		public int Peek()
		{
			CheckInvariants();
			if (_position >= _end)
			{
				RefillBuffer();
				if (_position >= _end) // eof:
				{
					CheckInvariants();
					return EndOfFile;
				}
			}
			Debug.Assert(_position < _end);
			var result = _buffer[_position];
			CheckInvariants();
			return result;
		}

		void RefillBuffer()
		{
			if (_end == _buffer.Length)
			{
				ResizeInternal(ref _buffer, System.Math.Max(System.Math.Max((_end - _start) * 2, _buffer.Length), _position), _start, _end - _start);
				_end -= _start;
				_position -= _start;
				_start = 0;
				_bufferResized = true;
			}
			// make the buffer full:
			_end += Reader.Read(_buffer, _end, _buffer.Length - _end);
			ClearInvalidChars();
		}

		/// <summary>次に読み取る文字位置を 1 文字分戻します。</summary>
		public void Back() { SeekRelative(-1); }

		/// <summary>次に読み取る文字位置をバッファの先頭からのオフセットを使用して設定します。</summary>
		/// <param name="offset">文字位置を設定するバッファの先頭からのオフセットを指定します。</param>
		public void Seek(int offset)
		{
			CheckInvariants();
			if (offset < 0)
				throw new ArgumentOutOfRangeException("offset");
			// no upper limit, we can seek beyond end in which case we are reading EOFs
			_position = _start + offset;
			CheckInvariants();
		}

		/// <summary>次に読み取る文字位置を現在の値からの変化量で指定します。</summary>
		/// <param name="disp">現在の設定されている次に読み取られる文字位置からの設定する文字位置の変化量を指定します。</param>
		public void SeekRelative(int disp)
		{
			CheckInvariants();
			if (disp < _start - _position)
				throw new ArgumentOutOfRangeException("disp");
			// no upper limit, we can seek beyond end in which case we are reading EOFs
			_position += disp;
			CheckInvariants();
		}

		/// <summary>複数行トークンの終了をマークします。</summary>
		public void MarkMultiLineTokenEnd()
		{
			CheckInvariants();
			_tokenEnd = System.Math.Min(_position, _end);
			_tokenEndLocation = CalcTokenEnd();
			DumpToken();
			CheckInvariants();
		}

		/// <summary>
		/// 単一行トークンの終了をマークします。
		/// 機能的にこれは <see cref="MarkMultiLineTokenEnd()"/> と同じですが、単一行と判明している場合はこちらの方が速くなります。
		/// </summary>
		public void MarkSingleLineTokenEnd()
		{
			CheckInvariants();
			_tokenEnd = System.Math.Min(_position, _end);
			_tokenEndLocation = new SourceLocation(TokenStart.Index + _tokenEnd - _start, TokenStart.Line, TokenStart.Column + _tokenEnd - _start);
			DumpToken();
			CheckInvariants();
		}

		/// <summary>複数行トークンの終了を指定された値だけ現在位置から移動した場所でマークします。</summary>
		/// <param name="disp">トークンの終了をマークする位置の現在位置からの変化量を指定します。</param>
		public void MarkMultiLineTokenEnd(int disp)
		{
			SeekRelative(disp);
			MarkMultiLineTokenEnd();
		}

		/// <summary>
		/// 単一行トークンの終了を指定された値だけ現在位置から移動した場所でマークします。
		/// 機能的にこれは <see cref="MarkMultiLineTokenEnd(int)"/> と同じですが、単一行と判明している場合はこちらの方が速くなります。
		/// </summary>
		/// <param name="disp">トークンの終了をマークする位置の現在位置からの変化量を指定します。</param>
		public void MarkSingleLineTokenEnd(int disp)
		{
			SeekRelative(disp);
			MarkSingleLineTokenEnd();
		}

		/// <summary>トークンが複数行であるかどうかを指定して、トークンの終了をマークします。</summary>
		/// <param name="multiLine">終了をマークするトークンが複数行にわたるかどうかを示す値を指定します。</param>
		public void MarkTokenEnd(bool multiLine)
		{
			if (multiLine)
				MarkMultiLineTokenEnd();
			else
				MarkSingleLineTokenEnd();
		}

		/// <summary>
		/// トークンの開始をマークします。これによってバッファは現在のトークンを破棄できるようになります
		/// このメソッドはトークンが読み取られていない場合も呼び出すことができます。
		/// </summary>
		public void DiscardToken()
		{
			CheckInvariants();
			// no token marked => mark it now:
			if (_tokenEnd == -1)
				MarkMultiLineTokenEnd();
			// the current token's end is the next token's start:
			TokenStart = _tokenEndLocation;
			_start = _tokenEnd;
			_tokenEnd = -1;
#if DEBUG
			_tokenEndLocation = SourceLocation.Invalid;
#endif
			CheckInvariants();
		}

		/// <summary>バッファ内の先頭からのオフセットを指定して、その位置にある文字を取得します。</summary>
		/// <param name="offset">取得する文字の位置を示すバッファ内の先頭からのオフセットを指定します。</param>
		/// <returns>バッファ内の先頭からのオフセットが示す位置にある文字。</returns>
		public char GetChar(int offset)
		{
			ContractUtils.RequiresArrayIndex(_end, offset, "offset");
			return _buffer[_start + offset];
		}

		/// <summary>次に読み取られる文字位置からの変化量を指定して、その位置にある文字を取得します。</summary>
		/// <param name="disp">取得する文字の位置を示す次に読み取られる文字位置からの変化量を指定します。</param>
		/// <returns>バッファ内の次に読み取られる文字位置からの指定量変化した位置にある文字。</returns>
		public char GetCharRelative(int disp)
		{
			CheckInvariants();
			if (disp < _start - _position)
				throw new ArgumentOutOfRangeException("disp");
			return _buffer[_position + disp];
		}

		/// <summary>現在のトークンが示すバッファ内の文字列を取得します。</summary>
		/// <returns>トークンに対応するバッファ内の文字列。</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public string GetTokenString()
		{
			CheckInvariants();
			if (_tokenEnd == -1)
				throw new InvalidOperationException("トークンの終端が記録されていません。");
			return new string(_buffer, _start, _tokenEnd - _start);
		}

		/// <summary>現在のトークンが示すバッファ内の範囲から指定されたオフセット以降の範囲にある文字列を取得します。</summary>
		/// <param name="offset">トークン内の部分文字列の取得の開始位置を示すオフセットを指定します。</param>
		/// <returns>指定されたオフセットから開始されトークンの終了までの部分を表す文字列。</returns>
		public string GetTokenSubstring(int offset) { return GetTokenSubstring(offset, _tokenEnd - _start - offset); }

		/// <summary>現在のトークンが示すバッファ内の範囲から指定された範囲にある文字列を取得します。</summary>
		/// <param name="offset">トークン内の部分文字列の取得の開始位置を示すオフセットを指定します。</param>
		/// <param name="length">トークン内で取得する部分文字列の文字数を指定します。</param>
		/// <returns>指定されたオフセットから開始され指定された文字数分の範囲を表す文字列。</returns>
		public string GetTokenSubstring(int offset, int length)
		{
			CheckInvariants();
			if (_tokenEnd == -1)
				throw new InvalidOperationException("トークンの終端が記録されていません。");
			ContractUtils.RequiresArrayRange(_tokenEnd - _start, offset, length, "offset", "length");
			return new string(_buffer, _start + offset, length);
		}

		SourceLocation CalcTokenEnd()
		{
			var endLine = TokenStart.Line;
			var endColumn = TokenStart.Column;
			for (int i = _start; i < _tokenEnd; i++)
			{
				if (_buffer[i] == '\n')
				{
					if (!_multiEols || i > _start && _buffer[i - 1] != '\r')
					{
						endColumn = FirstColumn;
						endLine++;
					}
				}
				else if (_multiEols && _buffer[i] == '\r')
				{
					endColumn = FirstColumn;
					endLine++;
				}
				else
					endColumn++;
			}
			return new SourceLocation(TokenStart.Index + _tokenEnd - _start, endLine, endColumn);
		}

		/// <summary>指定された文字が改行文字であるかどうかを判断します。</summary>
		/// <param name="current">改行文字かどうかを判断する文字を指定します。</param>
		/// <returns>
		/// 指定された文字が改行文字の場合は <c>true</c>。それ以外の場合は <c>false</c>。
		/// どの文字が改行文字として扱われるかはコンストラクタあるいは <see cref="Initialize"/> メソッドの引数に依存します。
		/// </returns>
		public bool IsEol(int current) { return current == '\n' || _multiEols && current == '\r'; }

		/// <summary>指定された文字にしたがって改行文字を読み込みます。</summary>
		/// <param name="current">このバッファの現在の文字を指定します。</param>
		/// <returns>読み込まれた改行文字の文字数。改行文字に遭遇しなかった場合は 0 が返されます。</returns>
		public int ReadEolOpt(int current)
		{
			if (current == '\n')
				return 1;
			if (current == '\r' && _multiEols)
			{
				if (Peek() == '\n')
				{
					SeekRelative(+1);
					return 2;
				}
				return 1;
			}
			return 0;
		}

		/// <summary>改行文字が現れるまで読み込み、改行文字の直前の文字を返します。返された文字はスキップされません。</summary>
		/// <returns>改行文字が現れる直前の文字。</returns>
		public int ReadLine()
		{
			int ch;
			do
				ch = Read();
			while (ch != EndOfFile && !IsEol(ch));
			Back();
			return ch;
		}

		/// <summary>配列のサイズを指定されたサイズに変更して、元の配列の部分を変更された配列の最初にコピーします。</summary>
		static void ResizeInternal(ref char[] array, int newSize, int start, int count)
		{
			Debug.Assert(array != null && newSize > 0 && count >= 0 && newSize >= count && start >= 0);
			var result = newSize != array.Length ? new char[newSize] : array;
			Array.Copy(array, start, result, 0, count);
			array = result;
		}

		[Conditional("DEBUG")]
		void ClearInvalidChars()
		{
			Array.Clear(_buffer, 0, _start);
			Array.Clear(_buffer, _end, _buffer.Length - _end);
		}

		[Conditional("DEBUG")]
		void CheckInvariants()
		{
			Debug.Assert(_buffer.Length >= 1);
			// _start == _end when discarding token and at beginning, when == 0
			Debug.Assert(_start >= 0 && _start <= _end);
			Debug.Assert(_end >= 0 && _end <= _buffer.Length);
			// position beyond _end means we are reading EOFs:
			Debug.Assert(_position >= _start);
			Debug.Assert(_tokenEnd >= -1 && _tokenEnd <= _end);
		}

		[Conditional("DUMP_TOKENS")]
		void DumpToken() { Console.WriteLine("--> `{0}` {1}", GetTokenString().Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t"), TokenSpan); }
	}
}
