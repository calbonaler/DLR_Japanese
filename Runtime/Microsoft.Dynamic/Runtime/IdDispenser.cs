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
using Microsoft.Contracts;
using SRC = System.Runtime.CompilerServices;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>�I�u�W�F�N�g�ɑ΂����ӎ��ʎq�̊��蓖�Ă��s���܂��B</summary>
	public static class IdDispenser
	{
		// ��r�q�̗B��̃C���X�^���X
		static readonly IEqualityComparer<object> _comparer = new WrapperComparer();
		[MultiRuntimeAware]
		static Dictionary<object, Wrapper> _hashtable = new Dictionary<object, Wrapper>(_comparer);
		static readonly object _synchObject = new object();  // �S�惍�b�N�ɑ΂���B��̃C���X�^���X
		// ��ӎ��ʎq�� long ���g�p���邱�Ƃŏd����S�z����K�v�͂���܂���B
		// 2005 �N���݂̃n�[�h�E�F�A�ł̓I�[�o�[�t���[����̂� 100 �N�ȏォ����܂��B
		[MultiRuntimeAware]
		static long _currentId = 42; // �ŋߓK�p������ӎ��ʎq
		// _cleanupId ����� _cleanupGC �̓n�b�V���e�[�u���N���[���A�b�v�̌����I�ȃX�P�W���[�����O�Ɏg�p����܂��B
		[MultiRuntimeAware]
		static long _cleanupId; // �ŋ߂̃N���[���A�b�v���� _currentId
		[MultiRuntimeAware]
		static int _cleanupGC; // �ŋ߂̃N���[���A�b�v���� GC.CollectionCount(0)

		/// <summary>�w�肳�ꂽ��ӎ��ʎq�Ɋ֘A�t����ꂽ�I�u�W�F�N�g���擾���܂��B</summary>
		/// <param name="id">�֘A�t����ꂽ�I�u�W�F�N�g���擾�����ӎ��ʎq���w�肵�܂��B</param>
		/// <returns>��ӎ��ʎq�Ɋ֘A�t����ꂽ�I�u�W�F�N�g�B�֘A�t����ꂽ�I�u�W�F�N�g�����݂��Ȃ��ꍇ�� <c>null</c> ��Ԃ��܂��B</returns>
		public static object GetObject(long id)
		{
			lock (_synchObject)
			{
				foreach (Wrapper w in _hashtable.Keys)
				{
					if (w.Target != null && w.Id == id)
						return w.Target;
				}
				return null;
			}
		}

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�ɑ΂����ӎ��ʎq���擾���܂��B</summary>
		/// <param name="o">��ӎ��ʎq���擾����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns>�I�u�W�F�N�g�Ɋ֘A�t����ꂽ��ӎ��ʎq�B</returns>
		public static long GetId(object o)
		{
			if (o == null)
				return 0;
			lock (_synchObject)
			{
				// �I�u�W�F�N�g�����݂��Ă���ꍇ�́A�����̎��ʎq��Ԃ�
				Wrapper res;
				if (_hashtable.TryGetValue(o, out res))
					return res.Id;
				var uniqueId = checked(++_currentId);
				var change = uniqueId - _cleanupId;
				// �Ō�̃N���[���A�b�v���璷���Ԍo�����ꍇ�́A�e�[�u�����N���[���A�b�v
				// �e�[�u���̃T�C�Y���v�Z�ɓ����
				if (change > 1234 + _hashtable.Count / 2)
				{
					// GC �����̊Ԃɔ������Ă������N���[���A�b�v���s���͈̂Ӗ�������
					// ��Q�Ƃ� GC �̊Ԃ� 0 �ɂȂ�
					var currentGC = GC.CollectionCount(0);
					if (currentGC != _cleanupGC)
					{
						Cleanup();
						_cleanupId = uniqueId;
						_cleanupGC = currentGC;
					}
					else
						_cleanupId += 1234;
				}
				var w = new Wrapper(o, uniqueId);
				_hashtable[w] = w;
				return uniqueId;
			}
		}

		/// <summary>�n�b�V���e�[�u���𑖍����āA��̗v�f���폜���܂��B</summary>
		static void Cleanup()
		{
			int liveCount = 0;
			int emptyCount = 0;
			foreach (Wrapper w in _hashtable.Keys)
			{
				if (w.Target != null)
					liveCount++;
				else
					emptyCount++;
			}
			// ��̃X���b�g�������o�Ă����ꍇ�́A�e�[�u�����ăn�b�V��
			if (emptyCount > liveCount / 4)
			{
				Dictionary<object, Wrapper> newtable = new Dictionary<object, Wrapper>(liveCount + liveCount / 4, _comparer);
				foreach (Wrapper w in _hashtable.Keys)
				{
					if (w.Target != null)
						newtable[w] = w;
				}
				_hashtable = newtable;
			}
		}

		/// <summary>�I�u�W�F�N�g�ւ̎�Q�ƁA�n�b�V���l�A�I�u�W�F�N�g ID ���L���b�V�������Q�ƃ��b�p�[��\���܂��B</summary>
		sealed class Wrapper
		{
			WeakReference _weakReference;
			int _hashCode;

			public Wrapper(object obj, long uniqueId)
			{
				_weakReference = new WeakReference(obj, true);
				_hashCode = obj == null ? 0 : SRC.RuntimeHelpers.GetHashCode(obj);
				Id = uniqueId;
			}

			public long Id { get; private set; }

			public object Target { get { return _weakReference.Target; } }

			[Confined]
			public override int GetHashCode() { return _hashCode; }
		}

		/// <summary><see cref="Wrapper"/> �𓧉߃G���x���[�v�Ƃ��Ĉ������l��r�q��\���܂��B</summary>
		sealed class WrapperComparer : IEqualityComparer<object>
		{
			bool IEqualityComparer<object>.Equals(object x, object y)
			{
				var wx = x as Wrapper;
				if (wx != null)
					x = wx.Target;
				var wy = y as Wrapper;
				if (wy != null)
					y = wy.Target;
				return ReferenceEquals(x, y);
			}

			int IEqualityComparer<object>.GetHashCode(object obj)
			{
				Wrapper wobj = obj as Wrapper;
				if (wobj != null)
					return wobj.GetHashCode();
				return obj == null ? 0 : SRC.RuntimeHelpers.GetHashCode(obj);
			}
		}
	}
}
