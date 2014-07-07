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
using System.CodeDom.Compiler;
using System.Diagnostics;

namespace Microsoft.Scripting.Runtime
{
	[GeneratedCode("DLR", "2.0")]
	public static partial class Cast
	{
		/// <summary>指定されたオブジェクトをブール値に変換します。</summary>
		/// <param name="o">変換されるオブジェクトを指定します。</param>
		/// <returns>変換されたオブジェクト。</returns>
		/// <exception cref="InvalidCastException">指定されたオブジェクトを型変換できませんでした。</exception>
		public static bool ExplicitCastToBoolean(object o)
		{
			if (o != null)
			{
				var type = o.GetType();
				if (type == BooleanType)
					return (bool)o;
				else if (type == NullableBooleanType)
					return (bool)(Nullable<bool>)o;
			}
			throw InvalidCast(o, "Boolean");
		}

		/// <summary>指定されたオブジェクトを 8 ビット符号なし整数に変換します。</summary>
		/// <param name="o">変換されるオブジェクトを指定します。</param>
		/// <returns>変換されたオブジェクト。</returns>
		/// <exception cref="InvalidCastException">指定されたオブジェクトを型変換できませんでした。</exception>
		public static byte ExplicitCastToByte(object o)
		{
			if (o != null)
			{
				var type = o.GetType();
				if (type == Int32Type)
					return (byte)(int)o;
				else if (type == DoubleType)
					return (byte)(double)o;
				else if (type == Int64Type)
					return (byte)(long)o;
				else if (type == Int16Type)
					return (byte)(short)o;
				else if (type == UInt32Type)
					return (byte)(uint)o;
				else if (type == UInt64Type)
					return (byte)(ulong)o;
				else if (type == UInt16Type)
					return (byte)(ushort)o;
				else if (type == SByteType)
					return (byte)(sbyte)o;
				else if (type == ByteType)
					return (byte)o;
				else if (type == SingleType)
					return (byte)(float)o;
				else if (type == CharType)
					return (byte)(char)o;
				else if (type == DecimalType)
					return (byte)(decimal)o;
				else if (type.IsEnum)
					return (byte)ExplicitCastEnumToByte(o);
				else if (type == NullableInt32Type)
					return (byte)(Nullable<int>)o;
				else if (type == NullableDoubleType)
					return (byte)(Nullable<double>)o;
				else if (type == NullableInt64Type)
					return (byte)(Nullable<long>)o;
				else if (type == NullableInt16Type)
					return (byte)(Nullable<short>)o;
				else if (type == NullableUInt32Type)
					return (byte)(Nullable<uint>)o;
				else if (type == NullableUInt64Type)
					return (byte)(Nullable<ulong>)o;
				else if (type == NullableUInt16Type)
					return (byte)(Nullable<ushort>)o;
				else if (type == NullableSByteType)
					return (byte)(Nullable<sbyte>)o;
				else if (type == NullableByteType)
					return (byte)(Nullable<byte>)o;
				else if (type == NullableSingleType)
					return (byte)(Nullable<float>)o;
				else if (type == NullableCharType)
					return (byte)(Nullable<char>)o;
				else if (type == NullableDecimalType)
					return (byte)(Nullable<decimal>)o;
			}
			throw InvalidCast(o, "Byte");
		}

		/// <summary>指定されたオブジェクトを <see cref="System.Char"/> に変換します。</summary>
		/// <param name="o">変換されるオブジェクトを指定します。</param>
		/// <returns>変換されたオブジェクト。</returns>
		/// <exception cref="InvalidCastException">指定されたオブジェクトを型変換できませんでした。</exception>
		public static char ExplicitCastToChar(object o)
		{
			if (o != null)
			{
				var type = o.GetType();
				if (type == Int32Type)
					return (char)(int)o;
				else if (type == DoubleType)
					return (char)(double)o;
				else if (type == Int64Type)
					return (char)(long)o;
				else if (type == Int16Type)
					return (char)(short)o;
				else if (type == UInt32Type)
					return (char)(uint)o;
				else if (type == UInt64Type)
					return (char)(ulong)o;
				else if (type == UInt16Type)
					return (char)(ushort)o;
				else if (type == SByteType)
					return (char)(sbyte)o;
				else if (type == ByteType)
					return (char)(byte)o;
				else if (type == SingleType)
					return (char)(float)o;
				else if (type == CharType)
					return (char)o;
				else if (type == DecimalType)
					return (char)(decimal)o;
				else if (type.IsEnum)
					return (char)ExplicitCastEnumToInt32(o);
				else if (type == NullableInt32Type)
					return (char)(Nullable<int>)o;
				else if (type == NullableDoubleType)
					return (char)(Nullable<double>)o;
				else if (type == NullableInt64Type)
					return (char)(Nullable<long>)o;
				else if (type == NullableInt16Type)
					return (char)(Nullable<short>)o;
				else if (type == NullableUInt32Type)
					return (char)(Nullable<uint>)o;
				else if (type == NullableUInt64Type)
					return (char)(Nullable<ulong>)o;
				else if (type == NullableUInt16Type)
					return (char)(Nullable<ushort>)o;
				else if (type == NullableSByteType)
					return (char)(Nullable<sbyte>)o;
				else if (type == NullableByteType)
					return (char)(Nullable<byte>)o;
				else if (type == NullableSingleType)
					return (char)(Nullable<float>)o;
				else if (type == NullableCharType)
					return (char)(Nullable<char>)o;
				else if (type == NullableDecimalType)
					return (char)(Nullable<decimal>)o;
			}
			throw InvalidCast(o, "Char");
		}

		/// <summary>指定されたオブジェクトを 10 進数値に変換します。</summary>
		/// <param name="o">変換されるオブジェクトを指定します。</param>
		/// <returns>変換されたオブジェクト。</returns>
		/// <exception cref="InvalidCastException">指定されたオブジェクトを型変換できませんでした。</exception>
		public static decimal ExplicitCastToDecimal(object o)
		{
			if (o != null)
			{
				var type = o.GetType();
				if (type == Int32Type)
					return (decimal)(int)o;
				else if (type == DoubleType)
					return (decimal)(double)o;
				else if (type == Int64Type)
					return (decimal)(long)o;
				else if (type == Int16Type)
					return (decimal)(short)o;
				else if (type == UInt32Type)
					return (decimal)(uint)o;
				else if (type == UInt64Type)
					return (decimal)(ulong)o;
				else if (type == UInt16Type)
					return (decimal)(ushort)o;
				else if (type == SByteType)
					return (decimal)(sbyte)o;
				else if (type == ByteType)
					return (decimal)(byte)o;
				else if (type == SingleType)
					return (decimal)(float)o;
				else if (type == CharType)
					return (decimal)(char)o;
				else if (type == DecimalType)
					return (decimal)o;
				else if (type.IsEnum)
					return (decimal)ExplicitCastEnumToInt64(o);
				else if (type == NullableInt32Type)
					return (decimal)(Nullable<int>)o;
				else if (type == NullableDoubleType)
					return (decimal)(Nullable<double>)o;
				else if (type == NullableInt64Type)
					return (decimal)(Nullable<long>)o;
				else if (type == NullableInt16Type)
					return (decimal)(Nullable<short>)o;
				else if (type == NullableUInt32Type)
					return (decimal)(Nullable<uint>)o;
				else if (type == NullableUInt64Type)
					return (decimal)(Nullable<ulong>)o;
				else if (type == NullableUInt16Type)
					return (decimal)(Nullable<ushort>)o;
				else if (type == NullableSByteType)
					return (decimal)(Nullable<sbyte>)o;
				else if (type == NullableByteType)
					return (decimal)(Nullable<byte>)o;
				else if (type == NullableSingleType)
					return (decimal)(Nullable<float>)o;
				else if (type == NullableCharType)
					return (decimal)(Nullable<char>)o;
				else if (type == NullableDecimalType)
					return (decimal)(Nullable<decimal>)o;
			}
			throw InvalidCast(o, "Decimal");
		}

