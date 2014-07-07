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
using System.Collections.ObjectModel;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting.Shell
{
	/// <summary>�R���\�[���S�̂ɑ΂���I�v�V������\���܂��B</summary>
	[Serializable]
	public class ConsoleOptions
	{
		/// <summary>�R���\�[���Ŏ����I�ɃC���f���g���s�����ǂ����������l���擾�܂��͐ݒ肵�܂��B</summary>
		public bool AutoIndent { get; set; }

		/// <summary>��O���n���h�����邩�ǂ����������l���擾�܂��͐ݒ肵�܂��B</summary>
		public bool HandleExceptions { get; set; }

		/// <summary>�R���\�[���Ń^�u�⊮���s�����ǂ����������l���擾�܂��͐ݒ肵�܂��B</summary>
		public bool TabCompletion { get; set; }

		/// <summary>�F���̃R���\�[�����g�p���邩�ǂ����������l���擾�܂��͐ݒ肵�܂��B</summary>
		public bool ColorfulConsole { get; set; }

		/// <summary>�g�p���@��\�����邩�ǂ����������l���擾�܂��͐ݒ肵�܂��B</summary>
		public bool PrintUsage { get; set; }

		/// <summary>�N�����I�v�V�����ŗ^����ꂽ���e�����̃X�N���v�g�R�}���h���擾�܂��͐ݒ肵�܂��B</summary>
		public string Command { get; set; }

		/// <summary>���s����t�@�C�������擾�܂��͐ݒ肵�܂��B</summary>
		public string FileName { get; set; }

		/// <summary>�o�[�W������\�����邩�ǂ����������l���擾�܂��͐ݒ肵�܂��B</summary>
		public bool PrintVersion { get; set; }

		/// <summary>�v�����v�g���o�����ƂȂ������Ɏ��s���I�������邩�ǂ����������l���擾�܂��͐ݒ肵�܂��B</summary>
		public bool Exit { get; set; }

		/// <summary>�R���\�[���̎����C���f���g�T�C�Y���擾�܂��͐ݒ肵�܂��B</summary>
		public int AutoIndentSize { get; set; }

		/// <summary>��͂���Ȃ������c��̈������擾�܂��͐ݒ肵�܂��B</summary>
		public ReadOnlyCollection<string> RemainingArgs { get; set; }

		/// <summary>�����������s�����ǂ����������l���擾�܂��͐ݒ肵�܂��B</summary>
		public bool Introspection { get; set; }

		/// <summary>���ۂ̎��s���}���`�X���b�h�A�p�[�g�����g�Ƃ��ă}�[�N���ꂽ�X���b�h�ōs�����ǂ����������l���擾�܂��͐ݒ肵�܂��B</summary>
		public bool IsMta { get; set; }

		/// <summary>�����[�g�R���\�[���� <see cref="ScriptEngine"/> �Ƃ̑��ݒʐM�Ɏg�p����Ɨ\������� IPC �`�����l�����擾�܂��͐ݒ肵�܂��B</summary>
		public string RemoteRuntimeChannel { get; set; }

		/// <summary><see cref="Microsoft.Scripting.Hosting.Shell.ConsoleOptions"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		public ConsoleOptions()
		{
			HandleExceptions = true;
			AutoIndentSize = 4;
		}

		/// <summary>��ɂȂ�I�v�V�������g�p���āA<see cref="Microsoft.Scripting.Hosting.Shell.ConsoleOptions"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="options">�ݒ���R�s�[����I�v�V�������w�肵�܂��B</param>
		protected ConsoleOptions(ConsoleOptions options)
		{
			ContractUtils.RequiresNotNull(options, "options");
			Command = options.Command;
			FileName = options.FileName;
			PrintVersion = options.PrintVersion;
			Exit = options.Exit;
			AutoIndentSize = options.AutoIndentSize;
			RemainingArgs = options.RemainingArgs.ToReadOnly();
			Introspection = options.Introspection;
			AutoIndent = options.AutoIndent;
			HandleExceptions = options.HandleExceptions;
			TabCompletion = options.TabCompletion;
			ColorfulConsole = options.ColorfulConsole;
			PrintUsage = options.PrintUsage;
			IsMta = options.IsMta;
			RemoteRuntimeChannel = options.RemoteRuntimeChannel;
		}
	}
}
