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

using MSAst = System.Linq.Expressions;
using System.Collections.Generic;

namespace Microsoft.Scripting.Debugging.CompilerServices
{
	/// <summary><see cref="DebugContext"/> �ւ� <see cref="System.Linq.Expressions.LambdaExpression"/> �Ɋւ���ǉ��̃f�o�b�O����񋟂��邽�߂ɃR���p�C���ɂ���Ďg�p�����N���X��\���܂��B</summary>
	public sealed class DebugLambdaInfo
	{
		public DebugLambdaInfo(IDebugCompilerSupport compilerSupport, string lambdaAlias, bool optimizeForLeafFrames, IList<MSAst.ParameterExpression> hiddenVariables, IDictionary<MSAst.ParameterExpression, string> variableAliases, object customPayload)
		{
			CompilerSupport = compilerSupport;
			LambdaAlias = lambdaAlias;
			HiddenVariables = hiddenVariables;
			VariableAliases = variableAliases;
			CustomPayload = customPayload;
			OptimizeForLeafFrames = optimizeForLeafFrames;
		}

		public IDebugCompilerSupport CompilerSupport { get; private set; }

		public string LambdaAlias { get; private set; }

		public IList<MSAst.ParameterExpression> HiddenVariables { get; private set; }

		public IDictionary<MSAst.ParameterExpression, string> VariableAliases { get; private set; }

		public object CustomPayload { get; private set; }

		public bool OptimizeForLeafFrames { get; private set; }
	}
}
