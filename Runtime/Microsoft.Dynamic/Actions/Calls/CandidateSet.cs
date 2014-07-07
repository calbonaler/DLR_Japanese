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
using System.Diagnostics;
using Microsoft.Contracts;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>すべて同じ数の論理引数を受け入れる <see cref="MethodCandidate"/> のコレクションを表します。</summary>
	sealed class CandidateSet : IList<MethodCandidate>
	{
		List<MethodCandidate> candidates = new List<MethodCandidate>();

		/// <summary>受け入れる論理引数の数を使用して、<see cref="Microsoft.Scripting.Actions.Calls.CandidateSet"/> クラスの新しいインスタンスを作成します。</summary>
		/// <param name="count">受け入れる論理引数の数を指定します。</param>
		internal CandidateSet(int count)
		{
			Arity = count;
			candidates = new List<MethodCandidate>();
		}

		/// <summary>受け入れる論理引数の数を取得します。</summary>
		internal int Arity { get; private set; }

		/// <summary>格納されているすべての <see cref="MethodCandidate"/> の引数リストに辞書引数が存在するかどうかを示す値を返します。</summary>
		/// <returns>すべての <see cref="MethodCandidate"/> の引数リストに辞書引数が存在すれば <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		internal bool IsParamsDictionaryOnly() { return candidates.TrueForAll(x => x.HasParamsDictionary); }

		/// <summary>このコレクション内で指定された <see cref="MethodCandidate"/> が存在する位置を返します。</summary>
		/// <param name="item">このコレクション内から検索する <see cref="MethodCandidate"/> を指定します。</param>
		/// <returns>指定された <see cref="MethodCandidate"/> が存在するインデックス。見つからない場合は -1。</returns>
		public int IndexOf(MethodCandidate item) { return candidates.IndexOf(item); }

		/// <summary>このコレクションの指定された位置に新しい <see cref="MethodCandidate"/> を追加します。</summary>
		/// <param name="index"><see cref="MethodCandidate"/> を追加する位置を指定します。</param>
		/// <param name="item">追加する <see cref="MethodCandidate"/> を指定します。</param>
		public void Insert(int index, MethodCandidate item)
		{
			Debug.Assert(item.Parameters.Count == Arity);
			candidates.Insert(index, item);
		}

		/// <summary>このコレクションから指定された位置にある <see cref="MethodCandidate"/> を削除します。</summary>
		/// <param name="index">削除する <see cref="MethodCandidate"/> の位置を指定します。</param>
		public void RemoveAt(int index) { candidates.RemoveAt(index); }

		/// <summary>このコレクション内の指定された位置にある <see cref="MethodCandidate"/> を取得または設定します。</summary>
		/// <param name="index"><see cref="MethodCandidate"/> を取得または設定する位置を指定します。</param>
		/// <returns>指定された位置にある <see cref="MethodCandidate"/>。</returns>
		public MethodCandidate this[int index]
		{
			get { return candidates[index]; }
			set
			{
				Debug.Assert(value.Parameters.Count == Arity);
				candidates[index] = value;
			}
		}

		/// <summary>このコレクションに新しい <see cref="MethodCandidate"/> を追加します。</summary>
		/// <param name="item">追加する <see cref="MethodCandidate"/> を指定します。</param>
		public void Add(MethodCandidate item)
		{
			Debug.Assert(item.Parameters.Count == Arity);
			candidates.Add(item);
		}

		/// <summary>このコレクション内のすべての <see cref="MethodCandidate"/> を削除します。</summary>
		public void Clear() { candidates.Clear(); }

		/// <summary>このコレクションに指定された <see cref="MethodCandidate"/> が存在するかどうかを返します。</summary>
		/// <param name="item">存在するかどうかを調べる <see cref="MethodCandidate"/> を指定します。</param>
		/// <returns>コレクション内に <see cref="MethodCandidate"/> が存在すれば <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool Contains(MethodCandidate item) { return candidates.Contains(item); }

		/// <summary>このコレクション内の <see cref="MethodCandidate"/> を指定された配列にコピーします。</summary>
		/// <param name="array"><see cref="MethodCandidate"/> がコピーされる配列を指定します。</param>
		/// <param name="arrayIndex"><paramref name="array"/> 内のコピーが開始される 0 から始まるインデックスを指定します。</param>
		public void CopyTo(MethodCandidate[] array, int arrayIndex) { candidates.CopyTo(array, arrayIndex); }

		/// <summary>このコレクションに格納されている <see cref="MethodCandidate"/> の数を取得します。</summary>
		public int Count { get { return candidates.Count; } }

		/// <summary>このコレクションが読み取り専用かどうかを示す値を取得します。</summary>
		public bool IsReadOnly { get { return false; } }

		/// <summary>このコレクション内から指定された <see cref="MethodCandidate"/> を削除します。</summary>
		/// <param name="item">削除する <see cref="MethodCandidate"/> を指定します。</param>
		/// <returns>削除が正常に実行された場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool Remove(MethodCandidate item) { return candidates.Remove(item); }

		/// <summary>このコレクションを反復処理する列挙子を返します。</summary>
		/// <returns>コレクションの反復処理に使用する列挙子。</returns>
		public IEnumerator<MethodCandidate> GetEnumerator() { return candidates.GetEnumerator(); }

		/// <summary>このコレクションを反復処理する列挙子を返します。</summary>
		/// <returns>コレクションの反復処理に使用する列挙子。</returns>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		
		/// <summary>このオブジェクトの文字列表現を取得します。</summary>
		/// <returns>このオブジェクトの文字列表現。</returns>
		[Confined]
		public override string ToString() { return string.Format("{0}: ({1} on {2})", Arity, candidates[0].Overload.Name, candidates[0].Overload.DeclaringType.FullName); }
	}
}
