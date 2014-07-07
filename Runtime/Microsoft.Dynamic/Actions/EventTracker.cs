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
	/// <summary>�C�x���g��\���܂��B</summary>
	public class EventTracker : MemberTracker
	{
		WeakDictionary<object, NormalHandlerList> _handlerLists;
		static readonly object _staticTarget = new object();

		MethodInfo _addMethod;
		MethodInfo _removeMethod;

		/// <summary>��ɂȂ� <see cref="EventInfo"/> ���g�p���āA<see cref="Microsoft.Scripting.Actions.EventTracker"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="eventInfo">��ɂȂ�C�x���g��\�� <see cref="EventInfo"/> ���w�肵�܂��B</param>
		internal EventTracker(EventInfo eventInfo)
		{
			Assert.NotNull(eventInfo);
			Event = eventInfo;
		}

		/// <summary>�����o��_���I�ɐ錾����^���擾���܂��B</summary>
		public override Type DeclaringType { get { return Event.DeclaringType; } }

		/// <summary><see cref="MemberTracker"/> �̎�ނ��擾���܂��B</summary>
		public override sealed TrackerTypes MemberType { get { return TrackerTypes.Event; } }

		/// <summary>�����o�̖��O���擾���܂��B</summary>
		public override string Name { get { return Event.Name; } }

		/// <summary>��ɂȂ� <see cref="EventInfo"/> ���擾���܂��B</summary>
		public EventInfo Event { get; private set; }

		/// <summary>���̃C�x���g�Ɏw�肳�ꂽ�f���Q�[�g���֘A�t����Ăяo���\�ȃ��\�b�h���擾���܂��B</summary>
		public MethodInfo CallableAddMethod
		{
			get
			{
				if (_addMethod == null)
					_addMethod = CompilerHelpers.TryGetCallableMethod(Event.GetAddMethod(true));
				return _addMethod;
			}
		}

		/// <summary>���̃C�x���g����w�肳�ꂽ�f���Q�[�g�̊֘A�t������������Ăяo���\�ȃ��\�b�h���擾���܂��B</summary>
		public MethodInfo CallableRemoveMethod
		{
			get
			{
				if (_removeMethod == null)
					_removeMethod = CompilerHelpers.TryGetCallableMethod(Event.GetRemoveMethod(true));
				return _removeMethod;
			}
		}

		// PrivateBinding �ݒ���`�F�b�N����K�v�͂���܂���: �C�x���g�̈ꕔ�ł���ǂ̃��\�b�h���p�u���b�N�łȂ��Ȃ�A�C�x���g�̓p�u���b�N�ł͂���܂���B
		// �����R�[�h�����łɃp�u���b�N�łȂ��C�x���g�ɑ΂���C�x���g�g���b�J�[�̎Q�Ƃ������Ă���Ȃ�΁A���́u�ÓI���v�� PrivateBinding �ݒ�ɉe������܂���B
		/// <summary>���̃C�x���g���ÓI���ǂ����������l���擾���܂��B</summary>
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
		/// �C���X�^���X�ɑ�������Ă���l���擾���� <see cref="System.Linq.Expressions.Expression"/> ���擾���܂��B
		/// �J�X�^�������o�g���b�J�[�͂��̃��\�b�h���I�[�o�[���C�h���āA�C���X�^���X�ւ̃o�C���h���̓Ǝ��̓����񋟂ł��܂��B
		/// </summary>
		/// <param name="resolverFactory">�I�[�o�[���[�h�����̕��@��\�� <see cref="OverloadResolverFactory"/> ���w�肵�܂��B</param>
		/// <param name="binder">����̃o�C���f�B���O�Z�}���e�B�N�X���w�肵�܂��B</param>
		/// <param name="type">���� <see cref="MemberTracker"/> ���A�N�Z�X���ꂽ�^���w�肵�܂��B</param>
		/// <param name="instance">�������ꂽ�C���X�^���X���w�肵�܂��B</param>
		/// <returns>�C���X�^���X�ɑ�������Ă���l���擾���� <see cref="System.Linq.Expressions.Expression"/>�B</returns>
		protected internal override DynamicMetaObject GetBoundValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type, DynamicMetaObject instance) { return binder.ReturnMemberTracker(type, new BoundMemberTracker(this, instance)); }

		/// <summary>
		/// �o�C���f�B���O���\�ȏꍇ�A�V���������o�g���b�J�[��Ԃ��w�肳�ꂽ�C���X�^���X�Ƀ����o�g���b�J�[���֘A�t���܂��B
		/// �o�C���f�B���O���s�\�ȏꍇ�A�����̃����o�g���b�J�[���Ԃ���܂��B
		/// �Ⴆ�΁A�ÓI�t�B�[���h�ւ̃o�C���f�B���O�́A���̃����o�g���b�J�[��Ԃ��܂��B
		/// �C���X�^���X�t�B�[���h�ւ̃o�C���f�B���O�́A�C���X�^���X��n�� GetBoundValue �܂��� SetBoundValue �𓾂�V���� <see cref="BoundMemberTracker"/> ��Ԃ��܂��B
		/// </summary>
		/// <param name="instance">�����o�g���b�J�[���֘A�t����C���X�^���X���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�C���X�^���X�Ɋ֘A�t����ꂽ�����o�g���b�J�[�B</returns>
		public override MemberTracker BindToInstance(DynamicMetaObject instance) { return IsStatic ? (MemberTracker)this : new BoundMemberTracker(this, instance); }

		/// <summary>���̃I�u�W�F�N�g�̕�����\����Ԃ��܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̕�����\���B</returns>
		[Confined]
		public override string ToString() { return Event.ToString(); }

		/// <summary>���̃C�x���g�Ɏw�肳�ꂽ�C�x���g �n���h����ǉ����܂��B</summary>
		/// <param name="target">�C�x���g �n���h����ǉ�����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="handler">�ǉ�����C�x���g �n���h�����w�肵�܂��B</param>
		/// <param name="delegateCreator">�w�肳�ꂽ�C�x���g �n���h�����f���Q�[�g�łȂ��ꍇ�Ƀf���Q�[�g�^�ɕϊ����� <see cref="DynamicDelegateCreator"/> ���w�肵�܂��B</param>
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
		
		/// <summary>���̃C�x���g����w�肳�ꂽ�C�x���g �n���h�����폜���܂��B</summary>
		/// <param name="target">�C�x���g �n���h�����폜����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="handler">�폜����C�x���g �n���h�����w�肵�܂��B</param>
		/// <param name="objectComparer">�J�X�^�� �n���h�� ���X�g����폜����ۂ̓������̔��f�Ɏg�p���� <see cref="IEqualityComparer&lt;Object&gt;"/> ���w�肵�܂��B</param>
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
