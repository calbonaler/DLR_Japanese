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
using System.Linq;

namespace Microsoft.Scripting.Utils
{
	/// <summary>�z��Ɋւ��郆�[�e�B���e�B���\�b�h��񋟂��܂��B</summary>
	public static class ArrayUtils
	{
		/// <summary><see cref="System.String"/> �^�̋�̔z���\���܂��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
		public static readonly string[] EmptyStrings = new string[0];

		/// <summary><see cref="System.Object"/> �^�̋�̔z���\���܂��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
		public static readonly object[] EmptyObjects = new object[0];

		/// <summary>�w�肳�ꂽ�z��̊ȈՃR�s�[���쐬���܂��B</summary>
		/// <typeparam name="T">�ȈՃR�s�[���쐬����z��̗v�f�^���w�肵�܂��B</typeparam>
		/// <param name="array">�ȈՃR�s�[���쐬����z����w�肵�܂��B</param>
		/// <returns>�z��̊ȈՃR�s�[�B</returns>
		public static T[] Copy<T>(T[] array)
		{
			ContractUtils.RequiresNotNull(array, "array");
			return array.Length > 0 ? (T[])array.Clone() : array;
		}

		/// <summary>�w�肳�ꂽ�V�[�P���X��z��ɕϊ����܂��B�V�[�P���X�����łɔz��ł���ꍇ�͌��̃V�[�P���X��Ԃ��܂��B</summary>
		/// <typeparam name="T">�z����擾����V�[�P���X�̗v�f�^���w�肵�܂��B</typeparam>
		/// <param name="items">�z����擾����V�[�P���X���w�肵�܂��B</param>
		/// <returns>�V�[�P���X�ɑΉ�����z��B</returns>
		public static T[] ToArray<T>(IEnumerable<T> items) { return items == null ? new T[0] : items as T[] ?? items.ToArray(); }

		/// <summary>�w�肳�ꂽ�z����w�肳�ꂽ�����փV�t�g���܂��B0 �����̃C���f�b�N�X�ɂȂ����v�f�͍폜����A�����͐؂�l�߂��܂��B</summary>
		/// <typeparam name="T">���V�t�g����z��̗v�f�^���w�肵�܂��B</typeparam>
		/// <param name="array">���V�t�g����z����w�肵�܂��B</param>
		/// <param name="count">�z��̃V�t�g�ʂ��w�肵�܂��B</param>
		/// <returns>���V�t�g���ꂽ�z��B</returns>
		public static T[] ShiftLeft<T>(T[] array, int count)
		{
			ContractUtils.RequiresNotNull(array, "array");
			if (count < 0)
				throw new ArgumentOutOfRangeException("count");
			var result = new T[array.Length - count];
			Array.Copy(array, count, result, 0, result.Length);
			return result;
		}

		/// <summary>�w�肳�ꂽ�v�f���R���N�V�����̐擪�ɒǉ������z���Ԃ��܂��B</summary>
		/// <typeparam name="T">�ǉ������v�f�̌^���w�肵�܂��B</typeparam>
		/// <param name="item">�ǉ�����v�f���w�肵�܂��B</param>
		/// <param name="items">�v�f��ǉ����錳�̃R���N�V�������w�肵�܂��B</param>
		/// <returns>�R���N�V�����̐擪�ɗv�f���ǉ����ꂽ�z��B</returns>
		public static T[] Insert<T>(T item, ICollection<T> items)
		{
			ContractUtils.RequiresNotNull(items, "items");
			var res = new T[items.Count + 1];
			res[0] = item;
			items.CopyTo(res, 1);
			return res;
		}

		/// <summary>�w�肳�ꂽ 2 �̗v�f���R���N�V�����̐擪�ɒǉ������z���Ԃ��܂��B</summary>
		/// <typeparam name="T">�ǉ������v�f�̌^���w�肵�܂��B</typeparam>
		/// <param name="item1">�ǉ����� 1 �Ԗڂ̗v�f���w�肵�܂��B</param>
		/// <param name="item2">�ǉ����� 2 �Ԗڂ̗v�f���w�肵�܂��B</param>
		/// <param name="items">�v�f��ǉ����錳�̃R���N�V�������w�肵�܂��B</param>
		/// <returns>�R���N�V�����̐擪�� 2 �̗v�f���ǉ����ꂽ�z��B</returns>
		public static T[] Insert<T>(T item1, T item2, ICollection<T> items)
		{
			ContractUtils.RequiresNotNull(items, "items");
			var res = new T[items.Count + 2];
			res[0] = item1;
			res[1] = item2;
			items.CopyTo(res, 2);
			return res;
		}

