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
	/// スコープストレージに対する最適化されたキャッシュ可能なサポートを提供します。
	/// このクラスはスコープ内で格納する値に対して使用される既定のオブジェクトです。
	/// </summary>
	/// <remarks>
	/// 実装は <see cref="ScopeVariableIgnoreCase"/> を保持する大文字と小文字を区別しないディクショナリを使用します。
	/// <see cref="ScopeVariableIgnoreCase"/> オブジェクトはそれぞれの可能なケーシングに対する <see cref="ScopeVariable"/> オブジェクトを保持します。
	/// </remarks>
	public sealed class ScopeStorage : IDynamicMetaObjectProvider
	{
		readonly Dictionary<string, ScopeVariableIgnoreCase> _storage = new Dictionary<string, ScopeVariableIgnoreCase>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// オプションで大文字と小文字を区別しないでスコープ内の指定された変数を取得します。
		/// 指定された名前が存在しない場合は <see cref="InvalidOperationException"/> が発生します。
		/// </summary>
		/// <param name="name">スコープ内に格納されている値を識別する名前を指定します。</param>
		/// <param name="ignoreCase">名前において大文字と小文字を区別するかどうかを示す値を指定します。</param>
		public dynamic GetValue(string name, bool ignoreCase)
		{
			object res;
			if (GetVariable(name, ignoreCase).TryGetValue(out res))
				return res;
			throw new KeyNotFoundException("no value");
		}

		/// <summary>
		/// オプションで大文字と小文字を区別しないでスコープ内の指定された変数の取得を試みます。
		/// もし値が存在した場合は <c>true</c>、それ以外の場合は <c>false</c> を返します。
		/// </summary>
		/// <param name="name">スコープ内に格納されている値を識別する名前を指定します。</param>
		/// <param name="ignoreCase">名前において大文字と小文字を区別するかどうかを示す値を指定します。</param>
		/// <param name="value">取得した値を格納する変数を指定します。</param>
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

		/// <summary>オプションで大文字と小文字を区別しないでスコープに指定された値を設定します。</summary>
		/// <param name="name">スコープに格納される値を識別する名前を指定します。</param>
		/// <param name="ignoreCase">名前において大文字と小文字を区別するかどうかを示す値を指定します。</param>
		/// <param name="value">スコープに設定する値を指定します。</param>
		public void SetValue(string name, bool ignoreCase, object value) { GetVariable(name, ignoreCase).SetValue(value); }

		/// <summary>オプションで大文字と小文字を区別しないでスコープから指定された値を削除します。削除が成功した場合は <c>true</c> を返します。</summary>
		/// <param name="name">スコープで格納される値を識別する名前を指定します。</param>
		/// <param name="ignoreCase">名前において大文字と小文字を区別するかどうかを示す値を指定します。</param>
		public bool DeleteValue(string name, bool ignoreCase)
		{
			if (!HasVariable(name))
				return false;
			return GetVariable(name, ignoreCase).DeleteValue();
		}

		/// <summary>オプションで大文字と小文字を区別しないでスコープに指定された値が含まれているかどうかを調べます。</summary>
		/// <param name="name">スコープに格納されているかどうかを調べる名前を指定します。</param>
		/// <param name="ignoreCase">名前において大文字と小文字を区別するかどうかを示す値を指定します。</param>
		public bool HasValue(string name, bool ignoreCase)
		{
			if (!HasVariable(name))
				return false;
			return GetVariable(name, ignoreCase).HasValue;
		}

		/// <summary>
		/// オプションで大文字と小文字を区別しないでスコープに対する <see cref="IScopeVariable"/> を取得します。
		/// <see cref="IScopeVariable"/> は後続のアクセスで辞書検索を実行せずに取得・設定・削除が行われます。
		/// </summary>
		/// <param name="name">スコープに格納されているかどうかを調べる名前を指定します。</param>
		/// <param name="ignoreCase">名前において大文字と小文字を区別するかどうかを示す値を指定します。</param>
		public IScopeVariable GetVariable(string name, bool ignoreCase)
		{
			if (ignoreCase)
				return GetVariableIgnoreCase(name);
			return GetVariable(name);
		}

		/// <summary>
		/// 大文字と小文字を区別するスコープに対する <see cref="ScopeVariable"/> を取得します。
		/// <see cref="ScopeVariable"/> は後続のアクセスで辞書検索を実行せずに取得・設定・削除が行われます。
		/// </summary>
		/// <param name="name">取得する <see cref="ScopeVariable"/> に対する名前を指定します。</param>
		public ScopeVariable GetVariable(string name) { return GetVariableIgnoreCase(name).GetCaseSensitiveStorage(name); }

		/// <summary>
		/// 大文字と小文字を区別しないスコープに対する <see cref="ScopeVariableIgnoreCase"/> を取得します。
		/// <see cref="ScopeVariableIgnoreCase"/> は後続のアクセスで辞書検索を実行せずに取得・設定・削除が行われます。
		/// </summary>
		/// <param name="name">取得する <see cref="ScopeVariableIgnoreCase"/> に対する名前を指定します。</param>
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

		/// <summary>大文字と小文字を区別しないでスコープ内で指定された変数を取得または設定します。</summary>
		public dynamic this[string index]
		{
			get { return GetValue(index, false); }
			set { SetValue(index, false, (object)value); }
		}

		/// <summary>
		/// このスコープにおいて値があるすべてのメンバの名前を返します。
		/// 返される名前にはすべての利用可能なケーシングが含まれます。
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
		/// このスコープにおいてすべてのメンバ名およびそれらに関連付けられた値を返します。
		/// 返される名前にはすべての利用可能なケーシングが含まれます。
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

		/// <summary>このオブジェクトに対して実行される操作をバインドする <see cref="System.Dynamic.DynamicMetaObject"/> を返します。</summary>
		/// <param name="parameter">ランタイム値の式ツリー表現。</param>
		/// <returns>このオブジェクトをバインドする <see cref="System.Dynamic.DynamicMetaObject"/>。</returns>
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

	/// <summary>変数ストレージに対する共通のインターフェイスを提供します。</summary>
	public interface IScopeVariable
	{
		/// <summary>スコープに値が存在するかどうかを示す値を取得します。</summary>
		bool HasValue { get; }

		/// <summary>値の取得を試みます。値が正常に取得された場合は <c>true</c> を返します。</summary>
		/// <param name="value">取得された値を格納する変数を指定します。</param>
		/// <returns>値が正常に取得された場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		bool TryGetValue(out dynamic value);

		/// <summary>スコープに指定された値を設定します。</summary>
		/// <param name="value">設定する値を指定します。</param>
		void SetValue(object value);

		/// <summary>スコープから現在の値を削除します。</summary>
		/// <returns>値が正常に削除された場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		bool DeleteValue();
	}

	/// <summary>対象のオブジェクトに対する弱参照を取得する方法を提供します。</summary>
	interface IWeakReferencable
	{
		/// <summary>このオブジェクトの弱参照を取得します。</summary>
		WeakReference WeakReference { get; }
	}

	/// <summary>スコープ内の値を格納します。<see cref="ScopeVariable"/> は大文字と小文字を区別し、単一の値のみを参照します。</summary>
	public sealed class ScopeVariable : IScopeVariable, IWeakReferencable
	{
		object _value;
		WeakReference _weakref;
		static readonly object _novalue = new object();

		/// <summary><see cref="Microsoft.Scripting.ScopeVariable"/> クラスの新しいインスタンスを初期化します。</summary>
		internal ScopeVariable() { _value = _novalue; }

		#region Public APIs

		/// <summary>スコープに値が存在するかどうかを示す値を取得します。</summary>
		public bool HasValue { get { return _value != _novalue; } }

		/// <summary>値の取得を試みます。値が正常に取得された場合は <c>true</c> を返します。</summary>
		/// <param name="value">取得された値を格納する変数を指定します。</param>
		/// <returns>値が正常に取得された場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool TryGetValue(out dynamic value)
		{
			value = _value;
			if ((object)value != _novalue)
				return true;
			value = null;
			return false;
		}

		/// <summary>スコープに指定された値を設定します。</summary>
		/// <param name="value">設定する値を指定します。</param>
		public void SetValue(object value) { _value = value; }

		/// <summary>スコープから現在の値を削除します。</summary>
		/// <returns>値が正常に削除された場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool DeleteValue() { return Interlocked.Exchange(ref _value, _novalue) != _novalue; }

		#endregion

		/// <summary>このオブジェクトの弱参照を取得します。</summary>
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

	/// <summary>スコープ内の値を格納します。<see cref="ScopeVariableIgnoreCase"/> は大文字と小文字を区別せず、複数の値を参照します。</summary>
	public sealed class ScopeVariableIgnoreCase : IScopeVariable, IWeakReferencable
	{
		readonly string _firstCasing;
		readonly ScopeVariable _firstVariable;
		WeakReference _weakref;
		Dictionary<string, ScopeVariable> _overflow;

		/// <summary>名前を使用して、<see cref="Microsoft.Scripting.ScopeVariableIgnoreCase"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="casing">最初の変数の名前を指定します。</param>
		internal ScopeVariableIgnoreCase(string casing)
		{
			_firstCasing = casing;
			_firstVariable = new ScopeVariable();
		}

		#region Public APIs

		/// <summary>スコープに値が存在するかどうかを示す値を取得します。</summary>
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

		/// <summary>値の取得を試みます。値が正常に取得された場合は <c>true</c> を返します。</summary>
		/// <param name="value">取得された値を格納する変数を指定します。</param>
		/// <returns>値が正常に取得された場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
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

		/// <summary>スコープに指定された値を設定します。</summary>
		/// <param name="value">設定する値を指定します。</param>
		public void SetValue(object value) { _firstVariable.SetValue(value); }

		/// <summary>スコープから現在の値を削除します。</summary>
		/// <returns>値が正常に削除された場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
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

		/// <summary>指定された名前に対する大文字と小文字を区別する <see cref="ScopeVariable"/> を取得します。</summary>
		/// <param name="name">大文字と小文字を区別する <see cref="ScopeVariable"/> に対する名前を指定します。</param>
		internal ScopeVariable GetCaseSensitiveStorage(string name)
		{
			if (name == _firstCasing)
				return _firstVariable; // common case, only 1 casing available
			return GetStorageSlow(name);
		}

		/// <summary>指定されたリストにこのスコープに含まれている名前を追加します。</summary>
		/// <param name="list">名前を追加するリストを指定します。</param>
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

		/// <summary>指定されたリストにこのスコープに含まれている名前と値を追加します。</summary>
		/// <param name="list">名前と値を追加するリストを指定します。</param>
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

		/// <summary>このオブジェクトの弱参照を取得します。</summary>
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
