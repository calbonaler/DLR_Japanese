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
	/// <summary>評価スタックに指定されたオブジェクトを読み込む命令を表します。</summary>
	sealed class LoadObjectInstruction : Instruction
	{
		readonly object _value;

		/// <summary>読み込むオブジェクトを使用して、<see cref="Microsoft.Scripting.Interpreter.LoadObjectInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="value">評価スタックに読み込むオブジェクトを指定します。</param>
		internal LoadObjectInstruction(object value) { _value = value; }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Push(_value);
			return +1;
		}

		/// <summary>このオブジェクトの文字列表現を取得します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		public override string ToString() { return "LoadObject(" + (_value ?? "null") + ")"; }
	}

	/// <summary>評価スタックにキャッシュされたオブジェクトを読み込む命令を表します。</summary>
	sealed class LoadCachedObjectInstruction : Instruction
	{
		readonly uint _index;

		/// <summary>読み込むオブジェクトのキャッシュインデックスを使用して、<see cref="Microsoft.Scripting.Interpreter.LoadCachedObjectInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="index">評価スタックに読み込むオブジェクトのキャッシュインデックスを指定します。</param>
		internal LoadCachedObjectInstruction(uint index) { _index = index; }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Push(frame.Interpreter._objects[_index]);
			return +1;
		}

		/// <summary>このオブジェクトのデバッグ用文字列表現を取得します。</summary>
		/// <param name="instructionIndex">この命令の命令インデックスを指定します。</param>
		/// <param name="cookie">デバッグ用 Cookie を指定します。</param>
		/// <param name="labelIndexer">ラベルを表すインデックスからラベルの遷移先インデックスを取得するデリゲートを指定します。</param>
		/// <param name="objects">デバッグ用 Cookie のリストを指定します。</param>
		/// <returns>デバッグ用文字列表現。</returns>
		public override string ToDebugString(int instructionIndex, object cookie, Func<int, int> labelIndexer, IList<object> objects) { return string.Format("LoadCached({0}: {1})", _index, objects[(int)_index]); }

		/// <summary>このオブジェクトの文字列表現を取得します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		public override string ToString() { return "LoadCached(" + _index + ")"; }
	}

	/// <summary>評価スタックのスタックトップの値を捨てる命令を表します。</summary>
	sealed class PopInstruction : Instruction
	{
		/// <summary>この命令の唯一のインスタンスを示します。</summary>
		internal static readonly PopInstruction Instance = new PopInstruction();

		PopInstruction() { }

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Pop();
			return +1;
		}
	}

	/// <summary>評価スタックのスタックトップの値を複製する命令を表します。</summary>
	sealed class DupInstruction : Instruction
	{
		/// <summary>この命令の唯一のインスタンスを示します。</summary>
		internal readonly static DupInstruction Instance = new DupInstruction();

		DupInstruction() { }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Push(frame.Peek());
			return +1;
		}
	}
}
