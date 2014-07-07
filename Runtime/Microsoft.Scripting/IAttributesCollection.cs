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

using System.Collections.Generic;

namespace Microsoft.Scripting
{
	/// <summary>シンボルおよび任意のオブジェクトを使用することでアクセス可能なディクショナリを表します。</summary>
	/// <remarks>
	/// このインターフェイスは概念的に <see cref="IDictionary&lt;Object, Object&gt;"/> を継承しますが、
	/// オブジェクトではなく <see cref="SymbolId"/> にインデックスされるようにしたいのでそのようにしません。
	/// </remarks>
	public interface IAttributesCollection : IEnumerable<KeyValuePair<object, object>>
	{
		/// <summary>指定したキーおよび値を持つ要素を <see cref="Microsoft.Scripting.IAttributesCollection"/> に追加します。</summary>
		/// <param name="name">追加する要素のキーとして使用する <see cref="SymbolId"/>。</param>
		/// <param name="value">追加する要素の値として使用するオブジェクト。</param>
		void Add(SymbolId name, object value);

		/// <summary>指定したキーに関連付けられている値を取得します。</summary>
		/// <param name="name">値を取得する対象のキー。</param>
		/// <param name="value">
		/// このメソッドが返されるときに、キーが見つかった場合は、指定したキーに関連付けられている値。それ以外の場合は <c>null</c>。
		/// このパラメーターは初期化せずに渡されます。
		/// </param>
		/// <returns>指定したキーを持つ要素が <see cref="Microsoft.Scripting.IAttributesCollection"/> を実装するオブジェクトに格納されている場合は
		/// <c>true</c>。それ以外の場合は <c>false</c>。
		/// </returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate")]
		bool TryGetValue(SymbolId name, out object value);

		/// <summary>指定したキーを持つ要素を <see cref="Microsoft.Scripting.IAttributesCollection"/> から削除します。</summary>
		/// <param name="name">削除する要素のキー。</param>
		/// <returns>
		/// 要素が正常に削除された場合は <c>true</c>。それ以外の場合は <c>false</c>。
		/// このメソッドは、<paramref name="name"/> が元の <see cref="Microsoft.Scripting.IAttributesCollection"/> に見つからなかった場合にも <c>false</c> を返します。
		/// </returns>
		bool Remove(SymbolId name);

		/// <summary>指定したキーの要素が <see cref="Microsoft.Scripting.IAttributesCollection"/> に格納されているかどうかを確認します。</summary>
		/// <param name="name"><see cref="Microsoft.Scripting.IAttributesCollection"/> 内で検索されるキー。</param>
		/// <returns>指定したキーを持つ要素を <see cref="Microsoft.Scripting.IAttributesCollection"/> が保持している場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		bool ContainsKey(SymbolId name);

		/// <summary>指定したキーを持つ要素を取得または設定します。</summary>
		/// <param name="name">取得または設定する要素のキー。</param>
		/// <returns>指定したキーを持つ要素。</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1043:UseIntegralOrStringArgumentForIndexers")]
		object this[SymbolId name] { get; set; }

		/// <summary><see cref="SymbolId"/> がキーである属性のディクショナリを取得します。</summary>
		IDictionary<SymbolId, object> SymbolAttributes { get; }

		/// <summary>指定したキーおよび値を持つ要素を <see cref="Microsoft.Scripting.IAttributesCollection"/> に追加します。</summary>
		/// <param name="name">追加する要素のキーとして使用するオブジェクト。</param>
		/// <param name="value">追加する要素の値として使用するオブジェクト。</param>
		void Add(object name, object value);

		/// <summary>指定したキーに関連付けられている値を取得します。</summary>
		/// <param name="name">値を取得する対象のキー。</param>
		/// <param name="value">
		/// このメソッドが返されるときに、キーが見つかった場合は、指定したキーに関連付けられている値。それ以外の場合は <c>null</c>。
		/// このパラメーターは初期化せずに渡されます。
		/// </param>
		/// <returns>指定したキーを持つ要素が <see cref="Microsoft.Scripting.IAttributesCollection"/> を実装するオブジェクトに格納されている場合は
		/// <c>true</c>。それ以外の場合は <c>false</c>。
		/// </returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate")]
		bool TryGetValue(object name, out object value);

		/// <summary>指定したキーを持つ要素を <see cref="Microsoft.Scripting.IAttributesCollection"/> から削除します。</summary>
		/// <param name="name">削除する要素のキー。</param>
		/// <returns>
		/// 要素が正常に削除された場合は <c>true</c>。それ以外の場合は <c>false</c>。
		/// このメソッドは、<paramref name="name"/> が元の <see cref="Microsoft.Scripting.IAttributesCollection"/> に見つからなかった場合にも <c>false</c> を返します。
		/// </returns>
		bool Remove(object name);

		/// <summary>指定したキーの要素が <see cref="Microsoft.Scripting.IAttributesCollection"/> に格納されているかどうかを確認します。</summary>
		/// <param name="name"><see cref="Microsoft.Scripting.IAttributesCollection"/> 内で検索されるキー。</param>
		/// <returns>指定したキーを持つ要素を <see cref="Microsoft.Scripting.IAttributesCollection"/> が保持している場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		bool ContainsKey(object name);

		/// <summary>このオブジェクトを <see cref="System.Collections.Generic.IDictionary&lt;Object, Object&gt;"/> として取得します。</summary>
		/// <returns><see cref="System.Collections.Generic.IDictionary&lt;Object, Object&gt;"/> という形式で取得された現在のオブジェクト。</returns>
		IDictionary<object, object> AsObjectKeyedDictionary();

		/// <summary><see cref="Microsoft.Scripting.IAttributesCollection"/> に格納されている要素の数を取得します。</summary>
		int Count { get; }

		/// <summary><see cref="Microsoft.Scripting.IAttributesCollection"/> のキーを保持している <see cref="System.Collections.Generic.ICollection&lt;Object&gt;"/> を取得します。</summary>
		ICollection<object> Keys { get; }
	}
}
