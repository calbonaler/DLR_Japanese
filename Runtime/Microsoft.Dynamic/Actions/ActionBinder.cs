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
	/// <summary>
	/// ����ɑ΂���o�C���f�B���O�Z�}���e�B�N�X��񋟂��܂��B
	/// ����ɂ̓A�N�V�����ɑ΂���K���𐶐����邽�߂̃T�|�[�g�Ɠ��l�ɕϊ����܂݂܂��B
	/// �����̍œK�����ꂽ�K���̓��\�b�h�Ăяo���A����̎��s����� <see cref="ActionBinder"/> �̕ϊ��Z�}���e�B�N�X���g�p���郁���o�̎擾�Ɏg�p����܂��B
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
	public abstract class ActionBinder
	{
		/// <summary>
		/// �o�C���_�[���p�u���b�N�łȂ������o�ɃA�N�Z�X�ł��邩�ǂ����������l���擾���܂��B
		/// ����ł́A�o�C���_�[�̓v���C�x�[�g�����o�փA�N�Z�X�ł��܂��񂪁A
		/// ���̒l���I�[�o�[���C�h���邱�ƂŁA�h���N���X�̓v���C�x�[�g�����o�ւ̃o�C���f�B���O�����p�\���ǂ������J�X�^�}�C�Y�ł��܂��B
		/// </summary>
		public virtual bool PrivateBinding { get { return false; } }

		/// <summary><see cref="Microsoft.Scripting.Actions.ActionBinder"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		protected ActionBinder() { }

		/// <summary>���s���ɃI�u�W�F�N�g���w�肳�ꂽ�^�ɕϊ����܂��B</summary>
		/// <param name="obj">�ϊ�����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="toType">�I�u�W�F�N�g��ϊ�����^���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�^�ɕϊ����ꂽ�I�u�W�F�N�g�B</returns>
		public virtual object Convert(object obj, Type toType)
		{
			if (obj == null)
			{
				if (!toType.IsValueType)
					return null;
			}
			else if (toType.IsValueType)
			{
				if (toType == obj.GetType())
					return obj;
			}
			else if (toType.IsAssignableFrom(obj.GetType()))
				return obj;
			throw Error.InvalidCast(obj != null ? obj.GetType().Name : "(null)", toType.Name);
		}

		/// <summary>
		/// �w�肳�ꂽ�k���ϊ����x���� <paramref name="fromType"/> ���� <paramref name="toType"/> �ɕϊ������݂��邩�ǂ�����Ԃ��܂��B
		/// �Ώۂ̕ϐ��� <c>null</c> �����e���Ȃ��ꍇ�� <paramref name="toNotNullable"/> �� <c>true</c> �ɂȂ�܂��B
		/// </summary>
		/// <param name="fromType">�ϊ����̌^���w�肵�܂��B</param>
		/// <param name="toType">�ϊ���̌^���w�肵�܂��B</param>
		/// <param name="toNotNullable">�ϊ���̕ϐ��� <c>null</c> �����e���Ȃ����ǂ����������l���w�肵�܂��B</param>
		/// <param name="level">�ϊ������s����k���ϊ����x�����w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�k���ϊ����x���� <paramref name="fromType"/> ���� <paramref name="toType"/> �ɕϊ������݂���� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public abstract bool CanConvertFrom(Type fromType, Type toType, bool toNotNullable, NarrowingLevel level);

		/// <summary>2 �̉������̌^�̊Ԃɕϊ������݂��Ȃ��ꍇ�́A2 �̉������̌^�̏�����Ԃ��܂��B</summary>
		/// <param name="t1">1 �Ԗڂ̉������̌^���w�肵�܂��B</param>
		/// <param name="t2">2 �Ԗڂ̉������̌^���w�肵�܂��B</param>
		/// <returns>2 �̉������̌^�̊Ԃłǂ��炪�K�؂��ǂ��������� <see cref="Candidate"/>�B</returns>
		public abstract Candidate PreferConvert(Type t1, Type t2);

		// TODO: revisit
		/// <summary>�w�肳�ꂽ <see cref="Expression"/> ���w�肳�ꂽ�^�ɕϊ����܂��B<see cref="Expression"/> �͕�����]���\�ł��B</summary>
		/// <param name="expr">������]���\�Ȏw�肳�ꂽ�^�ɕϊ������ <see cref="Expression"/> ���w�肵�܂��B</param>
		/// <param name="toType"><see cref="Expression"/> ���ϊ������^���w�肵�܂��B</param>
		/// <param name="kind">���s�����ϊ��̎�ނ��w�肵�܂��B</param>
		/// <param name="resolverFactory">���̕ϊ��Ɏg�p�ł��� <see cref="OverloadResolverFactory"/> ���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�^�ɕϊ����ꂽ <see cref="Expression"/>�B</returns>
		public virtual Expression ConvertExpression(Expression expr, Type toType, ConversionResultKind kind, OverloadResolverFactory resolverFactory)
		{
			ContractUtils.RequiresNotNull(expr, "expr");
			ContractUtils.RequiresNotNull(toType, "toType");
			if (toType == typeof(object))
				return expr.Type.IsValueType ? AstUtils.Convert(expr, toType) : expr;
			if (toType.IsAssignableFrom(expr.Type))
				return expr;
			var visType = CompilerHelpers.GetVisibleType(toType);
			return Expression.Convert(expr, toType);
		}

		/// <summary>�f���Q�[�g�ɂ���Ďw�肳���z��̎w�肳�ꂽ�C���f�b�N�X�ɑ��݂���l���w�肳�ꂽ�^�ɕϊ�����f���Q�[�g���擾���܂��B</summary>
		/// <param name="index">�ϊ���������������f���Q�[�g�������̃C���f�b�N�X���w�肵�܂��B</param>
		/// <param name="knownType">�ϊ�����l��\�� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <param name="toType">�ϊ���̌^���w�肵�܂��B</param>
		/// <param name="conversionResultKind">���s�����ϊ��̎�ނ��w�肵�܂��B</param>
		/// <returns>�f���Q�[�g�ɂ���Ďw�肳�ꂽ�z��̎w�肳�ꂽ�C���f�b�N�X�ɑ��݂���������w�肳�ꂽ�^�ɕϊ�����f���Q�[�g�B</returns>
		public virtual Func<object[], object> ConvertObject(int index, DynamicMetaObject knownType, Type toType, ConversionResultKind conversionResultKind) { throw new NotSupportedException(); }

		/// <summary>
		/// �w�肳�ꂽ�^����w�肳�ꂽ���O�̉��ł��郁���o���擾���܂��B
		/// ����̎����́A�^�A���R�����ꂽ�^�K�w�A�����ēo�^���ꂽ�g�����\�b�h�̏��Ɍ�������܂��B
		/// </summary>
		/// <param name="action">�����o�ɑ΂��鑀����w�肵�܂��B</param>
		/// <param name="type">�����o����������^���w�肵�܂��B</param>
		/// <param name="name">�������郁���o�̖��O���w�肵�܂��B</param>
		/// <returns>�������ꂽ�����o�̈ꗗ��\�� <see cref="MemberGroup"/>�B</returns>
		public virtual MemberGroup GetMember(MemberRequestKind action, Type type, string name)
		{
			// check for generic types w/ arity...
			var genTypes = type.GetNestedTypes(BindingFlags.Public).Where(x => x.Name.StartsWith(name + ReflectionUtils.GenericArityDelimiter));
			if (genTypes.Any())
				return new MemberGroup(genTypes.ToArray());
			var foundMembers = type.GetMember(name);
			if (!PrivateBinding)
				foundMembers = CompilerHelpers.FilterNonVisibleMembers(type, foundMembers);
			var members = new MemberGroup(foundMembers);
			if (members.Count == 0 && (members = new MemberGroup(type.GetMember(name, BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))).Count == 0)
				members = GetAllExtensionMembers(type, name);
			return members;
		}

		#region Error Production

		/// <summary>�w�肳�ꂽ�����o�̃W�F�l���b�N�^�����Ɋւ���G���[�𐶐����܂��B</summary>
		/// <param name="tracker">�G���[���������������o���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�����o�̃W�F�l���b�N�^�����Ɋւ���G���[��\�� <see cref="ErrorInfo"/>�B</returns>
		public virtual ErrorInfo MakeContainsGenericParametersError(MemberTracker tracker)
		{
			return ErrorInfo.FromException(
				Expression.New(
					typeof(InvalidOperationException).GetConstructor(new Type[] { typeof(string) }),
					AstUtils.Constant(Strings.InvalidOperation_ContainsGenericParameters(tracker.DeclaringType.Name, tracker.Name))
				)
			);
		}

		/// <summary>�w�肳�ꂽ�^�Ɏw�肳�ꂽ���O�̃����o��������Ȃ����Ƃ�\���G���[�𐶐����܂��B</summary>
		/// <param name="type">�����o�����������^���w�肵�܂��B</param>
		/// <param name="name">�������������o�̖��O���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�^�Ɏw�肳�ꂽ���O�̃����o��������Ȃ����Ƃ�\���G���[��\�� <see cref="ErrorInfo"/>�B</returns>
		public virtual ErrorInfo MakeMissingMemberErrorInfo(Type type, string name)
		{
			return ErrorInfo.FromException(
				Expression.New(
					typeof(MissingMemberException).GetConstructor(new Type[] { typeof(string) }),
					AstUtils.Constant(name)
				)
			);
		}

		/// <summary>�w�肳�ꂽ�����o�ɂ�����W�F�l���b�N�A�N�Z�X�Ɋւ���G���[�𐶐����܂��B</summary>
		/// <param name="info">�A�N�Z�X���������������o���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�����o�ɂ�����W�F�l���b�N�A�N�Z�X�Ɋւ���G���[��\�� <see cref="ErrorInfo"/>�B</returns>
		public virtual ErrorInfo MakeGenericAccessError(MemberTracker info)
		{
			return ErrorInfo.FromException(
				Expression.New(
					typeof(MemberAccessException).GetConstructor(new Type[] { typeof(string) }),
					AstUtils.Constant(info.Name)
				)
			);
		}

		/// <summary>�Z�b�g���t�B�[���h�܂��̓v���p�e�B�ɔh���N���X������N���X��ʂ��đ�������݂��Ƃ��ɌĂ΂�܂��B����̓���ł͑���������܂��B</summary>
		/// <param name="accessingType">�A�N�Z�X����^���w�肵�܂��B</param>
		/// <param name="self">��������������C���X�^���X���w�肵�܂��B</param>
		/// <param name="assigning">��������v���p�e�B�܂��̓t�B�[���h���w�肵�܂��B</param>
		/// <param name="assignedValue">��������l���w�肵�܂��B</param>
		/// <param name="context">������s�����\�b�h�I�[�o�[���[�h���������� <see cref="OverloadResolverFactory"/> ���w�肵�܂��B</param>
		/// <returns>�Z�b�g���t�B�[���h�܂��̓v���p�e�B�ɔh���N���X������N���X��ʂ��đ�������݂��ۂ̏���\�� <see cref="ErrorInfo"/>�B</returns>
		public virtual ErrorInfo MakeStaticAssignFromDerivedTypeError(Type accessingType, DynamicMetaObject self, MemberTracker assigning, DynamicMetaObject assignedValue, OverloadResolverFactory context)
		{
			switch (assigning.MemberType)
			{
				case TrackerTypes.Property:
					var pt = (PropertyTracker)assigning;
					var setter = pt.GetSetMethod() ?? pt.GetSetMethod(true);
					return ErrorInfo.FromValueNoError(
						AstUtils.SimpleCallHelper(
							setter,
							ConvertExpression(
								assignedValue.Expression,
								setter.GetParameters()[0].ParameterType,
								ConversionResultKind.ExplicitCast,
								context
							)
						)
					);
				case TrackerTypes.Field:
					var ft = (FieldTracker)assigning;
					return ErrorInfo.FromValueNoError(
						Expression.Assign(
							Expression.Field(null, ft.Field),
							ConvertExpression(assignedValue.Expression, ft.FieldType, ConversionResultKind.ExplicitCast, context)
						)
					);
				default:
					throw new InvalidOperationException();
			}
		}

		/// <summary>
		/// �ÓI�v���p�e�B���C���X�^���X�����o����A�N�Z�X���ꂽ�ꍇ��\�� <see cref="ErrorInfo"/> ���쐬���܂��B
		/// ����̓���ł́A�ÓI�����o�v���p�e�B�̓C���X�^���X��ʂ��ăA�N�Z�X����Ȃ���΂Ȃ�Ȃ����Ƃ�������O�𔭐������܂��B
		/// ����͗�O�A���b�Z�[�W���J�X�^�}�C�Y������A�A�N�Z�X���ꂽ�v���p�e�B��ǂݏ������� <see cref="ErrorInfo"/> �I�u�W�F�N�g�𐶐������肷�邽�߂ɂ��̃��\�b�h���I�[�o�[���C�h�ł��܂��B
		/// </summary>
		/// <param name="tracker">�C���X�^���X��ʂ��ăA�N�Z�X���ꂽ�ÓI�v���p�e�B���w�肵�܂��B</param>
		/// <param name="isAssignment">���[�U�[���v���p�e�B�ɒl�����������ǂ����������l���w�肵�܂��B</param>
		/// <param name="parameters">
		/// �v���p�e�B�ւ̃A�N�Z�X�Ɏg�p�����������w�肵�܂��B
		/// ���̃��X�g�ɂ͍ŏ��̗v�f�Ƃ��ăC���X�^���X���A<paramref name="isAssignment"/> �� <c>true</c> �̏ꍇ�́A�Ō�̗v�f�Ƃ��đ�����ꂽ�l���i�[����Ă��܂��B
		/// </param>
		/// <returns>��O�܂��̓v���p�e�B�̓ǂݏ��������\�� <see cref="ErrorInfo"/>�B</returns>
		public virtual ErrorInfo MakeStaticPropertyInstanceAccessError(PropertyTracker tracker, bool isAssignment, IEnumerable<DynamicMetaObject> parameters)
		{
			ContractUtils.RequiresNotNull(tracker, "tracker");
			ContractUtils.Requires(tracker.IsStatic, "tracker", Strings.ExpectedStaticProperty);
			ContractUtils.RequiresNotNull(parameters, "parameters");
			ContractUtils.RequiresNotNullItems(parameters, "parameters");
			string message = isAssignment ? Strings.StaticAssignmentFromInstanceError(tracker.Name, tracker.DeclaringType.Name) :
											Strings.StaticAccessFromInstanceError(tracker.Name, tracker.DeclaringType.Name);
			return ErrorInfo.FromException(
				Expression.New(
					typeof(MissingMemberException).GetConstructor(new Type[] { typeof(string) }),
					AstUtils.Constant(message)
				)
			);
		}

		/// <summary>
		/// �ÓI�v���p�e�B���C���X�^���X�����o����A�N�Z�X���ꂽ�ꍇ��\�� <see cref="ErrorInfo"/> ��\���܂��B
		/// ����̓���ł́A�ÓI�����o�v���p�e�B�̓C���X�^���X��ʂ��ăA�N�Z�X����Ȃ���΂Ȃ�Ȃ����Ƃ�������O�𔭐������܂��B
		/// ����͗�O�A���b�Z�[�W���J�X�^�}�C�Y������A�A�N�Z�X���ꂽ�v���p�e�B��ǂݏ������� <see cref="ErrorInfo"/> �I�u�W�F�N�g�𐶐������肷�邽�߂ɂ��̃��\�b�h���I�[�o�[���C�h�ł��܂��B
		/// </summary>
		/// <param name="tracker">�C���X�^���X��ʂ��ăA�N�Z�X���ꂽ�ÓI�v���p�e�B���w�肵�܂��B</param>
		/// <param name="isAssignment">���[�U�[���v���p�e�B�ɒl�����������ǂ����������l���w�肵�܂��B</param>
		/// <param name="parameters">
		/// �v���p�e�B�ւ̃A�N�Z�X�Ɏg�p�����������w�肵�܂��B
		/// ���̃��X�g�ɂ͍ŏ��̗v�f�Ƃ��ăC���X�^���X���A<paramref name="isAssignment"/> �� <c>true</c> �̏ꍇ�́A�Ō�̗v�f�Ƃ��đ�����ꂽ�l���i�[����Ă��܂��B
		/// </param>
		/// <returns>��O�܂��̓v���p�e�B�̓ǂݏ��������\�� <see cref="ErrorInfo"/>�B</returns>
		public ErrorInfo MakeStaticPropertyInstanceAccessError(PropertyTracker tracker, bool isAssignment, params DynamicMetaObject[] parameters)
		{
			return MakeStaticPropertyInstanceAccessError(tracker, isAssignment, (IEnumerable<DynamicMetaObject>)parameters);
		}

		/// <summary>�l�^�̃t�B�[���h�ɒl�����蓖�Ă悤�Ƃ����ۂɔ�������G���[�𐶐����܂��B</summary>
		/// <param name="field">������s��ꂽ�t�B�[���h���w�肵�܂��B</param>
		/// <param name="instance">������s��ꂽ�t�B�[���h��ێ����Ă���C���X�^���X���w�肵�܂��B</param>
		/// <param name="value">��������l���w�肵�܂��B</param>
		/// <returns>��O�܂��̓t�B�[���h�ւ̑�������\�� <see cref="ErrorInfo"/>�B</returns>
		public virtual ErrorInfo MakeSetValueTypeFieldError(FieldTracker field, DynamicMetaObject instance, DynamicMetaObject value)
		{
			return ErrorInfo.FromException(
				Expression.Throw(
					Expression.New(
						typeof(ArgumentException).GetConstructor(new Type[] { typeof(string) }),
						AstUtils.Constant("cannot assign to value types")
					),
					typeof(object)
				)
			);
		}

		/// <summary>�w�肳�ꂽ <see cref="Expression"/> ���w�肳�ꂽ�^�ɕϊ��ł��Ȃ��ꍇ�ɔ�������G���[�𐶐����܂��B</summary>
		/// <param name="toType"><paramref name="value"/> ���ϊ������^���w�肵�܂��B</param>
		/// <param name="value">�^��ϊ����� <see cref="Expression"/> ���w�肵�܂��B</param>
		/// <returns>��O�܂��͕ϊ������\�� <see cref="ErrorInfo"/>�B</returns>
		public virtual ErrorInfo MakeConversionError(Type toType, Expression value)
		{
			return ErrorInfo.FromException(
				Expression.Call(
					new Func<Type, object, Exception>(ScriptingRuntimeHelpers.CannotConvertError).Method,
					AstUtils.Constant(toType),
					AstUtils.Convert(value, typeof(object))
			   )
			);
		}

		/// <summary>
		/// ���������s�����ۂɃJ�X�^���G���[���b�Z�[�W��Ԃ��܂��B
		/// ��茘�S�ȃG���[�ԋp���J�j�Y������������܂ł��̃��\�b�h�͎g�p����܂��B
		/// </summary>
		/// <param name="type">�������s�����^���w�肵�܂��B</param>
		/// <param name="self">�������s�����C���X�^���X���w�肵�܂��B</param>
		/// <param name="name">�������������o�̖��O���w�肵�܂��B</param>
		/// <returns>�����o��������Ȃ��ꍇ�̗�O��\�� <see cref="ErrorInfo"/>�B</returns>
		public virtual ErrorInfo MakeMissingMemberError(Type type, DynamicMetaObject self, string name)
		{
			return ErrorInfo.FromException(
				Expression.New(
					typeof(MissingMemberException).GetConstructor(new Type[] { typeof(string) }),
					AstUtils.Constant(name)
				)
			);
		}

		/// <summary>�l�̑���̂��߂̃����o�����Ŏw�肳�ꂽ���O�̃����o��������Ȃ��ꍇ�ɔ�������G���[�𐶐����܂��B</summary>
		/// <param name="type">�������s�����^���w�肵�܂��B</param>
		/// <param name="self">�������s�����C���X�^���X���w�肵�܂��B</param>
		/// <param name="name">�l�̑���̂��߂Ɍ������������o�̖��O���w�肵�܂��B</param>
		/// <returns>��O�܂��͑���p�̃����o��\�� <see cref="ErrorInfo"/>�B</returns>
		public virtual ErrorInfo MakeMissingMemberErrorForAssign(Type type, DynamicMetaObject self, string name) { return MakeMissingMemberError(type, self, name); }
		
		/// <summary>�ǂݎ���p�̃v���p�e�B�ɒl�������悤�Ƃ����ꍇ�ɔ�������G���[�𐶐����܂��B</summary>
		/// <param name="type">�������s�����^���w�肵�܂��B</param>
		/// <param name="self">�������s�����C���X�^���X���w�肵�܂��B</param>
		/// <param name="name">�ǂݎ���p�̃v���p�e�B���������������o�̖��O���w�肵�܂��B</param>
		/// <returns>��O��\�� <see cref="ErrorInfo"/>�B</returns>
		public virtual ErrorInfo MakeMissingMemberErrorForAssignReadOnlyProperty(Type type, DynamicMetaObject self, string name) { return MakeMissingMemberError(type, self, name); }

		/// <summary>�����o�̍폜�̂��߂̌����Ŏw�肳�ꂽ���O�̃����o��������Ȃ��ꍇ�ɔ�������G���[�𐶐����܂��B</summary>
		/// <param name="type">�������s�����^���w�肵�܂��B</param>
		/// <param name="self">�������s�����C���X�^���X���w�肵�܂��B</param>
		/// <param name="name">�폜�̂��߂Ɍ������������o�̖��O���w�肵�܂��B</param>
		/// <returns>��O��\�� <see cref="ErrorInfo"/>�B</returns>
		public virtual ErrorInfo MakeMissingMemberErrorForDelete(Type type, DynamicMetaObject self, string name) { return MakeMissingMemberError(type, self, name); }

		#endregion

		/// <summary>�w�肳�ꂽ�^�ɑ΂��閼�O��Ԃ��܂��B</summary>
		/// <param name="t">���O���擾����^���w�肵�܂��B</param>
		/// <returns>�^�ɑ΂��閼�O�B</returns>
		public virtual string GetTypeName(Type t) { return t.Name; }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�ɑ΂���^�̖��O��Ԃ��܂��B</summary>
		/// <param name="arg">�^�̖��O���擾����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns>�I�u�W�F�N�g�̌^�ɑ΂��閼�O�B</returns>
		public virtual string GetObjectTypeName(object arg) { return GetTypeName(CompilerHelpers.GetType(arg)); }

		/// <summary>�w�肳�ꂽ�^����w�肳�ꂽ���O�̊g�������o���擾���܂��B���N���X����������܂��B�p���K�w�̌^�� 1 �ł��g�����\�b�h��񋟂����ꍇ�A�����͒�~���܂��B</summary>
		/// <param name="type">�g�������o����������^���w�肵�܂��B</param>
		/// <param name="name">��������g�������o�̖��O���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�^�̌p���֌W�Ō��������g�������o�̃��X�g�B</returns>
		public MemberGroup GetAllExtensionMembers(Type type, string name)
		{
			var curType = type;
			do
			{
				var res = GetExtensionMembers(curType, name);
				if (res.Count != 0)
					return res;
			} while ((curType = curType.BaseType) != null);
			return MemberGroup.EmptyGroup;
		}

		/// <summary>�w�肳�ꂽ�^����w�肳�ꂽ���O�̊g�������o���擾���܂��B���N���X�͌�������܂���B</summary>
		/// <param name="declaringType">�g�������o����������^���w�肵�܂��B</param>
		/// <param name="name">��������g�������o�̖��O���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�^�Ō��������g�������o�̃��X�g�B</returns>
		public MemberGroup GetExtensionMembers(Type declaringType, string name)
		{
			var members = GetExtensionTypes(declaringType).SelectMany(ext =>
			{
				var res = ext.GetMember(name, BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
					.Select(x => !PrivateBinding ? CompilerHelpers.TryGetVisibleMember(x) : x).Where(x => x != null)
					.Select(x => ext != declaringType ? MemberTracker.FromMemberInfo(x, declaringType) : MemberTracker.FromMemberInfo(x));
				// TODO: Support indexed getters/setters w/ multiple methods
				var getter = (MethodInfo)ext.GetMember("Get" + name, MemberTypes.Method, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
					.SingleOrDefault(x => x.IsDefined(typeof(PropertyMethodAttribute), false));
				var setter = (MethodInfo)ext.GetMember("Set" + name, MemberTypes.Method, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
					.SingleOrDefault(x => x.IsDefined(typeof(PropertyMethodAttribute), false));
				var deleter = (MethodInfo)ext.GetMember("Delete" + name, MemberTypes.Method, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
					.SingleOrDefault(x => x.IsDefined(typeof(PropertyMethodAttribute), false));
				if (getter != null || setter != null || deleter != null)
					res = res.Concat(Enumerable.Repeat(new ExtensionPropertyTracker(name, getter, setter, deleter, declaringType), 1));
				return res;
			});
			if (members.Any())
				return new MemberGroup(members.ToArray());
			return MemberGroup.EmptyGroup;
		}

		/// <summary>�w�肳�ꂽ�^�ɑ΂��邷�ׂĂ̊g���^���擾���܂��B</summary>
		/// <param name="t">�g���^���擾����^���w�肵�܂��B</param>
		/// <returns>�^�ɑ΂���g���^�B</returns>
		public virtual IList<Type> GetExtensionTypes(Type t) { return Type.EmptyTypes; } // None are provided by default, languages need to know how to provide these on their own terms.

		/// <summary>
		/// ���ꂪ���ׂĂ� <see cref="MemberTracker"/> �����ꎩ�g�̌^�Œu��������@���񋟂��܂��B
		/// ����Ɍ���� <see cref="MemberTracker"/> �𒼐ڌ��J���邱�Ƃ��ł��܂��B
		/// </summary>
		/// <param name="type"><paramref name="memberTracker"/> ���A�N�Z�X���ꂽ�^���w�肵�܂��B</param>
		/// <param name="memberTracker">���[�U�[�ɕԂ���郁���o���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�����o�ɑ΂��� <see cref="DynamicMetaObject"/>�B</returns>
		public virtual DynamicMetaObject ReturnMemberTracker(Type type, MemberTracker memberTracker)
		{
			if (memberTracker.MemberType == TrackerTypes.Bound)
			{
				var bmt = (BoundMemberTracker)memberTracker;
				return new DynamicMetaObject(
					Expression.New(
						typeof(BoundMemberTracker).GetConstructor(new Type[] { typeof(MemberTracker), typeof(object) }),
						AstUtils.Constant(bmt.BoundTo), bmt.Instance.Expression
					),
					BindingRestrictions.Empty
				);
			}
			return new DynamicMetaObject(AstUtils.Constant(memberTracker), BindingRestrictions.Empty, memberTracker);
		}

		/// <summary>�w�肳�ꂽ <see cref="OverloadResolverFactory"/> ���g�p���āA�w�肳�ꂽ���\�b�h���w�肳�ꂽ�����ŌĂяo�� <see cref="Expression"/> ���쐬���܂��B</summary>
		/// <param name="resolverFactory">�I�[�o�[���[�h���������� <see cref="OverloadResolverFactory"/> ���w�肵�܂��B</param>
		/// <param name="method">�Ăяo�����\�b�h��\�� <see cref="MethodInfo"/> ���w�肵�܂��B</param>
		/// <param name="parameters">���\�b�h�ɓn���������w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ���\�b�h���Ăяo�� <see cref="Expression"/> ���i�[���� <see cref="DynamicMetaObject"/>�B</returns>
		public DynamicMetaObject MakeCallExpression(OverloadResolverFactory resolverFactory, MethodInfo method, params DynamicMetaObject[] parameters)
		{
			var resolver = method.IsStatic ?
				resolverFactory.CreateOverloadResolver(parameters, new CallSignature(parameters.Length), CallTypes.None) :
				resolverFactory.CreateOverloadResolver(parameters, new CallSignature(parameters.Length - 1), CallTypes.ImplicitInstance);
			var target = resolver.ResolveOverload(method.Name, new MethodBase[] { method }, NarrowingLevel.None, NarrowingLevel.All);
			if (!target.Success)
				return DefaultBinder.MakeError(resolver.MakeInvalidParametersError(target), parameters.Aggregate(BindingRestrictions.Combine(parameters), (x, y) => x.Merge(BindingRestrictions.GetTypeRestriction(y.Expression, y.GetLimitType()))), typeof(object));
			return new DynamicMetaObject(target.MakeExpression(), target.RestrictedArguments.GetAllRestrictions());
		}
	}
}

