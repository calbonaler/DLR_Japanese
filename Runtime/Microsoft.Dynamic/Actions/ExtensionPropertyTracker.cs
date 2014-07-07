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
using System.Reflection;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Actions
{
	/// <summary>�g���v���p�e�B��\���܂��B</summary>
	public class ExtensionPropertyTracker : PropertyTracker
	{
		string _name;
		Type _declaringType;
		MethodInfo _getter, _setter, _deleter;

		/// <summary>���O�Aget �A�N�Z�T�Aset �A�N�Z�T�Adelete �A�N�Z�T����ѐ錾����^���g�p���āA<see cref="Microsoft.Scripting.Actions.ExtensionPropertyTracker"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="name">�g���v���p�e�B�̖��O���w�肵�܂��B</param>
		/// <param name="getter">get �A�N�Z�T��\�� <see cref="MethodInfo"/> ���w�肵�܂��B</param>
		/// <param name="setter">set �A�N�Z�T��\�� <see cref="MethodInfo"/> ���w�肵�܂��B</param>
		/// <param name="deleter">delete �A�N�Z�T��\�� <see cref="MethodInfo"/> ���w�肵�܂��B</param>
		/// <param name="declaringType">�g���v���p�e�B��錾����^���w�肵�܂��B���̒l�͊g���v���p�e�B���g������^�Ɠ������Ȃ�܂��B</param>
		public ExtensionPropertyTracker(string name, MethodInfo getter, MethodInfo setter, MethodInfo deleter, Type declaringType)
		{
			_name = name;
			_getter = getter;
			_setter = setter;
			_deleter = deleter;
			_declaringType = declaringType;
		}

		/// <summary>�����o�̖��O���擾���܂��B</summary>
		public override string Name { get { return _name; } }

		/// <summary>�����o��_���I�ɐ錾����^���擾���܂��B</summary>
		public override Type DeclaringType { get { return _declaringType; } }

		/// <summary>���̃v���p�e�B���ÓI�ł��邩�ǂ����������l���擾���܂��B</summary>
		public override bool IsStatic { get { return (GetGetMethod(true) ?? GetSetMethod(true)).IsDefined(typeof(StaticExtensionMethodAttribute), false); } }

		/// <summary>���̃v���p�e�B�̃p�u���b�N�܂��͔�p�u���b�N�� get �A�N�Z�T�[��Ԃ��܂��B</summary>
		/// <param name="privateMembers">��p�u���b�N�� get �A�N�Z�T�[��Ԃ����ǂ����������܂��B��p�u���b�N �A�N�Z�T�[��Ԃ��ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</param>
		/// <returns>
		/// <paramref name="privateMembers"/> �� <c>true</c> �̏ꍇ�́A���̃v���p�e�B�� get �A�N�Z�T�[��\�� <see cref="MethodInfo"/> �I�u�W�F�N�g�B
		/// <paramref name="privateMembers"/> �� <c>false</c> �� get �A�N�Z�T�[����p�u���b�N�̏ꍇ�A�܂��� <paramref name="privateMembers"/> �� <c>true</c> �ł� get �A�N�Z�T�[���Ȃ��ꍇ�́A<c>null</c> ��Ԃ��܂��B
		/// </returns>
		public override MethodInfo GetGetMethod(bool privateMembers) { return privateMembers || _getter == null || !_getter.IsPrivate ? _getter : null; }

		/// <summary>���̃v���p�e�B�̃p�u���b�N�܂��͔�p�u���b�N�� set �A�N�Z�T�[��Ԃ��܂��B</summary>
		/// <param name="privateMembers">��p�u���b�N�� set �A�N�Z�T�[��Ԃ����ǂ����������܂��B��p�u���b�N �A�N�Z�T�[��Ԃ��ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</param>
		/// <returns>
		/// <paramref name="privateMembers"/> �� <c>true</c> �̏ꍇ�́A���̃v���p�e�B�� set �A�N�Z�T�[��\�� <see cref="MethodInfo"/> �I�u�W�F�N�g�B
		/// <paramref name="privateMembers"/> �� <c>false</c> �� set �A�N�Z�T�[����p�u���b�N�̏ꍇ�A�܂��� <paramref name="privateMembers"/> �� <c>true</c> �ł� set �A�N�Z�T�[���Ȃ��ꍇ�́A<c>null</c> ��Ԃ��܂��B
		/// </returns>
		public override MethodInfo GetSetMethod(bool privateMembers) { return privateMembers || _setter == null || !_setter.IsPrivate ? _setter : null; }

		/// <summary>���̃v���p�e�B�̃p�u���b�N�܂��͔�p�u���b�N�� delete �A�N�Z�T�[��Ԃ��܂��B</summary>
		/// <param name="privateMembers">��p�u���b�N�� delete �A�N�Z�T�[��Ԃ����ǂ����������܂��B��p�u���b�N �A�N�Z�T�[��Ԃ��ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</param>
		/// <returns>
		/// <paramref name="privateMembers"/> �� <c>true</c> �̏ꍇ�́A���̃v���p�e�B�� delete �A�N�Z�T�[��\�� <see cref="MethodInfo"/> �I�u�W�F�N�g�B
		/// <paramref name="privateMembers"/> �� <c>false</c> �� delete �A�N�Z�T�[����p�u���b�N�̏ꍇ�A�܂��� <paramref name="privateMembers"/> �� <c>true</c> �ł� delete �A�N�Z�T�[���Ȃ��ꍇ�́A<c>null</c> ��Ԃ��܂��B
		/// </returns>
		public override MethodInfo GetDeleteMethod(bool privateMembers) { return privateMembers || _deleter == null || !_deleter.IsPrivate ? _deleter : null; }

		/// <summary>�v���p�e�B�̂��ׂẴC���f�b�N�X �p�����[�^�̔z���Ԃ��܂��B</summary>
		/// <returns>�C���f�b�N�X�̃p�����[�^�[���i�[���Ă��� <see cref="ParameterInfo"/> �^�̔z��B�v���p�e�B���C���f�b�N�X�t������Ă��Ȃ��ꍇ�A�z��̗v�f�̓[�� (0) �ł��B</returns>
		public override ParameterInfo[] GetIndexParameters() { return new ParameterInfo[0]; }

		/// <summary>���̃v���p�e�B�̌^���擾���܂��B</summary>
		public override Type PropertyType { get { return _getter != null ? _getter.ReturnType : _setter.GetParameters().Last().ParameterType; } }
	}
}
