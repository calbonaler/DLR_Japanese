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

namespace Microsoft.Scripting.Utils
{
	/// <summary>�_������s���A�ᔽ�����ꍇ�ɂ͗�O�𑗏o���郁�\�b�h��񋟂��܂��B</summary>
	public static class ContractUtils
	{
		/// <summary>�w�肳�ꂽ�����������ɗv�����܂��B</summary>
		/// <param name="precondition">�������w�肵�܂��B</param>
		/// <exception cref="ArgumentException">�������s�����ł��B</exception>
		public static void Requires(bool precondition) { Requires(precondition, null, Strings.MethodPreconditionViolated); }

		/// <summary>�w�肳�ꂽ�����������ɗv�����܂��B</summary>
		/// <param name="precondition">�������w�肵�܂��B</param>
		/// <param name="paramName">�����̖��O���w�肵�܂��B</param>
		/// <exception cref="ArgumentException">�������s�����ł��B</exception>
		public static void Requires(bool precondition, string paramName) { Requires(precondition, paramName, Strings.InvalidArgumentValue); }

		/// <summary>�w�肳�ꂽ�����������ɗv�����܂��B</summary>
		/// <param name="precondition">�������w�肵�܂��B</param>
		/// <param name="paramName">�����̖��O���w�肵�܂��B</param>
		/// <param name="message">�������s�����̂Ƃ����o������O�̃��b�Z�[�W���w�肵�܂��B</param>
		/// <exception cref="ArgumentException">�������s�����ł��B</exception>
		public static void Requires(bool precondition, string paramName, string message)
		{
			if (!precondition)
			{
				if (paramName != null)
					throw new ArgumentException(message, paramName);
				else
					throw new ArgumentException(message);
			}
		}

		/// <summary>�w�肳�ꂽ������ <c>null</c> �łȂ����Ƃ�v�����܂��B</summary>
		/// <param name="value"><c>null</c> �łȂ����Ƃ�v������Q�ƌ^�ϐ����w�肵�܂��B</param>
		/// <param name="paramName">�����̖��O���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException">������ <c>null</c> �ł��B</exception>
		public static void RequiresNotNull(object value, string paramName)
		{
			Assert.NotEmpty(paramName);
			if (value == null)
				throw new ArgumentNullException(paramName);
		}

		/// <summary>�w�肳�ꂽ <see cref="System.String"/> �^�̈������󕶎��łȂ����Ƃ�v�����܂��B</summary>
		/// <param name="str">��łȂ����Ƃ�v������ <see cref="System.String"/> �^�̈������w�肵�܂��B</param>
		/// <param name="paramName">�����̖��O���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException">������ <c>null</c> �ł��B</exception>
		/// <exception cref="ArgumentException">�����͋󕶎��ł��B</exception>
		public static void RequiresNotEmpty(string str, string paramName)
		{
			RequiresNotNull(str, paramName);
			if (str.Length <= 0)
				throw new ArgumentException(Strings.NonEmptyStringRequired, paramName);
		}

		/// <summary>�w�肳�ꂽ�V�[�P���X�̈�������łȂ����Ƃ�v�����܂��B</summary>
		/// <typeparam name="T">�V�[�P���X�̌^���w�肵�܂��B</typeparam>
		/// <param name="collection">��łȂ����Ƃ�v������V�[�P���X���w�肵�܂��B</param>
		/// <param name="paramName">�����̖��O���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException">������ <c>null</c> �ł��B</exception>
		/// <exception cref="ArgumentException">�����͋�̃V�[�P���X��\���Ă��܂��B</exception>
		public static void RequiresNotEmpty<T>(IEnumerable<T> collection, string paramName)
		{
			RequiresNotNull(collection, paramName);
			if (!collection.Any())
				throw new ArgumentException(Strings.NonEmptyCollectionRequired, paramName);
		}

