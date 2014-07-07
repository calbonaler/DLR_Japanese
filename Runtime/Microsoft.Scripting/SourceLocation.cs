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
using System.Globalization;

namespace Microsoft.Scripting
{
	/// <summary>�\�[�X�R�[�h���̈ʒu��\���܂��B</summary>
	[Serializable]
	public struct SourceLocation : IEquatable<SourceLocation>, IComparable<SourceLocation>, IComparable
	{
		// TODO: remove index
		/// <summary><see cref="Microsoft.Scripting.SourceLocation"/> �\���̂̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="index">�\�[�X�R�[�h���ł� 0 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		/// <param name="line">�\�[�X�R�[�h���ł� 1 ����n�܂�s�ԍ����w�肵�܂��B</param>
		/// <param name="column">�\�[�X�R�[�h���ł� 1 ����n�܂錅�ԍ����w�肵�܂��B</param>
		public SourceLocation(int index, int line, int column)�@: this()
		{
			ValidateLocation(index, line, column);
			Index = index;
			Line = line;
			Column = column;
		}

		static void ValidateLocation(int index, int line, int column)
		{
			if (index < 0)
				throw ErrorOutOfRange("index", 0);
			if (line < 1)
				throw ErrorOutOfRange("line", 1);
			if (column < 1)
				throw ErrorOutOfRange("column", 1);
		}

		static Exception ErrorOutOfRange(string paramName, int minValue) { return new ArgumentOutOfRangeException(paramName, string.Format("{0} must be greater than or equal to {1}", paramName, minValue)); }

		SourceLocation(int index, int line, int column, bool noChecks)�@: this()
		{
			if (!noChecks)
				ValidateLocation(index, line, column);
			Index = index;
			Line = line;
			Column = column;
		}

		/// <summary>�ǂ̏ꏊ�������Ă��Ȃ��L���Ȉʒu��\���܂��B</summary>
		public static readonly SourceLocation None = new SourceLocation(0, 0xfeefee, 0, true);

		/// <summary>�����Ȉʒu��\���܂��B</summary>
		public static readonly SourceLocation Invalid = new SourceLocation(0, 0, 0, true);

		/// <summary>�L���ȍŏ��̈ʒu��\���܂��B</summary>
		public static readonly SourceLocation MinValue = new SourceLocation(0, 1, 1);

		/// <summary>�\�[�X�R�[�h���ł� 0 ����n�܂�C���f�b�N�X���擾���܂��B</summary>
		public int Index { get; private set; }

		/// <summary>�\�[�X�R�[�h���ł� 1 ����n�܂�s�ԍ����擾���܂��B</summary>
		public int Line { get; private set; }

		/// <summary>�\�[�X�R�[�h���ł� 1 ����n�܂錅�ԍ����擾���܂��B</summary>
		public int Column { get; private set; }

		/// <summary>���̈ʒu���L�����ǂ����������l���擾���܂��B</summary>
		public bool IsValid { get { return Line > 0 && Column > 0; } }

		/// <summary>�w��̃I�u�W�F�N�g�����݂̃I�u�W�F�N�g�Ɠ��������ǂ����𔻒f���܂��B</summary>
		/// <param name="obj">���݂̃I�u�W�F�N�g�Ɣ�r����I�u�W�F�N�g�B</param>
		/// <returns>�w�肵���I�u�W�F�N�g�����݂̃I�u�W�F�N�g�Ɠ������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public override bool Equals(object obj) { return obj is SourceLocation && Equals((SourceLocation)obj); }

		/// <summary>����̌^�̃n�b�V���֐��Ƃ��ċ@�\���܂��B</summary>
		/// <returns>���݂̃I�u�W�F�N�g�̃n�b�V�� �R�[�h�B</returns>
		public override int GetHashCode() { return (Line << 16) ^ Column; }

		/// <summary>���݂̃I�u�W�F�N�g��\���������Ԃ��܂��B</summary>
		/// <returns>���݂̃I�u�W�F�N�g��\��������B</returns>
		public override string ToString() { return "(" + Line + "," + Column + ")"; }

		/// <summary>���݂̃I�u�W�F�N�g�̃f�o�b�O�p�̕������Ԃ��܂��B</summary>
		/// <returns>���݂̃I�u�W�F�N�g�̃f�o�b�O�p�̕�����B</returns>
		internal string ToDebugString() { return string.Format(CultureInfo.CurrentCulture, "({0},{1},{2})", Index, Line, Column); }

