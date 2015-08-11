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
using System.Diagnostics;

namespace Microsoft.Scripting.Debugging
{
	/// <summary>ローカル変数またはパラメータに関するデバッグ時の情報を提供するために使用されます。</summary>
	sealed class VariableInfo
	{
		int _localIndex;
		int _globalIndex;

		internal VariableInfo(SymbolId symbol, Type type, bool parameter, bool hidden, bool strongBoxed, int localIndex, int globalIndex)
		{
			Symbol = symbol;
			VariableType = type;
			IsParameter = parameter;
			IsHidden = hidden;
			IsStrongBoxed = strongBoxed;
			_localIndex = localIndex;
			_globalIndex = globalIndex;
		}

		internal VariableInfo(SymbolId symbol, Type type, bool parameter, bool hidden, bool strongBoxed) : this(symbol, type, parameter, hidden, strongBoxed, Int32.MaxValue, Int32.MaxValue)
		{
			Symbol = symbol;
			VariableType = type;
			IsParameter = parameter;
			IsHidden = hidden;
			IsStrongBoxed = strongBoxed;
		}

		internal SymbolId Symbol { get; private set; }

		/// <summary>このシンボルが調査中に隠されている必要があるかどうかを示す値を取得します。</summary>
		internal bool IsHidden { get; private set; }

		/// <summary>この変数のリフトされた値が参照渡しまたは <see cref="System.Runtime.CompilerServices.StrongBox&lt;T&gt;"/> を通して公開されているかどうかを示す値を取得します。</summary>
		internal bool IsStrongBoxed { get; private set; }

		/// <summary>参照渡し変数リストまたは StrongBox 変数リスト内のインデックスを取得します。</summary>
		internal int LocalIndex { get { Debug.Assert(_localIndex != Int32.MaxValue); return _localIndex; } }

		/// <summary>結合されたリスト内のインデックスを取得します。</summary>
		internal int GlobalIndex { get { Debug.Assert(_globalIndex != Int32.MaxValue); return _globalIndex; } }

		/// <summary>この変数の型を取得します。</summary>
		internal Type VariableType { get; private set; }

		/// <summary>この変数の名前を取得します。</summary>
		internal string Name { get { return SymbolTable.IdToString(Symbol); } }

		/// <summary>このシンボルがローカル変数または引数を表しているかどうかを示す値を取得します。</summary>
		internal bool IsParameter { get; private set; }
	}
}
