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
using System.Reflection;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Ast
{
	/// <summary>
	/// <see cref="SymbolId"/> の定数を表します。
	/// このノードは縮退可能であり、GlobalOptimizedRewriter によってリライトされます。
	/// TODO: このノードは GlobalOptimizedRewriter が厳密に型指定されたノードを認識し、リライトできるようにするために存在します。
	/// 機能が必要なくなれば、このクラスも取り除かれます。
	/// この型が取り除かれた場合、<see cref="Microsoft.Scripting.Ast.Utils.Constant(object)"/> の戻り値の型は <see cref="Expression"/> から <see cref="ConstantExpression"/> に変更します。
	/// </summary>
	sealed class SymbolConstantExpression : Expression
	{
		/// <summary>指定された <see cref="SymbolId"/> を使用して、<see cref="Microsoft.Scripting.Ast.SymbolConstantExpression"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="value">このノードに格納する <see cref="SymbolId"/> を指定します。</param>
		internal SymbolConstantExpression(SymbolId value) { Value = value; }

		/// <summary>
		/// ノードをより単純なノードに変形できることを示します。
		/// これが <c>true</c> を返す場合、<see cref="Reduce"/> を呼び出して単純化された形式を生成できます。
		/// </summary>
		public override bool CanReduce { get { return true; } }

		/// <summary>この <see cref="System.Linq.Expressions.Expression"/> が表す式の静的な型を取得します。</summary>
		public sealed override Type Type { get { return typeof(SymbolId); } }

		/// <summary>
		/// この式のノード型を返します。
		/// 拡張ノードは、このメソッドをオーバーライドするとき、<see cref="System.Linq.Expressions.ExpressionType.Extension"/> を返す必要があります。
		/// </summary>
		public sealed override ExpressionType NodeType { get { return ExpressionType.Extension; } }

		/// <summary>このノードに格納されている <see cref="SymbolId"/> を取得します。</summary>
		public SymbolId Value { get; private set; }

		static readonly Expression _SymbolIdEmpty = Expression.Field(null, typeof(SymbolId).GetField("Empty"));
		static readonly Expression _SymbolIdInvalid = Expression.Field(null, typeof(SymbolId).GetField("Invalid"));
		static readonly ConstructorInfo _SymbolIdCtor = typeof(SymbolId).GetConstructor(new[] { typeof(int) });

		/// <summary>
		/// このノードをより単純な式に変形します。
		/// <see cref="CanReduce"/> が <c>true</c> を返す場合、これは有効な式を返します。
		/// このメソッドは、それ自体も単純化する必要がある別のノードを返す場合があります。
		/// </summary>
		/// <returns>単純化された式。</returns>
		public override Expression Reduce() { return GetExpression(Value); }

		static Expression GetExpression(SymbolId value)
		{
			if (value == SymbolId.Empty)
				return _SymbolIdEmpty;
			else if (value == SymbolId.Invalid)
				return _SymbolIdInvalid;
			else
				return Expression.New(_SymbolIdCtor, AstUtils.Constant(value.Id));
		}

		/// <summary>
		/// ノードを単純化し、単純化された式の <paramref name="visitor"/> デリゲートを呼び出します。
		/// ノードを単純化できない場合、このメソッドは例外をスローします。
		/// </summary>
		/// <param name="visitor"><see cref="System.Func&lt;T,TResult&gt;"/> のインスタンス。</param>
		/// <returns>走査中の式、またはツリー内で走査中の式と置き換える式</returns>
		protected override Expression VisitChildren(ExpressionVisitor visitor) { return this; }
	}
}
