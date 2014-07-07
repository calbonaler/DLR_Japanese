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
using Microsoft.Scripting.Actions.Calls;

namespace Microsoft.Scripting.Actions
{
	/// <summary>特定のインスタンスに束縛された <see cref="MemberTracker"/> を表します。</summary>
	public class BoundMemberTracker : MemberTracker
	{
		DynamicMetaObject _instance;
		MemberTracker _tracker;
		object _objInst;

		/// <summary>基になる <see cref="MemberTracker"/> と、束縛されるインスタンスを表す <see cref="DynamicMetaObject"/> を使用して、<see cref="Microsoft.Scripting.Actions.BoundMemberTracker"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="tracker">このオブジェクトの基になる <see cref="MemberTracker"/> を指定します。</param>
		/// <param name="instance">束縛されるインスタンスを表す <see cref="DynamicMetaObject"/> を指定します。</param>
		public BoundMemberTracker(MemberTracker tracker, DynamicMetaObject instance)
		{
			_tracker = tracker;
			_instance = instance;
		}

		/// <summary>基になる <see cref="MemberTracker"/> と、束縛されるインスタンスを使用して、<see cref="Microsoft.Scripting.Actions.BoundMemberTracker"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="tracker">このオブジェクトの基になる <see cref="MemberTracker"/> を指定します。</param>
		/// <param name="instance">束縛されるインスタンスを指定します。</param>
		public BoundMemberTracker(MemberTracker tracker, object instance)
		{
			_tracker = tracker;
			_objInst = instance;
		}

		/// <summary><see cref="MemberTracker"/> の種類を取得します。</summary>
		public sealed override TrackerTypes MemberType { get { return TrackerTypes.Bound; } }

		/// <summary>メンバを論理的に宣言する型を取得します。</summary>
		public override Type DeclaringType { get { return _tracker.DeclaringType; } }

		/// <summary>メンバの名前を取得します。</summary>
		public override string Name { get { return _tracker.Name; } }

		/// <summary>この <see cref="MemberTracker"/> が関連付けられたインスタンスを表す <see cref="DynamicMetaObject"/> を取得します。</summary>
		public DynamicMetaObject Instance { get { return _instance; } }

		/// <summary>この <see cref="MemberTracker"/> が関連付けられたインスタンスを取得します。</summary>
		public object ObjectInstance { get { return _objInst; } }

		/// <summary>この <see cref="MemberTracker"/> が関連付けられた <see cref="MemberTracker"/> を取得します。</summary>
		public MemberTracker BoundTo { get { return _tracker; } }

		/// <summary>
		/// 値を取得する <see cref="System.Linq.Expressions.Expression"/> を取得します。
		/// 呼び出し元は GetErrorForGet を呼び出して、正確なエラーを表す <see cref="System.Linq.Expressions.Expression"/> または既定のエラーを表す <c>null</c> を取得できます。
		/// </summary>
		/// <param name="resolverFactory">オーバーロード解決の方法を表す <see cref="OverloadResolverFactory"/> を指定します。</param>
		/// <param name="binder">言語のバインディングセマンティクスを指定します。</param>
		/// <param name="type">この <see cref="MemberTracker"/> がアクセスされた型を指定します。</param>
		/// <returns>値を取得する <see cref="System.Linq.Expressions.Expression"/>。エラーが発生した場合は <c>null</c> が返されます。</returns>
		public override DynamicMetaObject GetValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type) { return _tracker.GetBoundValue(resolverFactory, binder, type, _instance); }

		/// <summary>値の取得に関連付けられているエラーを返します。</summary>
		/// <param name="binder">言語のバインディングセマンティクスを指定します。</param>
		/// <returns>値の取得に関連付けられているエラー。または、呼び出し元によって既定のエラーメッセージが提供されることを示す <c>null</c>。</returns>
		public override ErrorInfo GetError(ActionBinder binder) { return _tracker.GetBoundError(binder, _instance); }

		/// <summary>
		/// 値を代入する <see cref="System.Linq.Expressions.Expression"/> を取得します。
		/// 呼び出し元は GetErrorForSet を呼び出して、正確なエラーを表す <see cref="System.Linq.Expressions.Expression"/> または既定のエラーを表す <c>null</c> を取得できます。
		/// </summary>
		/// <param name="resolverFactory">オーバーロード解決の方法を表す <see cref="OverloadResolverFactory"/> を指定します。</param>
		/// <param name="binder">言語のバインディングセマンティクスを指定します。</param>
		/// <param name="type">この <see cref="MemberTracker"/> がアクセスされた型を指定します。</param>
		/// <param name="value">この <see cref="MemberTracker"/> に代入される値を指定します。</param>
		/// <returns>値を代入する <see cref="System.Linq.Expressions.Expression"/>。エラーが発生した場合は <c>null</c> が返されます。</returns>
		public override DynamicMetaObject SetValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type, DynamicMetaObject value) { return _tracker.SetBoundValue(resolverFactory, binder, type, value, _instance); }
	}
}
