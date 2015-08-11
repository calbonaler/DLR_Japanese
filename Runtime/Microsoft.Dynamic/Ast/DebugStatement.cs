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
		/// <summary>���c���[�̎��s���Ɏw�肳�ꂽ���b�Z�[�W���g���[�X ���X�i�[�ɏ������� <see cref="Expression"/> ��Ԃ��܂��B</summary>
		/// <param name="marker">���c���[�̎��s���Ƀg���[�X ���X�i�[�ɏ������܂�郁�b�Z�[�W���w�肵�܂��B</param>
		/// <returns>���b�Z�[�W���g���[�X ���X�i�[�ɏ������� <see cref="Expression"/>�B</returns>
		public static Expression DebugMarker(string marker)
		{
			ContractUtils.RequiresNotNull(marker, "marker");
#if DEBUG
			return CallDebugWriteLine(marker);
#else
            return Utils.Empty();
#endif
		}

		/// <summary>���c���[�̎��s���Ɏw�肳�ꂽ���b�Z�[�W���g���[�X ���X�i�[�ɏ������݁A�w�肳�ꂽ <see cref="Expression"/> ��Ԃ� <see cref="Expression"/> ��Ԃ��܂��B</summary>
		/// <param name="expression">���� <see cref="Expression"/> �S�̂̒l�ƂȂ� <see cref="Expression"/> ���w�肵�܂��B</param>
		/// <param name="marker">���c���[�̎��s���Ƀg���[�X ���X�i�[�ɏ������܂�郁�b�Z�[�W���w�肵�܂��B</param>
		/// <returns>���b�Z�[�W���g���[�X ���X�i�[�ɏ������݁A�w�肳�ꂽ <see cref="Expression"/> ��Ԃ� <see cref="Expression"/>�B</returns>
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

		/// <summary>�w�肳�ꂽ <see cref="Expression"/> �ɑ΂��ăf�o�b�O����ǉ����܂��B</summary>
		/// <param name="expression">�f�o�b�O����ǉ����� <see cref="Expression"/> ���w�肵�܂��B</param>
		/// <param name="document">�ǉ�����f�o�b�O�����w�肵�܂��B</param>
		/// <param name="start">�f�o�b�O���ɒǉ�����͈͂̊J�n�ʒu���w�肵�܂��B</param>
		/// <param name="end">�f�o�b�O���ɒǉ�����͈͂̏I���ʒu���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ <see cref="Expression"/> �Ƀf�o�b�O��񂪒ǉ����ꂽ <see cref="Expression"/>�B</returns>
		public static Expression AddDebugInfo(Expression expression, SymbolDocumentInfo document, SourceLocation start, SourceLocation end)
		{
			return document == null || !start.IsValid || !end.IsValid ? expression : AddDebugInfo(expression, document, start.Line, start.Column, end.Line, end.Column);
		}

		//The following method does not check the validaity of the span
		/// <summary>�w�肳�ꂽ <see cref="Expression"/> �ɑ΂��ăf�o�b�O����ǉ����܂��B</summary>
		/// <param name="expression">�f�o�b�O����ǉ����� <see cref="Expression"/> ���w�肵�܂��B</param>
		/// <param name="document">�ǉ�����f�o�b�O�����w�肵�܂��B</param>
		/// <param name="startLine">�f�o�b�O���ɒǉ�����͈͂̊J�n�ʒu�̍s�ԍ����w�肵�܂��B</param>
		/// <param name="startColumn">�f�o�b�O���ɒǉ�����͈͂̊J�n�ʒu�̗�ԍ����w�肵�܂��B</param>
		/// <param name="endLine">�f�o�b�O���ɒǉ�����͈͂̏I���ʒu�̍s�ԍ����w�肵�܂��B</param>
		/// <param name="endColumn">�f�o�b�O���ɒǉ�����͈͂̏I���ʒu�̗�ԍ����w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ <see cref="Expression"/> �Ƀf�o�b�O��񂪒ǉ����ꂽ <see cref="Expression"/>�B</returns>
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
