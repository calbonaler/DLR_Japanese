/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Microsoft Public License. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Microsoft Public License, please send an email to 
 * ironpy@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Microsoft Public License.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>メソッドを呼び出す命令を表します。</summary>
	public abstract partial class CallInstruction : Instruction
	{
		/// <summary>呼び出すメソッドを表す <see cref="MethodInfo"/> を取得します。</summary>
		public abstract MethodInfo Info { get; }

		/// <summary>インスタンスメソッドでは "this" も含むメソッドの引数の個数を取得します。</summary>
		public abstract int ArgumentCount { get; }

		/// <summary><see cref="Microsoft.Scripting.Interpreter.CallInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		internal CallInstruction() { }

		static readonly ConcurrentDictionary<MethodInfo, CallInstruction> _cache = new ConcurrentDictionary<MethodInfo, CallInstruction>();

		/// <summary>指定されたメソッドを呼び出す適切な <see cref="CallInstruction"/> クラスの派生クラスを作成します。</summary>
		/// <param name="info">呼び出すメソッドを指定します。</param>
		/// <returns>指定されたメソッドを呼び出す <see cref="CallInstruction"/> クラスの派生クラス。</returns>
		public static CallInstruction Create(MethodInfo info) { return Create(info, info.GetParameters()); }

		/// <summary>指定されたメソッドを呼び出す適切な <see cref="CallInstruction"/> クラスの派生クラスを作成します。</summary>
		/// <param name="info">呼び出すメソッドを指定します。</param>
		/// <param name="parameters">メソッドの仮引数を表す <see cref="ParameterInfo"/> の配列を指定します。</param>
		/// <returns>指定されたメソッドを呼び出す <see cref="CallInstruction"/> クラスの派生クラス。</returns>
		public static CallInstruction Create(MethodInfo info, ParameterInfo[] parameters)
		{
			int argumentCount = parameters.Length;
			if (!info.IsStatic)
				argumentCount++;
			// CLR バグ #796414 (Array.Get/Set に対するデリゲートを作成できない) に対する回避策:
			// T[]::Address - 戻り値 T& のため式ツリーではサポートされない
			if (info.DeclaringType != null && info.DeclaringType.IsArray && (info.Name == "Get" || info.Name == "Set"))
				return GetArrayAccessor(info, argumentCount);
			if (info is DynamicMethod || !info.IsStatic && info.DeclaringType.IsValueType || argumentCount >= MaxHelpers || Array.Exists(parameters, x => x.ParameterType.IsByRef))
				return new MethodInfoCallInstruction(info, argumentCount);
			return ShouldCache(info) ? _cache.GetOrAdd(info, x => CreateWorker(x, argumentCount, parameters)) : CreateWorker(info, argumentCount, parameters);
		}

		static CallInstruction CreateWorker(MethodInfo info, int argumentCount, ParameterInfo[] parameters)
		{
			try { return argumentCount < MaxArgs ? FastCreate(info, parameters) : SlowCreate(info, parameters); }
			catch (TargetInvocationException tie)
			{
				if (!(tie.InnerException is NotSupportedException))
					throw;
				return new MethodInfoCallInstruction(info, argumentCount);
			}
			catch (NotSupportedException)
			{
				// Delegate.CreateDelegate がメソッドをハンドルできない場合、遅いリフレクションバージョンにフォールバックする
				// 例えばこれはインターフェイスに定義されてクラスに実装されているジェネリックメソッドの場合に発生する可能性がある
				return new MethodInfoCallInstruction(info, argumentCount);
			}
		}

		static CallInstruction GetArrayAccessor(MethodInfo info, int argumentCount)
		{
			var isGetter = info.Name == "Get";
			switch (info.DeclaringType.GetArrayRank())
			{
				case 1:
					return Create(isGetter ? info.DeclaringType.GetMethod("GetValue", new[] { typeof(int) }) : new Action<Array, int, object>(ArrayItemSetter1).Method);
				case 2:
					return Create(isGetter ? info.DeclaringType.GetMethod("GetValue", new[] { typeof(int), typeof(int) }) : new Action<Array, int, int, object>(ArrayItemSetter2).Method);
				case 3:
					return Create(isGetter ? info.DeclaringType.GetMethod("GetValue", new[] { typeof(int), typeof(int), typeof(int) }) : new Action<Array, int, int, int, object>(ArrayItemSetter3).Method);
				default:
					return new MethodInfoCallInstruction(info, argumentCount);
			}
		}

		static void ArrayItemSetter1(Array array, int index0, object value) { array.SetValue(value, index0); }

		static void ArrayItemSetter2(Array array, int index0, int index1, object value) { array.SetValue(value, index0, index1); }

		static void ArrayItemSetter3(Array array, int index0, int index1, int index2, object value) { array.SetValue(value, index0, index1, index2); }

		static bool ShouldCache(MethodInfo info) { return !(info is DynamicMethod); }

		/// <summary>次の型またはこれ以上型が利用できない場合を表す <c>null</c> を取得します。</summary>
		static Type TryGetParameterOrReturnType(MethodInfo target, ParameterInfo[] pi, int index)
		{
			if (!target.IsStatic && --index < 0)
				return target.DeclaringType;
			if (index < pi.Length)
				return pi[index].ParameterType; // next in signature
			if (target.ReturnType == typeof(void) || index > pi.Length)
				return null; // no more parameters
			return target.ReturnType; // last parameter on Invoke is return type
		}

		static bool IndexIsNotReturnType(int index, MethodInfo target, ParameterInfo[] pi) { return pi.Length != index || (pi.Length == index && !target.IsStatic); }

		/// <summary>適切な <see cref="CallInstruction"/> 派生クラスのインスタンスを作成するためにリフレクションを使用します。</summary>
		static CallInstruction SlowCreate(MethodInfo info, ParameterInfo[] pis)
		{
			List<Type> types = new List<Type>();
			if (!info.IsStatic)
				types.Add(info.DeclaringType);
			foreach (var pi in pis)
				types.Add(pi.ParameterType);
			if (info.ReturnType != typeof(void))
				types.Add(info.ReturnType);
			Type[] arrTypes = types.ToArray();
			return (CallInstruction)Activator.CreateInstance(GetHelperType(info, arrTypes), info);
		}

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public sealed override int ProducedStack { get { return Info.ReturnType == typeof(void) ? 0 : 1; } }

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public sealed override int ConsumedStack { get { return ArgumentCount; } }
		
		/// <summary>この命令の名前を取得します。</summary>
		public sealed override string InstructionName { get { return "Call"; } }

		/// <summary>このオブジェクトの文字列表現を取得します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		public override string ToString() { return "Call(" + Info + ")"; }
	}

	sealed class MethodInfoCallInstruction : CallInstruction
	{
		readonly MethodInfo _target;
		readonly int _argumentCount;

		public override MethodInfo Info { get { return _target; } }

		public override int ArgumentCount { get { return _argumentCount; } }

		internal MethodInfoCallInstruction(MethodInfo target, int argumentCount)
		{
			_target = target;
			_argumentCount = argumentCount;
		}

		public override object Invoke(params object[] args) { return InvokeWorker(args); }

		public override object InvokeInstance(object instance, params object[] args)
		{
			if (_target.IsStatic)
			{
				try { return _target.Invoke(null, args); }
				catch (TargetInvocationException ex) { throw ExceptionHelpers.UpdateForRethrow(ex.InnerException); }
			}
			try { return _target.Invoke(instance, args); }
			catch (TargetInvocationException ex) { throw ExceptionHelpers.UpdateForRethrow(ex.InnerException); }
		}
		
		public override object Invoke() { return InvokeWorker(); }

		public override object Invoke(object arg0) { return InvokeWorker(arg0); }

		public override object Invoke(object arg0, object arg1) { return InvokeWorker(arg0, arg1); }

		public override object Invoke(object arg0, object arg1, object arg2) { return InvokeWorker(arg0, arg1, arg2); }

		public override object Invoke(object arg0, object arg1, object arg2, object arg3) { return InvokeWorker(arg0, arg1, arg2, arg3); }

		public override object Invoke(object arg0, object arg1, object arg2, object arg3, object arg4) { return InvokeWorker(arg0, arg1, arg2, arg3, arg4); }

		public override object Invoke(object arg0, object arg1, object arg2, object arg3, object arg4, object arg5) { return InvokeWorker(arg0, arg1, arg2, arg3, arg4, arg5); }

		public override object Invoke(object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6) { return InvokeWorker(arg0, arg1, arg2, arg3, arg4, arg5, arg6); }

		public override object Invoke(object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7) { return InvokeWorker(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7); }

		public override object Invoke(object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8) { return InvokeWorker(arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8); }
		
		object InvokeWorker(params object[] args)
		{
			if (_target.IsStatic)
			{
				try { return _target.Invoke(null, args); }
				catch (TargetInvocationException ex) { throw ExceptionHelpers.UpdateForRethrow(ex.InnerException); }
			}
			try { return _target.Invoke(args[0], Utils.ArrayUtils.RemoveFirst(args)); }
			catch (TargetInvocationException ex) { throw ExceptionHelpers.UpdateForRethrow(ex.InnerException); }
		}

		public sealed override int Run(InterpretedFrame frame)
		{
			var args = new object[ArgumentCount];
			for (int i = ArgumentCount - 1; i >= 0; i--)
				args[i] = frame.Pop();
			var ret = Invoke(args);
			if (_target.ReturnType != typeof(void))
				frame.Push(ret);
			return 1;
		}
	}
}
