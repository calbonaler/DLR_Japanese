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
using System.Reflection;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Numerics;
using Microsoft.Scripting.Utils;
using BigInt = System.Numerics.BigInteger;
using Complex = System.Numerics.Complex;

namespace Microsoft.Scripting.Ast
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces")]
	public static partial class Utils
	{
		static readonly ConstantExpression TrueLiteral = Expression.Constant(true, typeof(bool));
		static readonly ConstantExpression FalseLiteral = Expression.Constant(false, typeof(bool));
		static readonly ConstantExpression NullLiteral = Expression.Constant(null, typeof(object));
		static readonly ConstantExpression EmptyStringLiteral = Expression.Constant(string.Empty, typeof(string));
		static readonly ConstantExpression[] IntCache = new ConstantExpression[100];

		/// <summary>�w�肳�ꂽ�l�� <see cref="WeakReference"/> �Ń��b�v���A<see cref="WeakReference"/> ����l���擾���� <see cref="Expression"/> ��Ԃ��܂��B</summary>
		/// <param name="value"><see cref="WeakReference"/> �Ń��b�v����l���w�肵�܂��B</param>
		/// <returns><see cref="WeakReference"/> �Ń��b�v���ꂽ�w�肳�ꂽ�l����l���擾���� <see cref="Expression"/>�B</returns>
		public static MemberExpression WeakConstant(object value)
		{
			System.Diagnostics.Debug.Assert(!(value is Expression));
			return Expression.Property(
				Constant(new WeakReference(value)),
				typeof(WeakReference).GetProperty("Target")
			);
		}

		/// <summary>�w�肳�ꂽ�^�̎w�肳�ꂽ�l������ <see cref="ConstantExpression"/> ��Ԃ��܂��B</summary>
		/// <param name="value"><see cref="ConstantExpression"/> �Ɋi�[����l���w�肵�܂��B</param>
		/// <param name="type"><see cref="ConstantExpression"/> �Ɋi�[����l�̌^���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�^�̎w�肳�ꂽ�l������ <see cref="ConstantExpression"/>�B</returns>
		public static ConstantExpression Constant(object value, Type type) { return Expression.Constant(value, type); }

		// The helper API should return ConstantExpression after SymbolConstantExpression goes away
		/// <summary>�w�肳�ꂽ�l���\�z���� <see cref="Expression"/> ��Ԃ��܂��B</summary>
		/// <param name="value">�\�z���� <see cref="Expression"/> ��Ԃ��l���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�l���\�z���� <see cref="Expression"/>�B</returns>
		public static Expression Constant(object value)
		{
			if (value == null)
				return NullLiteral;
			if (value is SymbolId)
				return new SymbolConstantExpression((SymbolId)value);
			if (value is BigInteger)
				return BigIntegerConstant((BigInteger)value);
			else if (value is BigInt)
				return BigIntConstant((BigInt)value);
			else if (value is Complex)
			{
				var complex = (Complex)value;
				return Expression.New(typeof(Complex).GetConstructor(new[] { typeof(double), typeof(double) }), Constant(complex.Real), Constant(complex.Imaginary));
			}
			else if (value is Type)
				return Expression.Constant(value, typeof(Type));
			else if (value is ConstructorInfo)
				return Expression.Constant(value, typeof(ConstructorInfo));
			else if (value is EventInfo)
				return Expression.Constant(value, typeof(EventInfo));
			else if (value is FieldInfo)
				return Expression.Constant(value, typeof(FieldInfo));
			else if (value is MethodInfo)
				return Expression.Constant(value, typeof(MethodInfo));
			else if (value is PropertyInfo)
				return Expression.Constant(value, typeof(PropertyInfo));
			else
			{
				Type t = value.GetType();
				if (!t.IsEnum)
				{
					switch (Type.GetTypeCode(t))
					{
						case TypeCode.Boolean:
							return (bool)value ? TrueLiteral : FalseLiteral;
						case TypeCode.Int32:
							int x = (int)value;
							int cacheIndex = x + 2;
							if (cacheIndex >= 0 && cacheIndex < IntCache.Length)
							{
								ConstantExpression res;
								if ((res = IntCache[cacheIndex]) == null)
									IntCache[cacheIndex] = res = Constant(x, typeof(int));
								return res;
							}
							break;
						case TypeCode.String:
							if (string.IsNullOrEmpty((string)value))
								return EmptyStringLiteral;
							break;
					}
				}
				return Expression.Constant(value);
			}
		}

		static Expression BigIntegerConstant(BigInteger value)
		{
			int ival;
			if (value.AsInt32(out ival))
				return Expression.Call(new Func<int, BigInteger>(BigInteger.Create).Method, Constant(ival));
			long lval;
			if (value.AsInt64(out lval))
				return Expression.Call(new Func<long, BigInteger>(BigInteger.Create).Method, Constant(lval));
			return Expression.Call(new Func<byte[], BigInteger>(BigInteger.Create).Method, CreateArray(value.ToByteArray()));
		}

		static Expression BigIntConstant(BigInt value)
		{
			int ival;
			if (value.AsInt32(out ival))
				return Expression.New(typeof(BigInt).GetConstructor(new[] { typeof(int) }), Constant(ival));
			long lval;
			if (value.AsInt64(out lval))
				return Expression.New(typeof(BigInt).GetConstructor(new[] { typeof(long) }), Constant(lval));
			return Expression.New(typeof(BigInt).GetConstructor(new[] { typeof(byte[]) }), CreateArray(value.ToByteArray()));
		}

		static Expression CreateArray<T>(T[] array) { return Expression.NewArrayInit(typeof(T), Array.ConvertAll(array, x => Constant(x))); }
	}
}
