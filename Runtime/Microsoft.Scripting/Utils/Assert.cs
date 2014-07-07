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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Scripting.Utils
{
	/// <summary>表明を行うメソッドを提供します。</summary>
	public static class Assert
	{
		/// <summary>コード上で到達しない部分をマークし、到達した場合はメッセージを表示して例外を返します。</summary>
		public static Exception Unreachable
		{
			get
			{
				Debug.Fail("Unreachable");
				return new InvalidOperationException("Code supposed to be unreachable");
			}
		}

		/// <summary>1 つの参照型変数が <c>null</c> でないことを表明します。</summary>
		/// <param name="var"><c>null</c> でないことを表明する参照型変数を指定します。</param>
		[Conditional("DEBUG")]
		public static void NotNull(object var) { Debug.Assert(var != null); }

		/// <summary>2 つの参照型変数がどちらも <c>null</c> でないことを表明します。</summary>
		/// <param name="var1"><c>null</c> でないことを表明する 1 つ目の参照型変数を指定します。</param>
		/// <param name="var2"><c>null</c> でないことを表明する 2 つ目の参照型変数を指定します。</param>
		[Conditional("DEBUG")]
		public static void NotNull(object var1, object var2) { Debug.Assert(var1 != null && var2 != null); }

		/// <summary>3 つの参照型変数がどれも <c>null</c> でないことを表明します。</summary>
		/// <param name="var1"><c>null</c> でないことを表明する 1 つ目の参照型変数を指定します。</param>
		/// <param name="var2"><c>null</c> でないことを表明する 2 つ目の参照型変数を指定します。</param>
		/// <param name="var3"><c>null</c> でないことを表明する 3 つ目の参照型変数を指定します。</param>
		[Conditional("DEBUG")]
		public static void NotNull(object var1, object var2, object var3) { Debug.Assert(var1 != null && var2 != null && var3 != null); }

		/// <summary>4 つの参照型変数がどれも <c>null</c> でないことを表明します。</summary>
		/// <param name="var1"><c>null</c> でないことを表明する 1 つ目の参照型変数を指定します。</param>
		/// <param name="var2"><c>null</c> でないことを表明する 2 つ目の参照型変数を指定します。</param>
		/// <param name="var3"><c>null</c> でないことを表明する 3 つ目の参照型変数を指定します。</param>
		/// <param name="var4"><c>null</c> でないことを表明する 4 つ目の参照型変数を指定します。</param>
		[Conditional("DEBUG")]
		public static void NotNull(object var1, object var2, object var3, object var4) { Debug.Assert(var1 != null && var2 != null && var3 != null && var4 != null); }

		/// <summary>指定された <see cref="System.String"/> 型の変数が <c>null</c> または空でないことを表明します。</summary>
		/// <param name="str"><c>null</c> または空でないことを表明する変数を指定します。</param>
		[Conditional("DEBUG")]
		public static void NotEmpty(string str) { Debug.Assert(!string.IsNullOrEmpty(str)); }

		/// <summary>指定されたシーケンスが <c>null</c> または空でないことを表明します。</summary>
		/// <typeparam name="T">シーケンスの要素型を指定します。</typeparam>
		/// <param name="items">空でないことを表明するシーケンスを指定します。</param>
		[Conditional("DEBUG")]
		public static void NotEmpty<T>(IEnumerable<T> items) { Debug.Assert(items != null && items.Any()); }

		/// <summary>指定されたシーケンスに <c>null</c> である要素が含まれていないことを表明します。</summary>
		/// <typeparam name="T">シーケンスの要素型を指定します。</typeparam>
		/// <param name="items"><c>null</c> である要素が含まれていないことを表明するシーケンスを指定します。</param>
		[Conditional("DEBUG")]
		public static void NotNullItems<T>(IEnumerable<T> items) where T : class
		{
			Debug.Assert(items != null);
			foreach (var item in items)
				Debug.Assert(item != null);
		}
	}
}
