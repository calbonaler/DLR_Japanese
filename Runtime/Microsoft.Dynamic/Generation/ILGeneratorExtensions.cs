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
	/// <summary><see cref="ILGenerator"/> �Ɋւ���w���p�[ ���\�b�h���i�[���܂��B</summary>
	public static class ILGeneratorExtensions
	{
		#region Instruction helpers

		/// <summary>�w�肳�ꂽ�ʒu�ɂ��������]���X�^�b�N�ɓǂݍ��ޖ��߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="index">
		/// �X�^�b�N�ɓǂݍ��ވ����̃C���f�b�N�X���w�肵�܂��B
		/// �C���X�^���X ���\�b�h�̏ꍇ�A0 �� this �I�u�W�F�N�g��\���A�������X�g�̍��[�̈����̓C���f�b�N�X 1 �ɂȂ�܂��B
		/// �ÓI���\�b�h�̏ꍇ�� 0 ���������X�g�̍��[�̈�����\���܂��B
		/// </param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �� <c>null</c> �ł��B</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> �� 0 �ȏ�ł���K�v������܂��B</exception>
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

		/// <summary>�w�肳�ꂽ�ʒu�ɂ�������̃A�h���X��]���X�^�b�N�ɓǂݍ��ޖ��߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="index">
		/// �X�^�b�N�ɓǂݍ��ރA�h���X�ɑΉ���������̃C���f�b�N�X���w�肵�܂��B
		/// �C���X�^���X ���\�b�h�̏ꍇ�A0 �� this �I�u�W�F�N�g��\���A�������X�g�̍��[�̈����̓C���f�b�N�X 1 �ɂȂ�܂��B
		/// �ÓI���\�b�h�̏ꍇ�� 0 ���������X�g�̍��[�̈�����\���܂��B
		/// </param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �� <c>null</c> �ł��B</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> �� 0 �ȏ�ł���K�v������܂��B</exception>
		public static void EmitLoadArgAddress(this ILGenerator instance, int index)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresArrayIndex(int.MaxValue, index, "index");
			if (index <= byte.MaxValue)
				instance.Emit(OpCodes.Ldarga_S, (byte)index);
			else
				instance.Emit(OpCodes.Ldarga, index);
		}

		/// <summary>�]���X�^�b�N�̈�ԏ�ɂ���l���w�肳�ꂽ�ʒu�ɂ�������Ɋi�[���閽�߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="index">
		/// �X�^�b�N����l���i�[���������̃C���f�b�N�X���w�肵�܂��B
		/// �C���X�^���X ���\�b�h�̏ꍇ�A0 �� this �I�u�W�F�N�g��\���A�������X�g�̍��[�̈����̓C���f�b�N�X 1 �ɂȂ�܂��B
		/// �ÓI���\�b�h�̏ꍇ�� 0 ���������X�g�̍��[�̈�����\���܂��B
		/// </param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �� <c>null</c> �ł��B</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> �� 0 �ȏ�ł���K�v������܂��B</exception>
		public static void EmitStoreArg(this ILGenerator instance, int index)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresArrayIndex(int.MaxValue, index, "index");
			if (index <= byte.MaxValue)
				instance.Emit(OpCodes.Starg_S, (byte)index);
			else
				instance.Emit(OpCodes.Starg, index);
		}

		/// <summary>�]���X�^�b�N�ɂ��łɓǂݍ��܂�Ă���A�h���X�ɂ���w�肳�ꂽ�^�̃I�u�W�F�N�g���X�^�b�N�ɓǂݍ��ޖ��߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="type">�X�^�b�N�ɓǂݍ��ރI�u�W�F�N�g�̌^���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �܂��� <paramref name="type"/> �� <c>null</c> �ł��B</exception>
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

		/// <summary>�]���X�^�b�N�ɂ��łɓǂݍ��܂�Ă���w�肳�ꂽ�^�̃I�u�W�F�N�g���A�������X�^�b�N�ɓǂݍ��܂�Ă���A�h���X�Ɋi�[���閽�߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="type">�X�^�b�N����A�h���X�Ɋi�[����I�u�W�F�N�g�̌^���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �܂��� <paramref name="type"/> �� <c>null</c> �ł��B</exception>
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

		/// <summary>�]���X�^�b�N�ɂ��łɓǂݍ��܂�Ă���z��́A�������X�^�b�N�ɓǂݍ��܂�Ă���C���f�b�N�X�ɂ���v�f���X�^�b�N�ɓǂݍ��ޖ��߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="type">�z�񂩂�X�^�b�N�ɓǂݍ��ޗv�f�̌^���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �܂��� <paramref name="type"/> �� <c>null</c> �ł��B</exception>
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

		/// <summary>�]���X�^�b�N�ɂ��łɓǂݍ��܂�Ă���z��́A�������X�^�b�N�ɓǂݍ��܂�Ă���C���f�b�N�X�ɂ���v�f�ɁA�X�^�b�N����l���i�[���閽�߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="type">�X�^�b�N����z��Ɋi�[����v�f�̌^���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �܂��� <paramref name="type"/> �� <c>null</c> �ł��B</exception>
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

		/// <summary>�]���X�^�b�N�Ɏw�肳�ꂽ <see cref="System.Type"/> ��ǂݍ��ޖ��߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="type">�X�^�b�N�ɓǂݍ��� <see cref="System.Type"/> �I�u�W�F�N�g���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �܂��� <paramref name="type"/> �� <c>null</c> �ł��B</exception>
		public static void EmitType(this ILGenerator instance, Type type)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(type, "type");
			instance.Emit(OpCodes.Ldtoken, type);
			EmitCall(instance, new Func<RuntimeTypeHandle, Type>(Type.GetTypeFromHandle).Method);
		}

		/// <summary>�]���X�^�b�N�ɂ��łɓǂݍ��܂�Ă���I�u�W�F�N�g���w�肳�ꂽ�^�Ƀ{�b�N�X���������閽�߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="type">�{�b�N�X���������ꂽ��̃I�u�W�F�N�g�̌^���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �܂��� <paramref name="type"/> �� <c>null</c> �ł��B</exception>
		public static void EmitUnbox(this ILGenerator instance, Type type)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(type, "type");
			instance.Emit(OpCodes.Unbox_Any, type);
		}

		#endregion

		#region Fields, properties and methods

		/// <summary>�w�肳�ꂽ�^�ɂ���w�肳�ꂽ���O�̃p�u���b�N �v���p�e�B�̒l��]���X�^�b�N�ɓǂݍ��ޖ��߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="type">�l��ǂݍ��ރv���p�e�B�����݂���^���w�肵�܂��B</param>
		/// <param name="name">�l��ǂݍ��ރv���p�e�B�̖��O���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/>�A<paramref name="type"/> �܂��� <paramref name="name"/> �� <c>null</c> �ł��B</exception>
		/// <exception cref="ArgumentException"><paramref name="type"/> �� <paramref name="name"/> �Ƃ������O�̃p�u���b�N �v���p�e�B�͑��݂��܂���B</exception>
		/// <exception cref="InvalidOperationException">�v���p�e�B�͏������ݐ�p�œǂݎ�邱�Ƃ͂ł��܂���B</exception>
		public static void EmitPropertyGet(this ILGenerator instance, Type type, string name)
		{
			ContractUtils.RequiresNotNull(type, "type");
			ContractUtils.RequiresNotNull(name, "name");
			var pi = type.GetProperty(name);
			ContractUtils.Requires(pi != null, "name", Strings.PropertyDoesNotExist);
			EmitPropertyGet(instance, pi);
		}

		/// <summary>�w�肳�ꂽ <see cref="PropertyInfo"/> �ɂ���ĕ\�����v���p�e�B�̒l��]���X�^�b�N�ɓǂݍ��ޖ��߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="pi">�l��ǂݍ��ރv���p�e�B��\�� <see cref="PropertyInfo"/> ���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �܂��� <paramref name="pi"/> �� <c>null</c> �ł��B</exception>
		/// <exception cref="InvalidOperationException">�v���p�e�B�͏������ݐ�p�œǂݎ�邱�Ƃ͂ł��܂���B</exception>
		public static void EmitPropertyGet(this ILGenerator instance, PropertyInfo pi)
		{
			ContractUtils.RequiresNotNull(pi, "pi");
			if (!pi.CanRead)
				throw Error.CantReadProperty();
			EmitCall(instance, pi.GetGetMethod());
		}

		/// <summary>�w�肳�ꂽ�^�ɂ���w�肳�ꂽ���O�̃p�u���b�N �v���p�e�B�ɕ]���X�^�b�N�̈�ԏ�ɂ���l��ݒ肷�閽�߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="type">�l��ݒ肷��v���p�e�B�����݂���^���w�肵�܂��B</param>
		/// <param name="name">�l��ݒ肷��v���p�e�B�̖��O���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/>�A<paramref name="type"/> �܂��� <paramref name="name"/> �� <c>null</c> �ł��B</exception>
		/// <exception cref="ArgumentException"><paramref name="type"/> �� <paramref name="name"/> �Ƃ������O�̃p�u���b�N �v���p�e�B�͑��݂��܂���B</exception>
		/// <exception cref="InvalidOperationException">�v���p�e�B�͓ǂݎ���p�ŏ������ނ��Ƃ͂ł��܂���B</exception>
		public static void EmitPropertySet(this ILGenerator instance, Type type, string name)
		{
			ContractUtils.RequiresNotNull(type, "type");
			ContractUtils.RequiresNotNull(name, "name");
			var pi = type.GetProperty(name);
			ContractUtils.Requires(pi != null, "name", Strings.PropertyDoesNotExist);
			EmitPropertySet(instance, pi);
		}

		/// <summary>�w�肳�ꂽ <see cref="PropertyInfo"/> �ɂ���ĕ\�����v���p�e�B�ɕ]���X�^�b�N�̈�ԏ�ɂ���l��ݒ肷�閽�߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="pi">�l��ݒ肷��v���p�e�B��\�� <see cref="PropertyInfo"/> ���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �܂��� <paramref name="pi"/> �� <c>null</c> �ł��B</exception>
		/// <exception cref="InvalidOperationException">�v���p�e�B�͓ǂݎ���p�ŏ������ނ��Ƃ͂ł��܂���B</exception>
		public static void EmitPropertySet(this ILGenerator instance, PropertyInfo pi)
		{
			ContractUtils.RequiresNotNull(pi, "pi");
			if (!pi.CanWrite)
				throw Error.CantWriteProperty();
			EmitCall(instance, pi.GetSetMethod());
		}

		/// <summary>�w�肳�ꂽ <see cref="FieldInfo"/> �ɂ���ĕ\�����t�B�[���h�̃A�h���X��]���X�^�b�N�ɓǂݍ��ޖ��߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="fi">�A�h���X��ǂݍ��ރt�B�[���h��\�� <see cref="FieldInfo"/> ���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �܂��� <paramref name="fi"/> �� <c>null</c> �ł��B</exception>
		public static void EmitFieldAddress(this ILGenerator instance, FieldInfo fi)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(fi, "fi");
			if (fi.IsStatic)
				instance.Emit(OpCodes.Ldsflda, fi);
			else
				instance.Emit(OpCodes.Ldflda, fi);
		}

		/// <summary>�w�肳�ꂽ�^�ɂ���w�肳�ꂽ���O�̃p�u���b�N �t�B�[���h�̒l��]���X�^�b�N�ɓǂݍ��ޖ��߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="type">�l��ǂݍ��ރt�B�[���h�����݂���^���w�肵�܂��B</param>
		/// <param name="name">�l��ǂݍ��ރt�B�[���h�̖��O���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/>�A<paramref name="type"/> �܂��� <paramref name="name"/> �� <c>null</c> �ł��B</exception>
		/// <exception cref="ArgumentException"><paramref name="type"/> �� <paramref name="name"/> �Ƃ������O�̃p�u���b�N �t�B�[���h�͑��݂��܂���B</exception>
		public static void EmitFieldGet(this ILGenerator instance, Type type, String name)
		{
			ContractUtils.RequiresNotNull(type, "type");
			ContractUtils.RequiresNotNull(name, "name");
			var fi = type.GetField(name);
			ContractUtils.Requires(fi != null, "name", Strings.FieldDoesNotExist);
			EmitFieldGet(instance, fi);
		}

		/// <summary>�w�肳�ꂽ�^�ɂ���w�肳�ꂽ���O�̃p�u���b�N �t�B�[���h�ɕ]���X�^�b�N�̈�ԏ�ɂ���l��ݒ肷�閽�߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="type">�l��ݒ肷��t�B�[���h�����݂���^���w�肵�܂��B</param>
		/// <param name="name">�l��ݒ肷��t�B�[���h�̖��O���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/>�A<paramref name="type"/> �܂��� <paramref name="name"/> �� <c>null</c> �ł��B</exception>
		/// <exception cref="ArgumentException"><paramref name="type"/> �� <paramref name="name"/> �Ƃ������O�̃p�u���b�N �t�B�[���h�͑��݂��܂���B</exception>
		public static void EmitFieldSet(this ILGenerator instance, Type type, String name)
		{
			ContractUtils.RequiresNotNull(type, "type");
			ContractUtils.RequiresNotNull(name, "name");
			var fi = type.GetField(name);
			ContractUtils.Requires(fi != null, "name", Strings.FieldDoesNotExist);
			EmitFieldSet(instance, fi);
		}

		/// <summary>�w�肳�ꂽ <see cref="FieldInfo"/> �ɂ���ĕ\�����t�B�[���h�̒l��]���X�^�b�N�ɓǂݍ��ޖ��߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="fi">�l��ǂݍ��ރt�B�[���h��\�� <see cref="FieldInfo"/> ���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �܂��� <paramref name="fi"/> �� <c>null</c> �ł��B</exception>
		public static void EmitFieldGet(this ILGenerator instance, FieldInfo fi)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(fi, "fi");
			if (fi.IsStatic)
				instance.Emit(OpCodes.Ldsfld, fi);
			else
				instance.Emit(OpCodes.Ldfld, fi);
		}

		/// <summary>�w�肳�ꂽ <see cref="FieldInfo"/> �ɂ���ĕ\�����t�B�[���h�ɕ]���X�^�b�N�̈�ԏ�ɂ���l��ݒ肷�閽�߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="fi">�l��ݒ肷��t�B�[���h��\�� <see cref="FieldInfo"/> ���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �܂��� <paramref name="fi"/> �� <c>null</c> �ł��B</exception>
		public static void EmitFieldSet(this ILGenerator instance, FieldInfo fi)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(fi, "fi");
			if (fi.IsStatic)
				instance.Emit(OpCodes.Stsfld, fi);
			else
				instance.Emit(OpCodes.Stfld, fi);
		}

		/// <summary>�w�肳�ꂽ <see cref="ConstructorInfo"/> �ɂ���ĕ\�����R���X�g���N�^���Ăяo���ăI�u�W�F�N�g�̐V�����C���X�^���X���쐬���閽�߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="ci">�I�u�W�F�N�g�̏������Ɏg�p����R���X�g���N�^��\�� <see cref="ConstructorInfo"/> ���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �܂��� <paramref name="ci"/> �� <c>null</c> �ł��B</exception>
		/// <exception cref="ArgumentException">�쐬���悤�Ƃ����^�ɂ̓W�F�l���b�N�^�p�����[�^���܂܂�Ă��܂��B</exception>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
		public static void EmitNew(this ILGenerator instance, ConstructorInfo ci)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(ci, "ci");
			if (ci.DeclaringType.ContainsGenericParameters)
				throw Error.IllegalNew_GenericParams(ci.DeclaringType);
			instance.Emit(OpCodes.Newobj, ci);
		}

		/// <summary>�w�肳�ꂽ�^�ɂ���w�肳�ꂽ�����̌^�Ɉ�v����p�u���b�N �R���X�g���N�^���Ăяo���ăI�u�W�F�N�g�̐V�����C���X�^���X���쐬���閽�߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="type">�쐬����I�u�W�F�N�g�̌^���w�肵�܂��B</param>
		/// <param name="paramTypes">�I�u�W�F�N�g�̏������Ɏg�p����R���X�g���N�^�̈����̌^���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/>�A<paramref name="type"/> �܂��� <paramref name="paramTypes"/> �� <c>null</c> �ł��B</exception>
		/// <exception cref="ArgumentException"><paramref name="type"/> �� <paramref name="paramTypes"/> �̈����ƈ�v����p�u���b�N �R���X�g���N�^�����݂��܂���B</exception>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
		public static void EmitNew(this ILGenerator instance, Type type, Type[] paramTypes)
		{
			ContractUtils.RequiresNotNull(type, "type");
			ContractUtils.RequiresNotNull(paramTypes, "paramTypes");
			var ci = type.GetConstructor(paramTypes);
			ContractUtils.Requires(ci != null, "type", Strings.TypeDoesNotHaveConstructorForTheSignature);
			EmitNew(instance, ci);
		}

		/// <summary>�w�肳�ꂽ <see cref="MethodInfo"/> �ɂ���ĕ\����郁�\�b�h���Ăяo�����߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="mi">�Ăяo�����\�b�h��\�� <see cref="MethodInfo"/> ���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �܂��� <paramref name="mi"/> �� <c>null</c> �ł��B</exception>
		public static void EmitCall(this ILGenerator instance, MethodInfo mi)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(mi, "mi");
			if (mi.IsVirtual && !mi.DeclaringType.IsValueType)
				instance.Emit(OpCodes.Callvirt, mi);
			else
				instance.Emit(OpCodes.Call, mi);
		}

		/// <summary>�w�肳�ꂽ�^�ɂ���w�肳�ꂽ���O�̃p�u���b�N ���\�b�h���Ăяo�����߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="type">�Ăяo�����\�b�h�����݂���^���w�肵�܂��B</param>
		/// <param name="name">�Ăяo�����\�b�h�̖��O���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/>�A<paramref name="type"/> �܂��� <paramref name="name"/> �� <c>null</c> �ł��B</exception>
		/// <exception cref="ArgumentException"><paramref name="type"/> �� <paramref name="name"/> �Ƃ������O�̃p�u���b�N ���\�b�h�͑��݂��܂���B</exception>
		public static void EmitCall(this ILGenerator instance, Type type, string name)
		{
			ContractUtils.RequiresNotNull(type, "type");
			ContractUtils.RequiresNotNull(name, "name");
			var mi = type.GetMethod(name);
			ContractUtils.Requires(mi != null, "type", Strings.TypeDoesNotHaveMethodForName);
			EmitCall(instance, mi);
		}

		/// <summary>�w�肳�ꂽ�^�ɂ���w�肳�ꂽ���O�̎w�肳�ꂽ�����̌^�Ɉ�v����p�u���b�N ���\�b�h���Ăяo�����߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="type">�Ăяo�����\�b�h�����݂���^���w�肵�܂��B</param>
		/// <param name="name">�Ăяo�����\�b�h�̖��O���w�肵�܂��B</param>
		/// <param name="paramTypes">�Ăяo�����\�b�h�̈����̌^���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/>�A<paramref name="type"/>�A<paramref name="name"/> �܂��� <paramref name="paramTypes"/> �� <c>null</c> �ł��B</exception>
		/// <exception cref="ArgumentException"><paramref name="type"/> �Ɏw�肳�ꂽ�V�O�l�`���ƈ�v����p�u���b�N ���\�b�h�͑��݂��܂���B</exception>
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

		/// <summary><c>null</c> ��]���X�^�b�N�Ƀv�b�V�����閽�߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �� <c>null</c> �ł��B</exception>
		public static void EmitNull(this ILGenerator instance)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			instance.Emit(OpCodes.Ldnull);
		}

		/// <summary>�w�肳�ꂽ�������]���X�^�b�N�Ƀv�b�V�����閽�߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="value">�X�^�b�N�Ƀv�b�V�����镶������w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �܂��� <paramref name="value"/> �� <c>null</c> �ł��B</exception>
		public static void EmitString(this ILGenerator instance, string value)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(value, "value");
			instance.Emit(OpCodes.Ldstr, value);
		}

		/// <summary>�w�肳�ꂽ�u�[���l��]���X�^�b�N�Ƀv�b�V�����閽�߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="value">�X�^�b�N�Ƀv�b�V������u�[���l���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �� <c>null</c> �ł��B</exception>
		public static void EmitBoolean(this ILGenerator instance, bool value) { EmitInt32(instance, value ? 1 : 0); }

		/// <summary>�w�肳�ꂽ������]���X�^�b�N�Ƀv�b�V�����閽�߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="value">�X�^�b�N�Ƀv�b�V�����镶�����w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �� <c>null</c> �ł��B</exception>
		public static void EmitChar(this ILGenerator instance, char value)
		{
			EmitInt32(instance, value);
			instance.Emit(OpCodes.Conv_U2);
		}

		/// <summary>�w�肳�ꂽ 8 �r�b�g�����Ȃ�������]���X�^�b�N�Ƀv�b�V�����閽�߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="value">�X�^�b�N�Ƀv�b�V������ 8 �r�b�g�����Ȃ��������w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �� <c>null</c> �ł��B</exception>
		public static void EmitByte(this ILGenerator instance, byte value)
		{
			EmitInt32(instance, value);
			instance.Emit(OpCodes.Conv_U1);
		}

		/// <summary>�w�肳�ꂽ 8 �r�b�g�����t��������]���X�^�b�N�Ƀv�b�V�����閽�߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="value">�X�^�b�N�Ƀv�b�V������ 8 �r�b�g�����t���������w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �� <c>null</c> �ł��B</exception>
		[CLSCompliant(false)]
		public static void EmitSByte(this ILGenerator instance, sbyte value)
		{
			EmitInt32(instance, value);
			instance.Emit(OpCodes.Conv_I1);
		}

		/// <summary>�w�肳�ꂽ 16 �r�b�g�����t��������]���X�^�b�N�Ƀv�b�V�����閽�߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="value">�X�^�b�N�Ƀv�b�V������ 16 �r�b�g�����t���������w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �� <c>null</c> �ł��B</exception>
		public static void EmitInt16(this ILGenerator instance, short value)
		{
			EmitInt32(instance, value);
			instance.Emit(OpCodes.Conv_I2);
		}

		/// <summary>�w�肳�ꂽ 16 �r�b�g�����Ȃ�������]���X�^�b�N�Ƀv�b�V�����閽�߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="value">�X�^�b�N�Ƀv�b�V������ 16 �r�b�g�����Ȃ��������w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �� <c>null</c> �ł��B</exception>
		[CLSCompliant(false)]
		public static void EmitUInt16(this ILGenerator instance, ushort value)
		{
			EmitInt32(instance, value);
			instance.Emit(OpCodes.Conv_U2);
		}

		/// <summary>�w�肳�ꂽ 32 �r�b�g�����t��������]���X�^�b�N�Ƀv�b�V�����閽�߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="value">�X�^�b�N�Ƀv�b�V������ 32 �r�b�g�����t���������w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �� <c>null</c> �ł��B</exception>
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

		/// <summary>�w�肳�ꂽ 32 �r�b�g�����Ȃ�������]���X�^�b�N�Ƀv�b�V�����閽�߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="value">�X�^�b�N�Ƀv�b�V������ 32 �r�b�g�����Ȃ��������w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �� <c>null</c> �ł��B</exception>
		[CLSCompliant(false)]
		public static void EmitUInt32(this ILGenerator instance, uint value)
		{
			EmitInt32(instance, (int)value);
			instance.Emit(OpCodes.Conv_U4);
		}

		/// <summary>�w�肳�ꂽ 64 �r�b�g�����t��������]���X�^�b�N�Ƀv�b�V�����閽�߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="value">�X�^�b�N�Ƀv�b�V������ 64 �r�b�g�����t���������w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �� <c>null</c> �ł��B</exception>
		public static void EmitInt64(this ILGenerator instance, long value)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			instance.Emit(OpCodes.Ldc_I8, value);
		}

		/// <summary>�w�肳�ꂽ 64 �r�b�g�����Ȃ�������]���X�^�b�N�Ƀv�b�V�����閽�߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="value">�X�^�b�N�Ƀv�b�V������ 64 �r�b�g�����Ȃ��������w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �� <c>null</c> �ł��B</exception>
		[CLSCompliant(false)]
		public static void EmitUInt64(this ILGenerator instance, ulong value)
		{
			EmitInt64(instance, (long)value);
			instance.Emit(OpCodes.Conv_U8);
		}

		/// <summary>�w�肳�ꂽ�{���x���������_����]���X�^�b�N�Ƀv�b�V�����閽�߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="value">�X�^�b�N�Ƀv�b�V������{���x���������_�����w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �� <c>null</c> �ł��B</exception>
		public static void EmitDouble(this ILGenerator instance, double value)
		{
			ContractUtils.RequiresNotNull(instance, "instance");
			instance.Emit(OpCodes.Ldc_R8, value);
		}

		/// <summary>�w�肳�ꂽ�P���x���������_����]���X�^�b�N�Ƀv�b�V�����閽�߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="value">�X�^�b�N�Ƀv�b�V������P���x���������_�����w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �� <c>null</c> �ł��B</exception>
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
		/// <summary>�w�肳�ꂽ�^�� <c>Ldtoken</c> ���߂��g�p���ēǂݍ��񂾕����悢���ǂ����𔻒f���܂��B</summary>
		/// <param name="t">���f����^���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�^�� <c>Ldtoken</c> ���߂��g�p���ēǂݍ��񂾕����悢�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool ShouldLdtoken(Type t) { return t is TypeBuilder || t.IsGenericParameter || t.IsVisible; }

		/// <summary>�w�肳�ꂽ���\�b�h�܂��̓R���X�g���N�^�� <c>Ldtoken</c> ���߂��g�p���ēǂݍ��񂾕����悢���ǂ����𔻒f���܂��B</summary>
		/// <param name="mb">���f���郁�\�b�h�܂��̓R���X�g���N�^���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ���\�b�h�܂��̓R���X�g���N�^�� <c>Ldtoken</c> ���߂��g�p���ēǂݍ��񂾕����悢�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
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

		/// <summary>�w�肳�ꂽ 2 �̌^�̊Ԃł̈ÖٓI�ȃL���X�g���߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="from">�ϊ����̌^���w�肵�܂��B</param>
		/// <param name="to">�ϊ���̌^���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/>�A<paramref name="from"/> �܂��� <paramref name="to"/> �� <c>null</c> �ł��B</exception>
		/// <exception cref="ArgumentException"><paramref name="from"/> ���� <paramref name="to"/> �̊ԂɈÖٓI�Ȍ^�ϊ������݂��܂���B</exception>
		public static void EmitImplicitCast(this ILGenerator instance, Type from, Type to)
		{
			if (!TryEmitCast(instance, from, to, true))
				throw Error.NoImplicitCast(from, to);
		}

		/// <summary>�w�肳�ꂽ 2 �̌^�̊Ԃł̖����I�ȃL���X�g���߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="from">�ϊ����̌^���w�肵�܂��B</param>
		/// <param name="to">�ϊ���̌^���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/>�A<paramref name="from"/> �܂��� <paramref name="to"/> �� <c>null</c> �ł��B</exception>
		/// <exception cref="ArgumentException"><paramref name="from"/> ���� <paramref name="to"/> �̊Ԃɖ����I�Ȍ^�ϊ������݂��܂���B</exception>
		public static void EmitExplicitCast(this ILGenerator instance, Type from, Type to)
		{
			if (!TryEmitCast(instance, from, to, false))
				throw Error.NoExplicitCast(from, to);
		}

		/// <summary>�w�肳�ꂽ 2 �̌^�̊Ԃł̈ÖٓI�ȃL���X�g�����݂���΃L���X�g���߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="from">�ϊ����̌^���w�肵�܂��B</param>
		/// <param name="to">�ϊ���̌^���w�肵�܂��B</param>
		/// <returns>2 �̌^�̊ԂňÖٓI�ȕϊ������݂����ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/>�A<paramref name="from"/> �܂��� <paramref name="to"/> �� <c>null</c> �ł��B</exception>
		public static bool TryEmitImplicitCast(this ILGenerator instance, Type from, Type to) { return TryEmitCast(instance, from, to, true); }

		/// <summary>�w�肳�ꂽ 2 �̌^�̊Ԃł̖����I�ȃL���X�g�����݂���΃L���X�g���߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="from">�ϊ����̌^���w�肵�܂��B</param>
		/// <param name="to">�ϊ���̌^���w�肵�܂��B</param>
		/// <returns>2 �̌^�̊ԂŖ����I�ȕϊ������݂����ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/>�A<paramref name="from"/> �܂��� <paramref name="to"/> �� <c>null</c> �ł��B</exception>
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

		/// <summary>�w�肳�ꂽ 2 �̐��l�^�̊ԂŃL���X�g�����݂����ꍇ�̓L���X�g���߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="from">�ϊ����̌^���w�肵�܂��B</param>
		/// <param name="to">�ϊ���̌^���w�肵�܂��B</param>
		/// <param name="implicitOnly">�ÖٓI�ȕϊ��݂̂��s�����ǂ����������l���w�肵�܂��B</param>
		/// <returns>2 �̌^�̊ԂňÖٓI�܂��͖����I�ȕϊ������݂����ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �� <c>null</c> �ł��B</exception>
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
		/// <summary>�w�肳�ꂽ�^���{�b�N�X�����閽�߂𔭍s���܂��B���̃��\�b�h�� <see cref="System.Void"/> �^�� <c>null</c> �Q�ƂɃ{�b�N�X�����܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="type">�{�b�N�X�������l�̌^���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �܂��� <paramref name="type"/> �� <c>null</c> �ł��B</exception>
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

		/// <summary>�e�v�f���w�肳�ꂽ�R���N�V�����ɂ�菉�������ꂽ�V�����z��𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="items">�z��̊e�v�f������������R���N�V�������w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �܂��� <paramref name="items"/> �� <c>null</c> �ł��B</exception>
		/// <exception cref="ArgumentException">�R���N�V�����̗v�f��\���萔�𔭍s�ł��܂���B</exception>
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

		/// <summary>�w�肳�ꂽ�f���Q�[�g���Ăяo�����Ƃɂ��A�e�v�f�����������ꂽ�V�����z��𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="elementType">�z��̗v�f�̌^���w�肵�܂��B</param>
		/// <param name="count">�z��̗v�f�����w�肵�܂��B</param>
		/// <param name="emitter">�������Ɏg�p����e�v�f�𔭍s����f���Q�[�g���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/>�A<paramref name="elementType"/> �܂��� <paramref name="emitter"/> �� <c>null</c> �ł��B</exception>
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

		/// <summary>�w�肳�ꂽ 10 �i����]���X�^�b�N�Ƀv�b�V�����閽�߂𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="value">�X�^�b�N�Ƀv�b�V������ 10 �i�����w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �� <c>null</c> �ł��B</exception>
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

		/// <summary>�w�肳�ꂽ�^�ɂ����Ēl�����݂��Ȃ����Ƃ������l�𔭍s���܂��B</summary>
		/// <param name="instance">���߂��������� <see cref="ILGenerator"/> ���w�肵�܂��B</param>
		/// <param name="type">���݂��Ȃ����Ƃ�\���l�𔭍s����^���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException"><paramref name="instance"/> �� <c>null</c> �ł��B</exception>
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
