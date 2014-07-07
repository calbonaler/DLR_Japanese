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
	/// <summary>メソッドに対するバインディングの結果を示します。</summary>
	public enum BindingResult
	{
		/// <summary>バインディングは成功しました。ただ 1 つのメソッドが適用可能であったか最適な変換が存在しました。</summary>
		Success,
		/// <summary>複数のメソッドが指定された引数に対して適用可能であったか、どのメソッドも最適であると判断できませんでした。</summary>
		AmbiguousMatch,
		/// <summary>呼び出しに対して要求される引数の数に適合するオーバーロードは存在しません。</summary>
		IncorrectArgumentCount,
		/// <summary>
		/// どのメソッドも正常に呼び出すことができませんでした。以下の原因が考えられます。
		/// 実引数を正常に変換できませんでした。
		/// 名前付き引数を位置決定済み引数に代入できませんでした。
		/// 名前付き引数が複数回代入されました。(引数間で競合が発生しているか、名前付き引数が重複しています。)
		/// </summary>
		CallFailure,
		/// <summary>実引数を構築できませんでした。</summary>
		InvalidArguments,
		/// <summary>どのメソッドも呼び出し可能ではありません。たとえば、すべてのメソッドがバインドされていないジェネリック引数を含んでいます。</summary>
		NoCallableMethod,
	}
}
