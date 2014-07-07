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

namespace Microsoft.Scripting.Hosting
{
	/// <summary>
	/// ホストはこのクラスを使用してスクリプトの解析やコンパイル中に報告されたエラーを追跡することができます。
	/// <see cref="Microsoft.Scripting.ErrorSink"/> に対するもう 1 つのホスティング API です。
	/// </summary>
	public abstract class ErrorListener : MarshalByRefObject
	{
		/// <summary><see cref="Microsoft.Scripting.Hosting.ErrorListener"/> クラスの新しいインスタンスを初期化します。</summary>
		protected ErrorListener() { }

		/// <summary>エラーが報告されたときに呼び出されます。</summary>
		/// <param name="source">エラーが発生した <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> です。</param>
		/// <param name="message">エラーに対するメッセージです。</param>
		/// <param name="span">エラーが発生した場所を示す <see cref="Microsoft.Scripting.SourceSpan"/> です。</param>
		/// <param name="errorCode">エラーコードを示す整数値です。</param>
		/// <param name="severity">エラーの深刻さを示す値です。</param>
		public abstract void ErrorReported(ScriptSource source, string message, SourceSpan span, int errorCode, Severity severity);

		// TODO: Figure out what is the right lifetime
		/// <summary>対象のインスタンスの有効期間ポリシーを制御する、有効期間サービス オブジェクトを取得します。</summary>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
		public override object InitializeLifetimeService() { return null; }
	}
}
