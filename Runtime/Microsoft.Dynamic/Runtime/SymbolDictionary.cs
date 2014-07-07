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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Contracts;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>
	/// メンバのコレクションの格納に使用される単純なスレッドセーフのディクショナリです。
	/// 他のすべてのシンボルディクショナリと同様にこのクラスは <see cref="SymbolId"/> とオブジェクトの両方による検索をサポートします。
	/// </summary>
	/// <remarks>
	/// シンボルディクショナリは通常リテラル文字列によってインデックスされます。
	/// また、その文字列はシンボルを使用してハンドルされます。
	/// しかし文字列以外のキーを認める言語も存在します。
	/// この場合はオブジェクトによってインデックスされるディクショナリを作成して、シンボルによってインデックスされるディクショナリ内に保持します。
	/// そのようなアクセスは低速ですが許容できるものです。
	/// </remarks>
	public sealed class SymbolDictionary : BaseSymbolDictionary, IDictionary, IDictionary<object, object>, IAttributesCollection
	{
		Dictionary<SymbolId, object> _data = new Dictionary<SymbolId, object>();

		/// <summary><see cref="Microsoft.Scripting.Runtime.SymbolDictionary"/> クラスの新しいインスタンスを初期化します。</summary>
		public SymbolDictionary() { }

		/// <summary>基にする <see cref="IAttributesCollection"/> を使用して、<see cref="Microsoft.Scripting.Runtime.SymbolDictionary"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="from">要素がコピーされる <see cref="IAttributesCollection"/> を指定します。</param>
		public SymbolDictionary(IAttributesCollection from)
		{
			// enumeration of a dictionary requires locking the target dictionary.
			lock (from)
			{
				foreach (var kvp in from)
					Add(kvp.Key, kvp.Value);
			}
		}

		Dictionary<object, object> GetObjectKeysDictionary()
		{
			var objData = GetObjectKeysDictionaryIfExists();
			if (objData == null)
				_data.Add(ObjectKeys, objData = new Dictionary<object, object>());
			return objData;
		}

		Dictionary<object, object> GetObjectKeysDictionaryIfExists()
		{
			object objData;
			return _data.TryGetValue(ObjectKeys, out objData) ? (Dictionary<object, object>)objData : null;
		}

		/// <summary>指定したキーおよび値を持つ要素を <see cref="SymbolDictionary"/> に追加します。</summary>
		/// <param name="key">追加する要素のキーとして使用するオブジェクト。</param>
		/// <param name="value">追加する要素の値として使用するオブジェクト。</param>
		public void Add(object key, object value)
		{
			Debug.Assert(!(key is SymbolId));
			var strKey = key as string;
			lock (this)
			{
				if (strKey != null)
					_data.Add(SymbolTable.StringToId(strKey), value);
				else
					GetObjectKeysDictionary()[key] = value;
			}
		}

		/// <summary>指定したキーの要素が <see cref="SymbolDictionary"/> に格納されているかどうかを確認します。</summary>
		/// <param name="key"><see cref="SymbolDictionary"/> 内で検索されるキー。</param>
		/// <returns>指定したキーを持つ要素を <see cref="SymbolDictionary"/> が保持している場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		[Confined]
		public bool ContainsKey(object key)
		{
			Debug.Assert(!(key is SymbolId));
			var strKey = key as string;
			lock (this)
			{
				if (strKey != null)
					return SymbolTable.StringHasId(strKey) && _data.ContainsKey(SymbolTable.StringToId(strKey));
				else
				{
					var objData = GetObjectKeysDictionaryIfExists();
					return objData != null && objData.ContainsKey(key);
				}
			}
		}

		/// <summary><see cref="SymbolDictionary"/> のキーを保持している <see cref="ICollection&lt;Object&gt;"/> を取得します。</summary>
		public ICollection<object> Keys
		{
			get
			{
				lock (this)
				{
					IEnumerable<object> res = _data.Keys.Where(x => x != ObjectKeys).Select(x => SymbolTable.IdToString(x));
					var objData = GetObjectKeysDictionaryIfExists();
					return (objData != null ? res.Concat(objData.Keys) : res).ToArray();
				}
			}
		}

		/// <summary>指定したキーを持つ要素を <see cref="SymbolDictionary"/> から削除します。</summary>
		/// <param name="key">削除する要素のキー。</param>
		/// <returns>
		/// 要素が正常に削除された場合は <c>true</c>。それ以外の場合は <c>false</c>。
		/// このメソッドは、<paramref name="key"/> が元の <see cref="SymbolDictionary"/> に見つからなかった場合にも <c>false</c> を返します。
		/// </returns>
		public bool Remove(object key)
		{
			Debug.Assert(!(key is SymbolId));
			var strKey = key as string;
			lock (this)
			{
				if (strKey != null)
					return SymbolTable.StringHasId(strKey) && _data.Remove(SymbolTable.StringToId(strKey));
				else
				{
					var objData = GetObjectKeysDictionaryIfExists();
					return objData != null && objData.Remove(key);
				}
			}
		}

		/// <summary>指定したキーに関連付けられている値を取得します。</summary>
		/// <param name="key">値を取得する対象のキー。</param>
		/// <param name="value">
		/// このメソッドが返されるときに、キーが見つかった場合は、指定したキーに関連付けられている値。それ以外の場合は <c>null</c>。
		/// このパラメーターは初期化せずに渡されます。
		/// </param>
		/// <returns>指定したキーを持つ要素が <see cref="SymbolDictionary"/> に格納されている場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool TryGetValue(object key, out object value)
		{
			Debug.Assert(!(key is SymbolId));
			var strKey = key as string;
			lock (this)
			{
				value = null;
				if (strKey != null)
					return SymbolTable.StringHasId(strKey) && _data.TryGetValue(SymbolTable.StringToId(strKey), out value);
				else
				{
					var objData = GetObjectKeysDictionaryIfExists();
					return objData != null && objData.TryGetValue(key, out value);
				}
			}
		}

		/// <summary><see cref="SymbolDictionary"/> の値を保持している <see cref="ICollection&lt;Object&gt;"/> を取得します。</summary>
		public ICollection<object> Values
		{
			get
			{
				lock (this)
				{
					var objData = GetObjectKeysDictionaryIfExists();
					return objData == null ? (ICollection<object>)_data.Values : _data.Where(x => x.Key != ObjectKeys).Select(x => x.Value).Concat(objData.Values).ToArray();
				}
			}
		}

		/// <summary>指定したキーを持つ要素を取得または設定します。</summary>
		/// <param name="key">取得または設定する要素のキー。</param>
		/// <returns>指定したキーを持つ要素。</returns>
		public object this[object key]
		{
			get
			{
				object value;
				if (TryGetValue(key, out value))
					return value;
				throw new KeyNotFoundException(string.Format("'{0}'", key));
			}
			set
			{
				Debug.Assert(!(key is SymbolId));
				var strKey = key as string;
				lock (this)
				{
					if (strKey != null)
						_data[SymbolTable.StringToId(strKey)] = value;
					else
						GetObjectKeysDictionary()[key] = value;
				}
			}
		}

		void ICollection<KeyValuePair<object, object>>.Add(KeyValuePair<object, object> item) { Add(item.Key, item.Value); }

		/// <summary><see cref="SymbolDictionary"/> からすべての項目を削除します。</summary>
		public void Clear() { lock (this) _data.Clear(); }

		[Confined]
		bool ICollection<KeyValuePair<object, object>>.Contains(KeyValuePair<object, object> item)
		{
			object value;
			return TryGetValue(item.Key, out value) && value == item.Value;
		}

		void ICollection<KeyValuePair<object, object>>.CopyTo(KeyValuePair<object, object>[] array, int arrayIndex)
		{
			ContractUtils.RequiresNotNull(array, "array");
			ContractUtils.RequiresArrayRange(array, arrayIndex, Count, "arrayIndex", "array");
			lock (this)
			{
				foreach (var o in AsObjectKeyedDictionary())
					array[arrayIndex++] = o;
			}
		}

		/// <summary><see cref="SymbolDictionary"/> に格納されている要素の数を取得します。</summary>
		public int Count
		{
			get
			{
				lock (this)
				{
					var objData = GetObjectKeysDictionaryIfExists();
					return objData == null ? _data.Count : _data.Count + objData.Count - 1; // -1 is because data contains objData
				}
			}
		}

		/// <summary><see cref="SymbolDictionary"/> が読み取り専用かどうかを示す値を取得します。</summary>
		public bool IsReadOnly { get { return false; } }

		bool ICollection<KeyValuePair<object, object>>.Remove(KeyValuePair<object, object> item) { return ((ICollection<KeyValuePair<object, object>>)this).Contains(item) && Remove(item.Key); }

		[Pure]
		IEnumerator<KeyValuePair<object, object>> IEnumerable<KeyValuePair<object, object>>.GetEnumerator() { return GetEnumerator(); }

		[Pure]
		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

		/// <summary>指定したキーおよび値を持つ要素を <see cref="SymbolDictionary"/> に追加します。</summary>
		/// <param name="name">追加する要素のキーとして使用する <see cref="SymbolId"/>。</param>
		/// <param name="value">追加する要素の値として使用するオブジェクト。</param>
		public void Add(SymbolId name, object value) { lock (this) _data.Add(name, value); }

		/// <summary>指定したキーの要素が <see cref="SymbolDictionary"/> に格納されているかどうかを確認します。</summary>
		/// <param name="name"><see cref="SymbolDictionary"/> 内で検索されるキー。</param>
		/// <returns>指定したキーを持つ要素を <see cref="SymbolDictionary"/> が保持している場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool ContainsKey(SymbolId name) { lock (this) return _data.ContainsKey(name); }

		/// <summary>指定したキーを持つ要素を <see cref="SymbolDictionary"/> から削除します。</summary>
		/// <param name="name">削除する要素のキー。</param>
		/// <returns>
		/// 要素が正常に削除された場合は <c>true</c>。それ以外の場合は <c>false</c>。
		/// このメソッドは、<paramref name="name"/> が元の <see cref="SymbolDictionary"/> に見つからなかった場合にも <c>false</c> を返します。
		/// </returns>
		public bool Remove(SymbolId name) { lock (this) return _data.Remove(name); }

		/// <summary>指定したキーに関連付けられている値を取得します。</summary>
		/// <param name="name">値を取得する対象のキー。</param>
		/// <param name="value">
		/// このメソッドが返されるときに、キーが見つかった場合は、指定したキーに関連付けられている値。それ以外の場合は <c>null</c>。
		/// このパラメーターは初期化せずに渡されます。
		/// </param>
		/// <returns>指定したキーを持つ要素が <see cref="SymbolDictionary"/> に格納されている場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool TryGetValue(SymbolId name, out object value) { lock (this) return _data.TryGetValue(name, out value); }

		/// <summary>指定したキーを持つ要素を取得または設定します。</summary>
		/// <param name="name">取得または設定する要素のキー。</param>
		/// <returns>指定したキーを持つ要素。</returns>
		public object this[SymbolId name]
		{
			get { lock (this) return _data[name]; }
			set { lock (this) _data[name] = value; }
		}

		/// <summary><see cref="SymbolId"/> がキーである属性のディクショナリを取得します。</summary>
		public IDictionary<SymbolId, object> SymbolAttributes { get { lock (this) return GetObjectKeysDictionaryIfExists() == null ? _data : _data.Where(x => x.Key != ObjectKeys).ToDictionary(x => x.Key, x => x.Value); } }

		/// <summary>このオブジェクトを <see cref="System.Collections.Generic.IDictionary&lt;Object, Object&gt;"/> として取得します。</summary>
		/// <returns><see cref="System.Collections.Generic.IDictionary&lt;Object, Object&gt;"/> という形式で取得された現在のオブジェクト。</returns>
		public IDictionary<object, object> AsObjectKeyedDictionary() { return this; }

		[Pure]
		bool IDictionary.Contains(object key) { return ContainsKey(key); }

		[Pure]
		IDictionaryEnumerator IDictionary.GetEnumerator() { return GetEnumerator(); }

		/// <summary>このコレクションの要素を列挙するための列挙子を返します。</summary>
		/// <returns>要素の列挙に使用される列挙子。</returns>
		[Pure]
		public CheckedDictionaryEnumerator GetEnumerator()
		{
			var dataEnum = new TransformDictionaryEnumerator(_data);
			var objData = GetObjectKeysDictionaryIfExists();
			return objData == null ? (CheckedDictionaryEnumerator)dataEnum : new DictionaryUnionEnumerator(new IDictionaryEnumerator[] { dataEnum, objData.GetEnumerator() });
		}

		bool IDictionary.IsFixedSize { get { return false; } }

		ICollection IDictionary.Keys { get { return new List<object>(Keys); } }

		void IDictionary.Remove(object key) { Remove(key); }

		ICollection IDictionary.Values { get { return new List<object>(Values); } }

		void ICollection.CopyTo(Array array, int index)
		{
			ContractUtils.RequiresNotNull(array, "array");
			ContractUtils.RequiresArrayRange(array.Length, index, Count, "index", "array");
			lock (this)
			{
				foreach (var o in this)
					array.SetValue(o, index++);
			}
		}

		bool ICollection.IsSynchronized { get { return true; } }

		object ICollection.SyncRoot { get { return this; } } // TODO: We should really lock on something else...
	}
}
