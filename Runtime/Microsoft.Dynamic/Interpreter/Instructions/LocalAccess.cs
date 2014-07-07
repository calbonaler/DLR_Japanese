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
	/// <summary>値を参照に置き換えることができる命令を表します。</summary>
	interface IBoxableInstruction
	{
		/// <summary>指定されたインデックスが命令が対象とするインデックスと一致した場合に値を参照に置き換えた命令を取得します。</summary>
		/// <param name="index">命令が対象とするインデックスであるかどうかを調べるインデックスを指定します。</param>
		/// <returns>インデックスが命令が対象とするインデックスと一致した場合は値を参照に置き換えた命令。それ以外の場合は <c>null</c>。</returns>
		Instruction BoxIfIndexMatches(int index);
	}

	/// <summary>ローカル変数にアクセスする命令の基本クラスを表します。</summary>
	abstract class LocalAccessInstruction : Instruction
	{
		/// <summary>アクセスするローカル変数を指定して、<see cref="Microsoft.Scripting.Interpreter.LocalAccessInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="index">アクセスするローカル変数のインデックスを指定します。</param>
		protected LocalAccessInstruction(int index) { Index = index; }
		
		/// <summary>アクセスするローカル変数を示すインデックスを取得します。</summary>
		internal int Index { get; private set; }

		/// <summary>このオブジェクトのデバッグ用文字列表現を取得します。</summary>
		/// <param name="instructionIndex">この命令の命令インデックスを指定します。</param>
		/// <param name="cookie">デバッグ用 Cookie を指定します。</param>
		/// <param name="labelIndexer">ラベルを表すインデックスからラベルの遷移先インデックスを取得するデリゲートを指定します。</param>
		/// <param name="objects">デバッグ用 Cookie のリストを指定します。</param>
		/// <returns>デバッグ用文字列表現。</returns>
		public override string ToDebugString(int instructionIndex, object cookie, Func<int, int> labelIndexer, IList<object> objects) { return cookie == null ? InstructionName + "(" + Index + ")" : InstructionName + "(" + cookie + ": " + Index + ")"; }
	}

	/// <summary>指定されたローカル変数を評価スタックに読み込む命令を表します。</summary>
	sealed class LoadLocalInstruction : LocalAccessInstruction, IBoxableInstruction
	{
		/// <summary>読み込むローカル変数を使用して、<see cref="Microsoft.Scripting.Interpreter.LoadLocalInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="index">読み込むローカル変数を示すインデックスを指定します。</param>
		internal LoadLocalInstruction(int index) : base(index) { }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Push(frame.Data[Index]);
			return +1;
		}

		/// <summary>指定されたインデックスが命令が対象とするインデックスと一致した場合に値を参照に置き換えた命令を取得します。</summary>
		/// <param name="index">命令が対象とするインデックスであるかどうかを調べるインデックスを指定します。</param>
		/// <returns>インデックスが命令が対象とするインデックスと一致した場合は値を参照に置き換えた命令。それ以外の場合は <c>null</c>。</returns>
		public Instruction BoxIfIndexMatches(int index) { return index == Index ? InstructionList.LoadLocalBoxed(index) : null; }
	}

	/// <summary>指定されたローカル変数が参照する値を評価スタックに読み込む命令を表します。</summary>
	sealed class LoadLocalBoxedInstruction : LocalAccessInstruction
	{
		/// <summary>参照する値を読み込むローカル変数を使用して、<see cref="Microsoft.Scripting.Interpreter.LoadLocalBoxedInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="index">参照する値を読み込むローカル変数を示すインデックスを指定します。</param>
		internal LoadLocalBoxedInstruction(int index) : base(index) { }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Push(((StrongBox<object>)frame.Data[Index]).Value);
			return +1;
		}
	}

	/// <summary>指定されたローカル変数の値をクロージャから評価スタックに読み込む命令を表します。</summary>
	sealed class LoadLocalFromClosureInstruction : LocalAccessInstruction
	{
		/// <summary>読み込むローカル変数を使用して、<see cref="Microsoft.Scripting.Interpreter.LoadLocalFromClosureInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="index">クロージャから値を読み込むローカル変数のインデックスを指定します。</param>
		internal LoadLocalFromClosureInstruction(int index) : base(index) { }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Push(frame.Closure[Index].Value);
			return +1;
		}
	}

	/// <summary>指定されたローカル変数の参照をクロージャから評価スタックに読み込む命令を表します。</summary>
	sealed class LoadLocalFromClosureBoxedInstruction : LocalAccessInstruction
	{
		/// <summary>読み込むローカル変数を使用して、<see cref="Microsoft.Scripting.Interpreter.LoadLocalFromClosureBoxedInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="index">クロージャから参照を読み込むローカル変数のインデックスを指定します。</param>
		internal LoadLocalFromClosureBoxedInstruction(int index) : base(index) { }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Push(frame.Closure[Index]);
			return +1;
		}
	}

	/// <summary>指定されたローカル変数に値を消費せずに割り当てる命令を表します。</summary>
	sealed class AssignLocalInstruction : LocalAccessInstruction, IBoxableInstruction
	{
		/// <summary>値を割り当てるローカル変数を使用して、<see cref="Microsoft.Scripting.Interpreter.AssignLocalInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="index">値を割り当てるローカル変数を示すインデックスを指定します。</param>
		internal AssignLocalInstruction(int index) : base(index) { }

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Data[Index] = frame.Peek();
			return +1;
		}

		/// <summary>指定されたインデックスが命令が対象とするインデックスと一致した場合に値を参照に置き換えた命令を取得します。</summary>
		/// <param name="index">命令が対象とするインデックスであるかどうかを調べるインデックスを指定します。</param>
		/// <returns>インデックスが命令が対象とするインデックスと一致した場合は値を参照に置き換えた命令。それ以外の場合は <c>null</c>。</returns>
		public Instruction BoxIfIndexMatches(int index) { return index == Index ? InstructionList.AssignLocalBoxed(index) : null; }
	}

	/// <summary>指定されたローカル変数に値を格納する命令を表します。</summary>
	sealed class StoreLocalInstruction : LocalAccessInstruction, IBoxableInstruction
	{
		/// <summary>値を格納するローカル変数を使用して、<see cref="Microsoft.Scripting.Interpreter.StoreLocalInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="index">値を格納するローカル変数を示すインデックスを指定します。</param>
		internal StoreLocalInstruction(int index) : base(index) { }

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Data[Index] = frame.Pop();
			return +1;
		}

		/// <summary>指定されたインデックスが命令が対象とするインデックスと一致した場合に値を参照に置き換えた命令を取得します。</summary>
		/// <param name="index">命令が対象とするインデックスであるかどうかを調べるインデックスを指定します。</param>
		/// <returns>インデックスが命令が対象とするインデックスと一致した場合は値を参照に置き換えた命令。それ以外の場合は <c>null</c>。</returns>
		public Instruction BoxIfIndexMatches(int index) { return index == Index ? InstructionList.StoreLocalBoxed(index) : null; }
	}

	/// <summary>指定されたローカル変数が参照する値にスタックから値を消費せずに割り当てる命令を表します。</summary>
	sealed class AssignLocalBoxedInstruction : LocalAccessInstruction
	{
		/// <summary>参照する値を割り当てるローカル変数を使用して、<see cref="Microsoft.Scripting.Interpreter.AssignLocalBoxedInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="index">参照する値を割り当てるローカル変数を示すインデックスを指定します。</param>
		internal AssignLocalBoxedInstruction(int index) : base(index) { }

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			((StrongBox<object>)frame.Data[Index]).Value = frame.Peek();
			return +1;
		}
	}

	/// <summary>指定されたローカル変数が参照する値にスタックから値を格納する命令を表します。</summary>
	sealed class StoreLocalBoxedInstruction : LocalAccessInstruction
	{
		/// <summary>参照する値を格納するローカル変数を使用して、<see cref="Microsoft.Scripting.Interpreter.StoreLocalBoxedInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="index">参照する値を格納するローカル変数を示すインデックスを指定します。</param>
		internal StoreLocalBoxedInstruction(int index) : base(index) { }

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			((StrongBox<object>)frame.Data[Index]).Value = frame.Pop();
			return +1;
		}
	}

	/// <summary>指定されたローカル変数にクロージャを使用して値を消費せずに割り当てる命令を表します。</summary>
	sealed class AssignLocalToClosureInstruction : LocalAccessInstruction
	{
		/// <summary>値を割り当てるローカル変数を使用して、<see cref="Microsoft.Scripting.Interpreter.AssignLocalToClosureInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="index">クロージャを使用して値を割り当てるローカル変数を示すインデックスを指定します。</param>
		internal AssignLocalToClosureInstruction(int index) : base(index) { }

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Closure[Index].Value = frame.Peek();
			return +1;
		}
	}

	/// <summary>ローカル変数を初期化する命令の基本クラスを表します。</summary>
	abstract class InitializeLocalInstruction : LocalAccessInstruction
	{
		/// <summary>初期化するローカル変数を使用して、<see cref="Microsoft.Scripting.Interpreter.InitializeLocalInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="index">初期化するローカル変数を示すインデックスを指定します。</param>
		protected InitializeLocalInstruction(int index) : base(index) { }

		/// <summary>ローカル変数を既定の参照 (<c>null</c>) で初期化する命令を表します。</summary>
		internal sealed class Reference : InitializeLocalInstruction, IBoxableInstruction
		{
			/// <summary>初期化するローカル変数を使用して、<see cref="Microsoft.Scripting.Interpreter.InitializeLocalInstruction.Reference"/> クラスの新しいインスタンスを初期化します。</summary>
			/// <param name="index">初期化するローカル変数を示すインデックスを指定します。</param>
			internal Reference(int index) : base(index) { }

			/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
			/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
			/// <returns>次に実行する命令へのオフセット。</returns>
			public override int Run(InterpretedFrame frame)
			{
				frame.Data[Index] = null;
				return 1;
			}

			/// <summary>指定されたインデックスが命令が対象とするインデックスと一致した場合に値を参照に置き換えた命令を取得します。</summary>
			/// <param name="index">命令が対象とするインデックスであるかどうかを調べるインデックスを指定します。</param>
			/// <returns>インデックスが命令が対象とするインデックスと一致した場合は値を参照に置き換えた命令。それ以外の場合は <c>null</c>。</returns>
			public Instruction BoxIfIndexMatches(int index) { return index == Index ? InstructionList.InitImmutableRefBox(index) : null; }

			/// <summary>この命令の名前を取得します。</summary>
			public override string InstructionName { get { return "InitRef"; } }
		}

		/// <summary>ローカル変数を指定された不変値で初期化する命令を表します。</summary>
		internal sealed class ImmutableValue : InitializeLocalInstruction, IBoxableInstruction
		{
			readonly object _defaultValue;

			/// <summary>初期化するローカル変数を使用して、<see cref="Microsoft.Scripting.Interpreter.InitializeLocalInstruction.ImmutableValue"/> クラスの新しいインスタンスを初期化します。</summary>
			/// <param name="index">初期化するローカル変数を示すインデックスを指定します。</param>
			/// <param name="defaultValue">ローカル変数を初期化する不変値を指定します。</param>
			internal ImmutableValue(int index, object defaultValue) : base(index) { _defaultValue = defaultValue; }

			/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
			/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
			/// <returns>次に実行する命令へのオフセット。</returns>
			public override int Run(InterpretedFrame frame)
			{
				frame.Data[Index] = _defaultValue;
				return 1;
			}

			/// <summary>指定されたインデックスが命令が対象とするインデックスと一致した場合に値を参照に置き換えた命令を取得します。</summary>
			/// <param name="index">命令が対象とするインデックスであるかどうかを調べるインデックスを指定します。</param>
			/// <returns>インデックスが命令が対象とするインデックスと一致した場合は値を参照に置き換えた命令。それ以外の場合は <c>null</c>。</returns>
			public Instruction BoxIfIndexMatches(int index) { return index == Index ? new ImmutableBox(index, _defaultValue) : null; }

			/// <summary>この命令の名前を取得します。</summary>
			public override string InstructionName { get { return "InitImmutableValue"; } }
		}

		/// <summary>ローカル変数を指定された不変値への参照で初期化する命令を表します。</summary>
		internal sealed class ImmutableBox : InitializeLocalInstruction
		{
			readonly object _defaultValue; // immutable value:

			/// <summary>初期化するローカル変数を使用して、<see cref="Microsoft.Scripting.Interpreter.InitializeLocalInstruction.ImmutableBox"/> クラスの新しいインスタンスを初期化します。</summary>
			/// <param name="index">初期化するローカル変数を示すインデックスを指定します。</param>
			/// <param name="defaultValue">ローカル変数が初期化される参照が示す不変値を指定します。</param>
			internal ImmutableBox(int index, object defaultValue) : base(index) { _defaultValue = defaultValue; }

			/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
			/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
			/// <returns>次に実行する命令へのオフセット。</returns>
			public override int Run(InterpretedFrame frame)
			{
				frame.Data[Index] = new StrongBox<object>(_defaultValue);
				return 1;
			}

			/// <summary>この命令の名前を取得します。</summary>
			public override string InstructionName { get { return "InitImmutableBox"; } }
		}

		/// <summary>ローカル変数を元の値への参照で初期化する命令を表します。</summary>
		internal sealed class ParameterBox : InitializeLocalInstruction
		{
			/// <summary>初期化するローカル変数を使用して、<see cref="Microsoft.Scripting.Interpreter.InitializeLocalInstruction.ParameterBox"/> クラスの新しいインスタンスを初期化します。</summary>
			/// <param name="index">初期化するローカル変数を示すインデックスを指定します。</param>
			public ParameterBox(int index) : base(index) { }

			/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
			/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
			/// <returns>次に実行する命令へのオフセット。</returns>
			public override int Run(InterpretedFrame frame)
			{
				frame.Data[Index] = new StrongBox<object>(frame.Data[Index]);
				return 1;
			}

			/// <summary>この命令の名前を取得します。</summary>
			public override string InstructionName { get { return "InitParameterBox"; } }
		}

		/// <summary>ローカル変数を元の値で初期化する命令を表します。</summary>
		internal sealed class Parameter : InitializeLocalInstruction, IBoxableInstruction
		{
			/// <summary>初期化するローカル変数を使用して、<see cref="Microsoft.Scripting.Interpreter.InitializeLocalInstruction.Parameter"/> クラスの新しいインスタンスを初期化します。</summary>
			/// <param name="index">初期化するローカル変数を示すインデックスを指定します。</param>
			internal Parameter(int index) : base(index) { }
			
			/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
			/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
			/// <returns>次に実行する命令へのオフセット。</returns>
			public override int Run(InterpretedFrame frame) { return 1; } // nop

			/// <summary>指定されたインデックスが命令が対象とするインデックスと一致した場合に値を参照に置き換えた命令を取得します。</summary>
			/// <param name="index">命令が対象とするインデックスであるかどうかを調べるインデックスを指定します。</param>
			/// <returns>インデックスが命令が対象とするインデックスと一致した場合は値を参照に置き換えた命令。それ以外の場合は <c>null</c>。</returns>
			public Instruction BoxIfIndexMatches(int index) { return index == Index ? InstructionList.ParameterBox(index) : null; }

			/// <summary>この命令の名前を取得します。</summary>
			public override string InstructionName { get { return "InitParameter"; } }
		}

		/// <summary>ローカル変数を変更可能な値で初期化する命令を表します。</summary>
		internal sealed class MutableValue : InitializeLocalInstruction, IBoxableInstruction
		{
			readonly Type _type;

			/// <summary>初期化するローカル変数を使用して、<see cref="Microsoft.Scripting.Interpreter.InitializeLocalInstruction.MutableValue"/> クラスの新しいインスタンスを初期化します。</summary>
			/// <param name="index">初期化するローカル変数を示すインデックスを指定します。</param>
			/// <param name="type">初期化時にインスタンス化される型を指定します。この型には既定のコンストラクタが存在する必要があります。</param>
			internal MutableValue(int index, Type type) : base(index) { _type = type; }

			/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
			/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
			/// <returns>次に実行する命令へのオフセット。</returns>
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

			/// <summary>指定されたインデックスが命令が対象とするインデックスと一致した場合に値を参照に置き換えた命令を取得します。</summary>
			/// <param name="index">命令が対象とするインデックスであるかどうかを調べるインデックスを指定します。</param>
			/// <returns>インデックスが命令が対象とするインデックスと一致した場合は値を参照に置き換えた命令。それ以外の場合は <c>null</c>。</returns>
			public Instruction BoxIfIndexMatches(int index) { return index == Index ? new MutableBox(index, _type) : null; }

			/// <summary>この命令の名前を取得します。</summary>
			public override string InstructionName { get { return "InitMutableValue"; } }
		}

		/// <summary>ローカル変数を変更可能な値への参照で初期化する命令を表します。</summary>
		internal sealed class MutableBox : InitializeLocalInstruction
		{
			readonly Type _type;

			/// <summary>初期化するローカル変数を使用して、<see cref="Microsoft.Scripting.Interpreter.InitializeLocalInstruction.MutableBox"/> クラスの新しいインスタンスを初期化します。</summary>
			/// <param name="index">初期化するローカル変数を示すインデックスを指定します。</param>
			/// <param name="type">初期化時にインスタンス化される型を指定します。この型には既定のコンストラクタが存在する必要があります。</param>
			internal MutableBox(int index, Type type) : base(index) { _type = type; }

			/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
			/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
			/// <returns>次に実行する命令へのオフセット。</returns>
			public override int Run(InterpretedFrame frame)
			{
				frame.Data[Index] = new StrongBox<object>(Activator.CreateInstance(_type));
				return 1;
			}
			
			/// <summary>この命令の名前を取得します。</summary>
			public override string InstructionName { get { return "InitMutableBox"; } }
		}
	}

	/// <summary>評価スタックから参照を取得してランタイム変数を取得する命令を表します。</summary>
	sealed class RuntimeVariablesInstruction : Instruction
	{
		readonly int _count;

		/// <summary>取得するランタイム変数の数を使用して、<see cref="Microsoft.Scripting.Interpreter.RuntimeVariablesInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="count">取得するランタイム変数の数を指定します。</param>
		public RuntimeVariablesInstruction(int count) { _count = count; }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return _count; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			var ret = new IStrongBox[_count];
			for (int i = ret.Length - 1; i >= 0; i--)
				ret[i] = (IStrongBox)frame.Pop();
			frame.Push(RuntimeVariables.Create(ret));
			return +1;
		}

		/// <summary>この命令の名前を取得します。</summary>
		public override string InstructionName { get { return "GetRuntimeVariables"; } }
	}
}
