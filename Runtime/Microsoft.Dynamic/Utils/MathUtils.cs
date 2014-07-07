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
	/// <summary>���w�ƔC�Ӓ������Ɋւ��郆�[�e�B���e�B ���\�b�h�����J���܂��B</summary>
	public static class MathUtils
	{
		/// <summary>2 �� 32 �r�b�g�����t�������̏����v�Z���āA���ʂ𕉂̖�����Ɋۂ߂܂��B</summary>
		/// <param name="dividend">�폜�����w�肵�܂��B</param>
		/// <param name="divisor">�������w�肵�܂��B</param>
		/// <returns>2 �̐��l�̏��𕉂̖�����Ɋۂ߂����B</returns>
		/// <exception cref="DivideByZeroException"><paramref name="divisor"/> �� 0 �ł��B</exception>
		/// <remarks>�Ăяo�����̓I�[�o�[�t���[���`�F�b�N����K�v������܂��B</remarks>
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

		/// <summary>2 �� 64 �r�b�g�����t�������̏����v�Z���āA���ʂ𕉂̖�����Ɋۂ߂܂��B</summary>
		/// <param name="dividend">�폜�����w�肵�܂��B</param>
		/// <param name="divisor">�������w�肵�܂��B</param>
		/// <returns>2 �̐��l�̏��𕉂̖�����Ɋۂ߂����B</returns>
		/// <exception cref="DivideByZeroException"><paramref name="divisor"/> �� 0 �ł��B</exception>
		/// <remarks>�Ăяo�����̓I�[�o�[�t���[���`�F�b�N����K�v������܂��B</remarks>
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

		/// <summary>2 �� 32 �r�b�g�����t�������̊ۂߏ��Z�̏�]���v�Z���܂��B</summary>
		/// <param name="dividend">�폜�����w�肵�܂��B</param>
		/// <param name="divisor">�������w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ���l�̊ۂߏ��Z�̏�]�B</returns>
		/// <exception cref="DivideByZeroException"><paramref name="divisor"/> �� 0 �ł��B</exception>
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

		/// <summary>2 �� 64 �r�b�g�����t�������̊ۂߏ��Z�̏�]���v�Z���܂��B</summary>
		/// <param name="dividend">�폜�����w�肵�܂��B</param>
		/// <param name="divisor">�������w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ���l�̊ۂߏ��Z�̏�]�B</returns>
		/// <exception cref="DivideByZeroException"><paramref name="divisor"/> �� 0 �ł��B</exception>
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

		/// <summary>�{���x���������_�����w�肵�������_���̌����Ɋۂ߂܂��B���l�� 2 �̐��l�̒��ԂɈʒu����Ƃ��� 0 ���牓�����Ɋۂ߂��܂��B</summary>
		/// <param name="value">�ۂߑΏۂ̔{���x���������_�����w�肵�܂��B</param>
		/// <param name="precision">�ۂ߂��s�������_���̌������w�肵�܂��B���̒l���w�肵���ꍇ�A�ۂ߂͐������̌����ōs���܂��B</param>
		/// <returns><paramref name="value"/> �� <paramref name="precision"/> �ɂ���ĕ\����錅�Ŋۂ߂����l�B</returns>
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

		/// <summary>�{���x���������_���� -0 �ł��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="value">-0 ���ǂ����𔻒f����{���x���������_�����w�肵�܂��B</param>
		/// <returns>�{���x���������_���� -0 �ł���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool IsNegativeZero(double value) { return value == 0.0 && 1.0 / value < 0; }

		/// <summary>�w�肳�ꂽ 2 �̐��l���ӂ���э����Ƃ��钼�p�O�p�`�̎Εӂ��v�Z���܂��B</summary>
		/// <param name="x">���p�O�p�`�̒�ӂ̒������w�肵�܂��B</param>
		/// <param name="y">���p�O�p�`�̍������w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ���p�O�p�`�̎ΕӁB<c>Math.Sqrt(x * x + y * y)</c> �Ɠ������Ȃ�܂��B</returns>
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

		/// <summary>�w�肳�ꂽ�r�b�g���̗�����Ԃ��܂��B</summary>
		/// <param name="generator">�����𐶐����闐���W�F�l���[�^�[���w�肵�܂��B</param>
		/// <param name="bits">�Ԃ���闐���̃r�b�g�����w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�r�b�g���̗�����\���C�Ӓ������B</returns>
		/// <exception cref="ArgumentNullException"><paramref name="generator"/> �� <c>null</c> �ł��B</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="bits"/> �� 0 �ȉ��ł��B</exception>
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

		/// <summary>�w�肳�ꂽ�ő�l��菬���� 0 �ȏ�̗�����Ԃ��܂��B</summary>
		/// <param name="generator">�����𐶐����闐���W�F�l���[�^�[���w�肵�܂��B</param>
		/// <param name="maxValue">��������闐���̔r���I����l�B <paramref name="maxValue"/> �� 0 ���傫�Ȓl�ł���K�v������܂��B</param>
		/// <returns>0 �ȏ�� <paramref name="maxValue"/> �����̔C�Ӓ������B</returns>
		/// <exception cref="ArgumentNullException"><paramref name="generator"/> �� <c>null</c> �ł��B</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> �� 0 �ȉ��ł��B</exception>
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

		/// <summary>�w�肳�ꂽ�ő�l��菬���� 0 �ȏ�̗�����Ԃ��܂��B</summary>
		/// <param name="generator">�����𐶐����闐���W�F�l���[�^�[���w�肵�܂��B</param>
		/// <param name="maxValue">��������闐���̔r���I����l�B <paramref name="maxValue"/> �� 0 ���傫�Ȓl�ł���K�v������܂��B</param>
		/// <returns>0 �ȏ�� <paramref name="maxValue"/> �����̔C�Ӓ������B</returns>
		/// <exception cref="ArgumentNullException"><paramref name="generator"/> �� <c>null</c> �ł��B</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> �� 0 �ȉ��ł��B</exception>
		public static BigInteger NextBigInteger(this Random generator, BigInteger maxValue) { return new BigInteger(generator.NextBigInt(maxValue.Value)); }

		/// <summary>�C�Ӓ������̔{���x���������_���ւ̕ϊ������݂܂��B</summary>
		/// <param name="value">�{���x���������_���ւ̕ϊ����s���C�Ӓ��������w�肵�܂��B</param>
		/// <param name="result">�ϊ����ꂽ�{���x���������_�����Ԃ���܂��B</param>
		/// <returns>�C�Ӓ�������{���x���������_���ɕϊ��ł����ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
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

		/// <summary>�C�Ӓ������̔{���x���������_���ւ̕ϊ������݂܂��B</summary>
		/// <param name="value">�{���x���������_���ւ̕ϊ����s���C�Ӓ��������w�肵�܂��B</param>
		/// <param name="result">�ϊ����ꂽ�{���x���������_�����Ԃ���܂��B</param>
		/// <returns>�C�Ӓ�������{���x���������_���ɕϊ��ł����ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
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

		/// <summary>�C�Ӓ������� 32 �r�b�g�����t�������ւ̕ϊ������݂܂��B</summary>
		/// <param name="value">32 �r�b�g�����t�������֕ϊ�����C�Ӓ��������w�肵�܂��B</param>
		/// <param name="result">�ϊ����ꂽ 32 �r�b�g�����t���������Ԃ���܂��B</param>
		/// <returns>�C�Ӓ������� 32 �r�b�g�����t�������֕ϊ��ł����ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
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

		/// <summary>�C�Ӓ������� 64 �r�b�g�����t�������ւ̕ϊ������݂܂��B</summary>
		/// <param name="value">64 �r�b�g�����t�������֕ϊ�����C�Ӓ��������w�肵�܂��B</param>
		/// <param name="result">�ϊ����ꂽ 64 �r�b�g�����t���������Ԃ���܂��B</param>
		/// <returns>�C�Ӓ������� 64 �r�b�g�����t�������֕ϊ��ł����ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
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

		/// <summary>�C�Ӓ������� 32 �r�b�g�����Ȃ������ւ̕ϊ������݂܂��B</summary>
		/// <param name="value">32 �r�b�g�����Ȃ������֕ϊ�����C�Ӓ��������w�肵�܂��B</param>
		/// <param name="result">�ϊ����ꂽ 32 �r�b�g�����Ȃ��������Ԃ���܂��B</param>
		/// <returns>�C�Ӓ������� 32 �r�b�g�����Ȃ������֕ϊ��ł����ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
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

		/// <summary>�C�Ӓ������� 64 �r�b�g�����Ȃ������ւ̕ϊ������݂܂��B</summary>
		/// <param name="value">64 �r�b�g�����Ȃ������֕ϊ�����C�Ӓ��������w�肵�܂��B</param>
		/// <param name="result">�ϊ����ꂽ 64 �r�b�g�����Ȃ��������Ԃ���܂��B</param>
		/// <returns>�C�Ӓ������� 64 �r�b�g�����Ȃ������֕ϊ��ł����ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
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

		/// <summary>�C�Ӓ������̐�Βl��Ԃ��܂��B</summary>
		/// <param name="value">��Βl���擾����C�Ӓ��������w�肵�܂��B</param>
		/// <returns>�C�Ӓ������ɑ΂����Βl�B</returns>
		public static BigInt Abs(this BigInt value) { return BigInt.Abs(value); }

		/// <summary>�C�Ӓ������̎��R�ΐ���Ԃ��܂��B</summary>
		/// <param name="value">���R�ΐ����擾����C�Ӓ��������w�肵�܂��B</param>
		/// <returns>�C�Ӓ������ɑ΂��鎩�R�ΐ��B</returns>
		/// <exception cref="ArgumentOutOfRangeException">���R�ΐ��� <see cref="System.Double"/> �ŕ\���ł���͈͂𒴂��Ă��܂��B</exception>
		public static double Log(this BigInt value) { return BigInt.Log(value); }

		/// <summary>�C�Ӓ������̎w�肳�ꂽ��̑ΐ���Ԃ��܂��B</summary>
		/// <param name="value">�ΐ����擾����C�Ӓ��������w�肵�܂��B</param>
		/// <param name="baseValue">�擾����ΐ��̒���w�肵�܂��B</param>
		/// <returns>�C�Ӓ������ɑ΂���ΐ��B</returns>
		/// <exception cref="ArgumentOutOfRangeException">�ΐ��� <see cref="System.Double"/> �ŕ\���ł���͈͂𒴂��Ă��܂��B</exception>
		public static double Log(this BigInt value, double baseValue) { return BigInt.Log(value, baseValue); }

		/// <summary>�C�Ӓ������� 10 ���Ƃ���ΐ���Ԃ��܂��B</summary>
		/// <param name="value">10 ���Ƃ���ΐ����擾����C�Ӓ��������w�肵�܂��B</param>
		/// <returns>�C�Ӓ������ɑ΂��� 10 ���Ƃ���ΐ��B</returns>
		/// <exception cref="ArgumentOutOfRangeException">10 ���Ƃ���ΐ��� <see cref="System.Double"/> �ŕ\���ł���͈͂𒴂��Ă��܂��B</exception>
		public static double Log10(this BigInt value) { return BigInt.Log10(value); }

		/// <summary>�C�Ӓ������̎w�肳�ꂽ�����w���Ƃ���ݏ��Ԃ��܂��B</summary>
		/// <param name="value">�ݏ���擾����C�Ӓ��������w�肵�܂��B</param>
		/// <param name="exponent">�C�Ӓ�������ݏ悷��w�����w�肵�܂��B</param>
		/// <returns>�C�Ӓ������̗ݏ�B</returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="exponent"/> �̒l�� 0 �����ł��B</exception>
		public static BigInt Power(this BigInt value, int exponent) { return BigInt.Pow(value, exponent); }

		/// <summary>�C�Ӓ�������ݏ悵�ĕʂ̐��l�Ŋ�������]��Ԃ��܂��B</summary>
		/// <param name="value"><paramref name="exponent"/> �ŗݏ悷��C�Ӓ��������w�肵�܂��B</param>
		/// <param name="exponent">�C�Ӓ�������ݏ悷��C�Ӓ��������w�肵�܂��B</param>
		/// <param name="modulus">�ݏ悳�ꂽ�l������C�Ӓ��������w�肵�܂��B</param>
		/// <returns><paramref name="value"/> ^ <paramref name="exponent"/> �� <paramref name="modulus"/> �Ŋ��������ʐ�������]�B</returns>
		/// <exception cref="DivideByZeroException"><paramref name="modulus"/> �� 0 �ł��B</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="exponent"/> �� 0 �����ł��B</exception>
		public static BigInt ModPow(this BigInt value, BigInt exponent, BigInt modulus) { return BigInt.ModPow(value, exponent, modulus); }

		// generated by scripts/radix_generator.py
		static readonly uint[] maxCharsPerDigit = { 0, 0, 31, 20, 15, 13, 12, 11, 10, 10, 9, 9, 8, 8, 8, 8, 7, 7, 7, 7, 7, 7, 7, 7, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6 };
		static readonly uint[] groupRadixValues = { 0, 0, 2147483648, 3486784401, 1073741824, 1220703125, 2176782336, 1977326743, 1073741824, 3486784401, 1000000000, 2357947691, 429981696, 815730721, 1475789056, 2562890625, 268435456, 410338673, 612220032, 893871739, 1280000000, 1801088541, 2494357888, 3404825447, 191102976, 244140625, 308915776, 387420489, 481890304, 594823321, 729000000, 887503681, 1073741824, 1291467969, 1544804416, 1838265625, 2176782336 };

		/// <summary>�C�Ӓ��������w�肳�ꂽ��ŕ\�������������Ԃ��܂��B</summary>
		/// <param name="value">��ɂ�镶����\�����擾����C�Ӓ��������w�肵�܂��B</param>
		/// <param name="radix">�C�Ӓ�������\���������w�肵�܂��B</param>
		/// <returns>�C�Ӓ��������w�肳�ꂽ��ɂ���ĕ\��������B</returns>
		/// <exception cref="ArgumentOutOfRangeException">��� 2 �����܂��� 36 ���傫�Ȑ��ł��B</exception>
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

		/// <summary>�C�Ӓ�������\�� <see cref="System.UInt32"/> �l�̔z����擾���܂��B���̌��ʂ͕�������ʂ��܂���B</summary>
		/// <param name="value"><see cref="System.UInt32"/> �l�̔z����擾����C�Ӓ��������w�肵�܂��B</param>
		/// <returns>�C�Ӓ�������\�� <see cref="System.UInt32"/> �l�̔z��B</returns>
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

		/// <summary>�C�Ӓ�������\�� <see cref="System.UInt32"/> �z��̗v�f����Ԃ��܂��B</summary>
		/// <param name="value">�C�Ӓ�������\�� <see cref="System.UInt32"/> �z��̗v�f�����擾����C�Ӓ��������w�肵�܂��B</param>
		/// <returns>�C�Ӓ�������\�� <see cref="System.UInt32"/> �z��̗v�f���B</returns>
		public static int GetWordCount(this BigInt value)
		{
			byte[] bytes;
			return GetHighestByte(value, out bytes) / 4 + 1;
		}

		/// <summary>�C�Ӓ�������\�����߂ɕK�v�ȃo�C�g����Ԃ��܂��B</summary>
		/// <param name="value">�C�Ӓ�������\�����߂ɕK�v�ȃo�C�g�����擾����C�Ӓ��������w�肵�܂��B</param>
		/// <returns>�C�Ӓ�������\�����߂ɕK�v�ȃo�C�g���B</returns>
		public static int GetByteCount(this BigInt value)
		{
			byte[] bytes;
			return GetHighestByte(value, out bytes) + 1;
		}

		/// <summary>�C�Ӓ�������\�����߂ɕK�v�ȃr�b�g����Ԃ��܂��B</summary>
		/// <param name="self">�C�Ӓ�������\�����߂ɕK�v�ȃr�b�g�����擾����C�Ӓ��������w�肵�܂��B</param>
		/// <returns>�C�Ӓ�������\�����߂ɕK�v�ȃr�b�g���B</returns>
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
