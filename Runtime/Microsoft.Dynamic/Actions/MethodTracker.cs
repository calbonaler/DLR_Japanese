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
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Contracts;
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions
{
	using Ast = Expression;

	/// <summary>�P��̃��\�b�h��\���܂��B</summary>
	public class MethodTracker : MemberTracker
	{
		/// <summary>��ɂȂ� <see cref="MethodInfo"/> ���g�p���āA<see cref="Microsoft.Scripting.Actions.MethodTracker"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="method">��ɂȂ� <see cref="MethodInfo"/> ���w�肵�܂��B</param>
		internal MethodTracker(MethodInfo method)
		{
			ContractUtils.RequiresNotNull(method, "method");
			Method = method;
			IsStatic = method.IsStatic;
		}

		/// <summary>��ɂȂ� <see cref="MethodInfo"/> ����ѐÓI���\�b�h���ǂ����������l���g�p���āA<see cref="Microsoft.Scripting.Actions.MethodTracker"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="method">��ɂȂ� <see cref="MethodInfo"/> ���w�肵�܂��B</param>
		/// <param name="isStatic">���̃��\�b�h���ÓI���\�b�h�ł��邩�ǂ����������l���w�肵�܂��B</param>
		internal MethodTracker(MethodInfo method, bool isStatic)
		{
			ContractUtils.RequiresNotNull(method, "method");
			Method = method;
			IsStatic = isStatic;
		}

		/// <summary>�����o��_���I�ɐ錾����^���擾���܂��B</summary>
		public override Type DeclaringType { get { return Method.DeclaringType; } }

		/// <summary><see cref="MemberTracker"/> �̎�ނ��擾���܂��B</summary>
		public override sealed TrackerTypes MemberType { get { return TrackerTypes.Method; } }

		/// <summary>�����o�̖��O���擾���܂��B</summary>
		public override string Name { get { return Method.Name; } }

		/// <summary>��ɂȂ� <see cref="MethodInfo"/> ���擾���܂��B</summary>
		public MethodInfo Method { get; private set; }

		/// <summary>���̃��\�b�h���p�u���b�N ���\�b�h�ł��邩�ǂ����������l���擾���܂��B</summary>
		public bool IsPublic { get { return Method.IsPublic; } }

		/// <summary>���̃��\�b�h���ÓI���\�b�h�ł��邩�ǂ����������l���擾���܂��B</summary>
		public bool IsStatic { get; private set; }

		/// <summary>���̃I�u�W�F�N�g�̕�����\�����擾���܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̕�����\���B</returns>
		[Confined]
		public override string ToString() { return Method.ToString(); }

		/// <summary>
		/// �o�C���f�B���O���\�ȏꍇ�A�V���������o�g���b�J�[��Ԃ��w�肳�ꂽ�C���X�^���X�Ƀ����o�g���b�J�[���֘A�t���܂��B
		/// �o�C���f�B���O���s�\�ȏꍇ�A�����̃����o�g���b�J�[���Ԃ���܂��B
		/// �Ⴆ�΁A�ÓI�t�B�[���h�ւ̃o�C���f�B���O�́A���̃����o�g���b�J�[��Ԃ��܂��B
		/// �C���X�^���X�t�B�[���h�ւ̃o�C���f�B���O�́A�C���X�^���X��n�� GetBoundValue �܂��� SetBoundValue �𓾂�V���� <see cref="BoundMemberTracker"/> ��Ԃ��܂��B
		/// </summary>
		/// <param name="instance">�����o�g���b�J�[���֘A�t����C���X�^���X���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�C���X�^���X�Ɋ֘A�t����ꂽ�����o�g���b�J�[�B</returns>
		public override MemberTracker BindToInstance(DynamicMetaObject instance) { return IsStatic ? (MemberTracker)this : new BoundMemberTracker(this, instance); }

		/// <summary>
		/// �C���X�^���X�ɑ�������Ă���l���擾���� <see cref="Expression"/> ���擾���܂��B
		/// �J�X�^�������o�g���b�J�[�͂��̃��\�b�h���I�[�o�[���C�h���āA�C���X�^���X�ւ̃o�C���h���̓Ǝ��̓����񋟂ł��܂��B
		/// </summary>
		/// <param name="resolverFactory">�I�[�o�[���[�h�����̕��@��\�� <see cref="OverloadResolverFactory"/> ���w�肵�܂��B</param>
		/// <param name="binder">����̃o�C���f�B���O�Z�}���e�B�N�X���w�肵�܂��B</param>
		/// <param name="type">���� <see cref="MemberTracker"/> ���A�N�Z�X���ꂽ�^���w�肵�܂��B</param>
		/// <param name="instance">�������ꂽ�C���X�^���X���w�肵�܂��B</param>
		/// <returns>�C���X�^���X�ɑ�������Ă���l���擾���� <see cref="Expression"/>�B</returns>
		protected internal override DynamicMetaObject GetBoundValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type, DynamicMetaObject instance) { return binder.ReturnMemberTracker(type, BindToInstance(instance)); }

		/// <summary>
		/// �w�肳�ꂽ�������g�p���ăI�u�W�F�N�g�̌Ăяo�������s���� <see cref="Expression"/> ���擾���܂��B
		/// �Ăяo������ GetErrorForDoCall ���Ăяo���āA���m�ȃG���[��\�� <see cref="Expression"/> �܂��͊���̃G���[��\�� <c>null</c> ���擾�ł��܂��B
		/// </summary>
		/// <param name="resolverFactory">�I�[�o�[���[�h�����̕��@��\�� <see cref="OverloadResolverFactory"/> ���w�肵�܂��B</param>
		/// <param name="binder">����̃o�C���f�B���O�Z�}���e�B�N�X���w�肵�܂��B</param>
		/// <param name="arguments">�I�u�W�F�N�g�Ăяo���̈������w�肵�܂��B</param>
		/// <returns>�I�u�W�F�N�g�Ăяo�������s���� <see cref="Expression"/>�B�G���[�����������ꍇ�� <c>null</c> ���Ԃ���܂��B</returns>
		internal override DynamicMetaObject Call(OverloadResolverFactory resolverFactory, ActionBinder binder, params DynamicMetaObject[] arguments)
		{
			if (Method.IsPublic && Method.DeclaringType.IsVisible)
				return binder.MakeCallExpression(resolverFactory, Method, arguments);
			//methodInfo.Invoke(obj, object[] params)
			if (Method.IsStatic)
				return new DynamicMetaObject(
					Ast.Convert(
						Ast.Call(
							AstUtils.Constant(Method),
							typeof(MethodInfo).GetMethod("Invoke", new Type[] { typeof(object), typeof(object[]) }),
							AstUtils.Constant(null),
							AstUtils.NewArrayHelper(typeof(object), Array.ConvertAll(arguments, x => x.Expression))
						),
						Method.ReturnType
					),
					BindingRestrictions.Empty
				);
			if (arguments.Length == 0)
				throw Error.NoInstanceForCall();
			return new DynamicMetaObject(
				Ast.Convert(
					Ast.Call(
						AstUtils.Constant(Method),
						typeof(MethodInfo).GetMethod("Invoke", new Type[] { typeof(object), typeof(object[]) }),
						arguments[0].Expression,
						AstUtils.NewArrayHelper(typeof(object), Array.ConvertAll(ArrayUtils.RemoveFirst(arguments), x => x.Expression))
					),
					Method.ReturnType
				),
				BindingRestrictions.Empty
			);
		}
	}
}
