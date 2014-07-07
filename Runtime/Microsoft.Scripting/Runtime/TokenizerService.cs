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

using System.Collections.Generic;
using System.IO;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>言語固有のサービスとしてのトークナイザーを表します。</summary>
	public abstract class TokenizerService
	{
		/// <summary><see cref="Microsoft.Scripting.Runtime.TokenizerService"/> クラスの新しいインスタンスを初期化します。</summary>
		protected TokenizerService() { }

		/// <summary>指定された状態、<see cref="TextReader"/>、翻訳単位、初期位置を使用して、このオブジェクトを初期化します。</summary>
		/// <param name="state">トークナイザの状態を表す <see cref="System.Object"/> 値を指定します。</param>
		/// <param name="sourceReader">ソースコードを読み取る <see cref="TextReader"/> を指定します。</param>
		/// <param name="sourceUnit">ソースコードを格納する翻訳単位を指定します。</param>
		/// <param name="initialLocation">トークナイザの初期位置を指定します。</param>
		public abstract void Initialize(object state, TextReader sourceReader, SourceUnit sourceUnit, SourceLocation initialLocation);

		/// <summary>トークナイザの現在の内部状態を取得します。</summary>
		public abstract object CurrentState { get; }

		/// <summary>トークナイザの現在の位置を取得します。</summary>
		public abstract SourceLocation CurrentPosition { get; }

		/// <summary>トークナイザを次のトークンのすぐ後まで進め、そのカテゴリを取得します。</summary>
		/// <returns>スキャンしたトークンに関連付けられている情報。</returns>
		public abstract TokenInfo ReadToken();

		/// <summary>トークナイザが再開可能かどうかを示す値を取得します。</summary>
		public abstract bool IsRestartable { get; }

		/// <summary>トークナイザのエラーを処理する <see cref="Microsoft.Scripting.ErrorSink"/> オブジェクトを取得または設定します。</summary>
		public abstract ErrorSink ErrorSink { get; set; }

		/// <summary>トークナイザを次のトークンのすぐ後まで進めます。</summary>
		/// <returns>ストリームの末尾に到達した場合は <c>false</c></returns>
		public virtual bool SkipToken() { return ReadToken().Category != TokenCategory.EndOfStream; }

		/// <summary>ストリームのブロックを覆うすべてのトークンを取得します。</summary>
		/// <remarks>startLocation + length がトークンの中間である場合でも、スキャナはすべてのトークンを返すべきです。</remarks>
		/// <param name="characterCount">トークンの読み取りを終了するまでに読み取る文字数を指定します。</param>
		/// <returns>トークン</returns>
		public IEnumerable<TokenInfo> ReadTokens(int characterCount)
		{
			List<TokenInfo> tokens = new List<TokenInfo>();
			int start = CurrentPosition.Index;
			while (CurrentPosition.Index - start < characterCount)
			{
				var token = ReadToken();
				if (token.Category == TokenCategory.EndOfStream)
					break;
				tokens.Add(token);
			}
			return tokens;
		}

		/// <summary>現在の位置からすくなくとも指定された文字数分をスキャンします。</summary>
		/// <param name="countOfChars">トークンの読み取りを終了するまでに読み取る文字数を指定します。</param>
		/// <remarks>このメソッドは任意の開始位置における状態を調べるために使用されます。</remarks>
		/// <returns>ストリームの末尾に到達した場合は <c>false</c></returns>
		public bool SkipTokens(int countOfChars)
		{
			bool eos = false;
			int start_index = CurrentPosition.Index;
			while (CurrentPosition.Index - start_index < countOfChars && (eos = SkipToken())) { }
			return eos;
		}
	}
}
