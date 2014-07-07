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

namespace Microsoft.Scripting.Utils
{
	/// <summary>�悭���p������O�𑗏o���郆�[�e�B���e�B ���\�b�h���i�[���܂��B</summary>
	public static class ExceptionUtils
	{
		/// <summary>�������A�����̒l�A�G���[���b�Z�[�W���g�p���āA�V���� <see cref="ArgumentOutOfRangeException"/> ���쐬���܂��B</summary>
		/// <param name="paramName">��O�̌����ƂȂ����p�����[�^�[�̖��O�B</param>
		/// <param name="actualValue">���̗�O�̌����ł�������̒l�B</param>
		/// <param name="message">�G���[��������郁�b�Z�[�W�B</param>
		/// <returns>�V�����쐬���ꂽ <see cref="ArgumentOutOfRangeException"/>�B</returns>
		public static ArgumentOutOfRangeException MakeArgumentOutOfRangeException(string paramName, object actualValue, string message) { throw new ArgumentOutOfRangeException(paramName, actualValue, message); }

		/// <summary>�����̎w�肳�ꂽ�C���f�b�N�X�� <c>null</c> �ł��邱�Ƃ������V���� <see cref="ArgumentNullException"/> ���쐬���܂��B</summary>
		/// <param name="index"><c>null</c> �v�f���i�[����Ă�������̃C���f�b�N�X���w�肵�܂��B</param>
		/// <param name="arrayName">�����̖��O���w�肵�܂��B</param>
		/// <returns>������ <c>null</c> �v�f���i�[����Ă��邱�Ƃ������V�����쐬���ꂽ <see cref="ArgumentNullException"/>�B</returns>
		public static ArgumentNullException MakeArgumentItemNullException(int index, string arrayName) { return new ArgumentNullException(string.Format("{0}[{1}]", arrayName, index)); }
	}
}
