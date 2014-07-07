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

namespace Microsoft.Scripting.Utils
{
	/// <summary>契約を実行し、違反した場合には例外を送出するメソッドを提供します。</summary>
	public static class ContractUtils
	{
		/// <summary>指定された条件を引数に要求します。</summary>
		/// <param name="precondition">条件を指定します。</param>
		/// <exception cref="ArgumentException">条件が不成立です。</exception>
		public static void Requires(bool precondition) { Requires(precondition, null, Strings.MethodPreconditionViolated); }

		/// <summary>指定された条件を引数に要求します。</summary>
		/// <param name="precondition">条件を指定します。</param>
		/// <param name="paramName">引数の名前を指定します。</param>
		/// <exception cref="ArgumentException">条件が不成立です。</exception>
		public static void Requires(bool precondition, string paramName) { Requires(precondition, paramName, Strings.InvalidArgumentValue); }

		/// <summary>指定された条件を引数に要求します。</summary>
		/// <param name="precondition">条件を指定します。</param>
		/// <param name="paramName">引数の名前を指定します。</param>
		/// <param name="message">条件が不成立のとき送出される例外のメッセージを指定します。</param>
		/// <exception cref="ArgumentException">条件が不成立です。</exception>
		public static void Requires(bool precondition, string paramName, string message)
		{
			if (!precondition)
			{
				if (paramName != null)
					throw new ArgumentException(message, paramName);
				else
					throw new ArgumentException(message);
			}
		}

		/// <summary>指定された引数が <c>null</c> でないことを要求します。</summary>
		/// <param name="value"><c>null</c> でないことを要求する参照型変数を指定します。</param>
		/// <param name="paramName">引数の名前を指定します。</param>
		/// <exception cref="ArgumentNullException">引数が <c>null</c> です。</exception>
		public static void RequiresNotNull(object value, string paramName)
		{
			Assert.NotEmpty(paramName);
			if (value == null)
				throw new ArgumentNullException(paramName);
		}

		/// <summary>指定された <see cref="System.String"/> 型の引数が空文字でないことを要求します。</summary>
		/// <param name="str">空でないことを要求する <see cref="System.String"/> 型の引数を指定します。</param>
		/// <param name="paramName">引数の名前を指定します。</param>
		/// <exception cref="ArgumentNullException">引数は <c>null</c> です。</exception>
		/// <exception cref="ArgumentException">引数は空文字です。</exception>
		public static void RequiresNotEmpty(string str, string paramName)
		{
			RequiresNotNull(str, paramName);
			if (str.Length <= 0)
				throw new ArgumentException(Strings.NonEmptyStringRequired, paramName);
		}

		/// <summary>指定されたシーケンスの引数が空でないことを要求します。</summary>
		/// <typeparam name="T">シーケンスの型を指定します。</typeparam>
		/// <param name="collection">空でないことを要求するシーケンスを指定します。</param>
		/// <param name="paramName">引数の名前を指定します。</param>
		/// <exception cref="ArgumentNullException">引数は <c>null</c> です。</exception>
		/// <exception cref="ArgumentException">引数は空のシーケンスを表しています。</exception>
		public static void RequiresNotEmpty<T>(IEnumerable<T> collection, string paramName)
		{
			RequiresNotNull(collection, paramName);
			if (!collection.Any())
				throw new ArgumentException(Strings.NonEmptyCollectionRequired, paramName);
		}

		/// <summary>指定された列挙可能なコレクションに <c>null</c> である要素が含まれていないことを要求します。</summary>
		/// <param name="collection"><c>null</c> 要素が含まれていないことを要求するコレクションを指定します。</param>
		/// <param name="collectionName">コレクションの名前を指定します。</param>
		/// <exception cref="ArgumentNullException">コレクションは <c>null</c> です。</exception>
		/// <exception cref="ArgumentException">コレクションに <c>null</c> 要素が含まれています。</exception>
		public static void RequiresNotNullItems<T>(IEnumerable<T> collection, string collectionName)
		{
			Assert.NotNull(collectionName);
			RequiresNotNull(collection, collectionName);
			int i = 0;
			foreach (var item in collection)
			{
				if (item == null)
					throw ExceptionUtils.MakeArgumentItemNullException(i, collectionName);
				i++;
			}
		}

		/// <summary>指定されたインデックスがコレクション内の位置を示していることを要求します。</summary>
		/// <typeparam name="T">コレクションの要素型を指定します。</typeparam>
		/// <param name="collection">インデックスが位置を示していることを要求するコレクションを指定します。</param>
		/// <param name="index">コレクション内の位置を示していることを要求するインデックスを指定します。</param>
		/// <param name="indexName">インデックスの名前を指定します。</param>
		/// <exception cref="ArgumentOutOfRangeException">インデックスはコレクション外の場所を示しています。</exception>
		public static void RequiresArrayIndex<T>(ICollection<T> collection, int index, string indexName) { RequiresArrayIndex(collection.Count, index, indexName); }

