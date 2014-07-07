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
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions
{
	using Ast = Expression;

	public partial class DefaultBinder : ActionBinder
	{
		/// <summary>
		/// �����o�̎擾�����s���� <see cref="DynamicMetaObject"/> ���\�z���܂��B
		/// ���ׂẴr���g�C�� .NET ���\�b�h�A���Z�q���\�b�h�AGetBoundMember ����� StrongBox �C���X�^���X���T�|�[�g���܂��B
		/// </summary>
		/// <param name="name">
		/// �擾���郁���o�̖��O���w�肵�܂��B
		/// ���̖��O�� <see cref="DefaultBinder"/> �ł͏������ꂸ�A����ɖ��O�}���O�����O�A�啶���Ə���������ʂ��Ȃ������Ȃǂ��s�� GetMember API �ɓn����܂��B
		/// </param>
		/// <param name="target">�����o�[���擾����� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <returns>
		/// �����o���A�N�Z�X���ꂽ�ۂɕԂ����l��\���@<see cref="DynamicMetaObject"/> ��Ԃ��܂��B
		/// �Ԃ���� <see cref="DynamicMetaObject"/> �͕W���� DLR GetMemberBinder ����Ԃ����O�Ƀ{�b�N�X�����K�v�Ȓl�^�Ɍ����Ɍ^�w�肳��Ă���\��������܂��B
		/// ����͂�����{�b�N�X���̎��s�ɐӔC�����̂ŁA�J�X�^���{�b�N�X�������s����@������݂��܂��B
		/// </returns>
		public DynamicMetaObject GetMember(string name, DynamicMetaObject target) { return GetMember(name, target, new DefaultOverloadResolverFactory(this), false, null); }

		/// <summary>
		/// �����o�̎擾�����s���� <see cref="DynamicMetaObject"/> ���\�z���܂��B
		/// ���ׂẴr���g�C�� .NET ���\�b�h�A���Z�q���\�b�h�AGetBoundMember ����� StrongBox �C���X�^���X���T�|�[�g���܂��B
		/// </summary>
		/// <param name="name">
		/// �擾���郁���o�̖��O���w�肵�܂��B
		/// ���̖��O�� <see cref="DefaultBinder"/> �ł͏������ꂸ�A����ɖ��O�}���O�����O�A�啶���Ə���������ʂ��Ȃ������Ȃǂ��s�� GetMember API �ɓn����܂��B
		/// </param>
		/// <param name="target">�����o�[���擾����� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <param name="resolverFactory">
		/// GetMember �̎��s�ɕK�v�Ȃ�����Ăяo���ɑ΂���I�[�o�[���[�h�����ƃ��\�b�h�o�C���f�B���O��񋟂���
		/// <see cref="OverloadResolverFactory"/> ���w�肵�܂��B
		/// </param>
		/// <returns>
		/// �����o���A�N�Z�X���ꂽ�ۂɕԂ����l��\���@<see cref="DynamicMetaObject"/> ��Ԃ��܂��B
		/// �Ԃ���� <see cref="DynamicMetaObject"/> �͕W���� DLR GetMemberBinder ����Ԃ����O�Ƀ{�b�N�X�����K�v�Ȓl�^�Ɍ����Ɍ^�w�肳��Ă���\��������܂��B
		/// ����͂�����{�b�N�X���̎��s�ɐӔC�����̂ŁA�J�X�^���{�b�N�X�������s����@������݂��܂��B
		/// </returns>
		public DynamicMetaObject GetMember(string name, DynamicMetaObject target, OverloadResolverFactory resolverFactory) { return GetMember(name, target, resolverFactory, false, null); }

		/// <summary>
		/// �����o�̎擾�����s���� <see cref="DynamicMetaObject"/> ���\�z���܂��B
		/// ���ׂẴr���g�C�� .NET ���\�b�h�A���Z�q���\�b�h�AGetBoundMember ����� StrongBox �C���X�^���X���T�|�[�g���܂��B
		/// </summary>
		/// <param name="name">
		/// �擾���郁���o�̖��O���w�肵�܂��B
		/// ���̖��O�� <see cref="DefaultBinder"/> �ł͏������ꂸ�A����ɖ��O�}���O�����O�A�啶���Ə���������ʂ��Ȃ������Ȃǂ��s�� GetMember API �ɓn����܂��B
		/// </param>
		/// <param name="target">�����o�[���擾����� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <param name="resolverFactory">
		/// GetMember �̎��s�ɕK�v�Ȃ�����Ăяo���ɑ΂���I�[�o�[���[�h�����ƃ��\�b�h�o�C���f�B���O��񋟂���
		/// <see cref="OverloadResolverFactory"/> ���w�肵�܂��B
		/// </param>
		/// <param name="isNoThrow">���삪���s�����ۂɗ�O���X���[�����A�P�Ɏ��s��\���l��Ԃ����ǂ����������l���w�肵�܂��B</param>
		/// <param name="errorSuggestion">�擾���삪�G���[�ɂȂ����ۂɎg�p����� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <returns>
		/// �����o���A�N�Z�X���ꂽ�ۂɕԂ����l��\���@<see cref="DynamicMetaObject"/> ��Ԃ��܂��B
		/// �Ԃ���� <see cref="DynamicMetaObject"/> �͕W���� DLR GetMemberBinder ����Ԃ����O�Ƀ{�b�N�X�����K�v�Ȓl�^�Ɍ����Ɍ^�w�肳��Ă���\��������܂��B
		/// ����͂�����{�b�N�X���̎��s�ɐӔC�����̂ŁA�J�X�^���{�b�N�X�������s����@������݂��܂��B
		/// </returns>
		public DynamicMetaObject GetMember(string name, DynamicMetaObject target, OverloadResolverFactory resolverFactory, bool isNoThrow, DynamicMetaObject errorSuggestion)
		{
			ContractUtils.RequiresNotNull(name, "name");
			ContractUtils.RequiresNotNull(target, "target");
			ContractUtils.RequiresNotNull(resolverFactory, "resolverFactory");
			return MakeGetMemberTarget(new GetMemberInfo(name, resolverFactory, isNoThrow, errorSuggestion), target);
		}

		/// <summary>
		/// �����o�̎擾�����s���� <see cref="DynamicMetaObject"/> ���\�z���܂��B
		/// ���ׂẴr���g�C�� .NET ���\�b�h�A���Z�q���\�b�h�AGetBoundMember ����� StrongBox �C���X�^���X���T�|�[�g���܂��B
		/// </summary>
		/// <param name="name">
		/// �擾���郁���o�̖��O���w�肵�܂��B
		/// ���̖��O�� <see cref="DefaultBinder"/> �ł͏������ꂸ�A����ɖ��O�}���O�����O�A�啶���Ə���������ʂ��Ȃ������Ȃǂ��s�� GetMember API �ɓn����܂��B
		/// </param>
		/// <param name="target">�����o�[���擾����� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <param name="isNoThrow">���삪���s�����ۂɗ�O���X���[�����A�P�Ɏ��s��\���l��Ԃ����ǂ����������l���w�肵�܂��B</param>
		/// <param name="errorSuggestion">�擾���삪�G���[�ɂȂ����ۂɎg�p����� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <returns>
		/// �����o���A�N�Z�X���ꂽ�ۂɕԂ����l��\���@<see cref="DynamicMetaObject"/> ��Ԃ��܂��B
		/// �Ԃ���� <see cref="DynamicMetaObject"/> �͕W���� DLR GetMemberBinder ����Ԃ����O�Ƀ{�b�N�X�����K�v�Ȓl�^�Ɍ����Ɍ^�w�肳��Ă���\��������܂��B
		/// ����͂�����{�b�N�X���̎��s�ɐӔC�����̂ŁA�J�X�^���{�b�N�X�������s����@������݂��܂��B
		/// </returns>
		public DynamicMetaObject GetMember(string name, DynamicMetaObject target, bool isNoThrow, DynamicMetaObject errorSuggestion)
		{
			ContractUtils.RequiresNotNull(name, "name");
			ContractUtils.RequiresNotNull(target, "target");
			return MakeGetMemberTarget(new GetMemberInfo(name, new DefaultOverloadResolverFactory(this), isNoThrow, errorSuggestion), target);
		}

		DynamicMetaObject MakeGetMemberTarget(GetMemberInfo getMemInfo, DynamicMetaObject target)
		{
			var type = target.GetLimitType();
			var restrictions = target.Restrictions;
			var self = target;
			target = target.Restrict(target.GetLimitType());
			// ���ʂɔF�������^: TypeTracker, NamespaceTracker, StrongBox
			// TODO: TypeTracker ����� NamespaceTracker �͋Z�p�I�� IDO �ɂ���B
			var members = MemberGroup.EmptyGroup;
			if (typeof(TypeTracker).IsAssignableFrom(type))
			{
				restrictions = restrictions.Merge(BindingRestrictions.GetInstanceRestriction(target.Expression, target.Value));
				var tg = target.Value as TypeGroup;
				Type nonGen;
				if ((tg == null || tg.TryGetNonGenericType(out nonGen)) && (members = GetMember(MemberRequestKind.Get, ((TypeTracker)target.Value).Type, getMemInfo.Name)).Count > 0)
				{
					// �g���b�J�[�Ɋ֘A�t����ꂽ�^�ɂ��郁���o�������Ă���Ȃ�A�����Ԃ��B
					type = ((TypeTracker)target.Value).Type;
					self = null;
				}
			}
			if (members.Count == 0)
				members = GetMember(MemberRequestKind.Get, type, getMemInfo.Name); // Get the members
			if (members.Count == 0)
			{
				if (typeof(TypeTracker).IsAssignableFrom(type))
					// �W�F�l���b�N�łȂ��^���Ȃ����Ƃ��m�F���A����΃G���[��񍐂���B
					// ����͊���̃o�C���_�[�̃��[���o�[�W�����ɓK�����邪�A�����I�ɍ폜�����ׂ����̂Ǝv����
					System.Diagnostics.Debug.WriteLine(((TypeTracker)target.Value).Type);
				else if (type.IsInterface)
					members = GetMember(MemberRequestKind.Get, type = typeof(object), getMemInfo.Name); // ���ׂẴC���^�[�t�F�C�X�� object �����o�������Ă���
			}
			var propSelf = self;
			// �������������s������A���p�\�ł���� StrongBox �Ŏ����B
			if (members.Count == 0 && typeof(IStrongBox).IsAssignableFrom(type) && propSelf != null)
			{
				// �v���p�e�B/�t�B�[���h�͒��ڂ̒l��K�v�Ƃ��邽�߁A���\�b�h�� StrongBox �ɕێ�����B
				propSelf = new DynamicMetaObject(Ast.Field(AstUtils.Convert(propSelf.Expression, type), type.GetField("Value")), propSelf.Restrictions, ((IStrongBox)propSelf.Value).Value);
				type = type.GetGenericArguments()[0];
				members = GetMember(MemberRequestKind.Get, type, getMemInfo.Name);
			}
			MakeBodyHelper(getMemInfo, self, propSelf, type, members);
			getMemInfo.Body.Restrictions = restrictions;
			return getMemInfo.Body.GetMetaObject(target);
		}

		void MakeBodyHelper(GetMemberInfo getMemInfo, DynamicMetaObject self, DynamicMetaObject propSelf, Type type, MemberGroup members)
		{
			if (self != null)
				MakeOperatorGetMemberBody(getMemInfo, propSelf, type, "GetCustomMember");
			Expression error;
			var memberType = GetMemberType(members, out error);
			if (error == null)
				MakeSuccessfulMemberAccess(getMemInfo, self, propSelf, type, members, memberType);
			else
				getMemInfo.Body.FinishCondition(getMemInfo.ErrorSuggestion != null ? getMemInfo.ErrorSuggestion.Expression : error);
		}

		void MakeSuccessfulMemberAccess(GetMemberInfo getMemInfo, DynamicMetaObject self, DynamicMetaObject propSelf, Type type, MemberGroup members, TrackerTypes memberType)
		{
			switch (memberType)
			{
				case TrackerTypes.TypeGroup:
				case TrackerTypes.Type:
					getMemInfo.Body.FinishCondition(members.Skip(1).Aggregate((TypeTracker)members.First(), (x, y) => TypeGroup.Merge(x, (TypeTracker)y)).GetValue(getMemInfo.ResolutionFactory, this, type));
					break;
				case TrackerTypes.Method:
					// MethodGroup �ɂȂ�        
					MakeGenericBodyWorker(getMemInfo, type, ReflectionCache.GetMethodGroup(getMemInfo.Name, members), self);
					break;
				case TrackerTypes.Event:
				case TrackerTypes.Field:
				case TrackerTypes.Property:
				case TrackerTypes.Constructor:
				case TrackerTypes.Custom:
					// ���������̃����o�[���^����ꂽ��A���̌^�Ɉ�ԋ߂������o��T��
					MakeGenericBodyWorker(getMemInfo, type, members.Aggregate((w, x) => !IsTrackerApplicableForType(type, x) && (x.DeclaringType.IsSubclassOf(w.DeclaringType) || !IsTrackerApplicableForType(type, w)) ? x : w), propSelf);
					break;
				case TrackerTypes.All:
					// �ǂ̃����o��������Ȃ�����
					if (self != null)
						MakeOperatorGetMemberBody(getMemInfo, propSelf, type, "GetBoundMember");
					MakeMissingMemberRuleForGet(getMemInfo, self, type);
					break;
				default:
					throw new InvalidOperationException(memberType.ToString());
			}
		}

		static bool IsTrackerApplicableForType(Type type, MemberTracker mt) { return mt.DeclaringType == type || type.IsSubclassOf(mt.DeclaringType); }

		void MakeGenericBodyWorker(GetMemberInfo getMemInfo, Type type, MemberTracker tracker, DynamicMetaObject instance)
		{
			if (instance != null)
				tracker = tracker.BindToInstance(instance);
			var val = tracker.GetValue(getMemInfo.ResolutionFactory, this, type);
			if (val != null)
				getMemInfo.Body.FinishCondition(val);
			else if (tracker.GetError(this).Kind != ErrorInfoKind.Success && getMemInfo.IsNoThrow)
				getMemInfo.Body.FinishCondition(MakeOperationFailed());
			else
				getMemInfo.Body.FinishCondition(MakeError(tracker.GetError(this), typeof(object)));
		}

		void MakeOperatorGetMemberBody(GetMemberInfo getMemInfo, DynamicMetaObject instance, Type type, string name)
		{
			var getMem = GetMethod(type, name);
			if (getMem != null)
			{
				var tmp = Ast.Variable(typeof(object), "getVal");
				getMemInfo.Body.AddVariable(tmp);
				getMemInfo.Body.AddCondition(
					Ast.NotEqual(
						Ast.Assign(
							tmp,
							MakeCallExpression(
								getMemInfo.ResolutionFactory,
								getMem,
								new DynamicMetaObject(Ast.Convert(instance.Expression, type), instance.Restrictions, instance.Value),
								new DynamicMetaObject(Ast.Constant(getMemInfo.Name), BindingRestrictions.Empty, getMemInfo.Name)
							).Expression
						),
						Ast.Field(null, typeof(OperationFailed).GetField("Value"))
					),
					tmp
				);
			}
		}

		void MakeMissingMemberRuleForGet(GetMemberInfo getMemInfo, DynamicMetaObject self, Type type)
		{
			if (getMemInfo.ErrorSuggestion != null)
				getMemInfo.Body.FinishCondition(getMemInfo.ErrorSuggestion.Expression);
			else if (getMemInfo.IsNoThrow)
				getMemInfo.Body.FinishCondition(MakeOperationFailed());
			else
				getMemInfo.Body.FinishCondition(MakeError(MakeMissingMemberError(type, self, getMemInfo.Name), typeof(object)));
		}

		static MemberExpression MakeOperationFailed() { return Ast.Field(null, typeof(OperationFailed).GetField("Value")); }

		sealed class GetMemberInfo
		{
			public readonly string Name;
			public readonly OverloadResolverFactory ResolutionFactory;
			public readonly bool IsNoThrow;
			public readonly ConditionalBuilder Body = new ConditionalBuilder();
			public readonly DynamicMetaObject ErrorSuggestion;

			public GetMemberInfo(string name, OverloadResolverFactory resolutionFactory, bool noThrow, DynamicMetaObject errorSuggestion)
			{
				Name = name;
				ResolutionFactory = resolutionFactory;
				IsNoThrow = noThrow;
				ErrorSuggestion = errorSuggestion;
			}
		}
	}
}
