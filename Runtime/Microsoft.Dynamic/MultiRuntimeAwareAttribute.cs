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

namespace Microsoft.Scripting
{
	/// <summary>
	/// �ÓI�t�B�[���h�𕡐��̃����^�C������A�N�Z�X�����ꍇ�ł����S�ł���Ƃ��ă}�[�N���܂��B</summary>
	/// <remarks>
	/// ���̑����Ń}�[�N����Ă��Ȃ��������݉\�ȐÓI�t�B�[���h�̓����^�C���Ԃŋ��L����Ă����Ԃ𒲂ׂ�e�X�g�ɂ���ăt���O���t�����܂��B
	/// ���̑�����K�p����O�Ƀ��[�U�[�͏�Ԃ����L���Ă����S�ł��邱�Ƃ��m���ɂ���ׂ��ł��B
	/// ����͒ʏ�x������������邩�A���ׂẴ����^�C���œ���ŕs�ςȒl���L���b�V�����Ă���ϐ��ɓK�p���܂��B
	/// </remarks>
	[Conditional("DEBUG")]
	[AttributeUsage(AttributeTargets.Field)]
	public sealed class MultiRuntimeAwareAttribute : Attribute { }
}
