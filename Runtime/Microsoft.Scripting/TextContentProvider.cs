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

namespace Microsoft.Scripting
{
	/// <summary>
	/// �e�L�X�g�f�[�^�̒P��̃\�[�X����ɂ��� <see cref="TextReader"/> ���쐬����@�\��񋟂��܂��B
	/// ���̃N���X�͂��łɃf�R�[�h����Ă��邩�A���m�̓���̃G���R�[�f�B���O�ł���\�[�X�̓ǂݎ��Ɏg�p����܂��B
	/// </summary>
	/// <remarks>
	/// ���Ƃ��΁A�e�L�X�g�G�f�B�^�͊�ɂȂ�f�[�^�����[�U�[�����ڕҏW���郁�������̃e�L�X�g�o�b�t�@�ł��� <see cref="TextContentProvider"/> ��񋟂��邩������܂���B
	/// </remarks>
	[Serializable]
	public abstract class TextContentProvider
	{
		/// <summary>�f�[�^��񋟂��Ȃ� <see cref="TextContentProvider"/> �������܂��B</summary>
		public static readonly TextContentProvider Null = new NullTextContentProvider();

		/// <summary><see cref="TextContentProvider"/> ���쐬���ꂽ�R���e���c����ɂ���V���� <see cref="TextReader"/> ���쐬���܂��B</summary>
		/// <remarks>
		/// ���̃��\�b�h�͕�����Ăяo�����\��������܂��B
		/// ���Ƃ��΁A1 ��ڂ̓R�[�h���R���p�C�����邽�߁A2 ��ڂ̓G���[���b�Z�[�W��\�����邽�߂Ƀ\�[�X�R�[�h���擾���邽�߁A�Ȃǂł��B
		/// </remarks>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public abstract SourceCodeReader GetReader();

		sealed class NullTextContentProvider : TextContentProvider
		{
			internal NullTextContentProvider() { }

			public override SourceCodeReader GetReader() { return SourceCodeReader.Null; }
		}
	}
}
