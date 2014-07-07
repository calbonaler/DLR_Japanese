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

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	// TODO: this class should be abstract
	/// <summary>スコープに付加される言語ごとの情報を格納するスコープ拡張子を表します。</summary>
	public abstract class ScopeExtension
	{
		/// <summary><see cref="ScopeExtension"/> の空の配列を取得します。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
		public static readonly ScopeExtension[] EmptyArray = new ScopeExtension[0];

		/// <summary>このスコープ拡張子が関連付けられているスコープを取得します。</summary>
		public Scope Scope { get; private set; }

		/// <summary>関連づけるスコープを使用して、<see cref="Microsoft.Scripting.Runtime.ScopeExtension"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="scope">このスコープ拡張子を関連付けるスコープを指定します。</param>
		protected ScopeExtension(Scope scope)
		{
			ContractUtils.RequiresNotNull(scope, "scope");
			Scope = scope;
		}
	}
}
