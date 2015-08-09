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
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Ast
{
	/// <summary>Try �X�e�[�g�����g�����R�ȍ\���ō\�z�ł���r���_�[��񋟂��܂��B</summary>
	public sealed class TryStatementBuilder
	{
		readonly List<CatchBlock> _catchBlocks = new List<CatchBlock>();
		readonly Expression _body;
		readonly List<Expression> _faultBodies = new List<Expression>();
		Expression _finally;
		bool _enableJumpsFromFinally;

		/// <summary>�w�肳�ꂽ�{�̂��g�p���āA<see cref="Microsoft.Scripting.Ast.TryStatementBuilder"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="body">Try �X�e�[�g�����g�̖{�̂��w�肵�܂��B</param>
		internal TryStatementBuilder(Expression body) { _body = body; }

		/// <summary>�⑫�����O�̌^�Ɩ{�̂��g�p���āATry �X�e�[�g�����g�� Catch ���ǉ����܂��B</summary>
		/// <param name="type">�⑫�����O�̌^���w�肵�܂��B</param>
		/// <param name="body">Catch ��̖{�̂��w�肵�܂��B</param>
		/// <returns>Catch �傪�ǉ����ꂽ <see cref="TryStatementBuilder"/>�B</returns>
		public TryStatementBuilder Catch(Type type, params Expression[] body)
		{
			ContractUtils.RequiresNotNull(type, "type");
			ContractUtils.RequiresNotNullItems(body, "body");
			if (_finally != null)
				throw Error.FinallyAlreadyDefined();
			_catchBlocks.Add(Expression.Catch(type, body.Length == 1 ? body[0] : Utils.Block(body)));
			return this;
		}

		/// <summary>�⑫������O��ۑ�����ϐ��Ɩ{�̂��g�p���āATry �X�e�[�g�����g�� Catch ���ǉ����܂��B</summary>
		/// <param name="holder">�⑫������O��ۑ�����ϐ����w�肵�܂��B</param>
		/// <param name="body">Catch ��̖{�̂��w�肵�܂��B</param>
		/// <returns>Catch �傪�ǉ����ꂽ <see cref="TryStatementBuilder"/>�B</returns>
		public TryStatementBuilder Catch(ParameterExpression holder, params Expression[] body)
		{
			ContractUtils.RequiresNotNull(holder, "holder");
			ContractUtils.RequiresNotNullItems(body, "body");
			if (_finally != null)
				throw Error.FinallyAlreadyDefined();
			_catchBlocks.Add(Expression.Catch(holder, body.Length == 1 ? body[0] : Utils.Block(body)));
			return this;
		}

		/// <summary>�⑫�����O�̌^�A�⑫���邩�ǂ����𔻒f�����������і{�̂��g�p���āATry �X�e�[�g�����g�� Filter ���ǉ����܂��B</summary>
		/// <param name="type">�⑫�����O�̌^���w�肵�܂��B</param>
		/// <param name="condition">��O��⑫���邩�ǂ����𔻒f����������w�肵�܂��B</param>
		/// <param name="body">Filter ��̖{�̂��w�肵�܂��B</param>
		/// <returns>Filter �傪�ǉ����ꂽ <see cref="TryStatementBuilder"/>�B</returns>
		public TryStatementBuilder Filter(Type type, Expression condition, params Expression[] body)
		{
			ContractUtils.RequiresNotNull(type, "type");
			ContractUtils.RequiresNotNull(condition, "condition");
			ContractUtils.RequiresNotNullItems(body, "body");
			_catchBlocks.Add(Expression.Catch(type, body.Length == 1 ? body[0] : Utils.Block(body), condition));
			return this;
		}

		/// <summary>�⑫������O��ۑ�����ϐ��A�⑫���邩�ǂ����𔻒f�����������і{�̂��g�p���āATry �X�e�[�g�����g�� Filter ���ǉ����܂��B</summary>
		/// <param name="holder">�⑫������O��ۑ�����ϐ����w�肵�܂��B</param>
		/// <param name="condition">��O��⑫���邩�ǂ����𔻒f����������w�肵�܂��B</param>
		/// <param name="body">Filter ��̖{�̂��w�肵�܂��B</param>
		/// <returns>Filter �傪�ǉ����ꂽ <see cref="TryStatementBuilder"/>�B</returns>
		public TryStatementBuilder Filter(ParameterExpression holder, Expression condition, params Expression[] body)
		{
			ContractUtils.RequiresNotNull(holder, "holder");
			ContractUtils.RequiresNotNull(condition, "condition");
			ContractUtils.RequiresNotNullItems(body, "body");
			_catchBlocks.Add(Expression.Catch(holder, body.Length == 1 ? body[0] : Utils.Block(body), condition));
			return this;
		}

		/// <summary>�w�肳�ꂽ�{�̂��g�p���āATry �X�e�[�g�����g�� Finally ���ǉ����܂��B</summary>
		/// <param name="body">Finally ��̖{�̂��w�肵�܂��B</param>
		/// <returns>Finally �傪�ǉ����ꂽ <see cref="TryStatementBuilder"/>�B</returns>
		public TryStatementBuilder Finally(params Expression[] body)
		{
			ContractUtils.RequiresNotNullItems(body, "body");
			if (_finally != null)
				throw Error.FinallyAlreadyDefined();
			if (_faultBodies.Count > 0)
				throw Error.CannotHaveFaultAndFinally();
			_finally = body.Length == 1 ? body[0] : Utils.Block(body);
			return this;
		}

		/// <summary>�w�肳�ꂽ�{�̂��g�p���āATry �X�e�[�g�����g�ɊO���ւ̃W�����v���\�� Finally ���ǉ����܂��B</summary>
		/// <param name="body">�O���ւ̃W�����v���\�� Finally ��̖{�̂��w�肵�܂��B</param>
		/// <returns>�O���ւ̃W�����v���\�� Finally �傪�ǉ����ꂽ <see cref="TryStatementBuilder"/>�B</returns>
		public TryStatementBuilder FinallyWithJumps(params Expression[] body)
		{
			_enableJumpsFromFinally = true;
			return Finally(body);
		}

		/// <summary>�w�肳�ꂽ�{�̂��g�p���āATry �X�e�[�g�����g�� Fault ���ǉ����܂��B</summary>
		/// <param name="body">Fault ��̖{�̂��w�肵�܂��B</param>
		/// <returns>Fault �傪�ǉ����ꂽ <see cref="TryStatementBuilder"/>�B</returns>
		public TryStatementBuilder Fault(params Expression[] body)
		{
			ContractUtils.RequiresNotNullItems(body, "body");
			if (_finally != null)
				throw Error.CannotHaveFaultAndFinally();
			if (_faultBodies.Count > 0)
				throw Error.FaultAlreadyDefined();
			_faultBodies.AddRange(body);
			return this;
		}

		/// <summary>�񋟂��ꂽ <see cref="TryStatementBuilder"/> �𓙉��� <see cref="Expression"/> �ɕϊ����܂��B</summary>
		/// <param name="builder">�ϊ����� <see cref="TryStatementBuilder"/> �ł��B</param>
		/// <returns>�w�肳�ꂽ <see cref="TryStatementBuilder"/> �Ɠ����� <see cref="Expression"/>�B</returns>
		public static implicit operator Expression(TryStatementBuilder builder)
		{
			ContractUtils.RequiresNotNull(builder, "builder");
			return builder.ToExpression();
		}

		/// <summary>���� <see cref="TryStatementBuilder"/> �𓙉��� <see cref="Expression"/> �ɕϊ����܂��B</summary>
		/// <returns>���� <see cref="TryStatementBuilder"/> �Ɠ����� <see cref="Expression"/>�B</returns>
		public Expression ToExpression() {
			// �{���� filter �� fault �� DynamicMethod �œ��삵�Ȃ��̂Ŕ��s�ł��܂���B����ɒP���ȕό`���s���܂�:
            //   fault -> catch (Exception) { ...; rethrow }
            //   filter -> catch (ExceptionType) { if (!filter) rethrow; ... }
			// filter �̕ό`�͖{���� CLR �Z�}���e�B�N�X�Ɗ��S�ɓ����ł͂���܂��񂪁AIronPython �܂��� IronRuby �����҂�����̂ł��B
			// CLR �T�|�[�g�𓾂�ꍇ�A�{���� filter �� fault �u���b�N�ɐ؂�ւ��܂��B
			var catches = _catchBlocks.Select(x => x.Filter != null ? Expression.MakeCatchBlock(
				x.Test, x.Variable, Expression.Condition(x.Filter, x.Body, Expression.Rethrow(x.Body.Type)), null
			) : x);
            if (_faultBodies.Count > 0) {
				if (!catches.Any())
					throw new InvalidOperationException("fault �� catch �܂��� finally ��ƍ��킹�Ďg�p���邱�Ƃ͂ł��܂���B");
				catches = Enumerable.Repeat(
					Expression.Catch(typeof(Exception),
						Expression.Block(_faultBodies.Concat(Enumerable.Repeat(Expression.Rethrow(_body.Type), 1)))
					), 1
				);
            }
            var result = Expression.MakeTry(null, _body, _finally, null, catches);
            return _enableJumpsFromFinally ? Utils.FinallyFlowControl(result) : result;
        }
	}

#if TODO // better support for fault in interpreter
    public class TryFaultExpression : Expression, IInstructionProvider {
        private readonly Expression _body;
        private readonly Expression _fault;

        internal TryFaultExpression(Expression body, Expression fault) {
            _body = body;
            _fault = fault;
        }

        protected override ExpressionType NodeTypeImpl() {
            return ExpressionType.Extension;
        }

        protected override Type/*!*/ TypeImpl() {
            return _body.Type;
        }

        public override bool CanReduce {
            get {
                return true;
            }
        }

        public override Expression/*!*/ Reduce() {
            return Expression.TryCatch(
                _body,
                Expression.Catch(typeof(Exception), Expression.Block(_fault, Expression.Rethrow(_body.Type)))
            );
        }

        protected override Expression VisitChildren(ExpressionVisitor visitor) {
            Expression body = visitor(_body);
            Expression fault = visitor(_fault);
            if (body != _body || fault != _fault) {
                return new TryFaultExpression(body, fault);
            }

            return this;
        }

        public void AddInstructions(LightCompiler compiler) {
            compiler.Compile(Expression.TryFault(_body, _fault));
        }
    }
#endif

	public partial class Utils
	{
		/// <summary>�w�肳�ꂽ�{�̂��g�p���āA�V���� <see cref="TryStatementBuilder"/> ���쐬���܂��B</summary>
		/// <param name="body">Try �X�e�[�g�����g�̖{�̂��w�肵�܂��B</param>
		/// <returns>�V�����쐬���ꂽ <see cref="TryStatementBuilder"/>�B</returns>
		public static TryStatementBuilder Try(params Expression[] body)
		{
			ContractUtils.RequiresNotNullItems(body, "body");
			return new TryStatementBuilder(body.Length == 1 ? body[0] : Utils.Block(body));
		}
	}
}