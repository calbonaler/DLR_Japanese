/* ****************************************************************************
*
* Copyright (c) Microsoft Corporation. 
*
* This source code is subject to terms and conditions of the Microsoft Public License. A 
* copy of the license can be found in the License.html file at the root of this distribution. If 
* you cannot locate the Microsoft Public License, please send an email to 
* dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
* by the terms of the Microsoft Public License.
*
* You must not remove this notice, or any other, from this software.
*
*
* ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using System.Threading;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting
{
	/// <summary>
	/// �X�R�[�v�X�g���[�W�ɑ΂���œK�����ꂽ�L���b�V���\�ȃT�|�[�g��񋟂��܂��B
	/// ���̃N���X�̓X�R�[�v���Ŋi�[����l�ɑ΂��Ďg�p��������̃I�u�W�F�N�g�ł��B
	/// </summary>
	/// <remarks>
	/// ������ <see cref="ScopeVariableIgnoreCase"/> ��ێ�����啶���Ə���������ʂ��Ȃ��f�B�N�V���i�����g�p���܂��B
	/// <see cref="ScopeVariableIgnoreCase"/> �I�u�W�F�N�g�͂��ꂼ��̉\�ȃP�[�V���O�ɑ΂��� <see cref="ScopeVariable"/> �I�u�W�F�N�g��ێ����܂��B
	/// </remarks>
	public sealed class ScopeStorage : IDynamicMetaObjectProvider
	{
		readonly Dictionary<string, ScopeVariableIgnoreCase> _storage = new Dictionary<string, ScopeVariableIgnoreCase>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// �I�v�V�����ő啶���Ə���������ʂ��Ȃ��ŃX�R�[�v���̎w�肳�ꂽ�ϐ����擾���܂��B
		/// �w�肳�ꂽ���O�����݂��Ȃ��ꍇ�� <see cref="InvalidOperationException"/> ���������܂��B
		/// </summary>
		/// <param name="name">�X�R�[�v���Ɋi�[����Ă���l�����ʂ��閼�O���w�肵�܂��B</param>
		/// <param name="ignoreCase">���O�ɂ����đ啶���Ə���������ʂ��邩�ǂ����������l���w�肵�܂��B</param>
		public dynamic GetValue(string name, bool ignoreCase)
		{
			object res;
			if (GetVariable(name, ignoreCase).TryGetValue(out res))
				return res;
			throw new KeyNotFoundException("no value");
		}

		/// <summary>
		/// �I�v�V�����ő啶���Ə���������ʂ��Ȃ��ŃX�R�[�v���̎w�肳�ꂽ�ϐ��̎擾�����݂܂��B
		/// �����l�����݂����ꍇ�� <c>true</c>�A����ȊO�̏ꍇ�� <c>false</c> ��Ԃ��܂��B
		/// </summary>
		/// <param name="name">�X�R�[�v���Ɋi�[����Ă���l�����ʂ��閼�O���w�肵�܂��B</param>
		/// <param name="ignoreCase">���O�ɂ����đ啶���Ə���������ʂ��邩�ǂ����������l���w�肵�܂��B</param>
		/// <param name="value">�擾�����l���i�[����ϐ����w�肵�܂��B</param>
		public bool TryGetValue(string name, bool ignoreCase, out dynamic value)
		{
			if (HasVariable(name))
			{
				object objValue;
				if (GetVariable(name, ignoreCase).TryGetValue(out objValue))
				{
					value = objValue;
					return true;
				}
			}
			value = null;
			return false;
		}

		/// <summary>�I�v�V�����ő啶���Ə���������ʂ��Ȃ��ŃX�R�[�v�Ɏw�肳�ꂽ�l��ݒ肵�܂��B</summary>
		/// <param name="name">�X�R�[�v�Ɋi�[�����l�����ʂ��閼�O���w�肵�܂��B</param>
		/// <param name="ignoreCase">���O�ɂ����đ啶���Ə���������ʂ��邩�ǂ����������l���w�肵�܂��B</param>
		/// <param name="value">�X�R�[�v�ɐݒ肷��l���w�肵�܂��B</param>
		public void SetValue(string name, bool ignoreCase, object value) { GetVariable(name, ignoreCase).SetValue(value); }

		/// <summary>�I�v�V�����ő啶���Ə���������ʂ��Ȃ��ŃX�R�[�v����w�肳�ꂽ�l���폜���܂��B�폜�����������ꍇ�� <c>true</c> ��Ԃ��܂��B</summary>
		/// <param name="name">�X�R�[�v�Ŋi�[�����l�����ʂ��閼�O���w�肵�܂��B</param>
		/// <param name="ignoreCase">���O�ɂ����đ啶���Ə���������ʂ��邩�ǂ����������l���w�肵�܂��B</param>
		public bool DeleteValue(string name, bool ignoreCase)
		{
			if (!HasVariable(name))
				return false;
			return GetVariable(name, ignoreCase).DeleteValue();
		}

		/// <summary>�I�v�V�����ő啶���Ə���������ʂ��Ȃ��ŃX�R�[�v�Ɏw�肳�ꂽ�l���܂܂�Ă��邩�ǂ����𒲂ׂ܂��B</summary>
		/// <param name="name">�X�R�[�v�Ɋi�[����Ă��邩�ǂ����𒲂ׂ閼�O���w�肵�܂��B</param>
		/// <param name="ignoreCase">���O�ɂ����đ啶���Ə���������ʂ��邩�ǂ����������l���w�肵�܂��B</param>
		public bool HasValue(string name, bool ignoreCase)
		{
			if (!HasVariable(name))
				return false;
			return GetVariable(name, ignoreCase).HasValue;
		}

		/// <summary>
		/// �I�v�V�����ő啶���Ə���������ʂ��Ȃ��ŃX�R�[�v�ɑ΂��� <see cref="IScopeVariable"/> ���擾���܂��B
		/// <see cref="IScopeVariable"/> �͌㑱�̃A�N�Z�X�Ŏ������������s�����Ɏ擾�E�ݒ�E�폜���s���܂��B
		/// </summary>
		/// <param name="name">�X�R�[�v�Ɋi�[����Ă��邩�ǂ����𒲂ׂ閼�O���w�肵�܂��B</param>
		/// <param name="ignoreCase">���O�ɂ����đ啶���Ə���������ʂ��邩�ǂ����������l���w�肵�܂��B</param>
		public IScopeVariable GetVariable(string name, bool ignoreCase)
		{
			if (ignoreCase)
				return GetVariableIgnoreCase(name);
			return GetVariable(name);
		}

		/// <summary>
		/// �啶���Ə���������ʂ���X�R�[�v�ɑ΂��� <see cref="ScopeVariable"/> ���擾���܂��B
		/// <see cref="ScopeVariable"/> �͌㑱�̃A�N�Z�X�Ŏ������������s�����Ɏ擾�E�ݒ�E�폜���s���܂��B
		/// </summary>
		/// <param name="name">�擾���� <see cref="ScopeVariable"/> �ɑ΂��閼�O���w�肵�܂��B</param>
		public ScopeVariable GetVariable(string name) { return GetVariableIgnoreCase(name).GetCaseSensitiveStorage(name); }

		/// <summary>
		/// �啶���Ə���������ʂ��Ȃ��X�R�[�v�ɑ΂��� <see cref="ScopeVariableIgnoreCase"/> ���擾���܂��B
		/// <see cref="ScopeVariableIgnoreCase"/> �͌㑱�̃A�N�Z�X�Ŏ������������s�����Ɏ擾�E�ݒ�E�폜���s���܂��B
		/// </summary>
		/// <param name="name">�擾���� <see cref="ScopeVariableIgnoreCase"/> �ɑ΂��閼�O���w�肵�܂��B</param>
		public ScopeVariableIgnoreCase GetVariableIgnoreCase(string name)
		{
			ScopeVariableIgnoreCase storageInfo;
			lock (_storage)
			{
				if (!_storage.TryGetValue(name, out storageInfo))
					_storage[name] = storageInfo = new ScopeVariableIgnoreCase(name);
				return storageInfo;
			}
		}

		/// <summary>�啶���Ə���������ʂ��Ȃ��ŃX�R�[�v���Ŏw�肳�ꂽ�ϐ����擾�܂��͐ݒ肵�܂��B</summary>
		public dynamic this[string index]
		{
			get { return GetValue(index, false); }
			set { SetValue(index, false, (object)value); }
		}

		/// <summary>
		/// ���̃X�R�[�v�ɂ����Ēl�����邷�ׂẴ����o�̖��O��Ԃ��܂��B
		/// �Ԃ���閼�O�ɂ͂��ׂĂ̗��p�\�ȃP�[�V���O���܂܂�܂��B
		/// </summary>
		public IList<string> GetMemberNames()
		{
			List<string> res = new List<string>();
			lock (_storage)
			{
				foreach (var storage in _storage.Values)
					storage.AddNames(res);
			}
			return res;
		}

		/// <summary>
		/// ���̃X�R�[�v�ɂ����Ă��ׂẴ����o������т����Ɋ֘A�t����ꂽ�l��Ԃ��܂��B
		/// �Ԃ���閼�O�ɂ͂��ׂĂ̗��p�\�ȃP�[�V���O���܂܂�܂��B
		/// </summary>
		public IList<KeyValuePair<string, object>> GetItems()
		{
			List<KeyValuePair<string, object>> res = new List<KeyValuePair<string, object>>();
			lock (_storage)
			{
				foreach (var storage in _storage.Values)
					storage.AddItems(res);
			}
			return res;
		}

		bool HasVariable(string name) { lock (_storage) return _storage.ContainsKey(name); }

		#region IDynamicMetaObjectProvider Members

		/// <summary>���̃I�u�W�F�N�g�ɑ΂��Ď��s����鑀����o�C���h���� <see cref="System.Dynamic.DynamicMetaObject"/> ��Ԃ��܂��B</summary>
		/// <param name="parameter">�����^�C���l�̎��c���[�\���B</param>
		/// <returns>���̃I�u�W�F�N�g���o�C���h���� <see cref="System.Dynamic.DynamicMetaObject"/>�B</returns>
		public DynamicMetaObject GetMetaObject(Expression parameter) { return new Meta(parameter, this); }

		class Meta : DynamicMetaObject
		{
			public Meta(Expression parameter, ScopeStorage storage) : base(parameter, BindingRestrictions.Empty, storage) { }

			public override DynamicMetaObject BindGetMember(GetMemberBinder binder) { return DynamicTryGetValue(binder.Name, binder.IgnoreCase, binder.FallbackGetMember(this).Expression, _ => _); }

			public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
			{
				return DynamicTryGetValue(binder.Name, binder.IgnoreCase,
					binder.FallbackInvokeMember(this, args).Expression,
					x => binder.FallbackInvoke(new DynamicMetaObject(x, BindingRestrictions.Empty), args, null).Expression
				);
			}

			DynamicMetaObject DynamicTryGetValue(string name, bool ignoreCase, Expression fallback, Func<Expression, Expression> resultOp)
			{
				IScopeVariable variable = Value.GetVariable(name, ignoreCase);
				var tmp = Expression.Parameter(typeof(object));
				return new DynamicMetaObject(
					Expression.Block(
						new[] { tmp },
						Expression.Condition(
							Expression.Call(Variable(variable), variable.GetType().GetMethod("TryGetValue"), tmp),
							ExpressionUtils.Convert(resultOp(tmp), typeof(object)),
							ExpressionUtils.Convert(fallback, typeof(object))
						)
					),
					BindingRestrictions.GetInstanceRestriction(Expression, Value)
				);
			}

			static Expression Variable(IScopeVariable variable)
			{
				return Expression.Convert(
					Expression.Property(Expression.Constant(((IWeakReferencable)variable).WeakReference), typeof(WeakReference).GetProperty("Target")),
					variable.GetType()
				);
			}

			public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
			{
				IScopeVariable variable = Value.GetVariable(binder.Name, binder.IgnoreCase);
				var objExpression = ExpressionUtils.Convert(value.Expression, typeof(object));
				return new DynamicMetaObject(
					Expression.Block(
						Expression.Call(Variable(variable), variable.GetType().GetMethod("SetValue"), objExpression),
						objExpression
					),
					BindingRestrictions.GetInstanceRestriction(Expression, Value)
				);
			}

			public override DynamicMetaObject BindDeleteMember(DeleteMemberBinder binder)
			{
				IScopeVariable variable = Value.GetVariable(binder.Name, binder.IgnoreCase);
				return new DynamicMetaObject(
					Expression.Condition(
						Expression.Call(Variable(variable), variable.GetType().GetMethod("DeleteValue")),
						Expression.Default(binder.ReturnType),
						binder.FallbackDeleteMember(this).Expression
					),
					BindingRestrictions.GetInstanceRestriction(Expression, Value)
				);
			}

			public override IEnumerable<string> GetDynamicMemberNames() { return Value.GetMemberNames(); }

			public new ScopeStorage Value { get { return (ScopeStorage)base.Value; } }
		}

		#endregion
	}

	/// <summary>�ϐ��X�g���[�W�ɑ΂��鋤�ʂ̃C���^�[�t�F�C�X��񋟂��܂��B</summary>
	public interface IScopeVariable
	{
		/// <summary>�X�R�[�v�ɒl�����݂��邩�ǂ����������l���擾���܂��B</summary>
		bool HasValue { get; }

		/// <summary>�l�̎擾�����݂܂��B�l������Ɏ擾���ꂽ�ꍇ�� <c>true</c> ��Ԃ��܂��B</summary>
		/// <param name="value">�擾���ꂽ�l���i�[����ϐ����w�肵�܂��B</param>
		/// <returns>�l������Ɏ擾���ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		bool TryGetValue(out dynamic value);

		/// <summary>�X�R�[�v�Ɏw�肳�ꂽ�l��ݒ肵�܂��B</summary>
		/// <param name="value">�ݒ肷��l���w�肵�܂��B</param>
		void SetValue(object value);

		/// <summary>�X�R�[�v���猻�݂̒l���폜���܂��B</summary>
		/// <returns>�l������ɍ폜���ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		bool DeleteValue();
	}

	/// <summary>�Ώۂ̃I�u�W�F�N�g�ɑ΂����Q�Ƃ��擾������@��񋟂��܂��B</summary>
	interface IWeakReferencable
	{
		/// <summary>���̃I�u�W�F�N�g�̎�Q�Ƃ��擾���܂��B</summary>
		WeakReference WeakReference { get; }
	}

	/// <summary>�X�R�[�v���̒l���i�[���܂��B<see cref="ScopeVariable"/> �͑啶���Ə���������ʂ��A�P��̒l�݂̂��Q�Ƃ��܂��B</summary>
	public sealed class ScopeVariable : IScopeVariable, IWeakReferencable
	{
		object _value;
		WeakReference _weakref;
		static readonly object _novalue = new object();

		/// <summary><see cref="Microsoft.Scripting.ScopeVariable"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		internal ScopeVariable() { _value = _novalue; }

		#region Public APIs

		/// <summary>�X�R�[�v�ɒl�����݂��邩�ǂ����������l���擾���܂��B</summary>
		public bool HasValue { get { return _value != _novalue; } }

		/// <summary>�l�̎擾�����݂܂��B�l������Ɏ擾���ꂽ�ꍇ�� <c>true</c> ��Ԃ��܂��B</summary>
		/// <param name="value">�擾���ꂽ�l���i�[����ϐ����w�肵�܂��B</param>
		/// <returns>�l������Ɏ擾���ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public bool TryGetValue(out dynamic value)
		{
			value = _value;
			if ((object)value != _novalue)
				return true;
			value = null;
			return false;
		}

		/// <summary>�X�R�[�v�Ɏw�肳�ꂽ�l��ݒ肵�܂��B</summary>
		/// <param name="value">�ݒ肷��l���w�肵�܂��B</param>
		public void SetValue(object value) { _value = value; }

		/// <summary>�X�R�[�v���猻�݂̒l���폜���܂��B</summary>
		/// <returns>�l������ɍ폜���ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public bool DeleteValue() { return Interlocked.Exchange(ref _value, _novalue) != _novalue; }

		#endregion

		/// <summary>���̃I�u�W�F�N�g�̎�Q�Ƃ��擾���܂��B</summary>
		public WeakReference WeakReference
		{
			get
			{
				if (_weakref == null)
					_weakref = new WeakReference(this);
				return _weakref;
			}
		}
	}

	/// <summary>�X�R�[�v���̒l���i�[���܂��B<see cref="ScopeVariableIgnoreCase"/> �͑啶���Ə���������ʂ����A�����̒l���Q�Ƃ��܂��B</summary>
	public sealed class ScopeVariableIgnoreCase : IScopeVariable, IWeakReferencable
	{
		readonly string _firstCasing;
		readonly ScopeVariable _firstVariable;
		WeakReference _weakref;
		Dictionary<string, ScopeVariable> _overflow;

		/// <summary>���O���g�p���āA<see cref="Microsoft.Scripting.ScopeVariableIgnoreCase"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="casing">�ŏ��̕ϐ��̖��O���w�肵�܂��B</param>
		internal ScopeVariableIgnoreCase(string casing)
		{
			_firstCasing = casing;
			_firstVariable = new ScopeVariable();
		}

		#region Public APIs

		/// <summary>�X�R�[�v�ɒl�����݂��邩�ǂ����������l���擾���܂��B</summary>
		public bool HasValue
		{
			get
			{
				if (_firstVariable.HasValue)
					return true;
				if (_overflow != null)
				{
					lock (_overflow)
					{
						foreach (var entry in _overflow)
						{
							if (entry.Value.HasValue)
								return true;
						}
					}
				}
				return false;
			}
		}

		/// <summary>�l�̎擾�����݂܂��B�l������Ɏ擾���ꂽ�ꍇ�� <c>true</c> ��Ԃ��܂��B</summary>
		/// <param name="value">�擾���ꂽ�l���i�[����ϐ����w�肵�܂��B</param>
		/// <returns>�l������Ɏ擾���ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public bool TryGetValue(out dynamic value)
		{
			object objValue;
			if (_firstVariable.TryGetValue(out objValue))
			{
				value = objValue;
				return true;
			}
			if (_overflow != null)
			{
				lock (_overflow)
				{
					foreach (var entry in _overflow)
					{
						if (entry.Value.TryGetValue(out objValue))
						{
							value = objValue;
							return true;
						}
					}
				}
			}
			value = null;
			return false;
		}

		/// <summary>�X�R�[�v�Ɏw�肳�ꂽ�l��ݒ肵�܂��B</summary>
		/// <param name="value">�ݒ肷��l���w�肵�܂��B</param>
		public void SetValue(object value) { _firstVariable.SetValue(value); }

		/// <summary>�X�R�[�v���猻�݂̒l���폜���܂��B</summary>
		/// <returns>�l������ɍ폜���ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public bool DeleteValue()
		{
			bool res = _firstVariable.DeleteValue();
			if (_overflow != null)
			{
				lock (_overflow)
				{
					foreach (var entry in _overflow)
						res = entry.Value.DeleteValue() || res;
				}
			}
			return res;
		}

		#endregion

		#region Implementation Details

		/// <summary>�w�肳�ꂽ���O�ɑ΂���啶���Ə���������ʂ��� <see cref="ScopeVariable"/> ���擾���܂��B</summary>
		/// <param name="name">�啶���Ə���������ʂ��� <see cref="ScopeVariable"/> �ɑ΂��閼�O���w�肵�܂��B</param>
		internal ScopeVariable GetCaseSensitiveStorage(string name)
		{
			if (name == _firstCasing)
				return _firstVariable; // common case, only 1 casing available
			return GetStorageSlow(name);
		}

		/// <summary>�w�肳�ꂽ���X�g�ɂ��̃X�R�[�v�Ɋ܂܂�Ă��閼�O��ǉ����܂��B</summary>
		/// <param name="list">���O��ǉ����郊�X�g���w�肵�܂��B</param>
		internal void AddNames(List<string> list)
		{
			if (_firstVariable.HasValue)
				list.Add(_firstCasing);
			if (_overflow != null)
			{
				lock (_overflow)
				{
					foreach (var element in _overflow)
					{
						if (element.Value.HasValue)
							list.Add(element.Key);
					}
				}
			}

		}

		/// <summary>�w�肳�ꂽ���X�g�ɂ��̃X�R�[�v�Ɋ܂܂�Ă��閼�O�ƒl��ǉ����܂��B</summary>
		/// <param name="list">���O�ƒl��ǉ����郊�X�g���w�肵�܂��B</param>
		internal void AddItems(List<KeyValuePair<string, object>> list)
		{
			object value;
			if (_firstVariable.TryGetValue(out value))
				list.Add(new KeyValuePair<string, object>(_firstCasing, value));
			if (_overflow != null)
			{
				lock (_overflow)
				{
					foreach (var element in _overflow)
					{
						if (element.Value.TryGetValue(out value))
							list.Add(new KeyValuePair<string, object>(element.Key, value));
					}
				}
			}

		}

		ScopeVariable GetStorageSlow(string name)
		{
			if (_overflow == null)
				Interlocked.CompareExchange(ref _overflow, new Dictionary<string, ScopeVariable>(), null);
			lock (_overflow)
			{
				ScopeVariable res;
				if (!_overflow.TryGetValue(name, out res))
					_overflow[name] = res = new ScopeVariable();
				return res;
			}
		}

		#endregion

		/// <summary>���̃I�u�W�F�N�g�̎�Q�Ƃ��擾���܂��B</summary>
		public WeakReference WeakReference
		{
			get
			{
				if (_weakref == null)
					_weakref = new WeakReference(this);
				return _weakref;
			}
		}
	}
}
