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
using System.Runtime.CompilerServices;
using Microsoft.Contracts;

namespace Microsoft.Scripting.Utils
{
	/// <summary>
	/// �L�[�����̃I�u�W�F�N�g����Q�Ƃ���Ȃ��Ȃ�ƃL�[�����p�ł��Ȃ��Ȃ�f�B�N�V���i����\���܂��B
	/// �l�̓L�[���������Ă�����萶���������܂��B
	/// </summary>
	/// <typeparam name="TKey">�f�B�N�V���i���̃L�[�̌^���w�肵�܂��B</typeparam>
	/// <typeparam name="TValue">�f�B�N�V���i���̒l�̌^���w�肵�܂��B</typeparam>
	/// <remarks>
	/// ���݂��̃N���X�ɂ̓L�[�Ƃ��Ďg�p����Ă���I�u�W�F�N�g�����̃N���X�̂ǂ̃C���X�^���X�ł��l�Ƃ��Ďg�p���邱�Ƃ��ł��Ȃ��Ƃ�������������܂��B
	/// �����Ȃ���΁A�I�u�W�F�N�g�͉i���ɉ������܂���B
	/// ����͎�����A���̃N���X�̗��p�҂݂̂��l�Ƃ��Ďg�p����Ă���I�u�W�F�N�g�փA�N�Z�X�ł���悤�ɂ���K�v�����邱�Ƃ��Ӗ����܂��B
	/// 
	/// �܂��A���݃L�[�����W����Ă���l���ێ��������ԂɊւ���ۏ؂͑��݂��܂���B
	/// ���̖��� CheckCleanup() ���Ăяo���t�@�C�i���C�U�����_�~�[�̃E�H�b�`�h�b�O�I�u�W�F�N�g�������A�K�x�[�W�R���N�V�������� CheckCleanup() ���g���K�[���邱�Ƃŉ����ł���\��������܂��B
	/// </remarks>
	public class WeakDictionary<TKey, TValue> : IDictionary<TKey, TValue> where TKey : class
	{
		sealed class WeakComparer : EqualityComparer<object>
		{
			public override bool Equals(object x, object y)
			{
				TKey obj;
				var wx = x as HashableWeakReference;
				if (wx != null)
					x = wx.TryGetTarget(out obj) ? obj : null;
				var wy = y as HashableWeakReference;
				if (wy != null)
					y = wy.TryGetTarget(out obj) ? obj : null;
				return object.Equals(x, y);
			}

			public override int GetHashCode(object obj)
			{
				var wobj = obj as HashableWeakReference;
				if (wobj != null)
					return wobj.GetHashCode();
				return obj == null ? 0 : RuntimeHelpers.GetHashCode(obj);
			}
		}

		class HashableWeakReference
		{
			WeakReference<TKey> weakReference;
			int hashCode;

			public HashableWeakReference(TKey obj)
			{
				weakReference = new WeakReference<TKey>(obj, true);
				hashCode = obj == null ? 0 : RuntimeHelpers.GetHashCode(obj);
			}

			public bool TryGetTarget(out TKey obj) { return weakReference.TryGetTarget(out obj); }

			[Confined]
			public override int GetHashCode() { return hashCode; }

			[Confined]
			public override bool Equals(object obj)
			{
				TKey target;
				return TryGetTarget(out target) && target.Equals(obj);
			}
		}

		// The one and only comparer instance.
		static readonly IEqualityComparer<object> comparer = new WeakComparer();

		Dictionary<object, TValue> dict = new Dictionary<object, TValue>(comparer);
		int version, cleanupVersion;
		int cleanupGC = 0;

		/// <summary><see cref="Microsoft.Scripting.Utils.WeakDictionary&lt;TKey, TValue&gt;"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		public WeakDictionary() { }

