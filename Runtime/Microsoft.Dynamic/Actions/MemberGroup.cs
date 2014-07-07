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

using System.Collections.Generic;
using System.Reflection;
using Microsoft.Contracts;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions
{
	/// <summary>
	/// ���p�\�ȃ����o�𒲂ׂ邽�߂ɒʏ�v���ɉ����Đ�������� <see cref="MemberTracker"/> �̃R���N�V������\���܂��B
	/// ���̃N���X�ɂ͓�����ނ̕����̃����o���قȂ��ނ̕����̃����o���܂߂邱�Ƃ��ł��܂��B
	/// </summary>
	/// <remarks>
	/// �ł���ʓI�� <see cref="MemberGroup"/> �̎擾���� <see cref="ActionBinder.GetMember"/> �ł��B
	/// �������� DLR �͕p�ɂɃ��[�U�[�ɂ��l�𐶐����� <see cref="MemberTracker"/> �ɑ΂���o�C���f�B���O�����s���܂��B
	/// ��������̌��ʂ������o���̂𐶐�����Ȃ�΁A<see cref="ActionBinder"/> �� ReturnMemberTracker ��ʂ��ă��[�U�[�Ɍ��J����l��񋟂ł��܂��B
	/// <see cref="ActionBinder"/> �̓��[�U�[�ɑ΂��郁���o�̌��J�Ɠ����Ɍ^����̃����o�̎擾�Ɋւ������̋@�\��񋟂��܂��B
	/// �^����̃����o�̎擾�̓��t���N�V�����Ɍ����ɑΉ����A���[�U�[�ɑ΂��郁���o�̌��J�� <see cref="MemberTracker"/> �𒼐ڌ��J���邱�ƂɑΉ����܂��B
	/// </remarks>
	public class MemberGroup : IEnumerable<MemberTracker>
	{
		/// <summary>��� <see cref="MemberGroup"/> ��\���܂��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly MemberGroup EmptyGroup = new MemberGroup(MemberTracker.EmptyTrackers);

		readonly MemberTracker[] _members;

		/// <summary>�w�肳�ꂽ <see cref="MemberTracker"/> ���g�p���āA<see cref="Microsoft.Scripting.Actions.MemberGroup"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="members">���� <see cref="MemberGroup"/> �Ɋ܂߂郁���o���w�肵�܂��B</param>
		public MemberGroup(params MemberTracker[] members)
		{
			ContractUtils.RequiresNotNullItems(members, "members");
			_members = members;
		}

		/// <summary>�w�肳�ꂽ <see cref="MemberInfo"/> ���g�p���āA<see cref="Microsoft.Scripting.Actions.MemberGroup"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="members">���� <see cref="MemberGroup"/> �Ɋ܂߂郁���o���w�肵�܂��B</param>
		public MemberGroup(params MemberInfo[] members)
		{
			ContractUtils.RequiresNotNullItems(members, "members");
			_members = System.Array.ConvertAll(members, x => MemberTracker.FromMemberInfo(x));
		}

		/// <summary>���� <see cref="MemberGroup"/> ���Ɋ܂܂�Ă��郁���o�̐����擾���܂��B</summary>
		public int Count { get { return _members.Length; } }

		/// <summary>���� <see cref="MemberGroup"/> ���̎w�肳�ꂽ�ʒu�ɂ��郁���o���擾���܂��B</summary>
		/// <param name="index">�����o�̈ʒu������ 0 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�ʒu�ɂ��� <see cref="MemberTracker"/>�B</returns>
		public MemberTracker this[int index] { get { return _members[index]; } }

		/// <summary>���̃R���N�V�����𔽕���������񋓎q��Ԃ��܂��B</summary>
		/// <returns>�R���N�V�����𔽕��������邽�߂Ɏg�p�ł��� <see cref="System.Collections.Generic.IEnumerator&lt;MemberTracker&gt;"/>�B</returns>
		[Pure]
		public IEnumerator<MemberTracker> GetEnumerator() { return ((IEnumerable<MemberTracker>)_members).GetEnumerator(); }

		/// <summary>���̃R���N�V�����𔽕���������񋓎q��Ԃ��܂��B</summary>
		/// <returns>�R���N�V�����𔽕��������邽�߂Ɏg�p�ł��� <see cref="System.Collections.IEnumerator"/>�B</returns>
		[Pure]
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}
}