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
	/// <summary>�N�����̈������� <see cref="ConsoleHostOptions"/> ����͂��܂��B</summary>
	public class ConsoleHostOptionsParser
	{
		/// <summary>��͂��ꂽ <see cref="ConsoleHostOptions"/> ���擾���܂��B</summary>
		public ConsoleHostOptions Options { get; private set; }

		/// <summary>�����^�C���̃Z�b�g�A�b�v�����擾���܂��B</summary>
		public ScriptRuntimeSetup RuntimeSetup { get; private set; }

		/// <summary>��ɂȂ�I�v�V�����ƃ����^�C���̃Z�b�g�A�b�v�����g�p���āA<see cref="Microsoft.Scripting.Hosting.Shell.ConsoleHostOptionsParser"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="options">��ɂȂ�I�v�V�����̃C���X�^���X���w�肵�܂��B</param>
		/// <param name="runtimeSetup">�����^�C���̃Z�b�g�A�b�v�����w�肵�܂��B</param>
		public ConsoleHostOptionsParser(ConsoleHostOptions options, ScriptRuntimeSetup runtimeSetup)
		{
			ContractUtils.RequiresNotNull(options, "options");
			ContractUtils.RequiresNotNull(runtimeSetup, "runtimeSetup");
			Options = options;
			RuntimeSetup = runtimeSetup;
		}

		/// <summary>�w�肳�ꂽ�N�����̈�������͂��� <see cref="Options"/> ��ύX���܂��B</summary>
		/// <param name="args">��͂���N�����̈������w�肵�܂��B</param>
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
							throw new InvalidOptionException(string.Format("�s���Ȍ���ID '{0}'.", value));
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
		/// name == null �͈������I�v�V�������w�肵�Ă��Ȃ����Ƃ�\���܂��B���Ȃ킿�A�l�͈����S�̂��܂݂܂��B
		/// name == "" �̓I�v�V�����̖��O���� (�����̃Z�p���[�^) �ł��邱�Ƃ�\���܂��B���̏ꍇ�A�l�� null �ł��B
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

		/// <summary>�w�肳�ꂽ�I�v�V�����ɒl���K�{�ł���A�l���w�肳��Ă���K�v�����邱�Ƃ��m�F���܂��B</summary>
		/// <param name="optionName">�I�v�V�����̖��O���w�肵�܂��B</param>
		/// <param name="value">�I�v�V�����̒l���w�肵�܂��B</param>
		protected void OptionValueRequired(string optionName, string value)
		{
			if (value == null)
				throw new InvalidOptionException(String.Format(CultureInfo.CurrentCulture, "�I�v�V���� '{0}' �ɂ͒l���K�v�ł��B", optionName));
		}

		[Conditional("SILVERLIGHT")]
		void OptionNotAvailableOnSilverlight(string optionName) { throw new InvalidOptionException(string.Format("�I�v�V���� '{0}' �� Silverlight �ł͗��p�ł��܂���B", optionName)); }
	}
}
