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

using Microsoft.Contracts;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>継承によって拡張できない型を動的言語上で拡張できるようにします。</summary>
	/// <typeparam name="T">拡張する型を指定します。</typeparam>
	public class Extensible<T>
	{
		/// <summary><see cref="Microsoft.Scripting.Runtime.Extensible&lt;T&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		public Extensible() { }

		/// <summary>指定された値を使用して、<see cref="Microsoft.Scripting.Runtime.Extensible&lt;T&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="value">拡張する型の値を指定します。</param>
		public Extensible(T value) { Value = value; }

		/// <summary>このオブジェクトが拡張する型の値を取得します。</summary>
		public T Value { get; private set; }

		/// <summary>指定されたオブジェクトがこのオブジェクトと等しいかどうかを判断します。</summary>
		/// <param name="obj">比較するオブジェクトを指定します。</param>
		/// <returns>指定されたオブジェクトがこのオブジェクトと等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		[Confined]
		public override bool Equals(object obj) { return Equals(Value, obj); }

		/// <summary>このオブジェクトのハッシュ値を計算します。</summary>
		/// <returns>オブジェクトのハッシュ値。</returns>
		[Confined]
		public override int GetHashCode() { return Value.GetHashCode(); }

		/// <summary>このオブジェクトの文字列表現を返します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		[Confined]
		public override string ToString() { return Value.ToString(); }

		/// <summary>指定された <see cref="Extensible&lt;T&gt;"/> から基になる値を取得します。</summary>
		/// <param name="extensible">基になる値を取得する <see cref="Extensible&lt;T&gt;"/>。</param>
		/// <returns><see cref="Extensible&lt;T&gt;"/> の基になる値。</returns>
		public static implicit operator T(Extensible<T> extensible) { return extensible.Value; }
	}
}
