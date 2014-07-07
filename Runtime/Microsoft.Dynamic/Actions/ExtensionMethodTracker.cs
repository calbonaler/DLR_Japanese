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
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions
{
	/// <summary>拡張メソッドを表します。</summary>
	public class ExtensionMethodTracker : MethodTracker
	{
		readonly Type _declaringType;

		/// <summary>メソッド、静的性、拡張メソッドが拡張する型を使用して、<see cref="Microsoft.Scripting.Actions.ExtensionMethodTracker"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="method">拡張メソッドを表す <see cref="MethodInfo"/> を指定します。</param>
		/// <param name="isStatic">指定された拡張メソッドが静的かどうかを示す値を指定します。</param>
		/// <param name="declaringType">指定された拡張メソッドが拡張する型を指定します。</param>
		internal ExtensionMethodTracker(MethodInfo method, bool isStatic, Type declaringType)
			: base(method, isStatic)
		{
			ContractUtils.RequiresNotNull(declaringType, "declaringType");
			_declaringType = declaringType;
		}

		/// <summary>
		/// 拡張メソッドの宣言する型を取得します。
		/// このメソッドは拡張メソッドなので、宣言する型は実際には子の拡張メソッドが拡張する型であり、実際に宣言された型とは異なります。
		/// </summary>
		public override Type DeclaringType { get { return _declaringType; } }
	}
}
