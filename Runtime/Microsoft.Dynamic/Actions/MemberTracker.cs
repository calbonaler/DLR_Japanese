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
using System.Dynamic;
using System.Reflection;
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions
{
	/// <summary>
	/// 型の論理的なメンバを表します。
	/// これは .NET が型に存在することを知っている「物理的な」メンバを、型に論理的に存在するメンバから分離し、
	/// さらに <see cref="MemberGroup"/> や <see cref="NamespaceTracker"/> のような .NET リフレクション以上のレベルの他の抽象化を提供します。
	/// また、部分信頼では拡張できないリフレクション API 周辺のラッパーを提供します。
	/// </summary>
	public abstract class MemberTracker
	{
		/// <summary>空の <see cref="MemberTracker"/> の配列を表します。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
		public static readonly MemberTracker[] EmptyTrackers = new MemberTracker[0];

		static readonly Dictionary<Tuple<MemberInfo, Type>, MemberTracker> _trackers = new Dictionary<Tuple<MemberInfo, Type>, MemberTracker>();

		internal MemberTracker() { }

		/// <summary><see cref="MemberTracker"/> の種類を取得します。</summary>
		public abstract TrackerTypes MemberType { get; }

		/// <summary>メンバを論理的に宣言する型を取得します。</summary>
		public abstract Type DeclaringType { get; }

		/// <summary>メンバの名前を取得します。</summary>
		public abstract string Name { get; }

		/// <summary>指定された <see cref="MemberInfo"/> に対する <see cref="MemberTracker"/> を返します。</summary>
		/// <param name="member"><see cref="MemberTracker"/> を取得する <see cref="MemberInfo"/> を指定します。</param>
		/// <returns>指定された <see cref="MemberInfo"/> に対する <see cref="MemberTracker"/>。</returns>
		public static MemberTracker FromMemberInfo(MemberInfo member) { return FromMemberInfo(member, null); }

		/// <summary>指定された <see cref="MemberInfo"/> に対する <see cref="MemberTracker"/> を返します。拡張メソッドも取り扱うことができます。</summary>
		/// <param name="member"><see cref="MemberTracker"/> を取得する <see cref="MemberInfo"/> を指定します。</param>
		/// <param name="extending">拡張メソッドが宣言された型を指定します。</param>
		/// <returns>指定された <see cref="MemberInfo"/> に対する <see cref="MemberTracker"/>。</returns>
		public static MemberTracker FromMemberInfo(MemberInfo member, Type extending)
		{
			ContractUtils.RequiresNotNull(member, "member");
			lock (_trackers)
			{
				MemberTracker res;
				var key = Tuple.Create(member, extending);
				if (_trackers.TryGetValue(key, out res))
					return res;
				switch (member.MemberType)
				{
					case MemberTypes.Constructor: res = new ConstructorTracker((ConstructorInfo)member); break;
					case MemberTypes.Event: res = new EventTracker((EventInfo)member); break;
					case MemberTypes.Field: res = new FieldTracker((FieldInfo)member); break;
					case MemberTypes.Method:
						var mi = (MethodInfo)member;
						res = extending != null ? new ExtensionMethodTracker(mi, member.IsDefined(typeof(StaticExtensionMethodAttribute), false), extending) : new MethodTracker(mi);
						break;
					case MemberTypes.TypeInfo:
					case MemberTypes.NestedType: res = new ReflectedTypeTracker((Type)member); break;
					case MemberTypes.Property: res = new ReflectedPropertyTracker((PropertyInfo)member); break;
					default: throw Error.UnknownMemberType(member.MemberType);
				}
				return _trackers[key] = res;
			}
		}

		#region Public expression builders

		/// <summary>
		/// 値を取得する <see cref="System.Linq.Expressions.Expression"/> を取得します。
		/// 呼び出し元は GetErrorForGet を呼び出して、正確なエラーを表す <see cref="System.Linq.Expressions.Expression"/> または既定のエラーを表す <c>null</c> を取得できます。
		/// </summary>
		/// <param name="resolverFactory">オーバーロード解決の方法を表す <see cref="OverloadResolverFactory"/> を指定します。</param>
		/// <param name="binder">言語のバインディングセマンティクスを指定します。</param>
		/// <param name="type">この <see cref="MemberTracker"/> がアクセスされた型を指定します。</param>
		/// <returns>値を取得する <see cref="System.Linq.Expressions.Expression"/>。エラーが発生した場合は <c>null</c> が返されます。</returns>
		public virtual DynamicMetaObject GetValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type) { return binder.ReturnMemberTracker(type, this); }

		/// <summary>
		/// 値を代入する <see cref="System.Linq.Expressions.Expression"/> を取得します。
		/// 呼び出し元は GetErrorForSet を呼び出して、正確なエラーを表す <see cref="System.Linq.Expressions.Expression"/> または既定のエラーを表す <c>null</c> を取得できます。
		/// </summary>
		/// <param name="resolverFactory">オーバーロード解決の方法を表す <see cref="OverloadResolverFactory"/> を指定します。</param>
		/// <param name="binder">言語のバインディングセマンティクスを指定します。</param>
		/// <param name="type">この <see cref="MemberTracker"/> がアクセスされた型を指定します。</param>
		/// <param name="value">この <see cref="MemberTracker"/> に代入される値を指定します。</param>
		/// <returns>値を代入する <see cref="System.Linq.Expressions.Expression"/>。エラーが発生した場合は <c>null</c> が返されます。</returns>
		public virtual DynamicMetaObject SetValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type, DynamicMetaObject value) { return null; }

		/// <summary>
		/// 指定された引数を使用してオブジェクトの呼び出しを実行する <see cref="System.Linq.Expressions.Expression"/> を取得します。
		/// 呼び出し元は GetErrorForDoCall を呼び出して、正確なエラーを表す <see cref="System.Linq.Expressions.Expression"/> または既定のエラーを表す <c>null</c> を取得できます。
		/// </summary>
		/// <param name="resolverFactory">オーバーロード解決の方法を表す <see cref="OverloadResolverFactory"/> を指定します。</param>
		/// <param name="binder">言語のバインディングセマンティクスを指定します。</param>
		/// <param name="arguments">オブジェクト呼び出しの引数を指定します。</param>
		/// <returns>オブジェクト呼び出しを実行する <see cref="System.Linq.Expressions.Expression"/>。エラーが発生した場合は <c>null</c> が返されます。</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Call")] // TODO: fix
		internal virtual DynamicMetaObject Call(OverloadResolverFactory resolverFactory, ActionBinder binder, params DynamicMetaObject[] arguments) { return null; }

		#endregion

		#region Public error expression builders

		/// <summary>値の取得に関連付けられているエラーを返します。</summary>
		/// <param name="binder">言語のバインディングセマンティクスを指定します。</param>
		/// <returns>値の取得に関連付けられているエラー。または、呼び出し元によって既定のエラーメッセージが提供されることを示す <c>null</c>。</returns>
		public virtual ErrorInfo GetError(ActionBinder binder) { return null; }

		/// <summary>束縛されたインスタンスを通したメンバへのアクセスに関連付けられているエラーを返します。</summary>
		/// <param name="binder">言語のバインディングセマンティクスを指定します。</param>
		/// <param name="instance">束縛されたインスタンスを指定します。</param>
		/// <returns>束縛されたインスタンスを通したメンバへのアクセスに関連付けられているエラー。または、呼び出し元によって既定のエラーメッセージが提供されることを示す <c>null</c>。</returns>
		public virtual ErrorInfo GetBoundError(ActionBinder binder, DynamicMetaObject instance) { return null; }

		/// <summary>
		/// インスタンスに束縛されている値を取得する <see cref="System.Linq.Expressions.Expression"/> を取得します。
		/// カスタムメンバトラッカーはこのメソッドをオーバーライドして、インスタンスへのバインド時の独自の動作を提供できます。
		/// </summary>
		/// <param name="resolverFactory">オーバーロード解決の方法を表す <see cref="OverloadResolverFactory"/> を指定します。</param>
		/// <param name="binder">言語のバインディングセマンティクスを指定します。</param>
		/// <param name="type">この <see cref="MemberTracker"/> がアクセスされた型を指定します。</param>
		/// <param name="instance">束縛されたインスタンスを指定します。</param>
		/// <returns>インスタンスに束縛されている値を取得する <see cref="System.Linq.Expressions.Expression"/>。</returns>
		protected internal virtual DynamicMetaObject GetBoundValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type, DynamicMetaObject instance) { return GetValue(resolverFactory, binder, type); }

		/// <summary>
		/// インスタンスに束縛されている値を設定する <see cref="System.Linq.Expressions.Expression"/> を取得します。
		/// カスタムメンバトラッカーはこのメソッドをオーバーライドして、インスタンスへのバインド時の独自の動作を提供できます。
		/// </summary>
		/// <param name="resolverFactory">オーバーロード解決の方法を表す <see cref="OverloadResolverFactory"/> を指定します。</param>
		/// <param name="binder">言語のバインディングセマンティクスを指定します。</param>
		/// <param name="type">この <see cref="MemberTracker"/> がアクセスされた型を指定します。</param>
		/// <param name="value">設定する値を指定します。</param>
		/// <param name="instance">束縛されたインスタンスを指定します。</param>
		/// <returns>インスタンスに束縛されている値を設定する <see cref="System.Linq.Expressions.Expression"/>。</returns>
		protected internal virtual DynamicMetaObject SetBoundValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type, DynamicMetaObject value, DynamicMetaObject instance) { return SetValue(resolverFactory, binder, type, instance); }

		/// <summary>
		/// バインディングが可能な場合、新しいメンバトラッカーを返す指定されたインスタンスにメンバトラッカーを関連付けます。
		/// バインディングが不可能な場合、既存のメンバトラッカーが返されます。
		/// 例えば、静的フィールドへのバインディングは、元のメンバトラッカーを返します。
		/// インスタンスフィールドへのバインディングは、インスタンスを渡す GetBoundValue または SetBoundValue を得る新しい <see cref="BoundMemberTracker"/> を返します。
		/// </summary>
		/// <param name="instance">メンバトラッカーを関連付けるインスタンスを指定します。</param>
		/// <returns>指定されたインスタンスに関連付けられたメンバトラッカー。</returns>
		public virtual MemberTracker BindToInstance(DynamicMetaObject instance) { return this; }

		#endregion
	}
}
