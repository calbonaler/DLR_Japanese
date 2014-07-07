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
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using Microsoft.Scripting.Utils;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace Microsoft.Scripting.ComInterop
{
	static class ComRuntimeHelpers
	{
		[SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
		[SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "1#")]
		public static void CheckThrowException(int hresult, ref ExcepInfo excepInfo, uint argErr, string message)
		{
			if (ComHresults.IsSuccess(hresult))
				return;
			switch (hresult)
			{
				case ComHresults.DISP_E_BADPARAMCOUNT:
					// DISPPARAMS �ɑ΂��ēn���ꂽ�v�f�������\�b�h�܂��̓v���p�e�B���󂯓��������̐��ƈقȂ��Ă��܂�
					throw Error.DispBadParamCount(message);
				case ComHresults.DISP_E_BADVARTYPE:
					// rgvarg �̈����� 1 ���L���ȃo���A���g�^�ł͂���܂���B
					break;
				case ComHresults.DISP_E_EXCEPTION:
					// �A�v���P�[�V�����͗�O�𔭐�������K�v������܂��B���̏ꍇ�ApExcepInfo �œn���ꂽ�\���̂��i�[����ׂ��ł��B
					throw excepInfo.GetException();
				case ComHresults.DISP_E_MEMBERNOTFOUND:
					// �v�����ꂽ�����o�����݂��܂���B�܂��́AInvoke �ւ̌Ăяo���œǂݎ���p�̃v���p�e�B�ɒl��ݒ肵�悤�Ƃ��܂����B
					throw Error.DispMemberNotFound(message);
				case ComHresults.DISP_E_NONAMEDARGS:
					// ���� IDispatch ������͖��O�t���������T�|�[�g���Ă��܂���B
					throw Error.DispNoNamedArgs(message);
				case ComHresults.DISP_E_OVERFLOW:
					// rgvarg �̈����� 1 ���w�肳�ꂽ�^�ɕϊ��ł��܂���B
					throw Error.DispOverflow(message);
				case ComHresults.DISP_E_PARAMNOTFOUND:
					// ������ DISPID �� 1 �����\�b�h�̈����ɑΉ����Ă��܂���B���̏ꍇ�́ApuArgErr ���G���[���i�[���Ă���ŏ��̈����ɐݒ肷��K�v������܂��B
					break;
				case ComHresults.DISP_E_TYPEMISMATCH:
					// 1 �ȏ�̈������L���X�g�ł��܂���ł����B�������Ȃ��^�ł���ŏ��̈����� rgvarg ���̃C���f�b�N�X�� puArgErr �����ŕԂ���܂��B
					throw Error.DispTypeMismatch(argErr, message);
				case ComHresults.DISP_E_UNKNOWNINTERFACE:
					// riid �ɓn���ꂽ�C���^�[�t�F�C�X���ʎq�� IID_NULL �ł͂���܂���B
					break;
				case ComHresults.DISP_E_UNKNOWNLCID:
					// �Ăяo���ꂽ�����o�� LCID �ɂ���ĕ������������͂��܂����A���� LCID ��F���ł��܂���B
					break;
				case ComHresults.DISP_E_PARAMNOTOPTIONAL:
					// �K�{�̈������ȗ�����܂����B
					throw Error.DispParamNotOptional(message);
			}
			Marshal.ThrowExceptionForHR(hresult);
		}

		internal static void GetInfoFromType(ComTypes.ITypeInfo typeInfo, out string name, out string documentation)
		{
			int dwHelpContext;
			string strHelpFile;
			typeInfo.GetDocumentation(-1, out name, out documentation, out dwHelpContext, out strHelpFile);
		}

		internal static string GetNameOfMethod(ComTypes.ITypeInfo typeInfo, int memid)
		{
			int cNames;
			string[] rgNames = new string[1];
			typeInfo.GetNames(memid, rgNames, 1, out cNames);
			return rgNames[0];
		}

		internal static string GetNameOfLib(ComTypes.ITypeLib typeLib)
		{
			string name;
			string strDocString;
			int dwHelpContext;
			string strHelpFile;
			typeLib.GetDocumentation(-1, out name, out strDocString, out dwHelpContext, out strHelpFile);
			return name;
		}

		internal static string GetNameOfType(ComTypes.ITypeInfo typeInfo)
		{
			string name;
			string documentation;
			GetInfoFromType(typeInfo, out name, out documentation);
			return name;
		}

		/// <summary>IDispatch.GetTypeInfo ���g�p���āATypeInfo ���������܂��B</summary>
		/// <param name="dispatch"></param>
		/// <param name="throwIfMissingExpectedTypeInfo">TypeInfo �����݂��Ȃ��ꍇ�ɗ�O���X���[���邩�ǂ����������l���w�肵�܂��B</param>
		/// <returns></returns>
		internal static ComTypes.ITypeInfo GetITypeInfoFromIDispatch(IDispatch dispatch, bool throwIfMissingExpectedTypeInfo)
		{
			uint typeCount;
			var hresult = dispatch.TryGetTypeInfoCount(out typeCount);
			Marshal.ThrowExceptionForHR(hresult);
			Debug.Assert(typeCount <= 1);
			if (typeCount == 0)
				return null;
			var typeInfoPtr = IntPtr.Zero;
			hresult = dispatch.TryGetTypeInfo(0, 0, out typeInfoPtr);
			if (!ComHresults.IsSuccess(hresult))
			{
				CheckIfMissingTypeInfoIsExpected(hresult, throwIfMissingExpectedTypeInfo);
				return null;
			}
			if (typeInfoPtr == IntPtr.Zero)
			{ // IntPtr.Zero ��Ԃ��R���|�[�l���g�ɑ΂���h���
				if (throwIfMissingExpectedTypeInfo)
					Marshal.ThrowExceptionForHR(ComHresults.E_FAIL);
				return null;
			}
			ComTypes.ITypeInfo typeInfo = null;
			try { typeInfo = Marshal.GetObjectForIUnknown(typeInfoPtr) as ComTypes.ITypeInfo; }
			finally { Marshal.Release(typeInfoPtr); }
			return typeInfo;
		}

		/// <summary>
		/// ���̃��\�b�h�� TypeInfo ���I�u�W�F�N�g�ŗ��p�ł��Ȃ��ꍇ�ɌĂ΂�܂��B
		/// ����� TypeInfo �����݂��Ȃ����Ƃ��󂯓�����邩�ǂ����𔻒f���܂��B
		/// ����͓����G���[�����ł��A���ׂẴ}�V���ŁA�ǂ̂悤�ȏ󋵂ł��������邱�Ƃ��ۏ؂����ꍇ���܂߂邱�Ƃ��ł��܂��B
		/// ���̂悤�ȏꍇ�ATypeInfo �Ȃ��ő��삷��K�v�������܂��B
		/// �������A�ꎞ�I�ȕ��@�� TypeInfo �̌Ăяo�������s����ꍇ�A�{���ɗ\�z�ʂ�ɖ���\�����߂ɗ�O���X���[�������ƍl���邩������܂���B
		/// </summary>
		static void CheckIfMissingTypeInfoIsExpected(int hresult, bool throwIfMissingExpectedTypeInfo)
		{
			Debug.Assert(!ComHresults.IsSuccess(hresult));
			// Word.Basic �� IDispatch.GetTypeInfo �̐������Ȃ������ɂ���ɂ����Ԃ��܂��B
			// E_NOINTERFACE ��Ԃ�����������͂��ׂĂ̊��ł����Ȃ�ł��傤
			if (hresult == ComHresults.E_NOINTERFACE)
				return;
			// COM �R���|�[�l���g�͔��ɗ\�����Ȃ����@�ŐU�镑���̂ŁA���̕\���͐��ݓI�ɐ��񒴉z�I�ł��B
			// �������Ȃ���A���ʂ̗\�������P�[�X��\�����邱�Ƃ͗\�����Ȃ��V�i���I�𔭌����邱�Ƃ��m���ɂ��āA���̃R�[�h�Ƀo�O���Ȃ����Ƃ��m�F���ăV�i���I�����؂ł��܂��B
			Debug.Assert(hresult == ComHresults.TYPE_E_LIBNOTREGISTERED);
			if (throwIfMissingExpectedTypeInfo)
				Marshal.ThrowExceptionForHR(hresult);
		}

		[SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
		internal static ComTypes.TYPEATTR GetTypeAttrForTypeInfo(ComTypes.ITypeInfo typeInfo)
		{
			var pAttrs = IntPtr.Zero;
			typeInfo.GetTypeAttr(out pAttrs);
			// GetTypeAttr �� null ��Ԃ��܂��񂪁A����͈��S�̂��߂ł��B
			if (pAttrs == IntPtr.Zero)
				throw Error.CannotRetrieveTypeInformation();
			try { return (ComTypes.TYPEATTR)Marshal.PtrToStructure(pAttrs, typeof(ComTypes.TYPEATTR)); }
			finally { typeInfo.ReleaseTypeAttr(pAttrs); }
		}

		[SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
		internal static ComTypes.TYPELIBATTR GetTypeAttrForTypeLib(ComTypes.ITypeLib typeLib)
		{
			var pAttrs = IntPtr.Zero;
			typeLib.GetLibAttr(out pAttrs);
			// GetTypeAttr �� null ��Ԃ��܂��񂪁A����͈��S�̂��߂ł��B
			if (pAttrs == IntPtr.Zero)
				throw Error.CannotRetrieveTypeInformation();
			try { return (ComTypes.TYPELIBATTR)Marshal.PtrToStructure(pAttrs, typeof(ComTypes.TYPELIBATTR)); }
			finally { typeLib.ReleaseTLibAttr(pAttrs); }
		}

		public static BoundDispEvent CreateComEvent(object rcw, Guid sourceIid, int dispid) { return new BoundDispEvent(rcw, sourceIid, dispid); }

		public static DispCallable CreateDispCallable(IDispatchComObject dispatch, ComMethodDesc method) { return new DispCallable(dispatch, method.Name, method.DispId); }
	}

	/// <summary>
	/// ���̃N���X�� C# �ŕ\���ł��Ȃ��A�܂��̓A���Z�[�t �R�[�h�̏������݂�v�����郁�\�b�h���i�[���܂��B
	/// �����̃��\�b�h�̐������Ȃ��g�p�� GC �z�[���⑼�̖��������N�������ꂪ���邽�߁A�Ăяo�����͋ɂ߂ĐT�d�Ɏg�p����K�v������܂��B
	/// </summary>
	static class UnsafeMethods
	{
		[System.Runtime.Versioning.ResourceExposure(System.Runtime.Versioning.ResourceScope.None)]
		[System.Runtime.Versioning.ResourceConsumption(System.Runtime.Versioning.ResourceScope.Process, System.Runtime.Versioning.ResourceScope.Process)]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass")] // TODO: fix
		[DllImport("oleaut32.dll", PreserveSig = false)]
		internal static extern void VariantClear(IntPtr variant);

		[System.Runtime.Versioning.ResourceExposure(System.Runtime.Versioning.ResourceScope.Machine)]
		[System.Runtime.Versioning.ResourceConsumption(System.Runtime.Versioning.ResourceScope.Machine, System.Runtime.Versioning.ResourceScope.Machine)]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass")] // TODO: fix
		[DllImport("oleaut32.dll", PreserveSig = false)]
		internal static extern ComTypes.ITypeLib LoadRegTypeLib(ref Guid clsid, short majorVersion, short minorVersion, int lcid);

		#region public members

		static readonly MethodInfo _ConvertByrefToPtr = Create_ConvertByrefToPtr();

		public delegate IntPtr ConvertByrefToPtrDelegate<T>(ref T value);

		static readonly ConvertByrefToPtrDelegate<Variant> _ConvertVariantByrefToPtr = (ConvertByrefToPtrDelegate<Variant>)Delegate.CreateDelegate(typeof(ConvertByrefToPtrDelegate<Variant>), _ConvertByrefToPtr.MakeGenericMethod(typeof(Variant)));

		static MethodInfo Create_ConvertByrefToPtr()
		{
			// We dont use AssemblyGen.DefineMethod since that can create a anonymously-hosted DynamicMethod which cannot contain unverifiable code.
			var type = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("ComSnippets"), AssemblyBuilderAccess.Run).DefineDynamicModule("ComSnippets").DefineType("Type$ConvertByrefToPtr", TypeAttributes.Public);
			var mb = type.DefineMethod("ConvertByrefToPtr", MethodAttributes.Public | MethodAttributes.Static, typeof(IntPtr), new[] { typeof(Variant).MakeByRefType() });
			var typeParams = mb.DefineGenericParameters("T");
			typeParams[0].SetGenericParameterAttributes(GenericParameterAttributes.NotNullableValueTypeConstraint);
			mb.SetSignature(typeof(IntPtr), null, null, new[] { typeParams[0].MakeByRefType() }, null, null);
			var method = mb.GetILGenerator();
			method.Emit(OpCodes.Ldarg_0);
			method.Emit(OpCodes.Conv_I);
			method.Emit(OpCodes.Ret);
			return type.CreateType().GetMethod("ConvertByrefToPtr");
		}

		#region Generated Convert ByRef Delegates

		// *** BEGIN GENERATED CODE ***
		// generated by function: gen_ConvertByrefToPtrDelegates from: generate_comdispatch.py

		private static readonly ConvertByrefToPtrDelegate<SByte> _ConvertSByteByrefToPtr = (ConvertByrefToPtrDelegate<SByte>)Delegate.CreateDelegate(typeof(ConvertByrefToPtrDelegate<SByte>), _ConvertByrefToPtr.MakeGenericMethod(typeof(SByte)));
		private static readonly ConvertByrefToPtrDelegate<Int16> _ConvertInt16ByrefToPtr = (ConvertByrefToPtrDelegate<Int16>)Delegate.CreateDelegate(typeof(ConvertByrefToPtrDelegate<Int16>), _ConvertByrefToPtr.MakeGenericMethod(typeof(Int16)));
		private static readonly ConvertByrefToPtrDelegate<Int32> _ConvertInt32ByrefToPtr = (ConvertByrefToPtrDelegate<Int32>)Delegate.CreateDelegate(typeof(ConvertByrefToPtrDelegate<Int32>), _ConvertByrefToPtr.MakeGenericMethod(typeof(Int32)));
		private static readonly ConvertByrefToPtrDelegate<Int64> _ConvertInt64ByrefToPtr = (ConvertByrefToPtrDelegate<Int64>)Delegate.CreateDelegate(typeof(ConvertByrefToPtrDelegate<Int64>), _ConvertByrefToPtr.MakeGenericMethod(typeof(Int64)));
		private static readonly ConvertByrefToPtrDelegate<Byte> _ConvertByteByrefToPtr = (ConvertByrefToPtrDelegate<Byte>)Delegate.CreateDelegate(typeof(ConvertByrefToPtrDelegate<Byte>), _ConvertByrefToPtr.MakeGenericMethod(typeof(Byte)));
		private static readonly ConvertByrefToPtrDelegate<UInt16> _ConvertUInt16ByrefToPtr = (ConvertByrefToPtrDelegate<UInt16>)Delegate.CreateDelegate(typeof(ConvertByrefToPtrDelegate<UInt16>), _ConvertByrefToPtr.MakeGenericMethod(typeof(UInt16)));
		private static readonly ConvertByrefToPtrDelegate<UInt32> _ConvertUInt32ByrefToPtr = (ConvertByrefToPtrDelegate<UInt32>)Delegate.CreateDelegate(typeof(ConvertByrefToPtrDelegate<UInt32>), _ConvertByrefToPtr.MakeGenericMethod(typeof(UInt32)));
		private static readonly ConvertByrefToPtrDelegate<UInt64> _ConvertUInt64ByrefToPtr = (ConvertByrefToPtrDelegate<UInt64>)Delegate.CreateDelegate(typeof(ConvertByrefToPtrDelegate<UInt64>), _ConvertByrefToPtr.MakeGenericMethod(typeof(UInt64)));
		private static readonly ConvertByrefToPtrDelegate<IntPtr> _ConvertIntPtrByrefToPtr = (ConvertByrefToPtrDelegate<IntPtr>)Delegate.CreateDelegate(typeof(ConvertByrefToPtrDelegate<IntPtr>), _ConvertByrefToPtr.MakeGenericMethod(typeof(IntPtr)));
		private static readonly ConvertByrefToPtrDelegate<UIntPtr> _ConvertUIntPtrByrefToPtr = (ConvertByrefToPtrDelegate<UIntPtr>)Delegate.CreateDelegate(typeof(ConvertByrefToPtrDelegate<UIntPtr>), _ConvertByrefToPtr.MakeGenericMethod(typeof(UIntPtr)));
		private static readonly ConvertByrefToPtrDelegate<Single> _ConvertSingleByrefToPtr = (ConvertByrefToPtrDelegate<Single>)Delegate.CreateDelegate(typeof(ConvertByrefToPtrDelegate<Single>), _ConvertByrefToPtr.MakeGenericMethod(typeof(Single)));
		private static readonly ConvertByrefToPtrDelegate<Double> _ConvertDoubleByrefToPtr = (ConvertByrefToPtrDelegate<Double>)Delegate.CreateDelegate(typeof(ConvertByrefToPtrDelegate<Double>), _ConvertByrefToPtr.MakeGenericMethod(typeof(Double)));
		private static readonly ConvertByrefToPtrDelegate<Decimal> _ConvertDecimalByrefToPtr = (ConvertByrefToPtrDelegate<Decimal>)Delegate.CreateDelegate(typeof(ConvertByrefToPtrDelegate<Decimal>), _ConvertByrefToPtr.MakeGenericMethod(typeof(Decimal)));

		// *** END GENERATED CODE ***

		#endregion

		#region Generated Outer ConvertByrefToPtr

		// *** BEGIN GENERATED CODE ***
		// generated by function: gen_ConvertByrefToPtr from: generate_comdispatch.py

		[SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
		public static IntPtr ConvertSByteByrefToPtr(ref SByte value) { return _ConvertSByteByrefToPtr(ref value); }
		[SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
		public static IntPtr ConvertInt16ByrefToPtr(ref Int16 value) { return _ConvertInt16ByrefToPtr(ref value); }
		[SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
		public static IntPtr ConvertInt32ByrefToPtr(ref Int32 value) { return _ConvertInt32ByrefToPtr(ref value); }
		[SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
		public static IntPtr ConvertInt64ByrefToPtr(ref Int64 value) { return _ConvertInt64ByrefToPtr(ref value); }
		[SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
		public static IntPtr ConvertByteByrefToPtr(ref Byte value) { return _ConvertByteByrefToPtr(ref value); }
		[SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
		public static IntPtr ConvertUInt16ByrefToPtr(ref UInt16 value) { return _ConvertUInt16ByrefToPtr(ref value); }
		[SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
		public static IntPtr ConvertUInt32ByrefToPtr(ref UInt32 value) { return _ConvertUInt32ByrefToPtr(ref value); }
		[SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
		public static IntPtr ConvertUInt64ByrefToPtr(ref UInt64 value) { return _ConvertUInt64ByrefToPtr(ref value); }
		[SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
		public static IntPtr ConvertIntPtrByrefToPtr(ref IntPtr value) { return _ConvertIntPtrByrefToPtr(ref value); }
		[SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
		public static IntPtr ConvertUIntPtrByrefToPtr(ref UIntPtr value) { return _ConvertUIntPtrByrefToPtr(ref value); }
		[SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
		public static IntPtr ConvertSingleByrefToPtr(ref Single value) { return _ConvertSingleByrefToPtr(ref value); }
		[SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
		public static IntPtr ConvertDoubleByrefToPtr(ref Double value) { return _ConvertDoubleByrefToPtr(ref value); }
		[SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
		public static IntPtr ConvertDecimalByrefToPtr(ref Decimal value) { return _ConvertDecimalByrefToPtr(ref value); }

		// *** END GENERATED CODE ***

		#endregion

		[SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
		public static IntPtr ConvertVariantByrefToPtr(ref Variant value) { return _ConvertVariantByrefToPtr(ref value); }

		internal static Variant GetVariantForObject(object obj)
		{
			var variant = default(Variant);
			if (obj == null)
				return variant;
			InitVariantForObject(obj, ref variant);
			return variant;
		}

		internal static void InitVariantForObject(object obj, ref Variant variant)
		{
			Debug.Assert(obj != null);
			// GetNativeVariantForObject is very expensive for values that marshal as VT_DISPATCH
			// also is is extremely common scenario when object at hand is an RCW. 
			// Therefore we are going to test for IDispatch before defaulting to GetNativeVariantForObject.
			IDispatch disp = obj as IDispatch;
			if (disp != null)
			{
				variant.AsDispatch = obj;
				return;
			}
			System.Runtime.InteropServices.Marshal.GetNativeVariantForObject(obj, ConvertVariantByrefToPtr(ref variant));
		}

		public static object GetObjectForVariant(Variant variant) { return System.Runtime.InteropServices.Marshal.GetObjectForNativeVariant(UnsafeMethods.ConvertVariantByrefToPtr(ref variant)); }

		public static int IUnknownRelease(IntPtr interfacePointer) { return _IUnknownRelease(interfacePointer); }

		public static void IUnknownReleaseNotZero(IntPtr interfacePointer)
		{
			if (interfacePointer != IntPtr.Zero)
				IUnknownRelease(interfacePointer);
		}

		[SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
		[Obsolete("do not use this method", true)]
		public static int IDispatchInvoke(IntPtr dispatchPointer, int memberDispId, ComTypes.INVOKEKIND flags, ref ComTypes.DISPPARAMS dispParams, out Variant result, out ExcepInfo excepInfo, out uint argErr)
		{
			var hresult = _IDispatchInvoke(dispatchPointer, memberDispId, flags, ref dispParams, out result, out excepInfo, out argErr);
			if (hresult == ComHresults.DISP_E_MEMBERNOTFOUND && (flags & ComTypes.INVOKEKIND.INVOKE_FUNC) != 0 && (flags & (ComTypes.INVOKEKIND.INVOKE_PROPERTYPUT | ComTypes.INVOKEKIND.INVOKE_PROPERTYPUTREF)) == 0)
				// Word ���i�[���錋�ʈ�����n�����ɍČĂяo��
				hresult = _IDispatchInvokeNoResult(dispatchPointer, memberDispId, ComTypes.INVOKEKIND.INVOKE_FUNC, ref dispParams, out result, out excepInfo, out argErr);
			return hresult;
		}

		[Obsolete("do not use this method", true)]
		public static IntPtr GetIdsOfNamedParameters(IDispatch dispatch, string[] names, int methodDispId, out GCHandle pinningHandle)
		{
			pinningHandle = GCHandle.Alloc(null, GCHandleType.Pinned);
			var dispIds = new int[names.Length];
			var empty = Guid.Empty;
			var hresult = dispatch.TryGetIDsOfNames(ref empty, names, (uint)names.Length, 0, dispIds);
			if (hresult < 0)
				Marshal.ThrowExceptionForHR(hresult);
			if (methodDispId != dispIds[0])
				throw Error.GetIDsOfNamesInvalid(names[0]);
			var keywordArgDispIds = ArrayUtils.RemoveFirst(dispIds); // Remove the dispId of the method name
			pinningHandle.Target = keywordArgDispIds;
			return Marshal.UnsafeAddrOfPinnedArrayElement(keywordArgDispIds, 0);
		}

		#endregion

		#region non-public members

		static void EmitLoadArg(ILGenerator il, int index)
		{
			ContractUtils.Requires(index >= 0, "index");
			switch (index)
			{
				case 0:
					il.Emit(OpCodes.Ldarg_0);
					break;
				case 1:
					il.Emit(OpCodes.Ldarg_1);
					break;
				case 2:
					il.Emit(OpCodes.Ldarg_2);
					break;
				case 3:
					il.Emit(OpCodes.Ldarg_3);
					break;
				default:
					if (index <= byte.MaxValue)
						il.Emit(OpCodes.Ldarg_S, (byte)index);
					else
						il.Emit(OpCodes.Ldarg, index);
					break;
			}
		}

		/// <summary>
		/// "value" ���������̌Ăяo�����̃t���[���Ń��[�J���ϐ��ł��邱�Ƃ��m�F���܂��B
		/// ���̂��߁Abyref ���� IntPtr �ւ̕ϊ��͈��S�ȑ���ł��B
		/// ����ɁA�����ꂽ "value" ���s�����ꂽ�I�u�W�F�N�g�ɂ��邱�Ƃ������܂��B
		/// </summary>
		[Conditional("DEBUG")]
		public static void AssertByrefPointsToStack(IntPtr ptr)
		{
			if (Marshal.ReadInt32(ptr) == _dummyMarker)
				return; // Prevent recursion
			var dummy = _dummyMarker;
			var ptrToLocal = ConvertInt32ByrefToPtr(ref dummy);
			Debug.Assert(ptrToLocal.ToInt64() < ptr.ToInt64());
			Debug.Assert(ptr.ToInt64() - ptrToLocal.ToInt64() < 16 * 1024);
		}

		static readonly object _lock = new object();
		static ModuleBuilder _dynamicModule;

		internal static ModuleBuilder DynamicModule
		{
			get
			{
				if (_dynamicModule != null)
					return _dynamicModule;
				lock (_lock)
				{
					if (_dynamicModule == null)
					{
						var attributes = new[] { 
                            new CustomAttributeBuilder(typeof(UnverifiableCodeAttribute).GetConstructor(Type.EmptyTypes), new object[0]),
                            //PermissionSet(SecurityAction.Demand, Unrestricted = true)
                            new CustomAttributeBuilder(typeof(PermissionSetAttribute).GetConstructor(new[] { typeof(SecurityAction) }), 
                                new object[] { SecurityAction.Demand },
                                new[] { typeof(PermissionSetAttribute).GetProperty("Unrestricted") }, 
                                new object[] { true }
							)
                        };
						var name = typeof(VariantArray).Namespace + ".DynamicAssembly";
						var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(name), AssemblyBuilderAccess.Run, attributes);
						assembly.DefineVersionInfoResource();
						_dynamicModule = assembly.DefineDynamicModule(name);
					}
					return _dynamicModule;
				}
			}
		}

		const int _dummyMarker = 0x10101010;

		// �w�肳�ꂽ�C���^�[�t�F�C�X �|�C���^�̉��z�֐��e�[�u������̃A���}�l�[�W�֐��|�C���^�̊ԐړI�ȌĂяo���𔭍s���܂��B
		// ���̃A�v���[�`�� 900 ���߈ȓ��� Marshal.Release �Ɣ�ׂ�� x86 �� 300 ���߂܂łȂ�΂Ƃ邱�Ƃ��ł��܂��B
		// JIT �R���p�C���� pinvoke �X�^�u�C�����C������ pinvoke �^�[�Q�b�g�̒��ڌĂяo�����s�����Ƃ𓖂Ăɂ��Ă��܂��B
		delegate int IUnknownReleaseDelegate(IntPtr interfacePointer);
		static readonly IUnknownReleaseDelegate _IUnknownRelease = Create_IUnknownRelease();
		static IUnknownReleaseDelegate Create_IUnknownRelease()
		{
			var dm = new DynamicMethod("IUnknownRelease", typeof(int), new Type[] { typeof(IntPtr) }, DynamicModule);
			var method = dm.GetILGenerator();
			// return functionPtr(...)
			method.Emit(OpCodes.Ldarg_0);
			// functionPtr = *(IntPtr*)(*(interfacePointer) + VTABLE_OFFSET)
			int iunknownReleaseOffset = ((int)IDispatchMethodIndices.IUnknown_Release) * Marshal.SizeOf(typeof(IntPtr));
			method.Emit(OpCodes.Ldarg_0);
			method.Emit(OpCodes.Ldind_I);
			method.Emit(OpCodes.Ldc_I4, iunknownReleaseOffset);
			method.Emit(OpCodes.Add);
			method.Emit(OpCodes.Ldind_I);
			var signature = SignatureHelper.GetMethodSigHelper(CallingConvention.Winapi, typeof(int));
			signature.AddArgument(typeof(IntPtr));
			method.Emit(OpCodes.Calli, signature);
			method.Emit(OpCodes.Ret);
			return (IUnknownReleaseDelegate)dm.CreateDelegate(typeof(IUnknownReleaseDelegate));
		}

		internal static readonly IntPtr NullInterfaceId = GetNullInterfaceId();

		static IntPtr GetNullInterfaceId()
		{
			var size = Marshal.SizeOf(Guid.Empty);
			var ptr = Marshal.AllocHGlobal(size);
			for (int i = 0; i < size; i++)
				Marshal.WriteByte(ptr, i, 0);
			return ptr;
		}

		// �w�肳�ꂽ IDispatch �C���^�[�t�F�C�X �|�C���^�̉��z�֐��e�[�u������̃A���}�l�[�W�֐��|�C���^�̊ԐړI�ȌĂяo���𔭍s���܂��B
		// ����� C# �ŕ\�����邱�Ƃ͂ł��܂���B
		// �ԐړI�� PInvoke �Ăяo���ɂ���ăJ�X�^�� �}�[�V�������O���\�ɂȂ�܂��B
		// Variant �������ȒP�ɃX�^�b�N�Ɋm�ۂł��܂��B
		// JIT �R���p�C���� pinvoke �X�^�u�C�����C������ pinvoke �^�[�Q�b�g�̒��ڌĂяo�����s�����Ƃ𓖂Ăɂ��Ă��܂��B
		// IDispatch �̃}�l�[�W��`��ʂ��ČĂяo����Ăɂ̓X�^�b�N��̈����̍ăv�b�V�������Ȃ���΂Ȃ�Ȃ� CLR �X�^�u�����s����Ȃǃp�t�H�[�}���X��̖�肪����܂��B
		// Marshal.GetDelegateForFunctionPointer �͂����Ŏg�p�ł��܂����A�ƂĂ������R�X�g�ɂȂ�܂��B(x86 �� 2000 ����)
		delegate int IDispatchInvokeDelegate(IntPtr dispatchPointer, int memberDispId, ComTypes.INVOKEKIND flags, ref ComTypes.DISPPARAMS dispParams, out Variant result, out ExcepInfo excepInfo, out uint argErr);

		static readonly IDispatchInvokeDelegate _IDispatchInvoke = Create_IDispatchInvoke(true);
		static IDispatchInvokeDelegate _IDispatchInvokeNoResultImpl;
		static IDispatchInvokeDelegate _IDispatchInvokeNoResult
		{
			get
			{
				if (_IDispatchInvokeNoResultImpl == null)
				{
					lock (_IDispatchInvoke)
					{
						if (_IDispatchInvokeNoResultImpl == null)
							_IDispatchInvokeNoResultImpl = Create_IDispatchInvoke(false);
					}
				}
				return _IDispatchInvokeNoResultImpl;
			}
		}

		static IDispatchInvokeDelegate Create_IDispatchInvoke(bool returnResult) {
            const int dispatchPointerIndex = 0;
            const int memberDispIdIndex = 1;
            const int flagsIndex = 2;
            const int dispParamsIndex = 3;
            const int resultIndex = 4;
            const int exceptInfoIndex = 5;
            const int argErrIndex = 6;
            Debug.Assert(argErrIndex + 1 == typeof(IDispatchInvokeDelegate).GetMethod("Invoke").GetParameters().Length);
            var paramTypes = new Type[argErrIndex + 1];
            paramTypes[dispatchPointerIndex] = typeof(IntPtr);
            paramTypes[memberDispIdIndex] = typeof(int);
            paramTypes[flagsIndex] = typeof(ComTypes.INVOKEKIND);
            paramTypes[dispParamsIndex] = typeof(ComTypes.DISPPARAMS).MakeByRefType();
            paramTypes[resultIndex] = typeof(Variant).MakeByRefType();
            paramTypes[exceptInfoIndex] = typeof(ExcepInfo).MakeByRefType();
            paramTypes[argErrIndex] = typeof(uint).MakeByRefType();
			// ���؂��X�L�b�v���邽�߂ɁA���I���\�b�h�����̃A�Z���u���ɒ�`����
            var dm = new DynamicMethod("IDispatchInvoke", typeof(int), paramTypes, DynamicModule);
            var method = dm.GetILGenerator();
            // return functionPtr(...)
            EmitLoadArg(method, dispatchPointerIndex);
            EmitLoadArg(method, memberDispIdIndex);
			// ��� IID �̃A�h���X�𒼐ڔ��s
			// ����͉������邱�Ƃ��Ċm�ۂ���邱�Ƃ��Ȃ�
			// ����𒼐� Guid �𔭍s����悤�ɂ���ƁAIDispatch �Ăяo���� 30% �̃p�t�H�[�}���X�q�b�g������̂ŁA���� IntPtr ��n��
            if (IntPtr.Size == 4)
                method.Emit(OpCodes.Ldc_I4, UnsafeMethods.NullInterfaceId.ToInt32()); // riid
            else
                method.Emit(OpCodes.Ldc_I8, UnsafeMethods.NullInterfaceId.ToInt64()); // riid
            method.Emit(OpCodes.Conv_I);
            method.Emit(OpCodes.Ldc_I4_0); // lcid
            EmitLoadArg(method, flagsIndex);
            EmitLoadArg(method, dispParamsIndex);
            if (returnResult)
                EmitLoadArg(method, resultIndex);
            else
                method.Emit(OpCodes.Ldsfld, typeof(IntPtr).GetField("Zero"));
            EmitLoadArg(method, exceptInfoIndex);
            EmitLoadArg(method, argErrIndex);
            // functionPtr = *(IntPtr*)(*(dispatchPointer) + VTABLE_OFFSET)
            EmitLoadArg(method, dispatchPointerIndex);
            method.Emit(OpCodes.Ldind_I);
            method.Emit(OpCodes.Ldc_I4, ((int)IDispatchMethodIndices.IDispatch_Invoke) * Marshal.SizeOf(typeof(IntPtr)));
            method.Emit(OpCodes.Add);
            method.Emit(OpCodes.Ldind_I);
            var signature = SignatureHelper.GetMethodSigHelper(CallingConvention.Winapi, typeof(int));
            signature.AddArguments(new[] { typeof(IntPtr), typeof(int), typeof(IntPtr), typeof(int), typeof(ushort), typeof(IntPtr), typeof(IntPtr), typeof(IntPtr), typeof(IntPtr) }, null, null);
            method.Emit(OpCodes.Calli, signature);
            method.Emit(OpCodes.Ret);
            return (IDispatchInvokeDelegate)dm.CreateDelegate(typeof(IDispatchInvokeDelegate));
        }

		#endregion
	}

	static class NativeMethods
	{
		[System.Runtime.Versioning.ResourceExposure(System.Runtime.Versioning.ResourceScope.None)]
		[System.Runtime.Versioning.ResourceConsumption(System.Runtime.Versioning.ResourceScope.Process, System.Runtime.Versioning.ResourceScope.Process)]
		[DllImport("oleaut32.dll", PreserveSig = false)]
		internal static extern void VariantClear(IntPtr variant);
	}
}