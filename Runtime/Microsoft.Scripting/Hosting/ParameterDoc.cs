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
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting
{
	/// <summary>単一の引数に関するドキュメントを提供します。</summary>
	[Serializable]
	public class ParameterDoc
	{
		/// <summary>名前を使用して、<see cref="Microsoft.Scripting.Hosting.ParameterDoc"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="name">引数の名前を指定します。</param>
		public ParameterDoc(string name) : this(name, null, null, ParameterFlags.None) { }

		/// <summary>名前および引数に関する追加情報を使用して、<see cref="Microsoft.Scripting.Hosting.ParameterDoc"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="name">引数の名前を指定します。</param>
		/// <param name="paramFlags">引数に関する追加情報を表す <see cref="Microsoft.Scripting.Hosting.ParameterFlags"/> を指定します。</param>
		public ParameterDoc(string name, ParameterFlags paramFlags) : this(name, null, null, paramFlags) { }

		/// <summary>名前および引数の型名を使用して、<see cref="Microsoft.Scripting.Hosting.ParameterDoc"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="name">引数の名前を指定します。</param>
		/// <param name="typeName">引数の型名を指定します。null を指定することもできます。</param>
		public ParameterDoc(string name, string typeName) : this(name, typeName, null, ParameterFlags.None) { }

		/// <summary>名前、引数の型名およびドキュメントを使用して、<see cref="Microsoft.Scripting.Hosting.ParameterDoc"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="name">引数の名前を指定します。</param>
		/// <param name="typeName">引数の型名を指定します。null を指定することもできます。</param>
		/// <param name="documentation">引数に関するドキュメントを指定します。null を指定することもできます。</param>
		public ParameterDoc(string name, string typeName, string documentation) : this(name, typeName, documentation, ParameterFlags.None) { }

		/// <summary>名前、引数の型名、ドキュメントおよび追加情報を使用して、<see cref="Microsoft.Scripting.Hosting.ParameterDoc"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="name">引数の名前を指定します。</param>
		/// <param name="typeName">引数の型名を指定します。null を指定することもできます。</param>
		/// <param name="documentation">引数に関するドキュメントを指定します。null を指定することもできます。</param>
		/// <param name="paramFlags">引数に関する追加情報を表す <see cref="Microsoft.Scripting.Hosting.ParameterFlags"/> を指定します。</param>
		public ParameterDoc(string name, string typeName, string documentation, ParameterFlags paramFlags)
		{
			ContractUtils.RequiresNotNull(name, "name");
			Name = name;
			Flags = paramFlags;
			TypeName = typeName;
			Documentation = documentation;
		}

		/// <summary>引数の名前を取得します。</summary>
		public string Name { get; private set; }

		/// <summary>型情報が利用可能ならば、引数の型名を取得します。</summary>
		public string TypeName { get; private set; }

		/// <summary>引数に関する追加情報を取得します。</summary>
		public ParameterFlags Flags { get; private set; }

		/// <summary>この引数に対するドキュメントを取得します。</summary>
		public string Documentation { get; private set; }
	}
}
