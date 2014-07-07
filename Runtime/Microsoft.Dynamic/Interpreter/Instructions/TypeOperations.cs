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
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>指定された <see cref="LightDelegateCreator"/> を使用するデリゲートの作成を行う命令を表します。</summary>
	sealed class CreateDelegateInstruction : Instruction
	{
		readonly LightDelegateCreator _creator;

		/// <summary>指定された <see cref="LightDelegateCreator"/> を使用して、<see cref="Microsoft.Scripting.Interpreter.CreateDelegateInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="delegateCreator">デリゲートの作成を管理する <see cref="LightDelegateCreator"/> を指定します。</param>
		internal CreateDelegateInstruction(LightDelegateCreator delegateCreator) { _creator = delegateCreator; }

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return _creator.Interpreter.ClosureSize; } }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			StrongBox<object>[] closure;
			if (_creator.Interpreter.ClosureSize > 0)
			{
				closure = new StrongBox<object>[_creator.Interpreter.ClosureSize];
				for (int i = closure.Length - 1; i >= 0; i--)
					closure[i] = (StrongBox<object>)frame.Pop();
			}
			else
				closure = null;
			frame.Push(_creator.CreateDelegate(closure));
			return +1;
		}
	}

	/// <summary>指定されたコンストラクタを実行することによるインスタンスの作成を行う命令を表します。</summary>
	sealed class NewInstruction : Instruction
	{
		readonly ConstructorInfo _constructor;
		readonly int _argCount;

		/// <summary>指定されたコンストラクタを使用して、<see cref="Microsoft.Scripting.Interpreter.NewInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="constructor">作成されたインスタンスの初期化に使用されるコンストラクタを指定します。</param>
		public NewInstruction(ConstructorInfo constructor)
		{
			_constructor = constructor;
			_argCount = constructor.GetParameters().Length;
		}

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return _argCount; } }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			object[] args = new object[_argCount];
			for (int i = _argCount - 1; i >= 0; i--)
				args[i] = frame.Pop();
			object ret;
			try { ret = _constructor.Invoke(args); }
			catch (TargetInvocationException ex)
			{
				ExceptionHelpers.UpdateForRethrow(ex.InnerException);
				throw ex.InnerException;
			}
			frame.Push(ret);
			return +1;
		}

		/// <summary>このオブジェクトの文字列表現を取得します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		public override string ToString() { return "New " + _constructor.DeclaringType.Name + "(" + _constructor + ")"; }
	}

	/// <summary>型の既定値を評価スタックに読み込む命令を表します。</summary>
	/// <typeparam name="T">既定値を取得する型を指定します。</typeparam>
	sealed class DefaultValueInstruction<T> : Instruction
	{
		/// <summary><see cref="Microsoft.Scripting.Interpreter.DefaultValueInstruction&lt;T&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		internal DefaultValueInstruction() { }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.Push(default(T));
			return +1;
		}

		/// <summary>このオブジェクトの文字列表現を取得します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		public override string ToString() { return "New " + typeof(T); }
	}

	/// <summary>オブジェクトが指定された型に変換可能かどうかを判断する命令を表します。</summary>
	/// <typeparam name="T">オブジェクトが変換される型を指定します。</typeparam>
	sealed class TypeIsInstruction<T> : Instruction
	{
		/// <summary><see cref="Microsoft.Scripting.Interpreter.TypeIsInstruction&lt;T&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		internal TypeIsInstruction() { }

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			// unfortunately Type.IsInstanceOfType() is 35-times slower than "is T" so we use generic code:
			frame.Push(frame.Pop() is T);
			return +1;
		}

		/// <summary>このオブジェクトの文字列表現を取得します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		public override string ToString() { return "TypeIs " + typeof(T).Name; }
	}

	/// <summary>オブジェクトの指定された型への変換を試み、失敗した場合は <c>null</c> を返す命令を表します。</summary>
	/// <typeparam name="T"></typeparam>
	sealed class TypeAsInstruction<T> : Instruction
	{
		/// <summary><see cref="Microsoft.Scripting.Interpreter.TypeAsInstruction&lt;T&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		internal TypeAsInstruction() { }

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			// can't use as w/o generic constraint
			var value = frame.Pop();
			if (value is T)
				frame.Push(value);
			else
				frame.Push(null);
			return +1;
		}

		/// <summary>このオブジェクトの文字列表現を取得します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		public override string ToString() { return "TypeAs " + typeof(T).Name; }
	}

	/// <summary>オブジェクトの型が指定された型と等しいかどうかを判断する命令を表します。</summary>
	sealed class TypeEqualsInstruction : Instruction
	{
		/// <summary>この命令の唯一のインスタンスを示します。</summary>
		public static readonly TypeEqualsInstruction Instance = new TypeEqualsInstruction();

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return 2; } }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		TypeEqualsInstruction() { }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			var type = frame.Pop();
			var obj = frame.Pop();
			frame.Push(ScriptingRuntimeHelpers.BooleanToObject(obj != null && (object)obj.GetType() == type));
			return +1;
		}
	}
}
