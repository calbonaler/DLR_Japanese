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
	/// <summary>���\�b�h�o�C���_�[���s���o�C���f�B���O�̎�ނ��w�肵�܂��B</summary>
	public enum BinderType
	{
		/// <summary>���\�b�h�o�C���_�[�͒ʏ�̃o�C���f�B���O���s���܂��B</summary>
		Normal,
		/// <summary>���\�b�h�o�C���_�[�͓񍀉��Z�̃o�C���f�B���O���s���܂��B</summary>
		BinaryOperator,
		/// <summary>���\�b�h�o�C���_�[�͔�r���Z�̃o�C���f�B���O���s���܂��B</summary>
		ComparisonOperator,
		/// <summary>���\�b�h�o�C���_�[�͕Ԃ����C���X�^���X�Ŏg�p����Ȃ��L�[���[�h�����ɑ΂���v���p�e�B�܂��̓t�B�[���h��ݒ肵�܂��B</summary>
		Constructor
	}
}
