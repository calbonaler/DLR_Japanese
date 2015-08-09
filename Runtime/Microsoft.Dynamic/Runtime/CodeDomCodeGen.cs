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

using System.CodeDom;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>Code DOM ����̃\�[�X�R�[�h�������s���܂��B</summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")] // TODO: fix
	public abstract class CodeDomCodeGen
	{
		/// <summary><see cref="CodeObject.UserData"/> �ŃC���X�^���X�̌��̃\�[�X�t�@�C�����ł̈ʒu��ǐՂ��邽�߂Ɏg�p�����L�[�������܂��B</summary>
		protected static readonly object SourceSpanKey = typeof(SourceSpan);

		/// <summary>�������ꂽ�R�[�h���i�[���� <see cref="PositionTrackingWriter"/> ���擾���܂��B</summary>
		protected PositionTrackingWriter Writer { get; private set; }

		/// <summary>�w�肳�ꂽ������\�� <see cref="CodeExpressionStatement"/> �ɑ΂���R�[�h�𐶐����܂��B</summary>
		/// <param name="statement">�R�[�h�𐶐����� <see cref="CodeExpressionStatement"/> ���w�肵�܂��B</param>
		protected abstract void WriteExpressionStatement(CodeExpressionStatement statement);

		/// <summary>�w�肳�ꂽ���\�b�h�錾��\�� <see cref="CodeMemberMethod"/> �ɑ΂���R�[�h�𐶐����܂��B</summary>
		/// <param name="func">�R�[�h�𐶐����� <see cref="CodeMemberMethod"/> ���w�肵�܂��B</param>
		protected abstract void WriteFunctionDefinition(CodeMemberMethod func);

		/// <summary>�w�肳�ꂽ����������e�����`���ɕϊ����܂��B</summary>
		/// <param name="val">���e�����`���ɕϊ����镶����l���w�肵�܂��B</param>
		/// <returns>���e�����`���ɕϊ����ꂽ������B</returns>
		protected abstract string QuoteString(string val);

		/// <summary>�w�肳�ꂽ���\�b�h�錾��\�� <see cref="CodeMemberMethod"/> �ɑ΂���R�[�h�𐶐����܂��B</summary>
		/// <param name="codeDom">�R�[�h�𐶐����郁�\�b�h�錾��\�� <see cref="CodeMemberMethod"/> ���w�肵�܂��B</param>
		/// <param name="context"><see cref="SourceUnit"/> ���쐬���� <see cref="LanguageContext"/> ���w�肵�܂��B</param>
		/// <param name="path">���������\�[�X�R�[�h�̃p�X���w�肵�܂��B</param>
		/// <param name="kind">���������\�[�X�R�[�h�̎�ނ��w�肵�܂��B</param>
		/// <returns>�������ꂽ�\�[�X�R�[�h�ɑ΂���|����͒P�ʂ�\�� <see cref="SourceUnit"/>�B</returns>
		public SourceUnit GenerateCode(CodeMemberMethod codeDom, LanguageContext context, string path, SourceCodeKind kind)
		{
			ContractUtils.RequiresNotNull(codeDom, "codeDom");
			ContractUtils.RequiresNotNull(context, "context");
			ContractUtils.Requires(path == null || path.Length > 0, "path");
			// Convert the CodeDom to source code
			if (Writer != null)
				Writer.Close();
			Writer = new PositionTrackingWriter();
			WriteFunctionDefinition(codeDom);
			var src = context.CreateSnippet(Writer.ToString(), path, kind);
			src.SetLineMapping(Writer.GetLineMap());
			return src;
		}

		/// <summary>�w�肳�ꂽ�����̒l�ւ̎Q�Ƃ�\�� <see cref="CodeArgumentReferenceExpression"/> ����\�[�X�R�[�h�𐶐����܂��B</summary>
		/// <param name="expression">�R�[�h�𐶐����� <see cref="CodeArgumentReferenceExpression"/> ���w�肵�܂��B</param>
		protected virtual void WriteArgumentReferenceExpression(CodeArgumentReferenceExpression expression) { Writer.Write(expression.ParameterName); }

		/// <summary>�w�肳�ꂽ���e��������\�� <see cref="CodeSnippetExpression"/> ����\�[�X�R�[�h�𐶐����܂��B</summary>
		/// <param name="expression">�R�[�h�𐶐����� <see cref="CodeSnippetExpression"/> ���w�肵�܂��B</param>
		protected virtual void WriteSnippetExpression(CodeSnippetExpression expression) { Writer.Write(expression.Value); }

		/// <summary>�w�肳�ꂽ���e�����R�[�h�Ђ��g�p����X�e�[�g�����g��\�� <see cref="CodeSnippetStatement"/> ����\�[�X�R�[�h�𐶐����܂��B</summary>
		/// <param name="statement">�R�[�h�𐶐����� <see cref="CodeSnippetStatement"/> ���w�肵�܂��B</param>
		protected virtual void WriteSnippetStatement(CodeSnippetStatement statement)
		{
			Writer.Write(statement.Value);
			Writer.Write('\n');
		}

		/// <summary>�w�肳�ꂽ�X�e�[�g�����g��\�� <see cref="CodeStatement"/> ����\�[�X�R�[�h�𐶐����܂��B</summary>
		/// <param name="statement">�R�[�h�𐶐����� <see cref="CodeStatement"/> ���w�肵�܂��B</param>
		protected void WriteStatement(CodeStatement statement)
		{
			// Save statement source location
			if (statement.LinePragma != null)
				Writer.MapLocation(statement.LinePragma);
			var ces = statement as CodeExpressionStatement;
			if (ces != null)
			{
				WriteExpressionStatement(ces);
				return;
			}
			var css = statement as CodeSnippetStatement;
			if (css != null)
				WriteSnippetStatement(css);
		}

		/// <summary>�w�肳�ꂽ����\�� <see cref="CodeExpression"/> ����\�[�X�R�[�h�𐶐����܂��B</summary>
		/// <param name="expression">�R�[�h�𐶐����� <see cref="CodeExpression"/> ���w�肵�܂��B</param>
		protected void WriteExpression(CodeExpression expression)
		{
			var cse = expression as CodeSnippetExpression;
			if (cse != null)
			{
				WriteSnippetExpression(cse);
				return;
			}
			var cpe = expression as CodePrimitiveExpression;
			if (cpe != null)
			{
				WritePrimitiveExpression(cpe);
				return;
			}
			var cmie = expression as CodeMethodInvokeExpression;
			if (cmie != null)
			{
				WriteCallExpression(cmie);
				return;
			}
			var care = expression as CodeArgumentReferenceExpression;
			if (care != null)
				WriteArgumentReferenceExpression(care);
		}

		/// <summary>�w�肳�ꂽ�v���~�e�B�u �f�[�^�^�̒l��\�� <see cref="CodePrimitiveExpression"/> ����\�[�X�R�[�h�𐶐����܂��B</summary>
		/// <param name="expression">�R�[�h�𐶐����� <see cref="CodePrimitiveExpression"/> ���w�肵�܂��B</param>
		protected void WritePrimitiveExpression(CodePrimitiveExpression expression)
		{
			var strVal = expression.Value as string;
			if (strVal != null)
				Writer.Write(QuoteString(strVal));
			else
				Writer.Write(expression.Value);
		}

		/// <summary>�w�肳�ꂽ���\�b�h�Ăяo����\�� <see cref="CodeMethodInvokeExpression"/> ����\�[�X�R�[�h�𐶐����܂��B</summary>
		/// <param name="m">�R�[�h�𐶐����� <see cref="CodeMethodInvokeExpression"/> ���w�肵�܂��B</param>
		protected void WriteCallExpression(CodeMethodInvokeExpression m)
		{
			if (m.Method.TargetObject != null)
			{
				WriteExpression(m.Method.TargetObject);
				Writer.Write(".");
			}
			Writer.Write(m.Method.MethodName);
			Writer.Write("(");
			for (int i = 0; i < m.Parameters.Count; ++i)
			{
				if (i != 0)
					Writer.Write(",");
				WriteExpression(m.Parameters[i]);
			}
			Writer.Write(")");
		}
	}
}
