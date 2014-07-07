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
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Ast;

namespace Microsoft.Scripting.Interpreter
{
	using AstUtils = Microsoft.Scripting.Ast.Utils;
	using LoopFunc = Func<object[], StrongBox<object>[], InterpretedFrame, int>;

	/// <summary>ループ自体をデリゲートにコンパイルできるようにします。</summary>
	sealed class LoopCompiler : ExpressionVisitor
	{
		struct LoopVariable
		{
			public ExpressionAccess Access;

			// a variable that holds on the strong box for closure variables:
			public ParameterExpression BoxStorage;

			public LoopVariable(ExpressionAccess access, ParameterExpression box)
			{
				Access = access;
				BoxStorage = box;
			}

			public override string ToString() { return Access.ToString() + " " + BoxStorage; }
		}

		readonly ParameterExpression _frameDataVar;
		readonly ParameterExpression _frameClosureVar;
		readonly ParameterExpression _frameVar;
		readonly LabelTarget _returnLabel;
		// locals and closure variables defined outside the loop
		readonly Dictionary<ParameterExpression, LocalVariable> _outerVariables, _closureVariables;
		readonly LoopExpression _loop;
		List<ParameterExpression> _temps;
		// tracks variables that flow in and flow out for initialization and 
		readonly Dictionary<ParameterExpression, LoopVariable> _loopVariables;
		// variables which are defined and used within the loop
		HashSet<ParameterExpression> _loopLocals;

		readonly Dictionary<LabelTarget, BranchLabel> _labelMapping;
		readonly int _loopStartInstructionIndex;
		readonly int _loopEndInstructionIndex;

		/// <summary>ループ、ラベルマッピング、ローカル変数、クロージャ変数、開始および終了時点での命令インデックスを使用して、<see cref="Microsoft.Scripting.Interpreter.LoopCompiler"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="loop">コンパイルするループを表す <see cref="LoopExpression"/> を指定します。</param>
		/// <param name="labelMapping"><see cref="LabelTarget"/> から <see cref="BranchLabel"/> へのマッピングを指定します。</param>
		/// <param name="locals">ローカル変数に対する <see cref="ParameterExpression"/> から <see cref="LocalVariable"/> へのマッピングを指定します。</param>
		/// <param name="closureVariables">クロージャ変数に対する <see cref="ParameterExpression"/> から <see cref="LocalVariable"/> へのマッピングを指定します。</param>
		/// <param name="loopStartInstructionIndex">ループ開始時点での命令インデックスを指定します。</param>
		/// <param name="loopEndInstructionIndex">ループ終了時点での命令インデックスを指定します。</param>
		internal LoopCompiler(LoopExpression loop, Dictionary<LabelTarget, BranchLabel> labelMapping, Dictionary<ParameterExpression, LocalVariable> locals, Dictionary<ParameterExpression, LocalVariable> closureVariables, int loopStartInstructionIndex, int loopEndInstructionIndex)
		{
			_loop = loop;
			_outerVariables = locals;
			_closureVariables = closureVariables;
			_frameDataVar = Expression.Parameter(typeof(object[]));
			_frameClosureVar = Expression.Parameter(typeof(StrongBox<object>[]));
			_frameVar = Expression.Parameter(typeof(InterpretedFrame));
			_loopVariables = new Dictionary<ParameterExpression, LoopVariable>();
			_returnLabel = Expression.Label(typeof(int));
			_labelMapping = labelMapping;
			_loopStartInstructionIndex = loopStartInstructionIndex;
			_loopEndInstructionIndex = loopEndInstructionIndex;
		}

		/// <summary>ループをデリゲートにコンパイルします。</summary>
		/// <returns>コンパイルされたループを表すデリゲート。</returns>
		internal LoopFunc CreateDelegate()
		{
			var loop = (LoopExpression)Visit(_loop);
			var body = new List<Expression>();
			var finallyClause = new List<Expression>();
			foreach (var variable in _loopVariables)
			{
				LocalVariable local;
				if (!_outerVariables.TryGetValue(variable.Key, out local))
					local = _closureVariables[variable.Key];
				var elemRef = local.LoadFromArray(_frameDataVar, _frameClosureVar);
				if (local.InClosureOrBoxed)
				{
					var box = variable.Value.BoxStorage;
					Debug.Assert(box != null);
					body.Add(Expression.Assign(box, elemRef));
					AddTemp(box);
				}
				else
				{
					// Always initialize the variable even if it is only written to.
					// If a write-only variable is actually not assigned during execution of the loop we will still write some value back.
					// This value must be the original value, which we assign at entry.
					body.Add(Expression.Assign(variable.Key, AstUtils.Convert(elemRef, variable.Key.Type)));
					if ((variable.Value.Access & ExpressionAccess.Write) != 0)
						finallyClause.Add(Expression.Assign(elemRef, AstUtils.Box(variable.Key)));
					AddTemp(variable.Key);
				}
			}
			if (finallyClause.Count > 0)
				body.Add(Expression.TryFinally(loop, Expression.Block(finallyClause)));
			else
				body.Add(loop);
			body.Add(Expression.Label(_returnLabel, Expression.Constant(_loopEndInstructionIndex - _loopStartInstructionIndex)));
			return Expression.Lambda<LoopFunc>(
				_temps != null ? Expression.Block(_temps, body) : Expression.Block(body),
				new[] { _frameDataVar, _frameClosureVar, _frameVar }
			).Compile();
		}

		protected override Expression VisitExtension(Expression node)
		{
			// Reduce extensions before we visit them so that we operate on a plain DLR tree,
			// where we know relationships among the nodes (which nodes represent write context etc.).
			if (node.CanReduce)
				return Visit(node.Reduce());
			return base.VisitExtension(node);
		}