		/// <summary>�w�肵���L�[����ђl�����v�f�� <see cref="WeakDictionary&lt;TKey, TValue&gt;"/> �ɒǉ����܂��B</summary>
		/// <param name="key">�ǉ�����v�f�̃L�[�Ƃ��Ďg�p����I�u�W�F�N�g�B</param>
		/// <param name="value">�ǉ�����v�f�̒l�Ƃ��Ďg�p����I�u�W�F�N�g�B</param>
		/// <exception cref="ArgumentException">�����L�[�����v�f���A<see cref="WeakDictionary&lt;TKey, TValue&gt;"/> �Ɋ��ɑ��݂��܂��B</exception>
		public void Add(TKey key, TValue value)
		{
			CheckCleanup();
			Debug.Assert(!dict.ContainsKey(value));
			dict.Add(new HashableWeakReference(key), value);
		}

		/// <summary>�w�肵���L�[�̗v�f�� <see cref="WeakDictionary&lt;TKey, TValue&gt;"/> �Ɋi�[����Ă��邩�ǂ������m�F���܂��B</summary>
		/// <param name="key"><see cref="WeakDictionary&lt;TKey, TValue&gt;"/> ���Ō��������L�[�B</param>
		/// <returns>�w�肵���L�[�����v�f�� <see cref="WeakDictionary&lt;TKey, TValue&gt;"/> ���ێ����Ă���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		[Confined]
		public bool ContainsKey(TKey key) { return dict.ContainsKey(key); }

		/// <summary><see cref="WeakDictionary&lt;TKey, TValue&gt;"/> �̃L�[��ێ����Ă��� <see cref="System.Collections.Generic.ICollection&lt;T&gt;"/> ���擾���܂��B</summary>
		public ICollection<TKey> Keys { get { return this.Select(x => x.Key).ToArray(); } }

		/// <summary>�w�肵���L�[�����v�f�� <see cref="WeakDictionary&lt;TKey, TValue&gt;"/> ����폜���܂��B</summary>
		/// <param name="key">�폜����v�f�̃L�[�B</param>
		/// <returns>�v�f������ɍ폜���ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B���̃��\�b�h�́A<paramref name="key"/> ������ <see cref="WeakDictionary&lt;TKey, TValue&gt;"/> �Ɍ�����Ȃ������ꍇ�ɂ� <c>false</c> ��Ԃ��܂��B</returns>
		public bool Remove(TKey key) { return dict.Remove(key); }

		/// <summary>�w�肵���L�[�Ɋ֘A�t�����Ă���l���擾���܂��B</summary>
		/// <param name="key">�l���擾����Ώۂ̃L�[�B</param>
		/// <param name="value">
		/// ���̃��\�b�h���Ԃ����Ƃ��ɁA�L�[�����������ꍇ�́A�w�肵���L�[�Ɋ֘A�t�����Ă���l�B����ȊO�̏ꍇ�� <paramref name="value"/> �p�����[�^�[�̌^�ɑ΂������̒l�B
		/// ���̃p�����[�^�[�͏����������ɓn����܂��B
		/// </param>
		/// <returns>�w�肵���L�[�����v�f�� <see cref="WeakDictionary&lt;TKey, TValue&gt;"/> ����������I�u�W�F�N�g�Ɋi�[����Ă���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public bool TryGetValue(TKey key, out TValue value) { return dict.TryGetValue(key, out value); }

		/// <summary><see cref="WeakDictionary&lt;TKey, TValue&gt;"/> ���̒l���i�[���Ă��� <see cref="System.Collections.Generic.ICollection&lt;T&gt;"/> ���擾���܂��B</summary>
		public ICollection<TValue> Values { get { return this.Select(x => x.Value).ToArray(); } }

		/// <summary>�w�肵���L�[�����v�f���擾�܂��͐ݒ肵�܂��B</summary>
		/// <param name="key">�擾�܂��͐ݒ肷��v�f�̃L�[�B</param>
		/// <returns>�w�肵���L�[�����v�f�B</returns>
		/// <exception cref="KeyNotFoundException">�v���p�e�B�͎擾����܂����A<paramref name="key"/> ��������܂���B</exception>
		public TValue this[TKey key]
		{
			get { return dict[key]; }
			set
			{
				// If the WeakHash already holds this value as a key, it will lead to a circular-reference and result in the objects being kept alive forever.
				// The caller needs to ensure that this cannot happen.
				Debug.Assert(!dict.ContainsKey(value));
				dict[new HashableWeakReference(key)] = value;
			}
		}

