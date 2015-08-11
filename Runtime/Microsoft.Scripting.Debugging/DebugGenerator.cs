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
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Scripting.Debugging
{
	/// <summary>変形前にすでにジェネレータであるラムダをラップするために使用されます。</summary>
	sealed class DebugGenerator<T> : IEnumerator<T>, IDisposable
	{
		DebugFrame _frame;

		internal DebugGenerator(DebugFrame frame)
		{
			_frame = frame;
			_frame.RemapToGenerator(frame.FunctionInfo.Version);
		}

		public T Current { get { return (T)((IEnumerator)this).Current; } }

		object IEnumerator.Current { get { return ((IEnumerator)_frame.Generator).Current; } }

		void IDisposable.Dispose()
		{
			var innerDisposable = _frame.Generator as IDisposable;
			if (innerDisposable != null)
				innerDisposable.Dispose();
			GC.SuppressFinalize(this);
		}

		public bool MoveNext()
		{
			_frame.Thread.PushExistingFrame(_frame);

			if (_frame.FunctionInfo.SequencePoints[_frame.CurrentLocationCookie].SourceFile.DebugMode == DebugMode.FullyEnabled ||
				_frame.FunctionInfo.SequencePoints[_frame.CurrentLocationCookie].SourceFile.DebugMode == DebugMode.TracePoints && _frame.FunctionInfo.TraceLocations[_frame.CurrentLocationCookie])
			{
				try { _frame.DebugContext.DispatchDebugEvent(_frame.Thread, _frame.CurrentLocationCookie, TraceEventKind.FrameEnter, null); }
				catch (ForceToGeneratorLoopException) { /* ジェネレータループに入ろうとしているところであるので明示的に何もすることはない */ }
			}

			try
			{
				bool moveNext;
				_frame.DebugContext.GeneratorLoopProc(_frame, out moveNext);
				return moveNext;
			}
			finally
			{
				if (_frame.FunctionInfo.SequencePoints[0].SourceFile.DebugMode == DebugMode.FullyEnabled ||
					_frame.FunctionInfo.SequencePoints[_frame.CurrentLocationCookie].SourceFile.DebugMode == DebugMode.TracePoints && _frame.FunctionInfo.TraceLocations[_frame.CurrentLocationCookie])
					_frame.DebugContext.DispatchDebugEvent(_frame.Thread, _frame.CurrentLocationCookie, TraceEventKind.FrameExit, Current);

				var threadExit = _frame.Thread.PopFrame();
				if (threadExit && _frame.DebugContext.DebugMode == DebugMode.FullyEnabled)
					// スレッド終了イベントを発行
					_frame.DebugContext.DispatchDebugEvent(_frame.Thread, Int32.MaxValue, TraceEventKind.ThreadExit, null);
			}
		}

		public void Reset()
		{
			((IEnumerator)_frame.Generator).Reset();
		}
	}
}
