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
using System.Globalization;
using System.Linq;
using Microsoft.Scripting.Utils;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Hosting.Shell
{
	/// <summary>起動時の引数から <see cref="ConsoleHostOptions"/> を解析します。</summary>
	public class ConsoleHostOptionsParser
	{
		/// <summary>解析された <see cref="ConsoleHostOptions"/> を取得します。</summary>
		public ConsoleHostOptions Options { get; private set; }

		/// <summary>ランタイムのセットアップ情報を取得します。</summary>
		public ScriptRuntimeSetup RuntimeSetup { get; private set; }

		/// <summary>基になるオプションとランタイムのセットアップ情報を使用して、<see cref="Microsoft.Scripting.Hosting.Shell.ConsoleHostOptionsParser"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="options">基になるオプションのインスタンスを指定します。</param>
		/// <param name="runtimeSetup">ランタイムのセットアップ情報を指定します。</param>
		public ConsoleHostOptionsParser(ConsoleHostOptions options, ScriptRuntimeSetup runtimeSetup)
		{
			ContractUtils.RequiresNotNull(options, "options");
			ContractUtils.RequiresNotNull(runtimeSetup, "runtimeSetup");
			Options = options;
			RuntimeSetup = runtimeSetup;
		}

		/// <summary>指定された起動時の引数を解析して <see cref="Options"/> を変更します。</summary>
		/// <param name="args">解析する起動時の引数を指定します。</param>
		/// <exception cref="InvalidOptionException"></exception>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		public void Parse(string[] args)
		{
			ContractUtils.RequiresNotNull(args, "args");
			int i = 0;
			while (i < args.Length)
			{
				string name, value;
				var current = args[i++];
				ParseOption(current, out name, out value);
				switch (name)
				{
					case "console":
						Options.RunAction = ConsoleHostAction.RunConsole;
						break;
					case "run":
						OptionValueRequired(name, value);
						Options.RunAction = ConsoleHostAction.RunFile;
						Options.RunFile = value;
						break;
					case "lang":
						OptionValueRequired(name, value);
						var provider = RuntimeSetup.LanguageSetups.Where(x => x.Names.Any(n => DlrConfiguration.LanguageNameComparer.Equals(n, value))).Select(x => x.TypeName).FirstOrDefault();
						if (provider == null)
							throw new InvalidOptionException(string.Format("不明な言語ID '{0}'.", value));
						Options.LanguageProvider = provider;
						Options.HasLanguageProvider = true;
						break;
					case "path":
					case "paths":
						OptionValueRequired(name, value);
						Options.SourceUnitSearchPaths = value.Split(';').ToReadOnly();
						break;
					case "setenv":
						OptionNotAvailableOnSilverlight(name);
						Options.EnvironmentVars.AddRange(value.Split(';'));
						break;
					// first unknown/non-option:
					case null:
					default:
						Options.IgnoredArgs.Add(current);
						goto case "";
					// host/passthru argument separator
					case "/":
					case "":
						// ignore all arguments starting with the next one (arguments are not parsed):
						while (i < args.Length)
							Options.IgnoredArgs.Add(args[i++]);
						break;
				}
			}
		}

		/// <summary>
		/// name == null は引数がオプションを指定していないことを表します。すなわち、値は引数全体を含みます。
		/// name == "" はオプションの名前が空 (引数のセパレータ) であることを表します。その場合、値は null です。
		/// </summary>
		void ParseOption(string arg, out string name, out string value)
		{
			Debug.Assert(arg != null);
			int colon = arg.IndexOf(':');
			if (colon >= 0)
			{
				name = arg.Substring(0, colon);
				value = arg.Substring(colon + 1);
			}
			else
			{
				name = arg;
				value = null;
			}
			if (name.StartsWith("--"))
				name = name.Substring("--".Length);
			else if (name.StartsWith("-") && name.Length > 1)
				name = name.Substring("-".Length);
			else if (name.StartsWith("/") && name.Length > 1)
				name = name.Substring("/".Length);
			else
			{
				value = name;
				name = null;
			}
			if (name != null)
				name = name.ToLower(CultureInfo.InvariantCulture);
		}

		/// <summary>指定されたオプションに値が必須であり、値が指定されている必要があることを確認します。</summary>
		/// <param name="optionName">オプションの名前を指定します。</param>
		/// <param name="value">オプションの値を指定します。</param>
		protected void OptionValueRequired(string optionName, string value)
		{
			if (value == null)
				throw new InvalidOptionException(String.Format(CultureInfo.CurrentCulture, "オプション '{0}' には値が必要です。", optionName));
		}

		[Conditional("SILVERLIGHT")]
		void OptionNotAvailableOnSilverlight(string optionName) { throw new InvalidOptionException(string.Format("オプション '{0}' は Silverlight では利用できません。", optionName)); }
	}
}
