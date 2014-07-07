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
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting
{
	/// <summary>システム上のすべての <see cref="SymbolId"/> の共通のテーブルを提供します。</summary>
	public static class SymbolTable
	{
		static readonly object _lockObj = new object();
		
		const int InitialTableSize = 256;
		internal const int CaseVersionMask = unchecked((int)0xFF000000);
		const int CaseVersionIncrement = 0x01000000;
		
		static int _nextCaseInsensitiveId = 1;

		static readonly Dictionary<string, int> _idDict = new Dictionary<string, int>(InitialTableSize);
		static readonly Dictionary<string, int> _invariantDict = new Dictionary<string, int>(InitialTableSize, StringComparer.OrdinalIgnoreCase);
		static readonly Dictionary<int, string> _fieldDict = new Dictionary<int, string>(InitialTableSize) { { 0, null } };

		/// <summary>指定された文字列に対する <see cref="SymbolId"/> を取得します。</summary>
		/// <param name="value">取得する <see cref="SymbolId"/> が表す文字列を指定します。</param>
		/// <returns>文字列に対する <see cref="SymbolId"/>。</returns>
		public static SymbolId StringToId(string value)
		{
			ContractUtils.RequiresNotNull(value, "value");
			int res;
			lock (_lockObj)
			{
				// First, look up the identifier case-sensitively.
				if (!_idDict.TryGetValue(value, out res))
				{
					// OK, didn't find it, so let's look up the case-insensitive identifier.
					if (!_invariantDict.TryGetValue(value, out res))
					{
						// This is a whole new identifier.
						if (_nextCaseInsensitiveId == ~CaseVersionMask)
							throw Error.CantAddIdentifier(value);
						// allocate new ID at case version 1.
						res = _nextCaseInsensitiveId++ | CaseVersionIncrement;
					}
					else
					{
						// OK, this is a new casing of an existing identifier.
						// Throw if we've exhausted the number of casings.
						if (unchecked(((uint)res & CaseVersionMask) == CaseVersionMask))
							throw Error.CantAddCasing(value);
						// bump the case version
						res += CaseVersionIncrement;
					}
					// update the tables with the IDs
					_invariantDict[value] = res;
					_idDict[value] = res;
					_fieldDict[res] = value;
				}
			}
			return new SymbolId(res);
		}

		/// <summary>指定された <see cref="SymbolId"/> が表す文字列を取得します。</summary>
		/// <param name="id">取得する文字列を表す <see cref="SymbolId"/> を指定します。</param>
		/// <returns><see cref="SymbolId"/> に対する文字列。</returns>
		public static string IdToString(SymbolId id)
		{
			lock (_fieldDict)
				return _fieldDict[id.Id | (id.IsCaseInsensitive ? CaseVersionIncrement : 0)];
		}

		// Tries to lookup the SymbolId to see if it is valid
		/// <summary>指定された <see cref="SymbolId"/> が文字列を表しているかどうかを示す値を取得します。</summary>
		/// <param name="id">有効かどうかを確認する <see cref="SymbolId"/> を指定します。</param>
		/// <returns><see cref="SymbolId"/> が有効ならば <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool ContainsId(SymbolId id)
		{
			lock (_fieldDict)
				return _fieldDict.ContainsKey(id.Id | (id.IsCaseInsensitive ? CaseVersionIncrement : 0));
		}

		/// <summary>指定された文字列を表す <see cref="SymbolId"/> が存在するかどうかを示す値を取得します。</summary>
		/// <param name="symbol"><see cref="SymbolId"/> の存在を確認する文字列を指定します。</param>
		/// <returns>文字列に <see cref="SymbolId"/> が存在すれば <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool StringHasId(string symbol)
		{
			ContractUtils.RequiresNotNull(symbol, "symbol");
			lock (_lockObj)
				return _idDict.ContainsKey(symbol);
		}

		/// <summary>指定された文字列に対する <see cref="SymbolId"/> を取得します。文字列が <c>null</c> の場合は <see cref="SymbolId.Empty"/> が返されます。</summary>
		/// <param name="value">取得する <see cref="SymbolId"/> が表す文字列を指定します。</param>
		/// <returns>文字列に対する <see cref="SymbolId"/>。</returns>
		public static SymbolId StringToIdOrEmpty(string value) { return value == null ? SymbolId.Empty : StringToId(value); }
	}
}
