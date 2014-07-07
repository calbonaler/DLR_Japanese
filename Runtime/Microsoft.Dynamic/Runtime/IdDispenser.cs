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
using Microsoft.Contracts;
using SRC = System.Runtime.CompilerServices;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>オブジェクトに対する一意識別子の割り当てを行います。</summary>
	public static class IdDispenser
	{
		// 比較子の唯一のインスタンス
		static readonly IEqualityComparer<object> _comparer = new WrapperComparer();
		[MultiRuntimeAware]
		static Dictionary<object, Wrapper> _hashtable = new Dictionary<object, Wrapper>(_comparer);
		static readonly object _synchObject = new object();  // 全域ロックに対する唯一のインスタンス
		// 一意識別子に long を使用することで重複を心配する必要はありません。
		// 2005 年現在のハードウェアではオーバーフローするのに 100 年以上かかります。
		[MultiRuntimeAware]
		static long _currentId = 42; // 最近適用した一意識別子
		// _cleanupId および _cleanupGC はハッシュテーブルクリーンアップの効率的なスケジューリングに使用されます。
		[MultiRuntimeAware]
		static long _cleanupId; // 最近のクリーンアップ時の _currentId
		[MultiRuntimeAware]
		static int _cleanupGC; // 最近のクリーンアップ時の GC.CollectionCount(0)

		/// <summary>指定された一意識別子に関連付けられたオブジェクトを取得します。</summary>
		/// <param name="id">関連付けられたオブジェクトを取得する一意識別子を指定します。</param>
		/// <returns>一意識別子に関連付けられたオブジェクト。関連付けられたオブジェクトが存在しない場合は <c>null</c> を返します。</returns>
		public static object GetObject(long id)
		{
			lock (_synchObject)
			{
				foreach (Wrapper w in _hashtable.Keys)
				{
					if (w.Target != null && w.Id == id)
						return w.Target;
				}
				return null;
			}
		}

		/// <summary>指定されたオブジェクトに対する一意識別子を取得します。</summary>
		/// <param name="o">一意識別子を取得するオブジェクトを指定します。</param>
		/// <returns>オブジェクトに関連付けられた一意識別子。</returns>
		public static long GetId(object o)
		{
			if (o == null)
				return 0;
			lock (_synchObject)
			{
				// オブジェクトが存在している場合は、既存の識別子を返す
				Wrapper res;
				if (_hashtable.TryGetValue(o, out res))
					return res.Id;
				var uniqueId = checked(++_currentId);
				var change = uniqueId - _cleanupId;
				// 最後のクリーンアップから長時間経った場合は、テーブルをクリーンアップ
				// テーブルのサイズを計算に入れる
				if (change > 1234 + _hashtable.Count / 2)
				{
					// GC がその間に発生している限りクリーンアップを行うのは意味がある
					// 弱参照は GC の間は 0 になる
					var currentGC = GC.CollectionCount(0);
					if (currentGC != _cleanupGC)
					{
						Cleanup();
						_cleanupId = uniqueId;
						_cleanupGC = currentGC;
					}
					else
						_cleanupId += 1234;
				}
				var w = new Wrapper(o, uniqueId);
				_hashtable[w] = w;
				return uniqueId;
			}
		}

		/// <summary>ハッシュテーブルを走査して、空の要素を削除します。</summary>
		static void Cleanup()
		{
			int liveCount = 0;
			int emptyCount = 0;
			foreach (Wrapper w in _hashtable.Keys)
			{
				if (w.Target != null)
					liveCount++;
				else
					emptyCount++;
			}
			// 空のスロットが相当出てきた場合は、テーブルを再ハッシュ
			if (emptyCount > liveCount / 4)
			{
				Dictionary<object, Wrapper> newtable = new Dictionary<object, Wrapper>(liveCount + liveCount / 4, _comparer);
				foreach (Wrapper w in _hashtable.Keys)
				{
					if (w.Target != null)
						newtable[w] = w;
				}
				_hashtable = newtable;
			}
		}

		/// <summary>オブジェクトへの弱参照、ハッシュ値、オブジェクト ID をキャッシュする弱参照ラッパーを表します。</summary>
		sealed class Wrapper
		{
			WeakReference _weakReference;
			int _hashCode;

			public Wrapper(object obj, long uniqueId)
			{
				_weakReference = new WeakReference(obj, true);
				_hashCode = obj == null ? 0 : SRC.RuntimeHelpers.GetHashCode(obj);
				Id = uniqueId;
			}

			public long Id { get; private set; }

			public object Target { get { return _weakReference.Target; } }

			[Confined]
			public override int GetHashCode() { return _hashCode; }
		}

		/// <summary><see cref="Wrapper"/> を透過エンベロープとして扱う等値比較子を表します。</summary>
		sealed class WrapperComparer : IEqualityComparer<object>
		{
			bool IEqualityComparer<object>.Equals(object x, object y)
			{
				var wx = x as Wrapper;
				if (wx != null)
					x = wx.Target;
				var wy = y as Wrapper;
				if (wy != null)
					y = wy.Target;
				return ReferenceEquals(x, y);
			}

			int IEqualityComparer<object>.GetHashCode(object obj)
			{
				Wrapper wobj = obj as Wrapper;
				if (wobj != null)
					return wobj.GetHashCode();
				return obj == null ? 0 : SRC.RuntimeHelpers.GetHashCode(obj);
			}
		}
	}
}
