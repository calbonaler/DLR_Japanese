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
using System.Reflection;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Actions
{
	/// <summary>�^��\���܂��B</summary>
	public abstract class TypeTracker : MemberTracker, IMembersList
	{
		/// <summary><see cref="Microsoft.Scripting.Actions.TypeTracker"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		protected TypeTracker() { }

		/// <summary>���� <see cref="TypeTracker"/> �ɂ���ĕ\�����^���擾���܂��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
		public abstract Type Type { get; }

		/// <summary>���̌^���W�F�l���b�N�^���ǂ����������l���擾���܂��B</summary>
		public abstract bool IsGenericType { get; }

		/// <summary>���̌^���p�u���b�N�Ƃ��Đ錾����Ă��邩�ǂ����������l���擾���܂��B</summary>
		public abstract bool IsPublic { get; }

		/// <summary>���̌^�Ɋ܂܂�Ă��邷�ׂẴ����o�̖��O���擾���܂��B</summary>
		/// <returns>�����o�̖��O�̃��X�g�B</returns>
		public virtual IEnumerable<string> GetMemberNames()
		{
			HashSet<string> names = new HashSet<string>();
			CollectMembers(names, Type);
			return names;
		}

		/// <summary>�w�肳�ꂽ�^�Ɋ܂܂�Ă��邷�ׂẴ����o�̖��O���w�肳�ꂽ�Z�b�g�ɒǉ����܂��B</summary>
		/// <param name="names">�����o�̖��O��ǉ�����Z�b�g���w�肵�܂��B</param>
		/// <param name="t">�ǉ����郁���o���܂�ł���^���w�肵�܂��B</param>
		internal static void CollectMembers(ISet<string> names, Type t)
		{
			foreach (var mi in t.GetMembers())
			{
				if (mi.MemberType != MemberTypes.Constructor)
					names.Add(mi.Name);
			}
		}

		/// <summary>���I����S�̂ɂ킽�� <see cref="TypeTracker"/> ���� <see cref="Type"/> �ւ̈ÖٓI�ȕϊ���L�������܂��B</summary>
		/// <param name="tracker">�ϊ����� <see cref="TypeTracker"/>�B</param>
		/// <returns><see cref="TypeTracker"/> �ɑΉ����� <see cref="Type"/>�B</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates")]
		public static explicit operator Type(TypeTracker tracker)
		{
			var tg = tracker as TypeGroup;
			if (tg != null)
			{
				Type res;
				if (!tg.TryGetNonGenericType(out res))
					throw ScriptingRuntimeHelpers.SimpleTypeError("expected non-generic type, got generic-only type");
				return res;
			}
			return tracker.Type;
		}
	}
}
