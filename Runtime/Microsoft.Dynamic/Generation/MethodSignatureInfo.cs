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

using System.Linq;
using System.Reflection;
using Microsoft.Contracts;

namespace Microsoft.Scripting.Generation
{
	/// <summary>
	/// 等価なシグネチャをもつメソッドを取り除くヘルパー クラスです。
	/// 継承階層内のすべての型からメンバを返す GetDefaultMembers によって使用されます。
	/// </summary>
	public class MethodSignatureInfo
	{
		readonly ParameterInfo[] _pis;
		readonly bool _isStatic;
		readonly int _genericArity;

		/// <summary>メソッドの情報を保持する <see cref="MethodInfo"/> を使用して、<see cref="Microsoft.Scripting.Generation.MethodSignatureInfo"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="info">シグネチャ情報を取り出すメソッドを表す <see cref="MethodInfo"/> を指定します。</param>
		public MethodSignatureInfo(MethodInfo info) : this(info.IsStatic, info.GetParameters(), info.IsGenericMethodDefinition ? info.GetGenericArguments().Length : 0) { }

		/// <summary>メソッドの情報を直接指定することで、<see cref="Microsoft.Scripting.Generation.MethodSignatureInfo"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="isStatic">メソッドが静的であるかどうかを示す値を指定します。</param>
		/// <param name="pis">メソッドの仮引数に関する情報を格納する <see cref="ParameterInfo"/> の配列を指定します。</param>
		/// <param name="genericArity">メソッドのジェネリック型パラメータの数を指定します。</param>
		public MethodSignatureInfo(bool isStatic, ParameterInfo[] pis, int genericArity)
		{
			_isStatic = isStatic;
			_pis = pis;
			_genericArity = genericArity;
		}

		/// <summary>このオブジェクトが指定されたオブジェクトと等しいかどうかを判断します。</summary>
		/// <param name="obj">判断するオブジェクトを指定します。</param>
		/// <returns>このオブジェクトが指定されたオブジェクトと等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		[Confined]
		public override bool Equals(object obj)
		{
			MethodSignatureInfo args = obj as MethodSignatureInfo;
			return args != null && _isStatic == args._isStatic && _genericArity == args._genericArity && _pis.Select(x => x.ParameterType).SequenceEqual(args._pis.Select(x => x.ParameterType));
		}

		/// <summary>このオブジェクトのハッシュ値を計算します。</summary>
		/// <returns>オブジェクトのハッシュ値。</returns>
		[Confined]
		public override int GetHashCode() { return _pis.Aggregate(6551 ^ (_isStatic ? 79234 : 3123) ^ _genericArity, (x, y) => x ^ y.ParameterType.GetHashCode()); }
	}
}
