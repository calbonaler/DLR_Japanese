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
	/// <summary>�}�l�[�W���[�U�[�^�� COM �Ăяo���̈����Ƃ��ēn�����ꍇ�́A�}�[�V�������O����� VarEnum �^�𔻒f������@��񋟂��܂��B</summary>
	/// <remarks>
	/// (�v���~�e�B�u�^�� COM �I�u�W�F�N�g�ɑΗ�������̂Ƃ��Ă�) �}�l�[�W���[�U�[�^�� COM �Ăяo���̈����Ƃ��ēn�����ꍇ�A�}�[�V�������O����� VarEnum �^�𔻒f����K�v������܂��B
	/// ���L�̑I����������܂�:
	/// 1. ��O�𔭐�������
	///	   �Ǝ��o�[�W�����̃v���~�e�B�u�^��������͌���̌^���g�p���� COM ���\�b�h���ĂԂ��Ƃ��ł��܂���B(�Ⴆ�΁AIronRuby �� string �� <see cref="System.String"/> �ł͂���܂���)
	///	   �����I�ȃL���X�g���K�v�ɂȂ�܂��B
	/// 2. VT_DISPATCH �Ƃ��ă}�[�V�������O����
	///    ���̏ꍇ�ACOM �R�[�h�͒x���o�C���f�B���O�����ł��ׂĂ� API �ɃA�N�Z�X���邱�Ƃ��ł��܂����A�Â� COM �R���|�[�l���g�̓v���~�e�B�u�^��\�����Ă���ꍇ�A����ɋ@�\���܂���B
	/// 3. �ǂ̃v���~�e�B�u�^�������Ƃ��߂��̂��𔻒f����
	///    ����ɂ�� COM �R���|�[�l���g�� .NET ���\�b�h�Ɠ������炢�ȒP�ɗ��p�ł���悤�ɂȂ�܂��B
	/// 4. �^�C�v���C�u�����𗘗p���āA�\������Ă���^�������𔻒f����
	///    �������A�^�C�v���C�u�����͗��p�\�łȂ��ꍇ������܂��B
	/// <see cref="VarEnumSelector"/> �� 3 �Ԗڂ̑I�������������܂��B
	/// </remarks>
	class VarEnumSelector
	{
		static readonly Dictionary<VarEnum, Type> _ComToManagedPrimitiveTypes = CreateComToManagedPrimitiveTypes();
		static readonly IList<IList<VarEnum>> _ComPrimitiveTypeFamilies = CreateComPrimitiveTypeFamilies();

		internal VarEnumSelector(Type[] explicitArgTypes) { VariantBuilders = new ReadOnlyCollection<VariantBuilder>(Array.ConvertAll(explicitArgTypes, x => GetVariantBuilder(x))); }

		internal ReadOnlyCollection<VariantBuilder> VariantBuilders { get; private set; }

		/// <summary>Variant �Ƃ��ĕ\�����Ƃ��ł���悤�ɃI�u�W�F�N�g���ϊ������K�v������}�l�[�W�^���擾���܂��B</summary>
		/// <remarks>
		/// ��ʂɁA<see cref="Type"/> �� <see cref="VarEnum"/> �̊Ԃɂ͑��Α��̎ʑ������݂��܂��B
		/// ���������̃��\�b�h�͌��݂̎����ɕK�v�ȒP���Ȏʑ���Ԃ��܂��B
		/// ���Α��֌W�Ɋւ��闝�R�͎��̂悤�Ȃ��̂ł�:
		/// 1. <see cref="Int32"/> �� VT_ERROR �Ɠ��l�� VT_I4 �ɂ��}�b�s���O����A<see cref="Decimal"/> �� VT_DECIMAL �� VT_CY �Ƀ}�b�s���O����܂��B
		///    �������A����̓��b�p�[�^��������ƕω����܂��B
		/// 2. COM �^��\���^�����݂��Ȃ��ꍇ������܂��B__ComObject �̓v���C�x�[�g�ł���A<see cref="Object"/> �ł͔ėp�I�����܂��B
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

		/// <summary>���ꂼ��̃t�@�~���Ɋ܂܂�Ă���悤�� COM �^�̃t�@�~�����쐬���܂��B�t�@�~�����Ŏ�O�̌^�ɑ΂��Ă͊��S�ɖ������ȕϊ������݂��܂��B</summary>
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

		/// <summary>�����ɑ΂���œK�ȕϊ������݂����ӂȃv���~�e�B�u�^�����݂��邩�ǂ����𔻒f���܂��B</summary>
		static bool TryGetPrimitiveComTypeViaConversion(Type argumentType, out VarEnum primitiveVarEnum)
		{
			// ������ϊ��ł����ӂȌ^�t�@�~��������
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

		// Type.InvokeMember �� VT_DISPATCH �Ƃ��ăI�u�W�F�N�g���}�[�V�������O���悤�Ǝ��݁AVT_UNKNOWN�A���[�U�[��`�^���܂�ł��邱�Ƃ����� VT_RECORD �Ƀt�H�[���o�b�N���܂��B
		// VT_DISPATCH �����݂āAGetNativeVariantForObejct ���Ăяo���܂��B
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
			// �����̌���͗񋓑̂���ɂȂ�^�Ƃ��Ďg�p����邱�Ƃɑ΂��Ė����I�L���X�g���K�v�ł��B
			// �������A���݉^�p�A�Z���u���̗񋓑̂������Ƃ��Ďg�p�ł���悤�ɁA�����I�L���X�g�̕K�v�Ȃ� COM �ɑ΂��Ă��̕ϊ����ł���悤�ɂ��܂��B
			if (argumentType.IsEnum)
			{
				argumentType = Enum.GetUnderlyingType(argumentType);
				return GetComType(ref argumentType);
			}
			// COM �͒l�^�� null ��\���ł��Ȃ��̂ŁA��ɂȂ�^�֕ϊ�����
			// �l���Ȃ��ꍇ�͗�O���X���[����
			if (Utils.TypeUtils.IsNullableType(argumentType))
			{
				argumentType = Utils.TypeUtils.GetNonNullableType(argumentType);
				return GetComType(ref argumentType);
			}
			// COM �ɃW�F�l���b�N�^�͌��J�ł��Ȃ��̂ŁACOM �C���^�[�t�F�C�X�������ł��Ȃ��B
			if (argumentType.IsGenericType)
				return VarEnum.VT_UNKNOWN;
			VarEnum primitiveVarEnum;
			if (TryGetPrimitiveComType(argumentType, out primitiveVarEnum))
				return primitiveVarEnum;
			// �^������ COM �^�Ƀ}�[�V�������O������@��������Ȃ�����
			return VT_DEFAULT;
		}

		/// <summary>COM �ւ̌Ăяo���Ƃ��Ĉ������}�[�V�������O����ׂ� COM Variant �^���擾���܂��B</summary>
		VariantBuilder GetVariantBuilder(Type argumentType)
		{
			// argumentType �� MarshalType ���痈�Ă��āAnull �͓��I�I�u�W�F�N�g�� null �l��ێ����Ă��āA�Q�Ɠn���ł͂Ȃ����Ƃ��Ӗ�����
			if (argumentType == null)
				return new VariantBuilder(VarEnum.VT_EMPTY, new NullArgBuilder());
			if (argumentType == typeof(DBNull))
				return new VariantBuilder(VarEnum.VT_NULL, new NullArgBuilder());
			if (argumentType.IsByRef)
			{
				var elementType = argumentType.GetElementType();
				VarEnum elementVarEnum;
				if (elementType == typeof(object) || elementType == typeof(DBNull)) 
					elementVarEnum = VarEnum.VT_VARIANT; // ByRef ��n���Ӗ��̂Ȃ��l�B������������Ăяo����������������ɒu�������邩������Ȃ��B�o���A���g�Q�ƂƂ��ēn���K�v����
				else
					elementVarEnum = GetComType(ref elementType);
				return new VariantBuilder(elementVarEnum | VarEnum.VT_BYREF, GetSimpleArgBuilder(elementType, elementVarEnum));
			}
			var varEnum = GetComType(ref argumentType);
			var argBuilder = GetByValArgBuilder(argumentType, ref varEnum);
			return new VariantBuilder(varEnum, argBuilder);
		}

		// �l�n���}�[�V�������O�̌������ɌĂяo�����B
		// �l�n���̏ꍇ�A���̂��ׂẴ}�[�V�������O�^�������鎎�s�����s�����ꍇ�A�ϊ��܂��� IConvertible ���l���ɓ���邱�Ƃ��ł���B
		static ArgBuilder GetByValArgBuilder(Type elementType, ref VarEnum elementVarEnum)
		{
			// VT �̓}�[�V�������O�^���s���ł��邱�Ƃ�����
			if (elementVarEnum == VT_DEFAULT)
			{
				// �ϊ��̌��������݂�
				VarEnum convertibleTo;
				if (TryGetPrimitiveComTypeViaConversion(elementType, out convertibleTo))
				{
					elementVarEnum = convertibleTo;
					return new ConversionArgBuilder(elementType, GetSimpleArgBuilder(GetManagedMarshalType(elementVarEnum), elementVarEnum));
				}
				// IConvertible �ɂ��Ē��ׂ�
				if (typeof(IConvertible).IsAssignableFrom(elementType))
					return new ConvertibleArgBuilder();
			}
			return GetSimpleArgBuilder(elementType, elementVarEnum);
		}

		// ���̃��\�b�h�� Variant �ɂ���Ē��ڃT�|�[�g�����^�ɑ΂���r���_�[�𐶐����܂��B
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