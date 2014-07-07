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
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Runtime.Remoting;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting
{
	/// <summary>ホスティング API における言語を表します。<see cref="Microsoft.Scripting.Runtime.LanguageContext"/> に対するもう 1 つのホスティング API です。</summary>
	[DebuggerDisplay("{Setup.DisplayName}")]
	public sealed class ScriptEngine : MarshalByRefObject
	{
		LanguageSetup _config;
		ObjectOperations _operations;

		/// <summary>
		/// 指定されたランタイムおよび <see cref="Microsoft.Scripting.Runtime.LanguageContext"/> を使用して、
		/// <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> クラスの新しいインスタンスを初期化します。
		/// </summary>
		/// <param name="runtime">このエンジンに関連付けるランタイムを指定します。</param>
		/// <param name="context">このエンジンの基になる <see cref="Microsoft.Scripting.Runtime.LanguageContext"/> を指定します。</param>
		internal ScriptEngine(ScriptRuntime runtime, LanguageContext context)
		{
			Debug.Assert(runtime != null);
			Debug.Assert(context != null);
			Runtime = runtime;
			LanguageContext = context;
		}

		#region Object Operations

		/// <summary>エンジンに対する既定の <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> オブジェクトを取得します。</summary>
		/// <remarks>
		/// <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> オブジェクトはオブジェクトの型に対する規則や処理した操作をキャッシュするため、
		/// 既定の <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> に対する複数のオブジェクトの使用はキャッシュ効率を低下させます。
		/// やがて、いくつかの操作に対するキャッシュは <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> はキャッシュを停止させるまでに性能を低下させ、
		/// 指定されたオブジェクトに対する要求された操作の実装を全探索するようになります。
		/// 
		/// 新しい <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> インスタンスを作成するもう 1 つの理由は、
		/// インスタンスに <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> の特定の機能を関連付けるということです。
		/// 言語は言語ごとの振る舞いを操作がどのように実行されるのかを変更できる <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> に委譲することができます。
		/// 
		/// 単純なホスティングにおいては、これは十分な振る舞いとなります。
		/// </remarks>
		public ObjectOperations Operations
		{
			get
			{
				if (_operations == null)
					Interlocked.CompareExchange(ref _operations, CreateOperations(), null);
				return _operations;
			}
		}

		/// <summary>新しい <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> オブジェクトを作成します。</summary>
		public ObjectOperations CreateOperations() { return new ObjectOperations(new DynamicOperations(LanguageContext), this); }

		/// <summary>
		/// 指定された <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> に特有のあらゆるセマンティクスを継承する
		/// 新しい <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> オブジェクトを作成します。
		/// </summary>
		/// <param name="scope">作成する <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> の基となる <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> オブジェクトを指定します。</param>
		public ObjectOperations CreateOperations(ScriptScope scope)
		{
			ContractUtils.RequiresNotNull(scope, "scope");
			return new ObjectOperations(LanguageContext.Operations, this);
		}

		#endregion

		#region Code Execution (for convenience)

		/// <summary>式を実行します。実行は特にどのスコープにも関連付けられません。</summary>
		/// <param name="expression">実行する式を指定します。</param>
		/// <exception cref="NotSupportedException">エンジンはコードの実行をサポートしていません。</exception>
		/// <exception cref="ArgumentNullException"><paramref name="expression"/> は <c>null</c> 参照です。</exception>
		public dynamic Execute(string expression)
		{
			// ホストはスコープを必要としていないので、ここでは作成しない。
			// 言語はコードをどの DLR スコープにも関連付けられていないとして扱うため、適宜グローバル検索のセマンティクスを変更します。
			return CreateScriptSourceFromString(expression).Execute();
		}

		/// <summary>指定されたスコープで式を実行します。</summary>
		/// <param name="expression">実行する式を指定します。</param>
		/// <param name="scope">式を実行するスコープを指定します。</param>
		/// <exception cref="NotSupportedException">エンジンはコードの実行をサポートしていません。</exception>
		/// <exception cref="ArgumentNullException"><paramref name="expression"/> は <c>null</c> 参照です。</exception>
		/// <exception cref="ArgumentNullException"><paramref name="scope"/> は <c>null</c> 参照です。</exception>
		public dynamic Execute(string expression, ScriptScope scope) { return CreateScriptSourceFromString(expression).Execute(scope); }

		/// <summary>式を新しいスコープで実行し、結果を指定された型に変換します。</summary>
		/// <param name="expression">実行する式を指定します。</param>
		/// <exception cref="NotSupportedException">エンジンはコードの実行をサポートしていません。</exception>
		/// <exception cref="ArgumentNullException"><paramref name="expression"/> は <c>null</c> 参照です。</exception>
		public T Execute<T>(string expression) { return Operations.ConvertTo<T>((object)Execute(expression)); }

		/// <summary>式を指定されたスコープで実行し、結果を指定された型に変換します。</summary>
		/// <param name="expression">実行する式を指定します。</param>
		/// <param name="scope">式を実行するスコープを指定します。</param>
		/// <exception cref="NotSupportedException">エンジンはコードの実行をサポートしていません。</exception>
		/// <exception cref="ArgumentNullException"><paramref name="expression"/> は <c>null</c> 参照です。</exception>
		/// <exception cref="ArgumentNullException"><paramref name="scope"/> は <c>null</c> 参照です。</exception>
		public T Execute<T>(string expression, ScriptScope scope) { return Operations.ConvertTo<T>((object)Execute(expression, scope)); }

		/// <summary>指定されたファイルの内容を新しいスコープで実行し、そのスコープを返します。</summary>
		/// <param name="path">実行するファイルのパスを指定します。</param>
		/// <exception cref="NotSupportedException">エンジンはコードの実行をサポートしていません。</exception>
		/// <exception cref="ArgumentNullException"><paramref name="path"/> は <c>null</c> 参照です。</exception>
		public ScriptScope ExecuteFile(string path) { return ExecuteFile(path, CreateScope()); }

		/// <summary>指定されたファイルの内容を指定されたスコープで実行します。</summary>
		/// <param name="path">実行するファイルのパスを指定します。</param>
		/// <param name="scope">ファイルの内容を実行するスコープを指定します。</param>
		/// <exception cref="NotSupportedException">エンジンはコードの実行をサポートしていません。</exception>
		/// <exception cref="ArgumentNullException"><paramref name="path"/> は <c>null</c> 参照です。</exception>
		/// <exception cref="ArgumentNullException"><paramref name="scope"/> は <c>null</c> 参照です。</exception>
		public ScriptScope ExecuteFile(string path, ScriptScope scope)
		{
			CreateScriptSourceFromFile(path).Execute(scope);
			return scope;
		}

		/// <summary>指定されたスコープで式を実行し、結果を <see cref="System.Runtime.Remoting.ObjectHandle"/> でラップして返します。</summary>
		/// <param name="expression">実行する式を指定します。</param>
		/// <param name="scope">式を実行するスコープを指定します。</param>
		public ObjectHandle ExecuteAndWrap(string expression, ScriptScope scope) { return new ObjectHandle((object)Execute(expression, scope)); }

		/// <summary>空のスコープで式を実行し、結果を <see cref="System.Runtime.Remoting.ObjectHandle"/> でラップして返します。</summary>
		/// <param name="expression">実行する式を指定します。</param>
		public ObjectHandle ExecuteAndWrap(string expression) { return new ObjectHandle((object)Execute(expression)); }

		/// <summary>
		/// 指定されたスコープで式を実行し、結果を <see cref="System.Runtime.Remoting.ObjectHandle"/> でラップして返します。
		/// 例外が発生した場合は例外を捕捉し、その <see cref="System.Runtime.Remoting.ObjectHandle"/> が提供されます。
		/// </summary>
		/// <param name="expression">実行する式を指定します。</param>
		/// <param name="scope">式を実行するスコープを指定します。</param>
		/// <param name="exception">発生した例外に対する <see cref="System.Runtime.Remoting.ObjectHandle"/> が格納される変数を指定します。</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public ObjectHandle ExecuteAndWrap(string expression, ScriptScope scope, out ObjectHandle exception)
		{
			exception = null;
			try { return new ObjectHandle((object)Execute(expression, scope)); }
			catch (Exception e)
			{
				exception = new ObjectHandle(e);
				return null;
			}
		}

		/// <summary>
		/// 空のスコープで式を実行し、結果を <see cref="System.Runtime.Remoting.ObjectHandle"/> でラップして返します。
		/// 例外が発生した場合は例外を捕捉し、その <see cref="System.Runtime.Remoting.ObjectHandle"/> が提供されます。
		/// </summary>
		/// <param name="expression">実行する式を指定します。</param>
		/// <param name="exception">発生した例外に対する <see cref="System.Runtime.Remoting.ObjectHandle"/> が格納される変数を指定します。</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public ObjectHandle ExecuteAndWrap(string expression, out ObjectHandle exception)
		{
			exception = null;
			try { return new ObjectHandle((object)Execute(expression)); }
			catch (Exception e)
			{
				exception = new ObjectHandle(e);
				return null;
			}
		}

		#endregion

		#region Scopes

		/// <summary>新しい空の <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> を作成します。</summary>
		public ScriptScope CreateScope() { return new ScriptScope(this, new Scope()); }

		[Obsolete("IAttributesCollection is obsolete, use CreateScope(IDynamicMetaObjectProvider) instead")]
		public ScriptScope CreateScope(IAttributesCollection dictionary)
		{
			ContractUtils.RequiresNotNull(dictionary, "dictionary");
			return new ScriptScope(this, new Scope(dictionary));
		}

		/// <summary>
		/// ストレージとして任意のオブジェクトを用いる新しい <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> を作成します。
		/// <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> に対するアクセスはオブジェクトに対するメンバの取得、設定、削除になります。
		/// </summary>
		/// <param name="storage"><see cref="Microsoft.Scripting.Hosting.ScriptScope"/> のストレージとなるオブジェクトを指定します。</param>
		public ScriptScope CreateScope(IDynamicMetaObjectProvider storage)
		{
			ContractUtils.RequiresNotNull(storage, "storage");
			return new ScriptScope(this, new Scope(storage));
		}

		/// <summary>
		/// 指定されたパスに対する <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> が実行された
		/// <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> を取得します。
		/// </summary>
		/// <remarks>
		/// <see cref="Microsoft.Scripting.Hosting.ScriptSource.Path"/> プロパティは
		/// <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> の検索のキーとなります。
		/// ホストは <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> を作成し
		/// <see cref="Microsoft.Scripting.Hosting.ScriptSource.Path"/> プロパティを適切に設定することを確認する必要があります。
		/// 
		/// <see cref="GetScope"/> はファイルとその実行スコープをマッピングする必要があるようなツールにとって非常に役に立ちます。
		/// たとえば、エディタやインタプリタといったツールはファイル Bar をインポートしたり必要としたりしているファイル Foo を実行する可能性があります。
		/// 
		/// エディタのユーザーは後にファイル Bar を開き、そのコンテキスト内にある式を実行したいと思うかもしれません。
		/// ツールは Bar のインタプリタウィンドウ内の適切なコンテキストを設定することで、
		/// <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> を見つける必要があるでしょう。
		/// このメソッドはこのようなシナリオに対して有効となります。
		/// </remarks>
		public ScriptScope GetScope(string path)
		{
			ContractUtils.RequiresNotNull(path, "path");
			Scope scope = LanguageContext.GetScope(path);
			return scope != null ? new ScriptScope(this, scope) : null;
		}

		#endregion

		#region Source Unit Creation

		/// <summary>言語バインディングとして現在のエンジンを使用して、文字列から <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトを作成します。</summary>
		/// <param name="expression">作成する <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトの基になる文字列を指定します。</param>
		public ScriptSource CreateScriptSourceFromString(string expression)
		{
			ContractUtils.RequiresNotNull(expression, "expression");
			return CreateScriptSource(new SourceStringContentProvider(expression), null, SourceCodeKind.AutoDetect);
		}

		/// <summary>言語バインディングとして現在のエンジンを使用して、文字列から <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトを作成します。</summary>
		/// <param name="code">作成する <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトの基になる文字列を指定します。</param>
		/// <param name="kind">ソースコードの種類を示す <see cref="Microsoft.Scripting.SourceCodeKind"/> を指定します。</param>
		public ScriptSource CreateScriptSourceFromString(string code, SourceCodeKind kind)
		{
			ContractUtils.RequiresNotNull(code, "code");
			ContractUtils.Requires(kind.IsValid(), "kind");
			return CreateScriptSource(new SourceStringContentProvider(code), null, kind);
		}

		/// <summary>言語バインディングとして現在のエンジンを使用して、文字列から <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトを作成します。</summary>
		/// <param name="expression">作成する <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトの基になる文字列を指定します。</param>
		/// <param name="path">ソースコードのパスを指定します。</param>
		public ScriptSource CreateScriptSourceFromString(string expression, string path)
		{
			ContractUtils.RequiresNotNull(expression, "expression");
			return CreateScriptSource(new SourceStringContentProvider(expression), path, SourceCodeKind.AutoDetect);
		}

		/// <summary>言語バインディングとして現在のエンジンを使用して、文字列から <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトを作成します。</summary>
		/// <param name="code">作成する <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトの基になる文字列を指定します。</param>
		/// <param name="path">ソースコードのパスを指定します。</param>
		/// <param name="kind">ソースコードの種類を示す <see cref="Microsoft.Scripting.SourceCodeKind"/> を指定します。</param>
		public ScriptSource CreateScriptSourceFromString(string code, string path, SourceCodeKind kind)
		{
			ContractUtils.RequiresNotNull(code, "code");
			ContractUtils.Requires(kind.IsValid(), "kind");
			return CreateScriptSource(new SourceStringContentProvider(code), path, kind);
		}

		/// <summary>言語バインディングとして現在のエンジンを使用して、ファイルから <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトを作成します。</summary>
		/// <param name="path"><see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトの基になるファイルを示すパスを指定します。</param>
		/// <remarks>
		/// パスの拡張子は <see cref="Microsoft.Scripting.Hosting.ScriptRuntime.GetEngineByFileExtension"/> でこの言語エンジンに関連付けられている必要はありません。
		/// </remarks>
		public ScriptSource CreateScriptSourceFromFile(string path) { return CreateScriptSourceFromFile(path, Encoding.Default, SourceCodeKind.File); }

		/// <summary>言語バインディングとして現在のエンジンを使用して、ファイルから <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトを作成します。</summary>
		/// <param name="path"><see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトの基になるファイルを示すパスを指定します。</param>
		/// <param name="encoding">ソースコードのエンコーディングを指定します。</param>
		/// <remarks>
		/// パスの拡張子は <see cref="Microsoft.Scripting.Hosting.ScriptRuntime.GetEngineByFileExtension"/> でこの言語エンジンに関連付けられている必要はありません。
		/// </remarks>
		public ScriptSource CreateScriptSourceFromFile(string path, Encoding encoding) { return CreateScriptSourceFromFile(path, encoding, SourceCodeKind.File); }

		/// <summary>言語バインディングとして現在のエンジンを使用して、ファイルから <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトを作成します。</summary>
		/// <param name="path"><see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトの基になるファイルを示すパスを指定します。</param>
		/// <param name="encoding">ソースコードのエンコーディングを指定します。</param>
		/// <param name="kind">ソースコードの種類を示す <see cref="Microsoft.Scripting.SourceCodeKind"/> を指定します。</param>
		/// <remarks>
		/// パスの拡張子は <see cref="Microsoft.Scripting.Hosting.ScriptRuntime.GetEngineByFileExtension"/> でこの言語エンジンに関連付けられている必要はありません。
		/// </remarks>
		public ScriptSource CreateScriptSourceFromFile(string path, Encoding encoding, SourceCodeKind kind)
		{
			ContractUtils.RequiresNotNull(path, "path");
			ContractUtils.RequiresNotNull(encoding, "encoding");
			ContractUtils.Requires(kind.IsValid(), "kind");
			if (!LanguageContext.CanCreateSourceCode)
				throw new NotSupportedException("Invariant engine cannot create scripts");
			return new ScriptSource(this, LanguageContext.CreateFileUnit(path, encoding, kind));
		}

		/// <summary>
		/// 言語バインディングとして現在のエンジンを使用して、<see cref="System.CodeDom.CodeObject"/> から
		/// <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトを作成します。
		/// </summary>
		/// <param name="content">作成する <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトの基になる <see cref="System.CodeDom.CodeObject"/> を指定します。</param>
		/// <remarks>
		/// このメソッドは構文独立のセマンティクス式に対して最小限の CodeDom サポートしか行いません。
		/// 言語はより多くのことを行えますが、ホストは <see cref="System.CodeDom.CodeMemberMethod"/> および、下記のサブノードのみを認めます。
		///     <see cref="System.CodeDom.CodeSnippetStatement"/>
		///     <see cref="System.CodeDom.CodeSnippetExpression"/>
		///     <see cref="System.CodeDom.CodePrimitiveExpression"/>
		///     <see cref="System.CodeDom.CodeMethodInvokeExpression"/>
		///     <see cref="System.CodeDom.CodeExpressionStatement"/> (MethodInvoke の保持のため)
		/// </remarks>
		public ScriptSource CreateScriptSource(CodeObject content) { return CreateScriptSource(content, null, SourceCodeKind.File); }

		/// <summary>
		/// 言語バインディングとして現在のエンジンを使用して、<see cref="System.CodeDom.CodeObject"/> から
		/// <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトを作成します。
		/// </summary>
		/// <param name="content">作成する <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトの基になる <see cref="System.CodeDom.CodeObject"/> を指定します。</param>
		/// <param name="path">作成する <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> に対して設定されるパスを指定します。</param>
		/// <remarks>
		/// このメソッドは構文独立のセマンティクス式に対して最小限の CodeDom サポートしか行いません。
		/// 言語はより多くのことを行えますが、ホストは <see cref="System.CodeDom.CodeMemberMethod"/> および、下記のサブノードのみを認めます。
		///     <see cref="System.CodeDom.CodeSnippetStatement"/>
		///     <see cref="System.CodeDom.CodeSnippetExpression"/>
		///     <see cref="System.CodeDom.CodePrimitiveExpression"/>
		///     <see cref="System.CodeDom.CodeMethodInvokeExpression"/>
		///     <see cref="System.CodeDom.CodeExpressionStatement"/> (MethodInvoke の保持のため)
		/// </remarks>
		public ScriptSource CreateScriptSource(CodeObject content, string path) { return CreateScriptSource(content, path, SourceCodeKind.File); }

		/// <summary>
		/// 言語バインディングとして現在のエンジンを使用して、<see cref="System.CodeDom.CodeObject"/> から
		/// <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトを作成します。
		/// </summary>
		/// <param name="content">作成する <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトの基になる <see cref="System.CodeDom.CodeObject"/> を指定します。</param>
		/// <param name="kind">ソースコードの種類を示す <see cref="Microsoft.Scripting.SourceCodeKind"/> を指定します。</param>
		/// <remarks>
		/// このメソッドは構文独立のセマンティクス式に対して最小限の CodeDom サポートしか行いません。
		/// 言語はより多くのことを行えますが、ホストは <see cref="System.CodeDom.CodeMemberMethod"/> および、下記のサブノードのみを認めます。
		///     <see cref="System.CodeDom.CodeSnippetStatement"/>
		///     <see cref="System.CodeDom.CodeSnippetExpression"/>
		///     <see cref="System.CodeDom.CodePrimitiveExpression"/>
		///     <see cref="System.CodeDom.CodeMethodInvokeExpression"/>
		///     <see cref="System.CodeDom.CodeExpressionStatement"/> (MethodInvoke の保持のため)
		/// </remarks>
		public ScriptSource CreateScriptSource(CodeObject content, SourceCodeKind kind) { return CreateScriptSource(content, null, kind); }

		/// <summary>
		/// 言語バインディングとして現在のエンジンを使用して、<see cref="System.CodeDom.CodeObject"/> から
		/// <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトを作成します。
		/// </summary>
		/// <param name="content">作成する <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトの基になる <see cref="System.CodeDom.CodeObject"/> を指定します。</param>
		/// <param name="path">作成する <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> に対して設定されるパスを指定します。</param>
		/// <param name="kind">ソースコードの種類を示す <see cref="Microsoft.Scripting.SourceCodeKind"/> を指定します。</param>
		/// <remarks>
		/// このメソッドは構文独立のセマンティクス式に対して最小限の CodeDom サポートしか行いません。
		/// 言語はより多くのことを行えますが、ホストは <see cref="System.CodeDom.CodeMemberMethod"/> および、下記のサブノードのみを認めます。
		///     <see cref="System.CodeDom.CodeSnippetStatement"/>
		///     <see cref="System.CodeDom.CodeSnippetExpression"/>
		///     <see cref="System.CodeDom.CodePrimitiveExpression"/>
		///     <see cref="System.CodeDom.CodeMethodInvokeExpression"/>
		///     <see cref="System.CodeDom.CodeExpressionStatement"/> (MethodInvoke の保持のため)
		/// </remarks>
		public ScriptSource CreateScriptSource(CodeObject content, string path, SourceCodeKind kind)
		{
			ContractUtils.RequiresNotNull(content, "content");
			if (!LanguageContext.CanCreateSourceCode)
				throw new NotSupportedException("Invariant engine cannot create scripts");
			return new ScriptSource(this, LanguageContext.GenerateSourceCode(content, path, kind));
		}

		/// <summary>言語バインディングとして現在のエンジンを使用して、ストリームから <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトを作成します。</summary>
		/// <param name="content">
		/// 作成する <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトの基になるストリームを保持している
		/// <see cref="Microsoft.Scripting.StreamContentProvider"/> を指定します。
		/// </param>
		/// <param name="path">作成する <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> に対して設定されるパスを指定します。</param>
		public ScriptSource CreateScriptSource(StreamContentProvider content, string path)
		{
			ContractUtils.RequiresNotNull(content, "content");
			return CreateScriptSource(content, path, Encoding.Default, SourceCodeKind.File);
		}

		/// <summary>言語バインディングとして現在のエンジンを使用して、ストリームから <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトを作成します。</summary>
		/// <param name="content">
		/// 作成する <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトの基になるストリームを保持している
		/// <see cref="Microsoft.Scripting.StreamContentProvider"/> を指定します。
		/// </param>
		/// <param name="path">作成する <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> に対して設定されるパスを指定します。</param>
		/// <param name="encoding">ソースコードのエンコーディングを指定します。</param>
		public ScriptSource CreateScriptSource(StreamContentProvider content, string path, Encoding encoding)
		{
			ContractUtils.RequiresNotNull(content, "content");
			ContractUtils.RequiresNotNull(encoding, "encoding");
			return CreateScriptSource(content, path, encoding, SourceCodeKind.File);
		}

		/// <summary>言語バインディングとして現在のエンジンを使用して、ストリームから <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトを作成します。</summary>
		/// <param name="content">
		/// 作成する <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトの基になるストリームを保持している
		/// <see cref="Microsoft.Scripting.StreamContentProvider"/> を指定します。
		/// </param>
		/// <param name="path">作成する <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> に対して設定されるパスを指定します。</param>
		/// <param name="encoding">ソースコードのエンコーディングを指定します。</param>
		/// <param name="kind">ソースコードの種類を示す <see cref="Microsoft.Scripting.SourceCodeKind"/> を指定します。</param>
		public ScriptSource CreateScriptSource(StreamContentProvider content, string path, Encoding encoding, SourceCodeKind kind)
		{
			ContractUtils.RequiresNotNull(content, "content");
			ContractUtils.RequiresNotNull(encoding, "encoding");
			ContractUtils.Requires(kind.IsValid(), "kind");
			return CreateScriptSource(new LanguageBoundTextContentProvider(LanguageContext, content, encoding, path), path, kind);
		}

		/// <summary>言語バインディングとして現在のエンジンを使用して、コンテンツプロバイダから <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトを作成します。</summary>
		/// <param name="contentProvider">
		/// 作成する <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトの基になる <see cref="Microsoft.Scripting.TextContentProvider"/> を指定します。
		/// </param>
		/// <param name="path">作成する <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> に対して設定されるパスを指定します。</param>
		/// <param name="kind">ソースコードの種類を示す <see cref="Microsoft.Scripting.SourceCodeKind"/> を指定します。</param>
		/// <remarks>
		/// このメソッドはユーザーがコンテンツプロバイダを所有できるようにすることで、
		/// エディタのテキスト表現といったホスト内部のデータ構造をラップするストリームを実装できるようにします。
		/// </remarks>
		public ScriptSource CreateScriptSource(TextContentProvider contentProvider, string path, SourceCodeKind kind)
		{
			ContractUtils.RequiresNotNull(contentProvider, "contentProvider");
			ContractUtils.Requires(kind.IsValid(), "kind");
			if (!LanguageContext.CanCreateSourceCode)
				throw new NotSupportedException("Invariant engine cannot create scripts");
			return new ScriptSource(this, LanguageContext.CreateSourceUnit(contentProvider, path, kind));
		}

		#endregion

		/// <summary>言語固有のサービスを返します。</summary>
		/// <param name="args">サービスの取得に使用する引数を指定します。</param>
		/// <remarks>
		/// 共通に利用可能なサービスを次に示します。
		///     TokenCategorizer
		///         標準のソースコードトークン化を提供します。
		///     ExceptionOperations
		///         例外オブジェクトのフォーマット化を提供します。
		///     DocumentationProvider
		///         生存期間中のオブジェクトに対するドキュメントを提供します。
		/// </remarks>
		public TService GetService<TService>(params object[] args) where TService : class
		{
			if (typeof(TService) == typeof(TokenCategorizer))
			{
				var service = LanguageContext.GetService<TokenizerService>(Enumerable.Repeat<object>(LanguageContext, 1).Concat(args).ToArray());
				return service != null ? (TService)(object)new TokenCategorizer(service) : null;
			}
			else if (typeof(TService) == typeof(ExceptionOperations))
			{
				var service = LanguageContext.GetService<ExceptionOperations>();
				return service != null ? (TService)(object)service : (TService)(object)new ExceptionOperations(LanguageContext);
			}
			else if (typeof(TService) == typeof(DocumentationOperations))
			{
				var service = LanguageContext.GetService<DocumentationProvider>(args);
				return service != null ? (TService)(object)new DocumentationOperations(service) : null;
			}
			return LanguageContext.GetService<TService>(args);
		}

		#region Misc. engine information

		/// <summary>このエンジンが使用している読み取り専用の言語オプションを取得します。</summary>
		/// <remarks>
		/// 値はランタイムの初期化中に読み取り専用の後で決定されます。
		/// 構成ファイルを設定したり、明示的に <see cref="Microsoft.Scripting.Hosting.ScriptRuntimeSetup"/> を使用したりすることで設定を変更することができます。
		/// </remarks>
		public LanguageSetup Setup
		{
			get
			{
				if (_config == null)
				{
					// ユーザーはインバリアントなエンジンを取得できてはならない。
					Debug.Assert(!(LanguageContext is InvariantContext));
					//一致する言語構成を検索
					var config = Runtime.Manager.Configuration.GetLanguageConfig(LanguageContext);
					Debug.Assert(config != null);
					return _config = Runtime.Setup.LanguageSetups.FirstOrDefault(x => new AssemblyQualifiedTypeName(x.TypeName) == config.ProviderName);
				}
				return _config;
			}
		}

		/// <summary>エンジンが実行されるコンテキストに対する <see cref="Microsoft.Scripting.Hosting.ScriptRuntime"/> を取得します。</summary>
		public ScriptRuntime Runtime { get; private set; }

		/// <summary>エンジンのバージョンを取得します。</summary>
		public Version LanguageVersion { get { return LanguageContext.LanguageVersion; } }

		#endregion

		/// <summary>コンパイルコードのどのスコープにも関連付けられていない言語固有の <see cref="Microsoft.Scripting.CompilerOptions"/> を取得します。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public CompilerOptions GetCompilerOptions() { return LanguageContext.GetCompilerOptions(); }

		/// <summary>指定されたスコープに関連付けられている言語固有の <see cref="Microsoft.Scripting.CompilerOptions"/> を取得します。</summary>
		/// <param name="scope">取得する <see cref="Microsoft.Scripting.CompilerOptions"/> が関連付けられているスコープを指定します。</param>
		public CompilerOptions GetCompilerOptions(ScriptScope scope) { return LanguageContext.GetCompilerOptions(scope.Scope); }

		/// <summary>スクリプトが別のファイルやコードをインポートまたは要求したときに、ファイルのロードにエンジンによって使用される検索パスを取得または設定します。</summary>
		public ICollection<string> SearchPaths
		{
			get { return LanguageContext.SearchPaths; }
			set
			{
				ContractUtils.RequiresNotNull(value, "value");
				ContractUtils.RequiresNotNullItems(value, "value");
				LanguageContext.SearchPaths = value;
			}
		}

		#region Internal API Surface

		/// <summary>このエンジンの基になる <see cref="Microsoft.Scripting.Runtime.LanguageContext"/> を取得します。</summary>
		internal LanguageContext LanguageContext { get; private set; }

		/// <summary>指定された <see cref="Microsoft.Scripting.Runtime.LanguageContext"/> に対するデリゲートをこのインスタンスで呼び出します。</summary>
		/// <typeparam name="T">引数の型を指定します。</typeparam>
		/// <typeparam name="TRet">戻り値の型を指定します。</typeparam>
		/// <param name="f">このインスタンスで呼び出すデリゲートを指定します。</param>
		/// <param name="arg">デリゲートに指定する引数を指定します。</param>
		internal TRet Call<T, TRet>(Func<LanguageContext, T, TRet> f, T arg) { return f(LanguageContext, arg); }

		#endregion

		// TODO: Figure out what is the right lifetime
		/// <summary>対象のインスタンスの有効期間ポリシーを制御する、有効期間サービス オブジェクトを取得します。</summary>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
		public override object InitializeLifetimeService() { return null; }
	}
}
