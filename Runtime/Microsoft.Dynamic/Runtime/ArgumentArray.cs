/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Microsoft Public License. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Microsoft Public License, please send an email to 
 * ironruby@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Microsoft Public License.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

using System.Dynamic;
using System.Linq.Expressions;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	using AstUtils = Microsoft.Scripting.Ast.Utils;

	/// <summary>
	/// 動的サイトに渡されたすべての引数を Func/Action デリゲートが受け入れることができるそれより多くの引数でラップします。
	/// そのようなサイトに対する規則を生成するバインダーはまず引数をラップ解除して、それらに対するバインディングを実行する必要があります。
	/// </summary>
	public sealed class ArgumentArray
	{
		readonly object[] _arguments;

		// the index of the first item _arguments that represents an argument:
		readonly int _first;

		/// <summary>引数全体と実際に利用する範囲を使用して、<see cref="Microsoft.Scripting.Runtime.ArgumentArray"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="arguments">引数全体を指定します。</param>
		/// <param name="first"><paramref name="arguments"/> の中で引数として使用される最初の位置を示す 0 から始まるインデックスを指定します。</param>
		/// <param name="count">実際に引数として使用する要素の数を指定します。</param>
		internal ArgumentArray(object[] arguments, int first, int count)
		{
			_arguments = arguments;
			_first = first;
			Count = count;
		}

		/// <summary>引数を表すリストから実際に使用される要素の数を取得します。</summary>
		public int Count { get; private set; }

		/// <summary>指定されたインデックスにある引数を取得します。</summary>
		/// <param name="index">取得する引数の位置を示す 0 から始まるインデックスを指定します。</param>
		/// <returns>指定されたインデックスにある引数。</returns>
		public object this[int index]
		{
			get
			{
				ContractUtils.RequiresArrayIndex(_arguments, index, "index");
				return _arguments[_first + index];
			}
		}

		/// <summary>指定された <see cref="ArgumentArray"/> を表すインスタンスの指定されたインデックスにある引数を取得する <see cref="DynamicMetaObject"/> を返します。</summary>
		/// <param name="parameter">引数を取得する <see cref="ArgumentArray"/> のインスタンスを表す式を指定します。</param>
		/// <param name="index">取得する引数の位置を示す 0 から始まるインデックスを指定します。</param>
		/// <returns>指定された位置にある引数を示す <see cref="DynamicMetaObject"/>。</returns>
		public DynamicMetaObject GetMetaObject(Expression parameter, int index) { return DynamicMetaObject.Create(this[index], Expression.Property(AstUtils.Convert(parameter, typeof(ArgumentArray)), "Item", AstUtils.Constant(index))); }
	}
}
