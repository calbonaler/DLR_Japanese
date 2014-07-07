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
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Remoting;
using System.Security.Permissions;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting
{
	/// <summary>
	/// メンバ アクセス、変換、インデックスなどといったオブジェクトの操作に関する大規模なカタログを提供します。
	/// これらはより高機能なホストにおいて利用可能な内部調査およびツールサポートサービスとなります。
	/// </summary>
	/// <remarks>
	/// <see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> インスタンスは <see cref="Microsoft.Scripting.Hosting.ScriptEngine"/> より取得でき、
	/// 操作のセマンティクスに対してエンジンに関連付けられます。<see cref="Microsoft.Scripting.Hosting.ObjectOperations"/> には、
	/// エンジンのすべての使用に対して共有できる既定のインスタンスが存在しますが、非常に高機能なホストではインスタンスを作成することもできます。
	/// </remarks>
	public sealed class ObjectOperations : MarshalByRefObject
	{
		readonly DynamicOperations _ops;

		// friend class: DynamicOperations
		internal ObjectOperations(DynamicOperations ops, ScriptEngine engine)
		{
			Assert.NotNull(ops);
			Assert.NotNull(engine);
			_ops = ops;
			Engine = engine;
		}

		public ScriptEngine Engine { get; private set; }

		#region Local Operations

		/// <summary>指定されたオブジェクトが呼び出し可能かどうかを示す値を取得します。</summary>
		/// <param name="obj">呼び出し可能かどうかを調べるオブジェクトを指定します。</param>
		public bool IsCallable(object obj) { return _ops.IsCallable(obj); }

		/// <summary>指定されたオブジェクトを指定された引数によって呼び出します。</summary>
		/// <param name="obj">呼び出すオブジェクトを指定します。</param>
		/// <param name="parameters">オブジェクト呼び出しの引数を指定します。</param>
		public dynamic Invoke(object obj, params object[] parameters) { return _ops.Invoke(obj, parameters); }

		/// <summary>オブジェクトの指定されたメンバを指定された引数を用いて呼び出します。</summary>
		/// <param name="obj">呼び出すメンバを保持しているオブジェクトを指定します。</param>
		/// <param name="memberName">呼び出すメンバの名前を指定します。</param>
		/// <param name="parameters">メンバ呼び出しの引数を指定します。</param>
		public dynamic InvokeMember(object obj, string memberName, params object[] parameters) { return _ops.InvokeMember(obj, memberName, parameters); }

		/// <summary>指定されたオブジェクトに指定された引数を使用して新しいインスタンスを作成します。</summary>
		/// <param name="obj">インスタンスを作成する基になるオブジェクトを指定します。</param>
		/// <param name="parameters">インスタンスの作成の際に必要になる引数を指定します。</param>
		public dynamic CreateInstance(object obj, params object[] parameters) { return _ops.CreateInstance(obj, parameters); }

		/// <summary>オブジェクトの指定されたメンバを取得します。メンバが存在しないか、書き込み専用の場合は例外を発生させます。</summary>
		/// <param name="obj">取得するメンバを保持しているオブジェクトを指定します。</param>
		/// <param name="name">取得するメンバの名前を指定します。</param>
		public dynamic GetMember(object obj, string name) { return _ops.GetMember(obj, name); }

		/// <summary>オブジェクトの指定されたメンバを取得し、結果を指定された型に変換します。メンバが存在しないか、書き込み専用の場合は例外を発生させます。</summary>
		/// <param name="obj">取得するメンバを保持しているオブジェクトを指定します。</param>
		/// <param name="name">取得するメンバの名前を指定します。</param>
		public T GetMember<T>(object obj, string name) { return _ops.GetMember<T>(obj, name); 	}

		/// <summary>オブジェクトの指定されたメンバを取得します。メンバが正常に取得された場合は true を返します。</summary>
		/// <param name="obj">取得するメンバを保持しているオブジェクトを指定します。</param>
		/// <param name="name">取得するメンバの名前を指定します。</param>
		/// <param name="value">取得したメンバの値を格納する変数を指定します。</param>
		public bool TryGetMember(object obj, string name, out object value) { return _ops.TryGetMember(obj, name, out value); }

		/// <summary>オブジェクトに指定されたメンバが存在するかどうかを示す値を返します。</summary>
		/// <param name="obj">メンバが存在するかどうかを調べるオブジェクトを指定します。</param>
		/// <param name="name">存在するかどうかを調べるメンバの名前を指定します。</param>
		public bool ContainsMember(object obj, string name) { return _ops.ContainsMember(obj, name); }

		/// <summary>オブジェクトから指定されたメンバを削除します。</summary>
		/// <param name="obj">メンバを削除するオブジェクトを指定します。</param>
		/// <param name="name">削除するメンバの名前を指定します。</param>
		public void RemoveMember(object obj, string name) { _ops.RemoveMember(obj, name); }

		/// <summary>オブジェクトの指定されたメンバに指定された値を設定します。</summary>
		/// <param name="obj">設定するメンバを保持しているオブジェクトを指定します。</param>
		/// <param name="name">設定するメンバの名前を指定します。</param>
		/// <param name="value">メンバに設定する値を指定します。</param>
		public void SetMember(object obj, string name, object value) { _ops.SetMember(obj, name, value); }

		/// <summary>オブジェクトの指定されたメンバに指定された値を設定します。このオーバーロードは厳密に型指定されているため、ボックス化やキャストを避けることができます。</summary>
		/// <param name="obj">設定するメンバを保持しているオブジェクトを指定します。</param>
		/// <param name="name">設定するメンバの名前を指定します。</param>
		/// <param name="value">メンバに設定する値を指定します。</param>
		public void SetMember<T>(object obj, string name, T value) 	{ _ops.SetMember<T>(obj, name, value); }

		/// <summary>オブジェクトの指定されたメンバを取得します。メンバが存在しないか、書き込み専用の場合は例外を発生させます。</summary>
		/// <param name="obj">取得するメンバを保持しているオブジェクトを指定します。</param>
		/// <param name="name">取得するメンバの名前を指定します。</param>
		/// <param name="ignoreCase">メンバの検索に大文字と小文字を区別しないかどうかを示す値を指定します。</param>
		public dynamic GetMember(object obj, string name, bool ignoreCase) { return _ops.GetMember(obj, name, ignoreCase); }

		/// <summary>オブジェクトの指定されたメンバを取得し、結果を指定された型に変換します。メンバが存在しないか、書き込み専用の場合は例外を発生させます。</summary>
		/// <param name="obj">取得するメンバを保持しているオブジェクトを指定します。</param>
		/// <param name="name">取得するメンバの名前を指定します。</param>
		/// <param name="ignoreCase">メンバの検索に大文字と小文字を区別しないかどうかを示す値を指定します。</param>
		public T GetMember<T>(object obj, string name, bool ignoreCase) { return _ops.GetMember<T>(obj, name, ignoreCase); }
		
		/// <summary>オブジェクトの指定されたメンバを取得します。メンバが正常に取得された場合は true を返します。</summary>
		/// <param name="obj">取得するメンバを保持しているオブジェクトを指定します。</param>
		/// <param name="name">取得するメンバの名前を指定します。</param>
		/// <param name="ignoreCase">メンバの検索に大文字と小文字を区別しないかどうかを示す値を指定します。</param>
		/// <param name="value">取得したメンバの値を格納する変数を指定します。</param>
		public bool TryGetMember(object obj, string name, bool ignoreCase, out object value) { return _ops.TryGetMember(obj, name, ignoreCase, out value); }

		/// <summary>オブジェクトに指定されたメンバが存在するかどうかを示す値を返します。</summary>
		/// <param name="obj">メンバが存在するかどうかを調べるオブジェクトを指定します。</param>
		/// <param name="name">存在するかどうかを調べるメンバの名前を指定します。</param>
		/// <param name="ignoreCase">メンバの検索に大文字と小文字を区別しないかどうかを示す値を指定します。</param>
		public bool ContainsMember(object obj, string name, bool ignoreCase) { return _ops.ContainsMember(obj, name, ignoreCase); }

		/// <summary>オブジェクトから指定されたメンバを削除します。</summary>
		/// <param name="obj">メンバを削除するオブジェクトを指定します。</param>
		/// <param name="name">削除するメンバの名前を指定します。</param>
		/// <param name="ignoreCase">メンバの検索に大文字と小文字を区別しないかどうかを示す値を指定します。</param>
		public void RemoveMember(object obj, string name, bool ignoreCase) { _ops.RemoveMember(obj, name, ignoreCase); }

		/// <summary>オブジェクトの指定されたメンバに指定された値を設定します。</summary>
		/// <param name="obj">設定するメンバを保持しているオブジェクトを指定します。</param>
		/// <param name="name">設定するメンバの名前を指定します。</param>
		/// <param name="value">メンバに設定する値を指定します。</param>
		/// <param name="ignoreCase">メンバの検索に大文字と小文字を区別しないかどうかを示す値を指定します。</param>
		public void SetMember(object obj, string name, object value, bool ignoreCase) { _ops.SetMember(obj, name, value, ignoreCase); }

		/// <summary>オブジェクトの指定されたメンバに指定された値を設定します。このオーバーロードは厳密に型指定されているため、ボックス化やキャストを避けることができます。</summary>
		/// <param name="obj">設定するメンバを保持しているオブジェクトを指定します。</param>
		/// <param name="name">設定するメンバの名前を指定します。</param>
		/// <param name="value">メンバに設定する値を指定します。</param>
		/// <param name="ignoreCase">メンバの検索に大文字と小文字を区別しないかどうかを示す値を指定します。</param>
		public void SetMember<T>(object obj, string name, T value, bool ignoreCase) { _ops.SetMember<T>(obj, name, value, ignoreCase); }

		/// <summary>オブジェクトを指定された型に変換します。変換が明示的に行われるかどうかは言語仕様によって決定されます。</summary>
		/// <param name="obj">型を変換するオブジェクトを指定します。</param>
		public T ConvertTo<T>(object obj) { return _ops.ConvertTo<T>(obj); }

		/// <summary>オブジェクトを指定された型に変換します。変換が明示的に行われるかどうかは言語仕様によって決定されます。</summary>
		/// <param name="obj">型を変換するオブジェクトを指定します。</param>
		/// <param name="type">変換結果となる型を指定します。</param>
		public object ConvertTo(object obj, Type type)
		{
			ContractUtils.RequiresNotNull(type, "type");
			return _ops.ConvertTo(obj, type);
		}

		/// <summary>オブジェクトを指定された型に変換します。変換が成功した場合は true を返します。変換が明示的に行われるかどうかは言語仕様によって決定されます。</summary>
		/// <param name="obj">型を変換するオブジェクトを指定します。</param>
		/// <param name="result">変換されたオブジェクトを格納する変数を指定します。</param>
		public bool TryConvertTo<T>(object obj, out T result) { return _ops.TryConvertTo<T>(obj, out result); }

		/// <summary>オブジェクトを指定された型に変換します。変換が成功した場合は true を返します。変換が明示的に行われるかどうかは言語仕様によって決定されます。</summary>
		/// <param name="obj">型を変換するオブジェクトを指定します。</param>
		/// <param name="type">変換結果となる型を指定します。</param>
		/// <param name="result">変換されたオブジェクトを格納する変数を指定します。</param>
		public bool TryConvertTo(object obj, Type type, out object result) { return _ops.TryConvertTo(obj, type, out result); }

		/// <summary>オブジェクトを情報が欠落する可能性のある明示的変換を使用して指定された型に変換します。</summary>
		/// <param name="obj">型を変換するオブジェクトを指定します。</param>
		public T ExplicitConvertTo<T>(object obj) { return _ops.ExplicitConvertTo<T>(obj); }

		/// <summary>オブジェクトを情報が欠落する可能性のある明示的変換を使用して指定された型に変換します。</summary>
		/// <param name="obj">型を変換するオブジェクトを指定します。</param>
		/// <param name="type">変換結果となる型を指定します。</param>
		public object ExplicitConvertTo(object obj, Type type)
		{
			ContractUtils.RequiresNotNull(type, "type");
			return _ops.ExplicitConvertTo(obj, type);
		}

		/// <summary>オブジェクトを情報が欠落する可能性のある明示的変換を使用して指定された型に変換します。変換が成功した場合は true を返します。</summary>
		/// <param name="obj">型を変換するオブジェクトを指定します。</param>
		/// <param name="result">変換されたオブジェクトを格納する変数を指定します。</param>
		public bool TryExplicitConvertTo<T>(object obj, out T result) { return _ops.TryExplicitConvertTo<T>(obj, out result); }

		/// <summary>オブジェクトを情報が欠落する可能性のある明示的変換を使用して指定された型に変換します。変換が成功した場合は true を返します。</summary>
		/// <param name="obj">型を変換するオブジェクトを指定します。</param>
		/// <param name="type">変換結果となる型を指定します。</param>
		/// <param name="result">変換されたオブジェクトを格納する変数を指定します。</param>
		public bool TryExplicitConvertTo(object obj, Type type, out object result) { return _ops.TryExplicitConvertTo(obj, type, out result); }

		/// <summary>オブジェクトを指定された型に暗黙的に変換します。</summary>
		/// <param name="obj">型を変換するオブジェクトを指定します。</param>
		public T ImplicitConvertTo<T>(object obj) { return _ops.ImplicitConvertTo<T>(obj); }

		/// <summary>オブジェクトを指定された型に暗黙的に変換します。</summary>
		/// <param name="obj">型を変換するオブジェクトを指定します。</param>
		/// <param name="type">変換結果となる型を指定します。</param>
		public object ImplicitConvertTo(object obj, Type type)
		{
			ContractUtils.RequiresNotNull(type, "type");
			return _ops.ImplicitConvertTo(obj, type);
		}

		/// <summary>オブジェクトを指定された型に暗黙的に変換します。変換が成功した場合は true を返します。</summary>
		/// <param name="obj">型を変換するオブジェクトを指定します。</param>
		/// <param name="result">変換されたオブジェクトを格納する変数を指定します。</param>
		public bool TryImplicitConvertTo<T>(object obj, out T result) { return _ops.TryImplicitConvertTo<T>(obj, out result); }

		/// <summary>オブジェクトを指定された型に暗黙的に変換します。変換が成功した場合は true を返します。</summary>
		/// <param name="obj">型を変換するオブジェクトを指定します。</param>
		/// <param name="type">変換結果となる型を指定します。</param>
		/// <param name="result">変換されたオブジェクトを格納する変数を指定します。</param>
		public bool TryImplicitConvertTo(object obj, Type type, out object result) { return _ops.TryImplicitConvertTo(obj, type, out result); }

		/// <summary>汎用の単項演算を指定された対象に対して実行します。</summary>
		/// <param name="operation">単項演算の種類を示す <see cref="System.Linq.Expressions.ExpressionType"/> を指定します。</param>
		/// <param name="target">単項演算を作用させる対象を指定します。</param>
		public dynamic DoOperation(ExpressionType operation, object target) { return _ops.DoOperation<object, object>(operation, target); }

		/// <summary>汎用の単項演算を厳密に型指定された対象に対して実行します。</summary>
		/// <param name="operation">単項演算の種類を示す <see cref="System.Linq.Expressions.ExpressionType"/> を指定します。</param>
		/// <param name="target">単項演算を作用させる対象を指定します。</param>
		public TResult DoOperation<TTarget, TResult>(ExpressionType operation, TTarget target) { return _ops.DoOperation<TTarget, TResult>(operation, target); }

		/// <summary>汎用の二項演算を指定された対象に対して実行します。</summary>
		/// <param name="operation">二項演算の種類を示す <see cref="System.Linq.Expressions.ExpressionType"/> を指定します。</param>
		/// <param name="target">二項演算を作用させる左側の対象を指定します。</param>
		/// <param name="other">二項演算を作用させる右側の対象を指定します。</param>
		public dynamic DoOperation(ExpressionType operation, object target, object other) { return _ops.DoOperation<object, object, object>(operation, target, other); }

		/// <summary>汎用の二項演算を厳密に型指定された対象に対して実行します。</summary>
		/// <param name="operation">二項演算の種類を示す <see cref="System.Linq.Expressions.ExpressionType"/> を指定します。</param>
		/// <param name="target">二項演算を作用させる左側の対象を指定します。</param>
		/// <param name="other">二項演算を作用させる右側の対象を指定します。</param>
		public TResult DoOperation<TTarget, TOther, TResult>(ExpressionType operation, TTarget target, TOther other) { return _ops.DoOperation<TTarget, TOther, TResult>(operation, target, other); }

		/// <summary>指定されたオブジェクトに対して加算を実行します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">被加数を指定します。</param>
		/// <param name="other">加数を指定します。</param>
		public dynamic Add(object self, object other) { return DoOperation(ExpressionType.Add, self, other); }

		/// <summary>指定されたオブジェクトに対して減算を実行します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">被減数を指定します。</param>
		/// <param name="other">減数を指定します。</param>
		public dynamic Subtract(object self, object other) { return DoOperation(ExpressionType.Subtract, self, other); }

		/// <summary>指定されたオブジェクトに対して累乗を実行します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">底を指定します。</param>
		/// <param name="other">指数を指定します。</param>
		public dynamic Power(object self, object other) { return DoOperation(ExpressionType.Power, self, other); }

		/// <summary>指定されたオブジェクトに対して乗算を実行します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">被乗数を指定します。</param>
		/// <param name="other">乗数を指定します。</param>
		public dynamic Multiply(object self, object other) { return DoOperation(ExpressionType.Multiply, self, other); }

		/// <summary>指定されたオブジェクトに対して除算を実行します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">被除数を指定します。</param>
		/// <param name="other">除数を指定します。</param>
		public dynamic Divide(object self, object other) { return DoOperation(ExpressionType.Divide, self, other); }

		/// <summary>指定されたオブジェクトに対する剰余を取得します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">被除数を指定します。</param>
		/// <param name="other">除数を指定します。</param>
		public dynamic Modulo(object self, object other) { return DoOperation(ExpressionType.Modulo, self, other); }

		/// <summary>指定されたオブジェクトに対して左シフトを実行します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">左シフトするオブジェクトを指定します。</param>
		/// <param name="other">シフト量を指定します。</param>
		public dynamic LeftShift(object self, object other) { return DoOperation(ExpressionType.LeftShift, self, other); }

		/// <summary>指定されたオブジェクトに対して右シフトを実行します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">右シフトするオブジェクトを指定します。</param>
		/// <param name="other">シフト量を指定します。</param>
		public dynamic RightShift(object self, object other) { return DoOperation(ExpressionType.RightShift, self, other); }

		/// <summary>指定されたオブジェクトに対するビット積を取得します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">ビット積を取得する 1 番目のオブジェクトを指定します。</param>
		/// <param name="other">ビット積を取得する 2 番目のオブジェクトを指定します。</param>
		public dynamic BitwiseAnd(object self, object other) { return DoOperation(ExpressionType.And, self, other); }

		/// <summary>指定されたオブジェクトに対するビット和を取得します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">ビット和を取得する 1 番目のオブジェクトを指定します。</param>
		/// <param name="other">ビット和を取得する 2 番目のオブジェクトを指定します。</param>
		public dynamic BitwiseOr(object self, object other) { return DoOperation(ExpressionType.Or, self, other); }

		/// <summary>指定されたオブジェクトに対する排他的論理和を取得します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">排他的論理和を取得する 1 番目のオブジェクトを指定します。</param>
		/// <param name="other">排他的論理和を取得する 2 番目のオブジェクトを指定します。</param>
		public dynamic ExclusiveOr(object self, object other) { return DoOperation(ExpressionType.ExclusiveOr, self, other); }

		/// <summary>指定されたオブジェクトを比較して、左側のオブジェクトが右側のオブジェクト未満のときに true を返します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">比較する左側のオブジェクトを指定します。</param>
		/// <param name="other">比較する右側のオブジェクトを指定します。</param>
		public bool LessThan(object self, object other) { return ConvertTo<bool>(DoOperation<object, object, object>(ExpressionType.LessThan, self, other)); }

		/// <summary>指定されたオブジェクトを比較して、左側のオブジェクトが右側のオブジェクトよりも大きいときに true を返します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">比較する左側のオブジェクトを指定します。</param>
		/// <param name="other">比較する右側のオブジェクトを指定します。</param>
		public bool GreaterThan(object self, object other) { return ConvertTo<bool>(DoOperation<object, object, object>(ExpressionType.GreaterThan, self, other)); }

		/// <summary>指定されたオブジェクトを比較して、左側のオブジェクトが右側のオブジェクト以下のときに true を返します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">比較する左側のオブジェクトを指定します。</param>
		/// <param name="other">比較する右側のオブジェクトを指定します。</param>
		public bool LessThanOrEqual(object self, object other) { return ConvertTo<bool>(DoOperation<object, object, object>(ExpressionType.LessThanOrEqual, self, other)); }

		/// <summary>指定されたオブジェクトを比較して、左側のオブジェクトが右側のオブジェクト以上のときに true を返します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">比較する左側のオブジェクトを指定します。</param>
		/// <param name="other">比較する右側のオブジェクトを指定します。</param>
		public bool GreaterThanOrEqual(object self, object other) { return ConvertTo<bool>(DoOperation<object, object, object>(ExpressionType.GreaterThanOrEqual, self, other)); }

		/// <summary>指定されたオブジェクトを比較して、左側のオブジェクトが右側のオブジェクトと等しいときに true を返します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">比較する左側のオブジェクトを指定します。</param>
		/// <param name="other">比較する右側のオブジェクトを指定します。</param>
		public bool Equal(object self, object other) { return ConvertTo<bool>(DoOperation<object, object, object>(ExpressionType.Equal, self, other)); }

		/// <summary>指定されたオブジェクトを比較して、左側のオブジェクトが右側のオブジェクトと等しくないときに true を返します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">比較する左側のオブジェクトを指定します。</param>
		/// <param name="other">比較する右側のオブジェクトを指定します。</param>
		public bool NotEqual(object self, object other) { return ConvertTo<bool>(DoOperation<object, object, object>(ExpressionType.NotEqual, self, other)); }

		/// <summary>指定されたオブジェクトの文字列表現を言語固有の表示形式で返します。</summary>
		/// <param name="obj">文字列表現を取得するオブジェクトを指定します。</param>
		public string Format(object obj) { return _ops.Format(obj); }

		/// <summary>オブジェクトの既知のメンバの一覧を返します。</summary>
		/// <param name="obj">メンバの一覧を取得するオブジェクトを指定します。</param>
		public IList<string> GetMemberNames(object obj) { return _ops.GetMemberNames(obj); }

		/// <summary>指定されたオブジェクトに対する文字列で提供されるドキュメントを返します。</summary>
		/// <param name="obj">ドキュメントを取得するオブジェクトを指定します。</param>
		public string GetDocumentation(object obj) { return _ops.GetDocumentation(obj); }

		/// <summary>ユーザーに対する表示形式の指定されたオブジェクトの呼び出しに対して適用されるシグネチャのリストを返します。</summary>
		/// <param name="obj">シグネチャのリストを取得するオブジェクトを指定します。</param>
		public IList<string> GetCallSignatures(object obj) { return _ops.GetCallSignatures(obj); 	}

		#endregion

		#region Remote APIs

		/// <summary>指定されたリモートオブジェクトが呼び出し可能かどうかを示す値を取得します。</summary>
		/// <param name="obj">呼び出し可能かどうかを調べるリモートオブジェクトを指定します。</param>
		public bool IsCallable([NotNull]ObjectHandle obj) { return IsCallable(GetLocalObject(obj)); }

		/// <summary>指定されたリモートオブジェクトをリモートオブジェクトで表現された指定された引数によって呼び出します。</summary>
		/// <param name="obj">呼び出すリモートオブジェクトを指定します。</param>
		/// <param name="parameters">オブジェクト呼び出しのリモートオブジェクトで表現された引数を指定します。</param>
		public ObjectHandle Invoke([NotNull]ObjectHandle obj, params ObjectHandle[] parameters)
		{
			ContractUtils.RequiresNotNull(parameters, "parameters");
			return new ObjectHandle((object)Invoke(GetLocalObject(obj), GetLocalObjects(parameters)));
		}

		/// <summary>指定されたリモートオブジェクトを指定された引数によって呼び出します。</summary>
		/// <param name="obj">呼び出すリモートオブジェクトを指定します。</param>
		/// <param name="parameters">オブジェクト呼び出しの引数を指定します。</param>
		public ObjectHandle Invoke([NotNull]ObjectHandle obj, params object[] parameters) { return new ObjectHandle((object)Invoke(GetLocalObject(obj), parameters)); }

		/// <summary>指定されたリモートオブジェクトにリモートオブジェクトで表現された指定された引数を使用して新しいインスタンスを作成します。</summary>
		/// <param name="obj">インスタンスを作成する基になるリモートオブジェクトを指定します。</param>
		/// <param name="parameters">インスタンスの作成の際に必要になるリモートオブジェクトで表現された引数を指定します。</param>
		public ObjectHandle CreateInstance([NotNull]ObjectHandle obj, [NotNull]params ObjectHandle[] parameters) { return new ObjectHandle((object)CreateInstance(GetLocalObject(obj), GetLocalObjects(parameters))); }

		/// <summary>指定されたリモートオブジェクトに指定された引数を使用して新しいインスタンスを作成します。</summary>
		/// <param name="obj">インスタンスを作成する基になるリモートオブジェクトを指定します。</param>
		/// <param name="parameters">インスタンスの作成の際に必要になる引数を指定します。</param>
		public ObjectHandle CreateInstance([NotNull]ObjectHandle obj, params object[] parameters) { return new ObjectHandle((object)CreateInstance(GetLocalObject(obj), parameters)); }

		/// <summary>リモートオブジェクトの指定されたメンバにリモートオブジェクトによって指定された値を設定します。</summary>
		/// <param name="obj">設定するメンバを保持しているリモートオブジェクトを指定します。</param>
		/// <param name="name">設定するメンバの名前を指定します。</param>
		/// <param name="value">メンバに設定するリモートオブジェクトで表現された値を指定します。</param>
		public void SetMember([NotNull]ObjectHandle obj, string name, [NotNull]ObjectHandle value) { SetMember(GetLocalObject(obj), name, GetLocalObject(value)); }

		/// <summary>リモートオブジェクトの指定されたメンバに指定された値を設定します。このオーバーロードは厳密に型指定されているため、ボックス化やキャストを避けることができます。</summary>
		/// <param name="obj">設定するメンバを保持しているリモートオブジェクトを指定します。</param>
		/// <param name="name">設定するメンバの名前を指定します。</param>
		/// <param name="value">メンバに設定する値を指定します。</param>
		public void SetMember<T>([NotNull]ObjectHandle obj, string name, T value) { SetMember<T>(GetLocalObject(obj), name, value); }

		/// <summary>リモートオブジェクトの指定されたメンバを取得します。メンバが存在しないか、書き込み専用の場合は例外を発生させます。</summary>
		/// <param name="obj">取得するメンバを保持しているリモートオブジェクトを指定します。</param>
		/// <param name="name">取得するメンバの名前を指定します。</param>
		public ObjectHandle GetMember([NotNull]ObjectHandle obj, string name) { return new ObjectHandle((object)GetMember(GetLocalObject(obj), name)); }

		/// <summary>リモートオブジェクトの指定されたメンバを取得し、結果を指定された型に変換します。メンバが存在しないか、書き込み専用の場合は例外を発生させます。</summary>
		/// <param name="obj">取得するメンバを保持しているリモートオブジェクトを指定します。</param>
		/// <param name="name">取得するメンバの名前を指定します。</param>
		public T GetMember<T>([NotNull]ObjectHandle obj, string name) { return GetMember<T>(GetLocalObject(obj), name); }

		/// <summary>リモートオブジェクトの指定されたメンバを取得します。メンバが正常に取得された場合は true を返します。</summary>
		/// <param name="obj">取得するメンバを保持しているリモートオブジェクトを指定します。</param>
		/// <param name="name">取得するメンバの名前を指定します。</param>
		/// <param name="value">取得したメンバの値を格納する変数を指定します。</param>
		public bool TryGetMember([NotNull]ObjectHandle obj, string name, out ObjectHandle value)
		{
			object val;
			if (TryGetMember(GetLocalObject(obj), name, out val))
			{
				value = new ObjectHandle(val);
				return true;
			}
			value = null;
			return false;
		}

		/// <summary>リモートオブジェクトに指定されたメンバが存在するかどうかを示す値を返します。</summary>
		/// <param name="obj">メンバが存在するかどうかを調べるリモートオブジェクトを指定します。</param>
		/// <param name="name">存在するかどうかを調べるメンバの名前を指定します。</param>
		public bool ContainsMember([NotNull]ObjectHandle obj, string name) { return ContainsMember(GetLocalObject(obj), name); }

		/// <summary>リモートオブジェクトから指定されたメンバを削除します。</summary>
		/// <param name="obj">メンバを削除するリモートオブジェクトを指定します。</param>
		/// <param name="name">削除するメンバの名前を指定します。</param>
		public void RemoveMember([NotNull]ObjectHandle obj, string name) { RemoveMember(GetLocalObject(obj), name); }

		/// <summary>リモートオブジェクトを指定された型に変換します。変換が明示的に行われるかどうかは言語仕様によって決定されます。</summary>
		/// <param name="obj">型を変換するリモートオブジェクトを指定します。</param>
		/// <param name="type">変換結果となる型を指定します。</param>
		public ObjectHandle ConvertTo([NotNull]ObjectHandle obj, Type type) { return new ObjectHandle(ConvertTo(GetLocalObject(obj), type)); }

		/// <summary>リモートオブジェクトを指定された型に変換します。変換が成功した場合は true を返します。変換が明示的に行われるかどうかは言語仕様によって決定されます。</summary>
		/// <param name="obj">型を変換するリモートオブジェクトを指定します。</param>
		/// <param name="type">変換結果となる型を指定します。</param>
		/// <param name="result">変換されたリモートオブジェクトを格納する変数を指定します。</param>
		public bool TryConvertTo([NotNull]ObjectHandle obj, Type type, out ObjectHandle result)
		{
			object resultObj;
			if (TryConvertTo(GetLocalObject(obj), type, out resultObj))
			{
				result = new ObjectHandle(resultObj);
				return true;
			}
			result = null;
			return false;
		}

		/// <summary>リモートオブジェクトを情報が欠落する可能性のある明示的変換を使用して指定された型に変換します。</summary>
		/// <param name="obj">型を変換するリモートオブジェクトを指定します。</param>
		/// <param name="type">変換結果となる型を指定します。</param>
		public ObjectHandle ExplicitConvertTo([NotNull]ObjectHandle obj, Type type)
		{
			ContractUtils.RequiresNotNull(type, "type");
			return new ObjectHandle(_ops.ExplicitConvertTo(GetLocalObject(obj), type));
		}

		/// <summary>リモートオブジェクトを情報が欠落する可能性のある明示的変換を使用して指定された型に変換します。変換が成功した場合は true を返します。</summary>
		/// <param name="obj">型を変換するリモートオブジェクトを指定します。</param>
		/// <param name="type">変換結果となる型を指定します。</param>
		/// <param name="result">変換されたリモートオブジェクトを格納する変数を指定します。</param>
		public bool TryExplicitConvertTo([NotNull]ObjectHandle obj, Type type, out ObjectHandle result)
		{
			object outp;
			bool res = _ops.TryExplicitConvertTo(GetLocalObject(obj), type, out outp);
			if (res)
				result = new ObjectHandle(outp);
			else
				result = null;
			return res;
		}

		/// <summary>リモートオブジェクトを指定された型に暗黙的に変換します。</summary>
		/// <param name="obj">型を変換するリモートオブジェクトを指定します。</param>
		/// <param name="type">変換結果となる型を指定します。</param>
		public ObjectHandle ImplicitConvertTo([NotNull]ObjectHandle obj, Type type)
		{
			ContractUtils.RequiresNotNull(type, "type");
			return new ObjectHandle(_ops.ImplicitConvertTo(GetLocalObject(obj), type));
		}

		/// <summary>リモートオブジェクトを指定された型に暗黙的に変換します。変換が成功した場合は true を返します。</summary>
		/// <param name="obj">型を変換するリモートオブジェクトを指定します。</param>
		/// <param name="type">変換結果となる型を指定します。</param>
		/// <param name="result">変換されたリモートオブジェクトを格納する変数を指定します。</param>
		public bool TryImplicitConvertTo([NotNull]ObjectHandle obj, Type type, out ObjectHandle result)
		{
			object outp;
			bool res = _ops.TryImplicitConvertTo(GetLocalObject(obj), type, out outp);
			if (res)
				result = new ObjectHandle(outp);
			else
				result = null;
			return res;
		}

		/// <summary>リモートオブジェクトのラッピングを解除し、指定された型に変換します。</summary>
		/// <param name="obj">ラッピングを解除するリモートオブジェクトを指定します。</param>
		public T Unwrap<T>([NotNull]ObjectHandle obj) { return ConvertTo<T>(GetLocalObject(obj)); }

		/// <summary>汎用の単項演算を指定された対象に対して実行します。</summary>
		/// <param name="op">単項演算の種類を示す <see cref="System.Linq.Expressions.ExpressionType"/> を指定します。</param>
		/// <param name="target">単項演算を作用させる対象を指定します。</param>
		public ObjectHandle DoOperation(ExpressionType op, [NotNull]ObjectHandle target) { return new ObjectHandle((object)DoOperation(op, GetLocalObject(target))); }

		/// <summary>汎用の二項演算を指定された対象に対して実行します。</summary>
		/// <param name="op">二項演算の種類を示す <see cref="System.Linq.Expressions.ExpressionType"/> を指定します。</param>
		/// <param name="target">二項演算を作用させる左側の対象を指定します。</param>
		/// <param name="other">二項演算を作用させる右側の対象を指定します。</param>
		public ObjectHandle DoOperation(ExpressionType op, ObjectHandle target, ObjectHandle other) { return new ObjectHandle((object)DoOperation(op, GetLocalObject(target), GetLocalObject(other))); }

		/// <summary>指定されたリモートオブジェクトに対して加算を実行します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">被加数を指定します。</param>
		/// <param name="other">加数を指定します。</param>
		public ObjectHandle Add([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return new ObjectHandle((object)Add(GetLocalObject(self), GetLocalObject(other))); }

		/// <summary>指定されたリモートオブジェクトに対して減算を実行します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">被減数を指定します。</param>
		/// <param name="other">減数を指定します。</param>
		public ObjectHandle Subtract([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return new ObjectHandle((object)Subtract(GetLocalObject(self), GetLocalObject(other))); }

		/// <summary>指定されたリモートオブジェクトに対して累乗を実行します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">底を指定します。</param>
		/// <param name="other">指数を指定します。</param>
		public ObjectHandle Power([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return new ObjectHandle((object)Power(GetLocalObject(self), GetLocalObject(other))); }

		/// <summary>指定されたリモートオブジェクトに対して乗算を実行します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">被乗数を指定します。</param>
		/// <param name="other">乗数を指定します。</param>
		public ObjectHandle Multiply([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return new ObjectHandle((object)Multiply(GetLocalObject(self), GetLocalObject(other))); }

		/// <summary>指定されたリモートオブジェクトに対して除算を実行します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">被除数を指定します。</param>
		/// <param name="other">除数を指定します。</param>
		public ObjectHandle Divide([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return new ObjectHandle((object)Divide(GetLocalObject(self), GetLocalObject(other))); }

		/// <summary>指定されたリモートオブジェクトに対する剰余を取得します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">被除数を指定します。</param>
		/// <param name="other">除数を指定します。</param>      
		public ObjectHandle Modulo([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return new ObjectHandle((object)Modulo(GetLocalObject(self), GetLocalObject(other))); }

		/// <summary>指定されたリモートオブジェクトに対して左シフトを実行します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">左シフトするリモートオブジェクトを指定します。</param>
		/// <param name="other">シフト量を指定します。</param>
		public ObjectHandle LeftShift([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return new ObjectHandle((object)LeftShift(GetLocalObject(self), GetLocalObject(other))); }

		/// <summary>指定されたリモートオブジェクトに対して右シフトを実行します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">右シフトするリモートオブジェクトを指定します。</param>
		/// <param name="other">シフト量を指定します。</param>
		public ObjectHandle RightShift([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return new ObjectHandle((object)RightShift(GetLocalObject(self), GetLocalObject(other))); }

		/// <summary>指定されたリモートオブジェクトに対するビット積を取得します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">ビット積を取得する 1 番目のリモートオブジェクトを指定します。</param>
		/// <param name="other">ビット積を取得する 2 番目のリモートオブジェクトを指定します。</param>
		public ObjectHandle BitwiseAnd([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return new ObjectHandle((object)BitwiseAnd(GetLocalObject(self), GetLocalObject(other))); }

		/// <summary>指定されたリモートオブジェクトに対するビット和を取得します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">ビット和を取得する 1 番目のリモートオブジェクトを指定します。</param>
		/// <param name="other">ビット和を取得する 2 番目のリモートオブジェクトを指定します。</param>
		public ObjectHandle BitwiseOr([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return new ObjectHandle((object)BitwiseOr(GetLocalObject(self), GetLocalObject(other))); }

		/// <summary>指定されたリモートオブジェクトに対する排他的論理和を取得します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">排他的論理和を取得する 1 番目のリモートオブジェクトを指定します。</param>
		/// <param name="other">排他的論理和を取得する 2 番目のリモートオブジェクトを指定します。</param>
		public ObjectHandle ExclusiveOr([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return new ObjectHandle((object)ExclusiveOr(GetLocalObject(self), GetLocalObject(other))); }

		/// <summary>指定されたリモートオブジェクトを比較して、左側のリモートオブジェクトが右側のリモートオブジェクト未満のときに true を返します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">比較する左側のリモートオブジェクトを指定します。</param>
		/// <param name="other">比較する右側のリモートオブジェクトを指定します。</param>
		public bool LessThan([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return LessThan(GetLocalObject(self), GetLocalObject(other)); }

		/// <summary>指定されたリモートオブジェクトを比較して、左側のリモートオブジェクトが右側のリモートオブジェクトよりも大きいときに true を返します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">比較する左側のリモートオブジェクトを指定します。</param>
		/// <param name="other">比較する右側のリモートオブジェクトを指定します。</param>
		public bool GreaterThan([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return GreaterThan(GetLocalObject(self), GetLocalObject(other)); }

		/// <summary>指定されたリモートオブジェクトを比較して、左側のリモートオブジェクトが右側のリモートオブジェクト以下のときに true を返します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">比較する左側のリモートオブジェクトを指定します。</param>
		/// <param name="other">比較する右側のリモートオブジェクトを指定します。</param>
		public bool LessThanOrEqual([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return LessThanOrEqual(GetLocalObject(self), GetLocalObject(other)); }

		/// <summary>指定されたリモートオブジェクトを比較して、左側のリモートオブジェクトが右側のリモートオブジェクト以上のときに true を返します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">比較する左側のリモートオブジェクトを指定します。</param>
		/// <param name="other">比較する右側のリモートオブジェクトを指定します。</param>
		public bool GreaterThanOrEqual([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return GreaterThanOrEqual(GetLocalObject(self), GetLocalObject(other)); }

		/// <summary>指定されたリモートオブジェクトを比較して、左側のリモートオブジェクトが右側のリモートオブジェクトと等しいときに true を返します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">比較する左側のリモートオブジェクトを指定します。</param>
		/// <param name="other">比較する右側のリモートオブジェクトを指定します。</param>
		public bool Equal([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return Equal(GetLocalObject(self), GetLocalObject(other)); }

		/// <summary>指定されたリモートオブジェクトを比較して、左側のリモートオブジェクトが右側のリモートオブジェクトと等しくないときに true を返します。操作が実行できない場合には例外を発生させます。</summary>
		/// <param name="self">比較する左側のリモートオブジェクトを指定します。</param>
		/// <param name="other">比較する右側のリモートオブジェクトを指定します。</param>
		public bool NotEqual([NotNull]ObjectHandle self, [NotNull]ObjectHandle other) { return NotEqual(GetLocalObject(self), GetLocalObject(other)); }

		/// <summary>指定されたリモートオブジェクトの文字列表現を言語固有の表示形式で返します。</summary>
		/// <param name="obj">文字列表現を取得するリモートオブジェクトを指定します。</param>
		public string Format([NotNull]ObjectHandle obj) { return Format(GetLocalObject(obj)); }

		/// <summary>リモートオブジェクトの既知のメンバの一覧を返します。</summary>
		/// <param name="obj">メンバの一覧を取得するリモートオブジェクトを指定します。</param>
		public IList<string> GetMemberNames([NotNull]ObjectHandle obj) { return GetMemberNames(GetLocalObject(obj)); }

		/// <summary>指定されたリモートオブジェクトに対する文字列で提供されるドキュメントを返します。</summary>
		/// <param name="obj">ドキュメントを取得するリモートオブジェクトを指定します。</param>
		public string GetDocumentation([NotNull]ObjectHandle obj) { return GetDocumentation(GetLocalObject(obj)); }

		/// <summary>ユーザーに対する表示形式の指定されたリモートオブジェクトの呼び出しに対して適用されるシグネチャのリストを返します。</summary>
		/// <param name="obj">シグネチャのリストを取得するリモートオブジェクトを指定します。</param>
		public IList<string> GetCallSignatures([NotNull]ObjectHandle obj) { return GetCallSignatures(GetLocalObject(obj)); }

		static object GetLocalObject([NotNull]ObjectHandle obj)
		{
			ContractUtils.RequiresNotNull(obj, "obj");
			return obj.Unwrap();
		}

		static object[] GetLocalObjects(ObjectHandle[] ohs)
		{
			Debug.Assert(ohs != null);
			return ohs.Select(o => GetLocalObject(o)).ToArray();
		}

		#endregion

		/// <summary>対象のインスタンスの有効期間ポリシーを制御する、有効期間サービス オブジェクトを取得します。</summary>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
		public override object InitializeLifetimeService() { return null; }
	}
}
