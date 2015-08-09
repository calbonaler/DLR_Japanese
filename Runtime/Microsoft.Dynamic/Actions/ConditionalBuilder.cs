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
using System.Dynamic;
using System.Linq.Expressions;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions
{
	using Ast = Expression;

	/// <summary>
	/// false 文がいまだ不明な場合に一連の条件式を構築します。
	/// 条件およびその条件に対する true 文は追加し続けることができます。
	/// それぞれの後続の条件式は以前の条件の false 文になります。
	/// 最後に条件式ではない終端ノードを追加する必要があります。
	/// </summary>
	class ConditionalBuilder
	{
		readonly List<Expression> _conditions = new List<Expression>();
		readonly List<Expression> _bodies = new List<Expression>();
		readonly List<ParameterExpression> _variables = new List<ParameterExpression>();
		Expression _body;
		BindingRestrictions _restrictions = BindingRestrictions.Empty;

		/// <summary>新しい条件式と本体を追加します。最初の呼び出しは最上位の条件式に、後続の呼び出しは以前の条件式の false 文として追加されます。</summary>
		/// <param name="condition"><see cref="System.Boolean"/> 型の結果型をもつ条件式を指定します。</param>
		/// <param name="body"><paramref name="condition"/> が真の場合に実行される式を指定します。</param>
		public void AddCondition(Expression condition, Expression body)
		{
			Assert.NotNull(condition, body);
			_conditions.Add(condition);
			_bodies.Add(body);
		}

		/// <summary>先行するすべての条件が満たされない場合に実行される式を表す <see cref="DynamicMetaObject"/> を追加します。</summary>
		/// <param name="body">先行するすべての条件が満たされない場合に実行される式を保持する <see cref="DynamicMetaObject"/> を指定します。</param>
		public void FinishCondition(DynamicMetaObject body)
		{
			_restrictions = _restrictions.Merge(body.Restrictions);
			FinishCondition(body.Expression);
		}

		/// <summary>先行するすべての条件が満たされない場合に実行される式を追加します。</summary>
		/// <param name="body">先行するすべての条件が満たされない場合に実行される式を指定します。</param>
		public void FinishCondition(Expression body)
		{
			if (_body != null)
				throw new InvalidOperationException();
			for (int i = _bodies.Count - 1; i >= 0; i--)
			{
				var t = _bodies[i].Type;
				if (t != body.Type)
				{
					if (t.IsSubclassOf(body.Type)) // サブクラス
						t = body.Type;
					else if (!body.Type.IsSubclassOf(t)) // 互換ではないため object に
						t = typeof(object);
				}
				body = Ast.Condition(_conditions[i], AstUtils.Convert(_bodies[i], t), AstUtils.Convert(body, t));
			}
			_body = Ast.Block(_variables, body);
		}

		/// <summary>結果として生成される <see cref="DynamicMetaObject"/> に対して適用されるバインディング制約を取得または設定します。</summary>
		public BindingRestrictions Restrictions
		{
			get { return _restrictions; }
			set
			{
				ContractUtils.RequiresNotNull(value, "value");
				_restrictions = value;
			}
		}

		/// <summary>
		/// この条件式を表す結果のメタオブジェクトを取得します。
		/// FinishCondition が呼び出されている必要があります。
		/// </summary>
		/// <param name="types">結果の <see cref="DynamicMetaObject"/> への追加の制約を保持する <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <returns>この条件式を表す <see cref="DynamicMetaObject"/>。</returns>
		public DynamicMetaObject GetMetaObject(params DynamicMetaObject[] types)
		{
			if (_body == null)
				throw new InvalidOperationException("FinishCondition が呼び出されている必要があります。");
			return new DynamicMetaObject(_body, BindingRestrictions.Combine(types).Merge(_restrictions));
		}

		/// <summary>最終式のレベルにスコープされた変数を追加します。</summary>
		/// <param name="var">この条件式に追加する変数を指定します。</param>
		public void AddVariable(ParameterExpression var) { _variables.Add(var); }
	}
}