		/// <summary>指定されたオブジェクトを倍精度浮動小数点数に変換します。</summary>
		/// <param name="o">変換されるオブジェクトを指定します。</param>
		/// <returns>変換されたオブジェクト。</returns>
		/// <exception cref="InvalidCastException">指定されたオブジェクトを型変換できませんでした。</exception>
		public static double ExplicitCastToDouble(object o)
		{
			if (o != null)
			{
				Type type = o.GetType();
				if (type == Int32Type)
					return (double)(int)o;
				else if (type == DoubleType)
					return (double)o;
				else if (type == Int64Type)
					return (double)(long)o;
				else if (type == Int16Type)
					return (double)(short)o;
				else if (type == UInt32Type)
					return (double)(uint)o;
				else if (type == UInt64Type)
					return (double)(ulong)o;
				else if (type == UInt16Type)
					return (double)(ushort)o;
				else if (type == SByteType)
					return (double)(sbyte)o;
				else if (type == ByteType)
					return (double)(byte)o;
				else if (type == SingleType)
					return (double)(float)o;
				else if (type == CharType)
					return (double)(char)o;
				else if (type == DecimalType)
					return (double)(decimal)o;
				else if (type.IsEnum)
					return (double)ExplicitCastEnumToInt64(o);
				else if (type == NullableInt32Type)
					return (double)(Nullable<int>)o;
				else if (type == NullableDoubleType)
					return (double)(Nullable<double>)o;
				else if (type == NullableInt64Type)
					return (double)(Nullable<long>)o;
				else if (type == NullableInt16Type)
					return (double)(Nullable<short>)o;
				else if (type == NullableUInt32Type)
					return (double)(Nullable<uint>)o;
				else if (type == NullableUInt64Type)
					return (double)(Nullable<ulong>)o;
				else if (type == NullableUInt16Type)
					return (double)(Nullable<ushort>)o;
				else if (type == NullableSByteType)
					return (double)(Nullable<sbyte>)o;
				else if (type == NullableByteType)
					return (double)(Nullable<byte>)o;
				else if (type == NullableSingleType)
					return (double)(Nullable<float>)o;
				else if (type == NullableCharType)
					return (double)(Nullable<char>)o;
				else if (type == NullableDecimalType)
					return (double)(Nullable<decimal>)o;
			}
			throw InvalidCast(o, "Double");
		}

		/// <summary>指定されたオブジェクトを 16 ビット符号付き整数に変換します。</summary>
		/// <param name="o">変換されるオブジェクトを指定します。</param>
		/// <returns>変換されたオブジェクト。</returns>
		/// <exception cref="InvalidCastException">指定されたオブジェクトを型変換できませんでした。</exception>
		public static short ExplicitCastToInt16(object o)
		{
			if (o != null)
			{
				var type = o.GetType();
				if (type == Int32Type)
					return (short)(int)o;
				else if (type == DoubleType)
					return (short)(double)o;
				else if (type == Int64Type)
					return (short)(long)o;
				else if (type == Int16Type)
					return (short)o;
				else if (type == UInt32Type)
					return (short)(uint)o;
				else if (type == UInt64Type)
					return (short)(ulong)o;
				else if (type == UInt16Type)
					return (short)(ushort)o;
				else if (type == SByteType)
					return (short)(sbyte)o;
				else if (type == ByteType)
					return (short)(byte)o;
				else if (type == SingleType)
					return (short)(float)o;
				else if (type == CharType)
					return (short)(char)o;
				else if (type == DecimalType)
					return (short)(decimal)o;
				else if (type.IsEnum)
					return (short)ExplicitCastEnumToInt16(o);
				else if (type == NullableInt32Type)
					return (short)(Nullable<int>)o;
				else if (type == NullableDoubleType)
					return (short)(Nullable<double>)o;
				else if (type == NullableInt64Type)
					return (short)(Nullable<long>)o;
				else if (type == NullableInt16Type)
					return (short)(Nullable<short>)o;
				else if (type == NullableUInt32Type)
					return (short)(Nullable<uint>)o;
				else if (type == NullableUInt64Type)
					return (short)(Nullable<ulong>)o;
				else if (type == NullableUInt16Type)
					return (short)(Nullable<ushort>)o;
				else if (type == NullableSByteType)
					return (short)(Nullable<sbyte>)o;
				else if (type == NullableByteType)
					return (short)(Nullable<byte>)o;
				else if (type == NullableSingleType)
					return (short)(Nullable<float>)o;
				else if (type == NullableCharType)
					return (short)(Nullable<char>)o;
				else if (type == NullableDecimalType)
					return (short)(Nullable<decimal>)o;
			}
			throw InvalidCast(o, "Int16");
		}

		/// <summary>指定されたオブジェクトを 32 ビット符号付き整数に変換します。</summary>
		/// <param name="o">変換されるオブジェクトを指定します。</param>
		/// <returns>変換されたオブジェクト。</returns>
		/// <exception cref="InvalidCastException">指定されたオブジェクトを型変換できませんでした。</exception>
		public static int ExplicitCastToInt32(object o)
		{
			if (o != null)
			{
				var type = o.GetType();
				if (type == Int32Type)
					return (int)o;
				else if (type == DoubleType)
					return (int)(double)o;
				else if (type == Int64Type)
					return (int)(long)o;
				else if (type == Int16Type)
					return (int)(short)o;
				else if (type == UInt32Type)
					return (int)(uint)o;
				else if (type == UInt64Type)
					return (int)(ulong)o;
				else if (type == UInt16Type)
					return (int)(ushort)o;
				else if (type == SByteType)
					return (int)(sbyte)o;
				else if (type == ByteType)
					return (int)(byte)o;
				else if (type == SingleType)
					return (int)(float)o;
				else if (type == CharType)
					return (int)(char)o;
				else if (type == DecimalType)
					return (int)(decimal)o;
				else if (type.IsEnum)
					return (int)ExplicitCastEnumToInt32(o);
				else if (type == NullableInt32Type)
					return (int)(Nullable<int>)o;
				else if (type == NullableDoubleType)
					return (int)(Nullable<double>)o;
				else if (type == NullableInt64Type)
					return (int)(Nullable<long>)o;
				else if (type == NullableInt16Type)
					return (int)(Nullable<short>)o;
				else if (type == NullableUInt32Type)
					return (int)(Nullable<uint>)o;
				else if (type == NullableUInt64Type)
					return (int)(Nullable<ulong>)o;
				else if (type == NullableUInt16Type)
					return (int)(Nullable<ushort>)o;
				else if (type == NullableSByteType)
					return (int)(Nullable<sbyte>)o;
				else if (type == NullableByteType)
					return (int)(Nullable<byte>)o;
				else if (type == NullableSingleType)
					return (int)(Nullable<float>)o;
				else if (type == NullableCharType)
					return (int)(Nullable<char>)o;
				else if (type == NullableDecimalType)
					return (int)(Nullable<decimal>)o;
			}
			throw InvalidCast(o, "Int32");
		}

		/// <summary>指定されたオブジェクトを 64 ビット符号付き整数に変換します。</summary>
		/// <param name="o">変換されるオブジェクトを指定します。</param>
		/// <returns>変換されたオブジェクト。</returns>
		/// <exception cref="InvalidCastException">指定されたオブジェクトを型変換できませんでした。</exception>
		public static long ExplicitCastToInt64(object o)
		{
			if (o != null)
			{
				var type = o.GetType();
				if (type == Int32Type)
					return (long)(int)o;
				else if (type == DoubleType)
					return (long)(double)o;
				else if (type == Int64Type)
					return (long)o;
				else if (type == Int16Type)
					return (long)(short)o;
				else if (type == UInt32Type)
					return (long)(uint)o;
				else if (type == UInt64Type)
					return (long)(ulong)o;
				else if (type == UInt16Type)
					return (long)(ushort)o;
				else if (type == SByteType)
					return (long)(sbyte)o;
				else if (type == ByteType)
					return (long)(byte)o;
				else if (type == SingleType)
					return (long)(float)o;
				else if (type == CharType)
					return (long)(char)o;
				else if (type == DecimalType)
					return (long)(decimal)o;
				else if (type.IsEnum)
					return (long)ExplicitCastEnumToInt64(o);
				else if (type == NullableInt32Type)
					return (long)(Nullable<int>)o;
				else if (type == NullableDoubleType)
					return (long)(Nullable<double>)o;
				else if (type == NullableInt64Type)
					return (long)(Nullable<long>)o;
				else if (type == NullableInt16Type)
					return (long)(Nullable<short>)o;
				else if (type == NullableUInt32Type)
					return (long)(Nullable<uint>)o;
				else if (type == NullableUInt64Type)
					return (long)(Nullable<ulong>)o;
				else if (type == NullableUInt16Type)
					return (long)(Nullable<ushort>)o;
				else if (type == NullableSByteType)
					return (long)(Nullable<sbyte>)o;
				else if (type == NullableByteType)
					return (long)(Nullable<byte>)o;
				else if (type == NullableSingleType)
					return (long)(Nullable<float>)o;
				else if (type == NullableCharType)
					return (long)(Nullable<char>)o;
				else if (type == NullableDecimalType)
					return (long)(Nullable<decimal>)o;
			}
			throw InvalidCast(o, "Int64");
		}

