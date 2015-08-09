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

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>�P��̃��[�J���ϐ��܂��̓N���[�W���ϐ���\���܂��B</summary>
	public sealed class LocalVariable
	{
		const int IsBoxedFlag = 1;
		const int InClosureFlag = 2;

		int _flags;

		/// <summary>���[�J���ϐ������蓖�Ă���C���^�v���^�̃f�[�^�̈��\���C���f�b�N�X���擾���܂��B</summary>
		public int Index { get; private set; }

		/// <summary>���̕ϐ����{�b�N�X���\���ł��邩�ǂ����������l���擾�܂��͐ݒ肵�܂��B</summary>
		public bool IsBoxed
		{
			get { return (_flags & IsBoxedFlag) != 0; }
			set
			{
				if (value)
					_flags |= IsBoxedFlag;
				else
					_flags &= ~IsBoxedFlag;
			}
		}

		/// <summary>���̕ϐ����N���[�W���ł��邩�ǂ����������l���擾���܂��B</summary>
		public bool InClosure { get { return (_flags & InClosureFlag) != 0; } }

		/// <summary>���̕ϐ����N���[�W���܂��̓{�b�N�X���\���ł��邩�ǂ����������l���擾���܂��B</summary>
		public bool InClosureOrBoxed { get { return InClosure | IsBoxed; } }

		/// <summary>���蓖�Ă�C���f�b�N�X���g�p���āA<see cref="Microsoft.Scripting.Interpreter.LocalVariable"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="index">�ϐ������蓖�Ă�C���f�b�N�X���w�肵�܂��B</param>
		/// <param name="closure">�ϐ����N���[�W���ł��邩�ǂ����������l���w�肵�܂��B</param>
		/// <param name="boxed">�ϐ����{�b�N�X���\���ł��邩�ǂ����������l���w�肵�܂��B</param>
		internal LocalVariable(int index, bool closure, bool boxed)
		{
			Index = index;
			_flags = (closure ? InClosureFlag : 0) | (boxed ? IsBoxedFlag : 0);
		}

		/// <summary>�w�肳�ꂽ�f�[�^�z��܂��̓N���[�W���f�[�^���炱�̃��[�J���ϐ��̃f�[�^��\������Ԃ��܂��B</summary>
		/// <param name="frameData">�C���^�v���^�̃X�^�b�N�t���[���ɂ�����f�[�^�z���\�������w�肵�܂��B</param>
		/// <param name="closure">�C���^�v���^�̃N���[�W���f�[�^�̔z���\�������w�肵�܂��B</param>
		/// <returns>���̃��[�J���ϐ��̒l��ǂݏo�����B</returns>
		internal Expression LoadFromArray(Expression frameData, Expression closure)
		{
			Expression result = Expression.ArrayAccess(InClosure ? closure : frameData, Expression.Constant(Index));
			return IsBoxed ? Expression.Convert(result, typeof(StrongBox<object>)) : result;
		}

		/// <summary>���̃I�u�W�F�N�g�̕�����\�����擾���܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̕�����\���B</returns>
		public override string ToString() { return string.Format("{0}: {1} {2}", Index, IsBoxed ? "�{�b�N�X��" : null, InClosure ? "�N���[�W����" : null); }
	}

	/// <summary>���[�J���ϐ��̃f�[�^�z���ł̏ꏊ�Ɗ֘A�t����ꂽ <see cref="ParameterExpression"/> ���i�[���܂��B</summary>
	struct LocalDefinition
	{
		/// <summary>���[�J���ϐ��̃f�[�^�z���̈ʒu�������܂��B</summary>
		public int Index;
		/// <summary>���[�J���ϐ����֘A�t����ꂽ <see cref="ParameterExpression"/> �������܂��B</summary>
		public ParameterExpression Parameter;

		/// <summary>�w�肳�ꂽ�f�[�^���g�p���āA<see cref="Microsoft.Scripting.Interpreter.LocalDefinition"/> �\���̂̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="localIndex">���[�J���ϐ��̃f�[�^�z���̈ʒu���w�肵�܂��B</param>
		/// <param name="parameter">���[�J���ϐ����֘A�t����ꂽ <see cref="ParameterExpression"/> ���w�肵�܂��B</param>
		public LocalDefinition(int localIndex, ParameterExpression parameter)
		{
			Index = localIndex;
			Parameter = parameter;
		}
	}

	/// <summary>���[�J���ϐ��̃��X�g��\���܂��B</summary>
	public sealed class LocalVariables
	{
		readonly Dictionary<ParameterExpression, VariableScope> _variables = new Dictionary<ParameterExpression, VariableScope>();
		int _localCount;

		/// <summary><see cref="Microsoft.Scripting.Interpreter.LocalVariables"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		internal LocalVariables() { }

		/// <summary>�w�肳�ꂽ <see cref="ParameterExpression"/> �ɑΉ�����w�肳�ꂽ���߃C���f�b�N�X����X�R�[�v���n�܂郍�[�J���ϐ����`���܂��B</summary>
		/// <param name="variable">�쐬����郍�[�J���ϐ��ɑΉ��Â����� <see cref="ParameterExpression"/> ���w�肵�܂��B</param>
		/// <param name="start">�쐬����郍�[�J���ϐ��̃X�R�[�v�̊J�n�ʒu���������߃C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>���[�J���ϐ���`��\�� <see cref="LocalDefinition"/>�B</returns>
		internal LocalDefinition DefineLocal(ParameterExpression variable, int start)
		{
			var result = new LocalVariable(_localCount++, false, false);
			LocalCount = System.Math.Max(_localCount, LocalCount);
			VariableScope existing, newScope;
			if (_variables.TryGetValue(variable, out existing))
				(existing.ChildScopes ?? (existing.ChildScopes = new List<VariableScope>())).Add(newScope = new VariableScope(result, start, existing));
			else
				newScope = new VariableScope(result, start, null);
			_variables[variable] = newScope;
			return new LocalDefinition(result.Index, variable);
		}

		/// <summary>�w�肳�ꂽ���[�J���ϐ���`�ɂ���Ď�����郍�[�J���ϐ��̃X�R�[�v���w�肳�ꂽ���߃C���f�b�N�X�ŏI�����܂��B</summary>
		/// <param name="definition">�X�R�[�v���I�����郍�[�J���ϐ����������[�J���ϐ���`���w�肵�܂��B</param>
		/// <param name="end">���[�J���ϐ��̃X�R�[�v�̏I���ʒu���������߃C���f�b�N�X���w�肵�܂��B</param>
		internal void UndefineLocal(LocalDefinition definition, int end)
		{
			var scope = _variables[definition.Parameter];
			scope.Stop = end;
			if (scope.Parent != null)
				_variables[definition.Parameter] = scope.Parent;
			else
				_variables.Remove(definition.Parameter);
			_localCount--;
		}

		/// <summary>�w�肳�ꂽ�ϐ��\�����{�b�N�X���\���ɐ؂�ւ��܂��B</summary>
		/// <param name="variable">�{�b�N�X���\���ɐ؂�ւ���ϐ���\�� <see cref="ParameterExpression"/> ���w�肵�܂��B</param>
		/// <param name="instructions">���݂̕ϐ����g�p���Ă��閽�߂��i�[���ꂽ <see cref="InstructionList"/> ���w�肵�܂��B</param>
		internal void Box(ParameterExpression variable, InstructionList instructions)
		{
			var scope = _variables[variable];
			var local = scope.Variable;
			Debug.Assert(!local.IsBoxed && !local.InClosure);
			scope.Variable.IsBoxed = true;
			int curChild = 0;
			for (int i = scope.Start; i < scope.Stop && i < instructions.Count; i++)
			{
				if (scope.ChildScopes != null && scope.ChildScopes[curChild].Start == i)
					i = scope.ChildScopes[curChild++].Stop; // skip boxing in the child scope
				else
					instructions.SwitchToBoxed(local.Index, i);
			}
		}

		/// <summary>���݂܂łɍ쐬�������[�J���ϐ��̌����擾���܂��B</summary>
		public int LocalCount { get; private set; }

		/// <summary>�w�肳�ꂽ <see cref="ParameterExpression"/> �ɑΉ����郍�[�J���ϐ��̃f�[�^�z����̃C���f�b�N�X���擾���܂��B</summary>
		/// <param name="var">�擾����ʒu�ɂ��郍�[�J���ϐ����Ή����� <see cref="ParameterExpression"/> ���w�肵�܂��B</param>
		/// <returns><see cref="ParameterExpression"/> �ɑΉ����郍�[�J���ϐ��̃f�[�^�z����̃C���f�b�N�X�B</returns>
		public int GetLocalIndex(ParameterExpression var)
		{
			VariableScope loc;
			return _variables.TryGetValue(var, out loc) ? loc.Variable.Index : -1;
		}

		/// <summary>�w�肳�ꂽ <see cref="ParameterExpression"/> �ɑΉ����郍�[�J���ϐ��܂��̓N���[�W���ϐ��̎擾�����݂܂��B</summary>
		/// <param name="var">�擾����ϐ��ɑΉ����� <see cref="ParameterExpression"/> ���w�肵�܂��B</param>
		/// <param name="local">�擾���ꂽ���[�J���ϐ��܂��̓N���[�W���ϐ���\�� <see cref="LocalVariable"/> ���i�[����܂��B</param>
		/// <returns>���[�J���ϐ��܂��̓N���[�W���ϐ�������Ɏ擾���ꂽ�ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public bool TryGetLocalOrClosure(ParameterExpression var, out LocalVariable local)
		{
			VariableScope scope;
			if (_variables.TryGetValue(var, out scope))
			{
				local = scope.Variable;
				return true;
			}
			local = null;
			return ClosureVariables != null && ClosureVariables.TryGetValue(var, out local);
		}

		/// <summary>���݂̃X�R�[�v�Œ�`����Ă��郍�[�J���ϐ��̃R�s�[���擾���܂��B</summary>
		/// <returns>���̃X�R�[�v�Œ�`����Ă��郍�[�J���ϐ��̃R�s�[�B</returns>
		internal Dictionary<ParameterExpression, LocalVariable> CopyLocals() { return _variables.ToDictionary(x => x.Key, x => x.Value.Variable); }

		/// <summary>�w�肳�ꂽ <see cref="ParameterExpression"/> �ɑΉ�����ϐ������݂̃X�R�[�v�Œ�`����Ă��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="variable">��`����Ă��邩�ǂ����𒲂ׂ�ϐ��ɑΉ����� <see cref="ParameterExpression"/> ���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ <see cref="ParameterExpression"/> �ɑΉ�����ϐ������݂̃X�R�[�v�Œ�`����Ă���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		internal bool ContainsVariable(ParameterExpression variable) { return _variables.ContainsKey(variable); }

		/// <summary>�O���̃X�R�[�v�Œ�`���ꌻ�݂̃X�R�[�v�ŗ��p�\�ȕϐ����擾���܂��B</summary>
		internal Dictionary<ParameterExpression, LocalVariable> ClosureVariables { get; private set; }

		/// <summary>���݂̃X�R�[�v�Ɏw�肳�ꂽ <see cref="ParameterExpression"/> �ɑΉ�����N���[�W���ϐ���ǉ����܂��B</summary>
		/// <param name="variable">�ǉ�����N���[�W���ϐ��ɑΉ����� <see cref="ParameterExpression"/>�B</param>
		/// <returns>�ǉ������N���[�W���ϐ���\�� <see cref="LocalVariable"/>�B</returns>
		internal LocalVariable AddClosureVariable(ParameterExpression variable)
		{
			if (ClosureVariables == null)
				ClosureVariables = new Dictionary<ParameterExpression, LocalVariable>();
			LocalVariable result = new LocalVariable(ClosureVariables.Count, true, false);
			ClosureVariables.Add(variable, result);
			return result;
		}

		/// <summary>�ϐ�����`����Ă���ꏊ�Ǝg�p����閽�ߔ͈͂�ǐՂ��܂��B</summary>
		sealed class VariableScope
		{
			public readonly int Start;
			public int Stop = Int32.MaxValue;
			public readonly LocalVariable Variable;
			public readonly VariableScope Parent;
			public List<VariableScope> ChildScopes;

			public VariableScope(LocalVariable variable, int start, VariableScope parent)
			{
				Variable = variable;
				Start = start;
				Parent = parent;
			}
		}
	}
}
