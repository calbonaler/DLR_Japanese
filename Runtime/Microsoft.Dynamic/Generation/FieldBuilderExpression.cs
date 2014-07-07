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
using System.Reflection;
using System.Reflection.Emit;

namespace Microsoft.Scripting.Generation
{
	/// <summary>型の作成が完了していないときに <see cref="FieldBuilder"/> を抽象構文木 (AST) に埋め込むことができる単純な式を表します。</summary>
	public class FieldBuilderExpression : Expression
	{
		readonly FieldBuilder _builder;

		/// <summary>埋め込む <see cref="FieldBuilder"/> を使用して、<see cref="Microsoft.Scripting.Generation.FieldBuilderExpression"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="builder">抽象構文木 (AST) に埋め込む <see cref="FieldBuilder"/> を指定します。</param>
		public FieldBuilderExpression(FieldBuilder builder) { _builder = builder; }

		/// <summary>
		/// ノードをより単純なノードに変形できることを示します。
		/// これが <c>true</c> を返す場合、<see cref="Reduce"/> を呼び出して単純化された形式を生成できます。
		/// </summary>
		public override bool CanReduce { get { return true; } }

		/// <summary>この式のノード型を返します。 拡張ノードは、このメソッドをオーバーライドするとき、<see cref="System.Linq.Expressions.ExpressionType.Extension"/> を返す必要があります。</summary>
		public sealed override ExpressionType NodeType { get { return ExpressionType.Extension; } }

		/// <summary>この <see cref="System.Linq.Expressions.Expression"/> が表す式の静的な型を取得します。</summary>
		public sealed override Type Type { get { return _builder.FieldType; } }

		/// <summary>
		/// このノードをより単純な式に変形します。
		/// <see cref="CanReduce"/> が <c>true</c> を返す場合、これは有効な式を返します。
		/// このメソッドは、それ自体も単純化する必要がある別のノードを返す場合があります。
		/// </summary>
		/// <returns>単純化された式。</returns>
		public override Expression Reduce()
		{
			FieldInfo fi = GetFieldInfo();
			Debug.Assert(fi.Name == _builder.Name);
			return Expression.Field(null, fi);
		}

		FieldInfo GetFieldInfo() { return _builder.DeclaringType.Module.ResolveField(_builder.GetToken().Token); } // FieldBuilder を作成されたフィールドに変換

		/// <summary>ノードを単純化し、単純化された式の <paramref name="visitor"/> デリゲートを呼び出します。</summary>
		/// <param name="visitor"><see cref="System.Func&lt;T,TResult&gt;"/> のインスタンス。</param>
		/// <returns>走査中の式、またはツリー内で走査中の式と置き換える式</returns>
		protected override Expression VisitChildren(ExpressionVisitor visitor) { return this; }
	}
}
