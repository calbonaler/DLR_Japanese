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
	/// <summary>�X�R�[�v���̃��[�J���ϐ��̃f�B�N�V���i����\���܂��B</summary>
	public sealed class LocalsDictionary : CustomSymbolDictionary
	{
		readonly IRuntimeVariables _locals;
		readonly ReadOnlyCollection<SymbolId> _symbols;
		Dictionary<SymbolId, int> _boxes;

		/// <summary>�w�肳�ꂽ�����^�C���ϐ��Ɩ��O���g�p���āA<see cref="Microsoft.Scripting.Runtime.LocalsDictionary"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="locals">���[�J���ϐ��̒l���i�[���郉���^�C���ϐ����w�肵�܂��B</param>
		/// <param name="symbols">���[�J���ϐ��̖��O��\�� <see cref="SymbolId"/> �̔z����w�肵�܂��B</param>
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

		/// <summary>���W���[���̍œK�����ꂽ�����ɂ���ăL���b�V�������ǉ��̃L�[���擾���܂��B</summary>
		/// <returns>�ǉ��̃L�[��\�� <see cref="SymbolId"/> �̔z��B</returns>
		protected override ReadOnlyCollection<SymbolId> ExtraKeys { get { return _symbols; } }

		/// <summary>�ǉ��̒l�̐ݒ�����݁A�w�肳�ꂽ�L�[�ɑ΂���l������ɐݒ肳�ꂽ���ǂ����������l��Ԃ��܂��B</summary>
		/// <param name="key">�ݒ肷��l�ɑ΂���L�[���w�肵�܂��B</param>
		/// <param name="value">�ݒ肷��l���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�L�[�ɑ΂��Ēl������ɐݒ肳�ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
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

		/// <summary>�ǉ��̒l�̎擾�����݁A�w�肳�ꂽ�L�[�ɑ΂���l������Ɏ擾���ꂽ���ǂ����������l��Ԃ��܂��B�l�� <see cref="Uninitialized"/> �ł����Ă� <c>true</c> ��Ԃ��܂��B</summary>
		/// <param name="key">�擾����l�ɑ΂���L�[���w�肵�܂��B</param>
		/// <param name="value">�擾���ꂽ�l���i�[����܂��B</param>
		/// <returns>�w�肳�ꂽ�L�[�ɑ΂��Ēl������Ɏ擾���ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
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
