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
using Microsoft.Contracts;

namespace Microsoft.Scripting
{
	/// <summary>トークンに関する情報を格納します。</summary>
	[Serializable]
	public struct TokenInfo : IEquatable<TokenInfo>
	{
		/// <summary>このトークンの種類を取得または設定します。</summary>
		public TokenCategory Category { get; set; }

		/// <summary>このトークンに関連付けられているトリガを取得または設定します。</summary>
		public TokenTriggers Trigger { get; set; }

		/// <summary>このトークンのソースコード上の場所を取得または設定します。</summary>
		public SourceSpan SourceSpan { get; set; }

		/// <summary>ソースコード内の範囲、トークンの種類、トリガを使用して、<see cref="Microsoft.Scripting.TokenInfo"/> 構造体の新しいインスタンスを初期化します。</summary>
		/// <param name="span">トークンのソースコード上の場所を指定します。</param>
		/// <param name="category">トークンの種類を指定します。</param>
		/// <param name="trigger">トークンに関連付けられているトリガを指定します。</param>
		public TokenInfo(SourceSpan span, TokenCategory category, TokenTriggers trigger) : this()
		{
			Category = category;
			Trigger = trigger;
			SourceSpan = span;
		}

		/// <summary>このトークンと指定されたトークンが等しいかどうかを判断します。</summary>
		/// <param name="other">比較するトークンを指定します。</param>
		/// <returns>2 つのトークンが等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		[StateIndependent]
		public bool Equals(TokenInfo other) { return Category == other.Category && Trigger == other.Trigger && SourceSpan == other.SourceSpan; }

		/// <summary>このオブジェクトと指定されたオブジェクトが等しいかどうかを判断します。</summary>
		/// <param name="obj">比較するオブジェクトを指定します。</param>
		/// <returns>2 つのオブジェクトが等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public override bool Equals(object obj) { return obj is TokenInfo && Equals((TokenInfo)obj); }

		/// <summary>このオブジェクトのハッシュ値を計算します。</summary>
		/// <returns>計算されたハッシュ値。</returns>
		public override int GetHashCode() { return Category.GetHashCode() ^ Trigger.GetHashCode() ^ SourceSpan.GetHashCode(); }

		/// <summary>2 つのトークンが等しいかどうかを判断します。</summary>
		/// <param name="left">比較する 1 つ目のトークン。</param>
		/// <param name="right">比較する 2 つ目のトークン。</param>
		/// <returns>2 つのトークンが等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool operator ==(TokenInfo left, TokenInfo right) { return left.Equals(right); }

		/// <summary>2 つのトークンが等しくないかどうかを判断します。</summary>
		/// <param name="left">比較する 1 つ目のトークン。</param>
		/// <param name="right">比較する 2 つ目のトークン。</param>
		/// <returns>2 つのトークンが等しくない場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool operator !=(TokenInfo left, TokenInfo right) { return !left.Equals(right); }
	}
}
