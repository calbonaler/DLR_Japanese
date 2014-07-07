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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Contracts;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>
	/// �����o�̃R���N�V�����̊i�[�Ɏg�p�����P���ȃX���b�h�Z�[�t�̃f�B�N�V���i���ł��B
	/// ���̂��ׂẴV���{���f�B�N�V���i���Ɠ��l�ɂ��̃N���X�� <see cref="SymbolId"/> �ƃI�u�W�F�N�g�̗����ɂ�錟�����T�|�[�g���܂��B
	/// </summary>
	/// <remarks>
	/// �V���{���f�B�N�V���i���͒ʏ탊�e����������ɂ���ăC���f�b�N�X����܂��B
	/// �܂��A���̕�����̓V���{�����g�p���ăn���h������܂��B
	/// ������������ȊO�̃L�[��F�߂錾������݂��܂��B
	/// ���̏ꍇ�̓I�u�W�F�N�g�ɂ���ăC���f�b�N�X�����f�B�N�V���i�����쐬���āA�V���{���ɂ���ăC���f�b�N�X�����f�B�N�V���i�����ɕێ����܂��B
	/// ���̂悤�ȃA�N�Z�X�͒ᑬ�ł������e�ł�����̂ł��B
	/// </remarks>
	public sealed class SymbolDictionary : BaseSymbolDictionary, IDictionary, IDictionary<object, object>, IAttributesCollection
	{
		Dictionary<SymbolId, object> _data = new Dictionary<SymbolId, object>();

		/// <summary><see cref="Microsoft.Scripting.Runtime.SymbolDictionary"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		public SymbolDictionary() { }

		/// <summary>��ɂ��� <see cref="IAttributesCollection"/> ���g�p���āA<see cref="Microsoft.Scripting.Runtime.SymbolDictionary"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="from">�v�f���R�s�[����� <see cref="IAttributesCollection"/> ���w�肵�܂��B</param>
		public SymbolDictionary(IAttributesCollection from)
		{
			// enumeration of a dictionary requires locking the target dictionary.
			lock (from)
			{
				foreach (var kvp in from)
					Add(kvp.Key, kvp.Value);
			}
		}

		Dictionary<object, object> GetObjectKeysDictionary()
		{
			var objData = GetObjectKeysDictionaryIfExists();
			if (objData == null)
				_data.Add(ObjectKeys, objData = new Dictionary<object, object>());
			return objData;
		}

		Dictionary<object, object> GetObjectKeysDictionaryIfExists()
		{
			object objData;
			return _data.TryGetValue(ObjectKeys, out objData) ? (Dictionary<object, object>)objData : null;
		}

		/// <summary>�w�肵���L�[����ђl�����v�f�� <see cref="SymbolDictionary"/> �ɒǉ����܂��B</summary>
		/// <param name="key">�ǉ�����v�f�̃L�[�Ƃ��Ďg�p����I�u�W�F�N�g�B</param>
		/// <param name="value">�ǉ�����v�f�̒l�Ƃ��Ďg�p����I�u�W�F�N�g�B</param>
		public void Add(object key, object value)
		{
			Debug.Assert(!(key is SymbolId));
			var strKey = key as string;
			lock (this)
			{
				if (strKey != null)
					_data.Add(SymbolTable.StringToId(strKey), value);
				else
					GetObjectKeysDictionary()[key] = value;
			}
		}

		/// <summary>�w�肵���L�[�̗v�f�� <see cref="SymbolDictionary"/> �Ɋi�[����Ă��邩�ǂ������m�F���܂��B</summary>
		/// <param name="key"><see cref="SymbolDictionary"/> ���Ō��������L�[�B</param>
		/// <returns>�w�肵���L�[�����v�f�� <see cref="SymbolDictionary"/> ���ێ����Ă���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		[Confined]
		public bool ContainsKey(object key)
		{
			Debug.Assert(!(key is SymbolId));
			var strKey = key as string;
			lock (this)
			{
				if (strKey != null)
					return SymbolTable.StringHasId(strKey) && _data.ContainsKey(SymbolTable.StringToId(strKey));
				else
				{
					var objData = GetObjectKeysDictionaryIfExists();
					return objData != null && objData.ContainsKey(key);
				}
			}
		}

		/// <summary><see cref="SymbolDictionary"/> �̃L�[��ێ����Ă��� <see cref="ICollection&lt;Object&gt;"/> ���擾���܂��B</summary>
		public ICollection<object> Keys
		{
			get
			{
				lock (this)
				{
					IEnumerable<object> res = _data.Keys.Where(x => x != ObjectKeys).Select(x => SymbolTable.IdToString(x));
					var objData = GetObjectKeysDictionaryIfExists();
					return (objData != null ? res.Concat(objData.Keys) : res).ToArray();
				}
			}
		}

		/// <summary>�w�肵���L�[�����v�f�� <see cref="SymbolDictionary"/> ����폜���܂��B</summary>
		/// <param name="key">�폜����v�f�̃L�[�B</param>
		/// <returns>
		/// �v�f������ɍ폜���ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B
		/// ���̃��\�b�h�́A<paramref name="key"/> ������ <see cref="SymbolDictionary"/> �Ɍ�����Ȃ������ꍇ�ɂ� <c>false</c> ��Ԃ��܂��B
		/// </returns>
		public bool Remove(object key)
		{
			Debug.Assert(!(key is SymbolId));
			var strKey = key as string;
			lock (this)
			{
				if (strKey != null)
					return SymbolTable.StringHasId(strKey) && _data.Remove(SymbolTable.StringToId(strKey));
				else
				{
					var objData = GetObjectKeysDictionaryIfExists();
					return objData != null && objData.Remove(key);
				}
			}
		}

		/// <summary>�w�肵���L�[�Ɋ֘A�t�����Ă���l���擾���܂��B</summary>
		/// <param name="key">�l���擾����Ώۂ̃L�[�B</param>
		/// <param name="value">
		/// ���̃��\�b�h���Ԃ����Ƃ��ɁA�L�[�����������ꍇ�́A�w�肵���L�[�Ɋ֘A�t�����Ă���l�B����ȊO�̏ꍇ�� <c>null</c>�B
		/// ���̃p�����[�^�[�͏����������ɓn����܂��B
		/// </param>
		/// <returns>�w�肵���L�[�����v�f�� <see cref="SymbolDictionary"/> �Ɋi�[����Ă���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public bool TryGetValue(object key, out object value)
		{
			Debug.Assert(!(key is SymbolId));
			var strKey = key as string;
			lock (this)
			{
				value = null;
				if (strKey != null)
					return SymbolTable.StringHasId(strKey) && _data.TryGetValue(SymbolTable.StringToId(strKey), out value);
				else
				{
					var objData = GetObjectKeysDictionaryIfExists();
					return objData != null && objData.TryGetValue(key, out value);
				}
			}
		}

		/// <summary><see cref="SymbolDictionary"/> �̒l��ێ����Ă��� <see cref="ICollection&lt;Object&gt;"/> ���擾���܂��B</summary>
		public ICollection<object> Values
		{
			get
			{
				lock (this)
				{
					var objData = GetObjectKeysDictionaryIfExists();
					return objData == null ? (ICollection<object>)_data.Values : _data.Where(x => x.Key != ObjectKeys).Select(x => x.Value).Concat(objData.Values).ToArray();
				}
			}
		}

		/// <summary>�w�肵���L�[�����v�f���擾�܂��͐ݒ肵�܂��B</summary>
		/// <param name="key">�擾�܂��͐ݒ肷��v�f�̃L�[�B</param>
		/// <returns>�w�肵���L�[�����v�f�B</returns>
		public object this[object key]
		{
			get
			{
				object value;
				if (TryGetValue(key, out value))
					return value;
				throw new KeyNotFoundException(string.Format("'{0}'", key));
			}
			set
			{
				Debug.Assert(!(key is SymbolId));
				var strKey = key as string;
				lock (this)
				{
					if (strKey != null)
						_data[SymbolTable.StringToId(strKey)] = value;
					else
						GetObjectKeysDictionary()[key] = value;
				}
			}
		}

		void ICollection<KeyValuePair<object, object>>.Add(KeyValuePair<object, object> item) { Add(item.Key, item.Value); }

		/// <summary><see cref="SymbolDictionary"/> ���炷�ׂĂ̍��ڂ��폜���܂��B</summary>
		public void Clear() { lock (this) _data.Clear(); }

		[Confined]
		bool ICollection<KeyValuePair<object, object>>.Contains(KeyValuePair<object, object> item)
		{
			object value;
			return TryGetValue(item.Key, out value) && value == item.Value;
		}

		void ICollection<KeyValuePair<object, object>>.CopyTo(KeyValuePair<object, object>[] array, int arrayIndex)
		{
			ContractUtils.RequiresNotNull(array, "array");
			ContractUtils.RequiresArrayRange(array, arrayIndex, Count, "arrayIndex", "array");
			lock (this)
			{
				foreach (var o in AsObjectKeyedDictionary())
					array[arrayIndex++] = o;
			}
		}

		/// <summary><see cref="SymbolDictionary"/> �Ɋi�[����Ă���v�f�̐����擾���܂��B</summary>
		public int Count
		{
			get
			{
				lock (this)
				{
					var objData = GetObjectKeysDictionaryIfExists();
					return objData == null ? _data.Count : _data.Count + objData.Count - 1; // -1 is because data contains objData
				}
			}
		}

		/// <summary><see cref="SymbolDictionary"/> ���ǂݎ���p���ǂ����������l���擾���܂��B</summary>
		public bool IsReadOnly { get { return false; } }

		bool ICollection<KeyValuePair<object, object>>.Remove(KeyValuePair<object, object> item) { return ((ICollection<KeyValuePair<object, object>>)this).Contains(item) && Remove(item.Key); }

		[Pure]
		IEnumerator<KeyValuePair<object, object>> IEnumerable<KeyValuePair<object, object>>.GetEnumerator() { return GetEnumerator(); }

		[Pure]
		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

		/// <summary>�w�肵���L�[����ђl�����v�f�� <see cref="SymbolDictionary"/> �ɒǉ����܂��B</summary>
		/// <param name="name">�ǉ�����v�f�̃L�[�Ƃ��Ďg�p���� <see cref="SymbolId"/>�B</param>
		/// <param name="value">�ǉ�����v�f�̒l�Ƃ��Ďg�p����I�u�W�F�N�g�B</param>
		public void Add(SymbolId name, object value) { lock (this) _data.Add(name, value); }

		/// <summary>�w�肵���L�[�̗v�f�� <see cref="SymbolDictionary"/> �Ɋi�[����Ă��邩�ǂ������m�F���܂��B</summary>
		/// <param name="name"><see cref="SymbolDictionary"/> ���Ō��������L�[�B</param>
		/// <returns>�w�肵���L�[�����v�f�� <see cref="SymbolDictionary"/> ���ێ����Ă���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public bool ContainsKey(SymbolId name) { lock (this) return _data.ContainsKey(name); }

		/// <summary>�w�肵���L�[�����v�f�� <see cref="SymbolDictionary"/> ����폜���܂��B</summary>
		/// <param name="name">�폜����v�f�̃L�[�B</param>
		/// <returns>
		/// �v�f������ɍ폜���ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B
		/// ���̃��\�b�h�́A<paramref name="name"/> ������ <see cref="SymbolDictionary"/> �Ɍ�����Ȃ������ꍇ�ɂ� <c>false</c> ��Ԃ��܂��B
		/// </returns>
		public bool Remove(SymbolId name) { lock (this) return _data.Remove(name); }

		/// <summary>�w�肵���L�[�Ɋ֘A�t�����Ă���l���擾���܂��B</summary>
		/// <param name="name">�l���擾����Ώۂ̃L�[�B</param>
		/// <param name="value">
		/// ���̃��\�b�h���Ԃ����Ƃ��ɁA�L�[�����������ꍇ�́A�w�肵���L�[�Ɋ֘A�t�����Ă���l�B����ȊO�̏ꍇ�� <c>null</c>�B
		/// ���̃p�����[�^�[�͏����������ɓn����܂��B
		/// </param>
		/// <returns>�w�肵���L�[�����v�f�� <see cref="SymbolDictionary"/> �Ɋi�[����Ă���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public bool TryGetValue(SymbolId name, out object value) { lock (this) return _data.TryGetValue(name, out value); }

		/// <summary>�w�肵���L�[�����v�f���擾�܂��͐ݒ肵�܂��B</summary>
		/// <param name="name">�擾�܂��͐ݒ肷��v�f�̃L�[�B</param>
		/// <returns>�w�肵���L�[�����v�f�B</returns>
		public object this[SymbolId name]
		{
			get { lock (this) return _data[name]; }
			set { lock (this) _data[name] = value; }
		}

		/// <summary><see cref="SymbolId"/> ���L�[�ł��鑮���̃f�B�N�V���i�����擾���܂��B</summary>
		public IDictionary<SymbolId, object> SymbolAttributes { get { lock (this) return GetObjectKeysDictionaryIfExists() == null ? _data : _data.Where(x => x.Key != ObjectKeys).ToDictionary(x => x.Key, x => x.Value); } }

		/// <summary>���̃I�u�W�F�N�g�� <see cref="System.Collections.Generic.IDictionary&lt;Object, Object&gt;"/> �Ƃ��Ď擾���܂��B</summary>
		/// <returns><see cref="System.Collections.Generic.IDictionary&lt;Object, Object&gt;"/> �Ƃ����`���Ŏ擾���ꂽ���݂̃I�u�W�F�N�g�B</returns>
		public IDictionary<object, object> AsObjectKeyedDictionary() { return this; }

		[Pure]
		bool IDictionary.Contains(object key) { return ContainsKey(key); }

		[Pure]
		IDictionaryEnumerator IDictionary.GetEnumerator() { return GetEnumerator(); }

		/// <summary>���̃R���N�V�����̗v�f��񋓂��邽�߂̗񋓎q��Ԃ��܂��B</summary>
		/// <returns>�v�f�̗񋓂Ɏg�p�����񋓎q�B</returns>
		[Pure]
		public CheckedDictionaryEnumerator GetEnumerator()
		{
			var dataEnum = new TransformDictionaryEnumerator(_data);
			var objData = GetObjectKeysDictionaryIfExists();
			return objData == null ? (CheckedDictionaryEnumerator)dataEnum : new DictionaryUnionEnumerator(new IDictionaryEnumerator[] { dataEnum, objData.GetEnumerator() });
		}

		bool IDictionary.IsFixedSize { get { return false; } }

		ICollection IDictionary.Keys { get { return new List<object>(Keys); } }

		void IDictionary.Remove(object key) { Remove(key); }

		ICollection IDictionary.Values { get { return new List<object>(Values); } }

		void ICollection.CopyTo(Array array, int index)
		{
			ContractUtils.RequiresNotNull(array, "array");
			ContractUtils.RequiresArrayRange(array.Length, index, Count, "index", "array");
			lock (this)
			{
				foreach (var o in this)
					array.SetValue(o, index++);
			}
		}

		bool ICollection.IsSynchronized { get { return true; } }

		object ICollection.SyncRoot { get { return this; } } // TODO: We should really lock on something else...
	}
}
