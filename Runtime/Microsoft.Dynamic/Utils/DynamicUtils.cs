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
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Interpreter;

namespace Microsoft.Scripting.Utils
{
	using AstUtils = Microsoft.Scripting.Ast.Utils;

	/// <summary>動的操作と <see cref="DynamicMetaObject"/> に関するユーティリティ メソッドを提供します。</summary>
	public static class DynamicUtils
	{
		/// <summary>ランタイム値およびこの <see cref="DynamicMetaObject"/> をバインディングプロセス中に表す式に対する <see cref="DynamicMetaObject"/> のインスタンスを作成します。</summary>
		/// <param name="argValue"><see cref="DynamicMetaObject"/> によって表されるランタイム値を指定します。</param>
		/// <param name="parameterExpression">この <see cref="DynamicMetaObject"/> をバインディングプロセス中に表す式を指定します。</param>
		/// <returns><see cref="DynamicMetaObject"/> の新しいインスタンス。</returns>
		public static DynamicMetaObject ObjectToMetaObject(object argValue, Expression parameterExpression)
		{
			var ido = argValue as IDynamicMetaObjectProvider;
			return ido != null ? ido.GetMetaObject(parameterExpression) : new DynamicMetaObject(parameterExpression, BindingRestrictions.Empty, argValue);
		}

		/// <summary>引数に対するバインディングを行い <see cref="CallSite&lt;T&gt;"/> のターゲットを更新します。</summary>
		/// <typeparam name="TDelegate"><see cref="CallSite&lt;T&gt;"/> のターゲットの型を指定します。</typeparam>
		/// <param name="binder">動的操作を実際にバインドする <see cref="DynamicMetaObjectBinder"/> を指定します。</param>
		/// <param name="site">操作のバインド対象である <see cref="CallSite&lt;T&gt;"/> を指定します。</param>
		/// <param name="args">動的操作の引数の配列を指定します。</param>
		/// <param name="compilationThreshold">インタプリタがコンパイルを開始するまでの繰り返し数を指定します。</param>
		/// <returns><see cref="CallSite&lt;T&gt;"/> のターゲットを置き換える新しいデリゲート。</returns>
		public static TDelegate LightBind<TDelegate>(this DynamicMetaObjectBinder binder, CallSite<TDelegate> site, object[] args, int compilationThreshold) where TDelegate : class
		{
			var d = Bind<TDelegate>(binder, args).LightCompile(compilationThreshold);
			var lambda = ((Delegate)(object)d).Target as LightLambda;
			if (lambda != null)
				lambda.Compile += (_, e) =>
				{
					// site.Target 内でまだ規則が使用されている場合コンパイルされたデリゲートで置き換えます。
					// site.Target はコンパイルされるデリゲートを書き込む前に別のスレッドによって更新されることができます。
					// そのような場合、コンパイルされた規則を実行して、適用できないことを検知してからルールキャッシュによって置き換えます。
					// TODO: 解釈されるデリゲートは L1 および L2 キャッシュも置き換える?
					if (site.Target == d)
						site.Target = (TDelegate)(object)e.Compiled;
				};
			else
				PerfTrack.NoteEvent(PerfTrack.Category.Rules, "Rule not interpreted");
			return d;
		}

		/// <summary>指定された引数に対して動的操作のバインディングを実行します。</summary>
		/// <typeparam name="TDelegate">バインディングで生成されるデリゲートの型を指定します。</typeparam>
		/// <param name="binder">バインディングを実行する <see cref="DynamicMetaObjectBinder"/> を指定します。</param>
		/// <param name="args">動的操作の引数の配列を指定します。</param>
		/// <returns>バインディングの結果生成されたデリゲート。</returns>
		public static Expression<TDelegate>/*!*/ Bind<TDelegate>(this DynamicMetaObjectBinder binder, object[] args) where TDelegate : class
		{
			var returnLabel = LambdaSignature<TDelegate>.Instance.ReturnLabel.Type == typeof(object) && binder.ReturnType != typeof(void) && binder.ReturnType != typeof(object) ? Expression.Label(binder.ReturnType) : LambdaSignature<TDelegate>.Instance.ReturnLabel;
			var binding = binder.Bind(args, LambdaSignature<TDelegate>.Instance.Parameters, returnLabel);
			if (binding == null)
				throw new InvalidOperationException("CallSiteBinder.Bind は null でない式を返す必要があります。");
			return Stitch<TDelegate>(binding, returnLabel);
		}

		// TODO: This should be merged into CallSiteBinder.
		static Expression<TDelegate>/*!*/ Stitch<TDelegate>(Expression binding, LabelTarget returnLabel) where TDelegate : class
		{
			var updLabel = Expression.Label(CallSiteBinder.UpdateLabel);
			var site = Expression.Parameter(typeof(CallSite), "$site");
			var @params = ArrayUtils.Insert(site, LambdaSignature<TDelegate>.Instance.Parameters);
			Expression body;
			if (returnLabel != LambdaSignature<TDelegate>.Instance.ReturnLabel)
			{
				// TODO:
				// This allows the binder to produce a strongly typed binding expression that gets boxed if the call site's return value is of type object. 
				// The current implementation of CallSiteBinder is too strict as it requires the two types to be reference-assignable.
				var tmp = Expression.Parameter(typeof(object));
				body = Expression.Convert(
					Expression.Block(new[] { tmp },
						binding,
						updLabel,
						Expression.Label(returnLabel,
							Expression.Condition(Expression.NotEqual(Expression.Assign(tmp, Expression.Invoke(Expression.Property(Expression.Convert(site, typeof(CallSite<TDelegate>)), "Update"), @params)), AstUtils.Constant(null)),
								Expression.Convert(tmp, returnLabel.Type),
								Expression.Default(returnLabel.Type)
							)
						)
					), typeof(object)
				);
			}
			else
				body = Expression.Block(
					binding,
					updLabel,
					Expression.Label(returnLabel, Expression.Invoke(Expression.Property(Expression.Convert(site, typeof(CallSite<TDelegate>)), "Update"), @params))
				);
			return Expression.Lambda<TDelegate>(body, "CallSite.Target", true, @params);
		}

		// TODO: This should be merged into CallSiteBinder.
		sealed class LambdaSignature<TDelegate> where TDelegate : class
		{
			internal static readonly LambdaSignature<TDelegate> Instance = new LambdaSignature<TDelegate>();
			internal readonly ReadOnlyCollection<ParameterExpression> Parameters;
			internal readonly LabelTarget ReturnLabel;

			LambdaSignature()
			{
				if (!typeof(Delegate).IsAssignableFrom(typeof(TDelegate)))
					throw new InvalidOperationException();
				var invoke = typeof(TDelegate).GetMethod("Invoke");
				var pis = invoke.GetParameters();
				if (pis[0].ParameterType != typeof(CallSite))
					throw new InvalidOperationException();
				Parameters = new ReadOnlyCollection<ParameterExpression>(pis.Skip(1).Select((x, i) => Expression.Parameter(x.ParameterType, "$arg" + i)).ToArray());
				ReturnLabel = Expression.Label(invoke.ReturnType);
			}
		}
	}
}
