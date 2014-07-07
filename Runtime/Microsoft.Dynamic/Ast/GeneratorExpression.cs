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
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Ast
{
	/// <summary>
	/// <see cref="IEnumerable"/>、<see cref="IEnumerable&lt;T&gt;"/>、<see cref="IEnumerator"/>、または <see cref="IEnumerator&lt;T&gt;"/> 型のパラメータのないジェネレータを表します。
	/// その本体は一連の <see cref="YieldExpression"/> を格納することができます。
	/// 列挙子上でのそれぞれの MoveNext への呼び出しでジェネレータに入り、YieldReturn または YieldBreak に当たるまで式を実行します。
	/// </summary>
	public sealed class GeneratorExpression : Expression
	{
		Expression _reduced;
		readonly Type _type;

		/// <summary>指定された名前、型、ラベル、本体を使用して、<see cref="Microsoft.Scripting.Ast.GeneratorExpression"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="name">このジェネレータの名前を指定します。</param>
		/// <param name="type">このジェネレータの型を指定します。</param>
		/// <param name="label">このジェネレータから処理を譲るラベルを指定します。</param>
		/// <param name="body">このジェネレータの本体を指定します。</param>
		internal GeneratorExpression(string name, Type type, LabelTarget label, Expression body)
		{
			Target = label;
			Body = body;
			_type = type;
			Name = name;
		}

		/// <summary>ノードをより単純なノードに変形できることを示します。</summary>
		public override bool CanReduce { get { return true; } }

		/// <summary>この <see cref="System.Linq.Expressions.Expression"/> が表す式の静的な型を取得します。</summary>
		public sealed override Type Type { get { return _type; } }

		/// <summary>この <see cref="System.Linq.Expressions.Expression"/> のノード型を取得します。</summary>
		public sealed override ExpressionType NodeType { get { return ExpressionType.Extension; } }

		/// <summary>このジェネレータの名前を取得します。</summary>
		public string Name { get; private set; }

		/// <summary>YieldBreak または YieldReturn 式によって使用されるこのジェネレータから処理を譲るラベルを取得します。</summary>
		public LabelTarget Target { get; private set; }

		/// <summary>このジェネレータの本体を取得します。本体には YieldBreak または YieldReturn 式を含めることができます。</summary>
		public Expression Body { get; private set; }

		/// <summary>このノードをより単純な式に変形します。</summary>
		/// <returns>単純化された式。</returns>
		public override Expression Reduce() { return _reduced ?? (_reduced = new GeneratorRewriter(this).Reduce()); }

		/// <summary>ノードを単純化し、単純化された式の <paramref name="visitor"/> デリゲートを呼び出します。</summary>
		/// <param name="visitor"><see cref="System.Func&lt;T,TResult&gt;"/> のインスタンス。</param>
		/// <returns>走査中の式、またはツリー内で走査中の式と置き換える式</returns>
		protected override Expression VisitChildren(ExpressionVisitor visitor)
		{
			var b = visitor.Visit(Body);
			if (b == Body)
				return this;
			return Utils.Generator(Name, Target, b, Type);
		}

		/// <summary>このジェネレータの型が <see cref="IEnumerable"/> または <see cref="IEnumerable&lt;T&gt;"/> と等しいかどうかを示す値を取得します。</summary>
		internal bool IsEnumerable { get { return Utils.IsEnumerableType(Type); } }
	}

	public partial class Utils
	{
		/// <summary>指定されたラベルと本体を使用して、<see cref="IEnumerable&lt;T&gt;"/> 型のジェネレータを作成します。T は <paramref name="label"/> の型と等しくなります。</summary>
		/// <param name="label">ジェネレータから処理を譲るラベルを指定します。</param>
		/// <param name="body">ジェネレータの本体を指定します。</param>
		/// <returns>新しく作成されたジェネレータ。</returns>
		public static GeneratorExpression Generator(LabelTarget label, Expression body)
		{
			ContractUtils.RequiresNotNull(label, "label");
			ContractUtils.RequiresNotNull(body, "body");
			ContractUtils.Requires(label.Type != typeof(void), "label", "label must have a non-void type");
			return new GeneratorExpression("generator", typeof(IEnumerable<>).MakeGenericType(label.Type), label, body);
		}

		/// <summary>指定されたラベルと本体を使用して、<paramref name="type"/> 型のジェネレータを作成します。T は <paramref name="label"/> の型と等しくなります。</summary>
		/// <param name="label">ジェネレータから処理を譲るラベルを指定します。</param>
		/// <param name="body">ジェネレータの本体を指定します。</param>
		/// <param name="type">ジェネレータの型を指定します。ジェネレータの型は <see cref="IEnumerable"/>、<see cref="IEnumerable&lt;T&gt;"/>、<see cref="IEnumerator"/>、または <see cref="IEnumerator&lt;T&gt;"/> のいずれかである必要があります。</param>
		/// <returns>新しく作成されたジェネレータ。</returns>
		public static GeneratorExpression Generator(LabelTarget label, Expression body, Type type) { return Generator("generator", label, body, type); }

		/// <summary>指定された名前、ラベルと本体を使用して、<paramref name="type"/> 型のジェネレータを作成します。T は <paramref name="label"/> の型と等しくなります。</summary>
		/// <param name="name">ジェネレータの名前を指定します。</param>
		/// <param name="label">ジェネレータから処理を譲るラベルを指定します。</param>
		/// <param name="body">ジェネレータの本体を指定します。</param>
		/// <param name="type">ジェネレータの型を指定します。ジェネレータの型は <see cref="IEnumerable"/>、<see cref="IEnumerable&lt;T&gt;"/>、<see cref="IEnumerator"/>、または <see cref="IEnumerator&lt;T&gt;"/> のいずれかである必要があります。</param>
		/// <returns>新しく作成されたジェネレータ。</returns>
		public static GeneratorExpression Generator(string name, LabelTarget label, Expression body, Type type)
		{
			ContractUtils.RequiresNotNull(type, "type");
			ContractUtils.RequiresNotNull(body, "body");
			ContractUtils.RequiresNotNull(label, "label");
			ContractUtils.Requires(label.Type != typeof(void), "label", "label must have a non-void type");
			ContractUtils.Requires(body.Type == typeof(void), "body", "generator body must have a void type");
			// Generator type must be one of: IEnumerable, IEnumerator,
			// IEnumerable<T>, or IEnumerator<T>, where T is label.Ttpe
			if (type.IsGenericType)
			{
				var genType = type.GetGenericTypeDefinition();
				if (genType != typeof(IEnumerable<>) && genType != typeof(IEnumerator<>) || type.GetGenericArguments()[0] != label.Type)
					throw GeneratorTypeMustBeEnumerableOfT(label.Type);
			}
			else if (type != typeof(IEnumerable) && type != typeof(IEnumerator))
				throw GeneratorTypeMustBeEnumerableOfT(label.Type);
			return new GeneratorExpression(name, type, label, body);
		}

		static ArgumentException GeneratorTypeMustBeEnumerableOfT(Type type) { return new ArgumentException(string.Format("Generator must be of type IEnumerable<T>, IEnumerator<T>, IEnumerable, or IEnumerator, where T is '{0}'", type)); }

		/// <summary>指定された型が <see cref="IEnumerable"/> か <see cref="IEnumerable&lt;T&gt;"/> であるかどうかを調べます。</summary>
		/// <param name="type">調べる型を指定します。</param>
		/// <returns>型が列挙型であれば <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		internal static bool IsEnumerableType(Type type) { return type == typeof(IEnumerable) || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>); }

		#region Generator lambda factories

		/// <summary>
		/// パラメータのないジェネレータを含むラムダ式を作成します。
		/// IEnumerator を返す場合とても単純となり、定数時間の構築になります。
		/// しかしながら、結果が IEnumerable である場合、それぞれの GetEnumerator() への呼び出しがパラメータと同じ値の IEnumerator を返すことを確認するために、ツリー全体の探索を必要とします。
		/// </summary>
		/// <param name="label">内部のジェネレータから処理を譲るラベルを指定します。</param>
		/// <param name="body">内部のジェネレータの本体を指定します。</param>
		/// <param name="parameters">ラムダ式のパラメータを指定します。</param>
		/// <returns>パラメータのないジェネレータを含むラムダ式。</returns>
		public static Expression<T> GeneratorLambda<T>(LabelTarget label, Expression body, params ParameterExpression[] parameters) { return (Expression<T>)GeneratorLambda(typeof(T), label, body, null, parameters); }

		/// <summary>
		/// パラメータのないジェネレータを含むラムダ式を作成します。
		/// IEnumerator を返す場合とても単純となり、定数時間の構築になります。
		/// しかしながら、結果が IEnumerable である場合、それぞれの GetEnumerator() への呼び出しがパラメータと同じ値の IEnumerator を返すことを確認するために、ツリー全体の探索を必要とします。
		/// </summary>
		/// <param name="label">内部のジェネレータから処理を譲るラベルを指定します。</param>
		/// <param name="body">内部のジェネレータの本体を指定します。</param>
		/// <param name="name">内部のジェネレータの名前を指定します。</param>
		/// <param name="parameters">ラムダ式のパラメータを指定します。</param>
		/// <returns>パラメータのないジェネレータを含むラムダ式。</returns>
		public static Expression<T> GeneratorLambda<T>(LabelTarget label, Expression body, string name, params ParameterExpression[] parameters) { return (Expression<T>)GeneratorLambda(typeof(T), label, body, name, parameters); }

		/// <summary>
		/// パラメータのないジェネレータを含むラムダ式を作成します。
		/// IEnumerator を返す場合とても単純となり、定数時間の構築になります。
		/// しかしながら、結果が IEnumerable である場合、それぞれの GetEnumerator() への呼び出しがパラメータと同じ値の IEnumerator を返すことを確認するために、ツリー全体の探索を必要とします。
		/// </summary>
		/// <param name="label">内部のジェネレータから処理を譲るラベルを指定します。</param>
		/// <param name="body">内部のジェネレータの本体を指定します。</param>
		/// <param name="name">内部のジェネレータの名前を指定します。</param>
		/// <param name="parameters">ラムダ式のパラメータを指定します。</param>
		/// <returns>パラメータのないジェネレータを含むラムダ式。</returns>
		public static Expression<T> GeneratorLambda<T>(LabelTarget label, Expression body, string name, IEnumerable<ParameterExpression> parameters) { return (Expression<T>)GeneratorLambda(typeof(T), label, body, name, parameters); }

		/// <summary>
		/// パラメータのないジェネレータを含むラムダ式を作成します。
		/// IEnumerator を返す場合とても単純となり、定数時間の構築になります。
		/// しかしながら、結果が IEnumerable である場合、それぞれの GetEnumerator() への呼び出しがパラメータと同じ値の IEnumerator を返すことを確認するために、ツリー全体の探索を必要とします。
		/// </summary>
		/// <param name="delegateType">返されるラムダ式の型を指定します。</param>
		/// <param name="label">内部のジェネレータから処理を譲るラベルを指定します。</param>
		/// <param name="body">内部のジェネレータの本体を指定します。</param>
		/// <param name="parameters">ラムダ式のパラメータを指定します。</param>
		/// <returns>パラメータのないジェネレータを含むラムダ式。</returns>
		public static LambdaExpression GeneratorLambda(Type delegateType, LabelTarget label, Expression body, params ParameterExpression[] parameters) { return GeneratorLambda(delegateType, label, body, null, parameters); }

		/// <summary>
		/// パラメータのないジェネレータを含むラムダ式を作成します。
		/// IEnumerator を返す場合とても単純となり、定数時間の構築になります。
		/// しかしながら、結果が IEnumerable である場合、それぞれの GetEnumerator() への呼び出しがパラメータと同じ値の IEnumerator を返すことを確認するために、ツリー全体の探索を必要とします。
		/// </summary>
		/// <param name="delegateType">返されるラムダ式の型を指定します。</param>
		/// <param name="label">内部のジェネレータから処理を譲るラベルを指定します。</param>
		/// <param name="body">内部のジェネレータの本体を指定します。</param>
		/// <param name="name">内部のジェネレータの名前を指定します。</param>
		/// <param name="parameters">ラムダ式のパラメータを指定します。</param>
		/// <returns>パラメータのないジェネレータを含むラムダ式。</returns>
		public static LambdaExpression GeneratorLambda(Type delegateType, LabelTarget label, Expression body, string name, params ParameterExpression[] parameters) { return GeneratorLambda(delegateType, label, body, name, (IEnumerable<ParameterExpression>)parameters); }

		/// <summary>
		/// パラメータのないジェネレータを含むラムダ式を作成します。
		/// IEnumerator を返す場合とても単純となり、定数時間の構築になります。
		/// しかしながら、結果が IEnumerable である場合、それぞれの GetEnumerator() への呼び出しがパラメータと同じ値の IEnumerator を返すことを確認するために、ツリー全体の探索を必要とします。
		/// </summary>
		/// <param name="delegateType">返されるラムダ式の型を指定します。</param>
		/// <param name="label">内部のジェネレータから処理を譲るラベルを指定します。</param>
		/// <param name="body">内部のジェネレータの本体を指定します。</param>
		/// <param name="name">内部のジェネレータの名前を指定します。</param>
		/// <param name="parameters">ラムダ式のパラメータを指定します。</param>
		/// <returns>パラメータのないジェネレータを含むラムダ式。</returns>
		public static LambdaExpression GeneratorLambda(Type delegateType, LabelTarget label, Expression body, string name, IEnumerable<ParameterExpression> parameters)
		{
			ContractUtils.RequiresNotNull(delegateType, "delegateType");
			ContractUtils.Requires(typeof(Delegate).IsAssignableFrom(delegateType) && !delegateType.IsAbstract, "Lambda type parameter must be derived from System.Delegate");
			var generatorType = delegateType.GetMethod("Invoke").GetReturnType();
			if (IsEnumerableType(generatorType))
				body = TransformEnumerable(body, parameters); // rewrite body
			return Expression.Lambda(delegateType, Generator(name, label, body, generatorType), name, parameters);
		}

		// Creates a GeneratorLambda as a lambda containing a parameterless
		// generator. Because we want parameters to be captured by value and
		// not as variables, we have to do a transformation more like this:
		//    static IEnumerable<int> Foo(int count) {
		//        count *= 2;
		//        for (int i = 0; i < count; i++) {
		//            yield return i;
		//        }
		//    }
		//
		// Becomes:
		//
		//    static IEnumerable<int> Foo(int count) {
		//        return generator {
		//            int __count = count;
		//            __count *= 2;
		//            for (int i = 0; i < __count; i++) {
		//                yield return i;
		//            }
		//        }
		//    }
		//
		// This involves a full rewrite, unfortunately.
		static Expression TransformEnumerable(Expression body, IEnumerable<ParameterExpression> parameters)
		{
			var paramList = parameters.ToArray();
			if (paramList.Length == 0)
				return body;
			var vars = new ParameterExpression[paramList.Length];
			var map = new Dictionary<ParameterExpression, ParameterExpression>(paramList.Length);
			var block = new Expression[paramList.Length + 1];
			for (int i = 0; i < paramList.Length; i++)
			{
				map.Add(paramList[i], vars[i] = Expression.Variable(paramList[i].Type, paramList[i].Name));
				block[i] = Expression.Assign(vars[i], paramList[i]);
			}
			block[paramList.Length] = new LambdaParameterRewriter(map).Visit(body);
			return Expression.Block(vars, block);
		}
		#endregion
	}
}
