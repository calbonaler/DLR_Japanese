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
	/// <summary>���[�J���ϐ��܂��̓p�����[�^�Ɋւ���f�o�b�O���̏���񋟂��邽�߂Ɏg�p����܂��B</summary>
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

		/// <summary>���̃V���{�����������ɉB����Ă���K�v�����邩�ǂ����������l���擾���܂��B</summary>
		internal bool IsHidden { get; private set; }

		/// <summary>���̕ϐ��̃��t�g���ꂽ�l���Q�Ɠn���܂��� <see cref="System.Runtime.CompilerServices.StrongBox&lt;T&gt;"/> ��ʂ��Č��J����Ă��邩�ǂ����������l���擾���܂��B</summary>
		internal bool IsStrongBoxed { get; private set; }

		/// <summary>�Q�Ɠn���ϐ����X�g�܂��� StrongBox �ϐ����X�g���̃C���f�b�N�X���擾���܂��B</summary>
		internal int LocalIndex { get { Debug.Assert(_localIndex != Int32.MaxValue); return _localIndex; } }

		/// <summary>�������ꂽ���X�g���̃C���f�b�N�X���擾���܂��B</summary>
		internal int GlobalIndex { get { Debug.Assert(_globalIndex != Int32.MaxValue); return _globalIndex; } }

		/// <summary>���̕ϐ��̌^���擾���܂��B</summary>
		internal Type VariableType { get; private set; }

		/// <summary>���̕ϐ��̖��O���擾���܂��B</summary>
		internal string Name { get { return SymbolTable.IdToString(Symbol); } }

		/// <summary>���̃V���{�������[�J���ϐ��܂��͈�����\���Ă��邩�ǂ����������l���擾���܂��B</summary>
		internal bool IsParameter { get; private set; }
	}
}
