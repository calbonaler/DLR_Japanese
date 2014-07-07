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

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>�]���X�^�b�N�Ɏw�肳�ꂽ�I�u�W�F�N�g��ǂݍ��ޖ��߂�\���܂��B</summary>
	sealed class LoadObjectInstruction : Instruction
	{
		readonly object _value;

		/// <summary>�ǂݍ��ރI�u�W�F�N�g���g�p���āA<see cref="Microsoft.Scripting.Interpreter.LoadObjectInstruction"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="value">�]���X�^�b�N�ɓǂݍ��ރI�u�W�F�N�g���w�肵�܂��B</param>
		internal LoadObjectInstruction(object value) { _value = value; }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Push(_value);
			return +1;
		}

		/// <summary>���̃I�u�W�F�N�g�̕�����\�����擾���܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̕�����\���B</returns>
		public override string ToString() { return "LoadObject(" + (_value ?? "null") + ")"; }
	}

	/// <summary>�]���X�^�b�N�ɃL���b�V�����ꂽ�I�u�W�F�N�g��ǂݍ��ޖ��߂�\���܂��B</summary>
	sealed class LoadCachedObjectInstruction : Instruction
	{
		readonly uint _index;

		/// <summary>�ǂݍ��ރI�u�W�F�N�g�̃L���b�V���C���f�b�N�X���g�p���āA<see cref="Microsoft.Scripting.Interpreter.LoadCachedObjectInstruction"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="index">�]���X�^�b�N�ɓǂݍ��ރI�u�W�F�N�g�̃L���b�V���C���f�b�N�X���w�肵�܂��B</param>
		internal LoadCachedObjectInstruction(uint index) { _index = index; }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Push(frame.Interpreter._objects[_index]);
			return +1;
		}

		/// <summary>���̃I�u�W�F�N�g�̃f�o�b�O�p������\�����擾���܂��B</summary>
		/// <param name="instructionIndex">���̖��߂̖��߃C���f�b�N�X���w�肵�܂��B</param>
		/// <param name="cookie">�f�o�b�O�p Cookie ���w�肵�܂��B</param>
		/// <param name="labelIndexer">���x����\���C���f�b�N�X���烉�x���̑J�ڐ�C���f�b�N�X���擾����f���Q�[�g���w�肵�܂��B</param>
		/// <param name="objects">�f�o�b�O�p Cookie �̃��X�g���w�肵�܂��B</param>
		/// <returns>�f�o�b�O�p������\���B</returns>
		public override string ToDebugString(int instructionIndex, object cookie, Func<int, int> labelIndexer, IList<object> objects) { return string.Format("LoadCached({0}: {1})", _index, objects[(int)_index]); }

		/// <summary>���̃I�u�W�F�N�g�̕�����\�����擾���܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̕�����\���B</returns>
		public override string ToString() { return "LoadCached(" + _index + ")"; }
	}

	/// <summary>�]���X�^�b�N�̃X�^�b�N�g�b�v�̒l���̂Ă閽�߂�\���܂��B</summary>
	sealed class PopInstruction : Instruction
	{
		/// <summary>���̖��߂̗B��̃C���X�^���X�������܂��B</summary>
		internal static readonly PopInstruction Instance = new PopInstruction();

		PopInstruction() { }

		/// <summary>���̖��߂ŏ�����X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Pop();
			return +1;
		}
	}

	/// <summary>�]���X�^�b�N�̃X�^�b�N�g�b�v�̒l�𕡐����閽�߂�\���܂��B</summary>
	sealed class DupInstruction : Instruction
	{
		/// <summary>���̖��߂̗B��̃C���X�^���X�������܂��B</summary>
		internal readonly static DupInstruction Instance = new DupInstruction();

		DupInstruction() { }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Push(frame.Peek());
			return +1;
		}
	}
}
