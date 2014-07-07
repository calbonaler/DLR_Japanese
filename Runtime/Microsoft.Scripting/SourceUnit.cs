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
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting
{
	/// <summary>�|����͒P�ʂ�\���܂��B</summary>
	[DebuggerDisplay("{Path ?? \"<anonymous>\"}")]
	public sealed class SourceUnit
	{
		TextContentProvider _contentProvider;

		// SourceUnit is serializable => updated parse result is transmitted back to the host unless the unit is passed by-ref
		KeyValuePair<int, int>[] _lineMap;

		/// <summary>�|����͒P�ʂ����ʂ���z�X�g�ɂ��ݒ肳���l���擾���܂��B</summary>
		public string Path { get; private set; }

		/// <summary>���̖|����͒P�ʂ����ʂ���l�����݂��邩�ǂ����������l���擾���܂��B</summary>
		public bool HasPath { get { return Path != null; } }

		/// <summary>���̖|����͒P�ʂɂ���ĕێ������\�[�X�R�[�h�̎�ނ������l���擾���܂��B</summary>
		public SourceCodeKind Kind { get; private set; }

		// Path is valid to be null. In that case we cannot create a valid SymbolDocumentInfo.
		/// <summary>���̖|����͒P�ʂ�������� <see cref="System.Linq.Expressions.SymbolDocumentInfo"/> ���擾���܂��B</summary>
		public SymbolDocumentInfo Document { get { return Path == null ? null : Expression.SymbolDocument(Path, LanguageContext.LanguageGuid, LanguageContext.VendorGuid); } }

		/// <summary>���̖|����͒P�ʂ̌����\�� <see cref="LanguageContext"/> ���擾���܂��B</summary>
		public LanguageContext LanguageContext { get; private set; }

		/// <summary>�\�[�X�R�[�h����͂��邱�Ƃɂ��A�\�[�X�R�[�h�̏�Ԃ��擾���܂��B</summary>
		public ScriptCodeParseResult FetchCodeProperties() { return FetchCodeProperties(LanguageContext.GetCompilerOptions()); }

		/// <summary>�\�[�X�R�[�h����͂��邱�Ƃɂ��A�\�[�X�R�[�h�̏�Ԃ��擾���܂��B</summary>
		/// <param name="options">��͂Ɏg�p���� <see cref="Microsoft.Scripting.CompilerOptions"/> ���w�肵�܂��B</param>
		public ScriptCodeParseResult FetchCodeProperties(CompilerOptions options)
		{
			ContractUtils.RequiresNotNull(options, "options");
			Compile(options, ErrorSink.Null);
			return CodeProperties ?? ScriptCodeParseResult.Complete;
		}

		/// <summary>�\�[�X�R�[�h�̏�Ԃ������l���擾�܂��͐ݒ肵�܂��B</summary>
		public ScriptCodeParseResult? CodeProperties { get; set; }

		/// <summary>����A�\�[�X�R�[�h�A�p�X�A�\�[�X�R�[�h�̎�ނ��g�p���āA<see cref="Microsoft.Scripting.SourceUnit"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="context">���̖|����͒P�ʂ��ێ�����\�[�X�R�[�h�̌����\�� <see cref="LanguageContext"/> ���w�肵�܂��B</param>
		/// <param name="contentProvider">���̖|����͒P�ʂ��ێ�����\�[�X�R�[�h��񋟂��� <see cref="TextContentProvider"/> ���w�肵�܂��B</param>
		/// <param name="path">���̖|����͒P�ʂ����ʂ��镶������w�肵�܂��B</param>
		/// <param name="kind">���̖|����͒P�ʂ��ێ�����\�[�X�R�[�h�̎�ނ��w�肵�܂��B</param>
		public SourceUnit(LanguageContext context, TextContentProvider contentProvider, string path, SourceCodeKind kind)
		{
			ContractUtils.RequiresNotNull(context, "context");
			ContractUtils.RequiresNotNull(contentProvider, "contentProvider");
			ContractUtils.Requires(context.CanCreateSourceCode, "context");
			LanguageContext = context;
			_contentProvider = contentProvider;
			Kind = kind;
			Path = path;
		}

		/// <summary>�\�[�X�R�[�h��ǂݎ��V���� <see cref="System.IO.TextReader"/> ��Ԃ��܂��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public SourceCodeReader GetReader() { return _contentProvider.GetReader(); }

		/// <summary>�|����͒P�ʂ���w�肳�ꂽ�͈͂̍s��ǂݎ��܂��B</summary>
		/// <param name="start">�擾����s�� 1 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		/// <param name="count">�擾����s�����w�肵�܂��B</param>
		/// <returns>�ǂݎ��ꂽ�e�s���i�[���� <see cref="System.String"/> �^�̔z��B</returns>
		/// <exception cref="System.IO.IOException">I/O �G���[���������܂����B</exception>
		public string[] GetCodeLines(int start, int count)
		{
			ContractUtils.Requires(start > 0, "start");
			ContractUtils.Requires(count > 0, "count");
			List<string> result = new List<string>(count);
			using (var reader = GetReader())
			{
				string line;
				for (reader.SeekLine(start); count > 0 && (line = reader.ReadLine()) != null; count--)
					result.Add(line);
			}
			return result.ToArray();
		}

		/// <summary>�|����͒P�ʂ���w�肳�ꂽ�s��ǂݎ��܂��B</summary>
		/// <param name="line">�擾����s�� 1 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�s�̓��e�B���s�����͊܂܂�܂���B</returns>
		/// <exception cref="System.IO.IOException">I/O �G���[���������܂����B</exception>
		public string GetCodeLine(int line) { return GetCodeLines(line, 1).FirstOrDefault(); }

		/// <summary>�X�N���v�g�̖|����͂̓��e���擾���܂��B</summary>
		/// <returns>�R���e���c�S�́B</returns>
		/// <exception cref="System.IO.IOException">I/O �G���[���������܂����B</exception>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public string GetCode()
		{
			using (var reader = GetReader())
				return reader.ReadToEnd();
		}

		#region Line/File mapping

		/// <summary>�w�肳�ꂽ <see cref="SourceLocation"/> ������ۂ̃\�[�X�R�[�h��̈ʒu��\�� <see cref="SourceLocation"/> ��Ԃ��܂��B</summary>
		/// <param name="loc"><see cref="SourceLocation"/> ���w�肵�܂��B</param>
		public SourceLocation MapLocation(SourceLocation loc) { return new SourceLocation(loc.Index, MapLine(loc.Line), loc.Column); }

		int MapLine(int line)
		{
			if (_lineMap != null)
			{
				int match = BinarySearch(_lineMap, line);
				line += _lineMap[match].Value - _lineMap[match].Key;
				if (line < 1)
					line = 1; // this is the minimum value
			}
			return line;
		}

		static int BinarySearch(KeyValuePair<int, int>[] array, int line)
		{
			int match = Array.BinarySearch(array, new KeyValuePair<int, int>(line, 0), Comparer<KeyValuePair<int, int>>.Create((x, y) => x.Key - y.Key));
			if (match < 0)
			{
				// If we couldn't find an exact match for this line number, get the nearest matching line number less than this one
				match = ~match - 1;
				// If our index = -1, it means that this line is before any line numbers that we know about. If that's the case, use the first entry in the list
				if (match == -1)
					match = 0;
			}
			return match;
		}

		#endregion

		#region Parsing, Compilation, Execution

		/// <summary>���̖|����͒P�ʂł̃R���p�C�����f�o�b�O�V���{�����o�͉\���ǂ����������l���擾���܂��B</summary>
		public bool EmitDebugSymbols { get { return HasPath && LanguageContext.DomainManager.Configuration.DebugMode; } }

		/// <summary>���̖|����͒P�ʂ�<see cref="ScriptCode"/> �I�u�W�F�N�g�ɃR���p�C�����܂��B</summary>
		/// <exception cref="SyntaxErrorException">�R�[�h���R���p�C���ł��܂���ł����B</exception>
		/// <returns>�R���p�C�����ꂽ�R�[�h��\�� <see cref="ScriptCode"/>�B�G���[�ɂ���ăp�[�T�[���R�[�h���R���p�C���ł��Ȃ��ꍇ�� <c>null</c> �ɂȂ�܂��B</returns>
		public ScriptCode Compile() { return Compile(ErrorSink.Default); }

		/// <summary>���̖|����͒P�ʂ�<see cref="ScriptCode"/> �I�u�W�F�N�g�ɃR���p�C�����܂��B</summary>
		/// <param name="errorSink">�G���[���񍐂���� <see cref="ErrorSink"/> ���w�肵�܂��B</param>
		/// <exception cref="SyntaxErrorException">�R�[�h���R���p�C���ł��܂���ł����B</exception>
		/// <returns>�R���p�C�����ꂽ�R�[�h��\�� <see cref="ScriptCode"/>�B�G���[�ɂ���ăp�[�T�[���R�[�h���R���p�C���ł��Ȃ��ꍇ�� <c>null</c> �ɂȂ�܂��B</returns>
		public ScriptCode Compile(ErrorSink errorSink) { return Compile(LanguageContext.GetCompilerOptions(), errorSink); }

		/// <summary>���̖|����͒P�ʂ�<see cref="ScriptCode"/> �I�u�W�F�N�g�ɃR���p�C�����܂��B</summary>
		/// <param name="options">�R���p�C�����Ɏg�p����I�v�V�������w�肵�܂��B</param>
		/// <param name="errorSink">�G���[���񍐂���� <see cref="ErrorSink"/> ���w�肵�܂��B</param>
		/// <exception cref="SyntaxErrorException">�R�[�h���R���p�C���ł��܂���ł����B</exception>
		/// <returns>�R���p�C�����ꂽ�R�[�h��\�� <see cref="ScriptCode"/>�B�G���[�ɂ���ăp�[�T�[���R�[�h���R���p�C���ł��Ȃ��ꍇ�� <c>null</c> �ɂȂ�܂��B</returns>
		public ScriptCode Compile(CompilerOptions options, ErrorSink errorSink)
		{
			ContractUtils.RequiresNotNull(errorSink, "errorSink");
			ContractUtils.RequiresNotNull(options, "options");
			return LanguageContext.CompileSourceCode(this, options, errorSink);
		}

		/// <summary>�R�[�h���w�肵���X�R�[�v�Ŏ��s���A���ʂ�Ԃ��܂��B</summary>
		/// <param name="scope">�R�[�h�����s����X�R�[�v���w�肵�܂��B</param>
		/// <exception cref="SyntaxErrorException">�R�[�h���R���p�C���ł��܂���B</exception>
		public object Execute(Scope scope) { return Execute(scope, ErrorSink.Default); }

		/// <summary>�R�[�h���w�肵���X�R�[�v�Ŏ��s���A���ʂ�Ԃ��܂��B�G���[�͎w�肳�ꂽ <see cref="ErrorSink"/> �ɕ񍐂���܂��B</summary>
		/// <param name="scope">�R�[�h�����s����X�R�[�v���w�肵�܂��B</param>
		/// <param name="errorSink">�G���[���񍐂���� <see cref="ErrorSink"/> ���w�肵�܂��B</param>
		/// <exception cref="SyntaxErrorException">�R�[�h���R���p�C���ł��܂���B</exception>
		public object Execute(Scope scope, ErrorSink errorSink)
		{
			ContractUtils.RequiresNotNull(scope, "scope");
			ScriptCode compiledCode = Compile(LanguageContext.GetCompilerOptions(scope), errorSink);
			if (compiledCode == null)
				throw new SyntaxErrorException();
			return compiledCode.Run(scope);
		}

		/// <summary>�R�[�h������ɂ���č쐬���ꂽ�V�����X�R�[�v�Ŏ��s���A���ʂ�Ԃ��܂��B</summary>
		/// <exception cref="SyntaxErrorException">�R�[�h���R���p�C���ł��܂���B</exception>
		public object Execute() { return Compile().Run(); }

		/// <summary>�R�[�h������ɂ���č쐬���ꂽ�V�����X�R�[�v�Ŏ��s���A���ʂ�Ԃ��܂��B�G���[�͎w�肳�ꂽ <see cref="ErrorSink"/> �ɕ񍐂���܂��B</summary>
		/// <param name="errorSink">�G���[���񍐂���� <see cref="ErrorSink"/> ���w�肵�܂��B</param>
		/// <exception cref="SyntaxErrorException">�R�[�h���R���p�C���ł��܂���B</exception>
		public object Execute(ErrorSink errorSink) { return Compile(errorSink).Run(); }

		/// <summary>�R�[�h������ɂ���č쐬���ꂽ�V�����X�R�[�v�Ŏ��s���A���ʂ�Ԃ��܂��B�G���[�͎w�肳�ꂽ <see cref="ErrorSink"/> �ɕ񍐂���܂��B</summary>
		/// <param name="options">�R���p�C�����Ɏg�p����I�v�V�������w�肵�܂��B</param>
		/// <param name="errorSink">�G���[���񍐂���� <see cref="ErrorSink"/> ���w�肵�܂��B</param>
		/// <exception cref="SyntaxErrorException">�R�[�h���R���p�C���ł��܂���B</exception>
		public object Execute(CompilerOptions options, ErrorSink errorSink) { return Compile(options, errorSink).Run(); }

		/// <summary>�R�[�h�� OS �̃R�}���h�V�F������J�n���ꂽ�v���O�����ł���悤�Ɏ��s���A�R�[�h���s�̐����܂��̓G���[��Ԃ������v���Z�X�I���R�[�h��Ԃ��܂��B</summary>
		/// <exception cref="SyntaxErrorException">�R�[�h���R���p�C���ł��܂���B</exception>
		public int ExecuteProgram() { return LanguageContext.ExecuteProgram(this); }

		#endregion

		/// <summary>���̖|����͒P�ʂɍs�̃}�b�s���O��ݒ肵�܂��B</summary>
		/// <param name="lineMap">�s�̃}�b�s���O��\���z����w�肵�܂��B</param>
		public void SetLineMapping(KeyValuePair<int, int>[] lineMap) { _lineMap = lineMap == null || lineMap.Length == 0 ? null : lineMap; }
	}
}
