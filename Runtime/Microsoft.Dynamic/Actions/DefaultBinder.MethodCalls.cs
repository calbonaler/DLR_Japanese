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
using System.Diagnostics;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions
{
	using Ast = Expression;

	public partial class DefaultBinder : ActionBinder
	{
		/// <summary>
		/// 指定された引数を使用して、オーバーロードされたメソッド セットに対するバインディングを実行します。
		/// 引数は <see cref="CallSignature"/> オブジェクトによって指定された通りに消費されます。
		/// </summary>
		/// <param name="resolver">オーバーロードの解決とメソッドバインディングに使用される <see cref="DefaultOverloadResolver"/> を指定します。</param>
		/// <param name="targets">呼び出されるメソッド セットを指定します。</param>
		/// <returns>呼び出しの結果を表す <see cref="DynamicMetaObject"/>。</returns>
		public DynamicMetaObject CallMethod(DefaultOverloadResolver resolver, IList<MethodBase> targets) { return CallMethod(resolver, targets, BindingRestrictions.Empty, null); }

		/// <summary>
		/// 指定された引数を使用して、オーバーロードされたメソッド セットに対するバインディングを実行します。
		/// 引数は <see cref="CallSignature"/> オブジェクトによって指定された通りに消費されます。
		/// </summary>
		/// <param name="resolver">オーバーロードの解決とメソッドバインディングに使用される <see cref="DefaultOverloadResolver"/> を指定します。</param>
		/// <param name="targets">呼び出されるメソッド セットを指定します。</param>
		/// <param name="name">ターゲットからの名前に使用するメソッドの名前または <c>null</c> を指定します。</param>
		/// <returns>呼び出しの結果を表す <see cref="DynamicMetaObject"/>。</returns>
		public DynamicMetaObject CallMethod(DefaultOverloadResolver resolver, IList<MethodBase> targets, string name) { return CallMethod(resolver, targets, BindingRestrictions.Empty, name); }

		/// <summary>
		/// 指定された引数を使用して、オーバーロードされたメソッド セットに対するバインディングを実行します。
		/// 引数は <see cref="CallSignature"/> オブジェクトによって指定された通りに消費されます。
		/// </summary>
		/// <param name="resolver">オーバーロードの解決とメソッドバインディングに使用される <see cref="DefaultOverloadResolver"/> を指定します。</param>
		/// <param name="targets">呼び出されるメソッド セットを指定します。</param>
		/// <param name="restrictions">生成される <see cref="DynamicMetaObject"/> に対して適用される追加のバインディング制約を指定します。</param>
		/// <returns>呼び出しの結果を表す <see cref="DynamicMetaObject"/>。</returns>
		public DynamicMetaObject CallMethod(DefaultOverloadResolver resolver, IList<MethodBase> targets, BindingRestrictions restrictions) { return CallMethod(resolver, targets, restrictions, null); }

		/// <summary>
		/// 指定された引数を使用して、オーバーロードされたメソッド セットに対するバインディングを実行します。
		/// 引数は <see cref="CallSignature"/> オブジェクトによって指定された通りに消費されます。
		/// </summary>
		/// <param name="resolver">オーバーロードの解決とメソッドバインディングに使用される <see cref="DefaultOverloadResolver"/> を指定します。</param>
		/// <param name="targets">呼び出されるメソッド セットを指定します。</param>
		/// <param name="restrictions">生成される <see cref="DynamicMetaObject"/> に対して適用される追加のバインディング制約を指定します。</param>
		/// <param name="name">ターゲットからの名前に使用するメソッドの名前または <c>null</c> を指定します。</param>
		/// <returns>呼び出しの結果を表す <see cref="DynamicMetaObject"/>。</returns>
		public DynamicMetaObject CallMethod(DefaultOverloadResolver resolver, IList<MethodBase> targets, BindingRestrictions restrictions, string name)
		{
			BindingTarget target;
			return CallMethod(resolver, targets, restrictions, name, NarrowingLevel.None, NarrowingLevel.All, out target);
		}

		/// <summary>
		/// 指定された引数を使用して、オーバーロードされたメソッド セットに対するバインディングを実行します。
		/// 引数は <see cref="CallSignature"/> オブジェクトによって指定された通りに消費されます。
		/// </summary>
		/// <param name="minLevel">オーバーロードの解決に使用する最小の縮小変換レベルを指定します。</param>
		/// <param name="maxLevel">オーバーロードの解決に使用する最大の縮小変換レベルを指定します。</param>
		/// <param name="resolver">オーバーロードの解決とメソッドバインディングに使用される <see cref="DefaultOverloadResolver"/> を指定します。</param>
		/// <param name="targets">呼び出されるメソッド セットを指定します。</param>
		/// <param name="restrictions">生成される <see cref="DynamicMetaObject"/> に対して適用される追加のバインディング制約を指定します。</param>
		/// <param name="target">エラー情報の生成に使用できる結果として得られるバインディング ターゲットを格納する変数を指定します。</param>
		/// <param name="name">ターゲットからの名前に使用するメソッドの名前または <c>null</c> を指定します。</param>
		/// <returns>呼び出しの結果を表す <see cref="DynamicMetaObject"/>。</returns>
		public DynamicMetaObject CallMethod(DefaultOverloadResolver resolver, IList<MethodBase> targets, BindingRestrictions restrictions, string name, NarrowingLevel minLevel, NarrowingLevel maxLevel, out BindingTarget target)
		{
			ContractUtils.RequiresNotNull(resolver, "resolver");
			ContractUtils.RequiresNotNullItems(targets, "targets");
			ContractUtils.RequiresNotNull(restrictions, "restrictions");
			// attempt to bind to an individual method
			if ((target = resolver.ResolveOverload(name ?? (targets[0].IsConstructor ? targets[0].DeclaringType.Name : targets[0].Name), targets, minLevel, maxLevel)).Success)
				return new DynamicMetaObject(
					target.MakeExpression(),
					restrictions.Merge(
						MakeSplatTests(resolver.CallType, resolver.Signature, false, resolver.Arguments).Merge(target.RestrictedArguments.GetAllRestrictions())
					)
				); // if we succeed make the target for the rule
			// make an error rule
			var restriction = MakeSplatTests(resolver.CallType, resolver.Signature, true, resolver.Arguments);
			// restrict to the exact type of all parameters for errors
			for (int i = 0; i < resolver.Arguments.Count; i++)
				resolver.Arguments[i] = resolver.Arguments[i].Restrict(resolver.Arguments[i].GetLimitType());
			return MakeError(
				resolver.MakeInvalidParametersError(target),
				restrictions.Merge(BindingRestrictions.Combine(resolver.Arguments).Merge(restriction)),
				typeof(object)
			);
		}

		/// <summary>配列引数および辞書引数に対するテストを生成します。</summary>
		static BindingRestrictions MakeSplatTests(CallTypes callType, CallSignature signature, bool testTypes, IList<DynamicMetaObject> args)
		{
			var res = BindingRestrictions.Empty;
			if (signature.HasListArgument())
				res = MakeParamsArrayTest(callType, signature, testTypes, args);
			if (signature.HasDictionaryArgument())
				res = res.Merge(MakeParamsDictionaryTest(args, testTypes));
			return res;
		}

		/// <summary>splat テストを構築する正しい引数を取得します。<see cref="MakeParamsTest"/> が実際のテストを作成します。</summary>
		static BindingRestrictions MakeParamsArrayTest(CallTypes callType, CallSignature signature, bool testTypes, IList<DynamicMetaObject> args)
		{
			int listIndex = signature.IndexOf(ArgumentType.List);
			Debug.Assert(listIndex != -1);
			if (callType == CallTypes.ImplicitInstance)
				listIndex++;
			return MakeParamsTest(args[listIndex], testTypes);
		}

		/// <summary>
		/// 散開実引数のある呼び出しに対する制約を構築します。
		/// 引数がまだオブジェクトのコレクションであり同じ数の引数を持っていることを確認します。
		/// </summary>
		static BindingRestrictions MakeParamsTest(DynamicMetaObject splattee, bool testTypes)
		{
			var list = splattee.Value as IList<object>;
			if (list == null)
			{
				if (splattee.Value == null)
					return BindingRestrictions.GetExpressionRestriction(Ast.Equal(splattee.Expression, AstUtils.Constant(null)));
				else
					return BindingRestrictions.GetTypeRestriction(splattee.Expression, splattee.Value.GetType());
			}
			var res = BindingRestrictions.GetExpressionRestriction(
				Ast.AndAlso(
					Ast.TypeIs(splattee.Expression, typeof(IList<object>)),
					Ast.Equal(
						Ast.Property(
							Ast.Convert(splattee.Expression, typeof(IList<object>)),
							typeof(ICollection<object>).GetProperty("Count")
						),
						AstUtils.Constant(list.Count)
					)
				)
			);
			if (testTypes)
			{
				for (int i = 0; i < list.Count; i++)
				{
					res = res.Merge(
						BindingRestrictionsHelpers.GetRuntimeTypeRestriction(
							Ast.Call(
								AstUtils.Convert(splattee.Expression, typeof(IList<object>)),
								typeof(IList<object>).GetMethod("get_Item"),
								AstUtils.Constant(i)
							),
							CompilerHelpers.GetType(list[i])
						)
					);
				}
			}
			return res;
		}

		/// <summary>
		/// キーワード引数のある呼び出しに対する制約を構築します。
		/// 制約は辞書の個別のキーについてそれらが同じ名前を持っていることを確認するテストを含みます。
		/// </summary>
		static BindingRestrictions MakeParamsDictionaryTest(IList<DynamicMetaObject> args, bool testTypes)
		{
			var dict = (IDictionary)args[args.Count - 1].Value;
			// verify the dictionary has the same count and arguments.
			string[] names = new string[dict.Count];
			Type[] types = testTypes ? new Type[dict.Count] : null;
			int index = 0;
			foreach (DictionaryEntry entry in dict)
			{
				var name = entry.Key as string;
				if (name == null)
					throw ScriptingRuntimeHelpers.SimpleTypeError(string.Format("辞書引数には文字列が予期されましたが {0} が渡されました。", entry.Key));
				names[index] = name;
				if (types != null)
					types[index] = CompilerHelpers.GetType(entry.Value);
				index++;
			}
			return BindingRestrictions.GetExpressionRestriction(
				Ast.AndAlso(
					Ast.TypeIs(args[args.Count - 1].Expression, typeof(IDictionary)),
					Ast.Call(
						new Func<IDictionary, string[], Type[], bool>(BinderOps.CheckDictionaryMembers).Method,
						Ast.Convert(args[args.Count - 1].Expression, typeof(IDictionary)),
						AstUtils.Constant(names),
						testTypes ? AstUtils.Constant(types) : AstUtils.Constant(null, typeof(Type[]))
					)
				)
			);
		}
	}
}
