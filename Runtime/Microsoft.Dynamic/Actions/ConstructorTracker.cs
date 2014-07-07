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
using System.Reflection;
using Microsoft.Contracts;

namespace Microsoft.Scripting.Actions
{
	/// <summary>�R���X�g���N�^��\���܂��B</summary>
	public class ConstructorTracker : MemberTracker
	{
		ConstructorInfo _ctor;

		/// <summary>�w�肳�ꂽ <see cref="ConstructorInfo"/> ���g�p���āA<see cref="Microsoft.Scripting.Actions.ConstructorTracker"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="ctor">���̃g���b�J�[���\���R���X�g���N�^���w�肵�܂��B</param>
		public ConstructorTracker(ConstructorInfo ctor) { _ctor = ctor; }

		/// <summary>�����o��_���I�ɐ錾����^���擾���܂��B</summary>
		public override Type DeclaringType { get { return _ctor.DeclaringType; } }

		/// <summary><see cref="MemberTracker"/> �̎�ނ��擾���܂��B</summary>
		public sealed override TrackerTypes MemberType { get { return TrackerTypes.Constructor; } }

		/// <summary>�����o�̖��O���擾���܂��B</summary>
		public override string Name { get { return _ctor.Name; } }

		/// <summary>���̃R���X�g���N�^���p�u���b�N���ǂ����������l���擾���܂��B</summary>
		public bool IsPublic { get { return _ctor.IsPublic; } }

		/// <summary>���̃I�u�W�F�N�g�̕�����\����Ԃ��܂��B</summary>
		/// <returns>���̃I�u�W�F�N�g�̕�����\���B</returns>
		[Confined]
		public override string ToString() { return _ctor.ToString(); }
	}
}
