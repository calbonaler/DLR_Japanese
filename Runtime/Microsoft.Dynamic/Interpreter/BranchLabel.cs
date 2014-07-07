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

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>���s���Ɏ��ۂɎg�p�����J�ڐ惉�x����\���܂��B</summary>
	internal struct RuntimeLabel
	{
		/// <summary>�J�ڐ�̖��߂������C���f�b�N�X�������܂��B</summary>
		public readonly int Index;
		/// <summary>�J�ڐ�̃X�^�b�N�̐[���������܂��B</summary>
		public readonly int StackDepth;
		/// <summary>�J�ڐ�̌p���X�^�b�N�̐[���������܂��B</summary>
		public readonly int ContinuationStackDepth;

		/// <summary>�w�肳�ꂽ�J�ڐ�̏����g�p���āA<see cref="Microsoft.Scripting.Interpreter.RuntimeLabel"/> �\���̂̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="index">�J�ڐ�̖��߂������C���f�b�N�X���w�肵�܂��B</param>
		/// <param name="continuationStackDepth">�J�ڐ�̌p���X�^�b�N�̐[�����w�肵�܂��B</param>
		/// <param name="stackDepth">�J�ڐ�̃X�^�b�N�̐[�����w�肵�܂��B</param>
		public RuntimeLabel(int index, int continuationStackDepth, int stackDepth)
		{
			Index = index;
			ContinuationStackDepth = continuationStackDepth;
			StackDepth = stackDepth;
		}

		/// <summary>���̃I�u�W�F�N�g�̕�����\�����擾���܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̕�����\���B</returns>
		public override string ToString() { return string.Format("->{0} C({1}) S({2})", Index, ContinuationStackDepth, StackDepth); }
	}

	/// <summary>�����̃��x����\���܂��B</summary>
	public sealed class BranchLabel
	{
		/// <summary>�C���f�b�N�X���s���ȏ�Ԃ������l��\���܂��B</summary>
		internal const int UnknownIndex = int.MinValue;
		/// <summary>�J�ڐ�̃X�^�b�N�̐[�����s���ȏ�Ԃ������l��\���܂��B</summary>
		internal const int UnknownDepth = int.MinValue;

		int _stackDepth = UnknownDepth;
		int _continuationStackDepth = UnknownDepth;

		/// <summary>���̃��x���𖽗߃��X�g�ɒǉ�������ɍX�V����K�v�����邱�̃��x����ΏۂƂ��镪�򖽗߂̃C���f�b�N�X�̃��X�g</summary>
		List<int> _forwardBranchFixups;

		/// <summary><see cref="Microsoft.Scripting.Interpreter.BranchLabel"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		public BranchLabel()
		{
			LabelIndex = UnknownIndex;
			TargetIndex = UnknownIndex;
		}

		/// <summary>���̃��x���̃��x�����X�g���ł̃C���f�b�N�X���擾���܂��B</summary>
		internal int LabelIndex { get; set; }

		/// <summary>���̃��x���� <see cref="RuntimeLabel"/> ���֘A�t�����Ă��邩�ǂ����������l���擾���܂��B</summary>
		internal bool HasRuntimeLabel { get { return LabelIndex != UnknownIndex; } }

		/// <summary>���̃��x�����ΏۂƂ��閽�߂������C���f�b�N�X���擾���܂��B</summary>
		internal int TargetIndex { get; private set; }

		/// <summary>���̃��x���� <see cref="RuntimeLabel"/> �ɕϊ����܂��B</summary>
		/// <returns>���̃��x���ɑ΂��� <see cref="RuntimeLabel"/>�B</returns>
		internal RuntimeLabel ToRuntimeLabel()
		{
			Debug.Assert(TargetIndex != UnknownIndex && _stackDepth != UnknownDepth && _continuationStackDepth != UnknownDepth);
			return new RuntimeLabel(TargetIndex, _continuationStackDepth, _stackDepth);
		}

		/// <summary>���̃��x���̑Ώۂ����ɒǉ�����閽�߂̐擪�ɐݒ肵�܂��B</summary>
		/// <param name="instructions">���x����ݒ肷�閽�߃��X�g���w�肵�܂��B</param>
		internal void Mark(InstructionList instructions)
		{
			// TODO: �V���h�[�C���O���ꂽ���x�����T�|�[�g����K�v������
			// Block( goto label; label: ), Block(goto label; label:)
			ContractUtils.Requires(TargetIndex == UnknownIndex && _stackDepth == UnknownDepth && _continuationStackDepth == UnknownDepth);
			_stackDepth = instructions.CurrentStackDepth;
			_continuationStackDepth = instructions.CurrentContinuationsDepth;
			TargetIndex = instructions.Count;
			if (_forwardBranchFixups != null)
			{
				foreach (var branchIndex in _forwardBranchFixups)
					FixupBranch(instructions, branchIndex);
				_forwardBranchFixups = null;
			}
		}

		/// <summary>���̃��x���ɕ��򂷂�I�t�Z�b�g���򖽗߂̃I�t�Z�b�g���X�V�ł���悤�ɂ��܂��B</summary>
		/// <param name="instructions">�X�V����I�t�Z�b�g���򖽗߂��܂ޖ��߃��X�g���w�肵�܂��B</param>
		/// <param name="branchIndex">���̃��x���ɕ��򂷂�I�t�Z�b�g���򖽗߂��������߃��X�g���̃C���f�b�N�X���w�肵�܂��B</param>
		internal void AddBranch(InstructionList instructions, int branchIndex)
		{
			Debug.Assert(((TargetIndex == UnknownIndex) == (_stackDepth == UnknownDepth)));
			Debug.Assert(((TargetIndex == UnknownIndex) == (_continuationStackDepth == UnknownDepth)));
			if (TargetIndex == UnknownIndex)
				(_forwardBranchFixups ?? (_forwardBranchFixups = new List<int>())).Add(branchIndex);
			else
				FixupBranch(instructions, branchIndex);
		}

		void FixupBranch(InstructionList instructions, int branchIndex)
		{
			Debug.Assert(TargetIndex != UnknownIndex);
			instructions.FixupBranch(branchIndex, TargetIndex - branchIndex, _continuationStackDepth, _stackDepth);
		}
	}
}