		/// <summary>指定されたオブジェクトを 8 ビット符号付き整数に変換します。</summary>
		/// <param name="o">変換されるオブジェクトを指定します。</param>
		/// <returns>変換されたオブジェクト。</returns>
		/// <exception cref="InvalidCastException">指定されたオブジェクトを型変換できませんでした。</exception>
		[CLSCompliant(false)]
		public static sbyte ExplicitCastToSByte(object o)
		{
			if (o != null)
			{
				var type = o.GetType();
				if (type == Int32Type)
					return (sbyte)(int)o;
				else if (type == DoubleType)
					return (sbyte)(double)o;
				else if (type == Int64Type)
					return (sbyte)(long)o;
				else if (type == Int16Type)
					return (sbyte)(short)o;
				else if (type == UInt32Type)
					return (sbyte)(uint)o;
				else if (type == UInt64Type)
					return (sbyte)(ulong)o;
				else if (type == UInt16Type)
					return (sbyte)(ushort)o;
				else if (type == SByteType)
					return (sbyte)o;
				else if (type == ByteType)
					return (sbyte)(byte)o;
				else if (type == SingleType)
					return (sbyte)(float)o;
				else if (type == CharType)
					return (sbyte)(char)o;
				else if (type == DecimalType)
					return (sbyte)(decimal)o;
				else if (type.IsEnum)
					return (sbyte)ExplicitCastEnumToSByte(o);
				else if (type == NullableInt32Type)
					return (sbyte)(Nullable<int>)o;
				else if (type == NullableDoubleType)
					return (sbyte)(Nullable<double>)o;
				else if (type == NullableInt64Type)
					return (sbyte)(Nullable<long>)o;
				else if (type == NullableInt16Type)
					return (sbyte)(Nullable<short>)o;
				else if (type == NullableUInt32Type)
					return (sbyte)(Nullable<uint>)o;
				else if (type == NullableUInt64Type)
					return (sbyte)(Nullable<ulong>)o;
				else if (type == NullableUInt16Type)
					return (sbyte)(Nullable<ushort>)o;
				else if (type == NullableSByteType)
					return (sbyte)(Nullable<sbyte>)o;
				else if (type == NullableByteType)
					return (sbyte)(Nullable<byte>)o;
				else if (type == NullableSingleType)
					return (sbyte)(Nullable<float>)o;
				else if (type == NullableCharType)
					return (sbyte)(Nullable<char>)o;
				else if (type == NullableDecimalType)
					return (sbyte)(Nullable<decimal>)o;
			}
			throw InvalidCast(o, "SByte");
		}

		/// <summary>指定されたオブジェクトを単精度浮動小数点数に変換します。</summary>
		/// <param name="o">変換されるオブジェクトを指定します。</param>
		/// <returns>変換されたオブジェクト。</returns>
		/// <exception cref="InvalidCastException">指定されたオブジェクトを型変換できませんでした。</exception>
		public static float ExplicitCastToSingle(object o)
		{
			if (o != null)
			{
				var type = o.GetType();
				if (type == Int32Type)
					return (float)(int)o;
				else if (type == DoubleType)
					return (float)(double)o;
				else if (type == Int64Type)
					return (float)(long)o;
				else if (type == Int16Type)
					return (float)(short)o;
				else if (type == UInt32Type)
					return (float)(uint)o;
				else if (type == UInt64Type)
					return (float)(ulong)o;
				else if (type == UInt16Type)
					return (float)(ushort)o;
				else if (type == SByteType)
					return (float)(sbyte)o;
				else if (type == ByteType)
					return (float)(byte)o;
				else if (type == SingleType)
					return (float)o;
				else if (type == CharType)
					return (float)(char)o;
				else if (type == DecimalType)
					return (float)(decimal)o;
				else if (type.IsEnum)
					return (float)ExplicitCastEnumToInt64(o);
				else if (type == NullableInt32Type)
					return (float)(Nullable<int>)o;
				else if (type == NullableDoubleType)
					return (float)(Nullable<double>)o;
				else if (type == NullableInt64Type)
					return (float)(Nullable<long>)o;
				else if (type == NullableInt16Type)
					return (float)(Nullable<short>)o;
				else if (type == NullableUInt32Type)
					return (float)(Nullable<uint>)o;
				else if (type == NullableUInt64Type)
					return (float)(Nullable<ulong>)o;
				else if (type == NullableUInt16Type)
					return (float)(Nullable<ushort>)o;
				else if (type == NullableSByteType)
					return (float)(Nullable<sbyte>)o;
				else if (type == NullableByteType)
					return (float)(Nullable<byte>)o;
				else if (type == NullableSingleType)
					return (float)(Nullable<float>)o;
				else if (type == NullableCharType)
					return (float)(Nullable<char>)o;
				else if (type == NullableDecimalType)
					return (float)(Nullable<decimal>)o;
			}
			throw InvalidCast(o, "Single");
		}

		/// <summary>指定されたオブジェクトを 16 ビット符号なし整数に変換します。</summary>
		/// <param name="o">変換されるオブジェクトを指定します。</param>
		/// <returns>変換されたオブジェクト。</returns>
		/// <exception cref="InvalidCastException">指定されたオブジェクトを型変換できませんでした。</exception>
		[CLSCompliant(false)]
		public static ushort ExplicitCastToUInt16(object o)
		{
			if (o != null)
			{
				var type = o.GetType();
				if (type == Int32Type)
					return (ushort)(int)o;
				else if (type == DoubleType)
					return (ushort)(double)o;
				else if (type == Int64Type)
					return (ushort)(long)o;
				else if (type == Int16Type)
					return (ushort)(short)o;
				else if (type == UInt32Type)
					return (ushort)(uint)o;
				else if (type == UInt64Type)
					return (ushort)(ulong)o;
				else if (type == UInt16Type)
					return (ushort)o;
				else if (type == SByteType)
					return (ushort)(sbyte)o;
				else if (type == ByteType)
					return (ushort)(byte)o;
				else if (type == SingleType)
					return (ushort)(float)o;
				else if (type == CharType)
					return (ushort)(char)o;
				else if (type == DecimalType)
					return (ushort)(decimal)o;
				else if (type.IsEnum)
					return (ushort)ExplicitCastEnumToUInt16(o);
				else if (type == NullableInt32Type)
					return (ushort)(Nullable<int>)o;
				else if (type == NullableDoubleType)
					return (ushort)(Nullable<double>)o;
				else if (type == NullableInt64Type)
					return (ushort)(Nullable<long>)o;
				else if (type == NullableInt16Type)
					return (ushort)(Nullable<short>)o;
				else if (type == NullableUInt32Type)
					return (ushort)(Nullable<uint>)o;
				else if (type == NullableUInt64Type)
					return (ushort)(Nullable<ulong>)o;
				else if (type == NullableUInt16Type)
					return (ushort)(Nullable<ushort>)o;
				else if (type == NullableSByteType)
					return (ushort)(Nullable<sbyte>)o;
				else if (type == NullableByteType)
					return (ushort)(Nullable<byte>)o;
				else if (type == NullableSingleType)
					return (ushort)(Nullable<float>)o;
				else if (type == NullableCharType)
					return (ushort)(Nullable<char>)o;
				else if (type == NullableDecimalType)
					return (ushort)(Nullable<decimal>)o;
			}
			throw InvalidCast(o, "UInt16");
		}

		/// <summary>指定されたオブジェクトを 32 ビット符号なし整数に変換します。</summary>
		/// <param name="o">変換されるオブジェクトを指定します。</param>
		/// <returns>変換されたオブジェクト。</returns>
		/// <exception cref="InvalidCastException">指定されたオブジェクトを型変換できませんでした。</exception>
		[CLSCompliant(false)]
		public static uint ExplicitCastToUInt32(object o)
		{
			if (o != null)
			{
				var type = o.GetType();
				if (type == Int32Type)
					return (uint)(int)o;
				else if (type == DoubleType)
					return (uint)(double)o;
				else if (type == Int64Type)
					return (uint)(long)o;
				else if (type == Int16Type)
					return (uint)(short)o;
				else if (type == UInt32Type)
					return (uint)o;
				else if (type == UInt64Type)
					return (uint)(ulong)o;
				else if (type == UInt16Type)
					return (uint)(ushort)o;
				else if (type == SByteType)
					return (uint)(sbyte)o;
				else if (type == ByteType)
					return (uint)(byte)o;
				else if (type == SingleType)
					return (uint)(float)o;
				else if (type == CharType)
					return (uint)(char)o;
				else if (type == DecimalType)
					return (uint)(decimal)o;
				else if (type.IsEnum)
					return (uint)ExplicitCastEnumToUInt32(o);
				else if (type == NullableInt32Type)
					return (uint)(Nullable<int>)o;
				else if (type == NullableDoubleType)
					return (uint)(Nullable<double>)o;
				else if (type == NullableInt64Type)
					return (uint)(Nullable<long>)o;
				else if (type == NullableInt16Type)
					return (uint)(Nullable<short>)o;
				else if (type == NullableUInt32Type)
					return (uint)(Nullable<uint>)o;
				else if (type == NullableUInt64Type)
					return (uint)(Nullable<ulong>)o;
				else if (type == NullableUInt16Type)
					return (uint)(Nullable<ushort>)o;
				else if (type == NullableSByteType)
					return (uint)(Nullable<sbyte>)o;
				else if (type == NullableByteType)
					return (uint)(Nullable<byte>)o;
				else if (type == NullableSingleType)
					return (uint)(Nullable<float>)o;
				else if (type == NullableCharType)
					return (uint)(Nullable<char>)o;
				else if (type == NullableDecimalType)
					return (uint)(Nullable<decimal>)o;
			}
			throw InvalidCast(o, "UInt32");
		}

