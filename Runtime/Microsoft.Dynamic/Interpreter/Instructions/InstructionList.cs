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
// Enables instruction counting and displaying stats at process exit.
//#define STATS

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>�C���^�v���^�̖��߂̔z����i�[���܂��B</summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
	[DebuggerTypeProxy(typeof(InstructionArray.DebugView))]
	public struct InstructionArray
	{
		internal readonly int MaxStackDepth;
		internal readonly int MaxContinuationDepth;
		internal readonly Instruction[] Instructions;
		internal readonly object[] Objects;
		internal readonly RuntimeLabel[] Labels;

		// list of (instruction index, cookie) sorted by instruction index:
		internal readonly List<KeyValuePair<int, object>> DebugCookies;

		internal InstructionArray(int maxStackDepth, int maxContinuationDepth, Instruction[] instructions, object[] objects, RuntimeLabel[] labels, List<KeyValuePair<int, object>> debugCookies)
		{
			MaxStackDepth = maxStackDepth;
			MaxContinuationDepth = maxContinuationDepth;
			Instructions = instructions;
			DebugCookies = debugCookies;
			Objects = objects;
			Labels = labels;
		}

		internal int Length { get { return Instructions.Length; } }

		internal sealed class DebugView
		{
			readonly InstructionArray _array;

			public DebugView(InstructionArray array) { _array = array; }

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public InstructionList.DebugView.InstructionView[]/*!*/ A0 { get { return InstructionList.DebugView.GetInstructionViews(_array.Instructions, _array.Objects, i => _array.Labels[i].Index, _array.DebugCookies); } }
		}
	}

	/// <summary>�C���^�v���^�̖��߂̃��X�g��\���܂��B</summary>
	[DebuggerTypeProxy(typeof(InstructionList.DebugView))]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
	public sealed class InstructionList
	{
		readonly List<Instruction> _instructions = new List<Instruction>();
		List<object> _objects;
		int _maxContinuationDepth;
		int _runtimeLabelCount;
		List<BranchLabel> _labels;
		// list of (instruction index, cookie) sorted by instruction index:
		List<KeyValuePair<int, object>> _debugCookies = null;

		internal sealed class DebugView
		{
			readonly InstructionList _list;

			public DebugView(InstructionList list) { _list = list; }

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public InstructionView[]/*!*/ A0 { get { return GetInstructionViews(_list._instructions, _list._objects, i => _list._labels[i].TargetIndex, _list._debugCookies); } }

			internal static InstructionView[] GetInstructionViews(IList<Instruction> instructions, IList<object> objects, Func<int, int> labelIndexer, IList<KeyValuePair<int, object>> debugCookies)
			{
				var result = new List<InstructionView>();
				int stackDepth = 0;
				int continuationsDepth = 0;
				var cookieEnumerator = (debugCookies != null ? debugCookies : new KeyValuePair<int, object>[0]).GetEnumerator();
				var hasCookie = cookieEnumerator.MoveNext();
				for (int i = 0; i < instructions.Count; i++)
				{
					object cookie = null;
					while (hasCookie && cookieEnumerator.Current.Key == i)
					{
						cookie = cookieEnumerator.Current.Value;
						hasCookie = cookieEnumerator.MoveNext();
					}
					result.Add(new InstructionView(instructions[i], instructions[i].ToDebugString(i, cookie, labelIndexer, objects), i, stackDepth, continuationsDepth));
					stackDepth += instructions[i].StackBalance;
					continuationsDepth += instructions[i].ContinuationsBalance;
				}
				return result.ToArray();
			}

			[DebuggerDisplay("{GetValue(),nq}", Name = "{GetName(),nq}", Type = "{GetDisplayType(), nq}")]
			internal struct InstructionView
			{
				readonly int _index;
				readonly int _stackDepth;
				readonly int _continuationsDepth;
				readonly string _name;
				readonly Instruction _instruction;

				internal string GetName() { return _index.ToString() + (_continuationsDepth == 0 ? "" : " C(" + _continuationsDepth.ToString() + ")") + (_stackDepth == 0 ? "" : " S(" + _stackDepth.ToString() + ")"); }

				internal string GetValue() { return _name; }

				internal string GetDisplayType() { return _instruction.ContinuationsBalance.ToString() + "/" + _instruction.StackBalance.ToString(); }

				public InstructionView(Instruction instruction, string name, int index, int stackDepth, int continuationsDepth)
				{
					_instruction = instruction;
					_name = name;
					_index = index;
					_stackDepth = stackDepth;
					_continuationsDepth = continuationsDepth;
				}
			}
		}

		/// <summary>�w�肳�ꂽ���߂����̃��X�g�ɒǉ����܂��B</summary>
		/// <param name="instruction">�ǉ����閽�߂��w�肵�܂��B</param>
		public void Emit(Instruction instruction)
		{
			_instructions.Add(instruction);
			UpdateStackDepth(instruction);
		}

		void UpdateStackDepth(Instruction instruction)
		{
			Debug.Assert(instruction.ConsumedStack >= 0 && instruction.ProducedStack >= 0 && instruction.ConsumedContinuations >= 0 && instruction.ProducedContinuations >= 0);
			CurrentStackDepth -= instruction.ConsumedStack;
			Debug.Assert(CurrentStackDepth >= 0);
			CurrentStackDepth += instruction.ProducedStack;
			if (CurrentStackDepth > MaxStackDepth)
				MaxStackDepth = CurrentStackDepth;
			CurrentContinuationsDepth -= instruction.ConsumedContinuations;
			Debug.Assert(CurrentContinuationsDepth >= 0);
			CurrentContinuationsDepth += instruction.ProducedContinuations;
			if (CurrentContinuationsDepth > _maxContinuationDepth)
				_maxContinuationDepth = CurrentContinuationsDepth;
		}

		/// <summary>�w�肳�ꂽ�f�o�b�O�p Cookie ���ŋߒǉ����ꂽ���߂ɃA�^�b�`���܂��B</summary>
		[Conditional("DEBUG")]
		public void SetDebugCookie(object cookie)
		{
			if (_debugCookies == null)
				_debugCookies = new List<KeyValuePair<int, object>>();
			Debug.Assert(Count > 0);
			_debugCookies.Add(new KeyValuePair<int, object>(Count - 1, cookie));
		}

		/// <summary>���̃��X�g�Ɋi�[����Ă��閽�߂̐����擾���܂��B</summary>
		public int Count { get { return _instructions.Count; } }

		/// <summary>���݂̃X�^�b�N�̐[�����擾���܂��B</summary>
		public int CurrentStackDepth { get; private set; }

		/// <summary>���݂̌p���̐[�����擾���܂��B</summary>
		public int CurrentContinuationsDepth { get; private set; }

		/// <summary>���̖��߃��X�g�̖��߂����s����̂ɕK�v�ȃX�^�b�N�̐[�����擾���܂��B</summary>
		public int MaxStackDepth { get; private set; }

		/// <summary>���̖��߃��X�g�̎w�肳�ꂽ�C���f�b�N�X�ɂ��閽�߂��擾���܂��B</summary>
		/// <param name="index">�擾���閽�߂����� 0 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�C���f�b�N�X�ɂ��閽�߁B</returns>
		internal Instruction GetInstruction(int index) { return _instructions[index]; }

#if STATS
		static Dictionary<string, int> _executedInstructions = new Dictionary<string, int>();
		static Dictionary<string, Dictionary<object, bool>> _instances = new Dictionary<string, Dictionary<object, bool>>();

		static InstructionList()
		{
			AppDomain.CurrentDomain.ProcessExit += new EventHandler((_, __) =>
			{
				PerfTrack.DumpHistogram(_executedInstructions);
				Console.WriteLine("-- �S���s��: {0}", _executedInstructions.Values.Aggregate(0, (sum, value) => sum + value));
				Console.WriteLine("-----");
				var referenced = new Dictionary<string, int>();
				int total = 0;
				foreach (var entry in _instances)
				{
					referenced[entry.Key] = entry.Value.Count;
					total += entry.Value.Count;
				}
				PerfTrack.DumpHistogram(referenced);
				Console.WriteLine("-- �S�Q�Ɖ�: {0}", total);
				Console.WriteLine("-----");
			});
		}
#endif

		/// <summary>���̖��߃��X�g�S�̂� <see cref="InstructionArray"/> �Ƃ��Ď擾���܂��B</summary>
		/// <returns>���߃��X�g�S�̂�\�� <see cref="InstructionArray"/>�B</returns>
		public InstructionArray ToArray()
		{
#if STATS
			lock (_executedInstructions)
			{
				_instructions.ForEach(instr =>
				{
					int value = 0;
					var name = instr.GetType().Name;
					_executedInstructions.TryGetValue(name, out value);
					_executedInstructions[name] = value + 1;
					Dictionary<object, bool> dict;
					if (!_instances.TryGetValue(name, out dict))
						_instances[name] = dict = new Dictionary<object, bool>();
					dict[instr] = true;
				});
			}
#endif
			return new InstructionArray(MaxStackDepth, _maxContinuationDepth, _instructions.ToArray(), _objects != null ? _objects.ToArray() : null, BuildRuntimeLabels(), _debugCookies);
		}

		const int PushIntMinCachedValue = -100;
		const int PushIntMaxCachedValue = 100;
		const int CachedObjectCount = 256;

		static Instruction _null;
		static Instruction _true;
		static Instruction _false;
		static Instruction[] _ints;
		static Instruction[] _loadObjectCached;

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g���X�^�b�N�ɓǂݍ��ޖ��߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="value">�ǂݍ��ރI�u�W�F�N�g���w�肵�܂��B</param>
		public void EmitLoad(object value) { EmitLoad(value, null); }

		/// <summary>�w�肳�ꂽ�u�[���l���X�^�b�N�ɓǂݍ��ޖ��߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="value">�ǂݍ��ރu�[���l���w�肵�܂��B</param>
		public void EmitLoad(bool value)
		{
			if ((bool)value)
				Emit(_true ?? (_true = new LoadObjectInstruction(ScriptingRuntimeHelpers.True)));
			else
				Emit(_false ?? (_false = new LoadObjectInstruction(ScriptingRuntimeHelpers.False)));
		}

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g���w�肳�ꂽ�^�Ƃ��ēǂݍ��ޖ��߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="value">�ǂݍ��ރI�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="type">�ǂݍ��ރI�u�W�F�N�g�̌^���w�肵�܂��B<c>null</c> ���w�肷�邱�Ƃ��ł��܂��B</param>
		public void EmitLoad(object value, Type type)
		{
			if (value == null)
			{
				Emit(_null ?? (_null = new LoadObjectInstruction(null)));
				return;
			}
			if (type == null || type.IsValueType)
			{
				if (value is bool)
				{
					EmitLoad((bool)value);
					return;
				}
				if (value is int)
				{
					int i = (int)value;
					if (i >= PushIntMinCachedValue && i <= PushIntMaxCachedValue)
					{
						if (_ints == null)
							_ints = new Instruction[PushIntMaxCachedValue - PushIntMinCachedValue + 1];
						i -= PushIntMinCachedValue;
						Emit(_ints[i] ?? (_ints[i] = new LoadObjectInstruction(value)));
						return;
					}
				}
			}
			if (_objects == null)
			{
				_objects = new List<object>();
				if (_loadObjectCached == null)
					_loadObjectCached = new Instruction[CachedObjectCount];
			}
			if (_objects.Count < _loadObjectCached.Length)
			{
				uint index = (uint)_objects.Count;
				_objects.Add(value);
				Emit(_loadObjectCached[index] ?? (_loadObjectCached[index] = new LoadCachedObjectInstruction(index)));
			}
			else
				Emit(new LoadObjectInstruction(value));
		}

		/// <summary>�]���X�^�b�N�̃X�^�b�N�g�b�v�̒l�𕡐����閽�߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		public void EmitDup() { Emit(DupInstruction.Instance); }

		/// <summary>�]���X�^�b�N�̃X�^�b�N�g�b�v�̒l���̂Ă閽�߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		public void EmitPop() { Emit(PopInstruction.Instance); }

		/// <summary>�w�肳�ꂽ���߃C���f�b�N�X�ɂ��閽�߂����삷�郍�[�J���̃C���f�b�N�X���w�肳�ꂽ�l�ł���΁A���̖��߂� <see cref="StrongBox&lt;T&gt;"/> ���g�p������̂ɒu�������܂��B</summary>
		/// <param name="index"><see cref="StrongBox&lt;T&gt;"/> ���g�p������̂ɒu�������閽�߂����삷�郍�[�J���̃C���f�b�N�X���w�肵�܂��B</param>
		/// <param name="instructionIndex">�u�������閽�߂̃C���f�b�N�X���w�肵�܂��B</param>
		internal void SwitchToBoxed(int index, int instructionIndex)
		{
			var instruction = _instructions[instructionIndex] as IBoxableInstruction;
			if (instruction != null)
			{
				var newInstruction = instruction.BoxIfIndexMatches(index);
				if (newInstruction != null)
					_instructions[instructionIndex] = newInstruction;
			}
		}

		const int LocalInstrCacheSize = 64;

		static Instruction[] _loadLocal;
		static Instruction[] _loadLocalBoxed;
		static Instruction[] _loadLocalFromClosure;
		static Instruction[] _loadLocalFromClosureBoxed;
		static Instruction[] _assignLocal;
		static Instruction[] _storeLocal;
		static Instruction[] _assignLocalBoxed;
		static Instruction[] _storeLocalBoxed;
		static Instruction[] _assignLocalToClosure;
		static Instruction[] _initReference;
		static Instruction[] _initImmutableRefBox;
		static Instruction[] _parameterBox;
		static Instruction[] _parameter;

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�̃��[�J���ϐ��̒l��]���X�^�b�N�ɓǂݍ��ޖ��߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="index">�]���X�^�b�N�ɒl��ǂݍ��ރ��[�J���ϐ��̃C���f�b�N�X���w�肵�܂��B</param>
		public void EmitLoadLocal(int index)
		{
			if (_loadLocal == null)
				_loadLocal = new Instruction[LocalInstrCacheSize];
			Emit(index < _loadLocal.Length ? _loadLocal[index] ?? (_loadLocal[index] = new LoadLocalInstruction(index)) : new LoadLocalInstruction(index));
		}

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�� <see cref="StrongBox&lt;T&gt;"/> �ŎQ�Ƃ��ꂽ���[�J���ϐ��̒l��]���X�^�b�N�ɓǂݍ��ޖ��߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="index">�]���X�^�b�N�ɎQ�Ƃ��ꂽ�l��ǂݍ��ރ��[�J���ϐ��̃C���f�b�N�X���w�肵�܂��B</param>
		public void EmitLoadLocalBoxed(int index) { Emit(LoadLocalBoxed(index)); }

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�� <see cref="StrongBox&lt;T&gt;"/> �ŎQ�Ƃ��ꂽ���[�J���ϐ��̒l��]���X�^�b�N�ɓǂݍ��ޖ��߂��擾���܂��B</summary>
		/// <param name="index">�]���X�^�b�N�ɎQ�Ƃ��ꂽ�l��ǂݍ��ރ��[�J���ϐ��̃C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�Q�Ƃ��ꂽ���[�J���ϐ��̒l��]���X�^�b�N�ɓǂݍ��ޖ��߁B</returns>
		internal static Instruction LoadLocalBoxed(int index)
		{
			if (_loadLocalBoxed == null)
				_loadLocalBoxed = new Instruction[LocalInstrCacheSize];
			return index < _loadLocalBoxed.Length ? _loadLocalBoxed[index] ?? (_loadLocalBoxed[index] = new LoadLocalBoxedInstruction(index)) : new LoadLocalBoxedInstruction(index);
		}

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�̃��[�J���ϐ��̒l���N���[�W������]���X�^�b�N�ɓǂݍ��ޖ��߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="index">�]���X�^�b�N�ɒl��ǂݍ��ރ��[�J���ϐ��̃C���f�b�N�X���w�肵�܂��B</param>
		public void EmitLoadLocalFromClosure(int index)
		{
			if (_loadLocalFromClosure == null)
				_loadLocalFromClosure = new Instruction[LocalInstrCacheSize];
			Emit(index < _loadLocalFromClosure.Length ? _loadLocalFromClosure[index] ?? (_loadLocalFromClosure[index] = new LoadLocalFromClosureInstruction(index)) : new LoadLocalFromClosureInstruction(index));
		}

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�̃��[�J���ϐ��̎Q�Ƃ��N���[�W������]���X�^�b�N�ɓǂݍ��ޖ��߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="index">�]���X�^�b�N�ɎQ�Ƃ�ǂݍ��ރ��[�J���ϐ��̃C���f�b�N�X���w�肵�܂��B</param>
		public void EmitLoadLocalFromClosureBoxed(int index)
		{
			if (_loadLocalFromClosureBoxed == null)
				_loadLocalFromClosureBoxed = new Instruction[LocalInstrCacheSize];
			Emit(index < _loadLocalFromClosureBoxed.Length ? _loadLocalFromClosureBoxed[index] ?? (_loadLocalFromClosureBoxed[index] = new LoadLocalFromClosureBoxedInstruction(index)) : new LoadLocalFromClosureBoxedInstruction(index));
		}

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�̃��[�J���ϐ��ɒl��������Ɋ��蓖�Ă閽�߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="index">�l�����蓖�Ă��郍�[�J���ϐ��̃C���f�b�N�X���w�肵�܂��B</param>
		public void EmitAssignLocal(int index)
		{
			if (_assignLocal == null)
				_assignLocal = new Instruction[LocalInstrCacheSize];
			Emit(index < _assignLocal.Length ? _assignLocal[index] ?? (_assignLocal[index] = new AssignLocalInstruction(index)) : new AssignLocalInstruction(index));
		}

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�̃��[�J���ϐ��ɒl���i�[���閽�߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="index">�l���i�[����郍�[�J���ϐ��̃C���f�b�N�X���w�肵�܂��B</param>
		public void EmitStoreLocal(int index)
		{
			if (_storeLocal == null)
				_storeLocal = new Instruction[LocalInstrCacheSize];
			Emit(index < _storeLocal.Length ? _storeLocal[index] ?? (_storeLocal[index] = new StoreLocalInstruction(index)) : new StoreLocalInstruction(index));
		}

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�̃��[�J���ϐ��̎Q�Ɛ�ɒl��������Ɋ��蓖�Ă閽�߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="index">�l���Q�Ɛ�Ɋ��蓖�Ă��郍�[�J���ϐ��̃C���f�b�N�X���w�肵�܂��B</param>
		public void EmitAssignLocalBoxed(int index) { Emit(AssignLocalBoxed(index)); }

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�̃��[�J���ϐ��̎Q�Ɛ�ɒl��������Ɋ��蓖�Ă閽�߂��擾���܂��B</summary>
		/// <param name="index">�l���Q�Ɛ�Ɋ��蓖�Ă��郍�[�J���ϐ��̃C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>���[�J���ϐ��̎Q�Ɛ�ɒl��������Ɋ��蓖�Ă閽�߁B</returns>
		internal static Instruction AssignLocalBoxed(int index)
		{
			if (_assignLocalBoxed == null)
				_assignLocalBoxed = new Instruction[LocalInstrCacheSize];
			return index < _assignLocalBoxed.Length ? _assignLocalBoxed[index] ?? (_assignLocalBoxed[index] = new AssignLocalBoxedInstruction(index)) : new AssignLocalBoxedInstruction(index);
		}

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�̃��[�J���ϐ��̎Q�Ɛ�ɒl���i�[���閽�߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="index">�l���Q�Ɛ�Ɋi�[����郍�[�J���ϐ��̃C���f�b�N�X���w�肵�܂��B</param>
		public void EmitStoreLocalBoxed(int index) { Emit(StoreLocalBoxed(index)); }

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�̃��[�J���ϐ��̎Q�Ɛ�ɒl���i�[���閽�߂��擾���܂��B</summary>
		/// <param name="index">�l���Q�Ɛ�Ɋi�[����郍�[�J���ϐ��̃C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>���[�J���ϐ��̎Q�Ɛ�ɒl���i�[���閽�߁B</returns>
		internal static Instruction StoreLocalBoxed(int index)
		{
			if (_storeLocalBoxed == null)
				_storeLocalBoxed = new Instruction[LocalInstrCacheSize];
			return index < _storeLocalBoxed.Length ? _storeLocalBoxed[index] ?? (_storeLocalBoxed[index] = new StoreLocalBoxedInstruction(index)) : new StoreLocalBoxedInstruction(index);
		}

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�̃��[�J���ϐ��ɃN���[�W�����g�p���Ēl��������Ɋ��蓖�Ă閽�߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="index">�l�����蓖�Ă��郍�[�J���ϐ��̃C���f�b�N�X���w�肵�܂��B</param>
		public void EmitAssignLocalToClosure(int index)
		{
			if (_assignLocalToClosure == null)
				_assignLocalToClosure = new Instruction[LocalInstrCacheSize];
			Emit(index < _assignLocalToClosure.Length ? _assignLocalToClosure[index] ?? (_assignLocalToClosure[index] = new AssignLocalToClosureInstruction(index)) : new AssignLocalToClosureInstruction(index));
		}

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�̃��[�J���ϐ��ɃN���[�W�����g�p���Ēl���i�[���閽�߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="index">�l���i�[����郍�[�J���ϐ��̃C���f�b�N�X���w�肵�܂��B</param>
		public void EmitStoreLocalToClosure(int index)
		{
			EmitAssignLocalToClosure(index);
			EmitPop();
		}

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�̃��[�J���ϐ������������閽�߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="index">���������郍�[�J���ϐ��̃C���f�b�N�X���w�肵�܂��B</param>
		/// <param name="type">���������郍�[�J���ϐ��̌^���w�肵�܂��B</param>
		public void EmitInitializeLocal(int index, Type type)
		{
			var value = ScriptingRuntimeHelpers.GetPrimitiveDefaultValue(type);
			if (value != null)
				Emit(new InitializeLocalInstruction.ImmutableValue(index, value));
			else if (type.IsValueType)
				Emit(new InitializeLocalInstruction.MutableValue(index, type));
			else
				Emit(InitReference(index));
		}

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�̃��[�J���ϐ������������閽�߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="index">���������郍�[�J���ϐ��̃C���f�b�N�X���w�肵�܂��B</param>
		internal void EmitInitializeParameter(int index) { Emit(Parameter(index)); }

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�̃��[�J���ϐ������������閽�߂��擾���܂��B</summary>
		/// <param name="index">���������郍�[�J���ϐ��̃C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>���������������閽�߁B</returns>
		internal static Instruction Parameter(int index)
		{
			if (_parameter == null)
				_parameter = new Instruction[LocalInstrCacheSize];
			return index < _parameter.Length ? _parameter[index] ?? (_parameter[index] = new InitializeLocalInstruction.Parameter(index)) : new InitializeLocalInstruction.Parameter(index);
		}

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�̃��[�J���ϐ����Q�ƂƂ��ď��������閽�߂��擾���܂��B</summary>
		/// <param name="index">���������郍�[�J���ϐ��̃C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>���[�J���ϐ����Q�ƂƂ��ď��������閽�߁B</returns>
		internal static Instruction ParameterBox(int index)
		{
			if (_parameterBox == null)
				_parameterBox = new Instruction[LocalInstrCacheSize];
			return index < _parameterBox.Length ? _parameterBox[index] ?? (_parameterBox[index] = new InitializeLocalInstruction.ParameterBox(index)) : new InitializeLocalInstruction.ParameterBox(index);
		}

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�̃��[�J���ϐ�������̎Q�Ƃŏ��������閽�߂��擾���܂��B</summary>
		/// <param name="index">���������郍�[�J���ϐ��̃C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>���[�J���ϐ�������̎Q�Ƃŏ��������閽�߁B</returns>
		internal static Instruction InitReference(int index)
		{
			if (_initReference == null)
				_initReference = new Instruction[LocalInstrCacheSize];
			return index < _initReference.Length ? _initReference[index] ?? (_initReference[index] = new InitializeLocalInstruction.Reference(index)) : new InitializeLocalInstruction.Reference(index);
		}

		/// <summary>�w�肳�ꂽ���[�J���ϐ��� <c>null</c> ���Q�Ƃ��� <see cref="StrongBox&lt;T&gt;"/> �ŏ��������閽�߂��擾���܂��B</summary>
		/// <param name="index">���������郍�[�J���ϐ��̃C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>���[�J���ϐ��� <c>null</c> ���Q�Ƃ��� <see cref="StrongBox&lt;T&gt;"/> �ŏ��������閽�߁B</returns>
		internal static Instruction InitImmutableRefBox(int index)
		{
			if (_initImmutableRefBox == null)
				_initImmutableRefBox = new Instruction[LocalInstrCacheSize];
			return index < _initImmutableRefBox.Length ? _initImmutableRefBox[index] ?? (_initImmutableRefBox[index] = new InitializeLocalInstruction.ImmutableBox(index, null)) : new InitializeLocalInstruction.ImmutableBox(index, null);
		}

		/// <summary>�w�肳�ꂽ���̃����^�C���ϐ����쐬���閽�߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="count">�쐬����ю擾���郉���^�C���ϐ��̐����w�肵�܂��B</param>
		public void EmitNewRuntimeVariables(int count) { Emit(new RuntimeVariablesInstruction(count)); }

		/// <summary>�w�肳�ꂽ�^�̔z��̗v�f���擾���閽�߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="arrayType">�擾����z��̌^���w�肵�܂��B</param>
		public void EmitGetArrayItem(Type arrayType)
		{
			var elementType = arrayType.GetElementType();
			Emit(elementType.IsClass || elementType.IsInterface ? InstructionFactory<object>.Factory.GetArrayItem() : InstructionFactory.GetFactory(elementType).GetArrayItem());
		}

		/// <summary>�w�肳�ꂽ�^�̔z��̗v�f��ݒ肷�閽�߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="arrayType">�ݒ肷��z��̌^���w�肵�܂��B</param>
		public void EmitSetArrayItem(Type arrayType)
		{
			var elementType = arrayType.GetElementType();
			Emit(elementType.IsClass || elementType.IsInterface ? InstructionFactory<object>.Factory.SetArrayItem() : InstructionFactory.GetFactory(elementType).SetArrayItem());
		}

		/// <summary>�w�肳�ꂽ�v�f�^�̔z����쐬���閽�߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="elementType">�쐬����z��̗v�f�̌^���w�肵�܂��B</param>
		public void EmitNewArray(Type elementType) { Emit(InstructionFactory.GetFactory(elementType).NewArray()); }

		/// <summary>�w�肳�ꂽ�v�f�^�Ŏw�肳�ꂽ���������z����쐬���閽�߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="elementType">�쐬����z��̗v�f�̌^���w�肵�܂��B</param>
		/// <param name="rank">�쐬����z��̎������w�肵�܂��B</param>
		public void EmitNewArrayBounds(Type elementType, int rank) { Emit(new NewArrayBoundsInstruction(elementType, rank)); }

		/// <summary>�w�肳�ꂽ�v�f�^�̔z����w�肳�ꂽ���̗v�f�ŏ��������閽�߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="elementType">�쐬����z��̗v�f�̌^���w�肵�܂��B</param>
		/// <param name="elementCount">�쐬����z�������������v�f�̐����w�肵�܂��B</param>
		public void EmitNewArrayInit(Type elementType, int elementCount) { Emit(InstructionFactory.GetFactory(elementType).NewArrayInit(elementCount)); }

		/// <summary>�w�肳�ꂽ�^�̉��Z���߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="type">���Z�Ώۂ̃I�y�����h�̌^���w�肵�܂��B</param>
		/// <param name="checked">���Z���ɃI�[�o�[�t���[���`�F�b�N���邩�ǂ����������l���w�肵�܂��B</param>
		public void EmitAdd(Type type, bool @checked) { Emit(@checked ? AddOvfInstruction.Create(type) : AddInstruction.Create(type)); }

		/// <summary>�w�肳�ꂽ�^�̌��Z���߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="type">���Z�Ώۂ̃I�y�����h�̌^���w�肵�܂��B</param>
		/// <param name="checked">���Z���ɃI�[�o�[�t���[���`�F�b�N���邩�ǂ����������l���w�肵�܂��B</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
		public void EmitSub(Type type, bool @checked) { throw new NotSupportedException(); }

		/// <summary>�w�肳�ꂽ�^�̏�Z���߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="type">��Z�Ώۂ̃I�y�����h�̌^���w�肵�܂��B</param>
		/// <param name="checked">��Z���ɃI�[�o�[�t���[���`�F�b�N���邩�ǂ����������l���w�肵�܂��B</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
		public void EmitMul(Type type, bool @checked) { throw new NotSupportedException(); }

		/// <summary>�w�肳�ꂽ�^�̏��Z���߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="type">���Z�Ώۂ̃I�y�����h�̌^���w�肵�܂��B</param>
		public void EmitDiv(Type type) { Emit(DivInstruction.Create(type)); }

		/// <summary>�w�肳�ꂽ�^�̓��l��r���߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="type">��r�Ώۂ̃I�y�����h�̌^���w�肵�܂��B</param>
		public void EmitEqual(Type type) { Emit(EqualInstruction.Create(type)); }

		/// <summary>�w�肳�ꂽ�^�̕s���l��r���߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="type">��r�Ώۂ̃I�y�����h�̌^���w�肵�܂��B</param>
		public void EmitNotEqual(Type type) { Emit(NotEqualInstruction.Create(type)); }

		/// <summary>�w�肳�ꂽ�^�̏��Ȃ��r���߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="type">��r�Ώۂ̃I�y�����h�̌^���w�肵�܂��B</param>
		public void EmitLessThan(Type type) { Emit(LessThanInstruction.Create(type)); }

		/// <summary>�w�肳�ꂽ�^�̈ȉ���r���߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="type">��r�Ώۂ̃I�y�����h�̌^���w�肵�܂��B</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
		public void EmitLessThanOrEqual(Type type) { throw new NotSupportedException(); }

		/// <summary>�w�肳�ꂽ�^�̑�Ȃ��r���߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="type">��r�Ώۂ̃I�y�����h�̌^���w�肵�܂��B</param>
		public void EmitGreaterThan(Type type) { Emit(GreaterThanInstruction.Create(type)); }

		/// <summary>�w�肳�ꂽ�^�̈ȏ��r���߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="type">��r�Ώۂ̃I�y�����h�̌^���w�肵�܂��B</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
		public void EmitGreaterThanOrEqual(Type type) { throw new NotSupportedException(); }

		/// <summary>�I�[�o�[�t���[���`�F�b�N���鐔�l�^�̌^�ϊ����߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="from">�ϊ����̌^���w�肵�܂��B</param>
		/// <param name="to">�ϊ���̌^���w�肵�܂��B</param>
		public void EmitNumericConvertChecked(TypeCode from, TypeCode to) { Emit(new NumericConvertInstruction.Checked(from, to)); }

		/// <summary>�I�[�o�[�t���[���`�F�b�N���Ȃ����l�^�̌^�ϊ����߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="from">�ϊ����̌^���w�肵�܂��B</param>
		/// <param name="to">�ϊ���̌^���w�肵�܂��B</param>
		public void EmitNumericConvertUnchecked(TypeCode from, TypeCode to) { Emit(new NumericConvertInstruction.Unchecked(from, to)); }

		/// <summary>�_���ے薽�߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		public void EmitNot() { Emit(NotInstruction.Instance); }

		/// <summary>�w�肳�ꂽ�^�̊���l��]���X�^�b�N�ɓǂݍ��ޖ��߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="type">����l���擾����^���w�肵�܂��B</param>
		public void EmitDefaultValue(Type type) { Emit(InstructionFactory.GetFactory(type).DefaultValue()); }

		/// <summary>�w�肳�ꂽ�R���X�g���N�^���g�p���ăC���X�^���X���쐬���閽�߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="constructorInfo">�C���X�^���X�̍쐬�Ɏg�p�����R���X�g���N�^���w�肵�܂��B</param>
		public void EmitNew(ConstructorInfo constructorInfo) { Emit(new NewInstruction(constructorInfo)); }

		/// <summary>�w�肳�ꂽ <see cref="LightDelegateCreator"/> ���g�p���ăf���Q�[�g���쐬���閽�߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="creator">�f���Q�[�g�̍쐬���Ǘ����� <see cref="LightDelegateCreator"/> ���w�肵�܂��B</param>
		internal void EmitCreateDelegate(LightDelegateCreator creator) { Emit(new CreateDelegateInstruction(creator)); }

		/// <summary>�I�u�W�F�N�g�̌^���񋟂��ꂽ�^�Ɠ��������ǂ����𔻒f���閽�߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		public void EmitTypeEquals() { Emit(TypeEqualsInstruction.Instance); }

		/// <summary>�I�u�W�F�N�g���w�肳�ꂽ�^�ɕϊ��ł��邩�ǂ����𔻒f���閽�߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="type">���f����^���w�肵�܂��B</param>
		public void EmitTypeIs(Type type) { Emit(InstructionFactory.GetFactory(type).TypeIs()); }

		/// <summary>�I�u�W�F�N�g�̎w�肳�ꂽ�^�ւ̕ϊ������݁A���s�����ꍇ�� <c>null</c> ��Ԃ����߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="type">�ϊ���̌^���w�肵�܂��B</param>
		public void EmitTypeAs(Type type) { Emit(InstructionFactory.GetFactory(type).TypeAs()); }

		static readonly ConcurrentDictionary<FieldInfo, Instruction> _loadFields = new ConcurrentDictionary<FieldInfo, Instruction>();

		/// <summary>�w�肳�ꂽ�t�B�[���h�̒l��]���X�^�b�N�ɓǂݍ��ޖ��߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="field">�l���ǂݍ��܂��t�B�[���h���w�肵�܂��B</param>
		public void EmitLoadField(FieldInfo field) { Emit(_loadFields.GetOrAdd(field, x => x.IsStatic ? (Instruction)new LoadStaticFieldInstruction(x) : new LoadFieldInstruction(x))); }

		/// <summary>�w�肳�ꂽ�t�B�[���h�ɕ]���X�^�b�N����l���i�[���閽�߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="field">�l���i�[�����t�B�[���h���w�肵�܂��B</param>
		public void EmitStoreField(FieldInfo field) { Emit(field.IsStatic ? (Instruction)new StoreStaticFieldInstruction(field) : new StoreFieldInstruction(field)); }

		/// <summary>�w�肳�ꂽ���\�b�h���Ăяo�����߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="method">�Ăяo�����\�b�h���w�肵�܂��B</param>
		public void EmitCall(MethodInfo method) { EmitCall(method, method.GetParameters()); }

		/// <summary>�w�肳�ꂽ���\�b�h���Ăяo�����߂����̖��߃��X�g�ɒǉ����܂��B�������̔z��𖾎��I�Ɏw��ł��܂��B</summary>
		/// <param name="method">�Ăяo�����\�b�h���w�肵�܂��B</param>
		/// <param name="parameters">���\�b�h�̉�������\�� <see cref="ParameterInfo"/> �̔z����w�肵�܂��B</param>
		public void EmitCall(MethodInfo method, ParameterInfo[] parameters) { Emit(CallInstruction.Create(method, parameters)); }

		/// <summary>�w�肳�ꂽ�f���Q�[�g�^����� <see cref="CallSiteBinder"/> ���g�p���ē��I�Ăяo�����s�����߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="type">���I�Ăяo���T�C�g�̃f���Q�[�g�^���w�肵�܂��B</param>
		/// <param name="binder">���I����̃o�C���f�B���O���s�� <see cref="CallSiteBinder"/> ���w�肵�܂��B</param>
		public void EmitDynamic(Type type, CallSiteBinder binder) { Emit(CreateDynamicInstruction(type, binder)); }

		static readonly Dictionary<Type, Func<CallSiteBinder, Instruction>> _factories = new Dictionary<Type, Func<CallSiteBinder, Instruction>>();

		/// <summary>�w�肳�ꂽ�f���Q�[�g�^����� <see cref="CallSiteBinder"/> ���g�p���ē��I�Ăяo�����s�����߂��쐬���܂��B</summary>
		/// <param name="delegateType">���I�Ăяo���T�C�g�̃f���Q�[�g�^���w�肵�܂��B</param>
		/// <param name="binder">���I����̃o�C���f�B���O���s�� <see cref="CallSiteBinder"/> ���w�肵�܂��B</param>
		/// <returns>�쐬���ꂽ���I�Ăяo�����s�����߁B</returns>
		internal static Instruction CreateDynamicInstruction(Type delegateType, CallSiteBinder binder)
		{
			Func<CallSiteBinder, Instruction> factory;
			lock (_factories)
			{
				if (!_factories.TryGetValue(delegateType, out factory))
				{
					if (delegateType.GetMethod("Invoke").ReturnType == typeof(void))
						// TODO: We should generally support void returning binders but the only ones that exist are delete index/member who's perf isn't that critical.
						return new DynamicInstructionN(delegateType, CallSite.Create(delegateType, binder), true);
					var instructionType = DynamicInstructionN.GetDynamicInstructionType(delegateType);
					if (instructionType == null)
						return new DynamicInstructionN(delegateType, CallSite.Create(delegateType, binder));
					_factories[delegateType] = factory = (Func<CallSiteBinder, Instruction>)Delegate.CreateDelegate(typeof(Func<CallSiteBinder, Instruction>), instructionType.GetMethod("Factory"));
				}
			}
			return factory(binder);
		}

		static readonly RuntimeLabel[] EmptyRuntimeLabels = new RuntimeLabel[] { new RuntimeLabel(Interpreter.RethrowOnReturn, 0, 0) };

		RuntimeLabel[] BuildRuntimeLabels()
		{
			if (_runtimeLabelCount == 0)
				return EmptyRuntimeLabels;
			var result = new RuntimeLabel[_runtimeLabelCount + 1];
			foreach (var label in _labels)
			{
				if (label.HasRuntimeLabel)
					result[label.LabelIndex] = label.ToRuntimeLabel();
			}
			// "return and rethrow" label:
			result[result.Length - 1] = new RuntimeLabel(Interpreter.RethrowOnReturn, 0, 0);
			return result;
		}

		/// <summary>�V�������x�����쐬���܂��B</summary>
		/// <returns>�쐬���ꂽ���x���B</returns>
		public BranchLabel MakeLabel()
		{
			if (_labels == null)
				_labels = new List<BranchLabel>();
			var label = new BranchLabel();
			_labels.Add(label);
			return label;
		}

		/// <summary>�w�肳�ꂽ�ʒu�ɂ���I�t�Z�b�g���򖽗߂̃I�t�Z�b�g�����肵�܂��B</summary>
		/// <param name="branchIndex">�I�t�Z�b�g���򖽗߂̈ʒu���w�肵�܂��B</param>
		/// <param name="offset">�I�t�Z�b�g���򖽗߂̕����I�t�Z�b�g���w�肵�܂��B</param>
		/// <param name="targetContinuationDepth">�W�����v��̌p���̐[�����w�肵�܂��B</param>
		/// <param name="targetStackDepth">�W�����v��̃X�^�b�N�̐[�����w�肵�܂��B</param>
		internal void FixupBranch(int branchIndex, int offset, int targetContinuationDepth, int targetStackDepth) { _instructions[branchIndex] = ((OffsetInstruction)_instructions[branchIndex]).Fixup(offset, targetContinuationDepth, targetStackDepth); }

		int EnsureLabelIndex(BranchLabel label)
		{
			if (label.HasRuntimeLabel)
				return label.LabelIndex;
			return label.LabelIndex = _runtimeLabelCount++;
		}

		/// <summary>�����^�C�����x�������݂̈ʒu�ɐݒ肵�āA���x���C���f�b�N�X��Ԃ��܂��B</summary>
		/// <returns>�}�[�N���ꂽ�����^�C�����x����\���C���f�b�N�X�B</returns>
		public int MarkRuntimeLabel()
		{
			var handlerLabel = MakeLabel();
			MarkLabel(handlerLabel);
			return EnsureLabelIndex(handlerLabel);
		}

		/// <summary>�w�肳�ꂽ���x�������݂̈ʒu�ɐݒ肵�܂��B</summary>
		/// <param name="label">�ʒu��ݒ肷�郉�x�����w�肵�܂��B</param>
		public void MarkLabel(BranchLabel label) { label.Mark(this); }

		/// <summary>�w�肳�ꂽ���x���ɃW�����v���� goto ���߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="label">�W�����v��̃��x�����w�肵�܂��B</param>
		/// <param name="hasResult">���̕��򂪌��ʂ������ǂ����������l���w�肵�܂��B</param>
		/// <param name="hasValue">���̕��򂪒l��]�����邩�ǂ����������l���w�肵�܂��B</param>
		public void EmitGoto(BranchLabel label, bool hasResult, bool hasValue) { Emit(GotoInstruction.Create(EnsureLabelIndex(label), hasResult, hasValue)); }

		void EmitBranch(OffsetInstruction instruction, BranchLabel label)
		{
			Emit(instruction);
			label.AddBranch(this, Count - 1);
		}

		/// <summary>�w�肳�ꂽ���x���ɃW�����v���閳�������򖽗߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="label">�W�����v��̃��x�����w�肵�܂��B</param>
		public void EmitBranch(BranchLabel label) { EmitBranch(new BranchInstruction(), label); }

		/// <summary>�w�肳�ꂽ���x���ɃW�����v���閳�������򖽗߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="label">�W�����v��̃��x�����w�肵�܂��B</param>
		/// <param name="hasResult">���̕��򂪌��ʂ������ǂ����������l���w�肵�܂��B</param>
		/// <param name="hasValue">���̕��򂪒l��]�����邩�ǂ����������l���w�肵�܂��B</param>
		public void EmitBranch(BranchLabel label, bool hasResult, bool hasValue) { EmitBranch(new BranchInstruction(hasResult, hasValue), label); }

		/// <summary>�X�^�b�N�g�b�v�̒l�� null �łȂ���Ύw�肳�ꂽ���x���ɃW�����v���邪�X�^�b�N�͏���Ȃ����򖽗߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="leftNotNull">�X�^�b�N�g�b�v�̒l�� null �łȂ���΃W�����v���郉�x�����w�肵�܂��B</param>
		public void EmitCoalescingBranch(BranchLabel leftNotNull) { EmitBranch(new CoalescingBranchInstruction(), leftNotNull); }

		/// <summary>�X�^�b�N�g�b�v�̒l�� <c>true</c> �ł���Ύw�肳�ꂽ���x���ɃW�����v���镪�򖽗߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="elseLabel">�X�^�b�N�g�b�v�̒l�� <c>true</c> �ł���ꍇ�ɃW�����v���郉�x�����w�肵�܂��B</param>
		public void EmitBranchTrue(BranchLabel elseLabel) { EmitBranch(new BranchTrueInstruction(), elseLabel); }

		/// <summary>�X�^�b�N�g�b�v�̒l�� <c>false</c> �ł���Ύw�肳�ꂽ���x���ɃW�����v���镪�򖽗߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="elseLabel">�X�^�b�N�g�b�v�̒l�� <c>false</c> �ł���ꍇ�ɃW�����v���郉�x�����w�肵�܂��B</param>
		public void EmitBranchFalse(BranchLabel elseLabel) { EmitBranch(new BranchFalseInstruction(), elseLabel); }

		/// <summary>�l�����P���ȃX���[���߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		public void EmitThrow() { Emit(ThrowInstruction.Throw); }

		/// <summary>�l�������Ȃ��P���ȃX���[���߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		public void EmitThrowVoid() { Emit(ThrowInstruction.VoidThrow); }

		/// <summary>�l�����ăX���[���߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		public void EmitRethrow() { Emit(ThrowInstruction.Rethrow); }

		/// <summary>�l�������Ȃ��ăX���[���߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		public void EmitRethrowVoid() { Emit(ThrowInstruction.VoidRethrow); }

		/// <summary>finally �u���b�N�̊J�n�ʒu���������x�����w�肵�� try-finally �u���b�N�̊J�n���������߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="finallyStartLabel">finally �u���b�N�̊J�n�ʒu���������x�����w�肵�܂��B</param>
		public void EmitEnterTryFinally(BranchLabel finallyStartLabel) { Emit(EnterTryFinallyInstruction.Create(EnsureLabelIndex(finallyStartLabel))); }

		/// <summary>finally �u���b�N�̊J�n���������߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		public void EmitEnterFinally() { Emit(EnterFinallyInstruction.Instance); }

		/// <summary>finally �u���b�N�̏I�����������߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		public void EmitLeaveFinally() { Emit(LeaveFinallyInstruction.Instance); }

		/// <summary>try �u���b�N�{�̂��l�������ǂ����������l���w�肵�āAfault ��O�n���h���̏I�����������߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="hasValue">try �u���b�N�{�̂��l�������ǂ����������l���w�肵�܂��B</param>
		public void EmitLeaveFault(bool hasValue) { Emit(hasValue ? LeaveFaultInstruction.NonVoid : LeaveFaultInstruction.Void); }

		/// <summary>try �u���b�N�{�̂��l�����ꍇ�ɗ�O�n���h���̊J�n���������߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		public void EmitEnterExceptionHandlerNonVoid() { Emit(EnterExceptionHandlerInstruction.NonVoid); }

		/// <summary>try �u���b�N�{�̂��l�������Ȃ��ꍇ�ɗ�O�n���h���̊J�n���������߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		public void EmitEnterExceptionHandlerVoid() { Emit(EnterExceptionHandlerInstruction.Void); }

		/// <summary>��O�n���h���̏I�����������߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="hasValue">��O�n���h���{�̂��l�������ǂ����������l���w�肵�܂��B</param>
		/// <param name="tryExpressionEndLabel">try ���̏I�����������x�����w�肵�܂��B</param>
		public void EmitLeaveExceptionHandler(bool hasValue, BranchLabel tryExpressionEndLabel) { Emit(LeaveExceptionHandlerInstruction.Create(EnsureLabelIndex(tryExpressionEndLabel), hasValue)); }

		/// <summary>�l�ƈړ���I�t�Z�b�g�̃}�b�s���O���w�肵�� switch ���߂����̖��߃��X�g�ɒǉ����܂��B</summary>
		/// <param name="cases">�l����ړ���I�t�Z�b�g�ւ̃}�b�s���O���w�肵�܂��B</param>
		public void EmitSwitch(Dictionary<int, int> cases) { Emit(new SwitchInstruction(cases)); }
	}
}
