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
using System.Runtime.Serialization;
using System.Security.Permissions;
using Microsoft.Contracts;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting
{
	/// <summary>�啶���Ə���������ʂ���ꍇ�Ƃ��Ȃ��ꍇ�̗������T�|�[�g���镶����̓����\����񋟂��܂��B</summary>
	/// <remarks>
	/// �K��ł͂��ׂĂ̌����͑啶���Ə���������ʂ��܂��B
	/// �啶���Ə���������ʂ��Ȃ������͍ŏ��ɒʏ�� <see cref="SymbolId"/> ���쐬�����̂��A<see cref="CaseInsensitiveIdentifier"/> �v���p�e�B�ɃA�N�Z�X���邱�ƂŎ��s�ł��܂��B
	/// </remarks>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1036:OverrideMethodsOnComparableTypes"), Serializable]
	public struct SymbolId : ISerializable, IComparable, IComparable<SymbolId>, IEquatable<SymbolId>
	{
		/// <summary>�w�肳�ꂽ ID ���g�p���� <see cref="Microsoft.Scripting.SymbolId"/> �\���̂̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="value">���� <see cref="SymbolId"/> �̎��ʎq���w�肵�܂��B</param>
		public SymbolId(int value) : this() { Id = value; }

		SymbolId(SerializationInfo info, StreamingContext context) : this()
		{
			ContractUtils.RequiresNotNull(info, "info");
			Id = SymbolTable.StringToId(info.GetString("symbolName")).Id;
		}

		/// <summary><c>null</c> ������ɑ΂��� ID ��\���܂��B</summary>
		public const int EmptyId = 0;
		/// <summary>������ ID ��\���܂��B</summary>
		public const int InvalidId = -1;

		/// <summary><c>null</c> ������ɑ΂��� <see cref="SymbolId"/> ��\���܂��B</summary>
		public static readonly SymbolId Empty = new SymbolId(EmptyId);

		/// <summary>�����Ȓl�ɑ΂��� <see cref="SymbolId"/> ��\���܂��B</summary>
		public static readonly SymbolId Invalid = new SymbolId(InvalidId);

		/// <summary>���� <see cref="SymbolId"/> �� null �������\���Ă��邩�ǂ����������l���擾���܂��B</summary>
		public bool IsEmpty { get { return Id == EmptyId; } }

		/// <summary>���� <see cref="SymbolId"/> �������ł��邩�ǂ����������l���擾���܂��B</summary>
		public bool IsInvalid { get { return Id == InvalidId; } }

		/// <summary>���� <see cref="SymbolId"/> ���\�����镶����� ID ���擾���܂��B</summary>
		public int Id { get; private set; }

		/// <summary>���� <see cref="SymbolId"/> �ɑ΂���啶���Ə���������ʂ��Ȃ� <see cref="SymbolId"/> ���擾���܂��B</summary>
		public SymbolId CaseInsensitiveIdentifier { get { return new SymbolId(Id & ~SymbolTable.CaseVersionMask); } }

		/// <summary>���� <see cref="SymbolId"/> �ɑ΂���啶���Ə���������ʂ��Ȃ� ID ���擾���܂��B</summary>
		public int CaseInsensitiveId { get { return Id & ~SymbolTable.CaseVersionMask; } }

		/// <summary>���� <see cref="SymbolId"/> ���啶���Ə���������ʂ��Ȃ����ǂ����������l���擾���܂��B</summary>
		public bool IsCaseInsensitive { get { return (Id & SymbolTable.CaseVersionMask) == 0; } }

		/// <summary>���� <see cref="SymbolId"/> ���w�肳�ꂽ <see cref="SymbolId"/> �Ɠ����������\���Ă��邩�ǂ����������l���擾���܂��B</summary>
		/// <param name="other">��r���� <see cref="SymbolId"/> ���w�肵�܂��B</param>
		/// <returns>2 �� <see cref="SymbolId"/> ���������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		[StateIndependent]
		public bool Equals(SymbolId other)
		{
			if (Id == other.Id)
				return true;
			else if (IsCaseInsensitive || other.IsCaseInsensitive)
				return (Id & ~SymbolTable.CaseVersionMask) == (other.Id & ~SymbolTable.CaseVersionMask);
			return false;
		}

		/// <summary>���� <see cref="SymbolId"/> �Ǝw�肳�ꂽ <see cref="SymbolId"/> ���\����������r���܂��B</summary>
		/// <param name="other">��r���� <see cref="SymbolId"/> ���w�肵�܂��B</param>
		/// <returns>2 �� <see cref="SymbolId"/> �̃\�[�g�ɂ�����O��֌W��\�����l���w�肵�܂��B</returns>
		public int CompareTo(SymbolId other)
		{
			// Note that we could just compare _id which will result in a faster comparison. However, that will
			// mean that sorting will depend on the order in which the symbols were interned. This will often
			// not be expected. Hence, we just compare the symbol strings
			return string.Compare(SymbolTable.IdToString(this), SymbolTable.IdToString(other), IsCaseInsensitive || other.IsCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
		}

		/// <summary>���̃I�u�W�F�N�g�Ǝw�肳�ꂽ�I�u�W�F�N�g���r���܂��B</summary>
		/// <param name="obj">��r����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns>���̃I�u�W�F�N�g�̎w�肳�ꂽ�I�u�W�F�N�g�ɑ΂���O��֌W��\�����l���w�肵�܂��B</returns>
		public int CompareTo(object obj)
		{
			if (!(obj is SymbolId))
				return -1;
			return CompareTo((SymbolId)obj);
		}

		// Security, SerializationInfo, StreamingContext
		// When leaving a context we serialize out our ID as a name rather than a raw ID.
		// When we enter a new context we consult it's FieldTable to get the ID of the symbol name in the new context.

		/// <summary><see cref="System.Runtime.Serialization.SerializationInfo"/> �ɁA�I�u�W�F�N�g���V���A�������邽�߂ɕK�v�ȃf�[�^��ݒ肵�܂��B</summary>
		/// <param name="info">�f�[�^��ǂݍ��ސ�� <see cref="System.Runtime.Serialization.SerializationInfo"/>�B</param>
		/// <param name="context">���̃V���A�����̃V���A������ (<see cref="System.Runtime.Serialization.StreamingContext"/> ���Q��)�B</param>
		/// <exception cref="System.Security.SecurityException">�Ăяo�����ɁA�K�v�ȃA�N�Z�X��������܂���B</exception>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			ContractUtils.RequiresNotNull(info, "info");
			info.AddValue("symbolName", SymbolTable.IdToString(this));
		}

		/// <summary>���̃I�u�W�F�N�g�Ǝw�肳�ꂽ�I�u�W�F�N�g�����������ǂ�����Ԃ��܂��B</summary>
		/// <param name="obj">���������r����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns>2 �̃I�u�W�F�N�g���������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		[Confined]
		public override bool Equals(object obj) { return obj is SymbolId && Equals((SymbolId)obj); }

		/// <summary>���̃I�u�W�F�N�g�̃n�b�V���l���v�Z���܂��B</summary>
		/// <returns>���̃I�u�W�F�N�g�̃n�b�V���l�B</returns>
		[Confined]
		public override int GetHashCode() { return Id & ~SymbolTable.CaseVersionMask; }

		/// <summary>���̃I�u�W�F�N�g�̕�����\�����擾���܂��B���̃��\�b�h���V���{�����\����������擾���邽�߂Ɏg�p���Ȃ��ł��������B</summary>
		/// <returns>�I�u�W�F�N�g�̕�����\���B</returns>
		[Confined]
		public override string ToString() { return SymbolTable.IdToString(this); }

		/// <summary>�w�肳�ꂽ������ɑ΂��� <see cref="SymbolId"/> ���擾���܂��B</summary>
		/// <param name="s"><see cref="SymbolId"/> �ɑ΂��镶������w�肵�܂��B</param>
		/// <returns>������ɑ΂��� <see cref="SymbolId"/>�B</returns>
		public static explicit operator SymbolId(string s) { return SymbolTable.StringToId(s); }

		/// <summary>�w�肳�ꂽ <see cref="SymbolId"/> �����������ǂ������r���܂��B</summary>
		/// <param name="a">��r���� 1 �ڂ� <see cref="SymbolId"/> ���w�肵�܂��B</param>
		/// <param name="b">��r���� 2 �ڂ� <see cref="SymbolId"/> ���w�肵�܂��B</param>
		/// <returns>2 �� <see cref="SymbolId"/> ���������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool operator ==(SymbolId a, SymbolId b) { return a.Equals(b); }

		/// <summary>�w�肳�ꂽ <see cref="SymbolId"/> ���������Ȃ����ǂ������r���܂��B</summary>
		/// <param name="a">��r���� 1 �ڂ� <see cref="SymbolId"/> ���w�肵�܂��B</param>
		/// <param name="b">��r���� 2 �ڂ� <see cref="SymbolId"/> ���w�肵�܂��B</param>
		/// <returns>2 �� <see cref="SymbolId"/> ���������Ȃ��ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool operator !=(SymbolId a, SymbolId b) { return !a.Equals(b); }

		/// <summary>�w�肳�ꂽ 1 �ڂ� <see cref="SymbolId"/> �� 2 �ڂ� <see cref="SymbolId"/> �ȉ��ł��邩�ǂ������r���܂��B</summary>
		/// <param name="a">��r���� 1 �ڂ� <see cref="SymbolId"/> ���w�肵�܂��B</param>
		/// <param name="b">��r���� 2 �ڂ� <see cref="SymbolId"/> ���w�肵�܂��B</param>
		/// <returns>1 �ڂ� <see cref="SymbolId"/> �� 2 �ڂ� <see cref="SymbolId"/> �ȉ��̏ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool operator <=(SymbolId a, SymbolId b) { return a.CompareTo(b) <= 0; }

		/// <summary>�w�肳�ꂽ 1 �ڂ� <see cref="SymbolId"/> �� 2 �ڂ� <see cref="SymbolId"/> �������������ǂ������r���܂��B</summary>
		/// <param name="a">��r���� 1 �ڂ� <see cref="SymbolId"/> ���w�肵�܂��B</param>
		/// <param name="b">��r���� 2 �ڂ� <see cref="SymbolId"/> ���w�肵�܂��B</param>
		/// <returns>1 �ڂ� <see cref="SymbolId"/> �� 2 �ڂ� <see cref="SymbolId"/> �����������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool operator <(SymbolId a, SymbolId b) { return a.CompareTo(b) < 0; }

		/// <summary>�w�肳�ꂽ 1 �ڂ� <see cref="SymbolId"/> �� 2 �ڂ� <see cref="SymbolId"/> �ȏ�ł��邩�ǂ������r���܂��B</summary>
		/// <param name="a">��r���� 1 �ڂ� <see cref="SymbolId"/> ���w�肵�܂��B</param>
		/// <param name="b">��r���� 2 �ڂ� <see cref="SymbolId"/> ���w�肵�܂��B</param>
		/// <returns>1 �ڂ� <see cref="SymbolId"/> �� 2 �ڂ� <see cref="SymbolId"/> �ȏ�̏ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool operator >=(SymbolId a, SymbolId b) { return a.CompareTo(b) >= 0; }

		/// <summary>�w�肳�ꂽ 1 �ڂ� <see cref="SymbolId"/> �� 2 �ڂ� <see cref="SymbolId"/> �����傫�����ǂ������r���܂��B</summary>
		/// <param name="a">��r���� 1 �ڂ� <see cref="SymbolId"/> ���w�肵�܂��B</param>
		/// <param name="b">��r���� 2 �ڂ� <see cref="SymbolId"/> ���w�肵�܂��B</param>
		/// <returns>1 �ڂ� <see cref="SymbolId"/> �� 2 �ڂ� <see cref="SymbolId"/> �����傫���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool operator >(SymbolId a, SymbolId b) { return a.CompareTo(b) > 0; }
	}
}
