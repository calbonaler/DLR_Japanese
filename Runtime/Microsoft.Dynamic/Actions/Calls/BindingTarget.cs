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
using System.Linq.Expressions;

namespace Microsoft.Scripting.Actions.Calls
{
	public delegate object OptimizingCallDelegate(object[] args, out bool shouldOptimize);

	/// <summary><see cref="OverloadResolver"/> を使用した 1 つ以上のメソッドへのバインディングの結果をカプセル化します。</summary>
	/// <remarks>
	/// ユーザーは最初に <see cref="Result"/> プロパティからバインディングが成功したか、特定のエラーが発生したかを判断する必要があります。
	/// バインディングが成功した場合、<see cref="MakeExpression"/> からメソッドを呼び出す式を作成できます。
	/// バインディングが失敗した場合、呼び出し元は失敗の理由に基づいたカスタムエラーメッセージを作成できます。
	/// </remarks>
	public sealed class BindingTarget
	{
		/// <summary>メソッドバインディングが成功したことを示す <see cref="Microsoft.Scripting.Actions.Calls.BindingTarget"/> を作成します。</summary>
		/// <param name="name">メソッドの名前を指定します。</param>
		/// <param name="actualArgumentCount">メソッドに実際に渡された引数の数を指定します。</param>
		/// <param name="candidate">対象となるメソッドを指定します。</param>
		/// <param name="level">メソッドの <see cref="NarrowingLevel"/> を指定します。</param>
		/// <param name="restrictedArgs">本来バインディングを実行した <see cref="System.Dynamic.DynamicMetaObject"/> を指定します。</param>
		internal BindingTarget(string name, int actualArgumentCount, MethodCandidate candidate, NarrowingLevel level, RestrictedArguments restrictedArgs)
		{
			Name = name;
			MethodCandidate = candidate;
			RestrictedArguments = restrictedArgs;
			NarrowingLevel = level;
			ActualArgumentCount = actualArgumentCount;
		}

		/// <summary>引数の数が正しくないためにバインディングが失敗したことを示す <see cref="Microsoft.Scripting.Actions.Calls.BindingTarget"/> を作成します。</summary>
		/// <param name="name">メソッドの名前を指定します。</param>
		/// <param name="actualArgumentCount">メソッドに実際に渡された引数の数を指定します。</param>
		/// <param name="expectedArgCount">メソッドが受け入れ可能な引数の数を指定します。</param>
		internal BindingTarget(string name, int actualArgumentCount, int[] expectedArgCount)
		{
			Name = name;
			Result = BindingResult.IncorrectArgumentCount;
			ExpectedArgumentCount = expectedArgCount;
			ActualArgumentCount = actualArgumentCount;
		}

		/// <summary>1 つ以上の引数が変換できないためにバインディングが失敗したことを示す <see cref="Microsoft.Scripting.Actions.Calls.BindingTarget"/> を作成します。</summary>
		/// <param name="name">メソッドの名前を指定します。</param>
		/// <param name="actualArgumentCount">メソッドに実際に渡された引数の数を指定します。</param>
		/// <param name="failures">メソッドとそれに関連付けられたエラーを指定します。</param>
		internal BindingTarget(string name, int actualArgumentCount, CallFailure[] failures)
		{
			Name = name;
			Result = BindingResult.CallFailure;
			CallFailures = failures;
			ActualArgumentCount = actualArgumentCount;
		}

		/// <summary>一致があいまいであるためにバインディングが失敗したことを示す <see cref="Microsoft.Scripting.Actions.Calls.BindingTarget"/> を作成します。</summary>
		/// <param name="name">メソッドの名前を指定します。</param>
		/// <param name="actualArgumentCount">メソッドに実際に渡された引数の数を指定します。</param>
		/// <param name="ambiguousMatches">一致が発生した複数のメソッドを指定します。</param>
		internal BindingTarget(string name, int actualArgumentCount, MethodCandidate[] ambiguousMatches)
		{
			Name = name;
			Result = BindingResult.AmbiguousMatch;
			AmbiguousMatches = ambiguousMatches;
			ActualArgumentCount = actualArgumentCount;
		}

		/// <summary>他の失敗を示す <see cref="Microsoft.Scripting.Actions.Calls.BindingTarget"/> を作成します。</summary>
		/// <param name="name">メソッドの名前を指定します。</param>
		/// <param name="result">他の失敗を示す <see cref="BindingResult"/> を指定します。</param>
		internal BindingTarget(string name, BindingResult result)
		{
			Name = name;
			Result = result;
		}

