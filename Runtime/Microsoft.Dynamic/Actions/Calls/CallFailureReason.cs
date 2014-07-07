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

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary><see cref="OverloadResolver"/> �ɂ��Ăяo�������s�ł��Ȃ����R��\���܂��B</summary>
	public enum CallFailureReason
	{
		/// <summary>����l�B�G���[�͂���܂���B</summary>
		None,
		/// <summary>1 �ȏ�̈����̕ϊ��Ɏ��s���܂����B</summary>
		ConversionFailure,
		/// <summary>1 �ȏ�̖��O�t�������𐳏�Ɉʒu����ς݈����ɑ���ł��܂���ł����B</summary>
		UnassignableKeyword,
		/// <summary>1 �ȏ�̖��O�t���������d�����Ă��邩�A�ʒu����ς݈����Ƌ������Ă��܂��B</summary>
		DuplicateKeyword,
		/// <summary>�^�����𐄘_�ł��܂���ł����B</summary>
		TypeInference
	}
}
