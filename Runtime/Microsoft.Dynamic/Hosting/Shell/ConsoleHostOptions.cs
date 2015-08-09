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
		public KeyValuePair<string, string>[] GetHelp()
		{
			return new[] {
                new KeyValuePair<string, string>("/help",                       "���̃w���v��\�����܂��B"),
                new KeyValuePair<string, string>("/lang:<�g���q>",              "�֘A�t����ꂽ�g���q (py, js, vb, rb) ���猾����w�肵�܂��B�ŏ��̃t�@�C���̊g���q���画�f����܂��B����l�� IronPython �ł��B"),
                new KeyValuePair<string, string>("/paths:<�t�@�C���p�X���X�g>", "�C���|�[�g�p�X�̃Z�~�R������؂�̃��X�g (/run �̂�)."),
                new KeyValuePair<string, string>("/setenv:<�ϐ�1=�l1;...>",   "�w�肳�ꂽ���ϐ����R���\�[���v���Z�X�ɑ΂��Đݒ肵�܂��BSilverlight �ł͗��p�ł��܂���B"),
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
