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
using System.Dynamic;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>
	/// <see cref="DynamicMetaObject"/> �����łɐ��񂳂ꂽ�^��\���Ă��邱�Ƃ������܂��B
	/// ���łɊ��m�̌^�ɐ��񂳂�Ă��邪�A�^��񂪃L���v�`������Ă��Ȃ��ꍇ (�^���V�[������Ă��Ȃ��Ȃ�) �ɗL���ł��B
	/// </summary>
	public interface IRestrictedMetaObject
	{
		/// <summary>�w�肳�ꂽ�^�̐��񂳂ꂽ <see cref="DynamicMetaObject"/> ��Ԃ��܂��B</summary>
		/// <param name="type">���񂷂�^���w�肵�܂��B</param>
		/// <returns>�^�ɐ��񂳂ꂽ <see cref="DynamicMetaObject"/>�B</returns>
		DynamicMetaObject Restrict(Type type);
	}
}
