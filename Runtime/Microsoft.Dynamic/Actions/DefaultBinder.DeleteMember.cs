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
using System.Linq;
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions
{
	public partial class DefaultBinder : ActionBinder
	{
		/// <summary>指定されたインスタンスの指定された名前をもつメンバを削除します。</summary>
		/// <param name="name">削除するメンバの名前を指定します。</param>
		/// <param name="target">削除するメンバを保持しているインスタンスを指定します。</param>
		/// <returns>メンバの削除を表す <see cref="System.Linq.Expressions.Expression"/> を保持している <see cref="DynamicMetaObject"/>。</returns>
		public DynamicMetaObject DeleteMember(string name, DynamicMetaObject target) { return DeleteMember(name, target, new DefaultOverloadResolverFactory(this)); }

		/// <summary>指定されたインスタンスの指定された名前をもつメンバを削除します。</summary>
		/// <param name="name">削除するメンバの名前を指定します。</param>
		/// <param name="target">削除するメンバを保持しているインスタンスを指定します。</param>
		/// <param name="resolutionFactory">削除の実行に使用される <see cref="OverloadResolverFactory"/> を指定します。</param>
		/// <returns>メンバの削除を表す <see cref="System.Linq.Expressions.Expression"/> を保持している <see cref="DynamicMetaObject"/>。</returns>
		public DynamicMetaObject DeleteMember(string name, DynamicMetaObject target, OverloadResolverFactory resolutionFactory) { return DeleteMember(name, target, resolutionFactory, null); }

		/// <summary>指定されたインスタンスの指定された名前をもつメンバを削除します。</summary>
		/// <param name="name">削除するメンバの名前を指定します。</param>
		/// <param name="target">削除するメンバを保持しているインスタンスを指定します。</param>
		/// <param name="resolutionFactory">削除の実行に使用される <see cref="OverloadResolverFactory"/> を指定します。</param>
		/// <param name="errorSuggestion">削除が失敗された場合に返される <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <returns>メンバの削除を表す <see cref="System.Linq.Expressions.Expression"/> を保持している <see cref="DynamicMetaObject"/>。</returns>
		public DynamicMetaObject DeleteMember(string name, DynamicMetaObject target, OverloadResolverFactory resolutionFactory, DynamicMetaObject errorSuggestion)
		{
			ContractUtils.RequiresNotNull(name, "name");
			ContractUtils.RequiresNotNull(target, "target");
			return MakeDeleteMemberTarget(
				new SetOrDeleteMemberInfo(name, resolutionFactory),
				target.Restrict(target.GetLimitType()),
				errorSuggestion
			);
		}

		DynamicMetaObject MakeDeleteMemberTarget(SetOrDeleteMemberInfo delInfo, DynamicMetaObject target, DynamicMetaObject errorSuggestion)
		{
			var type = target.GetLimitType();
			var restrictions = target.Restrictions;
			var self = target;
			if (typeof(TypeTracker).IsAssignableFrom(type))
			{
				restrictions = restrictions.Merge(BindingRestrictions.GetInstanceRestriction(target.Expression, target.Value));
				type = ((TypeTracker)target.Value).Type;
				self = null;
			}
			delInfo.Body.Restrictions = restrictions;
			if (self == null || !MakeOperatorDeleteMemberBody(delInfo, self, type, "DeleteMember"))
			{
				var group = GetMember(MemberRequestKind.Delete, type, delInfo.Name);
				if (group.Count != 0)
				{
					if (group[0].MemberType == TrackerTypes.Property)
					{
						var del = ((PropertyTracker)group[0]).GetDeleteMethod(PrivateBinding);
						if (del != null)
						{
							delInfo.Body.FinishCondition(self == null ?
								MakeCallExpression(delInfo.ResolutionFactory, del) :
								MakeCallExpression(delInfo.ResolutionFactory, del, self)
							);
							return delInfo.Body.GetMetaObject(target);
						}
					}
					delInfo.Body.FinishCondition(errorSuggestion ?? MakeError(MakeUndeletableMemberError(GetDeclaringMemberType(group), delInfo.Name), typeof(void)));
				}
				else
					delInfo.Body.FinishCondition(errorSuggestion ?? MakeError(MakeMissingMemberErrorForDelete(type, self, delInfo.Name), typeof(void)));
			}
			return delInfo.Body.GetMetaObject(target);
		}

		static Type GetDeclaringMemberType(MemberGroup group) { return group.Aggregate(typeof(object), (x, y) => x.IsAssignableFrom(y.DeclaringType) ? y.DeclaringType : x); }

		/// <summary>メンバーインジェクターがこの型に定義されているか、関連付けられていれば呼び出します。</summary>
		bool MakeOperatorDeleteMemberBody(SetOrDeleteMemberInfo delInfo, DynamicMetaObject instance, Type type, string name)
		{
			var delMem = GetMethod(type, name);
			if (delMem != null)
			{
				var call = MakeCallExpression(delInfo.ResolutionFactory, delMem, instance, new DynamicMetaObject(AstUtils.Constant(delInfo.Name), BindingRestrictions.Empty, delInfo.Name));
				if (delMem.ReturnType == typeof(bool))
					delInfo.Body.AddCondition(call.Expression, AstUtils.Constant(null));
				else
					delInfo.Body.FinishCondition(call);
				return delMem.ReturnType != typeof(bool);
			}
			return false;
		}

		sealed class SetOrDeleteMemberInfo
		{
			public readonly string Name;
			public readonly OverloadResolverFactory ResolutionFactory;
			public readonly ConditionalBuilder Body = new ConditionalBuilder();
			public SetOrDeleteMemberInfo(string name, OverloadResolverFactory resolutionFactory)
			{
				Name = name;
				ResolutionFactory = resolutionFactory;
			}
		}
	}
}
