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
using System.Text;
using Microsoft.Scripting.Numerics;
using BigInt = System.Numerics.BigInteger;

namespace Microsoft.Scripting.Utils
{
	/// <summary>数学と任意長整数に関するユーティリティ メソッドを公開します。</summary>
	public static class MathUtils
	{
		/// <summary>2 つの 32 ビット符号付き整数の商を計算して、結果を負の無限大に丸めます。</summary>
		/// <param name="dividend">被除数を指定します。</param>
		/// <param name="divisor">除数を指定します。</param>
		/// <returns>2 つの数値の商を負の無限大に丸めた数。</returns>
		/// <exception cref="DivideByZeroException"><paramref name="divisor"/> は 0 です。</exception>
		/// <remarks>呼び出し元はオーバーフローをチェックする必要があります。</remarks>
		public static int FloorDivideUnchecked(int dividend, int divisor)
		{
			var q = dividend / divisor;
			if (dividend % divisor == 0)
				return q;
			else if (dividend >= 0)
				return divisor > 0 ? q : q - 1;
			else
				return divisor <= 0 ? q : q - 1;
		}

		/// <summary>2 つの 64 ビット符号付き整数の商を計算して、結果を負の無限大に丸めます。</summary>
		/// <param name="dividend">被除数を指定します。</param>
		/// <param name="divisor">除数を指定します。</param>
		/// <returns>2 つの数値の商を負の無限大に丸めた数。</returns>
		/// <exception cref="DivideByZeroException"><paramref name="divisor"/> は 0 です。</exception>
		/// <remarks>呼び出し元はオーバーフローをチェックする必要があります。</remarks>
		public static long FloorDivideUnchecked(long dividend, long divisor)
		{
			var q = dividend / divisor;
			if (dividend % divisor == 0)
				return q;
			else if (dividend >= 0)
				return divisor > 0 ? q : q - 1;
			else
				return divisor <= 0 ? q : q - 1;
		}

		/// <summary>2 つの 32 ビット符号付き整数の丸め除算の剰余を計算します。</summary>
		/// <param name="dividend">被除数を指定します。</param>
		/// <param name="divisor">除数を指定します。</param>
		/// <returns>指定された数値の丸め除算の剰余。</returns>
		/// <exception cref="DivideByZeroException"><paramref name="divisor"/> は 0 です。</exception>
		public static int FloorRemainder(int dividend, int divisor)
		{
			if (divisor == -1)
				return 0;
			var r = dividend % divisor;
			if (r == 0)
				return 0;
			else if (dividend >= 0)
				return divisor > 0 ? r : r + divisor;
			else
				return divisor <= 0 ? r : r + divisor;
		}

		/// <summary>2 つの 64 ビット符号付き整数の丸め除算の剰余を計算します。</summary>
		/// <param name="dividend">被除数を指定します。</param>
		/// <param name="divisor">除数を指定します。</param>
		/// <returns>指定された数値の丸め除算の剰余。</returns>
		/// <exception cref="DivideByZeroException"><paramref name="divisor"/> は 0 です。</exception>
		public static long FloorRemainder(long dividend, long divisor)
		{
			if (divisor == -1)
				return 0;
			var r = dividend % divisor;
			if (r == 0)
				return 0;
			else if (dividend >= 0)
				return divisor > 0 ? r : r + divisor;
			else
				return divisor <= 0 ? r : r + divisor;
		}

		static readonly double[] _RoundPowersOfTens = new double[] { 1E0, 1E1, 1E2, 1E3, 1E4, 1E5, 1E6, 1E7, 1E8, 1E9, 1E10, 1E11, 1E12, 1E13, 1E14, 1E15 };

		/// <summary>倍精度浮動小数点数を指定した小数点部の桁数に丸めます。数値が 2 つの数値の中間に位置するときは 0 から遠い方に丸められます。</summary>
		/// <param name="value">丸め対象の倍精度浮動小数点数を指定します。</param>
		/// <param name="precision">丸めを行う小数点部の桁数を指定します。負の値を指定した場合、丸めは整数部の桁数で行われます。</param>
		/// <returns><paramref name="value"/> を <paramref name="precision"/> によって表される桁で丸めた数値。</returns>
		public static double RoundAwayFromZero(double value, int precision)
		{
			if (precision < 0)
			{
				var num = precision > -16 ? _RoundPowersOfTens[-precision] : Math.Pow(10, -precision);
				return Math.Round(value / num, MidpointRounding.AwayFromZero) * num;
			}
			else
			{
				var num = precision < 16 ? _RoundPowersOfTens[precision] : Math.Pow(10, precision);
				return Math.Round(value * num, MidpointRounding.AwayFromZero) / num;
			}
		}

