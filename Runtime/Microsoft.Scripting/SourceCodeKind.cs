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

using System.ComponentModel;

namespace Microsoft.Scripting
{
	/// <summary>ソースコードの種類を定義します。パーサーは適宜初期状態を設定します。</summary>
	public enum SourceCodeKind
	{
		/// <summary>種類は指定されていません。</summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		Unspecified = 0,
		/// <summary>ソースコードは式を表しています。</summary>
		Expression = 1,
		/// <summary>ソースコードは複数のステートメントを表しています。</summary>
		Statements = 2,
		/// <summary>ソースコードは単一のステートメントを表しています。</summary>
		SingleStatement = 3,
		/// <summary>ソースコードはファイルの内容です。</summary>
		File = 4,
		/// <summary>ソースコードは対話コマンドです。 </summary>
		InteractiveCode = 5,
		/// <summary>言語パーサーが自動的に種類を決定します。決定できなかった場合は構文エラーが報告されます。</summary>
		AutoDetect = 6
	}
}

namespace Microsoft.Scripting.Utils
{
	/// <summary>列挙体の範囲に関するメソッドを提供します。</summary>
	public static partial class EnumBounds
	{
		/// <summary>指定された <see cref="SourceCodeKind"/> 列挙体が有効な値を示しているかどうかを返します。</summary>
		/// <param name="value">有効かどうかを調べる <see cref="SourceCodeKind"/> 列挙体の値を指定します。</param>
		/// <returns>有効な値であれば <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool IsValid(this SourceCodeKind value) { return value > SourceCodeKind.Unspecified && value <= SourceCodeKind.AutoDetect; }
	}
}
