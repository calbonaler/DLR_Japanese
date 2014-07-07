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
using Microsoft.Contracts;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>言語コンテキストを表します。</summary>
	/// <remarks>
	/// 典型的にそれぞれの言語に関連付けられたコンテキストが多くとも 1 つ存在しますが、
	/// 異なる扱いをされるコードを識別するために 1 つ以上のコンテキストを使用する言語もあります。
	/// コンテキストはメンバまたは演算子の探索中に使用されます。
	/// </remarks>
	[Serializable]
	public struct ContextId : IEquatable<ContextId>
	{
		static Dictionary<object, ContextId> _contexts = new Dictionary<object, ContextId>();
		static int _maxId = 1;

		/// <summary>空のコンテキストを表します。</summary>
		public static readonly ContextId Empty = new ContextId();

		/// <summary>指定された ID を用いて <see cref="Microsoft.Scripting.Runtime.ContextId"/> 構造体の新しいインスタンスを初期化します。</summary>
		/// <param name="id">このインスタンスの ID を指定します。</param>
		internal ContextId(int id) : this() { Id = id; }

		/// <summary>指定された名前でシステム内の言語を登録します。</summary>
		public static ContextId RegisterContext(object identifier)
		{
			lock (_contexts)
			{
				ContextId res;
				if (_contexts.TryGetValue(identifier, out res))
					throw Error.LanguageRegistered();
				ContextId id = new ContextId();
				id.Id = _maxId++;
				return id;
			}
		}

		/// <summary>指定されたコンテキスト識別子に対応する <see cref="ContextId"/> を検索します。</summary>
		public static ContextId LookupContext(object identifier)
		{
			ContextId res;
			lock (_contexts)
			{
				if (_contexts.TryGetValue(identifier, out res))
					return res;
			}
			return ContextId.Empty;
		}

		/// <summary>このインスタンスの ID を取得します。</summary>
		public int Id { get; private set; }

		/// <summary>指定された <see cref="ContextId"/> が現在の <see cref="ContextId"/> と等しいかどうかを判断します。</summary>
		/// <param name="other">比較する <see cref="ContextId"/> を指定します。</param>
		[StateIndependent]
		public bool Equals(ContextId other) { return Id == other.Id; }

		/// <summary>現在の <see cref="ContextId"/> に対するハッシュ値を返します。</summary>
		public override int GetHashCode() { return Id; }

		/// <summary>現在の <see cref="ContextId"/> が指定されたオブジェクトと等しいかどうかを判断します。</summary>
		/// <param name="obj">比較するオブジェクトを指定します。</param>
		public override bool Equals(object obj) { return obj is ContextId && Equals((ContextId)obj); }

		/// <summary>指定された 2 つの <see cref="ContextId"/> が等しいかどうかを判断します。</summary>
		/// <param name="self">比較する 1 つ目のオブジェクトを指定します。</param>
		/// <param name="other">比較する 2 つ目のオブジェクトを指定します。</param>
		public static bool operator ==(ContextId self, ContextId other) { return self.Equals(other); }

		/// <summary>指定された 2 つの <see cref="ContextId"/> が等しくないかどうかを判断します。</summary>
		/// <param name="self">比較する 1 つ目のオブジェクトを指定します。</param>
		/// <param name="other">比較する 2 つ目のオブジェクトを指定します。</param>
		public static bool operator !=(ContextId self, ContextId other) { return !self.Equals(other); }
	}
}
