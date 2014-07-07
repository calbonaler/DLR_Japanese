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
	/// コールサイトでの個別の引数に対する規則です。
	/// 複数のコールサイトは単一の宣言に対して一致させることができます。
	/// 引数の種類の中にはリストあるいはディクショナリのように複数の引数に展開されるものもあります。
	/// </summary>
	public enum ArgumentType
	{
		/// <summary>単純な名前のない位置が決定されている引数です。</summary>
		/// <example>Python では foo(1,2,3) はすべて単純な引数です。</example>
		Simple,
		/// <summary>コールサイトで関連付けられた名前を持つ引数です。</summary>
		/// <example>Python では foo(a=1) がこれにあたります。</example>
		Named,
		/// <summary>引数のリストを含む引数です。</summary>
		/// <example>
		/// Python では、foo(*(1,2*2,3)) は (a,b,c)=(1,4,3) として 3 つの宣言された引数を持つ def foo(a,b,c) に一致します。
		/// また、l=(1,4,3) として、1 つの宣言された引数を持つ def foo(*l) にも一致します。
		/// </example>
		List,
		/// <summary>名前付き引数のディクショナリを含んでいる引数です。</summary>
		/// <example>Python では、foo(**{'a':1, 'b':2}) がこれにあたります。</example>
		Dictionary,
		/// <summary>インスタンス引数です。</summary>
		Instance
	};
}
