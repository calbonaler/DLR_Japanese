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
	/// <summary>�g�[�N���̎�ނ�\���܂��B</summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
	public enum TokenCategory
	{
		/// <summary>�Ȃ�</summary>
		None,
		/// <summary>�X�g���[���̏I���</summary>
		EndOfStream,
		/// <summary>�󔒕���</summary>
		WhiteSpace,
		/// <summary>�u���b�N�R�����g</summary>
		Comment,
		/// <summary>�P��s�R�����g</summary>
		LineComment,
		/// <summary>�h�L�������g�R�����g</summary>
		DocComment,
		/// <summary>���l���e����</summary>
		NumericLiteral,
		/// <summary>�������e����</summary>
		CharacterLiteral,
		/// <summary>�����񃊃e����</summary>
		StringLiteral,
		/// <summary>���K�\�����e����</summary>
		RegularExpressionLiteral,
		/// <summary>�L�[���[�h</summary>
		Keyword,
		/// <summary>�f�B���N�e�B�u (��: #line)</summary>
		Directive,
		/// <summary>����œ���̈Ӗ�������؂蕶��</summary>
		Operator,
		/// <summary>2 �̌���v�f�Ԃŋ�؂�Ƃ��ē��삷��g�[�N��</summary>
		Delimiter,
		/// <summary>���ʎq</summary>
		Identifier,
		/// <summary>�g���ʁA�ۊ��ʁA�p����</summary>
		Grouping,
		/// <summary>�G���[</summary>
		Error,
		/// <summary>����ɂ���`���ꂽ�v�f</summary>
		LanguageDefined = 0x100
	}

	// not currently used, just for info
	/// <summary>�g�[�N���̎�ނ�\���܂��B</summary>
	public enum TokenKind
	{
		/// <summary>����l</summary>
		Default,
		/// <summary>�G���[</summary>
		Error,
		/// <summary>�󔒕���</summary>
		Whitespace,
		/// <summary>�s�̏I�[</summary>
		EndOfLine,
		/// <summary>�s��������</summary>
		LineJoin,               // Python: \<eoln>
		/// <summary>�C���f���g</summary>
		Indentation,

		// Comments:
		/// <summary>�P��s�R�����g</summary>
		SingleLineComment,      // #..., //..., '...
		/// <summary>�����s�R�����g</summary>
		MultiLineComment,       // /* ... */
		/// <summary>�l�X�g�\�ȃR�����g�J�n</summary>
		NestableCommentStart,   // Lua: --[[
		/// <summary>�l�X�g�\�ȃR�����g�I��</summary>
		NestableCommentEnd,     // ]]

		// DocComments:
		/// <summary>�P��s�h�L�������g�R�����g</summary>
		SingleLineDocComment,   // 
		/// <summary>�����s�h�L�������g�R�����g</summary>
		MultiLineDocComment,    // Ruby: =begin =end PHP: /** */

		// Directives:
		/// <summary>�f�B���N�e�B�u</summary>
		Directive,              // #line, etc.

		// Keywords:
		/// <summary>�L�[���[�h</summary>
		Keyword,

		// Identifiers:
		/// <summary>���ʎq</summary>
		Identifier,             // identifier
		/// <summary>����I���ʎq</summary>
		VerbatimIdentifier,     // PHP/CLR: i'...', 
		/// <summary>�ϐ�</summary>
		Variable,               // Ruby: @identifier, @@identifier; PHP, Ruby: $identifier, 

		// Numbers:
		/// <summary>�������e����</summary>
		IntegerLiteral,
		/// <summary>���������_���e����</summary>
		FloatLiteral,

		// Characters:
		/// <summary>�������e����</summary>
		CharacterLiteral,

		// Strings:
		/// <summary>������</summary>
		String,
		/// <summary>Unicode ������</summary>
		UnicodeString,
		/// <summary>�t�H�[�}�b�g���ꂽ������</summary>
		FormattedString,
		/// <summary>�t�H�[�}�b�g���ꂽ Unicode ������</summary>
		FormattedUnicodeString,

		// Groupings:
		/// <summary>���ۊ���</summary>
		LeftParenthesis,        // (
		/// <summary>�E�ۊ���</summary>
		RightParenthesis,       // )
		/// <summary>���p����</summary>
		LeftBracket,            // [
		/// <summary>�E�p����</summary>
		RightBracket,           // ]
		/// <summary>���g����</summary>
		LeftBrace,              // {
		/// <summary>�E�g����</summary>
		RightBrace,             // }

		// Delimiters:
		/// <summary>�J���}</summary>
		Comma,                  // ,
		/// <summary>�h�b�g (�s���I�h)</summary>
		Dot,                    // .
		/// <summary>�Z�~�R����</summary>
		Semicolon,              // ;
		/// <summary>�R����</summary>
		Colon,                  // :
		/// <summary>2 �A���R����</summary>
		DoubleColon,            // :: 
		/// <summary>3 �A���R����</summary>
		TripleColon,            // PHP/CLR: ::: 

		// Operators:
		/// <summary>���Z</summary>
		Plus,                   // +
		/// <summary>�C���N�������g</summary>
		PlusPlus,               // ++
		/// <summary>���Z���</summary>
		PlusEqual,              // +=
		/// <summary>���Z</summary>
		Minus,                  // -
		/// <summary>�f�N�������g</summary>
		MinusMinus,             // --
		/// <summary>���Z���</summary>
		MinusEqual,             // -=
		/// <summary>��Z</summary>
		Mul,                    // *
		/// <summary>��Z���</summary>
		MulEqual,               // *=
		/// <summary>���Z</summary>
		Div,                    // /
		/// <summary>���Z���</summary>
		DivEqual,               // /=
		/// <summary>���������Z</summary>
		FloorDivide,            // //
		/// <summary>���������Z���</summary>
		FloorDivideEqual,       // //=
		/// <summary>��]�Z</summary>
		Mod,                    // %
		/// <summary>��]�Z���</summary>
		ModEqual,               // %=
		/// <summary>�p��</summary>
		Power,                  // Python: **
		/// <summary>�p����</summary>
		PowerEqual,             // Python, Ruby: **=
		/// <summary>���V�t�g</summary>
		LeftShift,              // <<
		/// <summary>���V�t�g���</summary>
		LeftShiftEqual,         // <<= 
		/// <summary>�E�V�t�g</summary>
		RightShift,             // >>
		/// <summary>�E�V�t�g���</summary>
		RightShiftEqual,        // >>=
		/// <summary>�r�b�g��</summary>
		BitwiseAnd,             // &
		/// <summary>�r�b�g�ϑ��</summary>
		BitwiseAndEqual,        // &=
		/// <summary>�r�b�g�a</summary>
		BitwiseOr,              // |
		/// <summary>�r�b�g�a���</summary>
		BitwiseOrEqual,         // |=
		/// <summary>�r���I�_���a</summary>
		Xor,                    // ^
		/// <summary>�r���I�_���a���</summary>
		XorEqual,               // ^=
		/// <summary>�_����</summary>
		BooleanAnd,             // &&
		/// <summary>�_���ϑ��</summary>
		BooleanAndEqual,        // Ruby: &&=
		/// <summary>�_���a</summary>
		BooleanOr,              // ||
		/// <summary>�_���a���</summary>
		BooleanOrEqual,         // Ruby: ||=
		/// <summary>�`���_</summary>
		Twiddle,                // ~
		/// <summary>�`���_���</summary>
		TwiddleEqual,           // ~=
		/// <summary>��菬����</summary>
		LessThan,               // <
		/// <summary>���傫��</summary>
		GreaterThan,            // >
		/// <summary>�ȉ�</summary>
		LessThanOrEqual,        // <=
		/// <summary>�ȏ�</summary>
		GreaterThanOrEqual,     // >=
		/// <summary>���</summary>
		Assign,                 // =
		/// <summary>�G�C���A�X���</summary>
		AssignAlias,            // PHP: =&
		/// <summary>�R�������</summary>
		AssignColon,            // :=
		/// <summary>������</summary>
		Equal,                  // == 
		/// <summary>�����ɓ�����</summary>
		StrictEqual,            // ===
		/// <summary>�ے�</summary>
		Not,                    // !
		/// <summary>�������Ȃ�</summary>
		NotEqual,               // !=
		/// <summary>�����ɓ������Ȃ�</summary>
		StrictNotEqual,         // !==
		/// <summary>�������Ȃ�</summary>
		Unequal,                // <>
		/// <summary>��r����</summary>
		CompareEqual,           // Ruby: <=>
		/// <summary>�}�b�`����</summary>
		Match,                  // =~
		/// <summary>�}�b�`���Ȃ�</summary>
		NotMatch,               // !~
		/// <summary>�A���[</summary>
		Arrow,                  // PHP: ->
		/// <summary>��d�A���[</summary>
		DoubleArrow,            // PHP, Ruby: =>
		/// <summary>�o�b�N�N�I�[�g</summary>
		BackQuote,              // `
		/// <summary>2 �A���h�b�g</summary>
		DoubleDot,              // Ruby: ..
		/// <summary>3 �A���h�b�g</summary>
		TripleDot,              // Ruby: ...
		/// <summary>�A�b�g�}�[�N</summary>
		At,                     // @
		/// <summary>2 �A���A�b�g�}�[�N</summary>
		DoubleAt,               // @@
		/// <summary>�^�╄</summary>
		Question,               // ?
		/// <summary>2 �A���^�╄</summary>
		DoubleQuestion,         // ??
		/// <summary>�o�b�N�X���b�V��</summary>
		Backslash,              // \
		/// <summary>2 �A���o�b�N�X���b�V��</summary>
		DoubleBackslash,        // \\
		/// <summary>�h���L��</summary>
		Dollar,                 // $
		/// <summary>2 �A���h���L��</summary>
		DoubleDollar,           // $$
		/// <summary>����ɂ���`���ꂽ�v�f</summary>
		LanguageDefined,
	}
}
