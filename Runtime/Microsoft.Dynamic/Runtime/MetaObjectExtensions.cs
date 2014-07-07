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
	/// <summary><see cref="DynamicMetaObject"/> に関するヘルパー メソッドを提供します。</summary>
	public static class MetaObjectExtensions
	{
		/// <summary>操作のバインディングの保留が必要かどうかを判断します。</summary>
		/// <param name="self">バインディングの保留が必要かどうかを判断する <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <returns>バインディングの保留が必要である場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool NeedsDeferral(this DynamicMetaObject self)
		{
			if (self.HasValue)
				return false;
			if (self.Expression.Type.IsSealed)
				return typeof(IDynamicMetaObjectProvider).IsAssignableFrom(self.Expression.Type);
			return true;
		}

		/// <summary>指定された型に制約された <see cref="DynamicMetaObject"/> を返します。</summary>
		/// <param name="self">制約された <see cref="DynamicMetaObject"/> を返す <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <param name="type">制約する型を指定します。</param>
		/// <returns>型に制約された <see cref="DynamicMetaObject"/>。</returns>
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

		/// <summary>値を表す式を指定された式に置き換えた新しい <see cref="DynamicMetaObject"/> を作成します。</summary>
		/// <param name="self">元の <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <param name="newExpression">値を表す新しい式を指定します。</param>
		/// <returns>値を表す式が置き換えられた新しい <see cref="DynamicMetaObject"/>。</returns>
		public static DynamicMetaObject Clone(this DynamicMetaObject self, Expression newExpression) { return self.Clone(newExpression, self.Restrictions); }

		/// <summary>バインディング制約を指定された値に置き換えた新しい <see cref="DynamicMetaObject"/> を作成します。</summary>
		/// <param name="self">元の <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <param name="newRestrictions">新しいバインディング制約を指定します。</param>
		/// <returns>バインディング制約が置き換えられた新しい <see cref="DynamicMetaObject"/>。</returns>
		public static DynamicMetaObject Clone(this DynamicMetaObject self, BindingRestrictions newRestrictions) { return self.Clone(self.Expression, newRestrictions); }

		/// <summary>値を表す式とバインディング制約を指定された値に置き換えた新しい <see cref="DynamicMetaObject"/> を作成します。</summary>
		/// <param name="self">元の <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <param name="newExpression">値を表す新しい式を指定します。</param>
		/// <param name="newRestrictions">新しいバインディング制約を指定します。</param>
		/// <returns>値を表す式とバインディング制約が置き換えられた新しい <see cref="DynamicMetaObject"/>。</returns>
		public static DynamicMetaObject Clone(this DynamicMetaObject self, Expression newExpression, BindingRestrictions newRestrictions) { return self.HasValue ? new DynamicMetaObject(newExpression, newRestrictions, self.Value) : new DynamicMetaObject(newExpression, newRestrictions); }

		/// <summary>値が <c>null</c> の場合も考慮された <see cref="DynamicMetaObject"/> の制限型を取得します。</summary>
		/// <param name="self">制限型を取得する <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <returns>値が <c>null</c> の場合は <see cref="DynamicNull"/> 型。それ以外の場合は <see cref="DynamicMetaObject.LimitType"/>。</returns>
		public static Type GetLimitType(this DynamicMetaObject self) { return self.Value == null && self.HasValue ? typeof(DynamicNull) : self.LimitType; }

		/// <summary>値が <c>null</c> の場合も考慮された <see cref="DynamicMetaObject"/> のランタイム値の型を取得します。</summary>
		/// <param name="self">ランタイム値の型を取得する <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <returns>値が <c>null</c> の場合は <see cref="DynamicNull"/> 型。それ以外の場合は <see cref="DynamicMetaObject.RuntimeType"/>。</returns>
		public static Type GetRuntimeType(this DynamicMetaObject self) { return self.Value == null && self.HasValue ? typeof(DynamicNull) : self.RuntimeType; }
	}
}
