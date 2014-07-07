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
					// DISPPARAMS に対して渡された要素数がメソッドまたはプロパティが受け入れる引数の数と異なっています
					throw Error.DispBadParamCount(message);
				case ComHresults.DISP_E_BADVARTYPE:
					// rgvarg の引数の 1 つが有効なバリアント型ではありません。
					break;
				case ComHresults.DISP_E_EXCEPTION:
					// アプリケーションは例外を発生させる必要があります。この場合、pExcepInfo で渡された構造体を格納するべきです。
					throw excepInfo.GetException();
				case ComHresults.DISP_E_MEMBERNOTFOUND:
					// 要求されたメンバが存在しません。または、Invoke への呼び出しで読み取り専用のプロパティに値を設定しようとしました。
					throw Error.DispMemberNotFound(message);
				case ComHresults.DISP_E_NONAMEDARGS:
					// この IDispatch 野実装は名前付き引数をサポートしていません。
					throw Error.DispNoNamedArgs(message);
				case ComHresults.DISP_E_OVERFLOW:
					// rgvarg の引数の 1 つを指定された型に変換できません。
					throw Error.DispOverflow(message);
				case ComHresults.DISP_E_PARAMNOTFOUND:
					// 引数の DISPID の 1 つがメソッドの引数に対応していません。この場合は、puArgErr をエラーを格納している最初の引数に設定する必要があります。
					break;
				case ComHresults.DISP_E_TYPEMISMATCH:
					// 1 つ以上の引数をキャストできませんでした。正しくない型である最初の引数の rgvarg 内のインデックスが puArgErr 引数で返されます。
					throw Error.DispTypeMismatch(argErr, message);
				case ComHresults.DISP_E_UNKNOWNINTERFACE:
					// riid に渡されたインターフェイス識別子が IID_NULL ではありません。
					break;
				case ComHresults.DISP_E_UNKNOWNLCID:
					// 呼び出されたメンバは LCID によって文字列引数を解析しますが、その LCID を認識できません。
					break;
				case ComHresults.DISP_E_PARAMNOTOPTIONAL:
					// 必須の引数が省略されました。
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

		/// <summary>IDispatch.GetTypeInfo を使用して、TypeInfo を検索します。</summary>
		/// <param name="dispatch"></param>
		/// <param name="throwIfMissingExpectedTypeInfo">TypeInfo が存在しない場合に例外をスローするかどうかを示す値を指定します。</param>
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
			{ // IntPtr.Zero を返すコンポーネントに対する防御策
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
		/// このメソッドは TypeInfo がオブジェクトで利用できない場合に呼ばれます。
		/// これは TypeInfo が存在しないことを受け入れられるかどうかを判断します。
		/// これは同じエラーがいつでも、すべてのマシンで、どのような状況でも発生することが保証される場合も含めることができます。
		/// そのような場合、TypeInfo なしで操作する必要が生じます。
		/// しかし、一時的な方法で TypeInfo の呼び出しが失敗する場合、本当に予想通りに問題を表すために例外をスローしたいと考えるかもしれません。
		/// </summary>
		static void CheckIfMissingTypeInfoIsExpected(int hresult, bool throwIfMissingExpectedTypeInfo)
		{
			Debug.Assert(!ComHresults.IsSuccess(hresult));
			// Word.Basic は IDispatch.GetTypeInfo の正しくない実装により常にこれを返します。
			// E_NOINTERFACE を返すあらゆる実装はすべての環境でそうなるでしょう
			if (hresult == ComHresults.E_NOINTERFACE)
				return;
			// COM コンポーネントは非常に予期しない方法で振る舞うので、この表明は潜在的に制約超越的です。
			// しかしながら、共通の予期されるケースを表明することは予期しないシナリオを発見することを確実にして、このコードにバグがないことを確認してシナリオを検証できます。
			Debug.Assert(hresult == ComHresults.TYPE_E_LIBNOTREGISTERED);
			if (throwIfMissingExpectedTypeInfo)
				Marshal.ThrowExceptionForHR(hresult);
		}

		[SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
		internal static ComTypes.TYPEATTR GetTypeAttrForTypeInfo(ComTypes.ITypeInfo typeInfo)
		{
			var pAttrs = IntPtr.Zero;
			typeInfo.GetTypeAttr(out pAttrs);
			// GetTypeAttr は null を返しませんが、これは安全のためです。
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
			// GetTypeAttr は null を返しませんが、これは安全のためです。
			if (pAttrs == IntPtr.Zero)
				throw Error.CannotRetrieveTypeInformation();
			try { return (ComTypes.TYPELIBATTR)Marshal.PtrToStructure(pAttrs, typeof(ComTypes.TYPELIBATTR)); }
			finally { typeLib.ReleaseTLibAttr(pAttrs); }
		}

		public static BoundDispEvent CreateComEvent(object rcw, Guid sourceIid, int dispid) { return new BoundDispEvent(rcw, sourceIid, dispid); }

		public static DispCallable CreateDispCallable(IDispatchComObject dispatch, ComMethodDesc method) { return new DispCallable(dispatch, method.Name, method.DispId); }
	}

	/// <summary>
	/// このクラスは C# で表現できない、またはアンセーフ コードの書き込みを要求するメソッドを格納します。
	/// これらのメソッドの正しくない使用は GC ホールや他の問題を引き起こす恐れがあるため、呼び出し元は極めて慎重に使用する必要があります。
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
				// Word を格納する結果引数を渡さずに再呼び出し
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
		/// "value" がいくつかの呼び出し元のフレームでローカル変数であることを確認します。
		/// そのため、byref から IntPtr への変換は安全な操作です。
		/// 代わりに、許可された "value" をピンされたオブジェクトにすることを許可します。
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

		// 指定されたインターフェイス ポインタの仮想関数テーブルからのアンマネージ関数ポインタの間接的な呼び出しを発行します。
		// このアプローチは 900 命令以内の Marshal.Release と比べると x86 で 300 命令までならばとることができます。
		// JIT コンパイラが pinvoke スタブインライン化と pinvoke ターゲットの直接呼び出しを行うことを当てにしています。
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

		// 指定された IDispatch インターフェイス ポインタの仮想関数テーブルからのアンマネージ関数ポインタの間接的な呼び出しを発行します。
		// これを C# で表現することはできません。
		// 間接的な PInvoke 呼び出しによってカスタム マーシャリングが可能になります。
		// Variant 引数を簡単にスタックに確保できます。
		// JIT コンパイラが pinvoke スタブインライン化と pinvoke ターゲットの直接呼び出しを行うことを当てにしています。
		// IDispatch のマネージ定義を通して呼び出す代案にはスタック上の引数の再プッシュをしなければならない CLR スタブを実行するなどパフォーマンス上の問題があります。
		// Marshal.GetDelegateForFunctionPointer はここで使用できますが、とても高いコストになります。(x86 で 2000 命令)
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
			// 検証をスキップするために、動的メソッドをこのアセンブリに定義する
            var dm = new DynamicMethod("IDispatchInvoke", typeof(int), paramTypes, DynamicModule);
            var method = dm.GetILGenerator();
            // return functionPtr(...)
            EmitLoadArg(method, dispatchPointerIndex);
            EmitLoadArg(method, memberDispIdIndex);
			// 空の IID のアドレスを直接発行
			// これは解放されることも再確保されることもない
			// これを直接 Guid を発行するようにすると、IDispatch 呼び出しに 30% のパフォーマンスヒットがあるので、直接 IntPtr を渡す
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