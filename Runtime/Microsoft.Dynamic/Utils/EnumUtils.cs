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

namespace Microsoft.Scripting.Utils
{
	/// <summary>列挙体メンバ同士の演算に関するメソッドを公開します。</summary>
	public static class EnumUtils
	{
		/// <summary>指定された 2 つの列挙体メンバに対してビットごとの OR 演算を実行し結果を 1 番目のオペランドの型に変換します。</summary>
		/// <param name="self">ビットごとの OR 演算を実行する 1 番目のオペランドを指定します。</param>
		/// <param name="other">ビットごとの OR 演算を実行する 2 番目のオペランドを指定します。</param>
		/// <returns>2 つの列挙体メンバのビットごとの OR 演算の結果を 1 番目のオペランドの型に変換した値。</returns>
		public static object BitwiseOr(object self, object other)
		{
			if (self is Enum && other is Enum)
			{
				var selfType = self.GetType();
				var otherType = other.GetType();
				if (selfType == otherType)
				{
					var underType = Enum.GetUnderlyingType(selfType);
					if (underType == typeof(int))
						return Enum.ToObject(selfType, (int)self | (int)other);
					else if (underType == typeof(long))
						return Enum.ToObject(selfType, (long)self | (long)other);
					else if (underType == typeof(short))
						return Enum.ToObject(selfType, (short)self | (short)other);
					else if (underType == typeof(byte))
						return Enum.ToObject(selfType, (byte)self | (byte)other);
					else if (underType == typeof(sbyte))
						return Enum.ToObject(selfType, (sbyte)self | (sbyte)other);
					else if (underType == typeof(uint))
						return Enum.ToObject(selfType, (uint)self | (uint)other);
					else if (underType == typeof(ulong))
						return Enum.ToObject(selfType, (ulong)self | (ulong)other);
					else if (underType == typeof(ushort))
						return Enum.ToObject(selfType, (ushort)self | (ushort)other);
					else
						throw Assert.Unreachable;
				}
			}
			return null;
		}

		/// <summary>指定された 2 つの列挙体メンバに対してビットごとの AND 演算を実行し結果を 1 番目のオペランドの型に変換します。</summary>
		/// <param name="self">ビットごとの AND 演算を実行する 1 番目のオペランドを指定します。</param>
		/// <param name="other">ビットごとの AND 演算を実行する 2 番目のオペランドを指定します。</param>
		/// <returns>2 つの列挙体メンバのビットごとの AND 演算の結果を 1 番目のオペランドの型に変換した値。</returns>
		public static object BitwiseAnd(object self, object other)
		{
			if (self is Enum && other is Enum)
			{
				var selfType = self.GetType();
				var otherType = other.GetType();
				if (selfType == otherType)
				{
					var underType = Enum.GetUnderlyingType(selfType);
					if (underType == typeof(int))
						return Enum.ToObject(selfType, (int)self & (int)other);
					else if (underType == typeof(long))
						return Enum.ToObject(selfType, (long)self & (long)other);
					else if (underType == typeof(short))
						return Enum.ToObject(selfType, (short)self & (short)other);
					else if (underType == typeof(byte))
						return Enum.ToObject(selfType, (byte)self & (byte)other);
					else if (underType == typeof(sbyte))
						return Enum.ToObject(selfType, (sbyte)self & (sbyte)other);
					else if (underType == typeof(uint))
						return Enum.ToObject(selfType, (uint)self & (uint)other);
					else if (underType == typeof(ulong))
						return Enum.ToObject(selfType, (ulong)self & (ulong)other);
					else if (underType == typeof(ushort))
						return Enum.ToObject(selfType, (ushort)self & (ushort)other);
					else
						throw Assert.Unreachable;
				}
			}
			return null;
		}

		/// <summary>指定された 2 つの列挙体メンバに対してビットごとの XOR 演算を実行し結果を 1 番目のオペランドの型に変換します。</summary>
		/// <param name="self">ビットごとの XOR 演算を実行する 1 番目のオペランドを指定します。</param>
		/// <param name="other">ビットごとの XOR 演算を実行する 2 番目のオペランドを指定します。</param>
		/// <returns>2 つの列挙体メンバのビットごとの XOR 演算の結果を 1 番目のオペランドの型に変換した値。</returns>
		public static object ExclusiveOr(object self, object other)
		{
			if (self is Enum && other is Enum)
			{
				var selfType = self.GetType();
				var otherType = other.GetType();
				if (selfType == otherType)
				{
					var underType = Enum.GetUnderlyingType(selfType);
					if (underType == typeof(int))
						return Enum.ToObject(selfType, (int)self ^ (int)other);
					else if (underType == typeof(long))
						return Enum.ToObject(selfType, (long)self ^ (long)other);
					else if (underType == typeof(short))
						return Enum.ToObject(selfType, (short)self ^ (short)other);
					else if (underType == typeof(byte))
						return Enum.ToObject(selfType, (byte)self ^ (byte)other);
					else if (underType == typeof(sbyte))
						return Enum.ToObject(selfType, (sbyte)self ^ (sbyte)other);
					else if (underType == typeof(uint))
						return Enum.ToObject(selfType, (uint)self ^ (uint)other);
					else if (underType == typeof(ulong))
						return Enum.ToObject(selfType, (ulong)self ^ (ulong)other);
					else if (underType == typeof(ushort))
						return Enum.ToObject(selfType, (ushort)self ^ (ushort)other);
					else
						throw Assert.Unreachable;
				}
			}
			return null;
		}

		/// <summary>指定された列挙体メンバに対して 1 の補数を求め結果をその列挙型に変換します。</summary>
		/// <param name="self">1 の補数を求める列挙体メンバを指定します。</param>
		/// <returns>1 の補数の値を列挙型に変換した値。</returns>
		public static object OnesComplement(object self)
		{
			if (self is Enum)
			{
				var selfType = self.GetType();
				var underType = Enum.GetUnderlyingType(selfType);
				if (underType == typeof(int))
					return Enum.ToObject(selfType, ~(int)self);
				else if (underType == typeof(long))
					return Enum.ToObject(selfType, ~(long)self);
				else if (underType == typeof(short))
					return Enum.ToObject(selfType, ~(short)self);
				else if (underType == typeof(byte))
					return Enum.ToObject(selfType, ~(byte)self);
				else if (underType == typeof(sbyte))
					return Enum.ToObject(selfType, ~(sbyte)self);
				else if (underType == typeof(uint))
					return Enum.ToObject(selfType, ~(uint)self);
				else if (underType == typeof(ulong))
					return Enum.ToObject(selfType, ~(ulong)self);
				else if (underType == typeof(ushort))
					return Enum.ToObject(selfType, ~(ushort)self);
				else
					throw Assert.Unreachable;
			}
			return null;
		}
	}
}
