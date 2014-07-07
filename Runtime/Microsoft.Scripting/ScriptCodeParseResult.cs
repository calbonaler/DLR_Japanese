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

namespace Microsoft.Scripting
{
	/// <summary>ソースコードの解析結果を表します。</summary>
	public enum ScriptCodeParseResult
	{
		/// <summary>ソースコードは文法的に正確です。</summary>
		Complete,
		/// <summary>ソースコードは空のステートメントまたは式を表しています。</summary>
		Empty,
		/// <summary>ソースコードは既に無効であり、文法的に正しいとされる部分はありません。</summary>
		Invalid,
		/// <summary>最後のトークンが未完了です。しかし、ソースコードは正確に完了させることができます。</summary>
		IncompleteToken,
		/// <summary>最後のステートメントが未完了です。しかし、ソースコードは正確に完了させることができます。</summary>
		IncompleteStatement,
	}

	// TODO: rename or remove
	public static class ScriptCodeParseResultUtils
	{
		public static bool IsCompleteOrInvalid(/*this*/ ScriptCodeParseResult props, bool allowIncompleteStatement)
		{
			return props == ScriptCodeParseResult.Invalid || props != ScriptCodeParseResult.IncompleteToken &&
				(allowIncompleteStatement || props != ScriptCodeParseResult.IncompleteStatement);
		}
	}
}
