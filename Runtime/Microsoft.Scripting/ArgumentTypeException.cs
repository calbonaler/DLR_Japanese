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
using System.Runtime.Serialization;

namespace Microsoft.Scripting
{
	/// <summary>メソッドに渡された引数の型が正しくない場合にスローされる例外。</summary>
	[Serializable]
	public class ArgumentTypeException : Exception
	{
		/// <summary><see cref="Microsoft.Scripting.ArgumentTypeException"/> クラスの新しいインスタンスを初期化します。</summary>
		public ArgumentTypeException() : base() { }
		
		/// <summary>指定したメッセージを使用して、<see cref="Microsoft.Scripting.ArgumentTypeException"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="message">エラーを説明するメッセージ。</param>
		public ArgumentTypeException(string message) : base(message) { }

		/// <summary>指定したエラー メッセージと、この例外の原因である内部例外への参照を使用して、<see cref="Microsoft.Scripting.ArgumentTypeException"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="message">例外の原因を説明するエラー メッセージ。</param>
		/// <param name="innerException">現在の例外の原因である例外。内部例外が指定されていない場合は <c>null</c> 参照 (Visual Basic では、Nothing)。</param>
		public ArgumentTypeException(string message, Exception innerException) : base(message, innerException) { }

		/// <summary>シリアル化したデータを使用して、<see cref="Microsoft.Scripting.ArgumentTypeException"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="info">スローされている例外に関するシリアル化済みオブジェクト データを保持している <see cref="System.Runtime.Serialization.SerializationInfo"/>。</param>
		/// <param name="context">転送元または転送先に関するコンテキスト情報を含んでいる <see cref="System.Runtime.Serialization.StreamingContext"/>。</param>
		protected ArgumentTypeException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}
