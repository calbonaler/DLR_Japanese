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
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>メソッドを呼び出すことのできるインタスタンスを提供します。</summary>
	public class InstanceBuilder
	{
		// Index of actual argument expression or -1 if the instance is null.
		int _index;

		/// <summary>インスタンスを表す実引数のインデックスを使用して、<see cref="Microsoft.Scripting.Actions.Calls.InstanceBuilder"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="index">インスタンスを表す引数リスト内の実引数のインデックスを指定します。</param>
		public InstanceBuilder(int index)
		{
			ContractUtils.Requires(index >= -1, "index");
			_index = index;
		}

		/// <summary>このインスタンスが <c>null</c> であるかどうかを示す値を取得します。</summary>
		public virtual bool HasValue { get { return _index != -1; } }

		/// <summary>このビルダによって消費される実際の引数の数を取得します。</summary>
		public virtual int ConsumedArgumentCount { get { return 1; } }

		/// <summary>インスタンスの値を提供する <see cref="Expression"/> を返します。</summary>
		/// <param name="method">呼び出すメソッドを示す <see cref="MethodInfo"/> を指定します。</param>
		/// <param name="resolver">メソッドに対するオーバーロードを解決するために使用される <see cref="OverloadResolver"/> を指定します。</param>
		/// <param name="args">制約された引数を指定します。</param>
		/// <param name="hasBeenUsed">呼び出しが完了すると使用された引数に対応する位置に <c>true</c> が格納されます。</param>
		/// <returns>インスタンスの値を提供する <see cref="Expression"/>。</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")] // TODO
		protected internal virtual Expression ToExpression(ref MethodInfo method, OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed)
		{
			if (_index == -1)
				return AstUtils.Constant(null);
			ContractUtils.Requires(hasBeenUsed.Length == args.Length, "hasBeenUsed");
			ContractUtils.Requires(_index < args.Length, "args");
			ContractUtils.Requires(!hasBeenUsed[_index], "hasBeenUsed");
			hasBeenUsed[_index] = true;
			return resolver.Convert(args.Objects[_index], args.Types[_index], null, (method = GetCallableMethod(args, method)).DeclaringType);
		}

		/// <summary>インスタンスの値を提供するデリゲートを返します。</summary>
		/// <param name="method">呼び出すメソッドを示す <see cref="MethodInfo"/> を指定します。</param>
		/// <param name="resolver">メソッドに対するオーバーロードを解決するために使用される <see cref="OverloadResolver"/> を指定します。</param>
		/// <param name="args">制約された引数を指定します。</param>
		/// <param name="hasBeenUsed">呼び出しが完了すると使用された引数に対応する位置に <c>true</c> が格納されます。</param>
		/// <returns>インスタンスの値を提供するデリゲート。</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")] // TODO
		protected internal virtual Func<object[], object> ToDelegate(ref MethodInfo method, OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed)
		{
			if (_index == -1)
				return _ => null;
			var conv = resolver.GetConvertor(_index + 1, args.Objects[_index], null, (method = GetCallableMethod(args, method)).DeclaringType);
			if (conv != null)
				return conv;
			return (Func<object[], object>)Delegate.CreateDelegate(typeof(Func<object[], object>), _index + 1, new Func<object, object[], object>(ArgBuilder.ArgumentRead).Method);
		}

		MethodInfo GetCallableMethod(RestrictedArguments args, MethodInfo method)
		{
			// 不可視のメソッドを参照しているならば、同じものを呼び出すことができる可視であるよりよいメソッドの検索を試みます。
			// これが失敗した場合、とにかくバインドを行います。つまり、不可視のメソッドをフィルタするのは呼び出し側の責任となります。
			// 型に実装されたインターフェイスを通してアクセスが可能とされたメソッドにアクセスできるよう、これはメタインスタンスの制限型を使用します。
			// その他の場合、型が内部型であったり、メソッドがアクセス不可能であったりする可能性があります。
			return CompilerHelpers.TryGetCallableMethod(args.Objects[_index].LimitType, method);
		}
	}
}
