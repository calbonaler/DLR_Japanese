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
using System.Runtime.Remoting;
using System.Security.Permissions;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting
{
	/// <summary>言語内で発生した例外に関する操作を提供します。</summary>
	public sealed class ExceptionOperations : MarshalByRefObject
	{
		readonly LanguageContext _context;

		/// <summary>
		/// 言語に関する情報を表す <see cref="Microsoft.Scripting.Runtime.LanguageContext"/> を使用して、
		/// <see cref="Microsoft.Scripting.Hosting.ExceptionOperations"/> クラスの新しいインスタンスを初期化します。
		/// </summary>
		/// <param name="context">言語に関する情報を表す <see cref="Microsoft.Scripting.Runtime.LanguageContext"/> を指定します。</param>
		internal ExceptionOperations(LanguageContext context) { _context = context; }

		/// <summary>指定された例外を表す文字列を取得します。</summary>
		/// <param name="exception">文字列を取得する例外を指定します。</param>
		public string FormatException(Exception exception) { return _context.FormatException(exception); }

		/// <summary>指定された例外に対するメッセージおよび例外の型を取得します。</summary>
		/// <param name="exception">メッセージおよび例外の型を取得する例外を指定します。</param>
		/// <param name="message">取得するメッセージを格納する変数を指定します。</param>
		/// <param name="errorTypeName">取得する例外の型を格納する変数を指定します。</param>
		public void GetExceptionMessage(Exception exception, out string message, out string errorTypeName) { _context.GetExceptionMessage(exception, out message, out errorTypeName); }

		/// <summary>指定された例外をハンドルし、ハンドルに成功したかどうかを示す値を返します。</summary>
		/// <param name="exception">ハンドルする例外を指定します。</param>
		public bool HandleException(Exception exception)
		{
			ContractUtils.RequiresNotNull(exception, "exception");
			return false;
		}

		/// <summary>例外に対するスタックフレームを返します。</summary>
		/// <param name="exception">スタックフレームを取得する例外を指定します。</param>
		public IList<DynamicStackFrame> GetStackFrames(Exception exception)
		{
			ContractUtils.RequiresNotNull(exception, "exception");
			return _context.GetStackFrames(exception);
		}

		/// <summary>指定された例外を表す文字列を取得します。</summary>
		/// <param name="exception">文字列を取得する例外をラップしている <see cref="System.Runtime.Remoting.ObjectHandle"/> を指定します。</param>
		public string FormatException(ObjectHandle exception)
		{
			ContractUtils.RequiresNotNull(exception, "exception");
			var exceptionObj = exception.Unwrap() as Exception;
			ContractUtils.Requires(exceptionObj != null, "exception", "ObjectHandle must be to Exception object");
			return _context.FormatException(exceptionObj);
		}

		/// <summary>指定された例外に対するメッセージおよび例外の型を取得します。</summary>
		/// <param name="exception">メッセージおよび例外の型を取得する例外をラップしている <see cref="System.Runtime.Remoting.ObjectHandle"/> を指定します。</param>
		/// <param name="message">取得するメッセージを格納する変数を指定します。</param>
		/// <param name="errorTypeName">取得する例外の型を格納する変数を指定します。</param>
		public void GetExceptionMessage(ObjectHandle exception, out string message, out string errorTypeName)
		{
			ContractUtils.RequiresNotNull(exception, "exception");
			var exceptionObj = exception.Unwrap() as Exception;
			ContractUtils.Requires(exceptionObj != null, "exception", "ObjectHandle must be to Exception object");
			_context.GetExceptionMessage(exceptionObj, out message, out errorTypeName);
		}

		/// <summary>指定された例外をハンドルし、ハンドルに成功したかどうかを示す値を返します。</summary>
		/// <param name="exception">ハンドルする例外をラップしている <see cref="System.Runtime.Remoting.ObjectHandle"/> を指定します。</param>
		public bool HandleException(ObjectHandle exception)
		{
			ContractUtils.RequiresNotNull(exception, "exception");
			var exceptionObj = exception.Unwrap() as Exception;
			ContractUtils.Requires(exceptionObj != null, "exception", "ObjectHandle must be to Exception object");
			return false;
		}

		/// <summary>例外に対するスタックフレームを返します。</summary>
		/// <param name="exception">スタックフレームを取得する例外をラップしている <see cref="System.Runtime.Remoting.ObjectHandle"/> を指定します。</param>
		public IList<DynamicStackFrame> GetStackFrames(ObjectHandle exception)
		{
			ContractUtils.RequiresNotNull(exception, "exception");
			var exceptionObj = exception.Unwrap() as Exception;
			ContractUtils.Requires(exceptionObj != null, "exception", "ObjectHandle must be to Exception object");
			return _context.GetStackFrames(exceptionObj);
		}

		/// <summary>対象のインスタンスの有効期間ポリシーを制御する、有効期間サービス オブジェクトを取得します。</summary>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
		public override object InitializeLifetimeService() { return null; }
	}
}
