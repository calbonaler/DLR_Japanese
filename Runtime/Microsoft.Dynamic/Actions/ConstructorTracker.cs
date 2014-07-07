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
using System.Reflection;
using Microsoft.Contracts;

namespace Microsoft.Scripting.Actions
{
	/// <summary>コンストラクタを表します。</summary>
	public class ConstructorTracker : MemberTracker
	{
		ConstructorInfo _ctor;

		/// <summary>指定された <see cref="ConstructorInfo"/> を使用して、<see cref="Microsoft.Scripting.Actions.ConstructorTracker"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="ctor">このトラッカーが表すコンストラクタを指定します。</param>
		public ConstructorTracker(ConstructorInfo ctor) { _ctor = ctor; }

		/// <summary>メンバを論理的に宣言する型を取得します。</summary>
		public override Type DeclaringType { get { return _ctor.DeclaringType; } }

		/// <summary><see cref="MemberTracker"/> の種類を取得します。</summary>
		public sealed override TrackerTypes MemberType { get { return TrackerTypes.Constructor; } }

		/// <summary>メンバの名前を取得します。</summary>
		public override string Name { get { return _ctor.Name; } }

		/// <summary>このコンストラクタがパブリックかどうかを示す値を取得します。</summary>
		public bool IsPublic { get { return _ctor.IsPublic; } }

		/// <summary>このオブジェクトの文字列表現を返します。</summary>
		/// <returns>このオブジェクトの文字列表現。</returns>
		[Confined]
		public override string ToString() { return _ctor.ToString(); }
	}
}
