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

namespace Microsoft.Scripting
{
	/// <summary>トークンの種類を表します。</summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
	public enum TokenCategory
	{
		/// <summary>なし</summary>
		None,
		/// <summary>ストリームの終わり</summary>
		EndOfStream,
		/// <summary>空白文字</summary>
		WhiteSpace,
		/// <summary>ブロックコメント</summary>
		Comment,
		/// <summary>単一行コメント</summary>
		LineComment,
		/// <summary>ドキュメントコメント</summary>
		DocComment,
		/// <summary>数値リテラル</summary>
		NumericLiteral,
		/// <summary>文字リテラル</summary>
		CharacterLiteral,
		/// <summary>文字列リテラル</summary>
		StringLiteral,
		/// <summary>正規表現リテラル</summary>
		RegularExpressionLiteral,
		/// <summary>キーワード</summary>
		Keyword,
		/// <summary>ディレクティブ (例: #line)</summary>
		Directive,
		/// <summary>言語で特定の意味を持つ区切り文字</summary>
		Operator,
		/// <summary>2 つの言語要素間で区切りとして動作するトークン</summary>
		Delimiter,
		/// <summary>識別子</summary>
		Identifier,
		/// <summary>波括弧、丸括弧、角括弧</summary>
		Grouping,
		/// <summary>エラー</summary>
		Error,
		/// <summary>言語により定義された要素</summary>
		LanguageDefined = 0x100
	}

	// not currently used, just for info
	/// <summary>トークンの種類を表します。</summary>
	public enum TokenKind
	{
		/// <summary>既定値</summary>
		Default,
		/// <summary>エラー</summary>
		Error,
		/// <summary>空白文字</summary>
		Whitespace,
		/// <summary>行の終端</summary>
		EndOfLine,
		/// <summary>行結合文字</summary>
		LineJoin,               // Python: \<eoln>
		/// <summary>インデント</summary>
		Indentation,

		// Comments:
		/// <summary>単一行コメント</summary>
		SingleLineComment,      // #..., //..., '...
		/// <summary>複数行コメント</summary>
		MultiLineComment,       // /* ... */
		/// <summary>ネスト可能なコメント開始</summary>
		NestableCommentStart,   // Lua: --[[
		/// <summary>ネスト可能なコメント終了</summary>
		NestableCommentEnd,     // ]]

		// DocComments:
		/// <summary>単一行ドキュメントコメント</summary>
		SingleLineDocComment,   // 
		/// <summary>複数行ドキュメントコメント</summary>
		MultiLineDocComment,    // Ruby: =begin =end PHP: /** */

		// Directives:
		/// <summary>ディレクティブ</summary>
		Directive,              // #line, etc.

		// Keywords:
		/// <summary>キーワード</summary>
		Keyword,

		// Identifiers:
		/// <summary>識別子</summary>
		Identifier,             // identifier
		/// <summary>逐語的識別子</summary>
		VerbatimIdentifier,     // PHP/CLR: i'...', 
		/// <summary>変数</summary>
		Variable,               // Ruby: @identifier, @@identifier; PHP, Ruby: $identifier, 

		// Numbers:
		/// <summary>整数リテラル</summary>
		IntegerLiteral,
		/// <summary>浮動小数点リテラル</summary>
		FloatLiteral,

		// Characters:
		/// <summary>文字リテラル</summary>
		CharacterLiteral,

		// Strings:
		/// <summary>文字列</summary>
		String,
		/// <summary>Unicode 文字列</summary>
		UnicodeString,
		/// <summary>フォーマットされた文字列</summary>
		FormattedString,
		/// <summary>フォーマットされた Unicode 文字列</summary>
		FormattedUnicodeString,

		// Groupings:
		/// <summary>左丸括弧</summary>
		LeftParenthesis,        // (
		/// <summary>右丸括弧</summary>
		RightParenthesis,       // )
		/// <summary>左角括弧</summary>
		LeftBracket,            // [
		/// <summary>右角括弧</summary>
		RightBracket,           // ]
		/// <summary>左波括弧</summary>
		LeftBrace,              // {
		/// <summary>右波括弧</summary>
		RightBrace,             // }

		// Delimiters:
		/// <summary>カンマ</summary>
		Comma,                  // ,
		/// <summary>ドット (ピリオド)</summary>
		Dot,                    // .
		/// <summary>セミコロン</summary>
		Semicolon,              // ;
		/// <summary>コロン</summary>
		Colon,                  // :
		/// <summary>2 連続コロン</summary>
		DoubleColon,            // :: 
		/// <summary>3 連続コロン</summary>
		TripleColon,            // PHP/CLR: ::: 