		/// <summary>指定されたオブジェクトを 64 ビット符号なし整数に変換します。</summary>
		/// <param name="o">変換されるオブジェクトを指定します。</param>
		/// <returns>変換されたオブジェクト。</returns>
		/// <exception cref="InvalidCastException">指定されたオブジェクトを型変換できませんでした。</exception>
		[CLSCompliant(false)]
		public static ulong ExplicitCastToUInt64(object o)
		{
			if (o != null)
			{
				var type = o.GetType();
				if (type == Int32Type)
					return (ulong)(int)o;
				else if (type == DoubleType)
					return (ulong)(double)o;
				else if (type == Int64Type)
					return (ulong)(long)o;
				else if (type == Int16Type)
					return (ulong)(short)o;
				else if (type == UInt32Type)
					return (ulong)(uint)o;
				else if (type == UInt64Type)
					return (ulong)o;
				else if (type == UInt16Type)
					return (ulong)(ushort)o;
				else if (type == SByteType)
					return (ulong)(sbyte)o;
				else if (type == ByteType)
					return (ulong)(byte)o;
				else if (type == SingleType)
					return (ulong)(float)o;
				else if (type == CharType)
					return (ulong)(char)o;
				else if (type == DecimalType)
					return (ulong)(decimal)o;
				else if (type.IsEnum)
					return (ulong)ExplicitCastEnumToUInt64(o);
				else if (type == NullableInt32Type)
					return (ulong)(Nullable<int>)o;
				else if (type == NullableDoubleType)
					return (ulong)(Nullable<double>)o;
				else if (type == NullableInt64Type)
					return (ulong)(Nullable<long>)o;
				else if (type == NullableInt16Type)
					return (ulong)(Nullable<short>)o;
				else if (type == NullableUInt32Type)
					return (ulong)(Nullable<uint>)o;
				else if (type == NullableUInt64Type)
					return (ulong)(Nullable<ulong>)o;
				else if (type == NullableUInt16Type)
					return (ulong)(Nullable<ushort>)o;
				else if (type == NullableSByteType)
					return (ulong)(Nullable<sbyte>)o;
				else if (type == NullableByteType)
					return (ulong)(Nullable<byte>)o;
				else if (type == NullableSingleType)
					return (ulong)(Nullable<float>)o;
				else if (type == NullableCharType)
					return (ulong)(Nullable<char>)o;
				else if (type == NullableDecimalType)
					return (ulong)(Nullable<decimal>)o;
			}
			throw InvalidCast(o, "UInt64");
		}

		/// <summary>指定されたオブジェクトを <see cref="T:System.Nullable&lt;System.Boolean&gt;"/> に変換します。</summary>
		/// <param name="o">変換されるオブジェクトを指定します。</param>
		/// <returns>変換されたオブジェクト。</returns>
		/// <exception cref="InvalidCastException">指定されたオブジェクトを型変換できませんでした。</exception>
		public static Nullable<bool> ExplicitCastToNullableBoolean(object o)
		{
			if (o == null)
				return new Nullable<bool>();
			var type = o.GetType();
			if (type == BooleanType)
				return (Nullable<bool>)(bool)o;
			else if (type == NullableBooleanType)
				return (Nullable<bool>)o;
			throw InvalidCast(o, "Boolean");
		}

		/// <summary>指定されたオブジェクトを <see cref="T:System.Nullable&lt;System.Byte&gt;"/> に変換します。</summary>
		/// <param name="o">変換されるオブジェクトを指定します。</param>
		/// <returns>変換されたオブジェクト。</returns>
		/// <exception cref="InvalidCastException">指定されたオブジェクトを型変換できませんでした。</exception>
		public static Nullable<byte> ExplicitCastToNullableByte(object o)
		{
			if (o == null)
				return new Nullable<byte>();
			var type = o.GetType();
			if (type == Int32Type)
				return (Nullable<byte>)(int)o;
			else if (type == DoubleType)
				return (Nullable<byte>)(double)o;
			else if (type == Int64Type)
				return (Nullable<byte>)(long)o;
			else if (type == Int16Type)
				return (Nullable<byte>)(short)o;
			else if (type == UInt32Type)
				return (Nullable<byte>)(uint)o;
			else if (type == UInt64Type)
				return (Nullable<byte>)(ulong)o;
			else if (type == UInt16Type)
				return (Nullable<byte>)(ushort)o;
			else if (type == SByteType)
				return (Nullable<byte>)(sbyte)o;
			else if (type == ByteType)
				return (Nullable<byte>)(byte)o;
			else if (type == SingleType)
				return (Nullable<byte>)(float)o;
			else if (type == CharType)
				return (Nullable<byte>)(char)o;
			else if (type == DecimalType)
				return (Nullable<byte>)(decimal)o;
			else if (type.IsEnum)
				return (Nullable<byte>)ExplicitCastEnumToByte(o);
			else if (type == NullableInt32Type)
				return (Nullable<byte>)(Nullable<int>)o;
			else if (type == NullableDoubleType)
				return (Nullable<byte>)(Nullable<double>)o;
			else if (type == NullableInt64Type)
				return (Nullable<byte>)(Nullable<long>)o;
			else if (type == NullableInt16Type)
				return (Nullable<byte>)(Nullable<short>)o;
			else if (type == NullableUInt32Type)
				return (Nullable<byte>)(Nullable<uint>)o;
			else if (type == NullableUInt64Type)
				return (Nullable<byte>)(Nullable<ulong>)o;
			else if (type == NullableUInt16Type)
				return (Nullable<byte>)(Nullable<ushort>)o;
			else if (type == NullableSByteType)
				return (Nullable<byte>)(Nullable<sbyte>)o;
			else if (type == NullableByteType)
				return (Nullable<byte>)(Nullable<byte>)o;
			else if (type == NullableSingleType)
				return (Nullable<byte>)(Nullable<float>)o;
			else if (type == NullableCharType)
				return (Nullable<byte>)(Nullable<char>)o;
			else if (type == NullableDecimalType)
				return (Nullable<byte>)(Nullable<decimal>)o;
			throw InvalidCast(o, "Byte");
		}

		/// <summary>指定されたオブジェクトを <see cref="T:System.Nullable&lt;System.Char&gt;"/> に変換します。</summary>
		/// <param name="o">変換されるオブジェクトを指定します。</param>
		/// <returns>変換されたオブジェクト。</returns>
		/// <exception cref="InvalidCastException">指定されたオブジェクトを型変換できませんでした。</exception>
		public static Nullable<char> ExplicitCastToNullableChar(object o)
		{
			if (o == null)
				return new Nullable<char>();
			var type = o.GetType();
			if (type == Int32Type)
				return (Nullable<char>)(int)o;
			else if (type == DoubleType)
				return (Nullable<char>)(double)o;
			else if (type == Int64Type)
				return (Nullable<char>)(long)o;
			else if (type == Int16Type)
				return (Nullable<char>)(short)o;
			else if (type == UInt32Type)
				return (Nullable<char>)(uint)o;
			else if (type == UInt64Type)
				return (Nullable<char>)(ulong)o;
			else if (type == UInt16Type)
				return (Nullable<char>)(ushort)o;
			else if (type == SByteType)
				return (Nullable<char>)(sbyte)o;
			else if (type == ByteType)
				return (Nullable<char>)(byte)o;
			else if (type == SingleType)
				return (Nullable<char>)(float)o;
			else if (type == CharType)
				return (Nullable<char>)(char)o;
			else if (type == DecimalType)
				return (Nullable<char>)(decimal)o;
			else if (type.IsEnum)
				return (Nullable<char>)ExplicitCastEnumToInt32(o);
			else if (type == NullableInt32Type)
				return (Nullable<char>)(Nullable<int>)o;
			else if (type == NullableDoubleType)
				return (Nullable<char>)(Nullable<double>)o;
			else if (type == NullableInt64Type)
				return (Nullable<char>)(Nullable<long>)o;
			else if (type == NullableInt16Type)
				return (Nullable<char>)(Nullable<short>)o;
			else if (type == NullableUInt32Type)
				return (Nullable<char>)(Nullable<uint>)o;
			else if (type == NullableUInt64Type)
				return (Nullable<char>)(Nullable<ulong>)o;
			else if (type == NullableUInt16Type)
				return (Nullable<char>)(Nullable<ushort>)o;
			else if (type == NullableSByteType)
				return (Nullable<char>)(Nullable<sbyte>)o;
			else if (type == NullableByteType)
				return (Nullable<char>)(Nullable<byte>)o;
			else if (type == NullableSingleType)
				return (Nullable<char>)(Nullable<float>)o;
			else if (type == NullableCharType)
				return (Nullable<char>)(Nullable<char>)o;
			else if (type == NullableDecimalType)
				return (Nullable<char>)(Nullable<decimal>)o;
			throw InvalidCast(o, "Char");
		}

