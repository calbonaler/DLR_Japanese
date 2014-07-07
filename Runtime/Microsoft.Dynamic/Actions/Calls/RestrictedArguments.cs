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
using System.Collections.ObjectModel;
using System.Dynamic;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>���ꂼ��̎������ɖ����I�Ȍ^��ǉ������������̃R���N�V������\���܂��B</summary>
	public sealed class RestrictedArguments
	{
		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�ɑ΂��� <see cref="Microsoft.Scripting.Actions.Calls.RestrictedArguments"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="objects">�������̒l��\�� <see cref="DynamicMetaObject"/> �̔z����w�肵�܂��B</param>
		/// <param name="types">�������̐��񂳂ꂽ�^�̔z����w�肵�܂��B</param>
		/// <param name="hasUntypedRestrictions">���̎��������X�g�ɒP���Ȍ^����ȊO�ɐ��񂪑��݂��邩�ǂ����������l���w�肵�܂��B</param>
		public RestrictedArguments(DynamicMetaObject[] objects, Type[] types, bool hasUntypedRestrictions)
		{
			ContractUtils.RequiresNotNullItems(objects, "objects");
			ContractUtils.RequiresNotNull(types, "types");
			ContractUtils.Requires(objects.Length == types.Length, "objects");
			Objects = new ReadOnlyCollection<DynamicMetaObject>(objects);
			Types = new ReadOnlyCollection<Type>(types);
			HasUntypedRestrictions = hasUntypedRestrictions;
		}

		/// <summary>�������̐����擾���܂��B</summary>
		public int Length { get { return Objects.Count; } }

		/// <summary>�P���Ȍ^����ȊO�ɐ��񂪑��݂��邩�ǂ����������l���擾���܂��B</summary>
		public bool HasUntypedRestrictions { get; private set; }

		/// <summary>���̃R���N�V�����Ɋ܂܂�Ă��邷�ׂẴo�C���f�B���O����� 1 �̃Z�b�g�ɂ܂Ƃ߂Ď擾���܂��B</summary>
		/// <returns>1 �ɂ܂Ƃ߂�ꂽ�R���N�V�����Ɋ܂܂�Ă��邷�ׂĂ� <see cref="DynamicMetaObject"/> �̃o�C���f�B���O����B</returns>
		public BindingRestrictions GetAllRestrictions() { return BindingRestrictions.Combine(Objects); }

		/// <summary>�������̒l��\�� <see cref="DynamicMetaObject"/> ���擾���܂��B</summary>
		public ReadOnlyCollection<DynamicMetaObject> Objects { get; private set; }

		/// <summary>�Ή����� <see cref="DynamicMetaObject"/> �ɑ΂���^���擾���܂��B</summary>
		public ReadOnlyCollection<Type> Types { get; private set; }
	}
}
