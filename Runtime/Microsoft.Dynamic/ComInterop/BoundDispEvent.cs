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

		/// <summary>AddAssign ����� SubtractAssign �񍀉��Z�����s���������񋟂��܂��B</summary>
		/// <param name="binder">�Ăяo���T�C�g�ɂ��񋟂��ꂽ�o�C���_�[���w�肵�܂��B</param>
		/// <param name="handler">����̃n���h���[���w�肵�܂��B</param>
		/// <param name="result">����̌��ʂ��i�[����ϐ����w�肵�܂��B</param>
		/// <returns>���삪���������ꍇ�� <c>true</c>�B�Ăяo���T�C�g����������肷�ׂ��ꍇ�� <c>false</c>�B</returns>
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

		/// <summary>�C�x���g�Ƀn���h����ǉ����܂��B</summary>
		/// <param name="handler">�ǉ������n���h�����w�肵�܂��B</param>
		/// <returns>�n���h�����ǉ����ꂽ���̃C�x���g�B</returns>
		object AddHandler(object handler)
		{
			ContractUtils.RequiresNotNull(handler, "handler");
			VerifyHandler(handler);
			ComEventSink.FromRuntimeCallableWrapper(_rcw, _sourceIid, true).AddHandler(_dispid, handler);
			return this;
		}

		/// <summary>�C�x���g����n���h�����폜���܂��B</summary>
		/// <param name="handler">�폜�����n���h�����w�肵�܂��B</param>
		/// <returns>�n���h�����폜���ꂽ���̃C�x���g�B</returns>
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