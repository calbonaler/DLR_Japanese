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

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary><see cref="OverloadResolver"/> による特定のメソッドに対する呼び出しが実行できない理由を表します。</summary>
	public sealed class CallFailure
	{
		/// <summary><see cref="CallFailureReason.ConversionFailure"/> である <see cref="Microsoft.Scripting.Actions.Calls.CallFailure"/> を作成します。</summary>
		/// <param name="candidate">呼び出しが失敗したメソッドを指定します。</param>
		/// <param name="results">それぞれの引数に対して変換が成功したかどうかおよび変換される型を格納する <see cref="ConversionResult"/> を指定します。</param>
		internal CallFailure(MethodCandidate candidate, ConversionResult[] results)
		{
			Candidate = candidate;
			ConversionResults = results;
			Reason = CallFailureReason.ConversionFailure;
		}

		/// <summary><see cref="CallFailureReason.UnassignableKeyword"/> または <see cref="CallFailureReason.DuplicateKeyword"/> である <see cref="Microsoft.Scripting.Actions.Calls.CallFailure"/> を作成します。</summary>
		/// <param name="candidate">呼び出しが失敗したメソッドを指定します。</param>
		/// <param name="keywordArgs">重複しているか代入不可能とされた名前付き引数を指定します。</param>
		/// <param name="unassignable"><see cref="Reason"/> プロパティが <see cref="CallFailureReason.UnassignableKeyword"/> であるかどうかを示す値を指定します。</param>
		internal CallFailure(MethodCandidate candidate, string[] keywordArgs, bool unassignable)
		{
			Reason = unassignable ? CallFailureReason.UnassignableKeyword : CallFailureReason.DuplicateKeyword;
			Candidate = candidate;
			KeywordArguments = keywordArgs;
		}

		/// <summary>その他の失敗を表す <see cref="Microsoft.Scripting.Actions.Calls.CallFailure"/> を作成します。</summary>
		/// <param name="candidate">呼び出しが失敗したメソッドを指定します。</param>
		/// <param name="reason">失敗の理由を示す <see cref="CallFailureReason"/> を指定します。</param>
		internal CallFailure(MethodCandidate candidate, CallFailureReason reason)
		{
			Candidate = candidate;
			Reason = reason;
		}

		/// <summary>呼び出しが失敗したメソッドを取得します。</summary>
		public MethodCandidate Candidate { get; private set; }

		/// <summary><see cref="CallFailure"/> の他のどのプロパティを参照すべきかを決定する呼び出しが失敗した理由を取得します。</summary>
		public CallFailureReason Reason { get; private set; }

		/// <summary><see cref="Reason"/> プロパティが <see cref="CallFailureReason.ConversionFailure"/> の場合、それぞれの引数に対して、変換が成功したかどうかおよび変換される型を格納する <see cref="ConversionResult"/> を取得します。</summary>
		public IList<ConversionResult> ConversionResults { get; private set; }

		/// <summary><see cref="Reason"/> プロパティが <see cref="CallFailureReason.UnassignableKeyword"/> または <see cref="CallFailureReason.DuplicateKeyword"/> の場合、重複しているか代入不可能とされた名前付き引数を取得します。</summary>
		public IList<string> KeywordArguments { get; private set; }
	}
}
