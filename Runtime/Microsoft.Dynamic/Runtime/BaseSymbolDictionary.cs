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

namespace Microsoft.Scripting.Runtime
{
	/// <summary>
	/// <see cref="SymbolId"/> を使用するディクショナリの基本クラスです。
	/// <see cref="SymbolId"/> ディクショナリはクラスのメンバ、関数の環境、関数のローカル変数、およびその他の名前によってインデックス化された場所の検索に使用される高速なディクショナリです。
	/// <see cref="SymbolId"/> ディクショナリは <see cref="SymbolId"/> と
	/// (直接ユーザーコードに公開された場合は <see cref="T:System.Collections.Generic.Dictionary&lt;System.Object, System.Object&gt;"/> としてのディクショナリへの遅延バインディングアクセスをサポートする)
	/// <see cref="System.Object"/> によるキーをサポートします。
	/// <see cref="System.Object"/> によるインデックス化の場合は <c>null</c> は有効なキーとなります。
	/// </summary>
	public abstract class BaseSymbolDictionary
	{
		static readonly object _nullObject = new object();
		const int ObjectKeysId = -2;

		/// <summary><see cref="SymbolId"/> ディクショナリのオブジェクトを格納するキーを表します。</summary>
		internal static readonly SymbolId ObjectKeys = new SymbolId(ObjectKeysId);

		/// <summary><see cref="Microsoft.Scripting.Runtime.BaseSymbolDictionary"/> クラスの新しいインスタンスを初期化します。</summary>
		protected BaseSymbolDictionary() { }

		/// <summary>値のハッシュコードを求めます。常に <see cref="ArgumentTypeException"/> をスローします。</summary>
		/// <exception cref="ArgumentTypeException">ディクショナリはハッシュ可能ではありません。</exception>
		public int GetValueHashCode() { throw Error.DictionaryNotHashable(); }

		/// <summary>このオブジェクトと指定されたオブジェクトに含まれている値が等しいかどうかを判断します。</summary>
		/// <param name="other">値を比較するオブジェクトを指定します。</param>
		/// <returns>このオブジェクトと指定されたオブジェクトに含まれている値が等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public virtual bool ValueEquals(object other)
		{
			if (ReferenceEquals(this, other))
				return true;
			var oth = other as IAttributesCollection;
			var ths = this as IAttributesCollection;
			if (oth == null)
				return false;
			if (oth.Count != ths.Count)
				return false;
			foreach (var o in ths)
			{
				object res;
				if (!oth.TryGetValue(o.Key, out res))
					return false;
				if (res != null)
				{
					if (!res.Equals(o.Value))
						return false;
				}
				else if (o.Value != null)
				{
					if (!o.Value.Equals(res))
						return false;
				} // else both null and are equal
			}
			return true;
		}

		/// <summary>指定されたオブジェクトが <c>null</c> の場合は <c>null</c> オブジェクトに変換します。</summary>
		/// <param name="obj">変換するオブジェクトを指定します。</param>
		/// <returns>オブジェクトが <c>null</c> の場合は <c>null</c> オブジェクト。それ以外の場合は元のオブジェクト。</returns>
		public static object NullToObj(object obj) { return obj == null ? _nullObject : obj; }

		/// <summary>指定された <c>null</c> オブジェクトを <c>null</c> に変換します。</summary>
		/// <param name="obj">変換するオブジェクトを指定します。</param>
		/// <returns>オブジェクトが <c>null</c> オブジェクトの場合は <c>null</c>。それ以外の場合は元のオブジェクト。</returns>
		public static object ObjToNull(object obj) { return obj == _nullObject ? null : obj; }

		/// <summary>指定されたオブジェクトが <c>null</c> オブジェクトかどうかを判断します。</summary>
		/// <param name="obj">判断するオブジェクトを指定します。</param>
		/// <returns>オブジェクトが <c>null</c> オブジェクトの場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool IsNullObject(object obj) { return obj == _nullObject; }
	}
}
