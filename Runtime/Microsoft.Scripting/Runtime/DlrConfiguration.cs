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
	/// <summary>����Ɋւ���\������\���܂��B</summary>
	sealed class LanguageConfiguration
	{
		IDictionary<string, object> _options;
		LanguageContext _context;
		string _displayName;

		/// <summary>���̍\���Ƀ��[�h���ꂽ <see cref="Microsoft.Scripting.Runtime.LanguageContext"/> ���擾���܂��B</summary>
		public LanguageContext LanguageContext { get { return _context; } }

		/// <summary>����v���o�C�_�̃A�Z���u���C���^�����擾���܂��B</summary>
		public AssemblyQualifiedTypeName ProviderName { get; private set; }

		/// <summary>
		/// ����v���o�C�_�̃A�Z���u���C���^���A����̕\�����A�I�v�V�������g�p���āA<see cref="Microsoft.Scripting.Runtime.LanguageConfiguration"/>
		/// �N���X�̐V�����C���X�^���X�����������܂��B
		/// </summary>
		/// <param name="providerName">����v���o�C�_�̃A�Z���u���C���^�����w�肵�܂��B</param>
		/// <param name="displayName">����̕\�������w�肵�܂��B</param>
		/// <param name="options">�I�v�V�������w�肵�܂��B</param>
		public LanguageConfiguration(AssemblyQualifiedTypeName providerName, string displayName, IDictionary<string, object> options)
		{
			ProviderName = providerName;
			_displayName = displayName;
			_options = options;
		}

		/// <summary>
		/// �w�肵�� <see cref="ScriptDomainManager"/> ���g�p���� <see cref="Microsoft.Scripting.Runtime.LanguageContext"/> �I�u�W�F�N�g���쐬���A���̃C���X�^���X�Ɋ֘A�t���܂��B
		/// ���ݓI�Ƀ��[�U�[�R�[�h���Ăяo���\��������̂ŁA���b�N���ŌĂяo���Ȃ��ł��������B
		/// </summary>
		/// <param name="domainManager"><see cref="Microsoft.Scripting.Runtime.LanguageContext"/> �I�u�W�F�N�g�̍쐬�ɗ��p���� <see cref="ScriptDomainManager"/> ���w�肵�܂��B</param>
		/// <param name="alreadyLoaded"><see cref="Microsoft.Scripting.Runtime.LanguageContext"/> �I�u�W�F�N�g�����Ƀ��[�h����Ă��邩�ǂ����������l���i�[����ϐ����w�肵�܂��B</param>
		/// <exception cref="Microsoft.Scripting.InvalidImplementationException"><see cref="Microsoft.Scripting.Runtime.LanguageContext"/> �̎����̃C���X�^���X���Ɏ��s���܂����B</exception>
		internal LanguageContext LoadLanguageContext(ScriptDomainManager domainManager, out bool alreadyLoaded)
		{
			if (_context == null)
			{
				// �A�Z���u���̃��[�h�G���[�͂��̂܂ܑ��o����܂��B
				var assembly = domainManager.Platform.LoadAssembly(ProviderName.AssemblyName.FullName);

				Type type = assembly.GetType(ProviderName.TypeName);
				if (type == null)
					throw new InvalidOperationException(String.Format(
						"����̃��[�h�Ɏ��s���܂��� '{0}': �A�Z���u�� '{1}' �͌^ '{2}' ���܂�ł��܂���B", _displayName, assembly.Location, ProviderName.TypeName
					));

				if (!type.IsSubclassOf(typeof(LanguageContext)))
					throw new InvalidOperationException(String.Format(
						"����̃��[�h�Ɏ��s���܂��� '{0}': �^ '{1}' �� LanguageContext ���p�����Ă��Ȃ����߁A�L���Ȍ���v���o�C�_�ł͂���܂���B", _displayName, type
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

	/// <summary>���I���ꃉ���^�C���̍\�������i�[���܂��B</summary>
	public sealed class DlrConfiguration
	{
		bool _frozen;

		/// <summary>�t�@�C���̊g���q���r���� <see cref="System.StringComparer"/> ���擾���܂��B</summary>
		public static StringComparer FileExtensionComparer { get { return StringComparer.OrdinalIgnoreCase; } }
		/// <summary>����̖��O���r���� <see cref="System.StringComparer"/> ���擾���܂��B</summary>
		public static StringComparer LanguageNameComparer { get { return StringComparer.OrdinalIgnoreCase; } }
		/// <summary>�I�v�V�����̖��O���r���� <see cref="System.StringComparer"/> ���擾���܂��B</summary>
		public static StringComparer OptionNameComparer { get { return StringComparer.Ordinal; } }

		Dictionary<AssemblyQualifiedTypeName, LanguageConfiguration> _languages;
		IDictionary<string, object> _options;
		Dictionary<string, LanguageConfiguration> _languageNames;
		Dictionary<string, LanguageConfiguration> _languageExtensions;
		Dictionary<Type, LanguageConfiguration> _loadedProviderTypes;

		/// <summary>�w�肳�ꂽ�\���Ɋւ����񂩂� <see cref="Microsoft.Scripting.Runtime.DlrConfiguration"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="debugMode">�����^�C�����f�o�b�O���[�h�œ��삷�邩�ǂ����������l���w�肵�܂��B</param>
		/// <param name="privateBinding">CLR �����`�F�b�N�𖳎����邩�ǂ����������l���w�肵�܂��B</param>
		/// <param name="options">�啶���Ə���������ʂ���I�v�V�����̖��O�ƒl�̑g���w�肵�܂��B</param>
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

		/// <summary>�����^�C�����f�o�b�O���[�h�œ��삷�邩�ǂ����������l���擾���܂��B</summary>
		/// <remarks>
		/// �f�o�b�O���[�h�ł͎��̂悤�ɓ��삵�܂��B
		/// 1) �f�o�b�O�\�ȃ��\�b�h�ɑ΂��ăV���{�����o�͂���܂��B(<see cref="Microsoft.Scripting.SourceUnit"/> �Ɋ֘A�t����ꂽ���\�b�h).
		/// 2) ����W�^�ɑ΂��ăf�o�b�O�\�ȃ��\�b�h���o�͂���܂� (����͓��I���\�b�h�f�o�b�O�̏�ł� CLR �̐����ɂ����̂ł�).
		/// 3) ���ׂẴ��\�b�h�ɑ΂��� JIT �œK���������ɂȂ�܂��B
		/// 4) ����͂��̒l�Ɋ�Â����œK���𖳌�������\��������܂��B
		/// </remarks>
		public bool DebugMode { get; private set; }

		/// <summary>CLR �����`�F�b�N�𖳎����邩�ǂ����������l���擾���܂��B</summary>
		public bool PrivateBinding { get; private set; }

		/// <summary>���̍\���Ɍ���\����ǉ����܂��B</summary>
		/// <param name="languageTypeName">����v���o�C�_�̃A�Z���u���C���^�����w�肵�܂��B</param>
		/// <param name="displayName">����̕\�������w�肵�܂��B</param>
		/// <param name="names">�啶���Ə���������ʂ��Ȃ�����̖��O�̃��X�g���w�肵�܂��B</param>
		/// <param name="fileExtensions">�啶���Ə���������ʂ��Ȃ��t�@�C���̊g���q�̃��X�g���w�肵�܂��B</param>
		/// <param name="options">�I�v�V�����̃��X�g���w�肵�܂��B</param>
		public void AddLanguage(string languageTypeName, string displayName, IList<string> names, IList<string> fileExtensions, IDictionary<string, object> options)
		{
			ContractUtils.Requires(!_frozen, "�����^�C�������������ꂽ��͍\���͕ύX�ł��܂���B");
			ContractUtils.Requires(
				names.All(id => !string.IsNullOrEmpty(id) && !_languageNames.ContainsKey(id)),
				"names", "����̖��O�� null ��󕶎��ł͂Ȃ��A�܂�����Ԃŏd�����Ȃ�������łȂ���΂Ȃ�܂���B"
			);
			ContractUtils.Requires(
				fileExtensions.All(ext => !string.IsNullOrEmpty(ext) && !_languageExtensions.ContainsKey(ext)),
				"fileExtensions", "�t�@�C���g���q�� null ��󕶎��ł͂Ȃ��A�܂�����Ԃŏd�����Ȃ�������łȂ���΂Ȃ�܂���B"
			);
			ContractUtils.RequiresNotNull(displayName, "displayName");
			if (string.IsNullOrEmpty(displayName))
			{
				ContractUtils.Requires(names.Count > 0, "displayName", "�󕶎��łȂ��\��������ы�̃��X�g�łȂ����ꖼ�������Ȃ���΂Ȃ�܂���B");
				displayName = names[0];
			}
			var aqtn = AssemblyQualifiedTypeName.ParseArgument(languageTypeName, "languageTypeName");
			if (_languages.ContainsKey(aqtn))
				throw new ArgumentException(string.Format("���ꂪ�^�� '{0}' �ɂ����ďd�����Ă��܂��B", aqtn), "languageTypeName");

			// �O���[�o���Ȍ���I�v�V�������ŏ��ɒǉ����A����ŗL�̃I�v�V�����ŏ㏑���\�ɂ��܂��B
			var mergedOptions = new Dictionary<string, object>(_options);

			// �O���[�o���I�v�V����������ŗL�̃I�v�V�����Œu�������܂��B
			foreach (var option in options)
				mergedOptions[option.Key] = option.Value;
			var config = new LanguageConfiguration(aqtn, displayName, mergedOptions);

			_languages.Add(aqtn, config);

			// ���� ID ���g���q�̃��X�g�͏d����������܂��B
			foreach (var name in names)
				_languageNames[name] = config;
			foreach (var ext in fileExtensions)
				_languageExtensions[NormalizeExtension(ext)] = config;
		}

		/// <summary>�g���q�𐳋K�����܂��B�h�b�g�Ŏn�܂�Ȃ��ꍇ�̓h�b�g�Ŏn�܂�悤�Ȋg���q��Ԃ��܂��B</summary>
		/// <param name="extension">���K������g���q���w�肵�܂��B</param>
		internal static string NormalizeExtension(string extension) { return extension[0] == '.' ? extension : "." + extension; }

		/// <summary>���̃I�u�W�F�N�g��ύX�s�\�ɂ��܂��B</summary>
		internal void Freeze()
		{
			Debug.Assert(!_frozen);
			_frozen = true;
		}

		/// <summary>����̃��[�h�����݂܂��B���������ꍇ�� <c>true</c> ��Ԃ��܂��B</summary>
		/// <param name="manager">����̃��[�h�Ɏg�p���� <see cref="ScriptDomainManager"/> ���w�肵�܂��B</param>
		/// <param name="providerName">���[�h���錾��v���o�C�_�̃A�Z���u���C���^�����w�肵�܂��B</param>
		/// <param name="language">���[�h���ꂽ����v���o�C�_���i�[����ϐ����w�肵�܂��B</param>
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

		/// <summary>����̃��[�h�����݂܂��B���������ꍇ�� <c>true</c> ��Ԃ��܂��B</summary>
		/// <param name="manager">����̃��[�h�Ɏg�p���� <see cref="ScriptDomainManager"/> ���w�肵�܂��B</param>
		/// <param name="str">���[�h���錾������ʂ���t�@�C���g���q�܂��͌��ꖼ���w�肵�܂��B</param>
		/// <param name="isExtension"><paramref name="str"/> ���t�@�C���g���q��\�����ǂ����������l���w�肵�܂��B</param>
		/// <param name="language">���[�h���ꂽ����v���o�C�_���i�[����ϐ����w�肵�܂��B</param>
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
				// �P��̌��ꂪ 2 �̈قȂ�A�Z���u���C���^���ɂ���ēo�^����Ă��Ȃ������m�F���܂��B
				// �^�����[�h���邱�ƂȂ��� 2 �̃A�Z���u���C���^���������^���Q�Ƃ��Ă��邱�Ƃ��m���߂���@�͂Ȃ��̂ŁA�����[�h���s���܂��B
				// ���b�N�̎��s���Ƀ��[�U�[�R�[�h���Ăяo����邱�Ƃ�����邽�߁A�`�F�b�N�� config.LoadLanguageContext �̎��s��ɍs���܂��B
				lock (_loadedProviderTypes)
				{
					LanguageConfiguration existingConfig;
					Type type = language.GetType();
					if (_loadedProviderTypes.TryGetValue(type, out existingConfig))
						throw new InvalidOperationException(String.Format("�^ '{0}' �ɂ���Ď������ꂽ����͖��O '{1}' ���g�p���Ă��łɃ��[�h����Ă��܂��B", config.ProviderName, existingConfig.ProviderName));
					_loadedProviderTypes.Add(type, config);
				}
			}
			return language;
		}

		/// <summary>�w�肳�ꂽ����v���o�C�_���猾��̖��O�̃��X�g���擾���܂��B</summary>
		/// <param name="context">����ɑ΂��� <see cref="Microsoft.Scripting.Runtime.LanguageContext"/> ���w�肵�܂��B</param>
		public string[] GetLanguageNames(LanguageContext context)
		{
			ContractUtils.RequiresNotNull(context, "context");
			return _languageNames.Where(x => x.Value.LanguageContext == context).Select(x => x.Key).ToArray();
		}

		/// <summary>���̍\���ɓo�^����Ă��邷�ׂĂ̌��ꖼ�̃��X�g���擾���܂��B</summary>
		public string[] GetLanguageNames() { return _languageNames.Keys.ToArray(); }

		/// <summary>�w�肳�ꂽ����v���o�C�_���猾��̃t�@�C���g���q�̃��X�g���擾���܂��B</summary>
		/// <param name="context">����ɑ΂��� <see cref="Microsoft.Scripting.Runtime.LanguageContext"/> ���w�肵�܂��B</param>
		public string[] GetFileExtensions(LanguageContext context)
		{
			ContractUtils.RequiresNotNull(context, "context");
			return _languageExtensions.Where(x => x.Value.LanguageContext == context).Select(x => x.Key).ToArray();
		}

		/// <summary>���̍\���ɓo�^����Ă��邷�ׂĂ̌���̃t�@�C���g���q�̃��X�g���擾���܂��B</summary>
		public string[] GetFileExtensions() { return _languageExtensions.Keys.ToArray(); }

		/// <summary>�w�肳�ꂽ����v���o�C�_���猾��̍\�������擾���܂��B</summary>
		/// <param name="context">����ɑ΂��� <see cref="Microsoft.Scripting.Runtime.LanguageContext"/> ���w�肵�܂��B</param>
		internal LanguageConfiguration GetLanguageConfig(LanguageContext context) { return _languages.Values.FirstOrDefault(x => x.LanguageContext == context); }
	}
}
