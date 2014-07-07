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
using System.IO;
using System.Linq;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting
{
	/// <summary><see cref="Microsoft.Scripting.Hosting.ScriptRuntime"/> のセットアップに必要な情報を格納します。</summary>
	[Serializable]
	public sealed class ScriptRuntimeSetup
	{
		// host specification:
		Type _hostType = typeof(ScriptHost);

		// DLR options:
		bool _debugMode;
		bool _privateBinding;

		bool _frozen;

		/// <summary><see cref="Microsoft.Scripting.Hosting.ScriptRuntime"/>クラスの新しいインスタンスを初期化します。</summary>
		public ScriptRuntimeSetup()
		{
			LanguageSetups = new List<LanguageSetup>();
			Options = new Dictionary<string, object>();
			HostArguments = new List<object>();
		}

		/// <summary>ランタイムにロードされる言語に対するセットアップ情報を取得します。</summary>
		public IList<LanguageSetup> LanguageSetups { get; private set; }

		/// <summary>ランタイムがデバッグモードで動作するかどうかを示す値を取得または設定します。</summary>
		/// <remarks>
		/// デバッグモードでは次のように動作します。
		/// 1) デバッグ可能なメソッドに対してシンボルが出力されます。(<see cref="Microsoft.Scripting.SourceUnit"/> に関連付けられたメソッド).
		/// 2) 非収集型に対してデバッグ可能なメソッドが出力されます (これは動的メソッドデバッグの上での CLR の制限によるものです).
		/// 3) すべてのメソッドに対して JIT 最適化が無効になります。
		/// 4) 言語はこの値に基づいた最適化を無効化する可能性があります。
		/// </remarks>
		public bool DebugMode
		{
			get { return _debugMode; }
			set
			{
				CheckFrozen();
				_debugMode = value;
			}
		}

		/// <summary>CLR 可視性チェックを無視するかどうかを示す値を取得または設定します。</summary>
		public bool PrivateBinding
		{
			get { return _privateBinding; }
			set
			{
				CheckFrozen();
				_privateBinding = value;
			}
		}

		/// <summary>
		/// ホストの型を取得または設定します。
		/// <see cref="Microsoft.Scripting.Hosting.ScriptHost"/> のどの派生型にも設定できます。
		/// 設定すると、ホストはランタイムの振る舞いを制御するために一定のメソッドをオーバーライドできるようになります。
		/// </summary>
		public Type HostType
		{
			get { return _hostType; }
			set
			{
				ContractUtils.RequiresNotNull(value, "value");
				ContractUtils.Requires(typeof(ScriptHost).IsAssignableFrom(value), "value", "ScriptHost またはその派生型である必要があります。");
				CheckFrozen();
				_hostType = value;
			}
		}

		/// <summary>大文字と小文字を区別するオプションの名前と値の組を取得または設定します。</summary>
		public IDictionary<string, object> Options { get; private set; }

		/// <summary>ホスト型のコンストラクタに渡される引数を取得または設定します。</summary>
		public IList<object> HostArguments { get; private set; }

		/// <summary>動的言語ランタイムの構成情報にこのオブジェクトを変換します。</summary>
		internal DlrConfiguration ToConfiguration()
		{
			ContractUtils.Requires(LanguageSetups.Count > 0, "LanguageSetups", "ScriptRuntimeSetup は少なくとも 1 つの LanguageSetup を含んでいる必要があります。");

			// prepare
			var setups = new ReadOnlyCollection<LanguageSetup>(LanguageSetups.ToArray());
			var hostArguments = new ReadOnlyCollection<object>(HostArguments.ToArray());
			var options = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(Options));
			var config = new DlrConfiguration(_debugMode, _privateBinding, options);

			// validate
			foreach (var language in setups)
				config.AddLanguage(language.TypeName, language.DisplayName, language.Names, language.FileExtensions, language.Options);

			// commit
			LanguageSetups = setups;
			Options = options;
			HostArguments = hostArguments;
			Freeze(setups);

			return config;
		}

		void Freeze(ReadOnlyCollection<LanguageSetup> setups)
		{
			foreach (var language in setups)
				language.Freeze();
			_frozen = true;
		}

		void CheckFrozen()
		{
			if (_frozen)
				throw new InvalidOperationException("ScriptRuntime を作成した後に ScriptRuntimeSetup を変更することはできません。");
		}

		/// <summary> .NET の構成システム (.config ファイル) からセットアップ情報を読み出します。何も構成がない場合は、空のセットアップ情報を返します。</summary>
		public static ScriptRuntimeSetup ReadConfiguration()
		{
			var setup = new ScriptRuntimeSetup();
			Configuration.Section.LoadRuntimeSetup(setup, null);
			return setup;
		}

		/// <summary>指定された XML ファイルから構成情報を読み出します。</summary>
		/// <param name="configFileStream">構成情報を格納している XML ファイルを表すストリームを指定します。</param>
		public static ScriptRuntimeSetup ReadConfiguration(Stream configFileStream)
		{
			ContractUtils.RequiresNotNull(configFileStream, "configFileStream");
			var setup = new ScriptRuntimeSetup();
			Configuration.Section.LoadRuntimeSetup(setup, configFileStream);
			return setup;
		}

		/// <summary>指定された XML ファイルから構成情報を読み出します。</summary>
		/// <param name="configFilePath">構成情報を格納している XML ファイルの場所を示すパスを指定します。</param>
		public static ScriptRuntimeSetup ReadConfiguration(string configFilePath)
		{
			ContractUtils.RequiresNotNull(configFilePath, "configFilePath");
			using (var stream = File.OpenRead(configFilePath))
				return ReadConfiguration(stream);
		}
	}
}
