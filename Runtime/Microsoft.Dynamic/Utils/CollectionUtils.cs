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
using System.Linq;

namespace Microsoft.Scripting.Utils
{
	/// <summary>�R���N�V�����Ɋւ���g�����\�b�h��񋟂��܂��B</summary>
	public static class CollectionUtils
	{
		/// <summary>
		/// �V�[�P���X�� <see cref="ReadOnlyCollection&lt;T&gt;"/> �Ń��b�v���܂��B
		/// ���ׂẴf�[�^�͐V�����z��ɃR�s�[����邽�߁A�쐬���ꂽ��� <see cref="ReadOnlyCollection&lt;T&gt;"/> �͕ύX����܂���B
		/// �������A<paramref name="enumerable"/> �����ł� <see cref="ReadOnlyCollection&lt;T&gt;"/> �ł������ꍇ�ɂ͌��̃R���N�V�������Ԃ���܂��B
		/// </summary>
		/// <param name="enumerable">�ǂݎ���p�̃R���N�V�����ɕϊ�����V�[�P���X���w�肵�܂��B</param>
		/// <returns>�ύX����Ȃ��ǂݎ���p�̃R���N�V�����B</returns>
		internal static ReadOnlyCollection<T> ToReadOnly<T>(this IEnumerable<T> enumerable)
		{
			var roCollection = enumerable as ReadOnlyCollection<T>;
			if (roCollection != null)
				return roCollection;
			T[] array;
			if (enumerable != null && (array = enumerable.ToArray()).Length > 0)
				return new ReadOnlyCollection<T>(array);
			return EmptyReadOnlyCollection<T>.Instance;
		}

		/// <summary>���X�g����w�肳�ꂽ�q��Ɉ�v����v�f�̃C���f�b�N�X��Ԃ��܂��B</summary>
		/// <typeparam name="T">���X�g�̗v�f�^���w�肵�܂��B</typeparam>
		/// <param name="collection">�q��Ɉ�v����v�f���������郊�X�g���w�肵�܂��B</param>
		/// <param name="predicate">���X�g����v�f���������邽�߂̏q����w�肵�܂��B</param>
		/// <returns>���X�g�ŏq��Ɉ�v����v�f�����������ꍇ�͂��̃C���f�b�N�X�B����ȊO�̏ꍇ�� <c>-1</c> ���Ԃ���܂��B</returns>
		public static int FindIndex<T>(this IList<T> collection, Predicate<T> predicate)
		{
			ContractUtils.RequiresNotNull(collection, "collection");
			ContractUtils.RequiresNotNull(predicate, "predicate");
			for (int i = 0; i < collection.Count; i++)
			{
				if (predicate(collection[i]))
					return i;
			}
			return -1;
		}

		/// <summary>�C���f�b�N�X�ɂ���ăA�N�Z�X�\�ȃ��X�g�̖����ɂ��� 2 �̗v�f���������܂��B</summary>
		/// <typeparam name="T">�v�f���������郊�X�g�̗v�f�^���w�肵�܂��B</typeparam>
		/// <param name="list">�v�f���������郊�X�g���w�肵�܂��B</param>
		public static void SwapLastTwo<T>(this IList<T> list)
		{
			ContractUtils.RequiresNotNull(list, "list");
			ContractUtils.Requires(list.Count >= 2, "list");
			var temp = list[list.Count - 1];
			list[list.Count - 1] = list[list.Count - 2];
			list[list.Count - 2] = temp;
		}

		/// <summary>�w�肳�ꂽ�R���N�V�����̃n�b�V���l���v�Z���܂��B</summary>
		/// <typeparam name="T">�n�b�V���l���v�Z����R���N�V�����̗v�f�^���w�肵�܂��B</typeparam>
		/// <param name="items">�n�b�V���l���v�Z����R���N�V�������w�肵�܂��B</param>
		/// <returns>�R���N�V�����̃n�b�V���l�B</returns>
		public static int GetValueHashCode<T>(this ICollection<T> items) { return GetValueHashCode<T>(items, 0, items.Count); }

		/// <summary>�w�肳�ꂽ�R���N�V�����̎w�肳�ꂽ�͈͂̃n�b�V���l���v�Z���܂��B</summary>
		/// <typeparam name="T">�n�b�V���l���v�Z����R���N�V�����̗v�f�^���w�肵�܂��B</typeparam>
		/// <param name="items">�n�b�V���l���v�Z����R���N�V�������w�肵�܂��B</param>
		/// <param name="start">�n�b�V���l�̌v�Z���J�n����R���N�V�������̈ʒu���w�肵�܂��B</param>
		/// <param name="count">�n�b�V���l���v�Z����R���N�V�������̗v�f�����w�肵�܂��B</param>
		/// <returns>�R���N�V�����̎w�肳�ꂽ�͈͂̃n�b�V���l�B</returns>
		public static int GetValueHashCode<T>(this ICollection<T> items, int start, int count)
		{
			ContractUtils.RequiresNotNull(items, "items");
			ContractUtils.RequiresArrayRange(items.Count, start, count, "start", "count");
			if (count == 0)
				return 0;
			var en = items.Skip(start).Take(count);
			return en.Aggregate(en.First().GetHashCode(), (x, y) => ((x << 5) | (x >> 27)) ^ y.GetHashCode());
		}
	}

	static class EmptyReadOnlyCollection<T>
	{
		internal static ReadOnlyCollection<T> Instance = new ReadOnlyCollection<T>(new T[0]);
	}
}
