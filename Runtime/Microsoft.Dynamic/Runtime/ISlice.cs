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

namespace Microsoft.Scripting.Runtime
{
	/// <summary>Python �� Slice �I�u�W�F�N�g�ɉe�����ꂽ���l�z��̃X���C�X���擾����ꍇ�ɗL���ȃC���^�[�t�F�C�X�ł��B</summary>
	public interface ISlice
	{
		/// <summary>�X���C�X�̊J�n�C���f�b�N�X���擾���܂��B�J�n�C���f�b�N�X����`����Ă��Ȃ��ꍇ�� <c>null</c> ��Ԃ��܂��B</summary>
		object StartIndex { get; }

		/// <summary>�X���C�X�̏I���C���f�b�N�X���擾���܂��B�I���C���f�b�N�X����`����Ă��Ȃ��ꍇ�� <c>null</c> ��Ԃ��܂��B</summary>
		object StopIndex { get; }

		/// <summary>�擾����X�e�b�v�̒������擾���܂��B</summary>
		object StepCount { get; }
	}
}
