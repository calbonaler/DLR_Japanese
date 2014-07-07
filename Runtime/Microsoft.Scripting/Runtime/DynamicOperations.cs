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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Contracts;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>���I�ȃI�u�W�F�N�g�ɑ΂��鑀���񋟂��܂��B</summary>
	public sealed class DynamicOperations
	{
		LanguageContext _lc;
		
		Dictionary<int, Func<DynamicOperations, CallSiteBinder, object, object[], object>> _invokers = new Dictionary<int, Func<DynamicOperations, CallSiteBinder, object, object[], object>>();

		/// <summary>�p�ɂɍs���鑀��̃L���b�V���Ɏg�p����� SiteKey �̃f�B�N�V���i���ł��B</summary>
		Dictionary<SiteKey, SiteKey> _sites = new Dictionary<SiteKey, SiteKey>();

		/// <summary>�ŋ߂̃N���[���A�b�v���܂łɍ쐬�����T�C�g���ł��B </summary>
		int LastCleanup;

		/// <summary>����܂łɍ쐬�����T�C�g���ł��B</summary>
		int SitesCreated;

		/// <summary>�L���b�V���̃N���[���A�b�v�����s�����ŏ��̃T�C�g���ł��B</summary>
		const int CleanupThreshold = 20;

		/// <summary>�폜�ɕK�v�ƂȂ镽�ώg�p�񐔂Ƃ̍ŏ����ł��B</summary>
		const int RemoveThreshold = 2;

		/// <summary>�P��̃L���b�V���N���[���A�b�v�ō폜����ő�l�ł��B</summary>
		const int StopCleanupThreshold = CleanupThreshold / 2;

		/// <summary>����ȏ�N���[���A�b�v���s���Ȃ��Ƃ��ɁA�N���A���ׂ��T�C�g���ł��B</summary>
		const int ClearThreshold = 50;
		
		/// <summary>�w�肳�ꂽ����v���o�C�_���g�p���āA<see cref="Microsoft.Scripting.Runtime.DynamicOperations"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="languageContext">��ɂȂ錾��v���o�C�_���w�肵�܂��B</param>
		public DynamicOperations(LanguageContext languageContext)
		{
			ContractUtils.RequiresNotNull(languageContext, "languageContext");
			_lc = languageContext;
		}

		#region Basic Operations

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g���w�肳�ꂽ�����ɂ���ČĂяo���܂��B</summary>
		/// <param name="obj">�Ăяo���I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="parameters">�I�u�W�F�N�g�Ăяo���̈������w�肵�܂��B</param>
		public object Invoke(object obj, params object[] parameters) { return GetInvoker(parameters.Length)(this, _lc.CreateInvokeBinder(new CallInfo(parameters.Length)), obj, parameters); }

		/// <summary>�I�u�W�F�N�g�̎w�肳�ꂽ�����o���w�肳�ꂽ������p���ČĂяo���܂��B</summary>
		/// <param name="obj">�Ăяo�������o��ێ����Ă���I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="memberName">�Ăяo�������o�̖��O���w�肵�܂��B</param>
		/// <param name="parameters">�����o�Ăяo���̈������w�肵�܂��B</param>
		public object InvokeMember(object obj, string memberName, params object[] parameters) { return InvokeMember(obj, memberName, false, parameters); }

		/// <summary>�I�u�W�F�N�g�̎w�肳�ꂽ�����o���w�肳�ꂽ������p���ČĂяo���܂��B</summary>
		/// <param name="obj">�Ăяo�������o��ێ����Ă���I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="memberName">�Ăяo�������o�̖��O���w�肵�܂��B</param>
		/// <param name="ignoreCase">�����o�̌����ɑ啶���Ə���������ʂ��Ȃ����ǂ����������l���w�肵�܂��B</param>
		/// <param name="parameters">�����o�Ăяo���̈������w�肵�܂��B</param>
		public object InvokeMember(object obj, string memberName, bool ignoreCase, params object[] parameters) { return GetInvoker(parameters.Length)(this, _lc.CreateCallBinder(memberName, ignoreCase, new CallInfo(parameters.Length)), obj, parameters); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�Ɏw�肳�ꂽ�������g�p���ĐV�����C���X�^���X���쐬���܂��B</summary>
		/// <param name="obj">�C���X�^���X���쐬�����ɂȂ�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="parameters">�C���X�^���X�̍쐬�̍ۂɕK�v�ɂȂ�������w�肵�܂��B</param>
		public object CreateInstance(object obj, params object[] parameters) { return GetInvoker(parameters.Length)(this, _lc.CreateCreateBinder(new CallInfo(parameters.Length)), obj, parameters); }

		/// <summary>�I�u�W�F�N�g�̎w�肳�ꂽ�����o���擾���܂��B�����o�����݂��Ȃ����A�������ݐ�p�̏ꍇ�͗�O�𔭐������܂��B</summary>
		/// <param name="obj">�擾���郁���o��ێ����Ă���I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�擾���郁���o�̖��O���w�肵�܂��B</param>
		public object GetMember(object obj, string name) { return GetMember(obj, name, false); }

		/// <summary>�I�u�W�F�N�g�̎w�肳�ꂽ�����o���擾���A���ʂ��w�肳�ꂽ�^�ɕϊ����܂��B�����o�����݂��Ȃ����A�������ݐ�p�̏ꍇ�͗�O�𔭐������܂��B</summary>
		/// <param name="obj">�擾���郁���o��ێ����Ă���I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�擾���郁���o�̖��O���w�肵�܂��B</param>
		public T GetMember<T>(object obj, string name) { return GetMember<T>(obj, name, false); }

		/// <summary>�I�u�W�F�N�g�̎w�肳�ꂽ�����o���擾���܂��B�����o������Ɏ擾���ꂽ�ꍇ�� true ��Ԃ��܂��B</summary>
		/// <param name="obj">�擾���郁���o��ێ����Ă���I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�擾���郁���o�̖��O���w�肵�܂��B</param>
		/// <param name="value">�擾���������o�̒l���i�[����ϐ����w�肵�܂��B</param>
		public bool TryGetMember(object obj, string name, out object value) { return TryGetMember(obj, name, false, out value); }

		/// <summary>�I�u�W�F�N�g�Ɏw�肳�ꂽ�����o�����݂��邩�ǂ����������l��Ԃ��܂��B</summary>
		/// <param name="obj">�����o�����݂��邩�ǂ����𒲂ׂ�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">���݂��邩�ǂ����𒲂ׂ郁���o�̖��O���w�肵�܂��B</param>
		public bool ContainsMember(object obj, string name) { return ContainsMember(obj, name, false); }

		/// <summary>�I�u�W�F�N�g����w�肳�ꂽ�����o���폜���܂��B</summary>
		/// <param name="obj">�����o���폜����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�폜���郁���o�̖��O���w�肵�܂��B</param>
		public void RemoveMember(object obj, string name) { RemoveMember(obj, name, false); }

		/// <summary>�I�u�W�F�N�g�̎w�肳�ꂽ�����o�Ɏw�肳�ꂽ�l��ݒ肵�܂��B</summary>
		/// <param name="obj">�ݒ肷�郁���o��ێ����Ă���I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�ݒ肷�郁���o�̖��O���w�肵�܂��B</param>
		/// <param name="value">�����o�ɐݒ肷��l���w�肵�܂��B</param>
		public void SetMember(object obj, string name, object value) { SetMember(obj, name, value, false); }

		/// <summary>�I�u�W�F�N�g�̎w�肳�ꂽ�����o�Ɏw�肳�ꂽ�l��ݒ肵�܂��B���̃I�[�o�[���[�h�͌����Ɍ^�w�肳��Ă��邽�߁A�{�b�N�X����L���X�g������邱�Ƃ��ł��܂��B</summary>
		/// <param name="obj">�ݒ肷�郁���o��ێ����Ă���I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�ݒ肷�郁���o�̖��O���w�肵�܂��B</param>
		/// <param name="value">�����o�ɐݒ肷��l���w�肵�܂��B</param>
		public void SetMember<T>(object obj, string name, T value) { SetMember<T>(obj, name, value, false); }

		/// <summary>�I�u�W�F�N�g�̎w�肳�ꂽ�����o���擾���܂��B�����o�����݂��Ȃ����A�������ݐ�p�̏ꍇ�͗�O�𔭐������܂��B</summary>
		/// <param name="obj">�擾���郁���o��ێ����Ă���I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�擾���郁���o�̖��O���w�肵�܂��B</param>
		/// <param name="ignoreCase">�����o�̌����ɑ啶���Ə���������ʂ��Ȃ����ǂ����������l���w�肵�܂��B</param>
		public object GetMember(object obj, string name, bool ignoreCase)
		{
			var site = GetOrCreateSite<object, object>(_lc.CreateGetMemberBinder(name, ignoreCase));
			return site.Target(site, obj);
		}

		/// <summary>�I�u�W�F�N�g�̎w�肳�ꂽ�����o���擾���A���ʂ��w�肳�ꂽ�^�ɕϊ����܂��B�����o�����݂��Ȃ����A�������ݐ�p�̏ꍇ�͗�O�𔭐������܂��B</summary>
		/// <param name="obj">�擾���郁���o��ێ����Ă���I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�擾���郁���o�̖��O���w�肵�܂��B</param>
		/// <param name="ignoreCase">�����o�̌����ɑ啶���Ə���������ʂ��Ȃ����ǂ����������l���w�肵�܂��B</param>
		public T GetMember<T>(object obj, string name, bool ignoreCase)
		{
			var convertSite = GetOrCreateSite<object, T>(_lc.CreateConvertBinder(typeof(T), null));
			var site = GetOrCreateSite<object, object>(_lc.CreateGetMemberBinder(name, ignoreCase));
			return convertSite.Target(convertSite, site.Target(site, obj));
		}

		/// <summary>�I�u�W�F�N�g�̎w�肳�ꂽ�����o���擾���܂��B�����o������Ɏ擾���ꂽ�ꍇ�� true ��Ԃ��܂��B</summary>
		/// <param name="obj">�擾���郁���o��ێ����Ă���I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�擾���郁���o�̖��O���w�肵�܂��B</param>
		/// <param name="ignoreCase">�����o�̌����ɑ啶���Ə���������ʂ��Ȃ����ǂ����������l���w�肵�܂��B</param>
		/// <param name="value">�擾���������o�̒l���i�[����ϐ����w�肵�܂��B</param>
		public bool TryGetMember(object obj, string name, bool ignoreCase, out object value)
		{
			try
			{
				value = GetMember(obj, name, ignoreCase);
				return true;
			}
			catch (MissingMemberException)
			{
				value = null;
				return false;
			}
		}

		/// <summary>�I�u�W�F�N�g�Ɏw�肳�ꂽ�����o�����݂��邩�ǂ����������l��Ԃ��܂��B</summary>
		/// <param name="obj">�����o�����݂��邩�ǂ����𒲂ׂ�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">���݂��邩�ǂ����𒲂ׂ郁���o�̖��O���w�肵�܂��B</param>
		/// <param name="ignoreCase">�����o�̌����ɑ啶���Ə���������ʂ��Ȃ����ǂ����������l���w�肵�܂��B</param>
		public bool ContainsMember(object obj, string name, bool ignoreCase)
		{
			object dummy;
			return TryGetMember(obj, name, ignoreCase, out dummy);
		}

		/// <summary>�I�u�W�F�N�g����w�肳�ꂽ�����o���폜���܂��B</summary>
		/// <param name="obj">�����o���폜����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�폜���郁���o�̖��O���w�肵�܂��B</param>
		/// <param name="ignoreCase">�����o�̌����ɑ啶���Ə���������ʂ��Ȃ����ǂ����������l���w�肵�܂��B</param>
		public void RemoveMember(object obj, string name, bool ignoreCase)
		{
			var site = GetOrCreateSite<Action<CallSite, object>>(_lc.CreateDeleteMemberBinder(name, ignoreCase));
			site.Target(site, obj);
		}

		/// <summary>�I�u�W�F�N�g�̎w�肳�ꂽ�����o�Ɏw�肳�ꂽ�l��ݒ肵�܂��B</summary>
		/// <param name="obj">�ݒ肷�郁���o��ێ����Ă���I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�ݒ肷�郁���o�̖��O���w�肵�܂��B</param>
		/// <param name="value">�����o�ɐݒ肷��l���w�肵�܂��B</param>
		/// <param name="ignoreCase">�����o�̌����ɑ啶���Ə���������ʂ��Ȃ����ǂ����������l���w�肵�܂��B</param>
		public void SetMember(object obj, string name, object value, bool ignoreCase)
		{
			var site = GetOrCreateSite<object, object, object>(_lc.CreateSetMemberBinder(name, ignoreCase));
			site.Target(site, obj, value);
		}

		/// <summary>�I�u�W�F�N�g�̎w�肳�ꂽ�����o�Ɏw�肳�ꂽ�l��ݒ肵�܂��B���̃I�[�o�[���[�h�͌����Ɍ^�w�肳��Ă��邽�߁A�{�b�N�X����L���X�g������邱�Ƃ��ł��܂��B</summary>
		/// <param name="obj">�ݒ肷�郁���o��ێ����Ă���I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�ݒ肷�郁���o�̖��O���w�肵�܂��B</param>
		/// <param name="value">�����o�ɐݒ肷��l���w�肵�܂��B</param>
		/// <param name="ignoreCase">�����o�̌����ɑ啶���Ə���������ʂ��Ȃ����ǂ����������l���w�肵�܂��B</param>
		public void SetMember<T>(object obj, string name, T value, bool ignoreCase)
		{
			var site = GetOrCreateSite<object, T, object>(_lc.CreateSetMemberBinder(name, ignoreCase));
			site.Target(site, obj, value);
		}

		/// <summary>�I�u�W�F�N�g���w�肳�ꂽ�^�ɕϊ����܂��B�ϊ��������I�ɍs���邩�ǂ����͌���d�l�ɂ���Č��肳��܂��B</summary>
		/// <param name="obj">�^��ϊ�����I�u�W�F�N�g���w�肵�܂��B</param>
		public T ConvertTo<T>(object obj)
		{
			var site = GetOrCreateSite<object, T>(_lc.CreateConvertBinder(typeof(T), null));
			return site.Target(site, obj);
		}

		/// <summary>�I�u�W�F�N�g���w�肳�ꂽ�^�ɕϊ����܂��B�ϊ��������I�ɍs���邩�ǂ����͌���d�l�ɂ���Č��肳��܂��B</summary>
		/// <param name="obj">�^��ϊ�����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="type">�ϊ����ʂƂȂ�^���w�肵�܂��B</param>
		public object ConvertTo(object obj, Type type)
		{
			if (type.IsInterface || type.IsClass)
			{
				var site = GetOrCreateSite<object, object>(_lc.CreateConvertBinder(type, null));
				return site.Target(site, obj);
			}

			// TODO: We should probably cache these instead of using reflection all the time.
			foreach (MethodInfo mi in typeof(DynamicOperations).GetMember("ConvertTo"))
			{
				if (mi.IsGenericMethod)
				{
					try { return mi.MakeGenericMethod(type).Invoke(this, new [] { obj }); }
					catch (TargetInvocationException tie) { throw tie.InnerException; }
				}
			}

			throw new InvalidOperationException();
		}

		/// <summary>�I�u�W�F�N�g���w�肳�ꂽ�^�ɕϊ����܂��B�ϊ������������ꍇ�� true ��Ԃ��܂��B�ϊ��������I�ɍs���邩�ǂ����͌���d�l�ɂ���Č��肳��܂��B</summary>
		/// <param name="obj">�^��ϊ�����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="result">�ϊ����ꂽ�I�u�W�F�N�g���i�[����ϐ����w�肵�܂��B</param>
		public bool TryConvertTo<T>(object obj, out T result)
		{
			try
			{
				result = ConvertTo<T>(obj);
				return true;
			}
			catch (ArgumentTypeException)
			{
				result = default(T);
				return false;
			}
			catch (InvalidCastException)
			{
				result = default(T);
				return false;
			}
		}

		/// <summary>�I�u�W�F�N�g���w�肳�ꂽ�^�ɕϊ����܂��B�ϊ������������ꍇ�� true ��Ԃ��܂��B�ϊ��������I�ɍs���邩�ǂ����͌���d�l�ɂ���Č��肳��܂��B</summary>
		/// <param name="obj">�^��ϊ�����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="type">�ϊ����ʂƂȂ�^���w�肵�܂��B</param>
		/// <param name="result">�ϊ����ꂽ�I�u�W�F�N�g���i�[����ϐ����w�肵�܂��B</param>
		public bool TryConvertTo(object obj, Type type, out object result)
		{
			try
			{
				result = ConvertTo(obj, type);
				return true;
			}
			catch (ArgumentTypeException)
			{
				result = null;
				return false;
			}
			catch (InvalidCastException)
			{
				result = null;
				return false;
			}
		}

		/// <summary>�I�u�W�F�N�g����񂪌�������\���̂��閾���I�ϊ����g�p���Ďw�肳�ꂽ�^�ɕϊ����܂��B</summary>
		/// <param name="obj">�^��ϊ�����I�u�W�F�N�g���w�肵�܂��B</param>
		public T ExplicitConvertTo<T>(object obj)
		{
			var site = GetOrCreateSite<object, T>(_lc.CreateConvertBinder(typeof(T), true));
			return site.Target(site, obj);
		}

		/// <summary>�I�u�W�F�N�g����񂪌�������\���̂��閾���I�ϊ����g�p���Ďw�肳�ꂽ�^�ɕϊ����܂��B</summary>
		/// <param name="obj">�^��ϊ�����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="type">�ϊ����ʂƂȂ�^���w�肵�܂��B</param>
		public object ExplicitConvertTo(object obj, Type type)
		{
			var site = GetOrCreateSite<object, object>(_lc.CreateConvertBinder(type, true));
			return site.Target(site, obj);
		}

		/// <summary>�I�u�W�F�N�g����񂪌�������\���̂��閾���I�ϊ����g�p���Ďw�肳�ꂽ�^�ɕϊ����܂��B�ϊ������������ꍇ�� true ��Ԃ��܂��B</summary>
		/// <param name="obj">�^��ϊ�����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="type">�ϊ����ʂƂȂ�^���w�肵�܂��B</param>
		/// <param name="result">�ϊ����ꂽ�I�u�W�F�N�g���i�[����ϐ����w�肵�܂��B</param>
		public bool TryExplicitConvertTo(object obj, Type type, out object result)
		{
			try
			{
				result = ExplicitConvertTo(obj, type);
				return true;
			}
			catch (ArgumentTypeException)
			{
				result = null;
				return false;
			}
			catch (InvalidCastException)
			{
				result = null;
				return false;
			}
		}

		/// <summary>�I�u�W�F�N�g����񂪌�������\���̂��閾���I�ϊ����g�p���Ďw�肳�ꂽ�^�ɕϊ����܂��B�ϊ������������ꍇ�� true ��Ԃ��܂��B</summary>
		/// <param name="obj">�^��ϊ�����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="result">�ϊ����ꂽ�I�u�W�F�N�g���i�[����ϐ����w�肵�܂��B</param>
		public bool TryExplicitConvertTo<T>(object obj, out T result)
		{
			try
			{
				result = ExplicitConvertTo<T>(obj);
				return true;
			}
			catch (ArgumentTypeException)
			{
				result = default(T);
				return false;
			}
			catch (InvalidCastException)
			{
				result = default(T);
				return false;
			}
		}

		/// <summary>�I�u�W�F�N�g���w�肳�ꂽ�^�ɈÖٓI�ɕϊ����܂��B</summary>
		/// <param name="obj">�^��ϊ�����I�u�W�F�N�g���w�肵�܂��B</param>
		public T ImplicitConvertTo<T>(object obj)
		{
			var site = GetOrCreateSite<object, T>(_lc.CreateConvertBinder(typeof(T), false));
			return site.Target(site, obj);
		}

		/// <summary>�I�u�W�F�N�g���w�肳�ꂽ�^�ɈÖٓI�ɕϊ����܂��B</summary>
		/// <param name="obj">�^��ϊ�����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="type">�ϊ����ʂƂȂ�^���w�肵�܂��B</param>
		public object ImplicitConvertTo(object obj, Type type)
		{
			var site = GetOrCreateSite<object, object>(_lc.CreateConvertBinder(type, false));
			return site.Target(site, obj);
		}

		/// <summary>�I�u�W�F�N�g���w�肳�ꂽ�^�ɈÖٓI�ɕϊ����܂��B�ϊ������������ꍇ�� true ��Ԃ��܂��B</summary>
		/// <param name="obj">�^��ϊ�����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="type">�ϊ����ʂƂȂ�^���w�肵�܂��B</param>
		/// <param name="result">�ϊ����ꂽ�I�u�W�F�N�g���i�[����ϐ����w�肵�܂��B</param>
		public bool TryImplicitConvertTo(object obj, Type type, out object result)
		{
			try
			{
				result = ImplicitConvertTo(obj, type);
				return true;
			}
			catch (ArgumentTypeException)
			{
				result = null;
				return false;
			}
			catch (InvalidCastException)
			{
				result = null;
				return false;
			}
		}

		/// <summary>�I�u�W�F�N�g���w�肳�ꂽ�^�ɈÖٓI�ɕϊ����܂��B�ϊ������������ꍇ�� true ��Ԃ��܂��B</summary>
		/// <param name="obj">�^��ϊ�����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="result">�ϊ����ꂽ�I�u�W�F�N�g���i�[����ϐ����w�肵�܂��B</param>
		public bool TryImplicitConvertTo<T>(object obj, out T result)
		{
			try
			{
				result = ImplicitConvertTo<T>(obj);
				return true;
			}
			catch (ArgumentTypeException)
			{
				result = default(T);
				return false;
			}
			catch (InvalidCastException)
			{
				result = default(T);
				return false;
			}
		}

		/// <summary>�ėp�̒P�����Z�������Ɍ^�w�肳�ꂽ�Ώۂɑ΂��Ď��s���܂��B</summary>
		/// <param name="operation">�P�����Z�̎�ނ����� <see cref="System.Linq.Expressions.ExpressionType"/> ���w�肵�܂��B</param>
		/// <param name="target">�P�����Z����p������Ώۂ��w�肵�܂��B</param>
		public TResult DoOperation<TTarget, TResult>(ExpressionType operation, TTarget target)
		{
			var site = GetOrCreateSite<TTarget, TResult>(_lc.CreateUnaryOperationBinder(operation));
			return site.Target(site, target);
		}

		/// <summary>�ėp�̓񍀉��Z�������Ɍ^�w�肳�ꂽ�Ώۂɑ΂��Ď��s���܂��B</summary>
		/// <param name="operation">�񍀉��Z�̎�ނ����� <see cref="System.Linq.Expressions.ExpressionType"/> ���w�肵�܂��B</param>
		/// <param name="target">�񍀉��Z����p�����鍶���̑Ώۂ��w�肵�܂��B</param>
		/// <param name="other">�񍀉��Z����p������E���̑Ώۂ��w�肵�܂��B</param>
		public TResult DoOperation<TTarget, TOther, TResult>(ExpressionType operation, TTarget target, TOther other)
		{
			var site = GetOrCreateSite<TTarget, TOther, TResult>(_lc.CreateBinaryOperationBinder(operation));
			return site.Target(site, target, other);
		}

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�ɑ΂��镶����Œ񋟂����h�L�������g��Ԃ��܂��B</summary>
		/// <param name="o">�h�L�������g���擾����I�u�W�F�N�g���w�肵�܂��B</param>
		public string GetDocumentation(object o) { return _lc.GetDocumentation(o); }

		/// <summary>���[�U�[�ɑ΂���\���`���̎w�肳�ꂽ�I�u�W�F�N�g�̌Ăяo���ɑ΂��ēK�p�����V�O�l�`���̃��X�g��Ԃ��܂��B</summary>
		/// <param name="o">�V�O�l�`���̃��X�g���擾����I�u�W�F�N�g���w�肵�܂��B</param>
		public IList<string> GetCallSignatures(object o) { return _lc.GetCallSignatures(o); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g���Ăяo���\���ǂ����������l���擾���܂��B</summary>
		/// <param name="o">�Ăяo���\���ǂ����𒲂ׂ�I�u�W�F�N�g���w�肵�܂��B</param>
		public bool IsCallable(object o) { return _lc.IsCallable(o); }

		/// <summary>�I�u�W�F�N�g�̊��m�̃����o�̈ꗗ��Ԃ��܂��B</summary>
		/// <param name="obj">�����o�̈ꗗ���擾����I�u�W�F�N�g���w�肵�܂��B</param>
		public IList<string> GetMemberNames(object obj) { return _lc.GetMemberNames(obj); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�̕�����\��������ŗL�̕\���`���ŕԂ��܂��B</summary>
		/// <param name="obj">������\�����擾����I�u�W�F�N�g���w�肵�܂��B</param>
		public string Format(object obj) { return _lc.FormatObject(this, obj); }

		#endregion

		#region Private implementation details

		/// <summary>�w�肳�ꂽ�o�C���_�[�ɑ΂��Ďw�肳�ꂽ�^�������g�p���ē��I�T�C�g���擾�܂��͍쐬���܂��B</summary>
		/// <param name="siteBinder">���I�T�C�g�ɑ΂��ē���̎��s���o�C���f�B���O���s���o�C���_�[���w�肵�܂��B</param>
		/// <remarks>
		/// ���̃��\�b�h�̓L���b�V���ɓ��I�T�C�g�����݂���Ύ擾���A����ȊO�̏ꍇ�͍쐬���܂��B
		/// �ŋ߂̎g�p����L���b�V��������ɂȂ����ꍇ�̓L���b�V���̓N���[���A�b�v����܂��B
		/// </remarks>
		public CallSite<Func<CallSite, T1, TResult>> GetOrCreateSite<T1, TResult>(CallSiteBinder siteBinder) { return GetOrCreateSite<Func<CallSite, T1, TResult>>(siteBinder); }

		/// <summary>�w�肳�ꂽ�o�C���_�[�ɑ΂��Ďw�肳�ꂽ�^�������g�p���ē��I�T�C�g���擾�܂��͍쐬���܂��B</summary>
		/// <param name="siteBinder">���I�T�C�g�ɑ΂��ē���̎��s���o�C���f�B���O���s���o�C���_�[���w�肵�܂��B</param>
		/// <remarks>
		/// ���̃��\�b�h�̓L���b�V���ɓ��I�T�C�g�����݂���Ύ擾���A����ȊO�̏ꍇ�͍쐬���܂��B
		/// �ŋ߂̎g�p����L���b�V��������ɂȂ����ꍇ�̓L���b�V���̓N���[���A�b�v����܂��B
		/// </remarks>
		public CallSite<Func<CallSite, T1, T2, TResult>> GetOrCreateSite<T1, T2, TResult>(CallSiteBinder siteBinder) { return GetOrCreateSite<Func<CallSite, T1, T2, TResult>>(siteBinder); }

		/// <summary>�w�肳�ꂽ�o�C���_�[�ɑ΂��Ďw�肳�ꂽ�^�������g�p���ē��I�T�C�g���擾�܂��͍쐬���܂��B</summary>
		/// <param name="siteBinder">���I�T�C�g�ɑ΂��ē���̎��s���o�C���f�B���O���s���o�C���_�[���w�肵�܂��B</param>
		/// <remarks>
		/// ���̃��\�b�h�̓L���b�V���ɓ��I�T�C�g�����݂���Ύ擾���A����ȊO�̏ꍇ�͍쐬���܂��B
		/// �ŋ߂̎g�p����L���b�V��������ɂȂ����ꍇ�̓L���b�V���̓N���[���A�b�v����܂��B
		/// </remarks>
		public CallSite<TDelegate> GetOrCreateSite<TDelegate>(CallSiteBinder siteBinder) where TDelegate : class
		{
			SiteKey sk = new SiteKey(typeof(CallSite<TDelegate>), siteBinder);
			lock (_sites)
			{
				SiteKey old;
				if (!_sites.TryGetValue(sk, out old))
				{
					if (++SitesCreated < 0)
						SitesCreated = LastCleanup = 0; // �I�[�o�[�t���[�����̂� 0 �Ƀ��Z�b�g���܂��B
					sk.Site = CallSite<TDelegate>.Create(sk.SiteBinder);
					_sites[sk] = sk;
				}
				else
					sk = old;
				sk.HitCount++;
				CleanupNoLock();
			}
			return (CallSite<TDelegate>)sk.Site;
		}

		/// <summary>�L���b�V�����炠�܂�g�p����Ȃ����I�T�C�g���폜���܂��B</summary>
		void CleanupNoLock()
		{
			// cleanup only if we have too many sites and we've created a bunch since our last cleanup
			if (_sites.Count > CleanupThreshold && SitesCreated - LastCleanup > CleanupThreshold)
			{
				LastCleanup = SitesCreated;

				// calculate the average use, remove up to StopCleanupThreshold that are below average.
				int avgUse = _sites.Aggregate(0, (x, y) => x + y.Key.HitCount) / _sites.Count;
				if (avgUse == 1 && _sites.Count > ClearThreshold)
				{
					// we only have a bunch of one-off requests
					_sites.Clear();
					return;
				}

				var toRemove = _sites.Keys.Where(x => avgUse - x.HitCount > RemoveThreshold).Take(StopCleanupThreshold).ToList();
				// if we have a setup like weight(100), weight(1), weight(1), weight(1), ... we don't want
				// to just run through and remove all of the weight(1)'s. 

				if (toRemove.Count > 0)
				{
					foreach (var sk in toRemove)
						_sites.Remove(sk);
					// reset all hit counts so the next time through is fair to newly added members which may take precedence.
					foreach (var sk in _sites.Keys)
						sk.HitCount = 0;
				}
			}
		}

		/// <summary>
		/// ���ׂĂ̌ŗL�̓��I�T�C�g����т����̎g�p�p�^�[����ǐՂ��A�o�C���_�[�ƃT�C�g�^�̑g���n�b�V�����܂��B
		/// ����ɂ��̃N���X�̓q�b�g����ǐՂ��A�֘A�t����ꂽ�T�C�g��ێ����܂��B
		/// </summary>
		class SiteKey : IEquatable<SiteKey>
		{
			// �f�[�^�̃L�[����
			internal CallSiteBinder SiteBinder;
			Type _siteType;

			// ������r�ɂ͗p����ꂸ�A�L���b�V���ɂ̂݊֗^����
			public int HitCount;
			public CallSite Site;

			public SiteKey(Type siteType, CallSiteBinder siteBinder)
			{
				Debug.Assert(siteType != null);
				Debug.Assert(siteBinder != null);

				SiteBinder = siteBinder;
				_siteType = siteType;
			}

			[Confined]
			public override bool Equals(object obj) { return Equals(obj as SiteKey); }

			[Confined]
			public override int GetHashCode() { return SiteBinder.GetHashCode() ^ _siteType.GetHashCode(); }

			[StateIndependent]
			public bool Equals(SiteKey other) { return other != null && other.SiteBinder.Equals(SiteBinder) && other._siteType == _siteType; }

#if DEBUG
			[Confined]
			public override string ToString() { return string.Format("{0} {1}", SiteBinder.ToString(), HitCount); }
#endif
		}

		Func<DynamicOperations, CallSiteBinder, object, object[], object> GetInvoker(int paramCount)
		{
			Func<DynamicOperations, CallSiteBinder, object, object[], object> invoker;
			lock (_invokers)
			{
				if (!_invokers.TryGetValue(paramCount, out invoker))
				{
					var dynOps = Expression.Parameter(typeof(DynamicOperations));
					var callInfo = Expression.Parameter(typeof(CallSiteBinder));
					var target = Expression.Parameter(typeof(object));
					var args = Expression.Parameter(typeof(object[]));
					var funcType = Expression.GetDelegateType(Enumerable.Repeat(typeof(CallSite), 1).Concat(Enumerable.Repeat(typeof(object), paramCount + 2)).ToArray());
					var site = Expression.Variable(typeof(CallSite<>).MakeGenericType(funcType));
					_invokers[paramCount] = invoker = Expression.Lambda<Func<DynamicOperations, CallSiteBinder, object, object[], object>>(
						Expression.Block(
							new[] { site },
							Expression.Assign(site, Expression.Call(dynOps, new Func<CallSiteBinder, CallSite<Action>>(GetOrCreateSite<Action>).Method.GetGenericMethodDefinition().MakeGenericMethod(funcType), callInfo)),
							Expression.Invoke(
								Expression.Field(site, site.Type.GetField("Target")),
								Enumerable.Repeat<Expression>(site, 1).Concat(Enumerable.Repeat(target, 1)).Concat(Enumerable.Range(0, paramCount).Select(x => Expression.ArrayIndex(args, Expression.Constant(x))))
							)
						),
						new[] { dynOps, callInfo, target, args }
					).Compile();
				}
			}
			return invoker;
		}

		#endregion
	}
}