		/// <summary>�w�肳�ꂽ�񋓉\�ȃR���N�V������ <c>null</c> �ł���v�f���܂܂�Ă��Ȃ����Ƃ�v�����܂��B</summary>
		/// <param name="collection"><c>null</c> �v�f���܂܂�Ă��Ȃ����Ƃ�v������R���N�V�������w�肵�܂��B</param>
		/// <param name="collectionName">�R���N�V�����̖��O���w�肵�܂��B</param>
		/// <exception cref="ArgumentNullException">�R���N�V������ <c>null</c> �ł��B</exception>
		/// <exception cref="ArgumentException">�R���N�V������ <c>null</c> �v�f���܂܂�Ă��܂��B</exception>
		public static void RequiresNotNullItems<T>(IEnumerable<T> collection, string collectionName)
		{
			Assert.NotNull(collectionName);
			RequiresNotNull(collection, collectionName);
			int i = 0;
			foreach (var item in collection)
			{
				if (item == null)
					throw ExceptionUtils.MakeArgumentItemNullException(i, collectionName);
				i++;
			}
		}

		/// <summary>�w�肳�ꂽ�C���f�b�N�X���R���N�V�������̈ʒu�������Ă��邱�Ƃ�v�����܂��B</summary>
		/// <typeparam name="T">�R���N�V�����̗v�f�^���w�肵�܂��B</typeparam>
		/// <param name="collection">�C���f�b�N�X���ʒu�������Ă��邱�Ƃ�v������R���N�V�������w�肵�܂��B</param>
		/// <param name="index">�R���N�V�������̈ʒu�������Ă��邱�Ƃ�v������C���f�b�N�X���w�肵�܂��B</param>
		/// <param name="indexName">�C���f�b�N�X�̖��O���w�肵�܂��B</param>
		/// <exception cref="ArgumentOutOfRangeException">�C���f�b�N�X�̓R���N�V�����O�̏ꏊ�������Ă��܂��B</exception>
		public static void RequiresArrayIndex<T>(ICollection<T> collection, int index, string indexName) { RequiresArrayIndex(collection.Count, index, indexName); }

		/// <summary>�w�肳�ꂽ�C���f�b�N�X���R���N�V�������̈ʒu�������Ă��邱�Ƃ�v�����܂��B</summary>
		/// <param name="length">�C���f�b�N�X���ʒu�������Ă��邱�Ƃ�v������R���N�V�����̒������w�肵�܂��B</param>
		/// <param name="index">�R���N�V�������̈ʒu�������Ă��邱�Ƃ�v������C���f�b�N�X���w�肵�܂��B</param>
		/// <param name="indexName">�C���f�b�N�X�̖��O���w�肵�܂��B</param>
		/// <exception cref="ArgumentOutOfRangeException">�C���f�b�N�X�̓R���N�V�����O�̏ꏊ�������Ă��܂��B</exception>
		public static void RequiresArrayIndex(int length, int index, string indexName)
		{
			Assert.NotEmpty(indexName);
			Debug.Assert(length >= 0);
			if (index < 0 || index >= length)
				throw new ArgumentOutOfRangeException(indexName);
		}

		/// <summary>�w�肳�ꂽ�C���f�b�N�X���R���N�V�������̈ʒu�܂��͖����������Ă��邱�Ƃ�v�����܂��B</summary>
		/// <typeparam name="T">�R���N�V�����̗v�f�^���w�肵�܂��B</typeparam>
		/// <param name="collection">�C���f�b�N�X���ʒu�������Ă��邱�Ƃ�v������R���N�V�������w�肵�܂��B</param>
		/// <param name="index">�R���N�V�������̈ʒu�܂��͖����������Ă��邱�Ƃ�v������C���f�b�N�X���w�肵�܂��B</param>
		/// <param name="indexName">�C���f�b�N�X�̖��O���w�肵�܂��B</param>
		/// <exception cref="ArgumentOutOfRangeException">�C���f�b�N�X�̓R���N�V�����O�̏ꏊ�������Ă��܂��B</exception>
		public static void RequiresArrayInsertIndex<T>(ICollection<T> collection, int index, string indexName) { RequiresArrayInsertIndex(collection.Count, index, indexName); }

