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

namespace Microsoft.Scripting.Ast
{
	sealed class LambdaParameterRewriter : ExpressionVisitor
	{
		readonly Dictionary<ParameterExpression, ParameterExpression> _map;

		internal LambdaParameterRewriter(Dictionary<ParameterExpression, ParameterExpression> map) { _map = map; }

		// すべての部分で矛盾なく置き換えているため、引数のシャドーイングを心配する必要はありません。
		protected override Expression VisitParameter(ParameterExpression node)
		{
			ParameterExpression result;
			return _map.TryGetValue(node, out result) ? result : node;
		}
	}
}
