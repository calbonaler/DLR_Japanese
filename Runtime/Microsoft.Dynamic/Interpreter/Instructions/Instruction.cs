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
	/// <summary>インタプリタにオブジェクトが表す命令を提供できることを表します。</summary>
	public interface IInstructionProvider
	{
		/// <summary>指定されたインタプリタにこのオブジェクトが表す命令を追加します。</summary>
		/// <param name="compiler">命令を追加するインタプリタを指定します。</param>
		void AddInstructions(LightCompiler compiler);
	}

	/// <summary>インタプリタの命令を表します。</summary>
	public abstract partial class Instruction
	{
		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public virtual int ConsumedStack { get { return 0; } }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public virtual int ProducedStack { get { return 0; } }

		/// <summary>この命令で消費される継続の数を取得します。</summary>
		public virtual int ConsumedContinuations { get { return 0; } }

		/// <summary>この命令で生成される継続の数を取得します。</summary>
		public virtual int ProducedContinuations { get { return 0; } }

		/// <summary>この命令の前後でのスタックの要素数の増減を取得します。</summary>
		public int StackBalance { get { return ProducedStack - ConsumedStack; } }

		/// <summary>この命令の前後での継続の増減を取得します。</summary>
		public int ContinuationsBalance { get { return ProducedContinuations - ConsumedContinuations; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public abstract int Run(InterpretedFrame frame);

		/// <summary>この命令の名前を取得します。</summary>
		public virtual string InstructionName { get { return GetType().Name.Replace("Instruction", ""); } }

		/// <summary>このオブジェクトの文字列表現を取得します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		public override string ToString() { return InstructionName + "()"; }

		/// <summary>このオブジェクトのデバッグ用文字列表現を取得します。</summary>
		/// <param name="instructionIndex">この命令の命令インデックスを指定します。</param>
		/// <param name="cookie">デバッグ用 Cookie を指定します。</param>
		/// <param name="labelIndexer">ラベルを表すインデックスからラベルの遷移先インデックスを取得するデリゲートを指定します。</param>
		/// <param name="objects">デバッグ用 Cookie のリストを指定します。</param>
		/// <returns>デバッグ用文字列表現。</returns>
		public virtual string ToDebugString(int instructionIndex, object cookie, Func<int, int> labelIndexer, IList<object> objects) { return ToString(); }

		/// <summary>このオブジェクトの指定されたコンパイラに対するデバッグ用 Cookie を取得します。</summary>
		/// <param name="compiler">デバッグ用 Cookie を取得するコンパイラを指定します。</param>
		/// <returns>デバッグ用 Cookie。</returns>
		public virtual object GetDebugCookie(LightCompiler compiler) { return null; }
	}

	/// <summary>論理否定命令を表します。</summary>
	sealed class NotInstruction : Instruction
	{
		/// <summary>この命令の唯一のインスタンスを表します。</summary>
		public static readonly Instruction Instance = new NotInstruction();

		NotInstruction() { }

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Push((bool)frame.Pop() ? ScriptingRuntimeHelpers.False : ScriptingRuntimeHelpers.True);
			return +1;
		}
	}
}