		/// <summary>���̈ʒu���w�肳�ꂽ�ʒu�Ɠ��������ǂ����𔻒f���܂��B</summary>
		/// <param name="other">��r����ʒu���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�ʒu�����݂̈ʒu�Ɠ������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public bool Equals(SourceLocation other) { return CompareTo(other) == 0; }

		/// <summary>���̈ʒu���w�肳�ꂽ�ʒu�Ɣ�r���܂��B</summary>
		/// <param name="other">��r����ʒu���w�肵�܂��B</param>
		/// <returns>���̈ʒu���w�肳�ꂽ�ʒu������ɂ���ꍇ�� 0 ���傫���l�B�������ꍇ�� 0�B��ɂ���ꍇ�� 0 ��菬�����l�B</returns>
		public int CompareTo(SourceLocation other) { return Index.CompareTo(other.Index); }

		/// <summary>���̃I�u�W�F�N�g���w�肳�ꂽ�I�u�W�F�N�g�Ɣ�r���܂��B</summary>
		/// <param name="obj">��r����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns>���̃I�u�W�F�N�g���w�肳�ꂽ�I�u�W�F�N�g������ɂ���ꍇ�� 0 ���傫���l�B�������ꍇ�� 0�B��ɂ���ꍇ�� 0 ��菬�����l�B</returns>
		public int CompareTo(object obj)
		{
			if (ReferenceEquals(obj, null))
				return 1;
			if (obj is SourceLocation)
				return CompareTo((SourceLocation)obj);
			throw new ArgumentException();
		}

		/// <summary>�w�肳�ꂽ 2 �̈ʒu���r���ē��������ǂ����𔻒f���܂��B</summary>
		/// <param name="left">��r���� 1 �ڂ̈ʒu�B</param>
		/// <param name="right">��r���� 2 �ڂ̈ʒu�B</param>
		/// <returns>2 �̈ʒu���������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool operator ==(SourceLocation left, SourceLocation right) { return left.CompareTo(right) == 0; ; }

		/// <summary>�w�肳�ꂽ 2 �̈ʒu���r���ē������Ȃ����ǂ����𔻒f���܂��B</summary>
		/// <param name="left">��r���� 1 �ڂ̈ʒu�B</param>
		/// <param name="right">��r���� 2 �ڂ̈ʒu�B</param>
		/// <returns>2 �̈ʒu���������Ȃ��ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool operator !=(SourceLocation left, SourceLocation right) { return left.CompareTo(right) != 0; }

		/// <summary>�w�肳�ꂽ 2 �̈ʒu���r���� 1 �ڂ̈ʒu�� 2 �ڂ̈ʒu������ɂ��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="left">��r���� 1 �ڂ̈ʒu�B</param>
		/// <param name="right">��r���� 2 �ڂ̈ʒu�B</param>
		/// <returns>1 �ڂ̈ʒu�� 2 �ڂ̈ʒu������ɂ���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool operator <(SourceLocation left, SourceLocation right) { return left.CompareTo(right) < 0; }

		/// <summary>�w�肳�ꂽ 2 �̈ʒu���r���� 1 �ڂ̈ʒu�� 2 �ڂ̈ʒu������ɂ��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="left">��r���� 1 �ڂ̈ʒu�B</param>
		/// <param name="right">��r���� 2 �ڂ̈ʒu�B</param>
		/// <returns>1 �ڂ̈ʒu�� 2 �ڂ̈ʒu������ɂ���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool operator >(SourceLocation left, SourceLocation right) { return left.CompareTo(right) > 0; }

		/// <summary>�w�肳�ꂽ 2 �̈ʒu���r���� 1 �ڂ̈ʒu�� 2 �ڂ̈ʒu�Ɠ��������A�܂��́A��ɂ��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="left">��r���� 1 �ڂ̈ʒu�B</param>
		/// <param name="right">��r���� 2 �ڂ̈ʒu�B</param>
		/// <returns>1 �ڂ̈ʒu�� 2 �ڂ̈ʒu��������������ɂ���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool operator <=(SourceLocation left, SourceLocation right) { return left.CompareTo(right) <= 0; }

		/// <summary>�w�肳�ꂽ 2 �̈ʒu���r���� 1 �ڂ̈ʒu�� 2 �ڂ̈ʒu�Ɠ��������A�܂��́A��ɂ��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="left">��r���� 1 �ڂ̈ʒu�B</param>
		/// <param name="right">��r���� 2 �ڂ̈ʒu�B</param>
		/// <returns>1 �ڂ̈ʒu�� 2 �ڂ̈ʒu��������������ɂ���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool operator >=(SourceLocation left, SourceLocation right) { return left.CompareTo(right) >= 0; }
	}
}
