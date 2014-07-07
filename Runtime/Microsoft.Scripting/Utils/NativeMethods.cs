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
	/// <summary>ネイティブメソッドを提供します。</summary>
	static class NativeMethods
	{
		/// <summary>指定された環境変数に指定された値を設定します。環境変数が存在しない場合は新しく作成し、値に <c>null</c> が指定された場合はその変数を削除します。</summary>
		/// <param name="name">環境変数を表す名前を指定します。</param>
		/// <param name="value">環境変数に設定する値を指定します。</param>
		/// <returns>成功した場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool SetEnvironmentVariable(string name, string value);
	}
}
