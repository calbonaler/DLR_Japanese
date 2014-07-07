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
using System.Runtime.Serialization;
using System.Security.Permissions;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting
{
	/// <summary>構文解析が失敗した場合にスローされる例外。</summary>
	[Serializable]
	public class SyntaxErrorException : Exception
	{
		/// <summary><see cref="Microsoft.Scripting.SyntaxErrorException"/> クラスの新しいインスタンスを初期化します。</summary>
		public SyntaxErrorException() : base() { }

		/// <summary>指定したメッセージを使用して、<see cref="Microsoft.Scripting.SyntaxErrorException"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="message">エラーを説明するメッセージ。</param>
		public SyntaxErrorException(string message) : base(message) { }

		/// <summary>指定したエラー メッセージと、この例外の原因である内部例外への参照を使用して、<see cref="Microsoft.Scripting.SyntaxErrorException"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="message">例外の原因を説明するエラー メッセージ。</param>
		/// <param name="innerException">現在の例外の原因である例外。内部例外が指定されていない場合は <c>null</c> 参照 (Visual Basic では、Nothing)。</param>
		public SyntaxErrorException(string message, Exception innerException) : base(message, innerException) { }

		/// <summary>指定したメッセージを使用して、<see cref="Microsoft.Scripting.SyntaxErrorException"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="message">エラーを説明するメッセージ。</param>
		/// <param name="sourceUnit">エラーが発生した翻訳入力単位。</param>
		/// <param name="span">エラーが発生したソースコード上の範囲。</param>
		/// <param name="errorCode">エラーの種類を示す数値。</param>
		/// <param name="severity">エラーの深刻さを示す値。</param>
		public SyntaxErrorException(string message, SourceUnit sourceUnit, SourceSpan span, int errorCode, Severity severity) : base(message)
		{
			ContractUtils.RequiresNotNull(message, "message");
			RawSpan = span;
			Severity = severity;
			ErrorCode = errorCode;
			if (sourceUnit != null)
			{
				SourcePath = sourceUnit.Path;
				try
				{
					SourceCode = sourceUnit.GetCode();
					CodeLine = sourceUnit.GetCodeLine(Line);
				}
				catch (System.IO.IOException) { } // could not get source code.
			}
		}

		/// <summary>指定したメッセージを使用して、<see cref="Microsoft.Scripting.SyntaxErrorException"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="message">エラーを説明するメッセージ。</param>
		/// <param name="path">エラーが発生したファイルを示すパス。</param>
		/// <param name="code">エラーが発生したソースコード。</param>
		/// <param name="line">エラーが発生した行のソースコード。</param>
		/// <param name="span">エラーが発生したソースコード上の範囲。</param>
		/// <param name="errorCode">エラーの種類を示す数値。</param>
		/// <param name="severity">エラーの深刻さを示す値。</param>
		public SyntaxErrorException(string message, string path, string code, string line, SourceSpan span, int errorCode, Severity severity) : base(message)
		{
			ContractUtils.RequiresNotNull(message, "message");
			RawSpan = span;
			Severity = severity;
			ErrorCode = errorCode;
			SourcePath = path;
			SourceCode = code;
			CodeLine = line;
		}

		/// <summary>シリアル化したデータを使用して、<see cref="Microsoft.Scripting.SyntaxErrorException"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="info">スローされている例外に関するシリアル化済みオブジェクト データを保持している <see cref="System.Runtime.Serialization.SerializationInfo"/>。</param>
		/// <param name="context">転送元または転送先に関するコンテキスト情報を含んでいる <see cref="System.Runtime.Serialization.StreamingContext"/>。</param>
		protected SyntaxErrorException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			RawSpan = (SourceSpan)info.GetValue("Span", typeof(SourceSpan));
			SourceCode = info.GetString("SourceCode");
			SourcePath = info.GetString("SourcePath");
			Severity = (Severity)info.GetValue("Severity", typeof(Severity));
			ErrorCode = info.GetInt32("ErrorCode");
		}

		/// <summary>その例外に関する情報を使用して <see cref="System.Runtime.Serialization.SerializationInfo"/> を設定します。</summary>
		/// <param name="info">スローされている例外に関するシリアル化済みオブジェクト データを保持している <see cref="System.Runtime.Serialization.SerializationInfo"/>。</param>
		/// <param name="context">転送元または転送先に関するコンテキスト情報を含んでいる <see cref="System.Runtime.Serialization.StreamingContext"/>。</param>
		/// <exception cref="System.ArgumentNullException"><paramref name="info"/> パラメーターが <c>null</c> 参照 (Visual Basic の場合は Nothing) です。</exception>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			ContractUtils.RequiresNotNull(info, "info");
			base.GetObjectData(info, context);
			info.AddValue("Span", RawSpan);
			info.AddValue("SourceCode", SourceCode);
			info.AddValue("SourcePath", SourcePath);
			info.AddValue("Severity", Severity);
			info.AddValue("ErrorCode", ErrorCode);
		}

		/// <summary>エラーが発生したマッピングされていないソースコード上の範囲を取得します。</summary>
		public SourceSpan RawSpan { get; private set; }

		/// <summary>エラーが発生したソースコードを取得します。</summary>
		public string SourceCode { get; private set; }

		/// <summary>エラーが発生したファイルを示すパスを取得します。</summary>
		public string SourcePath { get; private set; }

		/// <summary>発生したエラーの深刻さを示す値を取得します。</summary>
		public Severity Severity { get; private set; }

		/// <summary>エラーが発生したソースコード上の 1 から始まる行番号を取得します。</summary>
		public int Line { get { return RawSpan.Start.Line; } }

		/// <summary>エラーが発生したソースコード上の 1 から始まる桁番号を取得します。</summary>
		public int Column { get { return RawSpan.Start.Column; } }

		/// <summary>発生したエラーの種類を示す数値を取得します。</summary>
		public int ErrorCode { get; private set; }

		/// <summary>エラーが発生したシンボルドキュメント名を取得します。</summary>
		public string SymbolDocumentName { get { return SourcePath; } }

		/// <summary>エラーが発生したソースコードの行を取得します。</summary>
		public string CodeLine { get; private set; }
	}
}
