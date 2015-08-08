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
			var targetInfo = TryGetDelegateTargets(target, args) ??
				TryGetMemberGroupTargets(target, args) ??
				TryGetMethodGroupTargets(target, args) ??
				TryGetBoundMemberTargets(target, args) ??
				TryGetOperatorTargets(target, args);
			if (targetInfo == null)
				return errorSuggestion ?? MakeCannotCallRule(target, target.GetLimitType()); // we can't call this object
			// we're calling a well-known MethodBase
			var res = CallMethod(
				resolverFactory.CreateOverloadResolver(targetInfo.Arguments, signature, targetInfo.CallType),
				targetInfo.Targets,
				targetInfo.Restrictions
			);
			if (res.Expression.Type.IsValueType)
				res = new DynamicMetaObject(AstUtils.Convert(res.Expression, typeof(object)), res.Restrictions);
			return res;
		}

		#region Target acquisition

		/// <summary>メソッドグループ内のメソッドに束縛します。</summary>
		static TargetInfo TryGetMethodGroupTargets(DynamicMetaObject target, DynamicMetaObject[] args)
		{
			var mg = target.Value as MethodGroup;
			if (mg == null)
				return null;
			return new TargetInfo(null, args, BindingRestrictions.GetInstanceRestriction(target.Expression, mg), mg.GetMethodBases());
		}

		/// <summary>メンバグループ内のメソッドに束縛します。</summary>
		static TargetInfo TryGetMemberGroupTargets(DynamicMetaObject target, DynamicMetaObject[] args)
		{
			var mg = target.Value as MemberGroup;
			if (mg == null)
				return null;
			return new TargetInfo(null, args, BindingRestrictions.GetInstanceRestriction(target.Expression, mg), mg.Where(x => x.MemberType == TrackerTypes.Method).Select(x => ((MethodTracker)x).Method).ToArray());
		}

		/// <summary>トラッカー内のインスタンスを使用して、オブジェクトインスタンスの型に基づいて制約することで、<see cref="BoundMemberTracker"/> に束縛します。</summary>
		static TargetInfo TryGetBoundMemberTargets(DynamicMetaObject target, DynamicMetaObject[] args)
		{
			var bmt = target.Value as BoundMemberTracker;
			if (bmt == null)
				return null;
			Debug.Assert(bmt.Instance == null); // ユーザーコードに漏れたトラッカーに対しては null にする
			// インスタンスは BoundMemberTracker から取り出され、適切な型に制約されます。
			var instance = new DynamicMetaObject(
				AstUtils.Convert(
					Ast.Property(
						Ast.Convert(target.Expression, typeof(BoundMemberTracker)),
						typeof(BoundMemberTracker).GetProperty("ObjectInstance")
					),
					bmt.BoundTo.DeclaringType
				),
				target.Restrictions
			).Restrict(CompilerHelpers.GetType(bmt.ObjectInstance));
			// 同じ BoundMemberTracker に対して実行することを保証するため、制約も追加する。
			var restrictions = BindingRestrictions.GetExpressionRestriction(
				Ast.Equal(
					Ast.Property(
						Ast.Convert(target.Expression, typeof(BoundMemberTracker)),
						typeof(BoundMemberTracker).GetProperty("BoundTo")
					),
					AstUtils.Constant(bmt.BoundTo)
				)
			);
			switch (bmt.BoundTo.MemberType)
			{
				case TrackerTypes.MethodGroup:
					return new TargetInfo(instance, args, restrictions, ((MethodGroup)bmt.BoundTo).GetMethodBases());
				case TrackerTypes.Method:
					return new TargetInfo(instance, args, restrictions, ((MethodTracker)bmt.BoundTo).Method);
				default:
					throw new InvalidOperationException(); // まだ何も束縛していない
			}
		}

		/// <summary>デリゲート型であれば Invoke メソッドに束縛します。</summary>
		static TargetInfo TryGetDelegateTargets(DynamicMetaObject target, DynamicMetaObject[] args)
		{
			if (!(target.Value is Delegate))
				return null;
			return new TargetInfo(target, args, target.Value.GetType().GetMethod("Invoke"));
		}

		/// <summary>演算子である Call メソッドへの束縛を試みます。</summary>
		TargetInfo TryGetOperatorTargets(DynamicMetaObject target, DynamicMetaObject[] args)
		{
			var res = GetMember(MemberRequestKind.Invoke, CompilerHelpers.GetType(target.Value), "Call").Where(x => x.MemberType == TrackerTypes.Method).Select(x => ((MethodTracker)x).Method).Where(x => x.IsSpecialName);
			return res.Any() ? new TargetInfo(null, ArrayUtils.Insert(target, args), res.ToArray()) : null;
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
			public readonly CallTypes CallType;
			public readonly DynamicMetaObject[] Arguments;
			public readonly MethodBase[] Targets;
			public readonly BindingRestrictions Restrictions;
			public TargetInfo(DynamicMetaObject instance, DynamicMetaObject[] arguments, params MethodBase[] targets) : this(instance, arguments, BindingRestrictions.Empty, targets) { }
			public TargetInfo(DynamicMetaObject instance, DynamicMetaObject[] arguments, BindingRestrictions additionalRestrictions, params MethodBase[] targets)
			{
				Assert.NotNullItems(targets);
				Assert.NotNull(additionalRestrictions);
				CallType = instance != null ? CallTypes.ImplicitInstance : CallTypes.None;
				Targets = targets;
				Restrictions = BindingRestrictions.Combine(arguments).Merge(additionalRestrictions).Merge(instance != null ? instance.Restrictions : BindingRestrictions.Empty);
				Arguments = instance != null ? ArrayUtils.Insert(instance, arguments) : arguments;
			}
		}
	}
}
