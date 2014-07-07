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

namespace Microsoft.Scripting.Runtime
{
	/// <summary>����R���e�L�X�g��\���܂��B</summary>
	/// <remarks>
	/// �T�^�I�ɂ��ꂼ��̌���Ɋ֘A�t����ꂽ�R���e�L�X�g�������Ƃ� 1 ���݂��܂����A
	/// �قȂ鈵���������R�[�h�����ʂ��邽�߂� 1 �ȏ�̃R���e�L�X�g���g�p���錾�������܂��B
	/// �R���e�L�X�g�̓����o�܂��͉��Z�q�̒T�����Ɏg�p����܂��B
	/// </remarks>
	[Serializable]
	public struct ContextId : IEquatable<ContextId>
	{
		static Dictionary<object, ContextId> _contexts = new Dictionary<object, ContextId>();
		static int _maxId = 1;

		/// <summary>��̃R���e�L�X�g��\���܂��B</summary>
		public static readonly ContextId Empty = new ContextId();

		/// <summary>�w�肳�ꂽ ID ��p���� <see cref="Microsoft.Scripting.Runtime.ContextId"/> �\���̂̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="id">���̃C���X�^���X�� ID ���w�肵�܂��B</param>
		internal ContextId(int id) : this() { Id = id; }

		/// <summary>�w�肳�ꂽ���O�ŃV�X�e�����̌����o�^���܂��B</summary>
		public static ContextId RegisterContext(object identifier)
		{
			lock (_contexts)
			{
				ContextId res;
				if (_contexts.TryGetValue(identifier, out res))
					throw Error.LanguageRegistered();
				ContextId id = new ContextId();
				id.Id = _maxId++;
				return id;
			}
		}

		/// <summary>�w�肳�ꂽ�R���e�L�X�g���ʎq�ɑΉ����� <see cref="ContextId"/> ���������܂��B</summary>
		public static ContextId LookupContext(object identifier)
		{
			ContextId res;
			lock (_contexts)
			{
				if (_contexts.TryGetValue(identifier, out res))
					return res;
			}
			return ContextId.Empty;
		}

		/// <summary>���̃C���X�^���X�� ID ���擾���܂��B</summary>
		public int Id { get; private set; }

		/// <summary>�w�肳�ꂽ <see cref="ContextId"/> �����݂� <see cref="ContextId"/> �Ɠ��������ǂ����𔻒f���܂��B</summary>
		/// <param name="other">��r���� <see cref="ContextId"/> ���w�肵�܂��B</param>
		[StateIndependent]
		public bool Equals(ContextId other) { return Id == other.Id; }

		/// <summary>���݂� <see cref="ContextId"/> �ɑ΂���n�b�V���l��Ԃ��܂��B</summary>
		public override int GetHashCode() { return Id; }

		/// <summary>���݂� <see cref="ContextId"/> ���w�肳�ꂽ�I�u�W�F�N�g�Ɠ��������ǂ����𔻒f���܂��B</summary>
		/// <param name="obj">��r����I�u�W�F�N�g���w�肵�܂��B</param>
		public override bool Equals(object obj) { return obj is ContextId && Equals((ContextId)obj); }

		/// <summary>�w�肳�ꂽ 2 �� <see cref="ContextId"/> �����������ǂ����𔻒f���܂��B</summary>
		/// <param name="self">��r���� 1 �ڂ̃I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="other">��r���� 2 �ڂ̃I�u�W�F�N�g���w�肵�܂��B</param>
		public static bool operator ==(ContextId self, ContextId other) { return self.Equals(other); }

		/// <summary>�w�肳�ꂽ 2 �� <see cref="ContextId"/> ���������Ȃ����ǂ����𔻒f���܂��B</summary>
		/// <param name="self">��r���� 1 �ڂ̃I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="other">��r���� 2 �ڂ̃I�u�W�F�N�g���w�肵�܂��B</param>
		public static bool operator !=(ContextId self, ContextId other) { return !self.Equals(other); }
	}
}
