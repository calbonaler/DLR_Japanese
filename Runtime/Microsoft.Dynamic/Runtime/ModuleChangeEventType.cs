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

namespace Microsoft.Scripting.Runtime
{
	/// <summary>シンボルがどのように変更されたかを指定します。</summary>
	public enum ModuleChangeType
	{
		/// <summary>モジュール内で新しい値が設定されました。(あるいは以前の値が変更されました。)</summary>
		Set,
		/// <summary>値がモジュールから削除されました。</summary>
		Delete,
	}
}
