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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions
{
	/// <summary>
	/// 言語に対するバインディングセマンティクスを提供します。
	/// これにはアクションに対する規則を生成するためのサポートと同様に変換も含みます。
	/// これらの最適化された規則はメソッド呼び出し、操作の実行および <see cref="ActionBinder"/> の変換セマンティクスを使用するメンバの取得に使用されます。
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
	public abstract class ActionBinder
	{
		/// <summary>
		/// バインダーがパブリックでないメンバにアクセスできるかどうかを示す値を取得します。
		/// 既定では、バインダーはプライベートメンバへアクセスできませんが、
		/// この値をオーバーライドすることで、派生クラスはプライベートメンバへのバインディングが利用可能かどうかをカスタマイズできます。
		/// </summary>
		public virtual bool PrivateBinding { get { return false; } }

		/// <summary><see cref="Microsoft.Scripting.Actions.ActionBinder"/> クラスの新しいインスタンスを初期化します。</summary>
		protected ActionBinder() { }

		/// <summary>実行時にオブジェクトを指定された型に変換します。</summary>
		/// <param name="obj">変換するオブジェクトを指定します。</param>
		/// <param name="toType">オブジェクトを変換する型を指定します。</param>
		/// <returns>指定された型に変換されたオブジェクト。</returns>
		public virtual object Convert(object obj, Type toType)
		{
			if (obj == null)
			{
				if (!toType.IsValueType)
					return null;
			}
			else if (toType.IsValueType)
			{
				if (toType == obj.GetType())
					return obj;
			}
			else if (toType.IsAssignableFrom(obj.GetType()))
				return obj;
			throw Error.InvalidCast(obj != null ? obj.GetType().Name : "(null)", toType.Name);
		}

		/// <summary>
		/// 指定された縮小変換レベルで <paramref name="fromType"/> から <paramref name="toType"/> に変換が存在するかどうかを返します。
		/// 対象の変数が <c>null</c> を許容しない場合は <paramref name="toNotNullable"/> は <c>true</c> になります。
		/// </summary>
		/// <param name="fromType">変換元の型を指定します。</param>
		/// <param name="toType">変換先の型を指定します。</param>
		/// <param name="toNotNullable">変換先の変数が <c>null</c> を許容しないかどうかを示す値を指定します。</param>
		/// <param name="level">変換を実行する縮小変換レベルを指定します。</param>
		/// <returns>指定された縮小変換レベルで <paramref name="fromType"/> から <paramref name="toType"/> に変換が存在すれば <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public abstract bool CanConvertFrom(Type fromType, Type toType, bool toNotNullable, NarrowingLevel level);

		/// <summary>2 つの仮引数の型の間に変換が存在しない場合は、2 つの仮引数の型の順序を返します。</summary>
		/// <param name="t1">1 番目の仮引数の型を指定します。</param>
		/// <param name="t2">2 番目の仮引数の型を指定します。</param>
		/// <returns>2 つの仮引数の型の間でどちらが適切かどうかを示す <see cref="Candidate"/>。</returns>
		public abstract Candidate PreferConvert(Type t1, Type t2);

		// TODO: revisit
		/// <summary>指定された <see cref="Expression"/> を指定された型に変換します。<see cref="Expression"/> は複数回評価可能です。</summary>
		/// <param name="expr">複数回評価可能な指定された型に変換される <see cref="Expression"/> を指定します。</param>
		/// <param name="toType"><see cref="Expression"/> が変換される型を指定します。</param>
		/// <param name="kind">実行される変換の種類を指定します。</param>
		/// <param name="resolverFactory">この変換に使用できる <see cref="OverloadResolverFactory"/> を指定します。</param>
		/// <returns>指定された型に変換された <see cref="Expression"/>。</returns>
		public virtual Expression ConvertExpression(Expression expr, Type toType, ConversionResultKind kind, OverloadResolverFactory resolverFactory)
		{
			ContractUtils.RequiresNotNull(expr, "expr");
			ContractUtils.RequiresNotNull(toType, "toType");
			if (toType == typeof(object))
				return expr.Type.IsValueType ? AstUtils.Convert(expr, toType) : expr;
			if (toType.IsAssignableFrom(expr.Type))
				return expr;
			var visType = CompilerHelpers.GetVisibleType(toType);
			return Expression.Convert(expr, toType);
		}

		/// <summary>デリゲートによって指定される配列の指定されたインデックスに存在する値を指定された型に変換するデリゲートを取得します。</summary>
		/// <param name="index">変換する引数を示すデリゲート引数内のインデックスを指定します。</param>
		/// <param name="knownType">変換する値を表す <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <param name="toType">変換先の型を指定します。</param>
		/// <param name="conversionResultKind">実行される変換の種類を指定します。</param>
		/// <returns>デリゲートによって指定された配列の指定されたインデックスに存在する引数を指定された型に変換するデリゲート。</returns>
		public virtual Func<object[], object> ConvertObject(int index, DynamicMetaObject knownType, Type toType, ConversionResultKind conversionResultKind) { throw new NotSupportedException(); }

		/// <summary>
		/// 指定された型から指定された名前の可視であるメンバを取得します。
		/// 既定の実装は、型、平坦化された型階層、そして登録された拡張メソッドの順に検索されます。
		/// </summary>
		/// <param name="action">メンバに対する操作を指定します。</param>
		/// <param name="type">メンバを検索する型を指定します。</param>
		/// <param name="name">検索するメンバの名前を指定します。</param>
		/// <returns>検索されたメンバの一覧を表す <see cref="MemberGroup"/>。</returns>
		public virtual MemberGroup GetMember(MemberRequestKind action, Type type, string name)
		{
			// check for generic types w/ arity...
			var genTypes = type.GetNestedTypes(BindingFlags.Public).Where(x => x.Name.StartsWith(name + ReflectionUtils.GenericArityDelimiter));
			if (genTypes.Any())
				return new MemberGroup(genTypes.ToArray());
			var foundMembers = type.GetMember(name);
			if (!PrivateBinding)
				foundMembers = CompilerHelpers.FilterNonVisibleMembers(type, foundMembers);
			var members = new MemberGroup(foundMembers);
			if (members.Count == 0 && (members = new MemberGroup(type.GetMember(name, BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))).Count == 0)
				members = GetAllExtensionMembers(type, name);
			return members;
		}

		#region Error Production

		/// <summary>指定されたメンバのジェネリック型引数に関するエラーを生成します。</summary>
		/// <param name="tracker">エラーが発生したメンバを指定します。</param>
		/// <returns>指定されたメンバのジェネリック型引数に関するエラーを表す <see cref="ErrorInfo"/>。</returns>
		public virtual ErrorInfo MakeContainsGenericParametersError(MemberTracker tracker)
		{
			return ErrorInfo.FromException(
				Expression.New(
					typeof(InvalidOperationException).GetConstructor(new Type[] { typeof(string) }),
					AstUtils.Constant(Strings.InvalidOperation_ContainsGenericParameters(tracker.DeclaringType.Name, tracker.Name))
				)
			);
		}

		/// <summary>指定された型に指定された名前のメンバが見つからないことを表すエラーを生成します。</summary>
		/// <param name="type">メンバを検索した型を指定します。</param>
		/// <param name="name">検索したメンバの名前を指定します。</param>
		/// <returns>指定された型に指定された名前のメンバが見つからないことを表すエラーを表す <see cref="ErrorInfo"/>。</returns>
		public virtual ErrorInfo MakeMissingMemberErrorInfo(Type type, string name)
		{
			return ErrorInfo.FromException(
				Expression.New(
					typeof(MissingMemberException).GetConstructor(new Type[] { typeof(string) }),
					AstUtils.Constant(name)
				)
			);
		}

		/// <summary>指定されたメンバにおけるジェネリックアクセスに関するエラーを生成します。</summary>
		/// <param name="info">アクセスが発生したメンバを指定します。</param>
		/// <returns>指定されたメンバにおけるジェネリックアクセスに関するエラーを表す <see cref="ErrorInfo"/>。</returns>
		public virtual ErrorInfo MakeGenericAccessError(MemberTracker info)
		{
			return ErrorInfo.FromException(
				Expression.New(
					typeof(MemberAccessException).GetConstructor(new Type[] { typeof(string) }),
					AstUtils.Constant(info.Name)
				)
			);
		}

		/// <summary>セットがフィールドまたはプロパティに派生クラスから基底クラスを通して代入を試みたときに呼ばれます。既定の動作では代入を許可します。</summary>
		/// <param name="accessingType">アクセスする型を指定します。</param>
		/// <param name="self">代入が発生したインスタンスを指定します。</param>
		/// <param name="assigning">代入されるプロパティまたはフィールドを指定します。</param>
		/// <param name="assignedValue">代入される値を指定します。</param>
		/// <param name="context">代入を行うメソッドオーバーロードを解決する <see cref="OverloadResolverFactory"/> を指定します。</param>
		/// <returns>セットがフィールドまたはプロパティに派生クラスから基底クラスを通して代入を試みた際の情報を表す <see cref="ErrorInfo"/>。</returns>
		public virtual ErrorInfo MakeStaticAssignFromDerivedTypeError(Type accessingType, DynamicMetaObject self, MemberTracker assigning, DynamicMetaObject assignedValue, OverloadResolverFactory context)
		{
			switch (assigning.MemberType)
			{
				case TrackerTypes.Property:
					var pt = (PropertyTracker)assigning;
					var setter = pt.GetSetMethod() ?? pt.GetSetMethod(true);
					return ErrorInfo.FromValueNoError(
						AstUtils.SimpleCallHelper(
							setter,
							ConvertExpression(
								assignedValue.Expression,
								setter.GetParameters()[0].ParameterType,
								ConversionResultKind.ExplicitCast,
								context
							)
						)
					);
				case TrackerTypes.Field:
					var ft = (FieldTracker)assigning;
					return ErrorInfo.FromValueNoError(
						Expression.Assign(
							Expression.Field(null, ft.Field),
							ConvertExpression(assignedValue.Expression, ft.FieldType, ConversionResultKind.ExplicitCast, context)
						)
					);
				default:
					throw new InvalidOperationException();
			}
		}

		/// <summary>
		/// 静的プロパティがインスタンスメンバからアクセスされた場合を表す <see cref="ErrorInfo"/> を作成します。
		/// 既定の動作では、静的メンバプロパティはインスタンスを通してアクセスされなければならないことを示す例外を発生させます。
		/// 言語は例外、メッセージをカスタマイズしたり、アクセスされたプロパティを読み書きする <see cref="ErrorInfo"/> オブジェクトを生成したりするためにこのメソッドをオーバーライドできます。
		/// </summary>
		/// <param name="tracker">インスタンスを通してアクセスされた静的プロパティを指定します。</param>
		/// <param name="isAssignment">ユーザーがプロパティに値を代入したかどうかを示す値を指定します。</param>
		/// <param name="parameters">
		/// プロパティへのアクセスに使用される引数を指定します。
		/// このリストには最初の要素としてインスタンスが、<paramref name="isAssignment"/> が <c>true</c> の場合は、最後の要素として代入された値が格納されています。
		/// </param>
		/// <returns>例外またはプロパティの読み書き操作を表す <see cref="ErrorInfo"/>。</returns>
		public virtual ErrorInfo MakeStaticPropertyInstanceAccessError(PropertyTracker tracker, bool isAssignment, IEnumerable<DynamicMetaObject> parameters)
		{
			ContractUtils.RequiresNotNull(tracker, "tracker");
			ContractUtils.Requires(tracker.IsStatic, "tracker", Strings.ExpectedStaticProperty);
			ContractUtils.RequiresNotNull(parameters, "parameters");
			ContractUtils.RequiresNotNullItems(parameters, "parameters");
			string message = isAssignment ? Strings.StaticAssignmentFromInstanceError(tracker.Name, tracker.DeclaringType.Name) :
											Strings.StaticAccessFromInstanceError(tracker.Name, tracker.DeclaringType.Name);
			return ErrorInfo.FromException(
				Expression.New(
					typeof(MissingMemberException).GetConstructor(new Type[] { typeof(string) }),
					AstUtils.Constant(message)
				)
			);
		}

		/// <summary>
		/// 静的プロパティがインスタンスメンバからアクセスされた場合を表す <see cref="ErrorInfo"/> を表します。
		/// 既定の動作では、静的メンバプロパティはインスタンスを通してアクセスされなければならないことを示す例外を発生させます。
		/// 言語は例外、メッセージをカスタマイズしたり、アクセスされたプロパティを読み書きする <see cref="ErrorInfo"/> オブジェクトを生成したりするためにこのメソッドをオーバーライドできます。
		/// </summary>
		/// <param name="tracker">インスタンスを通してアクセスされた静的プロパティを指定します。</param>
		/// <param name="isAssignment">ユーザーがプロパティに値を代入したかどうかを示す値を指定します。</param>
		/// <param name="parameters">
		/// プロパティへのアクセスに使用される引数を指定します。
		/// このリストには最初の要素としてインスタンスが、<paramref name="isAssignment"/> が <c>true</c> の場合は、最後の要素として代入された値が格納されています。
		/// </param>
		/// <returns>例外またはプロパティの読み書き操作を表す <see cref="ErrorInfo"/>。</returns>
		public ErrorInfo MakeStaticPropertyInstanceAccessError(PropertyTracker tracker, bool isAssignment, params DynamicMetaObject[] parameters)
		{
			return MakeStaticPropertyInstanceAccessError(tracker, isAssignment, (IEnumerable<DynamicMetaObject>)parameters);
		}

		/// <summary>値型のフィールドに値を割り当てようとした際に発生するエラーを生成します。</summary>
		/// <param name="field">代入が行われたフィールドを指定します。</param>
		/// <param name="instance">代入が行われたフィールドを保持しているインスタンスを指定します。</param>
		/// <param name="value">代入される値を指定します。</param>
		/// <returns>例外またはフィールドへの代入操作を表す <see cref="ErrorInfo"/>。</returns>
		public virtual ErrorInfo MakeSetValueTypeFieldError(FieldTracker field, DynamicMetaObject instance, DynamicMetaObject value)
		{
			return ErrorInfo.FromException(
				Expression.Throw(
					Expression.New(
						typeof(ArgumentException).GetConstructor(new Type[] { typeof(string) }),
						AstUtils.Constant("cannot assign to value types")
					),
					typeof(object)
				)
			);
		}

		/// <summary>指定された <see cref="Expression"/> を指定された型に変換できない場合に発生するエラーを生成します。</summary>
		/// <param name="toType"><paramref name="value"/> が変換される型を指定します。</param>
		/// <param name="value">型を変換する <see cref="Expression"/> を指定します。</param>
		/// <returns>例外または変換操作を表す <see cref="ErrorInfo"/>。</returns>
		public virtual ErrorInfo MakeConversionError(Type toType, Expression value)
		{
			return ErrorInfo.FromException(
				Expression.Call(
					new Func<Type, object, Exception>(ScriptingRuntimeHelpers.CannotConvertError).Method,
					AstUtils.Constant(toType),
					AstUtils.Convert(value, typeof(object))
			   )
			);
		}

		/// <summary>
		/// 検索が失敗した際にカスタムエラーメッセージを返します。
		/// より堅牢なエラー返却メカニズムを実装するまでこのメソッドは使用されます。
		/// </summary>
		/// <param name="type">検索を行った型を指定します。</param>
		/// <param name="self">検索を行ったインスタンスを指定します。</param>
		/// <param name="name">検索したメンバの名前を指定します。</param>
		/// <returns>メンバが見つからない場合の例外を表す <see cref="ErrorInfo"/>。</returns>
		public virtual ErrorInfo MakeMissingMemberError(Type type, DynamicMetaObject self, string name)
		{
			return ErrorInfo.FromException(
				Expression.New(
					typeof(MissingMemberException).GetConstructor(new Type[] { typeof(string) }),
					AstUtils.Constant(name)
				)
			);
		}

		/// <summary>値の代入のためのメンバ検索で指定された名前のメンバが見つからない場合に発生するエラーを生成します。</summary>
		/// <param name="type">検索を行った型を指定します。</param>
		/// <param name="self">検索を行ったインスタンスを指定します。</param>
		/// <param name="name">値の代入のために検索したメンバの名前を指定します。</param>
		/// <returns>例外または代入用のメンバを表す <see cref="ErrorInfo"/>。</returns>
		public virtual ErrorInfo MakeMissingMemberErrorForAssign(Type type, DynamicMetaObject self, string name) { return MakeMissingMemberError(type, self, name); }
		
		/// <summary>読み取り専用のプロパティに値を代入しようとした場合に発生するエラーを生成します。</summary>
		/// <param name="type">検索を行った型を指定します。</param>
		/// <param name="self">検索を行ったインスタンスを指定します。</param>
		/// <param name="name">読み取り専用のプロパティを検索したメンバの名前を指定します。</param>
		/// <returns>例外を表す <see cref="ErrorInfo"/>。</returns>
		public virtual ErrorInfo MakeMissingMemberErrorForAssignReadOnlyProperty(Type type, DynamicMetaObject self, string name) { return MakeMissingMemberError(type, self, name); }

		/// <summary>メンバの削除のための検索で指定された名前のメンバが見つからない場合に発生するエラーを生成します。</summary>
		/// <param name="type">検索を行った型を指定します。</param>
		/// <param name="self">検索を行ったインスタンスを指定します。</param>
		/// <param name="name">削除のために検索したメンバの名前を指定します。</param>
		/// <returns>例外を表す <see cref="ErrorInfo"/>。</returns>
		public virtual ErrorInfo MakeMissingMemberErrorForDelete(Type type, DynamicMetaObject self, string name) { return MakeMissingMemberError(type, self, name); }

		#endregion

		/// <summary>指定された型に対する名前を返します。</summary>
		/// <param name="t">名前を取得する型を指定します。</param>
		/// <returns>型に対する名前。</returns>
		public virtual string GetTypeName(Type t) { return t.Name; }

		/// <summary>指定されたオブジェクトに対する型の名前を返します。</summary>
		/// <param name="arg">型の名前を取得するオブジェクトを指定します。</param>
		/// <returns>オブジェクトの型に対する名前。</returns>
		public virtual string GetObjectTypeName(object arg) { return GetTypeName(CompilerHelpers.GetType(arg)); }

		/// <summary>指定された型から指定された名前の拡張メンバを取得します。基底クラスも検索されます。継承階層の型が 1 つでも拡張メソッドを提供した場合、検索は停止します。</summary>
		/// <param name="type">拡張メンバを検索する型を指定します。</param>
		/// <param name="name">検索する拡張メンバの名前を指定します。</param>
		/// <returns>指定された型の継承関係で見つかった拡張メンバのリスト。</returns>
		public MemberGroup GetAllExtensionMembers(Type type, string name)
		{
			var curType = type;
			do
			{
				var res = GetExtensionMembers(curType, name);
				if (res.Count != 0)
					return res;
			} while ((curType = curType.BaseType) != null);
			return MemberGroup.EmptyGroup;
		}

		/// <summary>指定された型から指定された名前の拡張メンバを取得します。基底クラスは検索されません。</summary>
		/// <param name="declaringType">拡張メンバを検索する型を指定します。</param>
		/// <param name="name">検索する拡張メンバの名前を指定します。</param>
		/// <returns>指定された型で見つかった拡張メンバのリスト。</returns>
		public MemberGroup GetExtensionMembers(Type declaringType, string name)
		{
			var members = GetExtensionTypes(declaringType).SelectMany(ext =>
			{
				var res = ext.GetMember(name, BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
					.Select(x => !PrivateBinding ? CompilerHelpers.TryGetVisibleMember(x) : x).Where(x => x != null)
					.Select(x => ext != declaringType ? MemberTracker.FromMemberInfo(x, declaringType) : MemberTracker.FromMemberInfo(x));
				// TODO: Support indexed getters/setters w/ multiple methods
				var getter = (MethodInfo)ext.GetMember("Get" + name, MemberTypes.Method, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
					.SingleOrDefault(x => x.IsDefined(typeof(PropertyMethodAttribute), false));
				var setter = (MethodInfo)ext.GetMember("Set" + name, MemberTypes.Method, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
					.SingleOrDefault(x => x.IsDefined(typeof(PropertyMethodAttribute), false));
				var deleter = (MethodInfo)ext.GetMember("Delete" + name, MemberTypes.Method, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
					.SingleOrDefault(x => x.IsDefined(typeof(PropertyMethodAttribute), false));
				if (getter != null || setter != null || deleter != null)
					res = res.Concat(Enumerable.Repeat(new ExtensionPropertyTracker(name, getter, setter, deleter, declaringType), 1));
				return res;
			});
			if (members.Any())
				return new MemberGroup(members.ToArray());
			return MemberGroup.EmptyGroup;
		}

		/// <summary>指定された型に対するすべての拡張型を取得します。</summary>
		/// <param name="t">拡張型を取得する型を指定します。</param>
		/// <returns>型に対する拡張型。</returns>
		public virtual IList<Type> GetExtensionTypes(Type t) { return Type.EmptyTypes; } // None are provided by default, languages need to know how to provide these on their own terms.

		/// <summary>
		/// 言語がすべての <see cref="MemberTracker"/> をそれ自身の型で置き換える機会を提供します。
		/// 代わりに言語は <see cref="MemberTracker"/> を直接公開することもできます。
		/// </summary>
		/// <param name="type"><paramref name="memberTracker"/> がアクセスされた型を指定します。</param>
		/// <param name="memberTracker">ユーザーに返されるメンバを指定します。</param>
		/// <returns>指定されたメンバに対する <see cref="DynamicMetaObject"/>。</returns>
		public virtual DynamicMetaObject ReturnMemberTracker(Type type, MemberTracker memberTracker)
		{
			if (memberTracker.MemberType == TrackerTypes.Bound)
			{
				var bmt = (BoundMemberTracker)memberTracker;
				return new DynamicMetaObject(
					Expression.New(
						typeof(BoundMemberTracker).GetConstructor(new Type[] { typeof(MemberTracker), typeof(object) }),
						AstUtils.Constant(bmt.BoundTo), bmt.Instance.Expression
					),
					BindingRestrictions.Empty
				);
			}
			return new DynamicMetaObject(AstUtils.Constant(memberTracker), BindingRestrictions.Empty, memberTracker);
		}

		/// <summary>指定された <see cref="OverloadResolverFactory"/> を使用して、指定されたメソッドを指定された引数で呼び出す <see cref="Expression"/> を作成します。</summary>
		/// <param name="resolverFactory">オーバーロードを解決する <see cref="OverloadResolverFactory"/> を指定します。</param>
		/// <param name="method">呼び出すメソッドを表す <see cref="MethodInfo"/> を指定します。</param>
		/// <param name="parameters">メソッドに渡す引数を指定します。</param>
		/// <returns>指定されたメソッドを呼び出す <see cref="Expression"/> を格納する <see cref="DynamicMetaObject"/>。</returns>
		public DynamicMetaObject MakeCallExpression(OverloadResolverFactory resolverFactory, MethodInfo method, params DynamicMetaObject[] parameters)
		{
			var resolver = method.IsStatic ?
				resolverFactory.CreateOverloadResolver(parameters, new CallSignature(parameters.Length), CallTypes.None) :
				resolverFactory.CreateOverloadResolver(parameters, new CallSignature(parameters.Length - 1), CallTypes.ImplicitInstance);
			var target = resolver.ResolveOverload(method.Name, new MethodBase[] { method }, NarrowingLevel.None, NarrowingLevel.All);
			if (!target.Success)
				return DefaultBinder.MakeError(resolver.MakeInvalidParametersError(target), parameters.Aggregate(BindingRestrictions.Combine(parameters), (x, y) => x.Merge(BindingRestrictions.GetTypeRestriction(y.Expression, y.GetLimitType()))), typeof(object));
			return new DynamicMetaObject(target.MakeExpression(), target.RestrictedArguments.GetAllRestrictions());
		}
	}
}

