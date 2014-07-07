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
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.ComInterop
{
	// ComBinder によって使用される操作をサポートする必要しかない。
	class ComMetaObject : DynamicMetaObject
	{
		internal ComMetaObject(Expression expression, BindingRestrictions restrictions, object arg) : base(expression, restrictions, arg) { }

		public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			return binder.Defer(ArrayUtils.Insert(WrapSelf(), args));
		}

		public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			return binder.Defer(ArrayUtils.Insert(WrapSelf(), args));
		}

		public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			return binder.Defer(WrapSelf());
		}

		public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			return binder.Defer(WrapSelf(), value);
		}

		public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			return binder.Defer(WrapSelf(), indexes);
		}

		public override DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			return binder.Defer(WrapSelf(), ArrayUtils.Append(indexes, value));
		}

		DynamicMetaObject WrapSelf()
		{
			return new DynamicMetaObject(
				ComObject.RcwToComObject(Expression),
				BindingRestrictions.GetExpressionRestriction(Expression.Call(new Func<object, bool>(ComObject.IsComObject).Method, Ast.Utils.Convert(Expression, typeof(object))))
			);
		}
	}
}