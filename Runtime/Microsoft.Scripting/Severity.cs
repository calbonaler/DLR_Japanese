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

namespace Microsoft.Scripting
{
	/// <summary>�G���[�̐[������\���܂��B</summary>
	public enum Severity
	{
		/// <summary>�����\�ȃG���[�ł��B</summary>
		Ignore,
		/// <summary>�x���ł��B</summary>
		Warning,
		/// <summary>�ʏ�̃G���[�ł��B</summary>
		Error,
		/// <summary>�v���I�ȃG���[�ł��B</summary>
		FatalError,
	}
}
