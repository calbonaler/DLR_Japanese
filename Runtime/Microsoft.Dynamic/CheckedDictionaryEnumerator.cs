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
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Scripting
{
	/// <summary>
	/// 列挙の状態を監視して無効な列挙操作で例外を発生させる列挙子の抽象基本クラスを表します。
	/// このクラスは主に非ジェネリック ディクショナリの列挙に使用されます。
	/// </summary>
	public abstract class CheckedDictionaryEnumerator : IDictionaryEnumerator, IEnumerator<KeyValuePair<object, object>>
	{
		EnumeratorState _enumeratorState = EnumeratorState.NotStarted;

		void CheckEnumeratorState()
		{
			if (_enumeratorState == EnumeratorState.NotStarted)
				throw Error.EnumerationNotStarted();
			else if (_enumeratorState == EnumeratorState.Ended)
				throw Error.EnumerationFinished();
		}

		/// <summary>現在のディクショナリ エントリのキーと値の両方を取得します。</summary>
		public DictionaryEntry Entry
		{
			get
			{
				CheckEnumeratorState();
				return new DictionaryEntry(Key, Value);
			}
		}

		/// <summary>現在のディクショナリ エントリのキーを取得します。</summary>
		public object Key
		{
			get
			{
				CheckEnumeratorState();
				return KeyCore;
			}
		}

		/// <summary>現在のディクショナリ エントリの値を取得します。</summary>
		public object Value
		{
			get
			{
				CheckEnumeratorState();
				return ValueCore;
			}
		}

		/// <summary>列挙子をコレクションの次の要素に進めます。</summary>
		/// <returns>列挙子が次の要素に正常に進んだ場合は <c>true</c>。列挙子がコレクションの末尾を越えた場合は <c>false</c>。</returns>
		/// <exception cref="InvalidOperationException">列挙子が作成された後に、コレクションが変更されました。</exception>
		public bool MoveNext()
		{
			if (_enumeratorState == EnumeratorState.Ended)
				throw Error.EnumerationFinished();
			var result = MoveNextCore();
			if (result)
				_enumeratorState = EnumeratorState.Started;
			else
				_enumeratorState = EnumeratorState.Ended;
			return result;
		}

		/// <summary>コレクション内の現在の要素を取得します。</summary>
		public object Current { get { return Entry; } }

		/// <summary>列挙子を初期位置、つまりコレクションの最初の要素の前に設定します。</summary>
		/// <exception cref="InvalidOperationException">列挙子が作成された後に、コレクションが変更されました。</exception>
		public void Reset()
		{
			ResetCore();
			_enumeratorState = EnumeratorState.NotStarted;
		}

		/// <summary>列挙子の現在位置にあるコレクション内の要素を取得します。</summary>
		KeyValuePair<object, object> IEnumerator<KeyValuePair<object, object>>.Current { get { return new KeyValuePair<object, object>(Key, Value); } }

		/// <summary>アンマネージ リソースの解放およびリセットに関連付けられているアプリケーション定義のタスクを実行します。</summary>
		public void Dispose() { GC.SuppressFinalize(this); }

		/// <summary>現在のディクショナリ エントリのキーを取得します。</summary>
		protected abstract object KeyCore { get; }

		/// <summary>現在のディクショナリ エントリの値を取得します。</summary>
		protected abstract object ValueCore { get; }

		/// <summary>列挙子をコレクションの次の要素に進めます。</summary>
		/// <returns>列挙子が次の要素に正常に進んだ場合は <c>true</c>。列挙子がコレクションの末尾を越えた場合は <c>false</c>。</returns>
		protected abstract bool MoveNextCore();

		/// <summary>列挙子を初期位置、つまりコレクションの最初の要素の前に設定します。</summary>
		protected abstract void ResetCore();

		enum EnumeratorState
		{
			NotStarted,
			Started,
			Ended
		}
	}
}
