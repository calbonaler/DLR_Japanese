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
using System.Linq.Expressions;

namespace Microsoft.Scripting.Ast
{
	public static partial class Utils
	{
		static readonly DefaultExpression VoidInstance = Expression.Empty();

		/// <summary><see cref="System.Void"/> �^�̋�̎���Ԃ��܂��B</summary>
		/// <returns><see cref="System.Void"/> �^�̋�̎��B</returns>
		public static DefaultExpression Empty() { return VoidInstance; }

		/// <summary>�w�肳�ꂽ�^�̊���l��Ԃ��܂��B�^�ɂ� <see cref="System.Void"/> ���w��ł��܂��B</summary>
		/// <param name="type">����l���쐬����^���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�^�̊���l�B</returns>
		public static DefaultExpression Default(Type type) { return type == typeof(void) ? Empty() : Expression.Default(type); }
	}
}





