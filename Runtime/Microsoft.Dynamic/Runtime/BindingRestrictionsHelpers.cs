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
	/// <summary>バインディング制約を取得するためのヘルパー メソッドを格納します。</summary>
	public static class BindingRestrictionsHelpers
	{
		//If the type is Microsoft.Scripting.Runtime.DynamicNull, create an instance restriction to test null
		/// <summary>指定された型に対して式をチェックするバインディング制約を取得します。</summary>
		/// <param name="expr">制約をテストする式を指定します。</param>
		/// <param name="type">制約する型を指定します。型には <see cref="DynamicNull"/> も含まれます。</param>
		/// <returns>型に対して式をチェックするバインディング制約。</returns>
		public static BindingRestrictions GetRuntimeTypeRestriction(Expression expr, Type type) { return type == typeof(DynamicNull) ? BindingRestrictions.GetInstanceRestriction(expr, null) : BindingRestrictions.GetTypeRestriction(expr, type); }

		/// <summary>指定された <see cref="DynamicMetaObject"/> の式を制限型に制約するバインディング制約を取得します。</summary>
		/// <param name="obj">制約をかける <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <returns>指定された <see cref="DynamicMetaObject"/> の制約とその式を制限型に制約するバインディング制約をマージした制約。</returns>
		public static BindingRestrictions GetRuntimeTypeRestriction(DynamicMetaObject obj) { return obj.Restrictions.Merge(GetRuntimeTypeRestriction(obj.Expression, obj.GetLimitType())); }
	}
}
