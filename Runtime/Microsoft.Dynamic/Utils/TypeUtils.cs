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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Utils
{
	/// <summary>型に関するユーティリティ メソッドを公開します。</summary>
	public static class TypeUtils
	{
		// keep in sync with System.Core version
		internal static Type GetNonNullableType(Type type) { return IsNullableType(type) ? type.GetGenericArguments()[0] : type; }

		// keep in sync with System.Core version
		internal static bool IsNullableType(Type type) { return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>); }

		// keep in sync with System.Core version
		internal static bool IsNumeric(Type type) { return !(type = GetNonNullableType(type)).IsEnum && IsNumeric(Type.GetTypeCode(type)); }

		internal static bool IsNumeric(TypeCode typeCode)
		{
			if (IsArithmetic(typeCode))
				return true;
			switch (typeCode)
			{
				case TypeCode.Char:
				case TypeCode.SByte:
				case TypeCode.Byte:
					return true;
			}
			return false;
		}

		// keep in sync with System.Core version
		internal static bool IsArithmetic(Type type)
		{
			type = GetNonNullableType(type);
			return !type.IsEnum && IsArithmetic(Type.GetTypeCode(type));
		}

		static bool IsArithmetic(TypeCode typeCode)
		{
			switch (typeCode)
			{
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.Double:
				case TypeCode.Single:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
					return true;
			}
			return false;
		}

		// keep in sync with System.Core version
		internal static bool IsIntegerOrBool(Type type)
		{
			type = GetNonNullableType(type);
			if (!type.IsEnum)
			{
				switch (Type.GetTypeCode(type))
				{
					case TypeCode.Int64:
					case TypeCode.Int32:
					case TypeCode.Int16:
					case TypeCode.UInt64:
					case TypeCode.UInt32:
					case TypeCode.UInt16:
					case TypeCode.Boolean:
					case TypeCode.SByte:
					case TypeCode.Byte:
						return true;
				}
			}
			return false;
		}

		internal static bool CanAssign(Type to, Type from)
		{
			return to == from ||
				!to.IsValueType && !from.IsValueType &&
				(to.IsAssignableFrom(from) || to.IsArray && from.IsArray && to.GetArrayRank() == from.GetArrayRank() && CanAssign(to.GetElementType(), from.GetElementType()));
		}

		static bool GetNumericConversionOrder(TypeCode code, out int x, out int y)
		{
			// implicit conversions:
			//    0     1     2     3     4     5     6
			// 0:             Char
			//                |
			//                v
			// 1:       U1 -> U2 -> U4 -> U8
			//          |     |     |     |
			//          v     v     v     v
			// 2: I1 -> I2 -> I4 -> I8 -> +
			//                            |
			//                            v
			// 3:                         +  -> R4 -> R8
			//                            |
			//                            v
			// 4:                         Decimal
			switch (code)
			{
				case TypeCode.Char: x = 2; y = 0; break;
				case TypeCode.Byte: x = 1; y = 1; break;
				case TypeCode.UInt16: x = 2; y = 1; break;
				case TypeCode.UInt32: x = 3; y = 1; break;
				case TypeCode.UInt64: x = 4; y = 1; break;
				case TypeCode.SByte: x = 0; y = 2; break;
				case TypeCode.Int16: x = 1; y = 2; break;
				case TypeCode.Int32: x = 2; y = 2; break;
				case TypeCode.Int64: x = 3; y = 2; break;
				case TypeCode.Single: x = 5; y = 3; break;
				case TypeCode.Double: x = 6; y = 3; break;
				case TypeCode.Decimal: x = 4; y = 4; break;
				default:
					x = y = 0;
					return false;
			}
			return true;
		}

		internal static bool IsNumericImplicitlyConvertible(TypeCode from, TypeCode to)
		{
			int fromX, fromY, toX, toY;
			return GetNumericConversionOrder(from, out fromX, out fromY) && GetNumericConversionOrder(to, out toX, out toY) && fromX <= toX && fromY <= toY;
		}

		internal static bool HasBuiltinEquality(Type left, Type right)
		{
			// Reference type can be compared to interfaces
			return left.IsInterface && !right.IsValueType || right.IsInterface && !left.IsValueType ||
				// Reference types compare if they are assignable
				!left.IsValueType && !right.IsValueType && (CanAssign(left, right) || CanAssign(right, left)) ||
				// Nullable<T> vs null
				IsNullableType(left) && right == typeof(DynamicNull) ||
				IsNullableType(right) && left == typeof(DynamicNull) ||
				left == right && (left == typeof(bool) || IsNumeric(left) || left.IsEnum);
		}

		internal static bool IsImplicitlyConvertible(Type source, Type destination)
		{
			if (AreEquivalent(source, destination) ||
				IsNumericImplicitlyConvertible(Type.GetTypeCode(source), Type.GetTypeCode(destination)) ||
				AreAssignable(source, destination) ||
				source.IsValueType && (destination == typeof(object) || destination == typeof(ValueType)) ||
				source.IsEnum && destination == typeof(Enum))
				return true;
			// check for implicit coercions first
			var nnsrc = GetNonNullableType(source);
			var nndst = GetNonNullableType(destination);
			// try exact match on types
			return nnsrc.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
				.Concat(nndst.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
				.Any(x =>
				{
					if (x.Name != "op_Implicit" || x.ReturnType != destination && x.ReturnType != nndst)
						return false;
					var @thisType = x.GetParameters()[0].GetType();
					return @thisType == source || @thisType == nnsrc;
				});
		}

		// keep in sync with System.Core version
		internal static bool AreEquivalent(Type t1, Type t2) { return t1 == t2 || t1.IsEquivalentTo(t2); }

		// keep in sync with System.Core version
		internal static bool AreReferenceAssignable(Type dest, Type src)
		{
			// WARNING: This actually implements "Is this identity assignable and/or reference assignable?"
			return dest == src || !dest.IsValueType && !src.IsValueType && AreAssignable(dest, src);
		}

		// keep in sync with System.Core version
		internal static bool AreAssignable(Type dest, Type src)
		{
			return dest == src || dest.IsAssignableFrom(src) ||
				dest.IsArray && src.IsArray && dest.GetArrayRank() == src.GetArrayRank() && AreReferenceAssignable(dest.GetElementType(), src.GetElementType()) ||
				src.IsArray && dest.IsGenericType &&
				(dest.GetGenericTypeDefinition() == typeof(IEnumerable<>) || dest.GetGenericTypeDefinition() == typeof(IList<>) || dest.GetGenericTypeDefinition() == typeof(ICollection<>)) &&
				dest.GetGenericArguments()[0] == src.GetElementType();
		}

		// keep in sync with System.Core version
		internal static Type GetConstantType(Type type)
		{
			// If it's a visible type, we're done
			if (type.IsVisible)
				return type;
			// Get the visible base type
			Type bt = type;
			while (!bt.IsVisible)
				bt = bt.BaseType;
			// If it's one of the known reflection types, return the known type.
			if (bt == typeof(Type) || bt == typeof(ConstructorInfo) || bt == typeof(EventInfo) || bt == typeof(FieldInfo) || bt == typeof(MethodInfo) || bt == typeof(PropertyInfo))
				return bt;
			// else return the original type
			return type;
		}

		/// <summary>COM オブジェクトを表す型を示します。すべての COM オブジェクトはこの型に代入可能です。</summary>
		public static readonly Type ComObjectType = typeof(object).Assembly.GetType("System.__ComObject");

		/// <summary>指定された型が COM オブジェクトを表しているかどうかを判断します。</summary>
		/// <param name="type">COM オブジェクトを表しているかどうかを調べる型を指定します。</param>
		/// <returns><paramref name="type"/> が COM オブジェクトを表す型である場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool IsComObjectType(Type/*!*/ type) { return ComObjectType.IsAssignableFrom(type); }

		/// <summary>指定されたオブジェクトが COM オブジェクトであるかどうかを判断します。</summary>
		/// <param name="obj">COM オブジェクトであるかどうかを調べるオブジェクトを指定します。</param>
		/// <returns><paramref name="obj"/> が COM オブジェクトの場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		/// <remarks><see cref="System.Runtime.InteropServices.Marshal.IsComObject"/> は部分信頼では動作しないため使用できません。</remarks>
		public static bool IsComObject(object obj) { return obj != null && IsComObjectType(obj.GetType()); }
	}
}
