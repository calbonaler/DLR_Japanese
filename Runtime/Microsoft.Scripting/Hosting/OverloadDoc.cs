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
using System.Collections.Generic;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting
{
	/// <summary>呼び出し可能オブジェクトの単一のオーバーロードに対するドキュメントを提供します。</summary>
	[Serializable]
	public class OverloadDoc
	{
		/// <summary>名前、ドキュメント、引数リストを使用して、<see cref="Microsoft.Scripting.Hosting.OverloadDoc"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="name">呼び出し可能オブジェクトの名前を指定します。</param>
		/// <param name="documentation">オーバーロードのドキュメントを指定します。null を指定することができます。</param>
		/// <param name="parameters">呼び出し可能オブジェクトの引数を指定します。</param>
		public OverloadDoc(string name, string documentation, ICollection<ParameterDoc> parameters) : this(name, documentation, parameters, null) { }

		/// <summary>名前、ドキュメント、引数リスト、戻り値を使用して、<see cref="Microsoft.Scripting.Hosting.OverloadDoc"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="name">呼び出し可能オブジェクトの名前を指定します。</param>
		/// <param name="documentation">オーバーロードのドキュメントを指定します。null を指定することができます。</param>
		/// <param name="parameters">呼び出し可能オブジェクトの引数を指定します。</param>
		/// <param name="returnParameter">戻り値に関する情報を格納する <see cref="Microsoft.Scripting.Hosting.ParameterDoc"/> オブジェクトを指定します。</param>
		public OverloadDoc(string name, string documentation, ICollection<ParameterDoc> parameters, ParameterDoc returnParameter)
		{
			ContractUtils.RequiresNotNull(name, "name");
			ContractUtils.RequiresNotNullItems(parameters, "parameters");
			Name = name;
			Parameters = parameters;
			Documentation = documentation;
			ReturnParameter = returnParameter;
		}

		/// <summary>呼び出し可能オブジェクトの名前を取得します。</summary>
		public string Name { get; private set; }

		/// <summary>オーバーロードのドキュメントを取得します。</summary>
		public string Documentation { get; private set; }

		/// <summary>呼び出し可能オブジェクトの引数を取得します。</summary>
		public ICollection<ParameterDoc> Parameters { get; private set; }

		/// <summary>戻り値に関する情報を格納する <see cref="Microsoft.Scripting.Hosting.ParameterDoc"/> オブジェクトを取得します。</summary>
		public ParameterDoc ReturnParameter { get; private set; }
	}
}
