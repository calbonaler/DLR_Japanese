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
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Threading;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting
{
	/// <summary>
	/// ホスティング API において動的言語ランタイム (DLR) を表します。
	/// <see cref="Microsoft.Scripting.Runtime.ScriptDomainManager"/> に対するもう 1 つのホスティング API です。
	/// </summary>
	public sealed class ScriptRuntime : MarshalByRefObject
	{
		readonly Dictionary<LanguageContext, ScriptEngine> _engines = new Dictionary<LanguageContext, ScriptEngine>();
		readonly InvariantContext _invariantContext;
		readonly object _lock = new object();
		ScriptScope _globals;
		Scope _scopeGlobals;
		ScriptEngine _invariantEngine;

		/// <summary>現在のアプリケーションドメインに <see cref="Microsoft.Scripting.Hosting.ScriptRuntime"/> を作成し、指定された設定を使用して初期化します。</summary>
		/// <param name="setup">初期化に使用する設定を格納している <see cref="Microsoft.Scripting.Hosting.ScriptRuntimeSetup"/> を指定します。</param>
		/// <exception cref="System.ArgumentNullException"><paramref name="setup"/> が <c>null</c> です。</exception>
		public ScriptRuntime(ScriptRuntimeSetup setup)
		{
			ContractUtils.RequiresNotNull(setup, "setup");

			// 構成エラーを即座に発見するため、最初に実行する
			DlrConfiguration config = (Setup = setup).ToConfiguration();
			try { Host = (ScriptHost)Activator.CreateInstance(setup.HostType, setup.HostArguments.ToArray()); }
			catch (TargetInvocationException e) { throw new InvalidImplementationException(Strings.InvalidCtorImplementation(setup.HostType, e.InnerException.Message), e.InnerException); }
			catch (Exception e) { throw new InvalidImplementationException(Strings.InvalidCtorImplementation(setup.HostType, e.Message), e); }
			
			IO = new ScriptIO((Manager = new ScriptDomainManager(new ScriptHostProxy(Host), config)).SharedIO);

			bool freshEngineCreated;
			_globals = new ScriptScope(GetEngineNoLockNoNotification(_invariantContext = new InvariantContext(Manager), out freshEngineCreated), Manager.Globals);

			// ランタイムはここまでですべて設定され、ホストのコードが呼び出されます。
			Host.Runtime = this;

			object noDefaultRefs;
			if (!setup.Options.TryGetValue("NoDefaultReferences", out noDefaultRefs) || Convert.ToBoolean(noDefaultRefs) == false)
			{
				LoadAssembly(typeof(string).Assembly);
				LoadAssembly(typeof(System.Diagnostics.Debug).Assembly);
			}
		}

		/// <summary>このインスタンスの基になっている <see cref="Microsoft.Scripting.Runtime.ScriptDomainManager"/> を取得します。</summary>
		internal ScriptDomainManager Manager { get; private set; }

		/// <summary>動的言語ランタイムに関連付けられているホストを取得します。</summary>
		public ScriptHost Host { get; private set; }

		/// <summary>動的言語ランタイムの入出力を取得します。</summary>
		public ScriptIO IO { get; private set; }

		/// <summary>現在のアプリケーション設定の言語設定を使用して新しいランタイムを作成します。</summary>
		public static ScriptRuntime CreateFromConfiguration() { return new ScriptRuntime(ScriptRuntimeSetup.ReadConfiguration()); }

		#region Remoting

		/// <summary>指定されたアプリケーションドメインに <see cref="Microsoft.Scripting.Hosting.ScriptRuntime"/> を作成し、指定された設定を使用して初期化します。</summary>
		/// <param name="domain"><see cref="Microsoft.Scripting.Hosting.ScriptRuntime"/> を作成するアプリケーションドメインを指定します。</param>
		/// <param name="setup">初期化に使用する設定を格納している <see cref="Microsoft.Scripting.Hosting.ScriptRuntimeSetup"/> を指定します。</param>
		/// <exception cref="System.ArgumentNullException"><paramref name="domain"/> が <c>null</c> です。</exception>
		public static ScriptRuntime CreateRemote(AppDomain domain, ScriptRuntimeSetup setup)
		{
			ContractUtils.RequiresNotNull(domain, "domain");
			return (ScriptRuntime)domain.CreateInstanceAndUnwrap(
				typeof(ScriptRuntime).Assembly.FullName, typeof(ScriptRuntime).FullName,
				false, BindingFlags.Default, null, new object[] { setup }, null, null
			);
		}

		// TODO: Figure out what is the right lifetime
		/// <summary>対象のインスタンスの有効期間ポリシーを制御する、有効期間サービス オブジェクトを取得します。</summary>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
		public override object InitializeLifetimeService() { return null; }

		#endregion

		/// <summary>このインスタンスの初期化に使用した <see cref="Microsoft.Scripting.Hosting.ScriptRuntimeSetup"/> を取得します。</summary>
		public ScriptRuntimeSetup Setup { get; private set; }

		#region Engines

		/// <summary>言語の名前から <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> を取得します。</summary>
		/// <param name="languageName">取得する <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> を表す言語の名前を指定します。</param>
		/// <exception cref="System.ArgumentNullException"><paramref name="languageName"/> が <c>null</c> です。</exception>
		/// <exception cref="System.ArgumentException"><paramref name="languageName"/> が未知の言語名を表しています。</exception>
		public ScriptEngine GetEngine(string languageName)
		{
			ContractUtils.RequiresNotNull(languageName, "languageName");
			ScriptEngine engine;
			if (!TryGetEngine(languageName, out engine))
				throw new ArgumentException(String.Format("未知の言語名: '{0}'", languageName));
			return engine;
		}

		/// <summary>言語プロバイダの型名から <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> を取得します。</summary>
		/// <param name="assemblyQualifiedTypeName">取得する <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> の言語プロバイダの型名を指定します。</param>
		/// <exception cref="System.ArgumentNullException"><paramref name="assemblyQualifiedTypeName"/> が <c>null</c> です。</exception>
		public ScriptEngine GetEngineByTypeName(string assemblyQualifiedTypeName)
		{
			ContractUtils.RequiresNotNull(assemblyQualifiedTypeName, "assemblyQualifiedTypeName");
			return GetEngine(Manager.GetLanguageByTypeName(assemblyQualifiedTypeName));
		}

		/// <summary>言語のソースファイルの拡張子から <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> を取得します。</summary>
		/// <param name="fileExtension">取得する <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> が表す言語のソースファイルの拡張子を指定します。</param>
		/// <exception cref="ArgumentException"><paramref name="fileExtension"/> が <c>null</c> です。</exception>
		/// <exception cref="ArgumentNullException"><paramref name="fileExtension"/> が未知の拡張子を表しています。</exception>
		public ScriptEngine GetEngineByFileExtension(string fileExtension)
		{
			ContractUtils.RequiresNotNull(fileExtension, "fileExtension");
			ScriptEngine engine;
			if (!TryGetEngineByFileExtension(fileExtension, out engine))
				throw new ArgumentException(String.Format("未知の拡張子: '{0}'", fileExtension));
			return engine;
		}

		/// <summary>言語の名前から <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> を取得します。見つからない場合は false を返します。</summary>
		/// <param name="languageName">取得する <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> を表す言語の名前を指定します。</param>
		/// <param name="engine">取得した <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> を格納する変数を指定します。</param>
		public bool TryGetEngine(string languageName, out ScriptEngine engine)
		{
			LanguageContext language;
			if (!Manager.TryGetLanguage(languageName, out language))
			{
				engine = null;
				return false;
			}
			engine = GetEngine(language);
			return true;
		}

		/// <summary>言語のソースファイルの拡張子から <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> を取得します。</summary>
		/// <param name="fileExtension">取得する <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> が表す言語のソースファイルの拡張子を指定します。</param>
		/// <param name="engine">取得した <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> を格納する変数を指定します。</param>
		public bool TryGetEngineByFileExtension(string fileExtension, out ScriptEngine engine)
		{
			LanguageContext language;
			if (!Manager.TryGetLanguageByFileExtension(fileExtension, out language))
			{
				engine = null;
				return false;
			}
			engine = GetEngine(language);
			return true;
		}

		/// <summary>指定された <see cref="Microsoft.Scripting.Runtime.LanguageContext"/> から <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> を取得します。</summary>
		/// <param name="language">取得する <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> が表す言語に対する <see cref="Microsoft.Scripting.Runtime.LanguageContext"/> を指定します。</param>
		internal ScriptEngine GetEngine(LanguageContext language)
		{
			Assert.NotNull(language);
			ScriptEngine engine;
			bool freshEngineCreated;
			lock (_engines)
				engine = GetEngineNoLockNoNotification(language, out freshEngineCreated);
			if (freshEngineCreated && !ReferenceEquals(language, _invariantContext))
				Host.EngineCreated(engine);
			return engine;
		}

		ScriptEngine GetEngineNoLockNoNotification(LanguageContext language, out bool freshEngineCreated)
		{
			Debug.Assert(_engines != null, "Invalid ScriptRuntime initialiation order");
			ScriptEngine engine;
			if (freshEngineCreated = !_engines.TryGetValue(language, out engine))
			{
				engine = new ScriptEngine(this, language);
				Thread.MemoryBarrier();
				_engines.Add(language, engine);
			}
			return engine;
		}

		#endregion

		#region Compilation, Module Creation

		/// <summary>インバリアントコンテキストを使用して、新しい空の <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> を作成します。</summary>
		public ScriptScope CreateScope() { return InvariantEngine.CreateScope(); }

		/// <summary>
		/// 指定された言語 ID に関連付けられた <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> を使用して、新しい空の
		/// <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> を作成します。
		/// </summary>
		/// <param name="languageId">
		/// <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> を作成する <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> を表す言語 ID を指定します。
		/// </param>
		public ScriptScope CreateScope(string languageId) { return GetEngine(languageId).CreateScope(); }

		/// <summary>インバリアントコンテキストを使用して、指定された任意のオブジェクトをストレージとする <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> を作成します。</summary>
		/// <param name="storage">作成する <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> のストレージとなるオブジェクトを指定します。</param>
		public ScriptScope CreateScope(IDynamicMetaObjectProvider storage) { return InvariantEngine.CreateScope(storage); }

		/// <summary>
		/// 指定された言語 ID に関連付けられた <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> を使用して、指定された任意のオブジェクトをストレージとする
		/// <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> を作成します。
		/// </summary>
		/// <param name="languageId">
		/// <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> を作成する <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> を表す言語 ID を指定します。
		/// </param>
		/// <param name="storage">作成する <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> のストレージとなるオブジェクトを指定します。</param>
		public ScriptScope CreateScope(string languageId, IDynamicMetaObjectProvider storage) { return GetEngine(languageId).CreateScope(storage); }

		[Obsolete("IAttributesCollection is obsolete, use CreateScope(IDynamicMetaObjectProvider) instead")]
		public ScriptScope CreateScope(IAttributesCollection dictionary) { return InvariantEngine.CreateScope(dictionary); }

		#endregion

		// TODO: file IO exceptions, parse exceptions, execution exceptions, etc.
		/// <summary>指定されたファイルの内容を新しいスコープで実行し、そのスコープを返します。エンジンはファイルの拡張子から判断されます。</summary>
		/// <param name="path">実行するファイルのパスを指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="path"/> が <c>null</c> です。</exception>
		/// <exception cref="ArgumentException">
		/// パスが空であるか、<see cref="System.IO.Path.GetInvalidPathChars"/>
		/// で定義される無効な文字を含んでいるか、拡張子がありません。
		/// </exception>
		public ScriptScope ExecuteFile(string path)
		{
			ContractUtils.RequiresNotEmpty(path, "path");
			string extension = Path.GetExtension(path);
			ScriptEngine engine;
			if (!TryGetEngineByFileExtension(extension, out engine))
				throw new ArgumentException(String.Format("ファイル拡張子 '{0}' はどの言語にも関連付けられていません。", extension));
			return engine.ExecuteFile(path);
		}

		/// <summary>指定されたファイルを検索し、ファイルが既にロードされていればスコープを返し、それ以外の場合はファイルをロードしてそのスコープを返します。</summary>
		/// <param name="path">使用するファイルのパスを指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="path"/> が <c>null</c> です。</exception>
		/// <exception cref="ArgumentException">ファイルの拡張子を言語エンジンに割り当てられません。</exception>
		/// <exception cref="InvalidOperationException">言語に 1 つも検索パスがありません。</exception>
		/// <exception cref="FileNotFoundException">ファイルは言語の検索パスに存在している必要があります。</exception>
		public ScriptScope UseFile(string path)
		{
			ContractUtils.RequiresNotEmpty(path, "path");
			string extension = Path.GetExtension(path);

			ScriptEngine engine;
			if (!TryGetEngineByFileExtension(extension, out engine))
				throw new ArgumentException(string.Format("ファイル拡張子 '{0}' はどの言語にも関連付けられていません。", extension));

			if (engine.SearchPaths.Count == 0)
				throw new InvalidOperationException(string.Format("言語 '{0}' には検索パスがありません。", engine.Setup.DisplayName));

			// See if the file is already loaded, if so return the scope
			foreach (string searchPath in engine.SearchPaths)
			{
				ScriptScope scope = engine.GetScope(Path.Combine(searchPath, path));
				if (scope != null)
					return scope;
			}

			// Find the file on disk, then load it
			foreach (string searchPath in engine.SearchPaths)
			{
				string filePath = Path.Combine(searchPath, path);
				if (Manager.Platform.FileExists(filePath))
					return ExecuteFile(filePath);
			}

			// Didn't find the file, throw
			throw new FileNotFoundException(string.Format("File '{0}' not found in language's search path: {1}", path, string.Join(", ", engine.SearchPaths)));
		}

		/// <summary>
		/// グローバルオブジェクトまたは <see cref="Microsoft.Scripting.Hosting.ScriptScope"/>
		/// としての <see cref="Microsoft.Scripting.Hosting.ScriptRuntime"/> の名前束縛を取得します。
		/// </summary>
		public ScriptScope Globals
		{
			get
			{
				if (_scopeGlobals == Manager.Globals)
					return _globals;
				lock (_lock)
				{
					if (_scopeGlobals != Manager.Globals)
						// make sure no one has changed the globals behind our back
						_globals = new ScriptScope(InvariantEngine, _scopeGlobals = Manager.Globals); // TODO: Should get LC from Scope when it's there
					return _globals;
				}
			}
		}

		/// <summary>
		/// アセンブリ内で使用可能な型を表すために、アセンブリの名前空間および <see cref="Microsoft.Scripting.Hosting.ScriptRuntime.Globals"/> に対する名前束縛を巡回します。
		/// </summary>
		/// <param name="assembly">巡回するアセンブリを指定します。</param>
		/// <remarks>
		/// それぞれの最上位名前空間の名前は Globals において名前空間を表す動的オブジェクトに結び付けられます。
		/// それぞれの最上位名前空間オブジェクト内では、ネストされた名前空間の名前がそれぞれの層の名前空間を表す動的オブジェクトに結び付けられます。
		/// 同じ名前空間修飾名に遭遇した場合、このメソッドは名前および名前空間を表すオブジェクトをマージします。
		/// </remarks>
		public void LoadAssembly(Assembly assembly) { Manager.LoadAssembly(assembly); }

		/// <summary>インバリアントコンテキストに対するエンジンの既定の <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> オブジェクトを取得します。</summary>
		public ObjectOperations Operations { get { return InvariantEngine.Operations; } }

		/// <summary>インバリアントコンテキストに対するエンジンの新しい <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> オブジェクトを作成しますオブジェクトを作成します。</summary>
		public ObjectOperations CreateOperations() { return InvariantEngine.CreateOperations(); }

		/// <summary>動的言語ランタイムをシャットダウンします。</summary>
		public void Shutdown()
		{
			List<LanguageContext> lcs;
			lock (_engines)
				lcs = new List<LanguageContext>(_engines.Keys);
			foreach (var language in lcs)
				language.Shutdown();
		}

		/// <summary>インバリアントコンテキストに対するエンジンを取得します。</summary>
		internal ScriptEngine InvariantEngine
		{
			get
			{
				if (_invariantEngine == null)
					_invariantEngine = GetEngine(_invariantContext);
				return _invariantEngine;
			}
		}
	}
}
