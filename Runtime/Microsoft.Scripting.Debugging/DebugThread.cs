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
using System.Threading;
using Microsoft.Scripting.Debugging.CompilerServices;

namespace Microsoft.Scripting.Debugging
{
	public abstract class DebugThread
	{
		internal DebugThread(DebugContext debugContext)
		{
			DebugContext = debugContext;
			ManagedThread = Thread.CurrentThread;
		}

		internal DebugContext DebugContext { get; private set; }

		internal Exception ThrownException { get; set; }

		internal Thread ManagedThread { get; private set; }

		internal bool IsInTraceback { get; set; }

		internal abstract IEnumerable<DebugFrame> Frames { get; }

		internal abstract DebugFrame GetLeafFrame();

		internal abstract bool TryGetLeafFrame(ref DebugFrame frame);

		internal abstract int FrameCount { get; }

		internal abstract void PushExistingFrame(DebugFrame frame);

		internal abstract bool PopFrame();

		internal abstract FunctionInfo GetLeafFrameFunctionInfo(out int stackDepth);
	}
}
