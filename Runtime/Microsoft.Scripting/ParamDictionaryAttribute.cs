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
	/// <summary>
	/// 通常の引数に束縛されないあらゆるキーワード引数を受け付けることができる引数をマークするために使用されます。
	/// この特別なキーワード引数は呼び出しで作成されるディクショナリ内で渡されます。
	/// </summary>
	/// <remarks>
	/// 引数ディクショナリをサポートするほとんどの言語は下記の型を使用できます。
	/// <list type="bullet">
	///		<item><description>IDictionary&lt;string, anything&gt;</description></item>
	///		<item><description>IDictionary&lt;object, anything&gt;</description></item>
	///		<item><description>Dictionary&lt;string, anything&gt;</description></item>
	///		<item><description>Dictionary&lt;object, anything&gt;</description></item>
	///		<item><description>IDictionary</description></item>
	///		<item><description>IAttributeCollection (旧式)</description></item>
	/// </list>
	/// 
	/// 言語レベルでのサポートのない言語では、ユーザーが自分でディクショナリを作成し、アイテムを格納しなければなりません。
	/// この属性はディクショナリとして <see cref="System.ParamArrayAttribute"/> と同値です。
	/// </remarks>
	/// <example>
	/// public static void KeywordArgFunction([ParamsDictionary]IDictionary&lt;string, object&gt; dict) {
	///     foreach (var v in dict) {
	///         Console.WriteLine("Key: {0} Value: {1}", v.Key, v.Value);
	///     }
	/// }
	/// 
	/// Python からは以下のように呼び出されます。
	/// 
	/// KeywordArgFunction(a = 2, b = "abc")
	/// 
	/// will print:
	///     Key: a Value = 2
	///     Key: b Value = abc
	/// </example>
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
	public sealed class ParamDictionaryAttribute : Attribute { }
}