		/// <summary>
		/// Check if any of the keys have gotten collected
		/// 
		/// Currently, there is also no guarantee of how long the values will be kept alive even after the keys
		/// get collected. This could be fixed by triggerring CheckCleanup() to be called on every garbage-collection
		/// by having a dummy watch-dog object with a finalizer which calls CheckCleanup().
		/// </summary>
		void CheckCleanup()
		{
			version++;
			long change = version - cleanupVersion;
			// Cleanup the table if it is a while since we have done it last time.
			// Take the size of the table into account.
			if (change > 1234 + dict.Count / 2)
			{
				// It makes sense to do the cleanup only if a GC has happened in the meantime.
				// WeakReferences can become zero only during the GC.
				bool garbage_collected;
				var currentGC = GC.CollectionCount(0);
				garbage_collected = currentGC != cleanupGC;
				if (garbage_collected)
				{
					cleanupGC = currentGC;
					Cleanup();
					cleanupVersion = version;
				}
				else
					cleanupVersion += 1234;
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2004:RemoveCallsToGCKeepAlive")]
		void Cleanup()
		{
			int liveCount = 0;
			int emptyCount = 0;
			foreach (HashableWeakReference w in dict.Keys)
			{
				TKey target;
				if (w.TryGetTarget(out target))
					liveCount++;
				else
					emptyCount++;
			}
			// Rehash the table if there is a significant number of empty slots
			if (emptyCount > liveCount / 4)
			{
				Dictionary<object, TValue> newtable = new Dictionary<object, TValue>(liveCount + liveCount / 4, comparer);
				foreach (var kvp in dict)
				{
					TKey target;
					if (((HashableWeakReference)kvp.Key).TryGetTarget(out target))
					{
						newtable[kvp.Key] = kvp.Value;
						GC.KeepAlive(target);
					}
				}
				dict = newtable;
			}
		}

		void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) { Add(item.Key, item.Value); }

		/// <summary><see cref="WeakDictionary&lt;TKey, TValue&gt;"/> ���炷�ׂĂ̍��ڂ��폜���܂��B</summary>
		public void Clear() { dict.Clear(); }

		[Confined]
		bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
		{
			TValue value;
			return TryGetValue(item.Key, out value) && EqualityComparer<TValue>.Default.Equals(value, item.Value);
		}

		void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			foreach (var kvp in this)
				array[arrayIndex++] = kvp;
		}

		/// <summary><see cref="WeakDictionary&lt;TKey, TValue&gt;"/> �Ɋi�[����Ă���v�f�̐����擾���܂��B</summary>
		public int Count
		{
			get
			{
				int count = 0;
				foreach (var kvp in this)
					count++;
				return count;
			}
		}

		/// <summary><see cref="WeakDictionary&lt;TKey, TValue&gt;"/> ���ǂݎ���p���ǂ����������l���擾���܂��B</summary>
		public bool IsReadOnly { get { return false; } }

		bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
		{
			TValue value;
			if (TryGetValue(item.Key, out value) && EqualityComparer<TValue>.Default.Equals(value, item.Value))
				return Remove(item.Key);
			return false;
		}

		/// <summary>�R���N�V�����𔽕���������񋓎q��Ԃ��܂��B</summary>
		/// <returns>�R���N�V�����𔽕��������邽�߂Ɏg�p�ł��� <see cref="System.Collections.Generic.IEnumerator&lt;T&gt;"/>�B</returns>
		[Pure]
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			foreach (var kvp in dict)
			{
				TKey realKey;
				if (((HashableWeakReference)kvp.Key).TryGetTarget(out realKey))
					yield return new KeyValuePair<TKey, TValue>(realKey, kvp.Value);
			}
		}

