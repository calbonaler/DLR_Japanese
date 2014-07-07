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
	/// �R���\�[���̓��o�͂𐧌䂵�܂��B
	/// ���̃C���^�[�t�F�C�X�� <see cref="System.IO.TextReader"/>�A<see cref="System.IO.TextWriter"/>�A<see cref="System.Console"/> �Ȃǂɒl���܂��B
	/// </summary>
	public interface IConsole
	{
		/// <summary>
		/// �P��s�̑Θb���͂��邢�͕����s�̃X�e�[�g�����g�Z�b�g�̃u���b�N��ǂݎ��܂��B
		/// �C�x���g�쓮�^�� GUI �R���\�[���ł͓��͂����p�\�ł��邱�Ƃ������C�x���g���u���b�N����ёҋ@����X���b�h���쐬���邱�ƂŁA���̃��\�b�h���������邱�Ƃ��ł��܂��B
		/// </summary>
		/// <param name="autoIndentSize">
		/// ���݂̃X�e�[�g�����g�Z�b�g�Ɏg�p�����C���f���g���x�����w�肵�܂��B
		/// �R���\�[���͎����C���f���g���T�|�[�g���Ȃ��ꍇ�A���̈����𖳎����邱�Ƃ��ł��܂��B
		/// </param>
		/// <returns>
		/// ���̓X�g���[�������Ă����ꍇ�� <c>null</c>�B����ȊO�̏ꍇ�͎��s����R�}���h��\��������B
		/// ���ʂ̓X�e�[�g�����g�̃u���b�N�Ƃ��ď��������悤�ȕ����s�̕�����Ȃ邱�Ƃ�����܂��B
		/// </returns>
		string ReadLine(int autoIndentSize);

		/// <summary>�w�肳�ꂽ��������w�肳�ꂽ�X�^�C���ŃR���\�[���ɏo�͂��܂��B</summary>
		/// <param name="text">�o�͂��镶������w�肵�܂��B</param>
		/// <param name="style">��������o�͂���X�^�C�����w�肵�܂��B</param>
		void Write(string text, Style style);

		/// <summary>�w�肳�ꂽ��������w�肳�ꂽ�X�^�C���ŃR���\�[���ɏo�͂��A�����ĉ��s�������o�͂��܂��B</summary>
		/// <param name="text">�o�͂��镶������w�肵�܂��B</param>
		/// <param name="style">��������o�͂���X�^�C�����w�肵�܂��B</param>
		void WriteLine(string text, Style style);

		/// <summary>�R���\�[���ɉ��s�������o�͂��܂��B</summary>
		void WriteLine();

		/// <summary>�R���\�[���̕W���o�͂�\�� <see cref="TextWriter"/> ���擾�܂��͐ݒ肵�܂��B</summary>
		TextWriter Output { get; set; }

		/// <summary>�R���\�[���̕W���G���[�o�͂�\�� <see cref="TextWriter"/> ���擾�܂��͐ݒ肵�܂��B</summary>
		TextWriter ErrorOutput { get; set; }
	}
}
