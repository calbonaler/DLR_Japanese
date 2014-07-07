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
using System.Reflection;
using System.Dynamic;
using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Utils;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>仮引数のバインディングに関する情報を表します。</summary>
	[Flags]
	public enum ParameterBindingFlags
	{
		/// <summary>追加情報はありません。</summary>
		None = 0,
		/// <summary>仮引数は <c>null</c> を拒否します。</summary>
		ProhibitNull = 1,
		/// <summary>仮引数は <c>null</c> 要素を拒否します。</summary>
		ProhibitNullItems = 2,
		/// <summary>仮引数は配列引数です。</summary>
		IsParamArray = 4,
		/// <summary>仮引数は辞書引数です。</summary>
		IsParamDictionary = 8,
		/// <summary>仮引数は隠し引数です。</summary>
		IsHidden = 16,
	}

	/// <summary>
	/// 仮引数の論理ビューを表します。
	/// 例えば、参照渡し縮小シグネチャの論理ビューは引数が値渡しされ (さらに、更新値は戻り値に含められ) ることであるため、
	/// 参照渡し引数のあるメソッドの参照渡し縮小シグネチャは基になる要素型の <see cref="ParameterWrapper"/> を用いて表されます。
	/// このクラスはメソッドに実際に渡された物理実引数を表現する <see cref="ArgBuilder"/> とは対照的です。
	/// </summary>
	public sealed class ParameterWrapper
	{
		/// <summary>仮引数のメタデータ、型、名前、バインディング情報を使用して、<see cref="Microsoft.Scripting.Actions.Calls.ParameterWrapper"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="info">仮引数のメタデータを表す <see cref="ParameterInfo"/> を指定します。</param>
		/// <param name="type">仮引数の型を指定します。</param>
		/// <param name="name">仮引数の名前を指定します。</param>
		/// <param name="flags">仮引数のバインディング情報を指定します。</param>
		public ParameterWrapper(ParameterInfo info, Type type, string name, ParameterBindingFlags flags)
		{
			ContractUtils.RequiresNotNull(type, "type");
			Type = type;
			ParameterInfo = info;
			Flags = flags;
			// params arrays & dictionaries don't allow assignment by keyword
			Name = IsParamArray || IsParamDict || name == null ? "<unknown>" : name;
		}

		/// <summary>この <see cref="ParameterWrapper"/> の名前を指定された名前に置き換えた新しい <see cref="ParameterWrapper"/> を作成します。</summary>
		/// <param name="name">作成される <see cref="ParameterWrapper"/> の名前を指定します。</param>
		/// <returns>この <see cref="ParameterWrapper"/> の名前を指定された名前に置き換えた新しい <see cref="ParameterWrapper"/>。</returns>
		public ParameterWrapper Clone(string name) { return new ParameterWrapper(ParameterInfo, Type, name, Flags); }

		/// <summary>この仮引数の型を取得します。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
		public Type Type { get; private set; }

		/// <summary>この仮引数のメタデータを表す <see cref="ParameterInfo"/> を取得します。これは <c>null</c> である可能性があります。</summary>
		public ParameterInfo ParameterInfo { get; private set; }

		/// <summary>この仮引数の名前を取得します。</summary>
		public string Name { get; private set; }

		/// <summary>この仮引数のバインディング情報を取得します。</summary>
		public ParameterBindingFlags Flags { get; private set; }

		/// <summary>この仮引数が <c>null</c> を拒否するかどうかを示す値を取得します。</summary>
		public bool ProhibitNull { get { return (Flags & ParameterBindingFlags.ProhibitNull) != 0; } }

		/// <summary>この仮引数が <c>null</c> 要素を拒否するかどうかを示す値を取得します。</summary>
		public bool ProhibitNullItems { get { return (Flags & ParameterBindingFlags.ProhibitNullItems) != 0; } }

		/// <summary>この仮引数が隠し引数かどうかを示す値を取得します。</summary>
		public bool IsHidden { get { return (Flags & ParameterBindingFlags.IsHidden) != 0; } }

		/// <summary>この仮引数が参照渡しであるかどうかを示す値を取得します。</summary>
		public bool IsByRef { get { return ParameterInfo != null && ParameterInfo.ParameterType.IsByRef; } }

		/// <summary>この仮引数が配列引数を表しているかどうかを示す値を取得します。(配列引数の展開により作成された仮引数では <c>false</c> を返します。)</summary>
		public bool IsParamArray { get { return (Flags & ParameterBindingFlags.IsParamArray) != 0; } }

		/// <summary>この仮引数が辞書引数を表しているかどうかを示す値を取得します。(辞書引数の展開により作成された仮引数では <c>false</c> を返します。)</summary>
		public bool IsParamDict { get { return (Flags & ParameterBindingFlags.IsParamDictionary) != 0; } }

		/// <summary>配列引数の展開された要素を表す仮引数を作成します。</summary>
		/// <returns>配列引数の展開された要素を表す仮引数。</returns>
		internal ParameterWrapper Expand()
		{
			Debug.Assert(IsParamArray);
			return new ParameterWrapper(ParameterInfo, Type.GetElementType(), null, (ProhibitNullItems ? ParameterBindingFlags.ProhibitNull : 0) | (IsHidden ? ParameterBindingFlags.IsHidden : 0));
		}
	}
}
