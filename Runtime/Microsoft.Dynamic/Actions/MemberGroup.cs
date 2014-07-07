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

using System.Collections.Generic;
using System.Reflection;
using Microsoft.Contracts;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions
{
	/// <summary>
	/// 利用可能なメンバを調べるために通常要求に応じて生成される <see cref="MemberTracker"/> のコレクションを表します。
	/// このクラスには同じ種類の複数のメンバも異なる種類の複数のメンバも含めることができます。
	/// </summary>
	/// <remarks>
	/// 最も一般的な <see cref="MemberGroup"/> の取得元は <see cref="ActionBinder.GetMember"/> です。
	/// ここから DLR は頻繁にユーザーによる値を生成する <see cref="MemberTracker"/> に対するバインディングを実行します。
	/// もし操作の結果がメンバ自体を生成するならば、<see cref="ActionBinder"/> は ReturnMemberTracker を通してユーザーに公開する値を提供できます。
	/// <see cref="ActionBinder"/> はユーザーに対するメンバの公開と同時に型からのメンバの取得に関する既定の機能を提供します。
	/// 型からのメンバの取得はリフレクションに厳密に対応し、ユーザーに対するメンバの公開は <see cref="MemberTracker"/> を直接公開することに対応します。
	/// </remarks>
	public class MemberGroup : IEnumerable<MemberTracker>
	{
		/// <summary>空の <see cref="MemberGroup"/> を表します。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly MemberGroup EmptyGroup = new MemberGroup(MemberTracker.EmptyTrackers);

		readonly MemberTracker[] _members;

		/// <summary>指定された <see cref="MemberTracker"/> を使用して、<see cref="Microsoft.Scripting.Actions.MemberGroup"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="members">この <see cref="MemberGroup"/> に含めるメンバを指定します。</param>
		public MemberGroup(params MemberTracker[] members)
		{
			ContractUtils.RequiresNotNullItems(members, "members");
			_members = members;
		}

		/// <summary>指定された <see cref="MemberInfo"/> を使用して、<see cref="Microsoft.Scripting.Actions.MemberGroup"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="members">この <see cref="MemberGroup"/> に含めるメンバを指定します。</param>
		public MemberGroup(params MemberInfo[] members)
		{
			ContractUtils.RequiresNotNullItems(members, "members");
			_members = System.Array.ConvertAll(members, x => MemberTracker.FromMemberInfo(x));
		}

		/// <summary>この <see cref="MemberGroup"/> 内に含まれているメンバの数を取得します。</summary>
		public int Count { get { return _members.Length; } }

		/// <summary>この <see cref="MemberGroup"/> 内の指定された位置にあるメンバを取得します。</summary>
		/// <param name="index">メンバの位置を示す 0 から始まるインデックスを指定します。</param>
		/// <returns>指定された位置にある <see cref="MemberTracker"/>。</returns>
		public MemberTracker this[int index] { get { return _members[index]; } }

		/// <summary>このコレクションを反復処理する列挙子を返します。</summary>
		/// <returns>コレクションを反復処理するために使用できる <see cref="System.Collections.Generic.IEnumerator&lt;MemberTracker&gt;"/>。</returns>
		[Pure]
		public IEnumerator<MemberTracker> GetEnumerator() { return ((IEnumerable<MemberTracker>)_members).GetEnumerator(); }

		/// <summary>このコレクションを反復処理する列挙子を返します。</summary>
		/// <returns>コレクションを反復処理するために使用できる <see cref="System.Collections.IEnumerator"/>。</returns>
		[Pure]
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}
}