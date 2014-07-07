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
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>�C���^�v���^�ɃI�u�W�F�N�g���\�����߂�񋟂ł��邱�Ƃ�\���܂��B</summary>
	public interface IInstructionProvider
	{
		/// <summary>�w�肳�ꂽ�C���^�v���^�ɂ��̃I�u�W�F�N�g���\�����߂�ǉ����܂��B</summary>
		/// <param name="compiler">���߂�ǉ�����C���^�v���^���w�肵�܂��B</param>
		void AddInstructions(LightCompiler compiler);
	}

	/// <summary>�C���^�v���^�̖��߂�\���܂��B</summary>
	public abstract partial class Instruction
	{
		/// <summary>���̖��߂ŏ�����X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public virtual int ConsumedStack { get { return 0; } }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public virtual int ProducedStack { get { return 0; } }

		/// <summary>���̖��߂ŏ�����p���̐����擾���܂��B</summary>
		public virtual int ConsumedContinuations { get { return 0; } }

		/// <summary>���̖��߂Ő��������p���̐����擾���܂��B</summary>
		public virtual int ProducedContinuations { get { return 0; } }

		/// <summary>���̖��߂̑O��ł̃X�^�b�N�̗v�f���̑������擾���܂��B</summary>
		public int StackBalance { get { return ProducedStack - ConsumedStack; } }

		/// <summary>���̖��߂̑O��ł̌p���̑������擾���܂��B</summary>
		public int ContinuationsBalance { get { return ProducedContinuations - ConsumedContinuations; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public abstract int Run(InterpretedFrame frame);

		/// <summary>���̖��߂̖��O���擾���܂��B</summary>
		public virtual string InstructionName { get { return GetType().Name.Replace("Instruction", ""); } }

		/// <summary>���̃I�u�W�F�N�g�̕�����\�����擾���܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̕�����\���B</returns>
		public override string ToString() { return InstructionName + "()"; }

		/// <summary>���̃I�u�W�F�N�g�̃f�o�b�O�p������\�����擾���܂��B</summary>
		/// <param name="instructionIndex">���̖��߂̖��߃C���f�b�N�X���w�肵�܂��B</param>
		/// <param name="cookie">�f�o�b�O�p Cookie ���w�肵�܂��B</param>
		/// <param name="labelIndexer">���x����\���C���f�b�N�X���烉�x���̑J�ڐ�C���f�b�N�X���擾����f���Q�[�g���w�肵�܂��B</param>
		/// <param name="objects">�f�o�b�O�p Cookie �̃��X�g���w�肵�܂��B</param>
		/// <returns>�f�o�b�O�p������\���B</returns>
		public virtual string ToDebugString(int instructionIndex, object cookie, Func<int, int> labelIndexer, IList<object> objects) { return ToString(); }

		/// <summary>���̃I�u�W�F�N�g�̎w�肳�ꂽ�R���p�C���ɑ΂���f�o�b�O�p Cookie ���擾���܂��B</summary>
		/// <param name="compiler">�f�o�b�O�p Cookie ���擾����R���p�C�����w�肵�܂��B</param>
		/// <returns>�f�o�b�O�p Cookie�B</returns>
		public virtual object GetDebugCookie(LightCompiler compiler) { return null; }
	}

	/// <summary>�_���ے薽�߂�\���܂��B</summary>
	sealed class NotInstruction : Instruction
	{
		/// <summary>���̖��߂̗B��̃C���X�^���X��\���܂��B</summary>
		public static readonly Instruction Instance = new NotInstruction();

		NotInstruction() { }

		/// <summary>���̖��߂ŏ�����X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
		/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
		/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Push((bool)frame.Pop() ? ScriptingRuntimeHelpers.False : ScriptingRuntimeHelpers.True);
			return +1;
		}
	}
}