		/// <summary>倍精度浮動小数点数が -0 であるかどうかを判断します。</summary>
		/// <param name="value">-0 かどうかを判断する倍精度浮動小数点数を指定します。</param>
		/// <returns>倍精度浮動小数点数が -0 である場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool IsNegativeZero(double value) { return value == 0.0 && 1.0 / value < 0; }

		/// <summary>指定された 2 つの数値を底辺および高さとする直角三角形の斜辺を計算します。</summary>
		/// <param name="x">直角三角形の底辺の長さを指定します。</param>
		/// <param name="y">直角三角形の高さを指定します。</param>
		/// <returns>指定された直角三角形の斜辺。<c>Math.Sqrt(x * x + y * y)</c> と等しくなります。</returns>
		public static double Hypot(double x, double y)
		{
			// sqrt(x^2 + y^2) = sqrt(x^2 * (1 + (y^2)/(x^2))) = sqrt(x^2) * sqrt(1 + (y/x)^2) == abs(x) * sqrt(1 + (y/x)^2)
			if (double.IsInfinity(x) || double.IsInfinity(y))
				return double.PositiveInfinity;
			//  First, get abs
			if (x < 0.0)
				x = -x;
			if (y < 0.0)
				y = -y;
			// Obvious cases
			if (x == 0.0)
				return y;
			if (y == 0.0)
				return x;
			// Divide smaller number by bigger number to safeguard the (y/x)*(y/x)
			if (x < y)
			{
				double temp = y;
				y = x;
				x = temp;
			}
			y /= x;
			return x * Math.Sqrt(1 + y * y);
		}

		// Helper for GetRandBits
		static uint GetWord(byte[] bytes, int start, int end)
		{
			uint four = 0;
			var bits = Math.Min(end - start, 32);
			start /= 8;
			for (var shift = 0; bits > 0; start++, bits -= 8, shift += 8)
				four |= (bytes[start] & (bits < 8 ? (1u << bits) - 1u : byte.MaxValue)) << shift;
			return four;
		}

		/// <summary>指定されたビット数の乱数を返します。</summary>
		/// <param name="generator">乱数を生成する乱数ジェネレーターを指定します。</param>
		/// <param name="bits">返される乱数のビット数を指定します。</param>
		/// <returns>指定されたビット数の乱数を表す任意長整数。</returns>
		/// <exception cref="ArgumentNullException"><paramref name="generator"/> は <c>null</c> です。</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="bits"/> は 0 以下です。</exception>
		public static BigInt GetRandBits(this Random generator, int bits)
		{
			ContractUtils.RequiresNotNull(generator, "generator");
			if (bits <= 0)
				throw new ArgumentOutOfRangeException("bits");
			var bytes = new byte[bits / 8 + 1];
			generator.NextBytes(bytes);
			bytes[bytes.Length - 1] = (byte)(bits % 8 == 0 ? 0 : bytes[bytes.Length - 1] & (1 << bits % 8) - 1);
			if (bits <= 32)
				return GetWord(bytes, 0, bits);
			else if (bits <= 64)
				return (ulong)GetWord(bytes, 0, 32) | (ulong)GetWord(bytes, 32, bits) << 32;
			return new BigInt(bytes);
		}

		/// <summary>指定された最大値より小さい 0 以上の乱数を返します。</summary>
		/// <param name="generator">乱数を生成する乱数ジェネレーターを指定します。</param>
		/// <param name="maxValue">生成される乱数の排他的上限値。 <paramref name="maxValue"/> は 0 より大きな値である必要があります。</param>
		/// <returns>0 以上で <paramref name="maxValue"/> 未満の任意長整数。</returns>
		/// <exception cref="ArgumentNullException"><paramref name="generator"/> は <c>null</c> です。</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> が 0 以下です。</exception>
		public static BigInt NextBigInt(this Random generator, BigInt maxValue)
		{
			ContractUtils.RequiresNotNull(generator, "generator");
			if (maxValue.Sign <= 0)
				throw new ArgumentOutOfRangeException("limit");
			var res = BigInt.Zero;
			while (true)
			{
				// if we've run out of significant digits, we can return the total
				if (maxValue.IsZero)
					return res;
				// if we're small enough to fit in an int, do so
				int iLimit;
				if (maxValue.AsInt32(out iLimit))
					return res + generator.Next(iLimit);
				// get the 3 or 4 uppermost bytes that fit into an int
				int hiData;
				byte[] data;
				var index = GetHighestByte(maxValue, out data);
				if (data[index] < 0x80)
				{
					hiData = data[index] << 24;
					data[index--] = 0;
				}
				else
					hiData = 0;
				hiData |= data[index] << 16;
				data[index--] = 0;
				hiData |= data[index] << 8;
				data[index--] = 0;
				hiData |= data[index];
				data[index--] = 0;
				// get a uniform random number for the uppermost portion of the bigint
				byte[] randomData = new byte[index + 2];
				generator.NextBytes(randomData);
				randomData[index + 1] = 0;
				res += new BigInt(randomData);
				res += (BigInt)generator.Next(hiData) << (index + 1) * 8;
				// sum it with a uniform random number for the remainder of the bigint
				maxValue = new BigInt(data);
			}
		}

