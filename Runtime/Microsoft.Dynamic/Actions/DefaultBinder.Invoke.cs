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

		/// <summary>�w�肳�ꂽ <see cref="DynamicMetaObject"/> �ɑ΂���Ăяo�������s�������̃o�C���f�B���O��񋟂��܂��B</summary>
		/// <param name="signature">�Ăяo����\���V�O�l�`�����w�肵�܂��B</param>
		/// <param name="target">�Ăяo����� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <param name="args"><paramref name="target"/> ���Ăяo���ۂ̈�����\�� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <returns>�Ăяo���܂��͎��s��\�� <see cref="DynamicMetaObject"/>�B</returns>
		public DynamicMetaObject Invoke(CallSignature signature, DynamicMetaObject target, params DynamicMetaObject[] args) { return Invoke(signature, new DefaultOverloadResolverFactory(this), target, args); }

		/// <summary>�w�肳�ꂽ <see cref="DynamicMetaObject"/> �ɑ΂���Ăяo�������s�������̃o�C���f�B���O��񋟂��܂��B</summary>
		/// <param name="signature">�Ăяo����\���V�O�l�`�����w�肵�܂��B</param>
		/// <param name="target">�Ăяo����� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <param name="args"><paramref name="target"/> ���Ăяo���ۂ̈�����\�� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <param name="resolverFactory">�I�[�o�[���[�h�̉����ƃ��\�b�h�o�C���f�B���O���s�� <see cref="OverloadResolverFactory"/> ���w�肵�܂��B</param>
		/// <returns>�Ăяo���܂��͎��s��\�� <see cref="DynamicMetaObject"/>�B</returns>
		public DynamicMetaObject Invoke(CallSignature signature, OverloadResolverFactory resolverFactory, DynamicMetaObject target, params DynamicMetaObject[] args) { return Invoke(signature, null, resolverFactory, target, args); }

		/// <summary>�w�肳�ꂽ <see cref="DynamicMetaObject"/> �ɑ΂���Ăяo�������s�������̃o�C���f�B���O��񋟂��܂��B</summary>
		/// <param name="signature">�Ăяo����\���V�O�l�`�����w�肵�܂��B</param>
		/// <param name="target">�Ăяo����� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <param name="args"><paramref name="target"/> ���Ăяo���ۂ̈�����\�� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <param name="resolverFactory">�I�[�o�[���[�h�̉����ƃ��\�b�h�o�C���f�B���O���s�� <see cref="OverloadResolverFactory"/> ���w�肵�܂��B</param>
		/// <param name="errorSuggestion">�I�u�W�F�N�g�̌Ăяo���Ɏ��s�����ۂ̌��ʂ��w�肵�܂��B</param>
		/// <returns>�Ăяo���܂��͎��s��\�� <see cref="DynamicMetaObject"/>�B</returns>
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

		/// <summary>���\�b�h�O���[�v���̃��\�b�h�ɑ������܂��B</summary>
		static TargetInfo TryGetMethodGroupTargets(DynamicMetaObject target, DynamicMetaObject[] args)
		{
			var mg = target.Value as MethodGroup;
			if (mg == null)
				return null;
			return new TargetInfo(null, args, BindingRestrictions.GetInstanceRestriction(target.Expression, mg), mg.GetMethodBases());
		}

		/// <summary>�����o�O���[�v���̃��\�b�h�ɑ������܂��B</summary>
		static TargetInfo TryGetMemberGroupTargets(DynamicMetaObject target, DynamicMetaObject[] args)
		{
			var mg = target.Value as MemberGroup;
			if (mg == null)
				return null;
			return new TargetInfo(null, args, BindingRestrictions.GetInstanceRestriction(target.Expression, mg), mg.Where(x => x.MemberType == TrackerTypes.Method).Select(x => ((MethodTracker)x).Method).ToArray());
		}

		/// <summary>�g���b�J�[���̃C���X�^���X���g�p���āA�I�u�W�F�N�g�C���X�^���X�̌^�Ɋ�Â��Đ��񂷂邱�ƂŁA<see cref="BoundMemberTracker"/> �ɑ������܂��B</summary>
		static TargetInfo TryGetBoundMemberTargets(DynamicMetaObject target, DynamicMetaObject[] args)
		{
			var bmt = target.Value as BoundMemberTracker;
			if (bmt == null)
				return null;
			Debug.Assert(bmt.Instance == null); // ���[�U�[�R�[�h�ɘR�ꂽ�g���b�J�[�ɑ΂��Ă� null �ɂ���
			// �C���X�^���X�� BoundMemberTracker ������o����A�K�؂Ȍ^�ɐ��񂳂�܂��B
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
			// ���� BoundMemberTracker �ɑ΂��Ď��s���邱�Ƃ�ۏ؂��邽�߁A������ǉ�����B
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
					throw new InvalidOperationException(); // �܂������������Ă��Ȃ�
			}
		}

		/// <summary>�f���Q�[�g�^�ł���� Invoke ���\�b�h�ɑ������܂��B</summary>
		static TargetInfo TryGetDelegateTargets(DynamicMetaObject target, DynamicMetaObject[] args)
		{
			if (!(target.Value is Delegate))
				return null;
			return new TargetInfo(target, args, target.Value.GetType().GetMethod("Invoke"));
		}

		/// <summary>���Z�q�ł��� Call ���\�b�h�ւ̑��������݂܂��B</summary>
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
		/// �Ăяo���̃^�[�Q�b�g�Ɋւ�������J�v�Z�������܂��B
		/// ����ɂ͌Ăяo���̎��s�ɕK�v�Ȃ����鐧��̂ق��ɁA�Ăяo���̈Öق̃C���X�^���X��Ăяo�����\�b�h���܂܂�܂��B
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
