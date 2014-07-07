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
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Contracts;
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions
{
	using Ast = Expression;

	/// <summary>単一のメソッドを表します。</summary>
	public class MethodTracker : MemberTracker
	{
		/// <summary>基になる <see cref="MethodInfo"/> を使用して、<see cref="Microsoft.Scripting.Actions.MethodTracker"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="method">基になる <see cref="MethodInfo"/> を指定します。</param>
		internal MethodTracker(MethodInfo method)
		{
			ContractUtils.RequiresNotNull(method, "method");
			Method = method;
			IsStatic = method.IsStatic;
		}

		/// <summary>基になる <see cref="MethodInfo"/> および静的メソッドかどうかを示す値を使用して、<see cref="Microsoft.Scripting.Actions.MethodTracker"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="method">基になる <see cref="MethodInfo"/> を指定します。</param>
		/// <param name="isStatic">このメソッドが静的メソッドであるかどうかを示す値を指定します。</param>
		internal MethodTracker(MethodInfo method, bool isStatic)
		{
			ContractUtils.RequiresNotNull(method, "method");
			Method = method;
			IsStatic = isStatic;
		}

		/// <summary>メンバを論理的に宣言する型を取得します。</summary>
		public override Type DeclaringType { get { return Method.DeclaringType; } }

		/// <summary><see cref="MemberTracker"/> の種類を取得します。</summary>
		public override sealed TrackerTypes MemberType { get { return TrackerTypes.Method; } }

		/// <summary>メンバの名前を取得します。</summary>
		public override string Name { get { return Method.Name; } }

		/// <summary>基になる <see cref="MethodInfo"/> を取得します。</summary>
		public MethodInfo Method { get; private set; }

		/// <summary>このメソッドがパブリック メソッドであるかどうかを示す値を取得します。</summary>
		public bool IsPublic { get { return Method.IsPublic; } }

		/// <summary>このメソッドが静的メソッドであるかどうかを示す値を取得します。</summary>
		public bool IsStatic { get; private set; }

		/// <summary>このオブジェクトの文字列表現を取得します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		[Confined]
		public override string ToString() { return Method.ToString(); }

		/// <summary>
		/// バインディングが可能な場合、新しいメンバトラッカーを返す指定されたインスタンスにメンバトラッカーを関連付けます。
		/// バインディングが不可能な場合、既存のメンバトラッカーが返されます。
		/// 例えば、静的フィールドへのバインディングは、元のメンバトラッカーを返します。
		/// インスタンスフィールドへのバインディングは、インスタンスを渡す GetBoundValue または SetBoundValue を得る新しい <see cref="BoundMemberTracker"/> を返します。
		/// </summary>
		/// <param name="instance">メンバトラッカーを関連付けるインスタンスを指定します。</param>
		/// <returns>指定されたインスタンスに関連付けられたメンバトラッカー。</returns>
		public override MemberTracker BindToInstance(DynamicMetaObject instance) { return IsStatic ? (MemberTracker)this : new BoundMemberTracker(this, instance); }

		/// <summary>
		/// インスタンスに束縛されている値を取得する <see cref="Expression"/> を取得します。
		/// カスタムメンバトラッカーはこのメソッドをオーバーライドして、インスタンスへのバインド時の独自の動作を提供できます。
		/// </summary>
		/// <param name="resolverFactory">オーバーロード解決の方法を表す <see cref="OverloadResolverFactory"/> を指定します。</param>
		/// <param name="binder">言語のバインディングセマンティクスを指定します。</param>
		/// <param name="type">この <see cref="MemberTracker"/> がアクセスされた型を指定します。</param>
		/// <param name="instance">束縛されたインスタンスを指定します。</param>
		/// <returns>インスタンスに束縛されている値を取得する <see cref="Expression"/>。</returns>
		protected internal override DynamicMetaObject GetBoundValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type, DynamicMetaObject instance) { return binder.ReturnMemberTracker(type, BindToInstance(instance)); }

		/// <summary>
		/// 指定された引数を使用してオブジェクトの呼び出しを実行する <see cref="Expression"/> を取得します。
		/// 呼び出し元は GetErrorForDoCall を呼び出して、正確なエラーを表す <see cref="Expression"/> または既定のエラーを表す <c>null</c> を取得できます。
		/// </summary>
		/// <param name="resolverFactory">オーバーロード解決の方法を表す <see cref="OverloadResolverFactory"/> を指定します。</param>
		/// <param name="binder">言語のバインディングセマンティクスを指定します。</param>
		/// <param name="arguments">オブジェクト呼び出しの引数を指定します。</param>
		/// <returns>オブジェクト呼び出しを実行する <see cref="Expression"/>。エラーが発生した場合は <c>null</c> が返されます。</returns>
		internal override DynamicMetaObject Call(OverloadResolverFactory resolverFactory, ActionBinder binder, params DynamicMetaObject[] arguments)
		{
			if (Method.IsPublic && Method.DeclaringType.IsVisible)
				return binder.MakeCallExpression(resolverFactory, Method, arguments);
			//methodInfo.Invoke(obj, object[] params)
			if (Method.IsStatic)
				return new DynamicMetaObject(
					Ast.Convert(
						Ast.Call(
							AstUtils.Constant(Method),
							typeof(MethodInfo).GetMethod("Invoke", new Type[] { typeof(object), typeof(object[]) }),
							AstUtils.Constant(null),
							AstUtils.NewArrayHelper(typeof(object), Array.ConvertAll(arguments, x => x.Expression))
						),
						Method.ReturnType
					),
					BindingRestrictions.Empty
				);
			if (arguments.Length == 0)
				throw Error.NoInstanceForCall();
			return new DynamicMetaObject(
				Ast.Convert(
					Ast.Call(
						AstUtils.Constant(Method),
						typeof(MethodInfo).GetMethod("Invoke", new Type[] { typeof(object), typeof(object[]) }),
						arguments[0].Expression,
						AstUtils.NewArrayHelper(typeof(object), Array.ConvertAll(ArrayUtils.RemoveFirst(arguments), x => x.Expression))
					),
					Method.ReturnType
				),
				BindingRestrictions.Empty
			);
		}
	}
}
