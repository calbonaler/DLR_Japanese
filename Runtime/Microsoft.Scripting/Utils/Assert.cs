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
	/// <summary>�\�����s�����\�b�h��񋟂��܂��B</summary>
	public static class Assert
	{
		/// <summary>�R�[�h��œ��B���Ȃ��������}�[�N���A���B�����ꍇ�̓��b�Z�[�W��\�����ė�O��Ԃ��܂��B</summary>
		public static Exception Unreachable
		{
			get
			{
				Debug.Fail("Unreachable");
				return new InvalidOperationException("Code supposed to be unreachable");
			}
		}

		/// <summary>1 �̎Q�ƌ^�ϐ��� <c>null</c> �łȂ����Ƃ�\�����܂��B</summary>
		/// <param name="var"><c>null</c> �łȂ����Ƃ�\������Q�ƌ^�ϐ����w�肵�܂��B</param>
		[Conditional("DEBUG")]
		public static void NotNull(object var) { Debug.Assert(var != null); }

		/// <summary>2 �̎Q�ƌ^�ϐ����ǂ���� <c>null</c> �łȂ����Ƃ�\�����܂��B</summary>
		/// <param name="var1"><c>null</c> �łȂ����Ƃ�\������ 1 �ڂ̎Q�ƌ^�ϐ����w�肵�܂��B</param>
		/// <param name="var2"><c>null</c> �łȂ����Ƃ�\������ 2 �ڂ̎Q�ƌ^�ϐ����w�肵�܂��B</param>
		[Conditional("DEBUG")]
		public static void NotNull(object var1, object var2) { Debug.Assert(var1 != null && var2 != null); }

		/// <summary>3 �̎Q�ƌ^�ϐ����ǂ�� <c>null</c> �łȂ����Ƃ�\�����܂��B</summary>
		/// <param name="var1"><c>null</c> �łȂ����Ƃ�\������ 1 �ڂ̎Q�ƌ^�ϐ����w�肵�܂��B</param>
		/// <param name="var2"><c>null</c> �łȂ����Ƃ�\������ 2 �ڂ̎Q�ƌ^�ϐ����w�肵�܂��B</param>
		/// <param name="var3"><c>null</c> �łȂ����Ƃ�\������ 3 �ڂ̎Q�ƌ^�ϐ����w�肵�܂��B</param>
		[Conditional("DEBUG")]
		public static void NotNull(object var1, object var2, object var3) { Debug.Assert(var1 != null && var2 != null && var3 != null); }

		/// <summary>4 �̎Q�ƌ^�ϐ����ǂ�� <c>null</c> �łȂ����Ƃ�\�����܂��B</summary>
		/// <param name="var1"><c>null</c> �łȂ����Ƃ�\������ 1 �ڂ̎Q�ƌ^�ϐ����w�肵�܂��B</param>
		/// <param name="var2"><c>null</c> �łȂ����Ƃ�\������ 2 �ڂ̎Q�ƌ^�ϐ����w�肵�܂��B</param>
		/// <param name="var3"><c>null</c> �łȂ����Ƃ�\������ 3 �ڂ̎Q�ƌ^�ϐ����w�肵�܂��B</param>
		/// <param name="var4"><c>null</c> �łȂ����Ƃ�\������ 4 �ڂ̎Q�ƌ^�ϐ����w�肵�܂��B</param>
		[Conditional("DEBUG")]
		public static void NotNull(object var1, object var2, object var3, object var4) { Debug.Assert(var1 != null && var2 != null && var3 != null && var4 != null); }

		/// <summary>�w�肳�ꂽ <see cref="System.String"/> �^�̕ϐ��� <c>null</c> �܂��͋�łȂ����Ƃ�\�����܂��B</summary>
		/// <param name="str"><c>null</c> �܂��͋�łȂ����Ƃ�\������ϐ����w�肵�܂��B</param>
		[Conditional("DEBUG")]
		public static void NotEmpty(string str) { Debug.Assert(!string.IsNullOrEmpty(str)); }

		/// <summary>�w�肳�ꂽ�V�[�P���X�� <c>null</c> �܂��͋�łȂ����Ƃ�\�����܂��B</summary>
		/// <typeparam name="T">�V�[�P���X�̗v�f�^���w�肵�܂��B</typeparam>
		/// <param name="items">��łȂ����Ƃ�\������V�[�P���X���w�肵�܂��B</param>
		[Conditional("DEBUG")]
		public static void NotEmpty<T>(IEnumerable<T> items) { Debug.Assert(items != null && items.Any()); }

		/// <summary>�w�肳�ꂽ�V�[�P���X�� <c>null</c> �ł���v�f���܂܂�Ă��Ȃ����Ƃ�\�����܂��B</summary>
		/// <typeparam name="T">�V�[�P���X�̗v�f�^���w�肵�܂��B</typeparam>
		/// <param name="items"><c>null</c> �ł���v�f���܂܂�Ă��Ȃ����Ƃ�\������V�[�P���X���w�肵�܂��B</param>
		[Conditional("DEBUG")]
		public static void NotNullItems<T>(IEnumerable<T> items) where T : class
		{
			Debug.Assert(items != null);
			foreach (var item in items)
				Debug.Assert(item != null);
		}
	}
}
