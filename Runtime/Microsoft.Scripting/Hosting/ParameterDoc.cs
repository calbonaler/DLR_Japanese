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
	/// <summary>�P��̈����Ɋւ���h�L�������g��񋟂��܂��B</summary>
	[Serializable]
	public class ParameterDoc
	{
		/// <summary>���O���g�p���āA<see cref="Microsoft.Scripting.Hosting.ParameterDoc"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="name">�����̖��O���w�肵�܂��B</param>
		public ParameterDoc(string name) : this(name, null, null, ParameterFlags.None) { }

		/// <summary>���O����ш����Ɋւ���ǉ������g�p���āA<see cref="Microsoft.Scripting.Hosting.ParameterDoc"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="name">�����̖��O���w�肵�܂��B</param>
		/// <param name="paramFlags">�����Ɋւ���ǉ�����\�� <see cref="Microsoft.Scripting.Hosting.ParameterFlags"/> ���w�肵�܂��B</param>
		public ParameterDoc(string name, ParameterFlags paramFlags) : this(name, null, null, paramFlags) { }

		/// <summary>���O����ш����̌^�����g�p���āA<see cref="Microsoft.Scripting.Hosting.ParameterDoc"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="name">�����̖��O���w�肵�܂��B</param>
		/// <param name="typeName">�����̌^�����w�肵�܂��Bnull ���w�肷�邱�Ƃ��ł��܂��B</param>
		public ParameterDoc(string name, string typeName) : this(name, typeName, null, ParameterFlags.None) { }

		/// <summary>���O�A�����̌^������уh�L�������g���g�p���āA<see cref="Microsoft.Scripting.Hosting.ParameterDoc"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="name">�����̖��O���w�肵�܂��B</param>
		/// <param name="typeName">�����̌^�����w�肵�܂��Bnull ���w�肷�邱�Ƃ��ł��܂��B</param>
		/// <param name="documentation">�����Ɋւ���h�L�������g���w�肵�܂��Bnull ���w�肷�邱�Ƃ��ł��܂��B</param>
		public ParameterDoc(string name, string typeName, string documentation) : this(name, typeName, documentation, ParameterFlags.None) { }

		/// <summary>���O�A�����̌^���A�h�L�������g����ђǉ������g�p���āA<see cref="Microsoft.Scripting.Hosting.ParameterDoc"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="name">�����̖��O���w�肵�܂��B</param>
		/// <param name="typeName">�����̌^�����w�肵�܂��Bnull ���w�肷�邱�Ƃ��ł��܂��B</param>
		/// <param name="documentation">�����Ɋւ���h�L�������g���w�肵�܂��Bnull ���w�肷�邱�Ƃ��ł��܂��B</param>
		/// <param name="paramFlags">�����Ɋւ���ǉ�����\�� <see cref="Microsoft.Scripting.Hosting.ParameterFlags"/> ���w�肵�܂��B</param>
		public ParameterDoc(string name, string typeName, string documentation, ParameterFlags paramFlags)
		{
			ContractUtils.RequiresNotNull(name, "name");
			Name = name;
			Flags = paramFlags;
			TypeName = typeName;
			Documentation = documentation;
		}

		/// <summary>�����̖��O���擾���܂��B</summary>
		public string Name { get; private set; }

		/// <summary>�^��񂪗��p�\�Ȃ�΁A�����̌^�����擾���܂��B</summary>
		public string TypeName { get; private set; }

		/// <summary>�����Ɋւ���ǉ������擾���܂��B</summary>
		public ParameterFlags Flags { get; private set; }

		/// <summary>���̈����ɑ΂���h�L�������g���擾���܂��B</summary>
		public string Documentation { get; private set; }
	}
}
