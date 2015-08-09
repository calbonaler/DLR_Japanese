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
	/// <summary>単一のローカル変数またはクロージャ変数を表します。</summary>
	public sealed class LocalVariable
	{
		const int IsBoxedFlag = 1;
		const int InClosureFlag = 2;

		int _flags;

		/// <summary>ローカル変数が割り当てられるインタプリタのデータ領域を表すインデックスを取得します。</summary>
		public int Index { get; private set; }

		/// <summary>この変数がボックス化表現であるかどうかを示す値を取得または設定します。</summary>
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

		/// <summary>この変数がクロージャであるかどうかを示す値を取得します。</summary>
		public bool InClosure { get { return (_flags & InClosureFlag) != 0; } }

		/// <summary>この変数がクロージャまたはボックス化表現であるかどうかを示す値を取得します。</summary>
		public bool InClosureOrBoxed { get { return InClosure | IsBoxed; } }

		/// <summary>割り当てるインデックスを使用して、<see cref="Microsoft.Scripting.Interpreter.LocalVariable"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="index">変数を割り当てるインデックスを指定します。</param>
		/// <param name="closure">変数がクロージャであるかどうかを示す値を指定します。</param>
		/// <param name="boxed">変数がボックス化表現であるかどうかを示す値を指定します。</param>
		internal LocalVariable(int index, bool closure, bool boxed)
		{
			Index = index;
			_flags = (closure ? InClosureFlag : 0) | (boxed ? IsBoxedFlag : 0);
		}

		/// <summary>指定されたデータ配列またはクロージャデータからこのローカル変数のデータを表す式を返します。</summary>
		/// <param name="frameData">インタプリタのスタックフレームにおけるデータ配列を表す式を指定します。</param>
		/// <param name="closure">インタプリタのクロージャデータの配列を表す式を指定します。</param>
		/// <returns>このローカル変数の値を読み出す式。</returns>
		internal Expression LoadFromArray(Expression frameData, Expression closure)
		{
			Expression result = Expression.ArrayAccess(InClosure ? closure : frameData, Expression.Constant(Index));
			return IsBoxed ? Expression.Convert(result, typeof(StrongBox<object>)) : result;
		}

		/// <summary>このオブジェクトの文字列表現を取得します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		public override string ToString() { return string.Format("{0}: {1} {2}", Index, IsBoxed ? "ボックス化" : null, InClosure ? "クロージャ内" : null); }
	}

	/// <summary>ローカル変数のデータ配列上での場所と関連付けられた <see cref="ParameterExpression"/> を格納します。</summary>
	struct LocalDefinition
	{
		/// <summary>ローカル変数のデータ配列上の位置を示します。</summary>
		public int Index;
		/// <summary>ローカル変数が関連付けられた <see cref="ParameterExpression"/> を示します。</summary>
		public ParameterExpression Parameter;

		/// <summary>指定されたデータを使用して、<see cref="Microsoft.Scripting.Interpreter.LocalDefinition"/> 構造体の新しいインスタンスを初期化します。</summary>
		/// <param name="localIndex">ローカル変数のデータ配列上の位置を指定します。</param>
		/// <param name="parameter">ローカル変数が関連付けられた <see cref="ParameterExpression"/> を指定します。</param>
		public LocalDefinition(int localIndex, ParameterExpression parameter)
		{
			Index = localIndex;
			Parameter = parameter;
		}
	}

	/// <summary>ローカル変数のリストを表します。</summary>
	public sealed class LocalVariables
	{
		readonly Dictionary<ParameterExpression, VariableScope> _variables = new Dictionary<ParameterExpression, VariableScope>();
		int _localCount;

		/// <summary><see cref="Microsoft.Scripting.Interpreter.LocalVariables"/> クラスの新しいインスタンスを初期化します。</summary>
		internal LocalVariables() { }

		/// <summary>指定された <see cref="ParameterExpression"/> に対応する指定された命令インデックスからスコープが始まるローカル変数を定義します。</summary>
		/// <param name="variable">作成されるローカル変数に対応づけられる <see cref="ParameterExpression"/> を指定します。</param>
		/// <param name="start">作成されるローカル変数のスコープの開始位置を示す命令インデックスを指定します。</param>
		/// <returns>ローカル変数定義を表す <see cref="LocalDefinition"/>。</returns>
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

		/// <summary>指定されたローカル変数定義によって示されるローカル変数のスコープを指定された命令インデックスで終了します。</summary>
		/// <param name="definition">スコープを終了するローカル変数を示すローカル変数定義を指定します。</param>
		/// <param name="end">ローカル変数のスコープの終了位置を示す命令インデックスを指定します。</param>
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

		/// <summary>指定された変数表現をボックス化表現に切り替えます。</summary>
		/// <param name="variable">ボックス化表現に切り替える変数を表す <see cref="ParameterExpression"/> を指定します。</param>
		/// <param name="instructions">現在の変数を使用している命令が格納された <see cref="InstructionList"/> を指定します。</param>
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

		/// <summary>現在までに作成したローカル変数の個数を取得します。</summary>
		public int LocalCount { get; private set; }

		/// <summary>指定された <see cref="ParameterExpression"/> に対応するローカル変数のデータ配列内のインデックスを取得します。</summary>
		/// <param name="var">取得する位置にあるローカル変数が対応する <see cref="ParameterExpression"/> を指定します。</param>
		/// <returns><see cref="ParameterExpression"/> に対応するローカル変数のデータ配列内のインデックス。</returns>
		public int GetLocalIndex(ParameterExpression var)
		{
			VariableScope loc;
			return _variables.TryGetValue(var, out loc) ? loc.Variable.Index : -1;
		}

		/// <summary>指定された <see cref="ParameterExpression"/> に対応するローカル変数またはクロージャ変数の取得を試みます。</summary>
		/// <param name="var">取得する変数に対応する <see cref="ParameterExpression"/> を指定します。</param>
		/// <param name="local">取得されたローカル変数またはクロージャ変数を表す <see cref="LocalVariable"/> が格納されます。</param>
		/// <returns>ローカル変数またはクロージャ変数が正常に取得された場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
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

		/// <summary>現在のスコープで定義されているローカル変数のコピーを取得します。</summary>
		/// <returns>このスコープで定義されているローカル変数のコピー。</returns>
		internal Dictionary<ParameterExpression, LocalVariable> CopyLocals() { return _variables.ToDictionary(x => x.Key, x => x.Value.Variable); }

		/// <summary>指定された <see cref="ParameterExpression"/> に対応する変数が現在のスコープで定義されているかどうかを判断します。</summary>
		/// <param name="variable">定義されているかどうかを調べる変数に対応する <see cref="ParameterExpression"/> を指定します。</param>
		/// <returns>指定された <see cref="ParameterExpression"/> に対応する変数が現在のスコープで定義されている場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		internal bool ContainsVariable(ParameterExpression variable) { return _variables.ContainsKey(variable); }

		/// <summary>外側のスコープで定義され現在のスコープで利用可能な変数を取得します。</summary>
		internal Dictionary<ParameterExpression, LocalVariable> ClosureVariables { get; private set; }

		/// <summary>現在のスコープに指定された <see cref="ParameterExpression"/> に対応するクロージャ変数を追加します。</summary>
		/// <param name="variable">追加するクロージャ変数に対応する <see cref="ParameterExpression"/>。</param>
		/// <returns>追加されるクロージャ変数を表す <see cref="LocalVariable"/>。</returns>
		internal LocalVariable AddClosureVariable(ParameterExpression variable)
		{
			if (ClosureVariables == null)
				ClosureVariables = new Dictionary<ParameterExpression, LocalVariable>();
			LocalVariable result = new LocalVariable(ClosureVariables.Count, true, false);
			ClosureVariables.Add(variable, result);
			return result;
		}

		/// <summary>変数が定義されている場所と使用される命令範囲を追跡します。</summary>
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
