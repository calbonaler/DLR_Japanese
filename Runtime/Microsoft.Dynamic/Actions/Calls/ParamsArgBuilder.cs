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
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions.Calls
{
	using Ast = Expression;

	/// <summary>配列引数を仮引数に提供する <see cref="ArgBuilder"/> です。</summary>
	sealed class ParamsArgBuilder : ArgBuilder
	{
		readonly int _start;
		readonly int _expandedCount;
		readonly Type _elementType;

		/// <summary>仮引数のメタデータ、要素型、展開の開始位置、展開された実引数の数を使用して、<see cref="Microsoft.Scripting.Actions.Calls.ParamsArgBuilder"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="info">配列引数を表す仮引数のメタデータを指定します。</param>
		/// <param name="elementType">配列引数の要素型を指定します。</param>
		/// <param name="start">配列引数の展開の開始位置を指定します。</param>
		/// <param name="expandedCount">展開された実引数の数を指定します。</param>
		internal ParamsArgBuilder(ParameterInfo info, Type elementType, int start, int expandedCount) : base(info)
		{
			Assert.NotNull(elementType);
			Debug.Assert(start >= 0);
			Debug.Assert(expandedCount >= 0);
			_start = start;
			_expandedCount = expandedCount;
			_elementType = elementType;
		}

		// Consumes all expanded arguments. 
		// Collapsed arguments are fetched from resolver provided storage, not from actual argument expressions.
		/// <summary>このビルダによって消費される実際の引数の数を取得します。展開された実引数の数だけ消費されます。</summary>
		public override int ConsumedArgumentCount { get { return _expandedCount; } }

		/// <summary>この引数の優先順位を取得します。</summary>
		public override int Priority { get { return 4; } }

		/// <summary>引数に渡される値を提供する <see cref="Expression"/> を返します。</summary>
		/// <param name="resolver">メソッドに対するオーバーロードを解決するために使用される <see cref="OverloadResolver"/> を指定します。</param>
		/// <param name="args">制約された引数を指定します。</param>
		/// <param name="hasBeenUsed">呼び出しが完了すると使用された引数に対応する位置に <c>true</c> が格納されます。</param>
		/// <returns>引数に渡される値を提供する <see cref="Expression"/>。引数がスキップされた場合 (つまり、呼び出し先に渡されない場合) <c>null</c> を返します。</returns>
		protected internal override Expression ToExpression(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed)
		{
			var result = new Expression[2 + _expandedCount + (resolver.ActualArguments.CollapsedCount > 0 ? 2 : 0)];
			var a = resolver.GetTemporary(_elementType.MakeArrayType(), "a");
			int e = 0;
			result[e++] = Ast.Assign(a, Ast.NewArrayBounds(_elementType, Ast.Constant(_expandedCount + resolver.ActualArguments.CollapsedCount)));
			int itemIndex = 0;
			for (int i = _start; ; i++)
			{
				// inject loop copying collapsed items:
				if (i == resolver.ActualArguments.SplatIndex)
				{
					var t = resolver.GetTemporary(typeof(int), "t");
					// for (int t = 0; t <= {collapsedCount}; t++) {
					//   a[{itemIndex} + t] = CONVERT<ElementType>(list.get_Item({splatIndex - firstSplatted} + t))
					// }
					result[e++] = Ast.Assign(t, AstUtils.Constant(0));
					// TODO: not implemented in the old interpreter Ast.PostIncrementAssign(indexVariable),
					result[e++] = AstUtils.Loop(Ast.LessThan(t, Ast.Constant(resolver.ActualArguments.CollapsedCount)), Ast.Assign(t, Ast.Add(t, AstUtils.Constant(1))),
						Ast.Assign(
							Ast.ArrayAccess(a, Ast.Add(AstUtils.Constant(itemIndex), t)),
							resolver.Convert(
								new DynamicMetaObject(
									resolver.GetSplattedItemExpression(Ast.Add(AstUtils.Constant(resolver.ActualArguments.SplatIndex - resolver.ActualArguments.FirstSplattedArg), t)),
									BindingRestrictions.Empty
								),
								null,
								ParameterInfo,
								_elementType
							)
						),
						null
					);
					itemIndex += resolver.ActualArguments.CollapsedCount;
				}
				if (i >= _start + _expandedCount)
					break;
				Debug.Assert(!hasBeenUsed[i]);
				hasBeenUsed[i] = true;
				// a[{itemIndex++}] = CONVERT<ElementType>({args[i]})
				result[e++] = Ast.Assign(Ast.ArrayAccess(a, AstUtils.Constant(itemIndex++)), resolver.Convert(args.Objects[i], args.Types[i], ParameterInfo, _elementType));
			}
			result[e++] = a;
			Debug.Assert(e == result.Length);
			return Ast.Block(result);
		}

		/// <summary>実引数から引数に渡される値を提供するデリゲートを返します。</summary>
		/// <param name="resolver">メソッドに対するオーバーロードを解決するために使用される <see cref="OverloadResolver"/> を指定します。</param>
		/// <param name="args">制約された引数を指定します。</param>
		/// <param name="hasBeenUsed">呼び出しが完了すると使用された引数に対応する位置に <c>true</c> が格納されます。</param>
		/// <returns>実引数から引数に渡される値を提供するデリゲート。引数がスキップされた場合 (つまり、呼び出し先に渡されない場合) <c>null</c> を返します。</returns>
		protected internal override Func<object[], object> ToDelegate(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed)
		{
			if (resolver.ActualArguments.CollapsedCount > 0)
				return null;
			var indexes = new List<Func<object[], object>>(_expandedCount);
			foreach (var i in Enumerable.Range(_start, _expandedCount).Where(i => !hasBeenUsed[i]))
			{
				indexes.Add(resolver.GetConvertor(i + 1, args.Objects[i], ParameterInfo, _elementType));
				hasBeenUsed[i] = true;
			}
			if (_elementType == typeof(object))
				return new ParamArrayDelegate<object>(indexes.ToArray(), _start).MakeParamsArray;
			Type genType = typeof(ParamArrayDelegate<>).MakeGenericType(_elementType);
			return (Func<object[], object>)Delegate.CreateDelegate(
				typeof(Func<object[], object>),
				Activator.CreateInstance(genType, indexes.ToArray(), _start),
				genType.GetMethod("MakeParamsArray"));
		}

		class ParamArrayDelegate<T>
		{
			readonly Func<object[], object>[] _indexes;
			readonly int _start;

			public ParamArrayDelegate(Func<object[], object>[] indexes, int start)
			{
				_indexes = indexes;
				_start = start;
			}

			public T[] MakeParamsArray(object[] args) { return Enumerable.Range(0, _indexes.Length).Select(i => _indexes[i] == null ? (T)args[_start + i + 1] : (T)_indexes[i](args)).ToArray(); }
		}

		/// <summary>引数に対して要求される型を取得します。<see cref="ParamsArgBuilder"/> の要素型の配列型になります。</summary>
		public override Type Type { get { return _elementType.MakeArrayType(); } }

		/// <summary>指定された引数に対するこの <see cref="ArgBuilder"/> のコピーを生成します。</summary>
		/// <param name="newType">コピーが基にする仮引数を指定します。</param>
		/// <returns>コピーされた <see cref="ArgBuilder"/>。</returns>
		public override ArgBuilder Clone(ParameterInfo newType) { return new ParamsArgBuilder(newType, newType.ParameterType.GetElementType(), _start, _expandedCount); }
	}
}
