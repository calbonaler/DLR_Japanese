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

using System.Dynamic;
using System.Linq.Expressions;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.ComInterop
{
	class DispCallableMetaObject : DynamicMetaObject
	{
		readonly DispCallable _callable;

		internal DispCallableMetaObject(Expression expression, DispCallable callable) : base(expression, BindingRestrictions.Empty, callable) { _callable = callable; }

		public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes) { return BindGetOrInvoke(indexes, binder.CallInfo) ?? base.BindGetIndex(binder, indexes); }

		public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args) { return BindGetOrInvoke(args, binder.CallInfo) ?? base.BindInvoke(binder, args); }

		DynamicMetaObject BindGetOrInvoke(DynamicMetaObject[] args, CallInfo callInfo)
		{
			ComMethodDesc method;
			if (_callable.DispatchComObject.TryGetMemberMethod(_callable.MemberName, out method) || _callable.DispatchComObject.TryGetMemberMethodExplicit(_callable.MemberName, out method))
				return BindComInvoke(method, args, callInfo, ComBinderHelpers.ProcessArgumentsForCom(ref args));
			return null;
		}

		public override DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value)
		{
			ComMethodDesc method;
			bool holdsNull = value.Value == null && value.HasValue;
			if (_callable.DispatchComObject.TryGetPropertySetter(_callable.MemberName, out method, value.LimitType, holdsNull) || _callable.DispatchComObject.TryGetPropertySetterExplicit(_callable.MemberName, out method, value.LimitType, holdsNull))
			{
				var result = BindComInvoke(method, ArrayUtils.Append(indexes, value), binder.CallInfo, ArrayUtils.Append(ComBinderHelpers.ProcessArgumentsForCom(ref indexes), false));
				// Make sure to return the value; some languages need it.
				return new DynamicMetaObject(
					Expression.Block(result.Expression, Expression.Convert(value.Expression, typeof(object))),
					result.Restrictions
				);
			}
			return base.BindSetIndex(binder, indexes, value);
		}

		DynamicMetaObject BindComInvoke(ComMethodDesc method, DynamicMetaObject[] indexes, CallInfo callInfo, bool[] isByRef)
		{
			return new ComInvokeBinder(callInfo, indexes, isByRef, DispCallableRestrictions(), Expression.Constant(method),
				Expression.Property(Ast.Utils.Convert(Expression, typeof(DispCallable)), typeof(DispCallable).GetProperty("DispatchObject")),
				method
			).Invoke();
		}

		BindingRestrictions DispCallableRestrictions()
		{
			var dispCall = Ast.Utils.Convert(Expression, typeof(DispCallable));
			return BindingRestrictions.GetTypeRestriction(Expression, typeof(DispCallable)).Merge(IDispatchMetaObject.IDispatchRestriction(
				Expression.Property(dispCall, typeof(DispCallable).GetProperty("DispatchComObject")),
				_callable.DispatchComObject.ComTypeDesc
			)).Merge(BindingRestrictions.GetExpressionRestriction(
				Expression.Equal(Expression.Property(dispCall, typeof(DispCallable).GetProperty("DispId")), Expression.Constant(_callable.DispId))
			));
		}
	}
}