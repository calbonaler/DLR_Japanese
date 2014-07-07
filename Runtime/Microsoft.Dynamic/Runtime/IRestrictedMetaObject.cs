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
using System.Dynamic;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>
	/// <see cref="DynamicMetaObject"/> がすでに制約された型を表していることを示します。
	/// すでに既知の型に制約されているが、型情報がキャプチャされていない場合 (型がシールされていないなど) に有効です。
	/// </summary>
	public interface IRestrictedMetaObject
	{
		/// <summary>指定された型の制約された <see cref="DynamicMetaObject"/> を返します。</summary>
		/// <param name="type">制約する型を指定します。</param>
		/// <returns>型に制約された <see cref="DynamicMetaObject"/>。</returns>
		DynamicMetaObject Restrict(Type type);
	}
}
