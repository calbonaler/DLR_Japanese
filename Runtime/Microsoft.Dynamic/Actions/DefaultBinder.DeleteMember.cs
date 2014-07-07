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
		/// <summary>�w�肳�ꂽ�C���X�^���X�̎w�肳�ꂽ���O���������o���폜���܂��B</summary>
		/// <param name="name">�폜���郁���o�̖��O���w�肵�܂��B</param>
		/// <param name="target">�폜���郁���o��ێ����Ă���C���X�^���X���w�肵�܂��B</param>
		/// <returns>�����o�̍폜��\�� <see cref="System.Linq.Expressions.Expression"/> ��ێ����Ă��� <see cref="DynamicMetaObject"/>�B</returns>
		public DynamicMetaObject DeleteMember(string name, DynamicMetaObject target) { return DeleteMember(name, target, new DefaultOverloadResolverFactory(this)); }

		/// <summary>�w�肳�ꂽ�C���X�^���X�̎w�肳�ꂽ���O���������o���폜���܂��B</summary>
		/// <param name="name">�폜���郁���o�̖��O���w�肵�܂��B</param>
		/// <param name="target">�폜���郁���o��ێ����Ă���C���X�^���X���w�肵�܂��B</param>
		/// <param name="resolutionFactory">�폜�̎��s�Ɏg�p����� <see cref="OverloadResolverFactory"/> ���w�肵�܂��B</param>
		/// <returns>�����o�̍폜��\�� <see cref="System.Linq.Expressions.Expression"/> ��ێ����Ă��� <see cref="DynamicMetaObject"/>�B</returns>
		public DynamicMetaObject DeleteMember(string name, DynamicMetaObject target, OverloadResolverFactory resolutionFactory) { return DeleteMember(name, target, resolutionFactory, null); }

		/// <summary>�w�肳�ꂽ�C���X�^���X�̎w�肳�ꂽ���O���������o���폜���܂��B</summary>
		/// <param name="name">�폜���郁���o�̖��O���w�肵�܂��B</param>
		/// <param name="target">�폜���郁���o��ێ����Ă���C���X�^���X���w�肵�܂��B</param>
		/// <param name="resolutionFactory">�폜�̎��s�Ɏg�p����� <see cref="OverloadResolverFactory"/> ���w�肵�܂��B</param>
		/// <param name="errorSuggestion">�폜�����s���ꂽ�ꍇ�ɕԂ���� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <returns>�����o�̍폜��\�� <see cref="System.Linq.Expressions.Expression"/> ��ێ����Ă��� <see cref="DynamicMetaObject"/>�B</returns>
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

		/// <summary>�����o�[�C���W�F�N�^�[�����̌^�ɒ�`����Ă��邩�A�֘A�t�����Ă���ΌĂяo���܂��B</summary>
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
