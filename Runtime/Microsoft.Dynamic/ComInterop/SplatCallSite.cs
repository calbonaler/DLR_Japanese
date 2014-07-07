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
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Scripting.ComInterop
{
	sealed class SplatCallSite
	{
		// 呼び出し可能なデリゲートまたは IDynamicMetaObjectProvider が格納されます
		internal readonly object _callable;

		// 与えられたイベントに渡す引数の数は呼び出しごとに異なる可能性がある?
		// そうでないなら、このレベルの間接化は要らない。展開を行うデリゲートをキャッシュできるから。
		internal CallSite<Func<CallSite, object, object[], object>> _site;

		internal SplatCallSite(object callable)
		{
			Debug.Assert(callable != null);
			_callable = callable;
		}

		internal object Invoke(object[] args)
		{
			Debug.Assert(args != null);
			// デリゲートなら、DynamicInvoke にバインディングを行わせる。
			var d = _callable as Delegate;
			if (d != null)
				return d.DynamicInvoke(args);
			// そうでないならば、コールサイトを作成し呼び出す。
			if (_site == null)
				_site = CallSite<Func<CallSite, object, object[], object>>.Create(SplatInvokeBinder.Instance);
			return _site.Target(_site, _callable, args);
		}
	}
}