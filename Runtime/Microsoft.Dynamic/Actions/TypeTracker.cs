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
using System.Reflection;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Actions
{
	/// <summary>型を表します。</summary>
	public abstract class TypeTracker : MemberTracker, IMembersList
	{
		/// <summary><see cref="Microsoft.Scripting.Actions.TypeTracker"/> クラスの新しいインスタンスを初期化します。</summary>
		protected TypeTracker() { }

		/// <summary>この <see cref="TypeTracker"/> によって表される型を取得します。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
		public abstract Type Type { get; }

		/// <summary>この型がジェネリック型かどうかを示す値を取得します。</summary>
		public abstract bool IsGenericType { get; }

		/// <summary>この型がパブリックとして宣言されているかどうかを示す値を取得します。</summary>
		public abstract bool IsPublic { get; }

		/// <summary>この型に含まれているすべてのメンバの名前を取得します。</summary>
		/// <returns>メンバの名前のリスト。</returns>
		public virtual IEnumerable<string> GetMemberNames()
		{
			HashSet<string> names = new HashSet<string>();
			CollectMembers(names, Type);
			return names;
		}

		/// <summary>指定された型に含まれているすべてのメンバの名前を指定されたセットに追加します。</summary>
		/// <param name="names">メンバの名前を追加するセットを指定します。</param>
		/// <param name="t">追加するメンバを含んでいる型を指定します。</param>
		internal static void CollectMembers(ISet<string> names, Type t)
		{
			foreach (var mi in t.GetMembers())
			{
				if (mi.MemberType != MemberTypes.Constructor)
					names.Add(mi.Name);
			}
		}

		/// <summary>動的言語全体にわたる <see cref="TypeTracker"/> から <see cref="Type"/> への暗黙的な変換を有効化します。</summary>
		/// <param name="tracker">変換元の <see cref="TypeTracker"/>。</param>
		/// <returns><see cref="TypeTracker"/> に対応する <see cref="Type"/>。</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates")]
		public static explicit operator Type(TypeTracker tracker)
		{
			var tg = tracker as TypeGroup;
			if (tg != null)
			{
				Type res;
				if (!tg.TryGetNonGenericType(out res))
					throw ScriptingRuntimeHelpers.SimpleTypeError("expected non-generic type, got generic-only type");
				return res;
			}
			return tracker.Type;
		}
	}
}
