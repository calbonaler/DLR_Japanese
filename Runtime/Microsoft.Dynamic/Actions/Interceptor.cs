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

using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Microsoft.Scripting.Actions
{
	/// <summary>
	/// インターセプターのプロトタイプです。
	/// インターセプターは実際の <see cref="CallSiteBinder"/> をラップする <see cref="CallSiteBinder"/> で、ラップされたバインダーが生成する式ツリー上での任意の操作を実行できます。
	/// </summary>
	/// <remarks>
	/// 次のような目的に対して適用できます。
	/// * 式ツリーのダンプ
	/// * 追加の書き換え
	/// * 静的コンパイル
	/// </remarks>
	public static class Interceptor
	{
		/// <summary>指定された式ツリーをインターセプトします。</summary>
		/// <param name="expression">インターセプトする式ツリーを指定します。</param>
		/// <returns>書き換えられた式ツリー。</returns>
		public static Expression Intercept(Expression expression) { return new InterceptorWalker().Visit(expression); }

		/// <summary>指定されたラムダ式をインターセプトします。</summary>
		/// <param name="lambda">インターセプトするラムダ式を指定します。</param>
		/// <returns>書き換えられたラムダ式。</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public static LambdaExpression Intercept(LambdaExpression lambda) { return new InterceptorWalker().Visit(lambda) as LambdaExpression; }

		class InterceptorSiteBinder : CallSiteBinder
		{
			readonly CallSiteBinder _binder;

			internal InterceptorSiteBinder(CallSiteBinder binder) { _binder = binder; }

			public override int GetHashCode() { return _binder.GetHashCode(); }

			public override bool Equals(object obj) { return obj != null && obj.Equals(_binder); }

			public override Expression Bind(object[] args, ReadOnlyCollection<ParameterExpression> parameters, LabelTarget returnLabel)
			{
				var binding = _binder.Bind(args, parameters, returnLabel);

				//
				// TODO: Implement interceptor action here
				//

				//
				// Call interceptor recursively to continue intercepting on rules
				//
				return Interceptor.Intercept(binding);
			}
		}

		class InterceptorWalker : ExpressionVisitor
		{
			protected override Expression VisitDynamic(DynamicExpression node) { return node.Binder is InterceptorSiteBinder ? node : Expression.MakeDynamic(node.DelegateType, new InterceptorSiteBinder(node.Binder), node.Arguments); }
		}
	}
}
