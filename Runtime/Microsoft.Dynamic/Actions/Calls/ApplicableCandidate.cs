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

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>適用可能なメソッドとその名前付き引数の関連付けを格納します。</summary>
	public sealed class ApplicableCandidate
	{
		/// <summary>適用可能なメソッドを取得します。</summary>
		public MethodCandidate Method { get; private set; }

		/// <summary>適用可能なメソッドに対する名前付き引数の関連付けを取得します。</summary>
		public ArgumentBinding ArgumentBinding { get; private set; }

		/// <summary>指定されたメソッドと名前付き引数の関連付けを使用して、<see cref="Microsoft.Scripting.Actions.Calls.ApplicableCandidate"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="method">適用可能なメソッドを指定します。</param>
		/// <param name="argBinding">適用可能なメソッドに対する名前付き引数の関連付けを指定します。</param>
		internal ApplicableCandidate(MethodCandidate method, ArgumentBinding argBinding)
		{
			Assert.NotNull(method, argBinding);
			Method = method;
			ArgumentBinding = argBinding;
		}

		/// <summary>指定されたインデックスに対応する仮引数を取得します。</summary>
		/// <param name="argumentIndex">仮引数に対応するインデックスを指定します。</param>
		/// <returns>指定されたインデックスに対応する仮引数。</returns>
		public ParameterWrapper GetParameter(int argumentIndex) { return Method.Parameters[ArgumentBinding.ArgumentToParameter(argumentIndex)]; }

		/// <summary>このオブジェクトの文字列表現を取得します。</summary>
		/// <returns>このオブジェクトに対する文字列表現。</returns>
		public override string ToString() { return Method.ToString(); }
	}
}
