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
	/// <summary><see cref="Microsoft.Scripting.Hosting.ScriptRuntime"/> �̃Z�b�g�A�b�v�ɕK�v�ȏ����i�[���܂��B</summary>
	[Serializable]
	public sealed class ScriptRuntimeSetup
	{
		// host specification:
		Type _hostType = typeof(ScriptHost);

		// DLR options:
		bool _debugMode;
		bool _privateBinding;

		bool _frozen;

		/// <summary><see cref="Microsoft.Scripting.Hosting.ScriptRuntime"/>�N���X�̐V�����C���X�^���X�����������܂��B</summary>
		public ScriptRuntimeSetup()
		{
			LanguageSetups = new List<LanguageSetup>();
			Options = new Dictionary<string, object>();
			HostArguments = new List<object>();
		}

		/// <summary>�����^�C���Ƀ��[�h����錾��ɑ΂���Z�b�g�A�b�v�����擾���܂��B</summary>
		public IList<LanguageSetup> LanguageSetups { get; private set; }

		/// <summary>�����^�C�����f�o�b�O���[�h�œ��삷�邩�ǂ����������l���擾�܂��͐ݒ肵�܂��B</summary>
		/// <remarks>
		/// �f�o�b�O���[�h�ł͎��̂悤�ɓ��삵�܂��B
		/// 1) �f�o�b�O�\�ȃ��\�b�h�ɑ΂��ăV���{�����o�͂���܂��B(<see cref="Microsoft.Scripting.SourceUnit"/> �Ɋ֘A�t����ꂽ���\�b�h).
		/// 2) ����W�^�ɑ΂��ăf�o�b�O�\�ȃ��\�b�h���o�͂���܂� (����͓��I���\�b�h�f�o�b�O�̏�ł� CLR �̐����ɂ����̂ł�).
		/// 3) ���ׂẴ��\�b�h�ɑ΂��� JIT �œK���������ɂȂ�܂��B
		/// 4) ����͂��̒l�Ɋ�Â����œK���𖳌�������\��������܂��B
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

		/// <summary>CLR �����`�F�b�N�𖳎����邩�ǂ����������l���擾�܂��͐ݒ肵�܂��B</summary>
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
		/// �z�X�g�̌^���擾�܂��͐ݒ肵�܂��B
		/// <see cref="Microsoft.Scripting.Hosting.ScriptHost"/> �̂ǂ̔h���^�ɂ��ݒ�ł��܂��B
		/// �ݒ肷��ƁA�z�X�g�̓����^�C���̐U�镑���𐧌䂷�邽�߂Ɉ��̃��\�b�h���I�[�o�[���C�h�ł���悤�ɂȂ�܂��B
		/// </summary>
		public Type HostType
		{
			get { return _hostType; }
			set
			{
				ContractUtils.RequiresNotNull(value, "value");
				ContractUtils.Requires(typeof(ScriptHost).IsAssignableFrom(value), "value", "ScriptHost �܂��͂��̔h���^�ł���K�v������܂��B");
				CheckFrozen();
				_hostType = value;
			}
		}

		/// <summary>�啶���Ə���������ʂ���I�v�V�����̖��O�ƒl�̑g���擾�܂��͐ݒ肵�܂��B</summary>
		public IDictionary<string, object> Options { get; private set; }

		/// <summary>�z�X�g�^�̃R���X�g���N�^�ɓn�����������擾�܂��͐ݒ肵�܂��B</summary>
		public IList<object> HostArguments { get; private set; }

		/// <summary>���I���ꃉ���^�C���̍\�����ɂ��̃I�u�W�F�N�g��ϊ����܂��B</summary>
		internal DlrConfiguration ToConfiguration()
		{
			ContractUtils.Requires(LanguageSetups.Count > 0, "LanguageSetups", "ScriptRuntimeSetup �͏��Ȃ��Ƃ� 1 �� LanguageSetup ���܂�ł���K�v������܂��B");

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
				throw new InvalidOperationException("ScriptRuntime ���쐬������� ScriptRuntimeSetup ��ύX���邱�Ƃ͂ł��܂���B");
		}

		/// <summary> .NET �̍\���V�X�e�� (.config �t�@�C��) ����Z�b�g�A�b�v����ǂݏo���܂��B�����\�����Ȃ��ꍇ�́A��̃Z�b�g�A�b�v����Ԃ��܂��B</summary>
		public static ScriptRuntimeSetup ReadConfiguration()
		{
			var setup = new ScriptRuntimeSetup();
			Configuration.Section.LoadRuntimeSetup(setup, null);
			return setup;
		}

		/// <summary>�w�肳�ꂽ XML �t�@�C������\������ǂݏo���܂��B</summary>
		/// <param name="configFileStream">�\�������i�[���Ă��� XML �t�@�C����\���X�g���[�����w�肵�܂��B</param>
		public static ScriptRuntimeSetup ReadConfiguration(Stream configFileStream)
		{
			ContractUtils.RequiresNotNull(configFileStream, "configFileStream");
			var setup = new ScriptRuntimeSetup();
			Configuration.Section.LoadRuntimeSetup(setup, configFileStream);
			return setup;
		}

		/// <summary>�w�肳�ꂽ XML �t�@�C������\������ǂݏo���܂��B</summary>
		/// <param name="configFilePath">�\�������i�[���Ă��� XML �t�@�C���̏ꏊ�������p�X���w�肵�܂��B</param>
		public static ScriptRuntimeSetup ReadConfiguration(string configFilePath)
		{
			ContractUtils.RequiresNotNull(configFilePath, "configFilePath");
			using (var stream = File.OpenRead(configFilePath))
				return ReadConfiguration(stream);
		}
	}
}
