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

namespace Microsoft.Scripting.Actions
{
	/// <summary>
	/// �R�[���T�C�g�ł̌ʂ̈����ɑ΂���K���ł��B
	/// �����̃R�[���T�C�g�͒P��̐錾�ɑ΂��Ĉ�v�����邱�Ƃ��ł��܂��B
	/// �����̎�ނ̒��ɂ̓��X�g���邢�̓f�B�N�V���i���̂悤�ɕ����̈����ɓW�J�������̂�����܂��B
	/// </summary>
	public enum ArgumentType
	{
		/// <summary>�P���Ȗ��O�̂Ȃ��ʒu�����肳��Ă�������ł��B</summary>
		/// <example>Python �ł� foo(1,2,3) �͂��ׂĒP���Ȉ����ł��B</example>
		Simple,
		/// <summary>�R�[���T�C�g�Ŋ֘A�t����ꂽ���O���������ł��B</summary>
		/// <example>Python �ł� foo(a=1) ������ɂ�����܂��B</example>
		Named,
		/// <summary>�����̃��X�g���܂ވ����ł��B</summary>
		/// <example>
		/// Python �ł́Afoo(*(1,2*2,3)) �� (a,b,c)=(1,4,3) �Ƃ��� 3 �̐錾���ꂽ���������� def foo(a,b,c) �Ɉ�v���܂��B
		/// �܂��Al=(1,4,3) �Ƃ��āA1 �̐錾���ꂽ���������� def foo(*l) �ɂ���v���܂��B
		/// </example>
		List,
		/// <summary>���O�t�������̃f�B�N�V���i�����܂�ł�������ł��B</summary>
		/// <example>Python �ł́Afoo(**{'a':1, 'b':2}) ������ɂ�����܂��B</example>
		Dictionary,
		/// <summary>�C���X�^���X�����ł��B</summary>
		Instance
	};
}
