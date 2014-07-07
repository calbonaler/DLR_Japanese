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

namespace Microsoft.Scripting.Actions
{
	/// <summary>言語が任意のメンバを検索プロセスに参加させることができるようにするカスタムメンバトラッカーを表します。</summary>
	public abstract class CustomTracker : MemberTracker
	{
		/// <summary><see cref="Microsoft.Scripting.Actions.CustomTracker"/> クラスの新しいインスタンスを初期化します。</summary>
		protected CustomTracker() { }

		/// <summary><see cref="MemberTracker"/> の種類を取得します。</summary>
		public sealed override TrackerTypes MemberType { get { return TrackerTypes.Custom; } }
	}
}
