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
using System.Reflection;
using System.Threading;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>言語に関する構成情報を表します。</summary>
	sealed class LanguageConfiguration
	{
		IDictionary<string, object> _options;
		LanguageContext _context;
		string _displayName;

		/// <summary>この構成にロードされた <see cref="Microsoft.Scripting.Runtime.LanguageContext"/> を取得します。</summary>
		public LanguageContext LanguageContext { get { return _context; } }

		/// <summary>言語プロバイダのアセンブリ修飾型名を取得します。</summary>
		public AssemblyQualifiedTypeName ProviderName { get; private set; }

		/// <summary>
		/// 言語プロバイダのアセンブリ修飾型名、言語の表示名、オプションを使用して、<see cref="Microsoft.Scripting.Runtime.LanguageConfiguration"/>
		/// クラスの新しいインスタンスを初期化します。
		/// </summary>
		/// <param name="providerName">言語プロバイダのアセンブリ修飾型名を指定します。</param>
		/// <param name="displayName">言語の表示名を指定します。</param>
		/// <param name="options">オプションを指定します。</param>
		public LanguageConfiguration(AssemblyQualifiedTypeName providerName, string displayName, IDictionary<string, object> options)
		{
			ProviderName = providerName;
			_displayName = displayName;
			_options = options;
		}

		/// <summary>
		/// 指定した <see cref="ScriptDomainManager"/> を使用して <see cref="Microsoft.Scripting.Runtime.LanguageContext"/> オブジェクトを作成し、このインスタンスに関連付けます。
		/// 潜在的にユーザーコードを呼び出す可能性があるので、ロック内で呼び出さないでください。
		/// </summary>
		/// <param name="domainManager"><see cref="Microsoft.Scripting.Runtime.LanguageContext"/> オブジェクトの作成に利用する <see cref="ScriptDomainManager"/> を指定します。</param>
		/// <param name="alreadyLoaded"><see cref="Microsoft.Scripting.Runtime.LanguageContext"/> オブジェクトが既にロードされているかどうかを示す値を格納する変数を指定します。</param>
		/// <exception cref="Microsoft.Scripting.InvalidImplementationException"><see cref="Microsoft.Scripting.Runtime.LanguageContext"/> の実装のインスタンス化に失敗しました。</exception>
		internal LanguageContext LoadLanguageContext(ScriptDomainManager domainManager, out bool alreadyLoaded)
		{
			if (_context == null)
			{
				// アセンブリのロードエラーはそのまま送出されます。
				var assembly = domainManager.Platform.LoadAssembly(ProviderName.AssemblyName.FullName);

				Type type = assembly.GetType(ProviderName.TypeName);
				if (type == null)
					throw new InvalidOperationException(String.Format(
						"言語のロードに失敗しました '{0}': アセンブリ '{1}' は型 '{2}' を含んでいません。", _displayName, assembly.Location, ProviderName.TypeName
					));

				if (!type.IsSubclassOf(typeof(LanguageContext)))
					throw new InvalidOperationException(String.Format(
						"言語のロードに失敗しました '{0}': 型 '{1}' は LanguageContext を継承していないため、有効な言語プロバイダではありません。", _displayName, type
					));

				LanguageContext context;
				try { context = (LanguageContext)Activator.CreateInstance(type, new object[] { domainManager, _options }); }
				catch (TargetInvocationException e)
				{
					throw new TargetInvocationException(String.Format("Failed to load language '{0}': {1}", _displayName, e.InnerException.Message), e.InnerException);
				}
				catch (Exception e) { throw new InvalidImplementationException(Strings.InvalidCtorImplementation(type, e.Message), e); }
				alreadyLoaded = Interlocked.CompareExchange(ref _context, context, null) != null;
			}
			else
				alreadyLoaded = true;
			return _context;
		}
	}

	/// <summary>動的言語ランタイムの構成情報を格納します。</summary>
	public sealed class DlrConfiguration
	{
		bool _frozen;

		/// <summary>ファイルの拡張子を比較する <see cref="System.StringComparer"/> を取得します。</summary>
		public static StringComparer FileExtensionComparer { get { return StringComparer.OrdinalIgnoreCase; } }
		/// <summary>言語の名前を比較する <see cref="System.StringComparer"/> を取得します。</summary>
		public static StringComparer LanguageNameComparer { get { return StringComparer.OrdinalIgnoreCase; } }
		/// <summary>オプションの名前を比較する <see cref="System.StringComparer"/> を取得します。</summary>
		public static StringComparer OptionNameComparer { get { return StringComparer.Ordinal; } }

		Dictionary<AssemblyQualifiedTypeName, LanguageConfiguration> _languages;
		IDictionary<string, object> _options;
		Dictionary<string, LanguageConfiguration> _languageNames;
		Dictionary<string, LanguageConfiguration> _languageExtensions;
		Dictionary<Type, LanguageConfiguration> _loadedProviderTypes;

		/// <summary>指定された構成に関する情報から <see cref="Microsoft.Scripting.Runtime.DlrConfiguration"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="debugMode">ランタイムがデバッグモードで動作するかどうかを示す値を指定します。</param>
		/// <param name="privateBinding">CLR 可視性チェックを無視するかどうかを示す値を指定します。</param>
		/// <param name="options">大文字と小文字を区別するオプションの名前と値の組を指定します。</param>
		public DlrConfiguration(bool debugMode, bool privateBinding, IDictionary<string, object> options)
		{
			ContractUtils.RequiresNotNull(options, "options");
			DebugMode = debugMode;
			PrivateBinding = privateBinding;
			_options = options;

			_languageNames = new Dictionary<string, LanguageConfiguration>(LanguageNameComparer);
			_languageExtensions = new Dictionary<string, LanguageConfiguration>(FileExtensionComparer);
			_languages = new Dictionary<AssemblyQualifiedTypeName, LanguageConfiguration>();
			_loadedProviderTypes = new Dictionary<Type, LanguageConfiguration>();
		}

		/// <summary>ランタイムがデバッグモードで動作するかどうかを示す値を取得します。</summary>
		/// <remarks>
		/// デバッグモードでは次のように動作します。
		/// 1) デバッグ可能なメソッドに対してシンボルが出力されます。(<see cref="Microsoft.Scripting.SourceUnit"/> に関連付けられたメソッド).
		/// 2) 非収集型に対してデバッグ可能なメソッドが出力されます (これは動的メソッドデバッグの上での CLR の制限によるものです).
		/// 3) すべてのメソッドに対して JIT 最適化が無効になります。
		/// 4) 言語はこの値に基づいた最適化を無効化する可能性があります。
		/// </remarks>
		public bool DebugMode { get; private set; }

		/// <summary>CLR 可視性チェックを無視するかどうかを示す値を取得します。</summary>
		public bool PrivateBinding { get; private set; }

		/// <summary>この構成に言語構成を追加します。</summary>
		/// <param name="languageTypeName">言語プロバイダのアセンブリ修飾型名を指定します。</param>
		/// <param name="displayName">言語の表示名を指定します。</param>
		/// <param name="names">大文字と小文字を区別しない言語の名前のリストを指定します。</param>
		/// <param name="fileExtensions">大文字と小文字を区別しないファイルの拡張子のリストを指定します。</param>
		/// <param name="options">オプションのリストを指定します。</param>
		public void AddLanguage(string languageTypeName, string displayName, IList<string> names, IList<string> fileExtensions, IDictionary<string, object> options)
		{
			ContractUtils.Requires(!_frozen, "ランタイムが初期化された後は構成は変更できません。");
			ContractUtils.Requires(
				names.All(id => !string.IsNullOrEmpty(id) && !_languageNames.ContainsKey(id)),
				"names", "言語の名前は null や空文字ではなく、また言語間で重複しない文字列でなければなりません。"
			);
			ContractUtils.Requires(
				fileExtensions.All(ext => !string.IsNullOrEmpty(ext) && !_languageExtensions.ContainsKey(ext)),
				"fileExtensions", "ファイル拡張子は null や空文字ではなく、また言語間で重複しない文字列でなければなりません。"
			);
			ContractUtils.RequiresNotNull(displayName, "displayName");
			if (string.IsNullOrEmpty(displayName))
			{
				ContractUtils.Requires(names.Count > 0, "displayName", "空文字でない表示名および空のリストでない言語名を持たなければなりません。");
				displayName = names[0];
			}
			var aqtn = AssemblyQualifiedTypeName.ParseArgument(languageTypeName, "languageTypeName");
			if (_languages.ContainsKey(aqtn))
				throw new ArgumentException(string.Format("言語が型名 '{0}' において重複しています。", aqtn), "languageTypeName");

			// グローバルな言語オプションを最初に追加し、言語固有のオプションで上書き可能にします。
			var mergedOptions = new Dictionary<string, object>(_options);

			// グローバルオプションを言語固有のオプションで置き換えます。
			foreach (var option in options)
				mergedOptions[option.Key] = option.Value;
			var config = new LanguageConfiguration(aqtn, displayName, mergedOptions);

			_languages.Add(aqtn, config);

			// 言語 ID 屋拡張子のリストは重複が許されます。
			foreach (var name in names)
				_languageNames[name] = config;
			foreach (var ext in fileExtensions)
				_languageExtensions[NormalizeExtension(ext)] = config;
		}

		/// <summary>拡張子を正規化します。ドットで始まらない場合はドットで始まるような拡張子を返します。</summary>
		/// <param name="extension">正規化する拡張子を指定します。</param>
		internal static string NormalizeExtension(string extension) { return extension[0] == '.' ? extension : "." + extension; }

		/// <summary>このオブジェクトを変更不可能にします。</summary>
		internal void Freeze()
		{
			Debug.Assert(!_frozen);
			_frozen = true;
		}

		/// <summary>言語のロードを試みます。成功した場合は <c>true</c> を返します。</summary>
		/// <param name="manager">言語のロードに使用する <see cref="ScriptDomainManager"/> を指定します。</param>
		/// <param name="providerName">ロードする言語プロバイダのアセンブリ修飾型名を指定します。</param>
		/// <param name="language">ロードされた言語プロバイダを格納する変数を指定します。</param>
		internal bool TryLoadLanguage(ScriptDomainManager manager, AssemblyQualifiedTypeName providerName, out LanguageContext language)
		{
			Assert.NotNull(manager);
			LanguageConfiguration config;
			if (_languages.TryGetValue(providerName, out config))
			{
				language = LoadLanguageContext(manager, config);
				return true;
			}
			language = null;
			return false;
		}

		/// <summary>言語のロードを試みます。成功した場合は <c>true</c> を返します。</summary>
		/// <param name="manager">言語のロードに使用する <see cref="ScriptDomainManager"/> を指定します。</param>
		/// <param name="str">ロードする言語を識別するファイル拡張子または言語名を指定します。</param>
		/// <param name="isExtension"><paramref name="str"/> がファイル拡張子を表すかどうかを示す値を指定します。</param>
		/// <param name="language">ロードされた言語プロバイダを格納する変数を指定します。</param>
		internal bool TryLoadLanguage(ScriptDomainManager manager, string str, bool isExtension, out LanguageContext language)
		{
			Assert.NotNull(manager, str);
			LanguageConfiguration config;
			if ((isExtension ? _languageExtensions : _languageNames).TryGetValue(str, out config))
			{
				language = LoadLanguageContext(manager, config);
				return true;
			}
			language = null;
			return false;
		}

		LanguageContext LoadLanguageContext(ScriptDomainManager manager, LanguageConfiguration config)
		{
			bool alreadyLoaded;
			var language = config.LoadLanguageContext(manager, out alreadyLoaded);

			if (!alreadyLoaded)
			{
				// 単一の言語が 2 つの異なるアセンブリ修飾型名によって登録されていないかを確認します。
				// 型をロードすることなしに 2 つのアセンブリ修飾型名が同じ型を参照していることを確かめる方法はないので、今ロードを行います。
				// ロックの実行中にユーザーコードが呼び出されることを避けるため、チェックは config.LoadLanguageContext の実行後に行います。
				lock (_loadedProviderTypes)
				{
					LanguageConfiguration existingConfig;
					Type type = language.GetType();
					if (_loadedProviderTypes.TryGetValue(type, out existingConfig))
						throw new InvalidOperationException(String.Format("型 '{0}' によって実装された言語は名前 '{1}' を使用してすでにロードされています。", config.ProviderName, existingConfig.ProviderName));
					_loadedProviderTypes.Add(type, config);
				}
			}
			return language;
		}

		/// <summary>指定された言語プロバイダから言語の名前のリストを取得します。</summary>
		/// <param name="context">言語に対する <see cref="Microsoft.Scripting.Runtime.LanguageContext"/> を指定します。</param>
		public string[] GetLanguageNames(LanguageContext context)
		{
			ContractUtils.RequiresNotNull(context, "context");
			return _languageNames.Where(x => x.Value.LanguageContext == context).Select(x => x.Key).ToArray();
		}

		/// <summary>この構成に登録されているすべての言語名のリストを取得します。</summary>
		public string[] GetLanguageNames() { return _languageNames.Keys.ToArray(); }

		/// <summary>指定された言語プロバイダから言語のファイル拡張子のリストを取得します。</summary>
		/// <param name="context">言語に対する <see cref="Microsoft.Scripting.Runtime.LanguageContext"/> を指定します。</param>
		public string[] GetFileExtensions(LanguageContext context)
		{
			ContractUtils.RequiresNotNull(context, "context");
			return _languageExtensions.Where(x => x.Value.LanguageContext == context).Select(x => x.Key).ToArray();
		}

		/// <summary>この構成に登録されているすべての言語のファイル拡張子のリストを取得します。</summary>
		public string[] GetFileExtensions() { return _languageExtensions.Keys.ToArray(); }

		/// <summary>指定された言語プロバイダから言語の構成情報を取得します。</summary>
		/// <param name="context">言語に対する <see cref="Microsoft.Scripting.Runtime.LanguageContext"/> を指定します。</param>
		internal LanguageConfiguration GetLanguageConfig(LanguageContext context) { return _languages.Values.FirstOrDefault(x => x.LanguageContext == context); }
	}
}
