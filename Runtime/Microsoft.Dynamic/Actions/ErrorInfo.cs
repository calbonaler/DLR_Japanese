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
using System.Diagnostics;
using System.Linq.Expressions;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions
{
	/// <summary>
	/// ���I���삪���s�ł��Ȃ��ꍇ�ɐ��������ׂ����ʂɊւ�������J�v�Z�������܂��B
	/// <see cref="ErrorInfo"/> �̓o�C���f�B���O�̎��s�ɉ����� <see cref="ActionBinder"/> �ɂ���Đ�������܂��B
	/// </summary>
	/// <remarks>
	/// <see cref="ErrorInfo"/> �͎��̂��� 1 ��ێ����܂�:
	/// �X���[������O���쐬���� <see cref="Expression"/>�B
	/// ���[�U�[�ɒ��ڕԂ���A�G���[�������������Ƃ������l�𐶐����� <see cref="Expression"/>�B(JavaScript �ɂ����� undefined �Ȃ�)
	/// ���[�U�[�ɒ��ڕԂ���邪�A���ۂ̓G���[��\���Ȃ��l�𐶐����� <see cref="Expression"/>�B
	/// </remarks>
	public sealed class ErrorInfo
	{
		ErrorInfo(Expression value, ErrorInfoKind kind)
		{
			Debug.Assert(value != null);
			Expression = value;
			Kind = kind;
		}

		/// <summary>�X���[������O��\���V���� <see cref="Microsoft.Scripting.Actions.ErrorInfo"/> ���쐬���܂��B</summary>
		/// <param name="exceptionValue">��O��\�� <see cref="Expression"/> ���w�肵�܂��B</param>
		/// <returns>��O��\�� <see cref="ErrorInfo"/>�B</returns>
		public static ErrorInfo FromException(Expression exceptionValue)
		{
			ContractUtils.RequiresNotNull(exceptionValue, "exceptionValue");
			ContractUtils.Requires(typeof(Exception).IsAssignableFrom(exceptionValue.Type), "exceptionValue", Strings.MustBeExceptionInstance);
			return new ErrorInfo(exceptionValue, ErrorInfoKind.Exception);
		}

		/// <summary>���[�U�[�ɕԂ����G���[��\���l��\���V���� <see cref="Microsoft.Scripting.Actions.ErrorInfo"/> ���쐬���܂��B</summary>
		/// <param name="resultValue">���[�U�[�ɕԂ����G���[��\�� <see cref="Expression"/> ���w�肵�܂��B</param>
		/// <returns>���[�U�[�ɕԂ����l��\�� <see cref="ErrorInfo"/>�B</returns>
		public static ErrorInfo FromValue(Expression resultValue)
		{
			ContractUtils.RequiresNotNull(resultValue, "resultValue");
			return new ErrorInfo(resultValue, ErrorInfoKind.Error);
		}

		/// <summary>���[�U�[�ɕԂ���邪�G���[�͕\���Ȃ��l��\���V���� <see cref="Microsoft.Scripting.Actions.ErrorInfo"/> ���쐬���܂��B</summary>
		/// <param name="resultValue">���[�U�[�ɕԂ����G���[��\���Ȃ� <see cref="Expression"/> ���w�肵�܂��B</param>
		/// <returns>���[�U�[�ɕԂ����G���[��\���Ȃ��l��\�� <see cref="ErrorInfo"/>�B</returns>
		public static ErrorInfo FromValueNoError(Expression resultValue)
		{
			ContractUtils.RequiresNotNull(resultValue, "resultValue");
			return new ErrorInfo(resultValue, ErrorInfoKind.Success);
		}

		/// <summary>���� <see cref="ErrorInfo"/> �I�u�W�F�N�g���\���l�̎�ނ��擾���܂��B</summary>
		public ErrorInfoKind Kind { get; private set; }

		/// <summary>���� <see cref="ErrorInfo"/> �I�u�W�F�N�g�̒l��\�� <see cref="Expression"/> ���擾���܂��B</summary>
		public Expression Expression { get; private set; }
	}

	/// <summary><see cref="ErrorInfo"/> ���\���l�̎�ނ�\���܂��B</summary>
	public enum ErrorInfoKind
	{
		/// <summary><see cref="ErrorInfo"/> �͗�O��\���܂��B</summary>
		Exception,
		/// <summary><see cref="ErrorInfo"/> �̓G���[��\���l��\���܂��B</summary>
		Error,
		/// <summary><see cref="ErrorInfo"/> �̓G���[�ł͂Ȃ��l��\���܂��B</summary>
		Success
	}
}
