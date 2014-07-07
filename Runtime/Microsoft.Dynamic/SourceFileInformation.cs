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
	/// <summary>特にファイル名や一意言語識別子などソースファイルに対するデバッグ情報の出力時に必要になる情報を格納します。</summary>
	public sealed class SourceFileInformation
	{
		/// <summary>ファイル名を使用して、<see cref="Microsoft.Scripting.SourceFileInformation"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="fileName">ソースファイルのファイル名を指定します。</param>
		public SourceFileInformation(string fileName) { FileName = fileName; }

		/// <summary>ファイル名および言語識別子を使用して、<see cref="Microsoft.Scripting.SourceFileInformation"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="fileName">ソースファイルのファイル名を指定します。</param>
		/// <param name="language">ソースファイルが書かれた言語を識別するグローバル一意識別子を指定します。</param>
		public SourceFileInformation(string fileName, Guid language)
		{
			FileName = fileName;
			LanguageGuid = language;
		}

		/// <summary>ファイル名、言語識別子およびベンダー識別子を使用して、<see cref="Microsoft.Scripting.SourceFileInformation"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="fileName">ソースファイルのファイル名を指定します。</param>
		/// <param name="language">ソースファイルが書かれた言語を識別するグローバル一意識別子を指定します。</param>
		/// <param name="vendor">ソースファイルが書かれた言語のベンダーを識別するグローバル一意識別子を指定します。</param>
		public SourceFileInformation(string fileName, Guid language, Guid vendor)
		{
			FileName = fileName;
			LanguageGuid = language;
			VendorGuid = vendor;
		}

		/// <summary>ソースファイルのファイル名を取得します。</summary>
		public string FileName { get; private set; }

		// TODO: save storage space if these are not supplied?

		/// <summary>ソースファイルが書かれた言語を識別するグローバル一意識別子を取得します。</summary>
		public Guid LanguageGuid { get; private set; }

		/// <summary>ソースファイルが書かれた言語のベンダーを識別するグローバル一意識別子を取得します。</summary>
		public Guid VendorGuid { get; private set; }
	}
}
