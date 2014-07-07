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

using System.Threading;

namespace Microsoft.Scripting.Utils
{
	/// <summary><see cref="Monitor"/> �Ɋւ��郆�[�e�B���e�B ���\�b�h�����J���܂��B</summary>
	public static class MonitorUtils
	{
		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�̔r�����b�N��������܂��B</summary>
		/// <param name="obj">�r�����b�N���������I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="lockTaken">���b�N��������ꂽ���ǂ����������܂��B���b�N��������ꂽ�ꍇ�� <c>false</c> �ɕύX����܂��B</param>
		/// <exception cref="System.ArgumentNullException"><paramref name="obj"/> �� <c>null</c> �ł��B</exception>
		/// <exception cref="SynchronizationLockException">���݂̃X���b�h�� <paramref name="obj"/> �ɑ΂��ă��b�N�����L���Ă��܂���B</exception>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
		public static void Exit(object obj, ref bool lockTaken)
		{
			try { }
			finally
			{
				// finally prevents thread abort to leak the lock:
				lockTaken = false;
				Monitor.Exit(obj);
			}
		}
	}
}
