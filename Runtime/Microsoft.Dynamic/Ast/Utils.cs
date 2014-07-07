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
using System.Linq.Expressions;

namespace Microsoft.Scripting.Ast
{
	/// <summary>�m�[�h�̃I�y�����h�ɑ΂���A�N�Z�X�����w�肵�܂��B</summary>
	[Flags]
	public enum ExpressionAccess
	{
		/// <summary>�m�[�h�̓I�y�����h�ɑ΂��ēǂݎ����������݂��ł��܂���B</summary>
		None = 0,
		/// <summary>�m�[�h�̓I�y�����h�̓ǂݎ��݂̂��s�����Ƃ��ł��܂��B</summary>
		Read = 1,
		/// <summary>�m�[�h�̓I�y�����h�̏������݂݂̂��s�����Ƃ��ł��܂��B</summary>
		Write = 2,
		/// <summary>�m�[�h�̓I�y�����h�ɑ΂��ēǂݎ��Ə������݂̗������s�����Ƃ��ł��܂��B</summary>
		ReadWrite = Read | Write,
	}

	/// <summary>�W���̎��c���[�Ɋ܂܂�Ȃ����܂��܂ȃm�[�h���쐬����t�@�N�g�� ���\�b�h�����J���܂��B</summary>
	public static partial class Utils
	{
		/// <summary>�w�肳�ꂽ <see cref="ExpressionType"/> �̃m�[�h���I�y�����h�������������邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="type">���f���� <see cref="ExpressionType"/> ���w�肵�܂��B</param>
		/// <returns><see cref="ExpressionType"/> �̃m�[�h���I�y�����h��������������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		/// <remarks>
		/// ���̃m�[�h�ɂ����Ă��ϐ��A�����o�A�z��v�f�̑������������ꍇ������܂�:
		/// MemberInit�ANewArrayInit�A�Q�Ɠn�������̂��� Call�A�Q�Ɠn�������̂��� New�A�Q�Ɠn�������̂��� Dynamic.
		/// </remarks>
		public static bool IsAssignment(this ExpressionType type) { return IsWriteOnlyAssignment(type) || IsReadWriteAssignment(type); }

		/// <summary>�w�肳�ꂽ <see cref="ExpressionType"/> �̃m�[�h���I�y�����h��ǂݎ�炸�ɏ����������邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="type">���f���� <see cref="ExpressionType"/> ���w�肵�܂��B</param>
		/// <returns><see cref="ExpressionType"/> �̃m�[�h���I�y�����h��ǂݎ�炸�ɏ�����������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool IsWriteOnlyAssignment(this ExpressionType type) { return type == ExpressionType.Assign; }

		/// <summary>�w�肳�ꂽ <see cref="ExpressionType"/> �̃m�[�h���I�y�����h��ǂݎ������ŏ����������邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="type">���f���� <see cref="ExpressionType"/> ���w�肵�܂��B</param>
		/// <returns><see cref="ExpressionType"/> �̃m�[�h���I�y�����h��ǂݎ������ŏ�����������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool IsReadWriteAssignment(this ExpressionType type)
		{
			switch (type)
			{
				// unary:
				case ExpressionType.PostDecrementAssign:
				case ExpressionType.PostIncrementAssign:
				case ExpressionType.PreDecrementAssign:
				case ExpressionType.PreIncrementAssign:
				// binary - compound:
				case ExpressionType.AddAssign:
				case ExpressionType.AddAssignChecked:
				case ExpressionType.AndAssign:
				case ExpressionType.DivideAssign:
				case ExpressionType.ExclusiveOrAssign:
				case ExpressionType.LeftShiftAssign:
				case ExpressionType.ModuloAssign:
				case ExpressionType.MultiplyAssign:
				case ExpressionType.MultiplyAssignChecked:
				case ExpressionType.OrAssign:
				case ExpressionType.PowerAssign:
				case ExpressionType.RightShiftAssign:
				case ExpressionType.SubtractAssign:
				case ExpressionType.SubtractAssignChecked:
					return true;
			}
			return false;
		}

		/// <summary>�w�肳�ꂽ <see cref="ExpressionType"/> �̃m�[�h�̃I�y�����h�ɑ΂��錠�����擾���܂��B</summary>
		/// <param name="type">�A�N�Z�X�����擾���� <see cref="ExpressionType"/> ���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ <see cref="ExpressionType"/> �̃m�[�h�̃I�y�����h�ɑ΂��錠���B</returns>
		public static ExpressionAccess GetLValueAccess(this ExpressionType type)
		{
			if (type.IsReadWriteAssignment())
				return ExpressionAccess.ReadWrite;
			if (type.IsWriteOnlyAssignment())
				return ExpressionAccess.Write;
			return ExpressionAccess.Read;
		}

		/// <summary>�w�肳�ꂽ <see cref="ExpressionType"/> �̃m�[�h�����Ӓl�Ƃ��ė��p�\���ǂ����𔻒f���܂��B</summary>
		/// <param name="type">���f���� <see cref="ExpressionType"/> ���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ <see cref="ExpressionType"/> �̃m�[�h�����Ӓl�Ƃ��ė��p�ł���� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool IsLValue(this ExpressionType type)
		{
			switch (type)
			{
				case ExpressionType.Index:
				case ExpressionType.MemberAccess:
				case ExpressionType.Parameter:
					return true;
			}
			return false;
		}
	}
}
