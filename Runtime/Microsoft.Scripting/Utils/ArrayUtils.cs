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
using System.Linq;

namespace Microsoft.Scripting.Utils
{
	/// <summary>配列に関するユーティリティメソッドを提供します。</summary>
	public static class ArrayUtils
	{
		/// <summary><see cref="System.String"/> 型の空の配列を表します。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
		public static readonly string[] EmptyStrings = new string[0];

		/// <summary><see cref="System.Object"/> 型の空の配列を表します。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
		public static readonly object[] EmptyObjects = new object[0];

		/// <summary>指定された配列の簡易コピーを作成します。</summary>
		/// <typeparam name="T">簡易コピーを作成する配列の要素型を指定します。</typeparam>
		/// <param name="array">簡易コピーを作成する配列を指定します。</param>
		/// <returns>配列の簡易コピー。</returns>
		public static T[] Copy<T>(T[] array)
		{
			ContractUtils.RequiresNotNull(array, "array");
			return array.Length > 0 ? (T[])array.Clone() : array;
		}

		/// <summary>指定されたシーケンスを配列に変換します。シーケンスがすでに配列である場合は元のシーケンスを返します。</summary>
		/// <typeparam name="T">配列を取得するシーケンスの要素型を指定します。</typeparam>
		/// <param name="items">配列を取得するシーケンスを指定します。</param>
		/// <returns>シーケンスに対応する配列。</returns>
		public static T[] ToArray<T>(IEnumerable<T> items) { return items == null ? new T[0] : items as T[] ?? items.ToArray(); }

		/// <summary>指定された配列を指定された数左へシフトします。0 未満のインデックスになった要素は削除され、長さは切り詰められます。</summary>
		/// <typeparam name="T">左シフトする配列の要素型を指定します。</typeparam>
		/// <param name="array">左シフトする配列を指定します。</param>
		/// <param name="count">配列のシフト量を指定します。</param>
		/// <returns>左シフトされた配列。</returns>
		public static T[] ShiftLeft<T>(T[] array, int count)
		{
			ContractUtils.RequiresNotNull(array, "array");
			if (count < 0)
				throw new ArgumentOutOfRangeException("count");
			var result = new T[array.Length - count];
			Array.Copy(array, count, result, 0, result.Length);
			return result;
		}

		/// <summary>指定された要素をコレクションの先頭に追加した配列を返します。</summary>
		/// <typeparam name="T">追加される要素の型を指定します。</typeparam>
		/// <param name="item">追加する要素を指定します。</param>
		/// <param name="items">要素を追加する元のコレクションを指定します。</param>
		/// <returns>コレクションの先頭に要素が追加された配列。</returns>
		public static T[] Insert<T>(T item, ICollection<T> items)
		{
			ContractUtils.RequiresNotNull(items, "items");
			var res = new T[items.Count + 1];
			res[0] = item;
			items.CopyTo(res, 1);
			return res;
		}

		/// <summary>指定された 2 個の要素をコレクションの先頭に追加した配列を返します。</summary>
		/// <typeparam name="T">追加される要素の型を指定します。</typeparam>
		/// <param name="item1">追加する 1 番目の要素を指定します。</param>
		/// <param name="item2">追加する 2 番目の要素を指定します。</param>
		/// <param name="items">要素を追加する元のコレクションを指定します。</param>
		/// <returns>コレクションの先頭に 2 個の要素が追加された配列。</returns>
		public static T[] Insert<T>(T item1, T item2, ICollection<T> items)
		{
			ContractUtils.RequiresNotNull(items, "items");
			var res = new T[items.Count + 2];
			res[0] = item1;
			res[1] = item2;
			items.CopyTo(res, 2);
			return res;
		}

		/// <summary>指定された任意個の要素をコレクションの末尾に追加した配列を返します。</summary>
		/// <typeparam name="T">追加される要素の型を指定します。</typeparam>
		/// <param name="items">要素を追加する元のコレクションを指定します。</param>
		/// <param name="added">追加する要素を指定します。</param>
		/// <returns>コレクションの末尾に要素が追加された配列。</returns>
		public static T[] Append<T>(ICollection<T> items, params T[] added)
		{
			ContractUtils.RequiresNotNull(items, "items1");
			ContractUtils.RequiresNotNull(added, "items2");
			var result = new T[items.Count + added.Length];
			items.CopyTo(result, 0);
			added.CopyTo(result, items.Count);
			return result;
		}

		/// <summary>指定された配列の最初の要素を削除した配列を返します。</summary>
		/// <typeparam name="T">配列の要素型を指定します。</typeparam>
		/// <param name="array">最初の要素を削除する配列を指定します。</param>
		/// <returns>最初の要素が削除された配列。</returns>
		public static T[] RemoveFirst<T>(T[] array) { return RemoveAt(array, 0); }

		/// <summary>指定された配列の最後の要素を削除した配列を返します。</summary>
		/// <typeparam name="T">配列の要素型を指定します。</typeparam>
		/// <param name="array">最後の要素を削除する配列を指定します。</param>
		/// <returns>最後の要素が削除された配列。</returns>
		public static T[] RemoveLast<T>(T[] array) { return RemoveAt(array, array.Length - 1); }

		/// <summary>指定された配列の指定されたインデックスにある要素を削除した配列を返します。</summary>
		/// <typeparam name="T">配列の要素型を指定します。</typeparam>
		/// <param name="array">要素が削除される配列を指定します。</param>
		/// <param name="indexToRemove">削除する要素の位置を示す 0 から始まるインデックスを指定します。</param>
		/// <returns>指定されたインデックスの要素が削除された配列。</returns>
		public static T[] RemoveAt<T>(T[] array, int indexToRemove)
		{
			ContractUtils.RequiresNotNull(array, "array");
			ContractUtils.RequiresArrayIndex(array, indexToRemove, "indexToRemove");
			var result = new T[array.Length - 1];
			if (indexToRemove > 0)
				Array.Copy(array, 0, result, 0, indexToRemove);
			var remaining = array.Length - indexToRemove - 1;
			if (remaining > 0)
				Array.Copy(array, array.Length - remaining, result, result.Length - remaining, remaining);
			return result;
		}

		/// <summary>指定された配列の指定されたインデックスに指定された任意個の要素を挿入した配列を返します。</summary>
		/// <typeparam name="T">配列の要素型を指定します。</typeparam>
		/// <param name="array">要素が挿入される配列を指定します。</param>
		/// <param name="index">要素の挿入の開始位置を示す 0 から始まるインデックスを指定します。</param>
		/// <param name="items">挿入する要素を指定します。</param>
		/// <returns>指定されたインデックスに指定された要素が挿入された配列。</returns>
		public static T[] InsertAt<T>(T[] array, int index, params T[] items)
		{
			ContractUtils.RequiresNotNull(array, "array");
			ContractUtils.RequiresNotNull(items, "items");
			ContractUtils.RequiresArrayInsertIndex(array, index, "index");
			if (items.Length == 0)
				return Copy(array);
			var result = new T[array.Length + items.Length];
			if (index > 0)
				Array.Copy(array, 0, result, 0, index);
			items.CopyTo(result, index);
			var remaining = array.Length - index;
			if (remaining > 0)
				Array.Copy(array, array.Length - remaining, result, result.Length - remaining, remaining);
			return result;
		}
	}
}
