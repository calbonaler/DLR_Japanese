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
	/// �A�Z���u�����Ƀ��^�f�[�^�Ƃ��Ċi�[����Ă���h�L�������g��񋟂���@�\��񋟂��܂��B
	/// ���̑�����K�p����ƁAXML �h�L�������g�����p�ł��Ȃ��ꍇ�ł����s���Ƀ��[�U�[�ɑ΂��ăh�L�������g��񋟂��邱�Ƃ��\�ɂȂ�܂��B
	/// </summary>
	[AttributeUsage(AttributeTargets.All)]
	public sealed class DocumentationAttribute : Attribute
	{
		/// <summary>�񋟂���h�L�������g���w�肵�āA<see cref="Microsoft.Scripting.Runtime.DocumentationAttribute"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="documentation">���[�U�[�ɒ񋟂����h�L�������g���w�肵�܂��B</param>
		public DocumentationAttribute(string documentation) { Documentation = documentation; }

		/// <summary>���[�U�[�ɒ񋟂����h�L�������g���擾���܂��B</summary>
		public string Documentation { get; private set; }
	}
}
