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
using System.Diagnostics;

namespace Microsoft.Scripting.Debugging.CompilerServices
{
	public sealed partial class DebugContext
	{
		static object _debugYieldValue;

		internal static object DebugYieldValue { get { return _debugYieldValue ?? (_debugYieldValue = new object()); } }

		internal object GeneratorLoopProc(DebugFrame frame, out bool moveNext)
		{
			Debug.Assert(frame.Generator != null);

			moveNext = true;
			var skipTraceEvent = true;

			// ForceSwitchToGeneratorLoop フラグをリセット
			frame.ForceSwitchToGeneratorLoop = false;

			while (true)
			{
				if (!skipTraceEvent && frame.FunctionInfo.SequencePoints[frame.CurrentLocationCookie].SourceFile.DebugMode == DebugMode.FullyEnabled ||
					frame.FunctionInfo.SequencePoints[frame.CurrentLocationCookie].SourceFile.DebugMode == DebugMode.TracePoints && frame.FunctionInfo.TraceLocations[frame.CurrentLocationCookie])
				{
					Debug.Assert(((IEnumerator)frame.Generator).Current == DebugYieldValue);
					frame.InGeneratorLoop = true;
					try { DispatchDebugEvent(frame.Thread, frame.CurrentLocationCookie, TraceEventKind.TracePoint, null); }
#if DEBUG
					catch (ForceToGeneratorLoopException)
					{
						Debug.Fail("ForceToGeneratorLoopException がジェネレータループ内でスローされました。");
						throw;
					}
#endif
					finally { frame.InGeneratorLoop = false; }
				}
				skipTraceEvent = false;

				// 次の yield へ進める
				try
				{
					moveNext = ((IEnumerator)frame.Generator).MoveNext();
					var current = ((IEnumerator)frame.Generator).Current;

					// 以前の既知のマーカーを更新
					if (frame.Generator.YieldMarkerLocation != Int32.MaxValue)
						frame.LastKnownGeneratorYieldMarker = frame.Generator.YieldMarkerLocation;

					// ユーザーコードの yield かデバッグ用 yield かを判定
					if (current != DebugYieldValue || !moveNext)
					{
						var retVal = moveNext ? current : null;
						Debug.Assert(retVal != DebugYieldValue);
						return retVal;
					}
				}
				catch (ForceToGeneratorLoopException)
				{
					// ネストされた catch ブロックから例外がスローされたか、その例外がキャンセルされている場合、ここに到達する
					skipTraceEvent = true;
				}
				catch (Exception ex)
				{
					if (frame.DebugContext.DebugMode == DebugMode.Disabled)
						throw;
					try
					{
						frame.InGeneratorLoop = true;
						DispatchDebugEvent(frame.Thread, frame.CurrentLocationCookie, TraceEventKind.ExceptionUnwind, ex);
					}
					finally { frame.InGeneratorLoop = false; }

					// Rethrow if the exception is not cancelled
					if (frame.ThrownException != null)
						throw;

					skipTraceEvent = true;
				}
			}
		}
	}
}
