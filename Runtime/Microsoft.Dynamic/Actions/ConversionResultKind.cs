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
	/// �ϊ�����̌��ʂ𔻕ʂ��܂��B
	/// ���ʂ͗�O�A����ɕϊ����ꂽ�� default(T) �ɂ���ė^����ꂽ�ꍇ�̒l�A�܂��͒l���ϊ��ł��邩�ǂ����������u�[���l�̏ꍇ������܂��B
	/// </summary>
	public enum ConversionResultKind
	{
		/// <summary>���p�\�ȈÖٓI�ϊ������݁A���p�\�ȕϊ������݂��Ȃ��ꍇ�ɂ͗�O���X���[���܂��B</summary>
		ImplicitCast,
		/// <summary>���p�\�ȈÖٓI����і����I�ϊ������݁A���p�\�ȕϊ������݂��Ȃ��ꍇ�ɂ͗�O���X���[���܂��B</summary>
		ExplicitCast,
		/// <summary>���p�\�ȈÖٓI�ϊ������݁A�ϊ������s����Ȃ��ꍇ�ɂ� <c>default(ReturnType)</c> ��Ԃ��܂��B</summary>
		ImplicitTry,
		/// <summary>���p�\�ȈÖٓI����і����I�ϊ������݁A�ϊ������s����Ȃ��ꍇ�ɂ� <c>default(ReturnType)</c> ��Ԃ��܂��B</summary>
		ExplicitTry
	}
}
