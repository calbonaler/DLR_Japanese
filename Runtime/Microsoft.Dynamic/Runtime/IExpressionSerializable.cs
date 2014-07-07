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

using System.Linq.Expressions;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>
	/// オブジェクトの式ツリーへのシリアル化を可能にします。
	/// 式ツリーはオブジェクトの逆シリアル化ができるように、アセンブリに出力されます。
	/// </summary>
	public interface IExpressionSerializable
	{
		/// <summary>オブジェクトの現在の状態を式ツリーにシリアル化します。</summary>
		/// <returns>オブジェクトの状態がシリアル化された式ツリー。</returns>
		Expression CreateExpression();
	}
}
