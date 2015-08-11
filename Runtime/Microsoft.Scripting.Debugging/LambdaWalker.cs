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
using MSAst = System.Linq.Expressions;

namespace Microsoft.Scripting.Debugging
{
	/// <summary>ローカル変数の情報を式から抽出するために使用されます。</summary>
	sealed class LambdaWalker : MSAst.ExpressionVisitor
	{
		internal LambdaWalker()
		{
			Locals = new List<MSAst.ParameterExpression>();
			StrongBoxedLocals = new HashSet<MSAst.ParameterExpression>();
		}

		internal List<MSAst.ParameterExpression> Locals { get; private set; }

		internal HashSet<MSAst.ParameterExpression> StrongBoxedLocals { get; private set; }

		protected override MSAst.Expression VisitBlock(MSAst.BlockExpression node)
		{
			// ブロックに記録されているすべての変数を記録
			foreach (var local in node.Variables)
				Locals.Add(local);
			return base.VisitBlock(node);
		}

		protected override MSAst.Expression VisitRuntimeVariables(MSAst.RuntimeVariablesExpression node)
		{
			// すべての StrongBox された変数を記録
			foreach (var local in node.Variables)
				StrongBoxedLocals.Add(local);
			return base.VisitRuntimeVariables(node);
		}

		// ネストされたラムダ式は明示的に走査しない。これらはすでに変形されているはず
		protected override MSAst.Expression VisitLambda<T>(MSAst.Expression<T> node) { return node; }
	}
}
