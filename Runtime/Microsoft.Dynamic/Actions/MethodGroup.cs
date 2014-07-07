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
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions
{
	/// <summary>
	/// ���\�b�h�̈�ӂ̃R���N�V������\���܂��B
	/// ��ʂɂ́A���̈�ӂȃZ�b�g�͈قȂ鍀���̃��\�b�h���܂ޓ������O�ŃI�[�o�[���[�h���ꂽ���ׂẴ��\�b�h�ł��B
	/// �����̃��\�b�h�͒P��̘_���I�ɃI�[�o�[���[�h���ꂽ .NET �^�̗v�f��\���܂��B
	/// </summary>
	/// <remarks>
	/// ��{�� DLR �o�C���_�[�Ƀ��\�b�h�݂̂��܂� <see cref="MemberGroup"/> ���񋟂��ꂽ�ꍇ�ɁA<see cref="MethodGroup"/> �𐶐����܂��B
	/// <see cref="MethodGroup"/> �͂��ꂼ��̈�ӂȃ��\�b�h�̃O���[�v���ƂɈ�ӂȃC���X�^���X�ƂȂ�܂��B
	/// </remarks>
	public class MethodGroup : MemberTracker
	{
		Dictionary<Type[], MethodGroup> _boundGenerics;

		/// <summary>�w�肳�ꂽ <see cref="MethodTracker"/> ���g�p���āA<see cref="Microsoft.Scripting.Actions.MethodGroup"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="methods"><see cref="MemberGroup"/> �Ɋi�[����� <see cref="MethodTracker"/> ���w�肵�܂��B</param>
		internal MethodGroup(params MethodTracker[] methods) { Methods = new ReadOnlyCollection<MethodTracker>(methods); }

		/// <summary><see cref="MemberTracker"/> �̎�ނ��擾���܂��B</summary>
		public override sealed TrackerTypes MemberType { get { return TrackerTypes.MethodGroup; } }

		/// <summary>�����o��_���I�ɐ錾����^���擾���܂��B</summary>
		public override Type DeclaringType { get { return Methods[0].DeclaringType; } }

		/// <summary>�����o�̖��O���擾���܂��B</summary>
		public override string Name { get { return Methods[0].Name; } }

		/// <summary>���� <see cref="MethodGroup"/> ���ɃC���X�^���X���\�b�h�����݂��邩�ǂ����������l���擾���܂��B</summary>
		public bool ContainsInstance { get { return Methods.Any(x => !x.IsStatic); } }

		/// <summary>���� <see cref="MethodGroup"/> ���ɐÓI���\�b�h�����݂��邩�ǂ����������l���擾���܂��B</summary>
		public bool ContainsStatic { get { return Methods.Any(x => x.IsStatic); } }

		/// <summary>���� <see cref="MethodGroup"/> �Ɋ܂܂�Ă��邷�ׂẴ��\�b�h���擾���܂��B</summary>
		public ReadOnlyCollection<MethodTracker> Methods { get; private set; }

		/// <summary>���� <see cref="MethodGroup"/> �Ɋ܂܂�Ă��邷�ׂẴ��\�b�h�ɑ΂��� <see cref="MethodBase"/> ���擾���܂��B</summary>
		/// <returns><see cref="MethodGroup"/> �Ɋ܂܂�Ă��邷�ׂẴ��\�b�h�ɑ΂��� <see cref="MethodBase"/> �̔z��B</returns>
		public MethodBase[] GetMethodBases() { return Methods.Select(x => (MethodBase)x.Method).ToArray(); }

		/// <summary>
		/// �l���擾���� <see cref="System.Linq.Expressions.Expression"/> ���擾���܂��B
		/// �Ăяo������ GetErrorForGet ���Ăяo���āA���m�ȃG���[��\�� <see cref="System.Linq.Expressions.Expression"/> �܂��͊���̃G���[��\�� <c>null</c> ���擾�ł��܂��B
		/// </summary>
		/// <param name="resolverFactory">�I�[�o�[���[�h�����̕��@��\�� <see cref="OverloadResolverFactory"/> ���w�肵�܂��B</param>
		/// <param name="binder">����̃o�C���f�B���O�Z�}���e�B�N�X���w�肵�܂��B</param>
		/// <param name="type">���� <see cref="MemberTracker"/> ���A�N�Z�X���ꂽ�^���w�肵�܂��B</param>
		/// <returns>�l���擾���� <see cref="System.Linq.Expressions.Expression"/>�B�G���[�����������ꍇ�� <c>null</c> ���Ԃ���܂��B</returns>
		public override DynamicMetaObject GetValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type) { return base.GetValue(resolverFactory, binder, type); }

		/// <summary>
		/// �o�C���f�B���O���\�ȏꍇ�A�V���������o�g���b�J�[��Ԃ��w�肳�ꂽ�C���X�^���X�Ƀ����o�g���b�J�[���֘A�t���܂��B
		/// �o�C���f�B���O���s�\�ȏꍇ�A�����̃����o�g���b�J�[���Ԃ���܂��B
		/// �Ⴆ�΁A�ÓI�t�B�[���h�ւ̃o�C���f�B���O�́A���̃����o�g���b�J�[��Ԃ��܂��B
		/// �C���X�^���X�t�B�[���h�ւ̃o�C���f�B���O�́A�C���X�^���X��n�� GetBoundValue �܂��� SetBoundValue �𓾂�V���� <see cref="BoundMemberTracker"/> ��Ԃ��܂��B
		/// </summary>
		/// <param name="instance">�����o�g���b�J�[���֘A�t����C���X�^���X���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�C���X�^���X�Ɋ֘A�t����ꂽ�����o�g���b�J�[�B</returns>
		public override MemberTracker BindToInstance(DynamicMetaObject instance) { return ContainsInstance ? (MemberTracker)new BoundMemberTracker(this, instance) : this; }

		/// <summary>
		/// �C���X�^���X�ɑ�������Ă���l���擾���� <see cref="System.Linq.Expressions.Expression"/> ���擾���܂��B
		/// �J�X�^�������o�g���b�J�[�͂��̃��\�b�h���I�[�o�[���C�h���āA�C���X�^���X�ւ̃o�C���h���̓Ǝ��̓����񋟂ł��܂��B
		/// </summary>
		/// <param name="resolverFactory">�I�[�o�[���[�h�����̕��@��\�� <see cref="OverloadResolverFactory"/> ���w�肵�܂��B</param>
		/// <param name="binder">����̃o�C���f�B���O�Z�}���e�B�N�X���w�肵�܂��B</param>
		/// <param name="type">���� <see cref="MemberTracker"/> ���A�N�Z�X���ꂽ�^���w�肵�܂��B</param>
		/// <param name="instance">�������ꂽ�C���X�^���X���w�肵�܂��B</param>
		/// <returns>�C���X�^���X�ɑ�������Ă���l���擾���� <see cref="System.Linq.Expressions.Expression"/>�B</returns>
		protected internal override DynamicMetaObject GetBoundValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type, DynamicMetaObject instance) { return binder.ReturnMemberTracker(type, BindToInstance(instance)); }

		/// <summary>
		/// ���� <see cref="MethodGroup"/> �Ɋ܂܂�Ă��郁�\�b�h�ɑ΂��Ďw�肳�ꂽ�W�F�l���b�N�^������K�p���邱�ƂŃW�F�l���b�N���\�b�h���쐬���܂��B
		/// �w�肳�ꂽ�^�����ɑ΂��ēK�p�ł���W�F�l���b�N���\�b�h��`�����݂��Ȃ��ꍇ�� <c>null</c> ��Ԃ��܂��B
		/// </summary>
		/// <param name="types">���� <see cref="MethodGroup"/> �Ɋ܂܂�Ă��郁�\�b�h�ɑ΂��ēK�p����W�F�l���b�N�^������\�� <see cref="Type"/> �^�̔z����w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�W�F�l���b�N�^�������K�p���ꂽ�W�F�l���b�N���\�b�h�̃R���N�V������\�� <see cref="MethodGroup"/>�B�K�p�ł��郁�\�b�h�����݂��Ȃ��ꍇ�� <c>null</c> ��Ԃ��܂��B</returns>
		public MethodGroup MakeGenericMethod(Type[] types)
		{
			// �L���b�V�����ꂽ���\�b�h���ŏ��ɒT��
			MethodGroup mg;
			if (_boundGenerics != null)
			{
				lock (_boundGenerics)
				{
					if (_boundGenerics.TryGetValue(types, out mg))
						return mg;
				}
			}
			// �W�F�l���b�N�^�[�Q�b�g��K�؂ȍ��� (�^�p�����[�^�̐�) ���g�p���ĒT��
			// �݊��ȃ^�[�Q�b�g�͒�`�ɂ�� MethodInfo (�R���X�g���N�^�͌^�������Ƃ�Ȃ�)
			var targets = Methods.Where(x => x.Method.ContainsGenericParameters && x.Method.GetGenericArguments().Length == types.Length).Select(x => (MethodTracker)MemberTracker.FromMemberInfo(x.Method.MakeGenericMethod(types)));
			if (!targets.Any())
				return null;
			// �������ꂽ�^���������^�[�Q�b�g���܂ސV���� MethodGroup ���쐬���A�L���b�V������
			mg = new MethodGroup(targets.ToArray());
			if (_boundGenerics == null)
				Interlocked.CompareExchange<Dictionary<Type[], MethodGroup>>(ref _boundGenerics, new Dictionary<Type[], MethodGroup>(1, ListEqualityComparer<Type>.Instance), null);
			lock (_boundGenerics)
				_boundGenerics[types] = mg;
			return mg;
		}

		sealed class ListEqualityComparer<T> : EqualityComparer<IEnumerable<T>>
		{
			internal static readonly ListEqualityComparer<T> Instance = new ListEqualityComparer<T>();

			ListEqualityComparer() { }

			public override bool Equals(IEnumerable<T> x, IEnumerable<T> y) { return x.SequenceEqual(y); }

			public override int GetHashCode(IEnumerable<T> obj) { return obj.Aggregate(6551, (x, y) => x ^ (x << 5) ^ y.GetHashCode()); }
		}
	}
}
