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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq.Expressions;
using Microsoft.Scripting.Utils;

[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "System.Dynamic")]

namespace Microsoft.Scripting.ComInterop
{
	/// <summary>動的に COM オブジェクトにバインドするためのヘルパー メソッドを提供します。</summary>
	public static class ComBinder
	{
		/// <summary>指定されたオブジェクトが COM オブジェクトかどうかを判断します。</summary>
		/// <param name="value">調べるオブジェクトを指定します。</param>
		/// <returns>オブジェクトが COM オブジェクトの場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool IsComObject(object value) { return ComObject.IsComObject(value); }

		/// <summary>指定されたオブジェクトに対する COM バインディングが可能かどうかを判断します。</summary>
		/// <param name="value">調べるオブジェクトを指定します。</param>
		/// <returns>オブジェクトに COM バインディング可能な場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool CanComBind(object value) { return IsComObject(value) || value is IPseudoComObject; }

		/// <summary>動的メンバ取得操作のバインディングの実行を試みます。</summary>
		/// <param name="binder">動的操作の詳細を表す <see cref="GetMemberBinder"/> のインスタンスを指定します。</param>
		/// <param name="instance">動的操作のターゲットを指定します。</param>
		/// <param name="result">バインディングの結果を表す新しい <see cref="DynamicMetaObject"/> が格納される変数を指定します。</param>
		/// <param name="delayInvocation">メンバ評価の遅延を許すかどうかを示す値を指定します。</param>
		/// <returns>操作が正常にバインドされた場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool TryBindGetMember(GetMemberBinder binder, DynamicMetaObject instance, out DynamicMetaObject result, bool delayInvocation)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			ContractUtils.RequiresNotNull(instance, "instance");
			if (TryGetMetaObject(ref instance))
			{
				var comGetMember = new ComGetMemberBinder(binder, delayInvocation);
				result = instance.BindGetMember(comGetMember);
				if (result.Expression.Type.IsValueType)
					result = new DynamicMetaObject(Expression.Convert(result.Expression, typeof(object)), result.Restrictions);
				return true;
			}
			result = null;
			return false;
		}

		/// <summary>動的メンバ取得操作のバインディングの実行を試みます。</summary>
		/// <param name="binder">動的操作の詳細を表す <see cref="GetMemberBinder"/> のインスタンスを指定します。</param>
		/// <param name="instance">動的操作のターゲットを指定します。</param>
		/// <param name="result">バインディングの結果を表す新しい <see cref="DynamicMetaObject"/> が格納される変数を指定します。</param>
		/// <returns>操作が正常にバインドされた場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool TryBindGetMember(GetMemberBinder binder, DynamicMetaObject instance, out DynamicMetaObject result) { return TryBindGetMember(binder, instance, out result, false); }

