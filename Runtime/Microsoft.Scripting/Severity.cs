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

namespace Microsoft.Scripting
{
	/// <summary>エラーの深刻さを表します。</summary>
	public enum Severity
	{
		/// <summary>無視可能なエラーです。</summary>
		Ignore,
		/// <summary>警告です。</summary>
		Warning,
		/// <summary>通常のエラーです。</summary>
		Error,
		/// <summary>致命的なエラーです。</summary>
		FatalError,
	}
}
