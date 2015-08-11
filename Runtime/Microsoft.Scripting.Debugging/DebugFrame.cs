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
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Debugging.CompilerServices;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Debugging
{
	public sealed class DebugFrame
	{
		Exception _thrownException;
		IRuntimeVariables _liftedLocals;
		Dictionary<IList<VariableInfo>, ScopeData> _variables;

		// Symbol used to set "$exception" variable when exceptions are thrown
		static readonly SymbolId _exceptionVariableSymbol = SymbolTable.StringToId("$exception");

		internal DebugFrame(DebugThread thread, FunctionInfo funcInfo)
		{
			LastKnownGeneratorYieldMarker = Int32.MaxValue;
			Thread = thread;
			FunctionInfo = funcInfo;
			_variables = new Dictionary<IList<VariableInfo>, ScopeData>();
		}

		internal DebugFrame(DebugThread thread, FunctionInfo funcInfo, IRuntimeVariables liftedLocals, int frameOrder) : this(thread, funcInfo)
		{
			_liftedLocals = liftedLocals;
			StackDepth = frameOrder;
		}

		/// <summary>�X���b�h���擾���܂��B</summary>
		internal DebugThread Thread { get; private set; }

		/// <summary>�X�^�b�N�̐[�� (�t���[���̏���) ���擾�܂��͐ݒ肵�܂��B</summary>
		internal int StackDepth { get; set; }

		/// <summary>�ϐ����擾���܂��B</summary>
		internal VariableInfo[] Variables
		{
			get
			{
				var locals = LocalsInCurrentScope;
				var scopeData = GetScopeDataForLocals(locals);
				if (_thrownException == null ? scopeData.VarInfos == null : scopeData.VarInfosWithException == null)
				{
					scopeData.VarInfos = FunctionInfo.Variables.Where(x => x.IsParameter && !x.IsHidden).Concat(locals.Where(x => !x.IsHidden)).ToArray();
					scopeData.VarInfosWithException = Utils.ArrayUtils.Append(scopeData.VarInfos, new VariableInfo(_exceptionVariableSymbol, typeof(Exception), false, false, false));
				}
				return _thrownException == null ? scopeData.VarInfos : scopeData.VarInfosWithException;
			}
		}

		/// <summary>���݂̃V�[�P���X�|�C���g�̃C���f�b�N�X���擾�܂��͐ݒ肵�܂��B</summary>
		internal int CurrentSequencePointIndex
		{
			get
			{
				int debugMarker = CurrentLocationCookie;
				if (debugMarker >= FunctionInfo.SequencePoints.Length)
				{
					Debug.Fail("DebugMarker ���ǂ̈ʒu�ɂ���v���܂���B");
					debugMarker = 0;
				}
				return debugMarker;
			}
			set
			{
				if (value < 0 || value >= FunctionInfo.SequencePoints.Length)
					throw new ArgumentOutOfRangeException("value");

				// �ʒu�̓g���[�X�C�x���g���̃��[�t�t���[�����ł̂ݕύX�\
				if (!IsInTraceback)
				{
					Debug.Fail("�t���[���̓g���[�X�C�x���g�ɂ���܂���B");
					throw new InvalidOperationException(ErrorStrings.JumpNotAllowedInNonLeafFrames);
				}

				var needsGenerator = (value != CurrentLocationCookie || _thrownException != null);

				// �قȂ�ʒu�֕ύX���悤�Ƃ��Ă��邩�X���[���ꂽ��O������ꍇ�̓W�F�l���[�^���Ċ��蓖��
				if (Generator == null && needsGenerator)
				{
					RemapToGenerator(FunctionInfo.Version);
					Debug.Assert(Generator != null);
				}

				// �{���ɕK�v�ȏꍇ�݈̂ʒu��ύX����
				if (value != CurrentLocationCookie)
				{
					Debug.Assert(Generator != null);
					Generator.YieldMarkerLocation = value;
				}

				// �ʒu���ύX���ꂽ���ǂ����Ɋւ�炸�ۗ����̗�O�̓L�����Z������K�v������
				ThrownException = null;

				// ���݂̃C�x���g���W�F�l���[�^���[�v���痈�Ă��Ȃ��ꍇ�A�W�F�l���[�^���[�v�֍s���悤�ɂ���
				if (!InGeneratorLoop && needsGenerator)
					ForceSwitchToGeneratorLoop = true;
			}
		}

		internal void RemapToLatestVersion()
		{
			RemapToGenerator(Int32.MaxValue);
			// �W�F�l���[�^���[�v�֋�������
			if (!InGeneratorLoop)
				ForceSwitchToGeneratorLoop = true;
		}

		internal FunctionInfo FunctionInfo { get; private set; }

		internal Exception ThrownException
		{
			get { return _thrownException; }
			set
			{
				if (_thrownException != null && value == null)
				{
					_thrownException = null;
					GetLocalsScope().Remove(_exceptionVariableSymbol);
				}
				else if (value != null && !GetLocalsScope().ContainsKey(_exceptionVariableSymbol))
				{
					_thrownException = value;
					GetLocalsScope()[_exceptionVariableSymbol] = _thrownException;
				}
			}
		}

		internal IDebuggableGenerator Generator { get; private set; }

		internal bool IsInTraceback { get; set; }

		internal bool InGeneratorLoop { get; set; }

		internal bool ForceSwitchToGeneratorLoop { get; set; }

		internal DebugContext DebugContext { get { return Thread.DebugContext; } }

		internal int CurrentLocationCookie
		{
			get
			{
				Debug.Assert(Generator != null || _liftedLocals is IDebugRuntimeVariables);
				return (Generator == null ? ((IDebugRuntimeVariables)_liftedLocals).DebugMarker :
					(Generator.YieldMarkerLocation != Int32.MaxValue ? Generator.YieldMarkerLocation : LastKnownGeneratorYieldMarker));
			}
		}

		internal int LastKnownGeneratorYieldMarker { get; set; }

		/// <summary>
		/// �W�F�l���[�^����W�F�l���[�^�̃��[�J���ϐ����g�p���ăt���[�����X�V���邽�߂ɌĂ΂�܂��B
		/// </summary>
		internal void ReplaceLiftedLocals(IRuntimeVariables liftedLocals)
		{
			Debug.Assert(_liftedLocals == null || liftedLocals.Count >= _liftedLocals.Count);

			var oldLiftecLocals = _liftedLocals;

			// IStrongBox �̃��X�g��V�������X�g�Œu��������
			_liftedLocals = liftedLocals;

			if (oldLiftecLocals != null)
			{
				for (int i = 0; i < oldLiftecLocals.Count; i++)
				{
					if (!FunctionInfo.Variables[i].IsParameter && i < _liftedLocals.Count)
						_liftedLocals[i] = oldLiftecLocals[i];
				}
			}

			// �X�R�[�v��ϐ��̏�Ԃ��N���A���ĐV������Ԃ̍쐬����������
			_variables.Clear();
		}

		/// <summary>�t���[���̏�Ԃ��W�F�l���[�^�����s�̂��߂Ɏg�p�ł���悤�ɍĊ��蓖�Ă��܂��B</summary>
		/// <param name="version"><see cref="Int32.MaxValue"/> ���w�肷��ƍŐV�̃o�[�W���������蓖�Ă܂��B</param>
		internal void RemapToGenerator(int version)
		{
			Debug.Assert(Generator == null || FunctionInfo.Version != version);

			// �w�肳�ꂽ�o�[�W�����ɑ΂��đΏۂƂȂ� FunctionInfo �̌��������݂�
			var targetFuncInfo = GetFunctionInfo(version);
			if (targetFuncInfo == null)
				throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, ErrorStrings.InvalidFunctionVersion, version));

			// �V�����W�F�l���[�^���쐬
			Generator = (IDebuggableGenerator)targetFuncInfo.GeneratorFactory.DynamicInvoke(Enumerable.Repeat(this, 1).Concat(FunctionInfo.Variables.Where(x => x.IsParameter).Select((x, i) => _liftedLocals[i])).ToArray());
			// FunctionInfo ��V�����o�[�W�����ɍX�V
			if (FunctionInfo != targetFuncInfo)
				FunctionInfo = targetFuncInfo;

			// �ŏ��� yield �ʒu�ֈړ������s
			((IEnumerator)Generator).MoveNext();
		}

		internal IAttributesCollection GetLocalsScope()
		{
			var locals = LocalsInCurrentScope;
			var scopeData = GetScopeDataForLocals(locals);
			if (scopeData.Scope == null)
			{
				Debug.Assert(_liftedLocals != null);
				var visibleLocals = FunctionInfo.Variables.Where(x => x.IsParameter && !x.IsHidden).Concat(locals.Where(x => !x.IsHidden)).ToArray();
				scopeData.Scope = new LocalsDictionary(new ScopedRuntimeVariables(visibleLocals, _liftedLocals), visibleLocals.Select(x => x.Symbol));
			}
			return scopeData.Scope;
		}

		FunctionInfo GetFunctionInfo(int version)
		{
			if (version == FunctionInfo.Version)
				return FunctionInfo;

			FunctionInfo lastFuncInfo = null;
			for (var funcInfo = FunctionInfo; funcInfo != null; )
			{
				if (funcInfo.Version == version)
					return funcInfo;
				lastFuncInfo = funcInfo;
				if (version > funcInfo.Version)
					funcInfo = funcInfo.NextVersion;
				else
					funcInfo = funcInfo.PreviousVersion;
			}

			// �o�[�W������ Int32.MaxValue �Ȃ�΍ŐV�̃t�@�N�g����Ԃ�
			if (version == Int32.MaxValue)
				return lastFuncInfo;

			return null;
		}

		ScopeData GetScopeDataForLocals(IList<VariableInfo> locals)
		{
			ScopeData scopeData;
			if (!_variables.TryGetValue(locals, out scopeData))
				_variables[locals] = scopeData = new ScopeData();
			return scopeData;
		}

		IList<VariableInfo> LocalsInCurrentScope
		{
			get
			{
				var locals = CurrentLocationCookie < FunctionInfo.VariableScopeMap.Length ? FunctionInfo.VariableScopeMap[CurrentLocationCookie] : null;
				if (locals == null)
				{
					Debug.Fail("DebugMarker �͂ǂ̃X�R�[�v�ɂ���v���܂���ł����B");
					// "����" �Ȉʒu�ɑ΂���ϐ���ێ�����g�ɑ΂���L�[�Ƃ��� null ���g��
					locals = FunctionInfo.VariableScopeMap[0];
				}

				return locals;
			}
		}

		class ScopeData
		{
			public VariableInfo[] VarInfos;
			public VariableInfo[] VarInfosWithException;
			public IAttributesCollection Scope;
		}
	}
}
