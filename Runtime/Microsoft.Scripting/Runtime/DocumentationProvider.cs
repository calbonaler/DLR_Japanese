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

using System.Collections.Generic;
using Microsoft.Scripting.Hosting;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>�������Ԓ��̃I�u�W�F�N�g�ɑ΂��錾��ŗL�̃h�L�������g��񋟂��܂��B</summary>
	public abstract class DocumentationProvider
	{
		/// <summary>�h���N���X�ŃI�[�o�[���C�h�����ƁA�w�肳�ꂽ�I�u�W�F�N�g�ɑ΂��郁���o�̃��X�g���擾���܂��B</summary>
		/// <param name="value">�����o�̃��X�g���擾����I�u�W�F�N�g���w�肵�܂��B</param>
		public abstract ICollection<MemberDoc> GetMembers(object value);

		/// <summary>�h���N���X�ŃI�[�o�[���C�h�����ƁA�w�肳�ꂽ�I�u�W�F�N�g�ɑ΂���I�[�o�[���[�h�̃��X�g���擾���܂��B</summary>
		/// <param name="value">�I�[�o�[���[�h�̃��X�g���擾����I�u�W�F�N�g���w�肵�܂��B</param>
		public abstract ICollection<OverloadDoc> GetOverloads(object value);
	}
}
