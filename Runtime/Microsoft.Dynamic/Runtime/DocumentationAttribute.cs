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

namespace Microsoft.Scripting.Runtime
{
	/// <summary> 
	/// アセンブリ内にメタデータとして格納されているドキュメントを提供する機構を提供します。
	/// この属性を適用すると、XML ドキュメントが利用できない場合でも実行時にユーザーに対してドキュメントを提供することが可能になります。
	/// </summary>
	[AttributeUsage(AttributeTargets.All)]
	public sealed class DocumentationAttribute : Attribute
	{
		/// <summary>提供するドキュメントを指定して、<see cref="Microsoft.Scripting.Runtime.DocumentationAttribute"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="documentation">ユーザーに提供されるドキュメントを指定します。</param>
		public DocumentationAttribute(string documentation) { Documentation = documentation; }

		/// <summary>ユーザーに提供されるドキュメントを取得します。</summary>
		public string Documentation { get; private set; }
	}
}
