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

using System.Collections.Generic;
using System.Dynamic;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary><see cref="DefaultOverloadResolver"/> を作成する方法を抽象化します。</summary>
	public abstract class OverloadResolverFactory
	{
		/// <summary>指定された引数および呼び出しシグネチャを使用して新しい <see cref="Microsoft.Scripting.Actions.DefaultOverloadResolver"/> を作成します。</summary>
		/// <param name="args">オーバーロード解決の対象となる引数のリストを指定します。</param>
		/// <param name="signature">オーバーロードを呼び出すシグネチャを指定します。</param>
		/// <param name="callType">オーバーロードを呼び出す方法を指定します。</param>
		/// <returns>指定された引数およびシグネチャに対するオーバーロードを解決する <see cref="DefaultOverloadResolver"/>。</returns>
		public abstract DefaultOverloadResolver CreateOverloadResolver(IList<DynamicMetaObject> args, CallSignature signature, CallTypes callType);
	}
}
