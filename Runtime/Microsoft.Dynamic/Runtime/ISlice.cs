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
	/// <summary>Python の Slice オブジェクトに影響された数値配列のスライスを取得する場合に有効なインターフェイスです。</summary>
	public interface ISlice
	{
		/// <summary>スライスの開始インデックスを取得します。開始インデックスが定義されていない場合は <c>null</c> を返します。</summary>
		object StartIndex { get; }

		/// <summary>スライスの終了インデックスを取得します。終了インデックスが定義されていない場合は <c>null</c> を返します。</summary>
		object StopIndex { get; }

		/// <summary>取得するステップの長さを取得します。</summary>
		object StepCount { get; }
	}
}