		/// <summary>指定されたオブジェクトを <see cref="T:System.Nullable&lt;System.Decimal&gt;"/> に変換します。</summary>
		/// <param name="o">変換されるオブジェクトを指定します。</param>
		/// <returns>変換されたオブジェクト。</returns>
		/// <exception cref="InvalidCastException">指定されたオブジェクトを型変換できませんでした。</exception>
		public static Nullable<decimal> ExplicitCastToNullableDecimal(object o)
		{
			if (o == null)
				return new Nullable<decimal>();
			var type = o.GetType();
			if (type == Int32Type)
				return (Nullable<decimal>)(int)o;
			else if (type == DoubleType)
				return (Nullable<decimal>)(double)o;
			else if (type == Int64Type)
				return (Nullable<decimal>)(long)o;
			else if (type == Int16Type)
				return (Nullable<decimal>)(short)o;
			else if (type == UInt32Type)
				return (Nullable<decimal>)(uint)o;
			else if (type == UInt64Type)
				return (Nullable<decimal>)(ulong)o;
			else if (type == UInt16Type)
				return (Nullable<decimal>)(ushort)o;
			else if (type == SByteType)
				return (Nullable<decimal>)(sbyte)o;
			else if (type == ByteType)
				return (Nullable<decimal>)(byte)o;
			else if (type == SingleType)
				return (Nullable<decimal>)(float)o;
			else if (type == CharType)
				return (Nullable<decimal>)(char)o;
			else if (type == DecimalType)
				return (Nullable<decimal>)(decimal)o;
			else if (type.IsEnum)
				return (Nullable<decimal>)ExplicitCastEnumToInt64(o);
			else if (type == NullableInt32Type)
				return (Nullable<decimal>)(Nullable<int>)o;
			else if (type == NullableDoubleType)
				return (Nullable<decimal>)(Nullable<double>)o;
			else if (type == NullableInt64Type)
				return (Nullable<decimal>)(Nullable<long>)o;
			else if (type == NullableInt16Type)
				return (Nullable<decimal>)(Nullable<short>)o;
			else if (type == NullableUInt32Type)
				return (Nullable<decimal>)(Nullable<uint>)o;
			else if (type == NullableUInt64Type)
				return (Nullable<decimal>)(Nullable<ulong>)o;
			else if (type == NullableUInt16Type)
				return (Nullable<decimal>)(Nullable<ushort>)o;
			else if (type == NullableSByteType)
				return (Nullable<decimal>)(Nullable<sbyte>)o;
			else if (type == NullableByteType)
				return (Nullable<decimal>)(Nullable<byte>)o;
			else if (type == NullableSingleType)
				return (Nullable<decimal>)(Nullable<float>)o;
			else if (type == NullableCharType)
				return (Nullable<decimal>)(Nullable<char>)o;
			else if (type == NullableDecimalType)
				return (Nullable<decimal>)(Nullable<decimal>)o;
			throw InvalidCast(o, "Decimal");
		}

		/// <summary>指定されたオブジェクトを <see cref="T:System.Nullable&lt;System.Double&gt;"/> に変換します。</summary>
		/// <param name="o">変換されるオブジェクトを指定します。</param>
		/// <returns>変換されたオブジェクト。</returns>
		/// <exception cref="InvalidCastException">指定されたオブジェクトを型変換できませんでした。</exception>
		public static Nullable<double> ExplicitCastToNullableDouble(object o)
		{
			if (o == null)
				return new Nullable<double>();
			var type = o.GetType();
			if (type == Int32Type)
				return (Nullable<double>)(int)o;
			else if (type == DoubleType)
				return (Nullable<double>)(double)o;
			else if (type == Int64Type)
				return (Nullable<double>)(long)o;
			else if (type == Int16Type)
				return (Nullable<double>)(short)o;
			else if (type == UInt32Type)
				return (Nullable<double>)(uint)o;
			else if (type == UInt64Type)
				return (Nullable<double>)(ulong)o;
			else if (type == UInt16Type)
				return (Nullable<double>)(ushort)o;
			else if (type == SByteType)
				return (Nullable<double>)(sbyte)o;
			else if (type == ByteType)
				return (Nullable<double>)(byte)o;
			else if (type == SingleType)
				return (Nullable<double>)(float)o;
			else if (type == CharType)
				return (Nullable<double>)(char)o;
			else if (type == DecimalType)
				return (Nullable<double>)(decimal)o;
			else if (type.IsEnum)
				return (Nullable<double>)ExplicitCastEnumToInt64(o);
			else if (type == NullableInt32Type)
				return (Nullable<double>)(Nullable<int>)o;
			else if (type == NullableDoubleType)
				return (Nullable<double>)(Nullable<double>)o;
			else if (type == NullableInt64Type)
				return (Nullable<double>)(Nullable<long>)o;
			else if (type == NullableInt16Type)
				return (Nullable<double>)(Nullable<short>)o;
			else if (type == NullableUInt32Type)
				return (Nullable<double>)(Nullable<uint>)o;
			else if (type == NullableUInt64Type)
				return (Nullable<double>)(Nullable<ulong>)o;
			else if (type == NullableUInt16Type)
				return (Nullable<double>)(Nullable<ushort>)o;
			else if (type == NullableSByteType)
				return (Nullable<double>)(Nullable<sbyte>)o;
			else if (type == NullableByteType)
				return (Nullable<double>)(Nullable<byte>)o;
			else if (type == NullableSingleType)
				return (Nullable<double>)(Nullable<float>)o;
			else if (type == NullableCharType)
				return (Nullable<double>)(Nullable<char>)o;
			else if (type == NullableDecimalType)
				return (Nullable<double>)(Nullable<decimal>)o;
			throw InvalidCast(o, "Double");
		}

		/// <summary>指定されたオブジェクトを <see cref="T:System.Nullable&lt;System.Int16&gt;"/> に変換します。</summary>
		/// <param name="o">変換されるオブジェクトを指定します。</param>
		/// <returns>変換されたオブジェクト。</returns>
		/// <exception cref="InvalidCastException">指定されたオブジェクトを型変換できませんでした。</exception>
		public static Nullable<short> ExplicitCastToNullableInt16(object o)
		{
			if (o == null)
				return new Nullable<short>();
			var type = o.GetType();
			if (type == Int32Type)
				return (Nullable<short>)(int)o;
			else if (type == DoubleType)
				return (Nullable<short>)(double)o;
			else if (type == Int64Type)
				return (Nullable<short>)(long)o;
			else if (type == Int16Type)
				return (Nullable<short>)(short)o;
			else if (type == UInt32Type)
				return (Nullable<short>)(uint)o;
			else if (type == UInt64Type)
				return (Nullable<short>)(ulong)o;
			else if (type == UInt16Type)
				return (Nullable<short>)(ushort)o;
			else if (type == SByteType)
				return (Nullable<short>)(sbyte)o;
			else if (type == ByteType)
				return (Nullable<short>)(byte)o;
			else if (type == SingleType)
				return (Nullable<short>)(float)o;
			else if (type == CharType)
				return (Nullable<short>)(char)o;
			else if (type == DecimalType)
				return (Nullable<short>)(decimal)o;
			else if (type.IsEnum)
				return (Nullable<short>)ExplicitCastEnumToInt16(o);
			else if (type == NullableInt32Type)
				return (Nullable<short>)(Nullable<int>)o;
			else if (type == NullableDoubleType)
				return (Nullable<short>)(Nullable<double>)o;
			else if (type == NullableInt64Type)
				return (Nullable<short>)(Nullable<long>)o;
			else if (type == NullableInt16Type)
				return (Nullable<short>)(Nullable<short>)o;
			else if (type == NullableUInt32Type)
				return (Nullable<short>)(Nullable<uint>)o;
			else if (type == NullableUInt64Type)
				return (Nullable<short>)(Nullable<ulong>)o;
			else if (type == NullableUInt16Type)
				return (Nullable<short>)(Nullable<ushort>)o;
			else if (type == NullableSByteType)
				return (Nullable<short>)(Nullable<sbyte>)o;
			else if (type == NullableByteType)
				return (Nullable<short>)(Nullable<byte>)o;
			else if (type == NullableSingleType)
				return (Nullable<short>)(Nullable<float>)o;
			else if (type == NullableCharType)
				return (Nullable<short>)(Nullable<char>)o;
			else if (type == NullableDecimalType)
				return (Nullable<short>)(Nullable<decimal>)o;
			throw InvalidCast(o, "Int16");
		}

