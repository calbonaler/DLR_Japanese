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
	/// <summary>����ŗL�̃T�[�r�X�Ƃ��Ẵg�[�N�i�C�U�[��\���܂��B</summary>
	public abstract class TokenizerService
	{
		/// <summary><see cref="Microsoft.Scripting.Runtime.TokenizerService"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		protected TokenizerService() { }

		/// <summary>�w�肳�ꂽ��ԁA<see cref="TextReader"/>�A�|��P�ʁA�����ʒu���g�p���āA���̃I�u�W�F�N�g�����������܂��B</summary>
		/// <param name="state">�g�[�N�i�C�U�̏�Ԃ�\�� <see cref="System.Object"/> �l���w�肵�܂��B</param>
		/// <param name="sourceReader">�\�[�X�R�[�h��ǂݎ�� <see cref="TextReader"/> ���w�肵�܂��B</param>
		/// <param name="sourceUnit">�\�[�X�R�[�h���i�[����|��P�ʂ��w�肵�܂��B</param>
		/// <param name="initialLocation">�g�[�N�i�C�U�̏����ʒu���w�肵�܂��B</param>
		public abstract void Initialize(object state, TextReader sourceReader, SourceUnit sourceUnit, SourceLocation initialLocation);

		/// <summary>�g�[�N�i�C�U�̌��݂̓�����Ԃ��擾���܂��B</summary>
		public abstract object CurrentState { get; }

		/// <summary>�g�[�N�i�C�U�̌��݂̈ʒu���擾���܂��B</summary>
		public abstract SourceLocation CurrentPosition { get; }

		/// <summary>�g�[�N�i�C�U�����̃g�[�N���̂�����܂Ői�߁A���̃J�e�S�����擾���܂��B</summary>
		/// <returns>�X�L���������g�[�N���Ɋ֘A�t�����Ă�����B</returns>
		public abstract TokenInfo ReadToken();

		/// <summary>�g�[�N�i�C�U���ĊJ�\���ǂ����������l���擾���܂��B</summary>
		public abstract bool IsRestartable { get; }

		/// <summary>�g�[�N�i�C�U�̃G���[���������� <see cref="Microsoft.Scripting.ErrorSink"/> �I�u�W�F�N�g���擾�܂��͐ݒ肵�܂��B</summary>
		public abstract ErrorSink ErrorSink { get; set; }

		/// <summary>�g�[�N�i�C�U�����̃g�[�N���̂�����܂Ői�߂܂��B</summary>
		/// <returns>�X�g���[���̖����ɓ��B�����ꍇ�� <c>false</c></returns>
		public virtual bool SkipToken() { return ReadToken().Category != TokenCategory.EndOfStream; }

		/// <summary>�X�g���[���̃u���b�N�𕢂����ׂẴg�[�N�����擾���܂��B</summary>
		/// <remarks>startLocation + length ���g�[�N���̒��Ԃł���ꍇ�ł��A�X�L���i�͂��ׂẴg�[�N����Ԃ��ׂ��ł��B</remarks>
		/// <param name="characterCount">�g�[�N���̓ǂݎ����I������܂łɓǂݎ�镶�������w�肵�܂��B</param>
		/// <returns>�g�[�N��</returns>
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

		/// <summary>���݂̈ʒu���炷���Ȃ��Ƃ��w�肳�ꂽ�����������X�L�������܂��B</summary>
		/// <param name="countOfChars">�g�[�N���̓ǂݎ����I������܂łɓǂݎ�镶�������w�肵�܂��B</param>
		/// <remarks>���̃��\�b�h�͔C�ӂ̊J�n�ʒu�ɂ������Ԃ𒲂ׂ邽�߂Ɏg�p����܂��B</remarks>
		/// <returns>�X�g���[���̖����ɓ��B�����ꍇ�� <c>false</c></returns>
		public bool SkipTokens(int countOfChars)
		{
			bool eos = false;
			int start_index = CurrentPosition.Index;
			while (CurrentPosition.Index - start_index < countOfChars && (eos = SkipToken())) { }
			return eos;
		}
	}
}
