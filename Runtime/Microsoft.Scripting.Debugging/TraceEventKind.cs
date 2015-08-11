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

namespace Microsoft.Scripting.Debugging
{
	public enum TraceEventKind
	{
		/// <summary>
		/// ���s���V�����t���[���ɓ���Ƃ��ɔ������܂��B
		/// Payload: �Ȃ�
		/// </summary>
		FrameEnter,

		/// <summary>
		/// ���s���t���[������o��Ƃ��ɔ������܂��B
		/// Payload: �֐�����̖߂�l
		/// </summary>
		FrameExit,

		/// <summary>
		/// ���s���f�o�b�O �X���b�h����o��Ƃ��ɔ������܂��B
		/// Payload: �Ȃ�
		/// </summary>
		ThreadExit,

		/// <summary>
		/// ���s���g���[�X �|�C���g�ɓ��B�����Ƃ��ɔ������܂��B
		/// Payload: �Ȃ�
		/// </summary>
		TracePoint,

		/// <summary>
		/// ���s���ɗ�O�����������Ƃ��ɔ������܂��B
		/// Payload: �X���[���ꂽ��O�I�u�W�F�N�g
		/// </summary>
		Exception,

		/// <summary>
		/// ��O���X���[���ꌻ�݂̃��\�b�h�ɂ���ăn���h������Ȃ������Ƃ��ɔ������܂��B
		/// Payload: �X���[���ꂽ��O�I�u�W�F�N�g
		/// </summary>
		ExceptionUnwind,
	}
}
