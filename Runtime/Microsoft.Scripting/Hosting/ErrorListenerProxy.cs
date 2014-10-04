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
	/// <see cref="Microsoft.Scripting.ErrorSink"/> と <see cref="Microsoft.Scripting.Hosting.ErrorListener"/> の橋渡しを行います。
	/// <see cref="Microsoft.Scripting.ErrorSink"/> に対して言語コンパイラから報告されたエラーはホストが提供する <see cref="Microsoft.Scripting.Hosting.ErrorListener"/> に転送されます。
	/// </summary>
	sealed class ErrorListenerProxySink : ErrorSink
	{
		readonly ErrorListener _listener;
		readonly ScriptSource _source;

		/// <summary>
		/// エラーの発生元となる <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> とエラーを転送する <see cref="Microsoft.Scripting.Hosting.ErrorListener"/> を使用して、
		/// <see cref="Microsoft.Scripting.Hosting.ErrorListenerProxySink"/> クラスの新しいインスタンスを初期化します。
		/// </summary>
		/// <param name="source">エラーの発生元となる <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> を指定します。</param>
		/// <param name="listener">発生したエラーを転送する <see cref="Microsoft.Scripting.Hosting.ErrorListener"/> を指定します。</param>
		public ErrorListenerProxySink(ScriptSource source, ErrorListener listener)
		{
			_listener = listener;
			_source = source;
		}

		/// <summary>この <see cref="Microsoft.Scripting.ErrorSink"/> オブジェクトにエラーを追加します。</summary>
		/// <param name="sourceUnit">エラーが発生したソースコードを示す <see cref="Microsoft.Scripting.SourceUnit"/> を指定します。</param>
		/// <param name="message">エラーに対するメッセージを指定します。</param>
		/// <param name="span">エラーが発生したソースコード上の場所を示す <see cref="Microsoft.Scripting.SourceSpan"/> を指定します。</param>
		/// <param name="errorCode">エラーコードを表す数値を指定します。</param>
		/// <param name="severity">エラーの深刻さを示す値を指定します。</param>
		public override void Add(SourceUnit sourceUnit, string message, SourceSpan span, int errorCode, Severity severity)
		{
			if (_listener != null)
				_listener.ErrorReported(sourceUnit != _source.SourceUnit ? new ScriptSource(_source.Engine.Runtime.GetEngine(sourceUnit.LanguageContext), sourceUnit): _source, message, span, errorCode, severity);
			else if (severity == Severity.FatalError || severity == Severity.Error)
				throw new SyntaxErrorException(message, sourceUnit, span, errorCode, severity);
		}

		/// <summary>
		/// この <see cref="Microsoft.Scripting.ErrorSink"/> オブジェクトにエラーを追加します。
		/// このオーバーロードは <see cref="Microsoft.Scripting.SourceUnit"/> オブジェクトが使用できない場合に呼び出されます。</summary>
		/// <param name="message">エラーに対するメッセージを指定します。</param>
		/// <param name="path">エラーが発生したソースコードのパスを指定します。</param>
		/// <param name="code">エラーが発生したソースコードを指定します。</param>
		/// <param name="line">エラーが発生した行を指定します。</param>
		/// <param name="span">エラーが発生したソースコード上の場所を示す <see cref="Microsoft.Scripting.SourceSpan"/> を指定します。</param>
		/// <param name="errorCode">エラーコードを表す数値を指定します。</param>
		/// <param name="severity">エラーの深刻さを示す値を指定します。</param>
		public override void Add(string message, string path, string code, string line, SourceSpan span, int errorCode, Severity severity)
		{
			if (_listener != null)
				_listener.ErrorReported(_source, message, span, errorCode, severity);
			else if (severity == Severity.FatalError || severity == Severity.Error)
				throw new SyntaxErrorException(message, path, code, line, span, errorCode, severity);
		}
	}
}