		/// <summary>指定されたオブジェクトを <see cref="T:System.Nullable&lt;System.Int32&gt;"/> に変換します。</summary>
		/// <param name="o">変換されるオブジェクトを指定します。</param>
		/// <returns>変換されたオブジェクト。</returns>
		/// <exception cref="InvalidCastException">指定されたオブジェクトを型変換できませんでした。</exception>
		public static Nullable<int> ExplicitCastToNullableInt32(object o)
		{
			if (o == null)
				return new Nullable<int>();
			var type = o.GetType();
			if (type == Int32Type)
				return (Nullable<int>)(int)o;
			else if (type == DoubleType)
				return (Nullable<int>)(double)o;
			else if (type == Int64Type)
				return (Nullable<int>)(long)o;
			else if (type == Int16Type)
				return (Nullable<int>)(short)o;
			else if (type == UInt32Type)
				return (Nullable<int>)(uint)o;
			else if (type == UInt64Type)
				return (Nullable<int>)(ulong)o;
			else if (type == UInt16Type)
				return (Nullable<int>)(ushort)o;
			else if (type == SByteType)
				return (Nullable<int>)(sbyte)o;
			else if (type == ByteType)
				return (Nullable<int>)(byte)o;
			else if (type == SingleType)
				return (Nullable<int>)(float)o;
			else if (type == CharType)
				return (Nullable<int>)(char)o;
			else if (type == DecimalType)
				return (Nullable<int>)(decimal)o;
			else if (type.IsEnum)
				return (Nullable<int>)ExplicitCastEnumToInt32(o);
			else if (type == NullableInt32Type)
				return (Nullable<int>)(Nullable<int>)o;
			else if (type == NullableDoubleType)
				return (Nullable<int>)(Nullable<double>)o;
			else if (type == NullableInt64Type)
				return (Nullable<int>)(Nullable<long>)o;
			else if (type == NullableInt16Type)
				return (Nullable<int>)(Nullable<short>)o;
			else if (type == NullableUInt32Type)
				return (Nullable<int>)(Nullable<uint>)o;
			else if (type == NullableUInt64Type)
				return (Nullable<int>)(Nullable<ulong>)o;
			else if (type == NullableUInt16Type)
				return (Nullable<int>)(Nullable<ushort>)o;
			else if (type == NullableSByteType)
				return (Nullable<int>)(Nullable<sbyte>)o;
			else if (type == NullableByteType)
				return (Nullable<int>)(Nullable<byte>)o;
			else if (type == NullableSingleType)
				return (Nullable<int>)(Nullable<float>)o;
			else if (type == NullableCharType)
				return (Nullable<int>)(Nullable<char>)o;
			else if (type == NullableDecimalType)
				return (Nullable<int>)(Nullable<decimal>)o;
			throw InvalidCast(o, "Int32");
		}

		/// <summary>指定されたオブジェクトを <see cref="T:System.Nullable&lt;System.Int64&gt;"/> に変換します。</summary>
		/// <param name="o">変換されるオブジェクトを指定します。</param>
		/// <returns>変換されたオブジェクト。</returns>
		/// <exception cref="InvalidCastException">指定されたオブジェクトを型変換できませんでした。</exception>
		public static Nullable<long> ExplicitCastToNullableInt64(object o)
		{
			if (o == null)
				return new Nullable<long>();
			var type = o.GetType();
			if (type == Int32Type)
				return (Nullable<long>)(int)o;
			else if (type == DoubleType)
				return (Nullable<long>)(double)o;
			else if (type == Int64Type)
				return (Nullable<long>)(long)o;
			else if (type == Int16Type)
				return (Nullable<long>)(short)o;
			else if (type == UInt32Type)
				return (Nullable<long>)(uint)o;
			else if (type == UInt64Type)
				return (Nullable<long>)(ulong)o;
			else if (type == UInt16Type)
				return (Nullable<long>)(ushort)o;
			else if (type == SByteType)
				return (Nullable<long>)(sbyte)o;
			else if (type == ByteType)
				return (Nullable<long>)(byte)o;
			else if (type == SingleType)
				return (Nullable<long>)(float)o;
			else if (type == CharType)
				return (Nullable<long>)(char)o;
			else if (type == DecimalType)
				return (Nullable<long>)(decimal)o;
			else if (type.IsEnum)
				return (Nullable<long>)ExplicitCastEnumToInt64(o);
			else if (type == NullableInt32Type)
				return (Nullable<long>)(Nullable<int>)o;
			else if (type == NullableDoubleType)
				return (Nullable<long>)(Nullable<double>)o;
			else if (type == NullableInt64Type)
				return (Nullable<long>)(Nullable<long>)o;
			else if (type == NullableInt16Type)
				return (Nullable<long>)(Nullable<short>)o;
			else if (type == NullableUInt32Type)
				return (Nullable<long>)(Nullable<uint>)o;
			else if (type == NullableUInt64Type)
				return (Nullable<long>)(Nullable<ulong>)o;
			else if (type == NullableUInt16Type)
				return (Nullable<long>)(Nullable<ushort>)o;
			else if (type == NullableSByteType)
				return (Nullable<long>)(Nullable<sbyte>)o;
			else if (type == NullableByteType)
				return (Nullable<long>)(Nullable<byte>)o;
			else if (type == NullableSingleType)
				return (Nullable<long>)(Nullable<float>)o;
			else if (type == NullableCharType)
				return (Nullable<long>)(Nullable<char>)o;
			else if (type == NullableDecimalType)
				return (Nullable<long>)(Nullable<decimal>)o;
			throw InvalidCast(o, "Int64");
		}

		/// <summary>指定されたオブジェクトを <see cref="T:System.Nullable&lt;System.SByte&gt;"/> に変換します。</summary>
		/// <param name="o">変換されるオブジェクトを指定します。</param>
		/// <returns>変換されたオブジェクト。</returns>
		/// <exception cref="InvalidCastException">指定されたオブジェクトを型変換できませんでした。</exception>
		[CLSCompliant(false)]
		public static Nullable<sbyte> ExplicitCastToNullableSByte(object o)
		{
			if (o == null)
				return new Nullable<sbyte>();
			var type = o.GetType();
			if (type == Int32Type)
				return (Nullable<sbyte>)(int)o;
			else if (type == DoubleType)
				return (Nullable<sbyte>)(double)o;
			else if (type == Int64Type)
				return (Nullable<sbyte>)(long)o;
			else if (type == Int16Type)
				return (Nullable<sbyte>)(short)o;
			else if (type == UInt32Type)
				return (Nullable<sbyte>)(uint)o;
			else if (type == UInt64Type)
				return (Nullable<sbyte>)(ulong)o;
			else if (type == UInt16Type)
				return (Nullable<sbyte>)(ushort)o;
			else if (type == SByteType)
				return (Nullable<sbyte>)(sbyte)o;
			else if (type == ByteType)
				return (Nullable<sbyte>)(byte)o;
			else if (type == SingleType)
				return (Nullable<sbyte>)(float)o;
			else if (type == CharType)
				return (Nullable<sbyte>)(char)o;
			else if (type == DecimalType)
				return (Nullable<sbyte>)(decimal)o;
			else if (type.IsEnum)
				return (Nullable<sbyte>)ExplicitCastEnumToSByte(o);
			else if (type == NullableInt32Type)
				return (Nullable<sbyte>)(Nullable<int>)o;
			else if (type == NullableDoubleType)
				return (Nullable<sbyte>)(Nullable<double>)o;
			else if (type == NullableInt64Type)
				return (Nullable<sbyte>)(Nullable<long>)o;
			else if (type == NullableInt16Type)
				return (Nullable<sbyte>)(Nullable<short>)o;
			else if (type == NullableUInt32Type)
				return (Nullable<sbyte>)(Nullable<uint>)o;
			else if (type == NullableUInt64Type)
				return (Nullable<sbyte>)(Nullable<ulong>)o;
			else if (type == NullableUInt16Type)
				return (Nullable<sbyte>)(Nullable<ushort>)o;
			else if (type == NullableSByteType)
				return (Nullable<sbyte>)(Nullable<sbyte>)o;
			else if (type == NullableByteType)
				return (Nullable<sbyte>)(Nullable<byte>)o;
			else if (type == NullableSingleType)
				return (Nullable<sbyte>)(Nullable<float>)o;
			else if (type == NullableCharType)
				return (Nullable<sbyte>)(Nullable<char>)o;
			else if (type == NullableDecimalType)
				return (Nullable<sbyte>)(Nullable<decimal>)o;
			throw InvalidCast(o, "SByte");
		}

		/// <summary>指定されたオブジェクトを <see cref="T:System.Nullable&lt;System.Single&gt;"/> に変換します。</summary>
		/// <param name="o">変換されるオブジェクトを指定します。</param>
		/// <returns>変換されたオブジェクト。</returns>
		/// <exception cref="InvalidCastException">指定されたオブジェクトを型変換できませんでした。</exception>
		public static Nullable<float> ExplicitCastToNullableSingle(object o)
		{
			if (o == null)
				return new Nullable<float>();
			var type = o.GetType();
			if (type == Int32Type)
				return (Nullable<float>)(int)o;
			else if (type == DoubleType)
				return (Nullable<float>)(double)o;
			else if (type == Int64Type)
				return (Nullable<float>)(long)o;
			else if (type == Int16Type)
				return (Nullable<float>)(short)o;
			else if (type == UInt32Type)
				return (Nullable<float>)(uint)o;
			else if (type == UInt64Type)
				return (Nullable<float>)(ulong)o;
			else if (type == UInt16Type)
				return (Nullable<float>)(ushort)o;
			else if (type == SByteType)
				return (Nullable<float>)(sbyte)o;
			else if (type == ByteType)
				return (Nullable<float>)(byte)o;
			else if (type == SingleType)
				return (Nullable<float>)(float)o;
			else if (type == CharType)
				return (Nullable<float>)(char)o;
			else if (type == DecimalType)
				return (Nullable<float>)(decimal)o;
			else if (type.IsEnum)
				return (Nullable<float>)ExplicitCastEnumToInt64(o);
			else if (type == NullableInt32Type)
				return (Nullable<float>)(Nullable<int>)o;
			else if (type == NullableDoubleType)
				return (Nullable<float>)(Nullable<double>)o;
			else if (type == NullableInt64Type)
				return (Nullable<float>)(Nullable<long>)o;
			else if (type == NullableInt16Type)
				return (Nullable<float>)(Nullable<short>)o;
			else if (type == NullableUInt32Type)
				return (Nullable<float>)(Nullable<uint>)o;
			else if (type == NullableUInt64Type)
				return (Nullable<float>)(Nullable<ulong>)o;
			else if (type == NullableUInt16Type)
				return (Nullable<float>)(Nullable<ushort>)o;
			else if (type == NullableSByteType)
				return (Nullable<float>)(Nullable<sbyte>)o;
			else if (type == NullableByteType)
				return (Nullable<float>)(Nullable<byte>)o;
			else if (type == NullableSingleType)
				return (Nullable<float>)(Nullable<float>)o;
			else if (type == NullableCharType)
				return (Nullable<float>)(Nullable<char>)o;
			else if (type == NullableDecimalType)
				return (Nullable<float>)(Nullable<decimal>)o;
			throw InvalidCast(o, "Single");
		}

