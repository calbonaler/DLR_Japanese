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

	/// <summary>フィールドを表します。</summary>
	public class FieldTracker : MemberTracker
	{
		/// <summary>基になる <see cref="FieldInfo"/> を使用して、<see cref="Microsoft.Scripting.Actions.FieldTracker"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="field">基になる <see cref="FieldInfo"/> を指定します。</param>
		public FieldTracker(FieldInfo field)
		{
			ContractUtils.RequiresNotNull(field, "field");
			Field = field;
		}

		/// <summary>メンバを論理的に宣言する型を取得します。</summary>
		public override Type DeclaringType { get { return Field.DeclaringType; } }

		/// <summary><see cref="MemberTracker"/> の種類を取得します。</summary>
		public sealed override TrackerTypes MemberType { get { return TrackerTypes.Field; } }

		/// <summary>メンバの名前を取得します。</summary>
		public override string Name { get { return Field.Name; } }

		/// <summary>フィールドがパブリックかどうかを示す値を取得します。</summary>
		public bool IsPublic { get { return Field.IsPublic; } }

		/// <summary>フィールドに対する書き込みが初期化時のみ可能であるかどうかを示す値を取得します。</summary>
		public bool IsInitOnly { get { return Field.IsInitOnly; } }

		/// <summary>値がコンパイル時に書き込まれ、変更できないかどうかを示す値を取得します。</summary>
		public bool IsLiteral { get { return Field.IsLiteral; } }

		/// <summary>このフィールドの型を取得します。</summary>
		public Type FieldType { get { return Field.FieldType; } }

		/// <summary>フィールドが静的かどうかを示す値を取得します。</summary>
		public bool IsStatic { get { return Field.IsStatic; } }

		/// <summary>基になる <see cref="FieldInfo"/> を取得します。</summary>
		public FieldInfo Field { get; private set; }

		/// <summary>このオブジェクトの文字列表現を取得します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		[Confined]
		public override string ToString() { return Field.ToString(); }

		/// <summary>
		/// 値を取得する <see cref="Expression"/> を取得します。
		/// 呼び出し元は GetErrorForGet を呼び出して、正確なエラーを表す <see cref="Expression"/> または既定のエラーを表す <c>null</c> を取得できます。
		/// </summary>
		/// <param name="resolverFactory">オーバーロード解決の方法を表す <see cref="OverloadResolverFactory"/> を指定します。</param>
		/// <param name="binder">言語のバインディングセマンティクスを指定します。</param>
		/// <param name="type">この <see cref="MemberTracker"/> がアクセスされた型を指定します。</param>
		/// <returns>値を取得する <see cref="Expression"/>。エラーが発生した場合は <c>null</c> が返されます。</returns>
		public override DynamicMetaObject GetValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type)
		{
			if (IsLiteral)
				return new DynamicMetaObject(AstUtils.Constant(Field.GetValue(null), typeof(object)), BindingRestrictions.Empty);
			if (!IsStatic)
				return binder.ReturnMemberTracker(type, this); // return the field tracker...
			if (Field.DeclaringType.ContainsGenericParameters)
				return null;
			if (IsPublic && DeclaringType.IsPublic)
				return new DynamicMetaObject(Ast.Convert(Ast.Field(null, Field), typeof(object)), BindingRestrictions.Empty);
			return new DynamicMetaObject(
				Ast.Call(AstUtils.Convert(AstUtils.Constant(Field), typeof(FieldInfo)), typeof(FieldInfo).GetMethod("GetValue"), AstUtils.Constant(null)),
				BindingRestrictions.Empty
			);
		}

		/// <summary>値の取得に関連付けられているエラーを返します。</summary>
		/// <param name="binder">言語のバインディングセマンティクスを指定します。</param>
		/// <returns>値の取得に関連付けられているエラー。または、呼び出し元によって既定のエラーメッセージが提供されることを示す <c>null</c>。</returns>
		public override ErrorInfo GetError(ActionBinder binder)
		{
			// FieldTracker only has one error - accessing a static field from 
			// a generic type.
			Debug.Assert(Field.DeclaringType.ContainsGenericParameters);
			return binder.MakeContainsGenericParametersError(this);
		}

		/// <summary>
		/// インスタンスに束縛されている値を取得する <see cref="Expression"/> を取得します。
		/// カスタムメンバトラッカーはこのメソッドをオーバーライドして、インスタンスへのバインド時の独自の動作を提供できます。
		/// </summary>
		/// <param name="resolverFactory">オーバーロード解決の方法を表す <see cref="OverloadResolverFactory"/> を指定します。</param>
		/// <param name="binder">言語のバインディングセマンティクスを指定します。</param>
		/// <param name="type">この <see cref="MemberTracker"/> がアクセスされた型を指定します。</param>
		/// <param name="instance">束縛されたインスタンスを指定します。</param>
		/// <returns>インスタンスに束縛されている値を取得する <see cref="Expression"/>。</returns>
		protected internal override DynamicMetaObject GetBoundValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type, DynamicMetaObject instance)
		{
			if (IsPublic && DeclaringType.IsVisible)
				return new DynamicMetaObject(
					AstUtils.Convert(Ast.Field(AstUtils.Convert(instance.Expression, Field.DeclaringType), Field), typeof(object)),
					BindingRestrictions.Empty
				);
			return DefaultBinder.MakeError(((DefaultBinder)binder).MakeNonPublicMemberGetError(resolverFactory, this, type, instance), BindingRestrictions.Empty, typeof(object));
		}

		/// <summary>
		/// バインディングが可能な場合、新しいメンバトラッカーを返す指定されたインスタンスにメンバトラッカーを関連付けます。
		/// バインディングが不可能な場合、既存のメンバトラッカーが返されます。
		/// 例えば、静的フィールドへのバインディングは、元のメンバトラッカーを返します。
		/// インスタンスフィールドへのバインディングは、インスタンスを渡す GetBoundValue または SetBoundValue を得る新しい <see cref="BoundMemberTracker"/> を返します。
		/// </summary>
		/// <param name="instance">メンバトラッカーを関連付けるインスタンスを指定します。</param>
		/// <returns>指定されたインスタンスに関連付けられたメンバトラッカー。</returns>
		public override MemberTracker BindToInstance(DynamicMetaObject instance) { return IsStatic ? (MemberTracker)this : new BoundMemberTracker(this, instance); }
	}
}
