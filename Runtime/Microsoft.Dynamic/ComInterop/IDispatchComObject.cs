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
	/// <summary>IDispatch ����������I�u�W�F�N�g��\���܂��B</summary>
	/// <remarks>
	/// ���݈ȉ��̂悤�Ȗ�肪����܂�:
	/// 1. IDispatchComObject �ł͂Ȃ� ComObjectWithTypeInfo ��I�񂾏ꍇ�A
	///    �����̏ꍇ IDispatch �̎������o�^����Ă���^�C�v���C�u�����Ɉˑ����Ă��邽�߁AIDispatchComObject �����܂�g�p�ł��܂���B
	///    ComObjectWithTypeInfo �ł͂Ȃ� IDispatchComObject ��I�񂾏ꍇ�A���[�U�[�͗��z�I�ł͂Ȃ��̌��𓾂邱�ƂɂȂ�܂��B
	/// 2. IDispatch �� 0 �̈��������� (����� 1 �ȏ�̊���̈���������?) ���\�b�h�ƃv���p�e�B�����ʂł��܂���B
	///    ���̂��߁Aobj.foo() �� foo �Ƃ������\�b�h���Ăяo���Ă���Ƃ��A�v���p�e�B foo ����Ԃ��ꂽ�֐��|�C���^���Ăяo���Ă���Ƃ��Ƃ��̂ŁA�����܂��ł��B
	///    IDispatch �Ɋ֘A�t����ꂽ ITypeInfo �����āA���\�b�h�܂��̓v���p�e�B�̂ǂ�����Ăяo���ׂ����𒲂ׂ悤�Ƃ��Ă��܂��B
	///    ITypeInfo �̓��\�b�h�����̈�����\������̂��A���\�b�h�܂��̓v���p�e�B���A�I�u�W�F�N�g�̊���̃v���p�e�B�͂ǂꂩ�A�R���N�V�����̗񋓎q�̍쐬���@�Ȃǂ�m���Ă��܂��B
	/// 3. IronPython �̓V�O�l�`�����������Aref ������߂�l�ɕϊ����Ă��܂��B
	///    �������ADispMethod �̃V�O�l�`���͑O�����ė��p�ł��Ȃ��̂ŁA���̕ϊ��͕s�\�ł��B
	///    �e�������邩������Ȃ����̃V�O�l�`���ϊ������邩������܂���B
	///    VB6 �� ref ������ IDispatch ���ǂ̂悤�Ɉ����Ă����̂ł��傤��?
	///    
	/// ����� IDispatch �I�u�W�F�N�g�ɑ΂���C�x���g���T�|�[�g���Ă��܂��B
	/// �w�i:
	/// COM �I�u�W�F�N�g�̓R�l�N�V���� �|�C���g�Ƃ��Ēm���郁�J�j�Y����ʂ��ăC�x���g���T�|�[�g���Ă��܂��B
	/// �R�l�N�V���� �|�C���g�͍쐬���ꂽ�I�u�W�F�N�g�����ۂ� COM �I�u�W�F�N�g���番�����܂��B(����̓C�x���g �V���N�ƃC�x���g �\�[�X�̏z�Q�Ƃ�h�����߂ł��B)
	/// �N���C�A���g�� COM �I�u�W�F�N�g�ɂ���Đ������ꂽ�C�x���g���w�ǂ������ꍇ�A(�\�[�X �C���^�[�t�F�C�X�Ƃ��Ă��m����) �R�[���o�b�N �C���^�[�t�F�C�X���������A������R�l�N�V���� �|�C���g�ɓn���܂� (Advise)�B
	/// 
	/// �����̏ڍ�:
	/// IDisaptchComObject.TryGetMember �v������M���ꂽ�ꍇ�A�܂��v�����ꂽ�����o���v���p�e�B�A���\�b�h�̂ǂ��炩���m�F���܂��B
	/// ���̊m�F�����s�����ꍇ�A�C�x���g���v�����ꂽ���ǂ����̔��f�����݂܂��B
	/// ������s�����߂ɁA�ȉ��̎菇�����s���܂�:
	/// 1. COM �I�u�W�F�N�g�� IConnectionPointContainer ���������Ă��邩�𒲂ׂ܂�
	/// 2. COM �I�u�W�F�N�g�̃R�N���X�L�q�̎擾�����݂܂�
	///    a. �I�u�W�F�N�g�� IProvideClassInfo �C���^�[�t�F�C�X��v�����܂��B���������ꍇ�� 3 �ɐi�݂܂��B
	///    b. �I�u�W�F�N�g�� IDispatch ����v���C�}�� �C���^�[�t�F�C�X�L�q���擾���܂�
	///    c. �I�u�W�F�N�g�̃^�C�v���C�u�����Ő錾����Ă���R�N���X���X�L�������܂�
	///    d. ���ɂ��̃v���C�}�� �C���^�[�t�F�C�X���������Ă���R�N���X�������܂�
	/// 3. �R�N���X���X�L�������Ă��ׂẴ\�[�X�C���^�[�t�F�C�X�����߂܂�
	/// 4. �\�[�X�C���^�[�t�F�C�X�ŔC�ӂ̃��\�b�h���v�����ꂽ���O�Ɉ�v���邩�ǂ����𔻒f���܂�
	/// 
	/// �������� TryGetMember ���C�x���g��v������Ɣ��f����΁ABoundDispEvent �N���X�̃C���X�^���X��Ԃ��܂��B
	/// ���̃N���X�� InPlaceAdd �� InPlaceSubtract ���Z�q����`����Ă��܂��B
	/// InPlaceAdd ���Z�q���Ăяo���Ǝ��̂悤�ɂȂ�܂�:
	/// 1. ComEventSinksContainer �N���X�̃C���X�^���X���쐬����܂� (RCW �����łɃC���X�^���X��ێ����Ă��Ȃ��ꍇ)
	///    ���̃C���X�^���X�̓C�x���g �V���N�̐������Ԃ� RCW ���g�̐������ԂɊ֘A�t���悤�Ǝ��݂āARCW ����n���O����܂��B
	///    ����͂������� RCW �����W�����΁A�C�x���g �V���N�����W����邱�Ƃ��Ӗ����܂��B(����̓C�x���g �V���N�̐������Ԃ� PIA �ɂ���Đ��䂳���̂Ɠ������@�ł��B)
	///    �ʍ�: ComEventSinksContainer �͂��ׂẴC�x���g �V���N�� Unadvise ����t�@�C�i���C�U���܂�ł��܂��B
	///    �ʍ�: ComEventSinksContainer �� ComEventSink �I�u�W�F�N�g�̃��X�g�ł��B
	/// 2. �v�����ꂽ�\�[�X �C���^�[�t�F�C�X�ɑ΂��� ComEventSink ���쐬����Ă��Ȃ��ꍇ�AComEventSink ���쐬�� Advise ���܂��B
	///    ���ꂼ��� ComEventSink �� COM �I�u�W�F�N�g���T�|�[�g����P��̃\�[�X �C���^�[�t�F�C�X���������Ă��܂��B
	/// 3. ComEventSink �̓��\�b�h�� DISPID ����C�x���g�����������ۂɌĂяo�����}���`�L���X�g �f���Q�[�g�ւ̃}�b�s���O���܂�ł��܂��B
	/// 4. ComEventSink �� COM ���p�҂ɑ΂���J�X�^�� IDispatch �Ƃ��Č��J����� IReflect �C���^�[�t�F�C�X���������Ă��܂��B
	///    ����ɂ���āAIDispatch.Invoke �ւ̌Ăяo��������肵�āA�J�X�^�����W�b�N��K�p���邱�Ƃ��ł���悤�ɂȂ�܂��B
	///    ���ɁA�Ăяo���ꂽ DISPID �ɑΉ�����}���`�L���X�g �f���Q�[�g�������ČĂяo���Ƃ������ƂȂǁB
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
			// �^��񂪂Ȃ���΁A�v���p�e�B get ���\�b�h�������Ă��邩�ǂ����͖{���ɕ�����Ȃ�
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
			// �^��񂪂Ȃ���΁A�v���p�e�B set ���\�b�h�������Ă��邩�ǂ����͖{���ɕ�����Ȃ�
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
				// ������ put �܂��� putref �������Ă��邩�ǂ����͕�����Ȃ��̂ŁA�����͂ł�������������
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
						catch (Exception ex) { members.Add(new KeyValuePair<string, object>(method.Name, ex)); } // �����̗��R�ŕ]�������s�����̂ŗ�O���O�ɓn���B
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
			// GetFuncDesc �� null �������ĕԂ��Ȃ����A���S�̂���
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
						return ComTypeDesc.EmptyEvents; // IConnectionPointContainer �͂Ȃ��B�܂�A���̃I�u�W�F�N�g�̓C�x���g���T�|�[�g���Ă��Ȃ��B
					if ((classTypeInfo = GetCoClassTypeInfo(this.RuntimeCallableWrapper, typeInfo)) == null)
						return ComTypeDesc.EmptyEvents; // �N���X��񂪌�����Ȃ��B���̃I�u�W�F�N�g�̓C�x���g���T�|�[�g���Ă��邩������Ȃ����A�����邱�Ƃ͂ł��Ȃ��B
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
					// �B����Ă����萧��̂������肷��֐��͍��̂Ƃ��닻���͂Ȃ�
					if ((funcDesc.wFuncFlags & (int)ComTypes.FUNCFLAGS.FUNCFLAG_FHIDDEN) != 0 || (funcDesc.wFuncFlags & (int)ComTypes.FUNCFLAGS.FUNCFLAG_FRESTRICTED) != 0)
						continue;
					var name = ComRuntimeHelpers.GetNameOfMethod(sourceTypeInfo, funcDesc.memid).ToUpper(System.Globalization.CultureInfo.InvariantCulture);
					// �Ƃ��ɃR�N���X�͕����̃\�[�X �C���^�[�t�F�C�X�������Ƃ�����B
					// �ʏ킱��͐V�����C�x���g��ǉ����āA�Â��C���^�[�t�F�C�X���c�����߂ɁA�C�x���g��V�����C���^�[�t�F�C�X�ɒu�����Ƃ��ɔ�������B
					// �������O�̍ŏ��̃C�x���g�����c���Ȃ��ŉ������Ă���̂ŁA���O�Փ˂̉\��������
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
			// IProvideClassInfo ��ʂ����N���X�̎擾�����s�����B�R�N���X�������邽�߂Ƀ^�C�v���C�u�����̃X�L���������݂�
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
								continue; // ���̊֐��̓X�N���v�g���[�U�[���g�p���邱�Ƃ��Ӑ}���Ă��Ȃ�
							var method = new ComMethodDesc(typeInfo, funcDesc);
							var name = method.Name.ToUpper(System.Globalization.CultureInfo.InvariantCulture);
							if ((funcDesc.invkind & ComTypes.INVOKEKIND.INVOKE_PROPERTYPUT) != 0)
							{
								puts.Add(name, method);
								// dispId == 0 �͓��ʂŁADo(SetItem) �o�C���_�[�ɑ΂��郁�\�b�h�L�q�q���i�[����K�v������B
								if (method.DispId == ComDispIds.DISPID_VALUE && setItem == null)
									setItem = method;
								continue;
							}
							if ((funcDesc.invkind & ComTypes.INVOKEKIND.INVOKE_PROPERTYPUTREF) != 0)
							{
								putrefs.Add(name, method);
								// dispId == 0 �͓��ʂŁADo(SetItem) �o�C���_�[�ɑ΂��郁�\�b�h�L�q�q���i�[����K�v������B
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
							// dispId == 0 �͓��ʂŁADo(GetItem) �o�C���_�[�ɑ΂��郁�\�b�h�L�q�q���i�[����K�v������B
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