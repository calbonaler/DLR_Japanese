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

namespace Microsoft.Scripting.Hosting
{
	/// <summary>
	/// <see cref="Microsoft.Scripting.ErrorSink"/> �� <see cref="Microsoft.Scripting.Hosting.ErrorListener"/> �̋��n�����s���܂��B
	/// <see cref="Microsoft.Scripting.ErrorSink"/> �ɑ΂��Č���R���p�C������񍐂��ꂽ�G���[�̓z�X�g���񋟂��� <see cref="Microsoft.Scripting.Hosting.ErrorListener"/> �ɓ]������܂��B
	/// </summary>
	sealed class ErrorListenerProxySink : ErrorSink
	{
		readonly ErrorListener _listener;
		readonly ScriptSource _source;

		/// <summary>
		/// �G���[�̔������ƂȂ� <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> �ƃG���[��]������ <see cref="Microsoft.Scripting.Hosting.ErrorListener"/> ���g�p���āA
		/// <see cref="Microsoft.Scripting.Hosting.ErrorListenerProxySink"/> �N���X�̐V�����C���X�^���X�����������܂��B
		/// </summary>
		/// <param name="source">�G���[�̔������ƂȂ� <see cref="Microsoft.Scripting.Hosting.ScriptSource"/> ���w�肵�܂��B</param>
		/// <param name="listener">���������G���[��]������ <see cref="Microsoft.Scripting.Hosting.ErrorListener"/> ���w�肵�܂��B</param>
		public ErrorListenerProxySink(ScriptSource source, ErrorListener listener)
		{
			_listener = listener;
			_source = source;
		}

		/// <summary>���� <see cref="Microsoft.Scripting.ErrorSink"/> �I�u�W�F�N�g�ɃG���[��ǉ����܂��B</summary>
		/// <param name="sourceUnit">�G���[�����������\�[�X�R�[�h������ <see cref="Microsoft.Scripting.SourceUnit"/> ���w�肵�܂��B</param>
		/// <param name="message">�G���[�ɑ΂��郁�b�Z�[�W���w�肵�܂��B</param>
		/// <param name="span">�G���[�����������\�[�X�R�[�h��̏ꏊ������ <see cref="Microsoft.Scripting.SourceSpan"/> ���w�肵�܂��B</param>
		/// <param name="errorCode">�G���[�R�[�h��\�����l���w�肵�܂��B</param>
		/// <param name="severity">�G���[�̐[�����������l���w�肵�܂��B</param>
		public override void Add(SourceUnit sourceUnit, string message, SourceSpan span, int errorCode, Severity severity)
		{
			if (_listener != null)
				_listener.ErrorReported(sourceUnit != _source.SourceUnit ? new ScriptSource(_source.Engine.Runtime.GetEngine(sourceUnit.LanguageContext), sourceUnit): _source, message, span, errorCode, severity);
			else if (severity == Severity.FatalError || severity == Severity.Error)
				throw new SyntaxErrorException(message, sourceUnit, span, errorCode, severity);
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
			if (_listener != null)
				_listener.ErrorReported(_source, message, span, errorCode, severity);
			else if (severity == Severity.FatalError || severity == Severity.Error)
				throw new SyntaxErrorException(message, path, code, line, span, errorCode, severity);
		}
	}
}
