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
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions
{
	/// <summary>
	/// メソッドの一意のコレクションを表します。
	/// 一般には、この一意なセットは異なる項数のメソッドも含む同じ名前でオーバーロードされたすべてのメソッドです。
	/// これらのメソッドは単一の論理的にオーバーロードされた .NET 型の要素を表します。
	/// </summary>
	/// <remarks>
	/// 基本の DLR バインダーにメソッドのみを含む <see cref="MemberGroup"/> が提供された場合に、<see cref="MethodGroup"/> を生成します。
	/// <see cref="MethodGroup"/> はそれぞれの一意なメソッドのグループごとに一意なインスタンスとなります。
	/// </remarks>
	public class MethodGroup : MemberTracker
	{
		Dictionary<Type[], MethodGroup> _boundGenerics;

		/// <summary>指定された <see cref="MethodTracker"/> を使用して、<see cref="Microsoft.Scripting.Actions.MethodGroup"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="methods"><see cref="MemberGroup"/> に格納される <see cref="MethodTracker"/> を指定します。</param>
		internal MethodGroup(params MethodTracker[] methods) { Methods = new ReadOnlyCollection<MethodTracker>(methods); }

		/// <summary><see cref="MemberTracker"/> の種類を取得します。</summary>
		public override sealed TrackerTypes MemberType { get { return TrackerTypes.MethodGroup; } }

		/// <summary>メンバを論理的に宣言する型を取得します。</summary>
		public override Type DeclaringType { get { return Methods[0].DeclaringType; } }

		/// <summary>メンバの名前を取得します。</summary>
		public override string Name { get { return Methods[0].Name; } }

		/// <summary>この <see cref="MethodGroup"/> 内にインスタンスメソッドが存在するかどうかを示す値を取得します。</summary>
		public bool ContainsInstance { get { return Methods.Any(x => !x.IsStatic); } }

		/// <summary>この <see cref="MethodGroup"/> 内に静的メソッドが存在するかどうかを示す値を取得します。</summary>
		public bool ContainsStatic { get { return Methods.Any(x => x.IsStatic); } }

		/// <summary>この <see cref="MethodGroup"/> に含まれているすべてのメソッドを取得します。</summary>
		public ReadOnlyCollection<MethodTracker> Methods { get; private set; }

		/// <summary>この <see cref="MethodGroup"/> に含まれているすべてのメソッドに対する <see cref="MethodBase"/> を取得します。</summary>
		/// <returns><see cref="MethodGroup"/> に含まれているすべてのメソッドに対する <see cref="MethodBase"/> の配列。</returns>
		public MethodBase[] GetMethodBases() { return Methods.Select(x => (MethodBase)x.Method).ToArray(); }

		/// <summary>
		/// 値を取得する <see cref="System.Linq.Expressions.Expression"/> を取得します。
		/// 呼び出し元は GetErrorForGet を呼び出して、正確なエラーを表す <see cref="System.Linq.Expressions.Expression"/> または既定のエラーを表す <c>null</c> を取得できます。
		/// </summary>
		/// <param name="resolverFactory">オーバーロード解決の方法を表す <see cref="OverloadResolverFactory"/> を指定します。</param>
		/// <param name="binder">言語のバインディングセマンティクスを指定します。</param>
		/// <param name="type">この <see cref="MemberTracker"/> がアクセスされた型を指定します。</param>
		/// <returns>値を取得する <see cref="System.Linq.Expressions.Expression"/>。エラーが発生した場合は <c>null</c> が返されます。</returns>
		public override DynamicMetaObject GetValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type) { return base.GetValue(resolverFactory, binder, type); }

		/// <summary>
		/// バインディングが可能な場合、新しいメンバトラッカーを返す指定されたインスタンスにメンバトラッカーを関連付けます。
		/// バインディングが不可能な場合、既存のメンバトラッカーが返されます。
		/// 例えば、静的フィールドへのバインディングは、元のメンバトラッカーを返します。
		/// インスタンスフィールドへのバインディングは、インスタンスを渡す GetBoundValue または SetBoundValue を得る新しい <see cref="BoundMemberTracker"/> を返します。
		/// </summary>
		/// <param name="instance">メンバトラッカーを関連付けるインスタンスを指定します。</param>
		/// <returns>指定されたインスタンスに関連付けられたメンバトラッカー。</returns>
		public override MemberTracker BindToInstance(DynamicMetaObject instance) { return ContainsInstance ? (MemberTracker)new BoundMemberTracker(this, instance) : this; }

		/// <summary>
		/// インスタンスに束縛されている値を取得する <see cref="System.Linq.Expressions.Expression"/> を取得します。
		/// カスタムメンバトラッカーはこのメソッドをオーバーライドして、インスタンスへのバインド時の独自の動作を提供できます。
		/// </summary>
		/// <param name="resolverFactory">オーバーロード解決の方法を表す <see cref="OverloadResolverFactory"/> を指定します。</param>
		/// <param name="binder">言語のバインディングセマンティクスを指定します。</param>
		/// <param name="type">この <see cref="MemberTracker"/> がアクセスされた型を指定します。</param>
		/// <param name="instance">束縛されたインスタンスを指定します。</param>
		/// <returns>インスタンスに束縛されている値を取得する <see cref="System.Linq.Expressions.Expression"/>。</returns>
		protected internal override DynamicMetaObject GetBoundValue(OverloadResolverFactory resolverFactory, ActionBinder binder, Type type, DynamicMetaObject instance) { return binder.ReturnMemberTracker(type, BindToInstance(instance)); }

		/// <summary>
		/// この <see cref="MethodGroup"/> に含まれているメソッドに対して指定されたジェネリック型引数を適用することでジェネリックメソッドを作成します。
		/// 指定された型引数に対して適用できるジェネリックメソッド定義が存在しない場合は <c>null</c> を返します。
		/// </summary>
		/// <param name="types">この <see cref="MethodGroup"/> に含まれているメソッドに対して適用するジェネリック型引数を表す <see cref="Type"/> 型の配列を指定します。</param>
		/// <returns>指定されたジェネリック型引数が適用されたジェネリックメソッドのコレクションを表す <see cref="MethodGroup"/>。適用できるメソッドが存在しない場合は <c>null</c> を返します。</returns>
		public MethodGroup MakeGenericMethod(Type[] types)
		{
			// キャッシュされたメソッドを最初に探す
			MethodGroup mg;
			if (_boundGenerics != null)
			{
				lock (_boundGenerics)
				{
					if (_boundGenerics.TryGetValue(types, out mg))
						return mg;
				}
			}
			// ジェネリックターゲットを適切な項数 (型パラメータの数) を使用して探す
			// 互換なターゲットは定義による MethodInfo (コンストラクタは型引数をとらない)
			var targets = Methods.Where(x => x.Method.ContainsGenericParameters && x.Method.GetGenericArguments().Length == types.Length).Select(x => (MethodTracker)MemberTracker.FromMemberInfo(x.Method.MakeGenericMethod(types)));
			if (!targets.Any())
				return null;
			// 束縛された型引数を持つターゲットを含む新しい MethodGroup を作成し、キャッシュする
			mg = new MethodGroup(targets.ToArray());
			if (_boundGenerics == null)
				Interlocked.CompareExchange<Dictionary<Type[], MethodGroup>>(ref _boundGenerics, new Dictionary<Type[], MethodGroup>(1, ListEqualityComparer<Type>.Instance), null);
			lock (_boundGenerics)
				_boundGenerics[types] = mg;
			return mg;
		}

		sealed class ListEqualityComparer<T> : EqualityComparer<IEnumerable<T>>
		{
			internal static readonly ListEqualityComparer<T> Instance = new ListEqualityComparer<T>();

			ListEqualityComparer() { }

			public override bool Equals(IEnumerable<T> x, IEnumerable<T> y) { return x.SequenceEqual(y); }

			public override int GetHashCode(IEnumerable<T> obj) { return obj.Aggregate(6551, (x, y) => x ^ (x << 5) ^ y.GetHashCode()); }
		}
	}
}
