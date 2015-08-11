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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Debugging.CompilerServices;

namespace Microsoft.Scripting.Debugging
{
	public static class RuntimeOps
	{
		public static DebugFrame CreateFrameForGenerator(DebugContext debugContext, FunctionInfo func) { return debugContext.CreateFrameForGenerator(func); }

		public static bool PopFrame(DebugThread thread) { return thread.PopFrame(); }

		public static void OnTraceEvent(DebugThread thread, int debugMarker, Exception exception) { thread.DebugContext.DispatchDebugEvent(thread, debugMarker, exception != null ? TraceEventKind.Exception : TraceEventKind.TracePoint, exception); }

		public static void OnTraceEventUnwind(DebugThread thread, int debugMarker, Exception exception) { thread.DebugContext.DispatchDebugEvent(thread, debugMarker, TraceEventKind.ExceptionUnwind, exception); }

		public static void OnFrameEnterTraceEvent(DebugThread thread) { thread.DebugContext.DispatchDebugEvent(thread, 0, TraceEventKind.FrameEnter, null); }

		public static void OnFrameExitTraceEvent(DebugThread thread, int debugMarker, object retVal) { thread.DebugContext.DispatchDebugEvent(thread, debugMarker, TraceEventKind.FrameExit, retVal); }

		public static void OnThreadExitEvent(DebugThread thread) { thread.DebugContext.DispatchDebugEvent(thread, Int32.MaxValue, TraceEventKind.ThreadExit, null); }

		public static void ReplaceLiftedLocals(DebugFrame frame, IRuntimeVariables liftedLocals) { frame.ReplaceLiftedLocals(liftedLocals); }

		public static object GeneratorLoopProc(DebugThread thread)
		{
			bool moveNext;
			return thread.DebugContext.GeneratorLoopProc(thread.GetLeafFrame(), out moveNext);
		}

		public static IEnumerator<T> CreateDebugGenerator<T>(DebugFrame frame) { return new DebugGenerator<T>(frame); }

		public static int GetCurrentSequencePointForGeneratorFrame(DebugFrame frame)
		{
			Debug.Assert(frame != null);
			Debug.Assert(frame.Generator != null);
			return frame.CurrentLocationCookie;
		}

		public static int GetCurrentSequencePointForLeafGeneratorFrame(DebugThread thread)
		{
			DebugFrame frame = thread.GetLeafFrame();
			Debug.Assert(frame.Generator != null);
			return frame.CurrentLocationCookie;
		}

		public static bool IsCurrentLeafFrameRemappingToGenerator(DebugThread thread)
		{
			DebugFrame frame = null;
			if (thread.TryGetLeafFrame(ref frame))
				return frame.ForceSwitchToGeneratorLoop;
			return false;
		}

		public static DebugThread GetCurrentThread(DebugContext debugContext) { return debugContext.GetCurrentThread(); }

		public static DebugThread GetThread(DebugFrame frame) { return frame.Thread; }

		public static bool[] GetTraceLocations(FunctionInfo functionInfo) { return functionInfo.TraceLocations; }

		public static void LiftVariables(DebugThread thread, IRuntimeVariables runtimeVariables) { ((DefaultDebugThread)thread).LiftVariables(runtimeVariables); }
	}
}
