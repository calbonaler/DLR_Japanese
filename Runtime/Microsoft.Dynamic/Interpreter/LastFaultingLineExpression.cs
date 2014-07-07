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
using System.Linq.Expressions;

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>最近実行に失敗した行番号の式ツリーノードを表します。</summary>
	public class LastFaultingLineExpression : Expression
	{
		readonly Expression _lineNumberExpression;

		/// <summary>指定された行番号を表す式を使用して、<see cref="Microsoft.Scripting.Interpreter.LastFaultingLineExpression"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="lineNumberExpression">行番号を表す式ツリーノードを指定します。</param>
		public LastFaultingLineExpression(Expression lineNumberExpression) { _lineNumberExpression = lineNumberExpression; }

		/// <summary>
		/// この式のノード型を返します。
		/// 拡張ノードは、このメソッドをオーバーライドするとき、<see cref="System.Linq.Expressions.ExpressionType.Extension"/> を返す必要があります。
		/// </summary>
		public sealed override ExpressionType NodeType { get { return ExpressionType.Extension; } }

		/// <summary>この <see cref="System.Linq.Expressions.Expression"/> が表す式の静的な型を取得します。</summary>
		public sealed override Type Type { get { return typeof(int); } }

		/// <summary>
		/// ノードをより単純なノードに変形できることを示します。
		/// これが <c>true</c> を返す場合、<see cref="Reduce"/> を呼び出して単純化された形式を生成できます。
		/// </summary>
		public override bool CanReduce { get { return true; } }

		/// <summary>
		/// このノードをより単純な式に変形します。
		/// <see cref="CanReduce"/> が <c>true</c> を返す場合、これは有効な式を返します。
		/// このメソッドは、それ自体も単純化する必要がある別のノードを返す場合があります。
		/// </summary>
		/// <returns>単純化された式。</returns>
		public override Expression Reduce() { return _lineNumberExpression; }

		/// <summary>
		/// ノードを単純化し、単純化された式の <paramref name="visitor"/> デリゲートを呼び出します。
		/// ノードを単純化できない場合、このメソッドは例外をスローします。
		/// </summary>
		/// <param name="visitor"><see cref="System.Func&lt;T,TResult&gt;"/> のインスタンス。</param>
		/// <returns>走査中の式、またはツリー内で走査中の式と置き換える式</returns>
		protected override Expression VisitChildren(ExpressionVisitor visitor)
		{
			var lineNo = visitor.Visit(_lineNumberExpression);
			if (lineNo != _lineNumberExpression)
				return new LastFaultingLineExpression(lineNo);
			return this;
		}
	}

	/// <summary>フレームの最近失敗した命令を表す行番号をプッシュする命令を表します。</summary>
	sealed class UpdateStackTraceInstruction : Instruction
	{
		/// <summary>行番号を検索するデバッグ情報を表します。</summary>
		internal DebugInfo[] _debugInfos;

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			var info = DebugInfo.GetMatchingDebugInfo(_debugInfos, frame.FaultingInstruction);
			frame.Push(info != null && !info.IsClear ? info.StartLine : -1);
			return +1;
		}
	}
}