		/// <summary>指定されたオブジェクトを <see cref="T:System.Nullable&lt;System.UInt16&gt;"/> に変換します。</summary>
		/// <param name="o">変換されるオブジェクトを指定します。</param>
		/// <returns>変換されたオブジェクト。</returns>
		/// <exception cref="InvalidCastException">指定されたオブジェクトを型変換できませんでした。</exception>
		[CLSCompliant(false)]
		public static Nullable<ushort> ExplicitCastToNullableUInt16(object o)
		{
			if (o == null)
				return new Nullable<ushort>();
			var type = o.GetType();
			if (type == Int32Type)
				return (Nullable<ushort>)(int)o;
			else if (type == DoubleType)
				return (Nullable<ushort>)(double)o;
			else if (type == Int64Type)
				return (Nullable<ushort>)(long)o;
			else if (type == Int16Type)
				return (Nullable<ushort>)(short)o;
			else if (type == UInt32Type)
				return (Nullable<ushort>)(uint)o;
			else if (type == UInt64Type)
				return (Nullable<ushort>)(ulong)o;
			else if (type == UInt16Type)
				return (Nullable<ushort>)(ushort)o;
			else if (type == SByteType)
				return (Nullable<ushort>)(sbyte)o;
			else if (type == ByteType)
				return (Nullable<ushort>)(byte)o;
			else if (type == SingleType)
				return (Nullable<ushort>)(float)o;
			else if (type == CharType)
				return (Nullable<ushort>)(char)o;
			else if (type == DecimalType)
				return (Nullable<ushort>)(decimal)o;
			else if (type.IsEnum)
				return (Nullable<ushort>)ExplicitCastEnumToUInt16(o);
			else if (type == NullableInt32Type)
				return (Nullable<ushort>)(Nullable<int>)o;
			else if (type == NullableDoubleType)
				return (Nullable<ushort>)(Nullable<double>)o;
			else if (type == NullableInt64Type)
				return (Nullable<ushort>)(Nullable<long>)o;
			else if (type == NullableInt16Type)
				return (Nullable<ushort>)(Nullable<short>)o;
			else if (type == NullableUInt32Type)
				return (Nullable<ushort>)(Nullable<uint>)o;
			else if (type == NullableUInt64Type)
				return (Nullable<ushort>)(Nullable<ulong>)o;
			else if (type == NullableUInt16Type)
				return (Nullable<ushort>)(Nullable<ushort>)o;
			else if (type == NullableSByteType)
				return (Nullable<ushort>)(Nullable<sbyte>)o;
			else if (type == NullableByteType)
				return (Nullable<ushort>)(Nullable<byte>)o;
			else if (type == NullableSingleType)
				return (Nullable<ushort>)(Nullable<float>)o;
			else if (type == NullableCharType)
				return (Nullable<ushort>)(Nullable<char>)o;
			else if (type == NullableDecimalType)
				return (Nullable<ushort>)(Nullable<decimal>)o;
			throw InvalidCast(o, "UInt16");
		}

		/// <summary>指定されたオブジェクトを <see cref="T:System.Nullable&lt;System.UInt32&gt;"/> に変換します。</summary>
		/// <param name="o">変換されるオブジェクトを指定します。</param>
		/// <returns>変換されたオブジェクト。</returns>
		/// <exception cref="InvalidCastException">指定されたオブジェクトを型変換できませんでした。</exception>
		[CLSCompliant(false)]
		public static Nullable<uint> ExplicitCastToNullableUInt32(object o)
		{
			if (o == null)
				return new Nullable<uint>();
			var type = o.GetType();
			if (type == Int32Type)
				return (Nullable<uint>)(int)o;
			else if (type == DoubleType)
				return (Nullable<uint>)(double)o;
			else if (type == Int64Type)
				return (Nullable<uint>)(long)o;
			else if (type == Int16Type)
				return (Nullable<uint>)(short)o;
			else if (type == UInt32Type)
				return (Nullable<uint>)(uint)o;
			else if (type == UInt64Type)
				return (Nullable<uint>)(ulong)o;
			else if (type == UInt16Type)
				return (Nullable<uint>)(ushort)o;
			else if (type == SByteType)
				return (Nullable<uint>)(sbyte)o;
			else if (type == ByteType)
				return (Nullable<uint>)(byte)o;
			else if (type == SingleType)
				return (Nullable<uint>)(float)o;
			else if (type == CharType)
				return (Nullable<uint>)(char)o;
			else if (type == DecimalType)
				return (Nullable<uint>)(decimal)o;
			else if (type.IsEnum)
				return (Nullable<uint>)ExplicitCastEnumToUInt32(o);
			else if (type == NullableInt32Type)
				return (Nullable<uint>)(Nullable<int>)o;
			else if (type == NullableDoubleType)
				return (Nullable<uint>)(Nullable<double>)o;
			else if (type == NullableInt64Type)
				return (Nullable<uint>)(Nullable<long>)o;
			else if (type == NullableInt16Type)
				return (Nullable<uint>)(Nullable<short>)o;
			else if (type == NullableUInt32Type)
				return (Nullable<uint>)(Nullable<uint>)o;
			else if (type == NullableUInt64Type)
				return (Nullable<uint>)(Nullable<ulong>)o;
			else if (type == NullableUInt16Type)
				return (Nullable<uint>)(Nullable<ushort>)o;
			else if (type == NullableSByteType)
				return (Nullable<uint>)(Nullable<sbyte>)o;
			else if (type == NullableByteType)
				return (Nullable<uint>)(Nullable<byte>)o;
			else if (type == NullableSingleType)
				return (Nullable<uint>)(Nullable<float>)o;
			else if (type == NullableCharType)
				return (Nullable<uint>)(Nullable<char>)o;
			else if (type == NullableDecimalType)
				return (Nullable<uint>)(Nullable<decimal>)o;
			throw InvalidCast(o, "UInt32");
		}

		/// <summary>指定されたオブジェクトを <see cref="T:System.Nullable&lt;System.UInt64&gt;"/> に変換します。</summary>
		/// <param name="o">変換されるオブジェクトを指定します。</param>
		/// <returns>変換されたオブジェクト。</returns>
		/// <exception cref="InvalidCastException">指定されたオブジェクトを型変換できませんでした。</exception>
		[CLSCompliant(false)]
		public static Nullable<ulong> ExplicitCastToNullableUInt64(object o)
		{
			if (o == null)
				return new Nullable<ulong>();
			var type = o.GetType();
			if (type == Int32Type)
				return (Nullable<ulong>)(int)o;
			else if (type == DoubleType)
				return (Nullable<ulong>)(double)o;
			else if (type == Int64Type)
				return (Nullable<ulong>)(long)o;
			else if (type == Int16Type)
				return (Nullable<ulong>)(short)o;
			else if (type == UInt32Type)
				return (Nullable<ulong>)(uint)o;
			else if (type == UInt64Type)
				return (Nullable<ulong>)(ulong)o;
			else if (type == UInt16Type)
				return (Nullable<ulong>)(ushort)o;
			else if (type == SByteType)
				return (Nullable<ulong>)(sbyte)o;
			else if (type == ByteType)
				return (Nullable<ulong>)(byte)o;
			else if (type == SingleType)
				return (Nullable<ulong>)(float)o;
			else if (type == CharType)
				return (Nullable<ulong>)(char)o;
			else if (type == DecimalType)
				return (Nullable<ulong>)(decimal)o;
			else if (type.IsEnum)
				return (Nullable<ulong>)ExplicitCastEnumToUInt64(o);
			else if (type == NullableInt32Type)
				return (Nullable<ulong>)(Nullable<int>)o;
			else if (type == NullableDoubleType)
				return (Nullable<ulong>)(Nullable<double>)o;
			else if (type == NullableInt64Type)
				return (Nullable<ulong>)(Nullable<long>)o;
			else if (type == NullableInt16Type)
				return (Nullable<ulong>)(Nullable<short>)o;
			else if (type == NullableUInt32Type)
				return (Nullable<ulong>)(Nullable<uint>)o;
			else if (type == NullableUInt64Type)
				return (Nullable<ulong>)(Nullable<ulong>)o;
			else if (type == NullableUInt16Type)
				return (Nullable<ulong>)(Nullable<ushort>)o;
			else if (type == NullableSByteType)
				return (Nullable<ulong>)(Nullable<sbyte>)o;
			else if (type == NullableByteType)
				return (Nullable<ulong>)(Nullable<byte>)o;
			else if (type == NullableSingleType)
				return (Nullable<ulong>)(Nullable<float>)o;
			else if (type == NullableCharType)
				return (Nullable<ulong>)(Nullable<char>)o;
			else if (type == NullableDecimalType)
				return (Nullable<ulong>)(Nullable<decimal>)o;
			throw InvalidCast(o, "UInt64");
		}

