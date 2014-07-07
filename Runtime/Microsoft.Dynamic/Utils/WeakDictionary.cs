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
using System.Runtime.CompilerServices;
using Microsoft.Contracts;

namespace Microsoft.Scripting.Utils
{
	/// <summary>
	/// キーが他のオブジェクトから参照されなくなるとキーが利用できなくなるディクショナリを表します。
	/// 値はキーが生存している限り生存し続けます。
	/// </summary>
	/// <typeparam name="TKey">ディクショナリのキーの型を指定します。</typeparam>
	/// <typeparam name="TValue">ディクショナリの値の型を指定します。</typeparam>
	/// <remarks>
	/// 現在このクラスにはキーとして使用されているオブジェクトをこのクラスのどのインスタンスでも値として使用することができないという制限があります。
	/// さもなければ、オブジェクトは永遠に解放されません。
	/// これは事実上、このクラスの利用者のみが値として使用されているオブジェクトへアクセスできるようにする必要があることを意味します。
	/// 
	/// また、現在キーが収集されてから値が保持される期間に関する保証は存在しません。
	/// この問題は CheckCleanup() を呼び出すファイナライザをもつダミーのウォッチドッグオブジェクトを持ち、ガベージコレクション毎に CheckCleanup() をトリガーすることで解決できる可能性があります。
	/// </remarks>
	public class WeakDictionary<TKey, TValue> : IDictionary<TKey, TValue> where TKey : class
	{
		sealed class WeakComparer : EqualityComparer<object>
		{
			public override bool Equals(object x, object y)
			{
				TKey obj;
				var wx = x as HashableWeakReference;
				if (wx != null)
					x = wx.TryGetTarget(out obj) ? obj : null;
				var wy = y as HashableWeakReference;
				if (wy != null)
					y = wy.TryGetTarget(out obj) ? obj : null;
				return object.Equals(x, y);
			}

			public override int GetHashCode(object obj)
			{
				var wobj = obj as HashableWeakReference;
				if (wobj != null)
					return wobj.GetHashCode();
				return obj == null ? 0 : RuntimeHelpers.GetHashCode(obj);
			}
		}

		class HashableWeakReference
		{
			WeakReference<TKey> weakReference;
			int hashCode;

			public HashableWeakReference(TKey obj)
			{
				weakReference = new WeakReference<TKey>(obj, true);
				hashCode = obj == null ? 0 : RuntimeHelpers.GetHashCode(obj);
			}

			public bool TryGetTarget(out TKey obj) { return weakReference.TryGetTarget(out obj); }

			[Confined]
			public override int GetHashCode() { return hashCode; }

			[Confined]
			public override bool Equals(object obj)
			{
				TKey target;
				return TryGetTarget(out target) && target.Equals(obj);
			}
		}

		// The one and only comparer instance.
		static readonly IEqualityComparer<object> comparer = new WeakComparer();

		Dictionary<object, TValue> dict = new Dictionary<object, TValue>(comparer);
		int version, cleanupVersion;
		int cleanupGC = 0;

		/// <summary><see cref="Microsoft.Scripting.Utils.WeakDictionary&lt;TKey, TValue&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		public WeakDictionary() { }

		/// <summary>指定したキーおよび値を持つ要素を <see cref="WeakDictionary&lt;TKey, TValue&gt;"/> に追加します。</summary>
		/// <param name="key">追加する要素のキーとして使用するオブジェクト。</param>
		/// <param name="value">追加する要素の値として使用するオブジェクト。</param>
		/// <exception cref="ArgumentException">同じキーを持つ要素が、<see cref="WeakDictionary&lt;TKey, TValue&gt;"/> に既に存在します。</exception>
		public void Add(TKey key, TValue value)
		{
			CheckCleanup();
			Debug.Assert(!dict.ContainsKey(value));
			dict.Add(new HashableWeakReference(key), value);
		}

		/// <summary>指定したキーの要素が <see cref="WeakDictionary&lt;TKey, TValue&gt;"/> に格納されているかどうかを確認します。</summary>
		/// <param name="key"><see cref="WeakDictionary&lt;TKey, TValue&gt;"/> 内で検索されるキー。</param>
		/// <returns>指定したキーを持つ要素を <see cref="WeakDictionary&lt;TKey, TValue&gt;"/> が保持している場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		[Confined]
		public bool ContainsKey(TKey key) { return dict.ContainsKey(key); }

		/// <summary><see cref="WeakDictionary&lt;TKey, TValue&gt;"/> のキーを保持している <see cref="System.Collections.Generic.ICollection&lt;T&gt;"/> を取得します。</summary>
		public ICollection<TKey> Keys { get { return this.Select(x => x.Key).ToArray(); } }

