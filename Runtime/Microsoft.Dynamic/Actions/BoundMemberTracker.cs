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
using Microsoft.Scripting.Actions.Calls;

namespace Microsoft.Scripting.Actions
{
	/// <summary>����̃C���X�^���X�ɑ������ꂽ <see cref="MemberTracker"/> ��\���܂��B</summary>
	public class BoundMemberTracker : MemberTracker
	{
		DynamicMetaObject _instance;
		MemberTracker _tracker;
		object _objInst;

		/// <summary>��ɂȂ� <see cref="MemberTracker"/> �ƁA���������C���X�^���X��\�� <see cref="DynamicMetaObject"/> ���g�p���āA<see cref="Microsoft.Scripting.Actions.BoundMemberTracker"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="tracker">���̃I�u�W�F�N�g�̊�ɂȂ� <see cref="MemberTracker"/> ���w�肵�܂��B</param>
		/// <param name="instance">���������C���X�^���X��\�� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		public BoundMemberTracker(MemberTracker tracker, DynamicMetaObject instance)
		{
			_tracker = tracker;
			_instance = instance;
		}

		/// <summary>��ɂȂ� <see cref="MemberTracker"/> �ƁA���������C���X�^���X���g�p���āA<see cref="Microsoft.Scripting.Actions.BoundMemberTracker"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="tracker">���̃I�u�W�F�N�g�̊�ɂȂ� <see cref="MemberTracker"/> ���w�肵�܂��B</param>
		/// <param name="instance">���������C���X�^���X���w�肵�܂��B</param>
		public BoundMemberTracker(MemberTracker tracker, object instance)
		{
			_tracker = tracker;
			_objInst = instance;
		}

		/// <summary><see cref="MemberTracker"/> �̎�ނ��擾���܂��B</summary>
		public sealed override TrackerTypes MemberType { get { return TrackerTypes.Bound; } }

		/// <summary>�����o��_���I�ɐ錾����^���擾���܂��B</summary>
		public override Type DeclaringType { get { return _tracker.DeclaringType; } }

		/// <summary>�����o�̖��O���擾���܂��B</summary>
		public override string Name { get { return _tracker.Name; } }

		/// <summary>���� <see cref="MemberTracker"/> ���֘A�t����ꂽ�C���X�^���X��\�� <see cref="DynamicMetaObject"/> ���擾���܂��B</summary>
		public DynamicMetaObject Instance { get { return _instance; } }

		/// <summary>���� <see cref="MemberTracker"/> ���֘A�t����ꂽ�C���X�^���X���擾���܂��B</summary>
		public object ObjectInstance { get { return _objInst; } }

		/// <summary>���� <see cref="MemberTracker"/> ���֘A�t����ꂽ <see cref="MemberTracker"/> ���擾���܂��B</summary>
		public MemberTracker BoundTo { get { return _tracker; } }

		/// <summary>
		/// �l���擾���� <see cref="System.Linq.Expressions.Expression"/> ���擾���܂��B
		/// �Ăяo������ GetErrorForGet ���Ăяo���āA���m�ȃG���[��\�� <see cref="System.Linq.Expressions.Expression"/> �܂��͊���̃G���[��\�� <c>null</c> ���擾�ł��܂��B
		/// </summary>
		/// <param name="resolverFactory">�I�[�o�[���[�h�����̕��@��\�� <see cref="OverloadResolverFactory"/> ���w�肵�܂��B</param>
		/// <param name="binder">����̃o�C���f�B���O�Z�}���e�B�N�X���w�肵�܂��B</param>
		/// <param name="type">���� <see cref="MemberTracker"/> ���A�N�Z�X���ꂽ�^���w�肵�܂��B</param>
		/// <returns>�l���擾���� <see cref="System.Linq.Expressions.Expression"/>�B�G���[�����������ꍇ�� <c>null</c> ���Ԃ���܂��B</returns>
		public override DynamicMetaObject GetValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type) { return _tracker.GetBoundValue(resolverFactory, binder, type, _instance); }

		/// <summary>�l�̎擾�Ɋ֘A�t�����Ă���G���[��Ԃ��܂��B</summary>
		/// <param name="binder">����̃o�C���f�B���O�Z�}���e�B�N�X���w�肵�܂��B</param>
		/// <returns>�l�̎擾�Ɋ֘A�t�����Ă���G���[�B�܂��́A�Ăяo�����ɂ���Ċ���̃G���[���b�Z�[�W���񋟂���邱�Ƃ����� <c>null</c>�B</returns>
		public override ErrorInfo GetError(ActionBinder binder) { return _tracker.GetBoundError(binder, _instance); }

		/// <summary>
		/// �l�������� <see cref="System.Linq.Expressions.Expression"/> ���擾���܂��B
		/// �Ăяo������ GetErrorForSet ���Ăяo���āA���m�ȃG���[��\�� <see cref="System.Linq.Expressions.Expression"/> �܂��͊���̃G���[��\�� <c>null</c> ���擾�ł��܂��B
		/// </summary>
		/// <param name="resolverFactory">�I�[�o�[���[�h�����̕��@��\�� <see cref="OverloadResolverFactory"/> ���w�肵�܂��B</param>
		/// <param name="binder">����̃o�C���f�B���O�Z�}���e�B�N�X���w�肵�܂��B</param>
		/// <param name="type">���� <see cref="MemberTracker"/> ���A�N�Z�X���ꂽ�^���w�肵�܂��B</param>
		/// <param name="value">���� <see cref="MemberTracker"/> �ɑ�������l���w�肵�܂��B</param>
		/// <returns>�l�������� <see cref="System.Linq.Expressions.Expression"/>�B�G���[�����������ꍇ�� <c>null</c> ���Ԃ���܂��B</returns>
		public override DynamicMetaObject SetValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type, DynamicMetaObject value) { return _tracker.SetBoundValue(resolverFactory, binder, type, value, _instance); }
	}
}
