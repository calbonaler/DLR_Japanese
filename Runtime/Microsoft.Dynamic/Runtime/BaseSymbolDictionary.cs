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

namespace Microsoft.Scripting.Runtime
{
	/// <summary>
	/// <see cref="SymbolId"/> ���g�p����f�B�N�V���i���̊�{�N���X�ł��B
	/// <see cref="SymbolId"/> �f�B�N�V���i���̓N���X�̃����o�A�֐��̊��A�֐��̃��[�J���ϐ��A����т��̑��̖��O�ɂ���ăC���f�b�N�X�����ꂽ�ꏊ�̌����Ɏg�p����鍂���ȃf�B�N�V���i���ł��B
	/// <see cref="SymbolId"/> �f�B�N�V���i���� <see cref="SymbolId"/> ��
	/// (���ڃ��[�U�[�R�[�h�Ɍ��J���ꂽ�ꍇ�� <see cref="T:System.Collections.Generic.Dictionary&lt;System.Object, System.Object&gt;"/> �Ƃ��Ẵf�B�N�V���i���ւ̒x���o�C���f�B���O�A�N�Z�X���T�|�[�g����)
	/// <see cref="System.Object"/> �ɂ��L�[���T�|�[�g���܂��B
	/// <see cref="System.Object"/> �ɂ��C���f�b�N�X���̏ꍇ�� <c>null</c> �͗L���ȃL�[�ƂȂ�܂��B
	/// </summary>
	public abstract class BaseSymbolDictionary
	{
		static readonly object _nullObject = new object();
		const int ObjectKeysId = -2;

		/// <summary><see cref="SymbolId"/> �f�B�N�V���i���̃I�u�W�F�N�g���i�[����L�[��\���܂��B</summary>
		internal static readonly SymbolId ObjectKeys = new SymbolId(ObjectKeysId);

		/// <summary><see cref="Microsoft.Scripting.Runtime.BaseSymbolDictionary"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		protected BaseSymbolDictionary() { }

		/// <summary>�l�̃n�b�V���R�[�h�����߂܂��B��� <see cref="ArgumentTypeException"/> ���X���[���܂��B</summary>
		/// <exception cref="ArgumentTypeException">�f�B�N�V���i���̓n�b�V���\�ł͂���܂���B</exception>
		public int GetValueHashCode() { throw Error.DictionaryNotHashable(); }

		/// <summary>���̃I�u�W�F�N�g�Ǝw�肳�ꂽ�I�u�W�F�N�g�Ɋ܂܂�Ă���l�����������ǂ����𔻒f���܂��B</summary>
		/// <param name="other">�l���r����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns>���̃I�u�W�F�N�g�Ǝw�肳�ꂽ�I�u�W�F�N�g�Ɋ܂܂�Ă���l���������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public virtual bool ValueEquals(object other)
		{
			if (ReferenceEquals(this, other))
				return true;
			var oth = other as IAttributesCollection;
			var ths = this as IAttributesCollection;
			if (oth == null)
				return false;
			if (oth.Count != ths.Count)
				return false;
			foreach (var o in ths)
			{
				object res;
				if (!oth.TryGetValue(o.Key, out res))
					return false;
				if (res != null)
				{
					if (!res.Equals(o.Value))
						return false;
				}
				else if (o.Value != null)
				{
					if (!o.Value.Equals(res))
						return false;
				} // else both null and are equal
			}
			return true;
		}

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�� <c>null</c> �̏ꍇ�� <c>null</c> �I�u�W�F�N�g�ɕϊ����܂��B</summary>
		/// <param name="obj">�ϊ�����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns>�I�u�W�F�N�g�� <c>null</c> �̏ꍇ�� <c>null</c> �I�u�W�F�N�g�B����ȊO�̏ꍇ�͌��̃I�u�W�F�N�g�B</returns>
		public static object NullToObj(object obj) { return obj == null ? _nullObject : obj; }

		/// <summary>�w�肳�ꂽ <c>null</c> �I�u�W�F�N�g�� <c>null</c> �ɕϊ����܂��B</summary>
		/// <param name="obj">�ϊ�����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns>�I�u�W�F�N�g�� <c>null</c> �I�u�W�F�N�g�̏ꍇ�� <c>null</c>�B����ȊO�̏ꍇ�͌��̃I�u�W�F�N�g�B</returns>
		public static object ObjToNull(object obj) { return obj == _nullObject ? null : obj; }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�� <c>null</c> �I�u�W�F�N�g���ǂ����𔻒f���܂��B</summary>
		/// <param name="obj">���f����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <returns>�I�u�W�F�N�g�� <c>null</c> �I�u�W�F�N�g�̏ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool IsNullObject(object obj) { return obj == _nullObject; }
	}
}
