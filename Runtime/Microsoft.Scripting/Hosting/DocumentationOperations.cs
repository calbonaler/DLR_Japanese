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
using System.Runtime.Remoting;
using System.Security.Permissions;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Hosting
{
	/// <summary>生存期間中のオブジェクトに対して REPL ウィンドウで使用されるドキュメントを提供します。</summary>
	public sealed class DocumentationOperations : MarshalByRefObject
	{
		readonly DocumentationProvider _provider;

		/// <summary>
		/// 指定された <see cref="Microsoft.Scripting.Runtime.DocumentationProvider"/> を使用して、
		/// <see cref="Microsoft.Scripting.Hosting.DocumentationOperations"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="provider">ドキュメントを提供する <see cref="Microsoft.Scripting.Runtime.DocumentationProvider"/> を指定します。</param>
		internal DocumentationOperations(DocumentationProvider provider) { _provider = provider; }

		/// <summary>指定されたオブジェクトに定義されている利用可能なメンバを取得します。</summary>
		/// <param name="value">メンバを取得するオブジェクトを指定します。</param>
		public ICollection<MemberDoc> GetMembers(object value) { return _provider.GetMembers(value); }

		/// <summary>指定されたオブジェクトが呼び出し可能であれば、利用可能なオーバーロードを取得します。</summary>
		/// <param name="value">オーバーロードを取得するオブジェクトを指定します。</param>
		public ICollection<OverloadDoc> GetOverloads(object value) { return _provider.GetOverloads(value); }

		/// <summary>指定されたリモートオブジェクトに定義されている利用可能なメンバを取得します。</summary>
		/// <param name="value">メンバを取得するリモートオブジェクトを指定します。</param>
		public ICollection<MemberDoc> GetMembers(ObjectHandle value) { return _provider.GetMembers(value.Unwrap()); }

		/// <summary>指定されたリモートオブジェクトが呼び出し可能であれば、利用可能なオーバーロードを取得します。</summary>
		/// <param name="value">オーバーロードを取得するリモートオブジェクトを指定します。</param>
		public ICollection<OverloadDoc> GetOverloads(ObjectHandle value) { return _provider.GetOverloads(value.Unwrap()); }

		// TODO: Figure out what is the right lifetime
		/// <summary>対象のインスタンスの有効期間ポリシーを制御する、有効期間サービス オブジェクトを取得します。</summary>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
		public override object InitializeLifetimeService() { return null; }
	}
}
