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
	/// <summary>�\�[�X�R�[�h���͈̔͂�\���܂��B</summary>
	[Serializable]
	public struct SourceSpan : IEquatable<SourceSpan>
	{
		/// <summary>�J�n�ʒu�ƏI���ʒu���g�p���āA<see cref="Microsoft.Scripting.SourceSpan"/> �\���̂̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="start">���͈̔͂̊J�n�ʒu���w�肵�܂��B</param>
		/// <param name="end">���͈̔͂̏I���ʒu���w�肵�܂��B</param>
		public SourceSpan(SourceLocation start, SourceLocation end) : this()
		{
			ValidateLocations(start, end);
			Start = start;
			End = end;
		}

		static void ValidateLocations(SourceLocation start, SourceLocation end)
		{
			if (start.IsValid && end.IsValid)
			{
				if (start > end)
					throw new ArgumentException("Start and End must be well ordered");
			}
			else if (start.IsValid || end.IsValid)
				throw new ArgumentException("Start and End must both be valid or both invalid");
		}

		/// <summary>�ǂ̈ʒu�������Ȃ��L���Ȕ͈͂�\���܂��B</summary>
		public static readonly SourceSpan None = new SourceSpan(SourceLocation.None, SourceLocation.None);

		/// <summary>�����Ȕ͈͂�\���܂��B</summary>
		public static readonly SourceSpan Invalid = new SourceSpan(SourceLocation.Invalid, SourceLocation.Invalid);

		/// <summary>���͈̔͂̊J�n�ʒu���擾���܂��B</summary>
		public SourceLocation Start { get; private set; }

		/// <summary>���͈̔͂̏I���ʒu���擾���܂��B�͈͂̌�̍ŏ��̕����ʒu��\���܂��B</summary>
		public SourceLocation End { get; private set; }

		/// <summary>���͈̔͂̒��� (�͈͂Ɋ܂܂�Ă��镶����) ���擾���܂��B</summary>
		public int Length { get { return End.Index - Start.Index; } }

		/// <summary>���͈̔͂Ɋ܂܂�Ă���ʒu���L�����ǂ����������l���擾���܂��B</summary>
		public bool IsValid { get { return Start.IsValid && End.IsValid; } }

		/// <summary>�w��̃I�u�W�F�N�g�����݂̃I�u�W�F�N�g�Ɠ��������ǂ����𔻒f���܂��B</summary>
		/// <param name="obj">���݂̃I�u�W�F�N�g�Ɣ�r����I�u�W�F�N�g�B</param>
		/// <returns>�w�肵���I�u�W�F�N�g�����݂̃I�u�W�F�N�g�Ɠ������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public override bool Equals(object obj) { return obj is SourceSpan && Equals((SourceSpan)obj); }

		/// <summary>���݂̃I�u�W�F�N�g��\���������Ԃ��܂��B</summary>
		/// <returns>���݂̃I�u�W�F�N�g��\��������B</returns>
		public override string ToString() { return Start.ToString() + " - " + End.ToString(); }

		// ���ꂼ��̗�� 7 bit (0 - 128), ���ꂼ��̍s�� 9 bit (0 - 512)�AXOR �͑傫�ȃt�@�C�����������ɏ����ɂȂ�B
		/// <summary>����̌^�̃n�b�V���֐��Ƃ��ċ@�\���܂��B</summary>
		/// <returns>���݂̃I�u�W�F�N�g�̃n�b�V�� �R�[�h�B</returns>
		public override int GetHashCode() { return (Start.Column) ^ (End.Column << 7) ^ (Start.Line << 14) ^ (End.Line << 23); }

		/// <summary>���݂̃I�u�W�F�N�g�̃f�o�b�O�p�̕������Ԃ��܂��B</summary>
		/// <returns>���݂̃I�u�W�F�N�g�̃f�o�b�O�p�̕�����B</returns>
		internal string ToDebugString() { return string.Format(CultureInfo.CurrentCulture, "{0}-{1}", Start.ToDebugString(), End.ToDebugString()); }

		/// <summary>���͈̔͂��w�肳�ꂽ�͈͂Ɠ��������ǂ����𔻒f���܂��B</summary>
		/// <param name="other">��r����͈͂��w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�͈͂����݂͈̔͂Ɠ������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public bool Equals(SourceSpan other) { return Start == other.Start && End == other.End; }

		/// <summary>�w�肳�ꂽ 2 �͈̔͂��r���ē��������ǂ����𔻒f���܂��B</summary>
		/// <param name="left">��r���� 1 �ڂ͈̔́B</param>
		/// <param name="right">��r���� 2 �ڂ͈̔́B</param>
		/// <returns>2 �͈̔͂��������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool operator ==(SourceSpan left, SourceSpan right) { return left.Equals(right); }

		/// <summary>�w�肳�ꂽ 2 �͈̔͂��r���ē������Ȃ����ǂ����𔻒f���܂��B</summary>
		/// <param name="left">��r���� 1 �ڂ͈̔́B</param>
		/// <param name="right">��r���� 2 �ڂ͈̔́B</param>
		/// <returns>2 �͈̔͂��������Ȃ��ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool operator !=(SourceSpan left, SourceSpan right) { return !left.Equals(right); }
	}
}
