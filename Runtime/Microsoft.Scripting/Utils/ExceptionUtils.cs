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

namespace Microsoft.Scripting.Utils
{
	/// <summary>よく利用される例外を送出するユーティリティ メソッドを格納します。</summary>
	public static class ExceptionUtils
	{
		/// <summary>引数名、引数の値、エラーメッセージを使用して、新しい <see cref="ArgumentOutOfRangeException"/> を作成します。</summary>
		/// <param name="paramName">例外の原因となったパラメーターの名前。</param>
		/// <param name="actualValue">この例外の原因である引数の値。</param>
		/// <param name="message">エラーを説明するメッセージ。</param>
		/// <returns>新しく作成された <see cref="ArgumentOutOfRangeException"/>。</returns>
		public static ArgumentOutOfRangeException MakeArgumentOutOfRangeException(string paramName, object actualValue, string message) { throw new ArgumentOutOfRangeException(paramName, actualValue, message); }

		/// <summary>引数の指定されたインデックスが <c>null</c> であることを示す新しい <see cref="ArgumentNullException"/> を作成します。</summary>
		/// <param name="index"><c>null</c> 要素が格納されている引数のインデックスを指定します。</param>
		/// <param name="arrayName">引数の名前を指定します。</param>
		/// <returns>引数に <c>null</c> 要素が格納されていることを示す新しく作成された <see cref="ArgumentNullException"/>。</returns>
		public static ArgumentNullException MakeArgumentItemNullException(int index, string arrayName) { return new ArgumentNullException(string.Format("{0}[{1}]", arrayName, index)); }
	}
}
