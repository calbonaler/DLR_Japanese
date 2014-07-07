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
using System.Runtime.InteropServices;

namespace Microsoft.Scripting.ComInterop
{
	class ErrorArgBuilder : SimpleArgBuilder
	{
		internal ErrorArgBuilder(Type parameterType) : base(parameterType) { Debug.Assert(parameterType == typeof(ErrorWrapper)); }

		internal override Expression Marshal(Expression parameter) { return Expression.Property(Ast.Utils.Convert(base.Marshal(parameter), typeof(ErrorWrapper)), "ErrorCode"); } // parameter.ErrorCode

		internal override Expression UnmarshalFromRef(Expression value) { return base.UnmarshalFromRef(Expression.New(typeof(ErrorWrapper).GetConstructor(new[] { typeof(int) }), value)); } // new ErrorWrapper(value)
	}
}