		/// <summary>指定された型を基にする <see cref="System.Nullable&lt;T&gt;"/> を作成します。</summary>
		/// <param name="type">基になる型を指定します。</param>
		/// <returns>型を基にする <see cref="System.Nullable&lt;T&gt;"/> のインスタンス。</returns>
		public static object NewNullableInstance(Type type)
		{
			if (type == Int32Type)
				return new Nullable<int>();
			else if (type == DoubleType)
				return new Nullable<double>();
			else if (type == BooleanType)
				return new Nullable<Boolean>();
			else if (type == Int64Type)
				return new Nullable<long>();
			else if (type == Int16Type)
				return new Nullable<short>();
			else if (type == UInt32Type)
				return new Nullable<uint>();
			else if (type == UInt64Type)
				return new Nullable<ulong>();
			else if (type == UInt16Type)
				return new Nullable<ushort>();
			else if (type == SByteType)
				return new Nullable<sbyte>();
			else if (type == ByteType)
				return new Nullable<byte>();
			else if (type == SingleType)
				return new Nullable<float>();
			else if (type == CharType)
				return new Nullable<char>();
			else if (type == DecimalType)
				return new Nullable<decimal>();
			else
				return NewNullableInstanceSlow(type);
		}

		static byte ExplicitCastEnumToByte(object o)
		{
			Debug.Assert(o is Enum);
			switch (((Enum)o).GetTypeCode())
			{
				case TypeCode.Byte: return (byte)o;
				case TypeCode.SByte: return (byte)(sbyte)o;
				case TypeCode.Int16: return (byte)(short)o;
				case TypeCode.UInt16: return (byte)(ushort)o;
				case TypeCode.Int32: return (byte)(int)o;
				case TypeCode.UInt32: return (byte)(uint)o;
				case TypeCode.Int64: return (byte)(long)o;
				case TypeCode.UInt64: return (byte)(ulong)o;
			}
			throw new InvalidOperationException("Invalid enum");
		}

		static sbyte ExplicitCastEnumToSByte(object o)
		{
			Debug.Assert(o is Enum);
			switch (((Enum)o).GetTypeCode())
			{
				case TypeCode.Byte: return (sbyte)(byte)o;
				case TypeCode.SByte: return (sbyte)o;
				case TypeCode.Int16: return (sbyte)(short)o;
				case TypeCode.UInt16: return (sbyte)(ushort)o;
				case TypeCode.Int32: return (sbyte)(int)o;
				case TypeCode.UInt32: return (sbyte)(uint)o;
				case TypeCode.Int64: return (sbyte)(long)o;
				case TypeCode.UInt64: return (sbyte)(ulong)o;
			}
			throw new InvalidOperationException("Invalid enum");
		}

		static short ExplicitCastEnumToInt16(object o)
		{
			Debug.Assert(o is Enum);
			switch (((Enum)o).GetTypeCode())
			{
				case TypeCode.Byte: return (short)(byte)o;
				case TypeCode.SByte: return (short)(sbyte)o;
				case TypeCode.Int16: return (short)o;
				case TypeCode.UInt16: return (short)(ushort)o;
				case TypeCode.Int32: return (short)(int)o;
				case TypeCode.UInt32: return (short)(uint)o;
				case TypeCode.Int64: return (short)(long)o;
				case TypeCode.UInt64: return (short)(ulong)o;
			}
			throw new InvalidOperationException("Invalid enum");
		}

		static ushort ExplicitCastEnumToUInt16(object o)
		{
			Debug.Assert(o is Enum);
			switch (((Enum)o).GetTypeCode())
			{
				case TypeCode.Byte: return (ushort)(byte)o;
				case TypeCode.SByte: return (ushort)(sbyte)o;
				case TypeCode.Int16: return (ushort)(short)o;
				case TypeCode.UInt16: return (ushort)o;
				case TypeCode.Int32: return (ushort)(int)o;
				case TypeCode.UInt32: return (ushort)(uint)o;
				case TypeCode.Int64: return (ushort)(long)o;
				case TypeCode.UInt64: return (ushort)(ulong)o;
			}
			throw new InvalidOperationException("Invalid enum");
		}

		static int ExplicitCastEnumToInt32(object o)
		{
			Debug.Assert(o is Enum);
			switch (((Enum)o).GetTypeCode())
			{
				case TypeCode.Byte: return (int)(byte)o;
				case TypeCode.SByte: return (int)(sbyte)o;
				case TypeCode.Int16: return (int)(short)o;
				case TypeCode.UInt16: return (int)(ushort)o;
				case TypeCode.Int32: return (int)o;
				case TypeCode.UInt32: return (int)(uint)o;
				case TypeCode.Int64: return (int)(long)o;
				case TypeCode.UInt64: return (int)(ulong)o;
			}
			throw new InvalidOperationException("Invalid enum");
		}

		static uint ExplicitCastEnumToUInt32(object o)
		{
			Debug.Assert(o is Enum);
			switch (((Enum)o).GetTypeCode())
			{
				case TypeCode.Byte: return (uint)(byte)o;
				case TypeCode.SByte: return (uint)(sbyte)o;
				case TypeCode.Int16: return (uint)(short)o;
				case TypeCode.UInt16: return (uint)(ushort)o;
				case TypeCode.Int32: return (uint)(int)o;
				case TypeCode.UInt32: return (uint)o;
				case TypeCode.Int64: return (uint)(long)o;
				case TypeCode.UInt64: return (uint)(ulong)o;
			}
			throw new InvalidOperationException("Invalid enum");
		}

		static long ExplicitCastEnumToInt64(object o)
		{
			Debug.Assert(o is Enum);
			switch (((Enum)o).GetTypeCode())
			{
				case TypeCode.Byte: return (long)(byte)o;
				case TypeCode.SByte: return (long)(sbyte)o;
				case TypeCode.Int16: return (long)(short)o;
				case TypeCode.UInt16: return (long)(ushort)o;
				case TypeCode.Int32: return (long)(int)o;
				case TypeCode.UInt32: return (long)(uint)o;
				case TypeCode.Int64: return (long)o;
				case TypeCode.UInt64: return (long)(ulong)o;
			}
			throw new InvalidOperationException("Invalid enum");
		}

		static ulong ExplicitCastEnumToUInt64(object o)
		{
			Debug.Assert(o is Enum);
			switch (((Enum)o).GetTypeCode())
			{
				case TypeCode.Byte: return (ulong)(byte)o;
				case TypeCode.SByte: return (ulong)(sbyte)o;
				case TypeCode.Int16: return (ulong)(short)o;
				case TypeCode.UInt16: return (ulong)(ushort)o;
				case TypeCode.Int32: return (ulong)(int)o;
				case TypeCode.UInt32: return (ulong)(uint)o;
				case TypeCode.Int64: return (ulong)(long)o;
				case TypeCode.UInt64: return (ulong)o;
			}
			throw new InvalidOperationException("Invalid enum");
		}

		static readonly Type BooleanType = typeof(bool);
		static readonly Type ByteType = typeof(byte);
		static readonly Type CharType = typeof(char);
		static readonly Type DecimalType = typeof(decimal);
		static readonly Type DoubleType = typeof(double);
		static readonly Type Int16Type = typeof(short);
		static readonly Type Int32Type = typeof(int);
		static readonly Type Int64Type = typeof(long);
		static readonly Type SByteType = typeof(sbyte);
		static readonly Type SingleType = typeof(float);
		static readonly Type UInt16Type = typeof(ushort);
		static readonly Type UInt32Type = typeof(uint);
		static readonly Type UInt64Type = typeof(ulong);
		static readonly Type NullableBooleanType = typeof(Nullable<bool>);
		static readonly Type NullableByteType = typeof(Nullable<byte>);
		static readonly Type NullableCharType = typeof(Nullable<char>);
		static readonly Type NullableDecimalType = typeof(Nullable<decimal>);
		static readonly Type NullableDoubleType = typeof(Nullable<double>);
		static readonly Type NullableInt16Type = typeof(Nullable<short>);
		static readonly Type NullableInt32Type = typeof(Nullable<int>);
		static readonly Type NullableInt64Type = typeof(Nullable<long>);
		static readonly Type NullableSByteType = typeof(Nullable<sbyte>);
		static readonly Type NullableSingleType = typeof(Nullable<float>);
		static readonly Type NullableUInt16Type = typeof(Nullable<ushort>);
		static readonly Type NullableUInt32Type = typeof(Nullable<uint>);
		static readonly Type NullableUInt64Type = typeof(Nullable<ulong>);
		static readonly Type NullableType = typeof(Nullable<>);
	}
}
