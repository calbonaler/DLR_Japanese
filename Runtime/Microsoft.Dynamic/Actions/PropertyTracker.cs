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
using System.Reflection;

using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Generation;

namespace Microsoft.Scripting.Actions
{
	/// <summary>
	/// �^�̃����o�Ƃ��Ă̘_���I�ȃv���p�e�B��\���܂��B
	/// ���̃N���X�� (<see cref="ReflectedPropertyTracker"/> �ɂ���Ď��������) �^�ɒ�`����Ă�����ۂ̃v���p�e�B�܂��́A(<see cref="ExtensionPropertyTracker"/> �ɂ���Ď��������) �g���v���p�e�B�̂ǂ��炩��\���܂��B
	/// </summary>
	public abstract class PropertyTracker : MemberTracker
	{
		/// <summary><see cref="MemberTracker"/> �̎�ނ��擾���܂��B</summary>
		public sealed override TrackerTypes MemberType { get { return TrackerTypes.Property; } }

		/// <summary>���̃v���p�e�B�̃p�u���b�N�� get �A�N�Z�T�[��Ԃ��܂��B</summary>
		/// <returns>���̃v���p�e�B�̃p�u���b�N�� get �A�N�Z�T�[��\�� <see cref="MethodInfo"/> �I�u�W�F�N�g�Bget �A�N�Z�T�[����p�u���b�N�܂��͑��݂��Ȃ��ꍇ�� <c>null</c>�B</returns>
		public MethodInfo GetGetMethod() { return GetGetMethod(false); }

		/// <summary>���̃v���p�e�B�̃p�u���b�N�� set �A�N�Z�T�[��Ԃ��܂��B</summary>
		/// <returns>���̃v���p�e�B�̃p�u���b�N�� set �A�N�Z�T�[��\�� <see cref="MethodInfo"/> �I�u�W�F�N�g�Bset �A�N�Z�T�[����p�u���b�N�܂��͑��݂��Ȃ��ꍇ�� <c>null</c>�B</returns>
		public MethodInfo GetSetMethod() { return GetSetMethod(false); }

		/// <summary>���̃v���p�e�B�̃p�u���b�N�� delete �A�N�Z�T�[��Ԃ��܂��B</summary>
		/// <returns>���̃v���p�e�B�̃p�u���b�N�� delete �A�N�Z�T�[��\�� <see cref="MethodInfo"/> �I�u�W�F�N�g�Bdelete �A�N�Z�T�[����p�u���b�N�܂��͑��݂��Ȃ��ꍇ�� <c>null</c>�B</returns>
		public MethodInfo GetDeleteMethod() { return GetDeleteMethod(false); }

		/// <summary>�h���N���X�ɂ���ăI�[�o�[���C�h���ꂽ�ꍇ�ɁA���̃v���p�e�B�̃p�u���b�N�܂��͔�p�u���b�N�� get �A�N�Z�T�[��Ԃ��܂��B</summary>
		/// <param name="privateMembers">��p�u���b�N�� get �A�N�Z�T�[��Ԃ����ǂ����������܂��B��p�u���b�N �A�N�Z�T�[��Ԃ��ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</param>
		/// <returns>
		/// <paramref name="privateMembers"/> �� <c>true</c> �̏ꍇ�́A���̃v���p�e�B�� get �A�N�Z�T�[��\�� <see cref="MethodInfo"/> �I�u�W�F�N�g�B
		/// <paramref name="privateMembers"/> �� <c>false</c> �� get �A�N�Z�T�[����p�u���b�N�̏ꍇ�A�܂��� <paramref name="privateMembers"/> �� <c>true</c> �ł� get �A�N�Z�T�[���Ȃ��ꍇ�́A<c>null</c> ��Ԃ��܂��B
		/// </returns>
		public abstract MethodInfo GetGetMethod(bool privateMembers);

		/// <summary>�h���N���X�ɂ���ăI�[�o�[���C�h���ꂽ�ꍇ�ɁA���̃v���p�e�B�̃p�u���b�N�܂��͔�p�u���b�N�� set �A�N�Z�T�[��Ԃ��܂��B</summary>
		/// <param name="privateMembers">��p�u���b�N�� set �A�N�Z�T�[��Ԃ����ǂ����������܂��B��p�u���b�N �A�N�Z�T�[��Ԃ��ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</param>
		/// <returns>
		/// <paramref name="privateMembers"/> �� <c>true</c> �̏ꍇ�́A���̃v���p�e�B�� set �A�N�Z�T�[��\�� <see cref="MethodInfo"/> �I�u�W�F�N�g�B
		/// <paramref name="privateMembers"/> �� <c>false</c> �� set �A�N�Z�T�[����p�u���b�N�̏ꍇ�A�܂��� <paramref name="privateMembers"/> �� <c>true</c> �ł� set �A�N�Z�T�[���Ȃ��ꍇ�́A<c>null</c> ��Ԃ��܂��B
		/// </returns>
		public abstract MethodInfo GetSetMethod(bool privateMembers);

