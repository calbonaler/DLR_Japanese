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
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Actions
{
	/// <summary>�^�ɒ�`����Ă�����ۂ̃v���p�e�B��\���܂��B</summary>
	public class ReflectedPropertyTracker : PropertyTracker
	{
		/// <summary>��ɂȂ� <see cref="PropertyInfo"/> ���g�p���āA<see cref="Microsoft.Scripting.Actions.ReflectedPropertyTracker"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="property">��ɂȂ�v���p�e�B��\�� <see cref="PropertyInfo"/> ���w�肵�܂��B</param>
		public ReflectedPropertyTracker(PropertyInfo property) { Property = property; }

		/// <summary>�����o�̖��O���擾���܂��B</summary>
		public override string Name { get { return Property.Name; } }

		/// <summary>�����o��_���I�ɐ錾����^���擾���܂��B</summary>
		public override Type DeclaringType { get { return Property.DeclaringType; } }

		/// <summary>���̃v���p�e�B���ÓI�ł��邩�ǂ����������l���擾���܂��B</summary>
		public override bool IsStatic { get { return (GetGetMethod(true) ?? GetSetMethod(true)).IsStatic; } }

		/// <summary>���̃v���p�e�B�̌^���擾���܂��B</summary>
		public override Type PropertyType { get { return Property.PropertyType; } }

		/// <summary>���̃v���p�e�B�̃p�u���b�N�܂��͔�p�u���b�N�� get �A�N�Z�T�[��Ԃ��܂��B</summary>
		/// <param name="privateMembers">��p�u���b�N�� get �A�N�Z�T�[��Ԃ����ǂ����������܂��B��p�u���b�N �A�N�Z�T�[��Ԃ��ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</param>
		/// <returns>
		/// <paramref name="privateMembers"/> �� <c>true</c> �̏ꍇ�́A���̃v���p�e�B�� get �A�N�Z�T�[��\�� <see cref="MethodInfo"/> �I�u�W�F�N�g�B
		/// <paramref name="privateMembers"/> �� <c>false</c> �� get �A�N�Z�T�[����p�u���b�N�̏ꍇ�A�܂��� <paramref name="privateMembers"/> �� <c>true</c> �ł� get �A�N�Z�T�[���Ȃ��ꍇ�́A<c>null</c> ��Ԃ��܂��B
		/// </returns>
		public override MethodInfo GetGetMethod(bool privateMembers) { return Property.GetGetMethod(privateMembers); }

		/// <summary>���̃v���p�e�B�̃p�u���b�N�܂��͔�p�u���b�N�� set �A�N�Z�T�[��Ԃ��܂��B</summary>
		/// <param name="privateMembers">��p�u���b�N�� set �A�N�Z�T�[��Ԃ����ǂ����������܂��B��p�u���b�N �A�N�Z�T�[��Ԃ��ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</param>
		/// <returns>
		/// <paramref name="privateMembers"/> �� <c>true</c> �̏ꍇ�́A���̃v���p�e�B�� set �A�N�Z�T�[��\�� <see cref="MethodInfo"/> �I�u�W�F�N�g�B
		/// <paramref name="privateMembers"/> �� <c>false</c> �� set �A�N�Z�T�[����p�u���b�N�̏ꍇ�A�܂��� <paramref name="privateMembers"/> �� <c>true</c> �ł� set �A�N�Z�T�[���Ȃ��ꍇ�́A<c>null</c> ��Ԃ��܂��B
		/// </returns>
		public override MethodInfo GetSetMethod(bool privateMembers) { return Property.GetSetMethod(privateMembers); }

		/// <summary>���̃v���p�e�B�̃p�u���b�N�܂��͔�p�u���b�N�� delete �A�N�Z�T�[��Ԃ��܂��B</summary>
		/// <param name="privateMembers">��p�u���b�N�� delete �A�N�Z�T�[��Ԃ����ǂ����������܂��B��p�u���b�N �A�N�Z�T�[��Ԃ��ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</param>
		/// <returns>
		/// <paramref name="privateMembers"/> �� <c>true</c> �̏ꍇ�́A���̃v���p�e�B�� delete �A�N�Z�T�[��\�� <see cref="MethodInfo"/> �I�u�W�F�N�g�B
		/// <paramref name="privateMembers"/> �� <c>false</c> �� delete �A�N�Z�T�[����p�u���b�N�̏ꍇ�A�܂��� <paramref name="privateMembers"/> �� <c>true</c> �ł� delete �A�N�Z�T�[���Ȃ��ꍇ�́A<c>null</c> ��Ԃ��܂��B
		/// </returns>
		public override MethodInfo GetDeleteMethod(bool privateMembers)
		{
			var res = Property.DeclaringType.GetMethod("Delete" + Property.Name, (privateMembers ? BindingFlags.NonPublic : 0) | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);
			return res != null && res.IsSpecialName && res.IsDefined(typeof(PropertyMethodAttribute), true) ? res : null;
		}

		/// <summary>�h���N���X�ŃI�[�o�[���C�h���ꂽ�ꍇ�ɁA�v���p�e�B�̂��ׂẴC���f�b�N�X �p�����[�^�̔z���Ԃ��܂��B</summary>
		/// <returns>�C���f�b�N�X�̃p�����[�^�[���i�[���Ă��� <see cref="ParameterInfo"/> �^�̔z��B�v���p�e�B���C���f�b�N�X�t������Ă��Ȃ��ꍇ�A�z��̗v�f�̓[�� (0) �ł��B</returns>
		public override ParameterInfo[] GetIndexParameters() { return Property.GetIndexParameters(); }

		/// <summary>��ɂȂ�v���p�e�B��\�� <see cref="PropertyInfo"/> ���擾���܂��B</summary>
		public PropertyInfo Property { get; private set; }

		/// <summary>���̃I�u�W�F�N�g�̕�����\�����擾���܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̕�����\���B</returns>
		[Confined]
		public override string ToString() { return Property.ToString(); }
	}
}
