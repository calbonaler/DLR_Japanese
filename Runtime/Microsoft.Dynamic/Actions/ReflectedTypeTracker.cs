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
using Microsoft.Contracts;

namespace Microsoft.Scripting.Actions
{
	/// <summary>単一の <see cref="Type"/> に対応する <see cref="TypeTracker"/> を表します。</summary>
	public class ReflectedTypeTracker : TypeTracker
	{
		readonly Type _type;

		/// <summary>基になる型を使用して、<see cref="Microsoft.Scripting.Actions.ReflectedTypeTracker"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="type">基になる型を表す <see cref="Type"/> を指定します。</param>
		public ReflectedTypeTracker(Type type) { _type = type; }

		/// <summary>現在の型が入れ子にされた型の場合は、これを宣言する型を取得します。</summary>
		public override Type DeclaringType { get { return _type.DeclaringType; } }

		/// <summary><see cref="MemberTracker"/> の種類を取得します。</summary>
		public override sealed TrackerTypes MemberType { get { return TrackerTypes.Type; } }

		/// <summary>メンバの名前を取得します。</summary>
		public override string Name { get { return _type.Name; } }

		/// <summary>この型がパブリックとして宣言されているかどうかを示す値を取得します。</summary>
		public override bool IsPublic { get { return _type.IsPublic; } }

		/// <summary>この <see cref="TypeTracker"/> によって表される型を取得します。</summary>
		public override Type Type { get { return _type; } }

		/// <summary>この型がジェネリック型かどうかを示す値を取得します。</summary>
		public override bool IsGenericType { get { return _type.IsGenericType; } }

		/// <summary>このオブジェクトの文字列表現を返します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		[Confined]
		public override string ToString() { return _type.ToString(); }
	}
}
