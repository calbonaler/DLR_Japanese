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
	/// <summary>型が制約された <see cref="DynamicMetaObject"/> を表します。</summary>
	public class RestrictedMetaObject : DynamicMetaObject, IRestrictedMetaObject
	{
		/// <summary><see cref="Microsoft.Scripting.Runtime.RestrictedMetaObject"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="expression">動的バインディング プロセスにおいてこの <see cref="RestrictedMetaObject"/> を表す式。</param>
		/// <param name="restriction">バインディングが有効となるバインディング制限のセット。</param>
		/// <param name="value"><see cref="RestrictedMetaObject"/> が表すランタイム値。</param>
		public RestrictedMetaObject(Expression expression, BindingRestrictions restriction, object value) : base(expression, restriction, value) { }

		/// <summary><see cref="Microsoft.Scripting.Runtime.RestrictedMetaObject"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="expression">動的バインディング プロセスにおいてこの <see cref="RestrictedMetaObject"/> を表す式。</param>
		/// <param name="restriction">バインディングが有効となるバインディング制限のセット。</param>
		public RestrictedMetaObject(Expression expression, BindingRestrictions restriction) : base(expression, restriction) { }

		/// <summary>指定された型の制約された <see cref="DynamicMetaObject"/> を返します。</summary>
		/// <param name="type">制約する型を指定します。</param>
		/// <returns>型に制約された <see cref="DynamicMetaObject"/>。</returns>
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
