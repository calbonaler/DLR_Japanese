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
	/// <summary>インタプリタの命令の配列を格納します。</summary>
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

	/// <summary>インタプリタの命令のリストを表します。</summary>
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

		/// <summary>指定された命令をこのリストに追加します。</summary>
		/// <param name="instruction">追加する命令を指定します。</param>
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

		/// <summary>指定されたデバッグ用 Cookie を最近追加された命令にアタッチします。</summary>
		[Conditional("DEBUG")]
		public void SetDebugCookie(object cookie)
		{
			if (_debugCookies == null)
				_debugCookies = new List<KeyValuePair<int, object>>();
			Debug.Assert(Count > 0);
			_debugCookies.Add(new KeyValuePair<int, object>(Count - 1, cookie));
		}

		/// <summary>このリストに格納されている命令の数を取得します。</summary>
		public int Count { get { return _instructions.Count; } }

		/// <summary>現在のスタックの深さを取得します。</summary>
		public int CurrentStackDepth { get; private set; }

		/// <summary>現在の継続の深さを取得します。</summary>
		public int CurrentContinuationsDepth { get; private set; }

		/// <summary>この命令リストの命令を実行するのに必要なスタックの深さを取得します。</summary>
		public int MaxStackDepth { get; private set; }

		/// <summary>この命令リストの指定されたインデックスにある命令を取得します。</summary>
		/// <param name="index">取得する命令を示す 0 から始まるインデックスを指定します。</param>
		/// <returns>指定されたインデックスにある命令。</returns>
		internal Instruction GetInstruction(int index) { return _instructions[index]; }

#if STATS
		static Dictionary<string, int> _executedInstructions = new Dictionary<string, int>();
		static Dictionary<string, Dictionary<object, bool>> _instances = new Dictionary<string, Dictionary<object, bool>>();

		static InstructionList()
		{
			AppDomain.CurrentDomain.ProcessExit += new EventHandler((_, __) =>
			{
				PerfTrack.DumpHistogram(_executedInstructions);
				Console.WriteLine("-- 全実行回数: {0}", _executedInstructions.Values.Aggregate(0, (sum, value) => sum + value));
				Console.WriteLine("-----");
				var referenced = new Dictionary<string, int>();
				int total = 0;
				foreach (var entry in _instances)
				{
					referenced[entry.Key] = entry.Value.Count;
					total += entry.Value.Count;
				}
				PerfTrack.DumpHistogram(referenced);
				Console.WriteLine("-- 全参照回数: {0}", total);
				Console.WriteLine("-----");
			});
		}
