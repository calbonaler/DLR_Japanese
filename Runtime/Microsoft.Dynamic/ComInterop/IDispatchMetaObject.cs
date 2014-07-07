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
	sealed class IDispatchMetaObject : ComFallbackMetaObject
	{
		readonly IDispatchComObject _self;

		internal IDispatchMetaObject(Expression expression, IDispatchComObject self) : base(expression, BindingRestrictions.Empty, self) { _self = self; }

		public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			ComMethodDesc method;
			return _self.TryGetMemberMethod(binder.Name, out method) || _self.TryGetMemberMethodExplicit(binder.Name, out method) ? BindComInvoke(args, method, binder.CallInfo, ComBinderHelpers.ProcessArgumentsForCom(ref args)) : base.BindInvokeMember(binder, args);
		}

		public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			ComMethodDesc method;
			return _self.TryGetGetItem(out method) ? BindComInvoke(args, method, binder.CallInfo, ComBinderHelpers.ProcessArgumentsForCom(ref args)) : base.BindInvoke(binder, args);
		}

		DynamicMetaObject BindComInvoke(DynamicMetaObject[] args, ComMethodDesc method, CallInfo callInfo, bool[] isByRef)
		{
			return new ComInvokeBinder(callInfo, args, isByRef, IDispatchRestriction(), Expression.Constant(method),
				Expression.Property(
					Ast.Utils.Convert(Expression, typeof(IDispatchComObject)),
					typeof(IDispatchComObject).GetProperty("DispatchObject")
				),
				method
			).Invoke();
		}

		public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
		{
			ComBinder.ComGetMemberBinder comBinder = binder as ComBinder.ComGetMemberBinder;
			ContractUtils.RequiresNotNull(binder, "binder");
			ComMethodDesc method;
			ComEventDesc @event;
			// 1. メソッド
			if (_self.TryGetMemberMethod(binder.Name, out method))
				return BindGetMember(method, comBinder != null && comBinder._CanReturnCallables);
			// 2. イベント
			if (_self.TryGetMemberEvent(binder.Name, out @event))
				return BindEvent(@event);
			// 3. 明示的な名前によるメソッド
			if (_self.TryGetMemberMethodExplicit(binder.Name, out method))
				return BindGetMember(method, comBinder != null && comBinder._CanReturnCallables);
			// 4. フォールバック
			return base.BindGetMember(binder);
		}

		DynamicMetaObject BindGetMember(ComMethodDesc method, bool canReturnCallables)
		{
			if (method.IsDataMember && method.ParamCount == 0)
				return BindComInvoke(DynamicMetaObject.EmptyMetaObjects, method, new CallInfo(0), new bool[0]);
			// ComGetMemberBinder は callables を予期しない。常に呼び出しを試みる
			if (!canReturnCallables)
				return BindComInvoke(DynamicMetaObject.EmptyMetaObjects, method, new CallInfo(0), new bool[0]);
			return new DynamicMetaObject(
				Expression.Call(new Func<IDispatchComObject, ComMethodDesc, DispCallable>(ComRuntimeHelpers.CreateDispCallable).Method,
					Ast.Utils.Convert(Expression, typeof(IDispatchComObject)),
					Expression.Constant(method)
				),
				IDispatchRestriction()
			);
		}

		DynamicMetaObject BindEvent(ComEventDesc @event)
		{
			// BoundDispEvent CreateComEvent(object rcw, Guid sourceIid, int dispid)
			return new DynamicMetaObject(
				Expression.Call(new Func<object, Guid, int, BoundDispEvent>(ComRuntimeHelpers.CreateComEvent).Method,
					ComObject.RcwFromComObject(Expression),
					Expression.Constant(@event.sourceIID),
					Expression.Constant(@event.dispid)
				),
				IDispatchRestriction()
			);
		}

		public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			ComMethodDesc getItem;
			return _self.TryGetGetItem(out getItem) ? BindComInvoke(indexes, getItem, binder.CallInfo, ComBinderHelpers.ProcessArgumentsForCom(ref indexes)) : base.BindGetIndex(binder, indexes);
		}

		public override DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			ComMethodDesc setItem;
			if (_self.TryGetSetItem(out setItem))
			{
				var result = BindComInvoke(ArrayUtils.Append(indexes, value), setItem, binder.CallInfo, ArrayUtils.Append(ComBinderHelpers.ProcessArgumentsForCom(ref indexes), false));
				// 値を返すことを確認。必要な言語もある
				return new DynamicMetaObject(Expression.Block(result.Expression, Expression.Convert(value.Expression, typeof(object))), result.Restrictions);
			}
			return base.BindSetIndex(binder, indexes, value);
		}

		public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			return TryPropertyPut(binder, value) ?? // 1. 単純なプロパティ設定
				TryEventHandlerNoop(binder, value) ?? // 2. put が落ちた時のイベント ハンドラ フックアップを調べる
				base.BindSetMember(binder, value); // 3. フォールバック
		}

		DynamicMetaObject TryPropertyPut(SetMemberBinder binder, DynamicMetaObject value)
		{
			ComMethodDesc method;
			bool holdsNull = value.Value == null && value.HasValue;
			if (_self.TryGetPropertySetter(binder.Name, out method, value.LimitType, holdsNull) ||
				_self.TryGetPropertySetterExplicit(binder.Name, out method, value.LimitType, holdsNull))
			{
				var result = new ComInvokeBinder(new CallInfo(1), new[] { value }, new[] { false }, IDispatchRestriction(), Expression.Constant(method),
					Expression.Property(Ast.Utils.Convert(Expression, typeof(IDispatchComObject)), typeof(IDispatchComObject).GetProperty("DispatchObject")),
					method
				).Invoke();
				// 値を返すことを確認。必要な言語もある。
				return new DynamicMetaObject(Expression.Block(result.Expression, Expression.Convert(value.Expression, typeof(object))), result.Restrictions);
			}
			return null;
		}

		DynamicMetaObject TryEventHandlerNoop(SetMemberBinder binder, DynamicMetaObject value)
		{
			ComEventDesc @event;
			return _self.TryGetMemberEvent(binder.Name, out @event) && value.LimitType == typeof(BoundDispEvent) ?
				new DynamicMetaObject(
					Expression.Constant(null),
					value.Restrictions.Merge(IDispatchRestriction()).Merge(BindingRestrictions.GetTypeRestriction(value.Expression, typeof(BoundDispEvent)))
				) : null;
		}

		BindingRestrictions IDispatchRestriction() { return IDispatchRestriction(Expression, _self.ComTypeDesc); }

		internal static BindingRestrictions IDispatchRestriction(Expression expr, ComTypeDesc typeDesc)
		{
			return BindingRestrictions.GetTypeRestriction(expr, typeof(IDispatchComObject)).Merge(
				BindingRestrictions.GetExpressionRestriction(
					Expression.Equal(
						Expression.Property(Ast.Utils.Convert(expr, typeof(IDispatchComObject)), typeof(IDispatchComObject).GetProperty("ComTypeDesc")),
						Expression.Constant(typeDesc)
					)
				)
			);
		}

		protected override ComUnwrappedMetaObject UnwrapSelf() { return new ComUnwrappedMetaObject(ComObject.RcwFromComObject(Expression), IDispatchRestriction(), _self.RuntimeCallableWrapper); }
	}
}