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
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>インタプリタによって解釈されるデリゲートの作成を管理します。これらのデリゲートは頻繁に実行される場合にのみコンパイルされます。</summary>
	sealed class LightDelegateCreator
	{
		// null if we are forced to compile
		readonly LambdaExpression _lambda;

		// Adaptive compilation support:
		Type _compiledDelegateType;
		Delegate _compiled;
		readonly object _compileLock = new object();

		/// <summary>デリゲートを解釈するインタプリタと対象のラムダ式を指定して、<see cref="Microsoft.Scripting.Interpreter.LightDelegateCreator"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="interpreter">作成されるデリゲートを解釈するインタプリタを指定します。</param>
		/// <param name="lambda">作成されるデリゲートの対象となるラムダ式を指定します。</param>
		internal LightDelegateCreator(Interpreter interpreter, LambdaExpression lambda)
		{
			Assert.NotNull(lambda);
			Interpreter = interpreter;
			_lambda = lambda;
		}

		/// <summary>作成されるデリゲートを解釈するインタプリタを取得します。</summary>
		internal Interpreter Interpreter { get; private set; }

		bool HasClosure { get { return Interpreter != null && Interpreter.ClosureSize > 0; } }

		/// <summary>デリゲートが JIT コードにコンパイルされたかどうかを示す値を取得します。</summary>
		internal bool HasCompiled { get { return _compiled != null; } }

		/// <summary>コンパイルされたデリゲートがラムダ式と同じ型を持っているかどうかを示す値を取得します。<c>false</c> の場合、型は解釈のために変更されています。</summary>
		internal bool SameDelegateType { get { return _compiledDelegateType == _lambda.Type; } }

		/// <summary>このラムダ式を対象とするデリゲートを作成します。</summary>
		/// <returns>作成されたデリゲート。</returns>
		internal Delegate CreateDelegate() { return CreateDelegate(null); }

		/// <summary>このラムダ式を対象とするデリゲートをクロージャ変数を指定して作成します。</summary>
		/// <param name="closure">対象となるデリゲートの実行環境を示すクロージャ変数を指定します。</param>
		/// <returns>作成されたデリゲート。</returns>
		internal Delegate CreateDelegate(StrongBox<object>[] closure)
		{
			if (_compiled != null)
			{
				// If the delegate type we want is not a Func/Action, we can't use the compiled code directly.
				// So instead just fall through and create an interpreted LightLambda, which will pick up the compiled delegate on its first run.
				//
				// Ideally, we would just rebind the compiled delegate using Delegate.CreateDelegate.
				// Unfortunately, it doesn't work on dynamic methods.
				if (SameDelegateType)
					return CreateCompiledDelegate(closure);
			}
			if (Interpreter == null)
			{
				// We can't interpret, so force a compile
				Compile(null);
				var compiled = CreateCompiledDelegate(closure);
				Debug.Assert(compiled.GetType() == _lambda.Type);
				return compiled;
			}
			// Otherwise, we'll create an interpreted LightLambda
			return new LightLambda(this, closure, Interpreter._compilationThreshold).MakeDelegate(_lambda.Type);
		}

		/// <summary>このラムダ式を対象とするコンパイル済みのデリゲートをクロージャ変数を指定して取得します。</summary>
		/// <param name="closure">対象となるデリゲートの実行環境を示すクロージャ変数を指定します。</param>
		/// <returns>コンパイル済みのデリゲート。</returns>
		internal Delegate CreateCompiledDelegate(StrongBox<object>[] closure)
		{
			Debug.Assert(HasClosure == (closure != null));
			if (HasClosure)
				// We need to apply the closure to get the actual delegate.
				return ((Func<StrongBox<object>[], Delegate>)_compiled)(closure);
			return _compiled;
		}

		/// <summary>軽量ラムダ式に対するコンパイル済みのデリゲートを作成して、これ以降の呼び出しでインタプリタを実行する代わりにコンパイルされたコードを実行するように保存します。</summary>
		/// <param name="state"><see cref="M:ThreadPool.QueueUserWorkItem(WaitCallback)"/> にこのメソッドを渡すためのダミー引数です。</param>
		internal void Compile(object state)
		{
			if (_compiled != null)
				return;
			// Compilation is expensive, we only want to do it once.
			lock (_compileLock)
			{
				if (_compiled != null)
					return;
				PerfTrack.NoteEvent(PerfTrack.Category.Compiler, "Interpreted lambda compiled");
				// Interpreter needs a standard delegate type.
				// So change the lambda's delegate type to Func<...> or Action<...> so it can be called from the LightLambda.Run methods.
				var lambda = _lambda;
				if (Interpreter != null)
				{
					_compiledDelegateType = GetFuncOrAction(lambda);
					lambda = Expression.Lambda(_compiledDelegateType, lambda.Body, lambda.Name, lambda.Parameters);
				}
				_compiled = HasClosure ? LightLambdaClosureVisitor.BindLambda(lambda, Interpreter.ClosureVariables) : lambda.Compile();
			}
		}

		static Type GetFuncOrAction(LambdaExpression lambda)
		{
			Type delegateType;
			var isVoid = lambda.ReturnType == typeof(void);
			if (isVoid && lambda.Parameters.Count == 2 && lambda.Parameters[0].IsByRef && lambda.Parameters[1].IsByRef)
				return typeof(ActionRef<,>).MakeGenericType(lambda.Parameters.Select(p => p.Type).ToArray());
			else
			{
				var types = lambda.Parameters.Select(p => p.IsByRef ? p.Type.MakeByRefType() : p.Type).ToArray();
				if (isVoid)
				{
					if (Expression.TryGetActionType(types, out delegateType))
						return delegateType;
				}
				else if (Expression.TryGetFuncType(ArrayUtils.Append(types, lambda.ReturnType), out delegateType))
					return delegateType;
				return lambda.Type;
			}
		}
	}
}
