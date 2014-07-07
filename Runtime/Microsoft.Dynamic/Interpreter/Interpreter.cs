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
	/// IL �ւ̃R���p�C���� JIT �ł̌Ăяo���̕K�v�Ȃ����c���[�����s�ł���P���� forth �`���̃X�^�b�N�}�V����\���܂��B
	/// ����͔��ɍ����ȃR���p�C�����Ԃƈ������s���p�t�H�[�}���X�Ƃ̃g���[�h�I�t�ł��B
	/// ���Ȃ��񐔂������s����Ȃ��R�[�h�ɑ΂��ẮA����͗ǂ��o�����X�ƂȂ�܂��B
	/// 
	/// �C���^�v���^�̃��C�����[�v�� <see cref="Interpreter.Run"/> ���\�b�h�ɑ��݂��܂��B
	/// </summary>
	sealed class Interpreter
	{
		/// <summary>�l�����݂��Ȃ����Ƃ������܂��B</summary>
		internal static readonly object NoValue = new object();
		/// <summary>���䂪�߂������ɗ�O���ăX���[���閽�߃C���f�b�N�X��\���܂��B</summary>
		internal const int RethrowOnReturn = int.MaxValue;

		// 0: �����R���p�C��, ��: ����
		internal readonly int _compilationThreshold;

		internal readonly object[] _objects;
		internal readonly RuntimeLabel[] _labels;

		internal readonly LambdaExpression _lambda;
		readonly ExceptionHandler[] _handlers;
		internal readonly DebugInfo[] _debugInfos;

		/// <summary>�w�肳�ꂽ�������g�p���āA<see cref="Microsoft.Scripting.Interpreter.Interpreter"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="lambda">�C���^�v���^�ɂ���Ď��s����郉���_����\�� <see cref="LambdaExpression"/> ���w�肵�܂��B</param>
		/// <param name="locals">���[�J���ϐ���\�� <see cref="LocalVariables"/> ���w�肵�܂��B</param>
		/// <param name="labelMapping"><see cref="LabelTarget"/> ���� <see cref="BranchLabel"/> �ւ̃}�b�s���O���w�肵�܂��B</param>
		/// <param name="instructions">���ۂɎ��s���閽�߂�\�� <see cref="InstructionArray"/> ���w�肵�܂��B</param>
		/// <param name="handlers">��O�n���h����\�� <see cref="ExceptionHandler"/> ���w�肵�܂��B</param>
		/// <param name="debugInfos">�f�o�b�O�����w�肵�܂��B</param>
		/// <param name="compilationThreshold">�C���^�v���^�ɂ���Ď��s�ł���ő�񐔂��w�肵�܂��B���̐��l�ȏ���s���ꂽ�ꍇ�����_���̓R���p�C������܂��B</param>
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

		/// <summary>�N���[�W���̂��߂Ɏg�p�����ϐ��̐����擾���܂��B</summary>
		internal int ClosureSize { get { return ClosureVariables == null ? 0 : ClosureVariables.Count; } }

		/// <summary>���[�J���ϐ��̐����擾���܂��B</summary>
		internal int LocalCount { get; private set; }

		/// <summary>�R���p�C���𓯊��I�Ɏ��s���邩�ǂ����������l���擾���܂��B</summary>
		internal bool CompileSynchronously { get { return _compilationThreshold <= 1; } }

		/// <summary>�C���^�v���^�����s���閽�߂��i�[���� <see cref="InstructionArray"/> ���擾���܂��B</summary>
		internal InstructionArray Instructions { get; private set; }

		/// <summary>�N���[�W���̂��߂Ɏg�p�����ϐ����擾���܂��B</summary>
		internal Dictionary<ParameterExpression, LocalVariable> ClosureVariables { get; private set; }

		/// <summary><see cref="LabelTarget"/> ���� <see cref="BranchLabel"/> �ɑ΂���}�b�s���O���擾���܂��B</summary>
		internal Dictionary<LabelTarget, BranchLabel> LabelMapping { get; private set; }

		/// <summary>�w�肳�ꂽ�X�^�b�N�t���[���Ŗ��߂����s���܂��B</summary>
		/// <param name="frame">���߂����s����X�^�b�N�t���[�����w�肵�܂��B</param>
		/// <remarks>
		/// �C���^�v���^�̃X�^�b�N�t���[���͂��̃��\�b�h�̂��ꂼ��� CLR �t���[�����`�F�C���̃C���^�v���^�̃X�^�b�N�t���[���ɑΉ�����悤�� Parent �̎Q�Ƃɂ���ĘA������܂��B
		/// ���̂��߃C���^�v���^�̃t���[�������̃��\�b�h�̃t���[���ɑ����邱�ƂŁACLR �X�^�b�N�g���[�X���C���^�v���^�̃X�^�b�N�g���[�X�Ɍ������邱�Ƃ��\�ɂȂ�܂��B
		/// <see cref="Run"/> ���\�b�h�̂��ꂼ��̌㑱����t���[���O���[�v�͒P��̃C���^�v���^�̃t���[���ɑΉ����Ă��܂��B
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
				// finally/fault �u���b�N�����s:
				Run(frame);
				// finally �u���b�N�̓n���h���ɂ���ĕߑ�������O���X���[�ł���B�Ȃ��A���̗�O�͈ȑO�̗�O��ł�����:
				if (frame.InstructionIndex == RethrowOnReturn)
					return ExceptionHandlingResult.Rethrow;
				return ExceptionHandlingResult.Return;
			}
			// ThreadAbortException �� CLR �ɂ���čăX���[����Ȃ��悤�Ɍ��݂� catch �ɂƂǂ܂�:
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
							return ExceptionHandlingResult.Continue; // ���̗�O�̃n���h���͏I������
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

		// Thread.CurrentThread �̌��݂� AbortReason �I�u�W�F�N�g�ɓ��B���邽�߂ɁA������ ThreadAbortException �C���X�^���X�� ExceptionState �v���p�e�B���g�p����K�v������
		[ThreadStatic]
		static ThreadAbortException _anyAbortException = null;

		/// <summary>�v������Ă��肩�n���h���� <see cref="ThreadAbortException"/> ��ߑ��ł��Ȃ��ꍇ�A�X���b�h�𒆎~���܂��B</summary>
		/// <param name="frame">���ݖ��߂����s���Ă���X�^�b�N�t���[�����w�肵�܂��B</param>
		/// <param name="targetLabelIndex">���̃��\�b�h���Ăяo�������߂��J�ڂ��悤�Ƃ��Ă��郉�x���̃C���f�b�N�X���w�肵�܂��B</param>
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
					// ���݂� AbortReason ��ۑ�����K�v������
					currentThread.Abort(_anyAbortException.ExceptionState);
				}
			}
		}

		/// <summary>��O���n���h���ŕߑ��\�ł���ꍇ�n���h���ɃW�����v���܂��B����ȊO�̏ꍇ�� "return and rethrow" ���x���ɃW�����v���܂��B</summary>
		/// <param name="frame">���ݖ��߂����s���Ă���X�^�b�N�t���[�����w�肵�܂��B</param>
		/// <param name="exception">�ߑ��\�ȃn���h���ֈړ������O���w�肵�܂��B</param>
		/// <param name="handler">��O��ߑ��\�ȃn���h�����Ԃ���܂��B</param>
		/// <returns>��O�n���h���܂��� "return and rethrow" ���x���ւ̃I�t�Z�b�g�B</returns>
		internal int GotoHandler(InterpretedFrame frame, object exception, out ExceptionHandler handler)
		{
			handler = _handlers.Where(x => x.Matches(exception.GetType(), frame.InstructionIndex)).Aggregate((ExceptionHandler)null, (x, y) => y.IsBetterThan(x) ? y : x);
			if (handler == null)
			{
				Debug.Assert(_labels[_labels.Length - 1].Index == RethrowOnReturn); // �Ō�̃��x���� "return and rethrow" ���x��:
				return frame.VoidGoto(_labels.Length - 1);
			}
			else
				return frame.Goto(handler.LabelIndex, exception);
		}
	}
}
