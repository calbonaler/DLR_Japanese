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

namespace Microsoft.Scripting.Actions
{
	/// <summary><see cref="MemberTracker"/> ���\�������o�̎�ނ��w�肵�܂��B</summary>
	[Flags]
	public enum TrackerTypes
	{
		/// <summary>�ǂ̎�ނ̃����o���w�肵�܂���B</summary>
		None = 0x00,
		/// <summary>�����o�� <see cref="ConstructorTracker"/> �ɂ��\�����R���X�g���N�^�ł��邱�Ƃ��w�肵�܂��B</summary>
		Constructor = 0x01,
		/// <summary>�����o�� <see cref="EventTracker"/> �ɂ��\�����C�x���g�ł��邱�Ƃ��w�肵�܂��B</summary>
		Event = 0x02,
		/// <summary>�����o�� <see cref="FieldTracker"/> �ɂ��\�����t�B�[���h�ł��邱�Ƃ��w�肵�܂��B</summary>
		Field = 0x04,
		/// <summary>�����o�� <see cref="MethodTracker"/> �ɂ��\����郁�\�b�h�ł��邱�Ƃ��w�肵�܂��B</summary>
		Method = 0x08,
		/// <summary>�����o�� <see cref="PropertyTracker"/> �ɂ��\�����v���p�e�B�ł��邱�Ƃ��w�肵�܂��B</summary>
		Property = 0x10,
		/// <summary>�����o�� <see cref="TypeTracker"/> �ɂ��\�����^�ł��邱�Ƃ��w�肵�܂��B</summary>
		Type = 0x20,
		/// <summary>�����o�� <see cref="NamespaceTracker"/> �ɂ��\����閼�O��Ԃł��邱�Ƃ��w�肵�܂��B</summary>
		Namespace = 0x40,
		/// <summary>�����o�� <see cref="MethodGroup"/> �ɂ��\����郁�\�b�h�I�[�o�[���[�h�̃O���[�v�ł��邱�Ƃ��w�肵�܂��B</summary>
		MethodGroup = 0x80,
		/// <summary>�����o�� <see cref="TypeGroup"/> �ɂ��\�����W�F�l���b�N �A���e�B�̈قȂ�^�̃O���[�v�ł��邱�Ƃ��w�肵�܂��B</summary>
		TypeGroup = 0x100,
		/// <summary>�����o�� <see cref="CustomTracker"/> �ɂ��\�����J�X�^�������o�ł��邱�Ƃ��w�肵�܂��B</summary>
		Custom = 0x200,
		/// <summary>�����o�� <see cref="BoundMemberTracker"/> �ɂ��\����A�C���X�^���X�Ɋ֘A�t�����Ă��邱�Ƃ��w�肵�܂��B</summary>        
		Bound = 0x400,
		/// <summary>���ׂẴ����o�̎�ނ��w�肵�܂��B</summary>
		All = Constructor | Event | Field | Method | Property | Type | Namespace | MethodGroup | TypeGroup | Bound,
	}
}
