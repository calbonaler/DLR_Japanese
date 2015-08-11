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

namespace Microsoft.Scripting.Debugging
{
	public enum TraceEventKind
	{
		/// <summary>
		/// 実行が新しいフレームに入るときに発生します。
		/// Payload: なし
		/// </summary>
		FrameEnter,

		/// <summary>
		/// 実行がフレームから出るときに発生します。
		/// Payload: 関数からの戻り値
		/// </summary>
		FrameExit,

		/// <summary>
		/// 実行がデバッグ スレッドから出るときに発生します。
		/// Payload: なし
		/// </summary>
		ThreadExit,

		/// <summary>
		/// 実行がトレース ポイントに到達したときに発生します。
		/// Payload: なし
		/// </summary>
		TracePoint,

		/// <summary>
		/// 実行中に例外が発生したときに発生します。
		/// Payload: スローされた例外オブジェクト
		/// </summary>
		Exception,

		/// <summary>
		/// 例外がスローされ現在のメソッドによってハンドルされなかったときに発生します。
		/// Payload: スローされた例外オブジェクト
		/// </summary>
		ExceptionUnwind,
	}
}
