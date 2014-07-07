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

namespace Microsoft.Scripting.Runtime
{
	/// <summary>��O�Ɋւ���w���p�[ ���\�b�h���i�[���܂��B</summary>
	public static class ExceptionHelpers
	{
		const string prevStackTraces = "PreviousStackTraces";

		/// <summary>��O���ăX���[�����O�ɃX�^�b�N�g���[�X���X�V���܂��B�������邱�ƂŁA���[�U�[�ɑÓ��ȃX�^�b�N�g���[�X��񋟂��邱�Ƃ��ł��܂��B</summary>
		/// <param name="rethrow">�ăX���[������O���w�肵�܂��B</param>
		/// <returns>�X�^�b�N�g���[�X��񂪍X�V���ꂽ��O�B</returns>
		public static Exception UpdateForRethrow(Exception rethrow)
		{
			List<StackTrace> prev;
			// ���I�X�^�b�N�g���[�X�f�[�^�� 1 �������Ă��Ȃ��ꍇ�́A���̗�O�I�u�W�F�N�g����f�[�^���L���v�`���ł��܂�
			StackTrace st = new StackTrace(rethrow, true);
			if (!TryGetAssociatedStackTraces(rethrow, out prev))
				AssociateStackTraces(rethrow, prev = new List<StackTrace>());
			prev.Add(st);
			return rethrow;
		}

		/// <summary>�w�肳�ꂽ��O�Ɋ֘A�t�����Ă��邷�ׂẴX�^�b�N�g���[�X�f�[�^��Ԃ��܂��B</summary>
		/// <param name="rethrow">�X�^�b�N�g���[�X�f�[�^���擾�����O���w�肵�܂��B</param>
		/// <returns>��O�Ɋ֘A�t����ꂽ�X�^�b�N�g���[�X�f�[�^�B</returns>
		public static IList<StackTrace> GetExceptionStackTraces(Exception rethrow)
		{
			List<StackTrace> result;
			return TryGetAssociatedStackTraces(rethrow, out result) ? result : null;
		}

		static void AssociateStackTraces(Exception e, List<StackTrace> traces) { e.Data[prevStackTraces] = traces; }

		static bool TryGetAssociatedStackTraces(Exception e, out List<StackTrace> traces) { return (traces = e.Data[prevStackTraces] as List<StackTrace>) != null; }
	}
}
