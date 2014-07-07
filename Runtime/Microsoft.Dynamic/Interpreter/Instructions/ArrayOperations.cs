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
	/// <summary>スタック内に存在する要素を使用して配列を新しく作成する命令を表します。</summary>
	/// <typeparam name="TElement">配列要素の型を指定します。</typeparam>
	public sealed class NewArrayInitInstruction<TElement> : Instruction
	{
		readonly int _elementCount;

		/// <summary>初期化に使用する要素数を使用して、<see cref="Microsoft.Scripting.Interpreter.NewArrayInitInstruction&lt;TElement&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="elementCount">初期化に使用され新しく作成される配列のサイズになる要素数を指定します。</param>
		internal NewArrayInitInstruction(int elementCount) { _elementCount = elementCount; }

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return _elementCount; } }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			var array = new TElement[_elementCount];
			for (int i = _elementCount - 1; i >= 0; i--)
				array[i] = (TElement)frame.Pop();
			frame.Push(array);
			return +1;
		}
	}

	/// <summary>スタックから要素数をポップすることでそのサイズの配列を作成する命令を表します。</summary>
	/// <typeparam name="TElement">配列要素の型を指定します。</typeparam>
	public sealed class NewArrayInstruction<TElement> : Instruction
	{
		/// <summary><see cref="Microsoft.Scripting.Interpreter.NewArrayInstruction&lt;TElement&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		internal NewArrayInstruction() { }

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Push(new TElement[(int)frame.Pop()]);
			return +1;
		}
	}

	/// <summary>スタックから各次元の要素数をポップすることで多次元配列を作成する命令を表します。</summary>
	public sealed class NewArrayBoundsInstruction : Instruction
	{
		readonly Type _elementType;
		readonly int _rank;

		/// <summary>配列要素の型および次元を指定して、<see cref="Microsoft.Scripting.Interpreter.NewArrayBoundsInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="elementType">新しく作成される配列の要素型を指定します。</param>
		/// <param name="rank">新しく作成される配列の次元を指定します。</param>
		internal NewArrayBoundsInstruction(Type elementType, int rank)
		{
			_elementType = elementType;
			_rank = rank;
		}

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return _rank; } }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			var lengths = new int[_rank];
			for (int i = _rank - 1; i >= 0; i--)
				lengths[i] = (int)frame.Pop();
			frame.Push(Array.CreateInstance(_elementType, lengths));
			return +1;
		}
	}

	/// <summary>配列の要素を取得する命令を表します。</summary>
	/// <typeparam name="TElement">配列要素の型を指定します。</typeparam>
	public sealed class GetArrayItemInstruction<TElement> : Instruction
	{
		/// <summary><see cref="Microsoft.Scripting.Interpreter.GetArrayItemInstruction&lt;TElement&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		internal GetArrayItemInstruction() { }

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return 2; } }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			var index = (int)frame.Pop();
			var array = (TElement[])frame.Pop();
			frame.Push(array[index]);
			return +1;
		}

		/// <summary>この命令の名前を取得します。</summary>
		public override string InstructionName { get { return "GetArrayItem"; } }
	}

	/// <summary>配列の要素を設定する命令を表します。</summary>
	/// <typeparam name="TElement">配列要素の型を指定します。</typeparam>
	public sealed class SetArrayItemInstruction<TElement> : Instruction
	{
		/// <summary><see cref="Microsoft.Scripting.Interpreter.SetArrayItemInstruction&lt;TElement&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		internal SetArrayItemInstruction() { }

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return 3; } }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 0; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			var value = (TElement)frame.Pop();
			var index = (int)frame.Pop();
			var array = (TElement[])frame.Pop();
			array[index] = value;
			return +1;
		}

		/// <summary>この命令の名前を取得します。</summary>
		public override string InstructionName { get { return "SetArrayItem"; } }
	}
}
