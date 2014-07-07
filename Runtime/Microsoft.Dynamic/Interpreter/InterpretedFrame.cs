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
	/// <summary>�C���^�v���^�ɂ���Ď��s�����v���V�[�W���ɑ΂���X�^�b�N�t���[����\���܂��B</summary>
	public sealed class InterpretedFrame
	{
		[ThreadStatic]
		static StrongBox<InterpretedFrame> threadedCurrentFrame;

		/// <summary>���݂̃X���b�h�Ŏ��s����Ă���v���V�[�W���̃X�^�b�N�t���[�����擾���܂��B</summary>
		public static InterpretedFrame CurrentFrame { get { return (threadedCurrentFrame ?? (threadedCurrentFrame = new StrongBox<InterpretedFrame>())).Value; } }

		/// <summary>���̃X�^�b�N�t���[���̃v���V�[�W�������s���Ă���C���^�v���^��\���܂��B</summary>
		internal readonly Interpreter Interpreter;

		int[] _continuations;
		int _continuationIndex;
		int _pendingContinuation;
		object _pendingValue;

		/// <summary>���̃X�^�b�N�t���[���̃f�[�^�̈��\���܂��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
		public readonly object[] Data;

		/// <summary>���̃X�^�b�N�t���[���ɒ񋟂��ꂽ�N���[�W������������f�[�^��\���܂��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
		public readonly StrongBox<object>[] Closure;

		/// <summary>���̃X�^�b�N�t���[���̃f�[�^�̈�Ŏ��Ƀf�[�^���v�b�V�������C���f�b�N�X��\���܂��B</summary>
		public int StackIndex;
		/// <summary>���̃X�^�b�N�t���[���Ŏ��Ɏ��s����閽�߂������C���f�b�N�X��\���܂��B</summary>
		public int InstructionIndex;
		// TODO: remove
		/// <summary>���̃X�^�b�N�t���[���ōŋߎ��s�������߂������C���f�b�N�X��\���܂��B</summary>
		public int FaultingInstruction;
		// ThreadAbortException ����͂����R�[�h���甭�������ۂɁA����͂��̗�O��ߑ�����ŏ��̃t���[���Ȃ�܂��B
		// �߂�ۂɂ��̃n���h�����܂ނǂ̃n���h�������݂̃X���b�h���ēx���~���܂���B
		/// <summary>���̃X�^�b�N�t���[���Ɋ֘A�t����ꂽ <see cref="System.Threading.ThreadAbortException"/> �ɑ΂����O�n���h����\���܂��B</summary>
		public ExceptionHandler CurrentAbortHandler;

		/// <summary>���ۂɎ��s���s���C���^�v���^�ƊO���̃X�R�[�v����n�����f�[�^���g�p���āA<see cref="Microsoft.Scripting.Interpreter.InterpretedFrame"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="interpreter">���ۂɂ��̃X�^�b�N�t���[���̃v���V�[�W�������s����C���^�v���^���w�肵�܂��B</param>
		/// <param name="closure">���̃X�^�b�N�t���[���ɊO���̃X�R�[�v����񋟂��ꂽ�f�[�^���w�肵�܂��B</param>
		internal InterpretedFrame(Interpreter interpreter, StrongBox<object>[] closure)
		{
			Interpreter = interpreter;
			StackIndex = interpreter.LocalCount;
			Data = new object[StackIndex + interpreter.Instructions.MaxStackDepth];
			if (interpreter.Instructions.MaxContinuationDepth > 0)
				_continuations = new int[interpreter.Instructions.MaxContinuationDepth];
			Closure = closure;
		}

		/// <summary>�w�肳�ꂽ���߃C���f�b�N�X�Ɋ֘A�t����ꂽ�f�o�b�O�����擾���܂��B</summary>
		/// <param name="instructionIndex">�f�o�b�O�����擾���閽�߂������C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>���߂Ɋ֘A�t����ꂽ�f�o�b�O���B</returns>
		public DebugInfo GetDebugInfo(int instructionIndex) { return DebugInfo.GetMatchingDebugInfo(Interpreter._debugInfos, instructionIndex); }

		/// <summary>���̃X�^�b�N�t���[���Ŏ��s���Ă���R�[�h�̊�ɂȂ��������_����\�� <see cref="LambdaExpression"/> ���擾���܂��B</summary>
		public LambdaExpression Lambda { get { return Interpreter._lambda; } }

		/// <summary>���̃X�^�b�N�t���[���̃f�[�^�̈�Ɏw�肳�ꂽ�f�[�^���v�b�V�����܂��B</summary>
		/// <param name="value">�v�b�V������f�[�^���w�肵�܂��B</param>
		public void Push(object value) { Data[StackIndex++] = value; }

		/// <summary>���̃X�^�b�N�t���[���̃f�[�^�̈�Ɏw�肳�ꂽ�u�[���l���v�b�V�����܂��B</summary>
		/// <param name="value">�v�b�V������u�[���l���w�肵�܂��B</param>
		public void Push(bool value) { Data[StackIndex++] = value ? ScriptingRuntimeHelpers.True : ScriptingRuntimeHelpers.False; }

		/// <summary>���̃X�^�b�N�t���[���̃f�[�^�̈�Ɏw�肳�ꂽ 32 �r�b�g�����t���������v�b�V�����܂��B</summary>
		/// <param name="value">�v�b�V������ 32 �r�b�g�����t���������w�肵�܂��B</param>
		public void Push(int value) { Data[StackIndex++] = ScriptingRuntimeHelpers.Int32ToObject(value); }

		/// <summary>���̃X�^�b�N�t���[���̃f�[�^�̈悩��f�[�^���|�b�v���܂��B</summary>
		/// <returns>�|�b�v���ꂽ�f�[�^�B</returns>
		public object Pop() { return Data[--StackIndex]; }

		/// <summary>���̃X�^�b�N�t���[���̃X�^�b�N�̐[�����w�肳�ꂽ�l�ɐݒ肵�܂��B</summary>
		/// <param name="depth">�ݒ肷��X�^�b�N�̐[���������l���w�肵�܂��B</param>
		internal void SetStackDepth(int depth) { StackIndex = Interpreter.LocalCount + depth; }

		/// <summary>���̃X�^�b�N�t���[���̃f�[�^�̈悩�玟�Ƀ|�b�v�����l�����ۂɂ̓|�b�v�����ɕԂ��܂��B</summary>
		/// <returns>���Ƀ|�b�v�����l�B</returns>
		public object Peek() { return Data[StackIndex - 1]; }

		/// <summary>���̃X�^�b�N�t���[���ɑΉ�����v���V�[�W�����Ăяo�����v���V�[�W���ɑ΂���X�^�b�N�t���[�����擾���܂��B</summary>
		public InterpretedFrame Parent { get; private set; }

		/// <summary>�w�肳�ꂽ�X�^�b�N�t���[�����C���^�v���^�ɂ���Ď��s����Ă��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="frame">���ׂ�X�^�b�N�t���[�����w�肵�܂��B</param>
		/// <returns>�X�^�b�N�t���[�����C���^�v���^�ɂ���Ď��s����Ă����ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool IsInterpretedFrame(StackFrame frame)
		{
			ContractUtils.RequiresNotNull(frame, "frame");
			var method = frame.GetMethod();
			return method.DeclaringType == typeof(Interpreter) && method.Name == "Run";
		}

		/// <summary>�P��� CLR �t���[�����P��̃C���^�v���^�ɂ��t���[�����\���ł���悤�ɁA�d������ CLR �t���[������菜���܂��B</summary>
		/// <param name="stackTrace">�d�����܂�ł���\���̂��� <see cref="StackFrame"/> �̃V�[�P���X���w�肵�܂��B</param>
		/// <returns>�d������菜���ꂽ <see cref="StackFrame"/> �̃V�[�P���X�B</returns>
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

		/// <summary>���̃t���[������т��̃t���[�����Ăяo�������ׂẴt���[���ɑ΂���X�^�b�N�g���[�X�p�̃f�o�b�O�����擾���܂��B</summary>
		/// <returns>���ׂẴt���[���ɑ΂���X�^�b�N�g���[�X�p�̃f�o�b�O���̃V�[�P���X�B</returns>
		public IEnumerable<InterpretedFrameInfo> GetStackTraceDebugInfo()
		{
			for (var frame = this; frame != null; frame = frame.Parent)
				yield return new InterpretedFrameInfo(frame.Lambda.Name, frame.GetDebugInfo(frame.InstructionIndex));
		}

		/// <summary>�w�肳�ꂽ��O�ɂ��̃X�^�b�N�t���[���Ɋւ���f�o�b�O�����i�[���܂��B</summary>
		/// <param name="exception">�f�o�b�O�����i�[�����O���w�肵�܂��B</param>
		internal void SaveTraceToException(Exception exception)
		{
			if (exception.Data[typeof(InterpretedFrameInfo)] == null)
				exception.Data[typeof(InterpretedFrameInfo)] = new List<InterpretedFrameInfo>(GetStackTraceDebugInfo()).ToArray();
		}

		/// <summary>�w�肳�ꂽ��O�ɃX�^�b�N�g���[�X��񂪑��݂��Ă���΂��̏����擾���܂��B</summary>
		/// <param name="exception">�X�^�b�N�g���[�X�����擾�����O���w�肵�܂��B</param>
		/// <returns>�擾���ꂽ�X�^�b�N�g���[�X���B</returns>
		public static InterpretedFrameInfo[] GetExceptionStackTrace(Exception exception) { return exception.Data[typeof(InterpretedFrameInfo)] as InterpretedFrameInfo[]; }

#if DEBUG
		/// <summary>���̃t���[������т��̃t���[�����Ăяo�������ׂẴt���[���ɑ΂���g���[�X�����擾���܂��B</summary>
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

		/// <summary>���̃X�^�b�N�t���[���̎��s�̊J�n�������A���݂̃X���b�h�Ŏ��s����Ă���t���[�������X�V���܂��B</summary>
		/// <returns>�X�^�b�N�t���[������̎��s���I������ꍇ�Ɏg�p������B</returns>
		internal StrongBox<InterpretedFrame> Enter()
		{
			if (threadedCurrentFrame == null)
				threadedCurrentFrame = new StrongBox<InterpretedFrame>();
			Parent = threadedCurrentFrame.Value;
			threadedCurrentFrame.Value = this;
			return threadedCurrentFrame;
		}

		/// <summary>���̃X�^�b�N�t���[���̎��s�̏I���������A���݂̃X���b�h�Ŏ��s����Ă���t���[�������X�V���܂��B</summary>
		/// <param name="currentFrame"><see cref="InterpretedFrame.Enter"/> �ŕԂ��ꂽ�����w�肵�܂��B</param>
		internal void Leave(StrongBox<InterpretedFrame> currentFrame) { currentFrame.Value = Parent; }

		/// <summary>���̃X�^�b�N�t���[���ɍŌ�Ƀv�b�V�������p���Ɋւ�������폜���܂��B</summary>
		public void RemoveContinuation() { _continuationIndex--; }

		/// <summary>�w�肳�ꂽ�p�������̃X�^�b�N�t���[���Ƀv�b�V�����܂��B</summary>
		/// <param name="continuation">�v�b�V������p�����s�����x���̃C���f�b�N�X���w�肵�܂��B</param>
		public void PushContinuation(int continuation) { _continuations[_continuationIndex++] = continuation; }

		/// <summary>���̃X�^�b�N�t���[���ɍŌ�Ƀv�b�V�����ꂽ�p���ɏ���������܂��B</summary>
		/// <returns>���������閽�߂ɑ΂���I�t�Z�b�g�B</returns>
		public int YieldToCurrentContinuation()
		{
			var target = Interpreter._labels[_continuations[_continuationIndex - 1]];
			SetStackDepth(target.StackDepth);
			return target.Index - InstructionIndex;
		}

		/// <summary>���̃X�^�b�N�t���[���ōŌ�Ƀv�b�V�����ꂽ�p���܂��͕ۗ����̌p���ɏ���������܂��B</summary>
		/// <returns>���������閽�߂ɑ΂���I�t�Z�b�g�B</returns>
		public int YieldToPendingContinuation()
		{
			Debug.Assert(_pendingContinuation >= 0);
			var pendingTarget = Interpreter._labels[_pendingContinuation];
			// ���݂̌p���͂�荂���D�揇�ʂ����� (continuationIndex �͌��݂̌p���̐[��):
			if (pendingTarget.ContinuationStackDepth < _continuationIndex)
				return YieldToCurrentContinuation();
			SetStackDepth(pendingTarget.StackDepth);
			if (_pendingValue != Interpreter.NoValue)
				Data[StackIndex - 1] = _pendingValue;
			return pendingTarget.Index - InstructionIndex;
		}

		/// <summary>�ۗ����̌p�����f�[�^�̈�Ƀv�b�V�����܂��B���̑���� 2 �̐V�����u���b�N���쐬���܂��B</summary>
		internal void PushPendingContinuation()
		{
			Push(_pendingContinuation);
			Push(_pendingValue);
#if DEBUG
			_pendingContinuation = -1;
#endif
		}

		/// <summary>�ۗ����̌p�����f�[�^�̈悩��|�b�v���܂��B���̑���� 2 �̃u���b�N������܂��B</summary>
		internal void PopPendingContinuation()
		{
			_pendingValue = Pop();
			_pendingContinuation = (int)Pop();
		}

		static MethodInfo _Goto;
		static MethodInfo _VoidGoto;

		/// <summary><see cref="InterpretedFrame.Goto"/> ��\�� <see cref="MethodInfo"/> ���擾���܂��B</summary>
		internal static MethodInfo GotoMethod { get { return _Goto ?? (_Goto = typeof(InterpretedFrame).GetMethod("Goto")); } }

		/// <summary><see cref="InterpretedFrame.VoidGoto"/> ��\�� <see cref="MethodInfo"/> ���擾���܂��B</summary>
		internal static MethodInfo VoidGotoMethod { get { return _VoidGoto ?? (_VoidGoto = typeof(InterpretedFrame).GetMethod("VoidGoto")); } }

		/// <summary>�l��n�����Ɏw�肳�ꂽ�C���f�b�N�X�̃��x���ɃW�����v���܂��B</summary>
		/// <param name="labelIndex">�W�����v��̃��x���������C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�W�����v��̃��x���Ώۂ̃I�t�Z�b�g�B</returns>
		public int VoidGoto(int labelIndex) { return Goto(labelIndex, Interpreter.NoValue); }

		/// <summary>�l��n���Ďw�肳�ꂽ�C���f�b�N�X�̃��x���ɃW�����v���܂��B</summary>
		/// <param name="labelIndex">�W�����v��̃��x���������C���f�b�N�X���w�肵�܂��B</param>
		/// <param name="value">�W�����v�̍ۂɓn���l���w�肵�܂��B</param>
		/// <returns>�W�����v��̃��x���Ώۂ̃I�t�Z�b�g�B</returns>
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
