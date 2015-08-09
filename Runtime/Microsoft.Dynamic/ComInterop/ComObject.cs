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
	/// COM �C���X�^���X�̃����^�C���Ăяo���\���b�p�[�ɑ΂���w���p�[ �N���X�ł��B
	/// ���ׂĂ̔ėp RCW �C���X�^���X�ɑ΂��Ă��̌^�̃C���X�^���X�� 1 �쐬���܂��B
	/// </summary>
	class ComObject : IDynamicMetaObjectProvider
	{
		/// <summary>�����^�C���Ăяo���\���b�p�[��\���܂��B</summary>
		readonly object _rcw;

		internal ComObject(object rcw)
		{
			Debug.Assert(ComObject.IsComObject(rcw));
			_rcw = rcw;
		}

		internal object RuntimeCallableWrapper { get { return _rcw; } }

		readonly static object _ComObjectInfoKey = new object();

		/// <summary>RCW �ɑΉ����� <see cref="ComObject"/> ���擾���܂��B</summary>
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

		/// <summary>�w�肳�ꂽ <see cref="ComObject"/> �����b�v�������� <see cref="Expression"/> ��Ԃ��܂��B</summary>
		internal static MemberExpression RcwFromComObject(Expression comObject)
		{
			Debug.Assert(comObject != null && typeof(ComObject).IsAssignableFrom(comObject.Type), "ComObject �ł���K�v������܂��B");
			return Expression.Property(
				Ast.Utils.Convert(comObject, typeof(ComObject)),
				typeof(ComObject).GetProperty("RuntimeCallableWrapper", BindingFlags.NonPublic | BindingFlags.Instance)
			);
		}

		/// <summary>�w�肳�ꂽ RCW �ɑΉ����� <see cref="ComObject"/> ���擾�܂��͍쐬���� <see cref="Expression"/> ��Ԃ��܂��B</summary>
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

		// System.Runtime.InteropServices.Marshal.IsComObject(obj) �͕����M���ł͎g�p�ł��Ȃ�
		internal static bool IsComObject(object obj) { return obj != null && ComObjectType.IsAssignableFrom(obj.GetType()); }
	}
}