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
using System.Collections.ObjectModel;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>保存される <see cref="ScriptCode"/> に適用され、ディスクからの <see cref="ScriptCode"/> の再作成に使用される属性を表します。</summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public sealed class DlrCachedCodeAttribute : Attribute { }

	/// <summary>キャッシュされ最適化されているメソッドをマークします。</summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public sealed class CachedOptimizedCodeAttribute : Attribute
	{
		readonly ReadOnlyCollection<string> _names;

		/// <summary><see cref="Microsoft.Scripting.Runtime.CachedOptimizedCodeAttribute"/> クラスの新しいインスタンスを初期化します。</summary>
		public CachedOptimizedCodeAttribute() { _names = EmptyReadOnlyCollection<string>.Instance; }

		/// <summary>スコープ内の名前を使用して、<see cref="Microsoft.Scripting.Runtime.CachedOptimizedCodeAttribute"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="names">最適化されたスコープに格納されている名前を指定します。</param>
		public CachedOptimizedCodeAttribute(string[] names)
		{
			ContractUtils.RequiresNotNull(names, "names");
			_names = names.ToReadOnly();
		}

		/// <summary>最適化されたスコープに格納されている名前を取得します。</summary>
		public ReadOnlyCollection<string> Names { get { return _names; } }
	}
}
