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
using Microsoft.Contracts;

namespace Microsoft.Scripting.Actions
{
	/// <summary>�P��� <see cref="Type"/> �ɑΉ����� <see cref="TypeTracker"/> ��\���܂��B</summary>
	public class ReflectedTypeTracker : TypeTracker
	{
		readonly Type _type;

		/// <summary>��ɂȂ�^���g�p���āA<see cref="Microsoft.Scripting.Actions.ReflectedTypeTracker"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="type">��ɂȂ�^��\�� <see cref="Type"/> ���w�肵�܂��B</param>
		public ReflectedTypeTracker(Type type) { _type = type; }

		/// <summary>���݂̌^������q�ɂ��ꂽ�^�̏ꍇ�́A�����錾����^���擾���܂��B</summary>
		public override Type DeclaringType { get { return _type.DeclaringType; } }

		/// <summary><see cref="MemberTracker"/> �̎�ނ��擾���܂��B</summary>
		public override sealed TrackerTypes MemberType { get { return TrackerTypes.Type; } }

		/// <summary>�����o�̖��O���擾���܂��B</summary>
		public override string Name { get { return _type.Name; } }

		/// <summary>���̌^���p�u���b�N�Ƃ��Đ錾����Ă��邩�ǂ����������l���擾���܂��B</summary>
		public override bool IsPublic { get { return _type.IsPublic; } }

		/// <summary>���� <see cref="TypeTracker"/> �ɂ���ĕ\�����^���擾���܂��B</summary>
		public override Type Type { get { return _type; } }

		/// <summary>���̌^���W�F�l���b�N�^���ǂ����������l���擾���܂��B</summary>
		public override bool IsGenericType { get { return _type.IsGenericType; } }

		/// <summary>���̃I�u�W�F�N�g�̕�����\����Ԃ��܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̕�����\���B</returns>
		[Confined]
		public override string ToString() { return _type.ToString(); }
	}
}
