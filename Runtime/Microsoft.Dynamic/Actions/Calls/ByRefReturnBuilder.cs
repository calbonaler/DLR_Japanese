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

using System.Collections.Generic;
using System.Linq.Expressions;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions.Calls
{
	using Ast = Expression;

	/// <summary>参照渡しされた引数の更新に対する <see cref="ReturnBuilder"/> を表します。</summary>
	sealed class ByRefReturnBuilder : ReturnBuilder
	{
		IList<int> _returnArgs;

		/// <summary>参照渡しされた引数の位置のリストを使用して、<see cref="Microsoft.Scripting.Actions.Calls.ByRefReturnBuilder"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="returnArgs">参照渡しされた引数の位置を示す 0 から始まるインデックスのリストを指定します。</param>
		public ByRefReturnBuilder(IList<int> returnArgs) : base(typeof(object)) { _returnArgs = returnArgs; }

		/// <summary>メソッド呼び出しの結果を返す <see cref="Expression"/> を返します。</summary>
		/// <param name="resolver">メソッドに対するオーバーロードを解決するために使用される <see cref="OverloadResolver"/> を指定します。</param>
		/// <param name="builders">メソッドに渡されたそれぞれの実引数に対する <see cref="ArgBuilder"/> のリストを指定します。</param>
		/// <param name="args">制約された引数を指定します。</param>
		/// <param name="ret">メソッド呼び出しの現在の結果を表す <see cref="Expression"/> を指定します。</param>
		/// <returns>メソッド呼び出しの結果を表す <see cref="Expression"/>。</returns>
		internal override Expression ToExpression(OverloadResolver resolver, IList<ArgBuilder> builders, RestrictedArguments args, Expression ret)
		{
			if (_returnArgs.Count == 1)
			{
				if (_returnArgs[0] == -1)
					return ret;
				return Ast.Block(ret, builders[_returnArgs[0]].ToReturnExpression(resolver));
			}
			Expression[] retValues = new Expression[_returnArgs.Count];
			int rIndex = 0;
			bool usesRet = false;
			foreach (int index in _returnArgs)
			{
				if (index == -1)
				{
					usesRet = true;
					retValues[rIndex++] = ret;
				}
				else
					retValues[rIndex++] = builders[index].ToReturnExpression(resolver);
			}
			Expression retArray = AstUtils.NewArrayHelper(typeof(object), retValues);
			if (!usesRet)
				retArray = Ast.Block(ret, retArray);
			return resolver.GetByRefArrayExpression(retArray);
		}

		/// <summary>戻り値を生成するような引数の数を取得します。</summary>
		public override int CountOutParams { get { return _returnArgs.Count; } }
	}
}
