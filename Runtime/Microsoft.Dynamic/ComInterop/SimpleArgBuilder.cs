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
using System.Diagnostics;
using System.Linq.Expressions;

namespace Microsoft.Scripting.ComInterop
{
	/// <summary>
	/// ユーザーにより引数の値として生成された値を生成します。
	/// このクラスはさらに元の引数に関する情報を追跡し、配列引数や辞書引数のある関数に対する拡張メソッドの作成に使用できます。
	/// </summary>
	class SimpleArgBuilder : ArgBuilder
	{
		internal SimpleArgBuilder(Type parameterType) { ParameterType = parameterType; }

		internal Type ParameterType { get; private set; }

		internal override Expression Marshal(Expression parameter)
		{
			Debug.Assert(parameter != null);
			return Ast.Utils.Convert(parameter, ParameterType);
		}

		internal override Expression UnmarshalFromRef(Expression newValue)
		{
			Debug.Assert(newValue != null && newValue.Type.IsAssignableFrom(ParameterType));
			return base.UnmarshalFromRef(newValue);
		}
	}
}