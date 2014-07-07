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
	/// バイナリデータを適切な言語セマンティクスで読み取る <see cref="TextContentProvider"/> を作成するために
	/// <see cref="LanguageContext"/>、<see cref="StreamContentProvider"/> および <see cref="Encoding"/> をバインドします。
	/// </summary>
	sealed class LanguageBoundTextContentProvider : TextContentProvider
	{
		LanguageContext _context;
		StreamContentProvider _streamProvider;
		Encoding _defaultEncoding;
		string _path;

		/// <summary>
		/// 指定された <see cref="LanguageContext"/>、<see cref="StreamContentProvider"/>、<see cref="Encoding"/> およびファイルパスを使用して、
		/// <see cref="Microsoft.Scripting.Runtime.LanguageBoundTextContentProvider"/> クラスの新しいインスタンスを初期化します。
		/// </summary>
		/// <param name="context">言語を表す <see cref="LanguageContext"/> を指定します。</param>
		/// <param name="streamProvider">基になるデータを提供できる <see cref="StreamContentProvider"/> を指定します。</param>
		/// <param name="defaultEncoding">既定のエンコーディングを指定します。</param>
		/// <param name="path">ソースコードのファイルパスを指定します。</param>
		public LanguageBoundTextContentProvider(LanguageContext context, StreamContentProvider streamProvider, Encoding defaultEncoding, string path)
		{
			Assert.NotNull(context, streamProvider, defaultEncoding);
			_context = context;
			_streamProvider = streamProvider;
			_defaultEncoding = defaultEncoding;
			_path = path;
		}

		/// <summary><see cref="TextContentProvider"/> が作成されたコンテンツを基にする新しい <see cref="System.IO.TextReader"/> を作成します。</summary>
		public override SourceCodeReader GetReader() { return _context.GetSourceReader(_streamProvider.GetStream(), _defaultEncoding, _path); }
	}
}
