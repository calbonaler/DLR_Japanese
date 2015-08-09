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
using System.Diagnostics.CodeAnalysis;
using Microsoft.Contracts;
using Microsoft.Scripting.Utils;
using BigInt = System.Numerics.BigInteger;

namespace Microsoft.Scripting.Numerics
{
	/// <summary>
	/// arbitrary precision integers
	/// </summary>
	[Serializable]
	public sealed class BigInteger : IFormattable, IComparable, IEquatable<BigInteger>, IComparable<BigInteger>
	{
		public BigInteger(byte[] value)
		{
			ContractUtils.RequiresNotNull(value, "value");
			Value = new BigInt(value);
		}
		public BigInteger(BigInt value) { Value = value; }
		public BigInteger(BigInteger value)
		{
			ContractUtils.RequiresNotNull(value, "value");
			Value = value.Value;
		}
		public BigInteger(int sign, byte[] value)
		{
			ContractUtils.RequiresNotNull(value, "value");
			ContractUtils.Requires(sign >= -1 && sign <= +1, "sign");
			Value = new BigInt(value);
			if (sign < 0)
				Value = -Value;
		}
		[CLSCompliant(false)]
		public BigInteger(int sign, uint[] value)
		{
			ContractUtils.RequiresNotNull(value, "value");
			ContractUtils.Requires(sign >= -1 && sign <= +1, "sign");
			var length = value.Length;
			while (length > 0 && value[length - 1] == 0)
				length--;
			ContractUtils.Requires(length == 0 || sign != 0, "sign");
			if (length == 0)
			{
				Value = 0;
				return;
			}
			bool highest = (value[length - 1] & 0x80000000) != 0;
			byte[] bytes = new byte[length * 4 + (highest ? 1 : 0)];
			int j = 0;
			for (int i = 0; i < length; i++)
			{
				bytes[j++] = (byte)(value[i] & 0xff);
				bytes[j++] = (byte)((value[i] >> 8) & 0xff);
				bytes[j++] = (byte)((value[i] >> 16) & 0xff);
				bytes[j++] = (byte)((value[i] >> 24) & 0xff);
			}
			Value = new BigInt(bytes);
			if (sign < 0)
				Value = -Value;
		}
		
		public static BigInteger Create(byte[] value) { return new BigInteger(value); }
		public static BigInteger Create(decimal value) { return new BigInteger(new BigInt(value)); }
		public static BigInteger Create(double value) { return new BigInteger(new BigInt(value)); }
		public static BigInteger Create(int value) { return new BigInteger(new BigInt(value)); }
		public static BigInteger Create(long value) { return new BigInteger(new BigInt(value)); }
		[CLSCompliant(false)]
		public static BigInteger Create(uint value) { return new BigInteger(new BigInt(value)); }
		[CLSCompliant(false)]
		public static BigInteger Create(ulong value) { return new BigInteger(new BigInt(value)); }

