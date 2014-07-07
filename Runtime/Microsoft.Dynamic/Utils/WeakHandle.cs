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
using System.Runtime.InteropServices;

namespace Microsoft.Scripting.Utils
{
	/// <summary>自動的に解放されない "弱い参照" を表します。</summary>
	public struct WeakHandle : IEquatable<WeakHandle>
	{
		/// <summary>指定されたオブジェクトを参照し、指定された復活の追跡を使用する <see cref="WeakHandle"/> 構造体の新しいインスタンスを初期化します。 </summary>
		/// <param name="target">参照するオブジェクトを指定します。</param>
		/// <param name="trackResurrection">終了後もオブジェクトを参照するかどうかを示す値を指定します。</param>
		public WeakHandle(object target, bool trackResurrection) { weakRef = GCHandle.Alloc(target, trackResurrection ? GCHandleType.WeakTrackResurrection : GCHandleType.Weak); }
		
		GCHandle weakRef;

		/// <summary>現在の <see cref="WeakHandle"/> オブジェクトが参照するオブジェクトが、ガベージ コレクションで収集されているかどうかを示す値を取得します。</summary>
		public bool IsAlive { get { return weakRef.IsAllocated; } }

		/// <summary>現在の <see cref="WeakHandle"/> オブジェクトが参照するオブジェクト (ターゲット) を取得します。 </summary>
		public object Target { get { return weakRef.Target; } }
		
		/// <summary><see cref="WeakHandle"/> オブジェクトを解放します。</summary>
		public void Free() { weakRef.Free(); }

		/// <summary>現在の <see cref="WeakHandle"/> オブジェクトの識別子を返します。</summary>
		/// <returns>現在の <see cref="WeakHandle"/> オブジェクトの識別子。</returns>
		public override int GetHashCode() { return weakRef.GetHashCode(); }

		/// <summary>指定されたオブジェクトが現在のオブジェクトと等しいかどうかを判断します。</summary>
		/// <param name="obj">等しいかどうかを調べるオブジェクトを指定します。</param>
		/// <returns>現在のオブジェクトが指定されたオブジェクトと等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public override bool Equals(object obj) { return obj is WeakHandle && Equals((WeakHandle)obj); }

		/// <summary>指定した <see cref="WeakHandle"/> オブジェクトが、現在の <see cref="WeakHandle"/> オブジェクトと等しいかどうかを判断します。</summary>
		/// <param name="other">現在の <see cref="WeakHandle"/> オブジェクトと比較する <see cref="WeakHandle"/> オブジェクト。</param>
		/// <returns>指定した <see cref="WeakHandle"/> オブジェクトが現在の <see cref="WeakHandle"/> オブジェクトと等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool Equals(WeakHandle other) { return weakRef.Equals(other.weakRef); }

		/// <summary><see cref="WeakHandle"/> の 2 つのオブジェクトが等しいかどうかを示す値を返します。</summary>
		/// <param name="left"><paramref name="right"/> パラメーターと比較する <see cref="WeakHandle"/> オブジェクト。</param>
		/// <param name="right"><paramref name="left"/> パラメーターと比較する <see cref="WeakHandle"/> オブジェクト。</param>
		/// <returns><paramref name="left"/> パラメーターと <paramref name="right"/> パラメーターが等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool operator ==(WeakHandle left, WeakHandle right) { return left.Equals(right); }

		/// <summary><see cref="WeakHandle"/> の 2 つのオブジェクトが等しくないかどうかを示す値を返します。</summary>
		/// <param name="left"><paramref name="right"/> パラメーターと比較する <see cref="WeakHandle"/> オブジェクト。</param>
		/// <param name="right"><paramref name="left"/> パラメーターと比較する <see cref="WeakHandle"/> オブジェクト。</param>
		/// <returns><paramref name="left"/> パラメーターと <paramref name="right"/> パラメーターが等しくない場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool operator !=(WeakHandle left, WeakHandle right) { return !(left == right); }
	}
}
