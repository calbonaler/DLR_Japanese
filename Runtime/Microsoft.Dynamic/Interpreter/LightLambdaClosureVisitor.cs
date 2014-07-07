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
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>
	/// <see cref="LambdaExpression"/> を訪問して、定数を <see cref="StrongBox&lt;T&gt;"/> のフィールドへの直接アクセスに置き換えるリライターを表します。
	/// これは ExpressionQuoter が LambdaCompiler に行っていることとよく似ています。
	/// さらに、インタプリタがするようにデバッグ情報追跡を挿入しています。
	/// </summary>
	sealed class LightLambdaClosureVisitor : ExpressionVisitor
	{
		/// <summary>ローカル変数のマッピング</summary>
		readonly Dictionary<ParameterExpression, LocalVariable> _closureVars;
		/// <summary>インタプリタからの <c>StrongBox&lt;Object&gt;[] closure</c> を格納している変数</summary>
		readonly ParameterExpression _closureArray;
		/// <summary>
		/// ネストされたスコープで定義されている変数のスタック。
		/// ネストされたスコープが変数インスタンスの 1 つをシャドーイングする場合に変数を解決する場合は、ここをまず検索します。
		/// </summary>
		readonly Stack<HashSet<ParameterExpression>> _shadowedVars = new Stack<HashSet<ParameterExpression>>();

		LightLambdaClosureVisitor(Dictionary<ParameterExpression, LocalVariable> closureVariables, ParameterExpression closureArray)
		{
			Assert.NotNull(closureVariables, closureArray);
			_closureArray = closureArray;
			_closureVars = closureVariables;
		}

		/// <summary>
		/// Walks the lambda and produces a higher order function, which can be used to bind the lambda to a closure array from the interpreter.
		/// ラムダ式を探索して高階関数を生成します。関数はラムダをインタプリタからのクロージャに束縛するために使用することができます。
		/// </summary>
		/// <param name="lambda">束縛するラムダ式を指定します。</param>
		/// <param name="closureVariables">外側のスコープで定義されてアクセスされる変数を指定します。</param>
		/// <returns>渡されたクロージャ配列に束縛するデリゲートの生成のために呼び出すことができるデリゲート。</returns>
		internal static Func<StrongBox<object>[], Delegate> BindLambda(LambdaExpression lambda, Dictionary<ParameterExpression, LocalVariable> closureVariables)
		{
			var closure = Expression.Parameter(typeof(StrongBox<object>[]), "closure");
			// Create a higher-order function which fills in the parameters & compile it
			return Expression.Lambda<Func<StrongBox<object>[], Delegate>>(new LightLambdaClosureVisitor(closureVariables, closure).Visit(lambda), closure).Compile();
		}

		protected override Expression VisitLambda<T>(Expression<T> node)
		{
			_shadowedVars.Push(new HashSet<ParameterExpression>(node.Parameters));
			var b = Visit(node.Body);
			_shadowedVars.Pop();
			if (b == node.Body)
				return node;
			return Expression.Lambda<T>(b, node.Name, node.TailCall, node.Parameters);
		}

		protected override Expression VisitBlock(BlockExpression node)
		{
			if (node.Variables.Count > 0)
				_shadowedVars.Push(new HashSet<ParameterExpression>(node.Variables));
			var b = Visit(node.Expressions);
			if (node.Variables.Count > 0)
				_shadowedVars.Pop();
			if (b == node.Expressions)
				return node;
			return Expression.Block(node.Variables, b);
		}

		protected override CatchBlock VisitCatchBlock(CatchBlock node)
		{
			if (node.Variable != null)
				_shadowedVars.Push(new HashSet<ParameterExpression>(new[] { node.Variable }));
			var b = Visit(node.Body);
			var f = Visit(node.Filter);
			if (node.Variable != null)
				_shadowedVars.Pop();
			if (b == node.Body && f == node.Filter)
				return node;
			return Expression.MakeCatchBlock(node.Test, node.Variable, b, f);
		}

		protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
		{
			var boxes = new List<Expression>();
			var vars = new List<ParameterExpression>();
			var indexes = new int[node.Variables.Count];
			for (int i = 0; i < node.Variables.Count; i++)
			{
				var box = GetClosureItem(node.Variables[i], false);
				if (box == null)
				{
					indexes[i] = vars.Count;
					vars.Add(node.Variables[i]);
				}
				else
				{
					indexes[i] = -1 - boxes.Count;
					boxes.Add(box);
				}
			}
			// No variables were rewritten. Just return the original node.
			if (boxes.Count == 0)
				return node;
			var boxesArray = Expression.NewArrayInit(typeof(IStrongBox), boxes);
			// All of them were rewritten. Just return the array, wrapped in a read-only collection.
			if (vars.Count == 0)
				return Expression.Invoke(Expression.Constant((Func<IStrongBox[], IRuntimeVariables>)RuntimeVariables.Create), boxesArray);
			// Otherwise, we need to return an object that merges them
			return Expression.Invoke(AstUtils.Constant((Func<IRuntimeVariables, IRuntimeVariables, int[], IRuntimeVariables>)MergedRuntimeVariables.Create), Expression.RuntimeVariables(vars), boxesArray, AstUtils.Constant(indexes));
		}

		protected override Expression VisitParameter(ParameterExpression node)
		{
			var closureItem = GetClosureItem(node, true);
			if (closureItem == null)
				return node;
			// Convert can go away if we switch to strongly typed StrongBox
			return AstUtils.Convert(closureItem, node.Type);
		}

		protected override Expression VisitBinary(BinaryExpression node)
		{
			if (node.NodeType == ExpressionType.Assign && node.Left.NodeType == ExpressionType.Parameter)
			{
				var variable = (ParameterExpression)node.Left;
				var closureItem = GetClosureItem(variable, true);
				if (closureItem != null)
				{
					// We need to convert to object to store the value in the box.
					return Expression.Block(new[] { variable },
						Expression.Assign(variable, Visit(node.Right)),
						Expression.Assign(closureItem, Ast.Utils.Convert(variable, typeof(object))),
						variable
					);
				}
			}
			return base.VisitBinary(node);
		}

		Expression GetClosureItem(ParameterExpression variable, bool unbox)
		{
			// Skip variables that are shadowed by a nested scope/lambda
			foreach (var hidden in _shadowedVars)
			{
				if (hidden.Contains(variable))
					return null;
			}
			LocalVariable loc;
			if (!_closureVars.TryGetValue(variable, out loc))
				throw new InvalidOperationException("unbound variable: " + variable.Name);
			var result = loc.LoadFromArray(null, _closureArray);
			return unbox ? LightCompiler.Unbox(result) : result;
		}

		protected override Expression VisitExtension(Expression node) { return Visit(node.ReduceExtensions()); } // Reduce extensions now so we can find embedded variables

		/// <summary>変数のリストを提供します。変数は値の読み込みおよび書き込みをサポートします。</summary>
		sealed class MergedRuntimeVariables : IRuntimeVariables
		{
			readonly IRuntimeVariables _first;
			readonly IRuntimeVariables _second;

			// For reach item, the index into the first or second list
			// Positive values mean the first array, negative means the second
			readonly int[] _indexes;

			MergedRuntimeVariables(IRuntimeVariables first, IRuntimeVariables second, int[] indexes)
			{
				_first = first;
				_second = second;
				_indexes = indexes;
			}

			internal static IRuntimeVariables Create(IRuntimeVariables first, IRuntimeVariables second, int[] indexes) { return new MergedRuntimeVariables(first, second, indexes); }

			int IRuntimeVariables.Count { get { return _indexes.Length; } }

			object IRuntimeVariables.this[int index]
			{
				get
				{
					index = _indexes[index];
					return index >= 0 ? _first[index] : _second[-1 - index];
				}
				set
				{
					index = _indexes[index];
					if (index >= 0)
						_first[index] = value;
					else
						_second[-1 - index] = value;
				}
			}
		}
	}
}
