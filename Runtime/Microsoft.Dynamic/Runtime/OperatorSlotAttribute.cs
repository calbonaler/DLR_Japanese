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

namespace Microsoft.Scripting.Runtime
{
	/// <summary>
	/// ���\�b�h�����Z�q�Ƃ��Č^�ɒǉ������g�����\�b�h�ł���Ƃ��ă}�[�N���܂��B
	/// ���O�� op_* ���\�b�h�Ɋ֘A�t����ꂽ�I�[�o�[���[�h��ǉ�����Ƃ��� CLS ���O�t���K��Ɉ�v���� "Add" �̂悤�Ȑ��`���̖��O�ł���K�v������܂��B
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public sealed class OperatorSlotAttribute : Attribute
	{
		/// <summary><see cref="Microsoft.Scripting.Runtime.OperatorSlotAttribute"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		public OperatorSlotAttribute() { }
	}
}
