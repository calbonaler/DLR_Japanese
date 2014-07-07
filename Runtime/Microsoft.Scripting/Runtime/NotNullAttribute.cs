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
	/// ������ <c>null</c> �񋖗e�ł���Ƃ��ă}�[�N���܂��B
	/// ���̑����͂��悢�G���[���b�Z�[�W�̐����⃁�\�b�h�I���̂��߂Ƀ��\�b�h�o�C���f�B���O�C���t���X�g���N�`���ɂ���Ďg�p����܂��B
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
	public sealed class NotNullAttribute : Attribute { }

	/// <summary>
	/// �z��^�̈����� <c>null</c> �ł���v�f�������Ȃ����̂Ƃ��ă}�[�N���܂��B
	/// ���̑����͂��悢�G���[���b�Z�[�W�̐����⃁�\�b�h�I���̂��߂Ƀ��\�b�h�o�C���f�B���O�C���t���X�g���N�`���ɂ���Ďg�p����܂��B
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
	public sealed class NotNullItemsAttribute : Attribute { }
}