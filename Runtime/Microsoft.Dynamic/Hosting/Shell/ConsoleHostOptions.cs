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

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Scripting.Hosting.Shell
{
	/// <summary><see cref="ConsoleHost"/> ���Q�Ƃ���I�v�V������\���܂��B</summary>
	public class ConsoleHostOptions
	{
		/// <summary><see cref="ConsoleHostOptionsParser"/> �ɂ���ĉ�͂��X�L�b�v���ꂽ�������擾���܂��B</summary>
		public List<string> IgnoredArgs { get; private set; }

		/// <summary><see cref="ConsoleHost"/> �����s����t�@�C�������擾�܂��͐ݒ肵�܂��B�w�肳��Ă��Ȃ��ꍇ�� <c>null</c> �ɂȂ�܂��B</summary>
		public string RunFile { get; set; }

		/// <summary>�����Ŏw�肳�ꂽ�ǉ��̌����p�X���擾�܂��͐ݒ肵�܂��B�w�肳��Ă��Ȃ��ꍇ�� <c>null</c> �ɂȂ�܂��B</summary>
		public ReadOnlyCollection<string> SourceUnitSearchPaths { get; set; }

		/// <summary><see cref="ConsoleHost"/> �ɂ���Ď��s����铮����擾�܂��͐ݒ肵�܂��B</summary>
		public ConsoleHostAction RunAction { get; set; }

		/// <summary>�����ō쐬�܂��͏㏑�����ꂽ���ϐ����擾���܂��B</summary>
		public List<string> EnvironmentVars { get; private set; }

		/// <summary><see cref="ConsoleHost"/> �Ŏg�p����錾��v���o�C�_�̌^�����擾�܂��͐ݒ肵�܂��B</summary>
		public string LanguageProvider { get; set; }

		/// <summary>����v���o�C�_�������Ŏw�肳�ꂽ���ǂ����������l���擾�܂��͐ݒ肵�܂��B</summary>
		public bool HasLanguageProvider { get; set; }

		/// <summary><see cref="Microsoft.Scripting.Hosting.Shell.ConsoleHostOptions"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		public ConsoleHostOptions()
		{
			IgnoredArgs = new List<string>();
			EnvironmentVars = new List<string>();
		}

		/// <summary><see cref="ConsoleHost"/> �̋N�����I�v�V�����Ɋւ���w���v���擾���܂��B</summary>
		/// <returns>�I�v�V���������Ɛ������i�[���ꂽ 2 �����z��B</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional")] // TODO: fix
		public KeyValuePair<string, string>[] GetHelp()
		{
			return new[] {
                new KeyValuePair<string, string>("/help",                     "Displays this help."),
                new KeyValuePair<string, string>("/lang:<extension>",         "Specify language by the associated extension (py, js, vb, rb). Determined by an extension of the first file. Defaults to IronPython."),
                new KeyValuePair<string, string>("/paths:<file-path-list>",   "Semicolon separated list of import paths (/run only)."),
                new KeyValuePair<string, string>("/setenv:<var1=value1;...>", "Sets specified environment variables for the console process. Not available on Silverlight."),
            };
		}
	}

	/// <summary><see cref="ConsoleHost"/> �ɂ���Ď��s����铮��������܂��B</summary>
	public enum ConsoleHostAction
	{
		/// <summary><see cref="ConsoleHost"/> �͉������܂���B</summary>
		None,
		/// <summary><see cref="ConsoleHost"/> �̓R���\�[�������s���܂��B</summary>
		RunConsole,
		/// <summary><see cref="ConsoleHost"/> �͎w�肳�ꂽ�t�@�C�������s���܂��B</summary>
		RunFile,
		/// <summary><see cref="ConsoleHost"/> �̓w���v��\�����܂��B</summary>
		DisplayHelp
	}
}
