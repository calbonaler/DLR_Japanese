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
using System.Collections.ObjectModel;
using System.Dynamic;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>それぞれの実引数に明示的な型を追加した実引数のコレクションを表します。</summary>
	public sealed class RestrictedArguments
	{
		/// <summary>指定されたオブジェクトに対する <see cref="Microsoft.Scripting.Actions.Calls.RestrictedArguments"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="objects">実引数の値を表す <see cref="DynamicMetaObject"/> の配列を指定します。</param>
		/// <param name="types">実引数の制約された型の配列を指定します。</param>
		/// <param name="hasUntypedRestrictions">この実引数リストに単純な型制約以外に制約が存在するかどうかを示す値を指定します。</param>
		public RestrictedArguments(DynamicMetaObject[] objects, Type[] types, bool hasUntypedRestrictions)
		{
			ContractUtils.RequiresNotNullItems(objects, "objects");
			ContractUtils.RequiresNotNull(types, "types");
			ContractUtils.Requires(objects.Length == types.Length, "objects");
			Objects = new ReadOnlyCollection<DynamicMetaObject>(objects);
			Types = new ReadOnlyCollection<Type>(types);
			HasUntypedRestrictions = hasUntypedRestrictions;
		}

		/// <summary>実引数の数を取得します。</summary>
		public int Length { get { return Objects.Count; } }

		/// <summary>単純な型制約以外に制約が存在するかどうかを示す値を取得します。</summary>
		public bool HasUntypedRestrictions { get; private set; }

		/// <summary>このコレクションに含まれているすべてのバインディング制約を 1 つのセットにまとめて取得します。</summary>
		/// <returns>1 つにまとめられたコレクションに含まれているすべての <see cref="DynamicMetaObject"/> のバインディング制約。</returns>
		public BindingRestrictions GetAllRestrictions() { return BindingRestrictions.Combine(Objects); }

		/// <summary>実引数の値を表す <see cref="DynamicMetaObject"/> を取得します。</summary>
		public ReadOnlyCollection<DynamicMetaObject> Objects { get; private set; }

		/// <summary>対応する <see cref="DynamicMetaObject"/> に対する型を取得します。</summary>
		public ReadOnlyCollection<Type> Types { get; private set; }
	}
}
