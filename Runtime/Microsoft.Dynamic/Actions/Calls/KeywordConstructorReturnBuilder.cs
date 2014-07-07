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
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Scripting.Runtime;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions.Calls
{
	using Ast = Expression;

	/// <summary>返戻値のフィールドまたはプロパティを使用されないキーワード引数を使用して更新します。</summary>
	sealed class KeywordConstructorReturnBuilder : ReturnBuilder
	{
		ReturnBuilder _builder;
		int _kwArgCount;
		int[] _indexesUsed;
		MemberInfo[] _membersSet;
		bool _privateBinding;

		/// <summary>基になる <see cref="ReturnBuilder"/>、キーワード引数の数、位置、設定するメンバ、CLR 可視性チェックを行うかどうかを使用して、<see cref="Microsoft.Scripting.Actions.Calls.KeywordConstructorReturnBuilder"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="builder">基になる <see cref="ReturnBuilder"/> を指定します。</param>
		/// <param name="kwArgCount">キーワード引数の数を指定します。</param>
		/// <param name="indexesUsed">使用するキーワード引数の位置を示す 0 から始まるインデックスの配列を指定します。</param>
		/// <param name="membersSet">設定するメンバを表す <see cref="MemberInfo"/> を指定します。</param>
		/// <param name="privateBinding">CLR 可視性チェックを無視するかどうかを示す値を指定します。</param>
		public KeywordConstructorReturnBuilder(ReturnBuilder builder, int kwArgCount, int[] indexesUsed, MemberInfo[] membersSet, bool privateBinding) : base(builder.ReturnType)
		{
			_builder = builder;
			_kwArgCount = kwArgCount;
			_indexesUsed = indexesUsed;
			_membersSet = membersSet;
			_privateBinding = privateBinding;
		}

		/// <summary>メソッド呼び出しの結果を返す <see cref="Expression"/> を返します。</summary>
		/// <param name="resolver">メソッドに対するオーバーロードを解決するために使用される <see cref="OverloadResolver"/> を指定します。</param>
		/// <param name="builders">メソッドに渡されたそれぞれの実引数に対する <see cref="ArgBuilder"/> のリストを指定します。</param>
		/// <param name="args">制約された引数を指定します。</param>
		/// <param name="ret">メソッド呼び出しの現在の結果を表す <see cref="Expression"/> を指定します。</param>
		/// <returns>メソッド呼び出しの結果を表す <see cref="Expression"/>。</returns>
		internal override Expression ToExpression(OverloadResolver resolver, IList<ArgBuilder> builders, RestrictedArguments args, Expression ret)
		{
			List<Expression> sets = new List<Expression>();
			ParameterExpression tmp = resolver.GetTemporary(ret.Type, "val");
			sets.Add(Ast.Assign(tmp, ret));
			for (int i = 0; i < _indexesUsed.Length; i++)
			{
				Expression value = args.Objects[args.Length - _kwArgCount + _indexesUsed[i]].Expression;
				switch (_membersSet[i].MemberType)
				{
					case MemberTypes.Field:
						FieldInfo fi = (FieldInfo)_membersSet[i];
						if (!fi.IsLiteral && !fi.IsInitOnly)
							sets.Add(Ast.Assign(Ast.Field(tmp, fi), ConvertToHelper(resolver, value, fi.FieldType)));
						else
							// call a helper which throws the error but "returns object"
							sets.Add(
								Ast.Convert(
									Ast.Call(
										new Func<bool, string, object>(ScriptingRuntimeHelpers.ReadOnlyAssignError).Method,
										AstUtils.Constant(true),
										AstUtils.Constant(fi.Name)
									),
									fi.FieldType
								)
							);
						break;
					case MemberTypes.Property:
						PropertyInfo pi = (PropertyInfo)_membersSet[i];
						if (pi.GetSetMethod(_privateBinding) != null)
							sets.Add(Ast.Assign(Ast.Property(tmp, pi), ConvertToHelper(resolver, value, pi.PropertyType)));
						else
							// call a helper which throws the error but "returns object"
							sets.Add(
								Ast.Convert(
									Ast.Call(
										new Func<bool, string, object>(ScriptingRuntimeHelpers.ReadOnlyAssignError).Method,
										AstUtils.Constant(false),
										AstUtils.Constant(pi.Name)
									),
									pi.PropertyType
								)
							);
						break;
				}
			}
			sets.Add(tmp);
			return _builder.ToExpression(resolver, builders, args, Ast.Block(sets.ToArray()));
		}

		// TODO: revisit
		static Expression ConvertToHelper(OverloadResolver resolver, Expression value, Type type)
		{
			if (type == value.Type)
				return value;
			if (type.IsAssignableFrom(value.Type))
				return AstUtils.Convert(value, type);
			return resolver.GetDynamicConversion(value, type);
		}
	}
}
