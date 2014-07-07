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
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>�^�����񂳂ꂽ <see cref="DynamicMetaObject"/> ��\���܂��B</summary>
	public class RestrictedMetaObject : DynamicMetaObject, IRestrictedMetaObject
	{
		/// <summary><see cref="Microsoft.Scripting.Runtime.RestrictedMetaObject"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="expression">���I�o�C���f�B���O �v���Z�X�ɂ����Ă��� <see cref="RestrictedMetaObject"/> ��\�����B</param>
		/// <param name="restriction">�o�C���f�B���O���L���ƂȂ�o�C���f�B���O�����̃Z�b�g�B</param>
		/// <param name="value"><see cref="RestrictedMetaObject"/> ���\�������^�C���l�B</param>
		public RestrictedMetaObject(Expression expression, BindingRestrictions restriction, object value) : base(expression, restriction, value) { }

		/// <summary><see cref="Microsoft.Scripting.Runtime.RestrictedMetaObject"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="expression">���I�o�C���f�B���O �v���Z�X�ɂ����Ă��� <see cref="RestrictedMetaObject"/> ��\�����B</param>
		/// <param name="restriction">�o�C���f�B���O���L���ƂȂ�o�C���f�B���O�����̃Z�b�g�B</param>
		public RestrictedMetaObject(Expression expression, BindingRestrictions restriction) : base(expression, restriction) { }

		/// <summary>�w�肳�ꂽ�^�̐��񂳂ꂽ <see cref="DynamicMetaObject"/> ��Ԃ��܂��B</summary>
		/// <param name="type">���񂷂�^���w�肵�܂��B</param>
		/// <returns>�^�ɐ��񂳂ꂽ <see cref="DynamicMetaObject"/>�B</returns>
		public DynamicMetaObject Restrict(Type type)
		{
			if (type == LimitType)
				return this;
			if (HasValue)
				return new RestrictedMetaObject(AstUtils.Convert(Expression, type), BindingRestrictionsHelpers.GetRuntimeTypeRestriction(Expression, type), Value);
			return new RestrictedMetaObject(AstUtils.Convert(Expression, type), BindingRestrictionsHelpers.GetRuntimeTypeRestriction(Expression, type));
		}
	}
}
