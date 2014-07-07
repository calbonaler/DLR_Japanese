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
	/// �R���p�C���̎��s�̂��߂ɗ������܂ꂽ�R���e�L�X�g��\���܂��B
	/// ����͂��̃N���X����h�����邱�ƂŁA�ǉ��̃R���e�L�X�g����񋟂��邱�Ƃ��ł��܂��B
	/// </summary>
	public sealed class CompilerContext
	{
		/// <summary><see cref="CompilerContext"/> �Ō��݃R���p�C������Ă��� <see cref="SourceUnit"/> ���擾���܂��B</summary>
		public SourceUnit SourceUnit { get; private set; }

		/// <summary>�p�[�T�[�̃R�[���o�b�N (��: �������̈�v�Ȃ�) ���ʒm�����I�u�W�F�N�g���擾���܂��B</summary>
		public ParserSink ParserSink { get; private set; }

		/// <summary>���݂� <see cref="ErrorSink"/> ���擾���܂��B</summary>
		public ErrorSink Errors { get; private set; }

		/// <summary>�R���p�C���ŗL�̃I�v�V�������擾���܂��B</summary>
		public CompilerOptions Options { get; private set; }

		/// <summary>�|����͒P�ʁA�R���p�C���I�v�V�����A<see cref="ErrorSink"/> ���g�p���āA<see cref="Microsoft.Scripting.Runtime.CompilerContext"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="sourceUnit">�R���p�C�������|����͒P�ʂ��w�肵�܂��B</param>
		/// <param name="options">�R���p�C���̃I�v�V�������w�肵�܂��B</param>
		/// <param name="errorSink">�G���[���ʒm�����I�u�W�F�N�g���w�肵�܂��B</param>
		public CompilerContext(SourceUnit sourceUnit, CompilerOptions options, ErrorSink errorSink) : this(sourceUnit, options, errorSink, ParserSink.Null) { }

		/// <summary>�|����͒P�ʁA�R���p�C���I�v�V�����A<see cref="ErrorSink"/>�A<see cref="ParserSink"/> ���g�p���āA<see cref="Microsoft.Scripting.Runtime.CompilerContext"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="sourceUnit">�R���p�C�������|����͒P�ʂ��w�肵�܂��B</param>
		/// <param name="options">�R���p�C���̃I�v�V�������w�肵�܂��B</param>
		/// <param name="errorSink">�G���[���ʒm�����I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="parserSink">�p�[�T�[�̃R�[���o�b�N���ʒm�����I�u�W�F�N�g���w�肵�܂��B</param>
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
