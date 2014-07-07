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

namespace Microsoft.Scripting.Runtime
{
	/// <summary>�V���{�����ǂ̂悤�ɕύX���ꂽ�����w�肵�܂��B</summary>
	public enum ModuleChangeType
	{
		/// <summary>���W���[�����ŐV�����l���ݒ肳��܂����B(���邢�͈ȑO�̒l���ύX����܂����B)</summary>
		Set,
		/// <summary>�l�����W���[������폜����܂����B</summary>
		Delete,
	}
}
