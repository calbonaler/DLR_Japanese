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
using Microsoft.Contracts;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions
{
	// TODO: 代わりに、this, list, dict, block のインデックスを記憶するだけで十分にするべきです。
	/// <summary>関数に渡される引数を表します。</summary>
	public struct Argument : IEquatable<Argument>
	{
		/// <summary>単純な名前のない位置が決定されている引数を表します。</summary>
		public static readonly Argument Simple = new Argument(ArgumentType.Simple, null);

		/// <summary>この引数の種類を取得します。</summary>
		public ArgumentType Kind { get; private set; }

		/// <summary>この引数が名前付き引数であれば、この引数の名前を取得します。それ以外の場合は <c>null</c> を返します。</summary>
		public string Name { get; private set; }

		/// <summary>指定された名前を使用して、<see cref="Microsoft.Scripting.Actions.Argument"/> 構造体の新しいインスタンスを初期化します。</summary>
		/// <param name="name">この名前付き引数の名前を指定します。</param>
		public Argument(string name) : this(ArgumentType.Named, name) { }

		/// <summary>引数の種類を使用して、<see cref="Microsoft.Scripting.Actions.Argument"/> 構造体の新しいインスタンスを初期化します。</summary>
		/// <param name="kind">この引数の種類を指定します。<see cref="ArgumentType.Named"/> を指定することはできません。</param>
		public Argument(ArgumentType kind) : this(kind, null) { }

		/// <summary>引数の名前および種類を使用して、<see cref="Microsoft.Scripting.Actions.Argument"/> 構造体の新しいインスタンスを初期化します。</summary>
		/// <param name="kind">この引数の種類を指定します。</param>
		/// <param name="name">引数の種類が名前付き引数の場合、引数の名前を指定します。</param>
		public Argument(ArgumentType kind, string name) : this()
		{
			ContractUtils.Requires((kind == ArgumentType.Named) ^ (name == null), "kind");
			Kind = kind;
			Name = name;
		}

		/// <summary>このオブジェクトが指定されたオブジェクトと等しいかどうかを判断します。</summary>
		/// <param name="obj">等価性を判断するオブジェクトを指定します。</param>
		/// <returns>このオブジェクトが指定されたオブジェクト等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public override bool Equals(object obj) { return obj is Argument && Equals((Argument)obj); }

		/// <summary>この引数が指定された引数と等しいかどうかを判断します。</summary>
		/// <param name="other">等しいかどうかを判断する <see cref="Argument"/> オブジェクトを指定します。</param>
		/// <returns>この引数と指定された引数が等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		[StateIndependent]
		public bool Equals(Argument other) { return Kind == other.Kind && Name == other.Name; }

		/// <summary>指定された 2 つの <see cref="Argument"/> オブジェクトが等しいかどうかを判断します。</summary>
		/// <param name="left">比較する 1 番目の <see cref="Argument"/>。</param>
		/// <param name="right">比較する 2 番目の <see cref="Argument"/>。</param>
		/// <returns>2 つの <see cref="Argument"/> オブジェクトが等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool operator ==(Argument left, Argument right) { return left.Equals(right); }

		/// <summary>指定された 2 つの <see cref="Argument"/> オブジェクトが等しくないかどうかを判断します。</summary>
		/// <param name="left">比較する 1 番目の <see cref="Argument"/>。</param>
		/// <param name="right">比較する 2 番目の <see cref="Argument"/>。</param>
		/// <returns>2 つの <see cref="Argument"/> オブジェクトが等しくない場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool operator !=(Argument left, Argument right) { return !left.Equals(right); }

		/// <summary>このオブジェクトのハッシュ値を計算します。</summary>
		/// <returns>計算されたハッシュ値。</returns>
		public override int GetHashCode() { return Name != null ? Name.GetHashCode() ^ (int)Kind : (int)Kind; }

		/// <summary>この引数が単純な引数かどうかを示す値を取得します。</summary>
		public bool IsSimple { get { return Equals(Simple); } }
		
		/// <summary>このオブジェクトの文字列表現を返します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		public override string ToString() { return Name == null ? Kind.ToString() : Kind.ToString() + ":" + Name; }

		/// <summary>このオブジェクトを表す <see cref="Expression"/> を作成します。</summary>
		/// <returns>このオブジェクトを表す <see cref="Expression"/>。</returns>
		internal Expression CreateExpression()
		{
			return Expression.New(typeof(Argument).GetConstructor(new Type[] { typeof(ArgumentType), typeof(string) }),
				AstUtils.Constant(Kind),
				AstUtils.Constant(Name, typeof(string))
			);
		}
	}
}


