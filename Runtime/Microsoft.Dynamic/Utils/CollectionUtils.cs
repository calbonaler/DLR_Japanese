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
using System.Linq;

namespace Microsoft.Scripting.Utils
{
	/// <summary>コレクションに関する拡張メソッドを提供します。</summary>
	public static class CollectionUtils
	{
		/// <summary>
		/// シーケンスを <see cref="ReadOnlyCollection&lt;T&gt;"/> でラップします。
		/// すべてのデータは新しい配列にコピーされるため、作成された後の <see cref="ReadOnlyCollection&lt;T&gt;"/> は変更されません。
		/// ただし、<paramref name="enumerable"/> がすでに <see cref="ReadOnlyCollection&lt;T&gt;"/> であった場合には元のコレクションが返されます。
		/// </summary>
		/// <param name="enumerable">読み取り専用のコレクションに変換するシーケンスを指定します。</param>
		/// <returns>変更されない読み取り専用のコレクション。</returns>
		internal static ReadOnlyCollection<T> ToReadOnly<T>(this IEnumerable<T> enumerable)
		{
			var roCollection = enumerable as ReadOnlyCollection<T>;
			if (roCollection != null)
				return roCollection;
			T[] array;
			if (enumerable != null && (array = enumerable.ToArray()).Length > 0)
				return new ReadOnlyCollection<T>(array);
			return EmptyReadOnlyCollection<T>.Instance;
		}

		/// <summary>リストから指定された述語に一致する要素のインデックスを返します。</summary>
		/// <typeparam name="T">リストの要素型を指定します。</typeparam>
		/// <param name="collection">述語に一致する要素を検索するリストを指定します。</param>
		/// <param name="predicate">リストから要素を検索するための述語を指定します。</param>
		/// <returns>リストで述語に一致する要素が見つかった場合はそのインデックス。それ以外の場合は <c>-1</c> が返されます。</returns>
		public static int FindIndex<T>(this IList<T> collection, Predicate<T> predicate)
		{
			ContractUtils.RequiresNotNull(collection, "collection");
			ContractUtils.RequiresNotNull(predicate, "predicate");
			for (int i = 0; i < collection.Count; i++)
			{
				if (predicate(collection[i]))
					return i;
			}
			return -1;
		}

		/// <summary>インデックスによってアクセス可能なリストの末尾にある 2 個の要素を交換します。</summary>
		/// <typeparam name="T">要素を交換するリストの要素型を指定します。</typeparam>
		/// <param name="list">要素を交換するリストを指定します。</param>
		public static void SwapLastTwo<T>(this IList<T> list)
		{
			ContractUtils.RequiresNotNull(list, "list");
			ContractUtils.Requires(list.Count >= 2, "list");
			var temp = list[list.Count - 1];
			list[list.Count - 1] = list[list.Count - 2];
			list[list.Count - 2] = temp;
		}

		/// <summary>指定されたコレクションのハッシュ値を計算します。</summary>
		/// <typeparam name="T">ハッシュ値を計算するコレクションの要素型を指定します。</typeparam>
		/// <param name="items">ハッシュ値を計算するコレクションを指定します。</param>
		/// <returns>コレクションのハッシュ値。</returns>
		public static int GetValueHashCode<T>(this ICollection<T> items) { return GetValueHashCode<T>(items, 0, items.Count); }

		/// <summary>指定されたコレクションの指定された範囲のハッシュ値を計算します。</summary>
		/// <typeparam name="T">ハッシュ値を計算するコレクションの要素型を指定します。</typeparam>
		/// <param name="items">ハッシュ値を計算するコレクションを指定します。</param>
		/// <param name="start">ハッシュ値の計算を開始するコレクション内の位置を指定します。</param>
		/// <param name="count">ハッシュ値を計算するコレクション内の要素数を指定します。</param>
		/// <returns>コレクションの指定された範囲のハッシュ値。</returns>
		public static int GetValueHashCode<T>(this ICollection<T> items, int start, int count)
		{
			ContractUtils.RequiresNotNull(items, "items");
			ContractUtils.RequiresArrayRange(items.Count, start, count, "start", "count");
			if (count == 0)
				return 0;
			var en = items.Skip(start).Take(count);
			return en.Aggregate(en.First().GetHashCode(), (x, y) => ((x << 5) | (x >> 27)) ^ y.GetHashCode());
		}
	}

	static class EmptyReadOnlyCollection<T>
	{
		internal static ReadOnlyCollection<T> Instance = new ReadOnlyCollection<T>(new T[0]);
	}
}
