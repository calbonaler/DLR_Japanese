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


namespace Microsoft.Scripting.Hosting
{
	/// <summary>メンバの種類を表します。</summary>
	public enum MemberKind
	{
		/// <summary>なし</summary>
		None,
		/// <summary>クラス</summary>
		Class,
		/// <summary>デリゲート</summary>
		Delegate,
		/// <summary>列挙体</summary>
		Enum,
		/// <summary>イベント</summary>
		Event,
		/// <summary>フィールド</summary>
		Field,
		/// <summary>関数</summary>
		Function,
		/// <summary>モジュール</summary>
		Module,
		/// <summary>プロパティ</summary>
		Property,
		/// <summary>定数</summary>
		Constant,
		/// <summary>列挙体のメンバ</summary>
		EnumMember,
		/// <summary>インスタンス</summary>
		Instance,
		/// <summary>メソッド</summary>
		Method,
		/// <summary>名前空間</summary>
		Namespace
	}
}
