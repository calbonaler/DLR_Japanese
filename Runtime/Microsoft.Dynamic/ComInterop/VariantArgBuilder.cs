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
using System.Runtime.InteropServices;

namespace Microsoft.Scripting.ComInterop
{
	class VariantArgBuilder : SimpleArgBuilder
	{
		readonly bool _isWrapper;

		internal VariantArgBuilder(Type parameterType) : base(parameterType) { _isWrapper = parameterType == typeof(VariantWrapper); }

		internal override Expression Marshal(Expression parameter)
		{
			parameter = base.Marshal(parameter);
			// parameter.WrappedObject
			return Ast.Utils.Convert(_isWrapper ? Expression.Property(Ast.Utils.Convert(parameter, typeof(VariantWrapper)), typeof(VariantWrapper).GetProperty("WrappedObject")) : parameter, typeof(object));
		}

		internal override Expression MarshalToRef(Expression parameter)
		{
			// parameter == UnsafeMethods.GetVariantForObject(parameter);
			return Expression.Call(new Func<object, Variant>(UnsafeMethods.GetVariantForObject).Method, Marshal(parameter));
		}

		internal override Expression UnmarshalFromRef(Expression value)
		{
			// value == IntPtr.Zero ? null : Marshal.GetObjectForNativeVariant(value);
			Expression unmarshal = Expression.Call(new Func<Variant, object>(UnsafeMethods.GetObjectForVariant).Method, value);
			if (_isWrapper)
				unmarshal = Expression.New(typeof(VariantWrapper).GetConstructor(new Type[] { typeof(object) }), unmarshal);
			return base.UnmarshalFromRef(unmarshal);
		}
	}
}