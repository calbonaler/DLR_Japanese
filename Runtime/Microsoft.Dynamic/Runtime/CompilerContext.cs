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

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>
	/// コンパイラの実行のために流し込まれたコンテキストを表します。
	/// 言語はこのクラスから派生することで、追加のコンテキスト情報を提供することができます。
	/// </summary>
	public sealed class CompilerContext
	{
		/// <summary><see cref="CompilerContext"/> で現在コンパイルされている <see cref="SourceUnit"/> を取得します。</summary>
		public SourceUnit SourceUnit { get; private set; }

		/// <summary>パーサーのコールバック (例: かっこの一致など) が通知されるオブジェクトを取得します。</summary>
		public ParserSink ParserSink { get; private set; }

		/// <summary>現在の <see cref="ErrorSink"/> を取得します。</summary>
		public ErrorSink Errors { get; private set; }

		/// <summary>コンパイラ固有のオプションを取得します。</summary>
		public CompilerOptions Options { get; private set; }

		/// <summary>翻訳入力単位、コンパイラオプション、<see cref="ErrorSink"/> を使用して、<see cref="Microsoft.Scripting.Runtime.CompilerContext"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="sourceUnit">コンパイルされる翻訳入力単位を指定します。</param>
		/// <param name="options">コンパイラのオプションを指定します。</param>
		/// <param name="errorSink">エラーが通知されるオブジェクトを指定します。</param>
		public CompilerContext(SourceUnit sourceUnit, CompilerOptions options, ErrorSink errorSink) : this(sourceUnit, options, errorSink, ParserSink.Null) { }

		/// <summary>翻訳入力単位、コンパイラオプション、<see cref="ErrorSink"/>、<see cref="ParserSink"/> を使用して、<see cref="Microsoft.Scripting.Runtime.CompilerContext"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="sourceUnit">コンパイルされる翻訳入力単位を指定します。</param>
		/// <param name="options">コンパイラのオプションを指定します。</param>
		/// <param name="errorSink">エラーが通知されるオブジェクトを指定します。</param>
		/// <param name="parserSink">パーサーのコールバックが通知されるオブジェクトを指定します。</param>
		public CompilerContext(SourceUnit sourceUnit, CompilerOptions options, ErrorSink errorSink, ParserSink parserSink)
		{
			ContractUtils.RequiresNotNull(sourceUnit, "sourceUnit");
			ContractUtils.RequiresNotNull(errorSink, "errorSink");
			ContractUtils.RequiresNotNull(parserSink, "parserSink");
			ContractUtils.RequiresNotNull(options, "options");
			SourceUnit = sourceUnit;
			Options = options;
			Errors = errorSink;
			ParserSink = parserSink;
		}
	}
}
