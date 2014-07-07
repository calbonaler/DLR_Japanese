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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting.Shell
{
	/// <summary>�^�u�⊮�Ȃǂ̍��x�ȋ@�\��������R���\�[����\���܂��B</summary>
	public sealed class SuperConsole : BasicConsole
	{
		/// <summary>�R�}���h�������Ǘ�����N���X�ł��B</summary>
		class History
		{
			protected List<string> _list = new List<string>();
			int _current;
			bool _increment;         // increment on Next()

			public string Current { get { return _current >= 0 && _current < _list.Count ? _list[_current] : string.Empty; } }

			public void Add(string line, bool setCurrentAsLast)
			{
				if (line != null && line.Length > 0)
				{
					var oldCount = _list.Count;
					_list.Add(line);
					if (setCurrentAsLast || _current == oldCount)
						_current = _list.Count;
					else
						_current++;
					// Do not increment on the immediately following Next()
					_increment = false;
				}
			}

			public string Previous()
			{
				if (_current > 0)
				{
					_current--;
					_increment = true;
				}
				return Current;
			}

			public string Next()
			{
				if (_current + 1 < _list.Count)
				{
					if (_increment)
						_current++;
					_increment = true;
				}
				return Current;
			}
		}

		/// <summary>���p�\�ȃI�v�V�����̃��X�g��\���܂��B</summary>
		class SuperConsoleOptions
		{
			List<string> _list = new List<string>();
			int _current;

			public int Count { get { return _list.Count; } }

			string Current { get { return _current >= 0 && _current < _list.Count ? _list[_current] : string.Empty; } }

			public void Clear()
			{
				_list.Clear();
				_current = -1;
			}

			public void Add(string line)
			{
				if (line != null && line.Length > 0)
					_list.Add(line);
			}

			public string Previous()
			{
				if (_list.Count > 0)
					_current = (_current - 1 + _list.Count) % _list.Count;
				return Current;
			}

			public string Next()
			{
				if (_list.Count > 0)
					_current = (_current + 1) % _list.Count;
				return Current;
			}

			public string Root { get; set; }
		}

		/// <summary>�J�[�\���ʒu���Ǘ����܂��B</summary>
		struct Cursor
		{
			/// <summary>�J�[�\���̊J�n�ʒu�� Y ���W��\���܂��B</summary>
			int _anchorTop;
			/// <summary>�J�[�\���̊J�n�ʒu�� X ���W��\���܂��B</summary>
			int _anchorLeft;

			public void Anchor()
			{
				_anchorTop = Console.CursorTop;
				_anchorLeft = Console.CursorLeft;
			}

			public void Reset()
			{
				Console.CursorTop = _anchorTop;
				Console.CursorLeft = _anchorLeft;
			}

			public void Place(int index)
			{
				Console.CursorLeft = (_anchorLeft + index) % Console.BufferWidth;
				int cursorTop = _anchorTop + (_anchorLeft + index) / Console.BufferWidth;
				if (cursorTop >= Console.BufferHeight)
				{
					_anchorTop -= cursorTop - Console.BufferHeight + 1;
					cursorTop = Console.BufferHeight - 1;
				}
				Console.CursorTop = cursorTop;
			}

			public static void Move(int delta)
			{
				int position = Console.CursorTop * Console.BufferWidth + Console.CursorLeft + delta;
				Console.CursorLeft = position % Console.BufferWidth;
				Console.CursorTop = position / Console.BufferWidth;
			}
		}

		/// <summary>�R���\�[���̓��̓o�b�t�@��\���܂��B</summary>
		StringBuilder _input = new StringBuilder();
		/// <summary>���݂̈ʒu (���̓o�b�t�@�ւ̃C���f�b�N�X) ��\���܂��B</summary>
		int _current;
		/// <summary>���݂̍s�Ŏ����C���f���g�̂��߂ɕ\�������󔒂̌���\���܂��B</summary>
		int _autoIndentSize;
		/// <summary>���݃X�N���[���Ƀ����_�����O���ꂽ�o�͂̒�����\���܂��B</summary>
		int _rendered;
		/// <summary>�R�}���h������\���܂��B</summary>
		History _history = new History();
		/// <summary>���݂̕����ŗ��p�\�ȃ^�u�I�v�V������\���܂��B</summary>
		SuperConsoleOptions _options = new SuperConsoleOptions();
		/// <summary>�J�[�\�� �A���J�[ (���[�`�����Ă΂ꂽ���̃J�[�\���̈ʒu) ��\���܂��B</summary>
		Cursor _cursor;
		/// <summary>���̃R���\�[�����A�^�b�`����Ă���R�}���h���C����\���܂��B</summary>
		CommandLine _commandLine;

		/// <summary>�A�^�b�`����R�}���h���C���ƃR���\�[�����F���ł��邩�ǂ����������l���g�p���āA<see cref="Microsoft.Scripting.Hosting.Shell.SuperConsole"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="commandLine">���̃R���\�[���ɃA�^�b�`����R�}���h���C�����w�肵�܂��B</param>
		/// <param name="colorful">���̃R���\�[���ɐF�����邩�ǂ����������l���w�肵�܂��B</param>
		public SuperConsole(CommandLine commandLine, bool colorful) : base(colorful)
		{
			ContractUtils.RequiresNotNull(commandLine, "commandLine");
			_commandLine = commandLine;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		bool GetOptions()
		{
			_options.Clear();
			int len = _input.Length;
			while (len > 0 && (char.IsLetterOrDigit(_input[len - 1]) || _input[len - 1] == '.' || _input[len - 1] == '_'))
				len--;
			string name = _input.ToString(len, _input.Length - len);
			if (name.Trim().Length > 0)
			{
				int lastDot = name.LastIndexOf('.');
				string attr, pref;
				if (lastDot < 0)
				{
					attr = string.Empty;
					pref = name;
					_options.Root = _input.ToString(0, len);
				}
				else
				{
					attr = name.Substring(0, lastDot);
					pref = name.Substring(lastDot + 1);
					_options.Root = _input.ToString(0, len + lastDot + 1);
				}
				try
				{
					foreach (string option in (string.IsNullOrEmpty(attr) ? _commandLine.GetGlobals(name) : _commandLine.GetMemberNames(attr)).Where(x => x.StartsWith(pref, StringComparison.CurrentCultureIgnoreCase)))
						_options.Add(option);
				}
				catch { _options.Clear(); }
				return true;
			}
			else
				return false;
		}

		void SetInput(string line)
		{
			_input.Length = 0;
			_input.Append(line);
			_current = _input.Length;
			Render();
		}

		void Initialize()
		{
			_cursor.Anchor();
			_input.Length = 0;
			_current = 0;
			_rendered = 0;
		}

		// Check if the user is backspacing the auto-indentation. In that case, we go back all the way to
		// the previous indentation level.
		// Return true if we did backspace the auto-indenation.
		bool BackspaceAutoIndentation()
		{
			if (_input.Length == 0 || _input.Length > _autoIndentSize)
				return false;
			// Is the auto-indenation all white space, or has the user since edited the auto-indentation?
			for (int i = 0; i < _input.Length; i++)
			{
				if (_input[i] != ' ')
					return false;
			}
			// Calculate the previous indentation level
			//!!! int newLength = ((input.Length - 1) / ConsoleOptions.AutoIndentSize) * ConsoleOptions.AutoIndentSize;            
			var newLength = _input.Length - 4;
			var backspaceSize = _input.Length - newLength;
			_input.Remove(newLength, backspaceSize);
			_current -= backspaceSize;
			Render();
			return true;
		}

		void OnBackspace()
		{
			if (BackspaceAutoIndentation())
				return;
			if (_input.Length > 0 && _current > 0)
			{
				_input.Remove(_current - 1, 1);
				_current--;
				Render();
			}
		}

		void OnDelete()
		{
			if (_input.Length > 0 && _current < _input.Length)
			{
				_input.Remove(_current, 1);
				Render();
			}
		}

		void Insert(ConsoleKeyInfo key)
		{
			char c;
			if (key.Key == ConsoleKey.F6)
			{
				Debug.Assert(FinalLineText.Length == 1);
				c = FinalLineText[0];
			}
			else
				c = key.KeyChar;
			Insert(c);
		}

		void Insert(char c)
		{
			if (_current == _input.Length)
			{
				if (Char.IsControl(c))
				{
					var s = MapCharacter(c);
					_current++;
					_input.Append(c);
					Output.Write(s);
					_rendered += s.Length;
				}
				else
				{
					_current++;
					_input.Append(c);
					Output.Write(c);
					_rendered++;
				}
			}
			else
			{
				_input.Insert(_current, c);
				_current++;
				Render();
			}
		}

		static string MapCharacter(char c)
		{
			if (c == 13)
				return "\r\n";
			if (c <= 26)
				return "^" + ((char)(c + 'A' - 1)).ToString();
			return "^?";
		}

		static int GetCharacterSize(char c) { return char.IsControl(c) ? MapCharacter(c).Length : 1; }

		void Render()
		{
			_cursor.Reset();
			StringBuilder output = new StringBuilder();
			int position = -1;
			for (int i = 0; i < _input.Length; i++)
			{
				if (i == _current)
					position = output.Length;
				if (char.IsControl(_input[i]))
					output.Append(MapCharacter(_input[i]));
				else
					output.Append(_input[i]);
			}
			if (_current == _input.Length)
				position = output.Length;
			var text = output.ToString();
			Output.Write(text);
			if (text.Length < _rendered)
				Output.Write(new string(' ', _rendered - text.Length));
			_rendered = text.Length;
			_cursor.Place(position);
		}

		void MoveLeft(ConsoleModifiers keyModifiers)
		{
			if ((keyModifiers & ConsoleModifiers.Control) == 0)
				MoveLeft();
			else if (_input.Length > 0 && _current != 0)
			{ // move back to the start of the previous word
				bool nonLetter = IsSeperator(_input[_current - 1]);
				while (_current > 0 && (_current - 1 < _input.Length))
				{
					MoveLeft();
					if (IsSeperator(_input[_current]) != nonLetter)
					{
						if (!nonLetter)
						{
							MoveRight();
							break;
						}
						nonLetter = false;
					}
				}
			}
		}

		static bool IsSeperator(char ch) { return !char.IsLetter(ch); }

		void MoveRight(ConsoleModifiers keyModifiers)
		{
			if ((keyModifiers & ConsoleModifiers.Control) == 0)
				MoveRight();
			else if (_input.Length != 0 && _current < _input.Length)
			{ // move to the next word
				bool nonLetter = IsSeperator(_input[_current]);
				while (_current < _input.Length)
				{
					MoveRight();
					if (_current == _input.Length)
						break;
					if (IsSeperator(_input[_current]) != nonLetter)
					{
						if (nonLetter)
							break;
						nonLetter = true;
					}
				}
			}
		}

		void MoveRight()
		{
			if (_current < _input.Length)
				Cursor.Move(GetCharacterSize(_input[_current++]));
		}

		void MoveLeft()
		{
			if (_current > 0 && _current - 1 < _input.Length)
				Cursor.Move(-GetCharacterSize(_input[--_current]));
		}

		const int TabSize = 4;

		void InsertTab()
		{
			for (int i = TabSize - _current % TabSize; i > 0; i--)
				Insert(' ');
		}

		void MoveHome()
		{
			_current = 0;
			_cursor.Reset();
		}

		void MoveEnd()
		{
			_current = _input.Length;
			_cursor.Place(_rendered);
		}

		/// <summary>�w�肳�ꂽ�C���f���g���ŃR���\�[������ 1 �s��ǂݎ��܂��B</summary>
		/// <param name="autoIndentSize">�s�̍��[�ɑ}�������C���f���g�̕����w�肵�܂��B</param>
		/// <returns>�ǂݎ��ꂽ������B������ɂ͎����C���f���g�̕����܂܂�܂��B</returns>
		public override string ReadLine(int autoIndentSize)
		{
			Initialize();
			_autoIndentSize = autoIndentSize;
			for (int i = 0; i < _autoIndentSize; i++)
				Insert(' ');
			bool inputChanged = false;
			bool optionsObsolete = false;
			for (; ; )
			{
				var key = Console.ReadKey(true);
				switch (key.Key)
				{
					case ConsoleKey.Backspace:
						OnBackspace();
						inputChanged = optionsObsolete = true;
						break;
					case ConsoleKey.Delete:
						OnDelete();
						inputChanged = optionsObsolete = true;
						break;
					case ConsoleKey.Enter:
						return OnEnter(inputChanged);
					case ConsoleKey.Tab:
						{
							bool prefix = false;
							if (optionsObsolete)
							{
								prefix = GetOptions();
								optionsObsolete = false;
							}
							// Displays the next option in the option list, or beeps if no options available for current input prefix.
							// If no input prefix, simply print tab.
							DisplayNextOption(key, prefix);
							inputChanged = true;
							break;
						}
					case ConsoleKey.UpArrow:
						SetInput(_history.Previous());
						optionsObsolete = true;
						inputChanged = false;
						break;
					case ConsoleKey.DownArrow:
						SetInput(_history.Next());
						optionsObsolete = true;
						inputChanged = false;
						break;
					case ConsoleKey.RightArrow:
						MoveRight(key.Modifiers);
						optionsObsolete = true;
						break;
					case ConsoleKey.LeftArrow:
						MoveLeft(key.Modifiers);
						optionsObsolete = true;
						break;
					case ConsoleKey.Escape:
						SetInput(String.Empty);
						inputChanged = optionsObsolete = true;
						break;
					case ConsoleKey.Home:
						MoveHome();
						optionsObsolete = true;
						break;
					case ConsoleKey.End:
						MoveEnd();
						optionsObsolete = true;
						break;
					case ConsoleKey.LeftWindows:
					case ConsoleKey.RightWindows:
						continue; // ignore these
					default:
						if (key.KeyChar == '\x0D')
							goto case ConsoleKey.Enter;      // Ctrl-M
						if (key.KeyChar == '\x08')
							goto case ConsoleKey.Backspace;  // Ctrl-H
						Insert(key);
						inputChanged = optionsObsolete = true;
						break;
				}
			}
		}

		/// <summary>
		/// �I�v�V�������X�g�̎��̃I�v�V������\�����邩�A���݂̓��̓v���t�B�b�N�X�ŗ��p�\�ȃI�v�V�������Ȃ��ꍇ�̓r�[�v��炵�܂��B
		/// ���̓v���t�B�b�N�X���Ȃ��ꍇ�͒P���Ƀ^�u���o�͂��܂��B
		/// </summary>
		void DisplayNextOption(ConsoleKeyInfo key, bool prefix)
		{
			if (_options.Count > 0)
				SetInput(_options.Root + ((key.Modifiers & ConsoleModifiers.Shift) != 0 ? _options.Previous() : _options.Next()));
			else
			{
				if (prefix)
					Console.Beep();
				else
					InsertTab();
			}
		}

		/// <summary>Enter �L�[���n���h�����܂��B���݂̓��͂���łȂ��ꍇ�͗����ɒǉ����܂��B</summary>
		/// <returns>���͕�����B</returns>
		string OnEnter(bool inputChanged)
		{
			Output.Write("\n");
			var line = _input.ToString();
			if (line == FinalLineText)
				return null;
			if (line.Length > 0)
				_history.Add(line, inputChanged);
			return line;
		}

		string FinalLineText { get { return Environment.OSVersion.Platform != PlatformID.Unix ? "\x1A" : "\x04"; } }
	}
}
