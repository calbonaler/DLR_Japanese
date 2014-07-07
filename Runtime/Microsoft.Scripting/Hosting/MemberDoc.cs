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
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting
{
	/// <summary>�������Ԓ��̃����o�Ɋւ���h�L�������g��񋟂��܂��B</summary>
	[Serializable]
	public class MemberDoc
	{
		/// <summary>�����o�̖��O����ю�ނ��g�p���āA<see cref="Microsoft.Scripting.Hosting.MemberDoc"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="name">�������郁���o�̖��O���w�肵�܂��B</param>
		/// <param name="kind">�������郁���o�̎�ނ��w�肵�܂��B</param>
		public MemberDoc(string name, MemberKind kind)
		{
			ContractUtils.RequiresNotNull(name, "name");
			ContractUtils.Requires(kind >= MemberKind.None && kind <= MemberKind.Namespace, "kind");
			Name = name;
			Kind = kind;
		}

		/// <summary>�����o�̖��O���擾���܂��B</summary>
		public string Name { get; private set; }

		/// <summary>���łɔ������Ă���ꍇ�Ƀ����o�̎�ނ��擾���܂��B</summary>
		public MemberKind Kind { get; private set; }
	}

}
