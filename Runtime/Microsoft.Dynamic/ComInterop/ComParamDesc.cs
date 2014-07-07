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
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Marshal = System.Runtime.InteropServices.Marshal;
using VarEnum = System.Runtime.InteropServices.VarEnum;

namespace Microsoft.Scripting.ComInterop
{
	/// <summary>タイプライブラリに定義されているメソッドの引数および戻り値に対する記述を表します。</summary>
	public class ComParamDesc
	{
		readonly VarEnum _vt;
		readonly string _name;

		/// <summary>COM メソッドの引数に対する記述を作成します。</summary>
		internal ComParamDesc(ref ELEMDESC elemDesc, string name)
		{
			// 既定値が引数の記述から抽出されたかどうかとは無関係に_defaultValue が DBNull.Value に設定されることを確認します。
			// そうすることの失敗は ToString() メソッド内の実行時例外を譲ることです
			DefaultValue = DBNull.Value;
			if (!string.IsNullOrEmpty(name))
			{
				// これは戻り値ではなく引数。
				IsOut = (elemDesc.desc.paramdesc.wParamFlags & PARAMFLAG.PARAMFLAG_FOUT) != 0;
				IsOptional = (elemDesc.desc.paramdesc.wParamFlags & PARAMFLAG.PARAMFLAG_FOPT) != 0;
				// TODO: PARAMDESCEX 構造体は解決されるべきメモリ問題が存在するため、現在無視します。
				//_defaultValue = PARAMDESCEX.GetDefaultValue(ref elemDesc.desc.paramdesc);
			}
			_name = name;
			_vt = (VarEnum)elemDesc.tdesc.vt;
			TYPEDESC typeDesc = elemDesc.tdesc;
			while (true)
			{
				if (_vt == VarEnum.VT_PTR)
					ByReference = true;
				else if (_vt == VarEnum.VT_ARRAY)
					IsArray = true;
				else
					break;
				var childTypeDesc = (TYPEDESC)Marshal.PtrToStructure(typeDesc.lpValue, typeof(TYPEDESC));
				_vt = (VarEnum)childTypeDesc.vt;
				typeDesc = childTypeDesc;
			}
			var vtWithoutByref = _vt;
			if ((_vt & VarEnum.VT_BYREF) != 0)
			{
				vtWithoutByref = (_vt & ~VarEnum.VT_BYREF);
				ByReference = true;
			}
			ParameterType = GetTypeForVarEnum(vtWithoutByref);
		}

		// TODO: 戻り値は異なる型で表されるようにする
		/// <summary>COM メソッドの戻り値に対する記述を作成します。</summary>
		internal ComParamDesc(ref ELEMDESC elemDesc) : this(ref elemDesc, string.Empty) { }

		//internal struct PARAMDESCEX {
		//    ulong _cByte;
		//    Variant _varDefaultValue;
		//    internal void Dummy() {
		//        _cByte = 0;
		//        throw Error.MethodShouldNotBeCalled();
		//    }
		//    internal static object GetDefaultValue(ref PARAMDESC paramdesc) {
		//        if ((paramdesc.wParamFlags & PARAMFLAG.PARAMFLAG_FHASDEFAULT) == 0)
		//            return DBNull.Value;
		//        var varValue = (PARAMDESCEX)Marshal.PtrToStructure(paramdesc.lpVarValue, typeof(PARAMDESCEX));
		//        if (varValue._cByte != (ulong)(Marshal.SizeOf((typeof(PARAMDESCEX)))))
		//            throw Error.DefaultValueCannotBeRead();
		//        return varValue._varDefaultValue.ToObject();
		//    }
		//}

		static Type GetTypeForVarEnum(VarEnum vt)
		{
			Type type;
			switch (vt)
			{
				// VARIANT でも使用できる VarEnum ですが、 TYPEDESC には現れません
				case VarEnum.VT_EMPTY:
				case VarEnum.VT_NULL:
				case VarEnum.VT_RECORD:
					throw new InvalidOperationException(string.Format("Unexpected VarEnum {0}.", vt));
				// VARIANT で使用されない VarEnum ですが、TYPEDESC には現れます
				case VarEnum.VT_VOID:
					type = null;
					break;
#if DISABLE // TODO: WTypes.h はこれらが VARIANT では使用されないことを示しますが、Type.InvokeMember は許容するようです
                case VarEnum.VT_I8:
                case VarEnum.UI8:
#endif
				case VarEnum.VT_HRESULT:
					type = typeof(int);
					break;
				case ((VarEnum)37): // VT_INT_PTR:
				case VarEnum.VT_PTR:
					type = typeof(IntPtr);
					break;
				case ((VarEnum)38): // VT_UINT_PTR:
					type = typeof(UIntPtr);
					break;
				case VarEnum.VT_SAFEARRAY:
				case VarEnum.VT_CARRAY:
					type = typeof(Array);
					break;
				case VarEnum.VT_LPSTR:
				case VarEnum.VT_LPWSTR:
					type = typeof(string);
					break;
				case VarEnum.VT_USERDEFINED:
					type = typeof(object);
					break;
				// TYPEDESC と同様に VARIANT でも使用される VarEnum は VarEnumSelector を使用する
				default:
					type = VarEnumSelector.GetManagedMarshalType(vt);
					break;
			}

			return type;
		}

		/// <summary>このオブジェクトの文字列表現を取得します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		public override string ToString()
		{
			StringBuilder result = new StringBuilder();
			if (IsOptional)
				result.Append("[Optional] ");
			if (IsOut)
				result.Append("[out] ");
			result.Append(ParameterType.Name);
			if (IsArray)
				result.Append("[]");
			if (ByReference)
				result.Append("&");
			result.Append(" ");
			result.Append(_name);
			if (DefaultValue != DBNull.Value)
			{
				result.Append("=");
				result.Append(DefaultValue.ToString());
			}
			return result.ToString();
		}

		/// <summary>この引数を使用して呼び出し元に情報を返すことができるかどうかを示す値を取得します。</summary>
		public bool IsOut { get; private set; }

		/// <summary>この引数が省略可能かどうかを示す値を取得します。</summary>
		public bool IsOptional { get; private set; }

		/// <summary>この引数が参照渡しかどうかを示す値を取得します。</summary>
		public bool ByReference { get; private set; }

		/// <summary>この引数が配列かどうかを示す値を取得します。</summary>
		public bool IsArray { get; private set; }

		/// <summary>この引数の型を取得します。</summary>
		public Type ParameterType { get; private set; }

		/// <summary>この引数の既定値を取得します。既定値が存在しない場合は <see cref="DBNull.Value"/> が返されます。</summary>
		internal object DefaultValue { get; private set; }
	}
}