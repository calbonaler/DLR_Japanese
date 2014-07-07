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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

using Microsoft.Contracts;
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions
{
	/// <summary>イベントを表します。</summary>
	public class EventTracker : MemberTracker
	{
		WeakDictionary<object, NormalHandlerList> _handlerLists;
		static readonly object _staticTarget = new object();

		MethodInfo _addMethod;
		MethodInfo _removeMethod;

		/// <summary>基になる <see cref="EventInfo"/> を使用して、<see cref="Microsoft.Scripting.Actions.EventTracker"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="eventInfo">基になるイベントを表す <see cref="EventInfo"/> を指定します。</param>
		internal EventTracker(EventInfo eventInfo)
		{
			Assert.NotNull(eventInfo);
			Event = eventInfo;
		}

		/// <summary>メンバを論理的に宣言する型を取得します。</summary>
		public override Type DeclaringType { get { return Event.DeclaringType; } }

		/// <summary><see cref="MemberTracker"/> の種類を取得します。</summary>
		public override sealed TrackerTypes MemberType { get { return TrackerTypes.Event; } }

		/// <summary>メンバの名前を取得します。</summary>
		public override string Name { get { return Event.Name; } }

		/// <summary>基になる <see cref="EventInfo"/> を取得します。</summary>
		public EventInfo Event { get; private set; }

		/// <summary>このイベントに指定されたデリゲートを関連付ける呼び出し可能なメソッドを取得します。</summary>
		public MethodInfo CallableAddMethod
		{
			get
			{
				if (_addMethod == null)
					_addMethod = CompilerHelpers.TryGetCallableMethod(Event.GetAddMethod(true));
				return _addMethod;
			}
		}

		/// <summary>このイベントから指定されたデリゲートの関連付けを解除する呼び出し可能なメソッドを取得します。</summary>
		public MethodInfo CallableRemoveMethod
		{
			get
			{
				if (_removeMethod == null)
					_removeMethod = CompilerHelpers.TryGetCallableMethod(Event.GetRemoveMethod(true));
				return _removeMethod;
			}
		}

		// PrivateBinding 設定をチェックする必要はありません: イベントの一部であるどのメソッドもパブリックでないなら、イベントはパブリックではありません。
		// もしコードがすでにパブリックでないイベントに対するイベントトラッカーの参照を持っているならば、その「静的性」は PrivateBinding 設定に影響されません。
		/// <summary>このイベントが静的かどうかを示す値を取得します。</summary>
		public bool IsStatic
		{
			get
			{
				var mi = Event.GetAddMethod(false) ??
					Event.GetRemoveMethod(false) ??
					Event.GetRaiseMethod(false) ??
					Event.GetAddMethod(true) ??
					Event.GetRemoveMethod(true) ??
					Event.GetRaiseMethod(true);
				MethodInfo m;
				Debug.Assert(
					((m = Event.GetAddMethod(true)) == null || m.IsStatic == mi.IsStatic) &&
					((m = Event.GetRaiseMethod(true)) == null || m.IsStatic == mi.IsStatic) &&
					((m = Event.GetRaiseMethod(true)) == null || m.IsStatic == mi.IsStatic),
					"Methods are either all static or all instance."
				);
				return mi.IsStatic;
			}
		}

		/// <summary>
		/// インスタンスに束縛されている値を取得する <see cref="System.Linq.Expressions.Expression"/> を取得します。
		/// カスタムメンバトラッカーはこのメソッドをオーバーライドして、インスタンスへのバインド時の独自の動作を提供できます。
		/// </summary>
		/// <param name="resolverFactory">オーバーロード解決の方法を表す <see cref="OverloadResolverFactory"/> を指定します。</param>
		/// <param name="binder">言語のバインディングセマンティクスを指定します。</param>
		/// <param name="type">この <see cref="MemberTracker"/> がアクセスされた型を指定します。</param>
		/// <param name="instance">束縛されたインスタンスを指定します。</param>
		/// <returns>インスタンスに束縛されている値を取得する <see cref="System.Linq.Expressions.Expression"/>。</returns>
		protected internal override DynamicMetaObject GetBoundValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type, DynamicMetaObject instance) { return binder.ReturnMemberTracker(type, new BoundMemberTracker(this, instance)); }

		/// <summary>
		/// バインディングが可能な場合、新しいメンバトラッカーを返す指定されたインスタンスにメンバトラッカーを関連付けます。
		/// バインディングが不可能な場合、既存のメンバトラッカーが返されます。
		/// 例えば、静的フィールドへのバインディングは、元のメンバトラッカーを返します。
		/// インスタンスフィールドへのバインディングは、インスタンスを渡す GetBoundValue または SetBoundValue を得る新しい <see cref="BoundMemberTracker"/> を返します。
		/// </summary>
		/// <param name="instance">メンバトラッカーを関連付けるインスタンスを指定します。</param>
		/// <returns>指定されたインスタンスに関連付けられたメンバトラッカー。</returns>
		public override MemberTracker BindToInstance(DynamicMetaObject instance) { return IsStatic ? (MemberTracker)this : new BoundMemberTracker(this, instance); }

		/// <summary>このオブジェクトの文字列表現を返します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		[Confined]
		public override string ToString() { return Event.ToString(); }

		/// <summary>このイベントに指定されたイベント ハンドラを追加します。</summary>
		/// <param name="target">イベント ハンドラを追加するオブジェクトを指定します。</param>
		/// <param name="handler">追加するイベント ハンドラを指定します。</param>
		/// <param name="delegateCreator">指定されたイベント ハンドラがデリゲートでない場合にデリゲート型に変換する <see cref="DynamicDelegateCreator"/> を指定します。</param>
		public void AddHandler(object target, object handler, DynamicDelegateCreator delegateCreator)
		{
			ContractUtils.RequiresNotNull(handler, "handler");
			ContractUtils.RequiresNotNull(delegateCreator, "delegateCreator");
			Delegate delegateHandler;
			IHandlerList stubs;
			// we can add event directly (signature does match):
			if (Event.EventHandlerType.IsAssignableFrom(handler.GetType()))
			{
				delegateHandler = (Delegate)handler;
				stubs = null;
			}
			else
			{
				// create signature converting stub:
				delegateHandler = delegateCreator.GetDelegate(handler, Event.EventHandlerType);
				stubs = GetHandlerList(target);
			}
			CallableAddMethod.Invoke(target, new object[] { delegateHandler });
			if (stubs != null)
				stubs.AddHandler(handler, delegateHandler); // remember the stub so that we could search for it on removal:
		}
		
		/// <summary>このイベントから指定されたイベント ハンドラを削除します。</summary>
		/// <param name="target">イベント ハンドラを削除するオブジェクトを指定します。</param>
		/// <param name="handler">削除するイベント ハンドラを指定します。</param>
		/// <param name="objectComparer">カスタム ハンドラ リストから削除する際の等価性の判断に使用する <see cref="IEqualityComparer&lt;Object&gt;"/> を指定します。</param>
		public void RemoveHandler(object target, object handler, IEqualityComparer<object> objectComparer)
		{
			ContractUtils.RequiresNotNull(handler, "handler");
			ContractUtils.RequiresNotNull(objectComparer, "objectComparer");
			var delegateHandler = Event.EventHandlerType.IsAssignableFrom(handler.GetType()) ? (Delegate)handler : GetHandlerList(target).RemoveHandler(handler, objectComparer);
			if (delegateHandler != null)
				CallableRemoveMethod.Invoke(target, new object[] { delegateHandler });
		}

		IHandlerList GetHandlerList(object instance)
		{
			if (TypeUtils.IsComObject(instance))
				return GetComHandlerList(instance);
			if (_handlerLists == null)
				Interlocked.CompareExchange(ref _handlerLists, new WeakDictionary<object, NormalHandlerList>(), null);
			if (instance == null)
				instance = _staticTarget; // targetting a static method, we'll use a random object as our place holder here...
			lock (_handlerLists)
			{
				NormalHandlerList result;
				if (_handlerLists.TryGetValue(instance, out result))
					return result;
				return _handlerLists[instance] = result = new NormalHandlerList();
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
		IHandlerList GetComHandlerList(object instance)
		{
			IHandlerList hl = (IHandlerList)Marshal.GetComObjectData(instance, this);
			if (hl == null)
			{
				lock (_staticTarget)
				{
					hl = (IHandlerList)Marshal.GetComObjectData(instance, this);
					if (hl == null)
					{
						hl = new ComHandlerList();
						if (!Marshal.SetComObjectData(instance, this, hl))
							throw new COMException("Failed to set COM Object Data");
					}
				}
			}
			return hl;
		}

		interface IHandlerList
		{
			void AddHandler(object callableObject, Delegate handler);
			Delegate RemoveHandler(object callableObject, IEqualityComparer<object> comparer);
		}

		sealed class ComHandlerList : IHandlerList
		{
			readonly CopyOnWriteList<KeyValuePair<object, Delegate>> _handlers = new CopyOnWriteList<KeyValuePair<object, Delegate>>();

			public void AddHandler(object callableObject, Delegate handler)
			{
				Assert.NotNull(handler);
				_handlers.Add(new KeyValuePair<object, Delegate>(callableObject, handler));
			}

			public Delegate RemoveHandler(object callableObject, IEqualityComparer<object> comparer)
			{
				var copyOfHandlers = _handlers.GetCopyForRead();
				for (int i = copyOfHandlers.Count - 1; i >= 0; i--)
				{
					if (comparer.Equals(copyOfHandlers[i].Key, callableObject))
					{
						var handler = copyOfHandlers[i].Value;
						_handlers.RemoveAt(i);
						return handler;
					}
				}
				return null;
			}
		}

		sealed class NormalHandlerList : IHandlerList
		{
			readonly CopyOnWriteList<KeyValuePair<WeakReference<object>, WeakReference<Delegate>>> _handlers = new CopyOnWriteList<KeyValuePair<WeakReference<object>, WeakReference<Delegate>>>();

			public void AddHandler(object callableObject, Delegate handler)
			{
				Assert.NotNull(handler);
				_handlers.Add(new KeyValuePair<WeakReference<object>, WeakReference<Delegate>>(new WeakReference<object>(callableObject), new WeakReference<Delegate>(handler)));
			}

			public Delegate RemoveHandler(object callableObject, IEqualityComparer<object> comparer)
			{
				var copyOfHandlers = _handlers.GetCopyForRead();
				for (int i = copyOfHandlers.Count - 1; i >= 0; i--)
				{
					object key;
					Delegate value;
					if (copyOfHandlers[i].Key.TryGetTarget(out key) && copyOfHandlers[i].Value.TryGetTarget(out value) && comparer.Equals(key, callableObject))
					{
						_handlers.RemoveAt(i);
						return value;
					}
				}
				return null;
			}
		}

		class CopyOnWriteList<T> : IList<T>
		{
			List<T> _list = new List<T>();

			List<T> GetNewListForWrite()
			{
				List<T> newList = new List<T>(_list.Count + 1);
				newList.AddRange(_list);
				return newList;
			}

			[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")] // TODO: fix
			public List<T> GetCopyForRead() { return _list; } // Just return the underlying list

			public int IndexOf(T item) { return _list.IndexOf(item); }

			public void Insert(int index, T item)
			{
				List<T> oldList, replacedList;
				do
				{
					oldList = _list;
					var newList = GetNewListForWrite();
					newList.Insert(index, item);
					replacedList = Interlocked.CompareExchange(ref _list, newList, oldList);
				} while (replacedList != oldList);
			}

			public void RemoveAt(int index)
			{
				List<T> oldList, replacedList;
				do
				{
					oldList = _list;
					var newList = GetNewListForWrite();
					newList.RemoveAt(index);
					replacedList = Interlocked.CompareExchange(ref _list, newList, oldList);
				} while (replacedList != oldList);
			}

			public T this[int index]
			{
				get { return _list[index]; }
				set
				{
					List<T> oldList, replacedList;
					do
					{
						oldList = _list;
						var newList = GetNewListForWrite();
						newList[index] = value;
						replacedList = Interlocked.CompareExchange(ref _list, newList, oldList);
					} while (replacedList != oldList);
				}
			}

			public void Add(T item)
			{
				List<T> oldList, replacedList;
				do
				{
					oldList = _list;
					var newList = GetNewListForWrite();
					newList.Add(item);
					replacedList = Interlocked.CompareExchange(ref _list, newList, oldList);
				} while (replacedList != oldList);
			}

			public void Clear() { _list = new List<T>(); }

			[Confined]
			public bool Contains(T item) { return _list.Contains(item); }

			public void CopyTo(T[] array, int arrayIndex) { _list.CopyTo(array, arrayIndex); }

			public int Count { get { return _list.Count; } }

			public bool IsReadOnly { get { return false; } }

			public bool Remove(T item)
			{
				List<T> oldList, replacedList;
				bool ret;
				do
				{
					oldList = _list;
					var newList = GetNewListForWrite();
					ret = newList.Remove(item);
					replacedList = Interlocked.CompareExchange(ref _list, newList, oldList);
				} while (replacedList != oldList);
				return ret;
			}

			[Pure]
			public IEnumerator<T> GetEnumerator() { return _list.GetEnumerator(); }

			[Pure]
			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		}
	}
}
