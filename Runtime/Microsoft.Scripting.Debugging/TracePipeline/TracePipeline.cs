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
using System.Diagnostics;
using System.Threading;
using Microsoft.Scripting.Debugging.CompilerServices;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Debugging
{
	/// <summary>�g���[�X�Z�b�V������\���܂��B</summary>
	public sealed class TracePipeline : ITracePipeline, IDebugCallback
	{
		readonly DebugContext _debugContext;
		readonly ThreadLocal<DebugFrame> _traceFrame;
		ITraceCallback _traceCallback;
		bool _closed;

		TracePipeline(DebugContext debugContext)
		{
			_debugContext = debugContext;
			_traceFrame = new ThreadLocal<DebugFrame>();
			debugContext.DebugCallback = this;
			debugContext.DebugMode = DebugMode.FullyEnabled;
		}

		public static ITracePipeline CreateInstance(DebugContext debugContext)
		{
			ContractUtils.RequiresNotNull(debugContext, "debugContext");
			if (debugContext.DebugCallback != null)
				throw new InvalidOperationException(ErrorStrings.DebugContextAlreadyConnectedToTracePipeline);
			return new TracePipeline(debugContext);
		}

		public void Close()
		{
			VerifyNotClosed();
			_debugContext.DebugCallback = null;
			_debugContext.DebugMode = DebugMode.Disabled;
			_closed = true;
		}

		public bool CanSetNextStatement(string sourceFile, SourceSpan sourceSpan)
		{
			VerifyNotClosed();
			ContractUtils.RequiresNotNull(sourceFile, "sourceFile");
			ContractUtils.Requires(sourceSpan != SourceSpan.Invalid && sourceSpan != SourceSpan.None, ErrorStrings.InvalidSourceSpan);
			// �X���b�h�I�u�W�F�N�g��������B���݂̃X���b�h�� FrameExit �g���[�X�o�b�N���ɂ��邩�ǂ��������ׂ�B
			if (_traceFrame.Value == null)
				return false;
			return GetSequencePointIndexForSourceSpan(sourceFile, sourceSpan, _traceFrame.Value) != Int32.MaxValue;
		}

		public void SetNextStatement(string sourceFile, SourceSpan sourceSpan)
		{
			VerifyNotClosed();
			ContractUtils.RequiresNotNull(sourceFile, "sourceFile");
			ContractUtils.Requires(sourceSpan != SourceSpan.Invalid && sourceSpan != SourceSpan.None, ErrorStrings.InvalidSourceSpan);
			// �X���b�h�I�u�W�F�N�g��������
			if (_traceFrame.Value == null)
				throw new InvalidOperationException(ErrorStrings.SetNextStatementOnlyAllowedInsideTraceback);
			int sequencePointIndex = GetSequencePointIndexForSourceSpan(sourceFile, sourceSpan, _traceFrame.Value);
			if (sequencePointIndex == Int32.MaxValue)
				throw new InvalidOperationException(ErrorStrings.InvalidSourceSpan);
			_traceFrame.Value.CurrentSequencePointIndex = sequencePointIndex;
		}

		public ITraceCallback TraceCallback
		{
			get
			{
				VerifyNotClosed();
				return _traceCallback;
			}
			set
			{
				VerifyNotClosed();
				_traceCallback = value;
			}
		}

		void IDebugCallback.OnDebugEvent(TraceEventKind kind, DebugThread thread, FunctionInfo functionInfo, int sequencePointIndex, int stackDepth, object payload)
		{
			if (_traceCallback == null)
				return;
			// TODO: �R�[���o�b�N����O���X���[�����ꍇ�ǂ�����̂�? ����Ԃ��ׂ���?
			var curThread = _traceFrame.Value;
			try
			{
				if (kind == TraceEventKind.FrameExit || kind == TraceEventKind.ThreadExit)
				{
					_traceCallback.OnTraceEvent(
						kind,
						kind == TraceEventKind.FrameExit ? functionInfo.Name : null,
						null,
						SourceSpan.None,
						null,
						payload,
						functionInfo != null ? functionInfo.CustomPayload : null
					);
				}
				else
				{
					var leafFrame = thread.GetLeafFrame();
					_traceFrame.Value = leafFrame;
					Debug.Assert(sequencePointIndex >= 0 && sequencePointIndex < functionInfo.SequencePoints.Length);
					var sourceSpan = functionInfo.SequencePoints[sequencePointIndex];
					_traceCallback.OnTraceEvent(
						kind,
						functionInfo.Name,
						sourceSpan.SourceFile.Name,
						sourceSpan.ToDlrSpan(),
						() => leafFrame.GetLocalsScope(),
						payload,
						functionInfo.CustomPayload
					);
				}
			}
			finally
			{
				_traceFrame.Value = curThread;
			}
		}

		int GetSequencePointIndexForSourceSpan(string sourceFile, SourceSpan sourceSpan, DebugFrame frame)
		{
			var debugSourceFile = _debugContext.Lookup(sourceFile);
			if (debugSourceFile == null)
				return Int32.MaxValue;

			var debugSourceSpan = new DebugSourceSpan(debugSourceFile, sourceSpan);
			var leafFrameFuncInfo = frame.FunctionInfo;
			var funcInfo = debugSourceFile.LookupFunctionInfo(debugSourceSpan);

			// funcInfo �����݂̃t���[���ƈ�v���邩�𒲂ׂ�
			if (funcInfo != leafFrameFuncInfo)
				return Int32.MaxValue;

			// �Ώۂ̃V�[�P���X�|�C���g�𓾂�
			return debugSourceSpan.GetSequencePointIndex(funcInfo);
		}

		void VerifyNotClosed()
		{
			if (_closed)
				throw new InvalidOperationException(ErrorStrings.ITracePipelineClosed);
		}
	}
}
