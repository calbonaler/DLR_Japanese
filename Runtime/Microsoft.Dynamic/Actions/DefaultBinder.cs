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
using System.Reflection;
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Runtime;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions
{
	using Ast = Expression;

	/// <summary><see cref="ActionBinder"/> �̊���̎�����񋟂��܂��B</summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
	public partial class DefaultBinder : ActionBinder
	{
		/// <summary><see cref="DefaultBinder"/> �̊���̃C���X�^���X��\���܂��B</summary>
		internal static readonly DefaultBinder Instance = new DefaultBinder();

		/// <summary><see cref="Microsoft.Scripting.Actions.DefaultBinder"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		public DefaultBinder() { }

		/// <summary>
		/// �w�肳�ꂽ�k���ϊ����x���� <paramref name="fromType"/> ���� <paramref name="toType"/> �ɕϊ������݂��邩�ǂ�����Ԃ��܂��B
		/// �Ώۂ̕ϐ��� <c>null</c> �����e���Ȃ��ꍇ�� <paramref name="toNotNullable"/> �� <c>true</c> �ɂȂ�܂��B
		/// </summary>
		/// <param name="fromType">�ϊ����̌^���w�肵�܂��B</param>
		/// <param name="toType">�ϊ���̌^���w�肵�܂��B</param>
		/// <param name="toNotNullable">�ϊ���̕ϐ��� <c>null</c> �����e���Ȃ����ǂ����������l���w�肵�܂��B</param>
		/// <param name="level">�ϊ������s����k���ϊ����x�����w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�k���ϊ����x���� <paramref name="fromType"/> ���� <paramref name="toType"/> �ɕϊ������݂���� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public override bool CanConvertFrom(Type fromType, Type toType, bool toNotNullable, NarrowingLevel level) { return toType.IsAssignableFrom(fromType); }

		/// <summary>2 �̉������̌^�̊Ԃɕϊ������݂��Ȃ��ꍇ�́A2 �̉������̌^�̏�����Ԃ��܂��B</summary>
		/// <param name="t1">1 �Ԗڂ̉������̌^���w�肵�܂��B</param>
		/// <param name="t2">2 �Ԗڂ̉������̌^���w�肵�܂��B</param>
		/// <returns>2 �̉������̌^�̊Ԃłǂ��炪�K�؂��ǂ��������� <see cref="Candidate"/>�B</returns>
		public override Candidate PreferConvert(Type t1, Type t2) { return Candidate.Ambiguous; }

		/// <summary>�o�C���_�[�ɂ�郁���o�̍폜�����s�����ۂɌĂ΂�܂��B</summary>
		/// <param name="type">�����o�̍폜���s�����^�ł��B</param>
		/// <param name="name">�폜���悤�Ƃ��������o�̖��O�ł��B</param>
		/// <returns>�����I�ȃ����o�̍폜�܂��̓G���[��\�� <see cref="ErrorInfo"/>�B</returns>
		public virtual ErrorInfo MakeUndeletableMemberError(Type type, string name) { return MakeReadOnlyMemberError(type, name); }

		/// <summary>
		/// ���[�U�[�� protected �܂��� private �����o�̒l���擾���悤�Ƃ����ۂɌĂ΂�܂��B
		/// ����̎����ł̓��t���N�V�������g�p���邱�ƂŃt�B�[���h�܂��̓v���p�e�B�ւ̃A�N�Z�X�������܂��B
		/// </summary>
		/// <param name="resolverFactory">�I�[�o�[���[�h�����̕��@��\�� <see cref="OverloadResolverFactory"/> �ł��B</param>
		/// <param name="member">�擾���s���郁���o�ł��B</param>
		/// <param name="type">�擾���s���郁���o��ێ����Ă���^�ł��B</param>
		/// <param name="instance">�擾���s���郁���o��ێ����Ă���C���X�^���X�ł��B</param>
		/// <returns>�����o�̎擾�܂��̓G���[��\�� <see cref="ErrorInfo"/>�B</returns>
		public virtual ErrorInfo MakeNonPublicMemberGetError(OverloadResolverFactory resolverFactory, MemberTracker member, Type type, DynamicMetaObject instance)
		{
			switch (member.MemberType)
			{
				case TrackerTypes.Field:
					return ErrorInfo.FromValueNoError(
						Ast.Call(AstUtils.Convert(AstUtils.Constant(((FieldTracker)member).Field), typeof(FieldInfo)), typeof(FieldInfo).GetMethod("GetValue"),
							AstUtils.Convert(instance.Expression, typeof(object))
						)
					);
				case TrackerTypes.Property:
					return ErrorInfo.FromValueNoError(
						MemberTracker.FromMemberInfo(((PropertyTracker)member).GetGetMethod(true)).Call(resolverFactory, this, instance).Expression
					);
				default:
					throw new InvalidOperationException();
			}
		}

		/// <summary>�������݂��s�����Ƃ��������o�[���ǂݎ���p�ł������ꍇ�ɌĂ΂�܂��B</summary>
		/// <param name="type">�������݂��s�����Ƃ��������o��ێ�����^�ł��B</param>
		/// <param name="name">�������݂��s�����Ƃ��������o�̖��O�ł��B</param>
		/// <returns>�G���[�܂��͋����I�ȏ������݂�\�� <see cref="ErrorInfo"/>�B</returns>
		public virtual ErrorInfo MakeReadOnlyMemberError(Type type, string name)
		{
			return ErrorInfo.FromException(
				Expression.New(
					typeof(MissingMemberException).GetConstructor(new Type[] { typeof(string) }),
					AstUtils.Constant(name)
				)
			);
		}

		/// <summary>�w�肳�ꂽ�C�x���g�ɃC�x���g �n���h�����֘A�t����ۂɌĂ΂�܂��B</summary>
		/// <param name="members">�֘A�t������C�x���g�ł��B</param>
		/// <param name="eventObject">�C�x���g��\�� <see cref="DynamicMetaObject"/> �ł��B</param>
		/// <param name="value">�֘A�t����n���h����\���l�ł��B</param>
		/// <param name="resolverFactory">�I�[�o�[���[�h�����̕��@��񋟂��� <see cref="OverloadResolverFactory"/> �ł��B</param>
		/// <returns>�G���[�܂��̓C�x���g�̊֘A�t����\�� <see cref="ErrorInfo"/>�B</returns>
		public virtual ErrorInfo MakeEventValidation(MemberGroup members, DynamicMetaObject eventObject, DynamicMetaObject value, OverloadResolverFactory resolverFactory)
		{
			// �C�x���g�̒ǉ����n���h�� - ����̓��[�U�[�������������s���Ă��邱�Ƃ��m�F����B
			return ErrorInfo.FromValueNoError(
				Expression.Call(new Action<EventTracker, object>(BinderOps.SetEvent).Method, AstUtils.Constant((EventTracker)members[0]), value.Expression)
			);
		}

		/// <summary>�w�肳�ꂽ <see cref="ErrorInfo"/> ���\���G���[�܂��͒l��\�� <see cref="DynamicMetaObject"/> ���쐬���܂��B</summary>
		/// <param name="error">�G���[�܂��͒l��ێ����Ă��� <see cref="ErrorInfo"/> ���w�肵�܂��B</param>
		/// <param name="type">���ʂƂ��� <see cref="DynamicMetaObject"/> ���\���^���w�肵�܂��B</param>
		/// <returns>�G���[�܂��͒l��\�� <see cref="DynamicMetaObject"/>�B</returns>
		public static DynamicMetaObject MakeError(ErrorInfo error, Type type) { return MakeError(error, BindingRestrictions.Empty, type); }

		/// <summary>�w�肳�ꂽ <see cref="ErrorInfo"/> ���\���G���[�܂��͒l��\�� <see cref="DynamicMetaObject"/> ���쐬���܂��B</summary>
		/// <param name="error">�G���[�܂��͒l��ێ����Ă��� <see cref="ErrorInfo"/> ���w�肵�܂��B</param>
		/// <param name="restrictions">��������� <see cref="DynamicMetaObject"/> �ɓK�p�����o�C���f�B���O������w�肵�܂��B</param>
		/// <param name="type">���ʂƂ��� <see cref="DynamicMetaObject"/> ���\���^���w�肵�܂��B</param>
		/// <returns>�G���[�܂��͒l��\�� <see cref="DynamicMetaObject"/>�B</returns>
		public static DynamicMetaObject MakeError(ErrorInfo error, BindingRestrictions restrictions, Type type)
		{
			switch (error.Kind)
			{
				case ErrorInfoKind.Error: // error meta objecT?
					return new DynamicMetaObject(AstUtils.Convert(error.Expression, type), restrictions);
				case ErrorInfoKind.Exception:
					return new DynamicMetaObject(AstUtils.Convert(Expression.Throw(error.Expression), type), restrictions);
				case ErrorInfoKind.Success:
					return new DynamicMetaObject(AstUtils.Convert(error.Expression, type), restrictions);
				default:
					throw new InvalidOperationException();
			}
		}

		static Expression MakeAmbiguousMatchError(MemberGroup members)
		{
			return Ast.Throw(
				Ast.New(
					typeof(AmbiguousMatchException).GetConstructor(new Type[] { typeof(string) }),
					AstUtils.Constant(string.Join(", ", members.Select(x => x.MemberType + " : " + x.ToString())))
				),
				typeof(object)
			);
		}

		/// <summary>
		/// �w�肳�ꂽ <see cref="MemberGroup"/> �Ɋ܂܂�Ă��� <see cref="MemberTracker"/> �̎�ނ�Ԃ��܂��B
		/// <see cref="MemberGroup"/> �ɈقȂ��ނ� <see cref="MemberTracker"/> �����݂����ꍇ�̓G���[��Ԃ��܂��B
		/// </summary>
		/// <param name="members">�܂܂�Ă��郁���o�̎�ނ𔻒肷�� <see cref="MemberGroup"/> ���w�肵�܂��B</param>
		/// <param name="error">�قȂ��ނ� <see cref="MemberTracker"/> ���܂܂�Ă����ꍇ�ɃG���[���i�[����ϐ����w�肵�܂��B</param>
		/// <returns>
		/// �w�肳�ꂽ <see cref="MemberGroup"/> �Ɋ܂܂�Ă��� <see cref="MemberTracker"/> �̎�ށB
		/// ���݂��Ȃ����قȂ��ނ� <see cref="MemberTracker"/> �����݂���ꍇ�� <see cref="TrackerTypes.All"/> ��Ԃ��܂��B
		/// </returns>
		public TrackerTypes GetMemberType(MemberGroup members, out Expression error)
		{
			error = null;
			var memberType = TrackerTypes.All;
			foreach (var mi in members)
			{
				if (mi.MemberType != memberType)
				{
					if (memberType != TrackerTypes.All)
					{
						error = MakeAmbiguousMatchError(members);
						return TrackerTypes.All;
					}
					memberType = mi.MemberType;
				}
			}
			return memberType;
		}

		/// <summary>�w�肳�ꂽ�^����т��̌^�K�w�̊g���^����w�肳�ꂽ���O�̃��\�b�h���������܂��B</summary>
		/// <param name="type">�������J�n����^���w�肵�܂��B</param>
		/// <param name="name">�������郁�\�b�h�̖��O���w�肵�܂��B</param>
		/// <returns>�����������\�b�h��\�� <see cref="MethodInfo"/>�B������Ȃ������ꍇ�� <c>null</c> ��Ԃ��A�������������ꍇ�͗�O���X���[���܂��B</returns>
		public MethodInfo GetMethod(Type type, string name)
		{
			// declaring type takes precedence
			var mi = GetSpecialNameMethod(type, name);
			if (mi != null)
				return mi;
			// then search extension types.
			for (var curType = type; curType != null; curType = curType.BaseType)
			{
				foreach (var t in GetExtensionTypes(curType))
				{
					var next = GetSpecialNameMethod(t, name);
					if (next != null)
					{
						if (mi != null)
							throw AmbiguousMatch(type, name);
						mi = next;
					}
				}
				if (mi != null)
					return mi;
			}
			return null;
		}

		static MethodInfo GetSpecialNameMethod(Type type, string name)
		{
			MethodInfo res = null;
			var candidates = type.GetMember(name, MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			foreach (MethodInfo candidate in candidates)
			{
				if (candidate.IsSpecialName)
				{
					if (ReferenceEquals(res, null))
						res = candidate;
					else
						throw AmbiguousMatch(type, name);
				}
			}
			return res;
		}

		static Exception AmbiguousMatch(Type type, string name) { throw new AmbiguousMatchException(string.Format("�^ {1} �� {0} �ɑ΂��镡���� SpecialName ���\�b�h��������܂����B", name, type)); }
	}
}