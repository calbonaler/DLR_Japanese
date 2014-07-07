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
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Interpreter
{
	/// <summary><see cref="LightLambda"/> がコンパイルされた場合に発生するイベントにデータを提供します。</summary>
	public sealed class LightLambdaCompileEventArgs : EventArgs
	{
		/// <summary>コンパイル済みのデリゲートを使用して、<see cref="Microsoft.Scripting.Interpreter.LightLambdaCompileEventArgs"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="compiled">コンパイル済みのデリゲートを指定します。</param>
		internal LightLambdaCompileEventArgs(Delegate compiled) { Compiled = compiled; }

		/// <summary>コンパイル済みのデリゲートを取得します。</summary>
		public Delegate Compiled { get; private set; }
	}

	/// <summary>インタプリタがコンパイル不要で実行可能なデリゲートを作成できる軽量ラムダ式を表します。</summary>
	public partial class LightLambda
	{
		readonly StrongBox<object>[] _closure;
		readonly Interpreter _interpreter;
		static readonly CacheDict<Type, Func<LightLambda, Delegate>> _runCache = new CacheDict<Type, Func<LightLambda, Delegate>>(100);

		// Adaptive compilation support
		readonly LightDelegateCreator _delegateCreator;
		Delegate _compiled;
		int _compilationThreshold;

		/// <summary><see cref="LightLambda"/> がコンパイルされた場合に発生します。</summary>
		public event EventHandler<LightLambdaCompileEventArgs> Compile;

		/// <summary>デリゲート作成を管理する <see cref="LightDelegateCreator"/>、クロージャ変数、コンパイルまでの実行回数を指定して、<see cref="Microsoft.Scripting.Interpreter.LightLambda"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="delegateCreator">デリゲート作成を管理する <see cref="LightDelegateCreator"/> を指定します。</param>
		/// <param name="closure">対象となるデリゲートの実行環境を示すクロージャ変数を指定します。</param>
		/// <param name="compilationThreshold">インタプリタでコードが何回実行されればコンパイルされるかを示す閾値を指定します。</param>
		internal LightLambda(LightDelegateCreator delegateCreator, StrongBox<object>[] closure, int compilationThreshold)
		{
			_delegateCreator = delegateCreator;
			_closure = closure;
			_interpreter = delegateCreator.Interpreter;
			_compilationThreshold = compilationThreshold;
		}

		static MethodInfo GetRunMethodOrFastCtor(Type delegateType, out Func<LightLambda, Delegate> fastCtor)
		{
			lock (_runCache)
			{
				if (_runCache.TryGetValue(delegateType, out fastCtor))
					return null;
				return MakeRunMethodOrFastCtor(delegateType, out fastCtor);
			}
		}

		static MethodInfo MakeRunMethodOrFastCtor(Type delegateType, out Func<LightLambda, Delegate> fastCtor)
		{
			var method = delegateType.GetMethod("Invoke");
			var paramInfos = method.GetParameters();
			Type[] paramTypes;
			string name = "Run";
			fastCtor = null;
			if (paramInfos.Length >= MaxParameters)
				return null;
			if (method.ReturnType == typeof(void))
			{
				name += "Void";
				paramTypes = new Type[paramInfos.Length];
			}
			else
			{
				paramTypes = new Type[paramInfos.Length + 1];
				paramTypes[paramTypes.Length - 1] = method.ReturnType;
			}
			MethodInfo runMethod;
			if (method.ReturnType == typeof(void) && paramTypes.Length == 2 && paramInfos[0].ParameterType.IsByRef && paramInfos[1].ParameterType.IsByRef)
			{
				runMethod = typeof(LightLambda).GetMethod("RunVoidRef2", BindingFlags.NonPublic | BindingFlags.Instance);
				paramTypes[0] = paramInfos[0].ParameterType.GetElementType();
				paramTypes[1] = paramInfos[1].ParameterType.GetElementType();
			}
			else if (method.ReturnType == typeof(void) && paramTypes.Length == 0)
				return typeof(LightLambda).GetMethod("RunVoid0", BindingFlags.NonPublic | BindingFlags.Instance);
			else
			{
				for (int i = 0; i < paramInfos.Length; i++)
				{
					if ((paramTypes[i] = paramInfos[i].ParameterType).IsByRef)
						return null;
				}
				if (Expression.GetDelegateType(paramTypes) == delegateType)
				{
					name = "Make" + name + paramInfos.Length;
					_runCache[delegateType] = fastCtor = (Func<LightLambda, Delegate>)Delegate.CreateDelegate(typeof(Func<LightLambda, Delegate>),
						typeof(LightLambda).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(paramTypes)
					);
					return null;
				}
				runMethod = typeof(LightLambda).GetMethod(name + paramInfos.Length, BindingFlags.NonPublic | BindingFlags.Instance);
			}
			return runMethod.MakeGenericMethod(paramTypes);
		}

		//TODO enable sharing of these custom delegates
		Delegate CreateCustomDelegate(Type delegateType)
		{
			PerfTrack.NoteEvent(PerfTrack.Category.Compiler, "Synchronously compiling a custom delegate");
			var method = delegateType.GetMethod("Invoke");
			var paramInfos = method.GetParameters();
			var parameters = new ParameterExpression[paramInfos.Length];
			var parametersAsObject = new Expression[paramInfos.Length];
			for (int i = 0; i < paramInfos.Length; i++)
			{
				parameters[i] = Expression.Parameter(paramInfos[i].ParameterType, paramInfos[i].Name);
				parametersAsObject[i] = Expression.Convert(parameters[i], typeof(object));
			}
			return Expression.Lambda(delegateType,
				Expression.Convert(
					Expression.Call(AstUtils.Constant(this), typeof(LightLambda).GetMethod("Run"), Expression.NewArrayInit(typeof(object), parametersAsObject)),
					method.ReturnType
				), parameters
			).Compile();
		}

		/// <summary>インタプリタを使用してコードを実行する指定された型のデリゲートを作成します。</summary>
		/// <param name="delegateType">デリゲートの型を指定します。</param>
		/// <returns>インタプリタによってコードが実行されるデリゲート。</returns>
		internal Delegate MakeDelegate(Type delegateType)
		{
			Func<LightLambda, Delegate> fastCtor;
			var method = GetRunMethodOrFastCtor(delegateType, out fastCtor);
			if (fastCtor != null)
				return fastCtor(this);
			else if (method == null)
				return CreateCustomDelegate(delegateType);
			return Delegate.CreateDelegate(delegateType, this, method);
		}

		bool TryGetCompiled()
		{
			// Use the compiled delegate if available.
			if (_delegateCreator.HasCompiled)
			{
				_compiled = _delegateCreator.CreateCompiledDelegate(_closure);
				// Send it to anyone who's interested.
				var compileEvent = Compile;
				if (compileEvent != null && _delegateCreator.SameDelegateType)
					compileEvent(this, new LightLambdaCompileEventArgs(_compiled));
				return true;
			}
			// Don't lock here, it's a frequently hit path.
			//
			// There could be multiple threads racing, but that is okay.
			// Two bad things can happen:
			//   * We miss decrements (some thread sets the counter forward)
			//   * We might enter the "if" branch more than once.
			//
			// The first is okay, it just means we take longer to compile.
			// The second we explicitly guard against inside of Compile().
			//
			// We can't miss 0. The first thread that writes -1 must have read 0 and hence start compilation.
			if (unchecked(_compilationThreshold--) == 0)
			{
				if (_interpreter.CompileSynchronously)
				{
					_delegateCreator.Compile(null);
					return TryGetCompiled();
				}
				else
					ThreadPool.QueueUserWorkItem(_delegateCreator.Compile); // Kick off the compile on another thread so this one can keep going
			}
			return false;
		}

		InterpretedFrame MakeFrame() { return new InterpretedFrame(_interpreter, _closure); }

		internal void RunVoidRef2<T0, T1>(ref T0 arg0, ref T1 arg1)
		{
			if (_compiled != null || TryGetCompiled())
			{
				((ActionRef<T0, T1>)_compiled)(ref arg0, ref arg1);
				return;
			}
			// copy in and copy out for today...
			var frame = MakeFrame();
			frame.Data[0] = arg0;
			frame.Data[1] = arg1;
			var currentFrame = frame.Enter();
			try { _interpreter.Run(frame); }
			finally
			{
				frame.Leave(currentFrame);
				arg0 = (T0)frame.Data[0];
				arg1 = (T1)frame.Data[1];
			}
		}

		/// <summary>引数を指定してこのラムダ式を実行し、結果が存在する場合は結果を返します。</summary>
		/// <param name="arguments">ラムダ式に与える引数を指定します。</param>
		/// <returns>ラムダ式に結果が存在する場合は結果。</returns>
		public object Run(params object[] arguments)
		{
			if (_compiled != null || TryGetCompiled())
				return _compiled.DynamicInvoke(arguments);
			var frame = MakeFrame();
			for (int i = 0; i < arguments.Length; i++)
				frame.Data[i] = arguments[i];
			var currentFrame = frame.Enter();
			try { _interpreter.Run(frame); }
			finally { frame.Leave(currentFrame); }
			return frame.Pop();
		}
	}
}
