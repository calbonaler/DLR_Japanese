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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.Contracts;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>
	/// �œK�����ꂽ�X���b�h�Z�[�t�̃V���{���f�B�N�V���i���ɑ΂��钊�ۊ��N���X�ł��B
	/// �����҂͂��̃N���X����h�����āA<see cref="ExtraKeys"/>�A<see cref="TrySetExtraValue"/> ����� <see cref="TryGetExtraValue"/> ���I�[�o�[���C�h���Ă��������B
	/// �l�̌������͍ŏ��ɍœK�����ꂽ�֐����g�p���Ēǉ��̃L�[����������܂��B
	/// �l��������Ȃ������ꍇ�́A��ɂȂ� .NET �f�B�N�V���i���ɒl���i�[����܂��B
	/// </summary>
	public abstract class CustomSymbolDictionary : BaseSymbolDictionary, System.Collections.IDictionary, IDictionary<object, object>, IAttributesCollection
	{
		Dictionary<SymbolId, object> _data;

		/// <summary><see cref="Microsoft.Scripting.Runtime.CustomSymbolDictionary"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		protected CustomSymbolDictionary() { }

		/// <summary>���W���[���̍œK�����ꂽ�����ɂ���ăL���b�V�������ǉ��̃L�[���擾���܂��B</summary>
		/// <returns>�ǉ��̃L�[��\�� <see cref="SymbolId"/> �̔z��B</returns>
		protected abstract ReadOnlyCollection<SymbolId> ExtraKeys { get; }

		/// <summary>�ǉ��̒l�̐ݒ�����݁A�w�肳�ꂽ�L�[�ɑ΂���l������ɐݒ肳�ꂽ���ǂ����������l��Ԃ��܂��B</summary>
		/// <param name="key">�ݒ肷��l�ɑ΂���L�[���w�肵�܂��B</param>
		/// <param name="value">�ݒ肷��l���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�L�[�ɑ΂��Ēl������ɐݒ肳�ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		protected abstract bool TrySetExtraValue(SymbolId key, object value);

		/// <summary>�ǉ��̒l�̎擾�����݁A�w�肳�ꂽ�L�[�ɑ΂���l������Ɏ擾���ꂽ���ǂ����������l��Ԃ��܂��B�l�� <see cref="Uninitialized"/> �ł����Ă� <c>true</c> ��Ԃ��܂��B</summary>
		/// <param name="key">�擾����l�ɑ΂���L�[���w�肵�܂��B</param>
		/// <param name="value">�擾���ꂽ�l���i�[����܂��B</param>
		/// <returns>�w�肳�ꂽ�L�[�ɑ΂��Ēl������Ɏ擾���ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate")]
		protected abstract bool TryGetExtraValue(SymbolId key, out object value);

		void InitializeData()
		{
			Debug.Assert(_data == null);
			_data = new Dictionary<SymbolId, object>();
		}

		Dictionary<object, object> GetObjectKeysDictionary()
		{
			var objData = GetObjectKeysDictionaryIfExists();
			if (objData == null)
			{
				if (_data == null)
					InitializeData();
				_data.Add(ObjectKeys, objData = new Dictionary<object, object>());
			}
			return objData;
		}

		Dictionary<object, object> GetObjectKeysDictionaryIfExists()
		{
			if (_data == null)
				return null;
			object objData;
			if (_data.TryGetValue(ObjectKeys, out objData))
				return (Dictionary<object, object>)objData;
			return null;
		}

		/// <summary>�w�肵���L�[����ђl�����v�f�� <see cref="CustomSymbolDictionary"/> �ɒǉ����܂��B</summary>
		/// <param name="key">�ǉ�����v�f�̃L�[�Ƃ��Ďg�p����I�u�W�F�N�g�B</param>
		/// <param name="value">�ǉ�����v�f�̒l�Ƃ��Ďg�p����I�u�W�F�N�g�B</param>
		public void Add(object key, object value)
		{
			Debug.Assert(!(key is SymbolId));
			var strKey = key as string;
			if (strKey != null)
				Add(SymbolTable.StringToId(strKey), value);
			else
				lock (this) GetObjectKeysDictionary()[key] = value;
		}

		/// <summary>�w�肵���L�[�̗v�f�� <see cref="CustomSymbolDictionary"/> �Ɋi�[����Ă��邩�ǂ������m�F���܂��B</summary>
		/// <param name="key"><see cref="CustomSymbolDictionary"/> ���Ō��������L�[�B</param>
		/// <returns>�w�肵���L�[�����v�f�� <see cref="CustomSymbolDictionary"/> ���ێ����Ă���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		[Confined]
		public bool ContainsKey(object key)
		{
			Debug.Assert(!(key is SymbolId));
			lock (this)
			{
				object dummy;
				return TryGetValue(key, out dummy);
			}
		}

		/// <summary><see cref="CustomSymbolDictionary"/> �̃L�[��ێ����Ă��� <see cref="ICollection&lt;Object&gt;"/> ���擾���܂��B</summary>
		public ICollection<object> Keys { get { lock (this) return AsObjectKeyedDictionary().Select(x => x.Key).ToArray(); } }

		/// <summary>�w�肵���L�[�����v�f�� <see cref="CustomSymbolDictionary"/> ����폜���܂��B</summary>
		/// <param name="key">�폜����v�f�̃L�[�B</param>
		/// <returns>
		/// �v�f������ɍ폜���ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B
		/// ���̃��\�b�h�́A<paramref name="key"/> ������ <see cref="CustomSymbolDictionary"/> �Ɍ�����Ȃ������ꍇ�ɂ� <c>false</c> ��Ԃ��܂��B
		/// </returns>
		public bool Remove(object key)
		{
			Debug.Assert(!(key is SymbolId));
			string strKey = key as string;
			if (strKey != null)
				return SymbolTable.StringHasId(strKey) && Remove(SymbolTable.StringToId(strKey));
			lock (this)
			{
				var objData = GetObjectKeysDictionaryIfExists();
				if (objData == null)
					return false;
				return objData.Remove(key);
			}
		}

		/// <summary>�w�肵���L�[�Ɋ֘A�t�����Ă���l���擾���܂��B</summary>
		/// <param name="key">�l���擾����Ώۂ̃L�[�B</param>
		/// <param name="value">
		/// ���̃��\�b�h���Ԃ����Ƃ��ɁA�L�[�����������ꍇ�́A�w�肵���L�[�Ɋ֘A�t�����Ă���l�B����ȊO�̏ꍇ�� <c>null</c>�B
		/// ���̃p�����[�^�[�͏����������ɓn����܂��B
		/// </param>
		/// <returns>�w�肵���L�[�����v�f�� <see cref="CustomSymbolDictionary"/> �Ɋi�[����Ă���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public bool TryGetValue(object key, out object value)
		{
			Debug.Assert(!(key is SymbolId));
			string strKey = key as string;
			value = null;
			if (strKey != null)
				return SymbolTable.StringHasId(strKey) && TryGetValue(SymbolTable.StringToId(strKey), out value);
			lock (this)
			{
				var objData = GetObjectKeysDictionaryIfExists();
				if (objData != null)
					return objData.TryGetValue(key, out value);
			}
			return false;
		}

		/// <summary><see cref="CustomSymbolDictionary"/> �̒l��ێ����Ă��� <see cref="ICollection&lt;Object&gt;"/> ���擾���܂��B</summary>
		public ICollection<object> Values { get { lock (this) return AsObjectKeyedDictionary().Select(x => x.Value).ToArray(); } }

		/// <summary>�w�肵���L�[�����v�f���擾�܂��͐ݒ肵�܂��B</summary>
		/// <param name="key">�擾�܂��͐ݒ肷��v�f�̃L�[�B</param>
		/// <returns>�w�肵���L�[�����v�f�B</returns>
		public object this[object key]
		{
			get
			{
				Debug.Assert(!(key is SymbolId));
				object res;
				if (TryGetValue(key, out res))
					return res;
				throw new KeyNotFoundException(key.ToString());
			}
			set
			{
				Debug.Assert(!(key is SymbolId));
				string strKey = key as string;
				if (strKey != null)
					this[SymbolTable.StringToId(strKey)] = value;
				else
					lock (this) GetObjectKeysDictionary()[key] = value;
			}
		}

		void ICollection<KeyValuePair<object, object>>.Add(KeyValuePair<object, object> item) { Add(item.Key, item.Value); }

		/// <summary><see cref="CustomSymbolDictionary"/> ���炷�ׂĂ̍��ڂ��폜���܂��B</summary>
		public void Clear()
		{
			lock (this)
			{
				foreach (var key in ExtraKeys)
				{
					if (key.Id < 0)
						break;
					TrySetExtraValue(key, Uninitialized.Instance);
				}
				_data = null;
			}
		}

		[Confined]
		bool ICollection<KeyValuePair<object, object>>.Contains(KeyValuePair<object, object> item) { object o; return TryGetValue(item.Key, out o) && o == item.Value; }

		void ICollection<KeyValuePair<object, object>>.CopyTo(KeyValuePair<object, object>[] array, int arrayIndex)
		{
			ContractUtils.RequiresNotNull(array, "array");
			ContractUtils.RequiresArrayRange(array, arrayIndex, Count, "araryIndex", "Count");
			foreach (var kvp in ((IEnumerable<KeyValuePair<object, object>>)this))
				array[arrayIndex++] = kvp;
		}

		/// <summary><see cref="CustomSymbolDictionary"/> �Ɋi�[����Ă���v�f�̐����擾���܂��B</summary>
		public int Count
		{
			get
			{
				int count = 0;
				foreach (var _ in this)
					count++;
				return count;
			}
		}

		/// <summary><see cref="CustomSymbolDictionary"/> ���ǂݎ���p���ǂ����������l���擾���܂��B</summary>
		public bool IsReadOnly { get { return false; } }

		bool ICollection<KeyValuePair<object, object>>.Remove(KeyValuePair<object, object> item) { return ((ICollection<KeyValuePair<object, object>>)this).Contains(item) && Remove(item.Key); }

		[Pure]
		IEnumerator<KeyValuePair<object, object>> IEnumerable<KeyValuePair<object, object>>.GetEnumerator() { return GetEnumerator(); }

		[Pure]
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }

		/// <summary>�w�肵���L�[����ђl�����v�f�� <see cref="CustomSymbolDictionary"/> �ɒǉ����܂��B</summary>
		/// <param name="name">�ǉ�����v�f�̃L�[�Ƃ��Ďg�p���� <see cref="SymbolId"/>�B</param>
		/// <param name="value">�ǉ�����v�f�̒l�Ƃ��Ďg�p����I�u�W�F�N�g�B</param>
		public void Add(SymbolId name, object value)
		{
			lock (this)
			{
				if (TrySetExtraValue(name, value))
					return;
				if (_data == null)
					InitializeData();
				_data.Add(name, value);
			}
		}

		/// <summary>�w�肵���L�[�̗v�f�� <see cref="CustomSymbolDictionary"/> �Ɋi�[����Ă��邩�ǂ������m�F���܂��B</summary>
		/// <param name="name"><see cref="CustomSymbolDictionary"/> ���Ō��������L�[�B</param>
		/// <returns>�w�肵���L�[�����v�f�� <see cref="CustomSymbolDictionary"/> ���ێ����Ă���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public bool ContainsKey(SymbolId name)
		{
			object value;
			return TryGetValue(name, out value);
		}

		/// <summary>�w�肵���L�[�����v�f�� <see cref="CustomSymbolDictionary"/> ����폜���܂��B</summary>
		/// <param name="name">�폜����v�f�̃L�[�B</param>
		/// <returns>
		/// �v�f������ɍ폜���ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B
		/// ���̃��\�b�h�́A<paramref name="name"/> ������ <see cref="CustomSymbolDictionary"/> �Ɍ�����Ȃ������ꍇ�ɂ� <c>false</c> ��Ԃ��܂��B
		/// </returns>
		public bool Remove(SymbolId name)
		{
			object value;
			if (TryGetExtraValue(name, out value))
			{
				if (value == Uninitialized.Instance)
					return false;
				if (TrySetExtraValue(name, Uninitialized.Instance))
					return true;
			}
			if (_data == null)
				return false;
			lock (this)
				return _data.Remove(name);
		}

		/// <summary>�w�肵���L�[�Ɋ֘A�t�����Ă���l���擾���܂��B</summary>
		/// <param name="name">�l���擾����Ώۂ̃L�[�B</param>
		/// <param name="value">
		/// ���̃��\�b�h���Ԃ����Ƃ��ɁA�L�[�����������ꍇ�́A�w�肵���L�[�Ɋ֘A�t�����Ă���l�B����ȊO�̏ꍇ�� <c>null</c>�B
		/// ���̃p�����[�^�[�͏����������ɓn����܂��B
		/// </param>
		/// <returns>�w�肵���L�[�����v�f�� <see cref="CustomSymbolDictionary"/> �Ɋi�[����Ă���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public bool TryGetValue(SymbolId name, out object value)
		{
			if (TryGetExtraValue(name, out value) && value != Uninitialized.Instance)
				return true;
			if (_data == null)
				return false;
			lock (this)
				return _data.TryGetValue(name, out value);
		}

		/// <summary>�w�肵���L�[�����v�f���擾�܂��͐ݒ肵�܂��B</summary>
		/// <param name="name">�擾�܂��͐ݒ肷��v�f�̃L�[�B</param>
		/// <returns>�w�肵���L�[�����v�f�B</returns>
		public object this[SymbolId name]
		{
			get
			{
				object res;
				if (TryGetValue(name, out res))
					return res;
				throw new KeyNotFoundException(SymbolTable.IdToString(name));
			}
			set
			{
				if (TrySetExtraValue(name, value))
					return;
				lock (this)
				{
					if (_data == null)
						InitializeData();
					_data[name] = value;
				}
			}
		}

		/// <summary><see cref="SymbolId"/> ���L�[�ł��鑮���̃f�B�N�V���i�����擾���܂��B</summary>
		public IDictionary<SymbolId, object> SymbolAttributes
		{
			get
			{
				Dictionary<SymbolId, object> d;
				lock (this)
				{
					if (_data != null)
						d = new Dictionary<SymbolId, object>(_data);
					else
						d = new Dictionary<SymbolId, object>();
					foreach (var extraKey in ExtraKeys)
					{
						object value;
						if (TryGetExtraValue(extraKey, out value) && value != Uninitialized.Instance)
							d.Add(extraKey, value);
					}
				}
				return d;
			}
		}

		/// <summary>���̃I�u�W�F�N�g�� <see cref="System.Collections.Generic.IDictionary&lt;Object, Object&gt;"/> �Ƃ��Ď擾���܂��B</summary>
		/// <returns><see cref="System.Collections.Generic.IDictionary&lt;Object, Object&gt;"/> �Ƃ����`���Ŏ擾���ꂽ���݂̃I�u�W�F�N�g�B</returns>
		public IDictionary<object, object> AsObjectKeyedDictionary() { return this; }

		[Pure]
		bool System.Collections.IDictionary.Contains(object key) { return ContainsKey(key); }

		[Pure]
		System.Collections.IDictionaryEnumerator System.Collections.IDictionary.GetEnumerator() { return GetEnumerator(); }

		class ExtraKeyEnumerator : CheckedDictionaryEnumerator
		{
			CustomSymbolDictionary _idDict;
			int _curIndex = -1;

			public ExtraKeyEnumerator(CustomSymbolDictionary idDict) { _idDict = idDict; }

			protected override object KeyCore { get { return SymbolTable.IdToString(_idDict.ExtraKeys[_curIndex]); } }

			protected override object ValueCore
			{
				get
				{
					object val;
					var hasExtraValue = _idDict.TryGetExtraValue(_idDict.ExtraKeys[_curIndex], out val);
					Debug.Assert(hasExtraValue && !(val is Uninitialized));
					return val;
				}
			}

			protected override bool MoveNextCore()
			{
				while (_curIndex < _idDict.ExtraKeys.Count - 1)
				{
					_curIndex++;
					if (_idDict.ExtraKeys[_curIndex].Id < 0)
						break;
					object val;
					if (_idDict.TryGetExtraValue(_idDict.ExtraKeys[_curIndex], out val) && val != Uninitialized.Instance)
						return true;
				}
				return false;
			}

			protected override void ResetCore() { _curIndex = -1; }
		}

		/// <summary>���̃R���N�V�����̗v�f��񋓂��邽�߂̗񋓎q��Ԃ��܂��B</summary>
		/// <returns>�v�f�̗񋓂Ɏg�p�����񋓎q�B</returns>
		[Pure]
		public CheckedDictionaryEnumerator GetEnumerator()
		{
			List<System.Collections.IDictionaryEnumerator> enums = new List<System.Collections.IDictionaryEnumerator>();
			enums.Add(new ExtraKeyEnumerator(this));
			if (_data != null)
				enums.Add(new TransformDictionaryEnumerator(_data));
			var objItems = GetObjectKeysDictionaryIfExists();
			if (objItems != null)
				enums.Add(objItems.GetEnumerator());
			return new DictionaryUnionEnumerator(enums);
		}

		bool System.Collections.IDictionary.IsFixedSize { get { return false; } }

		System.Collections.ICollection System.Collections.IDictionary.Keys { get { return new List<object>(Keys); } }

		void System.Collections.IDictionary.Remove(object key) { Remove(key); }

		System.Collections.ICollection System.Collections.IDictionary.Values { get { return new List<object>(Values); } }

		void System.Collections.ICollection.CopyTo(Array array, int index)
		{
			foreach (System.Collections.DictionaryEntry entry in this)
				array.SetValue(entry, index++);
		}

		bool System.Collections.ICollection.IsSynchronized { get { return true; } }

		object System.Collections.ICollection.SyncRoot { get { return this; } } // TODO: Sync root shouldn't be this, it should be data.
	}
}
