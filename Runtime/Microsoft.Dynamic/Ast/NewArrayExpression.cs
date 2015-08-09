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
	public static partial class Utils
	{
		/// <summary>
		/// 要素のリストからの 1 次元配列の初期化を表すノードを作成します。
		/// このメソッドは初期化リストのそれぞれの式に対して必要であれば <see cref="Convert"/> または <see cref="Expression.Quote"/> を使用した変換を行います。
		/// </summary>
		/// <param name="type">作成する 1 次元配列の要素の型を指定します。</param>
		/// <param name="initializers">配列の初期化リストを指定します。</param>
		/// <returns>新しい配列の初期化を表す <see cref="NewArrayExpression"/>。</returns>
		public static NewArrayExpression NewArrayHelper(Type type, IEnumerable<Expression> initializers)
		{
			ContractUtils.RequiresNotNull(type, "type");
			ContractUtils.RequiresNotNullItems(initializers, "initializers");
			ContractUtils.Requires(type != typeof(void), "type", "型を void にすることはできません。");
			return Expression.NewArrayInit(
				type,
				initializers.Select(x => !TypeUtils.AreReferenceAssignable(type, x.Type) ?
					(type.IsSubclassOf(typeof(Expression)) && TypeUtils.AreAssignable(type, x.GetType()) ?
						Expression.Quote(x) :
						Convert(x, type)
					) : x
				)
			);
		}
	}
}
