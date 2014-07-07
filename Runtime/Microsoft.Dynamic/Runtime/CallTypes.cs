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

namespace Microsoft.Scripting.Runtime
{
	/// <summary>メソッドを呼び出す方法を指定します。</summary>
	[Flags]
	public enum CallTypes
	{
		/// <summary>すべての引数を明示してメソッドを呼び出します。</summary>
		None = 0,
		/// <summary>暗黙のインスタンス引数を含んだままメソッドを呼び出します。</summary>
		ImplicitInstance,
	}
}
