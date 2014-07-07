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

namespace Microsoft.Scripting
{
	/// <summary>�g�[�N�i�C�U����쓮�����g���K���w�肵�܂��B</summary>
	[Flags]
	public enum TokenTriggers
	{
		/// <summary>�ǂ̃g���K���ݒ肳��Ă��܂���B����͊���l�ł��B</summary>
		None = 0,
		/// <summary>�����o�I���̊J�n��������������͂���܂����B</summary>
		MemberSelect = 1,
		/// <summary>����ɂ����ăy�A�ƂȂ�v�f�̊J�n�܂��͏I����������͂���܂����B</summary>
		MatchBraces = 2,
		/// <summary>�������X�g�̊J�n��������������͂���܂����B</summary>
		ParameterStart = 16,
		/// <summary>�������X�g���ň�������؂镶������͂���܂����B</summary>
		ParameterNext = 32,
		/// <summary>�������X�g�̏I����������������͂���܂����B</summary>
		ParameterEnd = 64,
		/// <summary>���\�b�h�̈������X�g���̈�������͂���܂����B</summary>
		Parameter = 128,
		/// <summary>�C���e���Z���X�̃��\�b�h��񑀍�̐���Ɏg�p�����t���O�ɑ΂���}�X�N�ł��B</summary>
		MethodTip = 240,
	}
}
