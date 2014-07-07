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
using System.Linq;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>実引数と仮引数の間の関連付けを表します。</summary>
	public struct ArgumentBinding : IEquatable<ArgumentBinding>
	{
		static readonly int[] EmptyBinding = new int[0];

		int[] _binding; // immutable

		/// <summary>位置が決定されている引数の数を使用して、<see cref="Microsoft.Scripting.Actions.Calls.ArgumentBinding"/> 構造体の新しいインスタンスを初期化します。</summary>
		/// <param name="positionalArgCount">位置が決定されている引数の数を指定します。</param>
		internal ArgumentBinding(int positionalArgCount) : this(positionalArgCount, EmptyBinding) { }

		/// <summary>位置が決定されている引数の数と名前付き引数の関連付けを使用して、<see cref="Microsoft.Scripting.Actions.Calls.ArgumentBinding"/> 構造体の新しいインスタンスを初期化します。</summary>
		/// <param name="positionalArgCount">位置が決定されている引数の数を指定します。</param>
		/// <param name="binding">名前付き引数の関連付けを指定します。</param>
		internal ArgumentBinding(int positionalArgCount, int[] binding) : this()
		{
			Assert.NotNull(binding);
			_binding = binding;
			PositionalArgCount = positionalArgCount;
		}

		/// <summary>位置が決定されている引数の数を取得します。</summary>
		public int PositionalArgCount { get; private set; }

		/// <summary>指定された実引数のインデックスに対する仮引数のインデックスを取得します。</summary>
		/// <param name="argumentIndex">実引数の引数リスト内の場所を示すインデックスを指定します。</param>
		/// <returns>仮引数の引数リスト内の場所を示すインデックス。</returns>
		public int ArgumentToParameter(int argumentIndex) { return argumentIndex < PositionalArgCount ? argumentIndex : PositionalArgCount + _binding[argumentIndex - PositionalArgCount]; }

		/// <summary>指定された <see cref="ArgumentBinding"/> がこの <see cref="ArgumentBinding"/> と等しいかどうかを判断します。</summary>
		/// <param name="other">比較する <see cref="ArgumentBinding"/>。</param>
		/// <returns>この <see cref="ArgumentBinding"/> が指定された <see cref="ArgumentBinding"/> と等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool Equals(ArgumentBinding other) { return PositionalArgCount == other.PositionalArgCount && _binding.SequenceEqual(other._binding); }

		/// <summary>指定されたオブジェクトがこのオブジェクトと等しいかどうかを判断します。</summary>
		/// <param name="obj">比較するオブジェクト。</param>
		/// <returns>このオブジェクトが指定されたオブジェクトと等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public override bool Equals(object obj) { return obj is ArgumentBinding && Equals((ArgumentBinding)obj); }

		/// <summary>このオブジェクトのハッシュ値を計算します。</summary>
		/// <returns>このオブジェクトに対するハッシュ値。</returns>
		public override int GetHashCode() { return PositionalArgCount.GetHashCode() ^ _binding.GetValueHashCode(); }

		/// <summary>2 つの <see cref="ArgumentBinding"/> が等しいかどうかを判断します。</summary>
		/// <param name="left">比較する 1 番目の <see cref="ArgumentBinding"/>。</param>
		/// <param name="right">比較する 2 番目の <see cref="ArgumentBinding"/>。</param>
		/// <returns>2 つの <see cref="ArgumentBinding"/> が等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool operator ==(ArgumentBinding left, ArgumentBinding right) { return left.Equals(right); }

		/// <summary>2 つの <see cref="ArgumentBinding"/> が等しくないかどうかを判断します。</summary>
		/// <param name="left">比較する 1 番目の <see cref="ArgumentBinding"/>。</param>
		/// <param name="right">比較する 2 番目の <see cref="ArgumentBinding"/>。</param>
		/// <returns>2 つの <see cref="ArgumentBinding"/> が等しくない場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool operator !=(ArgumentBinding left, ArgumentBinding right) { return !left.Equals(right); }
	}
}
