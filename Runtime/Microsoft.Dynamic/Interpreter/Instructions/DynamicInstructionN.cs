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
using System.Runtime.CompilerServices;

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>任意のデリゲート型に対する動的呼び出しを行う命令を表します。</summary>
	sealed partial class DynamicInstructionN : Instruction
	{
		readonly CallInstruction _target;
		readonly object _targetDelegate;
		readonly CallSite _site;
		readonly int _argumentCount;
		readonly bool _isVoid;

		/// <summary>指定されたデリゲート型と呼び出しサイトを使用して、<see cref="Microsoft.Scripting.Interpreter.DynamicInstructionN"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="delegateType">動的呼び出しに使用するデリゲート型を指定します。</param>
		/// <param name="site">動的呼び出しサイトを指定します。</param>
		public DynamicInstructionN(Type delegateType, CallSite site)
		{
			var methodInfo = delegateType.GetMethod("Invoke");
			var parameters = methodInfo.GetParameters();
			_target = CallInstruction.Create(methodInfo, parameters);
			_site = site;
			_argumentCount = parameters.Length - 1;
			_targetDelegate = site.GetType().GetField("Target").GetValue(site);
		}

		/// <summary>指定されたデリゲート型、呼び出しサイトおよび値を返さないかどうかを示す値を使用して、<see cref="Microsoft.Scripting.Interpreter.DynamicInstructionN"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="delegateType">動的呼び出しに使用するデリゲート型を指定します。</param>
		/// <param name="site">動的呼び出しサイトを指定します。</param>
		/// <param name="isVoid">この動的呼び出しが値を返さないかどうかを示す値を指定します。</param>
		public DynamicInstructionN(Type delegateType, CallSite site, bool isVoid) : this(delegateType, site) { _isVoid = isVoid; }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return _isVoid ? 0 : 1; } }

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return _argumentCount; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			object[] args = new object[_argumentCount + 1];
			args[0] = _site;
			for (int i = _argumentCount - 1; i >= 0; i--)
				args[i + 1] = frame.Pop();
			var ret = _target.InvokeInstance(_targetDelegate, args);
			if (!_isVoid)
				frame.Push(ret);
			return 1;
		}

		/// <summary>このオブジェクトの文字列表現を取得します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		public override string ToString() { return "DynamicInstructionN(" + _site + ")"; }
	}
}
