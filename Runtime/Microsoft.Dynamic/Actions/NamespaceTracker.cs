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
	/// <summary>CLS の名前空間を表します。</summary>
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

		/// <summary>指定された名前を使用して、<see cref="Microsoft.Scripting.Actions.NamespaceTracker"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="name">この名前空間の名前を指定します。</param>
		protected NamespaceTracker(string name)
		{
			UpdateId();
			_fullName = name;
		}

		/// <summary>このオブジェクトの文字列表現を返します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
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

		/// <summary>(子の名前空間ではなく) 現在の名前空間に寄与するすべてのアセンブリからすべての型をロードします。</summary>
		void LoadAllTypes()
		{
			foreach (string typeName in _typeNames.Values.SelectMany(x => x.NormalizedTypeNames))
			{
				MemberTracker value;
				if (!TryGetValue(SymbolTable.StringToId(typeName), out value))
				{
					Debug.Fail("TryGetMember は TypeLoadException を発生させるはずなので、ここに到達するのはあり得ない。");
					throw new TypeLoadException(typeName);
				}
			}
		}

		/// <summary>この名前空間の名前を取得します。</summary>
		public override string Name { get { return _fullName; } }

		/// <summary>指定されたアセンブリ内のすべての型を現在の名前空間以下の適切な名前空間に配置します。</summary>
		/// <param name="assem">配置する型を含んでいるアセンブリを指定します。</param>
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
		/// 型がどのアセンブリにも存在しない場合のフォールバックとして機能します。
		/// これはハードコードされた型リスト内に存在しない新しい型が追加された場合に発生します。
		/// </summary>
		/// <remarks>
		/// このコードは以下の理由より正確ではありません:
		/// 1. ジェネリック型を取り扱いません。(型衝突)
		/// 2. GetCustomMemberNames への以前の呼び出し (例えば Python における "from foo import *" など) はこの型に含まれません。
		/// 3. これはアセンブリに追加された新しい名前空間を取り扱いません。
		/// </remarks>
		TypeTracker CheckForUnlistedType(string nameString)
		{
			Assert.NotNull(nameString);
			var result = _packageAssemblies.Select(x => x.GetType(GetFullChildName(nameString), false)).FirstOrDefault(x => x != null && !x.IsNested && (x.IsPublic || _topPackage.DomainManager.Configuration.PrivateBinding));
			return result != null ? ReflectionCache.GetTypeTracker(result) : null;
		}

		#region IAttributesCollection Members

		/// <summary>指定したキーおよび値を持つ要素を <see cref="Microsoft.Scripting.IAttributesCollection"/> に追加します。</summary>
		/// <param name="name">追加する要素のキーとして使用する <see cref="SymbolId"/>。</param>
		/// <param name="value">追加する要素の値として使用するオブジェクト。</param>
		public void Add(SymbolId name, object value) { throw new InvalidOperationException(); }

		/// <summary>指定したキーに関連付けられている値を取得します。</summary>
		/// <param name="name">値を取得する対象のキー。</param>
		/// <param name="value">
		/// このメソッドが返されるときに、キーが見つかった場合は、指定したキーに関連付けられている値。それ以外の場合は <c>null</c>。
		/// このパラメーターは初期化せずに渡されます。
		/// </param>
		/// <returns>指定したキーを持つ要素が <see cref="Microsoft.Scripting.IAttributesCollection"/> を実装するオブジェクトに格納されている場合は
		/// <c>true</c>。それ以外の場合は <c>false</c>。
		/// </returns>
		public bool TryGetValue(SymbolId name, out object value)
		{
			MemberTracker tmp;
			bool res = TryGetValue(name, out tmp);
			value = tmp;
			return res;
		}

		/// <summary>指定したキーに関連付けられているメンバを取得します。</summary>
		/// <param name="name">メンバを取得する対象のキー。</param>
		/// <param name="value">
		/// このメソッドが返されるときに、キーが見つかった場合は、指定したキーに関連付けられている値。それ以外の場合は <c>null</c>。
		/// このパラメーターは初期化せずに渡されます。
		/// </param>
		/// <returns>指定したキーを持つ要素が <see cref="Microsoft.Scripting.IAttributesCollection"/> を実装するオブジェクトに格納されている場合は
		/// <c>true</c>。それ以外の場合は <c>false</c>。
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

		/// <summary>指定したキーを持つ要素を <see cref="Microsoft.Scripting.IAttributesCollection"/> から削除します。</summary>
		/// <param name="name">削除する要素のキー。</param>
		/// <returns>
		/// 要素が正常に削除された場合は <c>true</c>。それ以外の場合は <c>false</c>。
		/// このメソッドは、<paramref name="name"/> が元の <see cref="Microsoft.Scripting.IAttributesCollection"/> に見つからなかった場合にも <c>false</c> を返します。
		/// </returns>
		public bool Remove(SymbolId name) { throw new InvalidOperationException(); }

		/// <summary>指定したキーの要素が <see cref="Microsoft.Scripting.IAttributesCollection"/> に格納されているかどうかを確認します。</summary>
		/// <param name="name"><see cref="Microsoft.Scripting.IAttributesCollection"/> 内で検索されるキー。</param>
		/// <returns>指定したキーを持つ要素を <see cref="Microsoft.Scripting.IAttributesCollection"/> が保持している場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool ContainsKey(SymbolId name)
		{
			MemberTracker dummy;
			return TryGetValue(name, out dummy);
		}

		/// <summary>指定したキーを持つ要素を取得または設定します。</summary>
		/// <param name="name">取得または設定する要素のキー。</param>
		/// <returns>指定したキーを持つ要素。</returns>
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

		/// <summary><see cref="SymbolId"/> がキーである属性のディクショナリを取得します。</summary>
		public IDictionary<SymbolId, object> SymbolAttributes
		{
			get
			{
				LoadNamespaces();
				return this.Select(x => new KeyValuePair<string, object>(x.Key as string, x.Value)).Where(x => x.Key != null)
					.ToDictionary(x => SymbolTable.StringToId(x.Key), x => x.Value);
			}
		}

		/// <summary>指定したキーおよび値を持つ要素を <see cref="Microsoft.Scripting.IAttributesCollection"/> に追加します。</summary>
		/// <param name="name">追加する要素のキーとして使用するオブジェクト。</param>
		/// <param name="value">追加する要素の値として使用するオブジェクト。</param>
		public void Add(object name, object value) { throw new InvalidOperationException(); }

		/// <summary>指定したキーに関連付けられている値を取得します。</summary>
		/// <param name="name">値を取得する対象のキー。</param>
		/// <param name="value">
		/// このメソッドが返されるときに、キーが見つかった場合は、指定したキーに関連付けられている値。それ以外の場合は <c>null</c>。
		/// このパラメーターは初期化せずに渡されます。
		/// </param>
		/// <returns>指定したキーを持つ要素が <see cref="Microsoft.Scripting.IAttributesCollection"/> を実装するオブジェクトに格納されている場合は
		/// <c>true</c>。それ以外の場合は <c>false</c>。
		/// </returns>
		public bool TryGetValue(object name, out object value)
		{
			var str = name as string;
			if (str != null)
				return TryGetValue(SymbolTable.StringToId(str), out value);
			value = null;
			return false;
		}

		/// <summary>指定したキーを持つ要素を <see cref="Microsoft.Scripting.IAttributesCollection"/> から削除します。</summary>
		/// <param name="name">削除する要素のキー。</param>
		/// <returns>
		/// 要素が正常に削除された場合は <c>true</c>。それ以外の場合は <c>false</c>。
		/// このメソッドは、<paramref name="name"/> が元の <see cref="Microsoft.Scripting.IAttributesCollection"/> に見つからなかった場合にも <c>false</c> を返します。
		/// </returns>
		public bool Remove(object name) { throw new InvalidOperationException(); }

		/// <summary>指定したキーの要素が <see cref="Microsoft.Scripting.IAttributesCollection"/> に格納されているかどうかを確認します。</summary>
		/// <param name="name"><see cref="Microsoft.Scripting.IAttributesCollection"/> 内で検索されるキー。</param>
		/// <returns>指定したキーを持つ要素を <see cref="Microsoft.Scripting.IAttributesCollection"/> が保持している場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool ContainsKey(object name)
		{
			object dummy;
			return TryGetValue(name, out dummy);
		}

		/// <summary>このオブジェクトを <see cref="System.Collections.Generic.IDictionary&lt;Object, Object&gt;"/> として取得します。</summary>
		/// <returns><see cref="System.Collections.Generic.IDictionary&lt;Object, Object&gt;"/> という形式で取得された現在のオブジェクト。</returns>
		public IDictionary<object, object> AsObjectKeyedDictionary()
		{
			LoadNamespaces();
			lock (_topPackage.HierarchyLock)
				return _dict.ToDictionary(x => (object)x.Key, x => (object)x.Value);
		}

		/// <summary><see cref="Microsoft.Scripting.IAttributesCollection"/> に格納されている要素の数を取得します。</summary>
		public int Count { get { return _dict.Count; } }

		/// <summary><see cref="Microsoft.Scripting.IAttributesCollection"/> のキーを保持している <see cref="System.Collections.Generic.ICollection&lt;Object&gt;"/> を取得します。</summary>
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

		/// <summary>このコレクションを反復処理する列挙子を返します。</summary>
		/// <returns>コレクションを反復処理する列挙子。</returns>
		[Pure]
		public IEnumerator<KeyValuePair<object, object>> GetEnumerator()
		{
			foreach (var key in Keys)
				yield return new KeyValuePair<object, object>(key, this[SymbolTable.StringToId((string)key)]);
		}

		/// <summary>このコレクションを反復処理する列挙子を返します。</summary>
		/// <returns>コレクションを反復処理する列挙子。</returns>
		[Pure]
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }

		/// <summary>この名前空間に関連付けられたアセンブリのリストを取得します。</summary>
		public IList<Assembly> PackageAssemblies
		{
			get
			{
				LoadNamespaces();
				return _packageAssemblies;
			}
		}

		/// <summary>最上位名前空間に関連付けられたすべてのアセンブリ内の型を適切な名前空間に配置します。</summary>
		protected virtual void LoadNamespaces()
		{
			if (_topPackage != null)
				_topPackage.LoadNamespaces();
		}

		/// <summary>この名前空間の最上位名前空間を設定します。</summary>
		/// <param name="package">設定する最上位名前空間を指定します。</param>
		protected void SetTopPackage(TopNamespaceTracker package)
		{
			Assert.NotNull(package);
			_topPackage = package;
		}

		/// <summary>
		/// 単一のアセンブリによる単一の名前空間内のパブリックでネストされていないすべての型の名前を格納します。
		/// このクラスによってすべての型を積極的にロードすることなく名前空間を検査できるようになります。
		/// 型の積極的ロードは起動時間を遅くし、ワーキングセットを増加させ、さらに要求よりも早く TypeLoadException を発生させる可能性があるため意味的に正しくなくなります。
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

		/// <summary>この名前空間の一意識別子を取得します。</summary>
		public int Id { get { return _id; } }

		/// <summary>この名前空間に含まれているすべての名前空間または型の名前を取得します。</summary>
		/// <returns>名前空間内のすべての名前空間または型の名前のリスト。</returns>
		public IEnumerable<string> GetMemberNames()
		{
			LoadNamespaces();
			lock (_topPackage.HierarchyLock)
				return GetStringKeys().OrderBy(x => x).ToArray();
		}

		/// <summary><see cref="MemberTracker"/> の種類を取得します。</summary>
		public override sealed TrackerTypes MemberType { get { return TrackerTypes.Namespace; } }

		/// <summary>メンバを論理的に宣言する型を取得します。</summary>
		public override Type DeclaringType { get { return null; } }

		void UpdateId() { _id = Interlocked.Increment(ref _masterId); }

		/// <summary>この名前空間以下のすべての名前空間の一意識別子を更新します。</summary>
		protected void UpdateSubtreeIds()
		{
			// lock is held when this is called
			UpdateId();
			foreach (var ns in _dict.Select(x => x.Value as NamespaceTracker).Where(x => x != null))
				ns.UpdateSubtreeIds();
		}

		/// <summary>指定されたアセンブリから型を読み込みます。</summary>
		/// <param name="assembly">読み込む型を定義しているアセンブリを指定します。</param>
		/// <param name="includePrivateTypes">アセンブリ内のプライベートである型を読み込むかどうかを示す値を指定します。</param>
		/// <returns>アセンブリで定義された型の配列。</returns>
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
