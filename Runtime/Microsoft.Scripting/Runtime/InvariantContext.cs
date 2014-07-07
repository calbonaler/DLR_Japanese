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

namespace Microsoft.Scripting.Runtime
{
	/// <summary>言語中立な <see cref="LanguageContext"/> を表します。</summary>
	sealed class InvariantContext : LanguageContext
	{
		// friend: ScriptDomainManager
		/// <summary>指定された <see cref="ScriptDomainManager"/> を使用して、<see cref="InvariantContext"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="manager">言語コンテキストが実行される <see cref="ScriptDomainManager"/> を指定します。</param>
		internal InvariantContext(ScriptDomainManager manager) : base(manager) { }

		/// <summary>言語がコードを解析したり、翻訳入力単位を作成したりできるかどうかを示す値を取得します。</summary>
		public override bool CanCreateSourceCode { get { return false; } }

		/// <summary>ソースコードを指定されたコンパイラコンテキスト内で解析します。解析する翻訳単位はコンテキストによって保持されます。</summary>
		/// <param name="sourceUnit">解析する翻訳単位を指定します。</param>
		/// <param name="options">解析に関するオプションを指定します。</param>
		/// <param name="errorSink">解析時のエラーを処理する <see cref="ErrorSink"/> を指定します。</param>
		public override ScriptCode CompileSourceCode(SourceUnit sourceUnit, CompilerOptions options, ErrorSink errorSink) { throw new NotSupportedException(); }
	}
}
