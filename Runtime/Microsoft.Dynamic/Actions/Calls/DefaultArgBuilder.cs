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
using System.Diagnostics;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>メソッド呼び出しに対して引数の既定値を提供する <see cref="ArgBuilder"/> です。</summary>
	sealed class DefaultArgBuilder : ArgBuilder
	{
		/// <summary>指定された仮引数を使用して、<see cref="Microsoft.Scripting.Actions.Calls.ArgBuilder"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="info">この <see cref="ArgBuilder"/> が対応する仮引数を指定します。</param>
		public DefaultArgBuilder(ParameterInfo info) : base(info) { Assert.NotNull(info); }

		/// <summary>この引数の優先順位を取得します。</summary>
		public override int Priority { get { return 2; } }

		/// <summary>このビルダによって消費される実際の引数の数を取得します。</summary>
		public override int ConsumedArgumentCount { get { return 0; } }

		/// <summary>引数に渡される値を提供する <see cref="Expression"/> を返します。</summary>
		/// <param name="resolver">メソッドに対するオーバーロードを解決するために使用される <see cref="OverloadResolver"/> を指定します。</param>
		/// <param name="args">制約された引数を指定します。</param>
		/// <param name="hasBeenUsed">呼び出しが完了すると使用された引数に対応する位置に <c>true</c> が格納されます。</param>
		/// <returns>引数に渡される値を提供する <see cref="Expression"/>。引数がスキップされた場合 (つまり、呼び出し先に渡されない場合) <c>null</c> を返します。</returns>
		protected internal override Expression ToExpression(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed)
		{
			object value = ParameterInfo.DefaultValue;
			if (value is Missing)
				value = CompilerHelpers.GetMissingValue(ParameterInfo.ParameterType);
			if (ParameterInfo.ParameterType.IsByRef)
				return AstUtils.Constant(value, ParameterInfo.ParameterType.GetElementType());
			return resolver.Convert(new DynamicMetaObject(AstUtils.Constant(value), BindingRestrictions.Empty, value), CompilerHelpers.GetType(value), ParameterInfo, ParameterInfo.ParameterType);
		}

		/// <summary>実引数から引数に渡される値を提供するデリゲートを返します。</summary>
		/// <param name="resolver">メソッドに対するオーバーロードを解決するために使用される <see cref="OverloadResolver"/> を指定します。</param>
		/// <param name="args">制約された引数を指定します。</param>
		/// <param name="hasBeenUsed">呼び出しが完了すると使用された引数に対応する位置に <c>true</c> が格納されます。</param>
		/// <returns>実引数から引数に渡される値を提供するデリゲート。引数がスキップされた場合 (つまり、呼び出し先に渡されない場合) <c>null</c> を返します。</returns>
		protected internal override Func<object[], object> ToDelegate(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed)
		{
			if (ParameterInfo.ParameterType.IsByRef)
				return null;
			else if (ParameterInfo.DefaultValue is Missing && CompilerHelpers.GetMissingValue(ParameterInfo.ParameterType) is Missing)
				return null;  // reflection throws when we do this
			object val = ParameterInfo.DefaultValue;
			if (val is Missing)
				val = CompilerHelpers.GetMissingValue(ParameterInfo.ParameterType);
			Debug.Assert(val != Missing.Value);
			return _ => val;
		}
	}
}
