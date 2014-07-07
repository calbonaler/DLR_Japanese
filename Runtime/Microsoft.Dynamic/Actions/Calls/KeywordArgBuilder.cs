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

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>キーワード引数に対する値を提供する <see cref="ArgBuilder"/> です。
	/// 
	/// <see cref="KeywordArgBuilder"/> はエミット時における位置をユーザーから渡されたキーワード引数内における初期オフセット、キーワード引数の数、引数の総数から計算します。
	/// その後、単一の正確な引数のみを受け入れる基になる <see cref="ArgBuilder"/> に処理を委譲します。
	/// エミット時まで位置の計算を遅延させることで、ユーザーから渡された正確な引数の数を知らなくてもメソッドバインディングを完了できるようになります。
	/// したがって、メソッドバインダはメソッドオーバーロードセットとキーワード名にのみ依存することになり、ユーザー引数への依存はなくなります。
	/// ユーザー引数の数は事前に決定できますが、現在のメソッドバインダはこの形式をとっていません。
	/// </summary>
	sealed class KeywordArgBuilder : ArgBuilder
	{
		readonly int _kwArgCount, _kwArgIndex;
		readonly ArgBuilder _builder;

		/// <summary>基になる <see cref="ArgBuilder"/>、キーワード引数の数およびキーワード引数内の位置を使用して、<see cref="Microsoft.Scripting.Actions.Calls.KeywordArgBuilder"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="builder">基になる <see cref="ArgBuilder"/> を指定します。</param>
		/// <param name="kwArgCount">キーワード引数の数を指定します。</param>
		/// <param name="kwArgIndex">キーワード引数内の現在の引数の位置を指定します。</param>
		public KeywordArgBuilder(ArgBuilder builder, int kwArgCount, int kwArgIndex) : base(builder.ParameterInfo)
		{
			Debug.Assert(BuilderExpectsSingleParameter(builder));
			Debug.Assert(builder.ConsumedArgumentCount == 1);
			_builder = builder;
			Debug.Assert(kwArgIndex < kwArgCount);
			_kwArgCount = kwArgCount;
			_kwArgIndex = kwArgIndex;
		}

		/// <summary>この引数の優先順位を取得します。</summary>
		public override int Priority { get { return _builder.Priority; } }

		/// <summary>このビルダによって消費される実際の引数の数を取得します。</summary>
		public override int ConsumedArgumentCount { get { return 1; } }

		/// <summary>指定された <see cref="ArgBuilder"/> が単一の引数のみをことを保証します。</summary>
		/// <param name="builder">判断する <see cref="ArgBuilder"/> を指定します。</param>
		internal static bool BuilderExpectsSingleParameter(ArgBuilder builder) { return ((SimpleArgBuilder)builder).Index == 0; }

		/// <summary>引数に渡される値を提供する <see cref="Expression"/> を返します。</summary>
		/// <param name="resolver">メソッドに対するオーバーロードを解決するために使用される <see cref="OverloadResolver"/> を指定します。</param>
		/// <param name="args">制約された引数を指定します。</param>
		/// <param name="hasBeenUsed">呼び出しが完了すると使用された引数に対応する位置に <c>true</c> が格納されます。</param>
		/// <returns>引数に渡される値を提供する <see cref="Expression"/>。引数がスキップされた場合 (つまり、呼び出し先に渡されない場合) <c>null</c> を返します。</returns>
		protected internal override Expression ToExpression(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed)
		{
			Debug.Assert(BuilderExpectsSingleParameter(_builder));
			int index = GetKeywordIndex(args.Length);
			Debug.Assert(!hasBeenUsed[index]);
			hasBeenUsed[index] = true;
			return _builder.ToExpression(resolver, MakeRestrictedArg(args, index), new bool[1]);
		}

		/// <summary>引数に対して要求される型を取得します。<see cref="ArgBuilder"/> が引数を消費しない場合は <c>null</c> が返されます。</summary>
		public override Type Type { get { return _builder.Type; } }

		/// <summary>引数が返戻値を生成する (ref あるいは out のような) 場合、呼び出し元に追加で返される値を提供します。</summary>
		/// <param name="resolver">メソッドに対するオーバーロードを解決するために使用される <see cref="OverloadResolver"/> を指定します。</param>
		/// <returns>呼び出し基で追加で返される値を提供する <see cref="Expression"/>。</returns>
		internal override Expression ToReturnExpression(OverloadResolver resolver) { return _builder.ToReturnExpression(resolver); }

		/// <summary>メソッド呼び出しの後に提供された値を更新する <see cref="Expression"/> を返します。</summary>
		/// <param name="resolver">メソッドに対するオーバーロードを解決するために使用される <see cref="OverloadResolver"/> を指定します。</param>
		/// <param name="args">制約された引数を指定します。</param>
		/// <returns>提供された値を更新する <see cref="Expression"/>。更新が不要な場合は <c>null</c> を返します。</returns>
		internal override Expression UpdateFromReturn(OverloadResolver resolver, RestrictedArguments args) { return _builder.UpdateFromReturn(resolver, MakeRestrictedArg(args, GetKeywordIndex(args.Length))); }

		static RestrictedArguments MakeRestrictedArg(RestrictedArguments args, int index) { return new RestrictedArguments(new[] { args.Objects[index] }, new[] { args.Types[index] }, false); }

		int GetKeywordIndex(int paramCount) { return paramCount - _kwArgCount + _kwArgIndex; }

		/// <summary>参照私の引数によって渡される代入可能な値を取得します。呼び出し後は更新された値が格納されます。</summary>
		internal override Expression ByRefArgument { get { return _builder.ByRefArgument; } }

		/// <summary>指定された引数に対するこの <see cref="ArgBuilder"/> のコピーを生成します。</summary>
		/// <param name="newType">コピーが基にする仮引数を指定します。</param>
		/// <returns>コピーされた <see cref="ArgBuilder"/>。</returns>
		public override ArgBuilder Clone(ParameterInfo newType) { return new KeywordArgBuilder(_builder.Clone(newType), _kwArgCount, _kwArgIndex); }
	}
}
