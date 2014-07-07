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

using System.Linq.Expressions;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>メソッドビルダーによって使用される、戻り値を表す式を構築する方法を提供します。</summary>
	class ReturnBuilder
	{
		/// <summary>戻り値の型を使用して、<see cref="Microsoft.Scripting.Actions.Calls.ReturnBuilder"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="returnType"><see cref="ReturnBuilder"/> がスタックに置く値の型を指定します。</param>
		public ReturnBuilder(Type returnType)
		{
			Debug.Assert(returnType != null);
			ReturnType = returnType;
		}

		/// <summary>メソッド呼び出しの結果を返す <see cref="Expression"/> を返します。</summary>
		/// <param name="resolver">メソッドに対するオーバーロードを解決するために使用される <see cref="OverloadResolver"/> を指定します。</param>
		/// <param name="builders">メソッドに渡されたそれぞれの実引数に対する <see cref="ArgBuilder"/> のリストを指定します。</param>
		/// <param name="args">制約された引数を指定します。</param>
		/// <param name="ret">メソッド呼び出しの現在の結果を表す <see cref="Expression"/> を指定します。</param>
		/// <returns>メソッド呼び出しの結果を表す <see cref="Expression"/>。</returns>
		internal virtual Expression ToExpression(OverloadResolver resolver, IList<ArgBuilder> builders, RestrictedArguments args, Expression ret) { return ret; }

		/// <summary>戻り値を生成するような引数の数を取得します。</summary>
		public virtual int CountOutParams { get { return 0; } }

		/// <summary>このビルダーが表す戻り値の型を取得します。</summary>
		public Type ReturnType { get; private set; }
	}
}
