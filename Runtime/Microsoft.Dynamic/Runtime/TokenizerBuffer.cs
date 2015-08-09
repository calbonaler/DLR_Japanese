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
	/// <summary>�g�[�N�i�C�U���g�p�ł���o�b�t�@��񋟂��܂��B</summary>
	public sealed class TokenizerBuffer
	{
		const int FirstColumn = 1;
		/// <summary>�t�@�C���̖����ɒB�������Ƃ�����������\���܂��B</summary>
		public const int EndOfFile = -1;
		/// <summary>�o�b�t�@���Ŗ����ȕ�����\���܂��B</summary>
		public const int InvalidCharacter = -2;

		/// <summary>
		/// �����`���̉��s������F�߂邩�ǂ����������܂��B
		/// <c>false</c> �̏ꍇ \n �݂̂����s�����Ƃ��Ĉ����܂��B
		/// ����ȊO�̏ꍇ�� \n, \r\n, \r �����s�����Ƃ��Ĉ����܂��B
		/// </summary>
		bool _multiEols;
		char[] _buffer;
		/// <summary>
		/// �o�b�t�@�����T�C�Y���ꂽ���ǂ����������܂��B
		/// �o�b�t�@�̓��e�͂��̊J�n�ʒu�ɃV�t�g����A���͂�g�p����Ă��Ȃ��o�b�t�@���̃f�[�^�͔j������܂��B
		/// </summary>
		bool _bufferResized;
		/// <summary>���ɓǂݎ���镶���������o�b�t�@���̈ʒu�������܂��B</summary>
		int _position;
		/// <summary>�g�[�N���̏I���ʒu�������܂��B����͌�Ōv�Z����܂��B</summary>
		SourceLocation _tokenEndLocation;
		/// <summary>�o�b�t�@���̗L���ȕ����̍ŏ��������C���f�b�N�X�������܂��B</summary>
		int _start;
		/// <summary>�o�b�t�@���̗L���ȕ����̍Ō�̒���̕����������C���f�b�N�X�������܂��B</summary>
		int _end;
		/// <summary>���݂̃g�[�N�����I�����镶���̎��̕����������C���f�b�N�X�������܂��B(�g�[�N���̊J�n�ʒu�� <see cref="_start"/> �ɂȂ�܂��B)</summary>
		int _tokenEnd;

		/// <summary>���̃o�b�t�@���f�[�^���擾���� <see cref="TextReader"/> ���擾���܂��B</summary>
		public TextReader Reader { get; private set; }

		/// <summary>���ɓǂݎ���镶������ɂȂ� <see cref="TextReader"/> �̍ŏ��̕����ł��邩�ǂ����������l���擾���܂��B</summary>
		public bool AtBeginning { get { return _position == 0 && !_bufferResized; } }

		/// <summary>���݂̃g�[�N���Ɋ܂܂�Ă��镶�������擾���܂��B</summary>
		public int TokenLength
		{
			get
			{
				if (_tokenEnd == -1)
					throw new InvalidOperationException("�g�[�N���̏I�[���L�^����Ă��܂���B");
				return _tokenEnd - _start;
			}
		}

		/// <summary>���݂̃g�[�N���̎��̓ǂ݂Ƃ��镶���ʒu�ɑ��ΓI�Ȉʒu���擾���܂��B</summary>
		public int TokenRelativePosition { get { CheckInvariants(); return _position - _start; } }

		/// <summary>���ɓǂݎ���镶���������o�b�t�@���̈ʒu���擾���܂��B</summary>
		public int Position { get { CheckInvariants(); return _position; } }

		/// <summary>���݂̃g�[�N���̃\�[�X�R�[�h���ł͈̔͂��擾���܂��B</summary>
		public SourceSpan TokenSpan { get { return new SourceSpan(TokenStart, TokenEnd); } }

		/// <summary>���݂̃g�[�N���̍ŏ��̕����ɑΉ�����ʒu���擾���܂��B</summary>
		public SourceLocation TokenStart { get; private set; }

		/// <summary>���݂̃g�[�N���̍Ō�̕����̎��̕����ɑΉ�����ʒu���擾���܂��B</summary>
		public SourceLocation TokenEnd
		{
			get
			{
				if (_tokenEnd == -1)
					throw new InvalidOperationException("�g�[�N���̏I�[���L�^����Ă��܂���B");
				return _tokenEndLocation;
			}
		}

		/// <summary>�w�肳�ꂽ�������g�p���āA<see cref="Microsoft.Scripting.Runtime.TokenizerBuffer"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="reader">�f�[�^�̎擾���� <see cref="TextReader"/> ���w�肵�܂��B</param>
		/// <param name="initialLocation"><see cref="TextReader"/> �̌��݂̈ʒu�ɑΉ����� <see cref="SourceLocation"/> ���w�肵�܂��B</param>
		/// <param name="initialCapacity">�o�b�t�@�̏����e�ʂ� <see cref="System.Char"/> �̌��Ŏw�肵�܂��B</param>
		/// <param name="multiEols">'\n' �ȊO�̉��s�L�������s�Ɣ��f���邩�ǂ����������l���w�肵�܂��B</param>
		public TokenizerBuffer(TextReader reader, SourceLocation initialLocation, int initialCapacity, bool multiEols) { Initialize(reader, initialLocation, initialCapacity, multiEols); }

		/// <summary>�w�肳�ꂽ�������g�p���āA���� <see cref="TokenizerBuffer"/> �����������܂��B</summary>
		/// <param name="reader">�f�[�^�̎擾���� <see cref="TextReader"/> ���w�肵�܂��B</param>
		/// <param name="initialLocation"><see cref="TextReader"/> �̌��݂̈ʒu�ɑΉ����� <see cref="SourceLocation"/> ���w�肵�܂��B</param>
		/// <param name="initialCapacity">�o�b�t�@�̏����e�ʂ� <see cref="System.Char"/> �̌��Ŏw�肵�܂��B</param>
		/// <param name="multiEols">'\n' �ȊO�̉��s�L�������s�Ɣ��f���邩�ǂ����������l���w�肵�܂��B</param>
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

		/// <summary>��ɂȂ� <see cref="TextReader"/> ���玟�̕�����ǂݎ��A�����ʒu�� 1 �����i�߂܂��B</summary>
		/// <returns>�ǂݎ��ꂽ������\�������l�B������\�� <see cref="EndOfFile"/> ���Ԃ����\��������܂��B</returns>
		public int Read()
		{
			var result = Peek();
			_position++;
			return result;
		}

		/// <summary>���̕������w�肳�ꂽ�����ł���΁A�����ʒu�� 1 �����i�߂܂��B</summary>
		/// <param name="expectedChar">�����ʒu��i�߂镶�����w�肵�܂��B���̒l�ɂ� <see cref="EndOfFile"/> �Ȃǂ��w�肷�邱�Ƃ��ł��܂��B</param>
		/// <returns>���̕������w�肳�ꂽ�����̏ꍇ�ɂ� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
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

		/// <summary>�o�b�t�@���̌��݂̕����ȍ~���w�肳�ꂽ������ɓ������ꍇ�͂��̕������������ʒu��i�߂܂��B</summary>
		/// <param name="expectedString">�����ʒu��i�߂镶������w�肵�܂��B</param>
		/// <returns>�o�b�t�@���̌��݂̕����ȍ~���w�肳�ꂽ������ɓ������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
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

		/// <summary>��ɂȂ� <see cref="TextReader"/> ���玟�̕�����ǂݎ��A�o�b�t�@�Ɋi�[���Ă���Ԃ��܂��B</summary>
		/// <returns>�������L���ȏꍇ�͂��̕����B����ȊO�̏ꍇ�� <see cref="EndOfFile"/> �Ȃǂ̕����l�B</returns>
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

		/// <summary>���ɓǂݎ�镶���ʒu�� 1 �������߂��܂��B</summary>
		public void Back() { SeekRelative(-1); }

		/// <summary>���ɓǂݎ�镶���ʒu���o�b�t�@�̐擪����̃I�t�Z�b�g���g�p���Đݒ肵�܂��B</summary>
		/// <param name="offset">�����ʒu��ݒ肷��o�b�t�@�̐擪����̃I�t�Z�b�g���w�肵�܂��B</param>
		public void Seek(int offset)
		{
			CheckInvariants();
			if (offset < 0)
				throw new ArgumentOutOfRangeException("offset");
			// no upper limit, we can seek beyond end in which case we are reading EOFs
			_position = _start + offset;
			CheckInvariants();
		}

		/// <summary>���ɓǂݎ�镶���ʒu�����݂̒l����̕ω��ʂŎw�肵�܂��B</summary>
		/// <param name="disp">���݂̐ݒ肳��Ă��鎟�ɓǂݎ���镶���ʒu����̐ݒ肷�镶���ʒu�̕ω��ʂ��w�肵�܂��B</param>
		public void SeekRelative(int disp)
		{
			CheckInvariants();
			if (disp < _start - _position)
				throw new ArgumentOutOfRangeException("disp");
			// no upper limit, we can seek beyond end in which case we are reading EOFs
			_position += disp;
			CheckInvariants();
		}

		/// <summary>�����s�g�[�N���̏I�����}�[�N���܂��B</summary>
		public void MarkMultiLineTokenEnd()
		{
			CheckInvariants();
			_tokenEnd = System.Math.Min(_position, _end);
			_tokenEndLocation = CalcTokenEnd();
			DumpToken();
			CheckInvariants();
		}

		/// <summary>
		/// �P��s�g�[�N���̏I�����}�[�N���܂��B
		/// �@�\�I�ɂ���� <see cref="MarkMultiLineTokenEnd()"/> �Ɠ����ł����A�P��s�Ɣ������Ă���ꍇ�͂�����̕��������Ȃ�܂��B
		/// </summary>
		public void MarkSingleLineTokenEnd()
		{
			CheckInvariants();
			_tokenEnd = System.Math.Min(_position, _end);
			_tokenEndLocation = new SourceLocation(TokenStart.Index + _tokenEnd - _start, TokenStart.Line, TokenStart.Column + _tokenEnd - _start);
			DumpToken();
			CheckInvariants();
		}

		/// <summary>�����s�g�[�N���̏I�����w�肳�ꂽ�l�������݈ʒu����ړ������ꏊ�Ń}�[�N���܂��B</summary>
		/// <param name="disp">�g�[�N���̏I�����}�[�N����ʒu�̌��݈ʒu����̕ω��ʂ��w�肵�܂��B</param>
		public void MarkMultiLineTokenEnd(int disp)
		{
			SeekRelative(disp);
			MarkMultiLineTokenEnd();
		}

		/// <summary>
		/// �P��s�g�[�N���̏I�����w�肳�ꂽ�l�������݈ʒu����ړ������ꏊ�Ń}�[�N���܂��B
		/// �@�\�I�ɂ���� <see cref="MarkMultiLineTokenEnd(int)"/> �Ɠ����ł����A�P��s�Ɣ������Ă���ꍇ�͂�����̕��������Ȃ�܂��B
		/// </summary>
		/// <param name="disp">�g�[�N���̏I�����}�[�N����ʒu�̌��݈ʒu����̕ω��ʂ��w�肵�܂��B</param>
		public void MarkSingleLineTokenEnd(int disp)
		{
			SeekRelative(disp);
			MarkSingleLineTokenEnd();
		}

		/// <summary>�g�[�N���������s�ł��邩�ǂ������w�肵�āA�g�[�N���̏I�����}�[�N���܂��B</summary>
		/// <param name="multiLine">�I�����}�[�N����g�[�N���������s�ɂ킽�邩�ǂ����������l���w�肵�܂��B</param>
		public void MarkTokenEnd(bool multiLine)
		{
			if (multiLine)
				MarkMultiLineTokenEnd();
			else
				MarkSingleLineTokenEnd();
		}

		/// <summary>
		/// �g�[�N���̊J�n���}�[�N���܂��B����ɂ���ăo�b�t�@�͌��݂̃g�[�N����j���ł���悤�ɂȂ�܂�
		/// ���̃��\�b�h�̓g�[�N�����ǂݎ���Ă��Ȃ��ꍇ���Ăяo�����Ƃ��ł��܂��B
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

		/// <summary>�o�b�t�@���̐擪����̃I�t�Z�b�g���w�肵�āA���̈ʒu�ɂ��镶�����擾���܂��B</summary>
		/// <param name="offset">�擾���镶���̈ʒu�������o�b�t�@���̐擪����̃I�t�Z�b�g���w�肵�܂��B</param>
		/// <returns>�o�b�t�@���̐擪����̃I�t�Z�b�g�������ʒu�ɂ��镶���B</returns>
		public char GetChar(int offset)
		{
			ContractUtils.RequiresArrayIndex(_end, offset, "offset");
			return _buffer[_start + offset];
		}

		/// <summary>���ɓǂݎ���镶���ʒu����̕ω��ʂ��w�肵�āA���̈ʒu�ɂ��镶�����擾���܂��B</summary>
		/// <param name="disp">�擾���镶���̈ʒu���������ɓǂݎ���镶���ʒu����̕ω��ʂ��w�肵�܂��B</param>
		/// <returns>�o�b�t�@���̎��ɓǂݎ���镶���ʒu����̎w��ʕω������ʒu�ɂ��镶���B</returns>
		public char GetCharRelative(int disp)
		{
			CheckInvariants();
			if (disp < _start - _position)
				throw new ArgumentOutOfRangeException("disp");
			return _buffer[_position + disp];
		}

		/// <summary>���݂̃g�[�N���������o�b�t�@���̕�������擾���܂��B</summary>
		/// <returns>�g�[�N���ɑΉ�����o�b�t�@���̕�����B</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public string GetTokenString()
		{
			CheckInvariants();
			if (_tokenEnd == -1)
				throw new InvalidOperationException("�g�[�N���̏I�[���L�^����Ă��܂���B");
			return new string(_buffer, _start, _tokenEnd - _start);
		}

		/// <summary>���݂̃g�[�N���������o�b�t�@���͈̔͂���w�肳�ꂽ�I�t�Z�b�g�ȍ~�͈̔͂ɂ��镶������擾���܂��B</summary>
		/// <param name="offset">�g�[�N�����̕���������̎擾�̊J�n�ʒu�������I�t�Z�b�g���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�I�t�Z�b�g����J�n����g�[�N���̏I���܂ł̕�����\��������B</returns>
		public string GetTokenSubstring(int offset) { return GetTokenSubstring(offset, _tokenEnd - _start - offset); }

		/// <summary>���݂̃g�[�N���������o�b�t�@���͈̔͂���w�肳�ꂽ�͈͂ɂ��镶������擾���܂��B</summary>
		/// <param name="offset">�g�[�N�����̕���������̎擾�̊J�n�ʒu�������I�t�Z�b�g���w�肵�܂��B</param>
		/// <param name="length">�g�[�N�����Ŏ擾���镔��������̕��������w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�I�t�Z�b�g����J�n����w�肳�ꂽ���������͈̔͂�\��������B</returns>
		public string GetTokenSubstring(int offset, int length)
		{
			CheckInvariants();
			if (_tokenEnd == -1)
				throw new InvalidOperationException("�g�[�N���̏I�[���L�^����Ă��܂���B");
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

		/// <summary>�w�肳�ꂽ���������s�����ł��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="current">���s�������ǂ����𔻒f���镶�����w�肵�܂��B</param>
		/// <returns>
		/// �w�肳�ꂽ���������s�����̏ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B
		/// �ǂ̕��������s�����Ƃ��Ĉ����邩�̓R���X�g���N�^���邢�� <see cref="Initialize"/> ���\�b�h�̈����Ɉˑ����܂��B
		/// </returns>
		public bool IsEol(int current) { return current == '\n' || _multiEols && current == '\r'; }

		/// <summary>�w�肳�ꂽ�����ɂ��������ĉ��s������ǂݍ��݂܂��B</summary>
		/// <param name="current">���̃o�b�t�@�̌��݂̕������w�肵�܂��B</param>
		/// <returns>�ǂݍ��܂ꂽ���s�����̕������B���s�����ɑ������Ȃ������ꍇ�� 0 ���Ԃ���܂��B</returns>
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

		/// <summary>���s�����������܂œǂݍ��݁A���s�����̒��O�̕�����Ԃ��܂��B�Ԃ��ꂽ�����̓X�L�b�v����܂���B</summary>
		/// <returns>���s����������钼�O�̕����B</returns>
		public int ReadLine()
		{
			int ch;
			do
				ch = Read();
			while (ch != EndOfFile && !IsEol(ch));
			Back();
			return ch;
		}

		/// <summary>�z��̃T�C�Y���w�肳�ꂽ�T�C�Y�ɕύX���āA���̔z��̕�����ύX���ꂽ�z��̍ŏ��ɃR�s�[���܂��B</summary>
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
