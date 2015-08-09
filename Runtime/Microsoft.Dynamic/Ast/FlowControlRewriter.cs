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
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Ast
{
	/// <summary>
	/// このリライターの目的は単純です: 式ツリーは finally/fault を抜けるジャンプ (break, continue, return, goto) を許可しません。
	/// そのため現在のコードの代わりにフラグを格納して、finally/faultの最後にジャンプするコードに置き換えます。
	/// try-finally の最後では、その後正しいラベルにジャンプする分岐を発行します。
	/// 
	/// これをより複雑にするいくつかの事実があります:
	/// 
	///   1. もし finally が外側へのジャンプを含んでいれば、try/catch 内のジャンプも置き換える必要があります。
	///      これは次のような場合をサポートします:
	///          # returns 234
	///          def foo():
	///              try: return 123
	///              finally: return 234 
	///      
	///      ジャンプした後 finally に進みますが、finally はもう一度ジャンプするとしています。
	///      しかし、いったん IL finally が存在すれば、finally のジャンプを無視して元のジャンプに従うことを維持するため、"return 123" を置き換える必要があります。
	///      この物語の教訓: finally 内のあらゆるジャンプを書き換えるならば、try/catch 内のジャンプも同様にする必要がある。
	///      
	///  2. よりよりコードを生成するためには、たった 1 つの状態変数を持つ必要があり、そのため、複数の finally の外にジャンプしなければならない場合は、ジャンプを保持します。
	///     それはこのような場合です:
	///       foo:
	///       try { ... } finally {
	///           try { ... } finally {
	///             ...
	///             if (...) {
	///                 // 以前は goto foo;
	///                 $flow = 1; goto endInnerFinally; 
	///             }
	///             ...
	///             endInnerFinally:
	///           }
	///           switch ($flow) {
	///               case 1: goto endOuterFinally;
	///           }
	///           ...
	///           endOuterFinally:
	///       }
	///       switch ($flow) {
	///         case 1: $flow = 0; goto foo;
	///       }
	///       ...
	/// 
	/// </summary>
	sealed class FlowControlRewriter : ExpressionVisitor
	{
		sealed class BlockInfo
		{
			// このブロックは finally か?
			internal bool InFinally;
			// このブロックはフロー制御を必要としているか?
			internal bool HasFlow { get { return FlowLabel != null; } }
			// このブロック内で定義されたラベル
			// これにより直接ジャンプすればよいのか、サポートを必要としているのかを理解できます。
			internal readonly HashSet<LabelTarget> LabelDefs = new HashSet<LabelTarget>();
			// 2 つのプロパティはフロー制御で何を出力すべきかを教えてくれます。(存在すれば)
			internal HashSet<LabelTarget> NeedFlowLabels;
			// IL でできないジャンプを発行するには、状態変数を設定して FlowLabel にジャンプします。ジャンプの操作は FlowLabel のコード次第です。
			internal LabelTarget FlowLabel;
		}

		struct LabelInfo
		{
			internal readonly int FlowState;
			internal readonly ParameterExpression Variable;
			internal LabelInfo(int index, Type varType)
			{
				FlowState = index;
				Variable = varType != typeof(void) ? Expression.Variable(varType, null) : null;
			}
		}

		readonly Dictionary<LabelTarget, LabelInfo> _labels = new Dictionary<LabelTarget, LabelInfo>();
		readonly Stack<BlockInfo> _blocks = new Stack<BlockInfo>();
		ParameterExpression _flowVariable;

		// Rewriter entry point
		internal Expression Reduce(Expression node)
		{
			_blocks.Push(new BlockInfo());
			node = Visit(node);
			if (_flowVariable != null)
				node = Expression.Block(Enumerable.Repeat(_flowVariable, 1).Concat(_labels.Values.Select(x => x.Variable).Where(x => x != null)), node);
			_blocks.Pop();
			return node;
		}

		void EnsureFlow(BlockInfo block)
		{
			if (_flowVariable == null)
				_flowVariable = Expression.Variable(typeof(int), "$flow");
			if (!block.HasFlow)
			{
				block.FlowLabel = Expression.Label();
				block.NeedFlowLabels = new HashSet<LabelTarget>();
			}
		}

		LabelInfo EnsureLabelInfo(LabelTarget target)
		{
			LabelInfo result;
			if (!_labels.TryGetValue(target, out result))
				_labels.Add(target, result = new LabelInfo(_labels.Count + 1, target.Type));
			return result;
		}

		protected override Expression VisitExtension(Expression node)
		{
			var ffc = node as FinallyFlowControlExpression;
			if (ffc != null)
				return Visit(ffc.Body); // ネストされた finally フロー式をラップ解除し、それらを探索することでよりよりコードを生成
			// (すべての goto と try-finally ブロックの追跡を保持できる) 普通の DLR ツリー上で操作したいので、訪問前に拡張式を縮退
			if (node.CanReduce)
				return Visit(node.Reduce());
			return base.VisitExtension(node);
		}

		protected override Expression VisitLambda<T>(Expression<T> node) { return node; } // ネストされたラムダには再帰しない

		protected override Expression VisitTry(TryExpression node)
		{
			// finally/fault ブロックを最初に訪問
			var block = new BlockInfo { InFinally = true };
			_blocks.Push(block);
			var @finally = Visit(node.Finally);
			var fault = Visit(node.Fault);
			block.InFinally = false;
			var finallyEnd = block.FlowLabel;
			if (finallyEnd != null)
				block.FlowLabel = Expression.Label(); // 新しいターゲットを作成。try 後に発行される
			var @try = Visit(node.Body);
			IList<CatchBlock> handlers = Visit(node.Handlers, VisitCatchBlock);
			_blocks.Pop();
			if (@try == node.Body && handlers == node.Handlers && @finally == node.Finally && fault == node.Fault)
				return node;
			if (!block.HasFlow)
				return Expression.MakeTry(null, @try, @finally, fault, handlers);
			if (node.Type != typeof(void)) // 原理的にサポートはそれほど難しくないが、まだ誰にも必要とされていないので
				throw new NotSupportedException("FinallyFlowControlExpression は非 void 型の TryExpressions をサポートしていません。");
			//  もし finally 内に制御フローがあれば、外側を発行:
			//  try {
			//      // try ブロック本体とすべての catch ハンドル
			//  } catch (Exception all) {
			//      saved = all;
			//  } finally {
			//      finally_body
			//      if (saved != null)
			//          throw saved;
			//  }
			//  fault ハンドラを持っている場合は、これをより良くする。
			//  try {
			//      // try ブロック本体のすべての catch ハンドル
			//  } catch (Exception all) {
			//      fault_body
			//      throw all
			//  }
			if (handlers.Count > 0)
				@try = Expression.MakeTry(null, @try, null, null, handlers);
			var saved = Expression.Variable(typeof(Exception), "$exception");
			var all = Expression.Variable(typeof(Exception), "e");
			if (@finally != null)
			{
				handlers = new[] { Expression.Catch(all, Expression.Block(Expression.Assign(saved, all), Utils.Default(node.Type))) };
				@finally = Expression.Block(
					@finally,
					Expression.Condition(Expression.NotEqual(saved, AstUtils.Constant(null, saved.Type)), Expression.Throw(saved), Utils.Empty())
				);
				if (finallyEnd != null)
					@finally = Expression.Label(finallyEnd, @finally);
			}
			else
			{
				Debug.Assert(fault != null);
				fault = Expression.Block(fault, Expression.Throw(all));
				if (finallyEnd != null)
					fault = Expression.Label(finallyEnd, fault);
				handlers = new[] { Expression.Catch(all, fault) };
				fault = null;
			}
			// フロー制御を発行
			return Expression.Block(new[] { saved }, Expression.MakeTry(null, @try, @finally, fault, handlers), Expression.Label(block.FlowLabel), MakeFlowControlSwitch(block));
		}

		Expression MakeFlowControlSwitch(BlockInfo block) { return Expression.Switch(_flowVariable, null, null, block.NeedFlowLabels.Select(target => Expression.SwitchCase(MakeFlowJump(target), AstUtils.Constant(_labels[target].FlowState)))); }

		// 直接中断できるか、再ディスパッチを要求するかを判断します。
		// 直接中断できる場合は、_flowVariable をリセットし、そうでない場合は次の FlowLabel へジャンプする
		Expression MakeFlowJump(LabelTarget target)
		{
			foreach (var block in _blocks)
			{
				if (block.LabelDefs.Contains(target))
					break;
				if (block.InFinally || block.HasFlow)
				{
					EnsureFlow(block);
					block.NeedFlowLabels.Add(target);
					// 他の finally を抜ける必要があるので、そのフローラベルへジャンプする
					return Expression.Goto(block.FlowLabel);
				}
			}
			// フローが必要なければここに。フラグをリセットし本当の GoTo を発行
			return Expression.Block(Expression.Assign(_flowVariable, AstUtils.Constant(0)), Expression.Goto(target, _labels[target].Variable));
		}

		protected override Expression VisitGoto(GotoExpression node)
		{
			foreach (var block in _blocks)
			{
				if (block.LabelDefs.Contains(node.Target))
					break;
				if (block.InFinally || block.HasFlow)
				{
					EnsureFlow(block);
					block.NeedFlowLabels.Add(node.Target);
					var info = EnsureLabelInfo(node.Target);
					return Expression.Block(
						info.Variable == null ? (node.Value ?? Utils.Empty()) : Expression.Assign(info.Variable, node.Value),
						Expression.Assign(_flowVariable, AstUtils.Constant(info.FlowState)),
						Expression.Goto(block.FlowLabel)
					);
				}
			}
			return base.VisitGoto(node);
		}

		protected override Expression VisitBlock(BlockExpression node)
		{
			// ブロック内のすべてのラベルを捉えて、ブロックのスコープ内に定義します。
			// ブロック内で即時に定義されたラベルはブロック全体で有効になります。
			foreach (var e in node.Expressions)
			{
				var label = e as LabelExpression;
				if (label != null)
					VisitLabelTarget(label.Target);
			}
			return base.VisitBlock(node);
		}

		protected override LabelTarget VisitLabelTarget(LabelTarget node)
		{
			if (node != null)
			{
				EnsureLabelInfo(node);
				_blocks.Peek().LabelDefs.Add(node);
			}
			return node;
		}
	}
}