		public static BigInteger operator -(BigInteger value) { return new BigInteger(-value.Value); }
		public static BigInteger operator -(BigInteger left, BigInteger right) { return new BigInteger(left.Value - right.Value); }
		public static bool operator !=(BigInteger left, BigInteger right) { return Compare(left, right) != 0; }
		public static bool operator !=(BigInteger left, int right) { return !(left == right); }
		public static bool operator !=(int left, BigInteger right) { return !(left == right); }
		public static bool operator !=(BigInteger left, double right) { return !(left == right); }
		public static bool operator !=(double left, BigInteger right) { return !(left == right); }
		public static BigInteger operator %(BigInteger dividend, BigInteger divisor) { return new BigInteger(dividend.Value % divisor.Value); }
		public static BigInteger operator &(BigInteger left, BigInteger right) { return new BigInteger(left.Value & right.Value); }
		public static BigInteger operator *(BigInteger left, BigInteger right) { return new BigInteger(left.Value * right.Value); }
		public static BigInteger operator /(BigInteger dividend, BigInteger divisor) { return new BigInteger(dividend.Value / divisor.Value); }
		public static BigInteger operator ^(BigInteger left, BigInteger right) { return new BigInteger(left.Value ^ right.Value); }
		public static BigInteger operator |(BigInteger left, BigInteger right) { return new BigInteger(left.Value | right.Value); }
		public static BigInteger operator ~(BigInteger value) { return new BigInteger(~value.Value); }
		public static BigInteger operator +(BigInteger value) { return new BigInteger(value.Value); }
		public static BigInteger operator +(BigInteger left, BigInteger right) { return new BigInteger(left.Value + right.Value); }
		public static bool operator <(BigInteger left, BigInteger right) { return Compare(left, right) < 0; }
		public static BigInteger operator <<(BigInteger value, int shift) { return new BigInteger(value.Value << shift); }
		public static bool operator <=(BigInteger left, BigInteger right) { return Compare(left, right) <= 0; }
		public static bool operator ==(BigInteger left, BigInteger right) { return Compare(left, right) == 0; }
		public static bool operator ==(BigInteger left, int right) { return !ReferenceEquals(left, null) && left.Value == right; }
		public static bool operator ==(int left, BigInteger right) { return right == left; }
		public static bool operator ==(BigInteger left, double right) { return !ReferenceEquals(left, null) && right % 1 == 0 && left.Value == (BigInt)right; }
		public static bool operator ==(double left, BigInteger right) { return right == left; }
		public static bool operator >(BigInteger left, BigInteger right) { return Compare(left, right) > 0; }
		public static bool operator >=(BigInteger left, BigInteger right) { return Compare(left, right) >= 0; }
		public static BigInteger operator >>(BigInteger value, int shift) { return new BigInteger(value.Value >> shift); }
		[CLSCompliant(false)]
		public static explicit operator sbyte(BigInteger value) { return (sbyte)value.Value; }
		public static explicit operator decimal(BigInteger value) { return (decimal)value.Value; }
		public static explicit operator double(BigInteger value) { return (double)value.Value; }
		public static explicit operator float(BigInteger value) { return (float)value.Value; }
		[CLSCompliant(false)]
		public static explicit operator ulong(BigInteger value) { return (ulong)value.Value; }
		public static explicit operator long(BigInteger value) { return (long)value.Value; }
		[CLSCompliant(false)]
		public static explicit operator uint(BigInteger value) { return (uint)value.Value; }
		public static explicit operator int(BigInteger value) { return (int)value.Value; }
		public static explicit operator short(BigInteger value) { return (short)value.Value; }
		[CLSCompliant(false)]
		public static explicit operator ushort(BigInteger value) { return (ushort)value.Value; }
		public static explicit operator byte(BigInteger value) { return (byte)value.Value; }
		public static implicit operator BigInt(BigInteger value) { return value.Value; }
		public static explicit operator BigInteger(decimal value) { return new BigInteger((BigInt)value); }
		public static explicit operator BigInteger(double value) { return new BigInteger((BigInt)value); }
		public static explicit operator BigInteger(float value) { return new BigInteger((BigInt)value); }
		public static implicit operator BigInteger(byte value) { return new BigInteger((BigInt)value); }
		public static implicit operator BigInteger(int value) { return new BigInteger((BigInt)value); }
		public static implicit operator BigInteger(long value) { return new BigInteger((BigInt)value); }
		[CLSCompliant(false)]
		public static implicit operator BigInteger(sbyte value) { return new BigInteger((BigInt)value); }
		public static implicit operator BigInteger(short value) { return new BigInteger((BigInt)value); }
		[CLSCompliant(false)]
		public static implicit operator BigInteger(uint value) { return new BigInteger((BigInt)value); }
		[CLSCompliant(false)]
		public static implicit operator BigInteger(ulong value) { return new BigInteger((BigInt)value); }
		[CLSCompliant(false)]
		public static implicit operator BigInteger(ushort value) { return new BigInteger((BigInt)value); }
		public static implicit operator BigInteger(BigInt value) { return new BigInteger(value); }

