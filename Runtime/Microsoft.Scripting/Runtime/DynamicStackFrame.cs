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
using System.Reflection;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>スタックフレームに関する情報を格納します。</summary>
	[Serializable]
	public class DynamicStackFrame
	{
		/// <summary>メソッド、メソッド名、ファイル名、行番号を使用して、<see cref="Microsoft.Scripting.Runtime.DynamicStackFrame"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="method">スタックフレームが表すメソッドを指定します。</param>
		/// <param name="methodName">スタックフレームが表すメソッド名を指定します。</param>
		/// <param name="filename">スタックフレームが表すファイル名を指定します。</param>
		/// <param name="line">スタックフレームが表す行番号を指定します。</param>
		public DynamicStackFrame(MethodBase method, string methodName, string filename, int line)
		{
			MethodName = methodName;
			FileName = filename;
			FileLineNumber = line;
			Method = method;
		}

		/// <summary>スタックフレームが表すメソッドを取得します。</summary>
		public MethodBase Method { get; private set; }

		/// <summary>スタックフレームが表すメソッド名を取得します。</summary>
		public string MethodName { get; private set; }

		/// <summary>スタックフレームが表すファイル名を取得します。</summary>
		public string FileName { get; private set; }

		/// <summary>スタックフレームが表すファイルの行番号を取得します。</summary>
		public int FileLineNumber { get; private set; }

		/// <summary>このスタックフレームの文字列表現を取得します。</summary>
		public override string ToString()
		{
			return string.Format(
				"{0} in {1}:{2}, {3}",
				MethodName ?? "<function unknown>",
				FileName ?? "<filename unknown>",
				FileLineNumber,
				(Method != null ? Method.ToString() : "<method unknown>")
			);
		}
	}
}
