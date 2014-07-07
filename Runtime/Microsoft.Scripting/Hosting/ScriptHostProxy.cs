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

using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting
{
	/// <summary>DLR に対してホスティングを提供します。DLRに対する要求は <see cref="Microsoft.Scripting.Hosting.ScriptHost"/> に転送されます。</summary>
	sealed class ScriptHostProxy : DynamicRuntimeHostingProvider
	{
		readonly ScriptHost _host;

		/// <summary>指定されたホストを使用して、<see cref="Microsoft.Scripting.Hosting.ScriptHostProxy"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="host">要求を転送するホストを指定します。</param>
		public ScriptHostProxy(ScriptHost host)
		{
			Assert.NotNull(host);
			_host = host;
		}

		/// <summary>ホストに関連付けられた <see cref="Microsoft.Scripting.PlatformAdaptationLayer"/> を取得します。</summary>
		public override PlatformAdaptationLayer PlatformAdaptationLayer { get { return _host.PlatformAdaptationLayer; } }
	}
}