		protected override Expression VisitGoto(GotoExpression node)
		{
			BranchLabel label;
			var target = node.Target;
			var value = Visit(node.Value);
			// TODO: Is it possible for an inner reducible node of the loop to rely on nodes produced by reducing outer reducible nodes? 
			// Unknown label => must be within the loop:
			if (!_labelMapping.TryGetValue(target, out label))
				return node.Update(target, value);
			// Known label within the loop:
			if (label.TargetIndex >= _loopStartInstructionIndex && label.TargetIndex < _loopEndInstructionIndex)
				return node.Update(target, value);
			return Expression.Return(_returnLabel,
				value != null ?
					Expression.Call(_frameVar, InterpretedFrame.GotoMethod, Expression.Constant(label.LabelIndex), AstUtils.Box(value)) :
					Expression.Call(_frameVar, InterpretedFrame.VoidGotoMethod, Expression.Constant(label.LabelIndex)),
				node.Type
		   );
		}

		// Gather all outer variables accessed in the loop.
		// Determines which ones are read from and written to. 
		// We will consider a variable as "read" if it is read anywhere in the loop even though 
		// the first operation might actually always be "write". We could do better if we had CFG.

		protected override Expression VisitBlock(BlockExpression node)
		{
			var prevLocals = EnterVariableScope(((BlockExpression)node).Variables);
			var res = base.VisitBlock(node);
			ExitVariableScope(prevLocals);
			return res;
		}

		HashSet<ParameterExpression> EnterVariableScope(ICollection<ParameterExpression> variables)
		{
			var prevLocals = new HashSet<ParameterExpression>(_loopLocals ?? (_loopLocals = new HashSet<ParameterExpression>(variables)));
			_loopLocals.UnionWith(variables);
			return prevLocals;
		}

		protected override CatchBlock VisitCatchBlock(CatchBlock node)
		{
			if (node.Variable != null)
			{
				var prevLocals = EnterVariableScope(new[] { node.Variable });
				var res = base.VisitCatchBlock(node);
				ExitVariableScope(prevLocals);
				return res;
			}
			else
				return base.VisitCatchBlock(node);
		}

		protected override Expression VisitLambda<T>(Expression<T> node)
		{
			var prevLocals = EnterVariableScope(node.Parameters);
			try { return base.VisitLambda<T>(node); }
			finally { ExitVariableScope(prevLocals); }
		}

		void ExitVariableScope(HashSet<ParameterExpression> prevLocals) { _loopLocals = prevLocals; }

		protected override Expression VisitBinary(BinaryExpression node)
		{
			// reduce compound assignments:
			if (node.CanReduce)
				return Visit(node.Reduce());
			Debug.Assert(!node.NodeType.IsReadWriteAssignment());
			var param = node.Left as ParameterExpression;
			if (param != null && node.NodeType == ExpressionType.Assign)
			{
				var left = VisitVariable(param, ExpressionAccess.Write);
				var right = Visit(node.Right);
				// left parameter is a boxed variable:
				if (left.Type != param.Type)
				{
					Debug.Assert(left.Type == typeof(object));
					Expression rightVar;
					if (right.NodeType != ExpressionType.Parameter)
						right = Expression.Assign(rightVar = AddTemp(Expression.Parameter(right.Type)), right); // { left.Value = (object)(rightVar = right), rightVar }
					else
						rightVar = right; // { left.Value = (object)right, right }
					return Expression.Block(node.Update(left, Expression.Convert(right, left.Type)), rightVar);
				}
				else
					return node.Update(left, right);
			}
			else
				return base.VisitBinary(node);
		}

		protected override Expression VisitUnary(UnaryExpression node)
		{
			// reduce inplace increment/decrement:
			if (node.CanReduce)
				return Visit(node.Reduce());
			Debug.Assert(!node.NodeType.IsReadWriteAssignment());
			return base.VisitUnary(node);
		}

		// TODO: if we supported ref/out parameter we would need to override MethodCallExpression, VisitDynamic and VisitNew

		protected override Expression VisitParameter(ParameterExpression node) { return VisitVariable(node, ExpressionAccess.Read); }

		Expression VisitVariable(ParameterExpression node, ExpressionAccess access)
		{
			ParameterExpression box;
			LoopVariable existing;
			LocalVariable loc;
			if (_loopLocals.Contains(node))
				return node; // local to the loop - not propagated in or out
			else if (_loopVariables.TryGetValue(node, out existing))
				_loopVariables[node] = new LoopVariable(existing.Access | access, box = existing.BoxStorage); // existing outer variable that we are already tracking
			else if (_outerVariables.TryGetValue(node, out loc) || (_closureVariables != null && _closureVariables.TryGetValue(node, out loc)))
				// not tracking this variable yet, but defined in outer scope and seen for the 1st time
				_loopVariables[node] = new LoopVariable(access, box = loc.InClosureOrBoxed ? Expression.Parameter(typeof(StrongBox<object>), node.Name) : null);
			else
				return node; // node is a variable defined in a nested lambda -> skip
			if (box != null)
			{
				if ((access & ExpressionAccess.Write) != 0)
				{
					// compound assignments were reduced:
					Debug.Assert((access & ExpressionAccess.Read) == 0);
					// box.Value = (object)rhs
					return LightCompiler.Unbox(box);
				}
				else
					return Expression.Convert(LightCompiler.Unbox(box), node.Type); // (T)box.Value
			}
			return node;
		}

		ParameterExpression AddTemp(ParameterExpression variable)
		{
			(_temps ?? (_temps = new List<ParameterExpression>())).Add(variable);
			return variable;
		}
	}
}
