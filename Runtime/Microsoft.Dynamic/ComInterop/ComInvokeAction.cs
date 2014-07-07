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
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Microsoft.Scripting.ComInterop
{
	/// <summary>COM オブジェクトを呼び出すバインダーを表します。フォールバックした場合、単純にエラーを生成します。</summary>
	sealed class ComInvokeAction : InvokeBinder
	{
		internal ComInvokeAction(CallInfo callInfo) : base(callInfo) { }

		public override int GetHashCode() { return base.GetHashCode(); }

		public override bool Equals(object obj) { return base.Equals(obj as ComInvokeAction); }

		public override DynamicMetaObject FallbackInvoke(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
		{
			DynamicMetaObject res;
			if (ComBinder.TryBindInvoke(this, target, args, out res))
				return res;
			return errorSuggestion ?? new DynamicMetaObject(
				Expression.Throw(Expression.New(typeof(NotSupportedException).GetConstructor(new[] { typeof(string) }), Expression.Constant(Strings.CannotCall))),
				target.Restrictions.Merge(BindingRestrictions.Combine(args))
			);
		}
	}

	/// <summary>
	/// 他のネストされた動的サイトに引数を展開します。
	/// 動的サイトは <see cref="IDynamicMetaObjectProvider"/> の本当の呼び出しを行います。
	/// </summary>
	sealed class SplatInvokeBinder : CallSiteBinder
	{
		internal readonly static SplatInvokeBinder Instance = new SplatInvokeBinder();

		// 引数を展開して、ネストされたサイトにディスパッチする
		public override Expression Bind(object[] args, ReadOnlyCollection<ParameterExpression> parameters, LabelTarget returnLabel)
		{
			Debug.Assert(args.Length == 2);
			var count = ((object[])args[1]).Length;
			return Expression.IfThen(Expression.Equal(Expression.ArrayLength(parameters[1]), Expression.Constant(count)),
				Expression.Return(
					returnLabel,
					Expression.MakeDynamic(
						Expression.GetDelegateType(new[] { typeof(CallSite), typeof(object) }.Concat(Enumerable.Repeat(typeof(object).MakeByRefType(), count)).Concat(Enumerable.Repeat(typeof(object), 1)).ToArray()),
						new ComInvokeAction(new CallInfo(count)),
						Enumerable.Repeat<Expression>(parameters[0], 1).Concat(Enumerable.Range(0, count).Select(i => Expression.ArrayAccess(parameters[1], Expression.Constant(i))))
					)
				)
			);
		}
	}
}