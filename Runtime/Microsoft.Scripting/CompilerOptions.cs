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
	/// <summary>コンパイラオプションを表します。</summary>
	[Serializable]
	public class CompilerOptions : ICloneable
	{
		/// <summary><see cref="Microsoft.Scripting.CompilerOptions"/> クラスの新しいインスタンスを初期化します。</summary>
		public CompilerOptions() { }

		/// <summary>現在のインスタンスのコピーである新しいオブジェクトを作成します。</summary>
		/// <returns>このインスタンスのコピーである新しいオブジェクト。</returns>
		public virtual object Clone() { return base.MemberwiseClone(); }
	}
}
