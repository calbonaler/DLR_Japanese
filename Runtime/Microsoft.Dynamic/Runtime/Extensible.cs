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

using Microsoft.Contracts;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>�p���ɂ���Ċg���ł��Ȃ��^�𓮓I�����Ŋg���ł���悤�ɂ��܂��B</summary>
	/// <typeparam name="T">�g������^���w�肵�܂��B</typeparam>
	public class Extensible<T>
	{
		/// <summary><see cref="Microsoft.Scripting.Runtime.Extensible&lt;T&gt;"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		public Extensible() { }

		/// <summary>�w�肳�ꂽ�l���g�p���āA<see cref="Microsoft.Scripting.Runtime.Extensible&lt;T&gt;"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="value">�g������^�̒l���w�肵�܂��B</param>
		public Extensible(T value) { Value = value; }

		/// <summary>���̃I�u�W�F�N�g���g������^�̒l���擾���܂��B</summary>
		public T Value { get; private set; }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�����̃I�u�W�F�N�g�Ɠ��������ǂ����𔻒f���܂��B</summary>
		/// <param name="obj">��r����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�I�u�W�F�N�g�����̃I�u�W�F�N�g�Ɠ������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		[Confined]
		public override bool Equals(object obj) { return Equals(Value, obj); }

		/// <summary>���̃I�u�W�F�N�g�̃n�b�V���l���v�Z���܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̃n�b�V���l�B</returns>
		[Confined]
		public override int GetHashCode() { return Value.GetHashCode(); }

		/// <summary>���̃I�u�W�F�N�g�̕�����\����Ԃ��܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̕�����\���B</returns>
		[Confined]
		public override string ToString() { return Value.ToString(); }

		/// <summary>�w�肳�ꂽ <see cref="Extensible&lt;T&gt;"/> �����ɂȂ�l���擾���܂��B</summary>
		/// <param name="extensible">��ɂȂ�l���擾���� <see cref="Extensible&lt;T&gt;"/>�B</param>
		/// <returns><see cref="Extensible&lt;T&gt;"/> �̊�ɂȂ�l�B</returns>
		public static implicit operator T(Extensible<T> extensible) { return extensible.Value; }
	}
}
