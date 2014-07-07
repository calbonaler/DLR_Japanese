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
using Microsoft.Scripting.Interpreter;

namespace Microsoft.Scripting.Ast
{
	/// <summary>
	/// ツリー内の制御フローロジックを生成することで、ラップされたノード内のツリーにおける finally ブロックからのジャンプを可能にします。
	/// このノードの縮退には (ネストされたラムダではなく) 本体の式ツリーの探索が必要になります。
	/// あらゆる不明なジャンプを外側のスコープへのジャンプと仮定するので、このノードにはブロックを横断するジャンプを含めることができません。
	/// </summary>
	public sealed class FinallyFlowControlExpression : Expression, IInstructionProvider
	{
		Expression _reduced;

		/// <summary>指定された本体を使用して、<see cref="Microsoft.Scripting.Ast.FinallyFlowControlExpression"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="body">finally ブロックからのジャンプを可能にする本体を指定します。</param>
		internal FinallyFlowControlExpression(Expression body) { Body = body; }

		/// <summary>ノードをより単純なノードに変形できることを示します。</summary>
		public override bool CanReduce { get { return true; } }

		/// <summary>この <see cref="System.Linq.Expressions.Expression"/> が表す式の静的な型を取得します。</summary>
		public sealed override Type Type { get { return Body.Type; } }

		/// <summary>この <see cref="System.Linq.Expressions.Expression"/> のノード型を取得します。</summary>
		public sealed override ExpressionType NodeType { get { return ExpressionType.Extension; } }

		/// <summary>finally ブロックからのジャンプを有効にする本体を取得します。</summary>
		public Expression Body { get; private set; }

		/// <summary>このノードをより単純な式に変形します。</summary>
		/// <returns>単純化された式。</returns>
		public override Expression Reduce() { return _reduced ?? (_reduced = new FlowControlRewriter().Reduce(Body)); }

		/// <summary>ノードを単純化し、単純化された式の visitor デリゲートを呼び出します。</summary>
		/// <param name="visitor"><see cref="System.Func&lt;T,TResult&gt;"/> のインスタンス。</param>
		/// <returns>走査中の式、またはツリー内で走査中の式と置き換える式</returns>
		protected override Expression VisitChildren(ExpressionVisitor visitor)
		{
			var b = visitor.Visit(Body);
			if (b == Body)
				return this;
			return new FinallyFlowControlExpression(b);
		}

		void IInstructionProvider.AddInstructions(LightCompiler compiler) { compiler.Compile(Body); } // インタプリタは finally ブロックからのジャンプを扱うのでそのまま
	}

	public partial class Utils
	{
		/// <summary>指定された式ツリー内における finally ブロックからのジャンプを可能にします。</summary>
		/// <param name="body">finally ブロックからのジャンプを可能にする式ツリーを指定します。</param>
		/// <returns>finally ブロックからのジャンプが可能な式。</returns>
		public static Expression FinallyFlowControl(Expression body) { return new FinallyFlowControlExpression(body); }
	}
}