		/// <summary>バインディングの結果を取得します。</summary>
		public BindingResult Result { get; private set; }

		/// <summary>バインディングターゲットを呼び出す <see cref="Expression"/> を作成します。</summary>
		/// <returns>バインディングターゲットを呼び出す <see cref="Expression"/>。</returns>
		/// <exception cref="System.InvalidOperationException">バインディングが失敗しています。または、<see cref="System.Dynamic.DynamicMetaObject"/> に対するバインディングが完了していません。</exception>
		public Expression MakeExpression()
		{
			if (MethodCandidate == null)
				throw new InvalidOperationException("An expression cannot be produced because the method binding was unsuccessful.");
			if (RestrictedArguments == null)
				throw new InvalidOperationException("An expression cannot be produced because the method binding was done with Expressions, not MetaObject's");
			return MethodCandidate.MakeExpression(RestrictedArguments);
		}

		/// <summary>バインディングターゲットを呼び出すデリゲートを作成します。</summary>
		/// <returns>バインディングターゲットを呼び出すデリゲート。</returns>
		/// <exception cref="System.InvalidOperationException">バインディングが失敗しています。または、<see cref="System.Dynamic.DynamicMetaObject"/> に対するバインディングが完了していません。</exception>
		public OptimizingCallDelegate MakeDelegate()
		{
			if (MethodCandidate == null)
				throw new InvalidOperationException("An expression cannot be produced because the method binding was unsuccessful.");
			if (RestrictedArguments == null)
				throw new InvalidOperationException("An expression cannot be produced because the method binding was done with Expressions, not MetaObject's");
			return MethodCandidate.MakeDelegate(RestrictedArguments);
		}

		/// <summary>バインディングが成功した場合は、選択されたオーバーロードを取得します。失敗した場合は、<c>null</c> を返します。</summary>
		public OverloadInfo Overload { get { return MethodCandidate != null ? MethodCandidate.Overload : null; } }

		/// <summary><see cref="OverloadResolver"/> に提供されるメソッドの名前を指定します。</summary>
		public string Name { get; private set; }

		/// <summary>バインディングが成功した場合は、対象となるメソッドを取得します。失敗した場合は、<c>null</c> を返します。</summary>
		public MethodCandidate MethodCandidate { get; private set; }

		/// <summary><see cref="Result"/> が <see cref="BindingResult.AmbiguousMatch"/> の場合に、一致が発生した複数のメソッドを取得します。</summary>
		public IEnumerable<MethodCandidate> AmbiguousMatches { get; private set; }

		/// <summary><see cref="Result"/> が <see cref="BindingResult.CallFailure"/> の場合に、メソッドとそれに関連付けられた変換エラーを取得します。</summary>
		public ICollection<CallFailure> CallFailures { get; private set; }

		/// <summary><see cref="Result"/> が <see cref="BindingResult.IncorrectArgumentCount"/> の場合に、メソッドが受け入れ可能な引数の数を取得します。</summary>
		public IList<int> ExpectedArgumentCount { get; private set; }

		/// <summary>メソッドに実際に渡された引数の総数を取得します。</summary>
		public int ActualArgumentCount { get; private set; }

		/// <summary>
		/// 本来バインディングを実行した <see cref="System.Dynamic.DynamicMetaObject"/> を制約された状態で返します。
		/// 配列のメンバはそれぞれの引数に対応しています。すべてのメンバには値が存在します。
		/// </summary>
		public RestrictedArguments RestrictedArguments { get; private set; }

		/// <summary>バインディングの結果の型を取得します。どのメソッドも適用できない場合は <c>null</c> を返します。</summary>
		public Type ReturnType { get { return MethodCandidate != null ? MethodCandidate.ReturnType : null; } }

		/// <summary>呼び出しが成功した場合は、メソッドの <see cref="NarrowingLevel"/> を取得します。失敗した場合は <see cref="Microsoft.Scripting.Actions.Calls.NarrowingLevel.None"/> が返されます。</summary>
		public NarrowingLevel NarrowingLevel { get; private set; }

		/// <summary>バインディングが成功したかどうかを示す値を取得します。</summary>
		public bool Success { get { return Result == BindingResult.Success; } }
	}
}
