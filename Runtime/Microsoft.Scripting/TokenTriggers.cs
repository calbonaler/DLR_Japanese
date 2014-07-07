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

namespace Microsoft.Scripting
{
	/// <summary>トークナイザから駆動されるトリガを指定します。</summary>
	[Flags]
	public enum TokenTriggers
	{
		/// <summary>どのトリガも設定されていません。これは既定値です。</summary>
		None = 0,
		/// <summary>メンバ選択の開始を示す文字が解析されました。</summary>
		MemberSelect = 1,
		/// <summary>言語においてペアとなる要素の開始または終了部分が解析されました。</summary>
		MatchBraces = 2,
		/// <summary>引数リストの開始を示す文字が解析されました。</summary>
		ParameterStart = 16,
		/// <summary>引数リスト中で引数を区切る文字が解析されました。</summary>
		ParameterNext = 32,
		/// <summary>引数リストの終了を示す文字が解析されました。</summary>
		ParameterEnd = 64,
		/// <summary>メソッドの引数リスト内の引数が解析されました。</summary>
		Parameter = 128,
		/// <summary>インテリセンスのメソッド情報操作の制御に使用されるフラグに対するマスクです。</summary>
		MethodTip = 240,
	}
}
