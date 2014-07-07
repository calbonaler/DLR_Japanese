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
	/// <summary><see cref="Monitor"/> に関するユーティリティ メソッドを公開します。</summary>
	public static class MonitorUtils
	{
		/// <summary>指定されたオブジェクトの排他ロックを解放します。</summary>
		/// <param name="obj">排他ロックを解放するオブジェクトを指定します。</param>
		/// <param name="lockTaken">ロックが解放されたかどうかを示します。ロックが解放された場合は <c>false</c> に変更されます。</param>
		/// <exception cref="System.ArgumentNullException"><paramref name="obj"/> が <c>null</c> です。</exception>
		/// <exception cref="SynchronizationLockException">現在のスレッドは <paramref name="obj"/> に対してロックを所有していません。</exception>
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
