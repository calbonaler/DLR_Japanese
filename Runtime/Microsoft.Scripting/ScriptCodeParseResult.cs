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

namespace Microsoft.Scripting
{
	/// <summary>�\�[�X�R�[�h�̉�͌��ʂ�\���܂��B</summary>
	public enum ScriptCodeParseResult
	{
		/// <summary>�\�[�X�R�[�h�͕��@�I�ɐ��m�ł��B</summary>
		Complete,
		/// <summary>�\�[�X�R�[�h�͋�̃X�e�[�g�����g�܂��͎���\���Ă��܂��B</summary>
		Empty,
		/// <summary>�\�[�X�R�[�h�͊��ɖ����ł���A���@�I�ɐ������Ƃ���镔���͂���܂���B</summary>
		Invalid,
		/// <summary>�Ō�̃g�[�N�����������ł��B�������A�\�[�X�R�[�h�͐��m�Ɋ��������邱�Ƃ��ł��܂��B</summary>
		IncompleteToken,
		/// <summary>�Ō�̃X�e�[�g�����g���������ł��B�������A�\�[�X�R�[�h�͐��m�Ɋ��������邱�Ƃ��ł��܂��B</summary>
		IncompleteStatement,
	}

	// TODO: rename or remove
	public static class ScriptCodeParseResultUtils
	{
		public static bool IsCompleteOrInvalid(/*this*/ ScriptCodeParseResult props, bool allowIncompleteStatement)
		{
			return props == ScriptCodeParseResult.Invalid || props != ScriptCodeParseResult.IncompleteToken &&
				(allowIncompleteStatement || props != ScriptCodeParseResult.IncompleteStatement);
		}
	}
}
