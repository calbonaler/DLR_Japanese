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
using System.Collections.ObjectModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>通常ランタイムによって呼び出される言語固有の機能を提供します。</summary>
	public abstract class LanguageContext
	{
		DynamicOperations _operations;

		/// <summary>実行環境である <see cref="ScriptDomainManager"/> を使用して、<see cref="Microsoft.Scripting.Runtime.LanguageContext"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="domainManager"><see cref="LanguageContext"/> が実行される <see cref="ScriptDomainManager"/> を指定します。</param>
		protected LanguageContext(ScriptDomainManager domainManager)
		{
			ContractUtils.RequiresNotNull(domainManager, "domainManager");
			DomainManager = domainManager;
			ContextId = domainManager.GenerateContextId();
		}

		/// <summary>この <see cref="LanguageContext"/> にのみ公開されるメンバを含むコンテキスト ID を取得します。コンテキスト ID はスコープのフィルタリングに使用されます。</summary>
		public ContextId ContextId { get; private set; }

		/// <summary>この <see cref="LanguageContext"/> が実行される <see cref="ScriptDomainManager"/> を取得します。</summary>
		public ScriptDomainManager DomainManager { get; private set; }

		/// <summary>言語がコードを解析したり、翻訳入力単位を作成したりできるかどうかを示す値を取得します。</summary>
		public virtual bool CanCreateSourceCode { get { return true; } }

		#region Scope

		/// <summary>指定されたファイルに対するスコープを取得します。</summary>
		/// <param name="path">スコープを取得するファイルを指定します。</param>
		public virtual Scope GetScope(string path) { return null; }

		// TODO: remove
		/// <summary>指定されたスコープに <see cref="ScopeExtension"/> が存在すればそれを取得し、存在しない場合は作成して結果を返します。</summary>
		/// <param name="scope"><see cref="ScopeExtension"/> を取得するスコープを指定します。</param>
		public ScopeExtension EnsureScopeExtension(Scope scope)
		{
			ContractUtils.RequiresNotNull(scope, "scope");
			ScopeExtension extension = scope.GetExtension(ContextId);
			if (extension == null)
			{
				extension = CreateScopeExtension(scope);
				if (extension == null)
					throw Error.MustReturnScopeExtension();
				return scope.SetExtension(ContextId, extension);
			}
			return extension;
		}

		// TODO: remove
		/// <summary>指定されたスコープに対する <see cref="ScopeExtension"/> を作成します。</summary>
		/// <param name="scope">スコープを指定します。</param>
		public virtual ScopeExtension CreateScopeExtension(Scope scope) { return null; }

		/// <summary>スコープにある変数に値を格納します。</summary>
		/// <param name="scope">変数を格納するスコープを指定します。</param>
		/// <param name="name">値を格納する変数の名前を指定します。</param>
		/// <param name="value">変数に格納する値を指定します。</param>
		/// <remarks>
		/// 既定ではこのメソッドはかなり低速な <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> を利用します。.
		/// 言語は <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> を避け高速にするために、このメソッドをオーバーライドできます。
		/// 言語は言語で共通に使用されているスコープ型への高速なアクセスを提供できます。
		/// 通常このメソッドは言語自体がスコープの実装として利用している <see cref="ScopeStorage"/> や他の型も含みます。
		/// </remarks>
		public virtual void ScopeSetVariable(Scope scope, string name, object value) { Operations.SetMember(scope, name, value); }
		
		/// <summary>スコープ内の変数からの値の取得を試みます。成功した場合は <c>true</c> を返します。</summary>
		/// <param name="scope">変数を格納しているスコープを指定します。</param>
		/// <param name="name">値を取得する変数の名前を指定します。</param>
		/// <param name="value">取得した値を格納する変数を指定します。</param>
		/// <remarks>
		/// 既定ではこのメソッドはかなり低速な <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> を利用します。.
		/// 言語は <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> を避け高速にするために、このメソッドをオーバーライドできます。
		/// 言語は言語で共通に使用されているスコープ型への高速なアクセスを提供できます。
		/// 通常このメソッドは言語自体がスコープの実装として利用している <see cref="ScopeStorage"/> や他の型も含みます。
		/// </remarks>
		public virtual bool ScopeTryGetVariable(Scope scope, string name, out dynamic value) { return Operations.TryGetMember(scope, name, out value); }

		/// <summary>スコープ内の変数から値を取得し、結果を指定された型に変換します。</summary>
		/// <param name="scope">変数を格納しているスコープを指定します。</param>
		/// <param name="name">値を取得する変数の名前を指定します。</param>
		/// <remarks>
		/// 既定ではこのメソッドはかなり低速な <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> を利用します。.
		/// 言語は <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> を避け高速にするために、このメソッドをオーバーライドできます。
		/// 言語は言語で共通に使用されているスコープ型への高速なアクセスを提供できます。
		/// 通常このメソッドは言語自体がスコープの実装として利用している <see cref="ScopeStorage"/> や他の型も含みます。
		/// </remarks>
		public virtual T ScopeGetVariable<T>(Scope scope, string name) { return Operations.GetMember<T>(scope, name); }

		/// <summary>スコープ内の変数から値を取得します。</summary>
		/// <param name="scope">変数を格納しているスコープを指定します。</param>
		/// <param name="name">値を取得する変数の名前を指定します。</param>
		/// <remarks>
		/// 既定ではこのメソッドはかなり低速な <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> を利用します。.
		/// 言語は <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> を避け高速にするために、このメソッドをオーバーライドできます。
		/// 言語は言語で共通に使用されているスコープ型への高速なアクセスを提供できます。
		/// 通常このメソッドは言語自体がスコープの実装として利用している <see cref="ScopeStorage"/> や他の型も含みます。
		/// </remarks>
		public virtual dynamic ScopeGetVariable(Scope scope, string name) { return Operations.GetMember(scope, name); }

		#endregion

		#region Source Code Parsing & Compilation

		/// <summary>指定されたストリームから読み取られるソースコードに対する <see cref="SourceCodeReader"/> を取得します。</summary>
		/// <param name="stream">読み取り用にオープンされたストリームを指定します。ストリームはシークをサポートしている必要があります。</param>
		/// <param name="defaultEncoding">ストリームに Unicode または言語固有のプリアンブルがない場合に使用されるエンコーディングを指定します。</param>
		/// <param name="path">利用可能であれば翻訳単位のパスを指定します。</param>
		/// <exception cref="IOException">I/O エラーが発生しました。</exception>
		public virtual SourceCodeReader GetSourceReader(Stream stream, Encoding defaultEncoding, string path)
		{
			ContractUtils.RequiresNotNull(stream, "stream");
			ContractUtils.RequiresNotNull(defaultEncoding, "defaultEncoding");
			ContractUtils.Requires(stream.CanRead && stream.CanSeek, "stream", "The stream must support reading and seeking");

			var result = new StreamReader(stream, defaultEncoding, true);
			result.Peek();
			return new SourceCodeReader(result, result.CurrentEncoding);
		}

		/// <summary>どのスコープにも関連付けられていないコードのコンパイルに使用される言語固有の <see cref="CompilerOptions"/> オブジェクトを作成します。</summary>
		/// <remarks>言語は関連するあらゆるオプションを <see cref="LanguageContext"/> から新しく作成されるオプションのインスタンスに渡すべきです。</remarks>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public virtual CompilerOptions GetCompilerOptions() { return null; }

		/// <summary>指定されたスコープに関連付けられたコードのコンパイルに使用される言語固有の <see cref="CompilerOptions"/> オブジェクトを作成します。</summary>
		/// <param name="scope">作成する <see cref="CompilerOptions"/> に関連付けられるスコープを指定します。</param>
		public virtual CompilerOptions GetCompilerOptions(Scope scope) { return GetCompilerOptions(); }

		/// <summary>ソースコードを指定されたコンパイラコンテキスト内で解析します。解析する翻訳単位はコンテキストによって保持されます。</summary>
		/// <param name="sourceUnit">解析する翻訳単位を指定します。</param>
		/// <param name="options">解析に関するオプションを指定します。</param>
		/// <param name="errorSink">解析時のエラーを処理する <see cref="ErrorSink"/> を指定します。</param>
		/// <returns>失敗した場合は <c>null</c></returns>
		/// <remarks>ソースコードの状態や、翻訳単位の行やファイルのマッピングを設定することもありえます。</remarks>
		public abstract ScriptCode CompileSourceCode(SourceUnit sourceUnit, CompilerOptions options, ErrorSink errorSink);

		/// <summary>指定されたメソッドから <see cref="ScriptCode"/> を作成します。</summary>
		/// <param name="method"><see cref="ScriptCode"/> を作成するメソッドを表すデリゲート指定します。</param>
		/// <param name="path">ソースコードのパスを指定します。</param>
		/// <param name="customData">カスタムデータを指定します。</param>
		public virtual ScriptCode LoadCompiledCode(Delegate method, string path, string customData) { throw new NotSupportedException(); }

		/// <summary>コードを OS のコマンドシェルから開始されたプログラムであるように実行し、コード実行の成功またはエラー状態を示すプロセス終了コードを返します。</summary>
		/// <param name="program">プログラムを表す <see cref="SourceUnit"/> オブジェクトを指定します。</param>
		public virtual int ExecuteProgram(SourceUnit program)
		{
			ContractUtils.RequiresNotNull(program, "program");
			object returnValue = program.Execute();
			if (returnValue == null)
				return 0;
			var site = CallSite<Func<CallSite, object, int>>.Create(CreateConvertBinder(typeof(int), true));
			return site.Target(site, returnValue);
		}

		#endregion

		#region ScriptEngine API

		/// <summary>この <see cref="LanguageContext"/> が表す言語のバージョンを指定します。</summary>
		public virtual Version LanguageVersion { get { return new Version(0, 0); } }

		/// <summary>スクリプトが別のファイルやコードをインポートまたは要求したときに、ファイルのロードに使用される検索パスを取得または設定します。</summary>
		public virtual ICollection<string> SearchPaths
		{
			get { return Options.SearchPaths; }
			set { throw new NotSupportedException(); }
		}

		/// <summary><see cref="System.CodeDom.CodeObject"/> をソースコードに変換し、必要であれば行番号のマッピングを行います。</summary>
		/// <param name="codeDom">作成するソースコードの基になる <see cref="System.CodeDom.CodeObject"/> を指定します。</param>
		/// <param name="path">作成するソースコードのパスを指定します。</param>
		/// <param name="kind">作成するソースコードの種類を示す <see cref="Microsoft.Scripting.SourceCodeKind"/> を指定します。</param>
		/// <returns></returns>
		public virtual SourceUnit GenerateSourceCode(System.CodeDom.CodeObject codeDom, string path, SourceCodeKind kind) { throw new NotImplementedException(); }

		/// <summary>言語固有のサービスを返します。</summary>
		/// <param name="args">サービスの取得に使用する引数を指定します。</param>
		public virtual TService GetService<TService>(params object[] args) where TService : class { return null; }

		/// <summary>この言語を識別する GUID (Globally Unique Identifier) を取得します。</summary>
		public virtual Guid LanguageGuid { get { return Guid.Empty; } }

		/// <summary>この言語のベンダーを識別する GUID (Globally Unique Identifier) を取得します。</summary>
		public virtual Guid VendorGuid { get { return Guid.Empty; } }

		/// <summary>この言語エンジンをシャットダウンします。</summary>
		public virtual void Shutdown() { }

		/// <summary>指定された例外を表す文字列を取得します。</summary>
		/// <param name="exception">文字列を取得する例外を指定します。</param>
		public virtual string FormatException(Exception exception) { return exception.ToString(); }

		/// <summary>例外に対するスタックフレームを返します。</summary>
		/// <param name="exception">スタックフレームを取得する例外を指定します。</param>
		public virtual IList<DynamicStackFrame> GetStackFrames(Exception exception) { return new DynamicStackFrame[0]; }

		/// <summary>この言語に関する情報を取得します。</summary>
		public virtual LanguageOptions Options { get { return new LanguageOptions(); } }

		#region Source Units

		/// <summary>指定されたコード断片から新しい <see cref="SourceUnit"/> を作成します。</summary>
		/// <param name="code">作成する <see cref="SourceUnit"/> の基になるコード断片を指定します。</param>
		/// <param name="kind">作成する <see cref="SourceUnit"/> が保持するコードの種類を指定します。</param>
		public SourceUnit CreateSnippet(string code, SourceCodeKind kind) { return CreateSnippet(code, null, kind); }

		/// <summary>指定されたコード断片から新しい <see cref="SourceUnit"/> を作成します。</summary>
		/// <param name="code">作成する <see cref="SourceUnit"/> の基になるコード断片を指定します。</param>
		/// <param name="id">作成する <see cref="SourceUnit"/> を識別する文字列を指定します。</param>
		/// <param name="kind">作成する <see cref="SourceUnit"/> が保持するコードの種類を指定します。</param>
		public SourceUnit CreateSnippet(string code, string id, SourceCodeKind kind)
		{
			ContractUtils.RequiresNotNull(code, "code");
			return CreateSourceUnit(new SourceStringContentProvider(code), id, kind);
		}

		/// <summary>指定されたファイルから新しい <see cref="SourceUnit"/> を作成します。</summary>
		/// <param name="path">作成する <see cref="SourceUnit"/> の基になるファイルを表すパスを指定します。</param>
		public SourceUnit CreateFileUnit(string path) { return CreateFileUnit(path, Encoding.Default); }

		/// <summary>指定されたファイルから新しい <see cref="SourceUnit"/> を作成します。</summary>
		/// <param name="path">作成する <see cref="SourceUnit"/> の基になるファイルを表すパスを指定します。</param>
		/// <param name="encoding">ファイルを開く際に使用するエンコーディングを指定します。</param>
		public SourceUnit CreateFileUnit(string path, Encoding encoding) { return CreateFileUnit(path, encoding, SourceCodeKind.File); }

		/// <summary>指定されたファイルから新しい <see cref="SourceUnit"/> を作成します。</summary>
		/// <param name="path">作成する <see cref="SourceUnit"/> の基になるファイルを表すパスを指定します。</param>
		/// <param name="encoding">ファイルを開く際に使用するエンコーディングを指定します。</param>
		/// <param name="kind">作成する <see cref="SourceUnit"/> が保持するコードの種類を指定します。</param>
		public SourceUnit CreateFileUnit(string path, Encoding encoding, SourceCodeKind kind)
		{
			ContractUtils.RequiresNotNull(path, "path");
			return CreateSourceUnit(new FileStreamContentProvider(DomainManager.Platform, path), path, encoding, kind);
		}

		/// <summary>指定されたファイルのパスおよび内容から新しい <see cref="SourceUnit"/> を作成します。</summary>
		/// <param name="path">作成する <see cref="SourceUnit"/> を識別するファイルのパスを指定します。</param>
		/// <param name="content">作成する <see cref="SourceUnit"/> の基になるファイルの内容を指定します。</param>
		public SourceUnit CreateFileUnit(string path, string content)
		{
			ContractUtils.RequiresNotNull(path, "path");
			ContractUtils.RequiresNotNull(content, "content");
			return CreateSourceUnit(new SourceStringContentProvider(content), path, SourceCodeKind.File);
		}

		/// <summary>指定された <see cref="StreamContentProvider"/> から新しい <see cref="SourceUnit"/> を作成します。</summary>
		/// <param name="contentProvider">作成する <see cref="SourceUnit"/> の基になる <see cref="StreamContentProvider"/> を指定します。</param>
		/// <param name="path">作成する <see cref="SourceUnit"/> を識別するファイルのパスを指定します。</param>
		/// <param name="encoding">ストリームからのバイナリデータの読み取りに使用するエンコーディングを指定します。</param>
		/// <param name="kind">作成する <see cref="SourceUnit"/> が保持するコードの種類を指定します。</param>
		public SourceUnit CreateSourceUnit(StreamContentProvider contentProvider, string path, Encoding encoding, SourceCodeKind kind)
		{
			ContractUtils.RequiresNotNull(contentProvider, "contentProvider");
			ContractUtils.RequiresNotNull(encoding, "encoding");
			return CreateSourceUnit(new LanguageBoundTextContentProvider(this, contentProvider, encoding, path), path, kind);
		}

		/// <summary>指定された <see cref="TextContentProvider"/> から新しい <see cref="SourceUnit"/> を作成します。</summary>
		/// <param name="contentProvider">作成する <see cref="SourceUnit"/> の基になる <see cref="TextContentProvider"/> を指定します。</param>
		/// <param name="path">作成する <see cref="SourceUnit"/> を識別するファイルのパスを指定します。</param>
		/// <param name="kind">作成する <see cref="SourceUnit"/> が保持するコードの種類を指定します。</param>
		public SourceUnit CreateSourceUnit(TextContentProvider contentProvider, string path, SourceCodeKind kind)
		{
			ContractUtils.RequiresNotNull(contentProvider, "contentProvider");
			ContractUtils.Requires(kind.IsValid(), "kind");
			ContractUtils.Requires(CanCreateSourceCode);
			return new SourceUnit(this, contentProvider, path, kind);
		}

		#endregion

		#endregion

		/// <summary>コンパイラが使用する <see cref="ErrorSink"/> オブジェクトを取得します。</summary>
		public virtual ErrorSink CompilerErrorSink { get { return ErrorSink.Null; } }

		#region Object Operations Support

		static DynamicMetaObject ErrorMetaObject(Type resultType, DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
		{
			return errorSuggestion ?? new DynamicMetaObject(
				Expression.Throw(Expression.New(typeof(NotImplementedException)), resultType),
				target.Restrictions.Merge(BindingRestrictions.Combine(args))
			);
		}

		/// <summary>指定されたノード型の単項演算に対する <see cref="UnaryOperationBinder"/> を作成します。</summary>
		/// <param name="operation">単項演算の種類を表す <see cref="ExpressionType"/> を指定します。</param>
		public virtual UnaryOperationBinder CreateUnaryOperationBinder(ExpressionType operation) { return new DefaultUnaryOperationBinder(operation); }

		sealed class DefaultUnaryOperationBinder : UnaryOperationBinder
		{
			internal DefaultUnaryOperationBinder(ExpressionType operation) : base(operation) { }

			public override DynamicMetaObject FallbackUnaryOperation(DynamicMetaObject target, DynamicMetaObject errorSuggestion) { return ErrorMetaObject(ReturnType, target, new[] { target }, errorSuggestion); }
		}

		/// <summary>指定されたノード型の二項演算に対する <see cref="BinaryOperationBinder"/> を作成します。</summary>
		/// <param name="operation">二項演算の種類を表す <see cref="ExpressionType"/> を指定します。</param>
		public virtual BinaryOperationBinder CreateBinaryOperationBinder(ExpressionType operation) { return new DefaultBinaryOperationBinder(operation); }

		sealed class DefaultBinaryOperationBinder : BinaryOperationBinder
		{
			internal DefaultBinaryOperationBinder(ExpressionType operation) : base(operation) { }

			public override DynamicMetaObject FallbackBinaryOperation(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion) { return ErrorMetaObject(ReturnType, target, new[] { target, arg }, errorSuggestion); }
		}
		
		/// <summary>指定された型への明示的及び暗黙的な型変換操作に対する <see cref="ConvertBinder"/> を作成します。</summary>
		/// <param name="toType">変換先の型を指定します。</param>
		/// <param name="explicitCast">変換が明示的に行われるかどうかを示す値を指定します。<c>null</c> を指定すると明示的変換か暗黙的変換かは言語が判断するようになります。</param>
		public virtual ConvertBinder CreateConvertBinder(Type toType, bool? explicitCast) { return new DefaultConvertAction(toType, explicitCast ?? false); }

		sealed class DefaultConvertAction : ConvertBinder
		{
			internal DefaultConvertAction(Type type, bool @explicit) : base(type, @explicit) { }

			public override DynamicMetaObject FallbackConvert(DynamicMetaObject self, DynamicMetaObject errorSuggestion)
			{
				if (Type.IsAssignableFrom(self.LimitType))
					return new DynamicMetaObject(
						Expression.Convert(self.Expression, Type),
						BindingRestrictions.GetTypeRestriction(self.Expression, self.LimitType)
					);

				if (errorSuggestion != null)
					return errorSuggestion;

				return new DynamicMetaObject(
					Expression.Throw(
						Expression.Constant(
							new ArgumentTypeException(string.Format("Expected {0}, got {1}", Type.FullName, self.LimitType.FullName))
						),
						ReturnType
					),
					BindingRestrictions.GetTypeRestriction(self.Expression, self.LimitType)
				);
			}
		}
		
		/// <summary>指定されたメンバの取得操作に対する <see cref="GetMemberBinder"/> を作成します。</summary>
		/// <param name="name">取得するメンバの名前を指定します。</param>
		/// <param name="ignoreCase">メンバの検索で大文字と小文字を区別しないかどうかを示す値を指定します。</param>
		public virtual GetMemberBinder CreateGetMemberBinder(string name, bool ignoreCase) { return new DefaultGetMemberAction(name, ignoreCase); }

		sealed class DefaultGetMemberAction : GetMemberBinder
		{
			internal DefaultGetMemberAction(string name, bool ignoreCase) : base(name, ignoreCase) { }

			public override DynamicMetaObject FallbackGetMember(DynamicMetaObject self, DynamicMetaObject errorSuggestion)
			{
				return errorSuggestion ?? new DynamicMetaObject(
					Expression.Throw(
						Expression.New(
							typeof(MissingMemberException).GetConstructor(new[] { typeof(string) }),
							Expression.Constant(String.Format("unknown member: {0}", Name))
						),
						typeof(object)
					),
					self.Value == null ?
						BindingRestrictions.GetExpressionRestriction(Expression.Equal(self.Expression, Expression.Constant(null))) :
						BindingRestrictions.GetTypeRestriction(self.Expression, self.Value.GetType())
				);
			}
		}

		/// <summary>指定されたメンバの設定操作に対する <see cref="SetMemberBinder"/> を作成します。</summary>
		/// <param name="name">設定するメンバの名前を指定します。</param>
		/// <param name="ignoreCase">メンバの検索で大文字と小文字を区別しないかどうかを示す値を指定します。</param>
		public virtual SetMemberBinder CreateSetMemberBinder(string name, bool ignoreCase) { return new DefaultSetMemberAction(name, ignoreCase); }

		sealed class DefaultSetMemberAction : SetMemberBinder
		{
			internal DefaultSetMemberAction(string name, bool ignoreCase) : base(name, ignoreCase) { }

			public override DynamicMetaObject FallbackSetMember(DynamicMetaObject self, DynamicMetaObject value, DynamicMetaObject errorSuggestion) { return ErrorMetaObject(ReturnType, self, new DynamicMetaObject[] { value }, errorSuggestion); }
		}

		/// <summary>指定されたメンバの削除操作に対する <see cref="DeleteMemberBinder"/> を作成します。</summary>
		/// <param name="name">削除するメンバの名前を指定します。</param>
		/// <param name="ignoreCase">メンバの検索で大文字と小文字を区別しないかどうかを示す値を指定します。</param>
		public virtual DeleteMemberBinder CreateDeleteMemberBinder(string name, bool ignoreCase) { return new DefaultDeleteMemberAction(name, ignoreCase); }

		sealed class DefaultDeleteMemberAction : DeleteMemberBinder
		{
			internal DefaultDeleteMemberAction(string name, bool ignoreCase) : base(name, ignoreCase) { }

			public override DynamicMetaObject FallbackDeleteMember(DynamicMetaObject self, DynamicMetaObject errorSuggestion) { return ErrorMetaObject(ReturnType, self, DynamicMetaObject.EmptyMetaObjects, errorSuggestion); }
		}

		/// <summary>指定されたメンバの呼び出し操作に対する <see cref="InvokeMemberBinder"/> を作成します。</summary>
		/// <param name="name">呼び出すメンバの名前を指定します。</param>
		/// <param name="ignoreCase">メンバの検索で大文字と小文字を区別しないかどうかを示す値を指定します。</param>
		/// <param name="callInfo">引数に関する情報を表す <see cref="CallInfo"/> を指定します。</param>
		public virtual InvokeMemberBinder CreateCallBinder(string name, bool ignoreCase, CallInfo callInfo) { return new DefaultCallAction(this, name, ignoreCase, callInfo); }

		sealed class DefaultCallAction : InvokeMemberBinder
		{
			LanguageContext _context;

			internal DefaultCallAction(LanguageContext context, string name, bool ignoreCase, CallInfo callInfo) : base(name, ignoreCase, callInfo) { _context = context; }

			public override DynamicMetaObject FallbackInvokeMember(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion) { return ErrorMetaObject(ReturnType, target, Enumerable.Repeat(target, 1).Concat(args).ToArray(), errorSuggestion); }

			public override DynamicMetaObject FallbackInvoke(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
			{
				return new DynamicMetaObject(
					Expression.Dynamic(
						_context.CreateInvokeBinder(CallInfo),
						typeof(object),
						Enumerable.Repeat(target.Expression, 1).Concat(args.Select(x => x.Expression))
					),
					target.Restrictions.Merge(BindingRestrictions.Combine(args))
				);
			}
		}
		
		/// <summary>オブジェクトの呼び出し操作に対する <see cref="InvokeBinder"/> を作成します。</summary>
		/// <param name="callInfo">引数に関する情報を表す <see cref="CallInfo"/> を指定します。</param>
		public virtual InvokeBinder CreateInvokeBinder(CallInfo callInfo) { return new DefaultInvokeAction(callInfo); }

		sealed class DefaultInvokeAction : InvokeBinder
		{
			internal DefaultInvokeAction(CallInfo callInfo) : base(callInfo) { }

			public override DynamicMetaObject FallbackInvoke(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion) { return ErrorMetaObject(ReturnType, target, args, errorSuggestion); }
		}
		
		/// <summary>オブジェクトのインスタンス作成操作に対する <see cref="CreateInstanceBinder"/> を作成します。</summary>
		/// <param name="callInfo">引数に関する情報を表す <see cref="CallInfo"/> を指定します。</param>
		public virtual CreateInstanceBinder CreateCreateBinder(CallInfo callInfo) { return new DefaultCreateAction(callInfo); }

		sealed class DefaultCreateAction : CreateInstanceBinder
		{
			internal DefaultCreateAction(CallInfo callInfo) : base(callInfo) { }

			public override DynamicMetaObject FallbackCreateInstance(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion) { return ErrorMetaObject(ReturnType, target, args, errorSuggestion); }
		}

		/// <summary>動的オブジェクトに対する操作を表す <see cref="DynamicOperations"/> のインスタンスを取得します。</summary>
		public DynamicOperations Operations
		{
			get
			{
				if (_operations == null)
					Interlocked.CompareExchange(ref _operations, new DynamicOperations(this), null);
				return _operations;
			}
		}
		
		#endregion

		#region Object Introspection Support

		/// <summary>オブジェクトの既知のメンバの一覧を返します。</summary>
		/// <param name="obj">メンバの一覧を取得するオブジェクトを指定します。</param>
		public virtual IList<string> GetMemberNames(object obj)
		{
			var ido = obj as IDynamicMetaObjectProvider;
			if (ido != null)
				return new ReadOnlyCollection<string>(ido.GetMetaObject(Expression.Parameter(typeof(object))).GetDynamicMemberNames().ToArray());
			return ArrayUtils.EmptyStrings;
		}

		/// <summary>指定されたオブジェクトに対する文字列で提供されるドキュメントを返します。</summary>
		/// <param name="obj">ドキュメントを取得するオブジェクトを指定します。</param>
		public virtual string GetDocumentation(object obj) { return string.Empty; }

		/// <summary>ユーザーに対する表示形式の指定されたオブジェクトの呼び出しに対して適用されるシグネチャのリストを返します。</summary>
		/// <param name="obj">シグネチャのリストを取得するオブジェクトを指定します。</param>
		public virtual IList<string> GetCallSignatures(object obj) { return ArrayUtils.EmptyStrings; }

		/// <summary>指定されたオブジェクトが呼び出し可能かどうかを示す値を取得します。</summary>
		/// <param name="obj">呼び出し可能かどうかを調べるオブジェクトを指定します。</param>
		public virtual bool IsCallable(object obj)
		{
			if (obj == null)
				return false;
			return typeof(Delegate).IsAssignableFrom(obj.GetType());
		}

		#endregion

		#region Object formatting

		/// <summary>オブジェクトの文字列表現を言語固有のオブジェクト表現フォーマットで返します。</summary>
		/// <param name="operations">フォーマットに必要なあらゆる動的ディスパッチに使用される可能性のある動的サイトコンテナを指定します。</param>
		/// <param name="obj">フォーマットするオブジェクトを指定します。</param>
		/// <returns>オブジェクトの文字列表現</returns>
		public virtual string FormatObject(DynamicOperations operations, object obj) { return obj == null ? "null" : obj.ToString(); }

		/// <summary>指定された例外に対するメッセージおよび例外の型を取得します。</summary>
		/// <param name="exception">メッセージおよび例外の型を取得する例外を指定します。</param>
		/// <param name="message">取得するメッセージを格納する変数を指定します。</param>
		/// <param name="errorTypeName">取得する例外の型を格納する変数を指定します。</param>
		public virtual void GetExceptionMessage(Exception exception, out string message, out string errorTypeName)
		{
			message = exception.Message;
			errorTypeName = exception.GetType().Name;
		}

		#endregion
	}
}
