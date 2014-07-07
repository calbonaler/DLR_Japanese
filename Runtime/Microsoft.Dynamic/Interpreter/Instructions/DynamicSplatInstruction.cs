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
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>多数の引数を使用する動的呼び出しを実装します。引数は <see cref="ArgumentArray"/> でラップされます。</summary>
	sealed class DynamicSplatInstruction : Instruction
	{
		readonly CallSite<Func<CallSite, ArgumentArray, object>> _site;
		readonly int _argumentCount;

		/// <summary>指定された引数の数と動的呼び出しサイトを使用して、<see cref="Microsoft.Scripting.Interpreter.DynamicSplatInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="argumentCount">動的呼び出しの引数の数を指定します。</param>
		/// <param name="site">動的呼び出しに使用される呼び出しサイトを指定します。</param>
		internal DynamicSplatInstruction(int argumentCount, CallSite<Func<CallSite, ArgumentArray, object>> site)
		{
			_site = site;
			_argumentCount = argumentCount;
		}

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return _argumentCount; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			int first = frame.StackIndex - _argumentCount;
			frame.Data[first] = _site.Target(_site, new ArgumentArray(frame.Data, first, _argumentCount));
			frame.StackIndex = first + 1;
			return 1;
		}

		/// <summary>このオブジェクトの文字列表現を取得します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		public override string ToString() { return "DynamicSplatInstruction(" + _site + ")"; }
	}
}
