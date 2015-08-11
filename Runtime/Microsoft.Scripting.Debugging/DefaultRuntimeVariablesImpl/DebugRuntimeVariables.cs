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

using System.Runtime.CompilerServices;

namespace Microsoft.Scripting.Debugging
{
	/// <summary>
	/// <see cref="IDebugRuntimeVariables"/> の実装を表します。
	/// これは <see cref="IRuntimeVariables"/> と FunctionInfo および DebugMarker をラップします。
	/// </summary>
	class DebugRuntimeVariables : IDebugRuntimeVariables
	{
		readonly IRuntimeVariables _runtimeVariables;

		internal DebugRuntimeVariables(IRuntimeVariables runtimeVariables) { _runtimeVariables = runtimeVariables; }

		public int Count { get { return _runtimeVariables.Count - 2; } }

		public object this[int index]
		{
			get { return _runtimeVariables[2 + index]; }
			set { _runtimeVariables[2 + index] = value; }
		}

		public FunctionInfo FunctionInfo { get { return (FunctionInfo)_runtimeVariables[0]; } }

		public int DebugMarker { get { return (int)_runtimeVariables[1]; } }
	}
}
