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
	/// <summary>Try ステートメントを自然な構文で構築できるビルダーを提供します。</summary>
	public sealed class TryStatementBuilder
	{
		readonly List<CatchBlock> _catchBlocks = new List<CatchBlock>();
		readonly Expression _body;
		readonly List<Expression> _faultBodies = new List<Expression>();
		Expression _finally;
		bool _enableJumpsFromFinally;

		/// <summary>指定された本体を使用して、<see cref="Microsoft.Scripting.Ast.TryStatementBuilder"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="body">Try ステートメントの本体を指定します。</param>
		internal TryStatementBuilder(Expression body) { _body = body; }

		/// <summary>補足する例外の型と本体を使用して、Try ステートメントに Catch 句を追加します。</summary>
		/// <param name="type">補足する例外の型を指定します。</param>
		/// <param name="body">Catch 句の本体を指定します。</param>
		/// <returns>Catch 句が追加された <see cref="TryStatementBuilder"/>。</returns>
		public TryStatementBuilder Catch(Type type, params Expression[] body)
		{
			ContractUtils.RequiresNotNull(type, "type");
			ContractUtils.RequiresNotNullItems(body, "body");
			if (_finally != null)
				throw Error.FinallyAlreadyDefined();
			_catchBlocks.Add(Expression.Catch(type, body.Length == 1 ? body[0] : Utils.Block(body)));
			return this;
		}

		/// <summary>補足した例外を保存する変数と本体を使用して、Try ステートメントに Catch 句を追加します。</summary>
		/// <param name="holder">補足した例外を保存する変数を指定します。</param>
		/// <param name="body">Catch 句の本体を指定します。</param>
		/// <returns>Catch 句が追加された <see cref="TryStatementBuilder"/>。</returns>
		public TryStatementBuilder Catch(ParameterExpression holder, params Expression[] body)
		{
			ContractUtils.RequiresNotNull(holder, "holder");
			ContractUtils.RequiresNotNullItems(body, "body");
			if (_finally != null)
				throw Error.FinallyAlreadyDefined();
			_catchBlocks.Add(Expression.Catch(holder, body.Length == 1 ? body[0] : Utils.Block(body)));
			return this;
		}

		/// <summary>補足する例外の型、補足するかどうかを判断する条件および本体を使用して、Try ステートメントに Filter 句を追加します。</summary>
		/// <param name="type">補足する例外の型を指定します。</param>
		/// <param name="condition">例外を補足するかどうかを判断する条件を指定します。</param>
		/// <param name="body">Filter 句の本体を指定します。</param>
		/// <returns>Filter 句が追加された <see cref="TryStatementBuilder"/>。</returns>
		public TryStatementBuilder Filter(Type type, Expression condition, params Expression[] body)
		{
			ContractUtils.RequiresNotNull(type, "type");
			ContractUtils.RequiresNotNull(condition, "condition");
			ContractUtils.RequiresNotNullItems(body, "body");
			_catchBlocks.Add(Expression.Catch(type, body.Length == 1 ? body[0] : Utils.Block(body), condition));
			return this;
		}

		/// <summary>補足した例外を保存する変数、補足するかどうかを判断する条件および本体を使用して、Try ステートメントに Filter 句を追加します。</summary>
		/// <param name="holder">補足した例外を保存する変数を指定します。</param>
		/// <param name="condition">例外を補足するかどうかを判断する条件を指定します。</param>
		/// <param name="body">Filter 句の本体を指定します。</param>
		/// <returns>Filter 句が追加された <see cref="TryStatementBuilder"/>。</returns>
		public TryStatementBuilder Filter(ParameterExpression holder, Expression condition, params Expression[] body)
		{
			ContractUtils.RequiresNotNull(holder, "holder");
			ContractUtils.RequiresNotNull(condition, "condition");
			ContractUtils.RequiresNotNullItems(body, "body");
			_catchBlocks.Add(Expression.Catch(holder, body.Length == 1 ? body[0] : Utils.Block(body), condition));
			return this;
		}

		/// <summary>指定された本体を使用して、Try ステートメントに Finally 句を追加します。</summary>
		/// <param name="body">Finally 句の本体を指定します。</param>
		/// <returns>Finally 句が追加された <see cref="TryStatementBuilder"/>。</returns>
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

		/// <summary>指定された本体を使用して、Try ステートメントに外側へのジャンプが可能な Finally 句を追加します。</summary>
		/// <param name="body">外側へのジャンプが可能な Finally 句の本体を指定します。</param>
		/// <returns>外側へのジャンプが可能な Finally 句が追加された <see cref="TryStatementBuilder"/>。</returns>
		public TryStatementBuilder FinallyWithJumps(params Expression[] body)
		{
			_enableJumpsFromFinally = true;
			return Finally(body);
		}

		/// <summary>指定された本体を使用して、Try ステートメントに Fault 句を追加します。</summary>
		/// <param name="body">Fault 句の本体を指定します。</param>
		/// <returns>Fault 句が追加された <see cref="TryStatementBuilder"/>。</returns>
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

		/// <summary>提供された <see cref="TryStatementBuilder"/> を等価な <see cref="Expression"/> に変換します。</summary>
		/// <param name="builder">変換する <see cref="TryStatementBuilder"/> です。</param>
		/// <returns>指定された <see cref="TryStatementBuilder"/> と等価な <see cref="Expression"/>。</returns>
		public static implicit operator Expression(TryStatementBuilder builder)
		{
			ContractUtils.RequiresNotNull(builder, "builder");
			return builder.ToExpression();
		}

		/// <summary>この <see cref="TryStatementBuilder"/> を等価な <see cref="Expression"/> に変換します。</summary>
		/// <returns>この <see cref="TryStatementBuilder"/> と等価な <see cref="Expression"/>。</returns>
		public Expression ToExpression() {
			// 本当の filter や fault は DynamicMethod で動作しないので発行できません。代わりに単純な変形を行います:
            //   fault -> catch (Exception) { ...; rethrow }
            //   filter -> catch (ExceptionType) { if (!filter) rethrow; ... }
			// filter の変形は本当の CLR セマンティクスと完全に等価ではありませんが、IronPython または IronRuby が期待するものです。
			// CLR サポートを得る場合、本当の filter と fault ブロックに切り替えます。
			var catches = _catchBlocks.Select(x => x.Filter != null ? Expression.MakeCatchBlock(
				x.Test, x.Variable, Expression.Condition(x.Filter, x.Body, Expression.Rethrow(x.Body.Type)), null
			) : x);
            if (_faultBodies.Count > 0) {
				if (!catches.Any())
					throw new InvalidOperationException("fault は catch または finally 句と合わせて使用することはできません。");
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
		/// <summary>指定された本体を使用して、新しい <see cref="TryStatementBuilder"/> を作成します。</summary>
		/// <param name="body">Try ステートメントの本体を指定します。</param>
		/// <returns>新しく作成された <see cref="TryStatementBuilder"/>。</returns>
		public static TryStatementBuilder Try(params Expression[] body)
		{
			ContractUtils.RequiresNotNullItems(body, "body");
			return new TryStatementBuilder(body.Length == 1 ? body[0] : Utils.Block(body));
		}
	}
}