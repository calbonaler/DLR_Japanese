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
using System.Dynamic;
using System.Globalization;
using System.Linq.Expressions;

namespace Microsoft.Scripting.ComInterop
{
	/// <summary>IDispatch オブジェクトの束縛された dispmember を表します。</summary>
	sealed class DispCallable : IPseudoComObject
	{
		readonly IDispatchComObject _dispatch;
		readonly string _memberName;
		readonly int _dispId;

		internal DispCallable(IDispatchComObject dispatch, string memberName, int dispId)
		{
			_dispatch = dispatch;
			_memberName = memberName;
			_dispId = dispId;
		}

		public override string ToString() { return String.Format(CultureInfo.CurrentCulture, "<bound dispmethod {0}>", _memberName); }

		public IDispatchComObject DispatchComObject { get { return _dispatch; } }

		public IDispatch DispatchObject { get { return _dispatch.DispatchObject; } }

		public string MemberName { get { return _memberName; } }

		public int DispId { get { return _dispId; } }

		public DynamicMetaObject GetMetaObject(Expression parameter) { return new DispCallableMetaObject(parameter, this); }

		public override bool Equals(object obj)
		{
			var other = obj as DispCallable;
			return other != null && other._dispatch == _dispatch && other._dispId == _dispId;
		}

		public override int GetHashCode() { return _dispatch.GetHashCode() ^ _dispId; }
	}
}