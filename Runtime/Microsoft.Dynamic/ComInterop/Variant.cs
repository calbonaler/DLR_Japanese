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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.ComInterop
{
	/// <summary>
	/// 遅延バインディングに対する基本的な COM の型を表します。他のあらゆる COM データ型を格納できます。
	/// この構造体は COM 呼び出しに渡すことも、COM 呼び出しから返されることもできるように、アンマネージデータレイアウトに正確に一致するようになっています。
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
	struct Variant
	{
#if DEBUG
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2207:InitializeValueTypeStaticFieldsInline")]
		static Variant()
		{
			// Variant のサイズは 32 bit プロセッサでは 4 ポインタ (16 bytes) であり、64 bit プロセッサでは 3 ポインタ (24 bytes) です。
			int intPtrSize = Marshal.SizeOf(typeof(IntPtr));
			int variantSize = Marshal.SizeOf(typeof(Variant));
			if (intPtrSize == 4)
			{
				Debug.Assert(variantSize == (4 * intPtrSize));
			}
			else
			{
				Debug.Assert(intPtrSize == 8);
				Debug.Assert(variantSize == (3 * intPtrSize));
			}
		}
#endif
		// Variant 内のデータ型のほとんどは _typeUnion に格納されます
		[FieldOffset(0)]
		TypeUnion _typeUnion;

		// decimal は非常に大きなデータ型であり、TypeUnion._wReserved1 内などの通常使用されない領域も使用する必要があります。
		// したがって、TypeUnion と完全に重なるように宣言されます。
		// decimal は最初の 2 バイトを使用しないため、TypeUnion._vt は型をエンコードするためにそのまま使用できます。
		[FieldOffset(0)]
		decimal _decimal;

		[StructLayout(LayoutKind.Sequential)]
		struct TypeUnion
		{
			internal ushort _vt;
			internal ushort _wReserved1;
			internal ushort _wReserved2;
			internal ushort _wReserved3;
			internal UnionTypes _unionTypes;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct Record
		{
			IntPtr _record;
			IntPtr _recordInfo;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable")]
		[StructLayout(LayoutKind.Explicit)]
		struct UnionTypes
		{
			[FieldOffset(0)]
			internal sbyte _i1;
			[FieldOffset(0)]
			internal short _i2;
			[FieldOffset(0)]
			internal int _i4;
			[FieldOffset(0)]
			internal long _i8;
			[FieldOffset(0)]
			internal byte _ui1;
			[FieldOffset(0)]
			internal ushort _ui2;
			[FieldOffset(0)]
			internal uint _ui4;
			[FieldOffset(0)]
			internal ulong _ui8;
			[SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
			[FieldOffset(0)]
			internal IntPtr _int;
			[FieldOffset(0)]
			internal UIntPtr _uint;
			[FieldOffset(0)]
			internal short _bool;
			[FieldOffset(0)]
			internal int _error;
			[FieldOffset(0)]
			internal float _r4;
			[FieldOffset(0)]
			internal double _r8;
			[FieldOffset(0)]
			internal long _cy;
			[FieldOffset(0)]
			internal double _date;
			[SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
			[FieldOffset(0)]
			internal IntPtr _bstr;
			[SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
			[FieldOffset(0)]
			internal IntPtr _unknown;
			[SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
			[FieldOffset(0)]
			internal IntPtr _dispatch;
			[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
			[FieldOffset(0)]
			internal IntPtr _byref;
			[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
			[FieldOffset(0)]
			internal Record _record;
		}

		public override string ToString() { return String.Format(CultureInfo.CurrentCulture, "Variant ({0})", VariantType); }

		/// <summary>
		/// プリミティブ型は基本 COM 型です。
		/// プリミティブ型には int のような値型だけでなく BStr のような参照型も含まれます。
		/// プリミティブ型には配列やユーザー定義の COM 型 (IUnknown/IDispatch) などの複合型は含まれません。
		/// </summary>
		internal static bool IsPrimitiveType(VarEnum varEnum)
		{
			switch (varEnum)
			{
				case VarEnum.VT_I1:
				case VarEnum.VT_I2:
				case VarEnum.VT_I4:
				case VarEnum.VT_I8:
				case VarEnum.VT_UI1:
				case VarEnum.VT_UI2:
				case VarEnum.VT_UI4:
				case VarEnum.VT_UI8:
				case VarEnum.VT_INT:
				case VarEnum.VT_UINT:
				case VarEnum.VT_BOOL:
				case VarEnum.VT_ERROR:
				case VarEnum.VT_R4:
				case VarEnum.VT_R8:
				case VarEnum.VT_DECIMAL:
				case VarEnum.VT_CY:
				case VarEnum.VT_DATE:
				case VarEnum.VT_BSTR:
					return true;
			}
			return false;
		}

		/// <summary>Variant が表しているマネージオブジェクトを取得します。</summary>
		[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		public object ToObject()
		{
			// 単純なケースを先にチェック
			if (IsEmpty)
				return null;
			switch (VariantType)
			{
				case VarEnum.VT_NULL: return DBNull.Value;
				case VarEnum.VT_I1: return AsI1;
				case VarEnum.VT_I2: return AsI2;
				case VarEnum.VT_I4: return AsI4;
				case VarEnum.VT_I8: return AsI8;
				case VarEnum.VT_UI1: return AsUi1;
				case VarEnum.VT_UI2: return AsUi2;
				case VarEnum.VT_UI4: return AsUi4;
				case VarEnum.VT_UI8: return AsUi8;
				case VarEnum.VT_INT: return AsInt;
				case VarEnum.VT_UINT: return AsUint;
				case VarEnum.VT_BOOL: return AsBool;
				case VarEnum.VT_ERROR: return AsError;
				case VarEnum.VT_R4: return AsR4;
				case VarEnum.VT_R8: return AsR8;
				case VarEnum.VT_DECIMAL: return AsDecimal;
				case VarEnum.VT_CY: return AsCy;
				case VarEnum.VT_DATE: return AsDate;
				case VarEnum.VT_BSTR: return AsBstr;
				case VarEnum.VT_UNKNOWN: return AsUnknown;
				case VarEnum.VT_DISPATCH: return AsDispatch;
				case VarEnum.VT_VARIANT: return AsVariant;
				default:
					return AsVariant;
			}
		}

		/// <summary>この Variant に関連付けられたあらゆるアンマネージメモリを開放します。</summary>
		public void Clear()
		{
			// 相互運用遷移のコストを節約するため、プリミティブ型や参照渡しについては OLE32 の VariantClear を呼ぶ必要はありません。
			// 参照渡しはメモリが Variant 自体によって所有されていないことを表しますが、プリミティブ型は解放すべきリソースを持っていません。
			// したがって、SafeArray、BSTR、インターフェイスおよびユーザー型は異なるようにハンドルします。
			if ((VariantType & VarEnum.VT_BYREF) != 0)
				VariantType = VarEnum.VT_EMPTY;
			else if ((VariantType & VarEnum.VT_ARRAY) != 0 || VariantType == VarEnum.VT_BSTR || VariantType == VarEnum.VT_UNKNOWN || VariantType == VarEnum.VT_DISPATCH || VariantType == VarEnum.VT_RECORD)
			{
				NativeMethods.VariantClear(UnsafeMethods.ConvertVariantByrefToPtr(ref this));
				Debug.Assert(IsEmpty);
			}
			else
				VariantType = VarEnum.VT_EMPTY;
		}

		public VarEnum VariantType
		{
			get { return (VarEnum)_typeUnion._vt; }
			set { _typeUnion._vt = (ushort)value; }
		}

		internal bool IsEmpty { get { return _typeUnion._vt == ((ushort)VarEnum.VT_EMPTY); } }

		public void SetAsNull()
		{
			Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
			VariantType = VarEnum.VT_NULL;
		}

		public void SetAsIConvertible(IConvertible value)
		{
			Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
			switch (value.GetTypeCode())
			{
				case TypeCode.Empty: break;
				case TypeCode.Object: AsUnknown = value; break;
				case TypeCode.DBNull: SetAsNull(); break;
				case TypeCode.Boolean: AsBool = value.ToBoolean(CultureInfo.CurrentCulture); break;
				case TypeCode.Char: AsUi2 = value.ToChar(CultureInfo.CurrentCulture); break;
				case TypeCode.SByte: AsI1 = value.ToSByte(CultureInfo.CurrentCulture); break;
				case TypeCode.Byte: AsUi1 = value.ToByte(CultureInfo.CurrentCulture); break;
				case TypeCode.Int16: AsI2 = value.ToInt16(CultureInfo.CurrentCulture); break;
				case TypeCode.UInt16: AsUi2 = value.ToUInt16(CultureInfo.CurrentCulture); break;
				case TypeCode.Int32: AsI4 = value.ToInt32(CultureInfo.CurrentCulture); break;
				case TypeCode.UInt32: AsUi4 = value.ToUInt32(CultureInfo.CurrentCulture); break;
				case TypeCode.Int64: AsI8 = value.ToInt64(CultureInfo.CurrentCulture); break;
				case TypeCode.UInt64: AsI8 = value.ToInt64(CultureInfo.CurrentCulture); break;
				case TypeCode.Single: AsR4 = value.ToSingle(CultureInfo.CurrentCulture); break;
				case TypeCode.Double: AsR8 = value.ToDouble(CultureInfo.CurrentCulture); break;
				case TypeCode.Decimal: AsDecimal = value.ToDecimal(CultureInfo.CurrentCulture); break;
				case TypeCode.DateTime: AsDate = value.ToDateTime(CultureInfo.CurrentCulture); break;
				case TypeCode.String: AsBstr = value.ToString(CultureInfo.CurrentCulture); break;
				default:
					throw Assert.Unreachable;
			}
		}

		// VT_I1
		public sbyte AsI1
		{
			get
			{
				Debug.Assert(VariantType == VarEnum.VT_I1);
				return _typeUnion._unionTypes._i1;
			}
			set
			{
				Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
				VariantType = VarEnum.VT_I1;
				_typeUnion._unionTypes._i1 = value;
			}
		}

		public void SetAsByrefI1(ref sbyte value)
		{
			Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
			VariantType = (VarEnum.VT_I1 | VarEnum.VT_BYREF);
			_typeUnion._unionTypes._byref = UnsafeMethods.ConvertSByteByrefToPtr(ref value);
		}

		// VT_I2
		public short AsI2
		{
			get
			{
				Debug.Assert(VariantType == VarEnum.VT_I2);
				return _typeUnion._unionTypes._i2;
			}
			set
			{
				Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
				VariantType = VarEnum.VT_I2;
				_typeUnion._unionTypes._i2 = value;
			}
		}

		public void SetAsByrefI2(ref short value)
		{
			Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
			VariantType = (VarEnum.VT_I2 | VarEnum.VT_BYREF);
			_typeUnion._unionTypes._byref = UnsafeMethods.ConvertInt16ByrefToPtr(ref value);
		}

		// VT_I4
		public int AsI4
		{
			get
			{
				Debug.Assert(VariantType == VarEnum.VT_I4);
				return _typeUnion._unionTypes._i4;
			}
			set
			{
				Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
				VariantType = VarEnum.VT_I4;
				_typeUnion._unionTypes._i4 = value;
			}
		}

		public void SetAsByrefI4(ref int value)
		{
			Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
			VariantType = (VarEnum.VT_I4 | VarEnum.VT_BYREF);
			_typeUnion._unionTypes._byref = UnsafeMethods.ConvertInt32ByrefToPtr(ref value);
		}

		// VT_I8
		public long AsI8
		{
			get
			{
				Debug.Assert(VariantType == VarEnum.VT_I8);
				return _typeUnion._unionTypes._i8;
			}
			set
			{
				Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
				VariantType = VarEnum.VT_I8;
				_typeUnion._unionTypes._i8 = value;
			}
		}

		public void SetAsByrefI8(ref long value)
		{
			Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
			VariantType = (VarEnum.VT_I8 | VarEnum.VT_BYREF);
			_typeUnion._unionTypes._byref = UnsafeMethods.ConvertInt64ByrefToPtr(ref value);
		}

		// VT_UI1
		public byte AsUi1
		{
			get
			{
				Debug.Assert(VariantType == VarEnum.VT_UI1);
				return _typeUnion._unionTypes._ui1;
			}
			set
			{
				Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
				VariantType = VarEnum.VT_UI1;
				_typeUnion._unionTypes._ui1 = value;
			}
		}

		public void SetAsByrefUi1(ref byte value)
		{
			Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
			VariantType = (VarEnum.VT_UI1 | VarEnum.VT_BYREF);
			_typeUnion._unionTypes._byref = UnsafeMethods.ConvertByteByrefToPtr(ref value);
		}

		// VT_UI2
		public ushort AsUi2
		{
			get
			{
				Debug.Assert(VariantType == VarEnum.VT_UI2);
				return _typeUnion._unionTypes._ui2;
			}
			set
			{
				Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
				VariantType = VarEnum.VT_UI2;
				_typeUnion._unionTypes._ui2 = value;
			}
		}

		public void SetAsByrefUi2(ref ushort value)
		{
			Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
			VariantType = (VarEnum.VT_UI2 | VarEnum.VT_BYREF);
			_typeUnion._unionTypes._byref = UnsafeMethods.ConvertUInt16ByrefToPtr(ref value);
		}

		// VT_UI4
		public uint AsUi4
		{
			get
			{
				Debug.Assert(VariantType == VarEnum.VT_UI4);
				return _typeUnion._unionTypes._ui4;
			}
			set
			{
				Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
				VariantType = VarEnum.VT_UI4;
				_typeUnion._unionTypes._ui4 = value;
			}
		}

		public void SetAsByrefUi4(ref uint value)
		{
			Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
			VariantType = (VarEnum.VT_UI4 | VarEnum.VT_BYREF);
			_typeUnion._unionTypes._byref = UnsafeMethods.ConvertUInt32ByrefToPtr(ref value);
		}

		// VT_UI8
		public ulong AsUi8
		{
			get
			{
				Debug.Assert(VariantType == VarEnum.VT_UI8);
				return _typeUnion._unionTypes._ui8;
			}
			set
			{
				Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
				VariantType = VarEnum.VT_UI8;
				_typeUnion._unionTypes._ui8 = value;
			}
		}

		public void SetAsByrefUi8(ref ulong value)
		{
			Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
			VariantType = (VarEnum.VT_UI8 | VarEnum.VT_BYREF);
			_typeUnion._unionTypes._byref = UnsafeMethods.ConvertUInt64ByrefToPtr(ref value);
		}

		// VT_INT
		public IntPtr AsInt
		{
			get
			{
				Debug.Assert(VariantType == VarEnum.VT_INT);
				return _typeUnion._unionTypes._int;
			}
			set
			{
				Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
				VariantType = VarEnum.VT_INT;
				_typeUnion._unionTypes._int = value;
			}
		}

		public void SetAsByrefInt(ref IntPtr value)
		{
			Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
			VariantType = (VarEnum.VT_INT | VarEnum.VT_BYREF);
			_typeUnion._unionTypes._byref = UnsafeMethods.ConvertIntPtrByrefToPtr(ref value);
		}

		// VT_UINT
		public UIntPtr AsUint
		{
			get
			{
				Debug.Assert(VariantType == VarEnum.VT_UINT);
				return _typeUnion._unionTypes._uint;
			}
			set
			{
				Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
				VariantType = VarEnum.VT_UINT;
				_typeUnion._unionTypes._uint = value;
			}
		}

		public void SetAsByrefUint(ref UIntPtr value)
		{
			Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
			VariantType = (VarEnum.VT_UINT | VarEnum.VT_BYREF);
			_typeUnion._unionTypes._byref = UnsafeMethods.ConvertUIntPtrByrefToPtr(ref value);
		}

		// VT_BOOL
		public bool AsBool
		{
			get
			{
				Debug.Assert(VariantType == VarEnum.VT_BOOL);
				return _typeUnion._unionTypes._bool != 0;
			}
			set
			{
				Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
				VariantType = VarEnum.VT_BOOL;
				_typeUnion._unionTypes._bool = value ? (short)(-1) : (short)0;
			}
		}

		public void SetAsByrefBool(ref short value)
		{
			Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
			VariantType = (VarEnum.VT_BOOL | VarEnum.VT_BYREF);
			_typeUnion._unionTypes._byref = UnsafeMethods.ConvertInt16ByrefToPtr(ref value);
		}

		// VT_ERROR
		public int AsError
		{
			get
			{
				Debug.Assert(VariantType == VarEnum.VT_ERROR);
				return _typeUnion._unionTypes._error;
			}
			set
			{
				Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
				VariantType = VarEnum.VT_ERROR;
				_typeUnion._unionTypes._error = value;
			}
		}

		public void SetAsByrefError(ref int value)
		{
			Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
			VariantType = (VarEnum.VT_ERROR | VarEnum.VT_BYREF);
			_typeUnion._unionTypes._byref = UnsafeMethods.ConvertInt32ByrefToPtr(ref value);
		}

		// VT_R4
		public float AsR4
		{
			get
			{
				Debug.Assert(VariantType == VarEnum.VT_R4);
				return _typeUnion._unionTypes._r4;
			}
			set
			{
				Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
				VariantType = VarEnum.VT_R4;
				_typeUnion._unionTypes._r4 = value;
			}
		}

		public void SetAsByrefR4(ref float value)
		{
			Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
			VariantType = (VarEnum.VT_R4 | VarEnum.VT_BYREF);
			_typeUnion._unionTypes._byref = UnsafeMethods.ConvertSingleByrefToPtr(ref value);
		}

		// VT_R8
		public double AsR8
		{
			get
			{
				Debug.Assert(VariantType == VarEnum.VT_R8);
				return _typeUnion._unionTypes._r8;
			}
			set
			{
				Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
				VariantType = VarEnum.VT_R8;
				_typeUnion._unionTypes._r8 = value;
			}
		}

		public void SetAsByrefR8(ref double value)
		{
			Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
			VariantType = (VarEnum.VT_R8 | VarEnum.VT_BYREF);
			_typeUnion._unionTypes._byref = UnsafeMethods.ConvertDoubleByrefToPtr(ref value);
		}

		// VT_DECIMAL
		public decimal AsDecimal
		{
			get
			{
				Debug.Assert(VariantType == VarEnum.VT_DECIMAL);
				// decimal の最初のバイトは使用されませんが、通常 0 に設定します
				_typeUnion._vt = 0;
				return _decimal;
			}
			set
			{
				Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
				VariantType = VarEnum.VT_DECIMAL;
				_decimal = value;
				// _vt は _decimal と重なっているので、_decimal を設定した後に、設定する
				_typeUnion._vt = (ushort)VarEnum.VT_DECIMAL;
			}
		}

		public void SetAsByrefDecimal(ref decimal value)
		{
			Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
			VariantType = (VarEnum.VT_DECIMAL | VarEnum.VT_BYREF);
			_typeUnion._unionTypes._byref = UnsafeMethods.ConvertDecimalByrefToPtr(ref value);
		}

		// VT_CY
		public decimal AsCy
		{
			get
			{
				Debug.Assert(VariantType == VarEnum.VT_CY);
				return Decimal.FromOACurrency(_typeUnion._unionTypes._cy);
			}
			set
			{
				Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
				VariantType = VarEnum.VT_CY;
				_typeUnion._unionTypes._cy = Decimal.ToOACurrency(value);
			}
		}

		public void SetAsByrefCy(ref long value)
		{
			Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
			VariantType = (VarEnum.VT_CY | VarEnum.VT_BYREF);
			_typeUnion._unionTypes._byref = UnsafeMethods.ConvertInt64ByrefToPtr(ref value);
		}

		// VT_DATE
		public DateTime AsDate
		{
			get
			{
				Debug.Assert(VariantType == VarEnum.VT_DATE);
				return DateTime.FromOADate(_typeUnion._unionTypes._date);
			}
			set
			{
				Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
				VariantType = VarEnum.VT_DATE;
				_typeUnion._unionTypes._date = value.ToOADate();
			}
		}

		public void SetAsByrefDate(ref double value)
		{
			Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
			VariantType = (VarEnum.VT_DATE | VarEnum.VT_BYREF);
			_typeUnion._unionTypes._byref = UnsafeMethods.ConvertDoubleByrefToPtr(ref value);
		}

		// VT_BSTR
		public string AsBstr
		{
			get
			{
				Debug.Assert(VariantType == VarEnum.VT_BSTR);
				if (_typeUnion._unionTypes._bstr != IntPtr.Zero)
					return Marshal.PtrToStringBSTR(_typeUnion._unionTypes._bstr);
				else
					return null;
			}
			set
			{
				Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
				VariantType = VarEnum.VT_BSTR;
				if (value != null)
					Marshal.GetNativeVariantForObject(value, UnsafeMethods.ConvertVariantByrefToPtr(ref this));
			}
		}

		public void SetAsByrefBstr(ref IntPtr value)
		{
			Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
			VariantType = (VarEnum.VT_BSTR | VarEnum.VT_BYREF);
			_typeUnion._unionTypes._byref = UnsafeMethods.ConvertIntPtrByrefToPtr(ref value);
		}

		// VT_UNKNOWN
		public object AsUnknown
		{
			get
			{
				Debug.Assert(VariantType == VarEnum.VT_UNKNOWN);
				if (_typeUnion._unionTypes._dispatch != IntPtr.Zero)
					return Marshal.GetObjectForIUnknown(_typeUnion._unionTypes._unknown);
				else
					return null;
			}
			set
			{
				Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
				VariantType = VarEnum.VT_UNKNOWN;
				if (value != null)
					_typeUnion._unionTypes._unknown = Marshal.GetIUnknownForObject(value);
			}
		}

		public void SetAsByrefUnknown(ref IntPtr value)
		{
			Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
			VariantType = (VarEnum.VT_UNKNOWN | VarEnum.VT_BYREF);
			_typeUnion._unionTypes._byref = UnsafeMethods.ConvertIntPtrByrefToPtr(ref value);
		}

		// VT_DISPATCH
		public object AsDispatch
		{
			get
			{
				Debug.Assert(VariantType == VarEnum.VT_DISPATCH);
				if (_typeUnion._unionTypes._dispatch != IntPtr.Zero)
					return Marshal.GetObjectForIUnknown(_typeUnion._unionTypes._dispatch);
				else
					return null;
			}
			set
			{
				Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
				VariantType = VarEnum.VT_DISPATCH;
				if (value != null)
					_typeUnion._unionTypes._unknown = Marshal.GetIDispatchForObject(value);
			}
		}

		public void SetAsByrefDispatch(ref IntPtr value)
		{
			Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
			VariantType = (VarEnum.VT_DISPATCH | VarEnum.VT_BYREF);
			_typeUnion._unionTypes._byref = UnsafeMethods.ConvertIntPtrByrefToPtr(ref value);
		}

		// VT_VARIANT
		public object AsVariant
		{
			get { return Marshal.GetObjectForNativeVariant(UnsafeMethods.ConvertVariantByrefToPtr(ref this)); }
			set
			{
				Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
				if (value != null)
					UnsafeMethods.InitVariantForObject(value, ref this);
			}
		}

		public void SetAsByrefVariant(ref Variant value)
		{
			Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
			VariantType = VarEnum.VT_VARIANT | VarEnum.VT_BYREF;
			_typeUnion._unionTypes._byref = UnsafeMethods.ConvertVariantByrefToPtr(ref value);
		}

		// 他の参照渡し Variant の内容を渡す参照渡し Variant を構築します。
		public void SetAsByrefVariantIndirect(ref Variant value)
		{
			Debug.Assert(IsEmpty); // VariantClear はこれ以外の場合でも必要になる可能性がありますが、セッターは 1 回だけ呼び出すことができます。
			Debug.Assert((value.VariantType & VarEnum.VT_BYREF) == 0, "double indirection");
			switch (value.VariantType)
			{
				case VarEnum.VT_EMPTY:
				case VarEnum.VT_NULL:
					// これらは VT_BYREF と組み合わせることができません。Variant 参照として渡すことを試してください
					SetAsByrefVariant(ref value);
					return;
				case VarEnum.VT_RECORD:
					// VT_RECORD は VT_BYREF フラグが設定されているかどうかにかかわらず、同じ内部表現をするので奇妙
					_typeUnion._unionTypes._record = value._typeUnion._unionTypes._record;
					break;
				case VarEnum.VT_DECIMAL:
					_typeUnion._unionTypes._byref = UnsafeMethods.ConvertDecimalByrefToPtr(ref value._decimal);
					break;
				default:
					_typeUnion._unionTypes._byref = UnsafeMethods.ConvertIntPtrByrefToPtr(ref value._typeUnion._unionTypes._byref);
					break;
			}
			VariantType = (value.VariantType | VarEnum.VT_BYREF);
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		internal static System.Reflection.PropertyInfo GetAccessor(VarEnum varType)
		{
			switch (varType)
			{
				case VarEnum.VT_I1: return typeof(Variant).GetProperty("AsI1");
				case VarEnum.VT_I2: return typeof(Variant).GetProperty("AsI2");
				case VarEnum.VT_I4: return typeof(Variant).GetProperty("AsI4");
				case VarEnum.VT_I8: return typeof(Variant).GetProperty("AsI8");
				case VarEnum.VT_UI1: return typeof(Variant).GetProperty("AsUi1");
				case VarEnum.VT_UI2: return typeof(Variant).GetProperty("AsUi2");
				case VarEnum.VT_UI4: return typeof(Variant).GetProperty("AsUi4");
				case VarEnum.VT_UI8: return typeof(Variant).GetProperty("AsUi8");
				case VarEnum.VT_INT: return typeof(Variant).GetProperty("AsInt");
				case VarEnum.VT_UINT: return typeof(Variant).GetProperty("AsUint");
				case VarEnum.VT_BOOL: return typeof(Variant).GetProperty("AsBool");
				case VarEnum.VT_ERROR: return typeof(Variant).GetProperty("AsError");
				case VarEnum.VT_R4: return typeof(Variant).GetProperty("AsR4");
				case VarEnum.VT_R8: return typeof(Variant).GetProperty("AsR8");
				case VarEnum.VT_DECIMAL: return typeof(Variant).GetProperty("AsDecimal");
				case VarEnum.VT_CY: return typeof(Variant).GetProperty("AsCy");
				case VarEnum.VT_DATE: return typeof(Variant).GetProperty("AsDate");
				case VarEnum.VT_BSTR: return typeof(Variant).GetProperty("AsBstr");
				case VarEnum.VT_UNKNOWN: return typeof(Variant).GetProperty("AsUnknown");
				case VarEnum.VT_DISPATCH: return typeof(Variant).GetProperty("AsDispatch");
				case VarEnum.VT_VARIANT:
				case VarEnum.VT_RECORD:
				case VarEnum.VT_ARRAY:
					return typeof(Variant).GetProperty("AsVariant");
				default:
					throw Error.VariantGetAccessorNYI(varType);
			}
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		internal static System.Reflection.MethodInfo GetByrefSetter(VarEnum varType)
		{
			switch (varType)
			{
				case VarEnum.VT_I1: return typeof(Variant).GetMethod("SetAsByrefI1");
				case VarEnum.VT_I2: return typeof(Variant).GetMethod("SetAsByrefI2");
				case VarEnum.VT_I4: return typeof(Variant).GetMethod("SetAsByrefI4");
				case VarEnum.VT_I8: return typeof(Variant).GetMethod("SetAsByrefI8");
				case VarEnum.VT_UI1: return typeof(Variant).GetMethod("SetAsByrefUi1");
				case VarEnum.VT_UI2: return typeof(Variant).GetMethod("SetAsByrefUi2");
				case VarEnum.VT_UI4: return typeof(Variant).GetMethod("SetAsByrefUi4");
				case VarEnum.VT_UI8: return typeof(Variant).GetMethod("SetAsByrefUi8");
				case VarEnum.VT_INT: return typeof(Variant).GetMethod("SetAsByrefInt");
				case VarEnum.VT_UINT: return typeof(Variant).GetMethod("SetAsByrefUint");
				case VarEnum.VT_BOOL: return typeof(Variant).GetMethod("SetAsByrefBool");
				case VarEnum.VT_ERROR: return typeof(Variant).GetMethod("SetAsByrefError");
				case VarEnum.VT_R4: return typeof(Variant).GetMethod("SetAsByrefR4");
				case VarEnum.VT_R8: return typeof(Variant).GetMethod("SetAsByrefR8");
				case VarEnum.VT_DECIMAL: return typeof(Variant).GetMethod("SetAsByrefDecimal");
				case VarEnum.VT_CY: return typeof(Variant).GetMethod("SetAsByrefCy");
				case VarEnum.VT_DATE: return typeof(Variant).GetMethod("SetAsByrefDate");
				case VarEnum.VT_BSTR: return typeof(Variant).GetMethod("SetAsByrefBstr");
				case VarEnum.VT_UNKNOWN: return typeof(Variant).GetMethod("SetAsByrefUnknown");
				case VarEnum.VT_DISPATCH: return typeof(Variant).GetMethod("SetAsByrefDispatch");
				case VarEnum.VT_VARIANT:
					return typeof(Variant).GetMethod("SetAsByrefVariant");
				case VarEnum.VT_RECORD:
				case VarEnum.VT_ARRAY:
					return typeof(Variant).GetMethod("SetAsByrefVariantIndirect");
				default:
					throw Error.VariantGetAccessorNYI(varType);
			}
		}
	}
}