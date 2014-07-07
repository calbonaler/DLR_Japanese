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
using System.Reflection;

using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Generation;

namespace Microsoft.Scripting.Actions
{
	/// <summary>
	/// 型のメンバとしての論理的なプロパティを表します。
	/// このクラスは (<see cref="ReflectedPropertyTracker"/> によって実装される) 型に定義されている実際のプロパティまたは、(<see cref="ExtensionPropertyTracker"/> によって実装される) 拡張プロパティのどちらかを表します。
	/// </summary>
	public abstract class PropertyTracker : MemberTracker
	{
		/// <summary><see cref="MemberTracker"/> の種類を取得します。</summary>
		public sealed override TrackerTypes MemberType { get { return TrackerTypes.Property; } }

		/// <summary>このプロパティのパブリックな get アクセサーを返します。</summary>
		/// <returns>このプロパティのパブリックな get アクセサーを表す <see cref="MethodInfo"/> オブジェクト。get アクセサーが非パブリックまたは存在しない場合は <c>null</c>。</returns>
		public MethodInfo GetGetMethod() { return GetGetMethod(false); }

		/// <summary>このプロパティのパブリックな set アクセサーを返します。</summary>
		/// <returns>このプロパティのパブリックな set アクセサーを表す <see cref="MethodInfo"/> オブジェクト。set アクセサーが非パブリックまたは存在しない場合は <c>null</c>。</returns>
		public MethodInfo GetSetMethod() { return GetSetMethod(false); }

		/// <summary>このプロパティのパブリックな delete アクセサーを返します。</summary>
		/// <returns>このプロパティのパブリックな delete アクセサーを表す <see cref="MethodInfo"/> オブジェクト。delete アクセサーが非パブリックまたは存在しない場合は <c>null</c>。</returns>
		public MethodInfo GetDeleteMethod() { return GetDeleteMethod(false); }

		/// <summary>派生クラスによってオーバーライドされた場合に、このプロパティのパブリックまたは非パブリックな get アクセサーを返します。</summary>
		/// <param name="privateMembers">非パブリックな get アクセサーを返すかどうかを示します。非パブリック アクセサーを返す場合は <c>true</c>。それ以外の場合は <c>false</c>。</param>
		/// <returns>
		/// <paramref name="privateMembers"/> が <c>true</c> の場合は、このプロパティの get アクセサーを表す <see cref="MethodInfo"/> オブジェクト。
		/// <paramref name="privateMembers"/> が <c>false</c> で get アクセサーが非パブリックの場合、または <paramref name="privateMembers"/> が <c>true</c> でも get アクセサーがない場合は、<c>null</c> を返します。
		/// </returns>
		public abstract MethodInfo GetGetMethod(bool privateMembers);

		/// <summary>派生クラスによってオーバーライドされた場合に、このプロパティのパブリックまたは非パブリックな set アクセサーを返します。</summary>
		/// <param name="privateMembers">非パブリックな set アクセサーを返すかどうかを示します。非パブリック アクセサーを返す場合は <c>true</c>。それ以外の場合は <c>false</c>。</param>
		/// <returns>
		/// <paramref name="privateMembers"/> が <c>true</c> の場合は、このプロパティの set アクセサーを表す <see cref="MethodInfo"/> オブジェクト。
		/// <paramref name="privateMembers"/> が <c>false</c> で set アクセサーが非パブリックの場合、または <paramref name="privateMembers"/> が <c>true</c> でも set アクセサーがない場合は、<c>null</c> を返します。
		/// </returns>
		public abstract MethodInfo GetSetMethod(bool privateMembers);

		/// <summary>派生クラスによってオーバーライドされた場合に、このプロパティのパブリックまたは非パブリックな delete アクセサーを返します。</summary>
		/// <param name="privateMembers">非パブリックな delete アクセサーを返すかどうかを示します。非パブリック アクセサーを返す場合は <c>true</c>。それ以外の場合は <c>false</c>。</param>
		/// <returns>
		/// <paramref name="privateMembers"/> が <c>true</c> の場合は、このプロパティの delete アクセサーを表す <see cref="MethodInfo"/> オブジェクト。
		/// <paramref name="privateMembers"/> が <c>false</c> で delete アクセサーが非パブリックの場合、または <paramref name="privateMembers"/> が <c>true</c> でも delete アクセサーがない場合は、<c>null</c> を返します。
		/// </returns>
		public virtual MethodInfo GetDeleteMethod(bool privateMembers) { return null; }

		/// <summary>派生クラスでオーバーライドされた場合に、プロパティのすべてのインデックス パラメータの配列を返します。</summary>
		/// <returns>インデックスのパラメーターを格納している <see cref="ParameterInfo"/> 型の配列。プロパティがインデックス付けされていない場合、配列の要素はゼロ (0) です。</returns>
		public abstract ParameterInfo[] GetIndexParameters();

		/// <summary>派生クラスでオーバーライドされた場合に、このプロパティが静的であるかどうかを示す値を取得します。</summary>
		public abstract bool IsStatic { get; }

		/// <summary>派生クラスでオーバーライドされた場合に、このプロパティの型を取得します。</summary>
		public abstract Type PropertyType { get; }

