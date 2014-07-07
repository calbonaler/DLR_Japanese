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
using System.Linq.Expressions;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.ComInterop
{
	sealed class BoundDispEvent : DynamicObject
	{
		object _rcw;
		Guid _sourceIid;
		int _dispid;

		internal BoundDispEvent(object rcw, Guid sourceIid, int dispid)
		{
			_rcw = rcw;
			_sourceIid = sourceIid;
			_dispid = dispid;
		}

		/// <summary>AddAssign および SubtractAssign 二項演算を実行する実装を提供します。</summary>
		/// <param name="binder">呼び出しサイトにより提供されたバインダーを指定します。</param>
		/// <param name="handler">操作のハンドラーを指定します。</param>
		/// <param name="result">操作の結果を格納する変数を指定します。</param>
		/// <returns>操作が完了した場合は <c>true</c>。呼び出しサイトが動作を決定すべき場合は <c>false</c>。</returns>
		public override bool TryBinaryOperation(BinaryOperationBinder binder, object handler, out object result)
		{
			if (binder.Operation == ExpressionType.AddAssign)
			{
				result = AddHandler(handler);
				return true;
			}
			if (binder.Operation == ExpressionType.SubtractAssign)
			{
				result = RemoveHandler(handler);
				return true;
			}
			result = null;
			return false;
		}

		static void VerifyHandler(object handler)
		{
			if (handler is Delegate && handler.GetType() != typeof(Delegate))
				return; // delegate
			if (handler is IDynamicMetaObjectProvider)
				return; // IDMOP
			if (handler is DispCallable)
				return;
			throw Error.UnsupportedHandlerType();
		}

		/// <summary>イベントにハンドラを追加します。</summary>
		/// <param name="handler">追加されるハンドラを指定します。</param>
		/// <returns>ハンドラが追加された元のイベント。</returns>
		object AddHandler(object handler)
		{
			ContractUtils.RequiresNotNull(handler, "handler");
			VerifyHandler(handler);
			ComEventSink.FromRuntimeCallableWrapper(_rcw, _sourceIid, true).AddHandler(_dispid, handler);
			return this;
		}

		/// <summary>イベントからハンドラを削除します。</summary>
		/// <param name="handler">削除されるハンドラを指定します。</param>
		/// <returns>ハンドラが削除された元のイベント。</returns>
		object RemoveHandler(object handler)
		{
			ContractUtils.RequiresNotNull(handler, "handler");
			VerifyHandler(handler);
			var comEventSink = ComEventSink.FromRuntimeCallableWrapper(_rcw, _sourceIid, false);
			if (comEventSink != null)
				comEventSink.RemoveHandler(_dispid, handler);
			return this;
		}
	}
}