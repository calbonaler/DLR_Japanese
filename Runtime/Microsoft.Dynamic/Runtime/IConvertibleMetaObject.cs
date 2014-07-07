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
	/// <summary><see cref="System.Dynamic.DynamicMetaObject"/> が CLR 型に変換可能であることを示します。</summary>
	public interface IConvertibleMetaObject
	{
		/// <summary>この <see cref="System.Dynamic.DynamicMetaObject"/> が指定された型に変換可能であるかどうかを判断します。</summary>
		/// <param name="type">変換先の型を指定します。</param>
		/// <param name="isExplicit">変換が明示的に行われるかどうかを示す値を指定します。</param>
		/// <returns><see cref="System.Dynamic.DynamicMetaObject"/> が <paramref name="type"/> に指定された変換方法で変換可能な場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		bool CanConvertTo(Type type, bool isExplicit);
	}
}