		/// <summary>指定された最大値より小さい 0 以上の乱数を返します。</summary>
		/// <param name="generator">乱数を生成する乱数ジェネレーターを指定します。</param>
		/// <param name="maxValue">生成される乱数の排他的上限値。 <paramref name="maxValue"/> は 0 より大きな値である必要があります。</param>
		/// <returns>0 以上で <paramref name="maxValue"/> 未満の任意長整数。</returns>
		/// <exception cref="ArgumentNullException"><paramref name="generator"/> は <c>null</c> です。</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> が 0 以下です。</exception>
		public static BigInteger NextBigInteger(this Random generator, BigInteger maxValue) { return new BigInteger(generator.NextBigInt(maxValue.Value)); }

		/// <summary>任意長整数の倍精度浮動小数点数への変換を試みます。</summary>
		/// <param name="value">倍精度浮動小数点数への変換を行う任意長整数を指定します。</param>
		/// <param name="result">変換された倍精度浮動小数点数が返されます。</param>
		/// <returns>任意長整数を倍精度浮動小数点数に変換できた場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool TryToFloat64(this BigInt value, out double result)
		{
			try
			{
				result = (double)value;
				return true;
			}
			catch
			{
				result = 0;
				return false;
			}
		}

		/// <summary>任意長整数の倍精度浮動小数点数への変換を試みます。</summary>
		/// <param name="value">倍精度浮動小数点数への変換を行う任意長整数を指定します。</param>
		/// <param name="result">変換された倍精度浮動小数点数が返されます。</param>
		/// <returns>任意長整数を倍精度浮動小数点数に変換できた場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool TryToFloat64(this BigInteger value, out double result)
		{
			try
			{
				result = (double)value;
				return true;
			}
			catch
			{
				result = 0;
				return false;
			}
		}

		/// <summary>任意長整数の 32 ビット符号付き整数への変換を試みます。</summary>
		/// <param name="value">32 ビット符号付き整数へ変換する任意長整数を指定します。</param>
		/// <param name="result">変換された 32 ビット符号付き整数が返されます。</param>
		/// <returns>任意長整数を 32 ビット符号付き整数へ変換できた場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool AsInt32(this BigInt value, out int result)
		{
			if (value >= int.MinValue && value <= int.MaxValue)
			{
				result = (int)value;
				return true;
			}
			result = 0;
			return false;
		}

		/// <summary>任意長整数の 64 ビット符号付き整数への変換を試みます。</summary>
		/// <param name="value">64 ビット符号付き整数へ変換する任意長整数を指定します。</param>
		/// <param name="result">変換された 64 ビット符号付き整数が返されます。</param>
		/// <returns>任意長整数を 64 ビット符号付き整数へ変換できた場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool AsInt64(this BigInt value, out long result)
		{
			if (value >= long.MinValue && value <= long.MaxValue)
			{
				result = (long)value;
				return true;
			}
			result = 0;
			return false;
		}

		/// <summary>任意長整数の 32 ビット符号なし整数への変換を試みます。</summary>
		/// <param name="value">32 ビット符号なし整数へ変換する任意長整数を指定します。</param>
		/// <param name="result">変換された 32 ビット符号なし整数が返されます。</param>
		/// <returns>任意長整数を 32 ビット符号なし整数へ変換できた場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		[CLSCompliant(false)]
		public static bool AsUInt32(this BigInt value, out uint result)
		{
			if (value >= uint.MinValue && value <= uint.MaxValue)
			{
				result = (uint)value;
				return true;
			}
			result = 0;
			return false;
		}

