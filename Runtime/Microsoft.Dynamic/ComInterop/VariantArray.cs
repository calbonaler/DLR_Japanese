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
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.Scripting.ComInterop
{

	[StructLayout(LayoutKind.Sequential)]
	struct VariantArray1
	{
		public Variant Element0;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct VariantArray2
	{
		public Variant Element0, Element1;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct VariantArray4
	{
		public Variant Element0, Element1, Element2, Element3;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct VariantArray8
	{
		public Variant Element0, Element1, Element2, Element3, Element4, Element5, Element6, Element7;
	}

	// �w�肳�ꂽ���̒l�ɑ΂��� VariantArray �\���̂��擾����w���p�[ �N���X�ł��B�K�v�ł���΍\���̂��쐬���܂��B
	// ����𗘗p���闝�R�́A���c���[�� stackalloc ��s��������K�v���Ȃ����߂ł���A���̂��ߒ��� Variant �̔z����쐬�ł��܂���B
	static class VariantArray
	{
		// ����͔��ɏ����̗v�f���������Ȃ��̂ŁA�f�B�N�V���i���ɂ���K�v�͂���܂���B
		// (28 �ȉ��������ł��܂����A���ۂ� 0 - 2 �����i�[����܂���B)
		static readonly List<Type> _generatedTypes = new List<Type>(0);

		internal static MemberExpression GetStructField(ParameterExpression variantArray, int field) { return Expression.Field(variantArray, "Element" + field); }

		internal static Type GetStructType(int args)
		{
			Debug.Assert(args >= 0);
			if (args <= 1)
				return typeof(VariantArray1);
			if (args <= 2)
				return typeof(VariantArray2);
			if (args <= 4)
				return typeof(VariantArray4);
			if (args <= 8)
				return typeof(VariantArray8);
			int size = 1;
			while (size < args)
				size *= 2;
			lock (_generatedTypes)
			{
				// �����̌^������
				var type = _generatedTypes.Find(x => ("VariantArray" + size).Equals(x.Name, StringComparison.Ordinal));
				if (type != null)
					return type;
				// ������΍쐬����
				var builder = UnsafeMethods.DynamicModule.DefineType("VariantArray" + size, TypeAttributes.NotPublic | TypeAttributes.SequentialLayout, typeof(ValueType));
				for (int i = 0; i < size; i++)
					builder.DefineField("Element" + i, typeof(Variant), FieldAttributes.Public);
				_generatedTypes.Add(type = builder.CreateType());
				return type;
			}
		}
	}
}