		/// <summary>指定されたインデックスがコレクション内の位置を示していることを要求します。</summary>
		/// <param name="length">インデックスが位置を示していることを要求するコレクションの長さを指定します。</param>
		/// <param name="index">コレクション内の位置を示していることを要求するインデックスを指定します。</param>
		/// <param name="indexName">インデックスの名前を指定します。</param>
		/// <exception cref="ArgumentOutOfRangeException">インデックスはコレクション外の場所を示しています。</exception>
		public static void RequiresArrayIndex(int length, int index, string indexName)
		{
			Assert.NotEmpty(indexName);
			Debug.Assert(length >= 0);
			if (index < 0 || index >= length)
				throw new ArgumentOutOfRangeException(indexName);
		}

		/// <summary>指定されたインデックスがコレクション内の位置または末尾を示していることを要求します。</summary>
		/// <typeparam name="T">コレクションの要素型を指定します。</typeparam>
		/// <param name="collection">インデックスが位置を示していることを要求するコレクションを指定します。</param>
		/// <param name="index">コレクション内の位置または末尾を示していることを要求するインデックスを指定します。</param>
		/// <param name="indexName">インデックスの名前を指定します。</param>
		/// <exception cref="ArgumentOutOfRangeException">インデックスはコレクション外の場所を示しています。</exception>
		public static void RequiresArrayInsertIndex<T>(ICollection<T> collection, int index, string indexName) { RequiresArrayInsertIndex(collection.Count, index, indexName); }

		/// <summary>指定されたインデックスがコレクション内の位置または末尾を示していることを要求します。</summary>
		/// <param name="length">インデックスが位置を示していることを要求するコレクションの長さを指定します。</param>
		/// <param name="index">コレクション内の位置または末尾を示していることを要求するインデックスを指定します。</param>
		/// <param name="indexName">インデックスの名前を指定します。</param>
		/// <exception cref="ArgumentOutOfRangeException">インデックスはコレクション外の場所を示しています。</exception>
		public static void RequiresArrayInsertIndex(int length, int index, string indexName)
		{
			Assert.NotEmpty(indexName);
			Debug.Assert(length >= 0);
			if (index < 0 || index > length)
				throw new ArgumentOutOfRangeException(indexName);
		}

		/// <summary>指定された範囲がコレクション内にあることを要求します。</summary>
		/// <typeparam name="T">コレクションの要素型を指定します。</typeparam>
		/// <param name="collection">範囲が存在することを要求するコレクションを指定します。</param>
		/// <param name="offset">範囲の開始位置を示すインデックスを指定します。</param>
		/// <param name="count">範囲の長さを指定します。</param>
		/// <param name="offsetName">範囲の開始位置を表す引数の名前を指定します。</param>
		/// <param name="countName">範囲の長さを表す引数の名前を指定します。</param>
		/// <exception cref="ArgumentOutOfRangeException">指定された範囲がコレクション内にありません。</exception>
		public static void RequiresArrayRange<T>(ICollection<T> collection, int offset, int count, string offsetName, string countName) { RequiresArrayRange(collection.Count, offset, count, offsetName, countName); }

		/// <summary>指定された範囲がコレクション内にあることを要求します。</summary>
		/// <param name="length">範囲が存在することを要求するコレクションの長さを指定します。</param>
		/// <param name="offset">範囲の開始位置を示すインデックスを指定します。</param>
		/// <param name="count">範囲の長さを指定します。</param>
		/// <param name="offsetName">範囲の開始位置を表す引数の名前を指定します。</param>
		/// <param name="countName">範囲の長さを表す引数の名前を指定します。</param>
		/// <exception cref="ArgumentOutOfRangeException">指定された範囲がコレクション内にありません。</exception>
		public static void RequiresArrayRange(int length, int offset, int count, string offsetName, string countName)
		{
			Assert.NotEmpty(offsetName);
			Assert.NotEmpty(countName);
			Debug.Assert(length >= 0);
			if (count < 0)
				throw new ArgumentOutOfRangeException(countName);
			if (offset < 0 || length - offset < count)
				throw new ArgumentOutOfRangeException(offsetName);
		}

		/// <summary>不変条件を指定します。</summary>
		/// <param name="condition">不変条件となる条件を指定します。</param>
		[Conditional("FALSE")]
		public static void Invariant(bool condition) { Debug.Assert(condition); }

		/// <summary>メソッドの事後条件を指定します。</summary>
		/// <param name="condition">事後条件を指定します。</param>
		[Conditional("FALSE")]
		public static void Ensures(bool condition) { }

		/// <summary>メソッドの結果を表します。</summary>
		/// <typeparam name="T">結果型を指定します。</typeparam>
		/// <returns>メソッドの結果。</returns>
		public static T Result<T>() { return default(T); }
	}
}
