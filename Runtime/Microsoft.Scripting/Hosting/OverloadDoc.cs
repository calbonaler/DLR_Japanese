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
using System.Collections.Generic;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting
{
	/// <summary>�Ăяo���\�I�u�W�F�N�g�̒P��̃I�[�o�[���[�h�ɑ΂���h�L�������g��񋟂��܂��B</summary>
	[Serializable]
	public class OverloadDoc
	{
		/// <summary>���O�A�h�L�������g�A�������X�g���g�p���āA<see cref="Microsoft.Scripting.Hosting.OverloadDoc"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="name">�Ăяo���\�I�u�W�F�N�g�̖��O���w�肵�܂��B</param>
		/// <param name="documentation">�I�[�o�[���[�h�̃h�L�������g���w�肵�܂��Bnull ���w�肷�邱�Ƃ��ł��܂��B</param>
		/// <param name="parameters">�Ăяo���\�I�u�W�F�N�g�̈������w�肵�܂��B</param>
		public OverloadDoc(string name, string documentation, ICollection<ParameterDoc> parameters) : this(name, documentation, parameters, null) { }

		/// <summary>���O�A�h�L�������g�A�������X�g�A�߂�l���g�p���āA<see cref="Microsoft.Scripting.Hosting.OverloadDoc"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="name">�Ăяo���\�I�u�W�F�N�g�̖��O���w�肵�܂��B</param>
		/// <param name="documentation">�I�[�o�[���[�h�̃h�L�������g���w�肵�܂��Bnull ���w�肷�邱�Ƃ��ł��܂��B</param>
		/// <param name="parameters">�Ăяo���\�I�u�W�F�N�g�̈������w�肵�܂��B</param>
		/// <param name="returnParameter">�߂�l�Ɋւ�������i�[���� <see cref="Microsoft.Scripting.Hosting.ParameterDoc"/> �I�u�W�F�N�g���w�肵�܂��B</param>
		public OverloadDoc(string name, string documentation, ICollection<ParameterDoc> parameters, ParameterDoc returnParameter)
		{
			ContractUtils.RequiresNotNull(name, "name");
			ContractUtils.RequiresNotNullItems(parameters, "parameters");
			Name = name;
			Parameters = parameters;
			Documentation = documentation;
			ReturnParameter = returnParameter;
		}

		/// <summary>�Ăяo���\�I�u�W�F�N�g�̖��O���擾���܂��B</summary>
		public string Name { get; private set; }

		/// <summary>�I�[�o�[���[�h�̃h�L�������g���擾���܂��B</summary>
		public string Documentation { get; private set; }

		/// <summary>�Ăяo���\�I�u�W�F�N�g�̈������擾���܂��B</summary>
		public ICollection<ParameterDoc> Parameters { get; private set; }

		/// <summary>�߂�l�Ɋւ�������i�[���� <see cref="Microsoft.Scripting.Hosting.ParameterDoc"/> �I�u�W�F�N�g���擾���܂��B</summary>
		public ParameterDoc ReturnParameter { get; private set; }
	}
}
