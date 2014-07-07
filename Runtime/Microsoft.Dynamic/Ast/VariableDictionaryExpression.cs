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
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Runtime;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Ast
{
	public partial class Utils
	{
		/// <summary>指定されたローカル変数を格納するディクショナリを作成する <see cref="Expression"/> を返します。</summary>
		/// <param name="variables">ディクショナリに格納するローカル変数を指定します。</param>
		/// <returns>ローカル変数を格納するディクショナリを作成する <see cref="Expression"/>。</returns>
		public static Expression VariableDictionary(params ParameterExpression[] variables) { return VariableDictionary(variables.AsEnumerable()); }

		/// <summary>指定されたローカル変数を格納するディクショナリを作成する <see cref="Expression"/> を返します。</summary>
		/// <param name="variables">ディクショナリに格納するローカル変数を指定します。</param>
		/// <returns>ローカル変数を格納するディクショナリを作成する <see cref="Expression"/>。</returns>
		public static Expression VariableDictionary(IEnumerable<ParameterExpression> variables)
		{
			return Expression.New(
				typeof(LocalsDictionary).GetConstructor(new[] { typeof(IRuntimeVariables), typeof(SymbolId[]) }),
				Expression.RuntimeVariables(variables),
				AstUtils.Constant(variables.Select(v => SymbolTable.StringToIdOrEmpty(v.Name)).ToArray())
			);
		}
	}
}
