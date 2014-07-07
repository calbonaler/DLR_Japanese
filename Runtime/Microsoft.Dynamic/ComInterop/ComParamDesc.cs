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
	/// <summary>�^�C�v���C�u�����ɒ�`����Ă��郁�\�b�h�̈�������і߂�l�ɑ΂���L�q��\���܂��B</summary>
	public class ComParamDesc
	{
		readonly VarEnum _vt;
		readonly string _name;

		/// <summary>COM ���\�b�h�̈����ɑ΂���L�q���쐬���܂��B</summary>
		internal ComParamDesc(ref ELEMDESC elemDesc, string name)
		{
			// ����l�������̋L�q���璊�o���ꂽ���ǂ����Ƃ͖��֌W��_defaultValue �� DBNull.Value �ɐݒ肳��邱�Ƃ��m�F���܂��B
			// �������邱�Ƃ̎��s�� ToString() ���\�b�h���̎��s����O�����邱�Ƃł�
			DefaultValue = DBNull.Value;
			if (!string.IsNullOrEmpty(name))
			{
				// ����͖߂�l�ł͂Ȃ������B
				IsOut = (elemDesc.desc.paramdesc.wParamFlags & PARAMFLAG.PARAMFLAG_FOUT) != 0;
				IsOptional = (elemDesc.desc.paramdesc.wParamFlags & PARAMFLAG.PARAMFLAG_FOPT) != 0;
				// TODO: PARAMDESCEX �\���͉̂��������ׂ���������肪���݂��邽�߁A���ݖ������܂��B
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

		// TODO: �߂�l�͈قȂ�^�ŕ\�����悤�ɂ���
		/// <summary>COM ���\�b�h�̖߂�l�ɑ΂���L�q���쐬���܂��B</summary>
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
				// VARIANT �ł��g�p�ł��� VarEnum �ł����A TYPEDESC �ɂ͌���܂���
				case VarEnum.VT_EMPTY:
				case VarEnum.VT_NULL:
				case VarEnum.VT_RECORD:
					throw new InvalidOperationException(string.Format("Unexpected VarEnum {0}.", vt));
				// VARIANT �Ŏg�p����Ȃ� VarEnum �ł����ATYPEDESC �ɂ͌���܂�
				case VarEnum.VT_VOID:
					type = null;
					break;
#if DISABLE // TODO: WTypes.h �͂���炪 VARIANT �ł͎g�p����Ȃ����Ƃ������܂����AType.InvokeMember �͋��e����悤�ł�
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
				// TYPEDESC �Ɠ��l�� VARIANT �ł��g�p����� VarEnum �� VarEnumSelector ���g�p����
				default:
					type = VarEnumSelector.GetManagedMarshalType(vt);
					break;
			}

			return type;
		}

		/// <summary>���̃I�u�W�F�N�g�̕�����\�����擾���܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̕�����\���B</returns>
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

		/// <summary>���̈������g�p���ČĂяo�����ɏ���Ԃ����Ƃ��ł��邩�ǂ����������l���擾���܂��B</summary>
		public bool IsOut { get; private set; }

		/// <summary>���̈������ȗ��\���ǂ����������l���擾���܂��B</summary>
		public bool IsOptional { get; private set; }

		/// <summary>���̈������Q�Ɠn�����ǂ����������l���擾���܂��B</summary>
		public bool ByReference { get; private set; }

		/// <summary>���̈������z�񂩂ǂ����������l���擾���܂��B</summary>
		public bool IsArray { get; private set; }

		/// <summary>���̈����̌^���擾���܂��B</summary>
		public Type ParameterType { get; private set; }

		/// <summary>���̈����̊���l���擾���܂��B����l�����݂��Ȃ��ꍇ�� <see cref="DBNull.Value"/> ���Ԃ���܂��B</summary>
		internal object DefaultValue { get; private set; }
	}
}