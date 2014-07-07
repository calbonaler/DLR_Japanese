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
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>
	/// IL へのコンパイルと JIT での呼び出しの必要なく式ツリーを実行できる単純な forth 形式のスタックマシンを表します。
	/// これは非常に高速なコンパイル時間と悪い実行時パフォーマンスとのトレードオフです。
	/// 少ない回数しか実行されないコードに対しては、これは良いバランスとなります。
	/// 
	/// インタプリタのメインループは <see cref="Interpreter.Run"/> メソッドに存在します。
	/// </summary>
	sealed class Interpreter
	{
		/// <summary>値が存在しないことを示します。</summary>
		internal static readonly object NoValue = new object();
		/// <summary>制御が戻った時に例外を再スローする命令インデックスを表します。</summary>
		internal const int RethrowOnReturn = int.MaxValue;

		// 0: 同期コンパイル, 負: 既定
		internal readonly int _compilationThreshold;

		internal readonly object[] _objects;
		internal readonly RuntimeLabel[] _labels;

		internal readonly LambdaExpression _lambda;
		readonly ExceptionHandler[] _handlers;
		internal readonly DebugInfo[] _debugInfos;

		/// <summary>指定された引数を使用して、<see cref="Microsoft.Scripting.Interpreter.Interpreter"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="lambda">インタプリタによって実行されるラムダ式を表す <see cref="LambdaExpression"/> を指定します。</param>
		/// <param name="locals">ローカル変数を表す <see cref="LocalVariables"/> を指定します。</param>
		/// <param name="labelMapping"><see cref="LabelTarget"/> から <see cref="BranchLabel"/> へのマッピングを指定します。</param>
		/// <param name="instructions">実際に実行する命令を表す <see cref="InstructionArray"/> を指定します。</param>
		/// <param name="handlers">例外ハンドラを表す <see cref="ExceptionHandler"/> を指定します。</param>
		/// <param name="debugInfos">デバッグ情報を指定します。</param>
		/// <param name="compilationThreshold">インタプリタによって実行できる最大回数を指定します。この数値以上実行された場合ラムダ式はコンパイルされます。</param>
		internal Interpreter(LambdaExpression lambda, LocalVariables locals, Dictionary<LabelTarget, BranchLabel> labelMapping, InstructionArray instructions, ExceptionHandler[] handlers, DebugInfo[] debugInfos, int compilationThreshold)
		{
			_lambda = lambda;
			LocalCount = locals.LocalCount;
			ClosureVariables = locals.ClosureVariables;
			Instructions = instructions;
			_objects = instructions.Objects;
			_labels = instructions.Labels;
			LabelMapping = labelMapping;
			_handlers = handlers;
			_debugInfos = debugInfos;
			_compilationThreshold = compilationThreshold;
		}

		/// <summary>クロージャのために使用される変数の数を取得します。</summary>
		internal int ClosureSize { get { return ClosureVariables == null ? 0 : ClosureVariables.Count; } }

		/// <summary>ローカル変数の数を取得します。</summary>
		internal int LocalCount { get; private set; }

		/// <summary>コンパイルを同期的に実行するかどうかを示す値を取得します。</summary>
		internal bool CompileSynchronously { get { return _compilationThreshold <= 1; } }

		/// <summary>インタプリタが実行する命令を格納する <see cref="InstructionArray"/> を取得します。</summary>
		internal InstructionArray Instructions { get; private set; }

		/// <summary>クロージャのために使用される変数を取得します。</summary>
		internal Dictionary<ParameterExpression, LocalVariable> ClosureVariables { get; private set; }

		/// <summary><see cref="LabelTarget"/> から <see cref="BranchLabel"/> に対するマッピングを取得します。</summary>
		internal Dictionary<LabelTarget, BranchLabel> LabelMapping { get; private set; }

		/// <summary>指定されたスタックフレームで命令を実行します。</summary>
		/// <param name="frame">命令を実行するスタックフレームを指定します。</param>
		/// <remarks>
		/// インタプリタのスタックフレームはこのメソッドのそれぞれの CLR フレームがチェインのインタプリタのスタックフレームに対応するように Parent の参照によって連結されます。
		/// そのためインタプリタのフレームをこのメソッドのフレームに揃えることで、CLR スタックトレースをインタプリタのスタックトレースに結合することが可能になります。
		/// <see cref="Run"/> メソッドのそれぞれの後続するフレームグループは単一のインタプリタのフレームに対応しています。
		/// </remarks>
		[SpecialName, MethodImpl(MethodImplOptions.NoInlining)]
		public void Run(InterpretedFrame frame)
		{
			while (true)
			{
				try
				{
					for (int index = frame.InstructionIndex; index < Instructions.Instructions.Length; )
						frame.InstructionIndex = index += Instructions.Instructions[index].Run(frame);
					return;
				}
				catch (Exception exception)
				{
					switch (HandleException(frame, exception))
					{
						case ExceptionHandlingResult.Rethrow: throw;
						case ExceptionHandlingResult.Continue: continue;
						case ExceptionHandlingResult.Return: return;
					}
				}
			}
		}

		ExceptionHandlingResult HandleException(InterpretedFrame frame, Exception exception)
		{
			frame.SaveTraceToException(exception);
			frame.FaultingInstruction = frame.InstructionIndex;
			ExceptionHandler handler;
			frame.InstructionIndex += GotoHandler(frame, exception, out handler);
			if (handler == null || handler.IsFault)
			{
				// finally/fault ブロックを実行:
				Run(frame);
				// finally ブロックはハンドラによって捕捉される例外をスローできる。なお、その例外は以前の例外を打ち消す:
				if (frame.InstructionIndex == RethrowOnReturn)
					return ExceptionHandlingResult.Rethrow;
				return ExceptionHandlingResult.Return;
			}
			// ThreadAbortException が CLR によって再スローされないように現在の catch にとどまる:
			var abort = exception as ThreadAbortException;
			if (abort != null)
			{
				_anyAbortException = abort;
				frame.CurrentAbortHandler = handler;
			}
			while (true)
			{
				try
				{
					for (int index = frame.InstructionIndex; index < Instructions.Instructions.Length; )
					{
						var curInstr = Instructions.Instructions[index];
						frame.InstructionIndex = index += curInstr.Run(frame);
						if (curInstr is LeaveExceptionHandlerInstruction)
							return ExceptionHandlingResult.Continue; // この例外のハンドルは終了した
					}
					if (frame.InstructionIndex == RethrowOnReturn)
						return ExceptionHandlingResult.Rethrow;
					return ExceptionHandlingResult.Return;
				}
				catch (Exception nestedException)
				{
					switch (HandleException(frame, nestedException))
					{
						case ExceptionHandlingResult.Rethrow: throw;
						case ExceptionHandlingResult.Continue: continue;
						case ExceptionHandlingResult.Return: return ExceptionHandlingResult.Return;
						default: throw Assert.Unreachable;
					}
				}
			}
		}

		enum ExceptionHandlingResult
		{
			Rethrow,
			Continue,
			Return
		}

		// Thread.CurrentThread の現在の AbortReason オブジェクトに到達するために、あらゆる ThreadAbortException インスタンスの ExceptionState プロパティを使用する必要がある
		[ThreadStatic]
		static ThreadAbortException _anyAbortException = null;

		/// <summary>要求されておりかつハンドラが <see cref="ThreadAbortException"/> を捕捉できない場合、スレッドを中止します。</summary>
		/// <param name="frame">現在命令を実行しているスタックフレームを指定します。</param>
		/// <param name="targetLabelIndex">このメソッドを呼び出した命令が遷移しようとしているラベルのインデックスを指定します。</param>
		internal static void AbortThreadIfRequested(InterpretedFrame frame, int targetLabelIndex)
		{
			var abortHandler = frame.CurrentAbortHandler;
			if (abortHandler != null && !abortHandler.IsInside(frame.Interpreter._labels[targetLabelIndex].Index))
			{
				frame.CurrentAbortHandler = null;
				var currentThread = Thread.CurrentThread;
				if ((currentThread.ThreadState & System.Threading.ThreadState.AbortRequested) != 0)
				{
					Debug.Assert(_anyAbortException != null);
					// 現在の AbortReason を保存する必要がある
					currentThread.Abort(_anyAbortException.ExceptionState);
				}
			}
		}

		/// <summary>例外がハンドラで捕捉可能である場合ハンドラにジャンプします。それ以外の場合は "return and rethrow" ラベルにジャンプします。</summary>
		/// <param name="frame">現在命令を実行しているスタックフレームを指定します。</param>
		/// <param name="exception">捕捉可能なハンドラへ移動する例外を指定します。</param>
		/// <param name="handler">例外を捕捉可能なハンドラが返されます。</param>
		/// <returns>例外ハンドラまたは "return and rethrow" ラベルへのオフセット。</returns>
		internal int GotoHandler(InterpretedFrame frame, object exception, out ExceptionHandler handler)
		{
			handler = _handlers.Where(x => x.Matches(exception.GetType(), frame.InstructionIndex)).Aggregate((ExceptionHandler)null, (x, y) => y.IsBetterThan(x) ? y : x);
			if (handler == null)
			{
				Debug.Assert(_labels[_labels.Length - 1].Index == RethrowOnReturn); // 最後のラベルは "return and rethrow" ラベル:
				return frame.VoidGoto(_labels.Length - 1);
			}
			else
				return frame.Goto(handler.LabelIndex, exception);
		}
	}
}
