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
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>
	/// ユーザーによって実引数の値として生成された値を生成します。
	/// このクラスはさらに元の仮引数に関する情報を追跡し、配列引数や辞書引数を持つ関数に対する拡張メソッドを作成するために使用されます。
	/// </summary>
	public class SimpleArgBuilder : ArgBuilder
	{
		readonly Type _parameterType;

		/// <summary>実引数に対する仮引数の情報が利用できない場合に、<see cref="Microsoft.Scripting.Actions.Calls.SimpleArgBuilder"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="parameterType">対象となる仮引数の型を指定します。</param>
		/// <param name="index">実引数の位置を示すインデックスを指定します。</param>
		/// <param name="isParams">この引数が配列引数かどうかを示す値を指定します。</param>
		/// <param name="isParamsDict">この引数が辞書引数かどうかを示す値を指定します。</param>
		public SimpleArgBuilder(Type parameterType, int index, bool isParams, bool isParamsDict) : this(null, parameterType, index, isParams, isParamsDict) { }

		/// <summary>実引数に対する仮引数の情報が利用できない場合に、<see cref="Microsoft.Scripting.Actions.Calls.SimpleArgBuilder"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="info">仮引数の情報を表す <see cref="ParameterInfo"/> を指定します。</param>
		/// <param name="parameterType">対象となる仮引数の型を指定します。</param>
		/// <param name="index">実引数の位置を示すインデックスを指定します。</param>
		/// <param name="isParams">この引数が配列引数かどうかを示す値を指定します。</param>
		/// <param name="isParamsDict">この引数が辞書引数かどうかを示す値を指定します。</param>
		public SimpleArgBuilder(ParameterInfo info, Type parameterType, int index, bool isParams, bool isParamsDict) : base(info)
		{
			ContractUtils.Requires(index >= 0, "index");
			ContractUtils.RequiresNotNull(parameterType, "parameterType");
			Index = index;
			_parameterType = parameterType;
			IsParamsArray = isParams;
			IsParamsDict = isParamsDict;
		}

		/// <summary>この <see cref="SimpleArgBuilder"/> の引数の位置を指定された位置に置き換えた新しい <see cref="SimpleArgBuilder"/> を作成します。</summary>
		/// <param name="newIndex">作成する <see cref="SimpleArgBuilder"/> の引数の位置を示すインデックスを指定します。</param>
		/// <returns>この <see cref="SimpleArgBuilder"/> の引数の位置を指定された位置に置き換えた新しい <see cref="SimpleArgBuilder"/>。</returns>
		internal SimpleArgBuilder MakeCopy(int newIndex)
		{
			var result = Copy(newIndex);
			// Copy() must be overriden in derived classes and return an instance of the derived class:
			Debug.Assert(result.GetType() == GetType());
			return result;
		}

		/// <summary>この <see cref="SimpleArgBuilder"/> の引数の位置を指定された位置に置き換えた新しい <see cref="SimpleArgBuilder"/> を作成します。</summary>
		/// <param name="newIndex">作成する <see cref="SimpleArgBuilder"/> の引数の位置を示すインデックスを指定します。</param>
		/// <returns>この <see cref="SimpleArgBuilder"/> の引数の位置を指定された位置に置き換えた新しい <see cref="SimpleArgBuilder"/>。</returns>
		protected virtual SimpleArgBuilder Copy(int newIndex) { return new SimpleArgBuilder(ParameterInfo, _parameterType, newIndex, IsParamsArray, IsParamsDict); }

		/// <summary>このビルダによって消費される実際の引数の数を取得します。</summary>
		public override int ConsumedArgumentCount { get { return 1; } }

		/// <summary>この引数の優先順位を取得します。</summary>
		public override int Priority { get { return 0; } }

		/// <summary>この引数が配列引数であるかどうかを示す値を取得します。</summary>
		public bool IsParamsArray { get; private set; }

		/// <summary>この引数が辞書引数であるかどうかを示す値を取得します。</summary>
		public bool IsParamsDict { get; private set; }

		/// <summary>引数に渡される値を提供する <see cref="Expression"/> を返します。</summary>
		/// <param name="resolver">メソッドに対するオーバーロードを解決するために使用される <see cref="OverloadResolver"/> を指定します。</param>
		/// <param name="args">制約された引数を指定します。</param>
		/// <param name="hasBeenUsed">呼び出しが完了すると使用された引数に対応する位置に <c>true</c> が格納されます。</param>
		/// <returns>引数に渡される値を提供する <see cref="Expression"/>。引数がスキップされた場合 (つまり、呼び出し先に渡されない場合) <c>null</c> を返します。</returns>
		protected internal override Expression ToExpression(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed)
		{
			ContractUtils.Requires(hasBeenUsed.Length == args.Length, "hasBeenUsed");
			ContractUtils.RequiresArrayIndex(args.Length, Index, "args");
			ContractUtils.Requires(!hasBeenUsed[Index], "hasBeenUsed");
			hasBeenUsed[Index] = true;
			return resolver.Convert(args.Objects[Index], args.Types[Index], ParameterInfo, _parameterType);
		}

		/// <summary>実引数から引数に渡される値を提供するデリゲートを返します。</summary>
		/// <param name="resolver">メソッドに対するオーバーロードを解決するために使用される <see cref="OverloadResolver"/> を指定します。</param>
		/// <param name="args">制約された引数を指定します。</param>
		/// <param name="hasBeenUsed">呼び出しが完了すると使用された引数に対応する位置に <c>true</c> が格納されます。</param>
		/// <returns>実引数から引数に渡される値を提供するデリゲート。引数がスキップされた場合 (つまり、呼び出し先に渡されない場合) <c>null</c> を返します。</returns>
		protected internal override Func<object[], object> ToDelegate(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed)
		{
			var conv = resolver.GetConvertor(Index + 1, args.Objects[Index], ParameterInfo, _parameterType);
			if (conv != null)
				return conv;
			return (Func<object[], object>)Delegate.CreateDelegate(typeof(Func<object[], object>), Index + 1, new Func<object, object[], object>(ArgBuilder.ArgumentRead).Method);
		}

		// Index of actual argument expression.
		/// <summary>実引数の位置を示すインデックスを取得します。</summary>
		public int Index { get; private set; }

		/// <summary>引数に対して要求される型を取得します。<see cref="ArgBuilder"/> が引数を消費しない場合は <c>null</c> が返されます。</summary>
		public override Type Type { get { return _parameterType; } }

		/// <summary>指定された引数に対するこの <see cref="ArgBuilder"/> のコピーを生成します。</summary>
		/// <param name="newType">コピーが基にする仮引数を指定します。</param>
		/// <returns>コピーされた <see cref="ArgBuilder"/>。</returns>
		public override ArgBuilder Clone(ParameterInfo newType) { return new SimpleArgBuilder(newType, newType.ParameterType, Index, IsParamsArray, IsParamsDict); }
	}
}
