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
	/// バイナリデータの単一のソースを基にするストリームを作成する機能を提供します。
	/// このクラスは不明なエンコーディングのファイルを開く場合に使用されます。
	/// </summary>
	/// <remarks>
	/// <see cref="StreamContentProvider"/> はバイナリデータをテキストに変換する固有の方法をサポートできる言語によって提供される
	/// <see cref="TextContentProvider"/> によってラップされます。
	/// たとえば、ファイルの先頭に配置され残りの部分のエンコーディングを指定できるマーカーを認める言語もあります。
	/// </remarks>
	[Serializable]
	public abstract class StreamContentProvider
	{
		/// <summary><see cref="StreamContentProvider"/> が作成されたコンテンツを基にする新しい <see cref="Stream"/> を作成します。</summary>
		/// <remarks>
		/// たとえば、<see cref="StreamContentProvider"/> がファイルを基にしている場合、<see cref="GetStream"/> はファイルをもう一度開き新しいストリームを返します。
		/// このメソッドは複数回呼び出される可能性があります。
		/// たとえば、1 回目はコードをコンパイルするため、2 回目はエラーメッセージを表示するためにソースコードを取得するため、などです。
		/// </remarks>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public abstract Stream GetStream();
	}
}
