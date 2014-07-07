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
using System.Diagnostics;
using System.Linq;
using Microsoft.Contracts;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions
{
	/// <summary>
	/// �W�F�l���b�N�^�����̐��ɂ���ʂ��ꂽ�^�̃O���[�v��\���܂��B
	/// ���̃O���[�v���P��̌^�Ƃ��Ĉ���ꂽ�ꍇ�́A�O���[�v�Ɋ܂܂��W�F�l���b�N�łȂ��^��\�����ƂɂȂ�܂��B
	/// </summary>
	public sealed class TypeGroup : TypeTracker
	{
		readonly string _name;

		TypeGroup(Type t1, int arity1, Type t2, int arity2)
		{
			// TODO: types of different arities might be inherited, but we don't support that yet:
			Debug.Assert(t1.DeclaringType == t2.DeclaringType);
			Debug.Assert(arity1 != arity2);
			TypesByArity = new ReadOnlyDictionary<int, Type>(new Dictionary<int, System.Type>() { { arity1, t1 }, { arity2, t2 } });
			_name = ReflectionUtils.GetNormalizedTypeName(t1);
			Debug.Assert(_name == ReflectionUtils.GetNormalizedTypeName(t2));
		}

		TypeGroup(Type t1, TypeGroup existingTypes)
		{
			// TODO: types of different arities might be inherited, but we don't support that yet:
			Debug.Assert(t1.DeclaringType == existingTypes.DeclaringType);
			Debug.Assert(ReflectionUtils.GetNormalizedTypeName(t1) == existingTypes.Name);
			var typesByArity = new Dictionary<int, Type>(existingTypes.TypesByArity);
			typesByArity[GetGenericArity(t1)] = t1;
			TypesByArity = new ReadOnlyDictionary<int, Type>(typesByArity);
			_name = existingTypes.Name;
		}

		/// <summary>���̃I�u�W�F�N�g�̕�����\����Ԃ��܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̕�����\���B</returns>
		[Confined]
		public override string ToString() { return base.ToString() + ":" + Name + "(" + string.Join(", ", Types.Select(x => x.Name)) + ")"; }

		/// <summary>���� <see cref="TypeGroup"/> �Ɋ܂܂�Ă��邷�ׂĂ̌^�̃����o�̖��O���擾���܂��B</summary>
		/// <returns><see cref="TypeGroup"/> ���̂��ׂĂ̌^�̃����o�̖��O�B</returns>
		public override IEnumerable<string> GetMemberNames()
		{
			HashSet<string> members = new HashSet<string>();
			foreach (var t in Types)
				CollectMembers(members, t);
			return members;
		}

		/// <summary>�W�F�l���b�N�^�������w�肳�ꂽ�����^���擾���܂��B</summary>
		/// <param name="arity">�擾����^�̂��W�F�l���b�N�^�����̌����w�肵�܂��B</param>
		/// <returns>�W�F�l���b�N�^�������w�肳�ꂽ�����^��\�� <see cref="TypeTracker"/>�B�Ώۂ̌^������ <see cref="TypeGroup"/> �ɑ��݂��Ȃ��ꍇ�� <c>null</c> ��Ԃ��܂��B</returns>
		public TypeTracker GetTypeForArity(int arity)
		{
			Type typeWithMatchingArity;
			if (!TypesByArity.TryGetValue(arity, out typeWithMatchingArity))
				return null;
			return ReflectionCache.GetTypeTracker(typeWithMatchingArity);
		}

		/// <summary>������ <see cref="TypeTracker"/> �ɐV���� <see cref="TypeTracker"/> ���}�[�W���܂��B</summary>
		/// <param name="existingType">�}�[�W����� <see cref="TypeTracker"/> ���w�肵�܂��B<c>null</c> �ɂ��邱�Ƃ��ł��܂��B</param>
		/// <param name="newType">�}�[�W���ꂽ���X�g�ɒǉ������V�����^��\�� <see cref="TypeTracker"/> ���w�肵�܂��B</param>
		/// <returns>�}�[�W���ꂽ���X�g�B</returns>
		public static TypeTracker Merge(TypeTracker existingType, TypeTracker newType)
		{
			ContractUtils.RequiresNotNull(newType, "newType");
			if (existingType == null)
				return newType;
			var simpleType = existingType as ReflectedTypeTracker;
			if (simpleType != null)
			{
				var existingArity = GetGenericArity(simpleType.Type);
				var newArity = GetGenericArity(newType.Type);
				if (existingArity == newArity)
					return newType;
				return new TypeGroup(simpleType.Type, existingArity, newType.Type, newArity);
			}
			return new TypeGroup(newType.Type, existingType as TypeGroup);
		}

		/// <summary>�W�F�l���b�N�^�����̌����擾���܂��B</summary>
		static int GetGenericArity(Type type)
		{
			if (!type.IsGenericType)
				return 0;
			Debug.Assert(type.IsGenericTypeDefinition);
			return type.GetGenericArguments().Length;
		}

		/// <summary>
		/// ���� <see cref="TypeGroup"/> �Ɋ܂܂�Ă����W�F�l���b�N�^���擾���܂��B
		/// <see cref="TypeGroup"/> ���ɔ�W�F�l���b�N�^�����݂��Ȃ��ꍇ�͗�O���X���[���܂��B
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")] // TODO: fix
		public Type NonGenericType
		{
			get
			{
				Type nonGenericType;
				if (TryGetNonGenericType(out nonGenericType))
					return nonGenericType;
				throw Error.NonGenericWithGenericGroup(Name);
			}
		}

		/// <summary>���� <see cref="TypeGroup"/> �Ɋ܂܂�Ă����W�F�l���b�N�^�̎擾�����݂܂��B</summary>
		/// <param name="nonGenericType">�擾���ꂽ��W�F�l���b�N�^���i�[�����ϐ����w�肵�܂��B</param>
		/// <returns><see cref="TypeGroup"/> ���ɔ�W�F�l���b�N�^�����݂��Ď擾���ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public bool TryGetNonGenericType(out Type nonGenericType) { return TypesByArity.TryGetValue(0, out nonGenericType); }

		/// <summary>���� <see cref="TypeGroup"/> �Ɋ܂܂�Ă��邷�ׂĂ̌^���擾���܂��B</summary>
		public IEnumerable<Type> Types { get { return TypesByArity.Values; } }

		/// <summary>���� <see cref="TypeGroup"/> ������񋟂��ꂽ�W�F�l���b�N�^�����̐��ɉ������^��Ԃ��f�B�N�V���i�����擾���܂��B</summary>
		public ReadOnlyDictionary<int, Type> TypesByArity { get; private set; }

		#region MemberTracker overrides

		/// <summary><see cref="MemberTracker"/> �̎�ނ��擾���܂��B</summary>
		public override sealed TrackerTypes MemberType { get { return TrackerTypes.TypeGroup; } }

		/// <summary>���� <see cref="TypeGroup"/> �Ɋ܂܂�Ă��邷�ׂĂ̌^��錾����^���擾���܂��B</summary>
		public override Type DeclaringType { get { return Types.First().DeclaringType; } }

		/// <summary><see cref="TypeGroup"/> �̊�{�����擾���܂��B���̖��O�͌^�����̐��������Ă��ׂĂ̌^�ŋ��L����Ă��܂��B</summary>
		public override string Name { get { return _name; } }

		/// <summary>
		/// ���� <see cref="TypeGroup"/> �Ɋ܂܂�Ă����W�F�l���b�N�^���擾���܂��B
		/// <see cref="TypeGroup"/> ���ɔ�W�F�l���b�N�^�����݂��Ȃ��ꍇ�͗�O���X���[���܂��B
		/// </summary>
		public override Type Type { get { return NonGenericType; } }

		/// <summary>���� <see cref="TypeGroup"/> �ɔ�W�F�l���b�N�^���܂܂�Ă��邩�ǂ����������l���擾���܂��B</summary>
		public override bool IsGenericType { get { return TypesByArity.Keys.Any(x => x > 0); } }

		/// <summary>
		/// ���� <see cref="TypeGroup"/> �Ɋ܂܂�Ă����W�F�l���b�N�^���p�u���b�N�Ƃ��Đ錾����Ă��邩�ǂ����������l���擾���܂��B
		/// <see cref="TypeGroup"/> ���ɔ�W�F�l���b�N�^���܂܂�Ă��Ȃ��ꍇ�͗�O���X���[���܂��B
		/// </summary>
		public override bool IsPublic { get { return NonGenericType.IsPublic; } }

		#endregion
	}
}
