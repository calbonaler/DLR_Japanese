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
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Ast
{
	/// <summary>
	/// <see cref="GeneratorExpression"/> で YieldBreak または YieldReturn を表します。
	/// <see cref="Value"/> が <c>null</c> でない場合は YieldReturn、それ以外の場合は YieldBreak を表します。
	/// </summary>
	public sealed class YieldExpression : Expression
	{
		/// <summary>ラベル、渡される値、マーカーを使用して、<see cref="Microsoft.Scripting.Ast.YieldExpression"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="target">このジェネレータから譲られるラベルを指定します。</param>
		/// <param name="value">ラベルで渡される値を指定します。</param>
		/// <param name="yieldMarker">デバッグ用のマーカーを指定します。</param>
		internal YieldExpression(LabelTarget target, Expression value, int yieldMarker)
		{
			Target = target;
			Value = value;
			YieldMarker = yieldMarker;
		}

		/// <summary>
		/// ノードをより単純なノードに変形できることを示します。
		/// これが <c>true</c> を返す場合、<see cref="Expression.Reduce"/> を呼び出して単純化された形式を生成できます。
		/// </summary>
		public override bool CanReduce { get { return false; } }

		/// <summary>この <see cref="System.Linq.Expressions.Expression"/> が表す式の静的な型を取得します。</summary>
		public sealed override Type Type { get { return typeof(void); } }

		/// <summary>
		/// この式のノード型を返します。
		/// 拡張ノードは、このメソッドをオーバーライドするとき、<see cref="System.Linq.Expressions.ExpressionType.Extension"/> を返す必要があります。
		/// </summary>
		public sealed override ExpressionType NodeType { get { return ExpressionType.Extension; } }

		/// <summary>この式から譲られる値を取得します。</summary>
		public Expression Value { get; private set; }

		/// <summary>このジェネレータから譲られるラベルを取得します。</summary>
		public LabelTarget Target { get; private set; }

		/// <summary>デバッグ用のマーカーを取得します。</summary>
		public int YieldMarker { get; private set; }

		/// <summary>
		/// ノードを単純化し、単純化された式の <paramref name="visitor"/> デリゲートを呼び出します。
		/// ノードを単純化できない場合、このメソッドは例外をスローします。
		/// </summary>
		/// <param name="visitor"><see cref="System.Func&lt;T,TResult&gt;"/> のインスタンス。</param>
		/// <returns>走査中の式、またはツリー内で走査中の式と置き換える式</returns>
		protected override Expression VisitChildren(ExpressionVisitor visitor)
		{
			var v = visitor.Visit(Value);
			return v == Value ? this : Utils.MakeYield(Target, v, YieldMarker);
		}
	}

	public partial class Utils
	{
		/// <summary>指定されたラベルに処理を譲る YieldBreak ステートメントを作成します。</summary>
		/// <param name="target">処理が譲られるラベルを指定します。</param>
		/// <returns>YieldBreak ステートメントを表す <see cref="YieldExpression"/>。</returns>
		public static YieldExpression YieldBreak(LabelTarget target) { return MakeYield(target, null, -1); }

		/// <summary>指定されたラベルに処理を譲る YieldReturn ステートメントを作成します。</summary>
		/// <param name="target">処理が譲られるラベルを指定します。</param>
		/// <param name="value">渡される値を指定します。</param>
		/// <returns>YieldReturn ステートメントを表す <see cref="YieldExpression"/>。</returns>
		public static YieldExpression YieldReturn(LabelTarget target, Expression value) { return MakeYield(target, value, -1); }

		/// <summary>指定されたラベルに処理を譲る YieldReturn ステートメントを作成します。</summary>
		/// <param name="target">処理が譲られるラベルを指定します。</param>
		/// <param name="value">渡される値を指定します。</param>
		/// <param name="yieldMarker">デバッグ用のマーカーを指定します。</param>
		/// <returns>YieldReturn ステートメントを表す <see cref="YieldExpression"/>。</returns>
		public static YieldExpression YieldReturn(LabelTarget target, Expression value, int yieldMarker)
		{
			ContractUtils.RequiresNotNull(value, "value");
			return MakeYield(target, value, yieldMarker);
		}

		/// <summary>指定されたラベルに処理を譲る Yield ステートメントを作成します。</summary>
		/// <param name="target">処理が譲られるラベルを指定します。</param>
		/// <param name="value">渡される値を指定します。</param>
		/// <param name="yieldMarker">デバッグ用のマーカーを指定します。</param>
		/// <returns>YieldReturn ステートメントまたは YieldBreak ステートメントを表す <see cref="YieldExpression"/>。</returns>
		public static YieldExpression MakeYield(LabelTarget target, Expression value, int yieldMarker)
		{
			ContractUtils.RequiresNotNull(target, "target");
			ContractUtils.Requires(target.Type != typeof(void), "target", "ジェネレータのラベルは非 void 型である必要があります。");
			if (value != null && !TypeUtils.AreReferenceAssignable(target.Type, value.Type))
			{
				// C# は自動的にジェネレータの戻り値を引用します
				if (target.Type.IsSubclassOf(typeof(Expression)) && TypeUtils.AreAssignable(target.Type, value.GetType()))
					value = Expression.Quote(value);
				throw new ArgumentException(string.Format("型 '{0}' の式を型 '{1}' のジェネレータラベルに譲ることはできません。", value.Type, target.Type));
			}
			return new YieldExpression(target, value, yieldMarker);
		}
	}
}
