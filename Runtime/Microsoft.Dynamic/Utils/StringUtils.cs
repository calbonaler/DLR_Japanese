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
using System.Text;

namespace Microsoft.Scripting.Utils
{
	/// <summary>文字列に関するユーティリティ メソッドを公開します。</summary>
	public static class StringUtils
	{
		/// <summary>指定されたテキストを指定された幅で単語単位で自動的に改行し、全体をインデントします。</summary>
		/// <param name="text">単語単位で自動改行してインデントを行うテキストを指定します。</param>
		/// <param name="indentFirst">テキストの先頭行をインデントするかどうかを示す値を指定します。</param>
		/// <param name="lineWidth">テキストの自動改行の幅を示す文字数を指定します。</param>
		/// <returns>指定された幅で自動的に改行され全体がインデントされた文字列。</returns>
		public static string SplitWords(string text, bool indentFirst, int lineWidth)
		{
			ContractUtils.RequiresNotNull(text, "text");
			const string indent = "    ";
			if (text.Length <= lineWidth || lineWidth <= 0)
				return indentFirst ? indent + text : text;
			StringBuilder res = new StringBuilder();
			for (int start = 0, length = lineWidth; start < text.Length; length = Math.Min(text.Length - start, lineWidth))
			{
				if (length >= lineWidth)
				{
					// find last space to break on
					while (length > 0 && !char.IsWhiteSpace(text[start + length - 1]))
						length--;
				}
				if (indentFirst || res.Length > 0)
					res.Append(indent);
				var copying = length > 0 ? length : Math.Min(text.Length - start, lineWidth);
				res.Append(text, start, copying);
				start += copying;
				res.AppendLine();
			}
			return res.ToString();
		}

		/// <summary>指定されたテキストに含まれる先頭にバックスラッシュが付加されるような制御文字をエスケープします。</summary>
		/// <param name="value">制御文字をエスケープするテキストを指定します。</param>
		/// <returns>制御文字がエスケープされたテキスト。例えば CR は "\r" にエスケープされます。</returns>
		public static string AddSlashes(string value)
		{
			ContractUtils.RequiresNotNull(value, "value");
			// TODO: optimize
			StringBuilder result = new StringBuilder(value.Length);
			for (int i = 0; i < value.Length; i++)
			{
				switch (value[i])
				{
					case '\a': result.Append("\\a"); break;
					case '\b': result.Append("\\b"); break;
					case '\f': result.Append("\\f"); break;
					case '\n': result.Append("\\n"); break;
					case '\r': result.Append("\\r"); break;
					case '\t': result.Append("\\t"); break;
					case '\v': result.Append("\\v"); break;
					default: result.Append(value[i]); break;
				}
			}
			return result.ToString();
		}
	}
}
