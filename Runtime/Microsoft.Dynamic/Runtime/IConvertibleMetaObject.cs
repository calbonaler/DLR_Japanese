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
	/// <summary><see cref="System.Dynamic.DynamicMetaObject"/> �� CLR �^�ɕϊ��\�ł��邱�Ƃ������܂��B</summary>
	public interface IConvertibleMetaObject
	{
		/// <summary>���� <see cref="System.Dynamic.DynamicMetaObject"/> ���w�肳�ꂽ�^�ɕϊ��\�ł��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="type">�ϊ���̌^���w�肵�܂��B</param>
		/// <param name="isExplicit">�ϊ��������I�ɍs���邩�ǂ����������l���w�肵�܂��B</param>
		/// <returns><see cref="System.Dynamic.DynamicMetaObject"/> �� <paramref name="type"/> �Ɏw�肳�ꂽ�ϊ����@�ŕϊ��\�ȏꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		bool CanConvertTo(Type type, bool isExplicit);
	}
}
