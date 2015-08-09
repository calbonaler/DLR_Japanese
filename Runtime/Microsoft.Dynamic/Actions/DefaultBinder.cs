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
using System.Reflection;
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Runtime;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions
{
	using Ast = Expression;

	/// <summary><see cref="ActionBinder"/> の既定の実装を提供します。</summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
	public partial class DefaultBinder : ActionBinder
	{
		/// <summary><see cref="DefaultBinder"/> の既定のインスタンスを表します。</summary>
		internal static readonly DefaultBinder Instance = new DefaultBinder();

		/// <summary><see cref="Microsoft.Scripting.Actions.DefaultBinder"/> クラスの新しいインスタンスを初期化します。</summary>
		public DefaultBinder() { }

		/// <summary>
		/// 指定された縮小変換レベルで <paramref name="fromType"/> から <paramref name="toType"/> に変換が存在するかどうかを返します。
		/// 対象の変数が <c>null</c> を許容しない場合は <paramref name="toNotNullable"/> は <c>true</c> になります。
		/// </summary>
		/// <param name="fromType">変換元の型を指定します。</param>
		/// <param name="toType">変換先の型を指定します。</param>
		/// <param name="toNotNullable">変換先の変数が <c>null</c> を許容しないかどうかを示す値を指定します。</param>
		/// <param name="level">変換を実行する縮小変換レベルを指定します。</param>
		/// <returns>指定された縮小変換レベルで <paramref name="fromType"/> から <paramref name="toType"/> に変換が存在すれば <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public override bool CanConvertFrom(Type fromType, Type toType, bool toNotNullable, NarrowingLevel level) { return toType.IsAssignableFrom(fromType); }

		/// <summary>2 つの仮引数の型の間に変換が存在しない場合は、2 つの仮引数の型の順序を返します。</summary>
		/// <param name="t1">1 番目の仮引数の型を指定します。</param>
		/// <param name="t2">2 番目の仮引数の型を指定します。</param>
		/// <returns>2 つの仮引数の型の間でどちらが適切かどうかを示す <see cref="Candidate"/>。</returns>
		public override Candidate PreferConvert(Type t1, Type t2) { return Candidate.Ambiguous; }

		/// <summary>バインダーによるメンバの削除が失敗した際に呼ばれます。</summary>
		/// <param name="type">メンバの削除を行った型です。</param>
		/// <param name="name">削除しようとしたメンバの名前です。</param>
		/// <returns>強制的なメンバの削除またはエラーを表す <see cref="ErrorInfo"/>。</returns>
		public virtual ErrorInfo MakeUndeletableMemberError(Type type, string name) { return MakeReadOnlyMemberError(type, name); }

		/// <summary>
		/// ユーザーが protected または private メンバの値を取得しようとした際に呼ばれます。
		/// 既定の実装ではリフレクションを使用することでフィールドまたはプロパティへのアクセスを許可します。
		/// </summary>
		/// <param name="resolverFactory">オーバーロード解決の方法を表す <see cref="OverloadResolverFactory"/> です。</param>
		/// <param name="member">取得が行われるメンバです。</param>
		/// <param name="type">取得が行われるメンバを保持している型です。</param>
		/// <param name="instance">取得が行われるメンバを保持しているインスタンスです。</param>
		/// <returns>メンバの取得またはエラーを表す <see cref="ErrorInfo"/>。</returns>
		public virtual ErrorInfo MakeNonPublicMemberGetError(OverloadResolverFactory resolverFactory, MemberTracker member, Type type, DynamicMetaObject instance)
		{
			switch (member.MemberType)
			{
				case TrackerTypes.Field:
					return ErrorInfo.FromValueNoError(
						Ast.Call(AstUtils.Convert(AstUtils.Constant(((FieldTracker)member).Field), typeof(FieldInfo)), typeof(FieldInfo).GetMethod("GetValue"),
							AstUtils.Convert(instance.Expression, typeof(object))
						)
					);
				case TrackerTypes.Property:
					return ErrorInfo.FromValueNoError(
						MemberTracker.FromMemberInfo(((PropertyTracker)member).GetGetMethod(true)).Call(resolverFactory, this, instance).Expression
					);
				default:
					throw new InvalidOperationException();
			}
		}

		/// <summary>書き込みを行おうとしたメンバーが読み取り専用であった場合に呼ばれます。</summary>
		/// <param name="type">書き込みを行おうとしたメンバを保持する型です。</param>
		/// <param name="name">書き込みを行おうとしたメンバの名前です。</param>
		/// <returns>エラーまたは強制的な書き込みを表す <see cref="ErrorInfo"/>。</returns>
		public virtual ErrorInfo MakeReadOnlyMemberError(Type type, string name)
		{
			return ErrorInfo.FromException(
				Expression.New(
					typeof(MissingMemberException).GetConstructor(new Type[] { typeof(string) }),
					AstUtils.Constant(name)
				)
			);
		}

		/// <summary>指定されたイベントにイベント ハンドラを関連付ける際に呼ばれます。</summary>
		/// <param name="members">関連付けられるイベントです。</param>
		/// <param name="eventObject">イベントを表す <see cref="DynamicMetaObject"/> です。</param>
		/// <param name="value">関連付けるハンドラを表す値です。</param>
		/// <param name="resolverFactory">オーバーロード解決の方法を提供する <see cref="OverloadResolverFactory"/> です。</param>
		/// <returns>エラーまたはイベントの関連付けを表す <see cref="ErrorInfo"/>。</returns>
		public virtual ErrorInfo MakeEventValidation(MemberGroup members, DynamicMetaObject eventObject, DynamicMetaObject value, OverloadResolverFactory resolverFactory)
		{
			// イベントの追加をハンドル - これはユーザーが正しい事を行っていることを確認する。
			return ErrorInfo.FromValueNoError(
				Expression.Call(new Action<EventTracker, object>(BinderOps.SetEvent).Method, AstUtils.Constant((EventTracker)members[0]), value.Expression)
			);
		}

		/// <summary>指定された <see cref="ErrorInfo"/> が表すエラーまたは値を表す <see cref="DynamicMetaObject"/> を作成します。</summary>
		/// <param name="error">エラーまたは値を保持している <see cref="ErrorInfo"/> を指定します。</param>
		/// <param name="type">結果として <see cref="DynamicMetaObject"/> が表す型を指定します。</param>
		/// <returns>エラーまたは値を表す <see cref="DynamicMetaObject"/>。</returns>
		public static DynamicMetaObject MakeError(ErrorInfo error, Type type) { return MakeError(error, BindingRestrictions.Empty, type); }

		/// <summary>指定された <see cref="ErrorInfo"/> が表すエラーまたは値を表す <see cref="DynamicMetaObject"/> を作成します。</summary>
		/// <param name="error">エラーまたは値を保持している <see cref="ErrorInfo"/> を指定します。</param>
		/// <param name="restrictions">生成される <see cref="DynamicMetaObject"/> に適用されるバインディング制約を指定します。</param>
		/// <param name="type">結果として <see cref="DynamicMetaObject"/> が表す型を指定します。</param>
		/// <returns>エラーまたは値を表す <see cref="DynamicMetaObject"/>。</returns>
		public static DynamicMetaObject MakeError(ErrorInfo error, BindingRestrictions restrictions, Type type)
		{
			switch (error.Kind)
			{
				case ErrorInfoKind.Error: // error meta objecT?
					return new DynamicMetaObject(AstUtils.Convert(error.Expression, type), restrictions);
				case ErrorInfoKind.Exception:
					return new DynamicMetaObject(AstUtils.Convert(Expression.Throw(error.Expression), type), restrictions);
				case ErrorInfoKind.Success:
					return new DynamicMetaObject(AstUtils.Convert(error.Expression, type), restrictions);
				default:
					throw new InvalidOperationException();
			}
		}

		static Expression MakeAmbiguousMatchError(MemberGroup members)
		{
			return Ast.Throw(
				Ast.New(
					typeof(AmbiguousMatchException).GetConstructor(new Type[] { typeof(string) }),
					AstUtils.Constant(string.Join(", ", members.Select(x => x.MemberType + " : " + x.ToString())))
				),
				typeof(object)
			);
		}

		/// <summary>
		/// 指定された <see cref="MemberGroup"/> に含まれている <see cref="MemberTracker"/> の種類を返します。
		/// <see cref="MemberGroup"/> に異なる種類の <see cref="MemberTracker"/> が存在した場合はエラーを返します。
		/// </summary>
		/// <param name="members">含まれているメンバの種類を判定する <see cref="MemberGroup"/> を指定します。</param>
		/// <param name="error">異なる種類の <see cref="MemberTracker"/> が含まれていた場合にエラーを格納する変数を指定します。</param>
		/// <returns>
		/// 指定された <see cref="MemberGroup"/> に含まれている <see cref="MemberTracker"/> の種類。
		/// 存在しないか異なる種類の <see cref="MemberTracker"/> が存在する場合は <see cref="TrackerTypes.All"/> を返します。
		/// </returns>
		public TrackerTypes GetMemberType(MemberGroup members, out Expression error)
		{
			error = null;
			var memberType = TrackerTypes.All;
			foreach (var mi in members)
			{
				if (mi.MemberType != memberType)
				{
					if (memberType != TrackerTypes.All)
					{
						error = MakeAmbiguousMatchError(members);
						return TrackerTypes.All;
					}
					memberType = mi.MemberType;
				}
			}
			return memberType;
		}

		/// <summary>指定された型およびその型階層の拡張型から指定された名前のメソッドを検索します。</summary>
		/// <param name="type">検索を開始する型を指定します。</param>
		/// <param name="name">検索するメソッドの名前を指定します。</param>
		/// <returns>見つかったメソッドを表す <see cref="MethodInfo"/>。見つからなかった場合は <c>null</c> を返し、複数見つかった場合は例外をスローします。</returns>
		public MethodInfo GetMethod(Type type, string name)
		{
			// declaring type takes precedence
			var mi = GetSpecialNameMethod(type, name);
			if (mi != null)
				return mi;
			// then search extension types.
			for (var curType = type; curType != null; curType = curType.BaseType)
			{
				foreach (var t in GetExtensionTypes(curType))
				{
					var next = GetSpecialNameMethod(t, name);
					if (next != null)
					{
						if (mi != null)
							throw AmbiguousMatch(type, name);
						mi = next;
					}
				}
				if (mi != null)
					return mi;
			}
			return null;
		}

		static MethodInfo GetSpecialNameMethod(Type type, string name)
		{
			MethodInfo res = null;
			var candidates = type.GetMember(name, MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			foreach (MethodInfo candidate in candidates)
			{
				if (candidate.IsSpecialName)
				{
					if (ReferenceEquals(res, null))
						res = candidate;
					else
						throw AmbiguousMatch(type, name);
				}
			}
			return res;
		}

		static Exception AmbiguousMatch(Type type, string name) { throw new AmbiguousMatchException(string.Format("型 {1} で {0} に対する複数の SpecialName メソッドが見つかりました。", name, type)); }
	}
}