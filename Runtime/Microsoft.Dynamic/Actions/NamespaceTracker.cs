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
using Microsoft.Contracts;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions
{
	/// <summary>CLS �̖��O��Ԃ�\���܂��B</summary>
	public class NamespaceTracker : MemberTracker, IAttributesCollection, IMembersList
	{
		// _dict contains all the currently loaded entries. However, there may be pending types that have not yet been loaded in _typeNames
		internal readonly Dictionary<string, MemberTracker> _dict = new Dictionary<string, MemberTracker>();
		internal readonly List<Assembly> _packageAssemblies = new List<Assembly>();
		readonly Dictionary<Assembly, TypeNames> _typeNames = new Dictionary<Assembly, TypeNames>();
		readonly string _fullName; // null for the TopReflectedPackage
		TopNamespaceTracker _topPackage;
		int _id;
		static int _masterId;

		/// <summary>�w�肳�ꂽ���O���g�p���āA<see cref="Microsoft.Scripting.Actions.NamespaceTracker"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="name">���̖��O��Ԃ̖��O���w�肵�܂��B</param>
		protected NamespaceTracker(string name)
		{
			UpdateId();
			_fullName = name;
		}

		/// <summary>���̃I�u�W�F�N�g�̕�����\����Ԃ��܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̕�����\���B</returns>
		[Confined]
		public override string ToString() { return base.ToString() + ":" + _fullName; }

		NamespaceTracker GetOrCreateChildNamespace(string childName, Assembly assem)
		{
			// lock is held when this is called
			Assert.NotNull(childName, assem);
			Debug.Assert(!childName.Contains(Type.Delimiter)); // This is the simple name, not the full name
			Debug.Assert(_packageAssemblies.Contains(assem)); // Parent namespace must contain all the assemblies of the child
			// If we have a module, then we add the assembly to the InnerModule
			// If it's not a module, we'll wipe it out below, eg "def System(): pass" then 
			// "import System" will result in the namespace being visible.
			MemberTracker ret;
			NamespaceTracker package;
			if (_dict.TryGetValue(childName, out ret) && (package = ret as NamespaceTracker) != null)
			{
				if (!package._packageAssemblies.Contains(assem))
				{
					package._packageAssemblies.Add(assem);
					package.UpdateSubtreeIds();
				}
			}
			else
			{
				_dict[childName] = package = new NamespaceTracker(GetFullChildName(childName));
				package.SetTopPackage(_topPackage);
				package._packageAssemblies.Add(assem);
			}
			return package;
		}

		string GetFullChildName(string childName)
		{
			Assert.NotNull(childName);
			Debug.Assert(!childName.Contains(Type.Delimiter)); // This is the simple name, not the full name
			return _fullName == null ? childName : _fullName + Type.Delimiter + childName;
		}

		static Type LoadType(Assembly assem, string fullTypeName)
		{
			Assert.NotNull(assem, fullTypeName);
			var type = assem.GetType(fullTypeName);
			// We should ignore nested types. They will be loaded when the containing type is loaded
			Debug.Assert(type == null || !type.IsNested);
			return type;
		}

		void AddTypeName(string typeName, Assembly assem)
		{
			// lock is held when this is called
			Assert.NotNull(typeName, assem);
			Debug.Assert(!typeName.Contains(Type.Delimiter)); // This is the simple name, not the full name
			TypeNames name;
			if (!_typeNames.TryGetValue(assem, out name))
				_typeNames[assem] = name = new TypeNames(assem, _fullName);
			name.AddTypeName(typeName);
			var normalizedTypeName = ReflectionUtils.GetNormalizedTypeName(typeName);
			MemberTracker existingValue;
			if (_dict.TryGetValue(normalizedTypeName, out existingValue))
			{
				// A similarly named type, namespace, or module already exists.
				var newType = LoadType(assem, GetFullChildName(typeName));
				if (newType != null)
				{
					var existingTypeEntity = existingValue as TypeTracker;
					if (existingTypeEntity == null)
						Debug.Assert(existingValue is NamespaceTracker);
					_dict[normalizedTypeName] = existingTypeEntity == null ?
						MemberTracker.FromMemberInfo(newType) : // Replace the existing namespace or module with the new type
						TypeGroup.Merge(existingTypeEntity, ReflectionCache.GetTypeTracker(newType)); // Unify the new type with the existing type
				}
			}
		}

		/// <summary>(�q�̖��O��Ԃł͂Ȃ�) ���݂̖��O��ԂɊ�^���邷�ׂẴA�Z���u�����炷�ׂĂ̌^�����[�h���܂��B</summary>
		void LoadAllTypes()
		{
			foreach (string typeName in _typeNames.Values.SelectMany(x => x.NormalizedTypeNames))
			{
				MemberTracker value;
				if (!TryGetValue(SymbolTable.StringToId(typeName), out value))
				{
					Debug.Fail("TryGetMember �� TypeLoadException �𔭐�������͂��Ȃ̂ŁA�����ɓ��B����̂͂��蓾�Ȃ��B");
					throw new TypeLoadException(typeName);
				}
			}
		}

		/// <summary>���̖��O��Ԃ̖��O���擾���܂��B</summary>
		public override string Name { get { return _fullName; } }

		/// <summary>�w�肳�ꂽ�A�Z���u�����̂��ׂĂ̌^�����݂̖��O��Ԉȉ��̓K�؂Ȗ��O��Ԃɔz�u���܂��B</summary>
		/// <param name="assem">�z�u����^���܂�ł���A�Z���u�����w�肵�܂��B</param>
		protected void DiscoverAllTypes(Assembly assem)
		{
			// lock is held when this is called
			ContractUtils.RequiresNotNull(assem, "assem");
			NamespaceTracker previousPackage = null;
			var previousFullNamespace = string.Empty; // Note that String.Empty is not a valid namespace
			foreach (var type in LoadTypesFromAssembly(assem, _topPackage.DomainManager.Configuration.PrivateBinding).Where(x => !x.IsNested))
			{
				Debug.Assert(type.Namespace != string.Empty);
				(type.Namespace == previousFullNamespace ?
					previousPackage : // We have a cache hit. We dont need to call GetOrMakePackageHierarchy (which generates a fair amount of temporary substrings)
					(previousPackage = (previousFullNamespace = type.Namespace) == null ?
						this : // null is the top-level namespace
						type.Namespace.Split(Type.Delimiter).Aggregate(this, (x, y) => x.GetOrCreateChildNamespace(y, assem))))
				.AddTypeName(type.Name, assem);
			}
		}

		/// <summary>
		/// �^���ǂ̃A�Z���u���ɂ����݂��Ȃ��ꍇ�̃t�H�[���o�b�N�Ƃ��ċ@�\���܂��B
		/// ����̓n�[�h�R�[�h���ꂽ�^���X�g���ɑ��݂��Ȃ��V�����^���ǉ����ꂽ�ꍇ�ɔ������܂��B
		/// </summary>
		/// <remarks>
		/// ���̃R�[�h�͈ȉ��̗��R��萳�m�ł͂���܂���:
		/// 1. �W�F�l���b�N�^����舵���܂���B(�^�Փ�)
		/// 2. GetCustomMemberNames �ւ̈ȑO�̌Ăяo�� (�Ⴆ�� Python �ɂ����� "from foo import *" �Ȃ�) �͂��̌^�Ɋ܂܂�܂���B
		/// 3. ����̓A�Z���u���ɒǉ����ꂽ�V�������O��Ԃ���舵���܂���B
		/// </remarks>
		TypeTracker CheckForUnlistedType(string nameString)
		{
			Assert.NotNull(nameString);
			var result = _packageAssemblies.Select(x => x.GetType(GetFullChildName(nameString), false)).FirstOrDefault(x => x != null && !x.IsNested && (x.IsPublic || _topPackage.DomainManager.Configuration.PrivateBinding));
			return result != null ? ReflectionCache.GetTypeTracker(result) : null;
		}

		#region IAttributesCollection Members

		/// <summary>�w�肵���L�[����ђl�����v�f�� <see cref="Microsoft.Scripting.IAttributesCollection"/> �ɒǉ����܂��B</summary>
		/// <param name="name">�ǉ�����v�f�̃L�[�Ƃ��Ďg�p���� <see cref="SymbolId"/>�B</param>
		/// <param name="value">�ǉ�����v�f�̒l�Ƃ��Ďg�p����I�u�W�F�N�g�B</param>
		public void Add(SymbolId name, object value) { throw new InvalidOperationException(); }

		/// <summary>�w�肵���L�[�Ɋ֘A�t�����Ă���l���擾���܂��B</summary>
		/// <param name="name">�l���擾����Ώۂ̃L�[�B</param>
		/// <param name="value">
		/// ���̃��\�b�h���Ԃ����Ƃ��ɁA�L�[�����������ꍇ�́A�w�肵���L�[�Ɋ֘A�t�����Ă���l�B����ȊO�̏ꍇ�� <c>null</c>�B
		/// ���̃p�����[�^�[�͏����������ɓn����܂��B
		/// </param>
		/// <returns>�w�肵���L�[�����v�f�� <see cref="Microsoft.Scripting.IAttributesCollection"/> ����������I�u�W�F�N�g�Ɋi�[����Ă���ꍇ��
		/// <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B
		/// </returns>
		public bool TryGetValue(SymbolId name, out object value)
		{
			MemberTracker tmp;
			bool res = TryGetValue(name, out tmp);
			value = tmp;
			return res;
		}

		/// <summary>�w�肵���L�[�Ɋ֘A�t�����Ă��郁���o���擾���܂��B</summary>
		/// <param name="name">�����o���擾����Ώۂ̃L�[�B</param>
		/// <param name="value">
		/// ���̃��\�b�h���Ԃ����Ƃ��ɁA�L�[�����������ꍇ�́A�w�肵���L�[�Ɋ֘A�t�����Ă���l�B����ȊO�̏ꍇ�� <c>null</c>�B
		/// ���̃p�����[�^�[�͏����������ɓn����܂��B
		/// </param>
		/// <returns>�w�肵���L�[�����v�f�� <see cref="Microsoft.Scripting.IAttributesCollection"/> ����������I�u�W�F�N�g�Ɋi�[����Ă���ꍇ��
		/// <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B
		/// </returns>
		public bool TryGetValue(SymbolId name, out MemberTracker value)
		{
			lock (_topPackage.HierarchyLock)
			{
				LoadNamespaces();
				var strName = SymbolTable.IdToString(name);
				if (_dict.TryGetValue(strName, out value))
					return true;
				// Look up the type names and load the type if its name exists
				if (!strName.Contains(Type.Delimiter) &&
					(value = _typeNames.Where(x => x.Value.Contains(strName)).Aggregate((TypeTracker)null, (x, y) => y.Value.UpdateTypeEntity(x, strName)) ??
					CheckForUnlistedType(strName)) != null)
				{
					_dict[strName] = value;
					return true;
				}
				return false;
			}
		}

		/// <summary>�w�肵���L�[�����v�f�� <see cref="Microsoft.Scripting.IAttributesCollection"/> ����폜���܂��B</summary>
		/// <param name="name">�폜����v�f�̃L�[�B</param>
		/// <returns>
		/// �v�f������ɍ폜���ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B
		/// ���̃��\�b�h�́A<paramref name="name"/> ������ <see cref="Microsoft.Scripting.IAttributesCollection"/> �Ɍ�����Ȃ������ꍇ�ɂ� <c>false</c> ��Ԃ��܂��B
		/// </returns>
		public bool Remove(SymbolId name) { throw new InvalidOperationException(); }

		/// <summary>�w�肵���L�[�̗v�f�� <see cref="Microsoft.Scripting.IAttributesCollection"/> �Ɋi�[����Ă��邩�ǂ������m�F���܂��B</summary>
		/// <param name="name"><see cref="Microsoft.Scripting.IAttributesCollection"/> ���Ō��������L�[�B</param>
		/// <returns>�w�肵���L�[�����v�f�� <see cref="Microsoft.Scripting.IAttributesCollection"/> ���ێ����Ă���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public bool ContainsKey(SymbolId name)
		{
			MemberTracker dummy;
			return TryGetValue(name, out dummy);
		}

		/// <summary>�w�肵���L�[�����v�f���擾�܂��͐ݒ肵�܂��B</summary>
		/// <param name="name">�擾�܂��͐ݒ肷��v�f�̃L�[�B</param>
		/// <returns>�w�肵���L�[�����v�f�B</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
		public object this[SymbolId name]
		{
			get
			{
				object res;
				if (TryGetValue(name, out res))
					return res;
				throw new KeyNotFoundException();
			}
			set { throw new InvalidOperationException(); }
		}

		/// <summary><see cref="SymbolId"/> ���L�[�ł��鑮���̃f�B�N�V���i�����擾���܂��B</summary>
		public IDictionary<SymbolId, object> SymbolAttributes
		{
			get
			{
				LoadNamespaces();
				return this.Select(x => new KeyValuePair<string, object>(x.Key as string, x.Value)).Where(x => x.Key != null)
					.ToDictionary(x => SymbolTable.StringToId(x.Key), x => x.Value);
			}
		}

		/// <summary>�w�肵���L�[����ђl�����v�f�� <see cref="Microsoft.Scripting.IAttributesCollection"/> �ɒǉ����܂��B</summary>
		/// <param name="name">�ǉ�����v�f�̃L�[�Ƃ��Ďg�p����I�u�W�F�N�g�B</param>
		/// <param name="value">�ǉ�����v�f�̒l�Ƃ��Ďg�p����I�u�W�F�N�g�B</param>
		public void Add(object name, object value) { throw new InvalidOperationException(); }

		/// <summary>�w�肵���L�[�Ɋ֘A�t�����Ă���l���擾���܂��B</summary>
		/// <param name="name">�l���擾����Ώۂ̃L�[�B</param>
		/// <param name="value">
		/// ���̃��\�b�h���Ԃ����Ƃ��ɁA�L�[�����������ꍇ�́A�w�肵���L�[�Ɋ֘A�t�����Ă���l�B����ȊO�̏ꍇ�� <c>null</c>�B
		/// ���̃p�����[�^�[�͏����������ɓn����܂��B
		/// </param>
		/// <returns>�w�肵���L�[�����v�f�� <see cref="Microsoft.Scripting.IAttributesCollection"/> ����������I�u�W�F�N�g�Ɋi�[����Ă���ꍇ��
		/// <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B
		/// </returns>
		public bool TryGetValue(object name, out object value)
		{
			var str = name as string;
			if (str != null)
				return TryGetValue(SymbolTable.StringToId(str), out value);
			value = null;
			return false;
		}

		/// <summary>�w�肵���L�[�����v�f�� <see cref="Microsoft.Scripting.IAttributesCollection"/> ����폜���܂��B</summary>
		/// <param name="name">�폜����v�f�̃L�[�B</param>
		/// <returns>
		/// �v�f������ɍ폜���ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B
		/// ���̃��\�b�h�́A<paramref name="name"/> ������ <see cref="Microsoft.Scripting.IAttributesCollection"/> �Ɍ�����Ȃ������ꍇ�ɂ� <c>false</c> ��Ԃ��܂��B
		/// </returns>
		public bool Remove(object name) { throw new InvalidOperationException(); }

		/// <summary>�w�肵���L�[�̗v�f�� <see cref="Microsoft.Scripting.IAttributesCollection"/> �Ɋi�[����Ă��邩�ǂ������m�F���܂��B</summary>
		/// <param name="name"><see cref="Microsoft.Scripting.IAttributesCollection"/> ���Ō��������L�[�B</param>
		/// <returns>�w�肵���L�[�����v�f�� <see cref="Microsoft.Scripting.IAttributesCollection"/> ���ێ����Ă���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public bool ContainsKey(object name)
		{
			object dummy;
			return TryGetValue(name, out dummy);
		}

		/// <summary>���̃I�u�W�F�N�g�� <see cref="System.Collections.Generic.IDictionary&lt;Object, Object&gt;"/> �Ƃ��Ď擾���܂��B</summary>
		/// <returns><see cref="System.Collections.Generic.IDictionary&lt;Object, Object&gt;"/> �Ƃ����`���Ŏ擾���ꂽ���݂̃I�u�W�F�N�g�B</returns>
		public IDictionary<object, object> AsObjectKeyedDictionary()
		{
			LoadNamespaces();
			lock (_topPackage.HierarchyLock)
				return _dict.ToDictionary(x => (object)x.Key, x => (object)x.Value);
		}

		/// <summary><see cref="Microsoft.Scripting.IAttributesCollection"/> �Ɋi�[����Ă���v�f�̐����擾���܂��B</summary>
		public int Count { get { return _dict.Count; } }

		/// <summary><see cref="Microsoft.Scripting.IAttributesCollection"/> �̃L�[��ێ����Ă��� <see cref="System.Collections.Generic.ICollection&lt;Object&gt;"/> ���擾���܂��B</summary>
		public ICollection<object> Keys
		{
			get
			{
				LoadNamespaces();
				lock (_topPackage.HierarchyLock)
					return new List<object>(GetStringKeys());
			}
		}

		IEnumerable<string> GetStringKeys() { return _dict.Keys.Concat(_typeNames.SelectMany(x => x.Value.NormalizedTypeNames)).Distinct(); }

		#endregion

		/// <summary>���̃R���N�V�����𔽕���������񋓎q��Ԃ��܂��B</summary>
		/// <returns>�R���N�V�����𔽕���������񋓎q�B</returns>
		[Pure]
		public IEnumerator<KeyValuePair<object, object>> GetEnumerator()
		{
			foreach (var key in Keys)
				yield return new KeyValuePair<object, object>(key, this[SymbolTable.StringToId((string)key)]);
		}

		/// <summary>���̃R���N�V�����𔽕���������񋓎q��Ԃ��܂��B</summary>
		/// <returns>�R���N�V�����𔽕���������񋓎q�B</returns>
		[Pure]
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }

		/// <summary>���̖��O��ԂɊ֘A�t����ꂽ�A�Z���u���̃��X�g���擾���܂��B</summary>
		public IList<Assembly> PackageAssemblies
		{
			get
			{
				LoadNamespaces();
				return _packageAssemblies;
			}
		}

		/// <summary>�ŏ�ʖ��O��ԂɊ֘A�t����ꂽ���ׂẴA�Z���u�����̌^��K�؂Ȗ��O��Ԃɔz�u���܂��B</summary>
		protected virtual void LoadNamespaces()
		{
			if (_topPackage != null)
				_topPackage.LoadNamespaces();
		}

		/// <summary>���̖��O��Ԃ̍ŏ�ʖ��O��Ԃ�ݒ肵�܂��B</summary>
		/// <param name="package">�ݒ肷��ŏ�ʖ��O��Ԃ��w�肵�܂��B</param>
		protected void SetTopPackage(TopNamespaceTracker package)
		{
			Assert.NotNull(package);
			_topPackage = package;
		}

		/// <summary>
		/// �P��̃A�Z���u���ɂ��P��̖��O��ԓ��̃p�u���b�N�Ńl�X�g����Ă��Ȃ����ׂĂ̌^�̖��O���i�[���܂��B
		/// ���̃N���X�ɂ���Ă��ׂĂ̌^��ϋɓI�Ƀ��[�h���邱�ƂȂ����O��Ԃ������ł���悤�ɂȂ�܂��B
		/// �^�̐ϋɓI���[�h�͋N�����Ԃ�x�����A���[�L���O�Z�b�g�𑝉������A����ɗv���������� TypeLoadException �𔭐�������\�������邽�߈Ӗ��I�ɐ������Ȃ��Ȃ�܂��B
		/// </summary>
		class TypeNames
		{
			List<string> _simpleTypeNames = new List<string>();
			Dictionary<string, List<string>> _genericTypeNames = new Dictionary<string, List<string>>();
			Assembly _assembly;
			string _fullNamespace;

			internal TypeNames(Assembly assembly, string fullNamespace)
			{
				_assembly = assembly;
				_fullNamespace = fullNamespace;
			}

			internal bool Contains(string normalizedTypeName)
			{
				Debug.Assert(normalizedTypeName.IndexOf(Type.Delimiter) == -1); // This is the simple name, not the full name
				Debug.Assert(ReflectionUtils.GetNormalizedTypeName(normalizedTypeName) == normalizedTypeName);
				return NormalizedTypeNames.Contains(normalizedTypeName);
			}

			internal TypeTracker UpdateTypeEntity(TypeTracker existingTypeEntity, string normalizedTypeName)
			{
				Debug.Assert(!normalizedTypeName.Contains(Type.Delimiter)); // This is the simple name, not the full name
				Debug.Assert(ReflectionUtils.GetNormalizedTypeName(normalizedTypeName) == normalizedTypeName);
				var names = Enumerable.Empty<string>();
				if (_simpleTypeNames.Contains(normalizedTypeName)) // Look for a non-generic type
					names = Enumerable.Repeat(normalizedTypeName, 1);
				if (_genericTypeNames.ContainsKey(normalizedTypeName)) // Look for generic types
					names = names.Concat(_genericTypeNames[normalizedTypeName]);
				return names.Select(x => LoadType(_assembly, _fullNamespace == null ? x : _fullNamespace + Type.Delimiter + x)).Where(x => x != null)
					.Aggregate(existingTypeEntity, (x, y) => TypeGroup.Merge(x, ReflectionCache.GetTypeTracker(y)));
			}

			internal void AddTypeName(string typeName)
			{
				Debug.Assert(!typeName.Contains(Type.Delimiter)); // This is the simple name, not the full name
				var normalizedName = ReflectionUtils.GetNormalizedTypeName(typeName);
				(normalizedName == typeName ?
					_simpleTypeNames :
					(_genericTypeNames.ContainsKey(normalizedName) ?
						_genericTypeNames[normalizedName] :
						(_genericTypeNames[normalizedName] = new List<string>())))
				.Add(typeName);
			}

			internal IEnumerable<string> NormalizedTypeNames { get { return _simpleTypeNames.Concat(_genericTypeNames.Keys); } }
		}

		/// <summary>���̖��O��Ԃ̈�ӎ��ʎq���擾���܂��B</summary>
		public int Id { get { return _id; } }

		/// <summary>���̖��O��ԂɊ܂܂�Ă��邷�ׂĂ̖��O��Ԃ܂��͌^�̖��O���擾���܂��B</summary>
		/// <returns>���O��ԓ��̂��ׂĂ̖��O��Ԃ܂��͌^�̖��O�̃��X�g�B</returns>
		public IEnumerable<string> GetMemberNames()
		{
			LoadNamespaces();
			lock (_topPackage.HierarchyLock)
				return GetStringKeys().OrderBy(x => x).ToArray();
		}

		/// <summary><see cref="MemberTracker"/> �̎�ނ��擾���܂��B</summary>
		public override sealed TrackerTypes MemberType { get { return TrackerTypes.Namespace; } }

		/// <summary>�����o��_���I�ɐ錾����^���擾���܂��B</summary>
		public override Type DeclaringType { get { return null; } }

		void UpdateId() { _id = Interlocked.Increment(ref _masterId); }

		/// <summary>���̖��O��Ԉȉ��̂��ׂĂ̖��O��Ԃ̈�ӎ��ʎq���X�V���܂��B</summary>
		protected void UpdateSubtreeIds()
		{
			// lock is held when this is called
			UpdateId();
			foreach (var ns in _dict.Select(x => x.Value as NamespaceTracker).Where(x => x != null))
				ns.UpdateSubtreeIds();
		}

		/// <summary>�w�肳�ꂽ�A�Z���u������^��ǂݍ��݂܂��B</summary>
		/// <param name="assembly">�ǂݍ��ތ^���`���Ă���A�Z���u�����w�肵�܂��B</param>
		/// <param name="includePrivateTypes">�A�Z���u�����̃v���C�x�[�g�ł���^��ǂݍ��ނ��ǂ����������l���w�肵�܂��B</param>
		/// <returns>�A�Z���u���Œ�`���ꂽ�^�̔z��B</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal static Type[] LoadTypesFromAssembly(Assembly assembly, bool includePrivateTypes)
		{
			ContractUtils.RequiresNotNull(assembly, "assembly");
			if (!includePrivateTypes)
			{
				try { return assembly.GetExportedTypes(); }
				// GetExportedTypes does not work with dynamic assemblies
				// Some type loads may cause exceptions. Unfortunately, there is no way to ask GetExportedTypes for just the list of types that we successfully loaded.
				catch (Exception) { }
			}
			Type[] result;
			try { result = assembly.GetTypes(); }
			catch (ReflectionTypeLoadException ex) { result = ex.Types; }
			return Array.FindAll(result, x => x != null && (includePrivateTypes || x.IsPublic));
		}
	}
}
