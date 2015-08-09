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
	/// yield return �܂��� yield break ���������ꍇ�ɁA���̃����C�^�[�͊܂�ł���u���b�N�A�X�R�[�v�A�����Ď����X�^�b�N�̏�Ԃɂ��������ĕ��R�����܂��B
	/// ���ׂĂ̑��������X�R�[�v�̓W�F�l���[�^�̃N���[�W���ɏ��i���ꂽ�ϐ��������߁Ayield ���z���Đ����c�点�邱�Ƃ��ł��܂��B
	/// </summary>
	sealed class GeneratorRewriter : ExpressionVisitor
	{
		// 2 �̒萔�͓����I�Ɏg�p����܂��B�����͗L���� yield ��Ԃ̏ꍇ�͖������܂���B
		const int GotoRouterYielding = 0;
		const int GotoRouterNone = -1;
		// �J�n�O����ъ������̃W�F�l���[�^�̏�Ԃł��B
		internal const int NotStarted = -1;
		internal const int Finished = 0;

		sealed class YieldMarker
		{
			// ����: ���x���� try �u���b�N�𐶐����邲�Ƃɕω����܂��B
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

		// finally �����̏ꍇ�� 1 �ȏ�̃��^�[�����x����\���܂�
		readonly Stack<LabelTarget> _returnLabels = new Stack<LabelTarget>();
		ParameterExpression _gotoRouter;
		bool _inTryWithFinally;
		readonly List<YieldMarker> _yields = new List<YieldMarker>();
		List<int> _debugCookies;
		readonly HashSet<ParameterExpression> _vars = new HashSet<ParameterExpression>();
		// �\�ȍœK��: �ꎞ�ϐ��̍Ďg�p�B�ϐ���K�؂ɃX�R�[�v���āA�t���[���X�g���̕ϐ��������߂��K�v������܂��B
		readonly List<ParameterExpression> _temps = new List<ParameterExpression>();
		// �l�̂��� goto ���T�|�[�g����ϐ��B
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
			// �{�̂�K��
			var body = Visit(_generator.Body);
			Debug.Assert(_returnLabels.Count == 1);
			// GeneratorNext<T> �ɑ΂��郉���_���쐬�B�����_�O���̃X�R�[�v�ɂ���ϐ��������グ�܂��B
			// �N���[�Y�I�[�o�[�����K�v���Ȃ��ꎞ�ϐ������W
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
			// �񋓃t�@�N�g���� GeneratorNext<T> �̑���� Func<GeneratorNext<T>> ���Ƃ�B
			if (_generator.IsEnumerable)
				body = Expression.Lambda(body);
			// �萔�����łɃ����C�g���ꂽ��Ńc���[��T������̂ŁA�����ł� _debugCookies �z��� ConstantExpression ���쐬�ł��܂���B
			// ����ɁA�z��� _debugCookies ����̓��e�Ŕz��ŏ��������� NewArrayExpression ���쐬���܂��B
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

		/// <summary>�ϐ��ւ̒l�̑�����쐬���܂��B�����ւ̃W�����v���\�ɂ��邽�߂ɁA�\�Ȍ���E�ӂ̑�����v�b�V�����܂��B</summary>
		Expression MakeAssign(ParameterExpression variable, Expression value)
		{
			// TODO: ����͕s���S�ł��B
			// �����̃m�[�h�� yield �܂��� return (Switch, Loop, Goto, Label) ���܂ޏꍇ�A����͐������Ȃ��c���[�𐶐����Ē�~����\��������܂��B
			// �����̓T�|�[�g����܂��񂪁A(����ȊO�̑��̎��� yield ���܂ޏꍇ) �K�؂Ȏg�p��F�߂��Ɋ�������\�������邽�߁A�����ł͗�O���X���[�ł��܂���B
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
			// finally �u���b�N�� yield �̏ꍇ�ɁA�V���� return ���x�����v�b�V��
			_returnLabels.Push(Expression.Label());
			// �����̂��������� 1 �� null �ɂȂ�Ȃ�
			var @finally = Visit(node.Finally);
			var fault = Visit(node.Fault);
			var finallyReturn = _returnLabels.Pop();
			var finallyYields = _yields.Count;
			_inTryWithFinally = savedInTryWithFinally;
			if (@try == node.Body && handlers == node.Handlers && @finally == node.Finally && fault == node.Fault)
				return node;
			// yield �ł͂Ȃ������� return
			if (startYields == _yields.Count)
				return Expression.MakeTry(null, @try, @finally, fault, handlers);
			if (fault != null && finallyYields != catchYields)
				throw new NotSupportedException("fault �u���b�N�ɂ����� yield �̓T�|�[�g����Ă��܂���B"); // �N�������K�v�Ƃ����Afault �ɖ߂���@�����m�łȂ�
			// try �� yield ������΁Ayield ���x���𔭍s����V���� try �{�̂��\�z����K�v������
			var tryStart = Expression.Label();
			if (tryYields != startYields)
				@try = Expression.Block(MakeYieldRouter(startYields, tryYields, tryStart), @try);
			// yield �̂��� catch �����������n���h���ɕϊ�
			if (catchYields != tryYields)
			{
				var block = new List<Expression>();
				block.Add(MakeYieldRouter(tryYields, catchYields, tryStart));
				block.Add(null); // ���ƂŖ��߂��̃X���b�g
				for (int i = 0, n = handlers.Count; i < n; i++)
				{
					var c = handlers[i];
					if (c == node.Handlers[i])
						continue;
					if (handlers.IsReadOnly)
						handlers = handlers.ToArray();
					// catch �u���b�N�ɃX�R�[�v���ꂽ�ϐ�
					var exceptionVar = Expression.Variable(c.Test, null);
					// catch �u���b�N�{�̂���O�ւ̃A�N�Z�X�Ɏg�p����ϐ�
					// catch �u���b�N�Ɍ��̕ϐ����������ꍇ�Ďg�p���܂��B
					// catch �� yield ���܂�ł���\�������邽�߁A����͊����グ���܂��B
					var deferredVar = c.Variable ?? Expression.Variable(c.Test, null);
					_vars.Add(deferredVar);
					// �t�B���^�[����O�ϐ��Ɋm���ɃA�N�Z�X�ł���悤�ɂ���K�v������܂��B
					// catch (ExceptionType exceptionVar) {
					//     deferredVar = exceptionVar;
					// }
					handlers[i] = Expression.Catch(exceptionVar,
						Utils.Void(Expression.Assign(deferredVar, exceptionVar)),
						c.Filter != null && c.Variable != null ? Expression.Block(new[] { c.Variable }, Expression.Assign(c.Variable, exceptionVar), c.Filter) : c.Filter
					);
					// �ăX���[�� "throw defferedVar" �ɏ���������K�v������܂��B
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
				// ��O��ۑ����� catch �u���b�N��������K�v������̂ŁAfinally �� yield ������ꍇ�͍ăX���[�ł��܂��B
				// ����ɁA�ԋp�̃��W�b�N�������܂��B���̂悤�ɂȂ�܂�:
				//
				// try { ... } catch (Exception all) { saved = all; }
				// finally {
				//   if (_finallyReturnVar) goto finallyReturn;
				//   ...
				//   if (saved != null) throw saved;
				//   finallyReturn:
				// }
				// if (_finallyReturnVar) goto _return;

				// catch(Exception) ��������K�v�����邽�߁Acatch ������ꍇ�́Atry �Ń��b�v���܂��B
				if (handlers.Count > 0)
				{
					@try = Expression.MakeTry(null, @try, null, null, handlers);
					handlers = new CatchBlock[0];
				}
				// ����: �����̃��[�^�[�̏����͏d�v�ł��B
				// �ŏ��̌Ăяo���� "tryEnd" �Ɉʒu���邷�ׂẴ��x����ύX���A���̃��[�^�[�� "tryEnd" �փW�����v���܂��B
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
				// try �܂��� catch �� yield ���܂�ł���΁Afinally ���C�����A�X�L�b�v�ł���悤�ɂ��܂��B
				@finally = Expression.Block(MakeSkipFinallyBlock(finallyReturn), @finally, Expression.Label(finallyReturn));
			// �K�v�ł���ΊO���� try ���쐬
			if (handlers.Count > 0 || @finally != null || fault != null)
				@try = Expression.MakeTry(null, @try, @finally, fault, handlers);
			return Expression.Block(Expression.Label(tryStart), @try);
		}

		class RethrowRewriter : ExpressionVisitor
		{
			public RethrowRewriter(ParameterExpression exception) { _exception = exception; }

			readonly ParameterExpression _exception;

			protected override Expression VisitUnary(UnaryExpression node) { return node.NodeType == ExpressionType.Throw && node.Operand == null ? Expression.Throw(_exception, node.Type) : base.VisitUnary(node); }

			protected override Expression VisitLambda<T>(Expression<T> node) { return node; } // �����_�ɂ͍ċA���Ȃ�

			protected override Expression VisitTry(TryExpression node) { return node; } // ���� try �ɂ͍ċA���Ȃ�
		}

		// yield ���ł���� finally �u���b�N���X�L�b�v���܂����Ayield break �����s���̍ۂ͍s���܂���B
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

		// ��{�̎�������R�s�[����܂����B
		// �t�B���^�[���� yield �����O���邽�߂ɕK�v�ł��B
		protected override CatchBlock VisitCatchBlock(CatchBlock node)
		{
			var v = VisitAndConvert(node.Variable, "VisitCatchBlock");
			int yields = _yields.Count;
			var f = Visit(node.Filter);
			if (yields != _yields.Count)
				throw new NotSupportedException("filter �ɂ����� yield �͋�����Ă��܂���B"); // No one needs this yet, and it's not clear what it should even do
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
				// �O���� switch �X�e�[�g�����g����̂�����W�����v�� (�K�؂ɃW�����v�ł��Ȃ�) ���̃��x���ł͂Ȃ��A���̃��[�^�[�ɓ���ׂ��ł��B
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
			// �K�؂ȃX�^�b�N�X�s�����O��ۏ؂��邽�߂ɏk�ނ��Ȃ���΂Ȃ�܂���B
			return Visit(node.ReduceExtensions());
		}

		Expression VisitYield(YieldExpression node)
		{
			if (node.Target != _generator.Target)
				throw new InvalidOperationException("yield �ƃW�F�l���[�^�͓��� LabelTarget �I�u�W�F�N�g�����L���Ă���K�v������܂��B");
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
			// ��̂��߂ɕϐ���ۑ� (�����_�̊O���Ŋ����グ����)
			_vars.UnionWith(node.Variables);
			// ���ׂĂ̕ϐ�����菜���ꂽ�ȊO�͏���������ꂽ�{�̂ŐV�����u���b�N��Ԃ��B
			return Expression.Block(node.Type, b);
		}

		protected override Expression VisitLambda<T>(Expression<T> node) { return node; } // �l�X�g���ꂽ�����_�ɂ͍ċA���Ȃ�

		#region �l�̂��� goto �T�|�[�g

		// �����C�^�[�͎����ꎞ�ϐ��ɑ�����܂��B
		// �����l�̂��郉�x���ł���΁A�Ԃ���鎮�͑���̉E�ӂɃW�����v�ł��Ȃ����߁A�s���Ȏ��c���[�ƂȂ�܂��B
		// ���������āA�l�̂��郉�x������� goto ����菜���K�v������܂��B
		// MakeAssign �Ŏg�p�������̂������C�g����K�v�͂���܂����A���ׂă����C�g������͊ȒP�ł��B
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

		#region (���̒��Ԃɂ��� yield �������邽�߂�) �X�^�b�N�X�s�����O

		/// <summary>�]������Ă��邩�ǂ����ɂ�����炸���ɒ萔���c���Ă���� <c>true</c> ��Ԃ��܂��B</summary>
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
			// TODO(opt): yield ���܂�ł���Ō�̈�����ǐՂ���ꍇ�ł��A�c��̈��������[�J���ɑޔ�������K�v���Ȃ��悤�ɂ���
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

		// ���c���[�͒P�����Z�ւ̃W�����v���T�|�[�g���Ȃ��̂ŁA�P�����Z�������悤�Ƀ����C�g����K�v������܂��B
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
			// ���ӂ��E�ӂ̑O�ɕ]������邱�Ƃ�ۏ؂���K�v������܂��B���Ƃ��΁A
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
						// ����͕K�v�Ȃ�
						break;
					default:
						// �g�����͏�L�� Visit �ŏk�ނ����ׂ��ł������̂ɁA�قȂ�l���Ԃ��ꂽ
						throw Assert.Unreachable;
				}
			}
			else
			{
				// �����C�g���ꂽ���ӂ̍Ō�̎����擾
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
			// OpAssgin �m�[�h�ɑ΂���: yield ������΁A�k�ނ��ꂽ�l�ɃW�F�l���[�^�ϊ���K�p����K�v������B
			if (node.CanReduce)
				return Visit(node.Reduce());
			return Rewrite(node, node.Left, node.Right, node.Update);
		}

		protected override Expression VisitTypeBinary(TypeBinaryExpression node) { return Rewrite(node, node.Expression, node.Update); }

		protected override Expression VisitUnary(UnaryExpression node)
		{
			// OpAssgin �m�[�h�ɑ΂���: yield ������΁A�k�ނ��ꂽ�l�ɃW�F�l���[�^�ϊ���K�p����K�v������B
			if (node.CanReduce)
				return Visit(node.Reduce());
			return Rewrite(node, node.Operand, node.Update);
		}

		protected override Expression VisitMemberInit(MemberInitExpression node)
		{
			// �����ύX���ꂽ�猩��
			int yields = _yields.Count;
			var e = base.VisitMemberInit(node);
			if (yields == _yields.Count)
				return e;
			// yield ������B��{�m�[�h�ɏk�ނ��ăW�����v�ł���悤�ɂ���
			return e.Reduce();
		}

		protected override Expression VisitListInit(ListInitExpression node)
		{
			// �����ύX���ꂽ�猩��
			int yields = _yields.Count;
			var e = base.VisitListInit(node);
			if (yields == _yields.Count)
				return e;
			// yield ������B��{�m�[�h�ɏk�ނ��ăW�����v�ł���悤�ɂ���
			return e.Reduce();
		}

		#endregion
	}
}
