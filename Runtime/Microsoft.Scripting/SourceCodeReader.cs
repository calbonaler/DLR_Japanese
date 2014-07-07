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
using System.Text;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting
{
	/// <summary>�\�[�X�R�[�h��ǂݎ�� <see cref="TextReader"/> ��\���܂��B</summary>    
	public class SourceCodeReader : TextReader
	{
		/// <summary>�����ǂݎ��Ȃ� <see cref="SourceCodeReader"/> �������܂��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly new SourceCodeReader Null = new SourceCodeReader(TextReader.Null, null);

		/// <summary>�w�肳�ꂽ <see cref="TextReader"/> ����уG���R�[�f�B���O���g�p���āA<see cref="Microsoft.Scripting.SourceCodeReader"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="textReader">��ɂȂ� <see cref="TextReader"/> ���w�肵�܂��B</param>
		/// <param name="encoding">��ɂȂ�o�C�g�X�g���[������̃f�[�^�̓ǂݎ��Ɏg�p�����G���R�[�f�B���O���w�肵�܂��B��ɂȂ�f�[�^���e�L�X�g�̏ꍇ�� <c>null</c> ���w��ł��܂��B</param>
		public SourceCodeReader(TextReader textReader, Encoding encoding)
		{
			ContractUtils.RequiresNotNull(textReader, "textReader");
			Encoding = encoding;
			BaseReader = textReader;
		}

		/// <summary>
		/// ��ɂȂ�o�C�g�X�g���[������ǂݎ��ꂽ�f�[�^��ϊ����邽�߂Ƀ��[�_�[�ɂ���Ďg�p�����G���R�[�f�B���O���擾���܂��B
		/// ���[�_�[���e�L�X�g����ǂݎ���Ă��āA�f�R�[�h���s���Ă��Ȃ��ꍇ�� <c>null</c> �ɂȂ�܂��B
		/// </summary>
		public Encoding Encoding { get; private set; }

		/// <summary>��ɂȂ� <see cref="TextReader"/> ���擾���܂��B</summary>
		public TextReader BaseReader { get; private set; }

		/// <summary>�e�L�X�g ���[�_�[���� 1 �s���̕�����ǂݎ��A���̃f�[�^�𕶎���Ƃ��ĕԂ��܂��B</summary>
		/// <returns>���[�_�[�̎��̍s�B�܂��͂��ׂĂ̕������ǂݎ��ꂽ�ꍇ�� <c>null</c>�B</returns>
		public override string ReadLine() { return BaseReader.ReadLine(); }

		/// <summary>�e�L�X�g�X�g���[������w�肳�ꂽ�s�̍ŏ��̕������������܂��B</summary>
		/// <param name="line">�s�ԍ����w�肵�܂��B���݂̍s�ԍ��� 1 �Ɖ��肳��܂��B</param>
		/// <returns>�s�����������ꍇ�� <c>true</c>�A����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public virtual bool SeekLine(int line)
		{
			if (line < 1)
				throw new ArgumentOutOfRangeException("line");
			if (line == 1)
				return true;
			int current_line = 1;
			for (; ; )
			{
				int c = BaseReader.Read();
				if (c == '\r')
				{
					if (BaseReader.Peek() == '\n')
						BaseReader.Read();
					if (++current_line == line)
						return true;
				}
				else if (c == '\n' && ++current_line == line)
					return true;
				else if (c == -1)
					return false;
			}
		}

		/// <summary>�e�L�X�g ���[�_�[�̌��݈ʒu���疖���܂ł��ׂĂ̕�����ǂݎ��A1 �̕�����Ƃ��ĕԂ��܂��B</summary>
		/// <returns>�e�L�X�g ���[�_�[�̌��݈ʒu���疖���܂ł̂��ׂĂ̕������܂ޕ�����B</returns>
		public override string ReadToEnd() { return BaseReader.ReadToEnd(); }

		/// <summary>�w�肵���ő啶���������݂̃��[�_�[����ǂݎ��A�o�b�t�@�[�̎w�肵���C���f�b�N�X�ʒu�ɂ��̃f�[�^���������݂܂��B</summary>
		/// <param name="buffer">���̃��\�b�h���߂�Ƃ��A�w�肵�������z��� <paramref name="index"/> ���� (<paramref name="index"/> + <paramref name="count"/> - 1) �܂ł̒l���A���݂̃\�[�X����ǂݎ��ꂽ�����ɒu���������܂��B</param>
		/// <param name="index">�������݂��J�n���� <paramref name="buffer"/>���̈ʒu�B</param>
		/// <param name="count">�ǂݎ��Ώۂ̍ő啶�����B �w�肳�ꂽ���������o�b�t�@�[�ɓǂݎ��O�Ƀ��[�_�[�̖����ɓ��B�����ꍇ�A���\�b�h�͐����Ԃ��܂��B</param>
		/// <returns>
		/// �ǂݎ��ꂽ�������B
		/// ���̐��l�́A���[�_�[���Ɏg�p�ł���f�[�^�����邩�ǂ����ɂ���ĈقȂ�܂����A<paramref name="count"/> �ȉ��̐��l�ɂȂ�܂��B
		/// �ǂݎ��Ώۂ̕������Ȃ��ꍇ�ɂ��̃��\�b�h���Ăяo���ƁA0 (�[��) ���Ԃ���܂��B
		/// </returns>
		public override int Read(char[] buffer, int index, int count) { return BaseReader.Read(buffer, index, count); }

		/// <summary> �w�肵���ő啶���������݂̃e�L�X�g ���[�_�[����ǂݎ��A�o�b�t�@�[�̎w�肵���C���f�b�N�X�ʒu�ɂ��̃f�[�^���������݂܂��B</summary>
		/// <param name="buffer">���̃��\�b�h���߂�Ƃ��A�w�肵�������z��� <paramref name="index"/> ���� (<paramref name="index"/> + <paramref name="count"/> -1) �܂ł̒l���A���݂̃\�[�X����ǂݎ��ꂽ�����ɒu���������Ă��܂��B</param>
		/// <param name="index">�������݂��J�n���� <paramref name="buffer"/> ���̈ʒu�B</param>
		/// <param name="count">�ǂݎ��Ώۂ̍ő啶�����B</param>
		/// <returns>�ǂݎ��ꂽ�������B���̐��l�́A���ׂĂ̓��͕������ǂݎ��ꂽ���ǂ����ɂ���ĈقȂ�܂����A<paramref name="count"/> �ȉ��̐��l�ɂȂ�܂��B</returns>
		public override int ReadBlock(char[] buffer, int index, int count) { return BaseReader.ReadBlock(buffer, index, count); }

		/// <summary>���[�_�[�╶���̓ǂݎ�茳�̏�Ԃ�ύX�����ɁA���̕�����ǂݎ��܂��B ���[�_�[������ۂɕ�����ǂݎ�炸�Ɏ��̕�����Ԃ��܂��B</summary>
		/// <returns>�ǂݎ��Ώۂ̎��̕�����\�������B�g�p�ł��镶�����Ȃ����A���[�_�[���V�[�N���T�|�[�g���Ă��Ȃ��ꍇ�� -1�B</returns>
		public override int Peek() { return BaseReader.Peek(); }

		/// <summary> �e�L�X�g ���[�_�[���玟�̕�����ǂݎ��A1 ���������������ʒu��i�߂܂��B</summary>
		/// <returns>�e�L�X�g ���[�_�[����̎��̕����B����ȏ�ǂݎ��\�ȕ������Ȃ��ꍇ�� -1�B</returns>
		public override int Read() { return BaseReader.Read(); }

		/// <summary><see cref="Microsoft.Scripting.SourceCodeReader"/> �ɂ���Ďg�p����Ă���A���}�l�[�W ���\�[�X��������A�I�v�V�����Ń}�l�[�W ���\�[�X��������܂��B</summary>
		/// <param name="disposing">�}�l�[�W ���\�[�X�ƃA���}�l�[�W ���\�[�X�̗������������ꍇ�� <c>trie</c>�B�A���}�l�[�W ���\�[�X�������������ꍇ�� <c>false</c>�B</param>
		protected override void Dispose(bool disposing) { BaseReader.Dispose(); }
	}
}
