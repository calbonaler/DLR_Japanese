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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>既定の DLR バインダーから呼び出しが生成されたヘルパーメソッドを格納します。</summary>
	public static class BinderOps
	{
		/// <summary>指定された名前と値の配列の同じ位置の要素から <see cref="SymbolDictionary"/> を作成します。</summary>
		/// <param name="names"><see cref="SymbolDictionary"/> に格納する名前を含んでいる配列を指定します。</param>
		/// <param name="values"><see cref="SymbolDictionary"/> に格納する値を含んでいる配列を指定します。</param>
		/// <returns>指定された名前と値の配列の同じ位置の要素から作成された <see cref="SymbolDictionary"/>。</returns>
		public static SymbolDictionary MakeSymbolDictionary(string[] names, object[] values)
		{
			SymbolDictionary res = new SymbolDictionary();
			for (int i = 0; i < names.Length; i++)
				res[names[i]] = values[i];
			return res;
		}

		/// <summary>指定された名前と値の配列の同じ位置の要素から <see cref="Dictionary&lt;TKey, TValue&gt;"/> を作成します。</summary>
		/// <typeparam name="TKey">作成するディクショナリのキーの型を指定します。<see cref="String"/> または <see cref="Object"/> である必要があります。</typeparam>
		/// <typeparam name="TValue">作成するディクショナリの値の型を指定します。</typeparam>
		/// <param name="names"><see cref="Dictionary&lt;TKey, TValue&gt;"/> に格納する名前を含んでいる配列を指定します。</param>
		/// <param name="values"><see cref="Dictionary&lt;TKey, TValue&gt;"/> に格納する値を含んでいる配列を指定します。</param>
		/// <returns>指定された名前と値の配列の同じ位置の要素から作成された <see cref="Dictionary&lt;TKey, TValue&gt;"/>。</returns>
		public static Dictionary<TKey, TValue> MakeDictionary<TKey, TValue>(string[] names, object[] values)
		{
			Debug.Assert(typeof(TKey) == typeof(string) || typeof(TKey) == typeof(object));
			return names.Zip(values, (x, y) => new KeyValuePair<TKey, TValue>((TKey)(object)x, (TValue)y)).ToDictionary(x => x.Key, x => x.Value);
		}

		/// <summary>指定された操作がオペランドの型が不正であるために失敗したことを示す例外を返します。</summary>
		/// <param name="op">失敗した操作を指定します。</param>
		/// <param name="args">操作の引数を指定します。</param>
		/// <returns>操作がオペランドの型が不正であるために失敗したことを示す <see cref="ArgumentTypeException"/>。</returns>
		public static ArgumentTypeException BadArgumentsForOperation(ExpressionType op, params object[] args) { throw new ArgumentTypeException("演算 " + op.ToString() + " でサポートされていないオペランド型: " + string.Join(", ", args.Select(x => CompilerHelpers.GetType(x)))); }

		/// <summary>引数の数が正しくないために失敗したことを示す例外を返します。</summary>
		/// <param name="methodName">不正な数の引数が渡されたメソッドの名前を指定します。</param>
		/// <param name="formalNormalArgumentCount">配列引数およびキーワード引数を含まない引数の数を指定します。</param>
		/// <param name="defaultArgumentCount">メソッド宣言の中で既定値のある引数の数を指定します。</param>
		/// <param name="providedArgumentCount">呼び出しサイトで渡された引数の数を指定します。</param>
		/// <param name="hasArgList">メソッド宣言に配列引数を含むかどうかを示す値を指定します。</param>
		/// <param name="keywordArgumentsProvided">呼び出しサイトでキーワード引数が渡されたかどうかを示す値を指定します。</param>
		/// <returns>引数の数が正しくないために失敗したことを示す <see cref="ArgumentTypeException"/>。</returns>
		public static ArgumentTypeException TypeErrorForIncorrectArgumentCount(string methodName, int formalNormalArgumentCount, int defaultArgumentCount, int providedArgumentCount, bool hasArgList, bool keywordArgumentsProvided) { return TypeErrorForIncorrectArgumentCount(methodName, formalNormalArgumentCount, formalNormalArgumentCount, defaultArgumentCount, providedArgumentCount, hasArgList, keywordArgumentsProvided); }

		/// <summary>引数の数が正しくないために失敗したことを示す例外を返します。</summary>
		/// <param name="methodName">不正な数の引数が渡されたメソッドの名前を指定します。</param>
		/// <param name="minFormalNormalArgumentCount">配列引数およびキーワード引数を含まないこのメソッドが許容する引数の数の最小値を指定します。</param>
		/// <param name="maxFormalNormalArgumentCount">配列引数およびキーワード引数を含まないこのメソッドが許容する引数の数の最大値を指定します。</param>
		/// <param name="defaultArgumentCount">メソッド宣言の中で既定値のある引数の数を指定します。</param>
		/// <param name="providedArgumentCount">呼び出しサイトで渡された引数の数を指定します。</param>
		/// <param name="hasArgList">メソッド宣言に配列引数を含むかどうかを示す値を指定します。</param>
		/// <param name="keywordArgumentsProvided">呼び出しサイトでキーワード引数が渡されたかどうかを示す値を指定します。</param>
		/// <returns>引数の数が正しくないために失敗したことを示す <see cref="ArgumentTypeException"/>。</returns>
		public static ArgumentTypeException TypeErrorForIncorrectArgumentCount(string methodName, int minFormalNormalArgumentCount, int maxFormalNormalArgumentCount, int defaultArgumentCount, int providedArgumentCount, bool hasArgList, bool keywordArgumentsProvided)
		{
			int formalCount;
			string formalCountQualifier;
			var nonKeyword = keywordArgumentsProvided ? "非キーワード " : "";
			if (defaultArgumentCount > 0 || hasArgList || minFormalNormalArgumentCount != maxFormalNormalArgumentCount)
			{
				if (providedArgumentCount < minFormalNormalArgumentCount || maxFormalNormalArgumentCount == int.MaxValue)
				{
					formalCountQualifier = "最小";
					formalCount = minFormalNormalArgumentCount - defaultArgumentCount;
				}
				else
				{
					formalCountQualifier = "最大";
					formalCount = maxFormalNormalArgumentCount;
				}
			}
			else if (minFormalNormalArgumentCount == 0)
				return ScriptingRuntimeHelpers.SimpleTypeError(string.Format("{0}() は引数をとりません ({1} 個が指定されました)", methodName, providedArgumentCount));
			else
			{
				formalCountQualifier = "";
				formalCount = minFormalNormalArgumentCount;
			}
			return new ArgumentTypeException(string.Format("{0}() は{1} {2} 個の{3}引数をとります ({4} 個が指定されました)", methodName, formalCountQualifier, formalCount, nonKeyword, providedArgumentCount));
		}

		/// <summary>引数の数が正しくないために失敗したことを示す例外を返します。</summary>
		/// <param name="methodName">不正な数の引数が渡されたメソッドの名前を指定します。</param>
		/// <param name="formalNormalArgumentCount">配列引数およびキーワード引数を含まない引数の数を指定します。</param>
		/// <param name="defaultArgumentCount">メソッド宣言の中で既定値のある引数の数を指定します。</param>
		/// <param name="providedArgumentCount">呼び出しサイトで渡された引数の数を指定します。</param>
		/// <returns>引数の数が正しくないために失敗したことを示す <see cref="ArgumentTypeException"/>。</returns>
		public static ArgumentTypeException TypeErrorForIncorrectArgumentCount(string methodName, int formalNormalArgumentCount, int defaultArgumentCount, int providedArgumentCount) { return TypeErrorForIncorrectArgumentCount(methodName, formalNormalArgumentCount, defaultArgumentCount, providedArgumentCount, false, false); }

		/// <summary>引数の数が正しくないために失敗したことを示す例外を返します。</summary>
		/// <param name="methodName">不正な数の引数が渡されたメソッドの名前を指定します。</param>
		/// <param name="expectedArgumentCount">このメソッドで予期されている引数の数を指定します。</param>
		/// <param name="providedArgumentCount">呼び出しサイトで渡された引数の数を指定します。</param>
		/// <returns>引数の数が正しくないために失敗したことを示す <see cref="ArgumentTypeException"/>。</returns>
		public static ArgumentTypeException TypeErrorForIncorrectArgumentCount(string methodName, int expectedArgumentCount, int providedArgumentCount) { return TypeErrorForIncorrectArgumentCount(methodName, expectedArgumentCount, 0, providedArgumentCount); }

		/// <summary>予期しないキーワード引数が渡されたために失敗したことを示す例外を返します。</summary>
		/// <param name="methodName">予期しないキーワード引数が渡されたメソッドの名前を指定します。</param>
		/// <param name="argumentName">渡されたキーワード引数の名前を指定します。</param>
		/// <returns>予期しないキーワード引数が渡されたために失敗したことを示す <see cref="ArgumentTypeException"/>。</returns>
		public static ArgumentTypeException TypeErrorForExtraKeywordArgument(string methodName, string argumentName) { return new ArgumentTypeException(string.Format("{0}() には予期しないキーワード引数 '{1}' が指定されました。", methodName, argumentName)); }

		/// <summary>重複したキーワード引数が渡されたために失敗したことを示す例外を返します。</summary>
		/// <param name="methodName">重複したキーワード引数が渡されたメソッドの名前を指定します。</param>
		/// <param name="argumentName">重複のあるキーワード引数の名前を指定します。</param>
		/// <returns>重複したキーワード引数が渡されたために失敗したことを示す <see cref="ArgumentTypeException"/>。</returns>
		public static ArgumentTypeException TypeErrorForDuplicateKeywordArgument(string methodName, string argumentName) { return new ArgumentTypeException(string.Format("{0}() にはキーワード引数 '{1}' に複数の値が指定されました。", methodName, argumentName)); }

		/// <summary>メソッドの型引数を推論できないため失敗したことを示す例外を返します。</summary>
		/// <param name="methodName">推論できない型引数をもつメソッドの名前を指定します。</param>
		/// <returns>メソッドの型引数を推論できないため失敗したことを示す <see cref="ArgumentTypeException"/>。</returns>
		public static ArgumentTypeException TypeErrorForNonInferrableMethod(string methodName) { return new ArgumentTypeException(string.Format("メソッド '{0}' に対する型引数を使用法から推論できません。明示的な型引数の指定を試みてください。", methodName)); }

		/// <summary>指定されたメッセージを使用して、新しい <see cref="ArgumentTypeException"/> を作成します。</summary>
		/// <param name="message">メッセージを指定します。</param>
		/// <returns>新しく作成された <see cref="ArgumentTypeException"/>。</returns>
		public static ArgumentTypeException SimpleTypeError(string message) { return new ArgumentTypeException(message); }

		/// <summary>散開される引数に渡される実引数の型がシーケンスでないため失敗したことを示す例外を返します。</summary>
		/// <param name="methodName">シーケンス以外の型のオブジェクトが散開引数に渡されたメソッドの名前を指定します。</param>
		/// <param name="typeName">散開引数に渡されたオブジェクトの型の名前を指定します。</param>
		/// <returns>散開される引数に渡される実引数の型がシーケンスでないため失敗したことを示す <see cref="ArgumentTypeException"/>。</returns>
		public static ArgumentTypeException InvalidSplatteeError(string methodName, string typeName) { return new ArgumentTypeException(string.Format("* 以降の {0}() の引数はシーケンスである必要がありますが、{1} が指定されました。", methodName, typeName)); }

		/// <summary>指定されたオブジェクトに対するメソッドをリフレクションを使用して呼び出します。</summary>
		/// <param name="mb">呼び出すメソッドを指定します。</param>
		/// <param name="obj">メソッドを呼び出すオブジェクトを指定します。</param>
		/// <param name="args">メソッドに渡す実引数を指定します。</param>
		/// <returns>メソッドの戻り値。</returns>
		public static object InvokeMethod(MethodBase mb, object obj, object[] args)
		{
			try
			{
				return mb.Invoke(obj, args);
			}
			catch (TargetInvocationException tie)
			{
				throw tie.InnerException;
			}
		}

		/// <summary>指定されたコンストラクタをリフレクションを使用して呼び出します。</summary>
		/// <param name="ci">呼び出すコンストラクタを指定します。</param>
		/// <param name="args">コンストラクタに渡す実引数を指定します。</param>
		/// <returns>指定されたコンストラクタによって作成されたオブジェクト。</returns>
		public static object InvokeConstructor(ConstructorInfo ci, object[] args)
		{
			try
			{
				return ci.Invoke(args);
			}
			catch (TargetInvocationException tie)
			{
				throw tie.InnerException;
			}
		}

		// TODO: just emit this in the generated code
		/// <summary>指定されたディクショナリに指定された名前が存在して、名前に対する値が指定された型であるかどうかを判断します。</summary>
		/// <param name="dict">調べるディクショナリを指定します。</param>
		/// <param name="names">ディクショナリに含まれている項目の名前が格納された配列を指定します。</param>
		/// <param name="types">ディクショナリの <paramref name="names"/> 配列に対応する要素の型が格納された配列を指定します。この引数は省略可能です。</param>
		/// <returns>指定されたディクショナリに指定された名前が存在して、名前に対する値が指定された型である場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool CheckDictionaryMembers(IDictionary dict, string[] names, Type[] types) { return dict.Count == names.Length && names.Select((x, i) => dict.Contains(x) && (types == null || CompilerHelpers.GetType(dict[x]) == types[i])).All(x => x); }

		/// <summary>指定された <see cref="EventTracker"/> に指定された値が関連付けられているかどうかを判断します。</summary>
		/// <param name="eventTracker">値が関連付けられている <see cref="EventTracker"/> を指定します。</param>
		/// <param name="value">関連付けられている値を指定します。</param>
		/// <exception cref="ArgumentException">イベントが指定された値に関連付けられていません。</exception>
		/// <exception cref="ArgumentTypeException">関連付けられている値が期待された型ではありません。</exception>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")] // TODO: fix
		public static void SetEvent(EventTracker eventTracker, object value)
		{
			var et = value as EventTracker;
			if (et != null)
			{
				if (et != eventTracker)
					throw new ArgumentException(string.Format("{0}.{1} からのイベントが予期されましたが、{2}.{3} からのイベントが指定されました。", eventTracker.DeclaringType.Name, eventTracker.Name, et.DeclaringType.Name, et.Name));
				return;
			}
			var bmt = value as BoundMemberTracker;
			if (bmt == null)
				throw new ArgumentTypeException(string.Format("束縛されたイベントが予期されましたが、{0} が指定されました。", CompilerHelpers.GetType(value).Name));
			if (bmt.BoundTo.MemberType != TrackerTypes.Event)
				throw new ArgumentTypeException(string.Format("束縛されたイベントが予期されましたが、{0} が指定されました。", bmt.BoundTo.MemberType.ToString()));
			if (bmt.BoundTo != eventTracker)
				throw new ArgumentException(string.Format("{0}.{1} からのイベントが予期されましたが、{2}.{3} からのイベントが指定されました。", eventTracker.DeclaringType.Name, eventTracker.Name, bmt.BoundTo.DeclaringType.Name, bmt.BoundTo.Name));
		}
	}
}
