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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.Scripting.ComInterop
{
	/// <summary>マネージユーザー型が COM 呼び出しの引数として渡される場合の、マーシャリングされる VarEnum 型を判断する方法を提供します。</summary>
	/// <remarks>
	/// (プリミティブ型や COM オブジェクトに対立するものとしての) マネージユーザー型が COM 呼び出しの引数として渡される場合、マーシャリングされる VarEnum 型を判断する必要があります。
	/// 下記の選択肢があります:
	/// 1. 例外を発生させる
	///	   独自バージョンのプリミティブ型を持つ言語は言語の型を使用して COM メソッドを呼ぶことができません。(例えば、IronRuby の string は <see cref="System.String"/> ではありません)
	///	   明示的なキャストが必要になります。
	/// 2. VT_DISPATCH としてマーシャリングする
	///    その場合、COM コードは遅延バインディング方式ですべての API にアクセスすることができますが、古い COM コンポーネントはプリミティブ型を予期している場合、正常に機能しません。
	/// 3. どのプリミティブ型がもっとも近いのかを判断する
	///    これにより COM コンポーネントは .NET メソッドと同じくらい簡単に利用できるようになります。
	/// 4. タイプライブラリを利用して、予期されている型が何かを判断する
	///    しかし、タイプライブラリは利用可能でない場合もあります。
	/// <see cref="VarEnumSelector"/> は 3 番目の選択肢を実装します。
	/// </remarks>
	class VarEnumSelector
	{
		static readonly Dictionary<VarEnum, Type> _ComToManagedPrimitiveTypes = CreateComToManagedPrimitiveTypes();
		static readonly IList<IList<VarEnum>> _ComPrimitiveTypeFamilies = CreateComPrimitiveTypeFamilies();

		internal VarEnumSelector(Type[] explicitArgTypes) { VariantBuilders = new ReadOnlyCollection<VariantBuilder>(Array.ConvertAll(explicitArgTypes, x => GetVariantBuilder(x))); }

		internal ReadOnlyCollection<VariantBuilder> VariantBuilders { get; private set; }

		/// <summary>Variant として表すことができるようにオブジェクトが変換される必要があるマネージ型を取得します。</summary>
		/// <remarks>
		/// 一般に、<see cref="Type"/> と <see cref="VarEnum"/> の間には多対多の写像が存在します。
		/// しかしこのメソッドは現在の実装に必要な単純な写像を返します。
		/// 多対多関係に関する理由は次のようなものです:
		/// 1. <see cref="Int32"/> は VT_ERROR と同様に VT_I4 にもマッピングされ、<see cref="Decimal"/> は VT_DECIMAL と VT_CY にマッピングされます。
		///    しかし、これはラッパー型を混ぜると変化します。
		/// 2. COM 型を表す型が存在しない場合もあります。__ComObject はプライベートであり、<see cref="Object"/> では汎用的すぎます。
		/// </remarks>
		internal static Type GetManagedMarshalType(VarEnum varEnum)
		{
			Debug.Assert((varEnum & VarEnum.VT_BYREF) == 0);
			if (varEnum == VarEnum.VT_CY)
				return typeof(CurrencyWrapper);
			if (Variant.IsPrimitiveType(varEnum))
				return _ComToManagedPrimitiveTypes[varEnum];
			switch (varEnum)
			{
				case VarEnum.VT_EMPTY:
				case VarEnum.VT_NULL:
				case VarEnum.VT_UNKNOWN:
				case VarEnum.VT_DISPATCH:
				case VarEnum.VT_VARIANT:
					return typeof(object);
				case VarEnum.VT_ERROR:
					return typeof(ErrorWrapper);
				default:
					throw Error.UnexpectedVarEnum(varEnum);
			}
		}

		static Dictionary<VarEnum, Type> CreateComToManagedPrimitiveTypes()
		{
			Dictionary<VarEnum, Type> dict = new Dictionary<VarEnum, Type>();
			dict[VarEnum.VT_I1] = typeof(sbyte);
			dict[VarEnum.VT_I2] = typeof(short);
			dict[VarEnum.VT_I4] = typeof(int);
			dict[VarEnum.VT_I8] = typeof(long);
			dict[VarEnum.VT_UI1] = typeof(byte);
			dict[VarEnum.VT_UI2] = typeof(ushort);
			dict[VarEnum.VT_UI4] = typeof(uint);
			dict[VarEnum.VT_UI8] = typeof(ulong);
			dict[VarEnum.VT_INT] = typeof(IntPtr);
			dict[VarEnum.VT_UINT] = typeof(UIntPtr);
			dict[VarEnum.VT_BOOL] = typeof(bool);
			dict[VarEnum.VT_R4] = typeof(float);
			dict[VarEnum.VT_R8] = typeof(double);
			dict[VarEnum.VT_DECIMAL] = typeof(decimal);
			dict[VarEnum.VT_DATE] = typeof(DateTime);
			dict[VarEnum.VT_BSTR] = typeof(string);
			dict[VarEnum.VT_CY] = typeof(CurrencyWrapper);
			dict[VarEnum.VT_ERROR] = typeof(ErrorWrapper);
			return dict;
		}

		/// <summary>それぞれのファミリに含まれているような COM 型のファミリを作成します。ファミリ内で手前の型に対しては完全に無損失な変換が存在します。</summary>
		static IList<IList<VarEnum>> CreateComPrimitiveTypeFamilies()
		{
			return new VarEnum[][]
			{
                new[] { VarEnum.VT_I8, VarEnum.VT_I4, VarEnum.VT_I2, VarEnum.VT_I1 },
                new[] { VarEnum.VT_UI8, VarEnum.VT_UI4, VarEnum.VT_UI2, VarEnum.VT_UI1 },
                new[] { VarEnum.VT_INT },
                new[] { VarEnum.VT_UINT },
                new[] { VarEnum.VT_BOOL },
                new[] { VarEnum.VT_DATE },
                new[] { VarEnum.VT_R8, VarEnum.VT_R4 },
                new[] { VarEnum.VT_DECIMAL },
                new[] { VarEnum.VT_BSTR },
                // wrappers
                new[] { VarEnum.VT_CY },
                new[] { VarEnum.VT_ERROR },
            };
		}

		static bool TryGetPrimitiveComType(Type argumentType, out VarEnum primitiveVarEnum)
		{
			switch (Type.GetTypeCode(argumentType))
			{
				case TypeCode.Boolean:
					primitiveVarEnum = VarEnum.VT_BOOL;
					return true;
				case TypeCode.Char:
					primitiveVarEnum = VarEnum.VT_UI2;
					return true;
				case TypeCode.SByte:
					primitiveVarEnum = VarEnum.VT_I1;
					return true;
				case TypeCode.Byte:
					primitiveVarEnum = VarEnum.VT_UI1;
					return true;
				case TypeCode.Int16:
					primitiveVarEnum = VarEnum.VT_I2;
					return true;
				case TypeCode.UInt16:
					primitiveVarEnum = VarEnum.VT_UI2;
					return true;
				case TypeCode.Int32:
					primitiveVarEnum = VarEnum.VT_I4;
					return true;
				case TypeCode.UInt32:
					primitiveVarEnum = VarEnum.VT_UI4;
					return true;
				case TypeCode.Int64:
					primitiveVarEnum = VarEnum.VT_I8;
					return true;
				case TypeCode.UInt64:
					primitiveVarEnum = VarEnum.VT_UI8;
					return true;
				case TypeCode.Single:
					primitiveVarEnum = VarEnum.VT_R4;
					return true;
				case TypeCode.Double:
					primitiveVarEnum = VarEnum.VT_R8;
					return true;
				case TypeCode.Decimal:
					primitiveVarEnum = VarEnum.VT_DECIMAL;
					return true;
				case TypeCode.DateTime:
					primitiveVarEnum = VarEnum.VT_DATE;
					return true;
				case TypeCode.String:
					primitiveVarEnum = VarEnum.VT_BSTR;
					return true;
			}
			primitiveVarEnum = VarEnum.VT_VOID; // error
			if (argumentType == typeof(CurrencyWrapper))
				primitiveVarEnum = VarEnum.VT_CY;
			else if (argumentType == typeof(ErrorWrapper))
				primitiveVarEnum = VarEnum.VT_ERROR;
			else if (argumentType == typeof(IntPtr))
				primitiveVarEnum = VarEnum.VT_INT;
			else if (argumentType == typeof(UIntPtr))
				primitiveVarEnum = VarEnum.VT_UINT;
			return primitiveVarEnum != VarEnum.VT_VOID;
		}

		/// <summary>引数に対する最適な変換が存在する一意なプリミティブ型が存在するかどうかを判断します。</summary>
		static bool TryGetPrimitiveComTypeViaConversion(Type argumentType, out VarEnum primitiveVarEnum)
		{
			// 引数を変換できる一意な型ファミリを検索
			var compatibleComTypes = _ComPrimitiveTypeFamilies.SelectMany(x => x.Where(y => Utils.TypeUtils.IsImplicitlyConvertible(argumentType, _ComToManagedPrimitiveTypes[y])).Take(1)).ToArray();
			if (compatibleComTypes.Length > 1)
				throw Error.AmbiguousConversion(argumentType.Name, compatibleComTypes.Aggregate(Tuple.Create(0, ""), (x, y) => Tuple.Create(x.Item1 + 1, x.Item2 + (x.Item1 == compatibleComTypes.Length - 1 ? " and " : (x.Item1 != 0 ? ", " : "")) + _ComToManagedPrimitiveTypes[y].Name)).Item2);
			if (compatibleComTypes.Length == 1)
			{
				primitiveVarEnum = compatibleComTypes[0];
				return true;
			}
			primitiveVarEnum = VarEnum.VT_VOID; // error
			return false;
		}

		// Type.InvokeMember は VT_DISPATCH としてオブジェクトをマーシャリングしようと試み、VT_UNKNOWN、ユーザー定義型を含んでいることを示す VT_RECORD にフォールバックします。
		// VT_DISPATCH を試みて、GetNativeVariantForObejct を呼び出します。
		const VarEnum VT_DEFAULT = VarEnum.VT_RECORD;

		VarEnum GetComType(ref Type argumentType)
		{
			if (argumentType == typeof(Missing))
				return VarEnum.VT_RECORD; //actual variant type will be VT_ERROR | E_PARAMNOTFOUND 
			if (argumentType.IsArray)
				return VarEnum.VT_ARRAY; //actual variant type will be VT_ARRAY | VT_<ELEMENT_TYPE>
			if (argumentType == typeof(UnknownWrapper))
				return VarEnum.VT_UNKNOWN;
			else if (argumentType == typeof(DispatchWrapper))
				return VarEnum.VT_DISPATCH;
			else if (argumentType == typeof(VariantWrapper))
				return VarEnum.VT_VARIANT;
			else if (argumentType == typeof(BStrWrapper))
				return VarEnum.VT_BSTR;
			else if (argumentType == typeof(ErrorWrapper))
				return VarEnum.VT_ERROR;
			else if (argumentType == typeof(CurrencyWrapper))
				return VarEnum.VT_CY;
			// 多くの言語は列挙体が基になる型として使用されることに対して明示的キャストが必要です。
			// しかし、相互運用アセンブリの列挙体を引数として使用できるように、明示的キャストの必要なく COM に対してこの変換ができるようにします。
			if (argumentType.IsEnum)
			{
				argumentType = Enum.GetUnderlyingType(argumentType);
				return GetComType(ref argumentType);
			}
			// COM は値型の null を表現できないので、基になる型へ変換する
			// 値がない場合は例外をスローする
			if (Utils.TypeUtils.IsNullableType(argumentType))
			{
				argumentType = Utils.TypeUtils.GetNonNullableType(argumentType);
				return GetComType(ref argumentType);
			}
			// COM にジェネリック型は公開できないので、COM インターフェイスを実装できない。
			if (argumentType.IsGenericType)
				return VarEnum.VT_UNKNOWN;
			VarEnum primitiveVarEnum;
			if (TryGetPrimitiveComType(argumentType, out primitiveVarEnum))
				return primitiveVarEnum;
			// 型を特定の COM 型にマーシャリングする方法が見つからなかった
			return VT_DEFAULT;
		}

		/// <summary>COM への呼び出しとして引数をマーシャリングするべき COM Variant 型を取得します。</summary>
		VariantBuilder GetVariantBuilder(Type argumentType)
		{
			// argumentType は MarshalType から来ていて、null は動的オブジェクトが null 値を保持していて、参照渡しではないことを意味する
			if (argumentType == null)
				return new VariantBuilder(VarEnum.VT_EMPTY, new NullArgBuilder());
			if (argumentType == typeof(DBNull))
				return new VariantBuilder(VarEnum.VT_NULL, new NullArgBuilder());
			if (argumentType.IsByRef)
			{
				var elementType = argumentType.GetElementType();
				VarEnum elementVarEnum;
				if (elementType == typeof(object) || elementType == typeof(DBNull)) 
					elementVarEnum = VarEnum.VT_VARIANT; // ByRef を渡す意味のない値。もしかしたら呼び出し元がこれを何かに置き換えるかもしれない。バリアント参照として渡す必要あり
				else
					elementVarEnum = GetComType(ref elementType);
				return new VariantBuilder(elementVarEnum | VarEnum.VT_BYREF, GetSimpleArgBuilder(elementType, elementVarEnum));
			}
			var varEnum = GetComType(ref argumentType);
			var argBuilder = GetByValArgBuilder(argumentType, ref varEnum);
			return new VariantBuilder(varEnum, argBuilder);
		}

		// 値渡しマーシャリングの検索中に呼び出される。
		// 値渡しの場合、他のすべてのマーシャリング型を見つける試行が失敗した場合、変換または IConvertible を考慮に入れることができる。
		static ArgBuilder GetByValArgBuilder(Type elementType, ref VarEnum elementVarEnum)
		{
			// VT はマーシャリング型が不明であることを示す
			if (elementVarEnum == VT_DEFAULT)
			{
				// 変換の検索を試みる
				VarEnum convertibleTo;
				if (TryGetPrimitiveComTypeViaConversion(elementType, out convertibleTo))
				{
					elementVarEnum = convertibleTo;
					return new ConversionArgBuilder(elementType, GetSimpleArgBuilder(GetManagedMarshalType(elementVarEnum), elementVarEnum));
				}
				// IConvertible について調べる
				if (typeof(IConvertible).IsAssignableFrom(elementType))
					return new ConvertibleArgBuilder();
			}
			return GetSimpleArgBuilder(elementType, elementVarEnum);
		}

		// このメソッドは Variant によって直接サポートされる型に対するビルダーを生成します。
		static SimpleArgBuilder GetSimpleArgBuilder(Type elementType, VarEnum elementVarEnum)
		{
			SimpleArgBuilder argBuilder;
			switch (elementVarEnum)
			{
				case VarEnum.VT_BSTR:
					argBuilder = new StringArgBuilder(elementType);
					break;
				case VarEnum.VT_BOOL:
					argBuilder = new BoolArgBuilder(elementType);
					break;
				case VarEnum.VT_DATE:
					argBuilder = new DateTimeArgBuilder(elementType);
					break;
				case VarEnum.VT_CY:
					argBuilder = new CurrencyArgBuilder(elementType);
					break;
				case VarEnum.VT_DISPATCH:
					argBuilder = new DispatchArgBuilder(elementType);
					break;
				case VarEnum.VT_UNKNOWN:
					argBuilder = new UnknownArgBuilder(elementType);
					break;
				case VarEnum.VT_VARIANT:
				case VarEnum.VT_ARRAY:
				case VarEnum.VT_RECORD:
					argBuilder = new VariantArgBuilder(elementType);
					break;
				case VarEnum.VT_ERROR:
					argBuilder = new ErrorArgBuilder(elementType);
					break;
				default:
					var marshalType = GetManagedMarshalType(elementVarEnum);
					argBuilder = elementType == marshalType ? new SimpleArgBuilder(elementType) : new ConvertArgBuilder(elementType, marshalType);
					break;
			}
			return argBuilder;
		}
	}
}