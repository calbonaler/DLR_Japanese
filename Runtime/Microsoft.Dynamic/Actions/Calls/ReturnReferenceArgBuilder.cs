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
using System.Reflection;

namespace Microsoft.Scripting.Actions.Calls
{
	using Ast = Expression;

	/// <summary>
	/// <see cref="System.Runtime.CompilerServices.StrongBox&lt;T&gt;"/> が提供されないときに、参照引数に対する実引数を構築します。
	/// 更新された値は戻り値の一部として返されます。
	/// </summary>
	sealed class ReturnReferenceArgBuilder : SimpleArgBuilder
	{
		ParameterExpression _tmp;

		/// <summary>仮引数の情報および実引数の位置を使用して、<see cref="Microsoft.Scripting.Actions.Calls.ReturnReferenceArgBuilder"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="info">仮引数の情報を表す <see cref="ParameterInfo"/> を指定します。</param>
		/// <param name="index">実引数の位置を示すインデックスを指定します。</param>
		public ReturnReferenceArgBuilder(ParameterInfo info, int index) : base(info, info.ParameterType.GetElementType(), index, false, false) { }

		/// <summary>この <see cref="SimpleArgBuilder"/> の引数の位置を指定された位置に置き換えた新しい <see cref="SimpleArgBuilder"/> を作成します。</summary>
		/// <param name="newIndex">作成する <see cref="SimpleArgBuilder"/> の引数の位置を示すインデックスを指定します。</param>
		/// <returns>この <see cref="SimpleArgBuilder"/> の引数の位置を指定された位置に置き換えた新しい <see cref="SimpleArgBuilder"/>。</returns>
		protected override SimpleArgBuilder Copy(int newIndex) { return new ReturnReferenceArgBuilder(ParameterInfo, newIndex); }

		/// <summary>指定された引数に対するこの <see cref="ArgBuilder"/> のコピーを生成します。</summary>
		/// <param name="newType">コピーが基にする仮引数を指定します。</param>
		/// <returns>コピーされた <see cref="ArgBuilder"/>。</returns>
		public override ArgBuilder Clone(ParameterInfo newType) { return new ReturnReferenceArgBuilder(newType, Index); }

		/// <summary>引数に渡される値を提供する <see cref="Expression"/> を返します。</summary>
		/// <param name="resolver">メソッドに対するオーバーロードを解決するために使用される <see cref="OverloadResolver"/> を指定します。</param>
		/// <param name="args">制約された引数を指定します。</param>
		/// <param name="hasBeenUsed">呼び出しが完了すると使用された引数に対応する位置に <c>true</c> が格納されます。</param>
		/// <returns>引数に渡される値を提供する <see cref="Expression"/>。引数がスキップされた場合 (つまり、呼び出し先に渡されない場合) <c>null</c> を返します。</returns>
		protected internal override Expression ToExpression(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed)
		{
			_tmp = _tmp ?? resolver.GetTemporary(Type, "outParam");
			return Ast.Block(Ast.Assign(_tmp, base.ToExpression(resolver, args, hasBeenUsed)), _tmp);
		}

		/// <summary>引数が返戻値を生成する (ref あるいは out のような) 場合、呼び出し元に追加で返される値を提供します。</summary>
		/// <param name="resolver">メソッドに対するオーバーロードを解決するために使用される <see cref="OverloadResolver"/> を指定します。</param>
		/// <returns>呼び出し基で追加で返される値を提供する <see cref="Expression"/>。</returns>
		internal override Expression ToReturnExpression(OverloadResolver resolver) { return _tmp; }

		/// <summary>参照渡しの引数によって渡される代入可能な値を取得します。呼び出し後は更新された値が格納されます。</summary>
		internal override Expression ByRefArgument { get { return _tmp; } }

		/// <summary>この引数の優先順位を取得します。</summary>
		public override int Priority { get { return 5; } }
	}
}
