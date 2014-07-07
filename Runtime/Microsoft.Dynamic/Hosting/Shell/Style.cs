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

namespace Microsoft.Scripting.Hosting.Shell
{
	/// <summary>コンソールの表示スタイルを指定します。</summary>
	public enum Style
	{
		/// <summary>文字列はプロンプトとして表示されます。</summary>
		Prompt,
		/// <summary>文字列は標準の出力として表示されます。</summary>
		Out,
		/// <summary>文字列はエラー出力として表示されます。</summary>
		Error,
		/// <summary>文字列は警告として表示されます。</summary>
		Warning
	}
}
