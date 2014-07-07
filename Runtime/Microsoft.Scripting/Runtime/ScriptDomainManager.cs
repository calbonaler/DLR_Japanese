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
using System.Reflection;
using System.Threading;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>���I���ꃉ���^�C���ɂ����ăX�N���v�g�̃h���C�����Ǘ����܂��B</summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")] // TODO: fix
	public sealed class ScriptDomainManager
	{
		List<Assembly> _loadedAssemblies = new List<Assembly>();
		int _lastContextId; // �Ō�Ɍ���R���e�L�X�g�Ɋ��蓖�Ă�ꂽ ID

		/// <summary>�z�X�g�Ɋ֘A�t����ꂽ <see cref="Microsoft.Scripting.PlatformAdaptationLayer"/> ���擾���܂��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
		public PlatformAdaptationLayer Platform
		{
			get
			{
				var result = Host.PlatformAdaptationLayer;
				if (result == null)
					throw new InvalidImplementationException();
				return result;
			}
		}

		/// <summary>���̃I�u�W�F�N�g�Ɋ֘A�t����ꂽ <see cref="Microsoft.Scripting.Runtime.SharedIO"/> �I�u�W�F�N�g���擾���܂��B</summary>
		public SharedIO SharedIO { get; private set; }

		/// <summary>���̃I�u�W�F�N�g�̃z�X�g���擾���܂��B</summary>
		public DynamicRuntimeHostingProvider Host { get; private set; }

		/// <summary>���I���ꃉ���^�C���̍\���Ɏg�p���� <see cref="Microsoft.Scripting.Runtime.DlrConfiguration"/> ���擾���܂��B</summary>
		public DlrConfiguration Configuration { get; private set; }

		/// <summary>�w�肳�ꂽ�z�X�g����э\�������g�p���āA<see cref="Microsoft.Scripting.Runtime.ScriptDomainManager"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="hostingProvider">�z�X�g���w�肵�܂��B</param>
		/// <param name="configuration">���I���ꃉ���^�C���̍\�����s�� <see cref="DlrConfiguration"/> ���w�肵�܂��B</param>
		public ScriptDomainManager(DynamicRuntimeHostingProvider hostingProvider, DlrConfiguration configuration)
		{
			ContractUtils.RequiresNotNull(hostingProvider, "hostingProvider");
			ContractUtils.RequiresNotNull(configuration, "configuration");
			configuration.Freeze();
			Host = hostingProvider;
			Configuration = configuration;
			SharedIO = new SharedIO();
			Globals = new Scope(); // �����̊���̃X�R�[�v���쐬
		}

		#region Language Registration

		/// <summary>����R���e�L�X�g�� ID �𐶐����܂��B</summary>
		internal ContextId GenerateContextId() { return new ContextId(Interlocked.Increment(ref _lastContextId)); }

		/// <summary>�w�肳�ꂽ����v���o�C�_�̌^���猾����擾���܂��B</summary>
		/// <param name="providerType">������擾���錾��v���o�C�_�̌^���w�肵�܂��B</param>
		public LanguageContext GetLanguage(Type providerType)
		{
			ContractUtils.RequiresNotNull(providerType, "providerType");
			return GetLanguageByTypeName(providerType.AssemblyQualifiedName);
		}

		/// <summary>�w�肳�ꂽ����v���o�C�_�̃A�Z���u���C���^�����猾����擾���܂��B</summary>
		/// <param name="providerAssemblyQualifiedTypeName">������擾���錾��v���o�C�_�̃A�Z���u���C���^�����w�肵�܂��B</param>
		public LanguageContext GetLanguageByTypeName(string providerAssemblyQualifiedTypeName)
		{
			ContractUtils.RequiresNotNull(providerAssemblyQualifiedTypeName, "providerAssemblyQualifiedTypeName");
			LanguageContext language;
			if (!Configuration.TryLoadLanguage(this, AssemblyQualifiedTypeName.ParseArgument(providerAssemblyQualifiedTypeName, "providerAssemblyQualifiedTypeName"), out language))
				throw Error.UnknownLanguageProviderType();
			return language;
		}

		/// <summary>�w�肳�ꂽ���O���猾��̎擾�����݂܂��B���������ꍇ�� <c>true</c> ��Ԃ��܂��B</summary>
		/// <param name="languageName">�擾���錾���\�����O���w�肵�܂��B</param>
		/// <param name="language">�擾��������R���e�L�X�g���i�[����ϐ����w�肵�܂��B</param>
		public bool TryGetLanguage(string languageName, out LanguageContext language)
		{
			ContractUtils.RequiresNotNull(languageName, "languageName");
			return Configuration.TryLoadLanguage(this, languageName, false, out language);
		}

		/// <summary>�w�肳�ꂽ���O���猾����擾���܂��B</summary>
		/// <param name="languageName">�擾���錾���\�����O���w�肵�܂��B</param>
		public LanguageContext GetLanguageByName(string languageName)
		{
			LanguageContext language;
			if (!TryGetLanguage(languageName, out language))
				throw new ArgumentException(string.Format("Unknown language name: '{0}'", languageName));
			return language;
		}

		/// <summary>����̃\�[�X�t�@�C���̊g���q���猾��̎擾�����݂܂��B���������ꍇ�� <c>true</c> ��Ԃ��܂��B</summary>
		/// <param name="fileExtension">�擾���錾��̃\�[�X�t�@�C���̊g���q���w�肵�܂��B</param>
		/// <param name="language">�擾��������R���e�L�X�g���i�[����ϐ����w�肵�܂��B</param>
		public bool TryGetLanguageByFileExtension(string fileExtension, out LanguageContext language)
		{
			ContractUtils.RequiresNotEmpty(fileExtension, "fileExtension");
			return Configuration.TryLoadLanguage(this, DlrConfiguration.NormalizeExtension(fileExtension), true, out language);
		}

		/// <summary>����̃\�[�X�t�@�C���̊g���q���猾����擾���܂��B</summary>
		/// <param name="fileExtension">�擾���錾��̃\�[�X�t�@�C���̊g���q���w�肵�܂��B</param>
		public LanguageContext GetLanguageByExtension(string fileExtension)
		{
			LanguageContext language;
			if (!TryGetLanguageByFileExtension(fileExtension, out language))
				throw new ArgumentException(String.Format("Unknown file extension: '{0}'", fileExtension));
			return language;
		}

		#endregion

		/// <summary>���ϐ��̃R���N�V�������擾���܂��B</summary>
		public Scope Globals { get; set; }

		/// <summary>�z�X�g�� <see cref="LoadAssembly"/> ���Ăяo�����Ƃ��ɔ������܂��B</summary>
		public event AssemblyLoadEventHandler AssemblyLoad;

		/// <summary>���̃h���C���Ɏw�肵���A�Z���u�������[�h���܂��B</summary>
		/// <param name="assembly">���[�h����A�Z���u�����w�肵�܂��B</param>
		public bool LoadAssembly(Assembly assembly)
		{
			ContractUtils.RequiresNotNull(assembly, "assembly");
			lock (_loadedAssemblies)
			{
				if (_loadedAssemblies.Contains(assembly))
					return false; // only deliver the event if we've never added the assembly before
				_loadedAssemblies.Add(assembly);
			}
			var assmLoaded = AssemblyLoad;
			if (assmLoaded != null)
				assmLoaded(this, new AssemblyLoadEventArgs(assembly));
			return true;
		}

		/// <summary>���̃h���C���Ɋ��Ƀ��[�h����Ă���A�Z���u���̃��X�g���擾���܂��B</summary>
		public IList<Assembly> GetLoadedAssemblies()
		{
			lock (_loadedAssemblies)
				return _loadedAssemblies.ToArray();
		}
	}
}
