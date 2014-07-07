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

namespace Microsoft.Scripting.Runtime
{
	/// <summary>メソッドバインダーが行うバインディングの種類を指定します。</summary>
	public enum BinderType
	{
		/// <summary>メソッドバインダーは通常のバインディングを行います。</summary>
		Normal,
		/// <summary>メソッドバインダーは二項演算のバインディングを行います。</summary>
		BinaryOperator,
		/// <summary>メソッドバインダーは比較演算のバインディングを行います。</summary>
		ComparisonOperator,
		/// <summary>メソッドバインダーは返されるインスタンスで使用されないキーワード引数に対するプロパティまたはフィールドを設定します。</summary>
		Constructor
	}
}
