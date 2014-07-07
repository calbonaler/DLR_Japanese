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

namespace Microsoft.Scripting.Actions
{
	/// <summary><see cref="MemberTracker"/> が表すメンバの種類を指定します。</summary>
	[Flags]
	public enum TrackerTypes
	{
		/// <summary>どの種類のメンバも指定しません。</summary>
		None = 0x00,
		/// <summary>メンバが <see cref="ConstructorTracker"/> により表されるコンストラクタであることを指定します。</summary>
		Constructor = 0x01,
		/// <summary>メンバが <see cref="EventTracker"/> により表されるイベントであることを指定します。</summary>
		Event = 0x02,
		/// <summary>メンバが <see cref="FieldTracker"/> により表されるフィールドであることを指定します。</summary>
		Field = 0x04,
		/// <summary>メンバが <see cref="MethodTracker"/> により表されるメソッドであることを指定します。</summary>
		Method = 0x08,
		/// <summary>メンバが <see cref="PropertyTracker"/> により表されるプロパティであることを指定します。</summary>
		Property = 0x10,
		/// <summary>メンバが <see cref="TypeTracker"/> により表される型であることを指定します。</summary>
		Type = 0x20,
		/// <summary>メンバが <see cref="NamespaceTracker"/> により表される名前空間であることを指定します。</summary>
		Namespace = 0x40,
		/// <summary>メンバが <see cref="MethodGroup"/> により表されるメソッドオーバーロードのグループであることを指定します。</summary>
		MethodGroup = 0x80,
		/// <summary>メンバが <see cref="TypeGroup"/> により表されるジェネリック アリティの異なる型のグループであることを指定します。</summary>
		TypeGroup = 0x100,
		/// <summary>メンバが <see cref="CustomTracker"/> により表されるカスタムメンバであることを指定します。</summary>
		Custom = 0x200,
		/// <summary>メンバが <see cref="BoundMemberTracker"/> により表され、インスタンスに関連付けられていることを指定します。</summary>        
		Bound = 0x400,
		/// <summary>すべてのメンバの種類を指定します。</summary>
		All = Constructor | Event | Field | Method | Property | Type | Namespace | MethodGroup | TypeGroup | Bound,
	}
}
