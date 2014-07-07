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
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Microsoft.Contracts;
using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>���t���N�V���������o�̃L���b�V����񋟂��܂��B1 �̗v���ɑ΂��ď�� 1 �̒l�̃Z�b�g���Ԃ���܂��B</summary>
	public static class ReflectionCache
	{
		static readonly ConcurrentDictionary<MethodBaseCache, MethodGroup> _functions = new ConcurrentDictionary<MethodBaseCache, MethodGroup>();
		static readonly ConcurrentDictionary<Type, TypeTracker> _typeCache = new ConcurrentDictionary<Type, TypeTracker>();

		/// <summary>
		/// �w�肳�ꂽ�^���烁�\�b�h�O���[�v���擾���܂��B
		/// �Ԃ���郁�\�b�h�O���[�v�͌^/���O�̑g�ł͂Ȃ��A��`���ꂽ���\�b�h�Ɋ�Â��Ĉ�ӂł��B
		/// ����������ƁA��{�N���X�Ǝw�肳�ꂽ���O�ŐV�������\�b�h���`���Ȃ��h���N���X�ɑ΂��� GetMethodGroup �Ăяo���́A�����̌^�ɑ΂��ē����C���X�^���X��Ԃ��܂��B
		/// </summary>
		/// <param name="type">���\�b�h�O���[�v���擾����^���w�肵�܂��B</param>
		/// <param name="name">�擾���郁�\�b�h�O���[�v�̖��O���w�肵�܂��B</param>
		/// <returns>�擾���ꂽ���\�b�h�O���[�v�B</returns>
		public static MethodGroup GetMethodGroup(Type type, string name) { return GetMethodGroup(type, name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.InvokeMethod, null); }

		/// <summary>
		/// �w�肳�ꂽ�^���烁�\�b�h�O���[�v���擾���܂��B
		/// �Ԃ���郁�\�b�h�O���[�v�͌^/���O�̑g�ł͂Ȃ��A��`���ꂽ���\�b�h�Ɋ�Â��Ĉ�ӂł��B
		/// ����������ƁA��{�N���X�Ǝw�肳�ꂽ���O�ŐV�������\�b�h���`���Ȃ��h���N���X�ɑ΂��� GetMethodGroup �Ăяo���́A�����̌^�ɑ΂��ē����C���X�^���X��Ԃ��܂��B
		/// </summary>
		/// <param name="type">���\�b�h�O���[�v���擾����^���w�肵�܂��B</param>
		/// <param name="name">�擾���郁�\�b�h�O���[�v�̖��O���w�肵�܂��B</param>
		/// <param name="bindingFlags">�������@�𐧌䂷�� <see cref="BindingFlags"/> ���w�肵�܂��B</param>
		/// <param name="filter">�������ʂ̔z��ɓK�p�����t�B���^�[���w�肵�܂��B���̈����ɂ� <c>null</c> ���w��ł��܂��B</param>
		/// <returns>�擾���ꂽ���\�b�h�O���[�v�B</returns>
		public static MethodGroup GetMethodGroup(Type type, string name, BindingFlags bindingFlags, MemberFilter filter)
		{
			ContractUtils.RequiresNotNull(type, "type");
			ContractUtils.RequiresNotNull(name, "name");
			var mems = type.FindMembers(MemberTypes.Method, bindingFlags, filter ?? ((x, _) => x.Name == name), null);
			return mems.Length > 0 ? GetMethodGroup(name, Array.ConvertAll(mems, x => (MethodInfo)x)) : null;
		}

		/// <summary>�w�肳�ꂽ���\�b�h�z�񂩂烁�\�b�h�O���[�v���擾���܂��B</summary>
		/// <param name="name">�擾���郁�\�b�h�O���[�v�̖��O���w�肵�܂��B</param>
		/// <param name="methods">���\�b�h�O���[�v���쐬���郁�\�b�h�̔z����w�肵�܂��B</param>
		/// <returns>�擾���ꂽ���\�b�h�O���[�v�B</returns>
		public static MethodGroup GetMethodGroup(string name, MethodBase[] methods) { return _functions.GetOrAdd(new MethodBaseCache(name, methods), _ => new MethodGroup(Array.ConvertAll(methods, x => (MethodTracker)MemberTracker.FromMemberInfo(x)))); }

		/// <summary>�w�肳�ꂽ�����o�O���[�v���烁�\�b�h�O���[�v���擾���܂��B�����o�O���[�v�ɂ̓��\�b�h�݂̂��܂܂�Ă���K�v������܂��B</summary>
		/// <param name="name">�擾���郁�\�b�h�O���[�v�̖��O���w�肵�܂��B</param>
		/// <param name="mems">���\�b�h�O���[�v���쐬���郁���o�O���[�v���w�肵�܂��B</param>
		/// <returns>�擾���ꂽ���\�b�h�O���[�v�B</returns>
		public static MethodGroup GetMethodGroup(string name, MemberGroup mems)
		{
			if (mems.Count <= 0)
				return null;
			var bases = new MethodBase[mems.Count];
			var trackers = new MethodTracker[mems.Count];
			for (int i = 0; i < bases.Length; i++)
				bases[i] = (trackers[i] = (MethodTracker)mems[i]).Method;
			return _functions.GetOrAdd(new MethodBaseCache(name, bases), _ => new MethodGroup(trackers));
		}

		/// <summary>�w�肳�ꂽ�^�ɑ΂��� <see cref="TypeTracker"/> ��Ԃ��܂��B</summary>
		/// <param name="type"><see cref="TypeTracker"/> ���擾����^���w�肵�܂��B</param>
		/// <returns>�^�ɑ΂��� <see cref="TypeTracker"/>�B</returns>
		public static TypeTracker GetTypeTracker(Type type) { return _typeCache.GetOrAdd(type, x => new ReflectedTypeTracker(x)); }

		// TODO: Make me private again
		/// <summary>���\�b�h�̔z�񂨂�і��O���i�[���āA���\�b�h�O���[�v�̓��������`���܂��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")] // TODO: fix
		public class MethodBaseCache
		{
			readonly MethodBase[] _members;
			readonly string _name;

			/// <summary>�w�肳�ꂽ���O�ƃ��\�b�h�̔z����g�p���āA<see cref="Microsoft.Scripting.Runtime.ReflectionCache.MethodBaseCache"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
			/// <param name="name">���\�b�h�O���[�v�̖��O���w�肵�܂��B</param>
			/// <param name="members">���\�b�h�O���[�v�Ɋ܂܂�郁�\�b�h��\���z����w�肵�܂��B</param>
			public MethodBaseCache(string name, MethodBase[] members)
			{
				// sort by module ID / token so that the Equals / GetHashCode doesn't have to line up members if reflection returns them in different orders.
				Array.Sort(members, (x, y) => x.Module == y.Module ? x.MetadataToken.CompareTo(y.MetadataToken) : x.Module.ModuleVersionId.CompareTo(y.Module.ModuleVersionId));
				_name = name;
				_members = members;
			}

			/// <summary>���̃I�u�W�F�N�g�Ǝw�肳�ꂽ�I�u�W�F�N�g�����������ǂ����𔻒f���܂��B</summary>
			/// <param name="obj">���������ǂ����𒲂ׂ�I�u�W�F�N�g���w�肵�܂��B</param>
			/// <returns>���̃I�u�W�F�N�g�Ǝw�肳�ꂽ�I�u�W�F�N�g���������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
			[Confined]
			public override bool Equals(object obj)
			{
				var other = obj as MethodBaseCache;
				if (other == null || _members.Length != other._members.Length || other._name != _name)
					return false;
				return _members.Zip(other._members,
					(x, y) => x.DeclaringType == y.DeclaringType && x.MetadataToken == y.MetadataToken && x.IsGenericMethod == y.IsGenericMethod &&
						(!x.IsGenericMethod || x.GetGenericArguments().SequenceEqual(y.GetGenericArguments()))
				).All(x => x);
			}

			/// <summary>���̃I�u�W�F�N�g�̃n�b�V���l���v�Z���܂��B</summary>
			/// <returns>�I�u�W�F�N�g�̃n�b�V���l�B</returns>
			[Confined]
			public override int GetHashCode() { return _members.Aggregate(6551, (x, y) => x ^ (x << 5 ^ y.DeclaringType.GetHashCode() ^ y.MetadataToken)) ^ _name.GetHashCode(); }
		}
	}
}
