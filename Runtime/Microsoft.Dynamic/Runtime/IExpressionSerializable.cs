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

using System.Linq.Expressions;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>
	/// �I�u�W�F�N�g�̎��c���[�ւ̃V���A�������\�ɂ��܂��B
	/// ���c���[�̓I�u�W�F�N�g�̋t�V���A�������ł���悤�ɁA�A�Z���u���ɏo�͂���܂��B
	/// </summary>
	public interface IExpressionSerializable
	{
		/// <summary>�I�u�W�F�N�g�̌��݂̏�Ԃ����c���[�ɃV���A�������܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̏�Ԃ��V���A�������ꂽ���c���[�B</returns>
		Expression CreateExpression();
	}
}
