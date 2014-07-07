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
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions
{
	using Ast = Expression;

	public partial class DefaultBinder : ActionBinder
	{
		// TODO: Rename Call to Invoke, obsolete Call
		// TODO: Invoke overloads should also take CallInfo objects to simplify use for languages which don't need CallSignature features.

		/// <summary>指定された <see cref="DynamicMetaObject"/> に対する呼び出しを実行する既定のバインディングを提供します。</summary>
		/// <param name="signature">呼び出しを表すシグネチャを指定します。</param>
		/// <param name="target">呼び出される <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <param name="args"><paramref name="target"/> を呼び出す際の引数を表す <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <returns>呼び出しまたは失敗を表す <see cref="DynamicMetaObject"/>。</returns>
		public DynamicMetaObject Invoke(CallSignature signature, DynamicMetaObject target, params DynamicMetaObject[] args) { return Invoke(signature, new DefaultOverloadResolverFactory(this), target, args); }

		/// <summary>指定された <see cref="DynamicMetaObject"/> に対する呼び出しを実行する既定のバインディングを提供します。</summary>
		/// <param name="signature">呼び出しを表すシグネチャを指定します。</param>
		/// <param name="target">呼び出される <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <param name="args"><paramref name="target"/> を呼び出す際の引数を表す <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <param name="resolverFactory">オーバーロードの解決とメソッドバインディングを行う <see cref="OverloadResolverFactory"/> を指定します。</param>
		/// <returns>呼び出しまたは失敗を表す <see cref="DynamicMetaObject"/>。</returns>
		public DynamicMetaObject Invoke(CallSignature signature, OverloadResolverFactory resolverFactory, DynamicMetaObject target, params DynamicMetaObject[] args) { return Invoke(signature, null, resolverFactory, target, args); }

		/// <summary>指定された <see cref="DynamicMetaObject"/> に対する呼び出しを実行する既定のバインディングを提供します。</summary>
		/// <param name="signature">呼び出しを表すシグネチャを指定します。</param>
		/// <param name="target">呼び出される <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <param name="args"><paramref name="target"/> を呼び出す際の引数を表す <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <param name="resolverFactory">オーバーロードの解決とメソッドバインディングを行う <see cref="OverloadResolverFactory"/> を指定します。</param>
		/// <param name="errorSuggestion">オブジェクトの呼び出しに失敗した際の結果を指定します。</param>
		/// <returns>呼び出しまたは失敗を表す <see cref="DynamicMetaObject"/>。</returns>
		public DynamicMetaObject Invoke(CallSignature signature, DynamicMetaObject errorSuggestion, OverloadResolverFactory resolverFactory, DynamicMetaObject target, params DynamicMetaObject[] args)
		{
			ContractUtils.RequiresNotNullItems(args, "args");
			ContractUtils.RequiresNotNull(resolverFactory, "resolverFactory");
			ContractUtils.Requires(target.HasValue, "target", "target is needed to have value");
			var targetInfo = TryGetDelegateTargets(target, args, target.Value as Delegate) ??
				TryGetMemberGroupTargets(target, args, target.Value as MemberGroup) ??
				TryGetMethodGroupTargets(target, args, target.Value as MethodGroup) ??
				TryGetBoundMemberTargets(target, args, target.Value as BoundMemberTracker) ??
				TryGetOperatorTargets(target, args, target.Value);
			if (targetInfo != null)
			{
				// we're calling a well-known MethodBase
				var res = CallMethod(
					resolverFactory.CreateOverloadResolver(
						targetInfo.Instance != null ? ArrayUtils.Insert(targetInfo.Instance, targetInfo.Arguments) : targetInfo.Arguments,
						signature,
						targetInfo.Instance != null ? CallTypes.ImplicitInstance : CallTypes.None
					),
					targetInfo.Targets,
					targetInfo.Instance != null ?
						targetInfo.Instance.Restrictions.Merge(BindingRestrictions.Combine(targetInfo.Arguments).Merge(targetInfo.Restrictions)) :
						BindingRestrictions.Combine(targetInfo.Arguments).Merge(targetInfo.Restrictions)
				);
				if (res.Expression.Type.IsValueType)
					res = new DynamicMetaObject(AstUtils.Convert(res.Expression, typeof(object)), res.Restrictions);
				return res;
			}
			else
				return errorSuggestion ?? MakeCannotCallRule(target, target.GetLimitType()); // we can't call this object
		}

		#region Target acquisition

		/// <summary>メソッドグループ内のメソッドに束縛します。</summary>
		static TargetInfo TryGetMethodGroupTargets(DynamicMetaObject target, DynamicMetaObject[] args, MethodGroup mthgrp)
		{
			return mthgrp != null ? new TargetInfo(null, ArrayUtils.Insert(target, args), BindingRestrictions.GetInstanceRestriction(target.Expression, mthgrp), mthgrp.Methods.Select(x => x.Method).ToArray()) : null;
		}

		/// <summary>メンバグループ内のメソッドに束縛します。</summary>
		static TargetInfo TryGetMemberGroupTargets(DynamicMetaObject target, DynamicMetaObject[] args, MemberGroup mg)
		{
			return mg != null ? new TargetInfo(null, ArrayUtils.Insert(target, args), mg.Where(x => x.MemberType == TrackerTypes.Method).Select(x => ((MethodTracker)x).Method).ToArray()) : null;
		}

		/// <summary>トラッカー内のインスタンスを使用して、オブジェクトインスタンスの型に基づいて制約することで、<see cref="BoundMemberTracker"/> に束縛します。</summary>
		TargetInfo TryGetBoundMemberTargets(DynamicMetaObject self, DynamicMetaObject[] args, BoundMemberTracker bmt)
		{
			if (bmt != null)
			{
				Debug.Assert(bmt.Instance == null); // ユーザーコードに漏れたトラッカーに対しては null にする
				// インスタンスは BoundMemberTracker から取り出され、適切な型に制約されます。
				var instance = new DynamicMetaObject(
					AstUtils.Convert(
						Ast.Property(
							Ast.Convert(self.Expression, typeof(BoundMemberTracker)),
							typeof(BoundMemberTracker).GetProperty("ObjectInstance")
						),
						bmt.BoundTo.DeclaringType
					),
					self.Restrictions
				).Restrict(CompilerHelpers.GetType(bmt.ObjectInstance));
				// 同じ BoundMemberTracker に対して実行することを保証するため、制約も追加する。
				var restrictions = BindingRestrictions.GetExpressionRestriction(
					Ast.Equal(
						Ast.Property(
							Ast.Convert(self.Expression, typeof(BoundMemberTracker)),
							typeof(BoundMemberTracker).GetProperty("BoundTo")
						),
						AstUtils.Constant(bmt.BoundTo)
					)
				);
				MethodBase[] targets;
				switch (bmt.BoundTo.MemberType)
				{
					case TrackerTypes.MethodGroup:
						targets = ((MethodGroup)bmt.BoundTo).GetMethodBases();
						break;
					case TrackerTypes.Method:
						targets = new MethodBase[] { ((MethodTracker)bmt.BoundTo).Method };
						break;
					default:
						throw new InvalidOperationException(); // まだ何も束縛していない
				}
				return new TargetInfo(instance, args, restrictions, targets);
			}
			return null;
		}

		/// <summary>デリゲート型であれば Invoke メソッドに束縛します。</summary>
		static TargetInfo TryGetDelegateTargets(DynamicMetaObject target, DynamicMetaObject[] args, Delegate d) { return d != null ? new TargetInfo(target, args, d.GetType().GetMethod("Invoke")) : null; }

		/// <summary>演算子である Call メソッドへの束縛を試みます。</summary>
		TargetInfo TryGetOperatorTargets(DynamicMetaObject self, DynamicMetaObject[] args, object target)
		{
			var res = GetMember(MemberRequestKind.Invoke, CompilerHelpers.GetType(target), "Call").Where(x => x.MemberType == TrackerTypes.Method).Select(x => ((MethodTracker)x).Method).Where(x => x.IsSpecialName);
			return res.Any() ? new TargetInfo(null, ArrayUtils.Insert(self, args), res.ToArray()) : null;
		}

		#endregion

		DynamicMetaObject MakeCannotCallRule(DynamicMetaObject self, Type type)
		{
			return MakeError(
				ErrorInfo.FromException(
					Ast.New(
						typeof(ArgumentTypeException).GetConstructor(new[] { typeof(string) }),
						AstUtils.Constant(GetTypeName(type) + " is not callable")
					)
				),
				self.Restrictions.Merge(BindingRestrictionsHelpers.GetRuntimeTypeRestriction(self.Expression, type)),
				typeof(object)
			);
		}

		/// <summary>
		/// 呼び出しのターゲットに関する情報をカプセル化します。
		/// これには呼び出しの実行に必要なあらゆる制約のほかに、呼び出しの暗黙のインスタンスや呼び出すメソッドも含まれます。
		/// </summary>
		class TargetInfo
		{
			public readonly DynamicMetaObject Instance;
			public readonly DynamicMetaObject[] Arguments;
			public readonly MethodBase[] Targets;
			public readonly BindingRestrictions Restrictions;
			public TargetInfo(DynamicMetaObject instance, DynamicMetaObject[] arguments, params MethodBase[] args) : this(instance, arguments, BindingRestrictions.Empty, args) { }
			public TargetInfo(DynamicMetaObject instance, DynamicMetaObject[] arguments, BindingRestrictions restrictions, params MethodBase[] targets)
			{
				Assert.NotNullItems(targets);
				Assert.NotNull(restrictions);
				Instance = instance;
				Arguments = arguments;
				Targets = targets;
				Restrictions = restrictions;
			}
		}
	}
}
