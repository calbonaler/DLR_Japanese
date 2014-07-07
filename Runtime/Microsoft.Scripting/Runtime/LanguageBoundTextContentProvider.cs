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

using System.Text;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>
	/// �o�C�i���f�[�^��K�؂Ȍ���Z�}���e�B�N�X�œǂݎ�� <see cref="TextContentProvider"/> ���쐬���邽�߂�
	/// <see cref="LanguageContext"/>�A<see cref="StreamContentProvider"/> ����� <see cref="Encoding"/> ���o�C���h���܂��B
	/// </summary>
	sealed class LanguageBoundTextContentProvider : TextContentProvider
	{
		LanguageContext _context;
		StreamContentProvider _streamProvider;
		Encoding _defaultEncoding;
		string _path;

		/// <summary>
		/// �w�肳�ꂽ <see cref="LanguageContext"/>�A<see cref="StreamContentProvider"/>�A<see cref="Encoding"/> ����уt�@�C���p�X���g�p���āA
		/// <see cref="Microsoft.Scripting.Runtime.LanguageBoundTextContentProvider"/> �N���X�̐V�����C���X�^���X�����������܂��B
		/// </summary>
		/// <param name="context">�����\�� <see cref="LanguageContext"/> ���w�肵�܂��B</param>
		/// <param name="streamProvider">��ɂȂ�f�[�^��񋟂ł��� <see cref="StreamContentProvider"/> ���w�肵�܂��B</param>
		/// <param name="defaultEncoding">����̃G���R�[�f�B���O���w�肵�܂��B</param>
		/// <param name="path">�\�[�X�R�[�h�̃t�@�C���p�X���w�肵�܂��B</param>
		public LanguageBoundTextContentProvider(LanguageContext context, StreamContentProvider streamProvider, Encoding defaultEncoding, string path)
		{
			Assert.NotNull(context, streamProvider, defaultEncoding);
			_context = context;
			_streamProvider = streamProvider;
			_defaultEncoding = defaultEncoding;
			_path = path;
		}

		/// <summary><see cref="TextContentProvider"/> ���쐬���ꂽ�R���e���c����ɂ���V���� <see cref="System.IO.TextReader"/> ���쐬���܂��B</summary>
		public override SourceCodeReader GetReader() { return _context.GetSourceReader(_streamProvider.GetStream(), _defaultEncoding, _path); }
	}
}
