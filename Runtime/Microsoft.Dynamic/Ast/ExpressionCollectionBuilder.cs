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

using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.Scripting.Ast
{
	/// <summary><see cref="Expression"/> 型のコレクションを構築する方法を提供します。</summary>
	/// <typeparam name="TExpression">ビルダーに追加できる要素の型を指定します。</typeparam>
	public class ExpressionCollectionBuilder<TExpression> : IEnumerable<TExpression> where TExpression : Expression
	{
		/// <summary>このビルダーの 1 番目の要素を取得します。</summary>
		public TExpression Expression0 { get; private set; }

		/// <summary>このビルダーの 2 番目の要素を取得します。</summary>
		public TExpression Expression1 { get; private set; }

		/// <summary>このビルダーの 3 番目の要素を取得します。</summary>
		public TExpression Expression2 { get; private set; }

		/// <summary>このビルダーの 4 番目の要素を取得します。</summary>
		public TExpression Expression3 { get; private set; }

		/// <summary><see cref="Microsoft.Scripting.Ast.ExpressionCollectionBuilder&lt;TExpression&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		public ExpressionCollectionBuilder() { }

		/// <summary>このビルダーに追加された要素の数を取得します。</summary>
		public int Count { get; private set; }

		/// <summary>
		/// ビルダーに追加された要素の数が 5 個以上であればすべての要素を含む <see cref="ReadOnlyCollectionBuilder&lt;TExpression&gt;"/> を取得します。
		/// それ以外の場合は <c>null</c> を返します。
		/// </summary>
		public ReadOnlyCollectionBuilder<TExpression> Expressions { get; private set; }

		/// <summary>このビルダーに指定された式を追加します。</summary>
		/// <param name="expressions">このビルダーに追加する式を指定します。</param>
		public void Add(IEnumerable<TExpression> expressions)
		{
			if (expressions != null)
			{
				foreach (var expression in expressions)
					Add(expression);
			}
		}

		/// <summary>このビルダーに指定された式を追加します。</summary>
		/// <param name="expression">このビルダーに追加する式を指定します。</param>
		public void Add(TExpression expression)
		{
			if (expression == null)
				return;
			switch (Count)
			{
				case 0: Expression0 = expression; break;
				case 1: Expression1 = expression; break;
				case 2: Expression2 = expression; break;
				case 3: Expression3 = expression; break;
				case 4:
					Expressions = new ReadOnlyCollectionBuilder<TExpression> { Expression0, Expression1, Expression2, Expression3, expression };
					break;
				default:
					Expressions.Add(expression);
					break;
			}
			Count++;
		}

		IEnumerator<TExpression>/*!*/ GetItemEnumerator()
		{
			if (Count > 0)
				yield return Expression0;
			if (Count > 1)
				yield return Expression1;
			if (Count > 2)
				yield return Expression2;
			if (Count > 3)
				yield return Expression3;
		}

		/// <summary>このコレクションを反復処理する列挙子を取得します。</summary>
		/// <returns>コレクションを反復処理する列挙子。</returns>
		public IEnumerator<TExpression>/*!*/ GetEnumerator() { return Expressions != null ? Expressions.GetEnumerator() : GetItemEnumerator(); }

		/// <summary>このコレクションを反復処理する列挙子を取得します。</summary>
		/// <returns>コレクションを反復処理する列挙子。</returns>
		System.Collections.IEnumerator/*!*/ System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}

	/// <summary>メソッド呼び出しの引数を構築する方法を提供します。</summary>
	public class ExpressionCollectionBuilder : ExpressionCollectionBuilder<Expression>
	{
		/// <summary>このビルダーに含まれている引数を使用して、指定されたインスタンスでメソッドを呼び出す式を返します。</summary>
		/// <param name="instance">指定されたメソッドを呼び出すインスタンスを指定します。<c>null</c> を指定すると静的メソッドの呼び出しになります。</param>
		/// <param name="method">呼び出すメソッドを指定します。</param>
		/// <returns>このビルダーに含まれている引数を使用してメソッドを呼び出す式。</returns>
		public Expression/*!*/ ToMethodCall(Expression instance, MethodInfo/*!*/ method)
		{
			switch (Count)
			{
				case 0:
					return Expression.Call(instance, method);
				case 1:
					// we have no specialized subclass for instance method call expression with 1 arg:
					return instance != null ?
						Expression.Call(instance, method, new[] { Expression0 }) :
						Expression.Call(method, Expression0);
				case 2:
					return Expression.Call(instance, method, Expression0, Expression1);
				case 3:
					return Expression.Call(instance, method, Expression0, Expression1, Expression2);
				case 4:
					// we have no specialized subclass for instance method call expression with 4 args:
					return instance != null ?
						Expression.Call(instance, method, new[] { Expression0, Expression1, Expression2, Expression3 }) :
						Expression.Call(method, Expression0, Expression1, Expression2, Expression3);
				default:
					return Expression.Call(instance, method, Expressions);
			}
		}
	}
}
