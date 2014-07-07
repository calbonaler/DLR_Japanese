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
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting
{
	/// <summary>コードを表す文字列を格納する <see cref="TextContentProvider"/> を表します。</summary>
	[Serializable]
	sealed class SourceStringContentProvider : TextContentProvider
	{
		readonly string _code;

		/// <summary>指定されたコードを使用して、<see cref="Microsoft.Scripting.SourceStringContentProvider"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="code">基になるコードを表す文字列を指定します。</param>
		internal SourceStringContentProvider(string code)
		{
			ContractUtils.RequiresNotNull(code, "code");
			_code = code;
		}

		/// <summary><see cref="TextContentProvider"/> が作成されたコンテンツを基にする新しい <see cref="TextReader"/> を作成します。</summary>
		public override SourceCodeReader GetReader() { return new SourceCodeReader(new StringReader(_code), null); }
	}
}
