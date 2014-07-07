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
using System.Reflection;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>�X�^�b�N�t���[���Ɋւ�������i�[���܂��B</summary>
	[Serializable]
	public class DynamicStackFrame
	{
		/// <summary>���\�b�h�A���\�b�h���A�t�@�C�����A�s�ԍ����g�p���āA<see cref="Microsoft.Scripting.Runtime.DynamicStackFrame"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="method">�X�^�b�N�t���[�����\�����\�b�h���w�肵�܂��B</param>
		/// <param name="methodName">�X�^�b�N�t���[�����\�����\�b�h�����w�肵�܂��B</param>
		/// <param name="filename">�X�^�b�N�t���[�����\���t�@�C�������w�肵�܂��B</param>
		/// <param name="line">�X�^�b�N�t���[�����\���s�ԍ����w�肵�܂��B</param>
		public DynamicStackFrame(MethodBase method, string methodName, string filename, int line)
		{
			MethodName = methodName;
			FileName = filename;
			FileLineNumber = line;
			Method = method;
		}

		/// <summary>�X�^�b�N�t���[�����\�����\�b�h���擾���܂��B</summary>
		public MethodBase Method { get; private set; }

		/// <summary>�X�^�b�N�t���[�����\�����\�b�h�����擾���܂��B</summary>
		public string MethodName { get; private set; }

		/// <summary>�X�^�b�N�t���[�����\���t�@�C�������擾���܂��B</summary>
		public string FileName { get; private set; }

		/// <summary>�X�^�b�N�t���[�����\���t�@�C���̍s�ԍ����擾���܂��B</summary>
		public int FileLineNumber { get; private set; }

		/// <summary>���̃X�^�b�N�t���[���̕�����\�����擾���܂��B</summary>
		public override string ToString()
		{
			return string.Format(
				"{0} in {1}:{2}, {3}",
				MethodName ?? "<function unknown>",
				FileName ?? "<filename unknown>",
				FileLineNumber,
				(Method != null ? Method.ToString() : "<method unknown>")
			);
		}
	}
}
