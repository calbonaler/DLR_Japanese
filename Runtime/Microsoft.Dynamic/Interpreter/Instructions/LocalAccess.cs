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
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>�l���Q�Ƃɒu�������邱�Ƃ��ł��閽�߂�\���܂��B</summary>
	interface IBoxableInstruction
	{
		/// <summary>�w�肳�ꂽ�C���f�b�N�X�����߂��ΏۂƂ���C���f�b�N�X�ƈ�v�����ꍇ�ɒl���Q�Ƃɒu�����������߂��擾���܂��B</summary>
		/// <param name="index">���߂��ΏۂƂ���C���f�b�N�X�ł��邩�ǂ����𒲂ׂ�C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�C���f�b�N�X�����߂��ΏۂƂ���C���f�b�N�X�ƈ�v�����ꍇ�͒l���Q�Ƃɒu�����������߁B����ȊO�̏ꍇ�� <c>null</c>�B</returns>
		Instruction BoxIfIndexMatches(int index);
	}

	/// <summary>���[�J���ϐ��ɃA�N�Z�X���閽�߂̊�{�N���X��\���܂��B</summary>
	abstract class LocalAccessInstruction : Instruction
	{
		/// <summary>�A�N�Z�X���郍�[�J���ϐ����w�肵�āA<see cref="Microsoft.Scripting.Interpreter.LocalAccessInstruction"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="index">�A�N�Z�X���郍�[�J���ϐ��̃C���f�b�N�X���w�肵�܂��B</param>
		protected LocalAccessInstruction(int index) { Index = index; }
		
		/// <summary>�A�N�Z�X���郍�[�J���ϐ��������C���f�b�N�X���擾���܂��B</summary>
		internal int Index { get; private set; }

		/// <summary>���̃I�u�W�F�N�g�̃f�o�b�O�p������\�����擾���܂��B</summary>
		/// <param name="instructionIndex">���̖��߂̖��߃C���f�b�N�X���w�肵�܂��B</param>
		/// <param name="cookie">�f�o�b�O�p Cookie ���w�肵�܂��B</param>
		/// <param name="labelIndexer">���x����\���C���f�b�N�X���烉�x���̑J�ڐ�C���f�b�N�X���擾����f���Q�[�g���w�肵�܂��B</param>
		/// <param name="objects">�f�o�b�O�p Cookie �̃��X�g���w�肵�܂��B</param>
		/// <returns>�f�o�b�O�p������\���B</returns>
		public override string ToDebugString(int instructionIndex, object cookie, Func<int, int> labelIndexer, IList<object> objects) { return cookie == null ? InstructionName + "(" + Index + ")" : InstructionName + "(" + cookie + ": " + Index + ")"; }
	}

	/// <summary>�w�肳�ꂽ���[�J���ϐ���]���X�^�b�N�ɓǂݍ��ޖ��߂�\���܂��B</summary>
	sealed class LoadLocalInstruction : LocalAccessInstruction, IBoxableInstruction
	{
		/// <summary>�ǂݍ��ރ��[�J���ϐ����g�p���āA<see cref="Microsoft.Scripting.Interpreter.LoadLocalInstruction"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="index">�ǂݍ��ރ��[�J���ϐ��������C���f�b�N�X���w�肵�܂��B</param>
		internal LoadLocalInstruction(int index) : base(index) { }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Push(frame.Data[Index]);
			return +1;
		}

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�����߂��ΏۂƂ���C���f�b�N�X�ƈ�v�����ꍇ�ɒl���Q�Ƃɒu�����������߂��擾���܂��B</summary>
		/// <param name="index">���߂��ΏۂƂ���C���f�b�N�X�ł��邩�ǂ����𒲂ׂ�C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�C���f�b�N�X�����߂��ΏۂƂ���C���f�b�N�X�ƈ�v�����ꍇ�͒l���Q�Ƃɒu�����������߁B����ȊO�̏ꍇ�� <c>null</c>�B</returns>
		public Instruction BoxIfIndexMatches(int index) { return index == Index ? InstructionList.LoadLocalBoxed(index) : null; }
	}

	/// <summary>�w�肳�ꂽ���[�J���ϐ����Q�Ƃ���l��]���X�^�b�N�ɓǂݍ��ޖ��߂�\���܂��B</summary>
	sealed class LoadLocalBoxedInstruction : LocalAccessInstruction
	{
		/// <summary>�Q�Ƃ���l��ǂݍ��ރ��[�J���ϐ����g�p���āA<see cref="Microsoft.Scripting.Interpreter.LoadLocalBoxedInstruction"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="index">�Q�Ƃ���l��ǂݍ��ރ��[�J���ϐ��������C���f�b�N�X���w�肵�܂��B</param>
		internal LoadLocalBoxedInstruction(int index) : base(index) { }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Push(((StrongBox<object>)frame.Data[Index]).Value);
			return +1;
		}
	}

	/// <summary>�w�肳�ꂽ���[�J���ϐ��̒l���N���[�W������]���X�^�b�N�ɓǂݍ��ޖ��߂�\���܂��B</summary>
	sealed class LoadLocalFromClosureInstruction : LocalAccessInstruction
	{
		/// <summary>�ǂݍ��ރ��[�J���ϐ����g�p���āA<see cref="Microsoft.Scripting.Interpreter.LoadLocalFromClosureInstruction"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="index">�N���[�W������l��ǂݍ��ރ��[�J���ϐ��̃C���f�b�N�X���w�肵�܂��B</param>
		internal LoadLocalFromClosureInstruction(int index) : base(index) { }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Push(frame.Closure[Index].Value);
			return +1;
		}
	}

	/// <summary>�w�肳�ꂽ���[�J���ϐ��̎Q�Ƃ��N���[�W������]���X�^�b�N�ɓǂݍ��ޖ��߂�\���܂��B</summary>
	sealed class LoadLocalFromClosureBoxedInstruction : LocalAccessInstruction
	{
		/// <summary>�ǂݍ��ރ��[�J���ϐ����g�p���āA<see cref="Microsoft.Scripting.Interpreter.LoadLocalFromClosureBoxedInstruction"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="index">�N���[�W������Q�Ƃ�ǂݍ��ރ��[�J���ϐ��̃C���f�b�N�X���w�肵�܂��B</param>
		internal LoadLocalFromClosureBoxedInstruction(int index) : base(index) { }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Push(frame.Closure[Index]);
			return +1;
		}
	}

	/// <summary>�w�肳�ꂽ���[�J���ϐ��ɒl��������Ɋ��蓖�Ă閽�߂�\���܂��B</summary>
	sealed class AssignLocalInstruction : LocalAccessInstruction, IBoxableInstruction
	{
		/// <summary>�l�����蓖�Ă郍�[�J���ϐ����g�p���āA<see cref="Microsoft.Scripting.Interpreter.AssignLocalInstruction"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="index">�l�����蓖�Ă郍�[�J���ϐ��������C���f�b�N�X���w�肵�܂��B</param>
		internal AssignLocalInstruction(int index) : base(index) { }

		/// <summary>���̖��߂ŏ�����X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Data[Index] = frame.Peek();
			return +1;
		}

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�����߂��ΏۂƂ���C���f�b�N�X�ƈ�v�����ꍇ�ɒl���Q�Ƃɒu�����������߂��擾���܂��B</summary>
		/// <param name="index">���߂��ΏۂƂ���C���f�b�N�X�ł��邩�ǂ����𒲂ׂ�C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�C���f�b�N�X�����߂��ΏۂƂ���C���f�b�N�X�ƈ�v�����ꍇ�͒l���Q�Ƃɒu�����������߁B����ȊO�̏ꍇ�� <c>null</c>�B</returns>
		public Instruction BoxIfIndexMatches(int index) { return index == Index ? InstructionList.AssignLocalBoxed(index) : null; }
	}

	/// <summary>�w�肳�ꂽ���[�J���ϐ��ɒl���i�[���閽�߂�\���܂��B</summary>
	sealed class StoreLocalInstruction : LocalAccessInstruction, IBoxableInstruction
	{
		/// <summary>�l���i�[���郍�[�J���ϐ����g�p���āA<see cref="Microsoft.Scripting.Interpreter.StoreLocalInstruction"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="index">�l���i�[���郍�[�J���ϐ��������C���f�b�N�X���w�肵�܂��B</param>
		internal StoreLocalInstruction(int index) : base(index) { }

		/// <summary>���̖��߂ŏ�����X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Data[Index] = frame.Pop();
			return +1;
		}

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�����߂��ΏۂƂ���C���f�b�N�X�ƈ�v�����ꍇ�ɒl���Q�Ƃɒu�����������߂��擾���܂��B</summary>
		/// <param name="index">���߂��ΏۂƂ���C���f�b�N�X�ł��邩�ǂ����𒲂ׂ�C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�C���f�b�N�X�����߂��ΏۂƂ���C���f�b�N�X�ƈ�v�����ꍇ�͒l���Q�Ƃɒu�����������߁B����ȊO�̏ꍇ�� <c>null</c>�B</returns>
		public Instruction BoxIfIndexMatches(int index) { return index == Index ? InstructionList.StoreLocalBoxed(index) : null; }
	}

	/// <summary>�w�肳�ꂽ���[�J���ϐ����Q�Ƃ���l�ɃX�^�b�N����l��������Ɋ��蓖�Ă閽�߂�\���܂��B</summary>
	sealed class AssignLocalBoxedInstruction : LocalAccessInstruction
	{
		/// <summary>�Q�Ƃ���l�����蓖�Ă郍�[�J���ϐ����g�p���āA<see cref="Microsoft.Scripting.Interpreter.AssignLocalBoxedInstruction"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="index">�Q�Ƃ���l�����蓖�Ă郍�[�J���ϐ��������C���f�b�N�X���w�肵�܂��B</param>
		internal AssignLocalBoxedInstruction(int index) : base(index) { }

		/// <summary>���̖��߂ŏ�����X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			((StrongBox<object>)frame.Data[Index]).Value = frame.Peek();
			return +1;
		}
	}

	/// <summary>�w�肳�ꂽ���[�J���ϐ����Q�Ƃ���l�ɃX�^�b�N����l���i�[���閽�߂�\���܂��B</summary>
	sealed class StoreLocalBoxedInstruction : LocalAccessInstruction
	{
		/// <summary>�Q�Ƃ���l���i�[���郍�[�J���ϐ����g�p���āA<see cref="Microsoft.Scripting.Interpreter.StoreLocalBoxedInstruction"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="index">�Q�Ƃ���l���i�[���郍�[�J���ϐ��������C���f�b�N�X���w�肵�܂��B</param>
		internal StoreLocalBoxedInstruction(int index) : base(index) { }

		/// <summary>���̖��߂ŏ�����X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			((StrongBox<object>)frame.Data[Index]).Value = frame.Pop();
			return +1;
		}
	}

	/// <summary>�w�肳�ꂽ���[�J���ϐ��ɃN���[�W�����g�p���Ēl��������Ɋ��蓖�Ă閽�߂�\���܂��B</summary>
	sealed class AssignLocalToClosureInstruction : LocalAccessInstruction
	{
		/// <summary>�l�����蓖�Ă郍�[�J���ϐ����g�p���āA<see cref="Microsoft.Scripting.Interpreter.AssignLocalToClosureInstruction"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="index">�N���[�W�����g�p���Ēl�����蓖�Ă郍�[�J���ϐ��������C���f�b�N�X���w�肵�܂��B</param>
		internal AssignLocalToClosureInstruction(int index) : base(index) { }

		/// <summary>���̖��߂ŏ�����X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Closure[Index].Value = frame.Peek();
			return +1;
		}
	}

	/// <summary>���[�J���ϐ������������閽�߂̊�{�N���X��\���܂��B</summary>
	abstract class InitializeLocalInstruction : LocalAccessInstruction
	{
		/// <summary>���������郍�[�J���ϐ����g�p���āA<see cref="Microsoft.Scripting.Interpreter.InitializeLocalInstruction"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="index">���������郍�[�J���ϐ��������C���f�b�N�X���w�肵�܂��B</param>
		protected InitializeLocalInstruction(int index) : base(index) { }

		/// <summary>���[�J���ϐ�������̎Q�� (<c>null</c>) �ŏ��������閽�߂�\���܂��B</summary>
		internal sealed class Reference : InitializeLocalInstruction, IBoxableInstruction
		{
			/// <summary>���������郍�[�J���ϐ����g�p���āA<see cref="Microsoft.Scripting.Interpreter.InitializeLocalInstruction.Reference"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
			/// <param name="index">���������郍�[�J���ϐ��������C���f�b�N�X���w�肵�܂��B</param>
			internal Reference(int index) : base(index) { }

			/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
			/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
			/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
			public override int Run(InterpretedFrame frame)
			{
				frame.Data[Index] = null;
				return 1;
			}

			/// <summary>�w�肳�ꂽ�C���f�b�N�X�����߂��ΏۂƂ���C���f�b�N�X�ƈ�v�����ꍇ�ɒl���Q�Ƃɒu�����������߂��擾���܂��B</summary>
			/// <param name="index">���߂��ΏۂƂ���C���f�b�N�X�ł��邩�ǂ����𒲂ׂ�C���f�b�N�X���w�肵�܂��B</param>
			/// <returns>�C���f�b�N�X�����߂��ΏۂƂ���C���f�b�N�X�ƈ�v�����ꍇ�͒l���Q�Ƃɒu�����������߁B����ȊO�̏ꍇ�� <c>null</c>�B</returns>
			public Instruction BoxIfIndexMatches(int index) { return index == Index ? InstructionList.InitImmutableRefBox(index) : null; }

			/// <summary>���̖��߂̖��O���擾���܂��B</summary>
			public override string InstructionName { get { return "InitRef"; } }
		}

		/// <summary>���[�J���ϐ����w�肳�ꂽ�s�ϒl�ŏ��������閽�߂�\���܂��B</summary>
		internal sealed class ImmutableValue : InitializeLocalInstruction, IBoxableInstruction
		{
			readonly object _defaultValue;

			/// <summary>���������郍�[�J���ϐ����g�p���āA<see cref="Microsoft.Scripting.Interpreter.InitializeLocalInstruction.ImmutableValue"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
			/// <param name="index">���������郍�[�J���ϐ��������C���f�b�N�X���w�肵�܂��B</param>
			/// <param name="defaultValue">���[�J���ϐ�������������s�ϒl���w�肵�܂��B</param>
			internal ImmutableValue(int index, object defaultValue) : base(index) { _defaultValue = defaultValue; }

			/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
			/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
			/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
			public override int Run(InterpretedFrame frame)
			{
				frame.Data[Index] = _defaultValue;
				return 1;
			}

			/// <summary>�w�肳�ꂽ�C���f�b�N�X�����߂��ΏۂƂ���C���f�b�N�X�ƈ�v�����ꍇ�ɒl���Q�Ƃɒu�����������߂��擾���܂��B</summary>
			/// <param name="index">���߂��ΏۂƂ���C���f�b�N�X�ł��邩�ǂ����𒲂ׂ�C���f�b�N�X���w�肵�܂��B</param>
			/// <returns>�C���f�b�N�X�����߂��ΏۂƂ���C���f�b�N�X�ƈ�v�����ꍇ�͒l���Q�Ƃɒu�����������߁B����ȊO�̏ꍇ�� <c>null</c>�B</returns>
			public Instruction BoxIfIndexMatches(int index) { return index == Index ? new ImmutableBox(index, _defaultValue) : null; }

			/// <summary>���̖��߂̖��O���擾���܂��B</summary>
			public override string InstructionName { get { return "InitImmutableValue"; } }
		}

		/// <summary>���[�J���ϐ����w�肳�ꂽ�s�ϒl�ւ̎Q�Ƃŏ��������閽�߂�\���܂��B</summary>
		internal sealed class ImmutableBox : InitializeLocalInstruction
		{
			readonly object _defaultValue; // immutable value:

			/// <summary>���������郍�[�J���ϐ����g�p���āA<see cref="Microsoft.Scripting.Interpreter.InitializeLocalInstruction.ImmutableBox"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
			/// <param name="index">���������郍�[�J���ϐ��������C���f�b�N�X���w�肵�܂��B</param>
			/// <param name="defaultValue">���[�J���ϐ��������������Q�Ƃ������s�ϒl���w�肵�܂��B</param>
			internal ImmutableBox(int index, object defaultValue) : base(index) { _defaultValue = defaultValue; }

			/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
			/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
			/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
			public override int Run(InterpretedFrame frame)
			{
				frame.Data[Index] = new StrongBox<object>(_defaultValue);
				return 1;
			}

			/// <summary>���̖��߂̖��O���擾���܂��B</summary>
			public override string InstructionName { get { return "InitImmutableBox"; } }
		}

		/// <summary>���[�J���ϐ������̒l�ւ̎Q�Ƃŏ��������閽�߂�\���܂��B</summary>
		internal sealed class ParameterBox : InitializeLocalInstruction
		{
			/// <summary>���������郍�[�J���ϐ����g�p���āA<see cref="Microsoft.Scripting.Interpreter.InitializeLocalInstruction.ParameterBox"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
			/// <param name="index">���������郍�[�J���ϐ��������C���f�b�N�X���w�肵�܂��B</param>
			public ParameterBox(int index) : base(index) { }

			/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
			/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
			/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
			public override int Run(InterpretedFrame frame)
			{
				frame.Data[Index] = new StrongBox<object>(frame.Data[Index]);
				return 1;
			}

			/// <summary>���̖��߂̖��O���擾���܂��B</summary>
			public override string InstructionName { get { return "InitParameterBox"; } }
		}

		/// <summary>���[�J���ϐ������̒l�ŏ��������閽�߂�\���܂��B</summary>
		internal sealed class Parameter : InitializeLocalInstruction, IBoxableInstruction
		{
			/// <summary>���������郍�[�J���ϐ����g�p���āA<see cref="Microsoft.Scripting.Interpreter.InitializeLocalInstruction.Parameter"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
			/// <param name="index">���������郍�[�J���ϐ��������C���f�b�N�X���w�肵�܂��B</param>
			internal Parameter(int index) : base(index) { }
			
			/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
			/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
			/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
			public override int Run(InterpretedFrame frame) { return 1; } // nop

			/// <summary>�w�肳�ꂽ�C���f�b�N�X�����߂��ΏۂƂ���C���f�b�N�X�ƈ�v�����ꍇ�ɒl���Q�Ƃɒu�����������߂��擾���܂��B</summary>
			/// <param name="index">���߂��ΏۂƂ���C���f�b�N�X�ł��邩�ǂ����𒲂ׂ�C���f�b�N�X���w�肵�܂��B</param>
			/// <returns>�C���f�b�N�X�����߂��ΏۂƂ���C���f�b�N�X�ƈ�v�����ꍇ�͒l���Q�Ƃɒu�����������߁B����ȊO�̏ꍇ�� <c>null</c>�B</returns>
			public Instruction BoxIfIndexMatches(int index) { return index == Index ? InstructionList.ParameterBox(index) : null; }

			/// <summary>���̖��߂̖��O���擾���܂��B</summary>
			public override string InstructionName { get { return "InitParameter"; } }
		}

		/// <summary>���[�J���ϐ���ύX�\�Ȓl�ŏ��������閽�߂�\���܂��B</summary>
		internal sealed class MutableValue : InitializeLocalInstruction, IBoxableInstruction
		{
			readonly Type _type;

			/// <summary>���������郍�[�J���ϐ����g�p���āA<see cref="Microsoft.Scripting.Interpreter.InitializeLocalInstruction.MutableValue"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
			/// <param name="index">���������郍�[�J���ϐ��������C���f�b�N�X���w�肵�܂��B</param>
			/// <param name="type">���������ɃC���X�^���X�������^���w�肵�܂��B���̌^�ɂ͊���̃R���X�g���N�^�����݂���K�v������܂��B</param>
			internal MutableValue(int index, Type type) : base(index) { _type = type; }

			/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
			/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
			/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
			public override int Run(InterpretedFrame frame)
			{
				try { frame.Data[Index] = Activator.CreateInstance(_type); }
				catch (TargetInvocationException ex)
				{
					ExceptionHelpers.UpdateForRethrow(ex.InnerException);
					throw ex.InnerException;
				}
				return 1;
			}

			/// <summary>�w�肳�ꂽ�C���f�b�N�X�����߂��ΏۂƂ���C���f�b�N�X�ƈ�v�����ꍇ�ɒl���Q�Ƃɒu�����������߂��擾���܂��B</summary>
			/// <param name="index">���߂��ΏۂƂ���C���f�b�N�X�ł��邩�ǂ����𒲂ׂ�C���f�b�N�X���w�肵�܂��B</param>
			/// <returns>�C���f�b�N�X�����߂��ΏۂƂ���C���f�b�N�X�ƈ�v�����ꍇ�͒l���Q�Ƃɒu�����������߁B����ȊO�̏ꍇ�� <c>null</c>�B</returns>
			public Instruction BoxIfIndexMatches(int index) { return index == Index ? new MutableBox(index, _type) : null; }

			/// <summary>���̖��߂̖��O���擾���܂��B</summary>
			public override string InstructionName { get { return "InitMutableValue"; } }
		}

		/// <summary>���[�J���ϐ���ύX�\�Ȓl�ւ̎Q�Ƃŏ��������閽�߂�\���܂��B</summary>
		internal sealed class MutableBox : InitializeLocalInstruction
		{
			readonly Type _type;

			/// <summary>���������郍�[�J���ϐ����g�p���āA<see cref="Microsoft.Scripting.Interpreter.InitializeLocalInstruction.MutableBox"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
			/// <param name="index">���������郍�[�J���ϐ��������C���f�b�N�X���w�肵�܂��B</param>
			/// <param name="type">���������ɃC���X�^���X�������^���w�肵�܂��B���̌^�ɂ͊���̃R���X�g���N�^�����݂���K�v������܂��B</param>
			internal MutableBox(int index, Type type) : base(index) { _type = type; }

			/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
			/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
			/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
			public override int Run(InterpretedFrame frame)
			{
				frame.Data[Index] = new StrongBox<object>(Activator.CreateInstance(_type));
				return 1;
			}
			
			/// <summary>���̖��߂̖��O���擾���܂��B</summary>
			public override string InstructionName { get { return "InitMutableBox"; } }
		}
	}

	/// <summary>�]���X�^�b�N����Q�Ƃ��擾���ă����^�C���ϐ����擾���閽�߂�\���܂��B</summary>
	sealed class RuntimeVariablesInstruction : Instruction
	{
		readonly int _count;

		/// <summary>�擾���郉���^�C���ϐ��̐����g�p���āA<see cref="Microsoft.Scripting.Interpreter.RuntimeVariablesInstruction"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="count">�擾���郉���^�C���ϐ��̐����w�肵�܂��B</param>
		public RuntimeVariablesInstruction(int count) { _count = count; }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>���̖��߂ŏ�����X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ConsumedStack { get { return _count; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			var ret = new IStrongBox[_count];
			for (int i = ret.Length - 1; i >= 0; i--)
				ret[i] = (IStrongBox)frame.Pop();
			frame.Push(RuntimeVariables.Create(ret));
			return +1;
		}

		/// <summary>���̖��߂̖��O���擾���܂��B</summary>
		public override string InstructionName { get { return "GetRuntimeVariables"; } }
	}
}
