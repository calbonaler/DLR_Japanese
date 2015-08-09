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
	/// ���̃����C�^�[�̖ړI�͒P���ł�: ���c���[�� finally/fault �𔲂���W�����v (break, continue, return, goto) �������܂���B
	/// ���̂��ߌ��݂̃R�[�h�̑���Ƀt���O���i�[���āAfinally/fault�̍Ō�ɃW�����v����R�[�h�ɒu�������܂��B
	/// try-finally �̍Ō�ł́A���̌㐳�������x���ɃW�����v���镪��𔭍s���܂��B
	/// 
	/// �������蕡�G�ɂ��邢�����̎���������܂�:
	/// 
	///   1. ���� finally ���O���ւ̃W�����v���܂�ł���΁Atry/catch ���̃W�����v���u��������K�v������܂��B
	///      ����͎��̂悤�ȏꍇ���T�|�[�g���܂�:
	///          # returns 234
	///          def foo():
	///              try: return 123
	///              finally: return 234 
	///      
	///      �W�����v������ finally �ɐi�݂܂����Afinally �͂�����x�W�����v����Ƃ��Ă��܂��B
	///      �������A�������� IL finally �����݂���΁Afinally �̃W�����v�𖳎����Č��̃W�����v�ɏ]�����Ƃ��ێ����邽�߁A"return 123" ��u��������K�v������܂��B
	///      ���̕���̋��P: finally ���̂�����W�����v������������Ȃ�΁Atry/catch ���̃W�����v�����l�ɂ���K�v������B
	///      
	///  2. �����R�[�h�𐶐����邽�߂ɂ́A������ 1 �̏�ԕϐ������K�v������A���̂��߁A������ finally �̊O�ɃW�����v���Ȃ���΂Ȃ�Ȃ��ꍇ�́A�W�����v��ێ����܂��B
	///     ����͂��̂悤�ȏꍇ�ł�:
	///       foo:
	///       try { ... } finally {
	///           try { ... } finally {
	///             ...
	///             if (...) {
	///                 // �ȑO�� goto foo;
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
			// ���̃u���b�N�� finally ��?
			internal bool InFinally;
			// ���̃u���b�N�̓t���[�����K�v�Ƃ��Ă��邩?
			internal bool HasFlow { get { return FlowLabel != null; } }
			// ���̃u���b�N���Œ�`���ꂽ���x��
			// ����ɂ�蒼�ڃW�����v����΂悢�̂��A�T�|�[�g��K�v�Ƃ��Ă���̂��𗝉��ł��܂��B
			internal readonly HashSet<LabelTarget> LabelDefs = new HashSet<LabelTarget>();
			// 2 �̃v���p�e�B�̓t���[����ŉ����o�͂��ׂ����������Ă���܂��B(���݂����)
			internal HashSet<LabelTarget> NeedFlowLabels;
			// IL �łł��Ȃ��W�����v�𔭍s����ɂ́A��ԕϐ���ݒ肵�� FlowLabel �ɃW�����v���܂��B�W�����v�̑���� FlowLabel �̃R�[�h����ł��B
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
				return Visit(ffc.Body); // �l�X�g���ꂽ finally �t���[�������b�v�������A������T�����邱�Ƃł����R�[�h�𐶐�
			// (���ׂĂ� goto �� try-finally �u���b�N�̒ǐՂ�ێ��ł���) ���ʂ� DLR �c���[��ő��삵�����̂ŁA�K��O�Ɋg�������k��
			if (node.CanReduce)
				return Visit(node.Reduce());
			return base.VisitExtension(node);
		}

		protected override Expression VisitLambda<T>(Expression<T> node) { return node; } // �l�X�g���ꂽ�����_�ɂ͍ċA���Ȃ�

		protected override Expression VisitTry(TryExpression node)
		{
			// finally/fault �u���b�N���ŏ��ɖK��
			var block = new BlockInfo { InFinally = true };
			_blocks.Push(block);
			var @finally = Visit(node.Finally);
			var fault = Visit(node.Fault);
			block.InFinally = false;
			var finallyEnd = block.FlowLabel;
			if (finallyEnd != null)
				block.FlowLabel = Expression.Label(); // �V�����^�[�Q�b�g���쐬�Btry ��ɔ��s�����
			var @try = Visit(node.Body);
			IList<CatchBlock> handlers = Visit(node.Handlers, VisitCatchBlock);
			_blocks.Pop();
			if (@try == node.Body && handlers == node.Handlers && @finally == node.Finally && fault == node.Fault)
				return node;
			if (!block.HasFlow)
				return Expression.MakeTry(null, @try, @finally, fault, handlers);
			if (node.Type != typeof(void)) // �����I�ɃT�|�[�g�͂���قǓ���Ȃ����A�܂��N�ɂ��K�v�Ƃ���Ă��Ȃ��̂�
				throw new NotSupportedException("FinallyFlowControlExpression �͔� void �^�� TryExpressions ���T�|�[�g���Ă��܂���B");
			//  ���� finally ���ɐ���t���[������΁A�O���𔭍s:
			//  try {
			//      // try �u���b�N�{�̂Ƃ��ׂĂ� catch �n���h��
			//  } catch (Exception all) {
			//      saved = all;
			//  } finally {
			//      finally_body
			//      if (saved != null)
			//          throw saved;
			//  }
			//  fault �n���h���������Ă���ꍇ�́A��������ǂ�����B
			//  try {
			//      // try �u���b�N�{�̂̂��ׂĂ� catch �n���h��
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
			// �t���[����𔭍s
			return Expression.Block(new[] { saved }, Expression.MakeTry(null, @try, @finally, fault, handlers), Expression.Label(block.FlowLabel), MakeFlowControlSwitch(block));
		}

		Expression MakeFlowControlSwitch(BlockInfo block) { return Expression.Switch(_flowVariable, null, null, block.NeedFlowLabels.Select(target => Expression.SwitchCase(MakeFlowJump(target), AstUtils.Constant(_labels[target].FlowState)))); }

		// ���ڒ��f�ł��邩�A�ăf�B�X�p�b�`��v�����邩�𔻒f���܂��B
		// ���ڒ��f�ł���ꍇ�́A_flowVariable �����Z�b�g���A�����łȂ��ꍇ�͎��� FlowLabel �փW�����v����
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
					// ���� finally �𔲂���K�v������̂ŁA���̃t���[���x���փW�����v����
					return Expression.Goto(block.FlowLabel);
				}
			}
			// �t���[���K�v�Ȃ���΂����ɁB�t���O�����Z�b�g���{���� GoTo �𔭍s
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
			// �u���b�N���̂��ׂẴ��x���𑨂��āA�u���b�N�̃X�R�[�v���ɒ�`���܂��B
			// �u���b�N���ő����ɒ�`���ꂽ���x���̓u���b�N�S�̂ŗL���ɂȂ�܂��B
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
