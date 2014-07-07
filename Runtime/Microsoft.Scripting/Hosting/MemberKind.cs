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


namespace Microsoft.Scripting.Hosting
{
	/// <summary>�����o�̎�ނ�\���܂��B</summary>
	public enum MemberKind
	{
		/// <summary>�Ȃ�</summary>
		None,
		/// <summary>�N���X</summary>
		Class,
		/// <summary>�f���Q�[�g</summary>
		Delegate,
		/// <summary>�񋓑�</summary>
		Enum,
		/// <summary>�C�x���g</summary>
		Event,
		/// <summary>�t�B�[���h</summary>
		Field,
		/// <summary>�֐�</summary>
		Function,
		/// <summary>���W���[��</summary>
		Module,
		/// <summary>�v���p�e�B</summary>
		Property,
		/// <summary>�萔</summary>
		Constant,
		/// <summary>�񋓑̂̃����o</summary>
		EnumMember,
		/// <summary>�C���X�^���X</summary>
		Instance,
		/// <summary>���\�b�h</summary>
		Method,
		/// <summary>���O���</summary>
		Namespace
	}
}
