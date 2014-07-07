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

namespace Microsoft.Scripting.Runtime
{
	/// <summary>実行時にサポートされる明示的なキャストを実装します。</summary>
	public static partial class Cast
	{
		/// <summary>オブジェクトを指定された型に明示的に変換します。ユーザー定義の型変換演算子は考慮されません。</summary>
		/// <param name="o">型を変換するオブジェクトを指定します。</param>
		/// <param name="to">オブジェクトの変換先の型を指定します。</param>
		/// <returns>変換されたオブジェクト。</returns>
		/// <exception cref="InvalidCastException">指定された型への変換が失敗しました。</exception>
		public static object Explicit(object o, Type to)
		{
			if (o == null)
			{
				// Null objects can be only cast to Nullable<T> or any reference type
				if (to.IsValueType)
				{
					if (to.IsGenericType && to.GetGenericTypeDefinition() == NullableType)
						return NewNullableInstance(to.GetGenericArguments()[0]);
					else if (to == typeof(void))
						return null;
					else
						throw new InvalidCastException(string.Format("Cannot cast null to a value type {0}", to.Name));
				}
				else
					// Explicit cast to reference type is simply null
					return null;
			}
			if (to.IsValueType)
				return ExplicitCastToValueType(o, to);
			else
			{
				var type = o.GetType();
				if (to.IsInstanceOfType(o) || to.IsAssignableFrom(type))
					return o;
				else
					throw new InvalidCastException(string.Format("Cannot cast {0} to {1}", type.Name, to.Name));
			}
		}

		/// <summary>オブジェクトを指定された型に明示的に変換します。ユーザー定義の型変換演算子は考慮されません。</summary>
		/// <typeparam name="T">変換先の型を指定します。</typeparam>
		/// <param name="o">型を変換するオブジェクトを指定します。</param>
		/// <returns>変換されたオブジェクト。</returns>
		/// <exception cref="InvalidCastException">指定された型への変換が失敗しました。</exception>
		public static T Explicit<T>(object o) { return (T)Explicit(o, typeof(T)); }

		static object ExplicitCastToValueType(object o, Type to)
		{
			Debug.Assert(o != null);
			Debug.Assert(to.IsValueType);
			if (to == Int32Type) return ScriptingRuntimeHelpers.Int32ToObject(ExplicitCastToInt32(o));
			if (to == DoubleType) return ExplicitCastToDouble(o);
			if (to == BooleanType) return ScriptingRuntimeHelpers.BooleanToObject(ExplicitCastToBoolean(o));
			if (to == ByteType) return ExplicitCastToByte(o);
			if (to == CharType) return ExplicitCastToChar(o);
			if (to == DecimalType) return ExplicitCastToDecimal(o);
			if (to == Int16Type) return ExplicitCastToInt16(o);
			if (to == Int64Type) return ExplicitCastToInt64(o);
			if (to == SByteType) return ExplicitCastToSByte(o);
			if (to == SingleType) return ExplicitCastToSingle(o);
			if (to == UInt16Type) return ExplicitCastToUInt16(o);
			if (to == UInt32Type) return ExplicitCastToUInt32(o);
			if (to == UInt64Type) return ExplicitCastToUInt64(o);
			if (to == NullableBooleanType) return ExplicitCastToNullableBoolean(o);
			if (to == NullableByteType) return ExplicitCastToNullableByte(o);
			if (to == NullableCharType) return ExplicitCastToNullableChar(o);
			if (to == NullableDecimalType) return ExplicitCastToNullableDecimal(o);
			if (to == NullableDoubleType) return ExplicitCastToNullableDouble(o);
			if (to == NullableInt16Type) return ExplicitCastToNullableInt16(o);
			if (to == NullableInt32Type) return ExplicitCastToNullableInt32(o);
			if (to == NullableInt64Type) return ExplicitCastToNullableInt64(o);
			if (to == NullableSByteType) return ExplicitCastToNullableSByte(o);
			if (to == NullableSingleType) return ExplicitCastToNullableSingle(o);
			if (to == NullableUInt16Type) return ExplicitCastToNullableUInt16(o);
			if (to == NullableUInt32Type) return ExplicitCastToNullableUInt32(o);
			if (to == NullableUInt64Type) return ExplicitCastToNullableUInt64(o);
			if (to.IsAssignableFrom(o.GetType()))
				return o;
			throw new InvalidCastException();
		}

		static object NewNullableInstanceSlow(Type type) { return Activator.CreateInstance(NullableType.MakeGenericType(type)); }

		static InvalidCastException InvalidCast(object o, string typeName) { return new InvalidCastException(string.Format("Cannot cast {0} to {1}", o == null ? "(null)" : o.GetType().Name, typeName)); }
	}
}