		[Pure]
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}

	/// <summary>�ʏ�̎Q�ƂƎア�Q�Ƃ̗����ŃI�u�W�F�N�g�� ID ���}�b�s���O������@��񋟂��܂��B</summary>
	/// <typeparam name="T">�}�b�s���O�Ώۂ̃I�u�W�F�N�g�̌^���w�肵�܂��B</typeparam>
	public sealed class HybridMapping<T> where T : class
	{
		Dictionary<int, object> _dict = new Dictionary<int, object>();
		readonly object _synchObject = new object();
		readonly int _minimum;
		int _current;

		const int SIZE = 4096;
		const int MIN_RANGE = SIZE / 2;

		/// <summary><see cref="Microsoft.Scripting.Utils.HybridMapping&lt;T&gt;"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		public HybridMapping() : this(0) { }

		/// <summary>ID �̍ŏ��l���g�p���āA<see cref="Microsoft.Scripting.Utils.HybridMapping&lt;T&gt;"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="minimum">���蓖�Ă��� ID �̍ŏ��l���w�肵�܂��B</param>
		public HybridMapping(int minimum)
		{
			if (minimum < 0 || (SIZE - minimum) < MIN_RANGE)
				throw new ArgumentOutOfRangeException("offset", "invalid offset value");
			_minimum = minimum;
			_current = minimum;
		}

		int Add(object value)
		{
			lock (_synchObject)
			{
				var saved = _current;
				while (_dict.ContainsKey(_current))
				{
					if (++_current >= SIZE)
						_current = _minimum;
					if (_current == saved)
						throw new InvalidOperationException("HybridMapping is full");
				}
				_dict.Add(_current, value);
				return _current;
			}
		}

		static T GetActualValue(object value)
		{
			Debug.Assert(value is T || value is WeakReference<T>);
			T target;
			var wref = value as WeakReference<T>;
			return wref != null ? (wref.TryGetTarget(out target) ? target : null) : (T)value;
		}

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�̎�Q�Ƃ��}�b�s���O�ɒǉ����āA���̃}�b�s���O�̃I�u�W�F�N�g�ɑ΂��� ID ��Ԃ��܂��B</summary>
		/// <param name="value">�}�b�s���O����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns><paramref name="value"/> �ɑ΂��� ID�B</returns>
		public int WeakAdd(T value) { return Add(new WeakReference<T>(value)); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g���}�b�s���O�ɒǉ����āA���̃}�b�s���O�̃I�u�W�F�N�g�ɑ΂��� ID ��Ԃ��܂��B</summary>
		/// <param name="value">�}�b�s���O����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns><paramref name="value"/> �ɑ΂��� ID�B</returns>
		public int StrongAdd(T value) { return Add(value); }

		/// <summary>�w�肳�ꂽ ID �ɑΉ�����I�u�W�F�N�g���擾���܂��B</summary>
		/// <param name="id">�Ή�����I�u�W�F�N�g���擾���� ID ���w�肵�܂��B</param>
		/// <returns><paramref name="id"/> �ɑΉ�����I�u�W�F�N�g�����݂���ꍇ�͂��̃I�u�W�F�N�g�B����ȊO�̏ꍇ�� <c>null</c>�B</returns>
		public T GetObjectForId(int id)
		{
			object ret;
			return _dict.TryGetValue(id, out ret) ? GetActualValue(ret) : null;
		}

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�ɑΉ����� ID ���擾���܂��B</summary>
		/// <param name="value">�Ή����� ID ���擾����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns><paramref name="value"/> �����̃}�b�s���O�ɑ��݂���ꍇ�͂��� ID�B����ȊO�̏ꍇ�� -1�B</returns>
		public int GetIdForObject(T value)
		{
			lock (_synchObject)
			{
				var result = _dict.Select(x => (KeyValuePair<int, object>?)x).FirstOrDefault(x => EqualityComparer<T>.Default.Equals(GetActualValue(x.Value.Value), value));
				if (result != null)
					return result.Value.Key;
			}
			return -1;
		}

		/// <summary>�w�肳�ꂽ ID �ɑΉ�����I�u�W�F�N�g�����̃}�b�s���O����폜���܂��B</summary>
		/// <param name="id">�폜����I�u�W�F�N�g�� ID ���w�肵�܂��B</param>
		public void RemoveById(int id)
		{
			lock (_synchObject)
				_dict.Remove(id);
		}

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�����̃}�b�s���O����폜���܂��B</summary>
		/// <param name="value">�폜����I�u�W�F�N�g���w�肵�܂��B</param>
		public void Remove(T value) { RemoveById(GetIdForObject(value)); }
	}
}
