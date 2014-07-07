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
	/// <see cref="Microsoft.Scripting.Hosting.ErrorListener"/> �� <see cref="Microsoft.Scripting.ErrorSink"/> �̋��n�����s���܂��B
	/// <see cref="Microsoft.Scripting.Hosting.ErrorListenerProxySink"/> �Ƃ͋t�̋@�\��񋟂��܂��B
	/// </summary>
	public sealed class ErrorSinkProxyListener : ErrorListener
	{
		ErrorSink _errorSink;

		/// <summary>
		/// �G���[�����������ۂɃG���[���]������� <see cref="Microsoft.Scripting.ErrorSink"/> ���g�p���āA
		/// <see cref="Microsoft.Scripting.Hosting.ErrorSinkProxyListener"/> �N���X�̐V�����C���X�^���X�����������܂��B
		/// </summary>
		/// <param name="errorSink">�G���[�����������ۂɃG���[���]������� <see cref="Microsoft.Scripting.ErrorSink"/> ���w�肵�܂��B</param>
		public ErrorSinkProxyListener(ErrorSink errorSink) { _errorSink = errorSink; }

		/// <summary>�G���[���񍐂��ꂽ�Ƃ��ɌĂяo����܂��B</summary>
		/// <param name="source">�G���[���������� <see cref="ScriptSource"/> �ł��B</param>
		/// <param name="message">�G���[�ɑ΂��郁�b�Z�[�W�ł��B</param>
		/// <param name="span">�G���[�����������ꏊ������ <see cref="SourceSpan"/> �ł��B</param>
		/// <param name="errorCode">�G���[�R�[�h�����������l�ł��B</param>
		/// <param name="severity">�G���[�̐[�����������l�ł��B</param>
		public override void ErrorReported(ScriptSource source, string message, SourceSpan span, int errorCode, Severity severity)
		{
			// source ���v���L�V�I�u�W�F�N�g�ł���Asource.SourceUnit ���g�p�ł��Ȃ����߁Asource.SourceUnit �����݂� AppDomain �Ƀ}�[�V�������O�ł��܂���B
			string code = null;
			string line = null;
			try
			{
				code = source.GetCode();
				line = source.GetCodeLine(span.Start.Line);
			}
			catch (System.IO.IOException) { } // �\�[�X�R�[�h���擾�ł��Ȃ�
			_errorSink.Add(message, source.Path, code, line, span, errorCode, severity);
		}
	}
}
