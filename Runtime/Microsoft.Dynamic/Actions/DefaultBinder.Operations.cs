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
		/// <summary>指定された引数に対して、指定された演算を実行します。</summary>
		/// <param name="operation">演算の種類を示す <see cref="ExpressionType"/> を指定します。</param>
		/// <param name="args">演算のオペランドを指定します。</param>
		/// <returns>指定された演算の結果を表す <see cref="DynamicMetaObject"/>。</returns>
		public DynamicMetaObject DoOperation(ExpressionType operation, params DynamicMetaObject[] args) { return DoOperation(operation, new DefaultOverloadResolverFactory(this), args); }

		/// <summary>指定された引数に対して、指定された演算を実行します。</summary>
		/// <param name="operation">演算の種類を示す <see cref="ExpressionType"/> を指定します。</param>
		/// <param name="resolverFactory">オーバーロードの解決とメソッドバインディングを実行する <see cref="OverloadResolverFactory"/> を指定します。</param>
		/// <param name="args">演算のオペランドを指定します。</param>
		/// <returns>指定された演算の結果を表す <see cref="DynamicMetaObject"/>。</returns>
		public DynamicMetaObject DoOperation(ExpressionType operation, OverloadResolverFactory resolverFactory, params DynamicMetaObject[] args)
		{
			ContractUtils.RequiresNotNull(resolverFactory, "resolverFactory");
			ContractUtils.RequiresNotNullItems(args, "args");
			// Then try comparison / other ExpressionType
			return CompilerHelpers.IsComparisonOperator(operation) ?
				MakeComparisonRule(OperatorInfo.GetOperatorInfo(operation), resolverFactory, args) :
				MakeOperatorRule(OperatorInfo.GetOperatorInfo(operation), resolverFactory, args);
		}

		enum IndexType
		{
			Get,
			Set
		}

		/// <summary>
		/// 配列に対する直接のインデックス取得操作および既定のメンバを持つオブジェクトに対するインデックス取得操作を実行する <see cref="DynamicMetaObject"/> を作成します。
		/// インデックス取得操作が実行できなかった場合は <c>null</c> を返します。
		/// </summary>
		/// <param name="args">インデックス取得操作の対象および引数を表す <see cref="DynamicMetaObject"/> の配列を指定します。</param>
		/// <returns>インデックス取得操作の結果を表す <see cref="DynamicMetaObject"/>。操作が失敗した場合は <c>null</c>。</returns>
		public DynamicMetaObject GetIndex(DynamicMetaObject[] args) { return GetIndex(new DefaultOverloadResolverFactory(this), args); }

		/// <summary>
		/// 配列に対する直接のインデックス取得操作および既定のメンバを持つオブジェクトに対するインデックス取得操作を実行する <see cref="DynamicMetaObject"/> を作成します。
		/// インデックス取得操作が実行できなかった場合は <c>null</c> を返します。
		/// </summary>
		/// <param name="resolverFactory">オーバーロードの解決とメソッドバインディングを実行する <see cref="OverloadResolverFactory"/> を指定します。</param>
		/// <param name="args">インデックス取得操作の対象および引数を表す <see cref="DynamicMetaObject"/> の配列を指定します。</param>
		/// <returns>インデックス取得操作の結果を表す <see cref="DynamicMetaObject"/>。操作が失敗した場合は <c>null</c>。</returns>
		public DynamicMetaObject GetIndex(OverloadResolverFactory resolverFactory, DynamicMetaObject[] args) { return args[0].GetLimitType().IsArray ? MakeArrayIndexRule(resolverFactory, IndexType.Get, args) : MakeMethodIndexRule(IndexType.Get, resolverFactory, args); }

		/// <summary>
		/// 配列に対する直接のインデックス設定操作および既定のメンバを持つオブジェクトに対するインデックス設定操作を実行する <see cref="DynamicMetaObject"/> を作成します。
		/// インデックス設定操作が実行できなかった場合は <c>null</c> を返します。
		/// </summary>
		/// <param name="args">インデックス設定操作の対象および引数を表す <see cref="DynamicMetaObject"/> の配列を指定します。</param>
		/// <returns>インデックス設定操作の結果を表す <see cref="DynamicMetaObject"/>。操作が失敗した場合は <c>null</c>。</returns>
		public DynamicMetaObject SetIndex(DynamicMetaObject[] args) { return SetIndex(new DefaultOverloadResolverFactory(this), args); }

		/// <summary>
		/// 配列に対する直接のインデックス設定操作および既定のメンバを持つオブジェクトに対するインデックス設定操作を実行する <see cref="DynamicMetaObject"/> を作成します。
		/// インデックス設定操作が実行できなかった場合は <c>null</c> を返します。
		/// </summary>
		/// <param name="resolverFactory">オーバーロードの解決とメソッドバインディングを実行する <see cref="OverloadResolverFactory"/> を指定します。</param>
		/// <param name="args">インデックス設定操作の対象および引数を表す <see cref="DynamicMetaObject"/> の配列を指定します。</param>
		/// <returns>インデックス設定操作の結果を表す <see cref="DynamicMetaObject"/>。操作が失敗した場合は <c>null</c>。</returns>
		public DynamicMetaObject SetIndex(OverloadResolverFactory resolverFactory, DynamicMetaObject[] args) { return args[0].LimitType.IsArray ? MakeArrayIndexRule(resolverFactory, IndexType.Set, args) : MakeMethodIndexRule(IndexType.Set, resolverFactory, args); }

		/// <summary>指定された <see cref="DynamicMetaObject"/> が表す値の型に対するドキュメントを表す <see cref="DynamicMetaObject"/> を取得します。</summary>
		/// <param name="target">ドキュメントを取得する型の値を格納している <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <returns>型のドキュメントを表す <see cref="DynamicMetaObject"/>。</returns>
		public DynamicMetaObject GetDocumentation(DynamicMetaObject target)
		{
			var attrs = target.LimitType.GetCustomAttributes<DocumentationAttribute>(true);
			return new DynamicMetaObject(AstUtils.Constant(attrs.Any() ? attrs.First().Documentation : string.Empty), BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType));
		}

		/// <summary>指定された <see cref="DynamicMetaObject"/> が表す値の型のすべてのメンバの名前表す <see cref="DynamicMetaObject"/> を取得します。</summary>
		/// <param name="target">すべてのメンバの名前を取得する型の値を格納している <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <returns>型のすべてのメンバの名前を表す <see cref="DynamicMetaObject"/>。</returns>
		public DynamicMetaObject GetMemberNames(DynamicMetaObject target) { return new DynamicMetaObject(AstUtils.Constant(target.LimitType.GetMembers().Select(x => x.Name).Distinct().ToArray()), BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType)); }

		/// <summary>指定された <see cref="DynamicMetaObject"/> を呼び出す際のすべてのシグネチャを表す <see cref="DynamicMetaObject"/> を取得します。</summary>
		/// <param name="target">呼び出す場合のすべてのシグネチャを取得する <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <returns>呼び出しのすべてのシグネチャを表す <see cref="DynamicMetaObject"/>。</returns>
		public DynamicMetaObject GetCallSignatures(DynamicMetaObject target)
		{
			return new DynamicMetaObject(
				AstUtils.Constant((CompilerHelpers.GetMethodTargets(target.LimitType) ?? Enumerable.Empty<MethodBase>()).Select(m => string.Join(", ", m.GetParameters().Select(x => x.ParameterType.Name + " " + x.Name))).ToArray()),
				BindingRestrictionsHelpers.GetRuntimeTypeRestriction(target.Expression, target.GetLimitType()).Merge(target.Restrictions)
			);
		}

		/// <summary>指定された <see cref="DynamicMetaObject"/> が呼び出し可能かどうかを表す <see cref="DynamicMetaObject"/> を取得します。</summary>
		/// <param name="target">呼び出し可能かどうかを調べる <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <returns>呼び出し可能かどうかを表す <see cref="DynamicMetaObject"/>。</returns>
		public DynamicMetaObject GetIsCallable(DynamicMetaObject target)
		{
			// IsCallable() is tightly tied to Call actions. So in general, we need the call-action providers to also
			// provide IsCallable() status. 
			// This is just a rough fallback. We could also attempt to simulate the default CallBinder logic to see
			// if there are any applicable calls targets, but that would be complex (the callbinder wants the argument list, 
			// which we don't have here), and still not correct.
			return new DynamicMetaObject(
				AstUtils.Constant(typeof(Delegate).IsAssignableFrom(target.LimitType) || typeof(MethodGroup).IsAssignableFrom(target.LimitType)),
				BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType)
			);
		}

		#region Comparison operator

		DynamicMetaObject MakeComparisonRule(OperatorInfo info, OverloadResolverFactory resolverFactory, DynamicMetaObject[] args)
		{
			return
				TryComparisonMethod(info, resolverFactory, args[0], args) ??   // check the first type if it has an applicable method
				TryComparisonMethod(info, resolverFactory, args[0], args) ??   // then check the second type
				TryNumericComparison(info, resolverFactory, args) ??           // try Compare: cmp(x,y) (>, <, >=, <=, ==, !=) 0
				TryInvertedComparison(info, resolverFactory, args[0], args) ?? // try inverting the operator & result (e.g. if looking for Equals try NotEquals, LessThan for GreaterThan)...
				TryInvertedComparison(info, resolverFactory, args[0], args) ?? // inverted binding on the 2nd type
				TryNullComparisonRule(args) ??                // see if we're comparing to null w/ an object ref or a Nullable<T>
				TryPrimitiveCompare(info, args) ??            // see if this is a primitive type where we're comparing the two values.
				MakeOperatorError(info, args);                // no comparisons are possible            
		}

		DynamicMetaObject TryComparisonMethod(OperatorInfo info, OverloadResolverFactory resolverFactory, DynamicMetaObject target, DynamicMetaObject[] args)
		{
			var targets = GetApplicableMembers(target.GetLimitType(), info);
			return targets.Length > 0 ? TryMakeBindingTarget(resolverFactory, targets, args, BindingRestrictions.Empty) : null;
		}

		static DynamicMetaObject MakeOperatorError(OperatorInfo info, DynamicMetaObject[] args)
		{
			return new DynamicMetaObject(
				Ast.Throw(
					AstUtils.ComplexCallHelper(
						typeof(BinderOps).GetMethod("BadArgumentsForOperation"),
						ArrayUtils.Insert((Expression)AstUtils.Constant(info.Operator), args.Select(x => x != null ? x.Expression : null).ToArray())
					)
				),
				BindingRestrictions.Combine(args)
			);
		}

		DynamicMetaObject TryNumericComparison(OperatorInfo info, OverloadResolverFactory resolverFactory, DynamicMetaObject[] args)
		{
			var targets = FilterNonMethods(args[0].GetLimitType(), GetMember(MemberRequestKind.Operation, args[0].GetLimitType(), "Compare"));
			if (targets.Length > 0)
			{
				var target = resolverFactory.CreateOverloadResolver(args, new CallSignature(args.Length), CallTypes.None)
					.ResolveOverload(targets[0].Name, targets, NarrowingLevel.None, NarrowingLevel.All);
				if (target.Success)
				{
					var call = AstUtils.Convert(target.MakeExpression(), typeof(int));
					switch (info.Operator)
					{
						case ExpressionType.GreaterThan: call = Ast.GreaterThan(call, AstUtils.Constant(0)); break;
						case ExpressionType.LessThan: call = Ast.LessThan(call, AstUtils.Constant(0)); break;
						case ExpressionType.GreaterThanOrEqual: call = Ast.GreaterThanOrEqual(call, AstUtils.Constant(0)); break;
						case ExpressionType.LessThanOrEqual: call = Ast.LessThanOrEqual(call, AstUtils.Constant(0)); break;
						case ExpressionType.Equal: call = Ast.Equal(call, AstUtils.Constant(0)); break;
						case ExpressionType.NotEqual: call = Ast.NotEqual(call, AstUtils.Constant(0)); break;
					}
					return new DynamicMetaObject(call, target.RestrictedArguments.GetAllRestrictions());
				}
			}
			return null;
		}

		DynamicMetaObject TryInvertedComparison(OperatorInfo info, OverloadResolverFactory resolverFactory, DynamicMetaObject target, DynamicMetaObject[] args)
		{
			var revInfo = OperatorInfo.GetOperatorInfo(GetInvertedOperator(info.Operator));
			Debug.Assert(revInfo != null);
			// try the 1st type's opposite function result negated 
			var targets = GetApplicableMembers(target.GetLimitType(), revInfo);
			if (targets.Length > 0)
			{
				var t = resolverFactory.CreateOverloadResolver(args, new CallSignature(args.Length), CallTypes.None).ResolveOverload(targets[0].Name, targets, NarrowingLevel.None, NarrowingLevel.All);
				return t.Success ? new DynamicMetaObject(Ast.Not(t.MakeExpression()), t.RestrictedArguments.GetAllRestrictions()) : null;
			}
			else
				return null;
		}

		/// <summary>
		/// Produces a rule for comparing a value to null - supports comparing object references and nullable types.
		/// </summary>
		static DynamicMetaObject TryNullComparisonRule(DynamicMetaObject[] args)
		{
			var otherType = args[1].GetLimitType();
			var restrictions = BindingRestrictionsHelpers.GetRuntimeTypeRestriction(args[0].Expression, args[0].GetLimitType()).Merge(BindingRestrictions.Combine(args));
			if (args[0].GetLimitType() == typeof(DynamicNull))
			{
				if (!otherType.IsValueType)
					return new DynamicMetaObject(Ast.Equal(args[0].Expression, AstUtils.Constant(null)), restrictions);
				else if (TypeUtils.IsNullableType(otherType))
					return new DynamicMetaObject(Ast.Property(args[0].Expression, otherType.GetProperty("HasValue")), restrictions);
			}
			else if (otherType == typeof(DynamicNull))
			{
				if (!args[0].GetLimitType().IsValueType)
					return new DynamicMetaObject(Ast.Equal(args[0].Expression, AstUtils.Constant(null)), restrictions);
				else if (TypeUtils.IsNullableType(args[0].GetLimitType()))
					return new DynamicMetaObject(Ast.Property(args[0].Expression, otherType.GetProperty("HasValue")), restrictions);
			}
			return null;
		}

		static DynamicMetaObject TryPrimitiveCompare(OperatorInfo info, DynamicMetaObject[] args)
		{
			if (TypeUtils.GetNonNullableType(args[0].GetLimitType()) == TypeUtils.GetNonNullableType(args[1].GetLimitType()) && TypeUtils.IsNumeric(args[0].GetLimitType()))
			{
				var arg0 = args[0].Expression;
				var arg1 = args[1].Expression;
				// TODO: Nullable<PrimitveType> Support
				Expression expr;
				switch (info.Operator)
				{
					case ExpressionType.Equal: expr = Ast.Equal(arg0, arg1); break;
					case ExpressionType.NotEqual: expr = Ast.NotEqual(arg0, arg1); break;
					case ExpressionType.GreaterThan: expr = Ast.GreaterThan(arg0, arg1); break;
					case ExpressionType.LessThan: expr = Ast.LessThan(arg0, arg1); break;
					case ExpressionType.GreaterThanOrEqual: expr = Ast.GreaterThanOrEqual(arg0, arg1); break;
					case ExpressionType.LessThanOrEqual: expr = Ast.LessThanOrEqual(arg0, arg1); break;
					default: throw new InvalidOperationException();
				}
				return new DynamicMetaObject(
					expr,
					BindingRestrictionsHelpers.GetRuntimeTypeRestriction(arg0, args[0].GetLimitType()).Merge(BindingRestrictionsHelpers.GetRuntimeTypeRestriction(arg1, args[0].GetLimitType())).Merge(BindingRestrictions.Combine(args))
				);
			}
			return null;
		}

		#endregion

		#region Operator Rule

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")] // TODO: fix
		DynamicMetaObject MakeOperatorRule(OperatorInfo info, OverloadResolverFactory resolverFactory, DynamicMetaObject[] args)
		{
			return
				TryForwardOperator(info, resolverFactory, args) ??
				TryReverseOperator(info, resolverFactory, args) ??
				TryPrimitiveOperator(info, args) ??
				TryMakeDefaultUnaryRule(info, args) ??
				MakeOperatorError(info, args);
		}

		static DynamicMetaObject TryPrimitiveOperator(OperatorInfo info, DynamicMetaObject[] args)
		{
			if (args.Length == 2 && TypeUtils.GetNonNullableType(args[0].GetLimitType()) == TypeUtils.GetNonNullableType(args[1].GetLimitType()) && TypeUtils.IsArithmetic(args[0].GetLimitType()))
			{
				// TODO: Nullable<PrimitveType> Support
				Expression expr;
				var self = args[0].Restrict(args[0].GetLimitType());
				var arg0 = args[1].Restrict(args[0].GetLimitType());
				switch (info.Operator)
				{
					case ExpressionType.Add: expr = Ast.Add(self.Expression, arg0.Expression); break;
					case ExpressionType.Subtract: expr = Ast.Subtract(self.Expression, arg0.Expression); break;
					case ExpressionType.Divide: expr = Ast.Divide(self.Expression, arg0.Expression); break;
					case ExpressionType.Modulo: expr = Ast.Modulo(self.Expression, arg0.Expression); break;
					case ExpressionType.Multiply: expr = Ast.Multiply(self.Expression, arg0.Expression); break;
					case ExpressionType.LeftShift: expr = Ast.LeftShift(self.Expression, arg0.Expression); break;
					case ExpressionType.RightShift: expr = Ast.RightShift(self.Expression, arg0.Expression); break;
					case ExpressionType.And: expr = Ast.And(self.Expression, arg0.Expression); break;
					case ExpressionType.Or: expr = Ast.Or(self.Expression, arg0.Expression); break;
					case ExpressionType.ExclusiveOr: expr = Ast.ExclusiveOr(self.Expression, arg0.Expression); break;
					default: throw new InvalidOperationException();
				}
				return new DynamicMetaObject(expr, self.Restrictions.Merge(arg0.Restrictions));
			}
			return null;
		}

		DynamicMetaObject TryForwardOperator(OperatorInfo info, OverloadResolverFactory resolverFactory, DynamicMetaObject[] args)
		{
			var targets = GetApplicableMembers(args[0].GetLimitType(), info);
			return targets.Length > 0 ? TryMakeBindingTarget(resolverFactory, targets, args, BindingRestrictions.Empty) : null;
		}

		DynamicMetaObject TryReverseOperator(OperatorInfo info, OverloadResolverFactory resolverFactory, DynamicMetaObject[] args)
		{
			// we need a special conversion for the return type on MemberNames
			if (args.Length > 0)
			{
				var targets = GetApplicableMembers(args[0].LimitType, info);
				if (targets.Length > 0)
					return TryMakeBindingTarget(resolverFactory, targets, args, BindingRestrictions.Empty);
			}
			return null;
		}

		static DynamicMetaObject TryMakeDefaultUnaryRule(OperatorInfo info, DynamicMetaObject[] args)
		{
			if (args.Length == 1)
			{
				var restrictions = BindingRestrictionsHelpers.GetRuntimeTypeRestriction(args[0].Expression, args[0].GetLimitType()).Merge(BindingRestrictions.Combine(args));
				switch (info.Operator)
				{
					case ExpressionType.IsTrue:
						if (args[0].GetLimitType() == typeof(bool))
							return args[0];
						break;
					case ExpressionType.Negate:
						if (TypeUtils.IsArithmetic(args[0].GetLimitType()))
							return new DynamicMetaObject(Ast.Negate(args[0].Expression), restrictions);
						break;
					case ExpressionType.Not:
						if (TypeUtils.IsIntegerOrBool(args[0].GetLimitType()))
							return new DynamicMetaObject(Ast.Not(args[0].Expression), restrictions);
						break;
				}
			}
			return null;
		}

		#endregion

		#region Indexer Rule

		static Type GetArgType(DynamicMetaObject[] args, int index) { return args[index].HasValue ? args[index].GetLimitType() : args[index].Expression.Type; }

		DynamicMetaObject MakeMethodIndexRule(IndexType oper, OverloadResolverFactory resolverFactory, DynamicMetaObject[] args)
		{
			var defaults = GetMethodsFromDefaults(args[0].GetLimitType().GetDefaultMembers(), oper);
			if (defaults.Length != 0)
			{
				ParameterExpression arg2 = null;
				if (oper == IndexType.Set)
				{
					Debug.Assert(args.Length >= 2);
					// need to save arg2 in a temp because it's also our result
					arg2 = Ast.Variable(args[2].Expression.Type, "arg2Temp");
					args[2] = new DynamicMetaObject(Ast.Assign(arg2, args[2].Expression), args[2].Restrictions);
				}
				var restrictions = BindingRestrictions.Combine(args);
				var resolver = resolverFactory.CreateOverloadResolver(args, new CallSignature(args.Length), CallTypes.ImplicitInstance);
				var target = resolver.ResolveOverload(oper == IndexType.Get ? "get_Item" : "set_Item", defaults, NarrowingLevel.None, NarrowingLevel.All);
				if (target.Success)
					return oper == IndexType.Get ?
						new DynamicMetaObject(target.MakeExpression(), restrictions.Merge(target.RestrictedArguments.GetAllRestrictions())) :
						new DynamicMetaObject(Ast.Block(new[] { arg2 }, target.MakeExpression(), arg2), restrictions.Merge(target.RestrictedArguments.GetAllRestrictions()));
				return MakeError(resolver.MakeInvalidParametersError(target), restrictions, typeof(object));
			}
			return null;
		}

		DynamicMetaObject MakeArrayIndexRule(OverloadResolverFactory factory, IndexType oper, DynamicMetaObject[] args)
		{
			if (CanConvertFrom(GetArgType(args, 1), typeof(int), false, NarrowingLevel.All))
			{
				var restrictions = BindingRestrictionsHelpers.GetRuntimeTypeRestriction(args[0].Expression, args[0].GetLimitType()).Merge(BindingRestrictions.Combine(args));
				return oper == IndexType.Get ?
					new DynamicMetaObject(
						Ast.ArrayAccess(args[0].Expression, ConvertIfNeeded(factory, args[1].Expression, typeof(int))),
						restrictions
					) :
					new DynamicMetaObject(
						Ast.Assign(
							Ast.ArrayAccess(args[0].Expression, ConvertIfNeeded(factory, args[1].Expression, typeof(int))),
							ConvertIfNeeded(factory, args[2].Expression, args[0].GetLimitType().GetElementType())
						),
						restrictions.Merge(args[1].Restrictions)
					);
			}
			return null;
		}

		MethodInfo[] GetMethodsFromDefaults(MemberInfo[] defaults, IndexType op)
		{
			var methods = defaults.OfType<PropertyInfo>()
				.Select(x => op == IndexType.Get ? x.GetGetMethod(PrivateBinding) : x.GetSetMethod(PrivateBinding))
				.Where(x => x != null);
			// if we received methods from both declaring type & base types we need to filter them
			Dictionary<MethodSignatureInfo, MethodInfo> dict = new Dictionary<MethodSignatureInfo, MethodInfo>();
			foreach (var mb in methods)
			{
				MethodSignatureInfo args = new MethodSignatureInfo(mb);
				MethodInfo other;
				if (!dict.TryGetValue(args, out other) || other.DeclaringType.IsAssignableFrom(mb.DeclaringType))
					dict[args] = mb; // derived type replaces...
			}
			return dict.Values.ToArray();
		}

		#endregion

		#region Common helpers

		DynamicMetaObject TryMakeBindingTarget(OverloadResolverFactory resolverFactory, MethodInfo[] targets, DynamicMetaObject[] args, BindingRestrictions restrictions)
		{
			var target = resolverFactory.CreateOverloadResolver(args, new CallSignature(args.Length), CallTypes.None)
				.ResolveOverload(targets[0].Name, targets, NarrowingLevel.None, NarrowingLevel.All);
			return target.Success ? new DynamicMetaObject(target.MakeExpression(), restrictions.Merge(target.RestrictedArguments.GetAllRestrictions())) : null;
		}

		static ExpressionType GetInvertedOperator(ExpressionType op)
		{
			switch (op)
			{
				case ExpressionType.LessThan: return ExpressionType.GreaterThanOrEqual;
				case ExpressionType.LessThanOrEqual: return ExpressionType.GreaterThan;
				case ExpressionType.GreaterThan: return ExpressionType.LessThanOrEqual;
				case ExpressionType.GreaterThanOrEqual: return ExpressionType.LessThan;
				case ExpressionType.Equal: return ExpressionType.NotEqual;
				case ExpressionType.NotEqual: return ExpressionType.Equal;
				default: throw new InvalidOperationException();
			}
		}

		Expression ConvertIfNeeded(OverloadResolverFactory factory, Expression expression, Type type)
		{
			Assert.NotNull(expression, type);
			return expression.Type != type ? ConvertExpression(expression, type, ConversionResultKind.ExplicitCast, factory) : expression;
		}

		MethodInfo[] GetApplicableMembers(Type t, OperatorInfo info)
		{
			Assert.NotNull(t, info);
			var members = GetMember(MemberRequestKind.Operation, t, info.Name);
			if (members.Count == 0 && info.AlternateName != null)
				members = GetMember(MemberRequestKind.Operation, t, info.AlternateName);
			// filter down to just methods
			return FilterNonMethods(t, members);
		}

		static MethodInfo[] FilterNonMethods(Type t, MemberGroup members)
		{
			Assert.NotNull(t, members);
			return members.Where(x => x.MemberType == TrackerTypes.Method).Select(x => ((MethodTracker)x).Method).Where(x => x.DeclaringType != typeof(object) || t != typeof(DynamicNull)).ToArray();
		}

		#endregion
	}
}
