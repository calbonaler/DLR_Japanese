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
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Contracts;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>動的なオブジェクトに対する操作を提供します。</summary>
	public sealed class DynamicOperations
	{
		LanguageContext _lc;
		
		Dictionary<int, Func<DynamicOperations, CallSiteBinder, object, object[], object>> _invokers = new Dictionary<int, Func<DynamicOperations, CallSiteBinder, object, object[], object>>();

		/// <summary>頻繁に行われる操作のキャッシュに使用される SiteKey のディクショナリです。</summary>
		Dictionary<SiteKey, SiteKey> _sites = new Dictionary<SiteKey, SiteKey>();

		/// <summary>最近のクリーンアップ時までに作成したサイト数です。 </summary>
		int LastCleanup;

		/// <summary>これまでに作成したサイト数です。</summary>
		int SitesCreated;

		/// <summary>キャッシュのクリーンアップが実行される最小のサイト数です。</summary>
		const int CleanupThreshold = 20;

		/// <summary>削除に必要となる平均使用回数との最小差です。</summary>
		const int RemoveThreshold = 2;

		/// <summary>単一のキャッシュクリーンアップで削除する最大値です。</summary>
		const int StopCleanupThreshold = CleanupThreshold / 2;

		/// <summary>これ以上クリーンアップを行えないときに、クリアすべきサイト数です。</summary>
		const int ClearThreshold = 50;
		
		/// <summary>指定された言語プロバイダを使用して、<see cref="Microsoft.Scripting.Runtime.DynamicOperations"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="languageContext">基になる言語プロバイダを指定します。</param>
		public DynamicOperations(LanguageContext languageContext)
		{
			ContractUtils.RequiresNotNull(languageContext, "languageContext");
			_lc = languageContext;
		}

		#region Basic Operations

		/// <summary>指定されたオブジェクトを指定された引数によって呼び出します。</summary>
		/// <param name="obj">呼び出すオブジェクトを指定します。</param>
		/// <param name="parameters">オブジェクト呼び出しの引数を指定します。</param>
		public object Invoke(object obj, params object[] parameters) { return GetInvoker(parameters.Length)(this, _lc.CreateInvokeBinder(new CallInfo(parameters.Length)), obj, parameters); }

		/// <summary>オブジェクトの指定されたメンバを指定された引数を用いて呼び出します。</summary>
		/// <param name="obj">呼び出すメンバを保持しているオブジェクトを指定します。</param>
		/// <param name="memberName">呼び出すメンバの名前を指定します。</param>
		/// <param name="parameters">メンバ呼び出しの引数を指定します。</param>
		public object InvokeMember(object obj, string memberName, params object[] parameters) { return InvokeMember(obj, memberName, false, parameters); }

		/// <summary>オブジェクトの指定されたメンバを指定された引数を用いて呼び出します。</summary>
		/// <param name="obj">呼び出すメンバを保持しているオブジェクトを指定します。</param>
		/// <param name="memberName">呼び出すメンバの名前を指定します。</param>
		/// <param name="ignoreCase">メンバの検索に大文字と小文字を区別しないかどうかを示す値を指定します。</param>
		/// <param name="parameters">メンバ呼び出しの引数を指定します。</param>
		public object InvokeMember(object obj, string memberName, bool ignoreCase, params object[] parameters) { return GetInvoker(parameters.Length)(this, _lc.CreateCallBinder(memberName, ignoreCase, new CallInfo(parameters.Length)), obj, parameters); }

		/// <summary>指定されたオブジェクトに指定された引数を使用して新しいインスタンスを作成します。</summary>
		/// <param name="obj">インスタンスを作成する基になるオブジェクトを指定します。</param>
		/// <param name="parameters">インスタンスの作成の際に必要になる引数を指定します。</param>
		public object CreateInstance(object obj, params object[] parameters) { return GetInvoker(parameters.Length)(this, _lc.CreateCreateBinder(new CallInfo(parameters.Length)), obj, parameters); }

		/// <summary>オブジェクトの指定されたメンバを取得します。メンバが存在しないか、書き込み専用の場合は例外を発生させます。</summary>
		/// <param name="obj">取得するメンバを保持しているオブジェクトを指定します。</param>
		/// <param name="name">取得するメンバの名前を指定します。</param>
		public object GetMember(object obj, string name) { return GetMember(obj, name, false); }

		/// <summary>オブジェクトの指定されたメンバを取得し、結果を指定された型に変換します。メンバが存在しないか、書き込み専用の場合は例外を発生させます。</summary>
		/// <param name="obj">取得するメンバを保持しているオブジェクトを指定します。</param>
		/// <param name="name">取得するメンバの名前を指定します。</param>
		public T GetMember<T>(object obj, string name) { return GetMember<T>(obj, name, false); }

		/// <summary>オブジェクトの指定されたメンバを取得します。メンバが正常に取得された場合は true を返します。</summary>
		/// <param name="obj">取得するメンバを保持しているオブジェクトを指定します。</param>
		/// <param name="name">取得するメンバの名前を指定します。</param>
		/// <param name="value">取得したメンバの値を格納する変数を指定します。</param>
		public bool TryGetMember(object obj, string name, out object value) { return TryGetMember(obj, name, false, out value); }

		/// <summary>オブジェクトに指定されたメンバが存在するかどうかを示す値を返します。</summary>
		/// <param name="obj">メンバが存在するかどうかを調べるオブジェクトを指定します。</param>
		/// <param name="name">存在するかどうかを調べるメンバの名前を指定します。</param>
		public bool ContainsMember(object obj, string name) { return ContainsMember(obj, name, false); }

		/// <summary>オブジェクトから指定されたメンバを削除します。</summary>
		/// <param name="obj">メンバを削除するオブジェクトを指定します。</param>
		/// <param name="name">削除するメンバの名前を指定します。</param>
		public void RemoveMember(object obj, string name) { RemoveMember(obj, name, false); }

		/// <summary>オブジェクトの指定されたメンバに指定された値を設定します。</summary>
		/// <param name="obj">設定するメンバを保持しているオブジェクトを指定します。</param>
		/// <param name="name">設定するメンバの名前を指定します。</param>
		/// <param name="value">メンバに設定する値を指定します。</param>
		public void SetMember(object obj, string name, object value) { SetMember(obj, name, value, false); }

		/// <summary>オブジェクトの指定されたメンバに指定された値を設定します。このオーバーロードは厳密に型指定されているため、ボックス化やキャストを避けることができます。</summary>
		/// <param name="obj">設定するメンバを保持しているオブジェクトを指定します。</param>
		/// <param name="name">設定するメンバの名前を指定します。</param>
		/// <param name="value">メンバに設定する値を指定します。</param>
		public void SetMember<T>(object obj, string name, T value) { SetMember<T>(obj, name, value, false); }

		/// <summary>オブジェクトの指定されたメンバを取得します。メンバが存在しないか、書き込み専用の場合は例外を発生させます。</summary>
		/// <param name="obj">取得するメンバを保持しているオブジェクトを指定します。</param>
		/// <param name="name">取得するメンバの名前を指定します。</param>
		/// <param name="ignoreCase">メンバの検索に大文字と小文字を区別しないかどうかを示す値を指定します。</param>
		public object GetMember(object obj, string name, bool ignoreCase)
		{
			var site = GetOrCreateSite<object, object>(_lc.CreateGetMemberBinder(name, ignoreCase));
			return site.Target(site, obj);
		}

		/// <summary>オブジェクトの指定されたメンバを取得し、結果を指定された型に変換します。メンバが存在しないか、書き込み専用の場合は例外を発生させます。</summary>
		/// <param name="obj">取得するメンバを保持しているオブジェクトを指定します。</param>
		/// <param name="name">取得するメンバの名前を指定します。</param>
		/// <param name="ignoreCase">メンバの検索に大文字と小文字を区別しないかどうかを示す値を指定します。</param>
		public T GetMember<T>(object obj, string name, bool ignoreCase)
		{
			var convertSite = GetOrCreateSite<object, T>(_lc.CreateConvertBinder(typeof(T), null));
			var site = GetOrCreateSite<object, object>(_lc.CreateGetMemberBinder(name, ignoreCase));
			return convertSite.Target(convertSite, site.Target(site, obj));
		}

		/// <summary>オブジェクトの指定されたメンバを取得します。メンバが正常に取得された場合は true を返します。</summary>
		/// <param name="obj">取得するメンバを保持しているオブジェクトを指定します。</param>
		/// <param name="name">取得するメンバの名前を指定します。</param>
		/// <param name="ignoreCase">メンバの検索に大文字と小文字を区別しないかどうかを示す値を指定します。</param>
		/// <param name="value">取得したメンバの値を格納する変数を指定します。</param>
		public bool TryGetMember(object obj, string name, bool ignoreCase, out object value)
		{
			try
			{
				value = GetMember(obj, name, ignoreCase);
				return true;
			}
			catch (MissingMemberException)
			{
				value = null;
				return false;
			}
		}

		/// <summary>オブジェクトに指定されたメンバが存在するかどうかを示す値を返します。</summary>
		/// <param name="obj">メンバが存在するかどうかを調べるオブジェクトを指定します。</param>
		/// <param name="name">存在するかどうかを調べるメンバの名前を指定します。</param>
		/// <param name="ignoreCase">メンバの検索に大文字と小文字を区別しないかどうかを示す値を指定します。</param>
		public bool ContainsMember(object obj, string name, bool ignoreCase)
		{
			object dummy;
			return TryGetMember(obj, name, ignoreCase, out dummy);
		}

		/// <summary>オブジェクトから指定されたメンバを削除します。</summary>
		/// <param name="obj">メンバを削除するオブジェクトを指定します。</param>
		/// <param name="name">削除するメンバの名前を指定します。</param>
		/// <param name="ignoreCase">メンバの検索に大文字と小文字を区別しないかどうかを示す値を指定します。</param>
		public void RemoveMember(object obj, string name, bool ignoreCase)
		{
			var site = GetOrCreateSite<Action<CallSite, object>>(_lc.CreateDeleteMemberBinder(name, ignoreCase));
			site.Target(site, obj);
		}

		/// <summary>オブジェクトの指定されたメンバに指定された値を設定します。</summary>
		/// <param name="obj">設定するメンバを保持しているオブジェクトを指定します。</param>
		/// <param name="name">設定するメンバの名前を指定します。</param>
		/// <param name="value">メンバに設定する値を指定します。</param>
		/// <param name="ignoreCase">メンバの検索に大文字と小文字を区別しないかどうかを示す値を指定します。</param>
		public void SetMember(object obj, string name, object value, bool ignoreCase)
		{
			var site = GetOrCreateSite<object, object, object>(_lc.CreateSetMemberBinder(name, ignoreCase));
			site.Target(site, obj, value);
		}

		/// <summary>オブジェクトの指定されたメンバに指定された値を設定します。このオーバーロードは厳密に型指定されているため、ボックス化やキャストを避けることができます。</summary>
		/// <param name="obj">設定するメンバを保持しているオブジェクトを指定します。</param>
		/// <param name="name">設定するメンバの名前を指定します。</param>
		/// <param name="value">メンバに設定する値を指定します。</param>
		/// <param name="ignoreCase">メンバの検索に大文字と小文字を区別しないかどうかを示す値を指定します。</param>
		public void SetMember<T>(object obj, string name, T value, bool ignoreCase)
		{
			var site = GetOrCreateSite<object, T, object>(_lc.CreateSetMemberBinder(name, ignoreCase));
			site.Target(site, obj, value);
		}

		/// <summary>オブジェクトを指定された型に変換します。変換が明示的に行われるかどうかは言語仕様によって決定されます。</summary>
		/// <param name="obj">型を変換するオブジェクトを指定します。</param>
		public T ConvertTo<T>(object obj)
		{
			var site = GetOrCreateSite<object, T>(_lc.CreateConvertBinder(typeof(T), null));
			return site.Target(site, obj);
		}

		/// <summary>オブジェクトを指定された型に変換します。変換が明示的に行われるかどうかは言語仕様によって決定されます。</summary>
		/// <param name="obj">型を変換するオブジェクトを指定します。</param>
		/// <param name="type">変換結果となる型を指定します。</param>
		public object ConvertTo(object obj, Type type)
		{
			if (type.IsInterface || type.IsClass)
			{
				var site = GetOrCreateSite<object, object>(_lc.CreateConvertBinder(type, null));
				return site.Target(site, obj);
			}

			// TODO: We should probably cache these instead of using reflection all the time.
			foreach (MethodInfo mi in typeof(DynamicOperations).GetMember("ConvertTo"))
			{
				if (mi.IsGenericMethod)
				{
					try { return mi.MakeGenericMethod(type).Invoke(this, new [] { obj }); }
					catch (TargetInvocationException tie) { throw tie.InnerException; }
				}
			}

			throw new InvalidOperationException();
		}

		/// <summary>オブジェクトを指定された型に変換します。変換が成功した場合は true を返します。変換が明示的に行われるかどうかは言語仕様によって決定されます。</summary>
		/// <param name="obj">型を変換するオブジェクトを指定します。</param>
		/// <param name="result">変換されたオブジェクトを格納する変数を指定します。</param>
		public bool TryConvertTo<T>(object obj, out T result)
		{
			try
			{
				result = ConvertTo<T>(obj);
				return true;
			}
			catch (ArgumentTypeException)
			{
				result = default(T);
				return false;
			}
			catch (InvalidCastException)
			{
				result = default(T);
				return false;
			}
		}

		/// <summary>オブジェクトを指定された型に変換します。変換が成功した場合は true を返します。変換が明示的に行われるかどうかは言語仕様によって決定されます。</summary>
		/// <param name="obj">型を変換するオブジェクトを指定します。</param>
		/// <param name="type">変換結果となる型を指定します。</param>
		/// <param name="result">変換されたオブジェクトを格納する変数を指定します。</param>
		public bool TryConvertTo(object obj, Type type, out object result)
		{
			try
			{
				result = ConvertTo(obj, type);
				return true;
			}
			catch (ArgumentTypeException)
			{
				result = null;
				return false;
			}
			catch (InvalidCastException)
			{
				result = null;
				return false;
			}
		}

		/// <summary>オブジェクトを情報が欠落する可能性のある明示的変換を使用して指定された型に変換します。</summary>
		/// <param name="obj">型を変換するオブジェクトを指定します。</param>
		public T ExplicitConvertTo<T>(object obj)
		{
			var site = GetOrCreateSite<object, T>(_lc.CreateConvertBinder(typeof(T), true));
			return site.Target(site, obj);
		}

		/// <summary>オブジェクトを情報が欠落する可能性のある明示的変換を使用して指定された型に変換します。</summary>
		/// <param name="obj">型を変換するオブジェクトを指定します。</param>
		/// <param name="type">変換結果となる型を指定します。</param>
		public object ExplicitConvertTo(object obj, Type type)
		{
			var site = GetOrCreateSite<object, object>(_lc.CreateConvertBinder(type, true));
			return site.Target(site, obj);
		}

		/// <summary>オブジェクトを情報が欠落する可能性のある明示的変換を使用して指定された型に変換します。変換が成功した場合は true を返します。</summary>
		/// <param name="obj">型を変換するオブジェクトを指定します。</param>
		/// <param name="type">変換結果となる型を指定します。</param>
		/// <param name="result">変換されたオブジェクトを格納する変数を指定します。</param>
		public bool TryExplicitConvertTo(object obj, Type type, out object result)
		{
			try
			{
				result = ExplicitConvertTo(obj, type);
				return true;
			}
			catch (ArgumentTypeException)
			{
				result = null;
				return false;
			}
			catch (InvalidCastException)
			{
				result = null;
				return false;
			}
		}

		/// <summary>オブジェクトを情報が欠落する可能性のある明示的変換を使用して指定された型に変換します。変換が成功した場合は true を返します。</summary>
		/// <param name="obj">型を変換するオブジェクトを指定します。</param>
		/// <param name="result">変換されたオブジェクトを格納する変数を指定します。</param>
		public bool TryExplicitConvertTo<T>(object obj, out T result)
		{
			try
			{
				result = ExplicitConvertTo<T>(obj);
				return true;
			}
			catch (ArgumentTypeException)
			{
				result = default(T);
				return false;
			}
			catch (InvalidCastException)
			{
				result = default(T);
				return false;
			}
		}

		/// <summary>オブジェクトを指定された型に暗黙的に変換します。</summary>
		/// <param name="obj">型を変換するオブジェクトを指定します。</param>
		public T ImplicitConvertTo<T>(object obj)
		{
			var site = GetOrCreateSite<object, T>(_lc.CreateConvertBinder(typeof(T), false));
			return site.Target(site, obj);
		}

		/// <summary>オブジェクトを指定された型に暗黙的に変換します。</summary>
		/// <param name="obj">型を変換するオブジェクトを指定します。</param>
		/// <param name="type">変換結果となる型を指定します。</param>
		public object ImplicitConvertTo(object obj, Type type)
		{
			var site = GetOrCreateSite<object, object>(_lc.CreateConvertBinder(type, false));
			return site.Target(site, obj);
		}

		/// <summary>オブジェクトを指定された型に暗黙的に変換します。変換が成功した場合は true を返します。</summary>
		/// <param name="obj">型を変換するオブジェクトを指定します。</param>
		/// <param name="type">変換結果となる型を指定します。</param>
		/// <param name="result">変換されたオブジェクトを格納する変数を指定します。</param>
		public bool TryImplicitConvertTo(object obj, Type type, out object result)
		{
			try
			{
				result = ImplicitConvertTo(obj, type);
				return true;
			}
			catch (ArgumentTypeException)
			{
				result = null;
				return false;
			}
			catch (InvalidCastException)
			{
				result = null;
				return false;
			}
		}

		/// <summary>オブジェクトを指定された型に暗黙的に変換します。変換が成功した場合は true を返します。</summary>
		/// <param name="obj">型を変換するオブジェクトを指定します。</param>
		/// <param name="result">変換されたオブジェクトを格納する変数を指定します。</param>
		public bool TryImplicitConvertTo<T>(object obj, out T result)
		{
			try
			{
				result = ImplicitConvertTo<T>(obj);
				return true;
			}
			catch (ArgumentTypeException)
			{
				result = default(T);
				return false;
			}
			catch (InvalidCastException)
			{
				result = default(T);
				return false;
			}
		}

		/// <summary>汎用の単項演算を厳密に型指定された対象に対して実行します。</summary>
		/// <param name="operation">単項演算の種類を示す <see cref="System.Linq.Expressions.ExpressionType"/> を指定します。</param>
		/// <param name="target">単項演算を作用させる対象を指定します。</param>
		public TResult DoOperation<TTarget, TResult>(ExpressionType operation, TTarget target)
		{
			var site = GetOrCreateSite<TTarget, TResult>(_lc.CreateUnaryOperationBinder(operation));
			return site.Target(site, target);
		}

		/// <summary>汎用の二項演算を厳密に型指定された対象に対して実行します。</summary>
		/// <param name="operation">二項演算の種類を示す <see cref="System.Linq.Expressions.ExpressionType"/> を指定します。</param>
		/// <param name="target">二項演算を作用させる左側の対象を指定します。</param>
		/// <param name="other">二項演算を作用させる右側の対象を指定します。</param>
		public TResult DoOperation<TTarget, TOther, TResult>(ExpressionType operation, TTarget target, TOther other)
		{
			var site = GetOrCreateSite<TTarget, TOther, TResult>(_lc.CreateBinaryOperationBinder(operation));
			return site.Target(site, target, other);
		}

		/// <summary>指定されたオブジェクトに対する文字列で提供されるドキュメントを返します。</summary>
		/// <param name="o">ドキュメントを取得するオブジェクトを指定します。</param>
		public string GetDocumentation(object o) { return _lc.GetDocumentation(o); }

		/// <summary>ユーザーに対する表示形式の指定されたオブジェクトの呼び出しに対して適用されるシグネチャのリストを返します。</summary>
		/// <param name="o">シグネチャのリストを取得するオブジェクトを指定します。</param>
		public IList<string> GetCallSignatures(object o) { return _lc.GetCallSignatures(o); }

		/// <summary>指定されたオブジェクトが呼び出し可能かどうかを示す値を取得します。</summary>
		/// <param name="o">呼び出し可能かどうかを調べるオブジェクトを指定します。</param>
		public bool IsCallable(object o) { return _lc.IsCallable(o); }

		/// <summary>オブジェクトの既知のメンバの一覧を返します。</summary>
		/// <param name="obj">メンバの一覧を取得するオブジェクトを指定します。</param>
		public IList<string> GetMemberNames(object obj) { return _lc.GetMemberNames(obj); }

		/// <summary>指定されたオブジェクトの文字列表現を言語固有の表示形式で返します。</summary>
		/// <param name="obj">文字列表現を取得するオブジェクトを指定します。</param>
		public string Format(object obj) { return _lc.FormatObject(this, obj); }

		#endregion

		#region Private implementation details

		/// <summary>指定されたバインダーに対して指定された型引数を使用して動的サイトを取得または作成します。</summary>
		/// <param name="siteBinder">動的サイトに対して動作の実行時バインディングを行うバインダーを指定します。</param>
		/// <remarks>
		/// このメソッドはキャッシュに動的サイトが存在すれば取得し、それ以外の場合は作成します。
		/// 最近の使用からキャッシュが巨大になった場合はキャッシュはクリーンアップされます。
		/// </remarks>
		public CallSite<Func<CallSite, T1, TResult>> GetOrCreateSite<T1, TResult>(CallSiteBinder siteBinder) { return GetOrCreateSite<Func<CallSite, T1, TResult>>(siteBinder); }

		/// <summary>指定されたバインダーに対して指定された型引数を使用して動的サイトを取得または作成します。</summary>
		/// <param name="siteBinder">動的サイトに対して動作の実行時バインディングを行うバインダーを指定します。</param>
		/// <remarks>
		/// このメソッドはキャッシュに動的サイトが存在すれば取得し、それ以外の場合は作成します。
		/// 最近の使用からキャッシュが巨大になった場合はキャッシュはクリーンアップされます。
		/// </remarks>
		public CallSite<Func<CallSite, T1, T2, TResult>> GetOrCreateSite<T1, T2, TResult>(CallSiteBinder siteBinder) { return GetOrCreateSite<Func<CallSite, T1, T2, TResult>>(siteBinder); }

		/// <summary>指定されたバインダーに対して指定された型引数を使用して動的サイトを取得または作成します。</summary>
		/// <param name="siteBinder">動的サイトに対して動作の実行時バインディングを行うバインダーを指定します。</param>
		/// <remarks>
		/// このメソッドはキャッシュに動的サイトが存在すれば取得し、それ以外の場合は作成します。
		/// 最近の使用からキャッシュが巨大になった場合はキャッシュはクリーンアップされます。
		/// </remarks>
		public CallSite<TDelegate> GetOrCreateSite<TDelegate>(CallSiteBinder siteBinder) where TDelegate : class
		{
			SiteKey sk = new SiteKey(typeof(CallSite<TDelegate>), siteBinder);
			lock (_sites)
			{
				SiteKey old;
				if (!_sites.TryGetValue(sk, out old))
				{
					if (++SitesCreated < 0)
						SitesCreated = LastCleanup = 0; // オーバーフローしたので 0 にリセットします。
					sk.Site = CallSite<TDelegate>.Create(sk.SiteBinder);
					_sites[sk] = sk;
				}
				else
					sk = old;
				sk.HitCount++;
				CleanupNoLock();
			}
			return (CallSite<TDelegate>)sk.Site;
		}

		/// <summary>キャッシュからあまり使用されない動的サイトを削除します。</summary>
		void CleanupNoLock()
		{
			// cleanup only if we have too many sites and we've created a bunch since our last cleanup
			if (_sites.Count > CleanupThreshold && SitesCreated - LastCleanup > CleanupThreshold)
			{
				LastCleanup = SitesCreated;

				// calculate the average use, remove up to StopCleanupThreshold that are below average.
				int avgUse = _sites.Aggregate(0, (x, y) => x + y.Key.HitCount) / _sites.Count;
				if (avgUse == 1 && _sites.Count > ClearThreshold)
				{
					// we only have a bunch of one-off requests
					_sites.Clear();
					return;
				}

				var toRemove = _sites.Keys.Where(x => avgUse - x.HitCount > RemoveThreshold).Take(StopCleanupThreshold).ToList();
				// if we have a setup like weight(100), weight(1), weight(1), weight(1), ... we don't want
				// to just run through and remove all of the weight(1)'s. 

				if (toRemove.Count > 0)
				{
					foreach (var sk in toRemove)
						_sites.Remove(sk);
					// reset all hit counts so the next time through is fair to newly added members which may take precedence.
					foreach (var sk in _sites.Keys)
						sk.HitCount = 0;
				}
			}
		}

		/// <summary>
		/// すべての固有の動的サイトおよびそれらの使用パターンを追跡し、バインダーとサイト型の組をハッシュします。
		/// さらにこのクラスはヒット数を追跡し、関連付けられたサイトを保持します。
		/// </summary>
		class SiteKey : IEquatable<SiteKey>
		{
			// データのキー部分
			internal CallSiteBinder SiteBinder;
			Type _siteType;

			// 等価比較には用いられず、キャッシュにのみ関与する
			public int HitCount;
			public CallSite Site;

			public SiteKey(Type siteType, CallSiteBinder siteBinder)
			{
				Debug.Assert(siteType != null);
				Debug.Assert(siteBinder != null);

				SiteBinder = siteBinder;
				_siteType = siteType;
			}

			[Confined]
			public override bool Equals(object obj) { return Equals(obj as SiteKey); }

			[Confined]
			public override int GetHashCode() { return SiteBinder.GetHashCode() ^ _siteType.GetHashCode(); }

			[StateIndependent]
			public bool Equals(SiteKey other) { return other != null && other.SiteBinder.Equals(SiteBinder) && other._siteType == _siteType; }

#if DEBUG
			[Confined]
			public override string ToString() { return string.Format("{0} {1}", SiteBinder.ToString(), HitCount); }
#endif
		}

		Func<DynamicOperations, CallSiteBinder, object, object[], object> GetInvoker(int paramCount)
		{
			Func<DynamicOperations, CallSiteBinder, object, object[], object> invoker;
			lock (_invokers)
			{
				if (!_invokers.TryGetValue(paramCount, out invoker))
				{
					var dynOps = Expression.Parameter(typeof(DynamicOperations));
					var callInfo = Expression.Parameter(typeof(CallSiteBinder));
					var target = Expression.Parameter(typeof(object));
					var args = Expression.Parameter(typeof(object[]));
					var funcType = Expression.GetDelegateType(Enumerable.Repeat(typeof(CallSite), 1).Concat(Enumerable.Repeat(typeof(object), paramCount + 2)).ToArray());
					var site = Expression.Variable(typeof(CallSite<>).MakeGenericType(funcType));
					_invokers[paramCount] = invoker = Expression.Lambda<Func<DynamicOperations, CallSiteBinder, object, object[], object>>(
						Expression.Block(
							new[] { site },
							Expression.Assign(site, Expression.Call(dynOps, new Func<CallSiteBinder, CallSite<Action>>(GetOrCreateSite<Action>).Method.GetGenericMethodDefinition().MakeGenericMethod(funcType), callInfo)),
							Expression.Invoke(
								Expression.Field(site, site.Type.GetField("Target")),
								Enumerable.Repeat<Expression>(site, 1).Concat(Enumerable.Repeat(target, 1)).Concat(Enumerable.Range(0, paramCount).Select(x => Expression.ArrayIndex(args, Expression.Constant(x))))
							)
						),
						new[] { dynOps, callInfo, target, args }
					).Compile();
				}
			}
			return invoker;
		}

		#endregion
	}
}
