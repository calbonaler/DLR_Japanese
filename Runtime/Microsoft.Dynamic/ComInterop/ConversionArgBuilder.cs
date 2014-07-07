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
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.ComInterop
{
	class ConversionArgBuilder : ArgBuilder
	{
		SimpleArgBuilder _innerBuilder;
		Type _parameterType;

		internal ConversionArgBuilder(Type parameterType, SimpleArgBuilder innerBuilder)
		{
			_parameterType = parameterType;
			_innerBuilder = innerBuilder;
		}

		internal override Expression Marshal(Expression parameter) { return _innerBuilder.Marshal(Ast.Utils.Convert(parameter, _parameterType)); }

		internal override Expression MarshalToRef(Expression parameter) { throw Assert.Unreachable; } // InOut �̕ϊ��̓T�|�[�g���Ȃ�
	}
}