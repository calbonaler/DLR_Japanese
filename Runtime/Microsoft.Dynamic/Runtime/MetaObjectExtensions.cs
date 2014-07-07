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
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Runtime
{
	/// <summary><see cref="DynamicMetaObject"/> �Ɋւ���w���p�[ ���\�b�h��񋟂��܂��B</summary>
	public static class MetaObjectExtensions
	{
		/// <summary>����̃o�C���f�B���O�ۗ̕����K�v���ǂ����𔻒f���܂��B</summary>
		/// <param name="self">�o�C���f�B���O�ۗ̕����K�v���ǂ����𔻒f���� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <returns>�o�C���f�B���O�ۗ̕����K�v�ł���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool NeedsDeferral(this DynamicMetaObject self)
		{
			if (self.HasValue)
				return false;
			if (self.Expression.Type.IsSealed)
				return typeof(IDynamicMetaObjectProvider).IsAssignableFrom(self.Expression.Type);
			return true;
		}

		/// <summary>�w�肳�ꂽ�^�ɐ��񂳂ꂽ <see cref="DynamicMetaObject"/> ��Ԃ��܂��B</summary>
		/// <param name="self">���񂳂ꂽ <see cref="DynamicMetaObject"/> ��Ԃ� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <param name="type">���񂷂�^���w�肵�܂��B</param>
		/// <returns>�^�ɐ��񂳂ꂽ <see cref="DynamicMetaObject"/>�B</returns>
		public static DynamicMetaObject Restrict(this DynamicMetaObject self, Type type)
		{
			ContractUtils.RequiresNotNull(self, "self");
			ContractUtils.RequiresNotNull(type, "type");
			var rmo = self as IRestrictedMetaObject;
			if (rmo != null)
				return rmo.Restrict(type);
			if (type == self.Expression.Type && (type.IsSealed || self.Expression.NodeType == ExpressionType.New || self.Expression.NodeType == ExpressionType.NewArrayBounds || self.Expression.NodeType == ExpressionType.NewArrayInit))
				return self.Clone(self.Restrictions.Merge(BindingRestrictionsHelpers.GetRuntimeTypeRestriction(self.Expression, type)));
			if (type == typeof(DynamicNull))
				return self.Clone(AstUtils.Constant(null), self.Restrictions.Merge(BindingRestrictions.GetInstanceRestriction(self.Expression, null)));
			// if we're converting to a value type just unbox to preserve object identity.
			// If we're converting from Enum then we're going to a specific enum value and an unbox is not allowed.
			return self.Clone(
				type.IsValueType && self.Expression.Type != typeof(Enum) ?
					Expression.Unbox(self.Expression, CompilerHelpers.GetVisibleType(type)) :
					AstUtils.Convert(self.Expression, CompilerHelpers.GetVisibleType(type)),
				self.Restrictions.Merge(BindingRestrictionsHelpers.GetRuntimeTypeRestriction(self.Expression, type))
			);
		}

		/// <summary>�l��\�������w�肳�ꂽ���ɒu���������V���� <see cref="DynamicMetaObject"/> ���쐬���܂��B</summary>
		/// <param name="self">���� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <param name="newExpression">�l��\���V���������w�肵�܂��B</param>
		/// <returns>�l��\�������u��������ꂽ�V���� <see cref="DynamicMetaObject"/>�B</returns>
		public static DynamicMetaObject Clone(this DynamicMetaObject self, Expression newExpression) { return self.Clone(newExpression, self.Restrictions); }

		/// <summary>�o�C���f�B���O������w�肳�ꂽ�l�ɒu���������V���� <see cref="DynamicMetaObject"/> ���쐬���܂��B</summary>
		/// <param name="self">���� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <param name="newRestrictions">�V�����o�C���f�B���O������w�肵�܂��B</param>
		/// <returns>�o�C���f�B���O���񂪒u��������ꂽ�V���� <see cref="DynamicMetaObject"/>�B</returns>
		public static DynamicMetaObject Clone(this DynamicMetaObject self, BindingRestrictions newRestrictions) { return self.Clone(self.Expression, newRestrictions); }

		/// <summary>�l��\�����ƃo�C���f�B���O������w�肳�ꂽ�l�ɒu���������V���� <see cref="DynamicMetaObject"/> ���쐬���܂��B</summary>
		/// <param name="self">���� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <param name="newExpression">�l��\���V���������w�肵�܂��B</param>
		/// <param name="newRestrictions">�V�����o�C���f�B���O������w�肵�܂��B</param>
		/// <returns>�l��\�����ƃo�C���f�B���O���񂪒u��������ꂽ�V���� <see cref="DynamicMetaObject"/>�B</returns>
		public static DynamicMetaObject Clone(this DynamicMetaObject self, Expression newExpression, BindingRestrictions newRestrictions) { return self.HasValue ? new DynamicMetaObject(newExpression, newRestrictions, self.Value) : new DynamicMetaObject(newExpression, newRestrictions); }

		/// <summary>�l�� <c>null</c> �̏ꍇ���l�����ꂽ <see cref="DynamicMetaObject"/> �̐����^���擾���܂��B</summary>
		/// <param name="self">�����^���擾���� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <returns>�l�� <c>null</c> �̏ꍇ�� <see cref="DynamicNull"/> �^�B����ȊO�̏ꍇ�� <see cref="DynamicMetaObject.LimitType"/>�B</returns>
		public static Type GetLimitType(this DynamicMetaObject self) { return self.Value == null && self.HasValue ? typeof(DynamicNull) : self.LimitType; }

		/// <summary>�l�� <c>null</c> �̏ꍇ���l�����ꂽ <see cref="DynamicMetaObject"/> �̃����^�C���l�̌^���擾���܂��B</summary>
		/// <param name="self">�����^�C���l�̌^���擾���� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <returns>�l�� <c>null</c> �̏ꍇ�� <see cref="DynamicNull"/> �^�B����ȊO�̏ꍇ�� <see cref="DynamicMetaObject.RuntimeType"/>�B</returns>
		public static Type GetRuntimeType(this DynamicMetaObject self) { return self.Value == null && self.HasValue ? typeof(DynamicNull) : self.RuntimeType; }
	}
}
