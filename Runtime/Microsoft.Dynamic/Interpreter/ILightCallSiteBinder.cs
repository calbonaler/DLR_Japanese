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

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>インタプリタによって認識される動的呼び出しサイトのバインダーを表します。</summary>
	public interface ILightCallSiteBinder
	{
		/// <summary>このバインダーが <see cref="Microsoft.Scripting.Runtime.ArgumentArray"/> を受け入れることができるかどうかを示す値を取得します。</summary>
		bool AcceptsArgumentArray { get; }
	}
}
