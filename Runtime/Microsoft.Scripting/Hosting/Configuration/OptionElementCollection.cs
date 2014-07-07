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

using System.Configuration;

namespace Microsoft.Scripting.Hosting.Configuration
{
	/// <summary>言語オプションに関する構成要素のコレクションを格納する要素を表します。</summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1010:CollectionsShouldImplementGenericInterface")]
	public class OptionElementCollection : ConfigurationElementCollection
	{
		/// <summary><see cref="Microsoft.Scripting.Hosting.Configuration.OptionElementCollection"/> クラスの新しいインスタンスを初期化します。</summary>
		public OptionElementCollection() { AddElementName = "set"; }

		/// <summary><see cref="System.Configuration.ConfigurationElementCollection"/> の型を取得します。</summary>
		public override ConfigurationElementCollectionType CollectionType { get { return ConfigurationElementCollectionType.AddRemoveClearMap; } }

		/// <summary>新しい <see cref="System.Configuration.ConfigurationElement"/> を作成します。</summary>
		/// <returns>新しく作成した <see cref="System.Configuration.ConfigurationElement"/>。</returns>
		protected override ConfigurationElement CreateNewElement() { return new OptionElement(); }

		/// <summary>
		/// 重複する <see cref="System.Configuration.ConfigurationElement"/> を <see cref="System.Configuration.ConfigurationElementCollection"/>
		/// に追加しようとしたときに、例外をスローするかどうかを示す値を取得します。
		/// </summary>
		protected override bool ThrowOnDuplicate { get { return false; } }

		/// <summary>指定した構成要素の要素キーを取得します。</summary>
		/// <param name="element">キーを返す <see cref="System.Configuration.ConfigurationElement"/>。</param>
		/// <returns>指定した <see cref="System.Configuration.ConfigurationElement"/> のキーとして機能する <see cref="System.Object"/>。</returns>
		protected override object GetElementKey(ConfigurationElement element) { return ((OptionElement)element).GetKey(); }
	}
}