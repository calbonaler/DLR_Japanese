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

namespace Microsoft.Scripting.Debugging
{
	public sealed class DebugSourceFile
	{
		internal DebugSourceFile(string fileName, DebugMode debugMode)
		{
			Name = fileName;
			DebugMode = debugMode;
			FunctionInfoMap = new Dictionary<DebugSourceSpan, FunctionInfo>();
		}

		internal Dictionary<DebugSourceSpan, FunctionInfo> FunctionInfoMap { get; private set; }

		internal string Name { get; private set; }

		internal DebugMode DebugMode { get; set; }

		internal FunctionInfo LookupFunctionInfo(DebugSourceSpan span) { return FunctionInfoMap.Where(x => x.Key.Intersects(span)).Select(x => x.Value).FirstOrDefault(); }

		/// <summary>内部使用のみを想定しており、外部から使用するためのものではありません。</summary>
		public int Mode { get { return (int)DebugMode; } }
	}
}
