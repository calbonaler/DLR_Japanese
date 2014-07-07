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
	/// <summary>実行時に実際に使用される遷移先ラベルを表します。</summary>
	internal struct RuntimeLabel
	{
		/// <summary>遷移先の命令を示すインデックスを示します。</summary>
		public readonly int Index;
		/// <summary>遷移先のスタックの深さを示します。</summary>
		public readonly int StackDepth;
		/// <summary>遷移先の継続スタックの深さを示します。</summary>
		public readonly int ContinuationStackDepth;

		/// <summary>指定された遷移先の情報を使用して、<see cref="Microsoft.Scripting.Interpreter.RuntimeLabel"/> 構造体の新しいインスタンスを初期化します。</summary>
		/// <param name="index">遷移先の命令を示すインデックスを指定します。</param>
		/// <param name="continuationStackDepth">遷移先の継続スタックの深さを指定します。</param>
		/// <param name="stackDepth">遷移先のスタックの深さを指定します。</param>
		public RuntimeLabel(int index, int continuationStackDepth, int stackDepth)
		{
			Index = index;
			ContinuationStackDepth = continuationStackDepth;
			StackDepth = stackDepth;
		}

		/// <summary>このオブジェクトの文字列表現を取得します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		public override string ToString() { return string.Format("->{0} C({1}) S({2})", Index, ContinuationStackDepth, StackDepth); }
	}

	/// <summary>分岐先のラベルを表します。</summary>
	public sealed class BranchLabel
	{
		/// <summary>インデックスが不明な状態を示す値を表します。</summary>
		internal const int UnknownIndex = int.MinValue;
		/// <summary>遷移先のスタックの深さが不明な状態を示す値を表します。</summary>
		internal const int UnknownDepth = int.MinValue;

		int _stackDepth = UnknownDepth;
		int _continuationStackDepth = UnknownDepth;

		/// <summary>このラベルを命令リストに追加した後に更新する必要があるこのラベルを対象とする分岐命令のインデックスのリスト</summary>
		List<int> _forwardBranchFixups;

		/// <summary><see cref="Microsoft.Scripting.Interpreter.BranchLabel"/> クラスの新しいインスタンスを初期化します。</summary>
		public BranchLabel()
		{
			LabelIndex = UnknownIndex;
			TargetIndex = UnknownIndex;
		}

		/// <summary>このラベルのラベルリスト内でのインデックスを取得します。</summary>
		internal int LabelIndex { get; set; }

		/// <summary>このラベルに <see cref="RuntimeLabel"/> が関連付けられているかどうかを示す値を取得します。</summary>
		internal bool HasRuntimeLabel { get { return LabelIndex != UnknownIndex; } }

		/// <summary>このラベルが対象とする命令を示すインデックスを取得します。</summary>
		internal int TargetIndex { get; private set; }

		/// <summary>このラベルを <see cref="RuntimeLabel"/> に変換します。</summary>
		/// <returns>このラベルに対する <see cref="RuntimeLabel"/>。</returns>
		internal RuntimeLabel ToRuntimeLabel()
		{
			Debug.Assert(TargetIndex != UnknownIndex && _stackDepth != UnknownDepth && _continuationStackDepth != UnknownDepth);
			return new RuntimeLabel(TargetIndex, _continuationStackDepth, _stackDepth);
		}

		/// <summary>このラベルの対象を次に追加される命令の先頭に設定します。</summary>
		/// <param name="instructions">ラベルを設定する命令リストを指定します。</param>
		internal void Mark(InstructionList instructions)
		{
			// TODO: シャドーイングされたラベルをサポートする必要がある
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

		/// <summary>このラベルに分岐するオフセット分岐命令のオフセットを更新できるようにします。</summary>
		/// <param name="instructions">更新するオフセット分岐命令を含む命令リストを指定します。</param>
		/// <param name="branchIndex">このラベルに分岐するオフセット分岐命令を示す命令リスト内のインデックスを指定します。</param>
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
