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
using System.Linq.Expressions;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions
{
	/// <summary>
	/// 動的操作が実行できない場合に生成されるべき結果に関する情報をカプセル化します。
	/// <see cref="ErrorInfo"/> はバインディングの失敗に応じて <see cref="ActionBinder"/> によって生成されます。
	/// </summary>
	/// <remarks>
	/// <see cref="ErrorInfo"/> は次のうち 1 つを保持します:
	/// スローされる例外を作成する <see cref="Expression"/>。
	/// ユーザーに直接返され、エラーが発生したことを示す値を生成する <see cref="Expression"/>。(JavaScript における undefined など)
	/// ユーザーに直接返されるが、実際はエラーを表さない値を生成する <see cref="Expression"/>。
	/// </remarks>
	public sealed class ErrorInfo
	{
		ErrorInfo(Expression value, ErrorInfoKind kind)
		{
			Debug.Assert(value != null);
			Expression = value;
			Kind = kind;
		}

		/// <summary>スローされる例外を表す新しい <see cref="Microsoft.Scripting.Actions.ErrorInfo"/> を作成します。</summary>
		/// <param name="exceptionValue">例外を表す <see cref="Expression"/> を指定します。</param>
		/// <returns>例外を表す <see cref="ErrorInfo"/>。</returns>
		public static ErrorInfo FromException(Expression exceptionValue)
		{
			ContractUtils.RequiresNotNull(exceptionValue, "exceptionValue");
			ContractUtils.Requires(typeof(Exception).IsAssignableFrom(exceptionValue.Type), "exceptionValue", Strings.MustBeExceptionInstance);
			return new ErrorInfo(exceptionValue, ErrorInfoKind.Exception);
		}

		/// <summary>ユーザーに返されるエラーを表す値を表す新しい <see cref="Microsoft.Scripting.Actions.ErrorInfo"/> を作成します。</summary>
		/// <param name="resultValue">ユーザーに返されるエラーを表す <see cref="Expression"/> を指定します。</param>
		/// <returns>ユーザーに返される値を表す <see cref="ErrorInfo"/>。</returns>
		public static ErrorInfo FromValue(Expression resultValue)
		{
			ContractUtils.RequiresNotNull(resultValue, "resultValue");
			return new ErrorInfo(resultValue, ErrorInfoKind.Error);
		}

		/// <summary>ユーザーに返されるがエラーは表さない値を表す新しい <see cref="Microsoft.Scripting.Actions.ErrorInfo"/> を作成します。</summary>
		/// <param name="resultValue">ユーザーに返されるエラーを表さない <see cref="Expression"/> を指定します。</param>
		/// <returns>ユーザーに返されるエラーを表さない値を表す <see cref="ErrorInfo"/>。</returns>
		public static ErrorInfo FromValueNoError(Expression resultValue)
		{
			ContractUtils.RequiresNotNull(resultValue, "resultValue");
			return new ErrorInfo(resultValue, ErrorInfoKind.Success);
		}

		/// <summary>この <see cref="ErrorInfo"/> オブジェクトが表す値の種類を取得します。</summary>
		public ErrorInfoKind Kind { get; private set; }

		/// <summary>この <see cref="ErrorInfo"/> オブジェクトの値を表す <see cref="Expression"/> を取得します。</summary>
		public Expression Expression { get; private set; }
	}

	/// <summary><see cref="ErrorInfo"/> が表す値の種類を表します。</summary>
	public enum ErrorInfoKind
	{
		/// <summary><see cref="ErrorInfo"/> は例外を表します。</summary>
		Exception,
		/// <summary><see cref="ErrorInfo"/> はエラーを表す値を表します。</summary>
		Error,
		/// <summary><see cref="ErrorInfo"/> はエラーではない値を表します。</summary>
		Success
	}
}
