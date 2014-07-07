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
using System.Diagnostics;

namespace Microsoft.Scripting
{
	/// <summary>
	/// 静的フィールドを複数のランタイムからアクセスされる場合でも安全であるとしてマークします。</summary>
	/// <remarks>
	/// この属性でマークされていない書き込み可能な静的フィールドはランタイム間で共有されている状態を調べるテストによってフラグが付けられます。
	/// この属性を適用する前にユーザーは状態を共有しても安全であることを確実にするべきです。
	/// これは通常遅延初期化されるか、すべてのランタイムで同一で不変な値をキャッシュしている変数に適用します。
	/// </remarks>
	[Conditional("DEBUG")]
	[AttributeUsage(AttributeTargets.Field)]
	public sealed class MultiRuntimeAwareAttribute : Attribute { }
}
