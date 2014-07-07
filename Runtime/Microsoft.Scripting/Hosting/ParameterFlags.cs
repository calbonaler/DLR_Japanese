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

namespace Microsoft.Scripting.Hosting
{
	/// <summary>引数に関する追加情報を示します。</summary>
	[Flags]
	public enum ParameterFlags
	{
		/// <summary>追加情報はありません。</summary>
		None,
		/// <summary>引数は配列引数です。</summary>
		ParamsArray = 0x01,
		/// <summary>引数は辞書引数です。</summary>
		ParamsDict = 0x02,
	}
}
