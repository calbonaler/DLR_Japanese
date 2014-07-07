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
	/// <summary>オーバーロード解決の際の選択された候補を表します。</summary>
	public enum Candidate
	{
		/// <summary>2 つの候補は等価です。</summary>
		Equivalent = 0,
		/// <summary>1 番目の候補が選択されました。</summary>
		One = +1,
		/// <summary>2 番目の候補が選択されました。</summary>
		Two = -1,
		/// <summary>2 つの候補はあいまいであり選択できません。</summary>
		Ambiguous = 2
	}

	/// <summary><see cref="Microsoft.Scripting.Actions.Calls.Candidate"/> に対する拡張メソッドを提供します。</summary>
	internal static class CandidateExtension
	{
		/// <summary>現在の <see cref="Candidate"/> において選択済みであるかどうかを示す値を取得します。</summary>
		/// <param name="candidate">判断する <see cref="Candidate"/> を指定します。</param>
		/// <returns><see cref="Candidate"/> が選択済みである場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool Chosen(this Candidate candidate) { return candidate == Candidate.One || candidate == Candidate.Two; }

		/// <summary>現在の <see cref="Candidate"/> のもう片方を表す <see cref="Candidate"/> を取得します。</summary>
		/// <param name="candidate">もう片方を取得する <see cref="Candidate"/> を指定します。</param>
		/// <returns>もう片方を表す <see cref="Candidate"/>。</returns>
		public static Candidate TheOther(this Candidate candidate)
		{
			if (candidate == Candidate.One)
				return Candidate.Two;
			if (candidate == Candidate.Two)
				return Candidate.One;
			return candidate;
		}
	}
}
