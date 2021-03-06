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
	sealed class DateTimeArgBuilder : SimpleArgBuilder
	{
		internal DateTimeArgBuilder(Type parameterType) : base(parameterType) { Debug.Assert(parameterType == typeof(DateTime)); }

		internal override Expression MarshalToRef(Expression parameter) { return Expression.Call(Marshal(parameter), typeof(DateTime).GetMethod("ToOADate")); } // parameter.ToOADate()

		internal override Expression UnmarshalFromRef(Expression value) { return base.UnmarshalFromRef(Expression.Call(new Func<double, DateTime>(DateTime.FromOADate).Method, value)); } // DateTime.FromOADate(value)
	}
}