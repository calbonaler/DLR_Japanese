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
using System.Linq;
using Microsoft.Contracts;

namespace Microsoft.Scripting.Utils
{
	/// <summary>配列に値の等価性による等値比較のサポートを追加します。</summary>
	/// <typeparam name="T">配列の要素型を指定します。</typeparam>
	public class ValueArray<T> : IEquatable<ValueArray<T>>
	{
		readonly T[] _array;

		/// <summary>指定された配列を使用して、<see cref="Microsoft.Scripting.Utils.ValueArray&lt;T&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="array">このオブジェクトでラップする配列を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="array"/> または <paramref name="array"/> の要素が <c>null</c> です。</exception>
		public ValueArray(T[] array)
		{
			ContractUtils.RequiresNotNull(array, "array");
			ContractUtils.RequiresNotNullItems(array, "array");
			_array = array;
		}

		/// <summary>指定された <see cref="ValueArray&lt;T&gt;"/> がこのオブジェクトと等しいかどうかを判断します。</summary>
		/// <param name="other">このオブジェクトと等しいかどうかを調べるオブジェクトを指定します。</param>
		/// <returns>このオブジェクトが指定されたオブジェクトと等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		[StateIndependent]
		public bool Equals(ValueArray<T> other) { return other != null && _array.SequenceEqual(other._array); }

		/// <summary>指定されたオブジェクトがこのオブジェクトと等しいかどうかを判断します。</summary>
		/// <param name="obj">このオブジェクトと等しいかどうかを調べるオブジェクトを指定します。</param>
		/// <returns>このオブジェクトが指定されたオブジェクトと等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		[Confined]
		public override bool Equals(object obj) { return Equals(obj as ValueArray<T>); }

		/// <summary>このオブジェクトのハッシュ値を計算します。</summary>
		/// <returns>オブジェクトのハッシュ値。</returns>
		[Confined]
		public override int GetHashCode()
		{
			int val = 6551;
			for (int i = 0; i < _array.Length; i++)
				val ^= _array[i].GetHashCode();
			return val;
		}
	}
}
