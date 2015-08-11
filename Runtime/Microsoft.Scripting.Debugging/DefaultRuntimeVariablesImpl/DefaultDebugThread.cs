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
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Debugging.CompilerServices;

namespace Microsoft.Scripting.Debugging
{
	/// <summary>
	/// <see cref="DebugThread"/> の既定の実装を表します。
	/// これは DLR のローカル変数のリフトのために <see cref="System.Linq.Expressions.RuntimeVariablesExpression"/> を使用します。
	/// </summary>
	sealed class DefaultDebugThread : DebugThread
	{
		readonly List<FrameRuntimeVariablesPair> _frames;

		internal DefaultDebugThread(DebugContext debugContext) : base(debugContext) { _frames = new List<FrameRuntimeVariablesPair>(); }

		internal void LiftVariables(IRuntimeVariables runtimeVariables)
		{
			// すぐにフレームオブジェクトを作成しない。デバッグに実際に必要となったときに作成する。
			_frames.Add(new FrameRuntimeVariablesPair(new DebugRuntimeVariables(runtimeVariables), null));
		}

		internal override IEnumerable<DebugFrame> Frames { get { return Enumerable.Range(0, _frames.Count).Reverse().Select(i => GetFrame(i)); } }

		internal override DebugFrame GetLeafFrame() { return GetFrame(_frames.Count - 1); }

		internal override bool TryGetLeafFrame(ref DebugFrame frame)
		{
			if (_frames.Count <= 0)
			{
				frame = null;
				return false;
			}
			frame = _frames[_frames.Count - 1].Frame;
			return frame != null;
		}

		internal override int FrameCount { get { return _frames.Count; } }

		internal override void PushExistingFrame(DebugFrame frame) { _frames.Add(new FrameRuntimeVariablesPair(null, frame)); }

		internal override bool PopFrame()
		{
			Debug.Assert(_frames.Count > 0);
			_frames.RemoveAt(_frames.Count - 1);
			return _frames.Count == 0;
		}

		internal override FunctionInfo GetLeafFrameFunctionInfo(out int stackDepth)
		{
			stackDepth = _frames.Count - 1;
			if (stackDepth < 0)
			{
				stackDepth = Int32.MaxValue;
				return null;
			}
			var leafFrame = _frames[stackDepth].Frame;
			if (leafFrame != null)
			{
				Debug.Assert(stackDepth == leafFrame.StackDepth);
				return leafFrame.FunctionInfo;
			}
			else
			{
				Debug.Assert(_frames[stackDepth].RuntimeVariables is IDebugRuntimeVariables);
				return ((IDebugRuntimeVariables)_frames[stackDepth].RuntimeVariables).FunctionInfo;
			}
		}

		DebugFrame GetFrame(int index)
		{
			DebugFrame frame = null;
			if (index >= 0)
			{
				frame = _frames[index].Frame;
				if (frame == null)
				{
					var runtimeVariables = _frames[index].RuntimeVariables as IDebugRuntimeVariables;
					Debug.Assert(runtimeVariables != null);
					frame = new DebugFrame(this, runtimeVariables.FunctionInfo, runtimeVariables, index);
					_frames[index] = new FrameRuntimeVariablesPair(null, frame);
				}
			}

			if (index == _frames.Count - 1)
			{
				frame.IsInTraceback = IsInTraceback;
				frame.ThrownException = ThrownException;
			}

			return frame;
		}

		struct FrameRuntimeVariablesPair
		{
			public IRuntimeVariables RuntimeVariables;
			public DebugFrame Frame;

			public FrameRuntimeVariablesPair(IRuntimeVariables runtimeVariables, DebugFrame frame)
			{
				RuntimeVariables = runtimeVariables;
				Frame = frame;
			}
		}
	}
}
