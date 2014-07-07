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

namespace Microsoft.Scripting.Hosting
{
	/// <summary>
	/// <see cref="Microsoft.Scripting.Hosting.ErrorListener"/> と <see cref="Microsoft.Scripting.ErrorSink"/> の橋渡しを行います。
	/// <see cref="Microsoft.Scripting.Hosting.ErrorListenerProxySink"/> とは逆の機能を提供します。
	/// </summary>
	public sealed class ErrorSinkProxyListener : ErrorListener
	{
		ErrorSink _errorSink;

		/// <summary>
		/// エラーが発生した際にエラーが転送される <see cref="Microsoft.Scripting.ErrorSink"/> を使用して、
		/// <see cref="Microsoft.Scripting.Hosting.ErrorSinkProxyListener"/> クラスの新しいインスタンスを初期化します。
		/// </summary>
		/// <param name="errorSink">エラーが発生した際にエラーが転送される <see cref="Microsoft.Scripting.ErrorSink"/> を指定します。</param>
		public ErrorSinkProxyListener(ErrorSink errorSink) { _errorSink = errorSink; }

		/// <summary>エラーが報告されたときに呼び出されます。</summary>
		/// <param name="source">エラーが発生した <see cref="ScriptSource"/> です。</param>
		/// <param name="message">エラーに対するメッセージです。</param>
		/// <param name="span">エラーが発生した場所を示す <see cref="SourceSpan"/> です。</param>
		/// <param name="errorCode">エラーコードを示す整数値です。</param>
		/// <param name="severity">エラーの深刻さを示す値です。</param>
		public override void ErrorReported(ScriptSource source, string message, SourceSpan span, int errorCode, Severity severity)
		{
			// source がプロキシオブジェクトであり、source.SourceUnit を使用できないため、source.SourceUnit を現在の AppDomain にマーシャリングできません。
			string code = null;
			string line = null;
			try
			{
				code = source.GetCode();
				line = source.GetCodeLine(span.Start.Line);
			}
			catch (System.IO.IOException) { } // ソースコードを取得できない
			_errorSink.Add(message, source.Path, code, line, span, errorCode, severity);
		}
	}
}
