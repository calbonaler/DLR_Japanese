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
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>����������^����ʂ̌^�ɕϊ�����ۂɔ��������G���[�Ɋւ������\���܂��B</summary>
	public sealed class ConversionResult
	{
		/// <summary>�����̒l�A�^�A�ϊ���̌^�A����ѕϊ������s�������ǂ����������l���g�p���āA<see cref="Microsoft.Scripting.Actions.Calls.ConversionResult"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="arg">�������̒l���w�肵�܂��B</param>
		/// <param name="argType">�������̌^�܂��͐����^���w�肵�܂��B</param>
		/// <param name="toType">�l�̕ϊ���̌^���w�肵�܂��B</param>
		/// <param name="failed">�ϊ������s�������ǂ����������l���w�肵�܂��B</param>
		internal ConversionResult(object arg, Type argType, Type toType, bool failed)
		{
			Arg = arg;
			ArgType = argType;
			To = toType;
			Failed = failed;
		}

		/// <summary>���p�\�ł���Ύ������̒l���擾���܂��B</summary>
		public object Arg { get; private set; }

		/// <summary>�l�����m�ł���΁A�������̌^�܂��͐����^���擾���܂��B�l�� <c>null</c> �ł���ꍇ�́A<see cref="Microsoft.Scripting.Runtime.DynamicNull"/> ��Ԃ��܂��B</summary>
		public Type ArgType { get; private set; }

		/// <summary>�l�̕ϊ���̌^���擾���܂��B</summary>
		public Type To { get; private set; }

		/// <summary>�ϊ������s�������ǂ����������l���擾���܂��B</summary>
		public bool Failed { get; private set; }

		/// <summary>���X�g�̍Ō�� <see cref="ConversionResult"/> �� <see cref="Failed"/> �v���p�e�B�Ɏw�肳�ꂽ������ݒ肵�܂��B</summary>
		/// <param name="failures">�Ō�� <see cref="ConversionResult"/> �����������郊�X�g���w�肵�܂��B</param>
		/// <param name="isFailure">�Ō�� <see cref="ConversionResult"/> �� <see cref="Failed"/> �v���p�e�B�ɐݒ肷��l���w�肵�܂��B</param>
		internal static void ReplaceLastFailure(IList<ConversionResult> failures, bool isFailure)
		{
			ConversionResult failure = failures[failures.Count - 1];
			failures.RemoveAt(failures.Count - 1);
			failures.Add(new ConversionResult(failure.Arg, failure.ArgType, failure.To, isFailure));
		}

		/// <summary>�w�肳�ꂽ�o�C���_�[���g�p���Ĉ����̌^�����擾���܂��B</summary>
		/// <param name="binder">�^�����擾���� <see cref="ActionBinder"/> ���w�肵�܂��B</param>
		/// <returns>���݂̈����̌^���B</returns>
		public string GetArgumentTypeName(ActionBinder binder)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			return Arg != null ? binder.GetObjectTypeName(Arg) : binder.GetTypeName(ArgType);
		}
	}
}
