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
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting
{
	/// <summary>生存期間中のメンバに関するドキュメントを提供します。</summary>
	[Serializable]
	public class MemberDoc
	{
		/// <summary>メンバの名前および種類を使用して、<see cref="Microsoft.Scripting.Hosting.MemberDoc"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="name">説明するメンバの名前を指定します。</param>
		/// <param name="kind">説明するメンバの種類を指定します。</param>
		public MemberDoc(string name, MemberKind kind)
		{
			ContractUtils.RequiresNotNull(name, "name");
			ContractUtils.Requires(kind >= MemberKind.None && kind <= MemberKind.Namespace, "kind");
			Name = name;
			Kind = kind;
		}

		/// <summary>メンバの名前を取得します。</summary>
		public string Name { get; private set; }

		/// <summary>すでに判明している場合にメンバの種類を取得します。</summary>
		public MemberKind Kind { get; private set; }
	}

}
