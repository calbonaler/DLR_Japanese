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

namespace Microsoft.Scripting.Runtime
{
	/// <summary>例外に関するヘルパー メソッドを格納します。</summary>
	public static class ExceptionHelpers
	{
		const string prevStackTraces = "PreviousStackTraces";

		/// <summary>例外が再スローされる前にスタックトレースを更新します。こうすることで、ユーザーに妥当なスタックトレースを提供することができます。</summary>
		/// <param name="rethrow">再スローされる例外を指定します。</param>
		/// <returns>スタックトレース情報が更新された例外。</returns>
		public static Exception UpdateForRethrow(Exception rethrow)
		{
			List<StackTrace> prev;
			// 動的スタックトレースデータを 1 つも持っていない場合は、生の例外オブジェクトからデータをキャプチャできます
			StackTrace st = new StackTrace(rethrow, true);
			if (!TryGetAssociatedStackTraces(rethrow, out prev))
				AssociateStackTraces(rethrow, prev = new List<StackTrace>());
			prev.Add(st);
			return rethrow;
		}

		/// <summary>指定された例外に関連付けられているすべてのスタックトレースデータを返します。</summary>
		/// <param name="rethrow">スタックトレースデータを取得する例外を指定します。</param>
		/// <returns>例外に関連付けられたスタックトレースデータ。</returns>
		public static IList<StackTrace> GetExceptionStackTraces(Exception rethrow)
		{
			List<StackTrace> result;
			return TryGetAssociatedStackTraces(rethrow, out result) ? result : null;
		}

		static void AssociateStackTraces(Exception e, List<StackTrace> traces) { e.Data[prevStackTraces] = traces; }

		static bool TryGetAssociatedStackTraces(Exception e, out List<StackTrace> traces) { return (traces = e.Data[prevStackTraces] as List<StackTrace>) != null; }
	}
}
