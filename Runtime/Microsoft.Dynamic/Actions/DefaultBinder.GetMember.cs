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
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions
{
	using Ast = Expression;

	public partial class DefaultBinder : ActionBinder
	{
		/// <summary>
		/// メンバの取得を実行する <see cref="DynamicMetaObject"/> を構築します。
		/// すべてのビルトイン .NET メソッド、演算子メソッド、GetBoundMember および StrongBox インスタンスをサポートします。
		/// </summary>
		/// <param name="name">
		/// 取得するメンバの名前を指定します。
		/// この名前は <see cref="DefaultBinder"/> では処理されず、代わりに名前マングリング、大文字と小文字を区別しない検索などを行う GetMember API に渡されます。
		/// </param>
		/// <param name="target">メンバーが取得される <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <returns>
		/// メンバがアクセスされた際に返される値を表す　<see cref="DynamicMetaObject"/> を返します。
		/// 返される <see cref="DynamicMetaObject"/> は標準の DLR GetMemberBinder から返される前にボックス化が必要な値型に厳密に型指定されている可能性があります。
		/// 言語はあらゆるボックス化の実行に責任を持つので、カスタムボックス化を実行する機会も存在します。
		/// </returns>
		public DynamicMetaObject GetMember(string name, DynamicMetaObject target) { return GetMember(name, target, new DefaultOverloadResolverFactory(this), false, null); }

		/// <summary>
		/// メンバの取得を実行する <see cref="DynamicMetaObject"/> を構築します。
		/// すべてのビルトイン .NET メソッド、演算子メソッド、GetBoundMember および StrongBox インスタンスをサポートします。
		/// </summary>
		/// <param name="name">
		/// 取得するメンバの名前を指定します。
		/// この名前は <see cref="DefaultBinder"/> では処理されず、代わりに名前マングリング、大文字と小文字を区別しない検索などを行う GetMember API に渡されます。
		/// </param>
		/// <param name="target">メンバーが取得される <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <param name="resolverFactory">
		/// GetMember の実行に必要なあらゆる呼び出しに対するオーバーロード解決とメソッドバインディングを提供する
		/// <see cref="OverloadResolverFactory"/> を指定します。
		/// </param>
		/// <returns>
		/// メンバがアクセスされた際に返される値を表す　<see cref="DynamicMetaObject"/> を返します。
		/// 返される <see cref="DynamicMetaObject"/> は標準の DLR GetMemberBinder から返される前にボックス化が必要な値型に厳密に型指定されている可能性があります。
		/// 言語はあらゆるボックス化の実行に責任を持つので、カスタムボックス化を実行する機会も存在します。
		/// </returns>
		public DynamicMetaObject GetMember(string name, DynamicMetaObject target, OverloadResolverFactory resolverFactory) { return GetMember(name, target, resolverFactory, false, null); }

		/// <summary>
		/// メンバの取得を実行する <see cref="DynamicMetaObject"/> を構築します。
		/// すべてのビルトイン .NET メソッド、演算子メソッド、GetBoundMember および StrongBox インスタンスをサポートします。
		/// </summary>
		/// <param name="name">
		/// 取得するメンバの名前を指定します。
		/// この名前は <see cref="DefaultBinder"/> では処理されず、代わりに名前マングリング、大文字と小文字を区別しない検索などを行う GetMember API に渡されます。
		/// </param>
		/// <param name="target">メンバーが取得される <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <param name="resolverFactory">
		/// GetMember の実行に必要なあらゆる呼び出しに対するオーバーロード解決とメソッドバインディングを提供する
		/// <see cref="OverloadResolverFactory"/> を指定します。
		/// </param>
		/// <param name="isNoThrow">操作が失敗した際に例外をスローせず、単に失敗を表す値を返すかどうかを示す値を指定します。</param>
		/// <param name="errorSuggestion">取得操作がエラーになった際に使用される <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <returns>
		/// メンバがアクセスされた際に返される値を表す　<see cref="DynamicMetaObject"/> を返します。
		/// 返される <see cref="DynamicMetaObject"/> は標準の DLR GetMemberBinder から返される前にボックス化が必要な値型に厳密に型指定されている可能性があります。
		/// 言語はあらゆるボックス化の実行に責任を持つので、カスタムボックス化を実行する機会も存在します。
		/// </returns>
		public DynamicMetaObject GetMember(string name, DynamicMetaObject target, OverloadResolverFactory resolverFactory, bool isNoThrow, DynamicMetaObject errorSuggestion)
		{
			ContractUtils.RequiresNotNull(name, "name");
			ContractUtils.RequiresNotNull(target, "target");
			ContractUtils.RequiresNotNull(resolverFactory, "resolverFactory");
			return MakeGetMemberTarget(new GetMemberInfo(name, resolverFactory, isNoThrow, errorSuggestion), target);
		}

		/// <summary>
		/// メンバの取得を実行する <see cref="DynamicMetaObject"/> を構築します。
		/// すべてのビルトイン .NET メソッド、演算子メソッド、GetBoundMember および StrongBox インスタンスをサポートします。
		/// </summary>
		/// <param name="name">
		/// 取得するメンバの名前を指定します。
		/// この名前は <see cref="DefaultBinder"/> では処理されず、代わりに名前マングリング、大文字と小文字を区別しない検索などを行う GetMember API に渡されます。
		/// </param>
		/// <param name="target">メンバーが取得される <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <param name="isNoThrow">操作が失敗した際に例外をスローせず、単に失敗を表す値を返すかどうかを示す値を指定します。</param>
		/// <param name="errorSuggestion">取得操作がエラーになった際に使用される <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <returns>
		/// メンバがアクセスされた際に返される値を表す　<see cref="DynamicMetaObject"/> を返します。
		/// 返される <see cref="DynamicMetaObject"/> は標準の DLR GetMemberBinder から返される前にボックス化が必要な値型に厳密に型指定されている可能性があります。
		/// 言語はあらゆるボックス化の実行に責任を持つので、カスタムボックス化を実行する機会も存在します。
		/// </returns>
		public DynamicMetaObject GetMember(string name, DynamicMetaObject target, bool isNoThrow, DynamicMetaObject errorSuggestion)
		{
			ContractUtils.RequiresNotNull(name, "name");
			ContractUtils.RequiresNotNull(target, "target");
			return MakeGetMemberTarget(new GetMemberInfo(name, new DefaultOverloadResolverFactory(this), isNoThrow, errorSuggestion), target);
		}

		DynamicMetaObject MakeGetMemberTarget(GetMemberInfo getMemInfo, DynamicMetaObject target)
		{
			var type = target.GetLimitType();
			var restrictions = target.Restrictions;
			var self = target;
			target = target.Restrict(target.GetLimitType());
			// 特別に認識される型: TypeTracker, NamespaceTracker, StrongBox
			// TODO: TypeTracker および NamespaceTracker は技術的に IDO にする。
			var members = MemberGroup.EmptyGroup;
			if (typeof(TypeTracker).IsAssignableFrom(type))
			{
				restrictions = restrictions.Merge(BindingRestrictions.GetInstanceRestriction(target.Expression, target.Value));
				var tg = target.Value as TypeGroup;
				Type nonGen;
				if ((tg == null || tg.TryGetNonGenericType(out nonGen)) && (members = GetMember(MemberRequestKind.Get, ((TypeTracker)target.Value).Type, getMemInfo.Name)).Count > 0)
				{
					// トラッカーに関連付けられた型にあるメンバを持っているなら、それを返す。
					type = ((TypeTracker)target.Value).Type;
					self = null;
				}
			}
			if (members.Count == 0)
				members = GetMember(MemberRequestKind.Get, type, getMemInfo.Name); // Get the members
			if (members.Count == 0)
			{
				if (typeof(TypeTracker).IsAssignableFrom(type))
					// ジェネリックでない型がないことを確認し、あればエラーを報告する。
					// これは既定のバインダーのルールバージョンに適合するが、長期的に削除されるべきものと思われる
					System.Diagnostics.Debug.WriteLine(((TypeTracker)target.Value).Type);
				else if (type.IsInterface)
					members = GetMember(MemberRequestKind.Get, type = typeof(object), getMemInfo.Name); // すべてのインターフェイスは object メンバを持っている
			}
			var propSelf = self;
			// もし検索が失敗したら、利用可能であれば StrongBox で試す。
			if (members.Count == 0 && typeof(IStrongBox).IsAssignableFrom(type) && propSelf != null)
			{
				// プロパティ/フィールドは直接の値を必要とするため、メソッドは StrongBox に保持する。
				propSelf = new DynamicMetaObject(Ast.Field(AstUtils.Convert(propSelf.Expression, type), type.GetField("Value")), propSelf.Restrictions, ((IStrongBox)propSelf.Value).Value);
				type = type.GetGenericArguments()[0];
				members = GetMember(MemberRequestKind.Get, type, getMemInfo.Name);
			}
			MakeBodyHelper(getMemInfo, self, propSelf, type, members);
			getMemInfo.Body.Restrictions = restrictions;
			return getMemInfo.Body.GetMetaObject(target);
		}

		void MakeBodyHelper(GetMemberInfo getMemInfo, DynamicMetaObject self, DynamicMetaObject propSelf, Type type, MemberGroup members)
		{
			if (self != null)
				MakeOperatorGetMemberBody(getMemInfo, propSelf, type, "GetCustomMember");
			Expression error;
			var memberType = GetMemberType(members, out error);
			if (error == null)
				MakeSuccessfulMemberAccess(getMemInfo, self, propSelf, type, members, memberType);
			else
				getMemInfo.Body.FinishCondition(getMemInfo.ErrorSuggestion != null ? getMemInfo.ErrorSuggestion.Expression : error);
		}

		void MakeSuccessfulMemberAccess(GetMemberInfo getMemInfo, DynamicMetaObject self, DynamicMetaObject propSelf, Type type, MemberGroup members, TrackerTypes memberType)
		{
			switch (memberType)
			{
				case TrackerTypes.TypeGroup:
				case TrackerTypes.Type:
					getMemInfo.Body.FinishCondition(members.Skip(1).Aggregate((TypeTracker)members.First(), (x, y) => TypeGroup.Merge(x, (TypeTracker)y)).GetValue(getMemInfo.ResolutionFactory, this, type));
					break;
				case TrackerTypes.Method:
					// MethodGroup になる        
					MakeGenericBodyWorker(getMemInfo, type, ReflectionCache.GetMethodGroup(getMemInfo.Name, members), self);
					break;
				case TrackerTypes.Event:
				case TrackerTypes.Field:
				case TrackerTypes.Property:
				case TrackerTypes.Constructor:
				case TrackerTypes.Custom:
					// もし複数のメンバーが与えられたら、その型に一番近いメンバを探す
					MakeGenericBodyWorker(getMemInfo, type, members.Aggregate((w, x) => !IsTrackerApplicableForType(type, x) && (x.DeclaringType.IsSubclassOf(w.DeclaringType) || !IsTrackerApplicableForType(type, w)) ? x : w), propSelf);
					break;
				case TrackerTypes.All:
					// どのメンバも見つからなかった
					if (self != null)
						MakeOperatorGetMemberBody(getMemInfo, propSelf, type, "GetBoundMember");
					MakeMissingMemberRuleForGet(getMemInfo, self, type);
					break;
				default:
					throw new InvalidOperationException(memberType.ToString());
			}
		}

		static bool IsTrackerApplicableForType(Type type, MemberTracker mt) { return mt.DeclaringType == type || type.IsSubclassOf(mt.DeclaringType); }

		void MakeGenericBodyWorker(GetMemberInfo getMemInfo, Type type, MemberTracker tracker, DynamicMetaObject instance)
		{
			if (instance != null)
				tracker = tracker.BindToInstance(instance);
			var val = tracker.GetValue(getMemInfo.ResolutionFactory, this, type);
			if (val != null)
				getMemInfo.Body.FinishCondition(val);
			else if (tracker.GetError(this).Kind != ErrorInfoKind.Success && getMemInfo.IsNoThrow)
				getMemInfo.Body.FinishCondition(MakeOperationFailed());
			else
				getMemInfo.Body.FinishCondition(MakeError(tracker.GetError(this), typeof(object)));
		}

		void MakeOperatorGetMemberBody(GetMemberInfo getMemInfo, DynamicMetaObject instance, Type type, string name)
		{
			var getMem = GetMethod(type, name);
			if (getMem != null)
			{
				var tmp = Ast.Variable(typeof(object), "getVal");
				getMemInfo.Body.AddVariable(tmp);
				getMemInfo.Body.AddCondition(
					Ast.NotEqual(
						Ast.Assign(
							tmp,
							MakeCallExpression(
								getMemInfo.ResolutionFactory,
								getMem,
								new DynamicMetaObject(Ast.Convert(instance.Expression, type), instance.Restrictions, instance.Value),
								new DynamicMetaObject(Ast.Constant(getMemInfo.Name), BindingRestrictions.Empty, getMemInfo.Name)
							).Expression
						),
						Ast.Field(null, typeof(OperationFailed).GetField("Value"))
					),
					tmp
				);
			}
		}

		void MakeMissingMemberRuleForGet(GetMemberInfo getMemInfo, DynamicMetaObject self, Type type)
		{
			if (getMemInfo.ErrorSuggestion != null)
				getMemInfo.Body.FinishCondition(getMemInfo.ErrorSuggestion.Expression);
			else if (getMemInfo.IsNoThrow)
				getMemInfo.Body.FinishCondition(MakeOperationFailed());
			else
				getMemInfo.Body.FinishCondition(MakeError(MakeMissingMemberError(type, self, getMemInfo.Name), typeof(object)));
		}

		static MemberExpression MakeOperationFailed() { return Ast.Field(null, typeof(OperationFailed).GetField("Value")); }

		sealed class GetMemberInfo
		{
			public readonly string Name;
			public readonly OverloadResolverFactory ResolutionFactory;
			public readonly bool IsNoThrow;
			public readonly ConditionalBuilder Body = new ConditionalBuilder();
			public readonly DynamicMetaObject ErrorSuggestion;

			public GetMemberInfo(string name, OverloadResolverFactory resolutionFactory, bool noThrow, DynamicMetaObject errorSuggestion)
			{
				Name = name;
				ResolutionFactory = resolutionFactory;
				IsNoThrow = noThrow;
				ErrorSuggestion = errorSuggestion;
			}
		}
	}
}
