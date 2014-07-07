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

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Scripting.Utils
{
	/// <summary>
	/// 構築時に指定した最大容量を保持するキャッシュに使用されるディクショナリに似たオブジェクトを提供します。
	/// このクラスはスレッドセーフではありません。
	/// </summary>
	public class CacheDict<TKey, TValue>
	{
		readonly Dictionary<TKey, ValueInfo> _dict = new Dictionary<TKey, ValueInfo>();
		readonly LinkedList<TKey> _list = new LinkedList<TKey>();
		readonly int _capacity;

		/// <summary>最大容量を指定して、<see cref="Microsoft.Scripting.Utils.CacheDict&lt;TKey, TValue&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="capacity">格納する要素の最大量を指定します。</param>
		public CacheDict(int capacity) { _capacity = capacity; }

		/// <summary>指定されたキーに関連付けられた値の取得を試みます。</summary>
		/// <param name="key">関連付けられた値を取得するキーを指定します。</param>
		/// <param name="value">キーに関連付けられた値が返されます。</param>
		/// <returns>キーに対応する値が存在する場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool TryGetValue(TKey key, out TValue value)
		{
			ValueInfo storedValue;
			if (_dict.TryGetValue(key, out storedValue))
			{
				if (storedValue.Node.Previous != null)
				{
					// 値の情報を先頭に移動
					_list.Remove(storedValue.Node);
					_list.AddFirst(storedValue.Node);
				}
				value = storedValue.Value;
				return true;
			}
			value = default(TValue);
			return false;
		}

		/// <summary>指定されたキーおよび値を追加します。すでにキーが存在していた場合は元の値を置き換えます。</summary>
		/// <param name="key">追加するキーを指定します。</param>
		/// <param name="value">キーに関連付けられた値を指定します。</param>
		public void Add(TKey key, TValue value)
		{
			ValueInfo valueInfo;
			if (_dict.TryGetValue(key, out valueInfo))
				_list.Remove(valueInfo.Node); // リンクリストから元の項目を削除
			else if (_list.Count == _capacity)
			{
				// 容量に達したので、リンクリストの最後の要素を削除
				var node = _list.Last;
				_list.RemoveLast();
				var successful = _dict.Remove(node.Value);
				Debug.Assert(successful);
			}
			// 新しい項目をリストの最初とディクショナリに追加
			var listNode = new LinkedListNode<TKey>(key);
			_list.AddFirst(listNode);
			_dict[key] = new ValueInfo(value, listNode);
		}

		/// <summary>指定されたキーに関連付けられた値を取得または設定します。</summary>
		/// <param name="key">値に対応するキーを指定します。</param>
		/// <returns>キーに関連付けられた値。</returns>
		public TValue this[TKey key]
		{
			get
			{
				TValue res;
				if (TryGetValue(key, out res))
					return res;
				throw new KeyNotFoundException(key.ToString());
			}
			set { Add(key, value); }
		}

		struct ValueInfo
		{
			internal readonly TValue Value;
			internal readonly LinkedListNode<TKey> Node;

			internal ValueInfo(TValue value, LinkedListNode<TKey> node)
			{
				Value = value;
				Node = node;
			}
		}
	}
}
