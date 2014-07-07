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

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary><see cref="OverloadResolver"/> による呼び出しが実行できない理由を表します。</summary>
	public enum CallFailureReason
	{
		/// <summary>既定値。エラーはありません。</summary>
		None,
		/// <summary>1 つ以上の引数の変換に失敗しました。</summary>
		ConversionFailure,
		/// <summary>1 つ以上の名前付き引数を正常に位置決定済み引数に代入できませんでした。</summary>
		UnassignableKeyword,
		/// <summary>1 つ以上の名前付き引数が重複しているか、位置決定済み引数と競合しています。</summary>
		DuplicateKeyword,
		/// <summary>型引数を推論できませんでした。</summary>
		TypeInference
	}
}
