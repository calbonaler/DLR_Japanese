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
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting;
using System.Security.Permissions;
using System.Text;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting
{
	/// <summary>スクリプトに対する翻訳入力単位を表します。<see cref="Microsoft.Scripting.SourceUnit"/> に対するもう 1 つのホスティング API です。</summary>
	[DebuggerDisplay("{Path ?? \"<anonymous>\"}")]
	public sealed class ScriptSource : MarshalByRefObject
	{
		/// <summary>この翻訳入力単位の基となる <see cref="Microsoft.Scripting.SourceUnit"/> を取得します。</summary>
		internal SourceUnit SourceUnit { get; private set; }

		/// <summary>この翻訳入力単位を識別するパスを取得します。</summary>
		public string Path { get { return SourceUnit.Path; } }

		/// <summary>ソースコードの種類を取得します。</summary>
		public SourceCodeKind Kind { get { return SourceUnit.Kind; } }

		/// <summary>この翻訳入力単位に関連付けられている言語に対するエンジンを取得します。</summary>
		public ScriptEngine Engine { get; private set; }

		/// <summary>
		/// 指定されたエンジンおよび基となる <see cref="Microsoft.Scripting.SourceUnit"/>
		/// を使用して、<see cref="Microsoft.Scripting.Hosting.ScriptSource"/> クラスの新しいインスタンスを取得します。
		/// </summary>
		/// <param name="engine">このインスタンスに関連付ける言語に対するエンジンを指定します。</param>
		/// <param name="sourceUnit">このインスタンスの基となる <see cref="Microsoft.Scripting.SourceUnit"/> を指定します。</param>
		internal ScriptSource(ScriptEngine engine, SourceUnit sourceUnit)
		{
			Assert.NotNull(engine, sourceUnit);
			SourceUnit = sourceUnit;
			Engine = engine;
		}

		#region Compilation and Execution

		/// <summary>
		/// この <see cref="ScriptSource"/> を既定のスコープまたは他のスコープで再コンパイルの必要なしに繰り返し実行可能な
		/// <see cref="CompiledCode"/> オブジェクトにコンパイルします。
		/// </summary>
		/// <returns>コンパイルされたコードを表す <see cref="CompiledCode"/>。エラーによってパーサーがコードをコンパイルできない場合は <c>null</c> になります。</returns>
		/// <exception cref="SyntaxErrorException">コードをコンパイルできませんでした。</exception>
		public CompiledCode Compile() { return Compile(null, null); }

		/// <summary>
		/// この <see cref="ScriptSource"/> を既定のスコープまたは他のスコープで再コンパイルの必要なしに繰り返し実行可能な
		/// <see cref="CompiledCode"/> オブジェクトにコンパイルします。
		/// </summary>
		/// <param name="errorListener">エラーを報告する <see cref="ErrorListener"/> を指定します。</param>
		/// <returns>コンパイルされたコードを表す <see cref="CompiledCode"/>。エラーによってパーサーがコードをコンパイルできない場合は <c>null</c> になります。</returns>
		/// <exception cref="SyntaxErrorException">コードをコンパイルできませんでした。</exception>>
		public CompiledCode Compile(ErrorListener errorListener) { return Compile(null, errorListener); }

		/// <summary>
		/// この <see cref="ScriptSource"/> を既定のスコープまたは他のスコープで再コンパイルの必要なしに繰り返し実行可能な
		/// <see cref="CompiledCode"/> オブジェクトにコンパイルします。
		/// </summary>
		/// <param name="compilerOptions">コンパイル時に使用するオプションを指定します。</param>
		/// <returns>コンパイルされたコードを表す <see cref="CompiledCode"/>。エラーによってパーサーがコードをコンパイルできない場合は <c>null</c> になります。</returns>
		/// <exception cref="SyntaxErrorException">コードをコンパイルできませんでした。</exception>
		public CompiledCode Compile(CompilerOptions compilerOptions) { return Compile(compilerOptions, null); }

		/// <summary>
		/// この <see cref="ScriptSource"/> を既定のスコープまたは他のスコープで再コンパイルの必要なしに繰り返し実行可能な
		/// <see cref="CompiledCode"/> オブジェクトにコンパイルします。
		/// </summary>
		/// <param name="compilerOptions">コンパイル時に使用するオプションを指定します。</param>
		/// <param name="errorListener">エラーを報告する <see cref="ErrorListener"/> を指定します。</param>
		/// <returns>コンパイルされたコードを表す <see cref="CompiledCode"/>。エラーによってパーサーがコードをコンパイルできない場合は <c>null</c> になります。</returns>
		/// <exception cref="SyntaxErrorException">コードをコンパイルできませんでした。</exception>
		public CompiledCode Compile(CompilerOptions compilerOptions, ErrorListener errorListener)
		{
			var errorSink = new ErrorListenerProxySink(this, errorListener);
			var code = compilerOptions != null ? SourceUnit.Compile(compilerOptions, errorSink) : SourceUnit.Compile(errorSink);
			return code != null ? new CompiledCode(Engine, code) : null;
		}

		/// <summary>コードを指定したスコープで実行し、結果を返します。</summary>
		/// <param name="scope">コードを実行するスコープを指定します。</param>
		/// <exception cref="SyntaxErrorException">コードをコンパイルできません。</exception>
		public dynamic Execute(ScriptScope scope)
		{
			ContractUtils.RequiresNotNull(scope, "scope");
			return SourceUnit.Execute(scope.Scope);
		}

		/// <summary>コードを実行し、結果を返します。実行はどのスコープにも関連付けられません。</summary>
		/// <remarks>
		/// ホストがスコープを必要としないので、ここでは作成しません。
		/// 言語はコードが DLR スコープに関連付けられていないとして扱い、グローバル検索のセマンティクスを適宜変更する可能性があります。
		/// </remarks>
		public dynamic Execute() { return SourceUnit.Execute(); }

		/// <summary>コードを指定されたスコープで実行し、結果を指定された型に変換します。変換は言語によって定義されます。</summary>
		/// <param name="scope">コードを実行するスコープを指定します。</param>
		public T Execute<T>(ScriptScope scope) { return Engine.Operations.ConvertTo<T>((object)Execute(scope)); }

		/// <summary>コードを空のスコープで実行し、結果を指定された型に変換します。変換は言語によって定義されます。</summary>
		public T Execute<T>() { return Engine.Operations.ConvertTo<T>((object)Execute()); }

		/// <summary>コードを指定されたスコープで実行し、結果を <see cref="ObjectHandle"/> でラップします。</summary>
		/// <param name="scope">コードを実行するスコープを指定します。</param>
		public ObjectHandle ExecuteAndWrap(ScriptScope scope) { return new ObjectHandle((object)Execute(scope)); }

		/// <summary>コードを空のスコープで実行し、結果を <see cref="ObjectHandle"/> でラップします。</summary>
		public ObjectHandle ExecuteAndWrap() { return new ObjectHandle((object)Execute()); }

		/// <summary>コードを OS のコマンドシェルから開始されたプログラムであるように実行し、コード実行の成功またはエラー状態を示すプロセス終了コードを返します。</summary>
		/// <exception cref="SyntaxErrorException">コードがコンパイルできません。</exception>
		/// <remarks>
		/// 正確な動作は言語に依存します。終了コードを伝達する "exit" 例外がある言語も存在し、その場合例外は捕捉され終了コードが返されます。
		/// 既定の動作では言語特有の変換を使用して、整数に変換されたプログラムの実行結果を返します。
		/// </remarks>
		public int ExecuteProgram() { return SourceUnit.LanguageContext.ExecuteProgram(SourceUnit); }

		#endregion

		/// <summary>ソースコードを解析することにより、ソースコードの状態を取得します。</summary>
		public ScriptCodeParseResult FetchCodeProperties() { return SourceUnit.FetchCodeProperties(); }

		/// <summary>ソースコードを解析することにより、ソースコードの状態を取得します。</summary>
		/// <param name="options">解析に使用する <see cref="Microsoft.Scripting.CompilerOptions"/> を指定します。</param>
		public ScriptCodeParseResult FetchCodeProperties(CompilerOptions options) { return SourceUnit.FetchCodeProperties(options); }

		/// <summary>ソースコードを読み取る新しい <see cref="System.IO.TextReader"/> を返します。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public SourceCodeReader GetReader() { return SourceUnit.GetReader(); }

		/// <summary>コンテンツのエンコーディングを判定します。</summary>
		/// <returns>
		/// コンテンツを Unicode テキストにデコードするために、スクリプトの翻訳入力のリーダーによって使用されるエンコーディング。
		/// コンテンツが既にテキストで、デコードが行われていない場合は <c>null</c>。
		/// </returns>
		/// <remarks>
		/// スクリプトの翻訳入力が作成されたとき、指定された既定のエンコーディングはコンテンツプリアンブル (Unicode BOM または言語特有のエンコーディングプリアンブル)
		/// で見つかったエンコーディングに上書きされる可能性があります。その場合、プリアンブルエンコーディングが返されます。
		/// それ以外の場合は既定のエンコーディングが返されます。
		/// </remarks>
		/// <exception cref="IOException">I/O エラーが発生しました。</exception>
		public Encoding DetectEncoding()
		{
			using (var reader = SourceUnit.GetReader())
				return reader.Encoding;
		}

		/// <summary>翻訳入力単位から指定された範囲の行を読み取ります。</summary>
		/// <param name="start">取得する行の 1 から始まるインデックスを指定します。</param>
		/// <param name="count">取得する行数を指定します。</param>
		/// <returns>読み取られた各行を格納する <see cref="System.String"/> 型の配列。</returns>
		/// <exception cref="IOException">I/O エラーが発生しました。</exception>
		/// <remarks>どの文字列が改行記号と認識されるかは言語によります。言語が指定されていない場合、"\r", "\n", "\r\n" が改行記号と認識されます。</remarks>
		public string[] GetCodeLines(int start, int count) { return SourceUnit.GetCodeLines(start, count); }

		/// <summary>翻訳入力単位から指定された行を読み取ります。</summary>
		/// <param name="line">取得する行の 1 から始まるインデックスを指定します。</param>
		/// <returns>行の内容。改行文字は含まれません。</returns>
		/// <exception cref="IOException">I/O エラーが発生しました。</exception>
		/// <remarks>どの文字列が改行記号と認識されるかは言語によります。言語が指定されていない場合、"\r", "\n", "\r\n" が改行記号と認識されます。</remarks>
		public string GetCodeLine(int line) { return SourceUnit.GetCodeLine(line); }

		/// <summary>スクリプトの翻訳入力の内容を取得します。</summary>
		/// <returns>コンテンツ全体。</returns>
		/// <exception cref="IOException">I/O エラーが発生しました。</exception>
		/// <remarks>
		/// 結果には言語固有のプリアンブル (たとえば、 "#coding:UTF-8" は Ruby ではエンコーディングプリアンブルとして認識されます。) が含まれますが、
		/// コンテンツエンコーディングで指定されたプリアンブル (例: BOM) は含まれません。
		/// 翻訳入力単位の内容全体は単一のエンコーディングによってエンコードされます。(もし、バイナリストリームから読み取られた場合)
		/// </remarks>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public string GetCode() { return SourceUnit.GetCode(); }

		// TODO: can this be removed? no one uses it
		#region line number mapping

		/// <summary>指定された <see cref="SourceSpan"/> から実際のソースコード範囲を表す <see cref="SourceSpan"/> を返します。</summary>
		/// <param name="span"><see cref="SourceSpan"/> を指定します。</param>
		public SourceSpan MapSpan(SourceSpan span) { return new SourceSpan(MapLocation(span.Start), MapLocation(span.End)); }

		/// <summary>指定された <see cref="SourceLocation"/> から実際のソースコード上の位置を表す <see cref="SourceLocation"/> を返します。</summary>
		/// <param name="location"><see cref="SourceLocation"/> を指定します。</param>
		public SourceLocation MapLocation(SourceLocation location) { return SourceUnit.MapLocation(location); }

		#endregion

		// TODO: Figure out what is the right lifetime
		/// <summary>対象のインスタンスの有効期間ポリシーを制御する、有効期間サービス オブジェクトを取得します。</summary>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
		public override object InitializeLifetimeService() { return null; }
	}
}
