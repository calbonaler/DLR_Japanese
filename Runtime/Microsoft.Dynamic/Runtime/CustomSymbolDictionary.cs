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
using System.Diagnostics;
using System.Linq;
using Microsoft.Contracts;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>
	/// 最適化されたスレッドセーフのシンボルディクショナリに対する抽象基底クラスです。
	/// 実装者はこのクラスから派生して、<see cref="ExtraKeys"/>、<see cref="TrySetExtraValue"/> および <see cref="TryGetExtraValue"/> をオーバーライドしてください。
	/// 値の検索時は最初に最適化された関数を使用して追加のキーが検索されます。
	/// 値が見つからなかった場合は、基になる .NET ディクショナリに値が格納されます。
	/// </summary>
	public abstract class CustomSymbolDictionary : BaseSymbolDictionary, System.Collections.IDictionary, IDictionary<object, object>, IAttributesCollection
	{
		Dictionary<SymbolId, object> _data;

		/// <summary><see cref="Microsoft.Scripting.Runtime.CustomSymbolDictionary"/> クラスの新しいインスタンスを初期化します。</summary>
		protected CustomSymbolDictionary() { }

		/// <summary>モジュールの最適化された実装によってキャッシュされる追加のキーを取得します。</summary>
		/// <returns>追加のキーを表す <see cref="SymbolId"/> の配列。</returns>
		protected abstract ReadOnlyCollection<SymbolId> ExtraKeys { get; }

		/// <summary>追加の値の設定を試み、指定されたキーに対する値が正常に設定されたかどうかを示す値を返します。</summary>
		/// <param name="key">設定する値に対するキーを指定します。</param>
		/// <param name="value">設定する値を指定します。</param>
		/// <returns>指定されたキーに対して値が正常に設定された場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		protected abstract bool TrySetExtraValue(SymbolId key, object value);

		/// <summary>追加の値の取得を試み、指定されたキーに対する値が正常に取得されたかどうかを示す値を返します。値が <see cref="Uninitialized"/> であっても <c>true</c> を返します。</summary>
		/// <param name="key">取得する値に対するキーを指定します。</param>
		/// <param name="value">取得された値が格納されます。</param>
		/// <returns>指定されたキーに対して値が正常に取得された場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate")]
		protected abstract bool TryGetExtraValue(SymbolId key, out object value);

		void InitializeData()
		{
			Debug.Assert(_data == null);
			_data = new Dictionary<SymbolId, object>();
		}

		Dictionary<object, object> GetObjectKeysDictionary()
		{
			var objData = GetObjectKeysDictionaryIfExists();
			if (objData == null)
			{
				if (_data == null)
					InitializeData();
				_data.Add(ObjectKeys, objData = new Dictionary<object, object>());
			}
			return objData;
		}

		Dictionary<object, object> GetObjectKeysDictionaryIfExists()
		{
			if (_data == null)
				return null;
			object objData;
			if (_data.TryGetValue(ObjectKeys, out objData))
				return (Dictionary<object, object>)objData;
			return null;
		}

		/// <summary>指定したキーおよび値を持つ要素を <see cref="CustomSymbolDictionary"/> に追加します。</summary>
		/// <param name="key">追加する要素のキーとして使用するオブジェクト。</param>
		/// <param name="value">追加する要素の値として使用するオブジェクト。</param>
		public void Add(object key, object value)
		{
			Debug.Assert(!(key is SymbolId));
			var strKey = key as string;
			if (strKey != null)
				Add(SymbolTable.StringToId(strKey), value);
			else
				lock (this) GetObjectKeysDictionary()[key] = value;
		}

		/// <summary>指定したキーの要素が <see cref="CustomSymbolDictionary"/> に格納されているかどうかを確認します。</summary>
		/// <param name="key"><see cref="CustomSymbolDictionary"/> 内で検索されるキー。</param>
		/// <returns>指定したキーを持つ要素を <see cref="CustomSymbolDictionary"/> が保持している場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		[Confined]
		public bool ContainsKey(object key)
		{
			Debug.Assert(!(key is SymbolId));
			lock (this)
			{
				object dummy;
				return TryGetValue(key, out dummy);
			}
		}

		/// <summary><see cref="CustomSymbolDictionary"/> のキーを保持している <see cref="ICollection&lt;Object&gt;"/> を取得します。</summary>
		public ICollection<object> Keys { get { lock (this) return AsObjectKeyedDictionary().Select(x => x.Key).ToArray(); } }

		/// <summary>指定したキーを持つ要素を <see cref="CustomSymbolDictionary"/> から削除します。</summary>
		/// <param name="key">削除する要素のキー。</param>
		/// <returns>
		/// 要素が正常に削除された場合は <c>true</c>。それ以外の場合は <c>false</c>。
		/// このメソッドは、<paramref name="key"/> が元の <see cref="CustomSymbolDictionary"/> に見つからなかった場合にも <c>false</c> を返します。
		/// </returns>
		public bool Remove(object key)
		{
			Debug.Assert(!(key is SymbolId));
			string strKey = key as string;
			if (strKey != null)
				return SymbolTable.StringHasId(strKey) && Remove(SymbolTable.StringToId(strKey));
			lock (this)
			{
				var objData = GetObjectKeysDictionaryIfExists();
				if (objData == null)
					return false;
				return objData.Remove(key);
			}
		}

		/// <summary>指定したキーに関連付けられている値を取得します。</summary>
		/// <param name="key">値を取得する対象のキー。</param>
		/// <param name="value">
		/// このメソッドが返されるときに、キーが見つかった場合は、指定したキーに関連付けられている値。それ以外の場合は <c>null</c>。
		/// このパラメーターは初期化せずに渡されます。
		/// </param>
		/// <returns>指定したキーを持つ要素が <see cref="CustomSymbolDictionary"/> に格納されている場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool TryGetValue(object key, out object value)
		{
			Debug.Assert(!(key is SymbolId));
			string strKey = key as string;
			value = null;
			if (strKey != null)
				return SymbolTable.StringHasId(strKey) && TryGetValue(SymbolTable.StringToId(strKey), out value);
			lock (this)
			{
				var objData = GetObjectKeysDictionaryIfExists();
				if (objData != null)
					return objData.TryGetValue(key, out value);
			}
			return false;
		}

		/// <summary><see cref="CustomSymbolDictionary"/> の値を保持している <see cref="ICollection&lt;Object&gt;"/> を取得します。</summary>
		public ICollection<object> Values { get { lock (this) return AsObjectKeyedDictionary().Select(x => x.Value).ToArray(); } }

		/// <summary>指定したキーを持つ要素を取得または設定します。</summary>
		/// <param name="key">取得または設定する要素のキー。</param>
		/// <returns>指定したキーを持つ要素。</returns>
		public object this[object key]
		{
			get
			{
				Debug.Assert(!(key is SymbolId));
				object res;
				if (TryGetValue(key, out res))
					return res;
				throw new KeyNotFoundException(key.ToString());
			}
			set
			{
				Debug.Assert(!(key is SymbolId));
				string strKey = key as string;
				if (strKey != null)
					this[SymbolTable.StringToId(strKey)] = value;
				else
					lock (this) GetObjectKeysDictionary()[key] = value;
			}
		}

		void ICollection<KeyValuePair<object, object>>.Add(KeyValuePair<object, object> item) { Add(item.Key, item.Value); }

		/// <summary><see cref="CustomSymbolDictionary"/> からすべての項目を削除します。</summary>
		public void Clear()
		{
			lock (this)
			{
				foreach (var key in ExtraKeys)
				{
					if (key.Id < 0)
						break;
					TrySetExtraValue(key, Uninitialized.Instance);
				}
				_data = null;
			}
		}

		[Confined]
		bool ICollection<KeyValuePair<object, object>>.Contains(KeyValuePair<object, object> item) { object o; return TryGetValue(item.Key, out o) && o == item.Value; }

		void ICollection<KeyValuePair<object, object>>.CopyTo(KeyValuePair<object, object>[] array, int arrayIndex)
		{
			ContractUtils.RequiresNotNull(array, "array");
			ContractUtils.RequiresArrayRange(array, arrayIndex, Count, "araryIndex", "Count");
			foreach (var kvp in ((IEnumerable<KeyValuePair<object, object>>)this))
				array[arrayIndex++] = kvp;
		}

		/// <summary><see cref="CustomSymbolDictionary"/> に格納されている要素の数を取得します。</summary>
		public int Count
		{
			get
			{
				int count = 0;
				foreach (var _ in this)
					count++;
				return count;
			}
		}

		/// <summary><see cref="CustomSymbolDictionary"/> が読み取り専用かどうかを示す値を取得します。</summary>
		public bool IsReadOnly { get { return false; } }

		bool ICollection<KeyValuePair<object, object>>.Remove(KeyValuePair<object, object> item) { return ((ICollection<KeyValuePair<object, object>>)this).Contains(item) && Remove(item.Key); }

		[Pure]
		IEnumerator<KeyValuePair<object, object>> IEnumerable<KeyValuePair<object, object>>.GetEnumerator() { return GetEnumerator(); }

		[Pure]
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }

		/// <summary>指定したキーおよび値を持つ要素を <see cref="CustomSymbolDictionary"/> に追加します。</summary>
		/// <param name="name">追加する要素のキーとして使用する <see cref="SymbolId"/>。</param>
		/// <param name="value">追加する要素の値として使用するオブジェクト。</param>
		public void Add(SymbolId name, object value)
		{
			lock (this)
			{
				if (TrySetExtraValue(name, value))
					return;
				if (_data == null)
					InitializeData();
				_data.Add(name, value);
			}
		}

		/// <summary>指定したキーの要素が <see cref="CustomSymbolDictionary"/> に格納されているかどうかを確認します。</summary>
		/// <param name="name"><see cref="CustomSymbolDictionary"/> 内で検索されるキー。</param>
		/// <returns>指定したキーを持つ要素を <see cref="CustomSymbolDictionary"/> が保持している場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool ContainsKey(SymbolId name)
		{
			object value;
			return TryGetValue(name, out value);
		}

		/// <summary>指定したキーを持つ要素を <see cref="CustomSymbolDictionary"/> から削除します。</summary>
		/// <param name="name">削除する要素のキー。</param>
		/// <returns>
		/// 要素が正常に削除された場合は <c>true</c>。それ以外の場合は <c>false</c>。
		/// このメソッドは、<paramref name="name"/> が元の <see cref="CustomSymbolDictionary"/> に見つからなかった場合にも <c>false</c> を返します。
		/// </returns>
		public bool Remove(SymbolId name)
		{
			object value;
			if (TryGetExtraValue(name, out value))
			{
				if (value == Uninitialized.Instance)
					return false;
				if (TrySetExtraValue(name, Uninitialized.Instance))
					return true;
			}
			if (_data == null)
				return false;
			lock (this)
				return _data.Remove(name);
		}

		/// <summary>指定したキーに関連付けられている値を取得します。</summary>
		/// <param name="name">値を取得する対象のキー。</param>
		/// <param name="value">
		/// このメソッドが返されるときに、キーが見つかった場合は、指定したキーに関連付けられている値。それ以外の場合は <c>null</c>。
		/// このパラメーターは初期化せずに渡されます。
		/// </param>
		/// <returns>指定したキーを持つ要素が <see cref="CustomSymbolDictionary"/> に格納されている場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool TryGetValue(SymbolId name, out object value)
		{
			if (TryGetExtraValue(name, out value) && value != Uninitialized.Instance)
				return true;
			if (_data == null)
				return false;
			lock (this)
				return _data.TryGetValue(name, out value);
		}

		/// <summary>指定したキーを持つ要素を取得または設定します。</summary>
		/// <param name="name">取得または設定する要素のキー。</param>
		/// <returns>指定したキーを持つ要素。</returns>
		public object this[SymbolId name]
		{
			get
			{
				object res;
				if (TryGetValue(name, out res))
					return res;
				throw new KeyNotFoundException(SymbolTable.IdToString(name));
			}
			set
			{
				if (TrySetExtraValue(name, value))
					return;
				lock (this)
				{
					if (_data == null)
						InitializeData();
					_data[name] = value;
				}
			}
		}

		/// <summary><see cref="SymbolId"/> がキーである属性のディクショナリを取得します。</summary>
		public IDictionary<SymbolId, object> SymbolAttributes
		{
			get
			{
				Dictionary<SymbolId, object> d;
				lock (this)
				{
					if (_data != null)
						d = new Dictionary<SymbolId, object>(_data);
					else
						d = new Dictionary<SymbolId, object>();
					foreach (var extraKey in ExtraKeys)
					{
						object value;
						if (TryGetExtraValue(extraKey, out value) && value != Uninitialized.Instance)
							d.Add(extraKey, value);
					}
				}
				return d;
			}
		}

		/// <summary>このオブジェクトを <see cref="System.Collections.Generic.IDictionary&lt;Object, Object&gt;"/> として取得します。</summary>
		/// <returns><see cref="System.Collections.Generic.IDictionary&lt;Object, Object&gt;"/> という形式で取得された現在のオブジェクト。</returns>
		public IDictionary<object, object> AsObjectKeyedDictionary() { return this; }

		[Pure]
		bool System.Collections.IDictionary.Contains(object key) { return ContainsKey(key); }

		[Pure]
		System.Collections.IDictionaryEnumerator System.Collections.IDictionary.GetEnumerator() { return GetEnumerator(); }

		class ExtraKeyEnumerator : CheckedDictionaryEnumerator
		{
			CustomSymbolDictionary _idDict;
			int _curIndex = -1;

			public ExtraKeyEnumerator(CustomSymbolDictionary idDict) { _idDict = idDict; }

			protected override object KeyCore { get { return SymbolTable.IdToString(_idDict.ExtraKeys[_curIndex]); } }

			protected override object ValueCore
			{
				get
				{
					object val;
					var hasExtraValue = _idDict.TryGetExtraValue(_idDict.ExtraKeys[_curIndex], out val);
					Debug.Assert(hasExtraValue && !(val is Uninitialized));
					return val;
				}
			}

			protected override bool MoveNextCore()
			{
				while (_curIndex < _idDict.ExtraKeys.Count - 1)
				{
					_curIndex++;
					if (_idDict.ExtraKeys[_curIndex].Id < 0)
						break;
					object val;
					if (_idDict.TryGetExtraValue(_idDict.ExtraKeys[_curIndex], out val) && val != Uninitialized.Instance)
						return true;
				}
				return false;
			}

			protected override void ResetCore() { _curIndex = -1; }
		}

		/// <summary>このコレクションの要素を列挙するための列挙子を返します。</summary>
		/// <returns>要素の列挙に使用される列挙子。</returns>
		[Pure]
		public CheckedDictionaryEnumerator GetEnumerator()
		{
			List<System.Collections.IDictionaryEnumerator> enums = new List<System.Collections.IDictionaryEnumerator>();
			enums.Add(new ExtraKeyEnumerator(this));
			if (_data != null)
				enums.Add(new TransformDictionaryEnumerator(_data));
			var objItems = GetObjectKeysDictionaryIfExists();
			if (objItems != null)
				enums.Add(objItems.GetEnumerator());
			return new DictionaryUnionEnumerator(enums);
		}

		bool System.Collections.IDictionary.IsFixedSize { get { return false; } }

		System.Collections.ICollection System.Collections.IDictionary.Keys { get { return new List<object>(Keys); } }

		void System.Collections.IDictionary.Remove(object key) { Remove(key); }

		System.Collections.ICollection System.Collections.IDictionary.Values { get { return new List<object>(Values); } }

		void System.Collections.ICollection.CopyTo(Array array, int index)
		{
			foreach (System.Collections.DictionaryEntry entry in this)
				array.SetValue(entry, index++);
		}

		bool System.Collections.ICollection.IsSynchronized { get { return true; } }

		object System.Collections.ICollection.SyncRoot { get { return this; } } // TODO: Sync root shouldn't be this, it should be data.
	}
}
