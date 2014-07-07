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
	/// <summary>���ꒆ���� <see cref="LanguageContext"/> ��\���܂��B</summary>
	sealed class InvariantContext : LanguageContext
	{
		// friend: ScriptDomainManager
		/// <summary>�w�肳�ꂽ <see cref="ScriptDomainManager"/> ���g�p���āA<see cref="InvariantContext"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="manager">����R���e�L�X�g�����s����� <see cref="ScriptDomainManager"/> ���w�肵�܂��B</param>
		internal InvariantContext(ScriptDomainManager manager) : base(manager) { }

		/// <summary>���ꂪ�R�[�h����͂�����A�|����͒P�ʂ��쐬������ł��邩�ǂ����������l���擾���܂��B</summary>
		public override bool CanCreateSourceCode { get { return false; } }

		/// <summary>�\�[�X�R�[�h���w�肳�ꂽ�R���p�C���R���e�L�X�g���ŉ�͂��܂��B��͂���|��P�ʂ̓R���e�L�X�g�ɂ���ĕێ�����܂��B</summary>
		/// <param name="sourceUnit">��͂���|��P�ʂ��w�肵�܂��B</param>
		/// <param name="options">��͂Ɋւ���I�v�V�������w�肵�܂��B</param>
		/// <param name="errorSink">��͎��̃G���[���������� <see cref="ErrorSink"/> ���w�肵�܂��B</param>
		public override ScriptCode CompileSourceCode(SourceUnit sourceUnit, CompilerOptions options, ErrorSink errorSink) { throw new NotSupportedException(); }
	}
}