		/// <summary>任意長整数の 64 ビット符号なし整数への変換を試みます。</summary>
		/// <param name="value">64 ビット符号なし整数へ変換する任意長整数を指定します。</param>
		/// <param name="result">変換された 64 ビット符号なし整数が返されます。</param>
		/// <returns>任意長整数を 64 ビット符号なし整数へ変換できた場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		[CLSCompliant(false)]
		public static bool AsUInt64(this BigInt value, out ulong result)
		{
			if (value >= ulong.MinValue && value <= ulong.MaxValue)
			{
				result = (ulong)value;
				return true;
			}
			result = 0;
			return false;
		}

		/// <summary>任意長整数の絶対値を返します。</summary>
		/// <param name="value">絶対値を取得する任意長整数を指定します。</param>
		/// <returns>任意長整数に対する絶対値。</returns>
		public static BigInt Abs(this BigInt value) { return BigInt.Abs(value); }

		/// <summary>任意長整数の自然対数を返します。</summary>
		/// <param name="value">自然対数を取得する任意長整数を指定します。</param>
		/// <returns>任意長整数に対する自然対数。</returns>
		/// <exception cref="ArgumentOutOfRangeException">自然対数が <see cref="System.Double"/> で表現できる範囲を超えています。</exception>
		public static double Log(this BigInt value) { return BigInt.Log(value); }

		/// <summary>任意長整数の指定された底の対数を返します。</summary>
		/// <param name="value">対数を取得する任意長整数を指定します。</param>
		/// <param name="baseValue">取得する対数の底を指定します。</param>
		/// <returns>任意長整数に対する対数。</returns>
		/// <exception cref="ArgumentOutOfRangeException">対数が <see cref="System.Double"/> で表現できる範囲を超えています。</exception>
		public static double Log(this BigInt value, double baseValue) { return BigInt.Log(value, baseValue); }

		/// <summary>任意長整数の 10 を底とする対数を返します。</summary>
		/// <param name="value">10 を底とする対数を取得する任意長整数を指定します。</param>
		/// <returns>任意長整数に対する 10 を底とする対数。</returns>
		/// <exception cref="ArgumentOutOfRangeException">10 を底とする対数が <see cref="System.Double"/> で表現できる範囲を超えています。</exception>
		public static double Log10(this BigInt value) { return BigInt.Log10(value); }

		/// <summary>任意長整数の指定された数を指数とする累乗を返します。</summary>
		/// <param name="value">累乗を取得する任意長整数を指定します。</param>
		/// <param name="exponent">任意長整数を累乗する指数を指定します。</param>
		/// <returns>任意長整数の累乗。</returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="exponent"/> の値が 0 未満です。</exception>
		public static BigInt Power(this BigInt value, int exponent) { return BigInt.Pow(value, exponent); }

		/// <summary>任意長整数を累乗して別の数値で割った剰余を返します。</summary>
		/// <param name="value"><paramref name="exponent"/> で累乗する任意長整数を指定します。</param>
		/// <param name="exponent">任意長整数を累乗する任意長整数を指定します。</param>
		/// <param name="modulus">累乗された値を割る任意長整数を指定します。</param>
		/// <returns><paramref name="value"/> ^ <paramref name="exponent"/> を <paramref name="modulus"/> で割った結果生じた剰余。</returns>
		/// <exception cref="DivideByZeroException"><paramref name="modulus"/> が 0 です。</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="exponent"/> が 0 未満です。</exception>
		public static BigInt ModPow(this BigInt value, BigInt exponent, BigInt modulus) { return BigInt.ModPow(value, exponent, modulus); }

		// generated by scripts/radix_generator.py
		static readonly uint[] maxCharsPerDigit = { 0, 0, 31, 20, 15, 13, 12, 11, 10, 10, 9, 9, 8, 8, 8, 8, 7, 7, 7, 7, 7, 7, 7, 7, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6 };
		static readonly uint[] groupRadixValues = { 0, 0, 2147483648, 3486784401, 1073741824, 1220703125, 2176782336, 1977326743, 1073741824, 3486784401, 1000000000, 2357947691, 429981696, 815730721, 1475789056, 2562890625, 268435456, 410338673, 612220032, 893871739, 1280000000, 1801088541, 2494357888, 3404825447, 191102976, 244140625, 308915776, 387420489, 481890304, 594823321, 729000000, 887503681, 1073741824, 1291467969, 1544804416, 1838265625, 2176782336 };

