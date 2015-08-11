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
using System.Linq.Expressions;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Ast
{
	public partial class Utils
	{
		/// <summary>式ツリーの実行時に指定されたメッセージをトレース リスナーに書き込む <see cref="Expression"/> を返します。</summary>
		/// <param name="marker">式ツリーの実行時にトレース リスナーに書き込まれるメッセージを指定します。</param>
		/// <returns>メッセージをトレース リスナーに書き込む <see cref="Expression"/>。</returns>
		public static Expression DebugMarker(string marker)
		{
			ContractUtils.RequiresNotNull(marker, "marker");
#if DEBUG
			return CallDebugWriteLine(marker);
#else
            return Utils.Empty();
#endif
		}

		/// <summary>式ツリーの実行時に指定されたメッセージをトレース リスナーに書き込み、指定された <see cref="Expression"/> を返す <see cref="Expression"/> を返します。</summary>
		/// <param name="expression">この <see cref="Expression"/> 全体の値となる <see cref="Expression"/> を指定します。</param>
		/// <param name="marker">式ツリーの実行時にトレース リスナーに書き込まれるメッセージを指定します。</param>
		/// <returns>メッセージをトレース リスナーに書き込み、指定された <see cref="Expression"/> を返す <see cref="Expression"/>。</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "marker")]
		public static Expression DebugMark(Expression expression, string marker)
		{
			ContractUtils.RequiresNotNull(expression, "expression");
			ContractUtils.RequiresNotNull(marker, "marker");
#if DEBUG
			return Expression.Block(CallDebugWriteLine(marker), expression);
#else
            return expression;
#endif
		}

#if DEBUG
		static MethodCallExpression CallDebugWriteLine(string marker) { return Expression.Call(typeof(Debug).GetMethod("WriteLine", new[] { typeof(string) }), AstUtils.Constant(marker)); }
#endif

		/// <summary>指定された <see cref="Expression"/> に対してデバッグ情報を追加します。</summary>
		/// <param name="expression">デバッグ情報を追加する <see cref="Expression"/> を指定します。</param>
		/// <param name="document">追加するデバッグ情報を指定します。</param>
		/// <param name="start">デバッグ情報に追加する範囲の開始位置を指定します。</param>
		/// <param name="end">デバッグ情報に追加する範囲の終了位置を指定します。</param>
		/// <returns>指定された <see cref="Expression"/> にデバッグ情報が追加された <see cref="Expression"/>。</returns>
		public static Expression AddDebugInfo(Expression expression, SymbolDocumentInfo document, SourceLocation start, SourceLocation end)
		{
			return document == null || !start.IsValid || !end.IsValid ? expression : AddDebugInfo(expression, document, start.Line, start.Column, end.Line, end.Column);
		}

		//The following method does not check the validaity of the span
		/// <summary>指定された <see cref="Expression"/> に対してデバッグ情報を追加します。</summary>
		/// <param name="expression">デバッグ情報を追加する <see cref="Expression"/> を指定します。</param>
		/// <param name="document">追加するデバッグ情報を指定します。</param>
		/// <param name="startLine">デバッグ情報に追加する範囲の開始位置の行番号を指定します。</param>
		/// <param name="startColumn">デバッグ情報に追加する範囲の開始位置の列番号を指定します。</param>
		/// <param name="endLine">デバッグ情報に追加する範囲の終了位置の行番号を指定します。</param>
		/// <param name="endColumn">デバッグ情報に追加する範囲の終了位置の列番号を指定します。</param>
		/// <returns>指定された <see cref="Expression"/> にデバッグ情報が追加された <see cref="Expression"/>。</returns>
		public static Expression AddDebugInfo(Expression expression, SymbolDocumentInfo document, int startLine, int startColumn, int endLine, int endColumn)
		{
			if (expression == null)
				throw new ArgumentNullException("expression");
			var sequencePoint = Expression.DebugInfo(document, startLine, startColumn, endLine, endColumn);
			var clearance = Expression.ClearDebugInfo(document);
			//always attach a clearance
			if (expression.Type == typeof(void))
				return Expression.Block(sequencePoint, expression, clearance);
			else
			{
				//save the expression to a variable
				var p = Expression.Parameter(expression.Type, null);
				return Expression.Block(new[] { p }, sequencePoint, Expression.Assign(p, expression), clearance, p);
			}
		}
	}
}
