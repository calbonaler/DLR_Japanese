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
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>
	/// メソッドビルダーによって使用される実引数の値を提供します。
	/// メソッドに渡されるそれぞれの物理引数に対して 1 つの <see cref="ArgBuilder"/> が存在します。
	/// メソッドに定義された論理引数を表す <see cref="ParameterWrapper"/> とは対照的です。
	/// </summary>
	public abstract class ArgBuilder
	{
		internal const int AllArguments = -1;

		/// <summary>指定された仮引数を使用して、<see cref="Microsoft.Scripting.Actions.Calls.ArgBuilder"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="info">この <see cref="ArgBuilder"/> が対応する仮引数を指定します。</param>
		protected ArgBuilder(ParameterInfo info) { ParameterInfo = info; }

		/// <summary>この引数の優先順位を取得します。</summary>
		public abstract int Priority { get; }

		// can be null, e.g. for ctor return value builder or custom arg builders
		/// <summary>基になる仮引数を取得します。コンストラクタの返戻値に対する <see cref="ArgBuilder"/> などでは <c>null</c> になることもあります。</summary>
		public ParameterInfo ParameterInfo { get; private set; }

		/// <summary>このビルダによって消費される実際の引数の数を取得します。</summary>
		public abstract int ConsumedArgumentCount { get; }

		/// <summary>引数に渡される値を提供する <see cref="Expression"/> を返します。</summary>
		/// <param name="resolver">メソッドに対するオーバーロードを解決するために使用される <see cref="OverloadResolver"/> を指定します。</param>
		/// <param name="args">制約された引数を指定します。</param>
		/// <param name="hasBeenUsed">呼び出しが完了すると使用された引数に対応する位置に <c>true</c> が格納されます。</param>
		/// <returns>引数に渡される値を提供する <see cref="Expression"/>。引数がスキップされた場合 (つまり、呼び出し先に渡されない場合) <c>null</c> を返します。</returns>
		protected internal abstract Expression ToExpression(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed);

		/// <summary>実引数から引数に渡される値を提供するデリゲートを返します。</summary>
		/// <param name="resolver">メソッドに対するオーバーロードを解決するために使用される <see cref="OverloadResolver"/> を指定します。</param>
		/// <param name="args">制約された引数を指定します。</param>
		/// <param name="hasBeenUsed">呼び出しが完了すると使用された引数に対応する位置に <c>true</c> が格納されます。</param>
		/// <returns>実引数から引数に渡される値を提供するデリゲート。引数がスキップされた場合 (つまり、呼び出し先に渡されない場合) <c>null</c> を返します。</returns>
		protected internal virtual Func<object[], object> ToDelegate(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed) { return null; }

		/// <summary>
		/// 指定されたインデックスに対する実引数にアクセスする関数を表します。
		/// ToDelegate から返されるとき、クローズオーバーされた値は引数を最適化除去したデリゲート呼び出しを可能にします。
		/// この関数はリフレクションを用いて参照されるため、名前を変更する場合は呼び出し元の更新が必要になります。
		/// </summary>
		/// <param name="value">引数に対応するインデックスを指定します。</param>
		/// <param name="args">実引数を指定します。</param>
		/// <returns>指定されたインデックスに対応する実引数。</returns>
		public static object ArgumentRead(object value, object[] args) { return args[(int)value]; }

		/// <summary>引数に対して要求される型を取得します。<see cref="ArgBuilder"/> が引数を消費しない場合は <c>null</c> が返されます。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
		public virtual Type Type { get { return null; } }

		/// <summary>メソッド呼び出しの後に提供された値を更新する <see cref="Expression"/> を返します。</summary>
		/// <param name="resolver">メソッドに対するオーバーロードを解決するために使用される <see cref="OverloadResolver"/> を指定します。</param>
		/// <param name="args">制約された引数を指定します。</param>
		/// <returns>提供された値を更新する <see cref="Expression"/>。更新が不要な場合は <c>null</c> を返します。</returns>
		internal virtual Expression UpdateFromReturn(OverloadResolver resolver, RestrictedArguments args) { return null; }

		/// <summary>引数が返戻値を生成する (ref あるいは out のような) 場合、呼び出し元に追加で返される値を提供します。</summary>
		/// <param name="resolver">メソッドに対するオーバーロードを解決するために使用される <see cref="OverloadResolver"/> を指定します。</param>
		/// <returns>呼び出し基で追加で返される値を提供する <see cref="Expression"/>。</returns>
		internal virtual Expression ToReturnExpression(OverloadResolver resolver) { throw new InvalidOperationException(); }

		/// <summary>参照渡しの引数によって渡される代入可能な値を取得します。呼び出し後は更新された値が格納されます。</summary>
		internal virtual Expression ByRefArgument { get { return null; } }

		/// <summary>指定された引数に対するこの <see cref="ArgBuilder"/> のコピーを生成します。</summary>
		/// <param name="newType">コピーが基にする仮引数を指定します。</param>
		/// <returns>コピーされた <see cref="ArgBuilder"/>。</returns>
		public virtual ArgBuilder Clone(ParameterInfo newType) { return null; }
	}
}
