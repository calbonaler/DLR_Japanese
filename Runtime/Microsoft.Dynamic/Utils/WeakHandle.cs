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
using System.Runtime.InteropServices;

namespace Microsoft.Scripting.Utils
{
	/// <summary>�����I�ɉ������Ȃ� "�ア�Q��" ��\���܂��B</summary>
	public struct WeakHandle : IEquatable<WeakHandle>
	{
		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g���Q�Ƃ��A�w�肳�ꂽ�����̒ǐՂ��g�p���� <see cref="WeakHandle"/> �\���̂̐V�����C���X�^���X�����������܂��B </summary>
		/// <param name="target">�Q�Ƃ���I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="trackResurrection">�I������I�u�W�F�N�g���Q�Ƃ��邩�ǂ����������l���w�肵�܂��B</param>
		public WeakHandle(object target, bool trackResurrection) { weakRef = GCHandle.Alloc(target, trackResurrection ? GCHandleType.WeakTrackResurrection : GCHandleType.Weak); }
		
		GCHandle weakRef;

		/// <summary>���݂� <see cref="WeakHandle"/> �I�u�W�F�N�g���Q�Ƃ���I�u�W�F�N�g���A�K�x�[�W �R���N�V�����Ŏ��W����Ă��邩�ǂ����������l���擾���܂��B</summary>
		public bool IsAlive { get { return weakRef.IsAllocated; } }

		/// <summary>���݂� <see cref="WeakHandle"/> �I�u�W�F�N�g���Q�Ƃ���I�u�W�F�N�g (�^�[�Q�b�g) ���擾���܂��B </summary>
		public object Target { get { return weakRef.Target; } }
		
		/// <summary><see cref="WeakHandle"/> �I�u�W�F�N�g��������܂��B</summary>
		public void Free() { weakRef.Free(); }

		/// <summary>���݂� <see cref="WeakHandle"/> �I�u�W�F�N�g�̎��ʎq��Ԃ��܂��B</summary>
		/// <returns>���݂� <see cref="WeakHandle"/> �I�u�W�F�N�g�̎��ʎq�B</returns>
		public override int GetHashCode() { return weakRef.GetHashCode(); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�����݂̃I�u�W�F�N�g�Ɠ��������ǂ����𔻒f���܂��B</summary>
		/// <param name="obj">���������ǂ����𒲂ׂ�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns>���݂̃I�u�W�F�N�g���w�肳�ꂽ�I�u�W�F�N�g�Ɠ������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public override bool Equals(object obj) { return obj is WeakHandle && Equals((WeakHandle)obj); }

		/// <summary>�w�肵�� <see cref="WeakHandle"/> �I�u�W�F�N�g���A���݂� <see cref="WeakHandle"/> �I�u�W�F�N�g�Ɠ��������ǂ����𔻒f���܂��B</summary>
		/// <param name="other">���݂� <see cref="WeakHandle"/> �I�u�W�F�N�g�Ɣ�r���� <see cref="WeakHandle"/> �I�u�W�F�N�g�B</param>
		/// <returns>�w�肵�� <see cref="WeakHandle"/> �I�u�W�F�N�g�����݂� <see cref="WeakHandle"/> �I�u�W�F�N�g�Ɠ������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public bool Equals(WeakHandle other) { return weakRef.Equals(other.weakRef); }

		/// <summary><see cref="WeakHandle"/> �� 2 �̃I�u�W�F�N�g�����������ǂ����������l��Ԃ��܂��B</summary>
		/// <param name="left"><paramref name="right"/> �p�����[�^�[�Ɣ�r���� <see cref="WeakHandle"/> �I�u�W�F�N�g�B</param>
		/// <param name="right"><paramref name="left"/> �p�����[�^�[�Ɣ�r���� <see cref="WeakHandle"/> �I�u�W�F�N�g�B</param>
		/// <returns><paramref name="left"/> �p�����[�^�[�� <paramref name="right"/> �p�����[�^�[���������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool operator ==(WeakHandle left, WeakHandle right) { return left.Equals(right); }

		/// <summary><see cref="WeakHandle"/> �� 2 �̃I�u�W�F�N�g���������Ȃ����ǂ����������l��Ԃ��܂��B</summary>
		/// <param name="left"><paramref name="right"/> �p�����[�^�[�Ɣ�r���� <see cref="WeakHandle"/> �I�u�W�F�N�g�B</param>
		/// <param name="right"><paramref name="left"/> �p�����[�^�[�Ɣ�r���� <see cref="WeakHandle"/> �I�u�W�F�N�g�B</param>
		/// <returns><paramref name="left"/> �p�����[�^�[�� <paramref name="right"/> �p�����[�^�[���������Ȃ��ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool operator !=(WeakHandle left, WeakHandle right) { return !(left == right); }
	}
}