		/// <summary>動的メンバ設定操作のバインディングの実行を試みます。</summary>
		/// <param name="binder">動的操作の詳細を表す <see cref="SetMemberBinder"/> のインスタンスを指定します。</param>
		/// <param name="instance">動的操作のターゲットを指定します。</param>
		/// <param name="value">メンバ設定操作の値を表す <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <param name="result">バインディングの結果を表す新しい <see cref="DynamicMetaObject"/> が格納される変数を指定します。</param>
		/// <returns>操作が正常にバインドされた場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool TryBindSetMember(SetMemberBinder binder, DynamicMetaObject instance, DynamicMetaObject value, out DynamicMetaObject result)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(value, "value");
			if (TryGetMetaObject(ref instance))
			{
				result = instance.BindSetMember(binder, value);
				return true;
			}
			result = null;
			return false;
		}

		/// <summary>動的呼び出し操作のバインディングの実行を試みます。</summary>    
		/// <param name="binder">動的操作の詳細を表す <see cref="InvokeBinder"/> のインスタンスを指定します。</param>
		/// <param name="instance">動的操作のターゲットを指定します。</param>
		/// <param name="args">呼び出し操作の引数を表す <see cref="DynamicMetaObject"/> インスタンスの配列を指定します。</param>
		/// <param name="result">バインディングの結果を表す新しい <see cref="DynamicMetaObject"/> が格納される変数を指定します。</param>
		/// <returns>操作が正常にバインドされた場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool TryBindInvoke(InvokeBinder binder, DynamicMetaObject instance, DynamicMetaObject[] args, out DynamicMetaObject result)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(args, "args");
			if (TryGetMetaObjectInvoke(ref instance))
			{
				result = instance.BindInvoke(binder, args);
				return true;
			}
			result = null;
			return false;
		}

		/// <summary>動的メンバ呼び出し操作のバインディングの実行を試みます。</summary>
		/// <param name="binder">動的操作の詳細を表す <see cref="InvokeMemberBinder"/> のインスタンスを指定します。</param>
		/// <param name="instance">動的操作のターゲットを指定します。</param>
		/// <param name="args">メンバ呼び出し操作の引数を表す <see cref="DynamicMetaObject"/> インスタンスの配列を指定します。</param>
		/// <param name="result">バインディングの結果を表す新しい <see cref="DynamicMetaObject"/> が格納される変数を指定します。</param>
		/// <returns>操作が正常にバインドされた場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool TryBindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject instance, DynamicMetaObject[] args, out DynamicMetaObject result)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(args, "args");
			if (TryGetMetaObject(ref instance))
			{
				result = instance.BindInvokeMember(binder, args);
				return true;
			}
			result = null;
			return false;
		}

		/// <summary>動的インデックス取得操作のバインディングの実行を試みます。</summary>
		/// <param name="binder">動的操作の詳細を表す <see cref="GetIndexBinder"/> のインスタンスを指定します。</param>
		/// <param name="instance">動的操作のターゲットを指定します。</param>
		/// <param name="args">インデックス取得操作の引数を表す <see cref="DynamicMetaObject"/> インスタンスの配列を指定します。</param>
		/// <param name="result">バインディングの結果を表す新しい <see cref="DynamicMetaObject"/> が格納される変数を指定します。</param>
		/// <returns>操作が正常にバインドされた場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool TryBindGetIndex(GetIndexBinder binder, DynamicMetaObject instance, DynamicMetaObject[] args, out DynamicMetaObject result)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(args, "args");
			if (TryGetMetaObjectInvoke(ref instance))
			{
				result = instance.BindGetIndex(binder, args);
				return true;
			}
			result = null;
			return false;
		}

		/// <summary>動的インデックス設定操作のバインディングの実行を試みます。</summary>
		/// <param name="binder">動的操作の詳細を表す <see cref="SetIndexBinder"/> のインスタンスを指定します。</param>
		/// <param name="instance">動的操作のターゲットを指定します。</param>
		/// <param name="args">インデックス設定操作の引数を表す <see cref="DynamicMetaObject"/> インスタンスの配列を指定します。</param>
		/// <param name="value">インデックス設定操作の値を表す <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <param name="result">バインディングの結果を表す新しい <see cref="DynamicMetaObject"/> が格納される変数を指定します。</param>
		/// <returns>操作が正常にバインドされた場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool TryBindSetIndex(SetIndexBinder binder, DynamicMetaObject instance, DynamicMetaObject[] args, DynamicMetaObject value, out DynamicMetaObject result)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			ContractUtils.RequiresNotNull(instance, "instance");
			ContractUtils.RequiresNotNull(args, "args");
			ContractUtils.RequiresNotNull(value, "value");
			if (TryGetMetaObjectInvoke(ref instance))
			{
				result = instance.BindSetIndex(binder, args, value);
				return true;
			}
			result = null;
			return false;
		}

		/// <summary>動的変換操作のバインディングの実行を試みます。</summary>
		/// <param name="binder">動的操作の詳細を表す <see cref="ConvertBinder"/> のインスタンスを指定します。</param>
		/// <param name="instance">動的操作のターゲットを指定します。</param>
		/// <param name="result">バインディングの結果を表す新しい <see cref="DynamicMetaObject"/> が格納される変数を指定します。</param>
		/// <returns>操作が正常にバインドされた場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool TryConvert(ConvertBinder binder, DynamicMetaObject instance, out DynamicMetaObject result)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			ContractUtils.RequiresNotNull(instance, "instance");
			// COM オブジェクトのあらゆるインターフェイスへの変換は常に可能だと考えます。実行時の QueryInterface が結果になります。
			if (IsComObject(instance.Value) && binder.Type.IsInterface)
			{
				result = new DynamicMetaObject(Expression.Convert(instance.Expression, binder.Type),
					BindingRestrictions.GetExpressionRestriction(
						Expression.Call(
							typeof(ComObject).GetMethod("IsComObject", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic),
							Ast.Utils.Convert(instance.Expression, typeof(object))
						)
					)
				);
				return true;
			}
			result = null;
			return false;
		}

		/// <summary>
		/// 指定されたオブジェクトに関連付けられたメンバ名を取得します。
		/// このメソッドは <see cref="IsComObject"/> が <c>true</c> を返すオブジェクトに対してのみ動作します。
		/// </summary>
		/// <param name="value">メンバ名を要求するオブジェクトを指定します。</param>
		/// <returns>メンバ名のコレクション。</returns>
		public static IEnumerable<string> GetDynamicMemberNames(object value)
		{
			ContractUtils.RequiresNotNull(value, "value");
			ContractUtils.Requires(IsComObject(value), "value", Strings.ComObjectExpected);
			return ComObject.ObjectToComObject(value).GetMemberNames(false);
		}

		/// <summary>
		/// 指定されたオブジェクトに関連付けられたデータ形式のメンバ名を取得します。
		/// このメソッドは <see cref="IsComObject"/> が <c>true</c> を返すオブジェクトに対してのみ動作します。
		/// </summary>
		/// <param name="value">メンバ名を要求するオブジェクトを指定します。</param>
		/// <returns>メンバ名のコレクション。</returns>
		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		internal static IList<string> GetDynamicDataMemberNames(object value)
		{
			ContractUtils.RequiresNotNull(value, "value");
			ContractUtils.Requires(IsComObject(value), "value", Strings.ComObjectExpected);
			return ComObject.ObjectToComObject(value).GetMemberNames(true);
		}

		/// <summary>
		/// オブジェクトに対するデータ形式のメンバと関連付けられたオブジェクトを返します。
		/// このメソッドは <see cref="IsComObject"/> が <c>true</c> を返すオブジェクトに対してのみ動作します。
		/// </summary>
		/// <param name="value">データメンバを要求するオブジェクトを指定します。</param>
		/// <param name="names">値を取得するデータメンバの名前を指定します。</param>
		/// <returns>データメンバの名前と値のペアのコレクション。</returns>
		[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		internal static IList<KeyValuePair<string, object>> GetDynamicDataMembers(object value, IEnumerable<string> names)
		{
			ContractUtils.RequiresNotNull(value, "value");
			ContractUtils.Requires(IsComObject(value), "value", Strings.ComObjectExpected);
			return ComObject.ObjectToComObject(value).GetMembers(names);
		}

		static bool TryGetMetaObject(ref DynamicMetaObject instance)
		{
			// すでに COM DynamicMetaObject の場合は新しいものを作らない。
			// (COM からのフォールバックを呼び出す場合に、再帰を防ぐためにこれを行う。)
			if (instance is ComUnwrappedMetaObject)
				return false;
			if (IsComObject(instance.Value))
			{
				instance = new ComMetaObject(instance.Expression, instance.Restrictions, instance.Value);
				return true;
			}
			return false;
		}

		static bool TryGetMetaObjectInvoke(ref DynamicMetaObject instance)
		{
			// すでに COM DynamicMetaObject の場合は新しいものを作らない。
			// (COM からのフォールバックを呼び出す場合に、再帰を防ぐためにこれを行う。)
			if (TryGetMetaObject(ref instance))
				return true;
			if (instance.Value is IPseudoComObject)
			{
				instance = ((IPseudoComObject)instance.Value).GetMetaObject(instance.Expression);
				return true;
			}
			return false;
		}

		/// <summary>COM メンバ取得操作の特別なセマンティクスを示すバインダーです。</summary>
		internal class ComGetMemberBinder : GetMemberBinder
		{
			readonly GetMemberBinder _originalBinder;
			internal bool _CanReturnCallables;

			internal ComGetMemberBinder(GetMemberBinder originalBinder, bool CanReturnCallables) : base(originalBinder.Name, originalBinder.IgnoreCase)
			{
				_originalBinder = originalBinder;
				_CanReturnCallables = CanReturnCallables;
			}

			public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion) { return _originalBinder.FallbackGetMember(target, errorSuggestion); }

			public override int GetHashCode() { return _originalBinder.GetHashCode() ^ (_CanReturnCallables ? 1 : 0); }

			public override bool Equals(object obj)
			{
				var other = obj as ComGetMemberBinder;
				return other != null && _CanReturnCallables == other._CanReturnCallables && _originalBinder.Equals(other._originalBinder);
			}
		}
	}
}