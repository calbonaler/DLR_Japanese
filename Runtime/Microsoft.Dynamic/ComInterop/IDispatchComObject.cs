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
using System.Dynamic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace Microsoft.Scripting.ComInterop
{
	/// <summary>IDispatch を実装するオブジェクトを表します。</summary>
	/// <remarks>
	/// 現在以下のような問題があります:
	/// 1. IDispatchComObject ではなく ComObjectWithTypeInfo を選んだ場合、
	///    多くの場合 IDispatch の実装が登録されているタイプライブラリに依存しているため、IDispatchComObject をあまり使用できません。
	///    ComObjectWithTypeInfo ではなく IDispatchComObject を選んだ場合、ユーザーは理想的ではない体験を得ることになります。
	/// 2. IDispatch は 0 個の引数をもつ (さらに 1 つ以上の既定の引数をもつ?) メソッドとプロパティを識別できません。
	///    そのため、obj.foo() は foo というメソッドを呼び出しているとも、プロパティ foo から返された関数ポインタを呼び出しているともとれるので、あいまいです。
	///    IDispatch に関連付けられた ITypeInfo を見て、メソッドまたはプロパティのどちらを呼び出すべきかを調べようとしています。
	///    ITypeInfo はメソッドが何の引数を予期するのか、メソッドまたはプロパティか、オブジェクトの既定のプロパティはどれか、コレクションの列挙子の作成方法などを知っています。
	/// 3. IronPython はシグネチャを処理し、ref 引数を戻り値に変換しています。
	///    しかし、DispMethod のシグネチャは前もって利用できないので、この変換は不可能です。
	///    影響があるかもしれない他のシグネチャ変換があるかもしれません。
	///    VB6 は ref 引数と IDispatch をどのように扱っていたのでしょうか?
	///    
	/// さらに IDispatch オブジェクトに対するイベントもサポートしています。
	/// 背景:
	/// COM オブジェクトはコネクション ポイントとして知られるメカニズムを通してイベントをサポートしています。
	/// コネクション ポイントは作成されたオブジェクトを実際の COM オブジェクトから分離します。(これはイベント シンクとイベント ソースの循環参照を防ぐためです。)
	/// クライアントが COM オブジェクトによって生成されたイベントを購読したい場合、(ソース インターフェイスとしても知られる) コールバック インターフェイスを実装し、それをコネクション ポイントに渡します (Advise)。
	/// 
	/// 実装の詳細:
	/// IDisaptchComObject.TryGetMember 要求が受信された場合、まず要求されたメンバがプロパティ、メソッドのどちらかを確認します。
	/// この確認が失敗した場合、イベントが要求されたかどうかの判断を試みます。
	/// これを行うために、以下の手順を実行します:
	/// 1. COM オブジェクトが IConnectionPointContainer を実装しているかを調べます
	/// 2. COM オブジェクトのコクラス記述の取得を試みます
	///    a. オブジェクトに IProvideClassInfo インターフェイスを要求します。見つかった場合は 3 に進みます。
	///    b. オブジェクトの IDispatch からプライマリ インターフェイス記述を取得します
	///    c. オブジェクトのタイプライブラリで宣言されているコクラスをスキャンします
	///    d. 特にこのプライマリ インターフェイスを実装しているコクラスを見つけます
	/// 3. コクラスをスキャンしてすべてのソースインターフェイスを求めます
	/// 4. ソースインターフェイスで任意のメソッドが要求された名前に一致するかどうかを判断します
	/// 
	/// いったん TryGetMember がイベントを要求すると判断すれば、BoundDispEvent クラスのインスタンスを返します。
	/// このクラスは InPlaceAdd と InPlaceSubtract 演算子が定義されています。
	/// InPlaceAdd 演算子を呼び出すと次のようになります:
	/// 1. ComEventSinksContainer クラスのインスタンスが作成されます (RCW がすでにインスタンスを保持していない場合)
	///    このインスタンスはイベント シンクの生存期間を RCW 自身の生存期間に関連付けようと試みて、RCW からハングされます。
	///    これはいったん RCW が収集されれば、イベント シンクも収集されることを意味します。(これはイベント シンクの生存期間が PIA によって制御されるのと同じ方法です。)
	///    通告: ComEventSinksContainer はすべてのイベント シンクを Unadvise するファイナライザを含んでいます。
	///    通告: ComEventSinksContainer は ComEventSink オブジェクトのリストです。
	/// 2. 要求されたソース インターフェイスに対して ComEventSink が作成されていない場合、ComEventSink を作成し Advise します。
	///    それぞれの ComEventSink は COM オブジェクトがサポートする単一のソース インターフェイスを実装しています。
	/// 3. ComEventSink はメソッドの DISPID からイベントが発生した際に呼び出されるマルチキャスト デリゲートへのマッピングを含んでいます。
	/// 4. ComEventSink は COM 利用者に対するカスタム IDispatch として公開される IReflect インターフェイスを実装しています。
	///    これによって、IDispatch.Invoke への呼び出しを横取りして、カスタムロジックを適用することができるようになります。
	///    特に、呼び出された DISPID に対応するマルチキャスト デリゲートを見つけて呼び出すということなど。
	/// </remarks>
	sealed class IDispatchComObject : ComObject, IDynamicMetaObjectProvider
	{
		ComTypeDesc _comTypeDesc;
		static readonly Dictionary<Guid, ComTypeDesc> _CacheComTypeDesc = new Dictionary<Guid, ComTypeDesc>();

		internal IDispatchComObject(IDispatch rcw) : base(rcw) { DispatchObject = rcw; }

		public override string ToString()
		{
			var ctd = _comTypeDesc;
			string typeName = null;
			if (ctd != null)
				typeName = ctd.TypeName;
			if (string.IsNullOrEmpty(typeName))
				typeName = "IDispatch";
			return string.Format(CultureInfo.CurrentCulture, "{0} ({1})", RuntimeCallableWrapper.ToString(), typeName);
		}

		public ComTypeDesc ComTypeDesc
		{
			get
			{
				EnsureScanDefinedMethods();
				return _comTypeDesc;
			}
		}

		public IDispatch DispatchObject { get; private set; }

		static int GetIDsOfNames(IDispatch dispatch, string name, out int dispId)
		{
			int[] dispIds = new int[1];
			var emtpyRiid = Guid.Empty;
			var hresult = dispatch.TryGetIDsOfNames(ref emtpyRiid, new string[] { name }, 1, 0, dispIds);
			dispId = dispIds[0];
			return hresult;
		}

		static int Invoke(IDispatch dispatch, int memberDispId, out object result)
		{
			var emtpyRiid = Guid.Empty;
			ComTypes.DISPPARAMS dispParams = new ComTypes.DISPPARAMS();
			ComTypes.EXCEPINFO excepInfo = new ComTypes.EXCEPINFO();
			uint argErr;
			return dispatch.TryInvoke(memberDispId, ref emtpyRiid, 0, ComTypes.INVOKEKIND.INVOKE_PROPERTYGET, ref dispParams, out result, out excepInfo, out argErr);
		}

		internal bool TryGetGetItem(out ComMethodDesc value)
		{
			if (_comTypeDesc.GetItem != null)
			{
				value = _comTypeDesc.GetItem;
				return true;
			}
			return SlowTryGetGetItem(out value);
		}

		bool SlowTryGetGetItem(out ComMethodDesc value)
		{
			EnsureScanDefinedMethods();
			// 型情報がなければ、プロパティ get メソッドを持っているかどうかは本当に分からない
			if (_comTypeDesc.GetItem == null)
				_comTypeDesc.EnsureGetItem(new ComMethodDesc("[PROPERTYGET, DISPID(0)]", ComDispIds.DISPID_VALUE, ComTypes.INVOKEKIND.INVOKE_PROPERTYGET));
			value = _comTypeDesc.GetItem;
			return true;
		}

		internal bool TryGetSetItem(out ComMethodDesc value)
		{
			if (_comTypeDesc.SetItem != null)
			{
				value = _comTypeDesc.SetItem;
				return true;
			}
			return SlowTryGetSetItem(out value);
		}

		bool SlowTryGetSetItem(out ComMethodDesc value)
		{
			EnsureScanDefinedMethods();
			// 型情報がなければ、プロパティ set メソッドを持っているかどうかは本当に分からない
			if (_comTypeDesc.SetItem == null)
				_comTypeDesc.EnsureSetItem(new ComMethodDesc("[PROPERTYPUT, DISPID(0)]", ComDispIds.DISPID_VALUE, ComTypes.INVOKEKIND.INVOKE_PROPERTYPUT));
			value = _comTypeDesc.SetItem;
			return true;
		}

		internal bool TryGetMemberMethod(string name, out ComMethodDesc method)
		{
			EnsureScanDefinedMethods();
			return _comTypeDesc.TryGetFunc(name, out method);
		}

		internal bool TryGetMemberEvent(string name, out ComEventDesc @event)
		{
			EnsureScanDefinedEvents();
			return _comTypeDesc.TryGetEvent(name, out @event);
		}

		internal bool TryGetMemberMethodExplicit(string name, out ComMethodDesc method)
		{
			EnsureScanDefinedMethods();
			int dispId;
			int hresult = GetIDsOfNames(DispatchObject, name, out dispId);
			if (hresult == ComHresults.S_OK)
			{
				_comTypeDesc.AddFunc(name, method = new ComMethodDesc(name, dispId, ComTypes.INVOKEKIND.INVOKE_FUNC));
				return true;
			}
			else if (hresult == ComHresults.DISP_E_UNKNOWNNAME)
			{
				method = null;
				return false;
			}
			else
				throw Error.CouldNotGetDispId(name, string.Format(CultureInfo.InvariantCulture, "0x{1:X})", hresult));
		}

		internal bool TryGetPropertySetterExplicit(string name, out ComMethodDesc method, Type limitType, bool holdsNull)
		{
			EnsureScanDefinedMethods();
			int dispId;
			int hresult = GetIDsOfNames(DispatchObject, name, out dispId);
			if (hresult == ComHresults.S_OK)
			{
				// ここで put または putref を持っているかどうかは分からないので、推測はできず両方見つける
				var put = new ComMethodDesc(name, dispId, ComTypes.INVOKEKIND.INVOKE_PROPERTYPUT);
				_comTypeDesc.AddPut(name, put);
				var putref = new ComMethodDesc(name, dispId, ComTypes.INVOKEKIND.INVOKE_PROPERTYPUTREF);
				_comTypeDesc.AddPutRef(name, putref);
				if (ComBinderHelpers.PreferPut(limitType, holdsNull))
					method = put;
				else
					method = putref;
				return true;
			}
			else if (hresult == ComHresults.DISP_E_UNKNOWNNAME)
			{
				method = null;
				return false;
			}
			else
				throw Error.CouldNotGetDispId(name, string.Format(CultureInfo.InvariantCulture, "0x{1:X})", hresult));
		}

		internal override IList<string> GetMemberNames(bool dataOnly)
		{
			EnsureScanDefinedMethods();
			EnsureScanDefinedEvents();
			return ComTypeDesc.GetMemberNames(dataOnly);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal override IList<KeyValuePair<string, object>> GetMembers(IEnumerable<string> names)
		{
			if (names == null)
				names = GetMemberNames(true);
			var comType = RuntimeCallableWrapper.GetType();
			var members = new List<KeyValuePair<string, object>>();
			foreach (string name in names)
			{
				if (name != null)
				{
					ComMethodDesc method;
					if (ComTypeDesc.TryGetFunc(name, out method) && method.IsDataMember)
					{
						try { members.Add(new KeyValuePair<string, object>(method.Name, comType.InvokeMember(method.Name, BindingFlags.GetProperty, null, RuntimeCallableWrapper, new object[0], CultureInfo.InvariantCulture))); }
						catch (Exception ex) { members.Add(new KeyValuePair<string, object>(method.Name, ex)); } // 何かの理由で評価が失敗したので例外を外に渡す。
					}
				}
			}
			return members.ToArray();
		}

		DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
		{
			EnsureScanDefinedMethods();
			return new IDispatchMetaObject(parameter, this);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
		static void GetFuncDescForDescIndex(ComTypes.ITypeInfo typeInfo, int funcIndex, out ComTypes.FUNCDESC funcDesc, out IntPtr funcDescHandle)
		{
			var pFuncDesc = IntPtr.Zero;
			typeInfo.GetFuncDesc(funcIndex, out pFuncDesc);
			// GetFuncDesc は null を決して返さないが、安全のため
			if (pFuncDesc == IntPtr.Zero)
				throw Error.CannotRetrieveTypeInformation();
			funcDesc = (ComTypes.FUNCDESC)Marshal.PtrToStructure(funcDescHandle = pFuncDesc, typeof(ComTypes.FUNCDESC));
		}

		void EnsureScanDefinedEvents()
		{
			TypeDescScanHelper(
				x => x.Events,
				(typeInfo, typeAttr, typeDesc) =>
				{
					ComTypes.ITypeInfo classTypeInfo = null;
					var cpc = RuntimeCallableWrapper as ComTypes.IConnectionPointContainer;
					if (cpc == null)
						return ComTypeDesc.EmptyEvents; // IConnectionPointContainer はない。つまり、このオブジェクトはイベントをサポートしていない。
					if ((classTypeInfo = GetCoClassTypeInfo(this.RuntimeCallableWrapper, typeInfo)) == null)
						return ComTypeDesc.EmptyEvents; // クラス情報が見つからない。このオブジェクトはイベントをサポートしているかもしれないが、見つけることはできない。
					var events = new Dictionary<string, ComEventDesc>();
					var classTypeAttr = ComRuntimeHelpers.GetTypeAttrForTypeInfo(classTypeInfo);
					for (int i = 0; i < classTypeAttr.cImplTypes; i++)
					{
						int hRefType;
						classTypeInfo.GetRefTypeOfImplType(i, out hRefType);
						ComTypes.ITypeInfo interfaceTypeInfo;
						classTypeInfo.GetRefTypeInfo(hRefType, out interfaceTypeInfo);
						ComTypes.IMPLTYPEFLAGS flags;
						classTypeInfo.GetImplTypeFlags(i, out flags);
						if ((flags & ComTypes.IMPLTYPEFLAGS.IMPLTYPEFLAG_FSOURCE) != 0)
							ScanSourceInterface(interfaceTypeInfo, ref events);
					}
					if (events.Count == 0)
						events = ComTypeDesc.EmptyEvents;
					return events;
				},
				(actualDesc, events) => actualDesc.Events = events
			);
		}

		static void ScanSourceInterface(ComTypes.ITypeInfo sourceTypeInfo, ref Dictionary<string, ComEventDesc> events)
		{
			var sourceTypeAttribute = ComRuntimeHelpers.GetTypeAttrForTypeInfo(sourceTypeInfo);
			for (int index = 0; index < sourceTypeAttribute.cFuncs; index++)
			{
				var funcDescHandleToRelease = IntPtr.Zero;
				try
				{
					ComTypes.FUNCDESC funcDesc;
					GetFuncDescForDescIndex(sourceTypeInfo, index, out funcDesc, out funcDescHandleToRelease);
					// 隠されていたり制約のあったりする関数は今のところ興味はない
					if ((funcDesc.wFuncFlags & (int)ComTypes.FUNCFLAGS.FUNCFLAG_FHIDDEN) != 0 || (funcDesc.wFuncFlags & (int)ComTypes.FUNCFLAGS.FUNCFLAG_FRESTRICTED) != 0)
						continue;
					var name = ComRuntimeHelpers.GetNameOfMethod(sourceTypeInfo, funcDesc.memid).ToUpper(System.Globalization.CultureInfo.InvariantCulture);
					// ときにコクラスは複数のソース インターフェイスを持つことがある。
					// 通常これは新しいイベントを追加して、古いインターフェイスを残すために、イベントを新しいインターフェイスに置いたときに発生する。
					// 同じ名前の最初のイベントしか残さないで解決しているので、名前衝突の可能性がある
					if (events.ContainsKey(name) == false)
					{
						ComEventDesc eventDesc = new ComEventDesc();
						eventDesc.dispid = funcDesc.memid;
						eventDesc.sourceIID = sourceTypeAttribute.guid;
						events.Add(name, eventDesc);
					}
				}
				finally
				{
					if (funcDescHandleToRelease != IntPtr.Zero)
						sourceTypeInfo.ReleaseFuncDesc(funcDescHandleToRelease);
				}
			}
		}

		static ComTypes.ITypeInfo GetCoClassTypeInfo(object rcw, ComTypes.ITypeInfo typeInfo)
		{
			Debug.Assert(typeInfo != null);
			var provideClassInfo = rcw as IProvideClassInfo;
			if (provideClassInfo != null)
			{
				var typeInfoPtr = IntPtr.Zero;
				try
				{
					provideClassInfo.GetClassInfo(out typeInfoPtr);
					if (typeInfoPtr != IntPtr.Zero)
						return Marshal.GetObjectForIUnknown(typeInfoPtr) as ComTypes.ITypeInfo;
				}
				finally
				{
					if (typeInfoPtr != IntPtr.Zero)
						Marshal.Release(typeInfoPtr);
				}
			}
			// IProvideClassInfo を通したクラスの取得が失敗した。コクラスを見つけるためにタイプライブラリのスキャンを試みる
			ComTypes.ITypeLib typeLib;
			int typeInfoIndex;
			typeInfo.GetContainingTypeLib(out typeLib, out typeInfoIndex);
			var coclassDesc = ComTypeLibDesc.GetFromTypeLib(typeLib).GetCoClassForInterface(ComRuntimeHelpers.GetNameOfType(typeInfo));
			if (coclassDesc == null)
				return null;
			ComTypes.ITypeInfo typeInfoCoClass;
			Guid coclassGuid = coclassDesc.Guid;
			typeLib.GetTypeInfoOfGuid(ref coclassGuid, out typeInfoCoClass);
			return typeInfoCoClass;
		}

		void EnsureScanDefinedMethods()
		{
			TypeDescScanHelper(
				x => x.Funcs,
				(typeInfo, typeAttr, typeDesc) =>
				{
					ComMethodDesc getItem = null;
					ComMethodDesc setItem = null;
					Dictionary<string, ComMethodDesc> funcs = new Dictionary<string, ComMethodDesc>(typeAttr.cFuncs);
					Dictionary<string, ComMethodDesc> puts = new Dictionary<string, ComMethodDesc>();
					Dictionary<string, ComMethodDesc> putrefs = new Dictionary<string, ComMethodDesc>();
					for (int definedFuncIndex = 0; definedFuncIndex < typeAttr.cFuncs; definedFuncIndex++)
					{
						var funcDescHandleToRelease = IntPtr.Zero;
						try
						{
							ComTypes.FUNCDESC funcDesc;
							GetFuncDescForDescIndex(typeInfo, definedFuncIndex, out funcDesc, out funcDescHandleToRelease);
							if ((funcDesc.wFuncFlags & (int)ComTypes.FUNCFLAGS.FUNCFLAG_FRESTRICTED) != 0)
								continue; // この関数はスクリプトユーザーが使用することを意図していない
							var method = new ComMethodDesc(typeInfo, funcDesc);
							var name = method.Name.ToUpper(System.Globalization.CultureInfo.InvariantCulture);
							if ((funcDesc.invkind & ComTypes.INVOKEKIND.INVOKE_PROPERTYPUT) != 0)
							{
								puts.Add(name, method);
								// dispId == 0 は特別で、Do(SetItem) バインダーに対するメソッド記述子を格納する必要がある。
								if (method.DispId == ComDispIds.DISPID_VALUE && setItem == null)
									setItem = method;
								continue;
							}
							if ((funcDesc.invkind & ComTypes.INVOKEKIND.INVOKE_PROPERTYPUTREF) != 0)
							{
								putrefs.Add(name, method);
								// dispId == 0 は特別で、Do(SetItem) バインダーに対するメソッド記述子を格納する必要がある。
								if (method.DispId == ComDispIds.DISPID_VALUE && setItem == null)
									setItem = method;
								continue;
							}
							if (funcDesc.memid == ComDispIds.DISPID_NEWENUM)
							{
								funcs.Add("GETENUMERATOR", method);
								continue;
							}
							funcs.Add(name, method);
							// dispId == 0 は特別で、Do(GetItem) バインダーに対するメソッド記述子を格納する必要がある。
							if (funcDesc.memid == ComDispIds.DISPID_VALUE)
								getItem = method;
						}
						finally
						{
							if (funcDescHandleToRelease != IntPtr.Zero)
								typeInfo.ReleaseFuncDesc(funcDescHandleToRelease);
						}
					}
					return new { getItem, setItem, funcs, puts, putrefs };
				},
				(actualDesc, res) =>
				{
					actualDesc.Funcs = res.funcs;
					actualDesc.Puts = res.puts;
					actualDesc.PutRefs = res.putrefs;
					actualDesc.EnsureGetItem(res.getItem);
					actualDesc.EnsureSetItem(res.setItem);
				}
			);
		}

		void TypeDescScanHelper<TDesc, TResult>(Func<ComTypeDesc, TDesc> descGetter, Func<ComTypes.ITypeInfo, ComTypes.TYPEATTR, ComTypeDesc, TResult> body, Action<ComTypeDesc, TResult> @finally) where TDesc : class
		{
			if (_comTypeDesc != null && descGetter(_comTypeDesc) != null)
				return;

			var typeInfo = ComRuntimeHelpers.GetITypeInfoFromIDispatch(DispatchObject, true);
			if (typeInfo == null)
			{
				_comTypeDesc = ComTypeDesc.CreateEmptyTypeDesc();
				return;
			}
			var typeAttr = ComRuntimeHelpers.GetTypeAttrForTypeInfo(typeInfo);
			if (_comTypeDesc == null)
			{
				lock (_CacheComTypeDesc)
				{
					if (_CacheComTypeDesc.TryGetValue(typeAttr.guid, out _comTypeDesc) && descGetter(_comTypeDesc) != null)
						return;
				}
			}
			var typeDesc = ComTypeDesc.FromITypeInfo(typeInfo, typeAttr);

			var result = body(typeInfo, typeAttr, typeDesc);

			lock (_CacheComTypeDesc)
			{
				ComTypeDesc cachedTypeDesc;
				if (_CacheComTypeDesc.TryGetValue(typeAttr.guid, out cachedTypeDesc))
					_comTypeDesc = cachedTypeDesc;
				else
				{
					_comTypeDesc = typeDesc;
					_CacheComTypeDesc.Add(typeAttr.guid, _comTypeDesc);
				}
				@finally(_comTypeDesc, result);
			}
		}

		internal bool TryGetPropertySetter(string name, out ComMethodDesc method, Type limitType, bool holdsNull)
		{
			EnsureScanDefinedMethods();
			if (ComBinderHelpers.PreferPut(limitType, holdsNull))
				return _comTypeDesc.TryGetPut(name, out method) || _comTypeDesc.TryGetPutRef(name, out method);
			else
				return _comTypeDesc.TryGetPutRef(name, out method) || _comTypeDesc.TryGetPut(name, out method);
		}
	}
}