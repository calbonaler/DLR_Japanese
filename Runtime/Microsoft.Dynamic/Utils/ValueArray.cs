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
using System.Linq;
using Microsoft.Contracts;

namespace Microsoft.Scripting.Utils
{
	/// <summary>�z��ɒl�̓������ɂ�铙�l��r�̃T�|�[�g��ǉ����܂��B</summary>
	/// <typeparam name="T">�z��̗v�f�^���w�肵�܂��B</typeparam>
	public class ValueArray<T> : IEquatable<ValueArray<T>>
	{
		readonly T[] _array;

		/// <summary>�w�肳�ꂽ�z����g�p���āA<see cref="Microsoft.Scripting.Utils.ValueArray&lt;T&gt;"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="array">���̃I�u�W�F�N�g�Ń��b�v����z����w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="array"/> �܂��� <paramref name="array"/> �̗v�f�� <c>null</c> �ł��B</exception>
		public ValueArray(T[] array)
		{
			ContractUtils.RequiresNotNull(array, "array");
			ContractUtils.RequiresNotNullItems(array, "array");
			_array = array;
		}

		/// <summary>�w�肳�ꂽ <see cref="ValueArray&lt;T&gt;"/> �����̃I�u�W�F�N�g�Ɠ��������ǂ����𔻒f���܂��B</summary>
		/// <param name="other">���̃I�u�W�F�N�g�Ɠ��������ǂ����𒲂ׂ�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns>���̃I�u�W�F�N�g���w�肳�ꂽ�I�u�W�F�N�g�Ɠ������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		[StateIndependent]
		public bool Equals(ValueArray<T> other) { return other != null && _array.SequenceEqual(other._array); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�����̃I�u�W�F�N�g�Ɠ��������ǂ����𔻒f���܂��B</summary>
		/// <param name="obj">���̃I�u�W�F�N�g�Ɠ��������ǂ����𒲂ׂ�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns>���̃I�u�W�F�N�g���w�肳�ꂽ�I�u�W�F�N�g�Ɠ������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		[Confined]
		public override bool Equals(object obj) { return Equals(obj as ValueArray<T>); }

		/// <summary>���̃I�u�W�F�N�g�̃n�b�V���l���v�Z���܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̃n�b�V���l�B</returns>
		[Confined]
		public override int GetHashCode()
		{
			int val = 6551;
			for (int i = 0; i < _array.Length; i++)
				val ^= _array[i].GetHashCode();
			return val;
		}
	}
}
