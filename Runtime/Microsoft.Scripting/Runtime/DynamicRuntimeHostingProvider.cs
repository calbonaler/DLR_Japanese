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
	/// <summary>
	/// DLR はあらゆるホスティング API プロバイダにこのクラスを実装し、そのインスタンスをランタイムの初期化で提供することを要求します。
	/// DLR は基本的なホスト/システム依存の動作をこのクラスを用いて呼び出します。
	/// </summary>
	[Serializable]
	public abstract class DynamicRuntimeHostingProvider
	{
		/// <summary>ホストに関連付けられた <see cref="Microsoft.Scripting.PlatformAdaptationLayer"/> を取得します。</summary>
		public abstract PlatformAdaptationLayer PlatformAdaptationLayer { get; }
	}
}
