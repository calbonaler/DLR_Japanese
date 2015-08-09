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
	/// <summary>Code DOM からのソースコード生成を行います。</summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")] // TODO: fix
	public abstract class CodeDomCodeGen
	{
		/// <summary><see cref="CodeObject.UserData"/> でインスタンスの元のソースファイル内での位置を追跡するために使用されるキーを示します。</summary>
		protected static readonly object SourceSpanKey = typeof(SourceSpan);

		/// <summary>生成されたコードを格納する <see cref="PositionTrackingWriter"/> を取得します。</summary>
		protected PositionTrackingWriter Writer { get; private set; }

		/// <summary>指定された式文を表す <see cref="CodeExpressionStatement"/> に対するコードを生成します。</summary>
		/// <param name="statement">コードを生成する <see cref="CodeExpressionStatement"/> を指定します。</param>
		protected abstract void WriteExpressionStatement(CodeExpressionStatement statement);

		/// <summary>指定されたメソッド宣言を表す <see cref="CodeMemberMethod"/> に対するコードを生成します。</summary>
		/// <param name="func">コードを生成する <see cref="CodeMemberMethod"/> を指定します。</param>
		protected abstract void WriteFunctionDefinition(CodeMemberMethod func);

		/// <summary>指定された文字列をリテラル形式に変換します。</summary>
		/// <param name="val">リテラル形式に変換する文字列値を指定します。</param>
		/// <returns>リテラル形式に変換された文字列。</returns>
		protected abstract string QuoteString(string val);

		/// <summary>指定されたメソッド宣言を表す <see cref="CodeMemberMethod"/> に対するコードを生成します。</summary>
		/// <param name="codeDom">コードを生成するメソッド宣言を表す <see cref="CodeMemberMethod"/> を指定します。</param>
		/// <param name="context"><see cref="SourceUnit"/> を作成する <see cref="LanguageContext"/> を指定します。</param>
		/// <param name="path">生成されるソースコードのパスを指定します。</param>
		/// <param name="kind">生成されるソースコードの種類を指定します。</param>
		/// <returns>生成されたソースコードに対する翻訳入力単位を表す <see cref="SourceUnit"/>。</returns>
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

		/// <summary>指定された引数の値への参照を表す <see cref="CodeArgumentReferenceExpression"/> からソースコードを生成します。</summary>
		/// <param name="expression">コードを生成する <see cref="CodeArgumentReferenceExpression"/> を指定します。</param>
		protected virtual void WriteArgumentReferenceExpression(CodeArgumentReferenceExpression expression) { Writer.Write(expression.ParameterName); }

		/// <summary>指定されたリテラル式を表す <see cref="CodeSnippetExpression"/> からソースコードを生成します。</summary>
		/// <param name="expression">コードを生成する <see cref="CodeSnippetExpression"/> を指定します。</param>
		protected virtual void WriteSnippetExpression(CodeSnippetExpression expression) { Writer.Write(expression.Value); }

		/// <summary>指定されたリテラルコード片を使用するステートメントを表す <see cref="CodeSnippetStatement"/> からソースコードを生成します。</summary>
		/// <param name="statement">コードを生成する <see cref="CodeSnippetStatement"/> を指定します。</param>
		protected virtual void WriteSnippetStatement(CodeSnippetStatement statement)
		{
			Writer.Write(statement.Value);
			Writer.Write('\n');
		}

		/// <summary>指定されたステートメントを表す <see cref="CodeStatement"/> からソースコードを生成します。</summary>
		/// <param name="statement">コードを生成する <see cref="CodeStatement"/> を指定します。</param>
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

		/// <summary>指定された式を表す <see cref="CodeExpression"/> からソースコードを生成します。</summary>
		/// <param name="expression">コードを生成する <see cref="CodeExpression"/> を指定します。</param>
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

		/// <summary>指定されたプリミティブ データ型の値を表す <see cref="CodePrimitiveExpression"/> からソースコードを生成します。</summary>
		/// <param name="expression">コードを生成する <see cref="CodePrimitiveExpression"/> を指定します。</param>
		protected void WritePrimitiveExpression(CodePrimitiveExpression expression)
		{
			var strVal = expression.Value as string;
			if (strVal != null)
				Writer.Write(QuoteString(strVal));
			else
				Writer.Write(expression.Value);
		}

		/// <summary>指定されたメソッド呼び出しを表す <see cref="CodeMethodInvokeExpression"/> からソースコードを生成します。</summary>
		/// <param name="m">コードを生成する <see cref="CodeMethodInvokeExpression"/> を指定します。</param>
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