		/// <summary>�h���N���X�ɂ���ăI�[�o�[���C�h���ꂽ�ꍇ�ɁA���̃v���p�e�B�̃p�u���b�N�܂��͔�p�u���b�N�� delete �A�N�Z�T�[��Ԃ��܂��B</summary>
		/// <param name="privateMembers">��p�u���b�N�� delete �A�N�Z�T�[��Ԃ����ǂ����������܂��B��p�u���b�N �A�N�Z�T�[��Ԃ��ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</param>
		/// <returns>
		/// <paramref name="privateMembers"/> �� <c>true</c> �̏ꍇ�́A���̃v���p�e�B�� delete �A�N�Z�T�[��\�� <see cref="MethodInfo"/> �I�u�W�F�N�g�B
		/// <paramref name="privateMembers"/> �� <c>false</c> �� delete �A�N�Z�T�[����p�u���b�N�̏ꍇ�A�܂��� <paramref name="privateMembers"/> �� <c>true</c> �ł� delete �A�N�Z�T�[���Ȃ��ꍇ�́A<c>null</c> ��Ԃ��܂��B
		/// </returns>
		public virtual MethodInfo GetDeleteMethod(bool privateMembers) { return null; }

		/// <summary>�h���N���X�ŃI�[�o�[���C�h���ꂽ�ꍇ�ɁA�v���p�e�B�̂��ׂẴC���f�b�N�X �p�����[�^�̔z���Ԃ��܂��B</summary>
		/// <returns>�C���f�b�N�X�̃p�����[�^�[���i�[���Ă��� <see cref="ParameterInfo"/> �^�̔z��B�v���p�e�B���C���f�b�N�X�t������Ă��Ȃ��ꍇ�A�z��̗v�f�̓[�� (0) �ł��B</returns>
		public abstract ParameterInfo[] GetIndexParameters();

		/// <summary>�h���N���X�ŃI�[�o�[���C�h���ꂽ�ꍇ�ɁA���̃v���p�e�B���ÓI�ł��邩�ǂ����������l���擾���܂��B</summary>
		public abstract bool IsStatic { get; }

		/// <summary>�h���N���X�ŃI�[�o�[���C�h���ꂽ�ꍇ�ɁA���̃v���p�e�B�̌^���擾���܂��B</summary>
		public abstract Type PropertyType { get; }

		/// <summary>
		/// �l���擾���� <see cref="System.Linq.Expressions.Expression"/> ���擾���܂��B
		/// �Ăяo������ GetErrorForGet ���Ăяo���āA���m�ȃG���[��\�� <see cref="System.Linq.Expressions.Expression"/> �܂��͊���̃G���[��\�� <c>null</c> ���擾�ł��܂��B
		/// </summary>
		/// <param name="resolverFactory">�I�[�o�[���[�h�����̕��@��\�� <see cref="OverloadResolverFactory"/> ���w�肵�܂��B</param>
		/// <param name="binder">����̃o�C���f�B���O�Z�}���e�B�N�X���w�肵�܂��B</param>
		/// <param name="type">���� <see cref="MemberTracker"/> ���A�N�Z�X���ꂽ�^���w�肵�܂��B</param>
		/// <returns>�l���擾���� <see cref="System.Linq.Expressions.Expression"/>�B�G���[�����������ꍇ�� <c>null</c> ���Ԃ���܂��B</returns>
		public override DynamicMetaObject GetValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type)
		{
			if (!IsStatic || GetIndexParameters().Length > 0)
				return binder.ReturnMemberTracker(type, this); // need to bind to a value or parameters to get the value.
			var getter = ResolveGetter(binder.PrivateBinding);
			if (getter == null || getter.ContainsGenericParameters)
				return null; // no usable getter
			if (getter.IsPublic && getter.DeclaringType.IsPublic)
				return binder.MakeCallExpression(resolverFactory, getter);
			// private binding is just a call to the getter method...
			return MemberTracker.FromMemberInfo(getter).Call(resolverFactory, binder);
		}

		/// <summary>�l�̎擾�Ɋ֘A�t�����Ă���G���[��Ԃ��܂��B</summary>
		/// <param name="binder">����̃o�C���f�B���O�Z�}���e�B�N�X���w�肵�܂��B</param>
		/// <returns>�l�̎擾�Ɋ֘A�t�����Ă���G���[�B�܂��́A�Ăяo�����ɂ���Ċ���̃G���[���b�Z�[�W���񋟂���邱�Ƃ����� <c>null</c>�B</returns>
		public override ErrorInfo GetError(ActionBinder binder)
		{
			var getter = ResolveGetter(binder.PrivateBinding);
			if (getter == null)
				return binder.MakeMissingMemberErrorInfo(DeclaringType, Name);
			if (getter.ContainsGenericParameters)
				return binder.MakeGenericAccessError(this);
			throw new InvalidOperationException();
		}

