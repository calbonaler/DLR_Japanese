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
using System.Security.Permissions;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting
{
	/// <summary>標準のソースコードトークン化を提供します。</summary>
	public sealed class TokenCategorizer : MarshalByRefObject
	{
		readonly TokenizerService _tokenizer;

		/// <summary>指定された <see cref="Microsoft.Scripting.Runtime.TokenizerService"/> を使用して、<see cref="Microsoft.Scripting.Hosting.TokenCategorizer"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="tokenizer">ラップする <see cref="Microsoft.Scripting.Runtime.TokenizerService"/> を指定します。</param>
		internal TokenCategorizer(TokenizerService tokenizer)
		{
			Assert.NotNull(tokenizer);
			_tokenizer = tokenizer;
		}

		/// <summary>基になるトークナイザを初期化します。</summary>
		/// <param name="state">トークナイザの状態を指定します。</param>
		/// <param name="scriptSource">トークン化の対象となる <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> オブジェクトを指定します。</param>
		/// <param name="initialLocation">トークン化の開始位置を表す <see cref="Microsoft.Scripting.SourceLocation"/> を指定します。</param>
		public void Initialize(object state, ScriptSource scriptSource, SourceLocation initialLocation) { _tokenizer.Initialize(state, scriptSource.SourceUnit.GetReader(), scriptSource.SourceUnit, initialLocation); }

		/// <summary>トークナイザの現在の内部状態を取得します。</summary>
		public object CurrentState { get { return _tokenizer.CurrentState; } }

		/// <summary>トークナイザの現在の位置を取得します。</summary>
		public SourceLocation CurrentPosition { get { return _tokenizer.CurrentPosition; } }

		/// <summary>トークナイザを次のトークンのすぐ後まで進め、そのカテゴリを取得します。</summary>
		/// <returns>スキャンしたトークンに関連付けられている情報。</returns>
		public TokenInfo ReadToken() { return _tokenizer.ReadToken(); }

		/// <summary>トークナイザが再開可能かどうかを示す値を取得します。</summary>
		public bool IsRestartable { get { return _tokenizer.IsRestartable; } }

		// TODO: Should be ErrorListener
		/// <summary>トークナイザのエラーを処理する <see cref="Microsoft.Scripting.ErrorSink"/> オブジェクトを取得または設定します。</summary>
		public ErrorSink ErrorSink
		{
			get { return _tokenizer.ErrorSink; }
			set { _tokenizer.ErrorSink = value; }
		}

		/// <summary>トークナイザを次のトークンのすぐ後まで進めます。</summary>
		/// <returns>ストリームの末尾に到達した場合は <c>false</c></returns>
		public bool SkipToken() { return _tokenizer.SkipToken(); }

		/// <summary>ストリームのブロックを覆うすべてのトークンを取得します。</summary>
		/// <remarks>startLocation + length がトークンの中間である場合でも、スキャナはすべてのトークンを返すべきです。</remarks>
		/// <param name="characterCount">トークンの読み取りを終了するまでに読み取る文字数を指定します。</param>
		/// <returns>トークン</returns>
		public IEnumerable<TokenInfo> ReadTokens(int characterCount) { return _tokenizer.ReadTokens(characterCount); }

		/// <summary>現在の位置からすくなくとも指定された文字数分をスキャンします。</summary>
		/// <param name="characterCount">トークンの読み取りを終了するまでに読み取る文字数を指定します。</param>
		/// <remarks>このメソッドは任意の開始位置における状態を調べるために使用されます。</remarks>
		/// <returns>ストリームの末尾に到達した場合は <c>false</c></returns>
		public bool SkipTokens(int characterCount) { return _tokenizer.SkipTokens(characterCount); }

		// TODO: Figure out what is the right lifetime
		/// <summary>対象のインスタンスの有効期間ポリシーを制御する、有効期間サービス オブジェクトを取得します。</summary>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
		public override object InitializeLifetimeService() { return null; }
	}
}