		/// <summary>任意長整数を指定された基数で表現した文字列を返します。</summary>
		/// <param name="value">基数による文字列表現を取得する任意長整数を指定します。</param>
		/// <param name="radix">任意長整数を表現する基数を指定します。</param>
		/// <returns>任意長整数を指定された基数によって表す文字列。</returns>
		/// <exception cref="ArgumentOutOfRangeException">基数が 2 未満または 36 より大きな数です。</exception>
		public static string ToString(this BigInt value, int radix)
		{
			if (radix < 2 || radix > 36)
				throw ExceptionUtils.MakeArgumentOutOfRangeException("radix", radix, "radix must be [2, 36]");
			if (value.IsZero)
				return "0";
			var words = value.GetWords();
			var groups = new Stack<uint>();
			for (var length = words.Length; length > 0; )
			{
				ulong rem = 0;
				var seenNonZero = false;
				for (var i = length - 1; i >= 0; i--)
				{
					rem = (rem << 32) | words[i];
					if ((words[i] = (uint)(rem / groupRadixValues[radix])) != 0)
						seenNonZero = true;
					else if (!seenNonZero)
						length--;
					rem %= groupRadixValues[radix];
				}
				groups.Push((uint)rem);
			}
			var result = new StringBuilder();
			if (value.Sign < 0)
				result.Append("-");
			var buffer = new char[maxCharsPerDigit[radix]];
			for (int count = groups.Count; groups.Count > 0; )
			{
				var j = buffer.Length - 1;
				for (var group = groups.Pop(); j >= 0 && (groups.Count != count - 1 || group != 0); j--, group /= (uint)radix)
					buffer[j] = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"[(int)(group % radix)];
				result.Append(buffer, j + 1, buffer.Length - j - 1);
			}
			return result.Length == 0 ? "0" : result.ToString();
		}

		/// <summary>任意長整数を表す <see cref="System.UInt32"/> 値の配列を取得します。この結果は符号を区別しません。</summary>
		/// <param name="value"><see cref="System.UInt32"/> 値の配列を取得する任意長整数を指定します。</param>
		/// <returns>任意長整数を表す <see cref="System.UInt32"/> 値の配列。</returns>
		[CLSCompliant(false)]
		public static uint[] GetWords(this BigInt value)
		{
			if (value.IsZero)
				return new uint[] { 0 };
			byte[] bytes;
			uint[] result = new uint[GetHighestByte(value, out bytes) / 4 + 1];
			for (int i = 0, j = 0; i < bytes.Length && (i <= 0 || i % 4 != 0 || ++j < result.Length); i++)
				result[j] |= (uint)bytes[i] << 8 * i;
			return result;
		}

		/// <summary>任意長整数を表す <see cref="System.UInt32"/> 配列の要素数を返します。</summary>
		/// <param name="value">任意長整数を表す <see cref="System.UInt32"/> 配列の要素数を取得する任意長整数を指定します。</param>
		/// <returns>任意長整数を表す <see cref="System.UInt32"/> 配列の要素数。</returns>
		public static int GetWordCount(this BigInt value)
		{
			byte[] bytes;
			return GetHighestByte(value, out bytes) / 4 + 1;
		}

		/// <summary>任意長整数を表すために必要なバイト数を返します。</summary>
		/// <param name="value">任意長整数を表すために必要なバイト数を取得する任意長整数を指定します。</param>
		/// <returns>任意長整数を表すために必要なバイト数。</returns>
		public static int GetByteCount(this BigInt value)
		{
			byte[] bytes;
			return GetHighestByte(value, out bytes) + 1;
		}

		/// <summary>任意長整数を表すために必要なビット数を返します。</summary>
		/// <param name="self">任意長整数を表すために必要なビット数を取得する任意長整数を指定します。</param>
		/// <returns>任意長整数を表すために必要なビット数。</returns>
		public static int GetBitCount(this BigInt self)
		{
			if (self.IsZero)
				return 1;
			byte[] bytes;
			var index = GetHighestByte(self, out bytes);
			int count = index * 8;
			for (int hiByte = bytes[index]; hiByte > 0; hiByte >>= 1)
				count++;
			return count;
		}

		static int GetHighestByte(BigInt self, out byte[] bytes)
		{
			bytes = BigInt.Abs(self).ToByteArray();
			if (self.IsZero)
				return 0;
			var index = bytes.Length - 1;
			while (bytes[index] == 0)
				index--;
			return index;
		}
	}
}
