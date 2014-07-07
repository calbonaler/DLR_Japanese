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
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Generation
{
	/// <summary><see cref="ILGenerator"/> に関するヘルパー メソッドを格納します。</summary>
	public static class ILGeneratorExtensions
	{
		#region Instruction helpers

		/// <summary>指定された位置にある引数を評価スタックに読み込む命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="index">
		/// スタックに読み込む引数のインデックスを指定します。
		/// インスタンス メソッドの場合、0 は this オブジェクトを表し、引数リストの左端の引数はインデックス 1 になります。
		/// 静的メソッドの場合は 0 が引数リストの左端の引数を表します。
		/// </param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> は <c>null</c> です。</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> は 0 以上である必要があります。</exception>
		public static void EmitLoadArg(this ILGenerator instance, int index)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresArrayIndex(int.MaxValue, index, "index");
			switch (index)
			{
				case 0:
					instance.Emit(OpCodes.Ldarg_0);
					break;
				case 1:
					instance.Emit(OpCodes.Ldarg_1);
					break;
				case 2:
					instance.Emit(OpCodes.Ldarg_2);
					break;
				case 3:
					instance.Emit(OpCodes.Ldarg_3);
					break;
				default:
					if (index <= byte.MaxValue)
						instance.Emit(OpCodes.Ldarg_S, (byte)index);
					else
						instance.Emit(OpCodes.Ldarg, index);
					break;
			}
		}

		/// <summary>指定された位置にある引数のアドレスを評価スタックに読み込む命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="index">
		/// スタックに読み込むアドレスに対応する引数のインデックスを指定します。
		/// インスタンス メソッドの場合、0 は this オブジェクトを表し、引数リストの左端の引数はインデックス 1 になります。
		/// 静的メソッドの場合は 0 が引数リストの左端の引数を表します。
		/// </param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> は <c>null</c> です。</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> は 0 以上である必要があります。</exception>
		public static void EmitLoadArgAddress(this ILGenerator instance, int index)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresArrayIndex(int.MaxValue, index, "index");
			if (index <= byte.MaxValue)
				instance.Emit(OpCodes.Ldarga_S, (byte)index);
			else
				instance.Emit(OpCodes.Ldarga, index);
		}

		/// <summary>評価スタックの一番上にある値を指定された位置にある引数に格納する命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="index">
		/// スタックから値が格納される引数のインデックスを指定します。
		/// インスタンス メソッドの場合、0 は this オブジェクトを表し、引数リストの左端の引数はインデックス 1 になります。
		/// 静的メソッドの場合は 0 が引数リストの左端の引数を表します。
		/// </param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> は <c>null</c> です。</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> は 0 以上である必要があります。</exception>
		public static void EmitStoreArg(this ILGenerator instance, int index)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresArrayIndex(int.MaxValue, index, "index");
			if (index <= byte.MaxValue)
				instance.Emit(OpCodes.Starg_S, (byte)index);
			else
				instance.Emit(OpCodes.Starg, index);
		}

		/// <summary>評価スタックにすでに読み込まれているアドレスにある指定された型のオブジェクトをスタックに読み込む命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="type">スタックに読み込むオブジェクトの型を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> または <paramref name="type"/> が <c>null</c> です。</exception>
		public static void EmitLoadValueIndirect(this ILGenerator instance, Type type)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(type, "type");
			if (type.IsValueType)
			{
				if (type == typeof(int))
					instance.Emit(OpCodes.Ldind_I4);
				else if (type == typeof(uint))
					instance.Emit(OpCodes.Ldind_U4);
				else if (type == typeof(short))
					instance.Emit(OpCodes.Ldind_I2);
				else if (type == typeof(ushort))
					instance.Emit(OpCodes.Ldind_U2);
				else if (type == typeof(long) || type == typeof(ulong))
					instance.Emit(OpCodes.Ldind_I8);
				else if (type == typeof(char))
					instance.Emit(OpCodes.Ldind_I2);
				else if (type == typeof(bool))
					instance.Emit(OpCodes.Ldind_I1);
				else if (type == typeof(float))
					instance.Emit(OpCodes.Ldind_R4);
				else if (type == typeof(double))
					instance.Emit(OpCodes.Ldind_R8);
				else
					instance.Emit(OpCodes.Ldobj, type);
			}
			else if (type.IsGenericParameter)
				instance.Emit(OpCodes.Ldobj, type);
			else
				instance.Emit(OpCodes.Ldind_Ref);
		}

		/// <summary>評価スタックにすでに読み込まれている指定された型のオブジェクトを、同じくスタックに読み込まれているアドレスに格納する命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="type">スタックからアドレスに格納するオブジェクトの型を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> または <paramref name="type"/> が <c>null</c> です。</exception>
		public static void EmitStoreValueIndirect(this ILGenerator instance, Type type)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(type, "type");
			if (type.IsValueType)
			{
				if (type == typeof(int))
					instance.Emit(OpCodes.Stind_I4);
				else if (type == typeof(short))
					instance.Emit(OpCodes.Stind_I2);
				else if (type == typeof(long) || type == typeof(ulong))
					instance.Emit(OpCodes.Stind_I8);
				else if (type == typeof(char))
					instance.Emit(OpCodes.Stind_I2);
				else if (type == typeof(bool))
					instance.Emit(OpCodes.Stind_I1);
				else if (type == typeof(float))
					instance.Emit(OpCodes.Stind_R4);
				else if (type == typeof(double))
					instance.Emit(OpCodes.Stind_R8);
				else
					instance.Emit(OpCodes.Stobj, type);
			}
			else if (type.IsGenericParameter)
				instance.Emit(OpCodes.Stobj, type);
			else
				instance.Emit(OpCodes.Stind_Ref);
		}

		/// <summary>評価スタックにすでに読み込まれている配列の、同じくスタックに読み込まれているインデックスにある要素をスタックに読み込む命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="type">配列からスタックに読み込む要素の型を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> または <paramref name="type"/> が <c>null</c> です。</exception>
		public static void EmitLoadElement(this ILGenerator instance, Type type)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(type, "type");
			if (!type.IsValueType)
				instance.Emit(OpCodes.Ldelem_Ref);
			else if (type.IsEnum)
				instance.Emit(OpCodes.Ldelem, type);
			else
			{
				switch (Type.GetTypeCode(type))
				{
					case TypeCode.Boolean:
					case TypeCode.SByte:
						instance.Emit(OpCodes.Ldelem_I1);
						break;
					case TypeCode.Byte:
						instance.Emit(OpCodes.Ldelem_U1);
						break;
					case TypeCode.Int16:
						instance.Emit(OpCodes.Ldelem_I2);
						break;
					case TypeCode.Char:
					case TypeCode.UInt16:
						instance.Emit(OpCodes.Ldelem_U2);
						break;
					case TypeCode.Int32:
						instance.Emit(OpCodes.Ldelem_I4);
						break;
					case TypeCode.UInt32:
						instance.Emit(OpCodes.Ldelem_U4);
						break;
					case TypeCode.Int64:
					case TypeCode.UInt64:
						instance.Emit(OpCodes.Ldelem_I8);
						break;
					case TypeCode.Single:
						instance.Emit(OpCodes.Ldelem_R4);
						break;
					case TypeCode.Double:
						instance.Emit(OpCodes.Ldelem_R8);
						break;
					default:
						instance.Emit(OpCodes.Ldelem, type);
						break;
				}
			}
		}

		/// <summary>評価スタックにすでに読み込まれている配列の、同じくスタックに読み込まれているインデックスにある要素に、スタックから値を格納する命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="type">スタックから配列に格納する要素の型を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> または <paramref name="type"/> が <c>null</c> です。</exception>
		public static void EmitStoreElement(this ILGenerator instance, Type type)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(type, "type");
			if (type.IsEnum)
			{
				instance.Emit(OpCodes.Stelem, type);
				return;
			}
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Boolean:
				case TypeCode.SByte:
				case TypeCode.Byte:
					instance.Emit(OpCodes.Stelem_I1);
					break;
				case TypeCode.Char:
				case TypeCode.Int16:
				case TypeCode.UInt16:
					instance.Emit(OpCodes.Stelem_I2);
					break;
				case TypeCode.Int32:
				case TypeCode.UInt32:
					instance.Emit(OpCodes.Stelem_I4);
					break;
				case TypeCode.Int64:
				case TypeCode.UInt64:
					instance.Emit(OpCodes.Stelem_I8);
					break;
				case TypeCode.Single:
					instance.Emit(OpCodes.Stelem_R4);
					break;
				case TypeCode.Double:
					instance.Emit(OpCodes.Stelem_R8);
					break;
				default:
					if (type.IsValueType)
						instance.Emit(OpCodes.Stelem, type);
					else
						instance.Emit(OpCodes.Stelem_Ref);
					break;
			}
		}

		/// <summary>評価スタックに指定された <see cref="System.Type"/> を読み込む命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="type">スタックに読み込む <see cref="System.Type"/> オブジェクトを指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> または <paramref name="type"/> が <c>null</c> です。</exception>
		public static void EmitType(this ILGenerator instance, Type type)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(type, "type");
			instance.Emit(OpCodes.Ldtoken, type);
			EmitCall(instance, new Func<RuntimeTypeHandle, Type>(Type.GetTypeFromHandle).Method);
		}

		/// <summary>評価スタックにすでに読み込まれているオブジェクトを指定された型にボックス化解除する命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="type">ボックス化解除された後のオブジェクトの型を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> または <paramref name="type"/> が <c>null</c> です。</exception>
		public static void EmitUnbox(this ILGenerator instance, Type type)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(type, "type");
			instance.Emit(OpCodes.Unbox_Any, type);
		}

		#endregion

		#region Fields, properties and methods

		/// <summary>指定された型にある指定された名前のパブリック プロパティの値を評価スタックに読み込む命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="type">値を読み込むプロパティが存在する型を指定します。</param>
		/// <param name="name">値を読み込むプロパティの名前を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/>、<paramref name="type"/> または <paramref name="name"/> が <c>null</c> です。</exception>
		/// <exception cref="ArgumentException"><paramref name="type"/> に <paramref name="name"/> という名前のパブリック プロパティは存在しません。</exception>
		/// <exception cref="InvalidOperationException">プロパティは書き込み専用で読み取ることはできません。</exception>
		public static void EmitPropertyGet(this ILGenerator instance, Type type, string name)
		{
			ContractUtils.RequiresNotNull(type, "type");
			ContractUtils.RequiresNotNull(name, "name");
			var pi = type.GetProperty(name);
			ContractUtils.Requires(pi != null, "name", Strings.PropertyDoesNotExist);
			EmitPropertyGet(instance, pi);
		}

		/// <summary>指定された <see cref="PropertyInfo"/> によって表されるプロパティの値を評価スタックに読み込む命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="pi">値を読み込むプロパティを表す <see cref="PropertyInfo"/> を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> または <paramref name="pi"/> が <c>null</c> です。</exception>
		/// <exception cref="InvalidOperationException">プロパティは書き込み専用で読み取ることはできません。</exception>
		public static void EmitPropertyGet(this ILGenerator instance, PropertyInfo pi)
		{
			ContractUtils.RequiresNotNull(pi, "pi");
			if (!pi.CanRead)
				throw Error.CantReadProperty();
			EmitCall(instance, pi.GetGetMethod());
		}

		/// <summary>指定された型にある指定された名前のパブリック プロパティに評価スタックの一番上にある値を設定する命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="type">値を設定するプロパティが存在する型を指定します。</param>
		/// <param name="name">値を設定するプロパティの名前を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/>、<paramref name="type"/> または <paramref name="name"/> が <c>null</c> です。</exception>
		/// <exception cref="ArgumentException"><paramref name="type"/> に <paramref name="name"/> という名前のパブリック プロパティは存在しません。</exception>
		/// <exception cref="InvalidOperationException">プロパティは読み取り専用で書き込むことはできません。</exception>
		public static void EmitPropertySet(this ILGenerator instance, Type type, string name)
		{
			ContractUtils.RequiresNotNull(type, "type");
			ContractUtils.RequiresNotNull(name, "name");
			var pi = type.GetProperty(name);
			ContractUtils.Requires(pi != null, "name", Strings.PropertyDoesNotExist);
			EmitPropertySet(instance, pi);
		}

		/// <summary>指定された <see cref="PropertyInfo"/> によって表されるプロパティに評価スタックの一番上にある値を設定する命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="pi">値を設定するプロパティを表す <see cref="PropertyInfo"/> を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> または <paramref name="pi"/> が <c>null</c> です。</exception>
		/// <exception cref="InvalidOperationException">プロパティは読み取り専用で書き込むことはできません。</exception>
		public static void EmitPropertySet(this ILGenerator instance, PropertyInfo pi)
		{
			ContractUtils.RequiresNotNull(pi, "pi");
			if (!pi.CanWrite)
				throw Error.CantWriteProperty();
			EmitCall(instance, pi.GetSetMethod());
		}

		/// <summary>指定された <see cref="FieldInfo"/> によって表されるフィールドのアドレスを評価スタックに読み込む命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="fi">アドレスを読み込むフィールドを表す <see cref="FieldInfo"/> を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> または <paramref name="fi"/> が <c>null</c> です。</exception>
		public static void EmitFieldAddress(this ILGenerator instance, FieldInfo fi)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(fi, "fi");
			if (fi.IsStatic)
				instance.Emit(OpCodes.Ldsflda, fi);
			else
				instance.Emit(OpCodes.Ldflda, fi);
		}

		/// <summary>指定された型にある指定された名前のパブリック フィールドの値を評価スタックに読み込む命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="type">値を読み込むフィールドが存在する型を指定します。</param>
		/// <param name="name">値を読み込むフィールドの名前を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/>、<paramref name="type"/> または <paramref name="name"/> が <c>null</c> です。</exception>
		/// <exception cref="ArgumentException"><paramref name="type"/> に <paramref name="name"/> という名前のパブリック フィールドは存在しません。</exception>
		public static void EmitFieldGet(this ILGenerator instance, Type type, String name)
		{
			ContractUtils.RequiresNotNull(type, "type");
			ContractUtils.RequiresNotNull(name, "name");
			var fi = type.GetField(name);
			ContractUtils.Requires(fi != null, "name", Strings.FieldDoesNotExist);
			EmitFieldGet(instance, fi);
		}

		/// <summary>指定された型にある指定された名前のパブリック フィールドに評価スタックの一番上にある値を設定する命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="type">値を設定するフィールドが存在する型を指定します。</param>
		/// <param name="name">値を設定するフィールドの名前を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/>、<paramref name="type"/> または <paramref name="name"/> が <c>null</c> です。</exception>
		/// <exception cref="ArgumentException"><paramref name="type"/> に <paramref name="name"/> という名前のパブリック フィールドは存在しません。</exception>
		public static void EmitFieldSet(this ILGenerator instance, Type type, String name)
		{
			ContractUtils.RequiresNotNull(type, "type");
			ContractUtils.RequiresNotNull(name, "name");
			var fi = type.GetField(name);
			ContractUtils.Requires(fi != null, "name", Strings.FieldDoesNotExist);
			EmitFieldSet(instance, fi);
		}

		/// <summary>指定された <see cref="FieldInfo"/> によって表されるフィールドの値を評価スタックに読み込む命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="fi">値を読み込むフィールドを表す <see cref="FieldInfo"/> を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> または <paramref name="fi"/> が <c>null</c> です。</exception>
		public static void EmitFieldGet(this ILGenerator instance, FieldInfo fi)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(fi, "fi");
			if (fi.IsStatic)
				instance.Emit(OpCodes.Ldsfld, fi);
			else
				instance.Emit(OpCodes.Ldfld, fi);
		}

		/// <summary>指定された <see cref="FieldInfo"/> によって表されるフィールドに評価スタックの一番上にある値を設定する命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="fi">値を設定するフィールドを表す <see cref="FieldInfo"/> を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> または <paramref name="fi"/> が <c>null</c> です。</exception>
		public static void EmitFieldSet(this ILGenerator instance, FieldInfo fi)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(fi, "fi");
			if (fi.IsStatic)
				instance.Emit(OpCodes.Stsfld, fi);
			else
				instance.Emit(OpCodes.Stfld, fi);
		}

		/// <summary>指定された <see cref="ConstructorInfo"/> によって表されるコンストラクタを呼び出してオブジェクトの新しいインスタンスを作成する命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="ci">オブジェクトの初期化に使用するコンストラクタを表す <see cref="ConstructorInfo"/> を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> または <paramref name="ci"/> が <c>null</c> です。</exception>
		/// <exception cref="ArgumentException">作成しようとした型にはジェネリック型パラメータが含まれています。</exception>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
		public static void EmitNew(this ILGenerator instance, ConstructorInfo ci)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(ci, "ci");
			if (ci.DeclaringType.ContainsGenericParameters)
				throw Error.IllegalNew_GenericParams(ci.DeclaringType);
			instance.Emit(OpCodes.Newobj, ci);
		}

		/// <summary>指定された型にある指定された引数の型に一致するパブリック コンストラクタを呼び出してオブジェクトの新しいインスタンスを作成する命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="type">作成するオブジェクトの型を指定します。</param>
		/// <param name="paramTypes">オブジェクトの初期化に使用するコンストラクタの引数の型を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/>、<paramref name="type"/> または <paramref name="paramTypes"/> が <c>null</c> です。</exception>
		/// <exception cref="ArgumentException"><paramref name="type"/> に <paramref name="paramTypes"/> の引数と一致するパブリック コンストラクタが存在しません。</exception>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
		public static void EmitNew(this ILGenerator instance, Type type, Type[] paramTypes)
		{
			ContractUtils.RequiresNotNull(type, "type");
			ContractUtils.RequiresNotNull(paramTypes, "paramTypes");
			var ci = type.GetConstructor(paramTypes);
			ContractUtils.Requires(ci != null, "type", Strings.TypeDoesNotHaveConstructorForTheSignature);
			EmitNew(instance, ci);
		}

		/// <summary>指定された <see cref="MethodInfo"/> によって表されるメソッドを呼び出す命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="mi">呼び出すメソッドを表す <see cref="MethodInfo"/> を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> または <paramref name="mi"/> が <c>null</c> です。</exception>
		public static void EmitCall(this ILGenerator instance, MethodInfo mi)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(mi, "mi");
			if (mi.IsVirtual && !mi.DeclaringType.IsValueType)
				instance.Emit(OpCodes.Callvirt, mi);
			else
				instance.Emit(OpCodes.Call, mi);
		}

		/// <summary>指定された型にある指定された名前のパブリック メソッドを呼び出す命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="type">呼び出すメソッドが存在する型を指定します。</param>
		/// <param name="name">呼び出すメソッドの名前を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/>、<paramref name="type"/> または <paramref name="name"/> が <c>null</c> です。</exception>
		/// <exception cref="ArgumentException"><paramref name="type"/> に <paramref name="name"/> という名前のパブリック メソッドは存在しません。</exception>
		public static void EmitCall(this ILGenerator instance, Type type, string name)
		{
			ContractUtils.RequiresNotNull(type, "type");
			ContractUtils.RequiresNotNull(name, "name");
			var mi = type.GetMethod(name);
			ContractUtils.Requires(mi != null, "type", Strings.TypeDoesNotHaveMethodForName);
			EmitCall(instance, mi);
		}

		/// <summary>指定された型にある指定された名前の指定された引数の型に一致するパブリック メソッドを呼び出す命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="type">呼び出すメソッドが存在する型を指定します。</param>
		/// <param name="name">呼び出すメソッドの名前を指定します。</param>
		/// <param name="paramTypes">呼び出すメソッドの引数の型を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/>、<paramref name="type"/>、<paramref name="name"/> または <paramref name="paramTypes"/> が <c>null</c> です。</exception>
		/// <exception cref="ArgumentException"><paramref name="type"/> に指定されたシグネチャと一致するパブリック メソッドは存在しません。</exception>
		public static void EmitCall(this ILGenerator instance, Type type, string name, Type[] paramTypes)
		{
			ContractUtils.RequiresNotNull(type, "type");
			ContractUtils.RequiresNotNull(name, "name");
			ContractUtils.RequiresNotNull(paramTypes, "paramTypes");
			var mi = type.GetMethod(name, paramTypes);
			ContractUtils.Requires(mi != null, "type", Strings.TypeDoesNotHaveMethodForNameSignature);
			EmitCall(instance, mi);
		}

		#endregion

		#region Constants

		/// <summary><c>null</c> を評価スタックにプッシュする命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> は <c>null</c> です。</exception>
		public static void EmitNull(this ILGenerator instance)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			instance.Emit(OpCodes.Ldnull);
		}

		/// <summary>指定された文字列を評価スタックにプッシュする命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="value">スタックにプッシュする文字列を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> または <paramref name="value"/> は <c>null</c> です。</exception>
		public static void EmitString(this ILGenerator instance, string value)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(value, "value");
			instance.Emit(OpCodes.Ldstr, value);
		}

		/// <summary>指定されたブール値を評価スタックにプッシュする命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="value">スタックにプッシュするブール値を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> は <c>null</c> です。</exception>
		public static void EmitBoolean(this ILGenerator instance, bool value) { EmitInt32(instance, value ? 1 : 0); }

		/// <summary>指定された文字を評価スタックにプッシュする命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="value">スタックにプッシュする文字を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> は <c>null</c> です。</exception>
		public static void EmitChar(this ILGenerator instance, char value)
		{
			EmitInt32(instance, value);
			instance.Emit(OpCodes.Conv_U2);
		}

		/// <summary>指定された 8 ビット符号なし整数を評価スタックにプッシュする命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="value">スタックにプッシュする 8 ビット符号なし整数を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> は <c>null</c> です。</exception>
		public static void EmitByte(this ILGenerator instance, byte value)
		{
			EmitInt32(instance, value);
			instance.Emit(OpCodes.Conv_U1);
		}

		/// <summary>指定された 8 ビット符号付き整数を評価スタックにプッシュする命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="value">スタックにプッシュする 8 ビット符号付き整数を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> は <c>null</c> です。</exception>
		[CLSCompliant(false)]
		public static void EmitSByte(this ILGenerator instance, sbyte value)
		{
			EmitInt32(instance, value);
			instance.Emit(OpCodes.Conv_I1);
		}

		/// <summary>指定された 16 ビット符号付き整数を評価スタックにプッシュする命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="value">スタックにプッシュする 16 ビット符号付き整数を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> は <c>null</c> です。</exception>
		public static void EmitInt16(this ILGenerator instance, short value)
		{
			EmitInt32(instance, value);
			instance.Emit(OpCodes.Conv_I2);
		}

		/// <summary>指定された 16 ビット符号なし整数を評価スタックにプッシュする命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="value">スタックにプッシュする 16 ビット符号なし整数を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> は <c>null</c> です。</exception>
		[CLSCompliant(false)]
		public static void EmitUInt16(this ILGenerator instance, ushort value)
		{
			EmitInt32(instance, value);
			instance.Emit(OpCodes.Conv_U2);
		}

		/// <summary>指定された 32 ビット符号付き整数を評価スタックにプッシュする命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="value">スタックにプッシュする 32 ビット符号付き整数を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> は <c>null</c> です。</exception>
		public static void EmitInt32(this ILGenerator instance, int value)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			OpCode c;
			switch (value)
			{
				case -1:
					c = OpCodes.Ldc_I4_M1;
					break;
				case 0:
					c = OpCodes.Ldc_I4_0;
					break;
				case 1:
					c = OpCodes.Ldc_I4_1;
					break;
				case 2:
					c = OpCodes.Ldc_I4_2;
					break;
				case 3:
					c = OpCodes.Ldc_I4_3;
					break;
				case 4:
					c = OpCodes.Ldc_I4_4;
					break;
				case 5:
					c = OpCodes.Ldc_I4_5;
					break;
				case 6:
					c = OpCodes.Ldc_I4_6;
					break;
				case 7:
					c = OpCodes.Ldc_I4_7;
					break;
				case 8:
					c = OpCodes.Ldc_I4_8;
					break;
				default:
					if (value >= sbyte.MinValue && value <= sbyte.MaxValue)
						instance.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
					else
						instance.Emit(OpCodes.Ldc_I4, value);
					return;
			}
			instance.Emit(c);
		}

		/// <summary>指定された 32 ビット符号なし整数を評価スタックにプッシュする命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="value">スタックにプッシュする 32 ビット符号なし整数を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> は <c>null</c> です。</exception>
		[CLSCompliant(false)]
		public static void EmitUInt32(this ILGenerator instance, uint value)
		{
			EmitInt32(instance, (int)value);
			instance.Emit(OpCodes.Conv_U4);
		}

		/// <summary>指定された 64 ビット符号付き整数を評価スタックにプッシュする命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="value">スタックにプッシュする 64 ビット符号付き整数を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> は <c>null</c> です。</exception>
		public static void EmitInt64(this ILGenerator instance, long value)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			instance.Emit(OpCodes.Ldc_I8, value);
		}

		/// <summary>指定された 64 ビット符号なし整数を評価スタックにプッシュする命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="value">スタックにプッシュする 64 ビット符号なし整数を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> は <c>null</c> です。</exception>
		[CLSCompliant(false)]
		public static void EmitUInt64(this ILGenerator instance, ulong value)
		{
			EmitInt64(instance, (long)value);
			instance.Emit(OpCodes.Conv_U8);
		}

		/// <summary>指定された倍精度浮動小数点数を評価スタックにプッシュする命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="value">スタックにプッシュする倍精度浮動小数点数を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> は <c>null</c> です。</exception>
		public static void EmitDouble(this ILGenerator instance, double value)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			instance.Emit(OpCodes.Ldc_R8, value);
		}

		/// <summary>指定された単精度浮動小数点数を評価スタックにプッシュする命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="value">スタックにプッシュする単精度浮動小数点数を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> は <c>null</c> です。</exception>
		public static void EmitSingle(this ILGenerator instance, float value)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			instance.Emit(OpCodes.Ldc_R4, value);
		}

		// Note: we support emitting a lot more things as IL constants than Linq does
		static bool TryEmitConstant(ILGenerator instance, object value, Type type)
		{
			Debug.Assert(value != null);
			// Handle the easy cases
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Boolean:
					EmitBoolean(instance, (bool)value);
					return true;
				case TypeCode.SByte:
					EmitSByte(instance, (sbyte)value);
					return true;
				case TypeCode.Int16:
					EmitInt16(instance, (short)value);
					return true;
				case TypeCode.Int32:
					EmitInt32(instance, (int)value);
					return true;
				case TypeCode.Int64:
					EmitInt64(instance, (long)value);
					return true;
				case TypeCode.Single:
					EmitSingle(instance, (float)value);
					return true;
				case TypeCode.Double:
					EmitDouble(instance, (double)value);
					return true;
				case TypeCode.Char:
					EmitChar(instance, (char)value);
					return true;
				case TypeCode.Byte:
					EmitByte(instance, (byte)value);
					return true;
				case TypeCode.UInt16:
					EmitUInt16(instance, (ushort)value);
					return true;
				case TypeCode.UInt32:
					EmitUInt32(instance, (uint)value);
					return true;
				case TypeCode.UInt64:
					EmitUInt64(instance, (ulong)value);
					return true;
				case TypeCode.Decimal:
					EmitDecimal(instance, (decimal)value);
					return true;
				case TypeCode.String:
					EmitString(instance, (string)value);
					return true;
			}
			// Check for a few more types that we support emitting as constants
			var t = value as Type;
			if (t != null && ShouldLdtoken(t))
			{
				EmitType(instance, t);
				return true;
			}
			var mb = value as MethodBase;
			if (mb != null && ShouldLdtoken(mb))
			{
				if (mb.MemberType == MemberTypes.Constructor)
					instance.Emit(OpCodes.Ldtoken, (ConstructorInfo)mb);
				else
					instance.Emit(OpCodes.Ldtoken, (MethodInfo)mb);
				if (mb.DeclaringType != null && mb.DeclaringType.IsGenericType)
				{
					instance.Emit(OpCodes.Ldtoken, mb.DeclaringType);
					EmitCall(instance, new Func<RuntimeMethodHandle, RuntimeTypeHandle, MethodBase>(MethodBase.GetMethodFromHandle).Method);
				}
				else
					EmitCall(instance, new Func<RuntimeMethodHandle, MethodBase>(MethodBase.GetMethodFromHandle).Method);
				type = TypeUtils.GetConstantType(type);
				if (type != typeof(MethodBase))
					instance.Emit(OpCodes.Castclass, type);
				return true;
			}
			return false;
		}

		// TODO: Can we always ldtoken and let restrictedSkipVisibility sort things out?
		/// <summary>指定された型を <c>Ldtoken</c> 命令を使用して読み込んだ方がよいかどうかを判断します。</summary>
		/// <param name="t">判断する型を指定します。</param>
		/// <returns>指定された型を <c>Ldtoken</c> 命令を使用して読み込んだ方がよい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool ShouldLdtoken(Type t) { return t is TypeBuilder || t.IsGenericParameter || t.IsVisible; }

		/// <summary>指定されたメソッドまたはコンストラクタを <c>Ldtoken</c> 命令を使用して読み込んだ方がよいかどうかを判断します。</summary>
		/// <param name="mb">判断するメソッドまたはコンストラクタを指定します。</param>
		/// <returns>指定されたメソッドまたはコンストラクタを <c>Ldtoken</c> 命令を使用して読み込んだ方がよい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool ShouldLdtoken(MethodBase mb)
		{
			// Can't ldtoken on a DynamicMethod
			if (mb is DynamicMethod)
				return false;
			var dt = mb.DeclaringType;
			return dt == null || ShouldLdtoken(dt);
		}

		#endregion

		#region Conversions

		/// <summary>指定された 2 つの型の間での暗黙的なキャスト命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="from">変換元の型を指定します。</param>
		/// <param name="to">変換先の型を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/>、<paramref name="from"/> または <paramref name="to"/> が <c>null</c> です。</exception>
		/// <exception cref="ArgumentException"><paramref name="from"/> から <paramref name="to"/> の間に暗黙的な型変換が存在しません。</exception>
		public static void EmitImplicitCast(this ILGenerator instance, Type from, Type to)
		{
			if (!TryEmitCast(instance, from, to, true))
				throw Error.NoImplicitCast(from, to);
		}

		/// <summary>指定された 2 つの型の間での明示的なキャスト命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="from">変換元の型を指定します。</param>
		/// <param name="to">変換先の型を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/>、<paramref name="from"/> または <paramref name="to"/> が <c>null</c> です。</exception>
		/// <exception cref="ArgumentException"><paramref name="from"/> から <paramref name="to"/> の間に明示的な型変換が存在しません。</exception>
		public static void EmitExplicitCast(this ILGenerator instance, Type from, Type to)
		{
			if (!TryEmitCast(instance, from, to, false))
				throw Error.NoExplicitCast(from, to);
		}

		/// <summary>指定された 2 つの型の間での暗黙的なキャストが存在すればキャスト命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="from">変換元の型を指定します。</param>
		/// <param name="to">変換先の型を指定します。</param>
		/// <returns>2 つの型の間で暗黙的な変換が存在した場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/>、<paramref name="from"/> または <paramref name="to"/> が <c>null</c> です。</exception>
		public static bool TryEmitImplicitCast(this ILGenerator instance, Type from, Type to) { return TryEmitCast(instance, from, to, true); }

		/// <summary>指定された 2 つの型の間での明示的なキャストが存在すればキャスト命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="from">変換元の型を指定します。</param>
		/// <param name="to">変換先の型を指定します。</param>
		/// <returns>2 つの型の間で明示的な変換が存在した場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/>、<paramref name="from"/> または <paramref name="to"/> が <c>null</c> です。</exception>
		public static bool TryEmitExplicitCast(this ILGenerator instance, Type from, Type to) { return TryEmitCast(instance, from, to, false); }

		static bool TryEmitCast(ILGenerator instance, Type from, Type to, bool implicitOnly)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(from, "from");
			ContractUtils.RequiresNotNull(to, "to");
			// No cast necessary if identical types
			if (from == to)
				return true;
			if (to.IsAssignableFrom(from))
			{
				// T -> Nullable<T>
				if (TypeUtils.IsNullableType(to))
				{
					var nonNullableTo = TypeUtils.GetNonNullableType(to);
					if (TryEmitCast(instance, from, nonNullableTo, true))
					{
						EmitNew(instance, to.GetConstructor(new[] { nonNullableTo }));
						return true;
					}
					return false;
				}
				if (from.IsValueType && to == typeof(object) || to.IsInterface || from.IsEnum && to == typeof(Enum))
				{
					EmitBoxing(instance, from);
					return true;
				}
				// They are assignable and reference types.
				return true;
			}
			if (to == typeof(void))
			{
				instance.Emit(OpCodes.Pop);
				return true;
			}
			if (to.IsValueType && from == typeof(object))
			{
				if (implicitOnly)
					return false;
				instance.Emit(OpCodes.Unbox_Any, to);
				return true;
			}
			if (to.IsValueType != from.IsValueType)
				return false;
			if (!to.IsValueType)
			{
				if (implicitOnly)
					return false;
				instance.Emit(OpCodes.Castclass, to);
				return true;
			}
			if (to.IsEnum)
				to = Enum.GetUnderlyingType(to);
			if (from.IsEnum)
				from = Enum.GetUnderlyingType(from);
			if (to == from)
				return true;
			if (TryEmitNumericCast(instance, from, to, implicitOnly))
				return true;
			return false;
		}

		/// <summary>指定された 2 つの数値型の間でキャストが存在した場合はキャスト命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="from">変換元の型を指定します。</param>
		/// <param name="to">変換先の型を指定します。</param>
		/// <param name="implicitOnly">暗黙的な変換のみを行うかどうかを示す値を指定します。</param>
		/// <returns>2 つの型の間で暗黙的または明示的な変換が存在した場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> が <c>null</c> です。</exception>
		public static bool TryEmitNumericCast(this ILGenerator instance, Type from, Type to, bool implicitOnly)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			TypeCode fc = Type.GetTypeCode(from);
			TypeCode tc = Type.GetTypeCode(to);
			if (!TypeUtils.IsNumeric(fc) || !TypeUtils.IsNumeric(tc))
				return false; // numeric <-> non-numeric
			bool isImplicit = TypeUtils.IsNumericImplicitlyConvertible(fc, tc);
			if (implicitOnly && !isImplicit)
				return false;
			// IL conversion instruction also needed for floating point -> integer:
			if (!isImplicit || tc == TypeCode.Single || tc == TypeCode.Double || tc == TypeCode.Int64 || tc == TypeCode.UInt64)
			{
				switch (tc)
				{
					case TypeCode.SByte:
						instance.Emit(OpCodes.Conv_I1);
						break;
					case TypeCode.Int16:
						instance.Emit(OpCodes.Conv_I2);
						break;
					case TypeCode.Int32:
						instance.Emit(OpCodes.Conv_I4);
						break;
					case TypeCode.Int64:
						instance.Emit(OpCodes.Conv_I8);
						break;
					case TypeCode.Byte:
						instance.Emit(OpCodes.Conv_U1);
						break;
					case TypeCode.UInt16:
						instance.Emit(OpCodes.Conv_U2);
						break;
					case TypeCode.UInt32:
						instance.Emit(OpCodes.Conv_U4);
						break;
					case TypeCode.UInt64:
						instance.Emit(OpCodes.Conv_U8);
						break;
					case TypeCode.Single:
						instance.Emit(OpCodes.Conv_R4);
						break;
					case TypeCode.Double:
						instance.Emit(OpCodes.Conv_R8);
						break;
					default:
						throw Assert.Unreachable;
				}
			}
			return true;
		}

		// TODO: we should try to remove this. It caused a 4x degrade in a
		// conversion intense lambda. And also seems like a bad idea to mess
		// with CLR boxing semantics.
		/// <summary>指定された型をボックス化する命令を発行します。このメソッドは <see cref="System.Void"/> 型を <c>null</c> 参照にボックス化します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="type">ボックス化される値の型を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> または <paramref name="type"/> が <c>null</c> です。</exception>
		public static void EmitBoxing(this ILGenerator instance, Type type)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(type, "type");
			if (type.IsValueType)
			{
				if (type == typeof(void))
					instance.Emit(OpCodes.Ldnull);
				else if (type == typeof(int))
					EmitCall(instance, new Func<int, object>(ScriptingRuntimeHelpers.Int32ToObject).Method);
				else if (type == typeof(bool))
					EmitCall(instance, new Func<bool, object>(ScriptingRuntimeHelpers.BooleanToObject).Method);
				else
					instance.Emit(OpCodes.Box, type);
			}
			else if (type.IsGenericParameter)
				instance.Emit(OpCodes.Box, type);
		}

		#endregion

		#region Arrays

		/// <summary>各要素が指定されたコレクションにより初期化された新しい配列を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="items">配列の各要素を初期化するコレクションを指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> または <paramref name="items"/> が <c>null</c> です。</exception>
		/// <exception cref="ArgumentException">コレクションの要素を表す定数を発行できません。</exception>
		public static void EmitArray<T>(this ILGenerator instance, ICollection<T> items)
		{
			ContractUtils.RequiresNotNull(items, "items");
			EmitInt32(instance, items.Count);
			instance.Emit(OpCodes.Newarr, typeof(T));
			int i = 0;
			foreach (var item in items)
			{
				instance.Emit(OpCodes.Dup);
				EmitInt32(instance, i++);
				if (item == null)
					EmitNull(instance);
				else if (!TryEmitConstant(instance, item, item.GetType()))
					throw Error.CanotEmitConstant(item, item.GetType());
				EmitStoreElement(instance, typeof(T));
			}
		}

		/// <summary>指定されたデリゲートを呼び出すことにより、各要素が初期化された新しい配列を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="elementType">配列の要素の型を指定します。</param>
		/// <param name="count">配列の要素数を指定します。</param>
		/// <param name="emitter">初期化に使用する各要素を発行するデリゲートを指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/>、<paramref name="elementType"/> または <paramref name="emitter"/> が <c>null</c> です。</exception>
		public static void EmitArray(this ILGenerator instance, Type elementType, int count, Action<int> emitter)
		{
			ContractUtils.RequiresNotNull(elementType, "elementType");
			ContractUtils.RequiresNotNull(emitter, "emitter");
			ContractUtils.Requires(count >= 0, "count", Strings.CountCannotBeNegative);
			EmitInt32(instance, count);
			instance.Emit(OpCodes.Newarr, elementType);
			for (int i = 0; i < count; i++)
			{
				instance.Emit(OpCodes.Dup);
				EmitInt32(instance, i);
				emitter(i);
				EmitStoreElement(instance, elementType);
			}
		}

		#endregion

		#region Support for emitting constants

		/// <summary>指定された 10 進数を評価スタックにプッシュする命令を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="value">スタックにプッシュする 10 進数を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> が <c>null</c> です。</exception>
		public static void EmitDecimal(this ILGenerator instance, decimal value)
		{
			if (value == decimal.Zero)
			{
				EmitFieldGet(instance, typeof(decimal).GetField("Zero"));
				return;
			}
			if (value == decimal.One)
			{
				EmitFieldGet(instance, typeof(decimal).GetField("One"));
				return;
			}
			if (value == decimal.MinusOne)
			{
				EmitFieldGet(instance, typeof(decimal).GetField("MinusOne"));
				return;
			}
			if (decimal.Truncate(value) == value)
			{
				if (int.MinValue <= value && value <= int.MaxValue)
				{
					EmitInt32(instance, (int)value);
					EmitNew(instance, typeof(decimal).GetConstructor(new[] { typeof(int) }));
					return;
				}
				else if (long.MinValue <= value && value <= long.MaxValue)
				{
					EmitInt64(instance, (long)value);
					EmitNew(instance, typeof(decimal).GetConstructor(new[] { typeof(long) }));
					return;
				}
			}
			var bits = decimal.GetBits(value);
			EmitInt32(instance, bits[0]);
			EmitInt32(instance, bits[1]);
			EmitInt32(instance, bits[2]);
			EmitBoolean(instance, (bits[3] & 0x80000000) != 0);
			EmitByte(instance, (byte)(bits[3] >> 16));
			EmitNew(instance, typeof(decimal).GetConstructor(new[] { typeof(int), typeof(int), typeof(int), typeof(bool), typeof(byte) }));
		}

		/// <summary>指定された型において値が存在しないことを示す値を発行します。</summary>
		/// <param name="instance">命令を書き込む <see cref="ILGenerator"/> を指定します。</param>
		/// <param name="type">存在しないことを表す値を発行する型を指定します。</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> が <c>null</c> です。</exception>
		public static void EmitMissingValue(this ILGenerator instance, Type type)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			switch (Type.GetTypeCode(type))
			{
				default:
				case TypeCode.Object:
				case TypeCode.DateTime:
					if (type == typeof(object))
						instance.Emit(OpCodes.Ldsfld, typeof(Missing).GetField("Value")); // parameter of type object receives the actual Missing value
					else if (!type.IsValueType)
						EmitNull(instance); // reference type
					else if (type.IsSealed && !type.IsEnum)
					{
						var lb = instance.DeclareLocal(type);
						instance.Emit(OpCodes.Ldloca, lb);
						instance.Emit(OpCodes.Initobj, type);
						instance.Emit(OpCodes.Ldloc, lb);
					}
					else
						throw Error.NoDefaultValue();
					break;
				case TypeCode.Empty:
				case TypeCode.DBNull:
				case TypeCode.String:
					EmitNull(instance);
					break;
				case TypeCode.Boolean:
				case TypeCode.Char:
				case TypeCode.SByte:
				case TypeCode.Byte:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
					EmitInt32(instance, 0);
					break;
				case TypeCode.Int64:
				case TypeCode.UInt64:
					EmitInt64(instance, 0);
					break;
				case TypeCode.Single:
					EmitSingle(instance, default(float));
					break;
				case TypeCode.Double:
					EmitDouble(instance, default(double));
					break;
				case TypeCode.Decimal:
					EmitDecimal(instance, default(decimal));
					break;
			}
		}

		#endregion
	}
}
