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
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
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
		/// <summary>メンバの設定を実行する <see cref="DynamicMetaObject"/> を構築します。</summary>
		/// <param name="name">設定するメンバの名前を指定します。</param>
		/// <param name="target">メンバが設定される <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <param name="value">対象のメンバに設定される値を指定します。</param>
		/// <returns>メンバの設定を実行する <see cref="DynamicMetaObject"/>。</returns>
		public DynamicMetaObject SetMember(string name, DynamicMetaObject target, DynamicMetaObject value) { return SetMember(name, target, value, new DefaultOverloadResolverFactory(this)); }

		/// <summary>メンバの設定を実行する <see cref="DynamicMetaObject"/> を構築します。</summary>
		/// <param name="name">設定するメンバの名前を指定します。</param>
		/// <param name="target">メンバが設定される <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <param name="value">対象のメンバに設定される値を指定します。</param>
		/// <param name="resolverFactory">SetMember に必要なあらゆる呼び出しに関するオーバーロード解決とメソッドバインディングを提供する <see cref="OverloadResolverFactory"/> を指定します。</param>
		/// <returns>メンバの設定を実行する <see cref="DynamicMetaObject"/>。</returns>
		public DynamicMetaObject SetMember(string name, DynamicMetaObject target, DynamicMetaObject value, OverloadResolverFactory resolverFactory)
		{
			ContractUtils.RequiresNotNull(name, "name");
			ContractUtils.RequiresNotNull(target, "target");
			ContractUtils.RequiresNotNull(value, "value");
			ContractUtils.RequiresNotNull(resolverFactory, "resolverFactory");
			return MakeSetMemberTarget(new SetOrDeleteMemberInfo(name, resolverFactory), target, value, null);
		}

		/// <summary>メンバの設定を実行する <see cref="DynamicMetaObject"/> を構築します。</summary>
		/// <param name="name">設定するメンバの名前を指定します。</param>
		/// <param name="target">メンバが設定される <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <param name="value">対象のメンバに設定される値を指定します。</param>
		/// <param name="errorSuggestion">
		/// メンバが設定できない場合に結果として使用される <see cref="DynamicMetaObject"/> を指定します。
		/// <c>null</c> の場合は、言語によってオーバーライドされた <see cref="ActionBinder.MakeMissingMemberErrorForAssign"/> によって提供される言語特有のエラーコードが提供されます。
		/// </param>
		/// <returns>メンバの設定を実行する <see cref="DynamicMetaObject"/>。</returns>
		public DynamicMetaObject SetMember(string name, DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject errorSuggestion) { return SetMember(name, target, value, errorSuggestion, new DefaultOverloadResolverFactory(this)); }

		/// <summary>メンバの設定を実行する <see cref="DynamicMetaObject"/> を構築します。</summary>
		/// <param name="name">設定するメンバの名前を指定します。</param>
		/// <param name="target">メンバが設定される <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <param name="value">対象のメンバに設定される値を指定します。</param>
		/// <param name="resolverFactory">SetMember に必要なあらゆる呼び出しに関するオーバーロード解決とメソッドバインディングを提供する <see cref="OverloadResolverFactory"/> を指定します。</param>
		/// <param name="errorSuggestion">
		/// メンバが設定できない場合に結果として使用される <see cref="DynamicMetaObject"/> を指定します。
		/// <c>null</c> の場合は、言語によってオーバーライドされた <see cref="ActionBinder.MakeMissingMemberErrorForAssign"/> によって提供される言語特有のエラーコードが提供されます。
		/// </param>
		/// <returns>メンバの設定を実行する <see cref="DynamicMetaObject"/>。</returns>
		public DynamicMetaObject SetMember(string name, DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject errorSuggestion, OverloadResolverFactory resolverFactory)
		{
			ContractUtils.RequiresNotNull(name, "name");
			ContractUtils.RequiresNotNull(target, "target");
			ContractUtils.RequiresNotNull(value, "value");
			ContractUtils.RequiresNotNull(resolverFactory, "resolverFactory");
			return MakeSetMemberTarget(new SetOrDeleteMemberInfo(name, resolverFactory), target, value, errorSuggestion);
		}

		DynamicMetaObject MakeSetMemberTarget(SetOrDeleteMemberInfo memInfo, DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject errorSuggestion)
		{
			var type = target.GetLimitType();
			var self = target;
			target = target.Restrict(target.GetLimitType());
			memInfo.Body.Restrictions = target.Restrictions;
			if (typeof(TypeTracker).IsAssignableFrom(type))
			{
				type = ((TypeTracker)target.Value).Type;
				self = null;
				memInfo.Body.Restrictions = memInfo.Body.Restrictions.Merge(BindingRestrictions.GetInstanceRestriction(target.Expression, target.Value));
			}
			MakeSetMemberRule(memInfo, type, self, value, errorSuggestion);
			return memInfo.Body.GetMetaObject(target, value);
		}

		void MakeSetMemberRule(SetOrDeleteMemberInfo memInfo, Type type, DynamicMetaObject self, DynamicMetaObject value, DynamicMetaObject errorSuggestion)
		{
			if (MakeOperatorSetMemberBody(memInfo, self, value, type, "SetMember"))
				return;
			var members = GetMember(MemberRequestKind.Set, type, memInfo.Name);
			// if lookup failed try the strong-box type if available.
			if (self != null && members.Count == 0 && typeof(IStrongBox).IsAssignableFrom(type))
			{
				self = new DynamicMetaObject(Ast.Field(AstUtils.Convert(self.Expression, type), type.GetField("Value")), BindingRestrictions.Empty, ((IStrongBox)self.Value).Value);
				type = type.GetGenericArguments()[0];
				members = GetMember(MemberRequestKind.Set, type, memInfo.Name);
			}
			Expression error;
			var memberTypes = GetMemberType(members, out error);
			if (error == null)
			{
				switch (memberTypes)
				{
					case TrackerTypes.Method:
					case TrackerTypes.TypeGroup:
					case TrackerTypes.Type:
					case TrackerTypes.Constructor:
						memInfo.Body.FinishCondition(MakeError(MakeReadOnlyMemberError(type, memInfo.Name), BindingRestrictions.Empty, typeof(object)));
						break;
					case TrackerTypes.Event:
						memInfo.Body.FinishCondition(MakeError(MakeEventValidation(members, self, value, memInfo.ResolutionFactory), BindingRestrictions.Empty, typeof(object)));
						break;
					case TrackerTypes.Field:
						MakeFieldRule(memInfo, self, value, type, members);
						break;
					case TrackerTypes.Property:
						MakePropertyRule(memInfo, self, value, type, members);
						break;
					case TrackerTypes.Custom:
						memInfo.Body.FinishCondition((self != null ? members[0].BindToInstance(self) : members[0]).SetValue(memInfo.ResolutionFactory, this, type, value) ?? MakeError(members[0].GetError(this), typeof(object)));
						break;
					case TrackerTypes.All:
						// no match
						if (MakeOperatorSetMemberBody(memInfo, self, value, type, "SetMemberAfter"))
							return;
						memInfo.Body.FinishCondition(errorSuggestion ?? MakeError(MakeMissingMemberErrorForAssign(type, self, memInfo.Name), BindingRestrictions.Empty, typeof(object)));
						break;
					default:
						throw new InvalidOperationException();
				}
			}
			else
				memInfo.Body.FinishCondition(error);
		}

		void MakePropertyRule(SetOrDeleteMemberInfo memInfo, DynamicMetaObject instance, DynamicMetaObject target, Type targetType, MemberGroup properties)
		{
			var info = (PropertyTracker)properties[0];
			var setter = info.GetSetMethod(true);
			// Allow access to protected getters TODO: this should go, it supports IronPython semantics.
			if (setter != null && !setter.IsPublic && !setter.IsProtected() && !PrivateBinding)
				setter = null;
			if (setter != null)
			{
				setter = CompilerHelpers.GetCallableMethod(setter, PrivateBinding);
				if (info.IsStatic != (instance == null))
					memInfo.Body.FinishCondition(MakeError(MakeStaticPropertyInstanceAccessError(info, true, instance, target), typeof(object)));
				else if (info.IsStatic && info.DeclaringType != targetType)
					memInfo.Body.FinishCondition(MakeError(MakeStaticAssignFromDerivedTypeError(targetType, instance, info, target, memInfo.ResolutionFactory), typeof(object)));
				else if (setter.ContainsGenericParameters)
					memInfo.Body.FinishCondition(Ast.New(typeof(MemberAccessException).GetConstructor(new[] { typeof(string) }), AstUtils.Constant(memInfo.Name)));
				else if (setter.IsPublic && !setter.DeclaringType.IsValueType)
				{
					if (instance == null)
						memInfo.Body.FinishCondition(
							Ast.Block(
								AstUtils.SimpleCallHelper(
									setter,
									ConvertExpression(target.Expression, setter.GetParameters()[0].ParameterType, ConversionResultKind.ExplicitCast, memInfo.ResolutionFactory)
								),
								Ast.Constant(null)
							)
						);
					else
						memInfo.Body.FinishCondition(MakeReturnValue(MakeCallExpression(memInfo.ResolutionFactory, setter, instance, target), target));
				}
				else
					// TODO: Should be able to do better w/ value types.
					memInfo.Body.FinishCondition(
						MakeReturnValue(
							Ast.Call(
								AstUtils.Constant(((ReflectedPropertyTracker)info).Property), // TODO: Private binding on extension properties
								typeof(PropertyInfo).GetMethod("SetValue", new[] { typeof(object), typeof(object), typeof(object[]) }),
								instance == null ? AstUtils.Constant(null) : AstUtils.Convert(instance.Expression, typeof(object)),
								AstUtils.Convert(
									ConvertExpression(
										target.Expression,
										setter.GetParameters()[0].ParameterType,
										ConversionResultKind.ExplicitCast,
										memInfo.ResolutionFactory
									),
									typeof(object)
								),
								Ast.NewArrayInit(typeof(object))
							),
							target
						)
					);
			}
			else
				memInfo.Body.FinishCondition(MakeError(MakeMissingMemberErrorForAssignReadOnlyProperty(targetType, instance, memInfo.Name), typeof(object)));
		}

		void MakeFieldRule(SetOrDeleteMemberInfo memInfo, DynamicMetaObject instance, DynamicMetaObject target, Type targetType, MemberGroup fields)
		{
			var field = (FieldTracker)fields[0];
			// TODO: Tmp variable for target
			if (instance != null && field.DeclaringType.IsGenericType && field.DeclaringType.GetGenericTypeDefinition() == typeof(StrongBox<>))
				// work around a CLR bug where we can't access generic fields from dynamic methods.
				memInfo.Body.FinishCondition(
					MakeReturnValue(
						Ast.Assign(
							Ast.Field(AstUtils.Convert(instance.Expression, field.DeclaringType), field.DeclaringType.GetField("Value")),
							AstUtils.Convert(target.Expression, field.DeclaringType.GetGenericArguments()[0])
						),
						target
					)
				);
			else if (field.IsInitOnly || field.IsLiteral)
				memInfo.Body.FinishCondition(MakeError(MakeReadOnlyMemberError(targetType, memInfo.Name), typeof(object)));
			else if (field.IsStatic && targetType != field.DeclaringType)
				memInfo.Body.FinishCondition(MakeError(MakeStaticAssignFromDerivedTypeError(targetType, instance, field, target, memInfo.ResolutionFactory), typeof(object)));
			else if (field.DeclaringType.IsValueType && !field.IsStatic)
				memInfo.Body.FinishCondition(MakeError(MakeSetValueTypeFieldError(field, instance, target), typeof(object)));
			else if (field.IsPublic && field.DeclaringType.IsVisible)
				memInfo.Body.FinishCondition(!field.IsStatic && instance == null ?
					Ast.Throw(
						Ast.New(typeof(ArgumentException).GetConstructor(new[] { typeof(string) }), AstUtils.Constant("assignment to instance field w/o instance")),
						typeof(object)
					) : MakeReturnValue(
						Ast.Assign(
							Ast.Field(field.IsStatic ? null : AstUtils.Convert(instance.Expression, field.DeclaringType), field.Field),
							ConvertExpression(target.Expression, field.FieldType, ConversionResultKind.ExplicitCast, memInfo.ResolutionFactory)
						),
						target
					)
				);
			else
			{
				Debug.Assert(field.IsStatic || instance != null);
				memInfo.Body.FinishCondition(
					MakeReturnValue(
						Ast.Call(
							AstUtils.Convert(AstUtils.Constant(field.Field), typeof(FieldInfo)),
							typeof(FieldInfo).GetMethod("SetValue", new[] { typeof(object), typeof(object) }),
							field.IsStatic ? AstUtils.Constant(null) : AstUtils.Convert(instance.Expression, typeof(object)),
							AstUtils.Convert(target.Expression, typeof(object))
						),
						target
					)
				);
			}
		}

		DynamicMetaObject MakeReturnValue(DynamicMetaObject expression, DynamicMetaObject target) { return new DynamicMetaObject(Ast.Block(expression.Expression, Ast.Convert(target.Expression, typeof(object))), target.Restrictions.Merge(expression.Restrictions)); }

		Expression MakeReturnValue(Expression expression, DynamicMetaObject target) { return Ast.Block(expression, Ast.Convert(target.Expression, typeof(object))); }

		bool MakeOperatorSetMemberBody(SetOrDeleteMemberInfo memInfo, DynamicMetaObject self, DynamicMetaObject target, Type type, string name)
		{
			if (self != null)
			{
				var setMem = GetMethod(type, name);
				if (setMem != null)
				{
					var tmp = Ast.Variable(target.Expression.Type, "setValue");
					memInfo.Body.AddVariable(tmp);
					var call = Ast.Block(Ast.Assign(tmp, target.Expression), MakeCallExpression(
						memInfo.ResolutionFactory,
						setMem,
						self.Clone(AstUtils.Convert(self.Expression, type)),
						new DynamicMetaObject(AstUtils.Constant(memInfo.Name), BindingRestrictions.Empty, memInfo.Name),
						target.Clone(tmp)
					).Expression);
					if (setMem.ReturnType == typeof(bool))
						memInfo.Body.AddCondition(call, tmp);
					else
						memInfo.Body.FinishCondition(Ast.Block(call, AstUtils.Convert(tmp, typeof(object))));
					return setMem.ReturnType != typeof(bool);
				}
			}
			return false;
		}
	}
}
