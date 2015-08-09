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
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Ast
{
	/// <summary>
	/// yield return または yield break を見つけた場合に、このリライターは含んでいるブロック、スコープ、そして式をスタックの状態にしたがって平坦化します。
	/// すべての遭遇したスコープはジェネレータのクロージャに昇格された変数を持つため、yield を越えて生き残らせることができます。
	/// </summary>
	sealed class GeneratorRewriter : ExpressionVisitor
	{
		// 2 つの定数は内部的に使用されます。これらは有効な yield 状態の場合は矛盾しません。
		const int GotoRouterYielding = 0;
		const int GotoRouterNone = -1;
		// 開始前および完了時のジェネレータの状態です。
		internal const int NotStarted = -1;
		internal const int Finished = 0;

		sealed class YieldMarker
		{
			// 注釈: ラベルは try ブロックを生成するごとに変化します。
			internal LabelTarget Label = Expression.Label();
			internal readonly int State;
			internal YieldMarker(int state) { State = state; }
		}

		sealed class LabelInfo
		{
			internal readonly LabelTarget NewLabel;
			internal readonly ParameterExpression Temp;
			internal LabelInfo(LabelTarget old)
			{
				NewLabel = Expression.Label(old.Name);
				Temp = Expression.Parameter(old.Type, old.Name);
			}
		}

		readonly GeneratorExpression _generator;
		readonly ParameterExpression _current;
		readonly ParameterExpression _state;

		// finally 内部の場合は 1 つ以上のリターンラベルを表します
		readonly Stack<LabelTarget> _returnLabels = new Stack<LabelTarget>();
		ParameterExpression _gotoRouter;
		bool _inTryWithFinally;
		readonly List<YieldMarker> _yields = new List<YieldMarker>();
		List<int> _debugCookies;
		readonly HashSet<ParameterExpression> _vars = new HashSet<ParameterExpression>();
		// 可能な最適化: 一時変数の再使用。変数を適切にスコープして、フリーリスト内の変数を書き戻す必要があります。
		readonly List<ParameterExpression> _temps = new List<ParameterExpression>();
		// 値のある goto をサポートする変数。
		Dictionary<LabelTarget, LabelInfo> _labelTemps;

		internal GeneratorRewriter(GeneratorExpression generator)
		{
			_generator = generator;
			_state = Expression.Parameter(typeof(int).MakeByRefType(), "state");
			_current = Expression.Parameter(_generator.Target.Type.MakeByRefType(), "current");
			_returnLabels.Push(Expression.Label());
			_gotoRouter = Expression.Variable(typeof(int), "$gotoRouter");
		}

		internal Expression Reduce()
		{
			// 本体を訪問
			var body = Visit(_generator.Body);
			Debug.Assert(_returnLabels.Count == 1);
			// GeneratorNext<T> に対するラムダを作成。ラムダ外部のスコープにある変数を巻き上げます。
			// クローズオーバーされる必要がない一時変数を収集
			body = Expression.Block(_vars.Concat(_temps),
				Expression.Lambda(typeof(GeneratorNext<>).MakeGenericType(_generator.Target.Type),
					Expression.Block(
						Enumerable.Repeat(_gotoRouter, 1).Concat(_labelTemps != null ? _labelTemps.Values.Select(x => x.Temp) : Enumerable.Empty<ParameterExpression>()),
						Expression.Switch(
							Expression.Assign(_gotoRouter, _state),
							_yields.Select(x => Expression.SwitchCase(Expression.Goto(x.Label), AstUtils.Constant(x.State)))
							.Concat(Enumerable.Repeat(Expression.SwitchCase(Expression.Goto(_returnLabels.Peek()), AstUtils.Constant(Finished)), 1)).ToArray()
						),
						body,
						Expression.Assign(_state, AstUtils.Constant(Finished)),
						Expression.Label(_returnLabels.Peek())
					),
					_generator.Name, new[] { _state, _current }
				)
			);
			// 列挙ファクトリは GeneratorNext<T> の代わりに Func<GeneratorNext<T>> をとる。
			if (_generator.IsEnumerable)
				body = Expression.Lambda(body);
			// 定数がすでにリライトされた後でツリーを探索するので、ここでは _debugCookies 配列の ConstantExpression を作成できません。
			// 代わりに、配列を _debugCookies からの内容で配列で初期化する NewArrayExpression を作成します。
			var targetMethodExample = new Func<GeneratorNext<int>, IEnumerator<int>>(ScriptingRuntimeHelpers.MakeGenerator).Method.GetGenericMethodDefinition();
			return Expression.Call(targetMethodExample.DeclaringType, targetMethodExample.Name, new[] { _generator.Target.Type },
				_debugCookies != null ? new[] { body, Expression.NewArrayInit(typeof(int), _debugCookies.Select(x => AstUtils.Constant(x))) } : new[] { body }
			);
		}

		YieldMarker GetYieldMarker(YieldExpression node)
		{
			YieldMarker result = new YieldMarker(_yields.Count + 1);
			_yields.Add(result);
			if (node.YieldMarker != -1)
			{
				if (_debugCookies == null)
				{
					_debugCookies = new List<int>(1);
					_debugCookies.Add(int.MaxValue);
				}
				_debugCookies.Insert(result.State, node.YieldMarker);
			}
			else if (_debugCookies != null)
				_debugCookies.Insert(result.State, int.MaxValue);
			return result;
		}

		/// <summary>変数への値の代入を作成します。内部へのジャンプを可能にするために、可能な限り右辺の代入をプッシュします。</summary>
		Expression MakeAssign(ParameterExpression variable, Expression value)
		{
			// TODO: これは不完全です。
			// これらのノードが yield または return (Switch, Loop, Goto, Label) を含む場合、これは正しくないツリーを生成して停止する可能性があります。
			// これらはサポートされませんが、(これ以外の他の式が yield を含む場合) 適切な使用を認めずに完了する可能性があるため、ここでは例外をスローできません。
			switch (value.NodeType)
			{
				case ExpressionType.Block:
					return MakeAssignBlock(variable, (BlockExpression)value);
				case ExpressionType.Conditional:
					return MakeAssignConditional(variable, (ConditionalExpression)value);
			}
			return Expression.Assign(variable, value);
		}

		Expression MakeAssignBlock(ParameterExpression variable, BlockExpression node) { return Expression.Block(node.Variables, node.Expressions.Select((x, i) => i == node.Expressions.Count - 1 ? MakeAssign(variable, x) : x)); }

		Expression MakeAssignConditional(ParameterExpression variable, ConditionalExpression node) { return Expression.Condition(node.Test, MakeAssign(variable, node.IfTrue), MakeAssign(variable, node.IfFalse)); }

		#region VisitTry

		protected override Expression VisitTry(TryExpression node)
		{
			var startYields = _yields.Count;
			var savedInTryWithFinally = _inTryWithFinally;
			if (node.Finally != null || node.Fault != null)
				_inTryWithFinally = true;
			var @try = Visit(node.Body);
			var tryYields = _yields.Count;
			IList<CatchBlock> handlers = Visit(node.Handlers, VisitCatchBlock);
			var catchYields = _yields.Count;
			// finally ブロックの yield の場合に、新しい return ラベルをプッシュ
			_returnLabels.Push(Expression.Label());
			// これらのうちたった 1 つは null にならない
			var @finally = Visit(node.Finally);
			var fault = Visit(node.Fault);
			var finallyReturn = _returnLabels.Pop();
			var finallyYields = _yields.Count;
			_inTryWithFinally = savedInTryWithFinally;
			if (@try == node.Body && handlers == node.Handlers && @finally == node.Finally && fault == node.Fault)
				return node;
			// yield ではなくただの return
			if (startYields == _yields.Count)
				return Expression.MakeTry(null, @try, @finally, fault, handlers);
			if (fault != null && finallyYields != catchYields)
				throw new NotSupportedException("fault ブロックにおける yield はサポートされていません。"); // 誰もこれを必要とせず、fault に戻る方法が明確でない
			// try に yield があれば、yield ラベルを発行する新しい try 本体を構築する必要がある
			var tryStart = Expression.Label();
			if (tryYields != startYields)
				@try = Expression.Block(MakeYieldRouter(startYields, tryYields, tryStart), @try);
			// yield のある catch を延期したハンドラに変換
			if (catchYields != tryYields)
			{
				var block = new List<Expression>();
				block.Add(MakeYieldRouter(tryYields, catchYields, tryStart));
				block.Add(null); // あとで埋める空のスロット
				for (int i = 0, n = handlers.Count; i < n; i++)
				{
					var c = handlers[i];
					if (c == node.Handlers[i])
						continue;
					if (handlers.IsReadOnly)
						handlers = handlers.ToArray();
					// catch ブロックにスコープされた変数
					var exceptionVar = Expression.Variable(c.Test, null);
					// catch ブロック本体が例外へのアクセスに使用する変数
					// catch ブロックに元の変数があった場合再使用します。
					// catch は yield を含んでいる可能性があるため、これは巻き上げられます。
					var deferredVar = c.Variable ?? Expression.Variable(c.Test, null);
					_vars.Add(deferredVar);
					// フィルターが例外変数に確実にアクセスできるようにする必要があります。
					// catch (ExceptionType exceptionVar) {
					//     deferredVar = exceptionVar;
					// }
					handlers[i] = Expression.Catch(exceptionVar,
						Utils.Void(Expression.Assign(deferredVar, exceptionVar)),
						c.Filter != null && c.Variable != null ? Expression.Block(new[] { c.Variable }, Expression.Assign(c.Variable, exceptionVar), c.Filter) : c.Filter
					);
					// 再スローを "throw defferedVar" に書き換える必要があります。
					// if (deferredVar != null) {
					//     ... catch body ...
					// }
					block.Add(Expression.IfThen(Expression.NotEqual(deferredVar, AstUtils.Constant(null, deferredVar.Type)), new RethrowRewriter(deferredVar).Visit(c.Body)));
				}
				block[1] = Expression.MakeTry(null, @try, null, null, handlers);
				@try = Expression.Block(block);
				handlers = new CatchBlock[0]; // so we don't reuse these
			}
			if (finallyYields != catchYields)
			{
				// 例外を保存する catch ブロックを加える必要があるので、finally に yield がある場合は再スローできます。
				// さらに、返却のロジックも加えます。次のようになります:
				//
				// try { ... } catch (Exception all) { saved = all; }
				// finally {
				//   if (_finallyReturnVar) goto finallyReturn;
				//   ...
				//   if (saved != null) throw saved;
				//   finallyReturn:
				// }
				// if (_finallyReturnVar) goto _return;

				// catch(Exception) を加える必要があるため、catch がある場合は、try でラップします。
				if (handlers.Count > 0)
				{
					@try = Expression.MakeTry(null, @try, null, null, handlers);
					handlers = new CatchBlock[0];
				}
				// 注釈: これらのルーターの順序は重要です。
				// 最初の呼び出しは "tryEnd" に位置するすべてのラベルを変更し、次のルーターは "tryEnd" へジャンプします。
				var tryEnd = Expression.Label();
				var inFinallyRouter = MakeYieldRouter(catchYields, finallyYields, tryEnd);
				var inTryRouter = MakeYieldRouter(catchYields, finallyYields, tryStart);
				var all = Expression.Variable(typeof(Exception), "e");
				var saved = Expression.Variable(typeof(Exception), "$saved$" + _temps.Count);
				_temps.Add(saved);
				@try = Expression.Block(
					Expression.TryCatchFinally(
						Expression.Block(
							inTryRouter,
							@try,
							Expression.Assign(saved, AstUtils.Constant(null, saved.Type)),
							Expression.Label(tryEnd)
						),
						Expression.Block(
							MakeSkipFinallyBlock(finallyReturn),
							inFinallyRouter,
							@finally,
							Expression.Condition(Expression.NotEqual(saved, AstUtils.Constant(null, saved.Type)),
								Expression.Throw(saved),
								Utils.Empty()
							),
							Expression.Label(finallyReturn)
						),
						Expression.Catch(all, Utils.Void(Expression.Assign(saved, all)))
					),
					Expression.Condition(Expression.Equal(_gotoRouter, AstUtils.Constant(GotoRouterYielding)),
						Expression.Goto(_returnLabels.Peek()),
						Utils.Empty()
					)
				);
				@finally = null;
			}
			else if (@finally != null)
				// try または catch が yield を含んでいれば、finally を修正し、スキップできるようにします。
				@finally = Expression.Block(MakeSkipFinallyBlock(finallyReturn), @finally, Expression.Label(finallyReturn));
			// 必要であれば外側の try を作成
			if (handlers.Count > 0 || @finally != null || fault != null)
				@try = Expression.MakeTry(null, @try, @finally, fault, handlers);
			return Expression.Block(Expression.Label(tryStart), @try);
		}

		class RethrowRewriter : ExpressionVisitor
		{
			public RethrowRewriter(ParameterExpression exception) { _exception = exception; }

			readonly ParameterExpression _exception;

			protected override Expression VisitUnary(UnaryExpression node) { return node.NodeType == ExpressionType.Throw && node.Operand == null ? Expression.Throw(_exception, node.Type) : base.VisitUnary(node); }

			protected override Expression VisitLambda<T>(Expression<T> node) { return node; } // ラムダには再帰しない

			protected override Expression VisitTry(TryExpression node) { return node; } // 他の try には再帰しない
		}

		// yield 中であれば finally ブロックをスキップしますが、yield break を実行中の際は行いません。
		Expression MakeSkipFinallyBlock(LabelTarget target)
		{
			return Expression.Condition(
				Expression.AndAlso(
					Expression.Equal(_gotoRouter, AstUtils.Constant(GotoRouterYielding)),
					Expression.NotEqual(_state, AstUtils.Constant(Finished))
				),
				Expression.Goto(target),
				Utils.Empty()
			);
		}

		// 基本の実装からコピーされました。
		// フィルター内の yield を除外するために必要です。
		protected override CatchBlock VisitCatchBlock(CatchBlock node)
		{
			var v = VisitAndConvert(node.Variable, "VisitCatchBlock");
			int yields = _yields.Count;
			var f = Visit(node.Filter);
			if (yields != _yields.Count)
				throw new NotSupportedException("filter における yield は許可されていません。"); // No one needs this yet, and it's not clear what it should even do
			var b = Visit(node.Body);
			if (v == node.Variable && b == node.Body && f == node.Filter)
				return node;
			return Expression.MakeCatchBlock(node.Test, v, b, f);
		}

		#endregion

		SwitchExpression MakeYieldRouter(int start, int end, LabelTarget newTarget)
		{
			Debug.Assert(end > start);
			var cases = new SwitchCase[end - start];
			for (int i = start; i < end; i++)
			{
				cases[i - start] = Expression.SwitchCase(Expression.Goto(_yields[i].Label), AstUtils.Constant(_yields[i].State));
				// 外側の switch ステートメントからのあらゆるジャンプは (適切にジャンプできない) 元のラベルではなく、このルーターに入るべきです。
				_yields[i].Label = newTarget;
			}
			return Expression.Switch(_gotoRouter, cases);
		}

		protected override Expression VisitExtension(Expression node)
		{
			var yield = node as YieldExpression;
			if (yield != null)
				return VisitYield(yield);
			var ffc = node as FinallyFlowControlExpression;
			if (ffc != null)
				return Visit(node.ReduceExtensions());
			// 適切なスタックスピリングを保証するために縮退しなければなりません。
			return Visit(node.ReduceExtensions());
		}

		Expression VisitYield(YieldExpression node)
		{
			if (node.Target != _generator.Target)
				throw new InvalidOperationException("yield とジェネレータは同じ LabelTarget オブジェクトを共有している必要があります。");
			var value = Visit(node.Value);
			var block = new List<Expression>();
			if (value == null)
			{
				// Yield break
				block.Add(Expression.Assign(_state, AstUtils.Constant(Finished)));
				if (_inTryWithFinally)
					block.Add(Expression.Assign(_gotoRouter, AstUtils.Constant(GotoRouterYielding)));
				block.Add(Expression.Goto(_returnLabels.Peek()));
				return Expression.Block(block);
			}
			// Yield return
			block.Add(MakeAssign(_current, value));
			var marker = GetYieldMarker(node);
			block.Add(Expression.Assign(_state, AstUtils.Constant(marker.State)));
			if (_inTryWithFinally)
				block.Add(Expression.Assign(_gotoRouter, AstUtils.Constant(GotoRouterYielding)));
			block.Add(Expression.Goto(_returnLabels.Peek()));
			block.Add(Expression.Label(marker.Label));
			block.Add(Expression.Assign(_gotoRouter, AstUtils.Constant(GotoRouterNone)));
			block.Add(Utils.Empty());
			return Expression.Block(block);
		}

		protected override Expression VisitBlock(BlockExpression node)
		{
			var yields = _yields.Count;
			var b = Visit(node.Expressions);
			if (b == node.Expressions)
				return node;
			if (yields == _yields.Count)
				return Expression.Block(node.Type, node.Variables, b);
			// 後のために変数を保存 (ラムダの外側で巻き上げられる)
			_vars.UnionWith(node.Variables);
			// すべての変数が取り除かれた以外は書き換えられた本体で新しいブロックを返す。
			return Expression.Block(node.Type, b);
		}

		protected override Expression VisitLambda<T>(Expression<T> node) { return node; } // ネストされたラムダには再帰しない

		#region 値のある goto サポート

		// リライターは式を一時変数に代入します。
		// 式が値のあるラベルであれば、返される式は代入の右辺にジャンプできないため、不正な式ツリーとなります。
		// したがって、値のあるラベルおよび goto を取り除く必要があります。
		// MakeAssign で使用されるものをリライトする必要はありますが、すべてリライトするよりは簡単です。
		//
		// var = label[L](value1)
		// ...
		// goto[L](value2)
		//
		// ->
		//
		// { tmp = value1; label[L]: var = tmp }
		// ...
		// { tmp = value2; goto[L] }

		protected override Expression VisitLabel(LabelExpression node)
		{
			if (node.Target.Type == typeof(void))
				return base.VisitLabel(node);
			var info = GetLabelInfo(node.Target);
			return Expression.Block(MakeAssign(info.Temp, Visit(node.DefaultValue)), Expression.Label(info.NewLabel), info.Temp);
		}

		protected override Expression VisitGoto(GotoExpression node)
		{
			if (node.Target.Type == typeof(void))
				return base.VisitGoto(node);
			var info = GetLabelInfo(node.Target);
			return Expression.Block(MakeAssign(info.Temp, Visit(node.Value)), Expression.MakeGoto(node.Kind, info.NewLabel, null, node.Type));
		}

		LabelInfo GetLabelInfo(LabelTarget label)
		{
			LabelInfo temp;
			if (!(_labelTemps ?? (_labelTemps = new Dictionary<LabelTarget, LabelInfo>())).TryGetValue(label, out temp))
				_labelTemps[label] = temp = new LabelInfo(label);
			return temp;
		}

		#endregion

		#region (式の中間にある yield を許可するための) スタックスピリング

		/// <summary>評価されているかどうかにかかわらず式に定数が残っていれば <c>true</c> を返します。</summary>
		static bool IsConstant(Expression e) { return e is ConstantExpression; }

		Expression ToTemp(ICollection<Expression> block, Expression e)
		{
			Debug.Assert(e != null);
			if (IsConstant(e))
				return e;
			var temp = Expression.Variable(e.Type, "generatorTemp" + _temps.Count);
			_temps.Add(temp);
			block.Add(MakeAssign(temp, e));
			return temp;
		}

		Expression[] ToTemp(ICollection<Expression> block, ICollection<Expression> args) { return args.Select(x => ToTemp(block, x)).ToArray(); }

		Expression Rewrite(Expression node, System.Collections.ObjectModel.ReadOnlyCollection<Expression> arguments, Func<IEnumerable<Expression>, Expression> factory) { return Rewrite(node, null, arguments, (e, args) => factory(args)); }

		Expression Rewrite(Expression node, Expression expr, System.Collections.ObjectModel.ReadOnlyCollection<Expression> arguments, Func<Expression, IEnumerable<Expression>, Expression> factory)
		{
			var yields = _yields.Count;
			var newExpr = expr != null ? Visit(expr) : null;
			// TODO(opt): yield を含んでいる最後の引数を追跡する場合でも、残りの引数をローカルに退避させる必要がないようにする
			var newArgs = Visit(arguments);
			if (newExpr == expr && newArgs == arguments)
				return node;
			if (yields == _yields.Count)
				return factory(newExpr, newArgs);
			var block = new List<Expression>(newArgs.Count + 1);
			if (newExpr != null)
				newExpr = ToTemp(block, newExpr);
			var spilledArgs = ToTemp(block, newArgs);
			block.Add(factory(newExpr, spilledArgs));
			return Expression.Block(block);
		}

		// 式ツリーは単項演算へのジャンプをサポートしないので、単項演算も同じようにリライトする必要があります。
		Expression Rewrite(Expression node, Expression expr, Func<Expression, Expression> factory)
		{
			var yields = _yields.Count;
			var newExpr = Visit(expr);
			if (newExpr == expr)
				return node;
			if (yields == _yields.Count || IsConstant(newExpr))
				return factory(newExpr);
			var block = new List<Expression>(2);
			newExpr = ToTemp(block, newExpr);
			block.Add(factory(newExpr));
			return Expression.Block(block);
		}

		Expression Rewrite(Expression node, Expression expr1, Expression expr2, Func<Expression, Expression, Expression> factory)
		{
			var yields = _yields.Count;
			var newExpr1 = Visit(expr1);
			var yields1 = _yields.Count;
			var newExpr2 = Visit(expr2);
			if (newExpr1 == expr1 && newExpr2 == expr2)
				return node;
			// f({expr}, {expr})
			if (yields == _yields.Count)
				return factory(newExpr1, newExpr2);
			var block = new List<Expression>(3);
			// f({yield}, {expr}) -> { t = {yield}; f(t, {expr}) }
			// f({const}, yield) -> { t = {yield}; f({const}, t) }
			// f({expr|yield}, {yield}) -> { t1 = {expr|yeild}, t2 = {yield}; f(t1, t2) }
			newExpr1 = ToTemp(block, newExpr1);
			if (yields1 != _yields.Count)
				newExpr2 = ToTemp(block, newExpr2);
			block.Add(factory(newExpr1, newExpr2));
			return Expression.Block(block);
		}

		Expression VisitAssign(BinaryExpression node)
		{
			var yields = _yields.Count;
			var left = Visit(node.Left);
			var right = Visit(node.Right);
			if (left == node.Left && right == node.Right)
				return node;
			if (yields == _yields.Count)
				return Expression.Assign(left, right);
			var block = new List<Expression>();
			// 左辺が右辺の前に評価されることを保証する必要があります。たとえば、
			// {expr0}[{expr1},..,{exprN}] = {rhs} 
			// ->
			// { l0 = {expr0}; l1 = {expr1}; ..; lN = {exprN}; r = {rhs}; l0[l1,..,lN] = r } 
			if (left == node.Left)
			{
				switch (left.NodeType)
				{
					case ExpressionType.MemberAccess:
						var member = (MemberExpression)node.Left;
						left = member.Update(ToTemp(block, member.Expression));
						break;
					case ExpressionType.Index:
						var index = (IndexExpression)node.Left;
						left = index.Update(ToTemp(block, index.Object), ToTemp(block, index.Arguments));
						break;
					case ExpressionType.Parameter:
						// 操作は必要ない
						break;
					default:
						// 拡張式は上記の Visit で縮退されるべきであったのに、異なる値が返された
						throw Assert.Unreachable;
				}
			}
			else
			{
				// リライトされた左辺の最後の式を取得
				var leftBlock = (BlockExpression)left;
				block.AddRange(leftBlock.Expressions);
				block.RemoveAt(block.Count - 1);
				left = leftBlock.Expressions[leftBlock.Expressions.Count - 1];
			}
			if (right != node.Right)
				right = ToTemp(block, right);
			block.Add(Expression.Assign(left, right));
			return Expression.Block(block);
		}

		protected override Expression VisitDynamic(DynamicExpression node) { return Rewrite(node, node.Arguments, node.Update); }

		protected override Expression VisitIndex(IndexExpression node) { return Rewrite(node, node.Object, node.Arguments, node.Update); }

		protected override Expression VisitInvocation(InvocationExpression node) { return Rewrite(node, node.Expression, node.Arguments, node.Update); }

		protected override Expression VisitMethodCall(MethodCallExpression node) { return Rewrite(node, node.Object, node.Arguments, node.Update); }

		protected override Expression VisitNew(NewExpression node) { return Rewrite(node, node.Arguments, node.Update); }

		protected override Expression VisitNewArray(NewArrayExpression node) { return Rewrite(node, node.Expressions, node.Update); }

		protected override Expression VisitMember(MemberExpression node) { return Rewrite(node, node.Expression, node.Update); }

		protected override Expression VisitBinary(BinaryExpression node)
		{
			if (node.NodeType == ExpressionType.Assign)
				return VisitAssign(node);
			// OpAssgin ノードに対して: yield があれば、縮退された値にジェネレータ変換を適用する必要がある。
			if (node.CanReduce)
				return Visit(node.Reduce());
			return Rewrite(node, node.Left, node.Right, node.Update);
		}

		protected override Expression VisitTypeBinary(TypeBinaryExpression node) { return Rewrite(node, node.Expression, node.Update); }

		protected override Expression VisitUnary(UnaryExpression node)
		{
			// OpAssgin ノードに対して: yield があれば、縮退された値にジェネレータ変換を適用する必要がある。
			if (node.CanReduce)
				return Visit(node.Reduce());
			return Rewrite(node, node.Operand, node.Update);
		}

		protected override Expression VisitMemberInit(MemberInitExpression node)
		{
			// 何か変更されたら見る
			int yields = _yields.Count;
			var e = base.VisitMemberInit(node);
			if (yields == _yields.Count)
				return e;
			// yield がある。基本ノードに縮退してジャンプできるようにする
			return e.Reduce();
		}

		protected override Expression VisitListInit(ListInitExpression node)
		{
			// 何か変更されたら見る
			int yields = _yields.Count;
			var e = base.VisitListInit(node);
			if (yields == _yields.Count)
				return e;
			// yield がある。基本ノードに縮退してジャンプできるようにする
			return e.Reduce();
		}

		#endregion
	}
}
