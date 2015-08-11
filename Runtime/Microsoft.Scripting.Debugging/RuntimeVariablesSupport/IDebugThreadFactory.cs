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
using Microsoft.Scripting.Debugging.CompilerServices;
using MSAst = System.Linq.Expressions;

namespace Microsoft.Scripting.Debugging
{
	/// <summary>フレームやローカル変数の実行時またはデバッグ時における維持の方法を抽象化するために使用されます。</summary>
	interface IDebugThreadFactory
	{
		DebugThread CreateDebugThread(DebugContext debugContext);
		MSAst.Expression CreatePushFrameExpression(MSAst.ParameterExpression functionInfo, MSAst.ParameterExpression debugMarker, IEnumerable<MSAst.ParameterExpression> locals, IEnumerable<VariableInfo> varInfos, MSAst.Expression runtimeThread);
	}
}