#endif

		/// <summary>この命令リスト全体を <see cref="InstructionArray"/> として取得します。</summary>
		/// <returns>命令リスト全体を表す <see cref="InstructionArray"/>。</returns>
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

		/// <summary>指定されたオブジェクトをスタックに読み込む命令をこの命令リストに追加します。</summary>
		/// <param name="value">読み込むオブジェクトを指定します。</param>
		public void EmitLoad(object value) { EmitLoad(value, null); }

		/// <summary>指定されたブール値をスタックに読み込む命令をこの命令リストに追加します。</summary>
		/// <param name="value">読み込むブール値を指定します。</param>
		public void EmitLoad(bool value)
		{
			if ((bool)value)
				Emit(_true ?? (_true = new LoadObjectInstruction(ScriptingRuntimeHelpers.True)));
			else
				Emit(_false ?? (_false = new LoadObjectInstruction(ScriptingRuntimeHelpers.False)));
		}

		/// <summary>指定されたオブジェクトを指定された型として読み込む命令をこの命令リストに追加します。</summary>
		/// <param name="value">読み込むオブジェクトを指定します。</param>
		/// <param name="type">読み込むオブジェクトの型を指定します。<c>null</c> を指定することができます。</param>
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

		/// <summary>評価スタックのスタックトップの値を複製する命令をこの命令リストに追加します。</summary>
		public void EmitDup() { Emit(DupInstruction.Instance); }

		/// <summary>評価スタックのスタックトップの値を捨てる命令をこの命令リストに追加します。</summary>
		public void EmitPop() { Emit(PopInstruction.Instance); }

		/// <summary>指定された命令インデックスにある命令が操作するローカルのインデックスが指定された値であれば、その命令を <see cref="StrongBox&lt;T&gt;"/> を使用するものに置き換えます。</summary>
		/// <param name="index"><see cref="StrongBox&lt;T&gt;"/> を使用するものに置き換える命令が操作するローカルのインデックスを指定します。</param>
		/// <param name="instructionIndex">置き換える命令のインデックスを指定します。</param>
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

		/// <summary>指定されたインデックスのローカル変数の値を評価スタックに読み込む命令をこの命令リストに追加します。</summary>
		/// <param name="index">評価スタックに値を読み込むローカル変数のインデックスを指定します。</param>
		public void EmitLoadLocal(int index)
		{
			if (_loadLocal == null)
				_loadLocal = new Instruction[LocalInstrCacheSize];
			Emit(index < _loadLocal.Length ? _loadLocal[index] ?? (_loadLocal[index] = new LoadLocalInstruction(index)) : new LoadLocalInstruction(index));
		}

		/// <summary>指定されたインデックスの <see cref="StrongBox&lt;T&gt;"/> で参照されたローカル変数の値を評価スタックに読み込む命令をこの命令リストに追加します。</summary>
		/// <param name="index">評価スタックに参照された値を読み込むローカル変数のインデックスを指定します。</param>
		public void EmitLoadLocalBoxed(int index) { Emit(LoadLocalBoxed(index)); }

		/// <summary>指定されたインデックスの <see cref="StrongBox&lt;T&gt;"/> で参照されたローカル変数の値を評価スタックに読み込む命令を取得します。</summary>
		/// <param name="index">評価スタックに参照された値を読み込むローカル変数のインデックスを指定します。</param>
		/// <returns>参照されたローカル変数の値を評価スタックに読み込む命令。</returns>
		internal static Instruction LoadLocalBoxed(int index)
		{
			if (_loadLocalBoxed == null)
				_loadLocalBoxed = new Instruction[LocalInstrCacheSize];
			return index < _loadLocalBoxed.Length ? _loadLocalBoxed[index] ?? (_loadLocalBoxed[index] = new LoadLocalBoxedInstruction(index)) : new LoadLocalBoxedInstruction(index);
		}

		/// <summary>指定されたインデックスのローカル変数の値をクロージャから評価スタックに読み込む命令をこの命令リストに追加します。</summary>
		/// <param name="index">評価スタックに値を読み込むローカル変数のインデックスを指定します。</param>
		public void EmitLoadLocalFromClosure(int index)
		{
			if (_loadLocalFromClosure == null)
				_loadLocalFromClosure = new Instruction[LocalInstrCacheSize];
			Emit(index < _loadLocalFromClosure.Length ? _loadLocalFromClosure[index] ?? (_loadLocalFromClosure[index] = new LoadLocalFromClosureInstruction(index)) : new LoadLocalFromClosureInstruction(index));
		}

		/// <summary>指定されたインデックスのローカル変数の参照をクロージャから評価スタックに読み込む命令をこの命令リストに追加します。</summary>
		/// <param name="index">評価スタックに参照を読み込むローカル変数のインデックスを指定します。</param>
		public void EmitLoadLocalFromClosureBoxed(int index)
		{
			if (_loadLocalFromClosureBoxed == null)
				_loadLocalFromClosureBoxed = new Instruction[LocalInstrCacheSize];
			Emit(index < _loadLocalFromClosureBoxed.Length ? _loadLocalFromClosureBoxed[index] ?? (_loadLocalFromClosureBoxed[index] = new LoadLocalFromClosureBoxedInstruction(index)) : new LoadLocalFromClosureBoxedInstruction(index));
		}

		/// <summary>指定されたインデックスのローカル変数に値を消費せずに割り当てる命令をこの命令リストに追加します。</summary>
		/// <param name="index">値が割り当てられるローカル変数のインデックスを指定します。</param>
		public void EmitAssignLocal(int index)
		{
			if (_assignLocal == null)
				_assignLocal = new Instruction[LocalInstrCacheSize];
			Emit(index < _assignLocal.Length ? _assignLocal[index] ?? (_assignLocal[index] = new AssignLocalInstruction(index)) : new AssignLocalInstruction(index));
		}

		/// <summary>指定されたインデックスのローカル変数に値を格納する命令をこの命令リストに追加します。</summary>
		/// <param name="index">値が格納されるローカル変数のインデックスを指定します。</param>
		public void EmitStoreLocal(int index)
		{
			if (_storeLocal == null)
				_storeLocal = new Instruction[LocalInstrCacheSize];
			Emit(index < _storeLocal.Length ? _storeLocal[index] ?? (_storeLocal[index] = new StoreLocalInstruction(index)) : new StoreLocalInstruction(index));
		}

		/// <summary>指定されたインデックスのローカル変数の参照先に値を消費せずに割り当てる命令をこの命令リストに追加します。</summary>
		/// <param name="index">値が参照先に割り当てられるローカル変数のインデックスを指定します。</param>
		public void EmitAssignLocalBoxed(int index) { Emit(AssignLocalBoxed(index)); }

		/// <summary>指定されたインデックスのローカル変数の参照先に値を消費せずに割り当てる命令を取得します。</summary>
		/// <param name="index">値が参照先に割り当てられるローカル変数のインデックスを指定します。</param>
		/// <returns>ローカル変数の参照先に値を消費せずに割り当てる命令。</returns>
		internal static Instruction AssignLocalBoxed(int index)
		{
			if (_assignLocalBoxed == null)
				_assignLocalBoxed = new Instruction[LocalInstrCacheSize];
			return index < _assignLocalBoxed.Length ? _assignLocalBoxed[index] ?? (_assignLocalBoxed[index] = new AssignLocalBoxedInstruction(index)) : new AssignLocalBoxedInstruction(index);
		}

		/// <summary>指定されたインデックスのローカル変数の参照先に値を格納する命令をこの命令リストに追加します。</summary>
		/// <param name="index">値が参照先に格納されるローカル変数のインデックスを指定します。</param>
		public void EmitStoreLocalBoxed(int index) { Emit(StoreLocalBoxed(index)); }

		/// <summary>指定されたインデックスのローカル変数の参照先に値を格納する命令を取得します。</summary>
		/// <param name="index">値が参照先に格納されるローカル変数のインデックスを指定します。</param>
		/// <returns>ローカル変数の参照先に値を格納する命令。</returns>
		internal static Instruction StoreLocalBoxed(int index)
		{
			if (_storeLocalBoxed == null)
				_storeLocalBoxed = new Instruction[LocalInstrCacheSize];
			return index < _storeLocalBoxed.Length ? _storeLocalBoxed[index] ?? (_storeLocalBoxed[index] = new StoreLocalBoxedInstruction(index)) : new StoreLocalBoxedInstruction(index);
		}

		/// <summary>指定されたインデックスのローカル変数にクロージャを使用して値を消費せずに割り当てる命令をこの命令リストに追加します。</summary>
		/// <param name="index">値が割り当てられるローカル変数のインデックスを指定します。</param>
		public void EmitAssignLocalToClosure(int index)
		{
			if (_assignLocalToClosure == null)
				_assignLocalToClosure = new Instruction[LocalInstrCacheSize];
			Emit(index < _assignLocalToClosure.Length ? _assignLocalToClosure[index] ?? (_assignLocalToClosure[index] = new AssignLocalToClosureInstruction(index)) : new AssignLocalToClosureInstruction(index));
		}

		/// <summary>指定されたインデックスのローカル変数にクロージャを使用して値を格納する命令をこの命令リストに追加します。</summary>
		/// <param name="index">値が格納されるローカル変数のインデックスを指定します。</param>
		public void EmitStoreLocalToClosure(int index)
		{
			EmitAssignLocalToClosure(index);
			EmitPop();
		}

		/// <summary>指定されたインデックスのローカル変数を初期化する命令をこの命令リストに追加します。</summary>
		/// <param name="index">初期化するローカル変数のインデックスを指定します。</param>
		/// <param name="type">初期化するローカル変数の型を指定します。</param>
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

		/// <summary>指定されたインデックスのローカル変数を初期化する命令をこの命令リストに追加します。</summary>
		/// <param name="index">初期化するローカル変数のインデックスを指定します。</param>
		internal void EmitInitializeParameter(int index) { Emit(Parameter(index)); }

		/// <summary>指定されたインデックスのローカル変数を初期化する命令を取得します。</summary>
		/// <param name="index">初期化するローカル変数のインデックスを指定します。</param>
		/// <returns>引数を初期化する命令。</returns>
		internal static Instruction Parameter(int index)
		{
			if (_parameter == null)
				_parameter = new Instruction[LocalInstrCacheSize];
			return index < _parameter.Length ? _parameter[index] ?? (_parameter[index] = new InitializeLocalInstruction.Parameter(index)) : new InitializeLocalInstruction.Parameter(index);
		}

		/// <summary>指定されたインデックスのローカル変数を参照として初期化する命令を取得します。</summary>
		/// <param name="index">初期化するローカル変数のインデックスを指定します。</param>
		/// <returns>ローカル変数を参照として初期化する命令。</returns>
		internal static Instruction ParameterBox(int index)
		{
			if (_parameterBox == null)
				_parameterBox = new Instruction[LocalInstrCacheSize];
			return index < _parameterBox.Length ? _parameterBox[index] ?? (_parameterBox[index] = new InitializeLocalInstruction.ParameterBox(index)) : new InitializeLocalInstruction.ParameterBox(index);
		}

		/// <summary>指定されたインデックスのローカル変数を既定の参照で初期化する命令を取得します。</summary>
		/// <param name="index">初期化するローカル変数のインデックスを指定します。</param>
		/// <returns>ローカル変数を既定の参照で初期化する命令。</returns>
		internal static Instruction InitReference(int index)
		{
			if (_initReference == null)
				_initReference = new Instruction[LocalInstrCacheSize];
			return index < _initReference.Length ? _initReference[index] ?? (_initReference[index] = new InitializeLocalInstruction.Reference(index)) : new InitializeLocalInstruction.Reference(index);
		}

		/// <summary>指定されたローカル変数を <c>null</c> を参照する <see cref="StrongBox&lt;T&gt;"/> で初期化する命令を取得します。</summary>
		/// <param name="index">初期化するローカル変数のインデックスを指定します。</param>
		/// <returns>ローカル変数を <c>null</c> を参照する <see cref="StrongBox&lt;T&gt;"/> で初期化する命令。</returns>
		internal static Instruction InitImmutableRefBox(int index)
		{
			if (_initImmutableRefBox == null)
				_initImmutableRefBox = new Instruction[LocalInstrCacheSize];
			return index < _initImmutableRefBox.Length ? _initImmutableRefBox[index] ?? (_initImmutableRefBox[index] = new InitializeLocalInstruction.ImmutableBox(index, null)) : new InitializeLocalInstruction.ImmutableBox(index, null);
		}

		/// <summary>指定された数のランタイム変数を作成する命令をこの命令リストに追加します。</summary>
		/// <param name="count">作成および取得するランタイム変数の数を指定します。</param>
		public void EmitNewRuntimeVariables(int count) { Emit(new RuntimeVariablesInstruction(count)); }

		/// <summary>指定された型の配列の要素を取得する命令をこの命令リストに追加します。</summary>
		/// <param name="arrayType">取得する配列の型を指定します。</param>
		public void EmitGetArrayItem(Type arrayType)
		{
			var elementType = arrayType.GetElementType();
			Emit(elementType.IsClass || elementType.IsInterface ? InstructionFactory<object>.Factory.GetArrayItem() : InstructionFactory.GetFactory(elementType).GetArrayItem());
		}

		/// <summary>指定された型の配列の要素を設定する命令をこの命令リストに追加します。</summary>
		/// <param name="arrayType">設定する配列の型を指定します。</param>
		public void EmitSetArrayItem(Type arrayType)
		{
			var elementType = arrayType.GetElementType();
			Emit(elementType.IsClass || elementType.IsInterface ? InstructionFactory<object>.Factory.SetArrayItem() : InstructionFactory.GetFactory(elementType).SetArrayItem());
		}

		/// <summary>指定された要素型の配列を作成する命令をこの命令リストに追加します。</summary>
		/// <param name="elementType">作成する配列の要素の型を指定します。</param>
		public void EmitNewArray(Type elementType) { Emit(InstructionFactory.GetFactory(elementType).NewArray()); }

		/// <summary>指定された要素型で指定された次元をもつ配列を作成する命令をこの命令リストに追加します。</summary>
		/// <param name="elementType">作成する配列の要素の型を指定します。</param>
		/// <param name="rank">作成する配列の次元を指定します。</param>
		public void EmitNewArrayBounds(Type elementType, int rank) { Emit(new NewArrayBoundsInstruction(elementType, rank)); }

		/// <summary>指定された要素型の配列を指定された数の要素で初期化する命令をこの命令リストに追加します。</summary>
		/// <param name="elementType">作成する配列の要素の型を指定します。</param>
		/// <param name="elementCount">作成する配列を初期化する要素の数を指定します。</param>
		public void EmitNewArrayInit(Type elementType, int elementCount) { Emit(InstructionFactory.GetFactory(elementType).NewArrayInit(elementCount)); }

		/// <summary>指定された型の加算命令をこの命令リストに追加します。</summary>
		/// <param name="type">加算対象のオペランドの型を指定します。</param>
		/// <param name="checked">加算時にオーバーフローをチェックするかどうかを示す値を指定します。</param>
		public void EmitAdd(Type type, bool @checked) { Emit(@checked ? AddOvfInstruction.Create(type) : AddInstruction.Create(type)); }

		/// <summary>指定された型の減算命令をこの命令リストに追加します。</summary>
		/// <param name="type">減算対象のオペランドの型を指定します。</param>
		/// <param name="checked">減算時にオーバーフローをチェックするかどうかを示す値を指定します。</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
		public void EmitSub(Type type, bool @checked) { throw new NotSupportedException(); }

		/// <summary>指定された型の乗算命令をこの命令リストに追加します。</summary>
		/// <param name="type">乗算対象のオペランドの型を指定します。</param>
		/// <param name="checked">乗算時にオーバーフローをチェックするかどうかを示す値を指定します。</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
		public void EmitMul(Type type, bool @checked) { throw new NotSupportedException(); }

		/// <summary>指定された型の除算命令をこの命令リストに追加します。</summary>
		/// <param name="type">除算対象のオペランドの型を指定します。</param>
		public void EmitDiv(Type type) { Emit(DivInstruction.Create(type)); }

		/// <summary>指定された型の等値比較命令をこの命令リストに追加します。</summary>
		/// <param name="type">比較対象のオペランドの型を指定します。</param>
		public void EmitEqual(Type type) { Emit(EqualInstruction.Create(type)); }

		/// <summary>指定された型の不等値比較命令をこの命令リストに追加します。</summary>
		/// <param name="type">比較対象のオペランドの型を指定します。</param>
		public void EmitNotEqual(Type type) { Emit(NotEqualInstruction.Create(type)); }

		/// <summary>指定された型の小なり比較命令をこの命令リストに追加します。</summary>
		/// <param name="type">比較対象のオペランドの型を指定します。</param>
		public void EmitLessThan(Type type) { Emit(LessThanInstruction.Create(type)); }

		/// <summary>指定された型の以下比較命令をこの命令リストに追加します。</summary>
		/// <param name="type">比較対象のオペランドの型を指定します。</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
		public void EmitLessThanOrEqual(Type type) { throw new NotSupportedException(); }

		/// <summary>指定された型の大なり比較命令をこの命令リストに追加します。</summary>
		/// <param name="type">比較対象のオペランドの型を指定します。</param>
		public void EmitGreaterThan(Type type) { Emit(GreaterThanInstruction.Create(type)); }

		/// <summary>指定された型の以上比較命令をこの命令リストに追加します。</summary>
		/// <param name="type">比較対象のオペランドの型を指定します。</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters")]
		public void EmitGreaterThanOrEqual(Type type) { throw new NotSupportedException(); }

		/// <summary>オーバーフローをチェックする数値型の型変換命令をこの命令リストに追加します。</summary>
		/// <param name="from">変換元の型を指定します。</param>
		/// <param name="to">変換先の型を指定します。</param>
		public void EmitNumericConvertChecked(TypeCode from, TypeCode to) { Emit(new NumericConvertInstruction.Checked(from, to)); }

		/// <summary>オーバーフローをチェックしない数値型の型変換命令をこの命令リストに追加します。</summary>
		/// <param name="from">変換元の型を指定します。</param>
		/// <param name="to">変換先の型を指定します。</param>
		public void EmitNumericConvertUnchecked(TypeCode from, TypeCode to) { Emit(new NumericConvertInstruction.Unchecked(from, to)); }

		/// <summary>論理否定命令をこの命令リストに追加します。</summary>
		public void EmitNot() { Emit(NotInstruction.Instance); }

		/// <summary>指定された型の既定値を評価スタックに読み込む命令をこの命令リストに追加します。</summary>
		/// <param name="type">既定値を取得する型を指定します。</param>
		public void EmitDefaultValue(Type type) { Emit(InstructionFactory.GetFactory(type).DefaultValue()); }

		/// <summary>指定されたコンストラクタを使用してインスタンスを作成する命令をこの命令リストに追加します。</summary>
		/// <param name="constructorInfo">インスタンスの作成に使用されるコンストラクタを指定します。</param>
		public void EmitNew(ConstructorInfo constructorInfo) { Emit(new NewInstruction(constructorInfo)); }

		/// <summary>指定された <see cref="LightDelegateCreator"/> を使用してデリゲートを作成する命令をこの命令リストに追加します。</summary>
		/// <param name="creator">デリゲートの作成を管理する <see cref="LightDelegateCreator"/> を指定します。</param>
		internal void EmitCreateDelegate(LightDelegateCreator creator) { Emit(new CreateDelegateInstruction(creator)); }

		/// <summary>オブジェクトの型が提供された型と等しいかどうかを判断する命令をこの命令リストに追加します。</summary>
		public void EmitTypeEquals() { Emit(TypeEqualsInstruction.Instance); }

		/// <summary>オブジェクトが指定された型に変換できるかどうかを判断する命令をこの命令リストに追加します。</summary>
		/// <param name="type">判断する型を指定します。</param>
		public void EmitTypeIs(Type type) { Emit(InstructionFactory.GetFactory(type).TypeIs()); }

		/// <summary>オブジェクトの指定された型への変換を試み、失敗した場合は <c>null</c> を返す命令をこの命令リストに追加します。</summary>
		/// <param name="type">変換先の型を指定します。</param>
		public void EmitTypeAs(Type type) { Emit(InstructionFactory.GetFactory(type).TypeAs()); }

		static readonly ConcurrentDictionary<FieldInfo, Instruction> _loadFields = new ConcurrentDictionary<FieldInfo, Instruction>();

		/// <summary>指定されたフィールドの値を評価スタックに読み込む命令をこの命令リストに追加します。</summary>
		/// <param name="field">値が読み込まれるフィールドを指定します。</param>
		public void EmitLoadField(FieldInfo field) { Emit(_loadFields.GetOrAdd(field, x => x.IsStatic ? (Instruction)new LoadStaticFieldInstruction(x) : new LoadFieldInstruction(x))); }

		/// <summary>指定されたフィールドに評価スタックから値を格納する命令をこの命令リストに追加します。</summary>
		/// <param name="field">値が格納されるフィールドを指定します。</param>
		public void EmitStoreField(FieldInfo field) { Emit(field.IsStatic ? (Instruction)new StoreStaticFieldInstruction(field) : new StoreFieldInstruction(field)); }

		/// <summary>指定されたメソッドを呼び出す命令をこの命令リストに追加します。</summary>
		/// <param name="method">呼び出すメソッドを指定します。</param>
		public void EmitCall(MethodInfo method) { EmitCall(method, method.GetParameters()); }

		/// <summary>指定されたメソッドを呼び出す命令をこの命令リストに追加します。仮引数の配列を明示的に指定できます。</summary>
		/// <param name="method">呼び出すメソッドを指定します。</param>
		/// <param name="parameters">メソッドの仮引数を表す <see cref="ParameterInfo"/> の配列を指定します。</param>
		public void EmitCall(MethodInfo method, ParameterInfo[] parameters) { Emit(CallInstruction.Create(method, parameters)); }

		/// <summary>指定されたデリゲート型および <see cref="CallSiteBinder"/> を使用して動的呼び出しを行う命令をこの命令リストに追加します。</summary>
		/// <param name="type">動的呼び出しサイトのデリゲート型を指定します。</param>
		/// <param name="binder">動的操作のバインディングを行う <see cref="CallSiteBinder"/> を指定します。</param>
		public void EmitDynamic(Type type, CallSiteBinder binder) { Emit(CreateDynamicInstruction(type, binder)); }

		static readonly Dictionary<Type, Func<CallSiteBinder, Instruction>> _factories = new Dictionary<Type, Func<CallSiteBinder, Instruction>>();

		/// <summary>指定されたデリゲート型および <see cref="CallSiteBinder"/> を使用して動的呼び出しを行う命令を作成します。</summary>
		/// <param name="delegateType">動的呼び出しサイトのデリゲート型を指定します。</param>
		/// <param name="binder">動的操作のバインディングを行う <see cref="CallSiteBinder"/> を指定します。</param>
		/// <returns>作成された動的呼び出しを行う命令。</returns>
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

		/// <summary>新しいラベルを作成します。</summary>
		/// <returns>作成されたラベル。</returns>
		public BranchLabel MakeLabel()
		{
			if (_labels == null)
				_labels = new List<BranchLabel>();
			var label = new BranchLabel();
			_labels.Add(label);
			return label;
		}

		/// <summary>指定された位置にあるオフセット分岐命令のオフセットを決定します。</summary>
		/// <param name="branchIndex">オフセット分岐命令の位置を指定します。</param>
		/// <param name="offset">オフセット分岐命令の分岐先オフセットを指定します。</param>
		/// <param name="targetContinuationDepth">ジャンプ先の継続の深さを指定します。</param>
		/// <param name="targetStackDepth">ジャンプ先のスタックの深さを指定します。</param>
		internal void FixupBranch(int branchIndex, int offset, int targetContinuationDepth, int targetStackDepth) { _instructions[branchIndex] = ((OffsetInstruction)_instructions[branchIndex]).Fixup(offset, targetContinuationDepth, targetStackDepth); }

		int EnsureLabelIndex(BranchLabel label)
		{
			if (label.HasRuntimeLabel)
				return label.LabelIndex;
			return label.LabelIndex = _runtimeLabelCount++;
		}

		/// <summary>ランタイムラベルを現在の位置に設定して、ラベルインデックスを返します。</summary>
		/// <returns>マークされたランタイムラベルを表すインデックス。</returns>
		public int MarkRuntimeLabel()
		{
			var handlerLabel = MakeLabel();
			MarkLabel(handlerLabel);
			return EnsureLabelIndex(handlerLabel);
		}

		/// <summary>指定されたラベルを現在の位置に設定します。</summary>
		/// <param name="label">位置を設定するラベルを指定します。</param>
		public void MarkLabel(BranchLabel label) { label.Mark(this); }

		/// <summary>指定されたラベルにジャンプする goto 命令をこの命令リストに追加します。</summary>
		/// <param name="label">ジャンプ先のラベルを指定します。</param>
		/// <param name="hasResult">この分岐が結果をもつかどうかを示す値を指定します。</param>
		/// <param name="hasValue">この分岐が値を転送するかどうかを示す値を指定します。</param>
		public void EmitGoto(BranchLabel label, bool hasResult, bool hasValue) { Emit(GotoInstruction.Create(EnsureLabelIndex(label), hasResult, hasValue)); }

		void EmitBranch(OffsetInstruction instruction, BranchLabel label)
		{
			Emit(instruction);
			label.AddBranch(this, Count - 1);
		}

		/// <summary>指定されたラベルにジャンプする無条件分岐命令をこの命令リストに追加します。</summary>
		/// <param name="label">ジャンプ先のラベルを指定します。</param>
		public void EmitBranch(BranchLabel label) { EmitBranch(new BranchInstruction(), label); }

		/// <summary>指定されたラベルにジャンプする無条件分岐命令をこの命令リストに追加します。</summary>
		/// <param name="label">ジャンプ先のラベルを指定します。</param>
		/// <param name="hasResult">この分岐が結果をもつかどうかを示す値を指定します。</param>
		/// <param name="hasValue">この分岐が値を転送するかどうかを示す値を指定します。</param>
		public void EmitBranch(BranchLabel label, bool hasResult, bool hasValue) { EmitBranch(new BranchInstruction(hasResult, hasValue), label); }

		/// <summary>スタックトップの値が null でなければ指定されたラベルにジャンプするがスタックは消費しない分岐命令をこの命令リストに追加します。</summary>
		/// <param name="leftNotNull">スタックトップの値が null でなければジャンプするラベルを指定します。</param>
		public void EmitCoalescingBranch(BranchLabel leftNotNull) { EmitBranch(new CoalescingBranchInstruction(), leftNotNull); }

		/// <summary>スタックトップの値が <c>true</c> であれば指定されたラベルにジャンプする分岐命令をこの命令リストに追加します。</summary>
		/// <param name="elseLabel">スタックトップの値が <c>true</c> である場合にジャンプするラベルを指定します。</param>
		public void EmitBranchTrue(BranchLabel elseLabel) { EmitBranch(new BranchTrueInstruction(), elseLabel); }

		/// <summary>スタックトップの値が <c>false</c> であれば指定されたラベルにジャンプする分岐命令をこの命令リストに追加します。</summary>
		/// <param name="elseLabel">スタックトップの値が <c>false</c> である場合にジャンプするラベルを指定します。</param>
		public void EmitBranchFalse(BranchLabel elseLabel) { EmitBranch(new BranchFalseInstruction(), elseLabel); }

		/// <summary>値をもつ単純なスロー命令をこの命令リストに追加します。</summary>
		public void EmitThrow() { Emit(ThrowInstruction.Throw); }

		/// <summary>値をもたない単純なスロー命令をこの命令リストに追加します。</summary>
		public void EmitThrowVoid() { Emit(ThrowInstruction.VoidThrow); }

		/// <summary>値をもつ再スロー命令をこの命令リストに追加します。</summary>
		public void EmitRethrow() { Emit(ThrowInstruction.Rethrow); }

		/// <summary>値をもたない再スロー命令をこの命令リストに追加します。</summary>
		public void EmitRethrowVoid() { Emit(ThrowInstruction.VoidRethrow); }

		/// <summary>finally ブロックの開始位置を示すラベルを指定して try-finally ブロックの開始を示す命令をこの命令リストに追加します。</summary>
		/// <param name="finallyStartLabel">finally ブロックの開始位置を示すラベルを指定します。</param>
		public void EmitEnterTryFinally(BranchLabel finallyStartLabel) { Emit(EnterTryFinallyInstruction.Create(EnsureLabelIndex(finallyStartLabel))); }

		/// <summary>finally ブロックの開始を示す命令をこの命令リストに追加します。</summary>
		public void EmitEnterFinally() { Emit(EnterFinallyInstruction.Instance); }

		/// <summary>finally ブロックの終了を示す命令をこの命令リストに追加します。</summary>
		public void EmitLeaveFinally() { Emit(LeaveFinallyInstruction.Instance); }

		/// <summary>try ブロック本体が値をもつかどうかを示す値を指定して、fault 例外ハンドラの終了を示す命令をこの命令リストに追加します。</summary>
		/// <param name="hasValue">try ブロック本体が値をもつかどうかを示す値を指定します。</param>
		public void EmitLeaveFault(bool hasValue) { Emit(hasValue ? LeaveFaultInstruction.NonVoid : LeaveFaultInstruction.Void); }

		/// <summary>try ブロック本体が値をもつ場合に例外ハンドラの開始を示す命令をこの命令リストに追加します。</summary>
		public void EmitEnterExceptionHandlerNonVoid() { Emit(EnterExceptionHandlerInstruction.NonVoid); }

		/// <summary>try ブロック本体が値をもたない場合に例外ハンドラの開始を示す命令をこの命令リストに追加します。</summary>
		public void EmitEnterExceptionHandlerVoid() { Emit(EnterExceptionHandlerInstruction.Void); }

		/// <summary>例外ハンドラの終了を示す命令をこの命令リストに追加します。</summary>
		/// <param name="hasValue">例外ハンドラ本体が値をもつかどうかを示す値を指定します。</param>
		/// <param name="tryExpressionEndLabel">try 式の終了を示すラベルを指定します。</param>
		public void EmitLeaveExceptionHandler(bool hasValue, BranchLabel tryExpressionEndLabel) { Emit(LeaveExceptionHandlerInstruction.Create(EnsureLabelIndex(tryExpressionEndLabel), hasValue)); }

		/// <summary>値と移動先オフセットのマッピングを指定して switch 命令をこの命令リストに追加します。</summary>
		/// <param name="cases">値から移動先オフセットへのマッピングを指定します。</param>
		public void EmitSwitch(Dictionary<int, int> cases) { Emit(new SwitchInstruction(cases)); }
	}
}
