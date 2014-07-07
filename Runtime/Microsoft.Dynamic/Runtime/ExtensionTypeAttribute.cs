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
	/// <summary>�A�Z���u�����̃N���X�𑼂̌^�ɑ΂���g���^�Ƃ��ă}�[�N���܂��B</summary>
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
	public sealed class ExtensionTypeAttribute : Attribute
	{
		/// <summary>�g������^�Ɗg�������^���g�p���āA<see cref="Microsoft.Scripting.Runtime.ExtensionTypeAttribute"/> �N���X�̐V�����C���X�^���X�����������܂����B</summary>
		/// <param name="extends">�g�������^���w�肵�܂��B</param>
		/// <param name="extensionType">�g�������o��񋟂���^���w�肵�܂��B</param>
		public ExtensionTypeAttribute(Type extends, Type extensionType)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");
			if (extensionType != null && !extensionType.IsPublic && !extensionType.IsNestedPublic)
				throw Error.ExtensionMustBePublic(extensionType.FullName);
			Extends = extends;
			ExtensionType = extensionType;
		}

		/// <summary>�g�������^�ɒǉ�����g�������o���܂�ł���^���擾���܂��B</summary>
		public Type ExtensionType { get; private set; }

		/// <summary><see cref="ExtensionType"/> �ɂ���Ċg�������^���擾���܂��B</summary>
		public Type Extends { get; private set; }
	}
}
