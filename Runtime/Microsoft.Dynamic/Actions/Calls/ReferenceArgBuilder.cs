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
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using AstUtils = Microsoft.Scripting.Ast.Utils;
using RuntimeHelpers = Microsoft.Scripting.Runtime.ScriptingRuntimeHelpers;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>
	/// ユーザーが明示的に参照を使用して (copy-in または copy-out セマンティクスで) 渡すことを希望している引数を表します。
	/// ユーザーは呼び出しが完了した際に値が更新される <see cref="StrongBox&lt;T&gt;"/> オブジェクトを渡します。
	/// </summary>
	sealed class ReferenceArgBuilder : SimpleArgBuilder
	{
		readonly Type _elementType;
		ParameterExpression _tmp;

		/// <summary>仮引数に関する情報、要素型、引数の位置を指定して、<see cref="Microsoft.Scripting.Actions.Calls.ReferenceArgBuilder"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="info">仮引数の情報を表す <see cref="ParameterInfo"/> を指定します。</param>
		/// <param name="elementType">引数の要素型を指定します。</param>
		/// <param name="index">実引数の位置を示すインデックスを指定します。</param>
		public ReferenceArgBuilder(ParameterInfo info, Type elementType, int index) : base(info, typeof(StrongBox<>).MakeGenericType(elementType), index, false, false) { _elementType = elementType; }

		/// <summary>この <see cref="SimpleArgBuilder"/> の引数の位置を指定された位置に置き換えた新しい <see cref="SimpleArgBuilder"/> を作成します。</summary>
		/// <param name="newIndex">作成する <see cref="SimpleArgBuilder"/> の引数の位置を示すインデックスを指定します。</param>
		/// <returns>この <see cref="SimpleArgBuilder"/> の引数の位置を指定された位置に置き換えた新しい <see cref="SimpleArgBuilder"/>。</returns>
		protected override SimpleArgBuilder Copy(int newIndex) { return new ReferenceArgBuilder(ParameterInfo, _elementType, newIndex); }

		/// <summary>指定された引数に対するこの <see cref="ArgBuilder"/> のコピーを生成します。</summary>
		/// <param name="newType">コピーが基にする仮引数を指定します。</param>
		/// <returns>コピーされた <see cref="ArgBuilder"/>。</returns>
		public override ArgBuilder Clone(ParameterInfo newType)
		{
			var elementType = newType.ParameterType.GetElementType();
			return new ReferenceArgBuilder(newType, elementType, Index);
		}

		/// <summary>この引数の優先順位を取得します。</summary>
		public override int Priority { get { return 5; } }

		/// <summary>引数に渡される値を提供する <see cref="Expression"/> を返します。</summary>
		/// <param name="resolver">メソッドに対するオーバーロードを解決するために使用される <see cref="OverloadResolver"/> を指定します。</param>
		/// <param name="args">制約された引数を指定します。</param>
		/// <param name="hasBeenUsed">呼び出しが完了すると使用された引数に対応する位置に <c>true</c> が格納されます。</param>
		/// <returns>引数に渡される値を提供する <see cref="Expression"/>。引数がスキップされた場合 (つまり、呼び出し先に渡されない場合) <c>null</c> を返します。</returns>
		protected internal override Expression ToExpression(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed)
		{
			Debug.Assert(!hasBeenUsed[Index]);
			hasBeenUsed[Index] = true;
			return Expression.Condition(Expression.TypeIs(args.Objects[Index].Expression, Type),
				Expression.Assign(_tmp ?? (_tmp = resolver.GetTemporary(_elementType, "outParam")), Expression.Field(AstUtils.Convert(args.Objects[Index].Expression, Type), Type.GetField("Value"))),
				Expression.Throw(
					Expression.Call(
						new Func<Type, object, Exception>(RuntimeHelpers.MakeIncorrectBoxTypeError).Method,
						AstUtils.Constant(_elementType),
						AstUtils.Convert(args.Objects[Index].Expression, typeof(object))
					), _elementType
				)
			);
		}

		/// <summary>実引数から引数に渡される値を提供するデリゲートを返します。</summary>
		/// <param name="resolver">メソッドに対するオーバーロードを解決するために使用される <see cref="OverloadResolver"/> を指定します。</param>
		/// <param name="args">制約された引数を指定します。</param>
		/// <param name="hasBeenUsed">呼び出しが完了すると使用された引数に対応する位置に <c>true</c> が格納されます。</param>
		/// <returns>実引数から引数に渡される値を提供するデリゲート。引数がスキップされた場合 (つまり、呼び出し先に渡されない場合) <c>null</c> を返します。</returns>
		protected internal override Func<object[], object> ToDelegate(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed) { return null; }

		/// <summary>メソッド呼び出しの後に提供された値を更新する <see cref="Expression"/> を返します。</summary>
		/// <param name="resolver">メソッドに対するオーバーロードを解決するために使用される <see cref="OverloadResolver"/> を指定します。</param>
		/// <param name="args">制約された引数を指定します。</param>
		/// <returns>提供された値を更新する <see cref="Expression"/>。更新が不要な場合は <c>null</c> を返します。</returns>
		internal override Expression UpdateFromReturn(OverloadResolver resolver, RestrictedArguments args)
		{
			return Expression.Assign(Expression.Field(Expression.Convert(args.Objects[Index].Expression, Type), Type.GetField("Value")), _tmp);
		}

		/// <summary>参照渡しの引数によって渡される代入可能な値を取得します。呼び出し後は更新された値が格納されます。</summary>
		internal override Expression ByRefArgument { get { return _tmp; } }
	}
}
