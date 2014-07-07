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
	sealed class BoolArgBuilder : SimpleArgBuilder
	{
		internal BoolArgBuilder(Type parameterType) : base(parameterType) { Debug.Assert(parameterType == typeof(bool)); }

		// parameter ? -1 : 0
		internal override Expression MarshalToRef(Expression parameter) { return Expression.Condition(Marshal(parameter), Expression.Constant((short)(-1)), Expression.Constant((short)0)); }

		// parameter = temp != 0
		internal override Expression UnmarshalFromRef(Expression value) { return base.UnmarshalFromRef(Expression.NotEqual(value, Expression.Constant((short)0))); }
	}
}