		/// <summary>
		/// 値を取得する <see cref="System.Linq.Expressions.Expression"/> を取得します。
		/// 呼び出し元は GetErrorForGet を呼び出して、正確なエラーを表す <see cref="System.Linq.Expressions.Expression"/> または既定のエラーを表す <c>null</c> を取得できます。
		/// </summary>
		/// <param name="resolverFactory">オーバーロード解決の方法を表す <see cref="OverloadResolverFactory"/> を指定します。</param>
		/// <param name="binder">言語のバインディングセマンティクスを指定します。</param>
		/// <param name="type">この <see cref="MemberTracker"/> がアクセスされた型を指定します。</param>
		/// <returns>値を取得する <see cref="System.Linq.Expressions.Expression"/>。エラーが発生した場合は <c>null</c> が返されます。</returns>
		public override DynamicMetaObject GetValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type)
		{
			if (!IsStatic || GetIndexParameters().Length > 0)
				return binder.ReturnMemberTracker(type, this); // need to bind to a value or parameters to get the value.
			var getter = ResolveGetter(binder.PrivateBinding);
			if (getter == null || getter.ContainsGenericParameters)
				return null; // no usable getter
			if (getter.IsPublic && getter.DeclaringType.IsPublic)
				return binder.MakeCallExpression(resolverFactory, getter);
			// private binding is just a call to the getter method...
			return MemberTracker.FromMemberInfo(getter).Call(resolverFactory, binder);
		}

		/// <summary>値の取得に関連付けられているエラーを返します。</summary>
		/// <param name="binder">言語のバインディングセマンティクスを指定します。</param>
		/// <returns>値の取得に関連付けられているエラー。または、呼び出し元によって既定のエラーメッセージが提供されることを示す <c>null</c>。</returns>
		public override ErrorInfo GetError(ActionBinder binder)
		{
			var getter = ResolveGetter(binder.PrivateBinding);
			if (getter == null)
				return binder.MakeMissingMemberErrorInfo(DeclaringType, Name);
			if (getter.ContainsGenericParameters)
				return binder.MakeGenericAccessError(this);
			throw new InvalidOperationException();
		}

		/// <summary>
		/// インスタンスに束縛されている値を取得する <see cref="System.Linq.Expressions.Expression"/> を取得します。
		/// カスタムメンバトラッカーはこのメソッドをオーバーライドして、インスタンスへのバインド時の独自の動作を提供できます。
		/// </summary>
		/// <param name="resolverFactory">オーバーロード解決の方法を表す <see cref="OverloadResolverFactory"/> を指定します。</param>
		/// <param name="binder">言語のバインディングセマンティクスを指定します。</param>
		/// <param name="type">この <see cref="MemberTracker"/> がアクセスされた型を指定します。</param>
		/// <param name="instance">束縛されたインスタンスを指定します。</param>
		/// <returns>インスタンスに束縛されている値を取得する <see cref="System.Linq.Expressions.Expression"/>。</returns>
		protected internal override DynamicMetaObject GetBoundValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type, DynamicMetaObject instance)
		{
			if (instance != null && IsStatic)
				return null;
			if (GetIndexParameters().Length > 0)
				return binder.ReturnMemberTracker(type, BindToInstance(instance)); // need to bind to a value or parameters to get the value.
			var getter = GetGetMethod(true);
			if (getter == null || getter.ContainsGenericParameters)
				return null; // no usable getter
			getter = CompilerHelpers.TryGetCallableMethod(getter);
			var defaultBinder = (DefaultBinder)binder;
			if (binder.PrivateBinding || CompilerHelpers.IsVisible(getter))
				return defaultBinder.MakeCallExpression(resolverFactory, getter, instance);
			// private binding is just a call to the getter method...
			return DefaultBinder.MakeError(defaultBinder.MakeNonPublicMemberGetError(resolverFactory, this, type, instance), BindingRestrictions.Empty, typeof(object));
		}

		/// <summary>束縛されたインスタンスを通したメンバへのアクセスに関連付けられているエラーを返します。</summary>
		/// <param name="binder">言語のバインディングセマンティクスを指定します。</param>
		/// <param name="instance">束縛されたインスタンスを指定します。</param>
		/// <returns>束縛されたインスタンスを通したメンバへのアクセスに関連付けられているエラー。または、呼び出し元によって既定のエラーメッセージが提供されることを示す <c>null</c>。</returns>
		public override ErrorInfo GetBoundError(ActionBinder binder, DynamicMetaObject instance)
		{
			var getter = ResolveGetter(binder.PrivateBinding);
			if (getter == null)
				return binder.MakeMissingMemberErrorInfo(DeclaringType, Name);
			if (getter.ContainsGenericParameters)
				return binder.MakeGenericAccessError(this);
			if (IsStatic)
				return binder.MakeStaticPropertyInstanceAccessError(this, false, instance);
			throw new InvalidOperationException();
		}

		/// <summary>
		/// バインディングが可能な場合、新しいメンバトラッカーを返す指定されたインスタンスにメンバトラッカーを関連付けます。
		/// バインディングが不可能な場合、既存のメンバトラッカーが返されます。
		/// 例えば、静的フィールドへのバインディングは、元のメンバトラッカーを返します。
		/// インスタンスフィールドへのバインディングは、インスタンスを渡す GetBoundValue または SetBoundValue を得る新しい <see cref="BoundMemberTracker"/> を返します。
		/// </summary>
		/// <param name="instance">メンバトラッカーを関連付けるインスタンスを指定します。</param>
		/// <returns>指定されたインスタンスに関連付けられたメンバトラッカー。</returns>
		public override MemberTracker BindToInstance(DynamicMetaObject instance) { return new BoundMemberTracker(this, instance); }

		MethodInfo ResolveGetter(bool privateBinding)
		{
			var getter = GetGetMethod(true);
			if (getter != null)
			{
				getter = CompilerHelpers.TryGetCallableMethod(getter);
				if (privateBinding || CompilerHelpers.IsVisible(getter))
					return getter;
			}
			return null;
		}
	}
}
