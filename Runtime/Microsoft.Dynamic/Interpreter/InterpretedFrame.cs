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
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>インタプリタによって実行されるプロシージャに対するスタックフレームを表します。</summary>
	public sealed class InterpretedFrame
	{
		[ThreadStatic]
		static StrongBox<InterpretedFrame> threadedCurrentFrame;

		/// <summary>現在のスレッドで実行されているプロシージャのスタックフレームを取得します。</summary>
		public static InterpretedFrame CurrentFrame { get { return (threadedCurrentFrame ?? (threadedCurrentFrame = new StrongBox<InterpretedFrame>())).Value; } }

		/// <summary>このスタックフレームのプロシージャを実行しているインタプリタを表します。</summary>
		internal readonly Interpreter Interpreter;

		int[] _continuations;
		int _continuationIndex;
		int _pendingContinuation;
		object _pendingValue;

		/// <summary>このスタックフレームのデータ領域を表します。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
		public readonly object[] Data;

		/// <summary>このスタックフレームに提供されたクロージャを実現するデータを表します。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
		public readonly StrongBox<object>[] Closure;

		/// <summary>このスタックフレームのデータ領域で次にデータがプッシュされるインデックスを表します。</summary>
		public int StackIndex;
		/// <summary>このスタックフレームで次に実行される命令を示すインデックスを表します。</summary>
		public int InstructionIndex;
		// TODO: remove
		/// <summary>このスタックフレームで最近失敗した命令を示すインデックスを表します。</summary>
		public int FaultingInstruction;
		// ThreadAbortException が解析されるコードから発生した際に、これはその例外を捕捉する最初のフレームなります。
		// 戻る際にこのハンドラを含むどのハンドラも現在のスレッドを再度中止しません。
		/// <summary>このスタックフレームに関連付けられた <see cref="System.Threading.ThreadAbortException"/> に対する例外ハンドラを表します。</summary>
		public ExceptionHandler CurrentAbortHandler;

		/// <summary>実際に実行を行うインタプリタと外側のスコープから渡されるデータを使用して、<see cref="Microsoft.Scripting.Interpreter.InterpretedFrame"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="interpreter">実際にこのスタックフレームのプロシージャを実行するインタプリタを指定します。</param>
		/// <param name="closure">このスタックフレームに外側のスコープから提供されたデータを指定します。</param>
		internal InterpretedFrame(Interpreter interpreter, StrongBox<object>[] closure)
		{
			Interpreter = interpreter;
			StackIndex = interpreter.LocalCount;
			Data = new object[StackIndex + interpreter.Instructions.MaxStackDepth];
			if (interpreter.Instructions.MaxContinuationDepth > 0)
				_continuations = new int[interpreter.Instructions.MaxContinuationDepth];
			Closure = closure;
		}

		/// <summary>指定された命令インデックスに関連付けられたデバッグ情報を取得します。</summary>
		/// <param name="instructionIndex">デバッグ情報を取得する命令を示すインデックスを指定します。</param>
		/// <returns>命令に関連付けられたデバッグ情報。</returns>
		public DebugInfo GetDebugInfo(int instructionIndex) { return DebugInfo.GetMatchingDebugInfo(Interpreter._debugInfos, instructionIndex); }

		/// <summary>このスタックフレームで実行しているコードの基になったラムダ式を表す <see cref="LambdaExpression"/> を取得します。</summary>
		public LambdaExpression Lambda { get { return Interpreter._lambda; } }

		/// <summary>このスタックフレームのデータ領域に指定されたデータをプッシュします。</summary>
		/// <param name="value">プッシュするデータを指定します。</param>
		public void Push(object value) { Data[StackIndex++] = value; }

		/// <summary>このスタックフレームのデータ領域に指定されたブール値をプッシュします。</summary>
		/// <param name="value">プッシュするブール値を指定します。</param>
		public void Push(bool value) { Data[StackIndex++] = value ? ScriptingRuntimeHelpers.True : ScriptingRuntimeHelpers.False; }

		/// <summary>このスタックフレームのデータ領域に指定された 32 ビット符号付き整数をプッシュします。</summary>
		/// <param name="value">プッシュする 32 ビット符号付き整数を指定します。</param>
		public void Push(int value) { Data[StackIndex++] = ScriptingRuntimeHelpers.Int32ToObject(value); }

		/// <summary>このスタックフレームのデータ領域からデータをポップします。</summary>
		/// <returns>ポップされたデータ。</returns>
		public object Pop() { return Data[--StackIndex]; }

		/// <summary>このスタックフレームのスタックの深さを指定された値に設定します。</summary>
		/// <param name="depth">設定するスタックの深さを示す値を指定します。</param>
		internal void SetStackDepth(int depth) { StackIndex = Interpreter.LocalCount + depth; }

		/// <summary>このスタックフレームのデータ領域から次にポップされる値を実際にはポップせずに返します。</summary>
		/// <returns>次にポップされる値。</returns>
		public object Peek() { return Data[StackIndex - 1]; }

		/// <summary>このスタックフレームに対応するプロシージャを呼び出したプロシージャに対するスタックフレームを取得します。</summary>
		public InterpretedFrame Parent { get; private set; }

		/// <summary>指定されたスタックフレームがインタプリタによって実行されているかどうかを判断します。</summary>
		/// <param name="frame">調べるスタックフレームを指定します。</param>
		/// <returns>スタックフレームがインタプリタによって実行されていた場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool IsInterpretedFrame(StackFrame frame)
		{
			ContractUtils.RequiresNotNull(frame, "frame");
			var method = frame.GetMethod();
			return method.DeclaringType == typeof(Interpreter) && method.Name == "Run";
		}

		/// <summary>単一の CLR フレームが単一のインタプリタによるフレームを構成できるように、重複する CLR フレームを取り除きます。</summary>
		/// <param name="stackTrace">重複を含んでいる可能性のある <see cref="StackFrame"/> のシーケンスを指定します。</param>
		/// <returns>重複が取り除かれた <see cref="StackFrame"/> のシーケンス。</returns>
		public static IEnumerable<StackFrame> GroupStackFrames(IEnumerable<StackFrame> stackTrace)
		{
			bool inInterpretedFrame = false;
			foreach (var frame in stackTrace)
			{
				if (IsInterpretedFrame(frame))
				{
					if (inInterpretedFrame)
						continue;
					inInterpretedFrame = true;
				}
				else
					inInterpretedFrame = false;
				yield return frame;
			}
		}

		/// <summary>このフレームおよびこのフレームを呼び出したすべてのフレームに対するスタックトレース用のデバッグ情報を取得します。</summary>
		/// <returns>すべてのフレームに対するスタックトレース用のデバッグ情報のシーケンス。</returns>
		public IEnumerable<InterpretedFrameInfo> GetStackTraceDebugInfo()
		{
			for (var frame = this; frame != null; frame = frame.Parent)
				yield return new InterpretedFrameInfo(frame.Lambda.Name, frame.GetDebugInfo(frame.InstructionIndex));
		}

		/// <summary>指定された例外にこのスタックフレームに関するデバッグ情報を格納します。</summary>
		/// <param name="exception">デバッグ情報を格納する例外を指定します。</param>
		internal void SaveTraceToException(Exception exception)
		{
			if (exception.Data[typeof(InterpretedFrameInfo)] == null)
				exception.Data[typeof(InterpretedFrameInfo)] = new List<InterpretedFrameInfo>(GetStackTraceDebugInfo()).ToArray();
		}

		/// <summary>指定された例外にスタックトレース情報が存在していればその情報を取得します。</summary>
		/// <param name="exception">スタックトレース情報を取得する例外を指定します。</param>
		/// <returns>取得されたスタックトレース情報。</returns>
		public static InterpretedFrameInfo[] GetExceptionStackTrace(Exception exception) { return exception.Data[typeof(InterpretedFrameInfo)] as InterpretedFrameInfo[]; }

#if DEBUG
		/// <summary>このフレームおよびこのフレームを呼び出したすべてのフレームに対するトレース情報を取得します。</summary>
		internal string[] Trace
		{
			get
			{
				var trace = new List<string>();
				for (var frame = this; frame != null; frame = frame.Parent)
					trace.Add(frame.Lambda.Name);
				return trace.ToArray();
			}
		}
#endif

		/// <summary>このスタックフレームの実行の開始を示し、現在のスレッドで実行されているフレーム情報を更新します。</summary>
		/// <returns>スタックフレームからの実行を終了する場合に使用する情報。</returns>
		internal StrongBox<InterpretedFrame> Enter()
		{
			if (threadedCurrentFrame == null)
				threadedCurrentFrame = new StrongBox<InterpretedFrame>();
			Parent = threadedCurrentFrame.Value;
			threadedCurrentFrame.Value = this;
			return threadedCurrentFrame;
		}

		/// <summary>このスタックフレームの実行の終了を示し、現在のスレッドで実行されているフレーム情報を更新します。</summary>
		/// <param name="currentFrame"><see cref="InterpretedFrame.Enter"/> で返された情報を指定します。</param>
		internal void Leave(StrongBox<InterpretedFrame> currentFrame) { currentFrame.Value = Parent; }

		/// <summary>このスタックフレームに最後にプッシュした継続に関する情報を削除します。</summary>
		public void RemoveContinuation() { _continuationIndex--; }

		/// <summary>指定された継続をこのスタックフレームにプッシュします。</summary>
		/// <param name="continuation">プッシュする継続を行うラベルのインデックスを指定します。</param>
		public void PushContinuation(int continuation) { _continuations[_continuationIndex++] = continuation; }

		/// <summary>このスタックフレームに最後にプッシュされた継続に処理を譲ります。</summary>
		/// <returns>処理を譲る命令に対するオフセット。</returns>
		public int YieldToCurrentContinuation()
		{
			var target = Interpreter._labels[_continuations[_continuationIndex - 1]];
			SetStackDepth(target.StackDepth);
			return target.Index - InstructionIndex;
		}

		/// <summary>このスタックフレームで最後にプッシュされた継続または保留中の継続に処理を譲ります。</summary>
		/// <returns>処理を譲る命令に対するオフセット。</returns>
		public int YieldToPendingContinuation()
		{
			Debug.Assert(_pendingContinuation >= 0);
			var pendingTarget = Interpreter._labels[_pendingContinuation];
			// 現在の継続はより高い優先順位をもつ (continuationIndex は現在の継続の深さ):
			if (pendingTarget.ContinuationStackDepth < _continuationIndex)
				return YieldToCurrentContinuation();
			SetStackDepth(pendingTarget.StackDepth);
			if (_pendingValue != Interpreter.NoValue)
				Data[StackIndex - 1] = _pendingValue;
			return pendingTarget.Index - InstructionIndex;
		}

		/// <summary>保留中の継続をデータ領域にプッシュします。この操作は 2 個の新しいブロックを作成します。</summary>
		internal void PushPendingContinuation()
		{
			Push(_pendingContinuation);
			Push(_pendingValue);
#if DEBUG
			_pendingContinuation = -1;
#endif
		}

		/// <summary>保留中の継続をデータ領域からポップします。この操作は 2 個のブロックを消費します。</summary>
		internal void PopPendingContinuation()
		{
			_pendingValue = Pop();
			_pendingContinuation = (int)Pop();
		}

		static MethodInfo _Goto;
		static MethodInfo _VoidGoto;

		/// <summary><see cref="InterpretedFrame.Goto"/> を表す <see cref="MethodInfo"/> を取得します。</summary>
		internal static MethodInfo GotoMethod { get { return _Goto ?? (_Goto = typeof(InterpretedFrame).GetMethod("Goto")); } }

		/// <summary><see cref="InterpretedFrame.VoidGoto"/> を表す <see cref="MethodInfo"/> を取得します。</summary>
		internal static MethodInfo VoidGotoMethod { get { return _VoidGoto ?? (_VoidGoto = typeof(InterpretedFrame).GetMethod("VoidGoto")); } }

		/// <summary>値を渡さずに指定されたインデックスのラベルにジャンプします。</summary>
		/// <param name="labelIndex">ジャンプ先のラベルを示すインデックスを指定します。</param>
		/// <returns>ジャンプ先のラベル対象のオフセット。</returns>
		public int VoidGoto(int labelIndex) { return Goto(labelIndex, Interpreter.NoValue); }

		/// <summary>値を渡して指定されたインデックスのラベルにジャンプします。</summary>
		/// <param name="labelIndex">ジャンプ先のラベルを示すインデックスを指定します。</param>
		/// <param name="value">ジャンプの際に渡す値を指定します。</param>
		/// <returns>ジャンプ先のラベル対象のオフセット。</returns>
		public int Goto(int labelIndex, object value)
		{
			// TODO: we know this at compile time (except for compiled loop):
			var target = Interpreter._labels[labelIndex];
			if (_continuationIndex == target.ContinuationStackDepth)
			{
				SetStackDepth(target.StackDepth);
				if (value != Interpreter.NoValue)
					Data[StackIndex - 1] = value;
				return target.Index - InstructionIndex;
			}
			// if we are in the middle of executing jump we forget the previous target and replace it by a new one:
			_pendingContinuation = labelIndex;
			_pendingValue = value;
			return YieldToCurrentContinuation();
		}
	}
}
