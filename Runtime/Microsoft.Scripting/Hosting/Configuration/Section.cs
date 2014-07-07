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
using System.Configuration;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Hosting.Configuration
{
	//
	// <configSections>
	//   <section name="microsoft.scripting" type="Microsoft.Scripting.Hosting.Configuration.Section, Microsoft.Scripting" />
	// </configSections>
	//
	// <microsoft.scripting [debugMode="{bool}"]? [privateBinding="{bool}"]?>
	//   <languages>  <!-- BasicMap with key (type): inherits language nodes, overwrites previous nodes (last wins) -->
	//     <language names="{(semi)colon-separated}" extensions="{(semi)colon-separated-with-optional-dot}" type="{AQTN}" [displayName="{string}"]? />
	//   </languages>
	//
	//   <options>    <!-- AddRemoveClearMap with key (option, [language]?): inherits language nodes, overwrites previous nodes (last wins) -->
	//     <set option="{string}" value="{string}" [language="{language-name}"]? />
	//     <clear />
	//     <remove option="{string}" [language="{language-name}"]? />
	//   </options>
	//
	// </microsoft.scripting>
	//
	/// <summary>言語およびそのオプションに関する情報を格納する構成ファイル内のセクションを表します。</summary>
	public class Section : ConfigurationSection
	{
		/// <summary>このセクションの名前を表します。</summary>
		public static readonly string SectionName = "microsoft.scripting";

		const string _DebugMode = "debugMode";
		const string _PrivateBinding = "privateBinding";
		const string _Languages = "languages";
		const string _Options = "options";

		static ConfigurationPropertyCollection _Properties = new ConfigurationPropertyCollection() {
            new ConfigurationProperty(_DebugMode, typeof(bool?), null), 
            new ConfigurationProperty(_PrivateBinding, typeof(bool?), null), 
            new ConfigurationProperty(_Languages, typeof(LanguageElementCollection), null, ConfigurationPropertyOptions.IsDefaultCollection), 
            new ConfigurationProperty(_Options, typeof(OptionElementCollection), null), 
        };

		/// <summary>プロパティのコレクションを取得します。</summary>
		protected override ConfigurationPropertyCollection Properties { get { return _Properties; } }

		/// <summary>ランタイムがデバッグモードで動作するかどうかを示す値を取得または設定します。</summary>
		public bool? DebugMode
		{
			get { return (bool?)base[_DebugMode]; }
			set { base[_DebugMode] = value; }
		}

		/// <summary>CLR 可視性チェックを無視するかどうかを示す値を取得または設定します。</summary>
		public bool? PrivateBinding
		{
			get { return (bool?)base[_PrivateBinding]; }
			set { base[_PrivateBinding] = value; }
		}

		/// <summary>このセクションから言語に関する構成要素を取得します。</summary>
		/// <returns>セクションに含まれる言語構成要素。</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public IEnumerable<LanguageElement> GetLanguages()
		{
			var languages = this[_Languages] as LanguageElementCollection;
			if (languages != null)
				return languages.Cast<LanguageElement>();
			return Enumerable.Empty<LanguageElement>();
		}

		/// <summary>このセクションから言語オプションに関する構成要素を取得します。</summary>
		/// <returns>セクションに含まれる言語オプション構成要素。</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public IEnumerable<OptionElement> GetOptions()
		{
			var options = this[_Options] as OptionElementCollection;
			if (options != null)
				return options.Cast<OptionElement>();
			return Enumerable.Empty<OptionElement>();
		}

		static Section LoadFromFile(Stream configFileStream)
		{
			var result = new Section();
			using (var reader = XmlReader.Create(configFileStream))
			{
				if (reader.ReadToDescendant("configuration") && reader.ReadToDescendant(SectionName))
					result.DeserializeElement(reader, false);
				else
					return null;
			}
			return result;
		}

		/// <summary>指定されたストリームから <see cref="ScriptRuntimeSetup"/> オブジェクトに構成情報を読み込みます。</summary>
		/// <param name="setup">構成情報を書き込む <see cref="ScriptRuntimeSetup"/> を指定します。</param>
		/// <param name="configFileStream">構成情報を読み取るストリームを指定します。</param>
		internal static void LoadRuntimeSetup(ScriptRuntimeSetup setup, Stream configFileStream)
		{
			Section config;
			if (configFileStream != null)
				config = LoadFromFile(configFileStream);
			else
				config = System.Configuration.ConfigurationManager.GetSection(Section.SectionName) as Section;
			if (config == null)
				return;
			if (config.DebugMode.HasValue)
				setup.DebugMode = config.DebugMode.Value;
			if (config.PrivateBinding.HasValue)
				setup.PrivateBinding = config.PrivateBinding.Value;
			foreach (var languageConfig in config.GetLanguages())
			{
				var names = languageConfig.GetNamesArray();
				var extensions = languageConfig.GetExtensionsArray();
				var displayName = languageConfig.DisplayName ?? (names.Length > 0 ? names[0] : languageConfig.Type);
				// Honor the latest-wins behavior of the <languages> tag for options that were already included in the setup object;
				// Keep the options though.
				var language = setup.LanguageSetups.FirstOrDefault(x => x.TypeName == languageConfig.Type);
				if (language != null)
				{
					language.Names.Clear();
					foreach (string name in names)
						language.Names.Add(name);
					language.FileExtensions.Clear();
					foreach (string extension in extensions)
						language.FileExtensions.Add(extension);
					language.DisplayName = displayName;
				}
				else
					setup.LanguageSetups.Add(new LanguageSetup(languageConfig.Type, displayName, names, extensions));
			}
			foreach (var option in config.GetOptions())
			{
				if (string.IsNullOrEmpty(option.Language))
					setup.Options[option.Name] = option.Value; // common option:
				else // language specific option:
				{
					var language = setup.LanguageSetups.FirstOrDefault(x => x.Names.Any(s => DlrConfiguration.LanguageNameComparer.Equals(s, option.Language)));
					if (language != null)
						language.Options[option.Name] = option.Value;
					else
						throw new ConfigurationErrorsException(string.Format("Unknown language name: '{0}'", option.Language));
				}
			}
		}
	}
}