		[SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly BigInteger One = new BigInteger(BigInt.One);
		internal readonly BigInt Value;
		[SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly BigInteger Zero = new BigInteger(BigInt.Zero);

		public bool IsPowerOfTwo { get { return Value.IsPowerOfTwo; } }
		/// <summary>
		/// Return the sign of this BigInteger: -1, 0, or 1.
		/// </summary>
		public int Sign { get { return Value.Sign; } }

		public BigInteger Abs() { return new BigInteger(BigInt.Abs(Value)); }
		public static BigInteger Add(BigInteger left, BigInteger right) { return left + right; }
		public bool AsInt32(out int ret) { return Value.AsInt32(out ret); }
		public bool AsInt64(out long ret) { return Value.AsInt64(out ret); }
		[CLSCompliant(false)]
		public bool AsUInt32(out uint ret) { return Value.AsUInt32(out ret); }
		[CLSCompliant(false)]
		public bool AsUInt64(out ulong ret) { return Value.AsUInt64(out ret); }
		public static int Compare(BigInteger left, BigInteger right)
		{
			if (ReferenceEquals(left, right))
				return 0;
			if (ReferenceEquals(left, null))
				return -1;
			if (ReferenceEquals(right, null))
				return 1;
			return BigInt.Compare(left.Value, right.Value);
		}
		public int CompareTo(BigInteger other) { return Compare(this, other); }
		public int CompareTo(object obj)
		{
			if (obj is BigInteger)
				return Compare(this, (BigInteger)obj);
			throw new ArgumentException("BigInteger å^Ç™ó\ä˙Ç≥ÇÍÇ‹ÇµÇΩÅB");
		}
		public static BigInteger Divide(BigInteger dividend, BigInteger divisor) { return dividend / divisor; }
		public static BigInteger DivRem(BigInteger dividend, BigInteger divisor, out BigInteger remainder)
		{
			BigInt rem;
			var result = BigInt.DivRem(dividend.Value, divisor.Value, out rem);
			remainder = new BigInteger(rem);
			return new BigInteger(result);
		}
		public bool Equals(BigInteger other) { return this == other; }
		public override bool Equals(object obj) { return Equals(obj as BigInteger); }
		public int GetBitCount() { return Value.GetBitCount(); }
		public int GetByteCount() { return Value.GetByteCount(); }
		public override int GetHashCode() { return Value.GetHashCode(); }
		public int GetWordCount() { return Value.GetWordCount(); }
		[CLSCompliant(false)]
		public uint[] GetWords() { return Value.GetWords(); }
		/// <summary>
		/// Calculates the natural logarithm of the BigInteger.
		/// </summary>
		public double Log() { return BigInt.Log(Value); }
		public double Log(double baseValue) { return BigInt.Log(Value, baseValue); }
		/// <summary>
		/// Calculates log base 10 of a BigInteger.
		/// </summary>
		public double Log10() { return BigInt.Log10(Value); }
		public BigInteger ModPow(BigInteger exponent, BigInteger modulus) { return new BigInteger(BigInt.ModPow(Value, exponent.Value, modulus.Value)); }
		public BigInteger ModPow(int exponent, BigInteger modulus) { return new BigInteger(BigInt.ModPow(Value, exponent, modulus.Value)); }
		public static BigInteger Multiply(BigInteger left, BigInteger right) { return left * right; }
		public static BigInteger Negate(BigInteger value) { return -value; }
		public static BigInteger Parse(string value) { return new BigInteger(BigInt.Parse(value)); }
		public BigInteger Power(int exponent) { return new BigInteger(BigInt.Pow(Value, exponent)); }
		public static BigInteger Remainder(BigInteger dividend, BigInteger divisor) { return dividend % divisor; }
		public static BigInteger Subtract(BigInteger left, BigInteger right) { return left - right; }
		/// <summary>
		/// Return the value of this BigInteger as a little-endian twos-complement
		/// byte array, using the fewest number of bytes possible. If the value is zero,
		/// return an array of one byte whose element is 0x00.
		/// </summary>
		public byte[] ToByteArray() { return Value.ToByteArray(); }
		public override string ToString() { return Value.ToString(); }
		public string ToString(int radix) { return Value.ToString(radix); }
		[Confined]
		public string ToString(IFormatProvider provider) { return Value.ToString(provider); }
		public string ToString(string format, IFormatProvider formatProvider) { return Value.ToString(format, formatProvider); }
		public static bool TryParse(string value, out BigInteger result)
		{
			BigInt res;
			var succeeded = BigInt.TryParse(value, out res);
			result = new BigInteger(res);
			return succeeded;
		}
	}
}
