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
using Microsoft.Scripting.Hosting;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>生存期間中のオブジェクトに対する言語固有のドキュメントを提供します。</summary>
	public abstract class DocumentationProvider
	{
		/// <summary>派生クラスでオーバーライドされると、指定されたオブジェクトに対するメンバのリストを取得します。</summary>
		/// <param name="value">メンバのリストを取得するオブジェクトを指定します。</param>
		public abstract ICollection<MemberDoc> GetMembers(object value);

		/// <summary>派生クラスでオーバーライドされると、指定されたオブジェクトに対するオーバーロードのリストを取得します。</summary>
		/// <param name="value">オーバーロードのリストを取得するオブジェクトを指定します。</param>
		public abstract ICollection<OverloadDoc> GetOverloads(object value);
	}
}
