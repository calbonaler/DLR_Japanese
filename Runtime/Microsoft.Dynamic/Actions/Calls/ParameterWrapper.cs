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
using System.Diagnostics;
using System.Reflection;
using System.Dynamic;
using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Utils;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>�������̃o�C���f�B���O�Ɋւ������\���܂��B</summary>
	[Flags]
	public enum ParameterBindingFlags
	{
		/// <summary>�ǉ����͂���܂���B</summary>
		None = 0,
		/// <summary>�������� <c>null</c> �����ۂ��܂��B</summary>
		ProhibitNull = 1,
		/// <summary>�������� <c>null</c> �v�f�����ۂ��܂��B</summary>
		ProhibitNullItems = 2,
		/// <summary>�������͔z������ł��B</summary>
		IsParamArray = 4,
		/// <summary>�������͎��������ł��B</summary>
		IsParamDictionary = 8,
		/// <summary>�������͉B�������ł��B</summary>
		IsHidden = 16,
	}

	/// <summary>
	/// �������̘_���r���[��\���܂��B
	/// �Ⴆ�΁A�Q�Ɠn���k���V�O�l�`���̘_���r���[�͈������l�n������ (����ɁA�X�V�l�͖߂�l�Ɋ܂߂��) �邱�Ƃł��邽�߁A
	/// �Q�Ɠn�������̂��郁�\�b�h�̎Q�Ɠn���k���V�O�l�`���͊�ɂȂ�v�f�^�� <see cref="ParameterWrapper"/> ��p���ĕ\����܂��B
	/// ���̃N���X�̓��\�b�h�Ɏ��ۂɓn���ꂽ������������\������ <see cref="ArgBuilder"/> �Ƃ͑ΏƓI�ł��B
	/// </summary>
	public sealed class ParameterWrapper
	{
		/// <summary>�������̃��^�f�[�^�A�^�A���O�A�o�C���f�B���O�����g�p���āA<see cref="Microsoft.Scripting.Actions.Calls.ParameterWrapper"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="info">�������̃��^�f�[�^��\�� <see cref="ParameterInfo"/> ���w�肵�܂��B</param>
		/// <param name="type">�������̌^���w�肵�܂��B</param>
		/// <param name="name">�������̖��O���w�肵�܂��B</param>
		/// <param name="flags">�������̃o�C���f�B���O�����w�肵�܂��B</param>
		public ParameterWrapper(ParameterInfo info, Type type, string name, ParameterBindingFlags flags)
		{
			ContractUtils.RequiresNotNull(type, "type");
			Type = type;
			ParameterInfo = info;
			Flags = flags;
			// params arrays & dictionaries don't allow assignment by keyword
			Name = IsParamArray || IsParamDict || name == null ? "<unknown>" : name;
		}

		/// <summary>���� <see cref="ParameterWrapper"/> �̖��O���w�肳�ꂽ���O�ɒu���������V���� <see cref="ParameterWrapper"/> ���쐬���܂��B</summary>
		/// <param name="name">�쐬����� <see cref="ParameterWrapper"/> �̖��O���w�肵�܂��B</param>
		/// <returns>���� <see cref="ParameterWrapper"/> �̖��O���w�肳�ꂽ���O�ɒu���������V���� <see cref="ParameterWrapper"/>�B</returns>
		public ParameterWrapper Clone(string name) { return new ParameterWrapper(ParameterInfo, Type, name, Flags); }

		/// <summary>���̉������̌^���擾���܂��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
		public Type Type { get; private set; }

		/// <summary>���̉������̃��^�f�[�^��\�� <see cref="ParameterInfo"/> ���擾���܂��B����� <c>null</c> �ł���\��������܂��B</summary>
		public ParameterInfo ParameterInfo { get; private set; }

		/// <summary>���̉������̖��O���擾���܂��B</summary>
		public string Name { get; private set; }

		/// <summary>���̉������̃o�C���f�B���O�����擾���܂��B</summary>
		public ParameterBindingFlags Flags { get; private set; }

		/// <summary>���̉������� <c>null</c> �����ۂ��邩�ǂ����������l���擾���܂��B</summary>
		public bool ProhibitNull { get { return (Flags & ParameterBindingFlags.ProhibitNull) != 0; } }

		/// <summary>���̉������� <c>null</c> �v�f�����ۂ��邩�ǂ����������l���擾���܂��B</summary>
		public bool ProhibitNullItems { get { return (Flags & ParameterBindingFlags.ProhibitNullItems) != 0; } }

		/// <summary>���̉��������B���������ǂ����������l���擾���܂��B</summary>
		public bool IsHidden { get { return (Flags & ParameterBindingFlags.IsHidden) != 0; } }

		/// <summary>���̉��������Q�Ɠn���ł��邩�ǂ����������l���擾���܂��B</summary>
		public bool IsByRef { get { return ParameterInfo != null && ParameterInfo.ParameterType.IsByRef; } }

		/// <summary>���̉��������z�������\���Ă��邩�ǂ����������l���擾���܂��B(�z������̓W�J�ɂ��쐬���ꂽ�������ł� <c>false</c> ��Ԃ��܂��B)</summary>
		public bool IsParamArray { get { return (Flags & ParameterBindingFlags.IsParamArray) != 0; } }

		/// <summary>���̉�����������������\���Ă��邩�ǂ����������l���擾���܂��B(���������̓W�J�ɂ��쐬���ꂽ�������ł� <c>false</c> ��Ԃ��܂��B)</summary>
		public bool IsParamDict { get { return (Flags & ParameterBindingFlags.IsParamDictionary) != 0; } }

		/// <summary>�z������̓W�J���ꂽ�v�f��\�����������쐬���܂��B</summary>
		/// <returns>�z������̓W�J���ꂽ�v�f��\���������B</returns>
		internal ParameterWrapper Expand()
		{
			Debug.Assert(IsParamArray);
			return new ParameterWrapper(ParameterInfo, Type.GetElementType(), null, (ProhibitNullItems ? ParameterBindingFlags.ProhibitNull : 0) | (IsHidden ? ParameterBindingFlags.IsHidden : 0));
		}
	}
}
