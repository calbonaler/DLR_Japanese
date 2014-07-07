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
using System.Runtime.Remoting;
using System.Runtime.Serialization;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting.Providers
{
	/// <summary>
	/// ホスティング API プロバイダに対する高度な API を提供します。これらのメソッドはホストから使用するものではありません。
	/// これらは既存のホスティング API に影響を及ぼしたり言語固有の機能で拡張したりしたいと考える他のホスティング API 実装者に対して提供されます。
	/// </summary>
	public static class HostingHelpers
	{
		/// <summary>指定された <see cref="ScriptRuntime"/> から <see cref="ScriptDomainManager"/> を取得します。</summary>
		/// <param name="runtime"><see cref="ScriptDomainManager"/> を取得する <see cref="ScriptRuntime"/> を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="runtime"/> は <c>null</c> 参照です。</exception>
		/// <exception cref="SerializationException"><paramref name="runtime"/> はリモートです。</exception>
		public static ScriptDomainManager GetDomainManager(ScriptRuntime runtime)
		{
			ContractUtils.RequiresNotNull(runtime, "runtime");
			return runtime.Manager;
		}

		/// <summary>指定された <see cref="ScriptEngine"/> から <see cref="LanguageContext"/> を取得します。</summary>
		/// <param name="engine"><see cref="LanguageContext"/> を取得する <see cref="ScriptEngine"/> を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="engine"/> は <c>null</c> 参照です。</exception>
		/// <exception cref="SerializationException"><paramref name="engine"/> はリモートです。</exception>
		public static LanguageContext GetLanguageContext(ScriptEngine engine)
		{
			ContractUtils.RequiresNotNull(engine, "engine");
			return engine.LanguageContext;
		}

		/// <summary>指定された <see cref="ScriptSource"/> から <see cref="SourceUnit"/> を取得します。</summary>
		/// <param name="scriptSource"><see cref="SourceUnit"/> を取得する <see cref="ScriptSource"/> を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="scriptSource"/> は <c>null</c> 参照です。</exception>
		/// <exception cref="SerializationException"><paramref name="scriptSource"/> はリモートです。</exception>
		public static SourceUnit GetSourceUnit(ScriptSource scriptSource)
		{
			ContractUtils.RequiresNotNull(scriptSource, "scriptSource");
			return scriptSource.SourceUnit;
		}

		/// <summary>指定された <see cref="CompiledCode"/> から <see cref="ScriptCode"/> を取得します。</summary>
		/// <param name="compiledCode"><see cref="ScriptCode"/> を取得する <see cref="CompiledCode"/> を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="compiledCode"/> は <c>null</c> 参照です。</exception>
		/// <exception cref="SerializationException"><paramref name="compiledCode"/> はリモートです。</exception>
		public static ScriptCode GetScriptCode(CompiledCode compiledCode)
		{
			ContractUtils.RequiresNotNull(compiledCode, "compiledCode");
			return compiledCode.ScriptCode;
		}

		/// <summary>指定された <see cref="ScriptIO"/> から <see cref="SharedIO"/> を取得します。</summary>
		/// <param name="io"><see cref="SharedIO"/> を取得する <see cref="ScriptIO"/> を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="io"/> は <c>null</c> 参照です。</exception>
		/// <exception cref="SerializationException"><paramref name="io"/> はリモートです。</exception>
		public static SharedIO GetSharedIO(ScriptIO io)
		{
			ContractUtils.RequiresNotNull(io, "io");
			return io.SharedIO;
		}

		/// <summary>指定された <see cref="ScriptScope"/> から <see cref="Scope"/> を取得します。</summary>
		/// <param name="scriptScope"><see cref="Scope"/> を取得する <see cref="ScriptScope"/> を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="scriptScope"/> は <c>null</c> 参照です。</exception>
		/// <exception cref="SerializationException"><paramref name="scriptScope"/> はリモートです。</exception>
		public static Scope GetScope(ScriptScope scriptScope)
		{
			ContractUtils.RequiresNotNull(scriptScope, "scriptScope");
			return scriptScope.Scope;
		}

		/// <summary>指定された <see cref="ScriptEngine"/> および <see cref="Scope"/> から新しい <see cref="ScriptScope"/> を作成します。</summary>
		/// <param name="engine">新しい <see cref="ScriptScope"/> の基になるエンジンを指定します。</param>
		/// <param name="scope">新しい <see cref="ScriptScope"/> の基になる <see cref="Scope"/> を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="engine"/> は <c>null</c> 参照です。</exception>
		/// <exception cref="ArgumentNullException"><paramref name="scope"/> は <c>null</c> 参照です。</exception>
		/// <exception cref="ArgumentException"><paramref name="engine"/> は透過プロキシです。</exception>
		public static ScriptScope CreateScriptScope(ScriptEngine engine, Scope scope)
		{
			ContractUtils.RequiresNotNull(engine, "engine");
			ContractUtils.RequiresNotNull(scope, "scope");
			ContractUtils.Requires(!RemotingServices.IsTransparentProxy(engine), "engine", "The engine cannot be a transparent proxy");
			return new ScriptScope(engine, scope);
		}

		/// <summary><see cref="ScriptEngine"/> のアプリケーションドメイン内のコールバックを実行し、結果を返します。</summary>
		[Obsolete("LanguageContext を用いてサービスを実装し ScriptEngine.GetService を呼び出すことを推奨します。")]
		public static TRet CallEngine<T, TRet>(ScriptEngine engine, Func<LanguageContext, T, TRet> f, T arg) { return engine.Call(f, arg); }

		/// <summary>指定された <see cref="DocumentationProvider"/> から新しい <see cref="DocumentationOperations"/> を作成します。</summary>
		public static DocumentationOperations CreateDocumentationOperations(DocumentationProvider provider) { return new DocumentationOperations(provider); }
	}
}
