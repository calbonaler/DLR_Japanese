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
	/// <summary>�V�X�e����̂��ׂĂ� <see cref="SymbolId"/> �̋��ʂ̃e�[�u����񋟂��܂��B</summary>
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

		/// <summary>�w�肳�ꂽ������ɑ΂��� <see cref="SymbolId"/> ���擾���܂��B</summary>
		/// <param name="value">�擾���� <see cref="SymbolId"/> ���\����������w�肵�܂��B</param>
		/// <returns>������ɑ΂��� <see cref="SymbolId"/>�B</returns>
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

		/// <summary>�w�肳�ꂽ <see cref="SymbolId"/> ���\����������擾���܂��B</summary>
		/// <param name="id">�擾���镶�����\�� <see cref="SymbolId"/> ���w�肵�܂��B</param>
		/// <returns><see cref="SymbolId"/> �ɑ΂��镶����B</returns>
		public static string IdToString(SymbolId id)
		{
			lock (_fieldDict)
				return _fieldDict[id.Id | (id.IsCaseInsensitive ? CaseVersionIncrement : 0)];
		}

		// Tries to lookup the SymbolId to see if it is valid
		/// <summary>�w�肳�ꂽ <see cref="SymbolId"/> ���������\���Ă��邩�ǂ����������l���擾���܂��B</summary>
		/// <param name="id">�L�����ǂ������m�F���� <see cref="SymbolId"/> ���w�肵�܂��B</param>
		/// <returns><see cref="SymbolId"/> ���L���Ȃ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool ContainsId(SymbolId id)
		{
			lock (_fieldDict)
				return _fieldDict.ContainsKey(id.Id | (id.IsCaseInsensitive ? CaseVersionIncrement : 0));
		}

		/// <summary>�w�肳�ꂽ�������\�� <see cref="SymbolId"/> �����݂��邩�ǂ����������l���擾���܂��B</summary>
		/// <param name="symbol"><see cref="SymbolId"/> �̑��݂��m�F���镶������w�肵�܂��B</param>
		/// <returns>������� <see cref="SymbolId"/> �����݂���� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool StringHasId(string symbol)
		{
			ContractUtils.RequiresNotNull(symbol, "symbol");
			lock (_lockObj)
				return _idDict.ContainsKey(symbol);
		}

		/// <summary>�w�肳�ꂽ������ɑ΂��� <see cref="SymbolId"/> ���擾���܂��B������ <c>null</c> �̏ꍇ�� <see cref="SymbolId.Empty"/> ���Ԃ���܂��B</summary>
		/// <param name="value">�擾���� <see cref="SymbolId"/> ���\����������w�肵�܂��B</param>
		/// <returns>������ɑ΂��� <see cref="SymbolId"/>�B</returns>
		public static SymbolId StringToIdOrEmpty(string value) { return value == null ? SymbolId.Empty : StringToId(value); }
	}
}
