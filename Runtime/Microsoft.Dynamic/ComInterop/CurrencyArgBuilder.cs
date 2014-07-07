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
	sealed class CurrencyArgBuilder : SimpleArgBuilder
	{
		internal CurrencyArgBuilder(Type parameterType) : base(parameterType) { Debug.Assert(parameterType == typeof(CurrencyWrapper)); }

		internal override Expression Marshal(Expression parameter) { return Expression.Property(Ast.Utils.Convert(base.Marshal(parameter), typeof(CurrencyWrapper)), "WrappedObject"); } // parameter.WrappedObject

		// Decimal.ToOACurrency(parameter.WrappedObject)
		internal override Expression MarshalToRef(Expression parameter) { return Expression.Call(new Func<decimal, long>(decimal.ToOACurrency).Method, Marshal(parameter)); }

		internal override Expression UnmarshalFromRef(Expression value)
		{
			// Decimal.FromOACurrency(value)
			return base.UnmarshalFromRef(
				Expression.New(
					typeof(CurrencyWrapper).GetConstructor(new[] { typeof(decimal) }),
					Expression.Call(new Func<long, decimal>(decimal.FromOACurrency).Method, value)
				)
			);
		}
	}
}