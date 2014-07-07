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
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Contracts;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions
{
	/// <summary>コールサイトのシグネチャを豊富に表します。</summary>
	public struct CallSignature : IEquatable<CallSignature>
	{
		// TODO: invariant _infos != null ==> _argumentCount == _infos.Length

		/// <summary>
		/// 名前付き引数のような引数に関する追加の情報を格納します。
		/// 単純なシグネチャ、つまり式のリストの場合は null になります。
		/// </summary>
		readonly Argument[] _infos;
		/// <summary>シグネチャ内に含まれる引数の個数です。</summary>
		readonly int _argumentCount;

		/// <summary>すべての引数が名前付きでなく、位置が既に決定されているかどうかを示す値を取得します。</summary>
		public bool IsSimple { get { return _infos == null; } }

		/// <summary>シグネチャ内に含まれる引数の個数を取得します。</summary>
		public int ArgumentCount
		{
			get
			{
				Debug.Assert(_infos == null || _infos.Length == _argumentCount);
				return _argumentCount;
			}
		}

		/// <summary>指定された数の単純な引数を持つ <see cref="Microsoft.Scripting.Actions.CallSignature"/> 構造体の新しいインスタンスを初期化します。</summary>
		/// <param name="argumentCount">作成される <see cref="CallSignature"/> が保持する単純な引数の数を指定します。</param>
		public CallSignature(int argumentCount)
		{
			ContractUtils.Requires(argumentCount >= 0, "argumentCount");
			_argumentCount = argumentCount;
			_infos = null;
		}

		/// <summary>指定された引数のリストを使用して、<see cref="Microsoft.Scripting.Actions.CallSignature"/> 構造体の新しいインスタンスを初期化します。</summary>
		/// <param name="infos">引数のリストを指定します。</param>
		public CallSignature(params Argument[] infos)
		{
			if (infos != null)
			{
				_argumentCount = infos.Length;
				_infos = Array.Exists(infos, x => x.Kind != ArgumentType.Simple) ? infos : null;
			}
			else
			{
				_argumentCount = 0;
				_infos = null;
			}
		}

		/// <summary>指定された引数の種類のリストを使用して、<see cref="Microsoft.Scripting.Actions.CallSignature"/> 構造体の新しいインスタンスを初期化します。</summary>
		/// <param name="kinds">引数の種類のリストを指定します。</param>
		public CallSignature(params ArgumentType[] kinds)
		{
			if (kinds != null)
			{
				_argumentCount = kinds.Length;
				_infos = Array.Exists(kinds, x => x != ArgumentType.Simple) ? Array.ConvertAll(kinds, x => new Argument(x)) : null;
			}
			else
			{
				_argumentCount = 0;
				_infos = null;
			}
		}

		/// <summary>この <see cref="CallSignature"/> が指定された <see cref="CallSignature"/> と等しいかどうかを判断します。</summary>
		/// <param name="other">比較する <see cref="CallSignature"/> を指定します。</param>
		/// <returns>この <see cref="CallSignature"/> が指定された <see cref="CallSignature"/> と等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		[StateIndependent]
		public bool Equals(CallSignature other) { return _infos == null ? other._infos == null && other._argumentCount == _argumentCount : other._infos != null && _infos.SequenceEqual(other._infos); }

		/// <summary>このオブジェクトが指定されたオブジェクトと等しいかどうかを判断します。</summary>
		/// <param name="obj">比較するオブジェクトを指定します。</param>
		/// <returns>このオブジェクトが指定されたオブジェクトと等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public override bool Equals(object obj) { return obj is CallSignature && Equals((CallSignature)obj); }

		/// <summary>2 つの <see cref="CallSignature"/> が等しいかどうかを判断します。</summary>
		/// <param name="left">比較する 1 番目の <see cref="CallSignature"/>。</param>
		/// <param name="right">比較する 2 番目の <see cref="CallSignature"/>。</param>
		/// <returns>2 つの <see cref="CallSignature"/> が等しい場合は <c>true</c>。其れ以外の場合は <c>false</c>。</returns>
		public static bool operator ==(CallSignature left, CallSignature right) { return left.Equals(right); }

		/// <summary>2 つの <see cref="CallSignature"/> が等しくないかどうかを判断します。</summary>
		/// <param name="left">比較する 1 番目の <see cref="CallSignature"/>。</param>
		/// <param name="right">比較する 2 番目の <see cref="CallSignature"/>。</param>
		/// <returns>2 つの <see cref="CallSignature"/> が等しくない場合は <c>true</c>。其れ以外の場合は <c>false</c>。</returns>
		public static bool operator !=(CallSignature left, CallSignature right) { return !left.Equals(right); }

		/// <summary>このオブジェクトの文字列表現を返します。</summary>
		/// <returns>このオブジェクトの文字列表現。</returns>
		public override string ToString() { return _infos == null ? "Simple" : "(" + string.Join(", ", _infos) + ")"; }

		/// <summary>このオブジェクトのハッシュ値を計算します。</summary>
		/// <returns>このオブジェクトのハッシュ値。</returns>
		public override int GetHashCode() { return _infos == null ? 6551 : _infos.Aggregate(6551, (x, y) => x ^ (x << 5) ^ y.GetHashCode()); }

		/// <summary>この <see cref="CallSignature"/> に格納されている引数を <see cref="Argument"/> オブジェクトの配列として返します。</summary>
		/// <returns><see cref="CallSignature"/> に格納されているすべての引数が格納された <see cref="Argument"/> オブジェクトの配列。</returns>
		public Argument[] GetArgumentInfos() { return _infos != null ? ArrayUtils.Copy(_infos) : Enumerable.Repeat(Argument.Simple, _argumentCount).ToArray(); }

		/// <summary>この <see cref="CallSignature"/> の先頭に指定された引数を加えた新しい <see cref="CallSignature"/> を返します。</summary>
		/// <param name="info">先頭に追加する引数を指定します。</param>
		/// <returns>この <see cref="CallSignature"/> の先頭に引数が追加された新しい <see cref="CallSignature"/>。</returns>
		public CallSignature InsertArgument(Argument info) { return InsertArgumentAt(0, info); }

		/// <summary>この <see cref="CallSignature"/> の指定された位置に指定された引数を加えた新しい <see cref="CallSignature"/> を返します。</summary>
		/// <param name="index">引数を追加する位置を示す 0 から始まるインデックスを指定します。</param>
		/// <param name="info">追加する引数を指定します。</param>
		/// <returns>この <see cref="CallSignature"/> の指定された位置に引数が追加された新しい <see cref="CallSignature"/>。</returns>
		public CallSignature InsertArgumentAt(int index, Argument info)
		{
			if (IsSimple)
			{
				if (info.IsSimple)
					return new CallSignature(_argumentCount + 1);
				return new CallSignature(ArrayUtils.InsertAt(GetArgumentInfos(), index, info));
			}
			return new CallSignature(ArrayUtils.InsertAt(_infos, index, info));
		}

		/// <summary>この <see cref="CallSignature"/> の先頭から引数を取り除いた新しい <see cref="CallSignature"/> を返します。</summary>
		/// <returns>この <see cref="CallSignature"/> の先頭から引数が削除された新しい <see cref="CallSignature"/>。</returns>
		public CallSignature RemoveFirstArgument() { return RemoveArgumentAt(0); }

		/// <summary>この <see cref="CallSignature"/> の指定された位置から引数を取り除いた新しい <see cref="CallSignature"/> を返します。</summary>
		/// <param name="index">引数を削除する位置を示す 0 から始まるインデックスを指定します。</param>
		/// <returns>この <see cref="CallSignature"/> の指定された位置から引数が削除された新しい <see cref="CallSignature"/>。</returns>
		public CallSignature RemoveArgumentAt(int index)
		{
			if (_argumentCount == 0)
				throw new InvalidOperationException();
			if (IsSimple)
				return new CallSignature(_argumentCount - 1);
			return new CallSignature(ArrayUtils.RemoveAt(_infos, index));
		}

		/// <summary>この <see cref="CallSignature"/> 内で指定された種類の引数が最初に見つかった位置を示す 0 から始まるインデックスを返します。</summary>
		/// <param name="kind"><see cref="CallSignature"/> 内を検索する引数の種類を指定します。</param>
		/// <returns><see cref="CallSignature"/> 内で指定された種類の引数が最初に見つかった位置を示す 0 から始まるインデックス。見つからなかった場合は -1 を返します。</returns>
		public int IndexOf(ArgumentType kind) { return _infos == null ? (kind == ArgumentType.Simple && _argumentCount > 0 ? 0 : -1) : Array.FindIndex(_infos, x => x.Kind == kind); }

		/// <summary>この <see cref="CallSignature"/> 内に辞書引数が含まれているかどうかを示す値を返します。</summary>
		/// <returns><see cref="CallSignature"/> 内に辞書引数が含まれている場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool HasDictionaryArgument() { return IndexOf(ArgumentType.Dictionary) > -1; }

		/// <summary>この <see cref="CallSignature"/> 内にインスタンス引数が含まれているかどうかを示す値を返します。</summary>
		/// <returns><see cref="CallSignature"/> 内にインスタンス引数が含まれている場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool HasInstanceArgument() { return IndexOf(ArgumentType.Instance) > -1; }

		/// <summary>この <see cref="CallSignature"/> 内に配列引数が含まれているかどうかを示す値を返します。</summary>
		/// <returns><see cref="CallSignature"/> 内に配列引数が含まれている場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool HasListArgument() { return IndexOf(ArgumentType.List) > -1; }

		/// <summary>この <see cref="CallSignature"/> 内に名前付き引数が含まれているかどうかを示す値を返します。</summary>
		/// <returns><see cref="CallSignature"/> 内に名前付き引数が含まれている場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		internal bool HasNamedArgument() { return IndexOf(ArgumentType.Named) > -1; }

		/// <summary>この <see cref="CallSignature"/> 内に辞書引数または名前付き引数が含まれているかどうかを示す値を返します。</summary>
		/// <returns><see cref="CallSignature"/> 内に辞書引数または名前付き引数が含まれている場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool HasKeywordArgument() { return _infos != null && Array.Exists(_infos, x => x.Kind == ArgumentType.Dictionary || x.Kind == ArgumentType.Named); }

		/// <summary>この <see cref="CallSignature"/> 内の指定された位置に存在する引数の種類を返します。</summary>
		/// <param name="index">種類を取得する引数の位置を示す 0 から始まるインデックスを指定します。</param>
		/// <returns>指定された位置に存在する引数の種類。</returns>
		public ArgumentType GetArgumentKind(int index) { return _infos != null ? _infos[index].Kind : ArgumentType.Simple; } // TODO: Contract.Requires(index >= 0 && index < _argumentCount, "index");

		/// <summary>この <see cref="CallSignature"/> 内の指定された位置に存在する引数の名前を返します。</summary>
		/// <param name="index">名前を取得する引数の位置を示す 0 から始まるインデックスを指定します。</param>
		/// <returns>指定された位置に存在する引数の名前。名前が存在しない場合は <c>null</c> を返します。</returns>
		public string GetArgumentName(int index)
		{
			ContractUtils.Requires(index >= 0 && index < _argumentCount);
			return _infos != null ? _infos[index].Name : null;
		}

		/// <summary>ユーザーがコールサイトで提供した位置決定済み引数の数を返します。</summary>
		/// <returns>位置決定済みの引数の個数。</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		public int GetProvidedPositionalArgumentCount() { return _argumentCount - (_infos != null ? _infos.Count(x => x.Kind == ArgumentType.Dictionary || x.Kind == ArgumentType.List || x.Kind == ArgumentType.Named) : 0); }

		/// <summary>この <see cref="CallSignature"/> に格納されているすべての引数の名前を返します。</summary>
		/// <returns>この <see cref="CallSignature"/> 内のすべての引数の名前を含んでいる配列。</returns>
		public string[] GetArgumentNames() { return _infos == null ? ArrayUtils.EmptyStrings : _infos.Where(x => x.Name != null).Select(x => x.Name).ToArray(); }

		/// <summary>このオブジェクトを表す <see cref="Expression"/> を作成します。</summary>
		/// <returns>このオブジェクトを表す <see cref="Expression"/>。</returns>
		public Expression CreateExpression()
		{
			if (_infos == null)
				return Expression.New(typeof(CallSignature).GetConstructor(new[] { typeof(int) }), AstUtils.Constant(ArgumentCount));
			else
				return Expression.New(
					typeof(CallSignature).GetConstructor(new[] { typeof(Argument[]) }),
					Expression.NewArrayInit(typeof(Argument), _infos.Select(x => x.CreateExpression()))
				);
		}
	}
}