		/// <summary>�w�肳�ꂽ�C���f�b�N�X���R���N�V�������̈ʒu�܂��͖����������Ă��邱�Ƃ�v�����܂��B</summary>
		/// <param name="length">�C���f�b�N�X���ʒu�������Ă��邱�Ƃ�v������R���N�V�����̒������w�肵�܂��B</param>
		/// <param name="index">�R���N�V�������̈ʒu�܂��͖����������Ă��邱�Ƃ�v������C���f�b�N�X���w�肵�܂��B</param>
		/// <param name="indexName">�C���f�b�N�X�̖��O���w�肵�܂��B</param>
		/// <exception cref="ArgumentOutOfRangeException">�C���f�b�N�X�̓R���N�V�����O�̏ꏊ�������Ă��܂��B</exception>
		public static void RequiresArrayInsertIndex(int length, int index, string indexName)
		{
			Assert.NotEmpty(indexName);
			Debug.Assert(length >= 0);
			if (index < 0 || index > length)
				throw new ArgumentOutOfRangeException(indexName);
		}

		/// <summary>�w�肳�ꂽ�͈͂��R���N�V�������ɂ��邱�Ƃ�v�����܂��B</summary>
		/// <typeparam name="T">�R���N�V�����̗v�f�^���w�肵�܂��B</typeparam>
		/// <param name="collection">�͈͂����݂��邱�Ƃ�v������R���N�V�������w�肵�܂��B</param>
		/// <param name="offset">�͈͂̊J�n�ʒu�������C���f�b�N�X���w�肵�܂��B</param>
		/// <param name="count">�͈͂̒������w�肵�܂��B</param>
		/// <param name="offsetName">�͈͂̊J�n�ʒu��\�������̖��O���w�肵�܂��B</param>
		/// <param name="countName">�͈͂̒�����\�������̖��O���w�肵�܂��B</param>
		/// <exception cref="ArgumentOutOfRangeException">�w�肳�ꂽ�͈͂��R���N�V�������ɂ���܂���B</exception>
		public static void RequiresArrayRange<T>(ICollection<T> collection, int offset, int count, string offsetName, string countName) { RequiresArrayRange(collection.Count, offset, count, offsetName, countName); }

		/// <summary>�w�肳�ꂽ�͈͂��R���N�V�������ɂ��邱�Ƃ�v�����܂��B</summary>
		/// <param name="length">�͈͂����݂��邱�Ƃ�v������R���N�V�����̒������w�肵�܂��B</param>
		/// <param name="offset">�͈͂̊J�n�ʒu�������C���f�b�N�X���w�肵�܂��B</param>
		/// <param name="count">�͈͂̒������w�肵�܂��B</param>
		/// <param name="offsetName">�͈͂̊J�n�ʒu��\�������̖��O���w�肵�܂��B</param>
		/// <param name="countName">�͈͂̒�����\�������̖��O���w�肵�܂��B</param>
		/// <exception cref="ArgumentOutOfRangeException">�w�肳�ꂽ�͈͂��R���N�V�������ɂ���܂���B</exception>
		public static void RequiresArrayRange(int length, int offset, int count, string offsetName, string countName)
		{
			Assert.NotEmpty(offsetName);
			Assert.NotEmpty(countName);
			Debug.Assert(length >= 0);
			if (count < 0)
				throw new ArgumentOutOfRangeException(countName);
			if (offset < 0 || length - offset < count)
				throw new ArgumentOutOfRangeException(offsetName);
		}

		/// <summary>�s�Ϗ������w�肵�܂��B</summary>
		/// <param name="condition">�s�Ϗ����ƂȂ�������w�肵�܂��B</param>
		[Conditional("FALSE")]
		public static void Invariant(bool condition) { Debug.Assert(condition); }

		/// <summary>���\�b�h�̎���������w�肵�܂��B</summary>
		/// <param name="condition">����������w�肵�܂��B</param>
		[Conditional("FALSE")]
		public static void Ensures(bool condition) { }

		/// <summary>���\�b�h�̌��ʂ�\���܂��B</summary>
		/// <typeparam name="T">���ʌ^���w�肵�܂��B</typeparam>
		/// <returns>���\�b�h�̌��ʁB</returns>
		public static T Result<T>() { return default(T); }
	}
}
