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

namespace Microsoft.Scripting
{
	/// <summary>���Ƀt�@�C�������ӌ��ꎯ�ʎq�Ȃǃ\�[�X�t�@�C���ɑ΂���f�o�b�O���̏o�͎��ɕK�v�ɂȂ�����i�[���܂��B</summary>
	public sealed class SourceFileInformation
	{
		/// <summary>�t�@�C�������g�p���āA<see cref="Microsoft.Scripting.SourceFileInformation"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="fileName">�\�[�X�t�@�C���̃t�@�C�������w�肵�܂��B</param>
		public SourceFileInformation(string fileName) { FileName = fileName; }

		/// <summary>�t�@�C��������ь��ꎯ�ʎq���g�p���āA<see cref="Microsoft.Scripting.SourceFileInformation"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="fileName">�\�[�X�t�@�C���̃t�@�C�������w�肵�܂��B</param>
		/// <param name="language">�\�[�X�t�@�C���������ꂽ��������ʂ���O���[�o����ӎ��ʎq���w�肵�܂��B</param>
		public SourceFileInformation(string fileName, Guid language)
		{
			FileName = fileName;
			LanguageGuid = language;
		}

		/// <summary>�t�@�C�����A���ꎯ�ʎq����уx���_�[���ʎq���g�p���āA<see cref="Microsoft.Scripting.SourceFileInformation"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="fileName">�\�[�X�t�@�C���̃t�@�C�������w�肵�܂��B</param>
		/// <param name="language">�\�[�X�t�@�C���������ꂽ��������ʂ���O���[�o����ӎ��ʎq���w�肵�܂��B</param>
		/// <param name="vendor">�\�[�X�t�@�C���������ꂽ����̃x���_�[�����ʂ���O���[�o����ӎ��ʎq���w�肵�܂��B</param>
		public SourceFileInformation(string fileName, Guid language, Guid vendor)
		{
			FileName = fileName;
			LanguageGuid = language;
			VendorGuid = vendor;
		}

		/// <summary>�\�[�X�t�@�C���̃t�@�C�������擾���܂��B</summary>
		public string FileName { get; private set; }

		// TODO: save storage space if these are not supplied?

		/// <summary>�\�[�X�t�@�C���������ꂽ��������ʂ���O���[�o����ӎ��ʎq���擾���܂��B</summary>
		public Guid LanguageGuid { get; private set; }

		/// <summary>�\�[�X�t�@�C���������ꂽ����̃x���_�[�����ʂ���O���[�o����ӎ��ʎq���擾���܂��B</summary>
		public Guid VendorGuid { get; private set; }
	}
}
