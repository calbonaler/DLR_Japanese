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
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>スコープ内のローカル変数のディクショナリを表します。</summary>
	public sealed class LocalsDictionary : CustomSymbolDictionary
	{
		readonly IRuntimeVariables _locals;
		readonly ReadOnlyCollection<SymbolId> _symbols;
		Dictionary<SymbolId, int> _boxes;

		/// <summary>指定されたランタイム変数と名前を使用して、<see cref="Microsoft.Scripting.Runtime.LocalsDictionary"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="locals">ローカル変数の値を格納するランタイム変数を指定します。</param>
		/// <param name="symbols">ローカル変数の名前を表す <see cref="SymbolId"/> の配列を指定します。</param>
		public LocalsDictionary(IRuntimeVariables locals, IEnumerable<SymbolId> symbols)
		{
			Assert.NotNull(locals, symbols);
			_locals = locals;
			_symbols = symbols.ToReadOnly();
		}

		void EnsureBoxes()
		{
			if (_boxes == null)
				_boxes = _symbols.Select((x, i) => Tuple.Create(x, i)).ToDictionary(x => x.Item1, x => x.Item2);
		}

		/// <summary>モジュールの最適化された実装によってキャッシュされる追加のキーを取得します。</summary>
		/// <returns>追加のキーを表す <see cref="SymbolId"/> の配列。</returns>
		protected override ReadOnlyCollection<SymbolId> ExtraKeys { get { return _symbols; } }

		/// <summary>追加の値の設定を試み、指定されたキーに対する値が正常に設定されたかどうかを示す値を返します。</summary>
		/// <param name="key">設定する値に対するキーを指定します。</param>
		/// <param name="value">設定する値を指定します。</param>
		/// <returns>指定されたキーに対して値が正常に設定された場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		protected override bool TrySetExtraValue(SymbolId key, object value)
		{
			EnsureBoxes();
			int index;
			if (_boxes.TryGetValue(key, out index))
			{
				_locals[index] = value;
				return true;
			}
			return false;
		}

		/// <summary>追加の値の取得を試み、指定されたキーに対する値が正常に取得されたかどうかを示す値を返します。値が <see cref="Uninitialized"/> であっても <c>true</c> を返します。</summary>
		/// <param name="key">取得する値に対するキーを指定します。</param>
		/// <param name="value">取得された値が格納されます。</param>
		/// <returns>指定されたキーに対して値が正常に取得された場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		protected override bool TryGetExtraValue(SymbolId key, out object value)
		{
			EnsureBoxes();
			int index;
			if (_boxes.TryGetValue(key, out index))
			{
				value = _locals[index];
				return true;
			}
			value = null;
			return false;
		}
	}
}
