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

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>�X�^�b�N���ɑ��݂���v�f���g�p���Ĕz���V�����쐬���閽�߂�\���܂��B</summary>
	/// <typeparam name="TElement">�z��v�f�̌^���w�肵�܂��B</typeparam>
	public sealed class NewArrayInitInstruction<TElement> : Instruction
	{
		readonly int _elementCount;

		/// <summary>�������Ɏg�p����v�f�����g�p���āA<see cref="Microsoft.Scripting.Interpreter.NewArrayInitInstruction&lt;TElement&gt;"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="elementCount">�������Ɏg�p����V�����쐬�����z��̃T�C�Y�ɂȂ�v�f�����w�肵�܂��B</param>
		internal NewArrayInitInstruction(int elementCount) { _elementCount = elementCount; }

		/// <summary>���̖��߂ŏ�����X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ConsumedStack { get { return _elementCount; } }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			var array = new TElement[_elementCount];
			for (int i = _elementCount - 1; i >= 0; i--)
				array[i] = (TElement)frame.Pop();
			frame.Push(array);
			return +1;
		}
	}

	/// <summary>�X�^�b�N����v�f�����|�b�v���邱�Ƃł��̃T�C�Y�̔z����쐬���閽�߂�\���܂��B</summary>
	/// <typeparam name="TElement">�z��v�f�̌^���w�肵�܂��B</typeparam>
	public sealed class NewArrayInstruction<TElement> : Instruction
	{
		/// <summary><see cref="Microsoft.Scripting.Interpreter.NewArrayInstruction&lt;TElement&gt;"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		internal NewArrayInstruction() { }

		/// <summary>���̖��߂ŏ�����X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Push(new TElement[(int)frame.Pop()]);
			return +1;
		}
	}

	/// <summary>�X�^�b�N����e�����̗v�f�����|�b�v���邱�Ƃő������z����쐬���閽�߂�\���܂��B</summary>
	public sealed class NewArrayBoundsInstruction : Instruction
	{
		readonly Type _elementType;
		readonly int _rank;

		/// <summary>�z��v�f�̌^����ю������w�肵�āA<see cref="Microsoft.Scripting.Interpreter.NewArrayBoundsInstruction"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="elementType">�V�����쐬�����z��̗v�f�^���w�肵�܂��B</param>
		/// <param name="rank">�V�����쐬�����z��̎������w�肵�܂��B</param>
		internal NewArrayBoundsInstruction(Type elementType, int rank)
		{
			_elementType = elementType;
			_rank = rank;
		}

		/// <summary>���̖��߂ŏ�����X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ConsumedStack { get { return _rank; } }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			var lengths = new int[_rank];
			for (int i = _rank - 1; i >= 0; i--)
				lengths[i] = (int)frame.Pop();
			frame.Push(Array.CreateInstance(_elementType, lengths));
			return +1;
		}
	}

	/// <summary>�z��̗v�f���擾���閽�߂�\���܂��B</summary>
	/// <typeparam name="TElement">�z��v�f�̌^���w�肵�܂��B</typeparam>
	public sealed class GetArrayItemInstruction<TElement> : Instruction
	{
		/// <summary><see cref="Microsoft.Scripting.Interpreter.GetArrayItemInstruction&lt;TElement&gt;"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		internal GetArrayItemInstruction() { }

		/// <summary>���̖��߂ŏ�����X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ConsumedStack { get { return 2; } }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			var index = (int)frame.Pop();
			var array = (TElement[])frame.Pop();
			frame.Push(array[index]);
			return +1;
		}

		/// <summary>���̖��߂̖��O���擾���܂��B</summary>
		public override string InstructionName { get { return "GetArrayItem"; } }
	}

	/// <summary>�z��̗v�f��ݒ肷�閽�߂�\���܂��B</summary>
	/// <typeparam name="TElement">�z��v�f�̌^���w�肵�܂��B</typeparam>
	public sealed class SetArrayItemInstruction<TElement> : Instruction
	{
		/// <summary><see cref="Microsoft.Scripting.Interpreter.SetArrayItemInstruction&lt;TElement&gt;"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		internal SetArrayItemInstruction() { }

		/// <summary>���̖��߂ŏ�����X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ConsumedStack { get { return 3; } }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 0; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			var value = (TElement)frame.Pop();
			var index = (int)frame.Pop();
			var array = (TElement[])frame.Pop();
			array[index] = value;
			return +1;
		}

		/// <summary>���̖��߂̖��O���擾���܂��B</summary>
		public override string InstructionName { get { return "SetArrayItem"; } }
	}
}
