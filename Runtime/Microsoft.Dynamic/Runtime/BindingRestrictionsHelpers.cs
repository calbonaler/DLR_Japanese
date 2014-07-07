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
using System.Dynamic;
using System.Linq.Expressions;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>�o�C���f�B���O������擾���邽�߂̃w���p�[ ���\�b�h���i�[���܂��B</summary>
	public static class BindingRestrictionsHelpers
	{
		//If the type is Microsoft.Scripting.Runtime.DynamicNull, create an instance restriction to test null
		/// <summary>�w�肳�ꂽ�^�ɑ΂��Ď����`�F�b�N����o�C���f�B���O������擾���܂��B</summary>
		/// <param name="expr">������e�X�g���鎮���w�肵�܂��B</param>
		/// <param name="type">���񂷂�^���w�肵�܂��B�^�ɂ� <see cref="DynamicNull"/> ���܂܂�܂��B</param>
		/// <returns>�^�ɑ΂��Ď����`�F�b�N����o�C���f�B���O����B</returns>
		public static BindingRestrictions GetRuntimeTypeRestriction(Expression expr, Type type) { return type == typeof(DynamicNull) ? BindingRestrictions.GetInstanceRestriction(expr, null) : BindingRestrictions.GetTypeRestriction(expr, type); }

		/// <summary>�w�肳�ꂽ <see cref="DynamicMetaObject"/> �̎��𐧌��^�ɐ��񂷂�o�C���f�B���O������擾���܂��B</summary>
		/// <param name="obj">����������� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ <see cref="DynamicMetaObject"/> �̐���Ƃ��̎��𐧌��^�ɐ��񂷂�o�C���f�B���O������}�[�W��������B</returns>
		public static BindingRestrictions GetRuntimeTypeRestriction(DynamicMetaObject obj) { return obj.Restrictions.Merge(GetRuntimeTypeRestriction(obj.Expression, obj.GetLimitType())); }
	}
}
