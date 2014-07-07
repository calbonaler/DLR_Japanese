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
	/// テキストデータの単一のソースを基にする <see cref="TextReader"/> を作成する機能を提供します。
	/// このクラスはすでにデコードされているか、既知の特定のエンコーディングであるソースの読み取りに使用されます。
	/// </summary>
	/// <remarks>
	/// たとえば、テキストエディタは基になるデータがユーザーが直接編集するメモリ内のテキストバッファである <see cref="TextContentProvider"/> を提供するかもしれません。
	/// </remarks>
	[Serializable]
	public abstract class TextContentProvider
	{
		/// <summary>データを提供しない <see cref="TextContentProvider"/> を示します。</summary>
		public static readonly TextContentProvider Null = new NullTextContentProvider();

		/// <summary><see cref="TextContentProvider"/> が作成されたコンテンツを基にする新しい <see cref="TextReader"/> を作成します。</summary>
		/// <remarks>
		/// このメソッドは複数回呼び出される可能性があります。
		/// たとえば、1 回目はコードをコンパイルするため、2 回目はエラーメッセージを表示するためにソースコードを取得するため、などです。
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
