/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Microsoft Public License. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Microsoft Public License, please send an email to 
 * ironpy@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Microsoft Public License.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

using System;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.Scripting.Interpreter
{
	public partial class CallInstruction
	{
		const int MaxHelpers = 10;
		const int MaxArgs = 3;

		/// <summary>この命令が表すメソッドを指定されたインスタンスと引数を使用して呼び出します。</summary>
		/// <param name="instance">メソッドを呼び出すインスタンスを指定します。</param>
		/// <param name="args">メソッドを呼び出す引数を指定します。</param>
		/// <returns>メソッドの結果。</returns>
		public virtual object InvokeInstance(object instance, params object[] args) { return Invoke(Utils.ArrayUtils.Insert(instance, args)); }

		/// <summary>この命令が表すメソッドを指定された引数を使用して呼び出します。</summary>
		/// <param name="args">メソッドを呼び出す引数を指定します。</param>
		/// <returns>メソッドの結果。</returns>
		public virtual object Invoke(params object[] args)
		{
			switch (args.Length)
			{
				case 0: return Invoke();
				case 1: return Invoke(args[0]);
				case 2: return Invoke(args[0], args[1]);
				case 3: return Invoke(args[0], args[1], args[2]);
				case 4: return Invoke(args[0], args[1], args[2], args[3]);
				case 5: return Invoke(args[0], args[1], args[2], args[3], args[4]);
				case 6: return Invoke(args[0], args[1], args[2], args[3], args[4], args[5]);
				case 7: return Invoke(args[0], args[1], args[2], args[3], args[4], args[5], args[6]);
				case 8: return Invoke(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7]);
				case 9: return Invoke(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8]);
				default: throw new InvalidOperationException();
			}
		}

		/// <summary>この命令が表すメソッドを呼び出します。</summary>
		/// <returns>メソッドの結果。</returns>
		public virtual object Invoke() { throw new InvalidOperationException(); }

		/// <summary>この命令が表すメソッドを指定された引数を使用して呼び出します。</summary>
		/// <param name="arg0">メソッドを呼び出す 1 番目の引数を指定します。</param>
		/// <returns>メソッドの結果。</returns>
		public virtual object Invoke(object arg0) { throw new InvalidOperationException(); }

		/// <summary>この命令が表すメソッドを指定された引数を使用して呼び出します。</summary>
		/// <param name="arg0">メソッドを呼び出す 1 番目の引数を指定します。</param>
		/// <param name="arg1">メソッドを呼び出す 2 番目の引数を指定します。</param>
		/// <returns>メソッドの結果。</returns>
		public virtual object Invoke(object arg0, object arg1) { throw new InvalidOperationException(); }

		/// <summary>この命令が表すメソッドを指定された引数を使用して呼び出します。</summary>
		/// <param name="arg0">メソッドを呼び出す 1 番目の引数を指定します。</param>
		/// <param name="arg1">メソッドを呼び出す 2 番目の引数を指定します。</param>
		/// <param name="arg2">メソッドを呼び出す 3 番目の引数を指定します。</param>
		/// <returns>メソッドの結果。</returns>
		public virtual object Invoke(object arg0, object arg1, object arg2) { throw new InvalidOperationException(); }

		/// <summary>この命令が表すメソッドを指定された引数を使用して呼び出します。</summary>
		/// <param name="arg0">メソッドを呼び出す 1 番目の引数を指定します。</param>
		/// <param name="arg1">メソッドを呼び出す 2 番目の引数を指定します。</param>
		/// <param name="arg2">メソッドを呼び出す 3 番目の引数を指定します。</param>
		/// <param name="arg3">メソッドを呼び出す 4 番目の引数を指定します。</param>
		/// <returns>メソッドの結果。</returns>
		public virtual object Invoke(object arg0, object arg1, object arg2, object arg3) { throw new InvalidOperationException(); }

		/// <summary>この命令が表すメソッドを指定された引数を使用して呼び出します。</summary>
		/// <param name="arg0">メソッドを呼び出す 1 番目の引数を指定します。</param>
		/// <param name="arg1">メソッドを呼び出す 2 番目の引数を指定します。</param>
		/// <param name="arg2">メソッドを呼び出す 3 番目の引数を指定します。</param>
		/// <param name="arg3">メソッドを呼び出す 4 番目の引数を指定します。</param>
		/// <param name="arg4">メソッドを呼び出す 5 番目の引数を指定します。</param>
		/// <returns>メソッドの結果。</returns>
		public virtual object Invoke(object arg0, object arg1, object arg2, object arg3, object arg4) { throw new InvalidOperationException(); }

		/// <summary>この命令が表すメソッドを指定された引数を使用して呼び出します。</summary>
		/// <param name="arg0">メソッドを呼び出す 1 番目の引数を指定します。</param>
		/// <param name="arg1">メソッドを呼び出す 2 番目の引数を指定します。</param>
		/// <param name="arg2">メソッドを呼び出す 3 番目の引数を指定します。</param>
		/// <param name="arg3">メソッドを呼び出す 4 番目の引数を指定します。</param>
		/// <param name="arg4">メソッドを呼び出す 5 番目の引数を指定します。</param>
		/// <param name="arg5">メソッドを呼び出す 6 番目の引数を指定します。</param>
		/// <returns>メソッドの結果。</returns>
		public virtual object Invoke(object arg0, object arg1, object arg2, object arg3, object arg4, object arg5) { throw new InvalidOperationException(); }

		/// <summary>この命令が表すメソッドを指定された引数を使用して呼び出します。</summary>
		/// <param name="arg0">メソッドを呼び出す 1 番目の引数を指定します。</param>
		/// <param name="arg1">メソッドを呼び出す 2 番目の引数を指定します。</param>
		/// <param name="arg2">メソッドを呼び出す 3 番目の引数を指定します。</param>
		/// <param name="arg3">メソッドを呼び出す 4 番目の引数を指定します。</param>
		/// <param name="arg4">メソッドを呼び出す 5 番目の引数を指定します。</param>
		/// <param name="arg5">メソッドを呼び出す 6 番目の引数を指定します。</param>
		/// <param name="arg6">メソッドを呼び出す 7 番目の引数を指定します。</param>
		/// <returns>メソッドの結果。</returns>
		public virtual object Invoke(object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6) { throw new InvalidOperationException(); }

		/// <summary>この命令が表すメソッドを指定された引数を使用して呼び出します。</summary>
		/// <param name="arg0">メソッドを呼び出す 1 番目の引数を指定します。</param>
		/// <param name="arg1">メソッドを呼び出す 2 番目の引数を指定します。</param>
		/// <param name="arg2">メソッドを呼び出す 3 番目の引数を指定します。</param>
		/// <param name="arg3">メソッドを呼び出す 4 番目の引数を指定します。</param>
		/// <param name="arg4">メソッドを呼び出す 5 番目の引数を指定します。</param>
		/// <param name="arg5">メソッドを呼び出す 6 番目の引数を指定します。</param>
		/// <param name="arg6">メソッドを呼び出す 7 番目の引数を指定します。</param>
		/// <param name="arg7">メソッドを呼び出す 8 番目の引数を指定します。</param>
		/// <returns>メソッドの結果。</returns>
		public virtual object Invoke(object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7) { throw new InvalidOperationException(); }

		/// <summary>この命令が表すメソッドを指定された引数を使用して呼び出します。</summary>
		/// <param name="arg0">メソッドを呼び出す 1 番目の引数を指定します。</param>
		/// <param name="arg1">メソッドを呼び出す 2 番目の引数を指定します。</param>
		/// <param name="arg2">メソッドを呼び出す 3 番目の引数を指定します。</param>
		/// <param name="arg3">メソッドを呼び出す 4 番目の引数を指定します。</param>
		/// <param name="arg4">メソッドを呼び出す 5 番目の引数を指定します。</param>
		/// <param name="arg5">メソッドを呼び出す 6 番目の引数を指定します。</param>
		/// <param name="arg6">メソッドを呼び出す 7 番目の引数を指定します。</param>
		/// <param name="arg7">メソッドを呼び出す 8 番目の引数を指定します。</param>
		/// <param name="arg8">メソッドを呼び出す 9 番目の引数を指定します。</param>
		/// <returns>メソッドの結果。</returns>
		public virtual object Invoke(object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8) { throw new InvalidOperationException(); }

		/// <summary>
		/// メソッド シグネチャ全体が既知のプリミティブ型であれば、高速に作成されます。
		/// シグネチャに非プリミティブ型があればすべての型で動作する <see cref="SlowCreate"/> にフォールバックされます。
		/// 
		/// この作成過程が高速であるのは、型の作成にリフレクション (MakeGenericType および Activator.CreateInstance) の使用を避けているからです。
		/// 作成はシグネチャに沿ってそれぞれの型を取り出す一連のジェネリックメソッドの呼び出すを通して行われます。
		/// 型をすべて使用したときに、構築された型をもつ適切な <see cref="CallInstruction"/> が作成されます。
		/// 
		/// 1 つの軽減策は非プリミティブ型の戻り値の型に対して、緩いデリゲートのために object にフォールバックしていることです。
		/// </summary>
		static CallInstruction FastCreate(MethodInfo target, ParameterInfo[] pi)
		{
			Type t = TryGetParameterOrReturnType(target, pi, 0);
			if (t == null)
				return new ActionCallInstruction(target);
			if (t.IsEnum)
				return SlowCreate(target, pi);
			switch (Type.GetTypeCode(t))
			{
				case TypeCode.Object:
					if (t != typeof(object) && (IndexIsNotReturnType(0, target, pi) || t.IsValueType))
						goto default; // if we're on the return type relaxed delegates makes it ok to use object
					return FastCreate<object>(target, pi);
				case TypeCode.Int16: return FastCreate<short>(target, pi);
				case TypeCode.Int32: return FastCreate<int>(target, pi);
				case TypeCode.Int64: return FastCreate<long>(target, pi);
				case TypeCode.Boolean: return FastCreate<bool>(target, pi);
				case TypeCode.Char: return FastCreate<char>(target, pi);
				case TypeCode.Byte: return FastCreate<byte>(target, pi);
				case TypeCode.Decimal: return FastCreate<decimal>(target, pi);
				case TypeCode.DateTime: return FastCreate<DateTime>(target, pi);
				case TypeCode.Double: return FastCreate<double>(target, pi);
				case TypeCode.Single: return FastCreate<float>(target, pi);
				case TypeCode.UInt16: return FastCreate<ushort>(target, pi);
				case TypeCode.UInt32: return FastCreate<uint>(target, pi);
				case TypeCode.UInt64: return FastCreate<ulong>(target, pi);
				case TypeCode.String: return FastCreate<string>(target, pi);
				case TypeCode.SByte: return FastCreate<sbyte>(target, pi);
				default: return SlowCreate(target, pi);
			}
		}

		static CallInstruction FastCreate<T0>(MethodInfo target, ParameterInfo[] pi)
		{
			var t = TryGetParameterOrReturnType(target, pi, 1);
			if (t == null)
			{
				if (target.ReturnType == typeof(void))
					return new ActionCallInstruction<T0>(target);
				return new FuncCallInstruction<T0>(target);
			}
			if (t.IsEnum)
				return SlowCreate(target, pi);
			switch (Type.GetTypeCode(t))
			{
				case TypeCode.Object:
					if (t != typeof(object) && (IndexIsNotReturnType(1, target, pi) || t.IsValueType))
						goto default; // if we're on the return type relaxed delegates makes it ok to use object
					return FastCreate<T0, object>(target, pi);
				case TypeCode.Int16: return FastCreate<T0, short>(target, pi);
				case TypeCode.Int32: return FastCreate<T0, int>(target, pi);
				case TypeCode.Int64: return FastCreate<T0, long>(target, pi);
				case TypeCode.Boolean: return FastCreate<T0, bool>(target, pi);
				case TypeCode.Char: return FastCreate<T0, char>(target, pi);
				case TypeCode.Byte: return FastCreate<T0, byte>(target, pi);
				case TypeCode.Decimal: return FastCreate<T0, decimal>(target, pi);
				case TypeCode.DateTime: return FastCreate<T0, DateTime>(target, pi);
				case TypeCode.Double: return FastCreate<T0, double>(target, pi);
				case TypeCode.Single: return FastCreate<T0, float>(target, pi);
				case TypeCode.UInt16: return FastCreate<T0, ushort>(target, pi);
				case TypeCode.UInt32: return FastCreate<T0, uint>(target, pi);
				case TypeCode.UInt64: return FastCreate<T0, ulong>(target, pi);
				case TypeCode.String: return FastCreate<T0, string>(target, pi);
				case TypeCode.SByte: return FastCreate<T0, sbyte>(target, pi);
				default: return SlowCreate(target, pi);
			}
		}

		static CallInstruction FastCreate<T0, T1>(MethodInfo target, ParameterInfo[] pi)
		{
			var t = TryGetParameterOrReturnType(target, pi, 2);
			if (t == null)
			{
				if (target.ReturnType == typeof(void))
					return new ActionCallInstruction<T0, T1>(target);
				return new FuncCallInstruction<T0, T1>(target);
			}
			if (t.IsEnum)
				return SlowCreate(target, pi);
			switch (Type.GetTypeCode(t))
			{
				case TypeCode.Object:
					Debug.Assert(pi.Length == 2);
					if (t.IsValueType)
						goto default;
					return new FuncCallInstruction<T0, T1, Object>(target);
				case TypeCode.Int16: return new FuncCallInstruction<T0, T1, short>(target);
				case TypeCode.Int32: return new FuncCallInstruction<T0, T1, int>(target);
				case TypeCode.Int64: return new FuncCallInstruction<T0, T1, long>(target);
				case TypeCode.Boolean: return new FuncCallInstruction<T0, T1, bool>(target);
				case TypeCode.Char: return new FuncCallInstruction<T0, T1, char>(target);
				case TypeCode.Byte: return new FuncCallInstruction<T0, T1, byte>(target);
				case TypeCode.Decimal: return new FuncCallInstruction<T0, T1, decimal>(target);
				case TypeCode.DateTime: return new FuncCallInstruction<T0, T1, DateTime>(target);
				case TypeCode.Double: return new FuncCallInstruction<T0, T1, double>(target);
				case TypeCode.Single: return new FuncCallInstruction<T0, T1, float>(target);
				case TypeCode.UInt16: return new FuncCallInstruction<T0, T1, ushort>(target);
				case TypeCode.UInt32: return new FuncCallInstruction<T0, T1, uint>(target);
				case TypeCode.UInt64: return new FuncCallInstruction<T0, T1, ulong>(target);
				case TypeCode.String: return new FuncCallInstruction<T0, T1, string>(target);
				case TypeCode.SByte: return new FuncCallInstruction<T0, T1, sbyte>(target);
				default: return SlowCreate(target, pi);
			}
		}

		static Type GetHelperType(MethodInfo info, Type[] arrTypes)
		{
			if (info.ReturnType == typeof(void))
			{
				switch (arrTypes.Length)
				{
					case 0: return typeof(ActionCallInstruction);
					case 1: return typeof(ActionCallInstruction<>).MakeGenericType(arrTypes);
					case 2: return typeof(ActionCallInstruction<,>).MakeGenericType(arrTypes);
					case 3: return typeof(ActionCallInstruction<,,>).MakeGenericType(arrTypes);
					case 4: return typeof(ActionCallInstruction<,,,>).MakeGenericType(arrTypes);
					case 5: return typeof(ActionCallInstruction<,,,,>).MakeGenericType(arrTypes);
					case 6: return typeof(ActionCallInstruction<,,,,,>).MakeGenericType(arrTypes);
					case 7: return typeof(ActionCallInstruction<,,,,,,>).MakeGenericType(arrTypes);
					case 8: return typeof(ActionCallInstruction<,,,,,,,>).MakeGenericType(arrTypes);
					case 9: return typeof(ActionCallInstruction<,,,,,,,,>).MakeGenericType(arrTypes);
					default: throw new InvalidOperationException();
				}
			}
			switch (arrTypes.Length)
			{
				case 1: return typeof(FuncCallInstruction<>).MakeGenericType(arrTypes);
				case 2: return typeof(FuncCallInstruction<,>).MakeGenericType(arrTypes);
				case 3: return typeof(FuncCallInstruction<,,>).MakeGenericType(arrTypes);
				case 4: return typeof(FuncCallInstruction<,,,>).MakeGenericType(arrTypes);
				case 5: return typeof(FuncCallInstruction<,,,,>).MakeGenericType(arrTypes);
				case 6: return typeof(FuncCallInstruction<,,,,,>).MakeGenericType(arrTypes);
				case 7: return typeof(FuncCallInstruction<,,,,,,>).MakeGenericType(arrTypes);
				case 8: return typeof(FuncCallInstruction<,,,,,,,>).MakeGenericType(arrTypes);
				case 9: return typeof(FuncCallInstruction<,,,,,,,,>).MakeGenericType(arrTypes);
				case 10: return typeof(FuncCallInstruction<,,,,,,,,,>).MakeGenericType(arrTypes);
				default: throw new InvalidOperationException();
			}
		}

		/// <summary>指定された戻り値のあるメソッドをキャッシュして、メソッドの情報を格納する <see cref="MethodInfo"/> を返します。</summary>
		/// <typeparam name="TRet">メソッドの戻り値の型を指定します。</typeparam>
		/// <param name="method">キャッシュするメソッドを指定します。</param>
		/// <returns>キャッシュされたメソッドの情報を格納する <see cref="MethodInfo"/>。</returns>
		public static MethodInfo CacheFunc<TRet>(Func<TRet> method)
		{
			_cache[method.Method] = new FuncCallInstruction<TRet>(method);
			return method.Method;
		}

		/// <summary>指定された戻り値のあるメソッドをキャッシュして、メソッドの情報を格納する <see cref="MethodInfo"/> を返します。</summary>
		/// <typeparam name="TRet">メソッドの戻り値の型を指定します。</typeparam>
		/// <typeparam name="T0">メソッドの 1 番目の引数の型を指定します。</typeparam>
		/// <param name="method">キャッシュするメソッドを指定します。</param>
		/// <returns>キャッシュされたメソッドの情報を格納する <see cref="MethodInfo"/>。</returns>
		public static MethodInfo CacheFunc<T0, TRet>(Func<T0, TRet> method)
		{
			_cache[method.Method] = new FuncCallInstruction<T0, TRet>(method);
			return method.Method;
		}

		/// <summary>指定された戻り値のあるメソッドをキャッシュして、メソッドの情報を格納する <see cref="MethodInfo"/> を返します。</summary>
		/// <typeparam name="TRet">メソッドの戻り値の型を指定します。</typeparam>
		/// <typeparam name="T0">メソッドの 1 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T1">メソッドの 2 番目の引数の型を指定します。</typeparam>
		/// <param name="method">キャッシュするメソッドを指定します。</param>
		/// <returns>キャッシュされたメソッドの情報を格納する <see cref="MethodInfo"/>。</returns>
		public static MethodInfo CacheFunc<T0, T1, TRet>(Func<T0, T1, TRet> method)
		{
			_cache[method.Method] = new FuncCallInstruction<T0, T1, TRet>(method);
			return method.Method;
		}

		/// <summary>指定された戻り値のあるメソッドをキャッシュして、メソッドの情報を格納する <see cref="MethodInfo"/> を返します。</summary>
		/// <typeparam name="TRet">メソッドの戻り値の型を指定します。</typeparam>
		/// <typeparam name="T0">メソッドの 1 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T1">メソッドの 2 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T2">メソッドの 3 番目の引数の型を指定します。</typeparam>
		/// <param name="method">キャッシュするメソッドを指定します。</param>
		/// <returns>キャッシュされたメソッドの情報を格納する <see cref="MethodInfo"/>。</returns>
		public static MethodInfo CacheFunc<T0, T1, T2, TRet>(Func<T0, T1, T2, TRet> method)
		{
			_cache[method.Method] = new FuncCallInstruction<T0, T1, T2, TRet>(method);
			return method.Method;
		}

		/// <summary>指定された戻り値のあるメソッドをキャッシュして、メソッドの情報を格納する <see cref="MethodInfo"/> を返します。</summary>
		/// <typeparam name="TRet">メソッドの戻り値の型を指定します。</typeparam>
		/// <typeparam name="T0">メソッドの 1 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T1">メソッドの 2 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T2">メソッドの 3 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T3">メソッドの 4 番目の引数の型を指定します。</typeparam>
		/// <param name="method">キャッシュするメソッドを指定します。</param>
		/// <returns>キャッシュされたメソッドの情報を格納する <see cref="MethodInfo"/>。</returns>
		public static MethodInfo CacheFunc<T0, T1, T2, T3, TRet>(Func<T0, T1, T2, T3, TRet> method)
		{
			_cache[method.Method] = new FuncCallInstruction<T0, T1, T2, T3, TRet>(method);
			return method.Method;
		}

		/// <summary>指定された戻り値のあるメソッドをキャッシュして、メソッドの情報を格納する <see cref="MethodInfo"/> を返します。</summary>
		/// <typeparam name="TRet">メソッドの戻り値の型を指定します。</typeparam>
		/// <typeparam name="T0">メソッドの 1 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T1">メソッドの 2 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T2">メソッドの 3 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T3">メソッドの 4 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T4">メソッドの 5 番目の引数の型を指定します。</typeparam>
		/// <param name="method">キャッシュするメソッドを指定します。</param>
		/// <returns>キャッシュされたメソッドの情報を格納する <see cref="MethodInfo"/>。</returns>
		public static MethodInfo CacheFunc<T0, T1, T2, T3, T4, TRet>(Func<T0, T1, T2, T3, T4, TRet> method)
		{
			_cache[method.Method] = new FuncCallInstruction<T0, T1, T2, T3, T4, TRet>(method);
			return method.Method;
		}

		/// <summary>指定された戻り値のあるメソッドをキャッシュして、メソッドの情報を格納する <see cref="MethodInfo"/> を返します。</summary>
		/// <typeparam name="TRet">メソッドの戻り値の型を指定します。</typeparam>
		/// <typeparam name="T0">メソッドの 1 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T1">メソッドの 2 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T2">メソッドの 3 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T3">メソッドの 4 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T4">メソッドの 5 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T5">メソッドの 6 番目の引数の型を指定します。</typeparam>
		/// <param name="method">キャッシュするメソッドを指定します。</param>
		/// <returns>キャッシュされたメソッドの情報を格納する <see cref="MethodInfo"/>。</returns>
		public static MethodInfo CacheFunc<T0, T1, T2, T3, T4, T5, TRet>(Func<T0, T1, T2, T3, T4, T5, TRet> method)
		{
			_cache[method.Method] = new FuncCallInstruction<T0, T1, T2, T3, T4, T5, TRet>(method);
			return method.Method;
		}

		/// <summary>指定された戻り値のあるメソッドをキャッシュして、メソッドの情報を格納する <see cref="MethodInfo"/> を返します。</summary>
		/// <typeparam name="TRet">メソッドの戻り値の型を指定します。</typeparam>
		/// <typeparam name="T0">メソッドの 1 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T1">メソッドの 2 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T2">メソッドの 3 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T3">メソッドの 4 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T4">メソッドの 5 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T5">メソッドの 6 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T6">メソッドの 7 番目の引数の型を指定します。</typeparam>
		/// <param name="method">キャッシュするメソッドを指定します。</param>
		/// <returns>キャッシュされたメソッドの情報を格納する <see cref="MethodInfo"/>。</returns>
		public static MethodInfo CacheFunc<T0, T1, T2, T3, T4, T5, T6, TRet>(Func<T0, T1, T2, T3, T4, T5, T6, TRet> method)
		{
			_cache[method.Method] = new FuncCallInstruction<T0, T1, T2, T3, T4, T5, T6, TRet>(method);
			return method.Method;
		}

		/// <summary>指定された戻り値のあるメソッドをキャッシュして、メソッドの情報を格納する <see cref="MethodInfo"/> を返します。</summary>
		/// <typeparam name="TRet">メソッドの戻り値の型を指定します。</typeparam>
		/// <typeparam name="T0">メソッドの 1 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T1">メソッドの 2 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T2">メソッドの 3 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T3">メソッドの 4 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T4">メソッドの 5 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T5">メソッドの 6 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T6">メソッドの 7 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T7">メソッドの 8 番目の引数の型を指定します。</typeparam>
		/// <param name="method">キャッシュするメソッドを指定します。</param>
		/// <returns>キャッシュされたメソッドの情報を格納する <see cref="MethodInfo"/>。</returns>
		public static MethodInfo CacheFunc<T0, T1, T2, T3, T4, T5, T6, T7, TRet>(Func<T0, T1, T2, T3, T4, T5, T6, T7, TRet> method)
		{
			_cache[method.Method] = new FuncCallInstruction<T0, T1, T2, T3, T4, T5, T6, T7, TRet>(method);
			return method.Method;
		}

		/// <summary>指定された戻り値のあるメソッドをキャッシュして、メソッドの情報を格納する <see cref="MethodInfo"/> を返します。</summary>
		/// <typeparam name="TRet">メソッドの戻り値の型を指定します。</typeparam>
		/// <typeparam name="T0">メソッドの 1 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T1">メソッドの 2 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T2">メソッドの 3 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T3">メソッドの 4 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T4">メソッドの 5 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T5">メソッドの 6 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T6">メソッドの 7 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T7">メソッドの 8 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T8">メソッドの 9 番目の引数の型を指定します。</typeparam>
		/// <param name="method">キャッシュするメソッドを指定します。</param>
		/// <returns>キャッシュされたメソッドの情報を格納する <see cref="MethodInfo"/>。</returns>
		public static MethodInfo CacheFunc<T0, T1, T2, T3, T4, T5, T6, T7, T8, TRet>(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, TRet> method)
		{
			_cache[method.Method] = new FuncCallInstruction<T0, T1, T2, T3, T4, T5, T6, T7, T8, TRet>(method);
			return method.Method;
		}

		/// <summary>指定された戻り値のないメソッドをキャッシュして、メソッドの情報を格納する <see cref="MethodInfo"/> を返します。</summary>
		/// <param name="method">キャッシュするメソッドを指定します。</param>
		/// <returns>キャッシュされたメソッドの情報を格納する <see cref="MethodInfo"/>。</returns>
		public static MethodInfo CacheAction(Action method)
		{
			_cache[method.Method] = new ActionCallInstruction(method);
			return method.Method;
		}

		/// <summary>指定された戻り値のないメソッドをキャッシュして、メソッドの情報を格納する <see cref="MethodInfo"/> を返します。</summary>
		/// <typeparam name="T0">メソッドの 1 番目の引数の型を指定します。</typeparam>
		/// <param name="method">キャッシュするメソッドを指定します。</param>
		/// <returns>キャッシュされたメソッドの情報を格納する <see cref="MethodInfo"/>。</returns>
		public static MethodInfo CacheAction<T0>(Action<T0> method)
		{
			_cache[method.Method] = new ActionCallInstruction<T0>(method);
			return method.Method;
		}

		/// <summary>指定された戻り値のないメソッドをキャッシュして、メソッドの情報を格納する <see cref="MethodInfo"/> を返します。</summary>
		/// <typeparam name="T0">メソッドの 1 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T1">メソッドの 2 番目の引数の型を指定します。</typeparam>
		/// <param name="method">キャッシュするメソッドを指定します。</param>
		/// <returns>キャッシュされたメソッドの情報を格納する <see cref="MethodInfo"/>。</returns>
		public static MethodInfo CacheAction<T0, T1>(Action<T0, T1> method)
		{
			_cache[method.Method] = new ActionCallInstruction<T0, T1>(method);
			return method.Method;
		}

		/// <summary>指定された戻り値のないメソッドをキャッシュして、メソッドの情報を格納する <see cref="MethodInfo"/> を返します。</summary>
		/// <typeparam name="T0">メソッドの 1 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T1">メソッドの 2 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T2">メソッドの 3 番目の引数の型を指定します。</typeparam>
		/// <param name="method">キャッシュするメソッドを指定します。</param>
		/// <returns>キャッシュされたメソッドの情報を格納する <see cref="MethodInfo"/>。</returns>
		public static MethodInfo CacheAction<T0, T1, T2>(Action<T0, T1, T2> method)
		{
			_cache[method.Method] = new ActionCallInstruction<T0, T1, T2>(method);
			return method.Method;
		}

		/// <summary>指定された戻り値のないメソッドをキャッシュして、メソッドの情報を格納する <see cref="MethodInfo"/> を返します。</summary>
		/// <typeparam name="T0">メソッドの 1 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T1">メソッドの 2 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T2">メソッドの 3 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T3">メソッドの 4 番目の引数の型を指定します。</typeparam>
		/// <param name="method">キャッシュするメソッドを指定します。</param>
		/// <returns>キャッシュされたメソッドの情報を格納する <see cref="MethodInfo"/>。</returns>
		public static MethodInfo CacheAction<T0, T1, T2, T3>(Action<T0, T1, T2, T3> method)
		{
			_cache[method.Method] = new ActionCallInstruction<T0, T1, T2, T3>(method);
			return method.Method;
		}

		/// <summary>指定された戻り値のないメソッドをキャッシュして、メソッドの情報を格納する <see cref="MethodInfo"/> を返します。</summary>
		/// <typeparam name="T0">メソッドの 1 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T1">メソッドの 2 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T2">メソッドの 3 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T3">メソッドの 4 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T4">メソッドの 5 番目の引数の型を指定します。</typeparam>
		/// <param name="method">キャッシュするメソッドを指定します。</param>
		/// <returns>キャッシュされたメソッドの情報を格納する <see cref="MethodInfo"/>。</returns>
		public static MethodInfo CacheAction<T0, T1, T2, T3, T4>(Action<T0, T1, T2, T3, T4> method)
		{
			_cache[method.Method] = new ActionCallInstruction<T0, T1, T2, T3, T4>(method);
			return method.Method;
		}

		/// <summary>指定された戻り値のないメソッドをキャッシュして、メソッドの情報を格納する <see cref="MethodInfo"/> を返します。</summary>
		/// <typeparam name="T0">メソッドの 1 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T1">メソッドの 2 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T2">メソッドの 3 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T3">メソッドの 4 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T4">メソッドの 5 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T5">メソッドの 6 番目の引数の型を指定します。</typeparam>
		/// <param name="method">キャッシュするメソッドを指定します。</param>
		/// <returns>キャッシュされたメソッドの情報を格納する <see cref="MethodInfo"/>。</returns>
		public static MethodInfo CacheAction<T0, T1, T2, T3, T4, T5>(Action<T0, T1, T2, T3, T4, T5> method)
		{
			_cache[method.Method] = new ActionCallInstruction<T0, T1, T2, T3, T4, T5>(method);
			return method.Method;
		}

		/// <summary>指定された戻り値のないメソッドをキャッシュして、メソッドの情報を格納する <see cref="MethodInfo"/> を返します。</summary>
		/// <typeparam name="T0">メソッドの 1 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T1">メソッドの 2 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T2">メソッドの 3 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T3">メソッドの 4 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T4">メソッドの 5 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T5">メソッドの 6 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T6">メソッドの 7 番目の引数の型を指定します。</typeparam>
		/// <param name="method">キャッシュするメソッドを指定します。</param>
		/// <returns>キャッシュされたメソッドの情報を格納する <see cref="MethodInfo"/>。</returns>
		public static MethodInfo CacheAction<T0, T1, T2, T3, T4, T5, T6>(Action<T0, T1, T2, T3, T4, T5, T6> method)
		{
			_cache[method.Method] = new ActionCallInstruction<T0, T1, T2, T3, T4, T5, T6>(method);
			return method.Method;
		}

		/// <summary>指定された戻り値のないメソッドをキャッシュして、メソッドの情報を格納する <see cref="MethodInfo"/> を返します。</summary>
		/// <typeparam name="T0">メソッドの 1 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T1">メソッドの 2 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T2">メソッドの 3 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T3">メソッドの 4 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T4">メソッドの 5 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T5">メソッドの 6 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T6">メソッドの 7 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T7">メソッドの 8 番目の引数の型を指定します。</typeparam>
		/// <param name="method">キャッシュするメソッドを指定します。</param>
		/// <returns>キャッシュされたメソッドの情報を格納する <see cref="MethodInfo"/>。</returns>
		public static MethodInfo CacheAction<T0, T1, T2, T3, T4, T5, T6, T7>(Action<T0, T1, T2, T3, T4, T5, T6, T7> method)
		{
			_cache[method.Method] = new ActionCallInstruction<T0, T1, T2, T3, T4, T5, T6, T7>(method);
			return method.Method;
		}

		/// <summary>指定された戻り値のないメソッドをキャッシュして、メソッドの情報を格納する <see cref="MethodInfo"/> を返します。</summary>
		/// <typeparam name="T0">メソッドの 1 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T1">メソッドの 2 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T2">メソッドの 3 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T3">メソッドの 4 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T4">メソッドの 5 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T5">メソッドの 6 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T6">メソッドの 7 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T7">メソッドの 8 番目の引数の型を指定します。</typeparam>
		/// <typeparam name="T8">メソッドの 9 番目の引数の型を指定します。</typeparam>
		/// <param name="method">キャッシュするメソッドを指定します。</param>
		/// <returns>キャッシュされたメソッドの情報を格納する <see cref="MethodInfo"/>。</returns>
		public static MethodInfo CacheAction<T0, T1, T2, T3, T4, T5, T6, T7, T8>(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8> method)
		{
			_cache[method.Method] = new ActionCallInstruction<T0, T1, T2, T3, T4, T5, T6, T7, T8>(method);
			return method.Method;
		}
	}

	sealed class ActionCallInstruction : CallInstruction
	{
		readonly Action _target;
		public override MethodInfo Info { get { return _target.Method; } }
		public override int ArgumentCount { get { return 0; } }

		public ActionCallInstruction(Action target) { _target = target; }

		public ActionCallInstruction(MethodInfo target) { _target = (Action)Delegate.CreateDelegate(typeof(Action), target); }

		public override object Invoke()
		{
			_target();
			return null;
		}

		public override int Run(InterpretedFrame frame)
		{
			_target();
			return 1;
		}
	}

	sealed class ActionCallInstruction<T0> : CallInstruction
	{
		readonly Action<T0> _target;
		public override MethodInfo Info { get { return _target.Method; } }
		public override int ArgumentCount { get { return 1; } }

		public ActionCallInstruction(Action<T0> target) { _target = target; }

		public ActionCallInstruction(MethodInfo target) { _target = (Action<T0>)Delegate.CreateDelegate(typeof(Action<T0>), target); }

		public override object Invoke(object arg0)
		{
			_target(arg0 != null ? (T0)arg0 : default(T0));
			return null;
		}

		public override int Run(InterpretedFrame frame)
		{
			_target((T0)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 1;
			return 1;
		}
	}

	sealed class ActionCallInstruction<T0, T1> : CallInstruction
	{
		readonly Action<T0, T1> _target;
		public override MethodInfo Info { get { return _target.Method; } }
		public override int ArgumentCount { get { return 2; } }

		public ActionCallInstruction(Action<T0, T1> target) { _target = target; }

		public ActionCallInstruction(MethodInfo target) { _target = (Action<T0, T1>)Delegate.CreateDelegate(typeof(Action<T0, T1>), target); }

		public override object Invoke(object arg0, object arg1)
		{
			_target(arg0 != null ? (T0)arg0 : default(T0), arg1 != null ? (T1)arg1 : default(T1));
			return null;
		}

		public override int Run(InterpretedFrame frame)
		{
			_target((T0)frame.Data[frame.StackIndex - 2], (T1)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 2;
			return 1;
		}
	}

	sealed class ActionCallInstruction<T0, T1, T2> : CallInstruction
	{
		readonly Action<T0, T1, T2> _target;
		public override MethodInfo Info { get { return _target.Method; } }
		public override int ArgumentCount { get { return 3; } }

		public ActionCallInstruction(Action<T0, T1, T2> target) { _target = target; }

		public ActionCallInstruction(MethodInfo target) { _target = (Action<T0, T1, T2>)Delegate.CreateDelegate(typeof(Action<T0, T1, T2>), target); }

		public override object Invoke(object arg0, object arg1, object arg2)
		{
			_target(arg0 != null ? (T0)arg0 : default(T0), arg1 != null ? (T1)arg1 : default(T1), arg2 != null ? (T2)arg2 : default(T2));
			return null;
		}

		public override int Run(InterpretedFrame frame)
		{
			_target((T0)frame.Data[frame.StackIndex - 3], (T1)frame.Data[frame.StackIndex - 2], (T2)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 3;
			return 1;
		}
	}

	sealed class ActionCallInstruction<T0, T1, T2, T3> : CallInstruction
	{
		readonly Action<T0, T1, T2, T3> _target;
		public override MethodInfo Info { get { return _target.Method; } }
		public override int ArgumentCount { get { return 4; } }

		public ActionCallInstruction(Action<T0, T1, T2, T3> target) { _target = target; }

		public ActionCallInstruction(MethodInfo target) { _target = (Action<T0, T1, T2, T3>)Delegate.CreateDelegate(typeof(Action<T0, T1, T2, T3>), target); }

		public override object Invoke(object arg0, object arg1, object arg2, object arg3)
		{
			_target(arg0 != null ? (T0)arg0 : default(T0), arg1 != null ? (T1)arg1 : default(T1), arg2 != null ? (T2)arg2 : default(T2), arg3 != null ? (T3)arg3 : default(T3));
			return null;
		}

		public override int Run(InterpretedFrame frame)
		{
			_target((T0)frame.Data[frame.StackIndex - 4], (T1)frame.Data[frame.StackIndex - 3], (T2)frame.Data[frame.StackIndex - 2], (T3)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 4;
			return 1;
		}
	}

	sealed class ActionCallInstruction<T0, T1, T2, T3, T4> : CallInstruction
	{
		readonly Action<T0, T1, T2, T3, T4> _target;
		public override MethodInfo Info { get { return _target.Method; } }
		public override int ArgumentCount { get { return 5; } }

		public ActionCallInstruction(Action<T0, T1, T2, T3, T4> target) { _target = target; }

		public ActionCallInstruction(MethodInfo target) { _target = (Action<T0, T1, T2, T3, T4>)Delegate.CreateDelegate(typeof(Action<T0, T1, T2, T3, T4>), target); }

		public override object Invoke(object arg0, object arg1, object arg2, object arg3, object arg4)
		{
			_target(arg0 != null ? (T0)arg0 : default(T0), arg1 != null ? (T1)arg1 : default(T1), arg2 != null ? (T2)arg2 : default(T2), arg3 != null ? (T3)arg3 : default(T3), arg4 != null ? (T4)arg4 : default(T4));
			return null;
		}

		public override int Run(InterpretedFrame frame)
		{
			_target((T0)frame.Data[frame.StackIndex - 5], (T1)frame.Data[frame.StackIndex - 4], (T2)frame.Data[frame.StackIndex - 3], (T3)frame.Data[frame.StackIndex - 2], (T4)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 5;
			return 1;
		}
	}

	sealed class ActionCallInstruction<T0, T1, T2, T3, T4, T5> : CallInstruction
	{
		readonly Action<T0, T1, T2, T3, T4, T5> _target;
		public override MethodInfo Info { get { return _target.Method; } }
		public override int ArgumentCount { get { return 6; } }

		public ActionCallInstruction(Action<T0, T1, T2, T3, T4, T5> target) { _target = target; }

		public ActionCallInstruction(MethodInfo target) { _target = (Action<T0, T1, T2, T3, T4, T5>)Delegate.CreateDelegate(typeof(Action<T0, T1, T2, T3, T4, T5>), target); }

		public override object Invoke(object arg0, object arg1, object arg2, object arg3, object arg4, object arg5)
		{
			_target(arg0 != null ? (T0)arg0 : default(T0), arg1 != null ? (T1)arg1 : default(T1), arg2 != null ? (T2)arg2 : default(T2), arg3 != null ? (T3)arg3 : default(T3), arg4 != null ? (T4)arg4 : default(T4), arg5 != null ? (T5)arg5 : default(T5));
			return null;
		}

		public override int Run(InterpretedFrame frame)
		{
			_target((T0)frame.Data[frame.StackIndex - 6], (T1)frame.Data[frame.StackIndex - 5], (T2)frame.Data[frame.StackIndex - 4], (T3)frame.Data[frame.StackIndex - 3], (T4)frame.Data[frame.StackIndex - 2], (T5)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 6;
			return 1;
		}
	}

	sealed class ActionCallInstruction<T0, T1, T2, T3, T4, T5, T6> : CallInstruction
	{
		readonly Action<T0, T1, T2, T3, T4, T5, T6> _target;
		public override MethodInfo Info { get { return _target.Method; } }
		public override int ArgumentCount { get { return 7; } }

		public ActionCallInstruction(Action<T0, T1, T2, T3, T4, T5, T6> target) { _target = target; }

		public ActionCallInstruction(MethodInfo target) { _target = (Action<T0, T1, T2, T3, T4, T5, T6>)Delegate.CreateDelegate(typeof(Action<T0, T1, T2, T3, T4, T5, T6>), target); }

		public override object Invoke(object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
		{
			_target(arg0 != null ? (T0)arg0 : default(T0), arg1 != null ? (T1)arg1 : default(T1), arg2 != null ? (T2)arg2 : default(T2), arg3 != null ? (T3)arg3 : default(T3), arg4 != null ? (T4)arg4 : default(T4), arg5 != null ? (T5)arg5 : default(T5), arg6 != null ? (T6)arg6 : default(T6));
			return null;
		}

		public override int Run(InterpretedFrame frame)
		{
			_target((T0)frame.Data[frame.StackIndex - 7], (T1)frame.Data[frame.StackIndex - 6], (T2)frame.Data[frame.StackIndex - 5], (T3)frame.Data[frame.StackIndex - 4], (T4)frame.Data[frame.StackIndex - 3], (T5)frame.Data[frame.StackIndex - 2], (T6)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 7;
			return 1;
		}
	}

	sealed class ActionCallInstruction<T0, T1, T2, T3, T4, T5, T6, T7> : CallInstruction
	{
		readonly Action<T0, T1, T2, T3, T4, T5, T6, T7> _target;
		public override MethodInfo Info { get { return _target.Method; } }
		public override int ArgumentCount { get { return 8; } }

		public ActionCallInstruction(Action<T0, T1, T2, T3, T4, T5, T6, T7> target) { _target = target; }

		public ActionCallInstruction(MethodInfo target) { _target = (Action<T0, T1, T2, T3, T4, T5, T6, T7>)Delegate.CreateDelegate(typeof(Action<T0, T1, T2, T3, T4, T5, T6, T7>), target); }

		public override object Invoke(object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7)
		{
			_target(arg0 != null ? (T0)arg0 : default(T0), arg1 != null ? (T1)arg1 : default(T1), arg2 != null ? (T2)arg2 : default(T2), arg3 != null ? (T3)arg3 : default(T3), arg4 != null ? (T4)arg4 : default(T4), arg5 != null ? (T5)arg5 : default(T5), arg6 != null ? (T6)arg6 : default(T6), arg7 != null ? (T7)arg7 : default(T7));
			return null;
		}

		public override int Run(InterpretedFrame frame)
		{
			_target((T0)frame.Data[frame.StackIndex - 8], (T1)frame.Data[frame.StackIndex - 7], (T2)frame.Data[frame.StackIndex - 6], (T3)frame.Data[frame.StackIndex - 5], (T4)frame.Data[frame.StackIndex - 4], (T5)frame.Data[frame.StackIndex - 3], (T6)frame.Data[frame.StackIndex - 2], (T7)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 8;
			return 1;
		}
	}

	sealed class ActionCallInstruction<T0, T1, T2, T3, T4, T5, T6, T7, T8> : CallInstruction
	{
		readonly Action<T0, T1, T2, T3, T4, T5, T6, T7, T8> _target;
		public override MethodInfo Info { get { return _target.Method; } }
		public override int ArgumentCount { get { return 9; } }

		public ActionCallInstruction(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8> target) { _target = target; }

		public ActionCallInstruction(MethodInfo target) { _target = (Action<T0, T1, T2, T3, T4, T5, T6, T7, T8>)Delegate.CreateDelegate(typeof(Action<T0, T1, T2, T3, T4, T5, T6, T7, T8>), target); }

		public override object Invoke(object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8)
		{
			_target(arg0 != null ? (T0)arg0 : default(T0), arg1 != null ? (T1)arg1 : default(T1), arg2 != null ? (T2)arg2 : default(T2), arg3 != null ? (T3)arg3 : default(T3), arg4 != null ? (T4)arg4 : default(T4), arg5 != null ? (T5)arg5 : default(T5), arg6 != null ? (T6)arg6 : default(T6), arg7 != null ? (T7)arg7 : default(T7), arg8 != null ? (T8)arg8 : default(T8));
			return null;
		}

		public override int Run(InterpretedFrame frame)
		{
			_target((T0)frame.Data[frame.StackIndex - 9], (T1)frame.Data[frame.StackIndex - 8], (T2)frame.Data[frame.StackIndex - 7], (T3)frame.Data[frame.StackIndex - 6], (T4)frame.Data[frame.StackIndex - 5], (T5)frame.Data[frame.StackIndex - 4], (T6)frame.Data[frame.StackIndex - 3], (T7)frame.Data[frame.StackIndex - 2], (T8)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 9;
			return 1;
		}
	}

	sealed class FuncCallInstruction<TRet> : CallInstruction
	{
		readonly Func<TRet> _target;
		public override MethodInfo Info { get { return _target.Method; } }
		public override int ArgumentCount { get { return 0; } }

		public FuncCallInstruction(Func<TRet> target) { _target = target; }

		public FuncCallInstruction(MethodInfo target) { _target = (Func<TRet>)Delegate.CreateDelegate(typeof(Func<TRet>), target); }

		public override object Invoke() { return _target(); }

		public override int Run(InterpretedFrame frame)
		{
			frame.Data[frame.StackIndex - 0] = _target();
			frame.StackIndex += 1;
			return 1;
		}
	}

	sealed class FuncCallInstruction<T0, TRet> : CallInstruction
	{
		readonly Func<T0, TRet> _target;
		public override MethodInfo Info { get { return _target.Method; } }
		public override int ArgumentCount { get { return 1; } }

		public FuncCallInstruction(Func<T0, TRet> target) { _target = target; }

		public FuncCallInstruction(MethodInfo target) { _target = (Func<T0, TRet>)Delegate.CreateDelegate(typeof(Func<T0, TRet>), target); }

		public override object Invoke(object arg0) { return _target(arg0 != null ? (T0)arg0 : default(T0)); }

		public override int Run(InterpretedFrame frame)
		{
			frame.Data[frame.StackIndex - 1] = _target((T0)frame.Data[frame.StackIndex - 1]);
			return 1;
		}
	}

	sealed class FuncCallInstruction<T0, T1, TRet> : CallInstruction
	{
		readonly Func<T0, T1, TRet> _target;
		public override MethodInfo Info { get { return _target.Method; } }
		public override int ArgumentCount { get { return 2; } }

		public FuncCallInstruction(Func<T0, T1, TRet> target) { _target = target; }

		public FuncCallInstruction(MethodInfo target) { _target = (Func<T0, T1, TRet>)Delegate.CreateDelegate(typeof(Func<T0, T1, TRet>), target); }

		public override object Invoke(object arg0, object arg1) { return _target(arg0 != null ? (T0)arg0 : default(T0), arg1 != null ? (T1)arg1 : default(T1)); }

		public override int Run(InterpretedFrame frame)
		{
			frame.Data[frame.StackIndex - 2] = _target((T0)frame.Data[frame.StackIndex - 2], (T1)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 1;
			return 1;
		}
	}

	sealed class FuncCallInstruction<T0, T1, T2, TRet> : CallInstruction
	{
		readonly Func<T0, T1, T2, TRet> _target;
		public override MethodInfo Info { get { return _target.Method; } }
		public override int ArgumentCount { get { return 3; } }

		public FuncCallInstruction(Func<T0, T1, T2, TRet> target) { _target = target; }

		public FuncCallInstruction(MethodInfo target) { _target = (Func<T0, T1, T2, TRet>)Delegate.CreateDelegate(typeof(Func<T0, T1, T2, TRet>), target); }

		public override object Invoke(object arg0, object arg1, object arg2) { return _target(arg0 != null ? (T0)arg0 : default(T0), arg1 != null ? (T1)arg1 : default(T1), arg2 != null ? (T2)arg2 : default(T2)); }

		public override int Run(InterpretedFrame frame)
		{
			frame.Data[frame.StackIndex - 3] = _target((T0)frame.Data[frame.StackIndex - 3], (T1)frame.Data[frame.StackIndex - 2], (T2)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 2;
			return 1;
		}
	}

	sealed class FuncCallInstruction<T0, T1, T2, T3, TRet> : CallInstruction
	{
		readonly Func<T0, T1, T2, T3, TRet> _target;
		public override MethodInfo Info { get { return _target.Method; } }
		public override int ArgumentCount { get { return 4; } }

		public FuncCallInstruction(Func<T0, T1, T2, T3, TRet> target) { _target = target; }

		public FuncCallInstruction(MethodInfo target) { _target = (Func<T0, T1, T2, T3, TRet>)Delegate.CreateDelegate(typeof(Func<T0, T1, T2, T3, TRet>), target); }

		public override object Invoke(object arg0, object arg1, object arg2, object arg3) { return _target(arg0 != null ? (T0)arg0 : default(T0), arg1 != null ? (T1)arg1 : default(T1), arg2 != null ? (T2)arg2 : default(T2), arg3 != null ? (T3)arg3 : default(T3)); }

		public override int Run(InterpretedFrame frame)
		{
			frame.Data[frame.StackIndex - 4] = _target((T0)frame.Data[frame.StackIndex - 4], (T1)frame.Data[frame.StackIndex - 3], (T2)frame.Data[frame.StackIndex - 2], (T3)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 3;
			return 1;
		}
	}

	sealed class FuncCallInstruction<T0, T1, T2, T3, T4, TRet> : CallInstruction
	{
		readonly Func<T0, T1, T2, T3, T4, TRet> _target;
		public override MethodInfo Info { get { return _target.Method; } }
		public override int ArgumentCount { get { return 5; } }

		public FuncCallInstruction(Func<T0, T1, T2, T3, T4, TRet> target) { _target = target; }

		public FuncCallInstruction(MethodInfo target) { _target = (Func<T0, T1, T2, T3, T4, TRet>)Delegate.CreateDelegate(typeof(Func<T0, T1, T2, T3, T4, TRet>), target); }

		public override object Invoke(object arg0, object arg1, object arg2, object arg3, object arg4) { return _target(arg0 != null ? (T0)arg0 : default(T0), arg1 != null ? (T1)arg1 : default(T1), arg2 != null ? (T2)arg2 : default(T2), arg3 != null ? (T3)arg3 : default(T3), arg4 != null ? (T4)arg4 : default(T4)); }

		public override int Run(InterpretedFrame frame)
		{
			frame.Data[frame.StackIndex - 5] = _target((T0)frame.Data[frame.StackIndex - 5], (T1)frame.Data[frame.StackIndex - 4], (T2)frame.Data[frame.StackIndex - 3], (T3)frame.Data[frame.StackIndex - 2], (T4)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 4;
			return 1;
		}
	}

	sealed class FuncCallInstruction<T0, T1, T2, T3, T4, T5, TRet> : CallInstruction
	{
		readonly Func<T0, T1, T2, T3, T4, T5, TRet> _target;
		public override MethodInfo Info { get { return _target.Method; } }
		public override int ArgumentCount { get { return 6; } }

		public FuncCallInstruction(Func<T0, T1, T2, T3, T4, T5, TRet> target) { _target = target; }

		public FuncCallInstruction(MethodInfo target) { _target = (Func<T0, T1, T2, T3, T4, T5, TRet>)Delegate.CreateDelegate(typeof(Func<T0, T1, T2, T3, T4, T5, TRet>), target); }

		public override object Invoke(object arg0, object arg1, object arg2, object arg3, object arg4, object arg5) { return _target(arg0 != null ? (T0)arg0 : default(T0), arg1 != null ? (T1)arg1 : default(T1), arg2 != null ? (T2)arg2 : default(T2), arg3 != null ? (T3)arg3 : default(T3), arg4 != null ? (T4)arg4 : default(T4), arg5 != null ? (T5)arg5 : default(T5)); }

		public override int Run(InterpretedFrame frame)
		{
			frame.Data[frame.StackIndex - 6] = _target((T0)frame.Data[frame.StackIndex - 6], (T1)frame.Data[frame.StackIndex - 5], (T2)frame.Data[frame.StackIndex - 4], (T3)frame.Data[frame.StackIndex - 3], (T4)frame.Data[frame.StackIndex - 2], (T5)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 5;
			return 1;
		}
	}

	sealed class FuncCallInstruction<T0, T1, T2, T3, T4, T5, T6, TRet> : CallInstruction
	{
		readonly Func<T0, T1, T2, T3, T4, T5, T6, TRet> _target;
		public override MethodInfo Info { get { return _target.Method; } }
		public override int ArgumentCount { get { return 7; } }

		public FuncCallInstruction(Func<T0, T1, T2, T3, T4, T5, T6, TRet> target) { _target = target; }

		public FuncCallInstruction(MethodInfo target) { _target = (Func<T0, T1, T2, T3, T4, T5, T6, TRet>)Delegate.CreateDelegate(typeof(Func<T0, T1, T2, T3, T4, T5, T6, TRet>), target); }

		public override object Invoke(object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6) { return _target(arg0 != null ? (T0)arg0 : default(T0), arg1 != null ? (T1)arg1 : default(T1), arg2 != null ? (T2)arg2 : default(T2), arg3 != null ? (T3)arg3 : default(T3), arg4 != null ? (T4)arg4 : default(T4), arg5 != null ? (T5)arg5 : default(T5), arg6 != null ? (T6)arg6 : default(T6)); }

		public override int Run(InterpretedFrame frame)
		{
			frame.Data[frame.StackIndex - 7] = _target((T0)frame.Data[frame.StackIndex - 7], (T1)frame.Data[frame.StackIndex - 6], (T2)frame.Data[frame.StackIndex - 5], (T3)frame.Data[frame.StackIndex - 4], (T4)frame.Data[frame.StackIndex - 3], (T5)frame.Data[frame.StackIndex - 2], (T6)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 6;
			return 1;
		}
	}

	sealed class FuncCallInstruction<T0, T1, T2, T3, T4, T5, T6, T7, TRet> : CallInstruction
	{
		readonly Func<T0, T1, T2, T3, T4, T5, T6, T7, TRet> _target;
		public override MethodInfo Info { get { return _target.Method; } }
		public override int ArgumentCount { get { return 8; } }

		public FuncCallInstruction(Func<T0, T1, T2, T3, T4, T5, T6, T7, TRet> target) { _target = target; }

		public FuncCallInstruction(MethodInfo target) { _target = (Func<T0, T1, T2, T3, T4, T5, T6, T7, TRet>)Delegate.CreateDelegate(typeof(Func<T0, T1, T2, T3, T4, T5, T6, T7, TRet>), target); }

		public override object Invoke(object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7) { return _target(arg0 != null ? (T0)arg0 : default(T0), arg1 != null ? (T1)arg1 : default(T1), arg2 != null ? (T2)arg2 : default(T2), arg3 != null ? (T3)arg3 : default(T3), arg4 != null ? (T4)arg4 : default(T4), arg5 != null ? (T5)arg5 : default(T5), arg6 != null ? (T6)arg6 : default(T6), arg7 != null ? (T7)arg7 : default(T7)); }

		public override int Run(InterpretedFrame frame)
		{
			frame.Data[frame.StackIndex - 8] = _target((T0)frame.Data[frame.StackIndex - 8], (T1)frame.Data[frame.StackIndex - 7], (T2)frame.Data[frame.StackIndex - 6], (T3)frame.Data[frame.StackIndex - 5], (T4)frame.Data[frame.StackIndex - 4], (T5)frame.Data[frame.StackIndex - 3], (T6)frame.Data[frame.StackIndex - 2], (T7)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 7;
			return 1;
		}
	}

	sealed class FuncCallInstruction<T0, T1, T2, T3, T4, T5, T6, T7, T8, TRet> : CallInstruction
	{
		readonly Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, TRet> _target;
		public override MethodInfo Info { get { return _target.Method; } }
		public override int ArgumentCount { get { return 9; } }

		public FuncCallInstruction(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, TRet> target) { _target = target; }

		public FuncCallInstruction(MethodInfo target) { _target = (Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, TRet>)Delegate.CreateDelegate(typeof(Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, TRet>), target); }

		public override object Invoke(object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8) { return _target(arg0 != null ? (T0)arg0 : default(T0), arg1 != null ? (T1)arg1 : default(T1), arg2 != null ? (T2)arg2 : default(T2), arg3 != null ? (T3)arg3 : default(T3), arg4 != null ? (T4)arg4 : default(T4), arg5 != null ? (T5)arg5 : default(T5), arg6 != null ? (T6)arg6 : default(T6), arg7 != null ? (T7)arg7 : default(T7), arg8 != null ? (T8)arg8 : default(T8)); }

		public override int Run(InterpretedFrame frame)
		{
			frame.Data[frame.StackIndex - 9] = _target((T0)frame.Data[frame.StackIndex - 9], (T1)frame.Data[frame.StackIndex - 8], (T2)frame.Data[frame.StackIndex - 7], (T3)frame.Data[frame.StackIndex - 6], (T4)frame.Data[frame.StackIndex - 5], (T5)frame.Data[frame.StackIndex - 4], (T6)frame.Data[frame.StackIndex - 3], (T7)frame.Data[frame.StackIndex - 2], (T8)frame.Data[frame.StackIndex - 1]);
			frame.StackIndex -= 8;
			return 1;
		}
	}
}