		/// <summary>�w�肳�ꂽ�C�ӌ̗v�f���R���N�V�����̖����ɒǉ������z���Ԃ��܂��B</summary>
		/// <typeparam name="T">�ǉ������v�f�̌^���w�肵�܂��B</typeparam>
		/// <param name="items">�v�f��ǉ����錳�̃R���N�V�������w�肵�܂��B</param>
		/// <param name="added">�ǉ�����v�f���w�肵�܂��B</param>
		/// <returns>�R���N�V�����̖����ɗv�f���ǉ����ꂽ�z��B</returns>
		public static T[] Append<T>(ICollection<T> items, params T[] added)
		{
			ContractUtils.RequiresNotNull(items, "items1");
			ContractUtils.RequiresNotNull(added, "items2");
			var result = new T[items.Count + added.Length];
			items.CopyTo(result, 0);
			added.CopyTo(result, items.Count);
			return result;
		}

		/// <summary>�w�肳�ꂽ�z��̍ŏ��̗v�f���폜�����z���Ԃ��܂��B</summary>
		/// <typeparam name="T">�z��̗v�f�^���w�肵�܂��B</typeparam>
		/// <param name="array">�ŏ��̗v�f���폜����z����w�肵�܂��B</param>
		/// <returns>�ŏ��̗v�f���폜���ꂽ�z��B</returns>
		public static T[] RemoveFirst<T>(T[] array) { return RemoveAt(array, 0); }

		/// <summary>�w�肳�ꂽ�z��̍Ō�̗v�f���폜�����z���Ԃ��܂��B</summary>
		/// <typeparam name="T">�z��̗v�f�^���w�肵�܂��B</typeparam>
		/// <param name="array">�Ō�̗v�f���폜����z����w�肵�܂��B</param>
		/// <returns>�Ō�̗v�f���폜���ꂽ�z��B</returns>
		public static T[] RemoveLast<T>(T[] array) { return RemoveAt(array, array.Length - 1); }

		/// <summary>�w�肳�ꂽ�z��̎w�肳�ꂽ�C���f�b�N�X�ɂ���v�f���폜�����z���Ԃ��܂��B</summary>
		/// <typeparam name="T">�z��̗v�f�^���w�肵�܂��B</typeparam>
		/// <param name="array">�v�f���폜�����z����w�肵�܂��B</param>
		/// <param name="indexToRemove">�폜����v�f�̈ʒu������ 0 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�C���f�b�N�X�̗v�f���폜���ꂽ�z��B</returns>
		public static T[] RemoveAt<T>(T[] array, int indexToRemove)
		{
			ContractUtils.RequiresNotNull(array, "array");
			ContractUtils.RequiresArrayIndex(array, indexToRemove, "indexToRemove");
			var result = new T[array.Length - 1];
			if (indexToRemove > 0)
				Array.Copy(array, 0, result, 0, indexToRemove);
			var remaining = array.Length - indexToRemove - 1;
			if (remaining > 0)
				Array.Copy(array, array.Length - remaining, result, result.Length - remaining, remaining);
			return result;
		}

		/// <summary>�w�肳�ꂽ�z��̎w�肳�ꂽ�C���f�b�N�X�Ɏw�肳�ꂽ�C�ӌ̗v�f��}�������z���Ԃ��܂��B</summary>
		/// <typeparam name="T">�z��̗v�f�^���w�肵�܂��B</typeparam>
		/// <param name="array">�v�f���}�������z����w�肵�܂��B</param>
		/// <param name="index">�v�f�̑}���̊J�n�ʒu������ 0 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		/// <param name="items">�}������v�f���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�C���f�b�N�X�Ɏw�肳�ꂽ�v�f���}�����ꂽ�z��B</returns>
		public static T[] InsertAt<T>(T[] array, int index, params T[] items)
		{
			ContractUtils.RequiresNotNull(array, "array");
			ContractUtils.RequiresNotNull(items, "items");
			ContractUtils.RequiresArrayInsertIndex(array, index, "index");
			if (items.Length == 0)
				return Copy(array);
			var result = new T[array.Length + items.Length];
			if (index > 0)
				Array.Copy(array, 0, result, 0, index);
			items.CopyTo(result, index);
			var remaining = array.Length - index;
			if (remaining > 0)
				Array.Copy(array, array.Length - remaining, result, result.Length - remaining, remaining);
			return result;
		}
	}
}