		/// <summary>指定したキーを持つ要素を <see cref="WeakDictionary&lt;TKey, TValue&gt;"/> から削除します。</summary>
		/// <param name="key">削除する要素のキー。</param>
		/// <returns>要素が正常に削除された場合は <c>true</c>。それ以外の場合は <c>false</c>。このメソッドは、<paramref name="key"/> が元の <see cref="WeakDictionary&lt;TKey, TValue&gt;"/> に見つからなかった場合にも <c>false</c> を返します。</returns>
		public bool Remove(TKey key) { return dict.Remove(key); }

		/// <summary>指定したキーに関連付けられている値を取得します。</summary>
		/// <param name="key">値を取得する対象のキー。</param>
		/// <param name="value">
		/// このメソッドが返されるときに、キーが見つかった場合は、指定したキーに関連付けられている値。それ以外の場合は <paramref name="value"/> パラメーターの型に対する既定の値。
		/// このパラメーターは初期化せずに渡されます。
		/// </param>
		/// <returns>指定したキーを持つ要素が <see cref="WeakDictionary&lt;TKey, TValue&gt;"/> を実装するオブジェクトに格納されている場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool TryGetValue(TKey key, out TValue value) { return dict.TryGetValue(key, out value); }

		/// <summary><see cref="WeakDictionary&lt;TKey, TValue&gt;"/> 内の値を格納している <see cref="System.Collections.Generic.ICollection&lt;T&gt;"/> を取得します。</summary>
		public ICollection<TValue> Values { get { return this.Select(x => x.Value).ToArray(); } }

		/// <summary>指定したキーを持つ要素を取得または設定します。</summary>
		/// <param name="key">取得または設定する要素のキー。</param>
		/// <returns>指定したキーを持つ要素。</returns>
		/// <exception cref="KeyNotFoundException">プロパティは取得されますが、<paramref name="key"/> が見つかりません。</exception>
		public TValue this[TKey key]
		{
			get { return dict[key]; }
			set
			{
				// If the WeakHash already holds this value as a key, it will lead to a circular-reference and result in the objects being kept alive forever.
				// The caller needs to ensure that this cannot happen.
				Debug.Assert(!dict.ContainsKey(value));
				dict[new HashableWeakReference(key)] = value;
			}
		}

		/// <summary>
		/// Check if any of the keys have gotten collected
		/// 
		/// Currently, there is also no guarantee of how long the values will be kept alive even after the keys
		/// get collected. This could be fixed by triggerring CheckCleanup() to be called on every garbage-collection
		/// by having a dummy watch-dog object with a finalizer which calls CheckCleanup().
		/// </summary>
		void CheckCleanup()
		{
			version++;
			long change = version - cleanupVersion;
			// Cleanup the table if it is a while since we have done it last time.
			// Take the size of the table into account.
			if (change > 1234 + dict.Count / 2)
			{
				// It makes sense to do the cleanup only if a GC has happened in the meantime.
				// WeakReferences can become zero only during the GC.
				bool garbage_collected;
				var currentGC = GC.CollectionCount(0);
				garbage_collected = currentGC != cleanupGC;
				if (garbage_collected)
				{
					cleanupGC = currentGC;
					Cleanup();
					cleanupVersion = version;
				}
				else
					cleanupVersion += 1234;
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2004:RemoveCallsToGCKeepAlive")]
		void Cleanup()
		{
			int liveCount = 0;
			int emptyCount = 0;
			foreach (HashableWeakReference w in dict.Keys)
			{
				TKey target;
				if (w.TryGetTarget(out target))
					liveCount++;
				else
					emptyCount++;
			}
			// Rehash the table if there is a significant number of empty slots
			if (emptyCount > liveCount / 4)
			{
				Dictionary<object, TValue> newtable = new Dictionary<object, TValue>(liveCount + liveCount / 4, comparer);
				foreach (var kvp in dict)
				{
					TKey target;
					if (((HashableWeakReference)kvp.Key).TryGetTarget(out target))
					{
						newtable[kvp.Key] = kvp.Value;
						GC.KeepAlive(target);
					}
				}
				dict = newtable;
			}
		}

		void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) { Add(item.Key, item.Value); }

		/// <summary><see cref="WeakDictionary&lt;TKey, TValue&gt;"/> からすべての項目を削除します。</summary>
		public void Clear() { dict.Clear(); }

		[Confined]
		bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
		{
			TValue value;
			return TryGetValue(item.Key, out value) && EqualityComparer<TValue>.Default.Equals(value, item.Value);
		}

		void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			foreach (var kvp in this)
				array[arrayIndex++] = kvp;
		}

		/// <summary><see cref="WeakDictionary&lt;TKey, TValue&gt;"/> に格納されている要素の数を取得します。</summary>
		public int Count
		{
			get
			{
				int count = 0;
				foreach (var kvp in this)
					count++;
				return count;
			}
		}

		/// <summary><see cref="WeakDictionary&lt;TKey, TValue&gt;"/> が読み取り専用かどうかを示す値を取得します。</summary>
		public bool IsReadOnly { get { return false; } }

		bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
		{
			TValue value;
			if (TryGetValue(item.Key, out value) && EqualityComparer<TValue>.Default.Equals(value, item.Value))
				return Remove(item.Key);
			return false;
		}

		/// <summary>コレクションを反復処理する列挙子を返します。</summary>
		/// <returns>コレクションを反復処理するために使用できる <see cref="System.Collections.Generic.IEnumerator&lt;T&gt;"/>。</returns>
		[Pure]
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			foreach (var kvp in dict)
			{
				TKey realKey;
				if (((HashableWeakReference)kvp.Key).TryGetTarget(out realKey))
					yield return new KeyValuePair<TKey, TValue>(realKey, kvp.Value);
			}
		}

		[Pure]
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}

	/// <summary>通常の参照と弱い参照の両方でオブジェクトと ID をマッピングする方法を提供します。</summary>
	/// <typeparam name="T">マッピング対象のオブジェクトの型を指定します。</typeparam>
	public sealed class HybridMapping<T> where T : class
	{
		Dictionary<int, object> _dict = new Dictionary<int, object>();
		readonly object _synchObject = new object();
		readonly int _minimum;
		int _current;

		const int SIZE = 4096;
		const int MIN_RANGE = SIZE / 2;

		/// <summary><see cref="Microsoft.Scripting.Utils.HybridMapping&lt;T&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		public HybridMapping() : this(0) { }

		/// <summary>ID の最小値を使用して、<see cref="Microsoft.Scripting.Utils.HybridMapping&lt;T&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="minimum">割り当てられる ID の最小値を指定します。</param>
		public HybridMapping(int minimum)
		{
			if (minimum < 0 || (SIZE - minimum) < MIN_RANGE)
				throw new ArgumentOutOfRangeException("offset", "invalid offset value");
			_minimum = minimum;
			_current = minimum;
		}

		int Add(object value)
		{
			lock (_synchObject)
			{
				var saved = _current;
				while (_dict.ContainsKey(_current))
				{
					if (++_current >= SIZE)
						_current = _minimum;
					if (_current == saved)
						throw new InvalidOperationException("HybridMapping is full");
				}
				_dict.Add(_current, value);
				return _current;
			}
		}

		static T GetActualValue(object value)
		{
			Debug.Assert(value is T || value is WeakReference<T>);
			T target;
			var wref = value as WeakReference<T>;
			return wref != null ? (wref.TryGetTarget(out target) ? target : null) : (T)value;
		}

		/// <summary>指定されたオブジェクトの弱参照をマッピングに追加して、このマッピングのオブジェクトに対する ID を返します。</summary>
		/// <param name="value">マッピングするオブジェクトを指定します。</param>
		/// <returns><paramref name="value"/> に対する ID。</returns>
		public int WeakAdd(T value) { return Add(new WeakReference<T>(value)); }

		/// <summary>指定されたオブジェクトをマッピングに追加して、このマッピングのオブジェクトに対する ID を返します。</summary>
		/// <param name="value">マッピングするオブジェクトを指定します。</param>
		/// <returns><paramref name="value"/> に対する ID。</returns>
		public int StrongAdd(T value) { return Add(value); }

		/// <summary>指定された ID に対応するオブジェクトを取得します。</summary>
		/// <param name="id">対応するオブジェクトを取得する ID を指定します。</param>
		/// <returns><paramref name="id"/> に対応するオブジェクトが存在する場合はそのオブジェクト。それ以外の場合は <c>null</c>。</returns>
		public T GetObjectForId(int id)
		{
			object ret;
			return _dict.TryGetValue(id, out ret) ? GetActualValue(ret) : null;
		}

		/// <summary>指定されたオブジェクトに対応する ID を取得します。</summary>
		/// <param name="value">対応する ID を取得するオブジェクトを指定します。</param>
		/// <returns><paramref name="value"/> がこのマッピングに存在する場合はその ID。それ以外の場合は -1。</returns>
		public int GetIdForObject(T value)
		{
			lock (_synchObject)
			{
				var result = _dict.Select(x => (KeyValuePair<int, object>?)x).FirstOrDefault(x => EqualityComparer<T>.Default.Equals(GetActualValue(x.Value.Value), value));
				if (result != null)
					return result.Value.Key;
			}
			return -1;
		}

		/// <summary>指定された ID に対応するオブジェクトをこのマッピングから削除します。</summary>
		/// <param name="id">削除するオブジェクトの ID を指定します。</param>
		public void RemoveById(int id)
		{
			lock (_synchObject)
				_dict.Remove(id);
		}

		/// <summary>指定されたオブジェクトをこのマッピングから削除します。</summary>
		/// <param name="value">削除するオブジェクトを指定します。</param>
		public void Remove(T value) { RemoveById(GetIdForObject(value)); }
	}
}
