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
	/// <summary>ソースコードを読み取る <see cref="TextReader"/> を表します。</summary>    
	public class SourceCodeReader : TextReader
	{
		/// <summary>何も読み取らない <see cref="SourceCodeReader"/> を示します。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly new SourceCodeReader Null = new SourceCodeReader(TextReader.Null, null);

		/// <summary>指定された <see cref="TextReader"/> およびエンコーディングを使用して、<see cref="Microsoft.Scripting.SourceCodeReader"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="textReader">基になる <see cref="TextReader"/> を指定します。</param>
		/// <param name="encoding">基になるバイトストリームからのデータの読み取りに使用されるエンコーディングを指定します。基になるデータがテキストの場合は <c>null</c> を指定できます。</param>
		public SourceCodeReader(TextReader textReader, Encoding encoding)
		{
			ContractUtils.RequiresNotNull(textReader, "textReader");
			Encoding = encoding;
			BaseReader = textReader;
		}

		/// <summary>
		/// 基になるバイトストリームから読み取られたデータを変換するためにリーダーによって使用されるエンコーディングを取得します。
		/// リーダーがテキストから読み取っていて、デコードが行われていない場合は <c>null</c> になります。
		/// </summary>
		public Encoding Encoding { get; private set; }

		/// <summary>基になる <see cref="TextReader"/> を取得します。</summary>
		public TextReader BaseReader { get; private set; }

		/// <summary>テキスト リーダーから 1 行分の文字を読み取り、そのデータを文字列として返します。</summary>
		/// <returns>リーダーの次の行。またはすべての文字が読み取られた場合は <c>null</c>。</returns>
		public override string ReadLine() { return BaseReader.ReadLine(); }

		/// <summary>テキストストリームから指定された行の最初の文字を検索します。</summary>
		/// <param name="line">行番号を指定します。現在の行番号が 1 と仮定されます。</param>
		/// <returns>行が見つかった場合は <c>true</c>、それ以外の場合は <c>false</c>。</returns>
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

		/// <summary>テキスト リーダーの現在位置から末尾まですべての文字を読み取り、1 つの文字列として返します。</summary>
		/// <returns>テキスト リーダーの現在位置から末尾までのすべての文字を含む文字列。</returns>
		public override string ReadToEnd() { return BaseReader.ReadToEnd(); }

		/// <summary>指定した最大文字数を現在のリーダーから読み取り、バッファーの指定したインデックス位置にそのデータを書き込みます。</summary>
		/// <param name="buffer">このメソッドが戻るとき、指定した文字配列の <paramref name="index"/> から (<paramref name="index"/> + <paramref name="count"/> - 1) までの値が、現在のソースから読み取られた文字に置き換えられます。</param>
		/// <param name="index">書き込みを開始する <paramref name="buffer"/>内の位置。</param>
		/// <param name="count">読み取り対象の最大文字数。 指定された文字数をバッファーに読み取る前にリーダーの末尾に到達した場合、メソッドは制御を返します。</param>
		/// <returns>
		/// 読み取られた文字数。
		/// この数値は、リーダー内に使用できるデータがあるかどうかによって異なりますが、<paramref name="count"/> 以下の数値になります。
		/// 読み取り対象の文字がない場合にこのメソッドを呼び出すと、0 (ゼロ) が返されます。
		/// </returns>
		public override int Read(char[] buffer, int index, int count) { return BaseReader.Read(buffer, index, count); }

		/// <summary> 指定した最大文字数を現在のテキスト リーダーから読み取り、バッファーの指定したインデックス位置にそのデータを書き込みます。</summary>
		/// <param name="buffer">このメソッドが戻るとき、指定した文字配列の <paramref name="index"/> から (<paramref name="index"/> + <paramref name="count"/> -1) までの値が、現在のソースから読み取られた文字に置き換えられています。</param>
		/// <param name="index">書き込みを開始する <paramref name="buffer"/> 内の位置。</param>
		/// <param name="count">読み取り対象の最大文字数。</param>
		/// <returns>読み取られた文字数。この数値は、すべての入力文字が読み取られたかどうかによって異なりますが、<paramref name="count"/> 以下の数値になります。</returns>
		public override int ReadBlock(char[] buffer, int index, int count) { return BaseReader.ReadBlock(buffer, index, count); }

		/// <summary>リーダーや文字の読み取り元の状態を変更せずに、次の文字を読み取ります。 リーダーから実際に文字を読み取らずに次の文字を返します。</summary>
		/// <returns>読み取り対象の次の文字を表す整数。使用できる文字がないか、リーダーがシークをサポートしていない場合は -1。</returns>
		public override int Peek() { return BaseReader.Peek(); }

		/// <summary> テキスト リーダーから次の文字を読み取り、1 文字分だけ文字位置を進めます。</summary>
		/// <returns>テキスト リーダーからの次の文字。それ以上読み取り可能な文字がない場合は -1。</returns>
		public override int Read() { return BaseReader.Read(); }

		/// <summary><see cref="Microsoft.Scripting.SourceCodeReader"/> によって使用されているアンマネージ リソースを解放し、オプションでマネージ リソースも解放します。</summary>
		/// <param name="disposing">マネージ リソースとアンマネージ リソースの両方を解放する場合は <c>trie</c>。アンマネージ リソースだけを解放する場合は <c>false</c>。</param>
		protected override void Dispose(bool disposing) { BaseReader.Dispose(); }
	}
}
