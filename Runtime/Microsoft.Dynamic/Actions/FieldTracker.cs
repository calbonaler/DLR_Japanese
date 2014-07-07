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
using Microsoft.Contracts;
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions
{
	using Ast = Expression;

	/// <summary>�t�B�[���h��\���܂��B</summary>
	public class FieldTracker : MemberTracker
	{
		/// <summary>��ɂȂ� <see cref="FieldInfo"/> ���g�p���āA<see cref="Microsoft.Scripting.Actions.FieldTracker"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="field">��ɂȂ� <see cref="FieldInfo"/> ���w�肵�܂��B</param>
		public FieldTracker(FieldInfo field)
		{
			ContractUtils.RequiresNotNull(field, "field");
			Field = field;
		}

		/// <summary>�����o��_���I�ɐ錾����^���擾���܂��B</summary>
		public override Type DeclaringType { get { return Field.DeclaringType; } }

		/// <summary><see cref="MemberTracker"/> �̎�ނ��擾���܂��B</summary>
		public sealed override TrackerTypes MemberType { get { return TrackerTypes.Field; } }

		/// <summary>�����o�̖��O���擾���܂��B</summary>
		public override string Name { get { return Field.Name; } }

		/// <summary>�t�B�[���h���p�u���b�N���ǂ����������l���擾���܂��B</summary>
		public bool IsPublic { get { return Field.IsPublic; } }

		/// <summary>�t�B�[���h�ɑ΂��鏑�����݂����������̂݉\�ł��邩�ǂ����������l���擾���܂��B</summary>
		public bool IsInitOnly { get { return Field.IsInitOnly; } }

		/// <summary>�l���R���p�C�����ɏ������܂�A�ύX�ł��Ȃ����ǂ����������l���擾���܂��B</summary>
		public bool IsLiteral { get { return Field.IsLiteral; } }

		/// <summary>���̃t�B�[���h�̌^���擾���܂��B</summary>
		public Type FieldType { get { return Field.FieldType; } }

		/// <summary>�t�B�[���h���ÓI���ǂ����������l���擾���܂��B</summary>
		public bool IsStatic { get { return Field.IsStatic; } }

		/// <summary>��ɂȂ� <see cref="FieldInfo"/> ���擾���܂��B</summary>
		public FieldInfo Field { get; private set; }

		/// <summary>���̃I�u�W�F�N�g�̕�����\�����擾���܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̕�����\���B</returns>
		[Confined]
		public override string ToString() { return Field.ToString(); }

		/// <summary>
		/// �l���擾���� <see cref="Expression"/> ���擾���܂��B
		/// �Ăяo������ GetErrorForGet ���Ăяo���āA���m�ȃG���[��\�� <see cref="Expression"/> �܂��͊���̃G���[��\�� <c>null</c> ���擾�ł��܂��B
		/// </summary>
		/// <param name="resolverFactory">�I�[�o�[���[�h�����̕��@��\�� <see cref="OverloadResolverFactory"/> ���w�肵�܂��B</param>
		/// <param name="binder">����̃o�C���f�B���O�Z�}���e�B�N�X���w�肵�܂��B</param>
		/// <param name="type">���� <see cref="MemberTracker"/> ���A�N�Z�X���ꂽ�^���w�肵�܂��B</param>
		/// <returns>�l���擾���� <see cref="Expression"/>�B�G���[�����������ꍇ�� <c>null</c> ���Ԃ���܂��B</returns>
		public override DynamicMetaObject GetValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type)
		{
			if (IsLiteral)
				return new DynamicMetaObject(AstUtils.Constant(Field.GetValue(null), typeof(object)), BindingRestrictions.Empty);
			if (!IsStatic)
				return binder.ReturnMemberTracker(type, this); // return the field tracker...
			if (Field.DeclaringType.ContainsGenericParameters)
				return null;
			if (IsPublic && DeclaringType.IsPublic)
				return new DynamicMetaObject(Ast.Convert(Ast.Field(null, Field), typeof(object)), BindingRestrictions.Empty);
			return new DynamicMetaObject(
				Ast.Call(AstUtils.Convert(AstUtils.Constant(Field), typeof(FieldInfo)), typeof(FieldInfo).GetMethod("GetValue"), AstUtils.Constant(null)),
				BindingRestrictions.Empty
			);
		}

		/// <summary>�l�̎擾�Ɋ֘A�t�����Ă���G���[��Ԃ��܂��B</summary>
		/// <param name="binder">����̃o�C���f�B���O�Z�}���e�B�N�X���w�肵�܂��B</param>
		/// <returns>�l�̎擾�Ɋ֘A�t�����Ă���G���[�B�܂��́A�Ăяo�����ɂ���Ċ���̃G���[���b�Z�[�W���񋟂���邱�Ƃ����� <c>null</c>�B</returns>
		public override ErrorInfo GetError(ActionBinder binder)
		{
			// FieldTracker only has one error - accessing a static field from 
			// a generic type.
			Debug.Assert(Field.DeclaringType.ContainsGenericParameters);
			return binder.MakeContainsGenericParametersError(this);
		}

		/// <summary>
		/// �C���X�^���X�ɑ�������Ă���l���擾���� <see cref="Expression"/> ���擾���܂��B
		/// �J�X�^�������o�g���b�J�[�͂��̃��\�b�h���I�[�o�[���C�h���āA�C���X�^���X�ւ̃o�C���h���̓Ǝ��̓����񋟂ł��܂��B
		/// </summary>
		/// <param name="resolverFactory">�I�[�o�[���[�h�����̕��@��\�� <see cref="OverloadResolverFactory"/> ���w�肵�܂��B</param>
		/// <param name="binder">����̃o�C���f�B���O�Z�}���e�B�N�X���w�肵�܂��B</param>
		/// <param name="type">���� <see cref="MemberTracker"/> ���A�N�Z�X���ꂽ�^���w�肵�܂��B</param>
		/// <param name="instance">�������ꂽ�C���X�^���X���w�肵�܂��B</param>
		/// <returns>�C���X�^���X�ɑ�������Ă���l���擾���� <see cref="Expression"/>�B</returns>
		protected internal override DynamicMetaObject GetBoundValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type, DynamicMetaObject instance)
		{
			if (IsPublic && DeclaringType.IsVisible)
				return new DynamicMetaObject(
					AstUtils.Convert(Ast.Field(AstUtils.Convert(instance.Expression, Field.DeclaringType), Field), typeof(object)),
					BindingRestrictions.Empty
				);
			return DefaultBinder.MakeError(((DefaultBinder)binder).MakeNonPublicMemberGetError(resolverFactory, this, type, instance), BindingRestrictions.Empty, typeof(object));
		}

		/// <summary>
		/// �o�C���f�B���O���\�ȏꍇ�A�V���������o�g���b�J�[��Ԃ��w�肳�ꂽ�C���X�^���X�Ƀ����o�g���b�J�[���֘A�t���܂��B
		/// �o�C���f�B���O���s�\�ȏꍇ�A�����̃����o�g���b�J�[���Ԃ���܂��B
		/// �Ⴆ�΁A�ÓI�t�B�[���h�ւ̃o�C���f�B���O�́A���̃����o�g���b�J�[��Ԃ��܂��B
		/// �C���X�^���X�t�B�[���h�ւ̃o�C���f�B���O�́A�C���X�^���X��n�� GetBoundValue �܂��� SetBoundValue �𓾂�V���� <see cref="BoundMemberTracker"/> ��Ԃ��܂��B
		/// </summary>
		/// <param name="instance">�����o�g���b�J�[���֘A�t����C���X�^���X���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�C���X�^���X�Ɋ֘A�t����ꂽ�����o�g���b�J�[�B</returns>
		public override MemberTracker BindToInstance(DynamicMetaObject instance) { return IsStatic ? (MemberTracker)this : new BoundMemberTracker(this, instance); }
	}
}
