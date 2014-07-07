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

using System.Runtime.InteropServices;

namespace Microsoft.Scripting.Utils
{
	/// <summary>�l�C�e�B�u���\�b�h��񋟂��܂��B</summary>
	static class NativeMethods
	{
		/// <summary>�w�肳�ꂽ���ϐ��Ɏw�肳�ꂽ�l��ݒ肵�܂��B���ϐ������݂��Ȃ��ꍇ�͐V�����쐬���A�l�� <c>null</c> ���w�肳�ꂽ�ꍇ�͂��̕ϐ����폜���܂��B</summary>
		/// <param name="name">���ϐ���\�����O���w�肵�܂��B</param>
		/// <param name="value">���ϐ��ɐݒ肷��l���w�肵�܂��B</param>
		/// <returns>���������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool SetEnvironmentVariable(string name, string value);
	}
}
