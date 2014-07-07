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
using System.Security.Permissions;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting
{
	/// <summary>�f�B�X�N��̃t�@�C���ɂ���ĕێ������X�g���[���R���e���c�ɑ΂��� <see cref="StreamContentProvider"/> ��񋟂��܂��B</summary>
	[Serializable]
	sealed class FileStreamContentProvider : StreamContentProvider
	{
		readonly PALHolder _pal;

		/// <summary>���̃R���e���c��ێ����Ă���t�@�C���̃p�X���擾���܂��B</summary>
		internal string Path { get; private set; }

		/// <summary><see cref="PlatformAdaptationLayer"/> ����ъ�ɂȂ�t�@�C���̃p�X���g�p���āA<see cref="Microsoft.Scripting.FileStreamContentProvider"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="pal">�t�@�C���̃I�[�v���Ɏg�p���� <see cref="PlatformAdaptationLayer"/> ���w�肵�܂��B</param>
		/// <param name="path">�ΏۂƂȂ�R���e���c��ێ����Ă���t�@�C���̃p�X���w�肵�܂��B</param>
		internal FileStreamContentProvider(PlatformAdaptationLayer pal, string path)
		{
			Assert.NotNull(pal, path);
			Path = path;
			_pal = new PALHolder(pal);
		}

		/// <summary><see cref="FileStreamContentProvider"/> ���쐬���ꂽ�R���e���c����ɂ���V���� <see cref="Stream"/> ���쐬���܂��B</summary>
		public override Stream GetStream() { return _pal.GetStream(Path); }

		[Serializable]
		class PALHolder : MarshalByRefObject
		{
			[NonSerialized]
			readonly PlatformAdaptationLayer _pal;

			internal PALHolder(PlatformAdaptationLayer pal) { _pal = pal; }

			internal Stream GetStream(string path) { return _pal.OpenInputFileStream(path); }

			// TODO: Figure out what is the right lifetime
			[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
			public override object InitializeLifetimeService() { return null; }
		}
	}
}
