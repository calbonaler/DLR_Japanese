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

namespace Microsoft.Scripting.Runtime
{
	/// <summary>���W���[���̓��e���ύX���ꂽ�ꍇ�ɔ�������C�x���g�̃f�[�^��\���܂��B</summary>
	public class ModuleChangeEventArgs : EventArgs
	{
		/// <summary>�w�肳�ꂽ���O����ь^���g�p���āA<see cref="Microsoft.Scripting.Runtime.ModuleChangeEventArgs"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="name">�ύX�����������V���{���̖��O���w�肵�܂��B</param>
		/// <param name="changeType">���W���[���ɔ��������ύX������ <see cref="ModuleChangeType"/> ���w�肵�܂��B</param>
		public ModuleChangeEventArgs(string name, ModuleChangeType changeType)
		{
			Name = name;
			ChangeType = changeType;
		}

		/// <summary>�w�肳�ꂽ���O�A�^����ѕύX���ꂽ�l���g�p���āA<see cref="Microsoft.Scripting.Runtime.ModuleChangeEventArgs"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="name">�ύX�����������V���{���̖��O���w�肵�܂��B</param>
		/// <param name="changeType">���W���[���ɔ��������ύX������ <see cref="ModuleChangeType"/> ���w�肵�܂��B</param>
		/// <param name="value">�V���{���ɐV�����ݒ肳�ꂽ�l���w�肵�܂��B</param>
		public ModuleChangeEventArgs(string name, ModuleChangeType changeType, object value)
		{
			Name = name;
			ChangeType = changeType;
			Value = value;
		}

		/// <summary>�ύX�����������V���{���̖��O���w�肵�܂��B</summary>
		public string Name { get; private set; }

		/// <summary>�V���{�����ǂ̂悤�ɕύX���ꂽ�������� <see cref="ModuleChangeType"/> ���擾���܂��B</summary>
		public ModuleChangeType ChangeType { get; private set; }

		/// <summary>�V���{���ɐV�����ݒ肳�ꂽ�l���擾���܂��B</summary>
		public object Value { get; private set; }
	}
}
