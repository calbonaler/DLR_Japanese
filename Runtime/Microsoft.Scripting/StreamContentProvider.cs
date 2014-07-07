/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Microsoft Public License. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Microsoft Public License, please send an email to 
 * ironruby@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
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
	/// �o�C�i���f�[�^�̒P��̃\�[�X����ɂ���X�g���[�����쐬����@�\��񋟂��܂��B
	/// ���̃N���X�͕s���ȃG���R�[�f�B���O�̃t�@�C�����J���ꍇ�Ɏg�p����܂��B
	/// </summary>
	/// <remarks>
	/// <see cref="StreamContentProvider"/> �̓o�C�i���f�[�^���e�L�X�g�ɕϊ�����ŗL�̕��@���T�|�[�g�ł��錾��ɂ���Ē񋟂����
	/// <see cref="TextContentProvider"/> �ɂ���ă��b�v����܂��B
	/// ���Ƃ��΁A�t�@�C���̐擪�ɔz�u����c��̕����̃G���R�[�f�B���O���w��ł���}�[�J�[��F�߂錾�������܂��B
	/// </remarks>
	[Serializable]
	public abstract class StreamContentProvider
	{
		/// <summary><see cref="StreamContentProvider"/> ���쐬���ꂽ�R���e���c����ɂ���V���� <see cref="Stream"/> ���쐬���܂��B</summary>
		/// <remarks>
		/// ���Ƃ��΁A<see cref="StreamContentProvider"/> ���t�@�C������ɂ��Ă���ꍇ�A<see cref="GetStream"/> �̓t�@�C����������x�J���V�����X�g���[����Ԃ��܂��B
		/// ���̃��\�b�h�͕�����Ăяo�����\��������܂��B
		/// ���Ƃ��΁A1 ��ڂ̓R�[�h���R���p�C�����邽�߁A2 ��ڂ̓G���[���b�Z�[�W��\�����邽�߂Ƀ\�[�X�R�[�h���擾���邽�߁A�Ȃǂł��B
		/// </remarks>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public abstract Stream GetStream();
	}
}
