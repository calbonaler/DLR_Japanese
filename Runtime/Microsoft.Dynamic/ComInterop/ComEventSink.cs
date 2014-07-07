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
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace Microsoft.Scripting.ComInterop
{
	/// <summary>
	/// ����� RCW �ɑ΂���C�x���g �V���N���������܂��B
	/// TlbImp'd �̃A�Z���u���̃C�x���g�����ƈقȂ�A���̃N���X�͊e RCW �ɑ΂��� 1 �����C�x���g �V���N���쐬���܂���B
	/// (���_�� RCW �ɂ͕����� <see cref="ComEventSink"/> �����݂ł��܂��B�����������͂��ׂĎ������Ă���C���^�[�t�F�C�X���قȂ�܂��B)
	/// </summary>
	/// <remarks>
	/// ���ꂼ��� <see cref="ComEventSink"/> �� <see cref="ComEventSinkMethod"/> �I�u�W�F�N�g�̃��X�g��ێ����܂��B
	/// ���� <see cref="ComEventSinkMethod"/> �̓\�[�X �C���^�[�t�F�C�X��̒P��̃��\�b�h��\���A�Ăяo����]������}���`�L���X�g �f���Q�[�g��ێ����܂��B
	/// �ʍ�: ���� <see cref="ComEventSinkMethod"/> �������̃C�x���g �n���h�����Ăяo����悤�ɁA�}���`�L���X�g �f���Q�[�g���`�F�C�����܂��B
	/// <see cref="ComEventSink"/> �̓R�l�N�V�����|�C���g���� Unadvise ���邽�߂ɁA<see cref="IDisposable"/> ���������Ă��܂��B
	/// �ʏ�ARCW ���t�@�C�i���C�Y�����ƁA�Ή����� <see cref="IDisposable.Dispose"/> �� <see cref="ComEventSinksContainer"/> �̃t�@�C�i���C�U�ɂ���ăg���K�[����܂��B
	/// �ʍ�: <see cref="ComEventSinksContainer"/> �̐������Ԃ� RCW �̐������Ԃɑ�������܂��B
	/// </remarks>
	sealed class ComEventSink : MarshalByRefObject, IReflect, IDisposable
	{
		Guid _sourceIid;
		ComTypes.IConnectionPoint _connectionPoint;
		int _adviseCookie;
		List<ComEventSinkMethod> _comEventSinkMethods;
		object _lockObject = new object(); // DoNotLockOnObjectsWithWeakIdentity �x�����������邽�߁AComEventSink �̓��b�N�ł��܂���B

		/// <summary>���\�b�h�� ("[DISPID=N]" �̌`�ŕ�����Ƀt�H�[�}�b�g���ꂽ) DISPID�ƌĂяo���f���Q�[�g�̃��X�g���i�[���܂��B</summary>
		class ComEventSinkMethod
		{
			public string _name;
			public Func<object[], object> _handlers;
		}

		ComEventSink(object rcw, Guid sourceIid) { Initialize(rcw, sourceIid); }

		void Initialize(object rcw, Guid sourceIid)
		{
			_sourceIid = sourceIid;
			_adviseCookie = -1;
			Debug.Assert(_connectionPoint == null, "re-initializing event sink w/o unadvising from connection point");
			var cpc = rcw as ComTypes.IConnectionPointContainer;
			if (cpc == null)
				throw Error.COMObjectDoesNotSupportEvents();
			cpc.FindConnectionPoint(ref _sourceIid, out _connectionPoint);
			if (_connectionPoint == null)
				throw Error.COMObjectDoesNotSupportSourceInterface();
			// �Ȃ���������K�v������̂��ɂ��Ă� ComEventSinkProxy �̃R�����g��ǂ�ł��������B
			_connectionPoint.Advise(new ComEventSinkProxy(this, _sourceIid).GetTransparentProxy(), out _adviseCookie);
		}

		public static ComEventSink FromRuntimeCallableWrapper(object rcw, Guid sourceIid, bool createIfNotFound)
		{
			var comEventSinks = ComEventSinksContainer.FromRuntimeCallableWrapper(rcw, createIfNotFound);
			if (comEventSinks == null)
				return null;
			ComEventSink comEventSink = null;
			lock (comEventSinks)
			{
				foreach (var sink in comEventSinks)
				{
					if (sink._sourceIid == sourceIid)
					{
						comEventSink = sink;
						break;
					}
					else if (sink._sourceIid == Guid.Empty)
					{
						// �ȑO�ɔj�����ꂽ ComEventSink �I�u�W�F�N�g�����������̂ŁA�ė��p����B
						sink.Initialize(rcw, sourceIid);
						comEventSink = sink;
					}
				}
				if (comEventSink == null && createIfNotFound == true)
					comEventSinks.Add(comEventSink = new ComEventSink(rcw, sourceIid));
			}
			return comEventSink;
		}

		public void AddHandler(int dispid, object func)
		{
			var name = string.Format(CultureInfo.InvariantCulture, "[DISPID={0}]", dispid);
			lock (_lockObject)
			{
				var sinkMethod = FindSinkMethod(name);
				if (sinkMethod == null)
					(_comEventSinkMethods ?? (_comEventSinkMethods = new List<ComEventSinkMethod>())).Add(sinkMethod = new ComEventSinkMethod() { _name = name });
				sinkMethod._handlers += new SplatCallSite(func).Invoke;
			}
		}

		public void RemoveHandler(int dispid, object func)
		{
			var name = string.Format(CultureInfo.InvariantCulture, "[DISPID={0}]", dispid);
			lock (_lockObject)
			{
				var sinkEntry = FindSinkMethod(name);
				if (sinkEntry == null)
					return;
				// �f���Q�[�g���}���`�L���X�g�f���Q�[�g�`�F�C�������菜��
				// �폜�������n���h���ɑΉ�����f���Q�[�g��������K�v������܂��B
				// �f���Q�[�g�I�u�W�F�N�g�� Target �v���p�e�B�� ComEventCallContext �I�u�W�F�N�g�Ȃ̂ŁA����͊ȒP�ł��B
				foreach (var d in sinkEntry._handlers.GetInvocationList())
				{
					var callContext = d.Target as SplatCallSite;
					if (callContext != null && callContext._callable.Equals(func))
					{
						sinkEntry._handlers -= d as Func<object[], object>;
						break;
					}
				}
				// �f���Q�[�g�`�F�C������Ȃ�΁A�Ή����� ComEventSinkMethod ���폜
				if (sinkEntry._handlers == null)
					_comEventSinkMethods.Remove(sinkEntry);
				// �C���^�[�t�F�C�X�ɗv�f�����݂��Ȃ���΁A�R�l�N�V�����|�C���g���� Unadvise (Dispose �Ăяo���� IConnectionPoint.Unadvise ���Ăяo���܂�)
				if (_comEventSinkMethods.Count == 0)
					Dispose(); // �V�����n���h�����A�^�b�`���ꂽ�ꍇ�f�[�^�\�����ė��p����̂ŁA���X�g����͍폜���Ȃ�
			}
		}

		public object ExecuteHandler(string name, object[] args)
		{
			var site = FindSinkMethod(name);
			return site != null && site._handlers != null ? site._handlers(args) : null;
		}

		#region Unimplemented members

		public FieldInfo GetField(string name, BindingFlags bindingAttr) { return null; }

		public FieldInfo[] GetFields(BindingFlags bindingAttr) { return new FieldInfo[0]; }

		public MemberInfo[] GetMember(string name, BindingFlags bindingAttr) { return new MemberInfo[0]; }

		public MemberInfo[] GetMembers(BindingFlags bindingAttr) { return new MemberInfo[0]; }

		public MethodInfo GetMethod(string name, BindingFlags bindingAttr) { return null; }

		public MethodInfo GetMethod(string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers) { return null; }

		public MethodInfo[] GetMethods(BindingFlags bindingAttr) { return new MethodInfo[0]; }

		public PropertyInfo GetProperty(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers) { return null; }

		public PropertyInfo GetProperty(string name, BindingFlags bindingAttr) { return null; }

		public PropertyInfo[] GetProperties(BindingFlags bindingAttr) { return new PropertyInfo[0]; }

		#endregion

		public Type UnderlyingSystemType { get { return typeof(object); } }

		public object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters) { return ExecuteHandler(name, args); }

		public void Dispose()
		{
			DisposeAll();
			GC.SuppressFinalize(this);
		}

		~ComEventSink() { DisposeAll(); }

		void DisposeAll()
		{
			if (_connectionPoint == null || _adviseCookie == -1)
				return;
			try
			{
				_connectionPoint.Unadvise(_adviseCookie);
				// _connectionPoint �͂��̃I�u�W�F�N�g�̃R���X�g���N�^�� CLR �ɓ������̂ŁA���̎Q�ƃJ�E���^�̓C���N�������g����Ă���B
				// _connectionPoint �𑼂̃R���|�[�l���g�Ɍ��J���Ă��Ȃ��̂ŁA�����N���Ă��鑼�̃I�u�W�F�N�g�ɑ΂��� RCW ���L������S�z���������[�X�ł���B
				Marshal.ReleaseComObject(_connectionPoint);
			}
			catch (Exception ex)
			{
				COMException exCOM = ex as COMException;
				if (exCOM != null && exCOM.ErrorCode == ComHresults.CONNECT_E_NOCONNECTION)
				{
					Debug.Assert(false, "IConnectionPoint::Unadvise returned CONNECT_E_NOCONNECTION.");
					throw;
				}
			}
			finally
			{
				_connectionPoint = null;
				_adviseCookie = -1;
				_sourceIid = Guid.Empty;
			}
		}

		ComEventSinkMethod FindSinkMethod(string name) { return _comEventSinkMethods == null ? null : _comEventSinkMethods.Find(element => element._name == name); }
	}
}