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
using MSAst = System.Linq.Expressions;

namespace Microsoft.Scripting.Debugging
{
	using Ast = MSAst.Expression;

	/// <summary>
	/// <see cref="IDebugThreadFactory"/> の既定の実装を表します。
	/// これは DLR のローカル変数のリフトのために <see cref="System.Linq.Expressions.RuntimeVariablesExpression"/> を使用します。
	/// </summary>
	sealed class DefaultDebugThreadFactory : IDebugThreadFactory
	{
		public DebugThread CreateDebugThread(CompilerServices.DebugContext debugContext) { return new DefaultDebugThread(debugContext); }

		public MSAst.Expression CreatePushFrameExpression(MSAst.ParameterExpression functionInfo, MSAst.ParameterExpression debugMarker, IEnumerable<MSAst.ParameterExpression> locals, IEnumerable<VariableInfo> varInfos, Ast runtimeThread)
		{
			return Ast.Call(
				new System.Action<DebugThread, System.Runtime.CompilerServices.IRuntimeVariables>(RuntimeOps.LiftVariables).Method,
				runtimeThread,
				Ast.RuntimeVariables(Enumerable.Repeat(functionInfo, 1).Concat(Enumerable.Repeat(debugMarker, 1)).Concat(locals))
			);
		}
	}
}
