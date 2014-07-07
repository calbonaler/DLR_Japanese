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
	/// <summary>アセンブリ内のクラスを他の型に対する拡張型としてマークします。</summary>
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
	public sealed class ExtensionTypeAttribute : Attribute
	{
		/// <summary>拡張する型と拡張される型を使用して、<see cref="Microsoft.Scripting.Runtime.ExtensionTypeAttribute"/> クラスの新しいインスタンスを初期化しますｊ。</summary>
		/// <param name="extends">拡張される型を指定します。</param>
		/// <param name="extensionType">拡張メンバを提供する型を指定します。</param>
		public ExtensionTypeAttribute(Type extends, Type extensionType)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");
			if (extensionType != null && !extensionType.IsPublic && !extensionType.IsNestedPublic)
				throw Error.ExtensionMustBePublic(extensionType.FullName);
			Extends = extends;
			ExtensionType = extensionType;
		}

		/// <summary>拡張される型に追加する拡張メンバを含んでいる型を取得します。</summary>
		public Type ExtensionType { get; private set; }

		/// <summary><see cref="ExtensionType"/> によって拡張される型を取得します。</summary>
		public Type Extends { get; private set; }
	}
}
