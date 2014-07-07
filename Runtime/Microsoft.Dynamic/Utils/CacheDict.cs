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

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Scripting.Utils
{
	/// <summary>
	/// �\�z���Ɏw�肵���ő�e�ʂ�ێ�����L���b�V���Ɏg�p�����f�B�N�V���i���Ɏ����I�u�W�F�N�g��񋟂��܂��B
	/// ���̃N���X�̓X���b�h�Z�[�t�ł͂���܂���B
	/// </summary>
	public class CacheDict<TKey, TValue>
	{
		readonly Dictionary<TKey, ValueInfo> _dict = new Dictionary<TKey, ValueInfo>();
		readonly LinkedList<TKey> _list = new LinkedList<TKey>();
		readonly int _capacity;

		/// <summary>�ő�e�ʂ��w�肵�āA<see cref="Microsoft.Scripting.Utils.CacheDict&lt;TKey, TValue&gt;"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="capacity">�i�[����v�f�̍ő�ʂ��w�肵�܂��B</param>
		public CacheDict(int capacity) { _capacity = capacity; }

		/// <summary>�w�肳�ꂽ�L�[�Ɋ֘A�t����ꂽ�l�̎擾�����݂܂��B</summary>
		/// <param name="key">�֘A�t����ꂽ�l���擾����L�[���w�肵�܂��B</param>
		/// <param name="value">�L�[�Ɋ֘A�t����ꂽ�l���Ԃ���܂��B</param>
		/// <returns>�L�[�ɑΉ�����l�����݂���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public bool TryGetValue(TKey key, out TValue value)
		{
			ValueInfo storedValue;
			if (_dict.TryGetValue(key, out storedValue))
			{
				if (storedValue.Node.Previous != null)
				{
					// �l�̏���擪�Ɉړ�
					_list.Remove(storedValue.Node);
					_list.AddFirst(storedValue.Node);
				}
				value = storedValue.Value;
				return true;
			}
			value = default(TValue);
			return false;
		}

		/// <summary>�w�肳�ꂽ�L�[����ђl��ǉ����܂��B���łɃL�[�����݂��Ă����ꍇ�͌��̒l��u�������܂��B</summary>
		/// <param name="key">�ǉ�����L�[���w�肵�܂��B</param>
		/// <param name="value">�L�[�Ɋ֘A�t����ꂽ�l���w�肵�܂��B</param>
		public void Add(TKey key, TValue value)
		{
			ValueInfo valueInfo;
			if (_dict.TryGetValue(key, out valueInfo))
				_list.Remove(valueInfo.Node); // �����N���X�g���猳�̍��ڂ��폜
			else if (_list.Count == _capacity)
			{
				// �e�ʂɒB�����̂ŁA�����N���X�g�̍Ō�̗v�f���폜
				var node = _list.Last;
				_list.RemoveLast();
				var successful = _dict.Remove(node.Value);
				Debug.Assert(successful);
			}
			// �V�������ڂ����X�g�̍ŏ��ƃf�B�N�V���i���ɒǉ�
			var listNode = new LinkedListNode<TKey>(key);
			_list.AddFirst(listNode);
			_dict[key] = new ValueInfo(value, listNode);
		}

		/// <summary>�w�肳�ꂽ�L�[�Ɋ֘A�t����ꂽ�l���擾�܂��͐ݒ肵�܂��B</summary>
		/// <param name="key">�l�ɑΉ�����L�[���w�肵�܂��B</param>
		/// <returns>�L�[�Ɋ֘A�t����ꂽ�l�B</returns>
		public TValue this[TKey key]
		{
			get
			{
				TValue res;
				if (TryGetValue(key, out res))
					return res;
				throw new KeyNotFoundException(key.ToString());
			}
			set { Add(key, value); }
		}

		struct ValueInfo
		{
			internal readonly TValue Value;
			internal readonly LinkedListNode<TKey> Node;

			internal ValueInfo(TValue value, LinkedListNode<TKey> node)
			{
				Value = value;
				Node = node;
			}
		}
	}
}
