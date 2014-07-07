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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting
{
	/// <summary>翻訳入力単位を表します。</summary>
	[DebuggerDisplay("{Path ?? \"<anonymous>\"}")]
	public sealed class SourceUnit
	{
		TextContentProvider _contentProvider;

		// SourceUnit is serializable => updated parse result is transmitted back to the host unless the unit is passed by-ref
		KeyValuePair<int, int>[] _lineMap;

		/// <summary>翻訳入力単位を識別するホストにより設定される値を取得します。</summary>
		public string Path { get; private set; }

		/// <summary>この翻訳入力単位を識別する値が存在するかどうかを示す値を取得します。</summary>
		public bool HasPath { get { return Path != null; } }

		/// <summary>この翻訳入力単位によって保持されるソースコードの種類を示す値を取得します。</summary>
		public SourceCodeKind Kind { get; private set; }

		// Path is valid to be null. In that case we cannot create a valid SymbolDocumentInfo.
		/// <summary>この翻訳入力単位を説明する <see cref="System.Linq.Expressions.SymbolDocumentInfo"/> を取得します。</summary>
		public SymbolDocumentInfo Document { get { return Path == null ? null : Expression.SymbolDocument(Path, LanguageContext.LanguageGuid, LanguageContext.VendorGuid); } }

		/// <summary>この翻訳入力単位の言語を表す <see cref="LanguageContext"/> を取得します。</summary>
		public LanguageContext LanguageContext { get; private set; }

		/// <summary>ソースコードを解析することにより、ソースコードの状態を取得します。</summary>
		public ScriptCodeParseResult FetchCodeProperties() { return FetchCodeProperties(LanguageContext.GetCompilerOptions()); }

		/// <summary>ソースコードを解析することにより、ソースコードの状態を取得します。</summary>
		/// <param name="options">解析に使用する <see cref="Microsoft.Scripting.CompilerOptions"/> を指定します。</param>
		public ScriptCodeParseResult FetchCodeProperties(CompilerOptions options)
		{
			ContractUtils.RequiresNotNull(options, "options");
			Compile(options, ErrorSink.Null);
			return CodeProperties ?? ScriptCodeParseResult.Complete;
		}

		/// <summary>ソースコードの状態を示す値を取得または設定します。</summary>
		public ScriptCodeParseResult? CodeProperties { get; set; }

		/// <summary>言語、ソースコード、パス、ソースコードの種類を使用して、<see cref="Microsoft.Scripting.SourceUnit"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="context">この翻訳入力単位が保持するソースコードの言語を表す <see cref="LanguageContext"/> を指定します。</param>
		/// <param name="contentProvider">この翻訳入力単位が保持するソースコードを提供する <see cref="TextContentProvider"/> を指定します。</param>
		/// <param name="path">この翻訳入力単位を識別する文字列を指定します。</param>
		/// <param name="kind">この翻訳入力単位が保持するソースコードの種類を指定します。</param>
		public SourceUnit(LanguageContext context, TextContentProvider contentProvider, string path, SourceCodeKind kind)
		{
			ContractUtils.RequiresNotNull(context, "context");
			ContractUtils.RequiresNotNull(contentProvider, "contentProvider");
			ContractUtils.Requires(context.CanCreateSourceCode, "context");
			LanguageContext = context;
			_contentProvider = contentProvider;
			Kind = kind;
			Path = path;
		}

		/// <summary>ソースコードを読み取る新しい <see cref="System.IO.TextReader"/> を返します。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public SourceCodeReader GetReader() { return _contentProvider.GetReader(); }

		/// <summary>翻訳入力単位から指定された範囲の行を読み取ります。</summary>
		/// <param name="start">取得する行の 1 から始まるインデックスを指定します。</param>
		/// <param name="count">取得する行数を指定します。</param>
		/// <returns>読み取られた各行を格納する <see cref="System.String"/> 型の配列。</returns>
		/// <exception cref="System.IO.IOException">I/O エラーが発生しました。</exception>
		public string[] GetCodeLines(int start, int count)
		{
			ContractUtils.Requires(start > 0, "start");
			ContractUtils.Requires(count > 0, "count");
			List<string> result = new List<string>(count);
			using (var reader = GetReader())
			{
				string line;
				for (reader.SeekLine(start); count > 0 && (line = reader.ReadLine()) != null; count--)
					result.Add(line);
			}
			return result.ToArray();
		}

		/// <summary>翻訳入力単位から指定された行を読み取ります。</summary>
		/// <param name="line">取得する行の 1 から始まるインデックスを指定します。</param>
		/// <returns>行の内容。改行文字は含まれません。</returns>
		/// <exception cref="System.IO.IOException">I/O エラーが発生しました。</exception>
		public string GetCodeLine(int line) { return GetCodeLines(line, 1).FirstOrDefault(); }

		/// <summary>スクリプトの翻訳入力の内容を取得します。</summary>
		/// <returns>コンテンツ全体。</returns>
		/// <exception cref="System.IO.IOException">I/O エラーが発生しました。</exception>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public string GetCode()
		{
			using (var reader = GetReader())
				return reader.ReadToEnd();
		}

		#region Line/File mapping

		/// <summary>指定された <see cref="SourceLocation"/> から実際のソースコード上の位置を表す <see cref="SourceLocation"/> を返します。</summary>
		/// <param name="loc"><see cref="SourceLocation"/> を指定します。</param>
		public SourceLocation MapLocation(SourceLocation loc) { return new SourceLocation(loc.Index, MapLine(loc.Line), loc.Column); }

		int MapLine(int line)
		{
			if (_lineMap != null)
			{
				int match = BinarySearch(_lineMap, line);
				line += _lineMap[match].Value - _lineMap[match].Key;
				if (line < 1)
					line = 1; // this is the minimum value
			}
			return line;
		}

		static int BinarySearch(KeyValuePair<int, int>[] array, int line)
		{
			int match = Array.BinarySearch(array, new KeyValuePair<int, int>(line, 0), Comparer<KeyValuePair<int, int>>.Create((x, y) => x.Key - y.Key));
			if (match < 0)
			{
				// If we couldn't find an exact match for this line number, get the nearest matching line number less than this one
				match = ~match - 1;
				// If our index = -1, it means that this line is before any line numbers that we know about. If that's the case, use the first entry in the list
				if (match == -1)
					match = 0;
			}
			return match;
		}

		#endregion

		#region Parsing, Compilation, Execution

		/// <summary>この翻訳入力単位でのコンパイルがデバッグシンボルを出力可能かどうかを示す値を取得します。</summary>
		public bool EmitDebugSymbols { get { return HasPath && LanguageContext.DomainManager.Configuration.DebugMode; } }

		/// <summary>この翻訳入力単位を<see cref="ScriptCode"/> オブジェクトにコンパイルします。</summary>
		/// <exception cref="SyntaxErrorException">コードをコンパイルできませんでした。</exception>
		/// <returns>コンパイルされたコードを表す <see cref="ScriptCode"/>。エラーによってパーサーがコードをコンパイルできない場合は <c>null</c> になります。</returns>
		public ScriptCode Compile() { return Compile(ErrorSink.Default); }

		/// <summary>この翻訳入力単位を<see cref="ScriptCode"/> オブジェクトにコンパイルします。</summary>
		/// <param name="errorSink">エラーが報告される <see cref="ErrorSink"/> を指定します。</param>
		/// <exception cref="SyntaxErrorException">コードをコンパイルできませんでした。</exception>
		/// <returns>コンパイルされたコードを表す <see cref="ScriptCode"/>。エラーによってパーサーがコードをコンパイルできない場合は <c>null</c> になります。</returns>
		public ScriptCode Compile(ErrorSink errorSink) { return Compile(LanguageContext.GetCompilerOptions(), errorSink); }

		/// <summary>この翻訳入力単位を<see cref="ScriptCode"/> オブジェクトにコンパイルします。</summary>
		/// <param name="options">コンパイル時に使用するオプションを指定します。</param>
		/// <param name="errorSink">エラーが報告される <see cref="ErrorSink"/> を指定します。</param>
		/// <exception cref="SyntaxErrorException">コードをコンパイルできませんでした。</exception>
		/// <returns>コンパイルされたコードを表す <see cref="ScriptCode"/>。エラーによってパーサーがコードをコンパイルできない場合は <c>null</c> になります。</returns>
		public ScriptCode Compile(CompilerOptions options, ErrorSink errorSink)
		{
			ContractUtils.RequiresNotNull(errorSink, "errorSink");
			ContractUtils.RequiresNotNull(options, "options");
			return LanguageContext.CompileSourceCode(this, options, errorSink);
		}

		/// <summary>コードを指定したスコープで実行し、結果を返します。</summary>
		/// <param name="scope">コードを実行するスコープを指定します。</param>
		/// <exception cref="SyntaxErrorException">コードをコンパイルできません。</exception>
		public object Execute(Scope scope) { return Execute(scope, ErrorSink.Default); }

		/// <summary>コードを指定したスコープで実行し、結果を返します。エラーは指定された <see cref="ErrorSink"/> に報告されます。</summary>
		/// <param name="scope">コードを実行するスコープを指定します。</param>
		/// <param name="errorSink">エラーが報告される <see cref="ErrorSink"/> を指定します。</param>
		/// <exception cref="SyntaxErrorException">コードをコンパイルできません。</exception>
		public object Execute(Scope scope, ErrorSink errorSink)
		{
			ContractUtils.RequiresNotNull(scope, "scope");
			ScriptCode compiledCode = Compile(LanguageContext.GetCompilerOptions(scope), errorSink);
			if (compiledCode == null)
				throw new SyntaxErrorException();
			return compiledCode.Run(scope);
		}

		/// <summary>コードを言語によって作成された新しいスコープで実行し、結果を返します。</summary>
		/// <exception cref="SyntaxErrorException">コードをコンパイルできません。</exception>
		public object Execute() { return Compile().Run(); }

		/// <summary>コードを言語によって作成された新しいスコープで実行し、結果を返します。エラーは指定された <see cref="ErrorSink"/> に報告されます。</summary>
		/// <param name="errorSink">エラーが報告される <see cref="ErrorSink"/> を指定します。</param>
		/// <exception cref="SyntaxErrorException">コードをコンパイルできません。</exception>
		public object Execute(ErrorSink errorSink) { return Compile(errorSink).Run(); }

		/// <summary>コードを言語によって作成された新しいスコープで実行し、結果を返します。エラーは指定された <see cref="ErrorSink"/> に報告されます。</summary>
		/// <param name="options">コンパイル時に使用するオプションを指定します。</param>
		/// <param name="errorSink">エラーが報告される <see cref="ErrorSink"/> を指定します。</param>
		/// <exception cref="SyntaxErrorException">コードをコンパイルできません。</exception>
		public object Execute(CompilerOptions options, ErrorSink errorSink) { return Compile(options, errorSink).Run(); }

		/// <summary>コードを OS のコマンドシェルから開始されたプログラムであるように実行し、コード実行の成功またはエラー状態を示すプロセス終了コードを返します。</summary>
		/// <exception cref="SyntaxErrorException">コードがコンパイルできません。</exception>
		public int ExecuteProgram() { return LanguageContext.ExecuteProgram(this); }

		#endregion

		/// <summary>この翻訳入力単位に行のマッピングを設定します。</summary>
		/// <param name="lineMap">行のマッピングを表す配列を指定します。</param>
		public void SetLineMapping(KeyValuePair<int, int>[] lineMap) { _lineMap = lineMap == null || lineMap.Length == 0 ? null : lineMap; }
	}
}
