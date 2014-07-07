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
	/// <summary>
	/// 変換操作の結果を判別します。
	/// 結果は例外、正常に変換されたか default(T) によって与えられた場合の値、または値が変換できるかどうかを示すブール値の場合があります。
	/// </summary>
	public enum ConversionResultKind
	{
		/// <summary>利用可能な暗黙的変換を試み、利用可能な変換が存在しない場合には例外をスローします。</summary>
		ImplicitCast,
		/// <summary>利用可能な暗黙的および明示的変換を試み、利用可能な変換が存在しない場合には例外をスローします。</summary>
		ExplicitCast,
		/// <summary>利用可能な暗黙的変換を試み、変換が実行されない場合には <c>default(ReturnType)</c> を返します。</summary>
		ImplicitTry,
		/// <summary>利用可能な暗黙的および明示的変換を試み、変換が実行されない場合には <c>default(ReturnType)</c> を返します。</summary>
		ExplicitTry
	}
}
