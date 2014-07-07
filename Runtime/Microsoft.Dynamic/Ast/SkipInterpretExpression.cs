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

namespace Microsoft.Scripting.Ast
{
	//インタプリタが行うので、適応モードで行を追跡するコードを挿入する必要はない。
	// TODO: 適応コンパイラを向上させて、これを行う必要がないようにし、さらに言語から行追跡も取り除けるようにする。
	/// <summary>インタプリタで指定されたコードが実行されないようにマークします。</summary>
	public sealed class SkipInterpretExpression : Expression
	{
		/// <summary>指定された本体を使用して、<see cref="Microsoft.Scripting.Ast.SkipInterpretExpression"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="body">インタプリタが無視する式を指定します。</param>
		internal SkipInterpretExpression(Expression body)
		{
			if (body.Type != typeof(void))
				body = Expression.Block(body, Utils.Empty());
			Body = body;
		}

		/// <summary>インタプリタが無視する式を取得します。</summary>
		public Expression Body { get; private set; }

		/// <summary>この <see cref="System.Linq.Expressions.Expression"/> が表す式の静的な型を取得します。</summary>
		public sealed override Type Type { get { return typeof(void); } }

		/// <summary>
		/// この式のノード型を返します。
		/// 拡張ノードは、このメソッドをオーバーライドするとき、<see cref="System.Linq.Expressions.ExpressionType.Extension"/> を返す必要があります。
		/// </summary>
		public sealed override ExpressionType NodeType { get { return ExpressionType.Extension; } }

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
		public override Expression Reduce() { return Body; }

		/// <summary>
		/// ノードを単純化し、単純化された式の <paramref name="visitor"/> デリゲートを呼び出します。
		/// ノードを単純化できない場合、このメソッドは例外をスローします。
		/// </summary>
		/// <param name="visitor"><see cref="System.Func&lt;T,TResult&gt;"/> のインスタンス。</param>
		/// <returns>走査中の式、またはツリー内で走査中の式と置き換える式</returns>
		protected override Expression VisitChildren(ExpressionVisitor visitor)
		{
			var body = visitor.Visit(Body);
			return body == Body ? this : new SkipInterpretExpression(body);
		}
	}

	public static partial class Utils
	{
		/// <summary>指定された式をインタプリタによって実行されないとしてマークします。</summary>
		/// <param name="body">インタプリタが無視する式を指定します。</param>
		/// <returns>インタプリタで指定されたコードが実行されないことを表す <see cref="SkipInterpretExpression"/>。</returns>
		public static SkipInterpretExpression SkipInterpret(Expression body)
		{
			var skip = body as SkipInterpretExpression;
			return skip != null ? skip : new SkipInterpretExpression(body);
		}
	}
}
