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
	/// <summary>�W���̃\�[�X�R�[�h�g�[�N������񋟂��܂��B</summary>
	public sealed class TokenCategorizer : MarshalByRefObject
	{
		readonly TokenizerService _tokenizer;

		/// <summary>�w�肳�ꂽ <see cref="Microsoft.Scripting.Runtime.TokenizerService"/> ���g�p���āA<see cref="Microsoft.Scripting.Hosting.TokenCategorizer"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="tokenizer">���b�v���� <see cref="Microsoft.Scripting.Runtime.TokenizerService"/> ���w�肵�܂��B</param>
		internal TokenCategorizer(TokenizerService tokenizer)
		{
			Assert.NotNull(tokenizer);
			_tokenizer = tokenizer;
		}

		/// <summary>��ɂȂ�g�[�N�i�C�U�����������܂��B</summary>
		/// <param name="state">�g�[�N�i�C�U�̏�Ԃ��w�肵�܂��B</param>
		/// <param name="scriptSource">�g�[�N�����̑ΏۂƂȂ� <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="initialLocation">�g�[�N�����̊J�n�ʒu��\�� <see cref="Microsoft.Scripting.SourceLocation"/> ���w�肵�܂��B</param>
		public void Initialize(object state, ScriptSource scriptSource, SourceLocation initialLocation) { _tokenizer.Initialize(state, scriptSource.SourceUnit.GetReader(), scriptSource.SourceUnit, initialLocation); }

		/// <summary>�g�[�N�i�C�U�̌��݂̓�����Ԃ��擾���܂��B</summary>
		public object CurrentState { get { return _tokenizer.CurrentState; } }

		/// <summary>�g�[�N�i�C�U�̌��݂̈ʒu���擾���܂��B</summary>
		public SourceLocation CurrentPosition { get { return _tokenizer.CurrentPosition; } }

		/// <summary>�g�[�N�i�C�U�����̃g�[�N���̂�����܂Ői�߁A���̃J�e�S�����擾���܂��B</summary>
		/// <returns>�X�L���������g�[�N���Ɋ֘A�t�����Ă�����B</returns>
		public TokenInfo ReadToken() { return _tokenizer.ReadToken(); }

		/// <summary>�g�[�N�i�C�U���ĊJ�\���ǂ����������l���擾���܂��B</summary>
		public bool IsRestartable { get { return _tokenizer.IsRestartable; } }

		// TODO: Should be ErrorListener
		/// <summary>�g�[�N�i�C�U�̃G���[���������� <see cref="Microsoft.Scripting.ErrorSink"/> �I�u�W�F�N�g���擾�܂��͐ݒ肵�܂��B</summary>
		public ErrorSink ErrorSink
		{
			get { return _tokenizer.ErrorSink; }
			set { _tokenizer.ErrorSink = value; }
		}

		/// <summary>�g�[�N�i�C�U�����̃g�[�N���̂�����܂Ői�߂܂��B</summary>
		/// <returns>�X�g���[���̖����ɓ��B�����ꍇ�� <c>false</c></returns>
		public bool SkipToken() { return _tokenizer.SkipToken(); }

		/// <summary>�X�g���[���̃u���b�N�𕢂����ׂẴg�[�N�����擾���܂��B</summary>
		/// <remarks>startLocation + length ���g�[�N���̒��Ԃł���ꍇ�ł��A�X�L���i�͂��ׂẴg�[�N����Ԃ��ׂ��ł��B</remarks>
		/// <param name="characterCount">�g�[�N���̓ǂݎ����I������܂łɓǂݎ�镶�������w�肵�܂��B</param>
		/// <returns>�g�[�N��</returns>
		public IEnumerable<TokenInfo> ReadTokens(int characterCount) { return _tokenizer.ReadTokens(characterCount); }

		/// <summary>���݂̈ʒu���炷���Ȃ��Ƃ��w�肳�ꂽ�����������X�L�������܂��B</summary>
		/// <param name="characterCount">�g�[�N���̓ǂݎ����I������܂łɓǂݎ�镶�������w�肵�܂��B</param>
		/// <remarks>���̃��\�b�h�͔C�ӂ̊J�n�ʒu�ɂ������Ԃ𒲂ׂ邽�߂Ɏg�p����܂��B</remarks>
		/// <returns>�X�g���[���̖����ɓ��B�����ꍇ�� <c>false</c></returns>
		public bool SkipTokens(int characterCount) { return _tokenizer.SkipTokens(characterCount); }

		// TODO: Figure out what is the right lifetime
		/// <summary>�Ώۂ̃C���X�^���X�̗L�����ԃ|���V�[�𐧌䂷��A�L�����ԃT�[�r�X �I�u�W�F�N�g���擾���܂��B</summary>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
		public override object InitializeLifetimeService() { return null; }
	}
}
