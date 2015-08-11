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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Scripting.Utils;
using MSAst = System.Linq.Expressions;

namespace Microsoft.Scripting.Debugging.CompilerServices
{
	/// <summary>トレースバックでのコードの性能測定のためのサービスをコンパイラに提供します。</summary>
	public sealed partial class DebugContext
	{
		DebugMode _debugMode;
		readonly ThreadLocal<DebugThread> _thread;
		DebugThread _cachedThread;
		readonly Dictionary<string, DebugSourceFile> _sourceFiles;

		DebugContext(IDebugThreadFactory runtimeThreadFactory)
		{
			_thread = new ThreadLocal<DebugThread>(true);
			_sourceFiles = new Dictionary<string, DebugSourceFile>(StringComparer.OrdinalIgnoreCase);
			ThreadFactory = runtimeThreadFactory;
		}

		/// <summary><see cref="DebugContext"/> の新しいインスタンスを作成します。</summary>
		public static DebugContext CreateInstance() { return new DebugContext(new DefaultDebugThreadFactory()); }

		internal static DebugContext CreateInstance(IDebugThreadFactory runtimeThreadFactory) { return new DebugContext(runtimeThreadFactory); }

		/// <summary><see cref="System.Linq.Expressions.LambdaExpression"/> をデバッグ可能な <see cref="System.Linq.Expressions.LambdaExpression"/> に変形します。</summary>
		public MSAst.LambdaExpression TransformLambda(MSAst.LambdaExpression lambda, DebugLambdaInfo lambdaInfo)
		{
			ContractUtils.RequiresNotNull(lambda, "lambda");
			ContractUtils.RequiresNotNull(lambdaInfo, "lambdaInfo");
			return new DebuggableLambdaBuilder(this, lambdaInfo).Transform(lambda);
		}

		/// <summary><see cref="System.Linq.Expressions.LambdaExpression"/> をデバッグ可能な <see cref="System.Linq.Expressions.LambdaExpression"/> に変形します。</summary>
		public MSAst.LambdaExpression TransformLambda(MSAst.LambdaExpression lambda)
		{
			ContractUtils.RequiresNotNull(lambda, "lambda");
			return new DebuggableLambdaBuilder(this, new DebugLambdaInfo(null, null, false, null, null, null)).Transform(lambda);
		}

		/// <summary><see cref="DebugContext"/> によって維持されるソースファイルに関連付けられた状態をリセットします。</summary>
		public void ResetSourceFile(string sourceFileName)
		{
			ContractUtils.RequiresNotNull(sourceFileName, "sourceFileName");
			_sourceFiles.Remove(sourceFileName);
		}

		[Obsolete("do not call this property", true)]
		public int Mode { get { return (int)_debugMode; } }

		internal DebugMode DebugMode
		{
			get { return _debugMode; }
			set
			{
				_debugMode = value;
				// すべてのソースファイルに対するデバッグモードも更新する
				foreach (DebugSourceFile file in _sourceFiles.Values)
					file.DebugMode = value;
			}
		}

		internal DebugSourceFile Lookup(string sourceFile)
		{
			DebugSourceFile debugSourceFile;
			if (_sourceFiles.TryGetValue(sourceFile, out debugSourceFile))
				return debugSourceFile;
			return null;
		}

		// TODO: 中断モードであるスレッドのみを返すようにする
		/// <summary>スレッドを取得します。</summary>
		internal IEnumerable<DebugThread> Threads { get { return _thread.Values.Where(x => x != null && x.FrameCount > 0); } }

		/// <summary>デバッグ時のフックを取得または設定します。</summary>
		internal IDebugCallback DebugCallback { get; set; }

		internal DebugSourceFile GetDebugSourceFile(string sourceFile)
		{
			DebugSourceFile file;
			lock (((ICollection)_sourceFiles).SyncRoot)
			{
				if (!_sourceFiles.TryGetValue(sourceFile, out file))
				{
					file = new DebugSourceFile(sourceFile, _debugMode);
					_sourceFiles.Add(sourceFile, file);
				}
			}

			return file;
		}

		internal static FunctionInfo CreateFunctionInfo(Delegate generatorFactory, string name, DebugSourceSpan[] locationSpanMap, VariableInfo[][] scopedVariables, IList<VariableInfo> variables, object customPayload)
		{
			var funcInfo = new FunctionInfo(generatorFactory, name, locationSpanMap, scopedVariables, variables, customPayload);
			foreach (var sourceSpan in locationSpanMap)
			{
				lock (sourceSpan.SourceFile.FunctionInfoMap)
					sourceSpan.SourceFile.FunctionInfoMap[sourceSpan] = funcInfo;
			}
			return funcInfo;
		}

		internal DebugFrame CreateFrameForGenerator(FunctionInfo func) { return new DebugFrame(GetCurrentThread(), func); }

		internal void DispatchDebugEvent(DebugThread thread, int debugMarker, TraceEventKind eventKind, object payload)
		{
			DebugFrame leafFrame = null;
			bool hasFrameObject = false;

			FunctionInfo functionInfo;
			int stackDepth;
			if (eventKind != TraceEventKind.ThreadExit)
				functionInfo = thread.GetLeafFrameFunctionInfo(out stackDepth);
			else
			{
				stackDepth = Int32.MaxValue;
				functionInfo = null;
			}

			if (eventKind == TraceEventKind.Exception || eventKind == TraceEventKind.ExceptionUnwind)
				thread.ThrownException = (Exception)payload;
			thread.IsInTraceback = true;

			try
			{
				// イベントを発行
				var traceHook = DebugCallback;
				if (traceHook != null)
					traceHook.OnDebugEvent(eventKind, thread, functionInfo, debugMarker, stackDepth, payload);

				// フレームオブジェクトがトレースバック後に作成されたか調べる。作成されてた場合、再割り当てが必要かを調べる必要がある。
				hasFrameObject = thread.TryGetLeafFrame(ref leafFrame);
				if (hasFrameObject)
				{
					Debug.Assert(!leafFrame.InGeneratorLoop || (leafFrame.InGeneratorLoop && !leafFrame.ForceSwitchToGeneratorLoop));
					if (leafFrame.ForceSwitchToGeneratorLoop && !leafFrame.InGeneratorLoop)
						throw new ForceToGeneratorLoopException();
				}
			}
			finally
			{
				if (hasFrameObject)
					leafFrame.IsInTraceback = false;

				thread.IsInTraceback = false;
				thread.ThrownException = null;
			}
		}

		internal IDebugThreadFactory ThreadFactory { get; private set; }

		internal DebugThread GetCurrentThread()
		{
			var thread = _cachedThread;
			if (thread == null || thread.ManagedThread != Thread.CurrentThread)
			{
				if ((thread = _thread.Value) == null)
					_thread.Value = thread = ThreadFactory.CreateDebugThread(this);
				Interlocked.Exchange(ref _cachedThread, thread);
			}
			return thread;
		}
	}
}
