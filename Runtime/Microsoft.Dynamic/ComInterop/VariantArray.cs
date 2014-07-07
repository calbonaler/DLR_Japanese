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

	// 指定された個数の値に対する VariantArray 構造体を取得するヘルパー クラスです。必要であれば構造体を作成します。
	// これを利用する理由は、式ツリーで stackalloc やピンをする必要がないためであり、そのため直接 Variant の配列を作成できません。
	static class VariantArray
	{
		// これは非常に少数の要素しか持たないので、ディクショナリにする必要はありません。
		// (28 個以下が生成できますが、実際は 0 - 2 個しか格納されません。)
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
				// 既存の型を検索
				var type = _generatedTypes.Find(x => ("VariantArray" + size).Equals(x.Name, StringComparison.Ordinal));
				if (type != null)
					return type;
				// 無ければ作成する
				var builder = UnsafeMethods.DynamicModule.DefineType("VariantArray" + size, TypeAttributes.NotPublic | TypeAttributes.SequentialLayout, typeof(ValueType));
				for (int i = 0; i < size; i++)
					builder.DefineField("Element" + i, typeof(Variant), FieldAttributes.Public);
				_generatedTypes.Add(type = builder.CreateType());
				return type;
			}
		}
	}
}