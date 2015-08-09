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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions.Calls
{
	using Ast = Expression;

	/// <summary>辞書引数である実引数を仮引数に提供します。これは関数に提供されるすべての余分な名前/値ペアを関数に渡されるシンボルディクショナリに収集します。</summary>
	sealed class ParamsDictArgBuilder : ArgBuilder
	{
		readonly string[] _names;
		readonly int[] _nameIndexes;
		readonly int _argIndex;

		/// <summary>仮引数に関するメタデータ、実引数リスト内での辞書引数の開始位置、名前および対応するインデックスのリストを使用して、<see cref="Microsoft.Scripting.Actions.Calls.ParamsDictArgBuilder"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="info">仮引数に関するメタデータを指定します。</param>
		/// <param name="argIndex">実引数リスト内での辞書引数の開始位置を示す 0 から始まるインデックスを指定します。</param>
		/// <param name="names">辞書引数の名前のリストを指定します。</param>
		/// <param name="nameIndexes">辞書引数のインデックスのリストを指定します。</param>
		public ParamsDictArgBuilder(ParameterInfo info, int argIndex, string[] names, int[] nameIndexes) : base(info)
		{
			Assert.NotNull(info, names, nameIndexes);
			_argIndex = argIndex;
			_names = names;
			_nameIndexes = nameIndexes;
		}

		/// <summary>このビルダによって消費される実際の引数の数を取得します。<see cref="ParamsDictArgBuilder"/> では残りのすべての引数が消費されます。</summary>
		public override int ConsumedArgumentCount { get { return AllArguments; } }

		/// <summary>この引数の優先順位を取得します。</summary>
		public override int Priority { get { return 3; } }

		/// <summary>引数に渡される値を提供する <see cref="Expression"/> を返します。</summary>
		/// <param name="resolver">メソッドに対するオーバーロードを解決するために使用される <see cref="OverloadResolver"/> を指定します。</param>
		/// <param name="args">制約された引数を指定します。</param>
		/// <param name="hasBeenUsed">呼び出しが完了すると使用された引数に対応する位置に <c>true</c> が格納されます。</param>
		/// <returns>引数に渡される値を提供する <see cref="Expression"/>。引数がスキップされた場合 (つまり、呼び出し先に渡されない場合) <c>null</c> を返します。</returns>
		protected internal override Expression ToExpression(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed)
		{
			Type dictType = ParameterInfo.ParameterType;
			return Ast.Call(
				GetCreationDelegate(dictType).Method,
				Ast.NewArrayInit(typeof(string), _names.Select(x => AstUtils.Constant(x))),
				AstUtils.NewArrayHelper(typeof(object), GetParameters(hasBeenUsed).Select(x => args.Objects[x].Expression))
			);
		}

		/// <summary>引数に対して要求される型を取得します。</summary>
		public override Type Type { get { return typeof(IAttributesCollection); } }

		IEnumerable<int> GetParameters(bool[] hasBeenUsed)
		{
			var result = _nameIndexes.Select(x => x + _argIndex).Where(x => !hasBeenUsed[x]);
			foreach (var index in result)
				hasBeenUsed[index] = true;
			return result;
		}

		/// <summary>実引数から引数に渡される値を提供するデリゲートを返します。</summary>
		/// <param name="resolver">メソッドに対するオーバーロードを解決するために使用される <see cref="OverloadResolver"/> を指定します。</param>
		/// <param name="args">制約された引数を指定します。</param>
		/// <param name="hasBeenUsed">呼び出しが完了すると使用された引数に対応する位置に <c>true</c> が格納されます。</param>
		/// <returns>実引数から引数に渡される値を提供するデリゲート。引数がスキップされた場合 (つまり、呼び出し先に渡されない場合) <c>null</c> を返します。</returns>
		protected internal override Func<object[], object> ToDelegate(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed)
		{
			var indexes = GetParameters(hasBeenUsed).ToArray();
			var func = GetCreationDelegate(ParameterInfo.ParameterType);
			return actualArgs => func(_names, indexes.Select(i => actualArgs[i + 1]).ToArray());
		}

		Func<string[], object[], object> GetCreationDelegate(Type dictType)
		{
			Func<string[], object[], object> func = null;
			if (dictType == typeof(IDictionary))
				func = BinderOps.MakeDictionary<object, object>;
			else if (dictType == typeof(IAttributesCollection))
				func = BinderOps.MakeSymbolDictionary;
			else if (dictType.IsGenericType)
			{
				Type[] genArgs = dictType.GetGenericArguments();
				if ((dictType.GetGenericTypeDefinition() == typeof(IDictionary<,>) || dictType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) && (genArgs[0] == typeof(string) || genArgs[0] == typeof(object)))
				{
					var method = new Func<string[], object[], IDictionary<string, object>>(BinderOps.MakeDictionary<string, object>).Method.GetGenericMethodDefinition();
					func = (Func<string[], object[], object>)method.MakeGenericMethod(genArgs).CreateDelegate(typeof(Func<string[], object[], object>));
				}
			}
			if (func == null)
				throw new InvalidOperationException(string.Format("サポートされていない辞書引数型: {0}", dictType.FullName));
			return func;
		}
	}
}
