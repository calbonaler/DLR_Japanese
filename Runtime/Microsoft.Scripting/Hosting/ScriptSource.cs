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
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting;
using System.Security.Permissions;
using System.Text;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting
{
	/// <summary>�X�N���v�g�ɑ΂���|����͒P�ʂ�\���܂��B<see cref="Microsoft.Scripting.SourceUnit"/> �ɑ΂������ 1 �̃z�X�e�B���O API �ł��B</summary>
	[DebuggerDisplay("{Path ?? \"<anonymous>\"}")]
	public sealed class ScriptSource : MarshalByRefObject
	{
		/// <summary>���̖|����͒P�ʂ̊�ƂȂ� <see cref="Microsoft.Scripting.SourceUnit"/> ���擾���܂��B</summary>
		internal SourceUnit SourceUnit { get; private set; }

		/// <summary>���̖|����͒P�ʂ����ʂ���p�X���擾���܂��B</summary>
		public string Path { get { return SourceUnit.Path; } }

		/// <summary>�\�[�X�R�[�h�̎�ނ��擾���܂��B</summary>
		public SourceCodeKind Kind { get { return SourceUnit.Kind; } }

		/// <summary>���̖|����͒P�ʂɊ֘A�t�����Ă��錾��ɑ΂���G���W�����擾���܂��B</summary>
		public ScriptEngine Engine { get; private set; }

		/// <summary>
		/// �w�肳�ꂽ�G���W������ъ�ƂȂ� <see cref="Microsoft.Scripting.SourceUnit"/>
		/// ���g�p���āA<see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �N���X�̐V�����C���X�^���X���擾���܂��B
		/// </summary>
		/// <param name="engine">���̃C���X�^���X�Ɋ֘A�t���錾��ɑ΂���G���W�����w�肵�܂��B</param>
		/// <param name="sourceUnit">���̃C���X�^���X�̊�ƂȂ� <see cref="Microsoft.Scripting.SourceUnit"/> ���w�肵�܂��B</param>
		internal ScriptSource(ScriptEngine engine, SourceUnit sourceUnit)
		{
			Assert.NotNull(engine, sourceUnit);
			SourceUnit = sourceUnit;
			Engine = engine;
		}

		#region Compilation and Execution

		/// <summary>
		/// ���� <see cref="ScriptSource"/> ������̃X�R�[�v�܂��͑��̃X�R�[�v�ōăR���p�C���̕K�v�Ȃ��ɌJ��Ԃ����s�\��
		/// <see cref="CompiledCode"/> �I�u�W�F�N�g�ɃR���p�C�����܂��B
		/// </summary>
		/// <returns>�R���p�C�����ꂽ�R�[�h��\�� <see cref="CompiledCode"/>�B�G���[�ɂ���ăp�[�T�[���R�[�h���R���p�C���ł��Ȃ��ꍇ�� <c>null</c> �ɂȂ�܂��B</returns>
		/// <exception cref="SyntaxErrorException">�R�[�h���R���p�C���ł��܂���ł����B</exception>
		public CompiledCode Compile() { return Compile(null, null); }

		/// <summary>
		/// ���� <see cref="ScriptSource"/> ������̃X�R�[�v�܂��͑��̃X�R�[�v�ōăR���p�C���̕K�v�Ȃ��ɌJ��Ԃ����s�\��
		/// <see cref="CompiledCode"/> �I�u�W�F�N�g�ɃR���p�C�����܂��B
		/// </summary>
		/// <param name="errorListener">�G���[��񍐂��� <see cref="ErrorListener"/> ���w�肵�܂��B</param>
		/// <returns>�R���p�C�����ꂽ�R�[�h��\�� <see cref="CompiledCode"/>�B�G���[�ɂ���ăp�[�T�[���R�[�h���R���p�C���ł��Ȃ��ꍇ�� <c>null</c> �ɂȂ�܂��B</returns>
		/// <exception cref="SyntaxErrorException">�R�[�h���R���p�C���ł��܂���ł����B</exception>>
		public CompiledCode Compile(ErrorListener errorListener) { return Compile(null, errorListener); }

		/// <summary>
		/// ���� <see cref="ScriptSource"/> ������̃X�R�[�v�܂��͑��̃X�R�[�v�ōăR���p�C���̕K�v�Ȃ��ɌJ��Ԃ����s�\��
		/// <see cref="CompiledCode"/> �I�u�W�F�N�g�ɃR���p�C�����܂��B
		/// </summary>
		/// <param name="compilerOptions">�R���p�C�����Ɏg�p����I�v�V�������w�肵�܂��B</param>
		/// <returns>�R���p�C�����ꂽ�R�[�h��\�� <see cref="CompiledCode"/>�B�G���[�ɂ���ăp�[�T�[���R�[�h���R���p�C���ł��Ȃ��ꍇ�� <c>null</c> �ɂȂ�܂��B</returns>
		/// <exception cref="SyntaxErrorException">�R�[�h���R���p�C���ł��܂���ł����B</exception>
		public CompiledCode Compile(CompilerOptions compilerOptions) { return Compile(compilerOptions, null); }

		/// <summary>
		/// ���� <see cref="ScriptSource"/> ������̃X�R�[�v�܂��͑��̃X�R�[�v�ōăR���p�C���̕K�v�Ȃ��ɌJ��Ԃ����s�\��
		/// <see cref="CompiledCode"/> �I�u�W�F�N�g�ɃR���p�C�����܂��B
		/// </summary>
		/// <param name="compilerOptions">�R���p�C�����Ɏg�p����I�v�V�������w�肵�܂��B</param>
		/// <param name="errorListener">�G���[��񍐂��� <see cref="ErrorListener"/> ���w�肵�܂��B</param>
		/// <returns>�R���p�C�����ꂽ�R�[�h��\�� <see cref="CompiledCode"/>�B�G���[�ɂ���ăp�[�T�[���R�[�h���R���p�C���ł��Ȃ��ꍇ�� <c>null</c> �ɂȂ�܂��B</returns>
		/// <exception cref="SyntaxErrorException">�R�[�h���R���p�C���ł��܂���ł����B</exception>
		public CompiledCode Compile(CompilerOptions compilerOptions, ErrorListener errorListener)
		{
			var errorSink = new ErrorListenerProxySink(this, errorListener);
			var code = compilerOptions != null ? SourceUnit.Compile(compilerOptions, errorSink) : SourceUnit.Compile(errorSink);
			return code != null ? new CompiledCode(Engine, code) : null;
		}

		/// <summary>�R�[�h���w�肵���X�R�[�v�Ŏ��s���A���ʂ�Ԃ��܂��B</summary>
		/// <param name="scope">�R�[�h�����s����X�R�[�v���w�肵�܂��B</param>
		/// <exception cref="SyntaxErrorException">�R�[�h���R���p�C���ł��܂���B</exception>
		public dynamic Execute(ScriptScope scope)
		{
			ContractUtils.RequiresNotNull(scope, "scope");
			return SourceUnit.Execute(scope.Scope);
		}

		/// <summary>�R�[�h�����s���A���ʂ�Ԃ��܂��B���s�͂ǂ̃X�R�[�v�ɂ��֘A�t�����܂���B</summary>
		/// <remarks>
		/// �z�X�g���X�R�[�v��K�v�Ƃ��Ȃ��̂ŁA�����ł͍쐬���܂���B
		/// ����̓R�[�h�� DLR �X�R�[�v�Ɋ֘A�t�����Ă��Ȃ��Ƃ��Ĉ����A�O���[�o�������̃Z�}���e�B�N�X��K�X�ύX����\��������܂��B
		/// </remarks>
		public dynamic Execute() { return SourceUnit.Execute(); }

		/// <summary>�R�[�h���w�肳�ꂽ�X�R�[�v�Ŏ��s���A���ʂ��w�肳�ꂽ�^�ɕϊ����܂��B�ϊ��͌���ɂ���Ē�`����܂��B</summary>
		/// <param name="scope">�R�[�h�����s����X�R�[�v���w�肵�܂��B</param>
		public T Execute<T>(ScriptScope scope) { return Engine.Operations.ConvertTo<T>((object)Execute(scope)); }

		/// <summary>�R�[�h����̃X�R�[�v�Ŏ��s���A���ʂ��w�肳�ꂽ�^�ɕϊ����܂��B�ϊ��͌���ɂ���Ē�`����܂��B</summary>
		public T Execute<T>() { return Engine.Operations.ConvertTo<T>((object)Execute()); }

		/// <summary>�R�[�h���w�肳�ꂽ�X�R�[�v�Ŏ��s���A���ʂ� <see cref="ObjectHandle"/> �Ń��b�v���܂��B</summary>
		/// <param name="scope">�R�[�h�����s����X�R�[�v���w�肵�܂��B</param>
		public ObjectHandle ExecuteAndWrap(ScriptScope scope) { return new ObjectHandle((object)Execute(scope)); }

		/// <summary>�R�[�h����̃X�R�[�v�Ŏ��s���A���ʂ� <see cref="ObjectHandle"/> �Ń��b�v���܂��B</summary>
		public ObjectHandle ExecuteAndWrap() { return new ObjectHandle((object)Execute()); }

		/// <summary>�R�[�h�� OS �̃R�}���h�V�F������J�n���ꂽ�v���O�����ł���悤�Ɏ��s���A�R�[�h���s�̐����܂��̓G���[��Ԃ������v���Z�X�I���R�[�h��Ԃ��܂��B</summary>
		/// <exception cref="SyntaxErrorException">�R�[�h���R���p�C���ł��܂���B</exception>
		/// <remarks>
		/// ���m�ȓ���͌���Ɉˑ����܂��B�I���R�[�h��`�B���� "exit" ��O�����錾������݂��A���̏ꍇ��O�͕ߑ�����I���R�[�h���Ԃ���܂��B
		/// ����̓���ł͌�����L�̕ϊ����g�p���āA�����ɕϊ����ꂽ�v���O�����̎��s���ʂ�Ԃ��܂��B
		/// </remarks>
		public int ExecuteProgram() { return SourceUnit.LanguageContext.ExecuteProgram(SourceUnit); }

		#endregion

		/// <summary>�\�[�X�R�[�h����͂��邱�Ƃɂ��A�\�[�X�R�[�h�̏�Ԃ��擾���܂��B</summary>
		public ScriptCodeParseResult FetchCodeProperties() { return SourceUnit.FetchCodeProperties(); }

		/// <summary>�\�[�X�R�[�h����͂��邱�Ƃɂ��A�\�[�X�R�[�h�̏�Ԃ��擾���܂��B</summary>
		/// <param name="options">��͂Ɏg�p���� <see cref="Microsoft.Scripting.CompilerOptions"/> ���w�肵�܂��B</param>
		public ScriptCodeParseResult FetchCodeProperties(CompilerOptions options) { return SourceUnit.FetchCodeProperties(options); }

		/// <summary>�\�[�X�R�[�h��ǂݎ��V���� <see cref="System.IO.TextReader"/> ��Ԃ��܂��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public SourceCodeReader GetReader() { return SourceUnit.GetReader(); }

		/// <summary>�R���e���c�̃G���R�[�f�B���O�𔻒肵�܂��B</summary>
		/// <returns>
		/// �R���e���c�� Unicode �e�L�X�g�Ƀf�R�[�h���邽�߂ɁA�X�N���v�g�̖|����͂̃��[�_�[�ɂ���Ďg�p�����G���R�[�f�B���O�B
		/// �R���e���c�����Ƀe�L�X�g�ŁA�f�R�[�h���s���Ă��Ȃ��ꍇ�� <c>null</c>�B
		/// </returns>
		/// <remarks>
		/// �X�N���v�g�̖|����͂��쐬���ꂽ�Ƃ��A�w�肳�ꂽ����̃G���R�[�f�B���O�̓R���e���c�v���A���u�� (Unicode BOM �܂��͌�����L�̃G���R�[�f�B���O�v���A���u��)
		/// �Ō��������G���R�[�f�B���O�ɏ㏑�������\��������܂��B���̏ꍇ�A�v���A���u���G���R�[�f�B���O���Ԃ���܂��B
		/// ����ȊO�̏ꍇ�͊���̃G���R�[�f�B���O���Ԃ���܂��B
		/// </remarks>
		/// <exception cref="IOException">I/O �G���[���������܂����B</exception>
		public Encoding DetectEncoding()
		{
			using (var reader = SourceUnit.GetReader())
				return reader.Encoding;
		}

		/// <summary>�|����͒P�ʂ���w�肳�ꂽ�͈͂̍s��ǂݎ��܂��B</summary>
		/// <param name="start">�擾����s�� 1 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		/// <param name="count">�擾����s�����w�肵�܂��B</param>
		/// <returns>�ǂݎ��ꂽ�e�s���i�[���� <see cref="System.String"/> �^�̔z��B</returns>
		/// <exception cref="IOException">I/O �G���[���������܂����B</exception>
		/// <remarks>�ǂ̕����񂪉��s�L���ƔF������邩�͌���ɂ��܂��B���ꂪ�w�肳��Ă��Ȃ��ꍇ�A"\r", "\n", "\r\n" �����s�L���ƔF������܂��B</remarks>
		public string[] GetCodeLines(int start, int count) { return SourceUnit.GetCodeLines(start, count); }

		/// <summary>�|����͒P�ʂ���w�肳�ꂽ�s��ǂݎ��܂��B</summary>
		/// <param name="line">�擾����s�� 1 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�s�̓��e�B���s�����͊܂܂�܂���B</returns>
		/// <exception cref="IOException">I/O �G���[���������܂����B</exception>
		/// <remarks>�ǂ̕����񂪉��s�L���ƔF������邩�͌���ɂ��܂��B���ꂪ�w�肳��Ă��Ȃ��ꍇ�A"\r", "\n", "\r\n" �����s�L���ƔF������܂��B</remarks>
		public string GetCodeLine(int line) { return SourceUnit.GetCodeLine(line); }

		/// <summary>�X�N���v�g�̖|����͂̓��e���擾���܂��B</summary>
		/// <returns>�R���e���c�S�́B</returns>
		/// <exception cref="IOException">I/O �G���[���������܂����B</exception>
		/// <remarks>
		/// ���ʂɂ͌���ŗL�̃v���A���u�� (���Ƃ��΁A "#coding:UTF-8" �� Ruby �ł̓G���R�[�f�B���O�v���A���u���Ƃ��ĔF������܂��B) ���܂܂�܂����A
		/// �R���e���c�G���R�[�f�B���O�Ŏw�肳�ꂽ�v���A���u�� (��: BOM) �͊܂܂�܂���B
		/// �|����͒P�ʂ̓��e�S�̂͒P��̃G���R�[�f�B���O�ɂ���ăG���R�[�h����܂��B(�����A�o�C�i���X�g���[������ǂݎ��ꂽ�ꍇ)
		/// </remarks>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public string GetCode() { return SourceUnit.GetCode(); }

		// TODO: can this be removed? no one uses it
		#region line number mapping

		/// <summary>�w�肳�ꂽ <see cref="SourceSpan"/> ������ۂ̃\�[�X�R�[�h�͈͂�\�� <see cref="SourceSpan"/> ��Ԃ��܂��B</summary>
		/// <param name="span"><see cref="SourceSpan"/> ���w�肵�܂��B</param>
		public SourceSpan MapSpan(SourceSpan span) { return new SourceSpan(MapLocation(span.Start), MapLocation(span.End)); }

		/// <summary>�w�肳�ꂽ <see cref="SourceLocation"/> ������ۂ̃\�[�X�R�[�h��̈ʒu��\�� <see cref="SourceLocation"/> ��Ԃ��܂��B</summary>
		/// <param name="location"><see cref="SourceLocation"/> ���w�肵�܂��B</param>
		public SourceLocation MapLocation(SourceLocation location) { return SourceUnit.MapLocation(location); }

		#endregion

		// TODO: Figure out what is the right lifetime
		/// <summary>�Ώۂ̃C���X�^���X�̗L�����ԃ|���V�[�𐧌䂷��A�L�����ԃT�[�r�X �I�u�W�F�N�g���擾���܂��B</summary>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
		public override object InitializeLifetimeService() { return null; }
	}
}
