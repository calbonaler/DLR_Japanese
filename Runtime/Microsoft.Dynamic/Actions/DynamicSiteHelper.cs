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
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.Scripting.Actions
{
	/// <summary>���I�T�C�g�ɑ΂���w���p�[���\�b�h��񋟂��܂��B</summary>
	public static class DynamicSiteHelpers
	{
		/// <summary>���\�b�h���X�^�b�N�t���[���ɕ\�������ׂ��ł͂Ȃ����ǂ����𔻒f���܂��B</summary>
		/// <param name="mb">���f���郁�\�b�h���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ���\�b�h���X�^�b�N�t���[���ɕ\�������ׂ��ł͂Ȃ��ꍇ <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool IsInvisibleDlrStackFrame(MethodBase mb)
		{
			// ���̃��\�b�h���̓f���Q�[�g�^�V�O�l�`���ɑ΂��č쐬���ꂽ���I���\�b�h�ɑ΂��Ďg�p�����B
			// Microsoft.Scripting ���O��Ԃ̃��\�b�h���t�B���^����B
			// DLR �K���ɑ΂��Đ�������邩�ADLR �K���Ŏg�p����邷�ׂẴ��\�b�h���t�B���^����B
			return mb.Name == "_Scripting_" ||
				mb.DeclaringType != null && mb.DeclaringType.Namespace != null && mb.DeclaringType.Namespace.StartsWith("Microsoft.Scripting", StringComparison.Ordinal) ||
				CallSiteHelpers.IsInternalFrame(mb);
		}
	}
}
