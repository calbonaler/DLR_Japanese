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
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Scripting.Actions
{
	/// <summary>メソッドを副作用を持たないとしてマークします。コンボバインダーによってメソッド呼び出しを可能にするために使用されます。</summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
	public sealed class NoSideEffectsAttribute : Attribute { }
}
