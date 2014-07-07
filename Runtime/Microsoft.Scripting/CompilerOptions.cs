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

namespace Microsoft.Scripting
{
	/// <summary>�R���p�C���I�v�V������\���܂��B</summary>
	[Serializable]
	public class CompilerOptions : ICloneable
	{
		/// <summary><see cref="Microsoft.Scripting.CompilerOptions"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		public CompilerOptions() { }

		/// <summary>���݂̃C���X�^���X�̃R�s�[�ł���V�����I�u�W�F�N�g���쐬���܂��B</summary>
		/// <returns>���̃C���X�^���X�̃R�s�[�ł���V�����I�u�W�F�N�g�B</returns>
		public virtual object Clone() { return base.MemberwiseClone(); }
	}
}
