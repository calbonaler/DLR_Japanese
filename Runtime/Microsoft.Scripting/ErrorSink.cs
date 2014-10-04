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

using System.Threading;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting
{
	/// <summary>報告されたエラーを処理する方法を提供します。既定ではすべてのエラーは例外を発生させます。</summary>
	public class ErrorSink
	{
		/// <summary>既定の <see cref="Microsoft.Scripting.ErrorSink"/> を表します。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly ErrorSink/*!*/ Default = new ErrorSink();

		/// <summary>すべてのエラーを無視する <see cref="Microsoft.Scripting.ErrorSink"/> を表します。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly ErrorSink/*!*/ Null = new NullErrorSink();

		/// <summary><see cref="Microsoft.Scripting.ErrorSink"/> クラスの新しいインスタンスを初期化します。</summary>
		protected ErrorSink() { }

		/// <summary>この <see cref="Microsoft.Scripting.ErrorSink"/> オブジェクトにエラーを追加します。</summary>
		/// <param name="source">エラーが発生したソースコードを示す <see cref="Microsoft.Scripting.SourceUnit"/> を指定します。</param>
		/// <param name="message">エラーに対するメッセージを指定します。</param>
		/// <param name="span">エラーが発生したソースコード上の場所を示す <see cref="Microsoft.Scripting.SourceSpan"/> を指定します。</param>
		/// <param name="errorCode">エラーコードを表す数値を指定します。</param>
		/// <param name="severity">エラーの深刻さを示す値を指定します。</param>
		public virtual void Add(SourceUnit source, string/*!*/ message, SourceSpan span, int errorCode, Severity severity)
		{
			if (severity == Severity.FatalError || severity == Severity.Error)
				throw new SyntaxErrorException(message, source, span, errorCode, severity);
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
		public virtual void Add(string message, string path, string code, string line, SourceSpan span, int errorCode, Severity severity)
		{
			if (severity == Severity.FatalError || severity == Severity.Error)
				throw new SyntaxErrorException(message, path, code, line, span, errorCode, severity);
		}

		sealed class NullErrorSink : ErrorSink
		{
			internal NullErrorSink() { }

			public override void Add(SourceUnit source, string/*!*/ message, SourceSpan span, int errorCode, Severity severity) { }

			public override void Add(string message, string path, string code, string line, SourceSpan span, int errorCode, Severity severity) { }
		}
	}

	/// <summary>発生したエラーを <see cref="Microsoft.Scripting.Severity"/> 毎にカウントする <see cref="Microsoft.Scripting.ErrorSink"/> を表します。</summary>
	public class ErrorCounter : ErrorSink
	{
		readonly ErrorSink/*!*/ _sink;

		int _fatalErrorCount;
		int _errorCount;
		int _warningCount;

		/// <summary>発生したエラー中の <see cref="Microsoft.Scripting.Severity.FatalError"/> の個数を取得します。</summary>
		public int FatalErrorCount { get { return _fatalErrorCount; } }

		/// <summary>発生したエラー中の <see cref="Microsoft.Scripting.Severity.Error"/> の個数を取得します。</summary>
		public int ErrorCount { get { return _errorCount; } }

		/// <summary>発生したエラー中の <see cref="Microsoft.Scripting.Severity.Warning"/> の個数を取得します。</summary>
		public int WarningCount { get { return _warningCount; } }

		/// <summary>警告以外のエラーが発生したかどうかを示す値を取得します。</summary>
		public bool AnyError { get { return _errorCount > 0 || _fatalErrorCount > 0; } }

		/// <summary><see cref="Microsoft.Scripting.ErrorCounter"/> クラスの新しいインスタンスを初期化します。</summary>
		public ErrorCounter() : this(ErrorSink.Null) { }

		/// <summary>
		/// 基になる <see cref="Microsoft.Scripting.ErrorSink"/> オブジェクトを使用して、
		/// <see cref="Microsoft.Scripting.ErrorCounter"/> クラスの新しいインスタンスを初期化します。
		/// </summary>
		/// <param name="sink">発生したエラーを渡す <see cref="Microsoft.Scripting.ErrorSink"/> オブジェクトを指定します。</param>
		public ErrorCounter(ErrorSink/*!*/ sink)
		{
			ContractUtils.RequiresNotNull(sink, "sink");
			_sink = sink;
		}

		/// <summary><see cref="Microsoft.Scripting.Severity"/> 毎にエラーの個数をカウントします。</summary>
		/// <param name="severity">エラーの深刻さを示す値を指定します。</param>
		protected virtual void CountError(Severity severity)
		{
			if (severity == Severity.FatalError)
				Interlocked.Increment(ref _fatalErrorCount);
			else if (severity == Severity.Error)
				Interlocked.Increment(ref _errorCount);
			else if (severity == Severity.Warning)
				Interlocked.Increment(ref _warningCount);
		}

		/// <summary>このオブジェクトのすべてのカウンタをクリアします。</summary>
		public void ClearCounters() { _warningCount = _errorCount = _fatalErrorCount = 0; }

		/// <summary>この <see cref="Microsoft.Scripting.ErrorSink"/> オブジェクトにエラーを追加します。</summary>
		/// <param name="source">エラーが発生したソースコードを示す <see cref="Microsoft.Scripting.SourceUnit"/> を指定します。</param>
		/// <param name="message">エラーに対するメッセージを指定します。</param>
		/// <param name="span">エラーが発生したソースコード上の場所を示す <see cref="Microsoft.Scripting.SourceSpan"/> を指定します。</param>
		/// <param name="errorCode">エラーコードを表す数値を指定します。</param>
		/// <param name="severity">エラーの深刻さを示す値を指定します。</param>
		public override void Add(SourceUnit source, string/*!*/ message, SourceSpan span, int errorCode, Severity severity)
		{
			CountError(severity);
			_sink.Add(source, message, span, errorCode, severity);
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
			CountError(severity);
			_sink.Add(message, path, code, line, span, errorCode, severity);
		}
	}
}
