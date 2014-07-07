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
	/// <summary>���\�b�h�ɑ΂���o�C���f�B���O�̌��ʂ������܂��B</summary>
	public enum BindingResult
	{
		/// <summary>�o�C���f�B���O�͐������܂����B���� 1 �̃��\�b�h���K�p�\�ł��������œK�ȕϊ������݂��܂����B</summary>
		Success,
		/// <summary>�����̃��\�b�h���w�肳�ꂽ�����ɑ΂��ēK�p�\�ł��������A�ǂ̃��\�b�h���œK�ł���Ɣ��f�ł��܂���ł����B</summary>
		AmbiguousMatch,
		/// <summary>�Ăяo���ɑ΂��ėv�����������̐��ɓK������I�[�o�[���[�h�͑��݂��܂���B</summary>
		IncorrectArgumentCount,
		/// <summary>
		/// �ǂ̃��\�b�h������ɌĂяo�����Ƃ��ł��܂���ł����B�ȉ��̌������l�����܂��B
		/// �������𐳏�ɕϊ��ł��܂���ł����B
		/// ���O�t���������ʒu����ς݈����ɑ���ł��܂���ł����B
		/// ���O�t��������������������܂����B(�����Ԃŋ������������Ă��邩�A���O�t���������d�����Ă��܂��B)
		/// </summary>
		CallFailure,
		/// <summary>���������\�z�ł��܂���ł����B</summary>
		InvalidArguments,
		/// <summary>�ǂ̃��\�b�h���Ăяo���\�ł͂���܂���B���Ƃ��΁A���ׂẴ��\�b�h���o�C���h����Ă��Ȃ��W�F�l���b�N�������܂�ł��܂��B</summary>
		NoCallableMethod,
	}
}
