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
using System.Reflection;
using System.Threading;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>動的言語ランタイムにおいてスクリプトのドメインを管理します。</summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")] // TODO: fix
	public sealed class ScriptDomainManager
	{
		List<Assembly> _loadedAssemblies = new List<Assembly>();
		int _lastContextId; // 最後に言語コンテキストに割り当てられた ID

		/// <summary>ホストに関連付けられた <see cref="Microsoft.Scripting.PlatformAdaptationLayer"/> を取得します。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
		public PlatformAdaptationLayer Platform
		{
			get
			{
				var result = Host.PlatformAdaptationLayer;
				if (result == null)
					throw new InvalidImplementationException();
				return result;
			}
		}

		/// <summary>このオブジェクトに関連付けられた <see cref="Microsoft.Scripting.Runtime.SharedIO"/> オブジェクトを取得します。</summary>
		public SharedIO SharedIO { get; private set; }

		/// <summary>このオブジェクトのホストを取得します。</summary>
		public DynamicRuntimeHostingProvider Host { get; private set; }

		/// <summary>動的言語ランタイムの構成に使用する <see cref="Microsoft.Scripting.Runtime.DlrConfiguration"/> を取得します。</summary>
		public DlrConfiguration Configuration { get; private set; }

		/// <summary>指定されたホストおよび構成情報を使用して、<see cref="Microsoft.Scripting.Runtime.ScriptDomainManager"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="hostingProvider">ホストを指定します。</param>
		/// <param name="configuration">動的言語ランタイムの構成を行う <see cref="DlrConfiguration"/> を指定します。</param>
		public ScriptDomainManager(DynamicRuntimeHostingProvider hostingProvider, DlrConfiguration configuration)
		{
			ContractUtils.RequiresNotNull(hostingProvider, "hostingProvider");
			ContractUtils.RequiresNotNull(configuration, "configuration");
			configuration.Freeze();
			Host = hostingProvider;
			Configuration = configuration;
			SharedIO = new SharedIO();
			Globals = new Scope(); // 初期の既定のスコープを作成
		}

		#region Language Registration

		/// <summary>言語コンテキストの ID を生成します。</summary>
		internal ContextId GenerateContextId() { return new ContextId(Interlocked.Increment(ref _lastContextId)); }

		/// <summary>指定された言語プロバイダの型から言語を取得します。</summary>
		/// <param name="providerType">言語を取得する言語プロバイダの型を指定します。</param>
		public LanguageContext GetLanguage(Type providerType)
		{
			ContractUtils.RequiresNotNull(providerType, "providerType");
			return GetLanguageByTypeName(providerType.AssemblyQualifiedName);
		}

		/// <summary>指定された言語プロバイダのアセンブリ修飾型名から言語を取得します。</summary>
		/// <param name="providerAssemblyQualifiedTypeName">言語を取得する言語プロバイダのアセンブリ修飾型名を指定します。</param>
		public LanguageContext GetLanguageByTypeName(string providerAssemblyQualifiedTypeName)
		{
			ContractUtils.RequiresNotNull(providerAssemblyQualifiedTypeName, "providerAssemblyQualifiedTypeName");
			LanguageContext language;
			if (!Configuration.TryLoadLanguage(this, AssemblyQualifiedTypeName.ParseArgument(providerAssemblyQualifiedTypeName, "providerAssemblyQualifiedTypeName"), out language))
				throw Error.UnknownLanguageProviderType();
			return language;
		}

		/// <summary>指定された名前から言語の取得を試みます。成功した場合は <c>true</c> を返します。</summary>
		/// <param name="languageName">取得する言語を表す名前を指定します。</param>
		/// <param name="language">取得した言語コンテキストを格納する変数を指定します。</param>
		public bool TryGetLanguage(string languageName, out LanguageContext language)
		{
			ContractUtils.RequiresNotNull(languageName, "languageName");
			return Configuration.TryLoadLanguage(this, languageName, false, out language);
		}

		/// <summary>指定された名前から言語を取得します。</summary>
		/// <param name="languageName">取得する言語を表す名前を指定します。</param>
		public LanguageContext GetLanguageByName(string languageName)
		{
			LanguageContext language;
			if (!TryGetLanguage(languageName, out language))
				throw new ArgumentException(string.Format("Unknown language name: '{0}'", languageName));
			return language;
		}

		/// <summary>言語のソースファイルの拡張子から言語の取得を試みます。成功した場合は <c>true</c> を返します。</summary>
		/// <param name="fileExtension">取得する言語のソースファイルの拡張子を指定します。</param>
		/// <param name="language">取得した言語コンテキストを格納する変数を指定します。</param>
		public bool TryGetLanguageByFileExtension(string fileExtension, out LanguageContext language)
		{
			ContractUtils.RequiresNotEmpty(fileExtension, "fileExtension");
			return Configuration.TryLoadLanguage(this, DlrConfiguration.NormalizeExtension(fileExtension), true, out language);
		}

		/// <summary>言語のソースファイルの拡張子から言語を取得します。</summary>
		/// <param name="fileExtension">取得する言語のソースファイルの拡張子を指定します。</param>
		public LanguageContext GetLanguageByExtension(string fileExtension)
		{
			LanguageContext language;
			if (!TryGetLanguageByFileExtension(fileExtension, out language))
				throw new ArgumentException(String.Format("Unknown file extension: '{0}'", fileExtension));
			return language;
		}

		#endregion

		/// <summary>環境変数のコレクションを取得します。</summary>
		public Scope Globals { get; set; }

		/// <summary>ホストが <see cref="LoadAssembly"/> を呼び出したときに発生します。</summary>
		public event AssemblyLoadEventHandler AssemblyLoad;

		/// <summary>このドメインに指定したアセンブリをロードします。</summary>
		/// <param name="assembly">ロードするアセンブリを指定します。</param>
		public bool LoadAssembly(Assembly assembly)
		{
			ContractUtils.RequiresNotNull(assembly, "assembly");
			lock (_loadedAssemblies)
			{
				if (_loadedAssemblies.Contains(assembly))
					return false; // only deliver the event if we've never added the assembly before
				_loadedAssemblies.Add(assembly);
			}
			var assmLoaded = AssemblyLoad;
			if (assmLoaded != null)
				assmLoaded(this, new AssemblyLoadEventArgs(assembly));
			return true;
		}

		/// <summary>このドメインに既にロードされているアセンブリのリストを取得します。</summary>
		public IList<Assembly> GetLoadedAssemblies()
		{
			lock (_loadedAssemblies)
				return _loadedAssemblies.ToArray();
		}
	}
}
