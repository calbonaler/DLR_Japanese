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

using System.Threading;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting
{
	/// <summary>�񍐂��ꂽ�G���[������������@��񋟂��܂��B����ł͂��ׂẴG���[�͗�O�𔭐������܂��B</summary>
	public class ErrorSink
	{
		/// <summary>����� <see cref="Microsoft.Scripting.ErrorSink"/> ��\���܂��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly ErrorSink/*!*/ Default = new ErrorSink();

		/// <summary>���ׂẴG���[�𖳎����� <see cref="Microsoft.Scripting.ErrorSink"/> ��\���܂��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly ErrorSink/*!*/ Null = new NullErrorSink();

		/// <summary><see cref="Microsoft.Scripting.ErrorSink"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		protected ErrorSink() { }

		/// <summary>���� <see cref="Microsoft.Scripting.ErrorSink"/> �I�u�W�F�N�g�ɃG���[��ǉ����܂��B</summary>
		/// <param name="source">�G���[�����������\�[�X�R�[�h������ <see cref="Microsoft.Scripting.SourceUnit"/> ���w�肵�܂��B</param>
		/// <param name="message">�G���[�ɑ΂��郁�b�Z�[�W���w�肵�܂��B</param>
		/// <param name="span">�G���[�����������\�[�X�R�[�h��̏ꏊ������ <see cref="Microsoft.Scripting.SourceSpan"/> ���w�肵�܂��B</param>
		/// <param name="errorCode">�G���[�R�[�h��\�����l���w�肵�܂��B</param>
		/// <param name="severity">�G���[�̐[�����������l���w�肵�܂��B</param>
		public virtual void Add(SourceUnit source, string/*!*/ message, SourceSpan span, int errorCode, Severity severity)
		{
			if (severity == Severity.FatalError || severity == Severity.Error)
				throw new SyntaxErrorException(message, source, span, errorCode, severity);
		}

		/// <summary>
		/// ���� <see cref="Microsoft.Scripting.ErrorSink"/> �I�u�W�F�N�g�ɃG���[��ǉ����܂��B
		/// ���̃I�[�o�[���[�h�� <see cref="Microsoft.Scripting.SourceUnit"/> �I�u�W�F�N�g���g�p�ł��Ȃ��ꍇ�ɌĂяo����܂��B</summary>
		/// <param name="message">�G���[�ɑ΂��郁�b�Z�[�W���w�肵�܂��B</param>
		/// <param name="path">�G���[�����������\�[�X�R�[�h�̃p�X���w�肵�܂��B</param>
		/// <param name="code">�G���[�����������\�[�X�R�[�h���w�肵�܂��B</param>
		/// <param name="line">�G���[�����������s���w�肵�܂��B</param>
		/// <param name="span">�G���[�����������\�[�X�R�[�h��̏ꏊ������ <see cref="Microsoft.Scripting.SourceSpan"/> ���w�肵�܂��B</param>
		/// <param name="errorCode">�G���[�R�[�h��\�����l���w�肵�܂��B</param>
		/// <param name="severity">�G���[�̐[�����������l���w�肵�܂��B</param>
		public virtual void Add(string message, string path, string code, string line, SourceSpan span, int errorCode, Severity severity)
		{
			if (severity == Severity.FatalError || severity == Severity.Error)
				throw new SyntaxErrorException(message, path, code, line, span, errorCode, severity);
		}

		sealed class NullErrorSink : ErrorSink
		{
			internal NullErrorSink() { }

			public override void Add(SourceUnit source, string/*!*/ message, SourceSpan span, int errorCode, Severity severity) { }

			public override void Add(string message, string path, string code, string line, SourceSpan span, int errorCode, Severity severity) { }
		}
	}

	/// <summary>���������G���[�� <see cref="Microsoft.Scripting.Severity"/> ���ɃJ�E���g���� <see cref="Microsoft.Scripting.ErrorSink"/> ��\���܂��B</summary>
	public class ErrorCounter : ErrorSink
	{
		readonly ErrorSink/*!*/ _sink;

		int _fatalErrorCount;
		int _errorCount;
		int _warningCount;

		/// <summary>���������G���[���� <see cref="Microsoft.Scripting.Severity.FatalError"/> �̌����擾���܂��B</summary>
		public int FatalErrorCount { get { return _fatalErrorCount; } }

		/// <summary>���������G���[���� <see cref="Microsoft.Scripting.Severity.Error"/> �̌����擾���܂��B</summary>
		public int ErrorCount { get { return _errorCount; } }

		/// <summary>���������G���[���� <see cref="Microsoft.Scripting.Severity.Warning"/> �̌����擾���܂��B</summary>
		public int WarningCount { get { return _warningCount; } }

		/// <summary>�x���ȊO�̃G���[�������������ǂ����������l���擾���܂��B</summary>
		public bool AnyError { get { return _errorCount > 0 || _fatalErrorCount > 0; } }

		/// <summary><see cref="Microsoft.Scripting.ErrorCounter"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		public ErrorCounter() : this(ErrorSink.Null) { }

		/// <summary>
		/// ��ɂȂ� <see cref="Microsoft.Scripting.ErrorSink"/> �I�u�W�F�N�g���g�p���āA
		/// <see cref="Microsoft.Scripting.ErrorCounter"/> �N���X�̐V�����C���X�^���X�����������܂��B
		/// </summary>
		/// <param name="sink">���������G���[��n�� <see cref="Microsoft.Scripting.ErrorSink"/> �I�u�W�F�N�g���w�肵�܂��B</param>
		public ErrorCounter(ErrorSink/*!*/ sink)
		{
			ContractUtils.RequiresNotNull(sink, "sink");
			_sink = sink;
		}

		/// <summary><see cref="Microsoft.Scripting.Severity"/> ���ɃG���[�̌����J�E���g���܂��B</summary>
		/// <param name="severity">�G���[�̐[�����������l���w�肵�܂��B</param>
		protected virtual void CountError(Severity severity)
		{
			if (severity == Severity.FatalError)
				Interlocked.Increment(ref _fatalErrorCount);
			else if (severity == Severity.Error)
				Interlocked.Increment(ref _errorCount);
			else if (severity == Severity.Warning)
				Interlocked.Increment(ref _warningCount);
		}

		/// <summary>���̃I�u�W�F�N�g�̂��ׂẴJ�E���^���N���A���܂��B</summary>
		public void ClearCounters() { _warningCount = _errorCount = _fatalErrorCount = 0; }

		/// <summary>���� <see cref="Microsoft.Scripting.ErrorSink"/> �I�u�W�F�N�g�ɃG���[��ǉ����܂��B</summary>
		/// <param name="source">�G���[�����������\�[�X�R�[�h������ <see cref="Microsoft.Scripting.SourceUnit"/> ���w�肵�܂��B</param>
		/// <param name="message">�G���[�ɑ΂��郁�b�Z�[�W���w�肵�܂��B</param>
		/// <param name="span">�G���[�����������\�[�X�R�[�h��̏ꏊ������ <see cref="Microsoft.Scripting.SourceSpan"/> ���w�肵�܂��B</param>
		/// <param name="errorCode">�G���[�R�[�h��\�����l���w�肵�܂��B</param>
		/// <param name="severity">�G���[�̐[�����������l���w�肵�܂��B</param>
		public override void Add(SourceUnit source, string/*!*/ message, SourceSpan span, int errorCode, Severity severity)
		{
			CountError(severity);
			_sink.Add(source, message, span, errorCode, severity);
		}

		/// <summary>
		/// ���� <see cref="Microsoft.Scripting.ErrorSink"/> �I�u�W�F�N�g�ɃG���[��ǉ����܂��B
		/// ���̃I�[�o�[���[�h�� <see cref="Microsoft.Scripting.SourceUnit"/> �I�u�W�F�N�g���g�p�ł��Ȃ��ꍇ�ɌĂяo����܂��B</summary>
		/// <param name="message">�G���[�ɑ΂��郁�b�Z�[�W���w�肵�܂��B</param>
		/// <param name="path">�G���[�����������\�[�X�R�[�h�̃p�X���w�肵�܂��B</param>
		/// <param name="code">�G���[�����������\�[�X�R�[�h���w�肵�܂��B</param>
		/// <param name="line">�G���[�����������s���w�肵�܂��B</param>
		/// <param name="span">�G���[�����������\�[�X�R�[�h��̏ꏊ������ <see cref="Microsoft.Scripting.SourceSpan"/> ���w�肵�܂��B</param>
		/// <param name="errorCode">�G���[�R�[�h��\�����l���w�肵�܂��B</param>
		/// <param name="severity">�G���[�̐[�����������l���w�肵�܂��B</param>
		public override void Add(string message, string path, string code, string line, SourceSpan span, int errorCode, Severity severity)
		{
			CountError(severity);
			_sink.Add(message, path, code, line, span, errorCode, severity);
		}
	}
}