		/// <summary>
		/// �C���X�^���X�ɑ�������Ă���l���擾���� <see cref="System.Linq.Expressions.Expression"/> ���擾���܂��B
		/// �J�X�^�������o�g���b�J�[�͂��̃��\�b�h���I�[�o�[���C�h���āA�C���X�^���X�ւ̃o�C���h���̓Ǝ��̓����񋟂ł��܂��B
		/// </summary>
		/// <param name="resolverFactory">�I�[�o�[���[�h�����̕��@��\�� <see cref="OverloadResolverFactory"/> ���w�肵�܂��B</param>
		/// <param name="binder">����̃o�C���f�B���O�Z�}���e�B�N�X���w�肵�܂��B</param>
		/// <param name="type">���� <see cref="MemberTracker"/> ���A�N�Z�X���ꂽ�^���w�肵�܂��B</param>
		/// <param name="instance">�������ꂽ�C���X�^���X���w�肵�܂��B</param>
		/// <returns>�C���X�^���X�ɑ�������Ă���l���擾���� <see cref="System.Linq.Expressions.Expression"/>�B</returns>
		protected internal override DynamicMetaObject GetBoundValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type, DynamicMetaObject instance)
		{
			if (instance != null && IsStatic)
				return null;
			if (GetIndexParameters().Length > 0)
				return binder.ReturnMemberTracker(type, BindToInstance(instance)); // need to bind to a value or parameters to get the value.
			var getter = GetGetMethod(true);
			if (getter == null || getter.ContainsGenericParameters)
				return null; // no usable getter
			getter = CompilerHelpers.TryGetCallableMethod(getter);
			var defaultBinder = (DefaultBinder)binder;
			if (binder.PrivateBinding || CompilerHelpers.IsVisible(getter))
				return defaultBinder.MakeCallExpression(resolverFactory, getter, instance);
			// private binding is just a call to the getter method...
			return DefaultBinder.MakeError(defaultBinder.MakeNonPublicMemberGetError(resolverFactory, this, type, instance), BindingRestrictions.Empty, typeof(object));
		}

		/// <summary>�������ꂽ�C���X�^���X��ʂ��������o�ւ̃A�N�Z�X�Ɋ֘A�t�����Ă���G���[��Ԃ��܂��B</summary>
		/// <param name="binder">����̃o�C���f�B���O�Z�}���e�B�N�X���w�肵�܂��B</param>
		/// <param name="instance">�������ꂽ�C���X�^���X���w�肵�܂��B</param>
		/// <returns>�������ꂽ�C���X�^���X��ʂ��������o�ւ̃A�N�Z�X�Ɋ֘A�t�����Ă���G���[�B�܂��́A�Ăяo�����ɂ���Ċ���̃G���[���b�Z�[�W���񋟂���邱�Ƃ����� <c>null</c>�B</returns>
		public override ErrorInfo GetBoundError(ActionBinder binder, DynamicMetaObject instance)
		{
			var getter = ResolveGetter(binder.PrivateBinding);
			if (getter == null)
				return binder.MakeMissingMemberErrorInfo(DeclaringType, Name);
			if (getter.ContainsGenericParameters)
				return binder.MakeGenericAccessError(this);
			if (IsStatic)
				return binder.MakeStaticPropertyInstanceAccessError(this, false, instance);
			throw new InvalidOperationException();
		}

		/// <summary>
		/// �o�C���f�B���O���\�ȏꍇ�A�V���������o�g���b�J�[��Ԃ��w�肳�ꂽ�C���X�^���X�Ƀ����o�g���b�J�[���֘A�t���܂��B
		/// �o�C���f�B���O���s�\�ȏꍇ�A�����̃����o�g���b�J�[���Ԃ���܂��B
		/// �Ⴆ�΁A�ÓI�t�B�[���h�ւ̃o�C���f�B���O�́A���̃����o�g���b�J�[��Ԃ��܂��B
		/// �C���X�^���X�t�B�[���h�ւ̃o�C���f�B���O�́A�C���X�^���X��n�� GetBoundValue �܂��� SetBoundValue �𓾂�V���� <see cref="BoundMemberTracker"/> ��Ԃ��܂��B
		/// </summary>
		/// <param name="instance">�����o�g���b�J�[���֘A�t����C���X�^���X���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�C���X�^���X�Ɋ֘A�t����ꂽ�����o�g���b�J�[�B</returns>
		public override MemberTracker BindToInstance(DynamicMetaObject instance) { return new BoundMemberTracker(this, instance); }

		MethodInfo ResolveGetter(bool privateBinding)
		{
			var getter = GetGetMethod(true);
			if (getter != null)
			{
				getter = CompilerHelpers.TryGetCallableMethod(getter);
				if (privateBinding || CompilerHelpers.IsVisible(getter))
					return getter;
			}
			return null;
		}
	}
}
