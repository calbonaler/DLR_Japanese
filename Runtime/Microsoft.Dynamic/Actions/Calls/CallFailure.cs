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

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary><see cref="OverloadResolver"/> �ɂ�����̃��\�b�h�ɑ΂���Ăяo�������s�ł��Ȃ����R��\���܂��B</summary>
	public sealed class CallFailure
	{
		/// <summary><see cref="CallFailureReason.ConversionFailure"/> �ł��� <see cref="Microsoft.Scripting.Actions.Calls.CallFailure"/> ���쐬���܂��B</summary>
		/// <param name="candidate">�Ăяo�������s�������\�b�h���w�肵�܂��B</param>
		/// <param name="results">���ꂼ��̈����ɑ΂��ĕϊ��������������ǂ�������ѕϊ������^���i�[���� <see cref="ConversionResult"/> ���w�肵�܂��B</param>
		internal CallFailure(MethodCandidate candidate, ConversionResult[] results)
		{
			Candidate = candidate;
			ConversionResults = results;
			Reason = CallFailureReason.ConversionFailure;
		}

		/// <summary><see cref="CallFailureReason.UnassignableKeyword"/> �܂��� <see cref="CallFailureReason.DuplicateKeyword"/> �ł��� <see cref="Microsoft.Scripting.Actions.Calls.CallFailure"/> ���쐬���܂��B</summary>
		/// <param name="candidate">�Ăяo�������s�������\�b�h���w�肵�܂��B</param>
		/// <param name="keywordArgs">�d�����Ă��邩����s�\�Ƃ��ꂽ���O�t���������w�肵�܂��B</param>
		/// <param name="unassignable"><see cref="Reason"/> �v���p�e�B�� <see cref="CallFailureReason.UnassignableKeyword"/> �ł��邩�ǂ����������l���w�肵�܂��B</param>
		internal CallFailure(MethodCandidate candidate, string[] keywordArgs, bool unassignable)
		{
			Reason = unassignable ? CallFailureReason.UnassignableKeyword : CallFailureReason.DuplicateKeyword;
			Candidate = candidate;
			KeywordArguments = keywordArgs;
		}

		/// <summary>���̑��̎��s��\�� <see cref="Microsoft.Scripting.Actions.Calls.CallFailure"/> ���쐬���܂��B</summary>
		/// <param name="candidate">�Ăяo�������s�������\�b�h���w�肵�܂��B</param>
		/// <param name="reason">���s�̗��R������ <see cref="CallFailureReason"/> ���w�肵�܂��B</param>
		internal CallFailure(MethodCandidate candidate, CallFailureReason reason)
		{
			Candidate = candidate;
			Reason = reason;
		}

		/// <summary>�Ăяo�������s�������\�b�h���擾���܂��B</summary>
		public MethodCandidate Candidate { get; private set; }

		/// <summary><see cref="CallFailure"/> �̑��̂ǂ̃v���p�e�B���Q�Ƃ��ׂ��������肷��Ăяo�������s�������R���擾���܂��B</summary>
		public CallFailureReason Reason { get; private set; }

		/// <summary><see cref="Reason"/> �v���p�e�B�� <see cref="CallFailureReason.ConversionFailure"/> �̏ꍇ�A���ꂼ��̈����ɑ΂��āA�ϊ��������������ǂ�������ѕϊ������^���i�[���� <see cref="ConversionResult"/> ���擾���܂��B</summary>
		public IList<ConversionResult> ConversionResults { get; private set; }

		/// <summary><see cref="Reason"/> �v���p�e�B�� <see cref="CallFailureReason.UnassignableKeyword"/> �܂��� <see cref="CallFailureReason.DuplicateKeyword"/> �̏ꍇ�A�d�����Ă��邩����s�\�Ƃ��ꂽ���O�t���������擾���܂��B</summary>
		public IList<string> KeywordArguments { get; private set; }
	}
}
