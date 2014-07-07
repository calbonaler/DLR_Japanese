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
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Remoting;
using System.Security.Permissions;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting
{
	/// <summary>
	/// �����o �A�N�Z�X�A�ϊ��A�C���f�b�N�X�ȂǂƂ������I�u�W�F�N�g�̑���Ɋւ����K�͂ȃJ�^���O��񋟂��܂��B
	/// �����͂�荂�@�\�ȃz�X�g�ɂ����ė��p�\�ȓ�����������уc�[���T�|�[�g�T�[�r�X�ƂȂ�܂��B
	/// </summary>
	/// <remarks>
	/// <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> �C���X�^���X�� <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> ���擾�ł��A
	/// ����̃Z�}���e�B�N�X�ɑ΂��ăG���W���Ɋ֘A�t�����܂��B<see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> �ɂ́A
	/// �G���W���̂��ׂĂ̎g�p�ɑ΂��ċ��L�ł������̃C���X�^���X�����݂��܂����A���ɍ��@�\�ȃz�X�g�ł̓C���X�^���X���쐬���邱�Ƃ��ł��܂��B
	/// </remarks>
	public sealed class ObjectOperations : MarshalByRefObject
	{
		readonly DynamicOperations _ops;

		// friend class: DynamicOperations
		internal ObjectOperations(DynamicOperations ops, ScriptEngine engine)
		{
			Assert.NotNull(ops);
			Assert.NotNull(engine);
			_ops = ops;
			Engine = engine;
		}

		public ScriptEngine Engine { get; private set; }

		#region Local Operations

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g���Ăяo���\���ǂ����������l���擾���܂��B</summary>
		/// <param name="obj">�Ăяo���\���ǂ����𒲂ׂ�I�u�W�F�N�g���w�肵�܂��B</param>
		public bool IsCallable(object obj) { return _ops.IsCallable(obj); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g���w�肳�ꂽ�����ɂ���ČĂяo���܂��B</summary>
		/// <param name="obj">�Ăяo���I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="parameters">�I�u�W�F�N�g�Ăяo���̈������w�肵�܂��B</param>
		public dynamic Invoke(object obj, params object[] parameters) { return _ops.Invoke(obj, parameters); }

		/// <summary>�I�u�W�F�N�g�̎w�肳�ꂽ�����o���w�肳�ꂽ������p���ČĂяo���܂��B</summary>
		/// <param name="obj">�Ăяo�������o��ێ����Ă���I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="memberName">�Ăяo�������o�̖��O���w�肵�܂��B</param>
		/// <param name="parameters">�����o�Ăяo���̈������w�肵�܂��B</param>
		public dynamic InvokeMember(object obj, string memberName, params object[] parameters) { return _ops.InvokeMember(obj, memberName, parameters); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�Ɏw�肳�ꂽ�������g�p���ĐV�����C���X�^���X���쐬���܂��B</summary>
		/// <param name="obj">�C���X�^���X���쐬�����ɂȂ�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="parameters">�C���X�^���X�̍쐬�̍ۂɕK�v�ɂȂ�������w�肵�܂��B</param>
		public dynamic CreateInstance(object obj, params object[] parameters) { return _ops.CreateInstance(obj, parameters); }

		/// <summary>�I�u�W�F�N�g�̎w�肳�ꂽ�����o���擾���܂��B�����o�����݂��Ȃ����A�������ݐ�p�̏ꍇ�͗�O�𔭐������܂��B</summary>
		/// <param name="obj">�擾���郁���o��ێ����Ă���I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�擾���郁���o�̖��O���w�肵�܂��B</param>
		public dynamic GetMember(object obj, string name) { return _ops.GetMember(obj, name); }

		/// <summary>�I�u�W�F�N�g�̎w�肳�ꂽ�����o���擾���A���ʂ��w�肳�ꂽ�^�ɕϊ����܂��B�����o�����݂��Ȃ����A�������ݐ�p�̏ꍇ�͗�O�𔭐������܂��B</summary>
		/// <param name="obj">�擾���郁���o��ێ����Ă���I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�擾���郁���o�̖��O���w�肵�܂��B</param>
		public T GetMember<T>(object obj, string name) { return _ops.GetMember<T>(obj, name); 	}

		/// <summary>�I�u�W�F�N�g�̎w�肳�ꂽ�����o���擾���܂��B�����o������Ɏ擾���ꂽ�ꍇ�� true ��Ԃ��܂��B</summary>
		/// <param name="obj">�擾���郁���o��ێ����Ă���I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�擾���郁���o�̖��O���w�肵�܂��B</param>
		/// <param name="value">�擾���������o�̒l���i�[����ϐ����w�肵�܂��B</param>
		public bool TryGetMember(object obj, string name, out object value) { return _ops.TryGetMember(obj, name, out value); }

		/// <summary>�I�u�W�F�N�g�Ɏw�肳�ꂽ�����o�����݂��邩�ǂ����������l��Ԃ��܂��B</summary>
		/// <param name="obj">�����o�����݂��邩�ǂ����𒲂ׂ�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">���݂��邩�ǂ����𒲂ׂ郁���o�̖��O���w�肵�܂��B</param>
		public bool ContainsMember(object obj, string name) { return _ops.ContainsMember(obj, name); }

		/// <summary>�I�u�W�F�N�g����w�肳�ꂽ�����o���폜���܂��B</summary>
		/// <param name="obj">�����o���폜����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�폜���郁���o�̖��O���w�肵�܂��B</param>
		public void RemoveMember(object obj, string name) { _ops.RemoveMember(obj, name); }

		/// <summary>�I�u�W�F�N�g�̎w�肳�ꂽ�����o�Ɏw�肳�ꂽ�l��ݒ肵�܂��B</summary>
		/// <param name="obj">�ݒ肷�郁���o��ێ����Ă���I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�ݒ肷�郁���o�̖��O���w�肵�܂��B</param>
		/// <param name="value">�����o�ɐݒ肷��l���w�肵�܂��B</param>
		public void SetMember(object obj, string name, object value) { _ops.SetMember(obj, name, value); }

		/// <summary>�I�u�W�F�N�g�̎w�肳�ꂽ�����o�Ɏw�肳�ꂽ�l��ݒ肵�܂��B���̃I�[�o�[���[�h�͌����Ɍ^�w�肳��Ă��邽�߁A�{�b�N�X����L���X�g������邱�Ƃ��ł��܂��B</summary>
		/// <param name="obj">�ݒ肷�郁���o��ێ����Ă���I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�ݒ肷�郁���o�̖��O���w�肵�܂��B</param>
		/// <param name="value">�����o�ɐݒ肷��l���w�肵�܂��B</param>
		public void SetMember<T>(object obj, string name, T value) 	{ _ops.SetMember<T>(obj, name, value); }

		/// <summary>�I�u�W�F�N�g�̎w�肳�ꂽ�����o���擾���܂��B�����o�����݂��Ȃ����A�������ݐ�p�̏ꍇ�͗�O�𔭐������܂��B</summary>
		/// <param name="obj">�擾���郁���o��ێ����Ă���I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�擾���郁���o�̖��O���w�肵�܂��B</param>
		/// <param name="ignoreCase">�����o�̌����ɑ啶���Ə���������ʂ��Ȃ����ǂ����������l���w�肵�܂��B</param>
		public dynamic GetMember(object obj, string name, bool ignoreCase) { return _ops.GetMember(obj, name, ignoreCase); }

		/// <summary>�I�u�W�F�N�g�̎w�肳�ꂽ�����o���擾���A���ʂ��w�肳�ꂽ�^�ɕϊ����܂��B�����o�����݂��Ȃ����A�������ݐ�p�̏ꍇ�͗�O�𔭐������܂��B</summary>
		/// <param name="obj">�擾���郁���o��ێ����Ă���I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�擾���郁���o�̖��O���w�肵�܂��B</param>
		/// <param name="ignoreCase">�����o�̌����ɑ啶���Ə���������ʂ��Ȃ����ǂ����������l���w�肵�܂��B</param>
		public T GetMember<T>(object obj, string name, bool ignoreCase) { return _ops.GetMember<T>(obj, name, ignoreCase); }
		
		/// <summary>�I�u�W�F�N�g�̎w�肳�ꂽ�����o���擾���܂��B�����o������Ɏ擾���ꂽ�ꍇ�� true ��Ԃ��܂��B</summary>
		/// <param name="obj">�擾���郁���o��ێ����Ă���I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�擾���郁���o�̖��O���w�肵�܂��B</param>
		/// <param name="ignoreCase">�����o�̌����ɑ啶���Ə���������ʂ��Ȃ����ǂ����������l���w�肵�܂��B</param>
		/// <param name="value">�擾���������o�̒l���i�[����ϐ����w�肵�܂��B</param>
		public bool TryGetMember(object obj, string name, bool ignoreCase, out object value) { return _ops.TryGetMember(obj, name, ignoreCase, out value); }

		/// <summary>�I�u�W�F�N�g�Ɏw�肳�ꂽ�����o�����݂��邩�ǂ����������l��Ԃ��܂��B</summary>
		/// <param name="obj">�����o�����݂��邩�ǂ����𒲂ׂ�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">���݂��邩�ǂ����𒲂ׂ郁���o�̖��O���w�肵�܂��B</param>
		/// <param name="ignoreCase">�����o�̌����ɑ啶���Ə���������ʂ��Ȃ����ǂ����������l���w�肵�܂��B</param>
		public bool ContainsMember(object obj, string name, bool ignoreCase) { return _ops.ContainsMember(obj, name, ignoreCase); }

		/// <summary>�I�u�W�F�N�g����w�肳�ꂽ�����o���폜���܂��B</summary>
		/// <param name="obj">�����o���폜����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�폜���郁���o�̖��O���w�肵�܂��B</param>
		/// <param name="ignoreCase">�����o�̌����ɑ啶���Ə���������ʂ��Ȃ����ǂ����������l���w�肵�܂��B</param>
		public void RemoveMember(object obj, string name, bool ignoreCase) { _ops.RemoveMember(obj, name, ignoreCase); }

		/// <summary>�I�u�W�F�N�g�̎w�肳�ꂽ�����o�Ɏw�肳�ꂽ�l��ݒ肵�܂��B</summary>
		/// <param name="obj">�ݒ肷�郁���o��ێ����Ă���I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�ݒ肷�郁���o�̖��O���w�肵�܂��B</param>
		/// <param name="value">�����o�ɐݒ肷��l���w�肵�܂��B</param>
		/// <param name="ignoreCase">�����o�̌����ɑ啶���Ə���������ʂ��Ȃ����ǂ����������l���w�肵�܂��B</param>
		public void SetMember(object obj, string name, object value, bool ignoreCase) { _ops.SetMember(obj, name, value, ignoreCase); }

		/// <summary>�I�u�W�F�N�g�̎w�肳�ꂽ�����o�Ɏw�肳�ꂽ�l��ݒ肵�܂��B���̃I�[�o�[���[�h�͌����Ɍ^�w�肳��Ă��邽�߁A�{�b�N�X����L���X�g������邱�Ƃ��ł��܂��B</summary>
		/// <param name="obj">�ݒ肷�郁���o��ێ����Ă���I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�ݒ肷�郁���o�̖��O���w�肵�܂��B</param>
		/// <param name="value">�����o�ɐݒ肷��l���w�肵�܂��B</param>
		/// <param name="ignoreCase">�����o�̌����ɑ啶���Ə���������ʂ��Ȃ����ǂ����������l���w�肵�܂��B</param>
		public void SetMember<T>(object obj, string name, T value, bool ignoreCase) { _ops.SetMember<T>(obj, name, value, ignoreCase); }

		/// <summary>�I�u�W�F�N�g���w�肳�ꂽ�^�ɕϊ����܂��B�ϊ��������I�ɍs���邩�ǂ����͌���d�l�ɂ���Č��肳��܂��B</summary>
		/// <param name="obj">�^��ϊ�����I�u�W�F�N�g���w�肵�܂��B</param>
		public T ConvertTo<T>(object obj) { return _ops.ConvertTo<T>(obj); }

		/// <summary>�I�u�W�F�N�g���w�肳�ꂽ�^�ɕϊ����܂��B�ϊ��������I�ɍs���邩�ǂ����͌���d�l�ɂ���Č��肳��܂��B</summary>
		/// <param name="obj">�^��ϊ�����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="type">�ϊ����ʂƂȂ�^���w�肵�܂��B</param>
		public object ConvertTo(object obj, Type type)
		{
			ContractUtils.RequiresNotNull(type, "type");
			return _ops.ConvertTo(obj, type);
		}

		/// <summary>�I�u�W�F�N�g���w�肳�ꂽ�^�ɕϊ����܂��B�ϊ������������ꍇ�� true ��Ԃ��܂��B�ϊ��������I�ɍs���邩�ǂ����͌���d�l�ɂ���Č��肳��܂��B</summary>
		/// <param name="obj">�^��ϊ�����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="result">�ϊ����ꂽ�I�u�W�F�N�g���i�[����ϐ����w�肵�܂��B</param>
		public bool TryConvertTo<T>(object obj, out T result) { return _ops.TryConvertTo<T>(obj, out result); }

		/// <summary>�I�u�W�F�N�g���w�肳�ꂽ�^�ɕϊ����܂��B�ϊ������������ꍇ�� true ��Ԃ��܂��B�ϊ��������I�ɍs���邩�ǂ����͌���d�l�ɂ���Č��肳��܂��B</summary>
		/// <param name="obj">�^��ϊ�����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="type">�ϊ����ʂƂȂ�^���w�肵�܂��B</param>
		/// <param name="result">�ϊ����ꂽ�I�u�W�F�N�g���i�[����ϐ����w�肵�܂��B</param>
		public bool TryConvertTo(object obj, Type type, out object result) { return _ops.TryConvertTo(obj, type, out result); }

		/// <summary>�I�u�W�F�N�g����񂪌�������\���̂��閾���I�ϊ����g�p���Ďw�肳�ꂽ�^�ɕϊ����܂��B</summary>
		/// <param name="obj">�^��ϊ�����I�u�W�F�N�g���w�肵�܂��B</param>
		public T ExplicitConvertTo<T>(object obj) { return _ops.ExplicitConvertTo<T>(obj); }

		/// <summary>�I�u�W�F�N�g����񂪌�������\���̂��閾���I�ϊ����g�p���Ďw�肳�ꂽ�^�ɕϊ����܂��B</summary>
		/// <param name="obj">�^��ϊ�����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="type">�ϊ����ʂƂȂ�^���w�肵�܂��B</param>
		public object ExplicitConvertTo(object obj, Type type)
		{
			ContractUtils.RequiresNotNull(type, "type");
			return _ops.ExplicitConvertTo(obj, type);
		}

		/// <summary>�I�u�W�F�N�g����񂪌�������\���̂��閾���I�ϊ����g�p���Ďw�肳�ꂽ�^�ɕϊ����܂��B�ϊ������������ꍇ�� true ��Ԃ��܂��B</summary>
		/// <param name="obj">�^��ϊ�����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="result">�ϊ����ꂽ�I�u�W�F�N�g���i�[����ϐ����w�肵�܂��B</param>
		public bool TryExplicitConvertTo<T>(object obj, out T result) { return _ops.TryExplicitConvertTo<T>(obj, out result); }

		/// <summary>�I�u�W�F�N�g����񂪌�������\���̂��閾���I�ϊ����g�p���Ďw�肳�ꂽ�^�ɕϊ����܂��B�ϊ������������ꍇ�� true ��Ԃ��܂��B</summary>
		/// <param name="obj">�^��ϊ�����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="type">�ϊ����ʂƂȂ�^���w�肵�܂��B</param>
		/// <param name="result">�ϊ����ꂽ�I�u�W�F�N�g���i�[����ϐ����w�肵�܂��B</param>
		public bool TryExplicitConvertTo(object obj, Type type, out object result) { return _ops.TryExplicitConvertTo(obj, type, out result); }

		/// <summary>�I�u�W�F�N�g���w�肳�ꂽ�^�ɈÖٓI�ɕϊ����܂��B</summary>
		/// <param name="obj">�^��ϊ�����I�u�W�F�N�g���w�肵�܂��B</param>
		public T ImplicitConvertTo<T>(object obj) { return _ops.ImplicitConvertTo<T>(obj); }

		/// <summary>�I�u�W�F�N�g���w�肳�ꂽ�^�ɈÖٓI�ɕϊ����܂��B</summary>
		/// <param name="obj">�^��ϊ�����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="type">�ϊ����ʂƂȂ�^���w�肵�܂��B</param>
		public object ImplicitConvertTo(object obj, Type type)
		{
			ContractUtils.RequiresNotNull(type, "type");
			return _ops.ImplicitConvertTo(obj, type);
		}

		/// <summary>�I�u�W�F�N�g���w�肳�ꂽ�^�ɈÖٓI�ɕϊ����܂��B�ϊ������������ꍇ�� true ��Ԃ��܂��B</summary>
		/// <param name="obj">�^��ϊ�����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="result">�ϊ����ꂽ�I�u�W�F�N�g���i�[����ϐ����w�肵�܂��B</param>
		public bool TryImplicitConvertTo<T>(object obj, out T result) { return _ops.TryImplicitConvertTo<T>(obj, out result); }

		/// <summary>�I�u�W�F�N�g���w�肳�ꂽ�^�ɈÖٓI�ɕϊ����܂��B�ϊ������������ꍇ�� true ��Ԃ��܂��B</summary>
		/// <param name="obj">�^��ϊ�����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="type">�ϊ����ʂƂȂ�^���w�肵�܂��B</param>
		/// <param name="result">�ϊ����ꂽ�I�u�W�F�N�g���i�[����ϐ����w�肵�܂��B</param>
		public bool TryImplicitConvertTo(object obj, Type type, out object result) { return _ops.TryImplicitConvertTo(obj, type, out result); }

		/// <summary>�ėp�̒P�����Z���w�肳�ꂽ�Ώۂɑ΂��Ď��s���܂��B</summary>
		/// <param name="operation">�P�����Z�̎�ނ����� <see cref="System.Linq.Expressions.ExpressionType"/> ���w�肵�܂��B</param>
		/// <param name="target">�P�����Z����p������Ώۂ��w�肵�܂��B</param>
		public dynamic DoOperation(ExpressionType operation, object target) { return _ops.DoOperation<object, object>(operation, target); }

		/// <summary>�ėp�̒P�����Z�������Ɍ^�w�肳�ꂽ�Ώۂɑ΂��Ď��s���܂��B</summary>
		/// <param name="operation">�P�����Z�̎�ނ����� <see cref="System.Linq.Expressions.ExpressionType"/> ���w�肵�܂��B</param>
		/// <param name="target">�P�����Z����p������Ώۂ��w�肵�܂��B</param>
		public TResult DoOperation<TTarget, TResult>(ExpressionType operation, TTarget target) { return _ops.DoOperation<TTarget, TResult>(operation, target); }

		/// <summary>�ėp�̓񍀉��Z���w�肳�ꂽ�Ώۂɑ΂��Ď��s���܂��B</summary>
		/// <param name="operation">�񍀉��Z�̎�ނ����� <see cref="System.Linq.Expressions.ExpressionType"/> ���w�肵�܂��B</param>
		/// <param name="target">�񍀉��Z����p�����鍶���̑Ώۂ��w�肵�܂��B</param>
		/// <param name="other">�񍀉��Z����p������E���̑Ώۂ��w�肵�܂��B</param>
		public dynamic DoOperation(ExpressionType operation, object target, object other) { return _ops.DoOperation<object, object, object>(operation, target, other); }

		/// <summary>�ėp�̓񍀉��Z�������Ɍ^�w�肳�ꂽ�Ώۂɑ΂��Ď��s���܂��B</summary>
		/// <param name="operation">�񍀉��Z�̎�ނ����� <see cref="System.Linq.Expressions.ExpressionType"/> ���w�肵�܂��B</param>
		/// <param name="target">�񍀉��Z����p�����鍶���̑Ώۂ��w�肵�܂��B</param>
		/// <param name="other">�񍀉��Z����p������E���̑Ώۂ��w�肵�܂��B</param>
		public TResult DoOperation<TTarget, TOther, TResult>(ExpressionType operation, TTarget target, TOther other) { return _ops.DoOperation<TTarget, TOther, TResult>(operation, target, other); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�ɑ΂��ĉ��Z�����s���܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">��������w�肵�܂��B</param>
		/// <param name="other">�������w�肵�܂��B</param>
		public dynamic Add(object self, object other) { return DoOperation(ExpressionType.Add, self, other); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�ɑ΂��Č��Z�����s���܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">�팸�����w�肵�܂��B</param>
		/// <param name="other">�������w�肵�܂��B</param>
		public dynamic Subtract(object self, object other) { return DoOperation(ExpressionType.Subtract, self, other); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�ɑ΂��ėݏ�����s���܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">����w�肵�܂��B</param>
		/// <param name="other">�w�����w�肵�܂��B</param>
		public dynamic Power(object self, object other) { return DoOperation(ExpressionType.Power, self, other); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�ɑ΂��ď�Z�����s���܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">��搔���w�肵�܂��B</param>
		/// <param name="other">�搔���w�肵�܂��B</param>
		public dynamic Multiply(object self, object other) { return DoOperation(ExpressionType.Multiply, self, other); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�ɑ΂��ď��Z�����s���܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">�폜�����w�肵�܂��B</param>
		/// <param name="other">�������w�肵�܂��B</param>
		public dynamic Divide(object self, object other) { return DoOperation(ExpressionType.Divide, self, other); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�ɑ΂����]���擾���܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">�폜�����w�肵�܂��B</param>
		/// <param name="other">�������w�肵�܂��B</param>
		public dynamic Modulo(object self, object other) { return DoOperation(ExpressionType.Modulo, self, other); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�ɑ΂��č��V�t�g�����s���܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">���V�t�g����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="other">�V�t�g�ʂ��w�肵�܂��B</param>
		public dynamic LeftShift(object self, object other) { return DoOperation(ExpressionType.LeftShift, self, other); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�ɑ΂��ĉE�V�t�g�����s���܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">�E�V�t�g����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="other">�V�t�g�ʂ��w�肵�܂��B</param>
		public dynamic RightShift(object self, object other) { return DoOperation(ExpressionType.RightShift, self, other); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�ɑ΂���r�b�g�ς��擾���܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">�r�b�g�ς��擾���� 1 �Ԗڂ̃I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="other">�r�b�g�ς��擾���� 2 �Ԗڂ̃I�u�W�F�N�g���w�肵�܂��B</param>
		public dynamic BitwiseAnd(object self, object other) { return DoOperation(ExpressionType.And, self, other); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�ɑ΂���r�b�g�a���擾���܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">�r�b�g�a���擾���� 1 �Ԗڂ̃I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="other">�r�b�g�a���擾���� 2 �Ԗڂ̃I�u�W�F�N�g���w�肵�܂��B</param>
		public dynamic BitwiseOr(object self, object other) { return DoOperation(ExpressionType.Or, self, other); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�ɑ΂���r���I�_���a���擾���܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">�r���I�_���a���擾���� 1 �Ԗڂ̃I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="other">�r���I�_���a���擾���� 2 �Ԗڂ̃I�u�W�F�N�g���w�肵�܂��B</param>
		public dynamic ExclusiveOr(object self, object other) { return DoOperation(ExpressionType.ExclusiveOr, self, other); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g���r���āA�����̃I�u�W�F�N�g���E���̃I�u�W�F�N�g�����̂Ƃ��� true ��Ԃ��܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">��r���鍶���̃I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="other">��r����E���̃I�u�W�F�N�g���w�肵�܂��B</param>
		public bool LessThan(object self, object other) { return ConvertTo<bool>(DoOperation<object, object, object>(ExpressionType.LessThan, self, other)); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g���r���āA�����̃I�u�W�F�N�g���E���̃I�u�W�F�N�g�����傫���Ƃ��� true ��Ԃ��܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">��r���鍶���̃I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="other">��r����E���̃I�u�W�F�N�g���w�肵�܂��B</param>
		public bool GreaterThan(object self, object other) { return ConvertTo<bool>(DoOperation<object, object, object>(ExpressionType.GreaterThan, self, other)); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g���r���āA�����̃I�u�W�F�N�g���E���̃I�u�W�F�N�g�ȉ��̂Ƃ��� true ��Ԃ��܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">��r���鍶���̃I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="other">��r����E���̃I�u�W�F�N�g���w�肵�܂��B</param>
		public bool LessThanOrEqual(object self, object other) { return ConvertTo<bool>(DoOperation<object, object, object>(ExpressionType.LessThanOrEqual, self, other)); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g���r���āA�����̃I�u�W�F�N�g���E���̃I�u�W�F�N�g�ȏ�̂Ƃ��� true ��Ԃ��܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">��r���鍶���̃I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="other">��r����E���̃I�u�W�F�N�g���w�肵�܂��B</param>
		public bool GreaterThanOrEqual(object self, object other) { return ConvertTo<bool>(DoOperation<object, object, object>(ExpressionType.GreaterThanOrEqual, self, other)); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g���r���āA�����̃I�u�W�F�N�g���E���̃I�u�W�F�N�g�Ɠ������Ƃ��� true ��Ԃ��܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">��r���鍶���̃I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="other">��r����E���̃I�u�W�F�N�g���w�肵�܂��B</param>
		public bool Equal(object self, object other) { return ConvertTo<bool>(DoOperation<object, object, object>(ExpressionType.Equal, self, other)); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g���r���āA�����̃I�u�W�F�N�g���E���̃I�u�W�F�N�g�Ɠ������Ȃ��Ƃ��� true ��Ԃ��܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">��r���鍶���̃I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="other">��r����E���̃I�u�W�F�N�g���w�肵�܂��B</param>
		public bool NotEqual(object self, object other) { return ConvertTo<bool>(DoOperation<object, object, object>(ExpressionType.NotEqual, self, other)); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�̕�����\��������ŗL�̕\���`���ŕԂ��܂��B</summary>
		/// <param name="obj">������\�����擾����I�u�W�F�N�g���w�肵�܂��B</param>
		public string Format(object obj) { return _ops.Format(obj); }

		/// <summary>�I�u�W�F�N�g�̊��m�̃����o�̈ꗗ��Ԃ��܂��B</summary>
		/// <param name="obj">�����o�̈ꗗ���擾����I�u�W�F�N�g���w�肵�܂��B</param>
		public IList<string> GetMemberNames(object obj) { return _ops.GetMemberNames(obj); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�ɑ΂��镶����Œ񋟂����h�L�������g��Ԃ��܂��B</summary>
		/// <param name="obj">�h�L�������g���擾����I�u�W�F�N�g���w�肵�܂��B</param>
		public string GetDocumentation(object obj) { return _ops.GetDocumentation(obj); }

		/// <summary>���[�U�[�ɑ΂���\���`���̎w�肳�ꂽ�I�u�W�F�N�g�̌Ăяo���ɑ΂��ēK�p�����V�O�l�`���̃��X�g��Ԃ��܂��B</summary>
		/// <param name="obj">�V�O�l�`���̃��X�g���擾����I�u�W�F�N�g���w�肵�܂��B</param>
		public IList<string> GetCallSignatures(object obj) { return _ops.GetCallSignatures(obj); 	}

		#endregion

		#region Remote APIs

		/// <summary>�w�肳�ꂽ�����[�g�I�u�W�F�N�g���Ăяo���\���ǂ����������l���擾���܂��B</summary>
		/// <param name="obj">�Ăяo���\���ǂ����𒲂ׂ郊���[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		public bool IsCallable([NotNull]ObjectHandle obj) { return IsCallable(GetLocalObject(obj)); }

		/// <summary>�w�肳�ꂽ�����[�g�I�u�W�F�N�g�������[�g�I�u�W�F�N�g�ŕ\�����ꂽ�w�肳�ꂽ�����ɂ���ČĂяo���܂��B</summary>
		/// <param name="obj">�Ăяo�������[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="parameters">�I�u�W�F�N�g�Ăяo���̃����[�g�I�u�W�F�N�g�ŕ\�����ꂽ�������w�肵�܂��B</param>
		public ObjectHandle Invoke([NotNull]ObjectHandle obj, params ObjectHandle[] parameters)
		{
			ContractUtils.RequiresNotNull(parameters, "parameters");
			return new ObjectHandle((object)Invoke(GetLocalObject(obj), GetLocalObjects(parameters)));
		}

		/// <summary>�w�肳�ꂽ�����[�g�I�u�W�F�N�g���w�肳�ꂽ�����ɂ���ČĂяo���܂��B</summary>
		/// <param name="obj">�Ăяo�������[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="parameters">�I�u�W�F�N�g�Ăяo���̈������w�肵�܂��B</param>
		public ObjectHandle Invoke([NotNull]ObjectHandle obj, params object[] parameters) { return new ObjectHandle((object)Invoke(GetLocalObject(obj), parameters)); }

		/// <summary>�w�肳�ꂽ�����[�g�I�u�W�F�N�g�Ƀ����[�g�I�u�W�F�N�g�ŕ\�����ꂽ�w�肳�ꂽ�������g�p���ĐV�����C���X�^���X���쐬���܂��B</summary>
		/// <param name="obj">�C���X�^���X���쐬�����ɂȂ郊���[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="parameters">�C���X�^���X�̍쐬�̍ۂɕK�v�ɂȂ郊���[�g�I�u�W�F�N�g�ŕ\�����ꂽ�������w�肵�܂��B</param>
		public ObjectHandle CreateInstance([NotNull]ObjectHandle obj, [NotNull]params ObjectHandle[] parameters) { return new ObjectHandle((object)CreateInstance(GetLocalObject(obj), GetLocalObjects(parameters))); }

		/// <summary>�w�肳�ꂽ�����[�g�I�u�W�F�N�g�Ɏw�肳�ꂽ�������g�p���ĐV�����C���X�^���X���쐬���܂��B</summary>
		/// <param name="obj">�C���X�^���X���쐬�����ɂȂ郊���[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="parameters">�C���X�^���X�̍쐬�̍ۂɕK�v�ɂȂ�������w�肵�܂��B</param>
		public ObjectHandle CreateInstance([NotNull]ObjectHandle obj, params object[] parameters) { return new ObjectHandle((object)CreateInstance(GetLocalObject(obj), parameters)); }

		/// <summary>�����[�g�I�u�W�F�N�g�̎w�肳�ꂽ�����o�Ƀ����[�g�I�u�W�F�N�g�ɂ���Ďw�肳�ꂽ�l��ݒ肵�܂��B</summary>
		/// <param name="obj">�ݒ肷�郁���o��ێ����Ă��郊���[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�ݒ肷�郁���o�̖��O���w�肵�܂��B</param>
		/// <param name="value">�����o�ɐݒ肷�郊���[�g�I�u�W�F�N�g�ŕ\�����ꂽ�l���w�肵�܂��B</param>
		public void SetMember([NotNull]ObjectHandle obj, string name, [NotNull]ObjectHandle value) { SetMember(GetLocalObject(obj), name, GetLocalObject(value)); }

		/// <summary>�����[�g�I�u�W�F�N�g�̎w�肳�ꂽ�����o�Ɏw�肳�ꂽ�l��ݒ肵�܂��B���̃I�[�o�[���[�h�͌����Ɍ^�w�肳��Ă��邽�߁A�{�b�N�X����L���X�g������邱�Ƃ��ł��܂��B</summary>
		/// <param name="obj">�ݒ肷�郁���o��ێ����Ă��郊���[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�ݒ肷�郁���o�̖��O���w�肵�܂��B</param>
		/// <param name="value">�����o�ɐݒ肷��l���w�肵�܂��B</param>
		public void SetMember<T>([NotNull]ObjectHandle obj, string name, T value) { SetMember<T>(GetLocalObject(obj), name, value); }

		/// <summary>�����[�g�I�u�W�F�N�g�̎w�肳�ꂽ�����o���擾���܂��B�����o�����݂��Ȃ����A�������ݐ�p�̏ꍇ�͗�O�𔭐������܂��B</summary>
		/// <param name="obj">�擾���郁���o��ێ����Ă��郊���[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�擾���郁���o�̖��O���w�肵�܂��B</param>
		public ObjectHandle GetMember([NotNull]ObjectHandle obj, string name) { return new ObjectHandle((object)GetMember(GetLocalObject(obj), name)); }

		/// <summary>�����[�g�I�u�W�F�N�g�̎w�肳�ꂽ�����o���擾���A���ʂ��w�肳�ꂽ�^�ɕϊ����܂��B�����o�����݂��Ȃ����A�������ݐ�p�̏ꍇ�͗�O�𔭐������܂��B</summary>
		/// <param name="obj">�擾���郁���o��ێ����Ă��郊���[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�擾���郁���o�̖��O���w�肵�܂��B</param>
		public T GetMember<T>([NotNull]ObjectHandle obj, string name) { return GetMember<T>(GetLocalObject(obj), name); }

		/// <summary>�����[�g�I�u�W�F�N�g�̎w�肳�ꂽ�����o���擾���܂��B�����o������Ɏ擾���ꂽ�ꍇ�� true ��Ԃ��܂��B</summary>
		/// <param name="obj">�擾���郁���o��ێ����Ă��郊���[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�擾���郁���o�̖��O���w�肵�܂��B</param>
		/// <param name="value">�擾���������o�̒l���i�[����ϐ����w�肵�܂��B</param>
		public bool TryGetMember([NotNull]ObjectHandle obj, string name, out ObjectHandle value)
		{
			object val;
			if (TryGetMember(GetLocalObject(obj), name, out val))
			{
				value = new ObjectHandle(val);
				return true;
			}
			value = null;
			return false;
		}

		/// <summary>�����[�g�I�u�W�F�N�g�Ɏw�肳�ꂽ�����o�����݂��邩�ǂ����������l��Ԃ��܂��B</summary>
		/// <param name="obj">�����o�����݂��邩�ǂ����𒲂ׂ郊���[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">���݂��邩�ǂ����𒲂ׂ郁���o�̖��O���w�肵�܂��B</param>
		public bool ContainsMember([NotNull]ObjectHandle obj, string name) { return ContainsMember(GetLocalObject(obj), name); }

		/// <summary>�����[�g�I�u�W�F�N�g����w�肳�ꂽ�����o���폜���܂��B</summary>
		/// <param name="obj">�����o���폜���郊���[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="name">�폜���郁���o�̖��O���w�肵�܂��B</param>
		public void RemoveMember([NotNull]ObjectHandle obj, string name) { RemoveMember(GetLocalObject(obj), name); }

		/// <summary>�����[�g�I�u�W�F�N�g���w�肳�ꂽ�^�ɕϊ����܂��B�ϊ��������I�ɍs���邩�ǂ����͌���d�l�ɂ���Č��肳��܂��B</summary>
		/// <param name="obj">�^��ϊ����郊���[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="type">�ϊ����ʂƂȂ�^���w�肵�܂��B</param>
		public ObjectHandle ConvertTo([NotNull]ObjectHandle obj, Type type) { return new ObjectHandle(ConvertTo(GetLocalObject(obj), type)); }

		/// <summary>�����[�g�I�u�W�F�N�g���w�肳�ꂽ�^�ɕϊ����܂��B�ϊ������������ꍇ�� true ��Ԃ��܂��B�ϊ��������I�ɍs���邩�ǂ����͌���d�l�ɂ���Č��肳��܂��B</summary>
		/// <param name="obj">�^��ϊ����郊���[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="type">�ϊ����ʂƂȂ�^���w�肵�܂��B</param>
		/// <param name="result">�ϊ����ꂽ�����[�g�I�u�W�F�N�g���i�[����ϐ����w�肵�܂��B</param>
		public bool TryConvertTo([NotNull]ObjectHandle obj, Type type, out ObjectHandle result)
		{
			object resultObj;
			if (TryConvertTo(GetLocalObject(obj), type, out resultObj))
			{
				result = new ObjectHandle(resultObj);
				return true;
			}
			result = null;
			return false;
		}

		/// <summary>�����[�g�I�u�W�F�N�g����񂪌�������\���̂��閾���I�ϊ����g�p���Ďw�肳�ꂽ�^�ɕϊ����܂��B</summary>
		/// <param name="obj">�^��ϊ����郊���[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="type">�ϊ����ʂƂȂ�^���w�肵�܂��B</param>
		public ObjectHandle ExplicitConvertTo([NotNull]ObjectHandle obj, Type type)
		{
			ContractUtils.RequiresNotNull(type, "type");
			return new ObjectHandle(_ops.ExplicitConvertTo(GetLocalObject(obj), type));
		}

		/// <summary>�����[�g�I�u�W�F�N�g����񂪌�������\���̂��閾���I�ϊ����g�p���Ďw�肳�ꂽ�^�ɕϊ����܂��B�ϊ������������ꍇ�� true ��Ԃ��܂��B</summary>
		/// <param name="obj">�^��ϊ����郊���[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="type">�ϊ����ʂƂȂ�^���w�肵�܂��B</param>
		/// <param name="result">�ϊ����ꂽ�����[�g�I�u�W�F�N�g���i�[����ϐ����w�肵�܂��B</param>
		public bool TryExplicitConvertTo([NotNull]ObjectHandle obj, Type type, out ObjectHandle result)
		{
			object outp;
			bool res = _ops.TryExplicitConvertTo(GetLocalObject(obj), type, out outp);
			if (res)
				result = new ObjectHandle(outp);
			else
				result = null;
			return res;
		}

		/// <summary>�����[�g�I�u�W�F�N�g���w�肳�ꂽ�^�ɈÖٓI�ɕϊ����܂��B</summary>
		/// <param name="obj">�^��ϊ����郊���[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="type">�ϊ����ʂƂȂ�^���w�肵�܂��B</param>
		public ObjectHandle ImplicitConvertTo([NotNull]ObjectHandle obj, Type type)
		{
			ContractUtils.RequiresNotNull(type, "type");
			return new ObjectHandle(_ops.ImplicitConvertTo(GetLocalObject(obj), type));
		}

		/// <summary>�����[�g�I�u�W�F�N�g���w�肳�ꂽ�^�ɈÖٓI�ɕϊ����܂��B�ϊ������������ꍇ�� true ��Ԃ��܂��B</summary>
		/// <param name="obj">�^��ϊ����郊���[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="type">�ϊ����ʂƂȂ�^���w�肵�܂��B</param>
		/// <param name="result">�ϊ����ꂽ�����[�g�I�u�W�F�N�g���i�[����ϐ����w�肵�܂��B</param>
		public bool TryImplicitConvertTo([NotNull]ObjectHandle obj, Type type, out ObjectHandle result)
		{
			object outp;
			bool res = _ops.TryImplicitConvertTo(GetLocalObject(obj), type, out outp);
			if (res)
				result = new ObjectHandle(outp);
			else
				result = null;
			return res;
		}

		/// <summary>�����[�g�I�u�W�F�N�g�̃��b�s���O���������A�w�肳�ꂽ�^�ɕϊ����܂��B</summary>
		/// <param name="obj">���b�s���O���������郊���[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		public T Unwrap<T>([NotNull]ObjectHandle obj) { return ConvertTo<T>(GetLocalObject(obj)); }

		/// <summary>�ėp�̒P�����Z���w�肳�ꂽ�Ώۂɑ΂��Ď��s���܂��B</summary>
		/// <param name="op">�P�����Z�̎�ނ����� <see cref="System.Linq.Expressions.ExpressionType"/> ���w�肵�܂��B</param>
		/// <param name="target">�P�����Z����p������Ώۂ��w�肵�܂��B</param>
		public ObjectHandle DoOperation(ExpressionType op, [NotNull]ObjectHandle target) { return new ObjectHandle((object)DoOperation(op, GetLocalObject(target))); }

		/// <summary>�ėp�̓񍀉��Z���w�肳�ꂽ�Ώۂɑ΂��Ď��s���܂��B</summary>
		/// <param name="op">�񍀉��Z�̎�ނ����� <see cref="System.Linq.Expressions.ExpressionType"/> ���w�肵�܂��B</param>
		/// <param name="target">�񍀉��Z����p�����鍶���̑Ώۂ��w�肵�܂��B</param>
		/// <param name="other">�񍀉��Z����p������E���̑Ώۂ��w�肵�܂��B</param>
		public ObjectHandle DoOperation(ExpressionType op, ObjectHandle target, ObjectHandle other) { return new ObjectHandle((object)DoOperation(op, GetLocalObject(target), GetLocalObject(other))); }

		/// <summary>�w�肳�ꂽ�����[�g�I�u�W�F�N�g�ɑ΂��ĉ��Z�����s���܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">��������w�肵�܂��B</param>
		/// <param name="other">�������w�肵�܂��B</param>
		public ObjectHandle Add([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return new ObjectHandle((object)Add(GetLocalObject(self), GetLocalObject(other))); }

		/// <summary>�w�肳�ꂽ�����[�g�I�u�W�F�N�g�ɑ΂��Č��Z�����s���܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">�팸�����w�肵�܂��B</param>
		/// <param name="other">�������w�肵�܂��B</param>
		public ObjectHandle Subtract([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return new ObjectHandle((object)Subtract(GetLocalObject(self), GetLocalObject(other))); }

		/// <summary>�w�肳�ꂽ�����[�g�I�u�W�F�N�g�ɑ΂��ėݏ�����s���܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">����w�肵�܂��B</param>
		/// <param name="other">�w�����w�肵�܂��B</param>
		public ObjectHandle Power([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return new ObjectHandle((object)Power(GetLocalObject(self), GetLocalObject(other))); }

		/// <summary>�w�肳�ꂽ�����[�g�I�u�W�F�N�g�ɑ΂��ď�Z�����s���܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">��搔���w�肵�܂��B</param>
		/// <param name="other">�搔���w�肵�܂��B</param>
		public ObjectHandle Multiply([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return new ObjectHandle((object)Multiply(GetLocalObject(self), GetLocalObject(other))); }

		/// <summary>�w�肳�ꂽ�����[�g�I�u�W�F�N�g�ɑ΂��ď��Z�����s���܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">�폜�����w�肵�܂��B</param>
		/// <param name="other">�������w�肵�܂��B</param>
		public ObjectHandle Divide([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return new ObjectHandle((object)Divide(GetLocalObject(self), GetLocalObject(other))); }

		/// <summary>�w�肳�ꂽ�����[�g�I�u�W�F�N�g�ɑ΂����]���擾���܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">�폜�����w�肵�܂��B</param>
		/// <param name="other">�������w�肵�܂��B</param>      
		public ObjectHandle Modulo([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return new ObjectHandle((object)Modulo(GetLocalObject(self), GetLocalObject(other))); }

		/// <summary>�w�肳�ꂽ�����[�g�I�u�W�F�N�g�ɑ΂��č��V�t�g�����s���܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">���V�t�g���郊���[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="other">�V�t�g�ʂ��w�肵�܂��B</param>
		public ObjectHandle LeftShift([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return new ObjectHandle((object)LeftShift(GetLocalObject(self), GetLocalObject(other))); }

		/// <summary>�w�肳�ꂽ�����[�g�I�u�W�F�N�g�ɑ΂��ĉE�V�t�g�����s���܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">�E�V�t�g���郊���[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="other">�V�t�g�ʂ��w�肵�܂��B</param>
		public ObjectHandle RightShift([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return new ObjectHandle((object)RightShift(GetLocalObject(self), GetLocalObject(other))); }

		/// <summary>�w�肳�ꂽ�����[�g�I�u�W�F�N�g�ɑ΂���r�b�g�ς��擾���܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">�r�b�g�ς��擾���� 1 �Ԗڂ̃����[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="other">�r�b�g�ς��擾���� 2 �Ԗڂ̃����[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		public ObjectHandle BitwiseAnd([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return new ObjectHandle((object)BitwiseAnd(GetLocalObject(self), GetLocalObject(other))); }

		/// <summary>�w�肳�ꂽ�����[�g�I�u�W�F�N�g�ɑ΂���r�b�g�a���擾���܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">�r�b�g�a���擾���� 1 �Ԗڂ̃����[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="other">�r�b�g�a���擾���� 2 �Ԗڂ̃����[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		public ObjectHandle BitwiseOr([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return new ObjectHandle((object)BitwiseOr(GetLocalObject(self), GetLocalObject(other))); }

		/// <summary>�w�肳�ꂽ�����[�g�I�u�W�F�N�g�ɑ΂���r���I�_���a���擾���܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">�r���I�_���a���擾���� 1 �Ԗڂ̃����[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="other">�r���I�_���a���擾���� 2 �Ԗڂ̃����[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		public ObjectHandle ExclusiveOr([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return new ObjectHandle((object)ExclusiveOr(GetLocalObject(self), GetLocalObject(other))); }

		/// <summary>�w�肳�ꂽ�����[�g�I�u�W�F�N�g���r���āA�����̃����[�g�I�u�W�F�N�g���E���̃����[�g�I�u�W�F�N�g�����̂Ƃ��� true ��Ԃ��܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">��r���鍶���̃����[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="other">��r����E���̃����[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		public bool LessThan([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return LessThan(GetLocalObject(self), GetLocalObject(other)); }

		/// <summary>�w�肳�ꂽ�����[�g�I�u�W�F�N�g���r���āA�����̃����[�g�I�u�W�F�N�g���E���̃����[�g�I�u�W�F�N�g�����傫���Ƃ��� true ��Ԃ��܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">��r���鍶���̃����[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="other">��r����E���̃����[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		public bool GreaterThan([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return GreaterThan(GetLocalObject(self), GetLocalObject(other)); }

		/// <summary>�w�肳�ꂽ�����[�g�I�u�W�F�N�g���r���āA�����̃����[�g�I�u�W�F�N�g���E���̃����[�g�I�u�W�F�N�g�ȉ��̂Ƃ��� true ��Ԃ��܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">��r���鍶���̃����[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="other">��r����E���̃����[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		public bool LessThanOrEqual([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return LessThanOrEqual(GetLocalObject(self), GetLocalObject(other)); }

		/// <summary>�w�肳�ꂽ�����[�g�I�u�W�F�N�g���r���āA�����̃����[�g�I�u�W�F�N�g���E���̃����[�g�I�u�W�F�N�g�ȏ�̂Ƃ��� true ��Ԃ��܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">��r���鍶���̃����[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="other">��r����E���̃����[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		public bool GreaterThanOrEqual([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return GreaterThanOrEqual(GetLocalObject(self), GetLocalObject(other)); }

		/// <summary>�w�肳�ꂽ�����[�g�I�u�W�F�N�g���r���āA�����̃����[�g�I�u�W�F�N�g���E���̃����[�g�I�u�W�F�N�g�Ɠ������Ƃ��� true ��Ԃ��܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">��r���鍶���̃����[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="other">��r����E���̃����[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		public bool Equal([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return Equal(GetLocalObject(self), GetLocalObject(other)); }

		/// <summary>�w�肳�ꂽ�����[�g�I�u�W�F�N�g���r���āA�����̃����[�g�I�u�W�F�N�g���E���̃����[�g�I�u�W�F�N�g�Ɠ������Ȃ��Ƃ��� true ��Ԃ��܂��B���삪���s�ł��Ȃ��ꍇ�ɂ͗�O�𔭐������܂��B</summary>
		/// <param name="self">��r���鍶���̃����[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="other">��r����E���̃����[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		public bool NotEqual([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return NotEqual(GetLocalObject(self), GetLocalObject(other)); }

		/// <summary>�w�肳�ꂽ�����[�g�I�u�W�F�N�g�̕�����\��������ŗL�̕\���`���ŕԂ��܂��B</summary>
		/// <param name="obj">������\�����擾���郊���[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		public string Format([NotNull]ObjectHandle obj) { return Format(GetLocalObject(obj)); }

		/// <summary>�����[�g�I�u�W�F�N�g�̊��m�̃����o�̈ꗗ��Ԃ��܂��B</summary>
		/// <param name="obj">�����o�̈ꗗ���擾���郊���[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		public IList<string> GetMemberNames([NotNull]ObjectHandle obj) { return GetMemberNames(GetLocalObject(obj)); }

		/// <summary>�w�肳�ꂽ�����[�g�I�u�W�F�N�g�ɑ΂��镶����Œ񋟂����h�L�������g��Ԃ��܂��B</summary>
		/// <param name="obj">�h�L�������g���擾���郊���[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		public string GetDocumentation([NotNull]ObjectHandle obj) { return GetDocumentation(GetLocalObject(obj)); }

		/// <summary>���[�U�[�ɑ΂���\���`���̎w�肳�ꂽ�����[�g�I�u�W�F�N�g�̌Ăяo���ɑ΂��ēK�p�����V�O�l�`���̃��X�g��Ԃ��܂��B</summary>
		/// <param name="obj">�V�O�l�`���̃��X�g���擾���郊���[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		public IList<string> GetCallSignatures([NotNull]ObjectHandle obj) { return GetCallSignatures(GetLocalObject(obj)); }

		static object GetLocalObject([NotNull]ObjectHandle obj)
		{
			ContractUtils.RequiresNotNull(obj, "obj");
			return obj.Unwrap();
		}

		static object[] GetLocalObjects(ObjectHandle[] ohs)
		{
			Debug.Assert(ohs != null);
			return ohs.Select(o => GetLocalObject(o)).ToArray();
		}

		#endregion

		/// <summary>�Ώۂ̃C���X�^���X�̗L�����ԃ|���V�[�𐧌䂷��A�L�����ԃT�[�r�X �I�u�W�F�N�g���擾���܂��B</summary>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
		public override object InitializeLifetimeService() { return null; }
	}
}
