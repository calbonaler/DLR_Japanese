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
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.Scripting.Actions
{
	/// <summary>
	/// 動的サイトを消費する動的サイトを見つけ、単一のコンボ動的サイトに変形する式ツリーのリライターを表します。
	/// コンボ動的サイトは個別のメタバインダーを実行し、単一の動的サイトで結果のコードを生成します。
	/// </summary>
	public class ComboActionRewriter : ExpressionVisitor
	{
		/// <summary>
		/// コンボ動的サイトの生成に使用する縮退可能なノードです。
		/// 動的サイトを発見するたびに、それらを <see cref="ComboDynamicSiteExpression"/> に置換します。
		/// 動的サイトの子が <see cref="ComboDynamicSiteExpression"/> となる場合は、バインディングマッピング情報を更新して子を親とマージします。
		/// 入力のうち 1 つでも副作用を発生させる場合は、結合を停止します。
		/// </summary>
		class ComboDynamicSiteExpression : Expression
		{
			readonly Type _type;

			public ComboDynamicSiteExpression(Type type, List<BinderMappingInfo> binders, Expression[] inputs)
			{
				Binders = binders;
				Inputs = inputs;
				_type = type;
			}

			public override bool CanReduce { get { return true; } }

			public sealed override Type Type { get { return _type; } }

			public sealed override ExpressionType NodeType { get { return ExpressionType.Extension; } }

			public Expression[] Inputs { get; private set; }

			public List<BinderMappingInfo> Binders { get; private set; }

			public override Expression Reduce() { return Expression.Dynamic(new ComboBinder(Binders), Type, Inputs); } // we just reduce to a simple DynamicExpression
		}

		/// <summary><see cref="System.Linq.Expressions.DynamicExpression"/> の子を走査します。</summary>
		/// <param name="node">走査する式。</param>
		/// <returns>式またはいずれかの部分式が変更された場合は変更された式。それ以外の場合は元の式。</returns>
		protected override Expression VisitDynamic(DynamicExpression node)
		{
			var metaBinder = node.Binder as DynamicMetaObjectBinder;
			if (metaBinder == null)
				return node; // 組み合わせることができないので、DynamicMetaObjectBinder 以外のバインダーによるノードはリライトしません。
			// 新しい動的サイトノードのために実引数を収集します。
			bool foundSideEffectingArgs = false;
			List<Expression> inputs = new List<Expression>();
			// 引数マッピングはそれぞれのメタバインダーに対して 1 つの List<ComboParameterMappingInfo> で、内部リストはそれぞれの特定のバインダーに対するマッピングを含みます。
			List<BinderMappingInfo> binders = new List<BinderMappingInfo>();
			List<ParameterMappingInfo> myInfo = new List<ParameterMappingInfo>();
			int actionCount = 0;
			foreach (var e in node.Arguments)
			{
				if (!foundSideEffectingArgs)
				{
					// 引数の結合を試します...
					var rewritten = Visit(e);
					var combo = rewritten as ComboDynamicSiteExpression;
					ConstantExpression ce;
					if (combo != null)
					{
						// これら自身の式と組み合わせるアクション式はこれまでいくつのアクションがあったかを記憶します。
						// これらの子がアクションを消費する場合、それらのオフセットは押し上げられるため。
						int baseActionCount = actionCount;
						binders.AddRange(combo.Binders.Select(x => new BinderMappingInfo(x.Binder, x.MappingInfo.Select(y =>
						{
							if (y.IsParameter)
							{
								y = ParameterMappingInfo.Parameter(inputs.Count); // 子からのすべての入力はここで自分たちのものに
								inputs.Add(combo.Inputs[y.ParameterIndex]);
							}
							else if (y.IsAction)
							{
								y = ParameterMappingInfo.Action(y.ActionIndex + baseActionCount);
								actionCount++;
							}
							else
								Debug.Assert(y.Constant != null); // 定数はそのまま流す
							return y;
						}).ToArray())));
						myInfo.Add(ParameterMappingInfo.Action(actionCount++));
					}
					else if ((ce = rewritten as ConstantExpression) != null)
						myInfo.Add(ParameterMappingInfo.Fixed(ce)); // 定数はコンボに直せる
					else if (IsSideEffectFree(rewritten))
					{
						// これは入力引数として扱える
						myInfo.Add(ParameterMappingInfo.Parameter(inputs.Count));
						inputs.Add(rewritten);
					}
					else
					{
						// この引数は理解できないことをしているためそのままにしなければならず、
						// すべての残りの引数はこれが副作用を与えるかのように通常通り評価される必要がある。
						foundSideEffectingArgs = true;
						myInfo.Add(ParameterMappingInfo.Parameter(inputs.Count));
						inputs.Add(e);
					}
				}
				else
				{
					// 副作用があるかもしれない引数に出会ってしまったため、これ以上の結合はできない
					myInfo.Add(ParameterMappingInfo.Parameter(inputs.Count));
					inputs.Add(e);
				}
			}
			binders.Add(new BinderMappingInfo(metaBinder, myInfo));
			// TODO: あらゆる重複している入力を取り除く (例えば複数回与えられるローカルなど)
			return new ComboDynamicSiteExpression(node.Type, binders, inputs.ToArray());
		}

		bool IsSideEffectFree(Expression rewritten)
		{
			if (rewritten is ParameterExpression)
				return true;
			if (rewritten.NodeType == ExpressionType.TypeIs)
				return IsSideEffectFree(((UnaryExpression)rewritten).Operand);
			var be = rewritten as BinaryExpression;
			if (be != null && be.Method == null && IsSideEffectFree(be.Left) && IsSideEffectFree(be.Right))
				return true;
			var mc = rewritten as MethodCallExpression;
			if (mc != null && mc.Method != null)
				return mc.Method.IsDefined(typeof(NoSideEffectsAttribute), false);
			var ce = rewritten as ConditionalExpression;
			if (ce != null)
				return IsSideEffectFree(ce.Test) && IsSideEffectFree(ce.IfTrue) && IsSideEffectFree(ce.IfFalse);
			var me = rewritten as MemberExpression;
			if (me != null && me.Member is System.Reflection.FieldInfo)
				return false;
			return false;
		}
	}
}
