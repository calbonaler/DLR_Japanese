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
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.Scripting.ComInterop
{
	/// <summary>
	/// COM インスタンスのランタイム呼び出し可能ラッパーに対するヘルパー クラスです。
	/// すべての汎用 RCW インスタンスに対してこの型のインスタンスを 1 つ作成します。
	/// </summary>
	class ComObject : IDynamicMetaObjectProvider
	{
		/// <summary>ランタイム呼び出し可能ラッパーを表します。</summary>
		readonly object _rcw;

		internal ComObject(object rcw)
		{
			Debug.Assert(ComObject.IsComObject(rcw));
			_rcw = rcw;
		}

		internal object RuntimeCallableWrapper { get { return _rcw; } }

		readonly static object _ComObjectInfoKey = new object();

		/// <summary>RCW に対応する <see cref="ComObject"/> を取得します。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
		public static ComObject ObjectToComObject(object rcw)
		{
			Debug.Assert(ComObject.IsComObject(rcw));
			// Marshal.Get/SetComObjectData has a LinkDemand for UnmanagedCode which will turn into
			// a full demand. We could avoid this by making this method SecurityCritical
			var data = Marshal.GetComObjectData(rcw, _ComObjectInfoKey);
			if (data != null)
				return (ComObject)data;
			lock (_ComObjectInfoKey)
			{
				data = Marshal.GetComObjectData(rcw, _ComObjectInfoKey);
				if (data != null)
					return (ComObject)data;
				var comObjectInfo = CreateComObject(rcw);
				if (!Marshal.SetComObjectData(rcw, _ComObjectInfoKey, comObjectInfo))
					throw Error.SetComObjectDataFailed();
				return comObjectInfo;
			}
		}

		/// <summary>指定された <see cref="ComObject"/> をラップ解除する <see cref="Expression"/> を返します。</summary>
		internal static MemberExpression RcwFromComObject(Expression comObject)
		{
			Debug.Assert(comObject != null && typeof(ComObject).IsAssignableFrom(comObject.Type), "ComObject である必要があります。");
			return Expression.Property(
				Ast.Utils.Convert(comObject, typeof(ComObject)),
				typeof(ComObject).GetProperty("RuntimeCallableWrapper", BindingFlags.NonPublic | BindingFlags.Instance)
			);
		}

		/// <summary>指定された RCW に対応する <see cref="ComObject"/> を取得または作成する <see cref="Expression"/> を返します。</summary>
		internal static MethodCallExpression RcwToComObject(Expression rcw) { return Expression.Call(new Func<object, ComObject>(ObjectToComObject).Method, Ast.Utils.Convert(rcw, typeof(object))); }

		static ComObject CreateComObject(object rcw)
		{
			var dispatchObject = rcw as IDispatch;
			return dispatchObject != null ? new IDispatchComObject(dispatchObject) : new ComObject(rcw);
		}

		internal virtual IList<string> GetMemberNames(bool dataOnly) { return new string[0]; }

		internal virtual IList<KeyValuePair<string, object>> GetMembers(IEnumerable<string> names) { return new KeyValuePair<string, object>[0]; }

		DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) { return new ComFallbackMetaObject(parameter, BindingRestrictions.Empty, this); }

		static readonly Type ComObjectType = typeof(object).Assembly.GetType("System.__ComObject");

		// System.Runtime.InteropServices.Marshal.IsComObject(obj) は部分信頼では使用できない
		internal static bool IsComObject(object obj) { return obj != null && ComObjectType.IsAssignableFrom(obj.GetType()); }
	}
}