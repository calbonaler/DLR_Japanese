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
using System.Security.Permissions;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting
{
	/// <summary>DLR 上でのホストを表します。</summary>
	/// <remarks>
	/// <see cref="Microsoft.Scripting.Hosting.ScriptHost"/> は <see cref="Microsoft.Scripting.Hosting.ScriptRuntime"/> とともに、同一のアプリケーションドメインに配置されます。
	/// ホストはいくつかの通知を取得したり、TryGetSourceUnit や ResolveSourceUnit などのように操作をカスタマイズするために派生クラスを実装できます。
	///
	/// 派生クラスのコンストラクタ引数は <see cref="Microsoft.Scripting.Hosting.ScriptRuntime"/> の初期化に関与する
	/// <see cref="Microsoft.Scripting.Hosting.ScriptRuntimeSetup"/> インスンタスによって指定されます。
	/// 
	/// ホストがリモートであれば、DLR (すなわち、<see cref="Microsoft.Scripting.Hosting.ScriptHost"/> も)
	/// およびそのアプリケーションドメインに生存しているオブジェクトへのアクセスの必要性に関しては、<see cref="Microsoft.Scripting.Hosting.ScriptHost"/>
	/// の派生クラスのコンストラクタに引数として <see cref="System.MarshalByRefObject"/> を渡すことができます。
	/// </remarks>
	public class ScriptHost : MarshalByRefObject
	{
		ScriptRuntime _runtime;

		/// <summary>ホストがアタッチされるランタイムを取得します。</summary>
		public ScriptRuntime Runtime
		{
			get
			{
				if (_runtime == null)
					throw new InvalidOperationException("Host not initialized");
				return _runtime;
			}
			internal set
			{
				// ScriptRuntime によって完全に初期化されたときに呼ばれます。
				Assert.NotNull(value);
				_runtime = value;
				RuntimeAttached(); // ホスト実装にランタイムが利用可能になったことを通知します。
			}
		}

		/// <summary>ホストに関連付けられた <see cref="Microsoft.Scripting.PlatformAdaptationLayer"/> を取得します。</summary>
		public virtual PlatformAdaptationLayer PlatformAdaptationLayer { get { return PlatformAdaptationLayer.Default; } }

		#region Notifications

		/// <summary>
		/// 関連付けられたランタイムの初期化が完了した後に呼び出されます。
		/// ホストはアセンブリのロードなどのランタイムの追加の初期化を実行するためにこのメソッドをオーバーライドできます。
		/// </summary>
		protected virtual void RuntimeAttached() { }

		/// <summary>
		/// ランタイムに新しい言語がロードされた後に呼び出されます。
		/// ホストは言語エンジンの追加の初期化を実行するためにこのメソッドをオーバーライドできます。
		/// </summary>
		/// <param name="engine">ロードされた言語を指定します。</param>
		protected internal virtual void EngineCreated(ScriptEngine engine) { }

		#endregion

		// TODO: Figure out what is the right lifetime
		/// <summary>対象のインスタンスの有効期間ポリシーを制御する、有効期間サービス オブジェクトを取得します。</summary>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
		public override object InitializeLifetimeService() { return null; }
	}

}
