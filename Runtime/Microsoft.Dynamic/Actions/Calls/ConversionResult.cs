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
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>引数をある型から別の型に変換する際に発生したエラーに関する情報を表します。</summary>
	public sealed class ConversionResult
	{
		/// <summary>引数の値、型、変換先の型、および変換が失敗したかどうかを示す値を使用して、<see cref="Microsoft.Scripting.Actions.Calls.ConversionResult"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="arg">実引数の値を指定します。</param>
		/// <param name="argType">実引数の型または制限型を指定します。</param>
		/// <param name="toType">値の変換先の型を指定します。</param>
		/// <param name="failed">変換が失敗したかどうかを示す値を指定します。</param>
		internal ConversionResult(object arg, Type argType, Type toType, bool failed)
		{
			Arg = arg;
			ArgType = argType;
			To = toType;
			Failed = failed;
		}

		/// <summary>利用可能であれば実引数の値を取得します。</summary>
		public object Arg { get; private set; }

		/// <summary>値が既知であれば、実引数の型または制限型を取得します。値が <c>null</c> である場合は、<see cref="Microsoft.Scripting.Runtime.DynamicNull"/> を返します。</summary>
		public Type ArgType { get; private set; }

		/// <summary>値の変換先の型を取得します。</summary>
		public Type To { get; private set; }

		/// <summary>変換が失敗したかどうかを示す値を取得します。</summary>
		public bool Failed { get; private set; }

		/// <summary>リストの最後の <see cref="ConversionResult"/> の <see cref="Failed"/> プロパティに指定された引数を設定します。</summary>
		/// <param name="failures">最後の <see cref="ConversionResult"/> を書き換えるリストを指定します。</param>
		/// <param name="isFailure">最後の <see cref="ConversionResult"/> の <see cref="Failed"/> プロパティに設定する値を指定します。</param>
		internal static void ReplaceLastFailure(IList<ConversionResult> failures, bool isFailure)
		{
			ConversionResult failure = failures[failures.Count - 1];
			failures.RemoveAt(failures.Count - 1);
			failures.Add(new ConversionResult(failure.Arg, failure.ArgType, failure.To, isFailure));
		}

		/// <summary>指定されたバインダーを使用して引数の型名を取得します。</summary>
		/// <param name="binder">型名を取得する <see cref="ActionBinder"/> を指定します。</param>
		/// <returns>現在の引数の型名。</returns>
		public string GetArgumentTypeName(ActionBinder binder)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			return Arg != null ? binder.GetObjectTypeName(Arg) : binder.GetTypeName(ArgType);
		}
	}
}
