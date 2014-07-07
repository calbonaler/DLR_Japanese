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
	/// <summary>�e�L�X�g���ǉ����ꂽ�ꍇ�Ɍ����I�� (�s, ��) �̏���ǐՂ��A���̃\�[�X�R�[�h�̐������ꂽ�\�[�X�R�[�h�̊Ԃ̍s�}�b�s���O�����W���邱�ƂŁA�������f�o�b�O���𐶐��ł���悤�ɂ��܂��B</summary>
	public class PositionTrackingWriter : StringWriter
	{
		List<KeyValuePair<int, int>> _lineMap = new List<KeyValuePair<int, int>>();
		List<KeyValuePair<int, string>> _fileMap = new List<KeyValuePair<int, string>>();

		int _line = 1;
		int _column = 1;

		/// <summary><see cref="Microsoft.Scripting.Runtime.PositionTrackingWriter"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		public PositionTrackingWriter() { }

		/// <summary>���C�^�[�̌��݂̈ʒu���w�肳�ꂽ���̃\�[�X�R�[�h�̈ʒu�ƑΉ�����ƃ}�[�N���܂��B</summary>
		/// <param name="linePragma">�������ꂽ�R�[�h�Ō��݂̈ʒu�ƑΉ�����s�v���O�}���w�肵�܂��B</param>
		public void MapLocation(CodeLinePragma linePragma)
		{
			_lineMap.Add(new KeyValuePair<int, int>(_line, linePragma.LineNumber));
			_fileMap.Add(new KeyValuePair<int, string>(_line, linePragma.FileName));
		}

		/// <summary>���̃��C�^�[�̐������ꂽ�\�[�X�R�[�h���猳�̃\�[�X�R�[�h�ւ̍s�}�b�s���O���擾���܂��B</summary>
		/// <returns>�擾���ꂽ�s�}�b�s���O�B</returns>
		public KeyValuePair<int, int>[] GetLineMap() { return _lineMap.ToArray(); }

		/// <summary>���̃��C�^�[�̐������ꂽ�\�[�X�R�[�h�̍s�ԍ����猳�̃\�[�X�R�[�h�̃t�@�C�����ւ̃}�b�s���O���擾���܂��B</summary>
		/// <returns>�s�ԍ�����t�@�C�����ւ̃}�b�s���O�B</returns>
		public KeyValuePair<int, string>[] GetFileMap() { return _fileMap.ToArray(); }

		/// <summary>������ɕ������������݂܂��B</summary>
		/// <param name="value">�������ޕ����B</param>
		/// <exception cref="ObjectDisposedException">���C�^�[�������܂����B</exception>
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

		/// <summary>���݂̕�����ɕ�������������݂܂��B</summary>
		/// <param name="value">�������ޕ�����B</param>
		/// <exception cref="ObjectDisposedException">���C�^�[�������܂����B</exception>
		public override void Write(string value)
		{
			UpdateLineColumn(0, value.Length, (a, b, c) => value.IndexOf(a, b, c));
			base.Write(value);
		}

		/// <summary>�����z��̈ꕔ�𕶎���ɏ������݂܂��B</summary>
		/// <param name="buffer">�f�[�^�̏������݌��̕����z��B</param>
		/// <param name="index">�f�[�^�̓ǂݎ����J�n����A�o�b�t�@�[���̈ʒu�B</param>
		/// <param name="count">�������ޕ����̍ő吔�B</param>
		/// <exception cref="ArgumentNullException"><paramref name="buffer"/> �� <c>null</c> �ł��B</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> �܂��� <paramref name="count"/> �����̒l�ł��B</exception>
		/// <exception cref="ArgumentException">(<paramref name="index"/> + <paramref name="count"/>) �̒l�� <paramref name="buffer"/>.Length �����傫�Ȓl�ł��B</exception>
		/// <exception cref="ObjectDisposedException">���C�^�[�������܂����B</exception>
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