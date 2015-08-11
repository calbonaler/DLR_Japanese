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
using System.Collections.Generic;

namespace Microsoft.Scripting.Debugging
{
	public sealed class FunctionInfo
	{
		internal FunctionInfo(Delegate generatorFactory, string name, DebugSourceSpan[] sequencePoints, VariableInfo[][] scopedVariables, IList<VariableInfo> variables, object customPayload)
		{
			GeneratorFactory = generatorFactory;
			Name = name;
			SequencePoints = sequencePoints;
			VariableScopeMap = scopedVariables;
			Variables = variables;
			CustomPayload = customPayload;
			TraceLocations = new bool[sequencePoints.Length];
		}

		internal Delegate GeneratorFactory { get; private set; }

		internal IList<VariableInfo> Variables { get; private set; }

		internal VariableInfo[][] VariableScopeMap { get; private set; }

		internal FunctionInfo PreviousVersion { get; set; }

		internal FunctionInfo NextVersion { get; set; }

		internal int Version { get; set; }

		/// <summary>シーケンスポイントを取得または設定します。</summary>
		internal DebugSourceSpan[] SequencePoints { get; private set; }

		/// <summary>名前を取得または設定します。</summary>
		internal string Name { get; private set; }

		/// <summary>カスタムペイロードを取得または設定します。</summary>
		internal object CustomPayload { get; private set; }

		/// <summary>トレース位置を取得または設定します。</summary>
		internal bool[] TraceLocations { get; private set; }
	}
}
