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
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Threading;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting
{
	/// <summary>
	/// �z�X�e�B���O API �ɂ����ē��I���ꃉ���^�C�� (DLR) ��\���܂��B
	/// <see cref="Microsoft.Scripting.Runtime.ScriptDomainManager"/> �ɑ΂������ 1 �̃z�X�e�B���O API �ł��B
	/// </summary>
	public sealed class ScriptRuntime : MarshalByRefObject
	{
		readonly Dictionary<LanguageContext, ScriptEngine> _engines = new Dictionary<LanguageContext, ScriptEngine>();
		readonly InvariantContext _invariantContext;
		readonly object _lock = new object();
		ScriptScope _globals;
		Scope _scopeGlobals;
		ScriptEngine _invariantEngine;

		/// <summary>���݂̃A�v���P�[�V�����h���C���� <see cref="Microsoft.Scripting.Hosting.ScriptRuntime"/> ���쐬���A�w�肳�ꂽ�ݒ���g�p���ď��������܂��B</summary>
		/// <param name="setup">�������Ɏg�p����ݒ���i�[���Ă��� <see cref="Microsoft.Scripting.Hosting.ScriptRuntimeSetup"/> ���w�肵�܂��B</param>
		/// <exception cref="System.ArgumentNullException"><paramref name="setup"/> �� <c>null</c> �ł��B</exception>
		public ScriptRuntime(ScriptRuntimeSetup setup)
		{
			ContractUtils.RequiresNotNull(setup, "setup");

			// �\���G���[�𑦍��ɔ������邽�߁A�ŏ��Ɏ��s����
			DlrConfiguration config = (Setup = setup).ToConfiguration();
			try { Host = (ScriptHost)Activator.CreateInstance(setup.HostType, setup.HostArguments.ToArray()); }
			catch (TargetInvocationException e) { throw new InvalidImplementationException(Strings.InvalidCtorImplementation(setup.HostType, e.InnerException.Message), e.InnerException); }
			catch (Exception e) { throw new InvalidImplementationException(Strings.InvalidCtorImplementation(setup.HostType, e.Message), e); }
			
			IO = new ScriptIO((Manager = new ScriptDomainManager(new ScriptHostProxy(Host), config)).SharedIO);

			bool freshEngineCreated;
			_globals = new ScriptScope(GetEngineNoLockNoNotification(_invariantContext = new InvariantContext(Manager), out freshEngineCreated), Manager.Globals);

			// �����^�C���͂����܂łł��ׂĐݒ肳��A�z�X�g�̃R�[�h���Ăяo����܂��B
			Host.Runtime = this;

			object noDefaultRefs;
			if (!setup.Options.TryGetValue("NoDefaultReferences", out noDefaultRefs) || Convert.ToBoolean(noDefaultRefs) == false)
			{
				LoadAssembly(typeof(string).Assembly);
				LoadAssembly(typeof(System.Diagnostics.Debug).Assembly);
			}
		}

		/// <summary>���̃C���X�^���X�̊�ɂȂ��Ă��� <see cref="Microsoft.Scripting.Runtime.ScriptDomainManager"/> ���擾���܂��B</summary>
		internal ScriptDomainManager Manager { get; private set; }

		/// <summary>���I���ꃉ���^�C���Ɋ֘A�t�����Ă���z�X�g���擾���܂��B</summary>
		public ScriptHost Host { get; private set; }

		/// <summary>���I���ꃉ���^�C���̓��o�͂��擾���܂��B</summary>
		public ScriptIO IO { get; private set; }

		/// <summary>���݂̃A�v���P�[�V�����ݒ�̌���ݒ���g�p���ĐV���������^�C�����쐬���܂��B</summary>
		public static ScriptRuntime CreateFromConfiguration() { return new ScriptRuntime(ScriptRuntimeSetup.ReadConfiguration()); }

		#region Remoting

		/// <summary>�w�肳�ꂽ�A�v���P�[�V�����h���C���� <see cref="Microsoft.Scripting.Hosting.ScriptRuntime"/> ���쐬���A�w�肳�ꂽ�ݒ���g�p���ď��������܂��B</summary>
		/// <param name="domain"><see cref="Microsoft.Scripting.Hosting.ScriptRuntime"/> ���쐬����A�v���P�[�V�����h���C�����w�肵�܂��B</param>
		/// <param name="setup">�������Ɏg�p����ݒ���i�[���Ă��� <see cref="Microsoft.Scripting.Hosting.ScriptRuntimeSetup"/> ���w�肵�܂��B</param>
		/// <exception cref="System.ArgumentNullException"><paramref name="domain"/> �� <c>null</c> �ł��B</exception>
		public static ScriptRuntime CreateRemote(AppDomain domain, ScriptRuntimeSetup setup)
		{
			ContractUtils.RequiresNotNull(domain, "domain");
			return (ScriptRuntime)domain.CreateInstanceAndUnwrap(
				typeof(ScriptRuntime).Assembly.FullName, typeof(ScriptRuntime).FullName,
				false, BindingFlags.Default, null, new object[] { setup }, null, null
			);
		}

		// TODO: Figure out what is the right lifetime
		/// <summary>�Ώۂ̃C���X�^���X�̗L�����ԃ|���V�[�𐧌䂷��A�L�����ԃT�[�r�X �I�u�W�F�N�g���擾���܂��B</summary>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
		public override object InitializeLifetimeService() { return null; }

		#endregion

		/// <summary>���̃C���X�^���X�̏������Ɏg�p���� <see cref="Microsoft.Scripting.Hosting.ScriptRuntimeSetup"/> ���擾���܂��B</summary>
		public ScriptRuntimeSetup Setup { get; private set; }

		#region Engines

		/// <summary>����̖��O���� <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> ���擾���܂��B</summary>
		/// <param name="languageName">�擾���� <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> ��\������̖��O���w�肵�܂��B</param>
		/// <exception cref="System.ArgumentNullException"><paramref name="languageName"/> �� <c>null</c> �ł��B</exception>
		/// <exception cref="System.ArgumentException"><paramref name="languageName"/> �����m�̌��ꖼ��\���Ă��܂��B</exception>
		public ScriptEngine GetEngine(string languageName)
		{
			ContractUtils.RequiresNotNull(languageName, "languageName");
			ScriptEngine engine;
			if (!TryGetEngine(languageName, out engine))
				throw new ArgumentException(String.Format("���m�̌��ꖼ: '{0}'", languageName));
			return engine;
		}

		/// <summary>����v���o�C�_�̌^������ <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> ���擾���܂��B</summary>
		/// <param name="assemblyQualifiedTypeName">�擾���� <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> �̌���v���o�C�_�̌^�����w�肵�܂��B</param>
		/// <exception cref="System.ArgumentNullException"><paramref name="assemblyQualifiedTypeName"/> �� <c>null</c> �ł��B</exception>
		public ScriptEngine GetEngineByTypeName(string assemblyQualifiedTypeName)
		{
			ContractUtils.RequiresNotNull(assemblyQualifiedTypeName, "assemblyQualifiedTypeName");
			return GetEngine(Manager.GetLanguageByTypeName(assemblyQualifiedTypeName));
		}

		/// <summary>����̃\�[�X�t�@�C���̊g���q���� <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> ���擾���܂��B</summary>
		/// <param name="fileExtension">�擾���� <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> ���\������̃\�[�X�t�@�C���̊g���q���w�肵�܂��B</param>
		/// <exception cref="ArgumentException"><paramref name="fileExtension"/> �� <c>null</c> �ł��B</exception>
		/// <exception cref="ArgumentNullException"><paramref name="fileExtension"/> �����m�̊g���q��\���Ă��܂��B</exception>
		public ScriptEngine GetEngineByFileExtension(string fileExtension)
		{
			ContractUtils.RequiresNotNull(fileExtension, "fileExtension");
			ScriptEngine engine;
			if (!TryGetEngineByFileExtension(fileExtension, out engine))
				throw new ArgumentException(String.Format("���m�̊g���q: '{0}'", fileExtension));
			return engine;
		}

		/// <summary>����̖��O���� <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> ���擾���܂��B������Ȃ��ꍇ�� false ��Ԃ��܂��B</summary>
		/// <param name="languageName">�擾���� <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> ��\������̖��O���w�肵�܂��B</param>
		/// <param name="engine">�擾���� <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> ���i�[����ϐ����w�肵�܂��B</param>
		public bool TryGetEngine(string languageName, out ScriptEngine engine)
		{
			LanguageContext language;
			if (!Manager.TryGetLanguage(languageName, out language))
			{
				engine = null;
				return false;
			}
			engine = GetEngine(language);
			return true;
		}

		/// <summary>����̃\�[�X�t�@�C���̊g���q���� <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> ���擾���܂��B</summary>
		/// <param name="fileExtension">�擾���� <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> ���\������̃\�[�X�t�@�C���̊g���q���w�肵�܂��B</param>
		/// <param name="engine">�擾���� <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> ���i�[����ϐ����w�肵�܂��B</param>
		public bool TryGetEngineByFileExtension(string fileExtension, out ScriptEngine engine)
		{
			LanguageContext language;
			if (!Manager.TryGetLanguageByFileExtension(fileExtension, out language))
			{
				engine = null;
				return false;
			}
			engine = GetEngine(language);
			return true;
		}

		/// <summary>�w�肳�ꂽ <see cref="Microsoft.Scripting.Runtime.LanguageContext"/> ���� <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> ���擾���܂��B</summary>
		/// <param name="language">�擾���� <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> ���\������ɑ΂��� <see cref="Microsoft.Scripting.Runtime.LanguageContext"/> ���w�肵�܂��B</param>
		internal ScriptEngine GetEngine(LanguageContext language)
		{
			Assert.NotNull(language);
			ScriptEngine engine;
			bool freshEngineCreated;
			lock (_engines)
				engine = GetEngineNoLockNoNotification(language, out freshEngineCreated);
			if (freshEngineCreated && !ReferenceEquals(language, _invariantContext))
				Host.EngineCreated(engine);
			return engine;
		}

		ScriptEngine GetEngineNoLockNoNotification(LanguageContext language, out bool freshEngineCreated)
		{
			Debug.Assert(_engines != null, "Invalid ScriptRuntime initialiation order");
			ScriptEngine engine;
			if (freshEngineCreated = !_engines.TryGetValue(language, out engine))
			{
				engine = new ScriptEngine(this, language);
				Thread.MemoryBarrier();
				_engines.Add(language, engine);
			}
			return engine;
		}

		#endregion

		#region Compilation, Module Creation

		/// <summary>�C���o���A���g�R���e�L�X�g���g�p���āA�V������� <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> ���쐬���܂��B</summary>
		public ScriptScope CreateScope() { return InvariantEngine.CreateScope(); }

		/// <summary>
		/// �w�肳�ꂽ���� ID �Ɋ֘A�t����ꂽ <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> ���g�p���āA�V�������
		/// <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> ���쐬���܂��B
		/// </summary>
		/// <param name="languageId">
		/// <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> ���쐬���� <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> ��\������ ID ���w�肵�܂��B
		/// </param>
		public ScriptScope CreateScope(string languageId) { return GetEngine(languageId).CreateScope(); }

		/// <summary>�C���o���A���g�R���e�L�X�g���g�p���āA�w�肳�ꂽ�C�ӂ̃I�u�W�F�N�g���X�g���[�W�Ƃ��� <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> ���쐬���܂��B</summary>
		/// <param name="storage">�쐬���� <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> �̃X�g���[�W�ƂȂ�I�u�W�F�N�g���w�肵�܂��B</param>
		public ScriptScope CreateScope(IDynamicMetaObjectProvider storage) { return InvariantEngine.CreateScope(storage); }

		/// <summary>
		/// �w�肳�ꂽ���� ID �Ɋ֘A�t����ꂽ <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> ���g�p���āA�w�肳�ꂽ�C�ӂ̃I�u�W�F�N�g���X�g���[�W�Ƃ���
		/// <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> ���쐬���܂��B
		/// </summary>
		/// <param name="languageId">
		/// <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> ���쐬���� <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> ��\������ ID ���w�肵�܂��B
		/// </param>
		/// <param name="storage">�쐬���� <see cref="Microsoft.Scripting.Hosting.ScriptScope"/> �̃X�g���[�W�ƂȂ�I�u�W�F�N�g���w�肵�܂��B</param>
		public ScriptScope CreateScope(string languageId, IDynamicMetaObjectProvider storage) { return GetEngine(languageId).CreateScope(storage); }

		[Obsolete("IAttributesCollection is obsolete, use CreateScope(IDynamicMetaObjectProvider) instead")]
		public ScriptScope CreateScope(IAttributesCollection dictionary) { return InvariantEngine.CreateScope(dictionary); }

		#endregion

		// TODO: file IO exceptions, parse exceptions, execution exceptions, etc.
		/// <summary>�w�肳�ꂽ�t�@�C���̓��e��V�����X�R�[�v�Ŏ��s���A���̃X�R�[�v��Ԃ��܂��B�G���W���̓t�@�C���̊g���q���画�f����܂��B</summary>
		/// <param name="path">���s����t�@�C���̃p�X���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="path"/> �� <c>null</c> �ł��B</exception>
		/// <exception cref="ArgumentException">
		/// �p�X����ł��邩�A<see cref="System.IO.Path.GetInvalidPathChars"/>
		/// �Œ�`����閳���ȕ������܂�ł��邩�A�g���q������܂���B
		/// </exception>
		public ScriptScope ExecuteFile(string path)
		{
			ContractUtils.RequiresNotEmpty(path, "path");
			string extension = Path.GetExtension(path);
			ScriptEngine engine;
			if (!TryGetEngineByFileExtension(extension, out engine))
				throw new ArgumentException(String.Format("�t�@�C���g���q '{0}' �͂ǂ̌���ɂ��֘A�t�����Ă��܂���B", extension));
			return engine.ExecuteFile(path);
		}

		/// <summary>�w�肳�ꂽ�t�@�C�����������A�t�@�C�������Ƀ��[�h����Ă���΃X�R�[�v��Ԃ��A����ȊO�̏ꍇ�̓t�@�C�������[�h���Ă��̃X�R�[�v��Ԃ��܂��B</summary>
		/// <param name="path">�g�p����t�@�C���̃p�X���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="path"/> �� <c>null</c> �ł��B</exception>
		/// <exception cref="ArgumentException">�t�@�C���̊g���q������G���W���Ɋ��蓖�Ă��܂���B</exception>
		/// <exception cref="InvalidOperationException">����� 1 �������p�X������܂���B</exception>
		/// <exception cref="FileNotFoundException">�t�@�C���͌���̌����p�X�ɑ��݂��Ă���K�v������܂��B</exception>
		public ScriptScope UseFile(string path)
		{
			ContractUtils.RequiresNotEmpty(path, "path");
			string extension = Path.GetExtension(path);

			ScriptEngine engine;
			if (!TryGetEngineByFileExtension(extension, out engine))
				throw new ArgumentException(string.Format("�t�@�C���g���q '{0}' �͂ǂ̌���ɂ��֘A�t�����Ă��܂���B", extension));

			if (engine.SearchPaths.Count == 0)
				throw new InvalidOperationException(string.Format("���� '{0}' �ɂ͌����p�X������܂���B", engine.Setup.DisplayName));

			// See if the file is already loaded, if so return the scope
			foreach (string searchPath in engine.SearchPaths)
			{
				ScriptScope scope = engine.GetScope(Path.Combine(searchPath, path));
				if (scope != null)
					return scope;
			}

			// Find the file on disk, then load it
			foreach (string searchPath in engine.SearchPaths)
			{
				string filePath = Path.Combine(searchPath, path);
				if (Manager.Platform.FileExists(filePath))
					return ExecuteFile(filePath);
			}

			// Didn't find the file, throw
			throw new FileNotFoundException(string.Format("File '{0}' not found in language's search path: {1}", path, string.Join(", ", engine.SearchPaths)));
		}

		/// <summary>
		/// �O���[�o���I�u�W�F�N�g�܂��� <see cref="Microsoft.Scripting.Hosting.ScriptScope"/>
		/// �Ƃ��Ă� <see cref="Microsoft.Scripting.Hosting.ScriptRuntime"/> �̖��O�������擾���܂��B
		/// </summary>
		public ScriptScope Globals
		{
			get
			{
				if (_scopeGlobals == Manager.Globals)
					return _globals;
				lock (_lock)
				{
					if (_scopeGlobals != Manager.Globals)
						// make sure no one has changed the globals behind our back
						_globals = new ScriptScope(InvariantEngine, _scopeGlobals = Manager.Globals); // TODO: Should get LC from Scope when it's there
					return _globals;
				}
			}
		}

		/// <summary>
		/// �A�Z���u�����Ŏg�p�\�Ȍ^��\�����߂ɁA�A�Z���u���̖��O��Ԃ���� <see cref="Microsoft.Scripting.Hosting.ScriptRuntime.Globals"/> �ɑ΂��閼�O���������񂵂܂��B
		/// </summary>
		/// <param name="assembly">���񂷂�A�Z���u�����w�肵�܂��B</param>
		/// <remarks>
		/// ���ꂼ��̍ŏ�ʖ��O��Ԃ̖��O�� Globals �ɂ����Ė��O��Ԃ�\�����I�I�u�W�F�N�g�Ɍ��ѕt�����܂��B
		/// ���ꂼ��̍ŏ�ʖ��O��ԃI�u�W�F�N�g���ł́A�l�X�g���ꂽ���O��Ԃ̖��O�����ꂼ��̑w�̖��O��Ԃ�\�����I�I�u�W�F�N�g�Ɍ��ѕt�����܂��B
		/// �������O��ԏC�����ɑ��������ꍇ�A���̃��\�b�h�͖��O����і��O��Ԃ�\���I�u�W�F�N�g���}�[�W���܂��B
		/// </remarks>
		public void LoadAssembly(Assembly assembly) { Manager.LoadAssembly(assembly); }

		/// <summary>�C���o���A���g�R���e�L�X�g�ɑ΂���G���W���̊���� <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> �I�u�W�F�N�g���擾���܂��B</summary>
		public ObjectOperations Operations { get { return InvariantEngine.Operations; } }

		/// <summary>�C���o���A���g�R���e�L�X�g�ɑ΂���G���W���̐V���� <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> �I�u�W�F�N�g���쐬���܂��I�u�W�F�N�g���쐬���܂��B</summary>
		public ObjectOperations CreateOperations() { return InvariantEngine.CreateOperations(); }

		/// <summary>���I���ꃉ���^�C�����V���b�g�_�E�����܂��B</summary>
		public void Shutdown()
		{
			List<LanguageContext> lcs;
			lock (_engines)
				lcs = new List<LanguageContext>(_engines.Keys);
			foreach (var language in lcs)
				language.Shutdown();
		}

		/// <summary>�C���o���A���g�R���e�L�X�g�ɑ΂���G���W�����擾���܂��B</summary>
		internal ScriptEngine InvariantEngine
		{
			get
			{
				if (_invariantEngine == null)
					_invariantEngine = GetEngine(_invariantContext);
				return _invariantEngine;
			}
		}
	}
}
