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
using System.Runtime.Serialization;
using System.Security.Permissions;
using Microsoft.Contracts;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting
{
	/// <summary>大文字と小文字を区別する場合としない場合の両方をサポートする文字列の内部表現を提供します。</summary>
	/// <remarks>
	/// 規定ではすべての検索は大文字と小文字を区別します。
	/// 大文字と小文字を区別しない検索は最初に通常の <see cref="SymbolId"/> を作成したのち、<see cref="CaseInsensitiveIdentifier"/> プロパティにアクセスすることで実行できます。
	/// </remarks>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1036:OverrideMethodsOnComparableTypes"), Serializable]
	public struct SymbolId : ISerializable, IComparable, IComparable<SymbolId>, IEquatable<SymbolId>
	{
		/// <summary>指定された ID を使用して <see cref="Microsoft.Scripting.SymbolId"/> 構造体の新しいインスタンスを初期化します。</summary>
		/// <param name="value">この <see cref="SymbolId"/> の識別子を指定します。</param>
		public SymbolId(int value) : this() { Id = value; }

		SymbolId(SerializationInfo info, StreamingContext context) : this()
		{
			ContractUtils.RequiresNotNull(info, "info");
			Id = SymbolTable.StringToId(info.GetString("symbolName")).Id;
		}

		/// <summary><c>null</c> 文字列に対する ID を表します。</summary>
		public const int EmptyId = 0;
		/// <summary>無効な ID を表します。</summary>
		public const int InvalidId = -1;

		/// <summary><c>null</c> 文字列に対する <see cref="SymbolId"/> を表します。</summary>
		public static readonly SymbolId Empty = new SymbolId(EmptyId);

		/// <summary>無効な値に対する <see cref="SymbolId"/> を表します。</summary>
		public static readonly SymbolId Invalid = new SymbolId(InvalidId);

		/// <summary>この <see cref="SymbolId"/> が null 文字列を表しているかどうかを示す値を取得します。</summary>
		public bool IsEmpty { get { return Id == EmptyId; } }

		/// <summary>この <see cref="SymbolId"/> が無効であるかどうかを示す値を取得します。</summary>
		public bool IsInvalid { get { return Id == InvalidId; } }

		/// <summary>この <see cref="SymbolId"/> が表現する文字列の ID を取得します。</summary>
		public int Id { get; private set; }

		/// <summary>この <see cref="SymbolId"/> に対する大文字と小文字を区別しない <see cref="SymbolId"/> を取得します。</summary>
		public SymbolId CaseInsensitiveIdentifier { get { return new SymbolId(Id & ~SymbolTable.CaseVersionMask); } }

		/// <summary>この <see cref="SymbolId"/> に対する大文字と小文字を区別しない ID を取得します。</summary>
		public int CaseInsensitiveId { get { return Id & ~SymbolTable.CaseVersionMask; } }

		/// <summary>この <see cref="SymbolId"/> が大文字と小文字を区別しないかどうかを示す値を取得します。</summary>
		public bool IsCaseInsensitive { get { return (Id & SymbolTable.CaseVersionMask) == 0; } }

		/// <summary>この <see cref="SymbolId"/> が指定された <see cref="SymbolId"/> と同じ文字列を表しているかどうかを示す値を取得します。</summary>
		/// <param name="other">比較する <see cref="SymbolId"/> を指定します。</param>
		/// <returns>2 つの <see cref="SymbolId"/> が等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		[StateIndependent]
		public bool Equals(SymbolId other)
		{
			if (Id == other.Id)
				return true;
			else if (IsCaseInsensitive || other.IsCaseInsensitive)
				return (Id & ~SymbolTable.CaseVersionMask) == (other.Id & ~SymbolTable.CaseVersionMask);
			return false;
		}

		/// <summary>この <see cref="SymbolId"/> と指定された <see cref="SymbolId"/> が表す文字列を比較します。</summary>
		/// <param name="other">比較する <see cref="SymbolId"/> を指定します。</param>
		/// <returns>2 つの <see cref="SymbolId"/> のソートにおける前後関係を表す数値を指定します。</returns>
		public int CompareTo(SymbolId other)
		{
			// Note that we could just compare _id which will result in a faster comparison. However, that will
			// mean that sorting will depend on the order in which the symbols were interned. This will often
			// not be expected. Hence, we just compare the symbol strings
			return string.Compare(SymbolTable.IdToString(this), SymbolTable.IdToString(other), IsCaseInsensitive || other.IsCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
		}

		/// <summary>このオブジェクトと指定されたオブジェクトを比較します。</summary>
		/// <param name="obj">比較するオブジェクトを指定します。</param>
		/// <returns>このオブジェクトの指定されたオブジェクトに対する前後関係を表す数値を指定します。</returns>
		public int CompareTo(object obj)
		{
			if (!(obj is SymbolId))
				return -1;
			return CompareTo((SymbolId)obj);
		}

		// Security, SerializationInfo, StreamingContext
		// When leaving a context we serialize out our ID as a name rather than a raw ID.
		// When we enter a new context we consult it's FieldTable to get the ID of the symbol name in the new context.

		/// <summary><see cref="System.Runtime.Serialization.SerializationInfo"/> に、オブジェクトをシリアル化するために必要なデータを設定します。</summary>
		/// <param name="info">データを読み込む先の <see cref="System.Runtime.Serialization.SerializationInfo"/>。</param>
		/// <param name="context">このシリアル化のシリアル化先 (<see cref="System.Runtime.Serialization.StreamingContext"/> を参照)。</param>
		/// <exception cref="System.Security.SecurityException">呼び出し元に、必要なアクセス許可がありません。</exception>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			ContractUtils.RequiresNotNull(info, "info");
			info.AddValue("symbolName", SymbolTable.IdToString(this));
		}

		/// <summary>このオブジェクトと指定されたオブジェクトが等しいかどうかを返します。</summary>
		/// <param name="obj">等価性を比較するオブジェクトを指定します。</param>
		/// <returns>2 つのオブジェクトが等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		[Confined]
		public override bool Equals(object obj) { return obj is SymbolId && Equals((SymbolId)obj); }

		/// <summary>このオブジェクトのハッシュ値を計算します。</summary>
		/// <returns>このオブジェクトのハッシュ値。</returns>
		[Confined]
		public override int GetHashCode() { return Id & ~SymbolTable.CaseVersionMask; }

		/// <summary>このオブジェクトの文字列表現を取得します。このメソッドをシンボルが表す文字列を取得するために使用しないでください。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		[Confined]
		public override string ToString() { return SymbolTable.IdToString(this); }

		/// <summary>指定された文字列に対する <see cref="SymbolId"/> を取得します。</summary>
		/// <param name="s"><see cref="SymbolId"/> に対する文字列を指定します。</param>
		/// <returns>文字列に対する <see cref="SymbolId"/>。</returns>
		public static explicit operator SymbolId(string s) { return SymbolTable.StringToId(s); }

		/// <summary>指定された <see cref="SymbolId"/> が等しいかどうかを比較します。</summary>
		/// <param name="a">比較する 1 つ目の <see cref="SymbolId"/> を指定します。</param>
		/// <param name="b">比較する 2 つ目の <see cref="SymbolId"/> を指定します。</param>
		/// <returns>2 つの <see cref="SymbolId"/> が等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool operator ==(SymbolId a, SymbolId b) { return a.Equals(b); }

		/// <summary>指定された <see cref="SymbolId"/> が等しくないかどうかを比較します。</summary>
		/// <param name="a">比較する 1 つ目の <see cref="SymbolId"/> を指定します。</param>
		/// <param name="b">比較する 2 つ目の <see cref="SymbolId"/> を指定します。</param>
		/// <returns>2 つの <see cref="SymbolId"/> が等しくない場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool operator !=(SymbolId a, SymbolId b) { return !a.Equals(b); }

		/// <summary>指定された 1 つ目の <see cref="SymbolId"/> が 2 つ目の <see cref="SymbolId"/> 以下であるかどうかを比較します。</summary>
		/// <param name="a">比較する 1 つ目の <see cref="SymbolId"/> を指定します。</param>
		/// <param name="b">比較する 2 つ目の <see cref="SymbolId"/> を指定します。</param>
		/// <returns>1 つ目の <see cref="SymbolId"/> が 2 つ目の <see cref="SymbolId"/> 以下の場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool operator <=(SymbolId a, SymbolId b) { return a.CompareTo(b) <= 0; }

		/// <summary>指定された 1 つ目の <see cref="SymbolId"/> が 2 つ目の <see cref="SymbolId"/> よりも小さいかどうかを比較します。</summary>
		/// <param name="a">比較する 1 つ目の <see cref="SymbolId"/> を指定します。</param>
		/// <param name="b">比較する 2 つ目の <see cref="SymbolId"/> を指定します。</param>
		/// <returns>1 つ目の <see cref="SymbolId"/> が 2 つ目の <see cref="SymbolId"/> よりも小さい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool operator <(SymbolId a, SymbolId b) { return a.CompareTo(b) < 0; }

		/// <summary>指定された 1 つ目の <see cref="SymbolId"/> が 2 つ目の <see cref="SymbolId"/> 以上であるかどうかを比較します。</summary>
		/// <param name="a">比較する 1 つ目の <see cref="SymbolId"/> を指定します。</param>
		/// <param name="b">比較する 2 つ目の <see cref="SymbolId"/> を指定します。</param>
		/// <returns>1 つ目の <see cref="SymbolId"/> が 2 つ目の <see cref="SymbolId"/> 以上の場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool operator >=(SymbolId a, SymbolId b) { return a.CompareTo(b) >= 0; }

		/// <summary>指定された 1 つ目の <see cref="SymbolId"/> が 2 つ目の <see cref="SymbolId"/> よりも大きいかどうかを比較します。</summary>
		/// <param name="a">比較する 1 つ目の <see cref="SymbolId"/> を指定します。</param>
		/// <param name="b">比較する 2 つ目の <see cref="SymbolId"/> を指定します。</param>
		/// <returns>1 つ目の <see cref="SymbolId"/> が 2 つ目の <see cref="SymbolId"/> よりも大きい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool operator >(SymbolId a, SymbolId b) { return a.CompareTo(b) > 0; }
	}
}
