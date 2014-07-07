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
using System.Diagnostics;
using System.IO;

namespace Microsoft.Scripting.Utils
{
	/// <summary>
	/// �R���\�[�����̓X�g���[�� (Console.OpenStandardInput) �ɂ͏��ʂ̃f�[�^��ǂݎ�����ۂɔ�������o�O������܂��B
	/// ���̃N���X�͕W�����̓X�g���[�����\���ȗʂ̃f�[�^���ǂݎ���邱�Ƃ�ۏ؂���o�b�t�@�Ń��b�v���܂��B
	/// </summary>
	public sealed class ConsoleInputStream : Stream
	{
		/// <summary>�W�����̓X�g���[���̗B��̃C���X�^���X���擾���܂��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly ConsoleInputStream Instance = new ConsoleInputStream();

		// ���S�̂��� 0x1000 ���g�p���܂��B (MSVCRT �͕W�����̓X�g���[���o�b�t�@�̂��߂ɂ��̒l���g�p���܂�)
		const int MinimalBufferSize = 0x1000;

		Stream _input;
		object _lock = new object();
		byte[] _buffer = new byte[MinimalBufferSize];
		int _bufferPos;
		int _bufferSize;

		ConsoleInputStream() { _input = Console.OpenStandardInput(); }

		/// <summary>���݂̃X�g���[�����ǂݎ����T�|�[�g���邩�ǂ����������l���擾���܂��B</summary>
		public override bool CanRead { get { return true; } }

		/// <summary>���݂̃X�g���[������o�C�g �V�[�P���X��ǂݎ��A�ǂݎ�����o�C�g���̕������X�g���[���̈ʒu��i�߂܂��B</summary>
		/// <param name="buffer">�o�C�g�z��B ���̃��\�b�h���߂�Ƃ��A�w�肵���o�C�g�z��� <paramref name="offset"/> ���� (<paramref name="offset"/> + <paramref name="count"/> -1) �܂ł̒l���A���݂̃\�[�X����ǂݎ��ꂽ�o�C�g�ɒu���������܂��B</param>
		/// <param name="offset">���݂̃X�g���[������ǂݎ�����f�[�^�̊i�[���J�n����ʒu������ <paramref name="buffer"/> ���̃o�C�g �I�t�Z�b�g�B�C���f�b�N�X�ԍ��� 0 ����n�܂�܂��B</param>
		/// <param name="count">���݂̃X�g���[������ǂݎ��ő�o�C�g���B</param>
		/// <returns>�o�b�t�@�[�ɓǂݎ��ꂽ���v�o�C�g���B �v�����������̃o�C�g����ǂݎ�邱�Ƃ��ł��Ȃ������ꍇ�A���̒l�͗v�������o�C�g����菬�����Ȃ�܂��B�X�g���[���̖����ɓ��B�����ꍇ�� 0 (�[��) �ɂȂ邱�Ƃ�����܂��B</returns>
		public override int Read(byte[] buffer, int offset, int count)
		{
			int result;
			lock (_lock)
			{
				if (_bufferSize > 0)
				{
					result = Math.Min(count, _bufferSize);
					Buffer.BlockCopy(_buffer, _bufferPos, buffer, offset, result);
					_bufferPos += result;
					_bufferSize -= result;
					offset += result;
					count -= result;
				}
				else
					result = 0;
				if (count > 0)
				{
					Debug.Assert(_bufferSize == 0);
					if (count < MinimalBufferSize)
					{
						int bytesRead = _input.Read(_buffer, 0, MinimalBufferSize);
						int bytesToReturn = Math.Min(bytesRead, count);
						Buffer.BlockCopy(_buffer, 0, buffer, offset, bytesToReturn);
						_bufferSize = bytesRead - bytesToReturn;
						_bufferPos = bytesToReturn;
						result += bytesToReturn;
					}
					else
						result += _input.Read(buffer, offset, count);
				}
			}
			return result;
		}

		/// <summary>���݂̃X�g���[�����V�[�N���T�|�[�g���邩�ǂ����������l���擾���܂��B</summary>
		public override bool CanSeek { get { return false; } }

		/// <summary>���݂̃X�g���[�����������݂��T�|�[�g���邩�ǂ����������l���擾���܂��B</summary>
		public override bool CanWrite { get { return false; } }

		/// <summary>�X�g���[���ɑΉ����邷�ׂẴo�b�t�@�[���N���A���A�o�b�t�@�[���̃f�[�^����ɂȂ�f�o�C�X�ɏ������݂܂��B</summary>
		public override void Flush() { throw new NotSupportedException(); }

		/// <summary>�X�g���[���̒������o�C�g�P�ʂŎ擾���܂��B</summary>
		public override long Length { get { throw new NotSupportedException(); } }

		/// <summary>���݂̃X�g���[�����̈ʒu���擾�܂��͐ݒ肵�܂��B</summary>
		public override long Position
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		/// <summary>���݂̃X�g���[�����̈ʒu��ݒ肵�܂��B</summary>
		/// <param name="offset"><paramref name="origin"/> �p�����[�^�[����̃o�C�g �I�t�Z�b�g�B</param>
		/// <param name="origin">�V�����ʒu���擾���邽�߂Ɏg�p����Q�ƃ|�C���g������ <see cref="System.IO.SeekOrigin"/> �^�̒l�B</param>
		/// <returns>���݂̃X�g���[�����̐V�����ʒu�B</returns>
		public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }

		/// <summary>���݂̃X�g���[���̒�����ݒ肵�܂��B</summary>
		/// <param name="value">���݂̃X�g���[���̊�]�̒��� (�o�C�g��)�B</param>
		public override void SetLength(long value) { throw new NotSupportedException(); }

		/// <summary>���݂̃X�g���[���Ƀo�C�g �V�[�P���X���������݁A�������񂾃o�C�g���̕������X�g���[���̌��݈ʒu��i�߂܂��B</summary>
		/// <param name="buffer">�o�C�g�z��B ���̃��\�b�h�́A<paramref name="buffer"/> ���猻�݂̃X�g���[���ɁA<paramref name="count"/> �Ŏw�肳�ꂽ�o�C�g�������R�s�[���܂��B</param>
		/// <param name="offset">���݂̃X�g���[���ւ̃o�C�g�̃R�s�[���J�n����ʒu������ <paramref name="buffer"/> ���̃o�C�g �I�t�Z�b�g�B�C���f�b�N�X�ԍ��� 0 ����n�܂�܂��B</param>
		/// <param name="count">���݂̃X�g���[���ɏ������ރo�C�g���B</param>
		public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
	}
}