		// Operators:
		/// <summary>加算</summary>
		Plus,                   // +
		/// <summary>インクリメント</summary>
		PlusPlus,               // ++
		/// <summary>加算代入</summary>
		PlusEqual,              // +=
		/// <summary>減算</summary>
		Minus,                  // -
		/// <summary>デクリメント</summary>
		MinusMinus,             // --
		/// <summary>減算代入</summary>
		MinusEqual,             // -=
		/// <summary>乗算</summary>
		Mul,                    // *
		/// <summary>乗算代入</summary>
		MulEqual,               // *=
		/// <summary>除算</summary>
		Div,                    // /
		/// <summary>除算代入</summary>
		DivEqual,               // /=
		/// <summary>整数化除算</summary>
		FloorDivide,            // //
		/// <summary>整数化除算代入</summary>
		FloorDivideEqual,       // //=
		/// <summary>剰余算</summary>
		Mod,                    // %
		/// <summary>剰余算代入</summary>
		ModEqual,               // %=
		/// <summary>冪乗</summary>
		Power,                  // Python: **
		/// <summary>冪乗代入</summary>
		PowerEqual,             // Python, Ruby: **=
		/// <summary>左シフト</summary>
		LeftShift,              // <<
		/// <summary>左シフト代入</summary>
		LeftShiftEqual,         // <<= 
		/// <summary>右シフト</summary>
		RightShift,             // >>
		/// <summary>右シフト代入</summary>
		RightShiftEqual,        // >>=
		/// <summary>ビット積</summary>
		BitwiseAnd,             // &
		/// <summary>ビット積代入</summary>
		BitwiseAndEqual,        // &=
		/// <summary>ビット和</summary>
		BitwiseOr,              // |
		/// <summary>ビット和代入</summary>
		BitwiseOrEqual,         // |=
		/// <summary>排他的論理和</summary>
		Xor,                    // ^
		/// <summary>排他的論理和代入</summary>
		XorEqual,               // ^=
		/// <summary>論理積</summary>
		BooleanAnd,             // &&
		/// <summary>論理積代入</summary>
		BooleanAndEqual,        // Ruby: &&=
		/// <summary>論理和</summary>
		BooleanOr,              // ||
		/// <summary>論理和代入</summary>
		BooleanOrEqual,         // Ruby: ||=
		/// <summary>チルダ</summary>
		Twiddle,                // ~
		/// <summary>チルダ代入</summary>
		TwiddleEqual,           // ~=
		/// <summary>より小さい</summary>
		LessThan,               // <
		/// <summary>より大きい</summary>
		GreaterThan,            // >
		/// <summary>以下</summary>
		LessThanOrEqual,        // <=
		/// <summary>以上</summary>
		GreaterThanOrEqual,     // >=
		/// <summary>代入</summary>
		Assign,                 // =
		/// <summary>エイリアス代入</summary>
		AssignAlias,            // PHP: =&
		/// <summary>コロン代入</summary>
		AssignColon,            // :=
		/// <summary>等しい</summary>
		Equal,                  // == 
		/// <summary>厳密に等しい</summary>
		StrictEqual,            // ===
		/// <summary>否定</summary>
		Not,                    // !
		/// <summary>等しくない</summary>
		NotEqual,               // !=
		/// <summary>厳密に等しくない</summary>
		StrictNotEqual,         // !==
		/// <summary>等しくない</summary>
		Unequal,                // <>
		/// <summary>比較等価</summary>
		CompareEqual,           // Ruby: <=>
		/// <summary>マッチする</summary>
		Match,                  // =~
		/// <summary>マッチしない</summary>
		NotMatch,               // !~
		/// <summary>アロー</summary>
		Arrow,                  // PHP: ->
		/// <summary>二重アロー</summary>
		DoubleArrow,            // PHP, Ruby: =>
		/// <summary>バッククオート</summary>
		BackQuote,              // `
		/// <summary>2 連続ドット</summary>
		DoubleDot,              // Ruby: ..
		/// <summary>3 連続ドット</summary>
		TripleDot,              // Ruby: ...
		/// <summary>アットマーク</summary>
		At,                     // @
		/// <summary>2 連続アットマーク</summary>
		DoubleAt,               // @@
		/// <summary>疑問符</summary>
		Question,               // ?
		/// <summary>2 連続疑問符</summary>
		DoubleQuestion,         // ??
		/// <summary>バックスラッシュ</summary>
		Backslash,              // \
		/// <summary>2 連続バックスラッシュ</summary>
		DoubleBackslash,        // \\
		/// <summary>ドル記号</summary>
		Dollar,                 // $
		/// <summary>2 連続ドル記号</summary>
		DoubleDollar,           // $$
		/// <summary>言語により定義された要素</summary>
		LanguageDefined,
	}
}
