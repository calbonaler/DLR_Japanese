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
using System.Globalization;

namespace Microsoft.Scripting
{
	/// <summary>ソースコード内の位置を表します。</summary>
	[Serializable]
	public struct SourceLocation : IEquatable<SourceLocation>, IComparable<SourceLocation>, IComparable
	{
		// TODO: remove index
		/// <summary><see cref="Microsoft.Scripting.SourceLocation"/> 構造体の新しいインスタンスを初期化します。</summary>
		/// <param name="index">ソースコード内での 0 から始まるインデックスを指定します。</param>
		/// <param name="line">ソースコード内での 1 から始まる行番号を指定します。</param>
		/// <param name="column">ソースコード内での 1 から始まる桁番号を指定します。</param>
		public SourceLocation(int index, int line, int column)　: this()
		{
			ValidateLocation(index, line, column);
			Index = index;
			Line = line;
			Column = column;
		}

		static void ValidateLocation(int index, int line, int column)
		{
			if (index < 0)
				throw ErrorOutOfRange("index", 0);
			if (line < 1)
				throw ErrorOutOfRange("line", 1);
			if (column < 1)
				throw ErrorOutOfRange("column", 1);
		}

		static Exception ErrorOutOfRange(string paramName, int minValue) { return new ArgumentOutOfRangeException(paramName, string.Format("{0} must be greater than or equal to {1}", paramName, minValue)); }

		SourceLocation(int index, int line, int column, bool noChecks)　: this()
		{
			if (!noChecks)
				ValidateLocation(index, line, column);
			Index = index;
			Line = line;
			Column = column;
		}

		/// <summary>どの場所も示していない有効な位置を表します。</summary>
		public static readonly SourceLocation None = new SourceLocation(0, 0xfeefee, 0, true);

		/// <summary>無効な位置を表します。</summary>
		public static readonly SourceLocation Invalid = new SourceLocation(0, 0, 0, true);

		/// <summary>有効な最小の位置を表します。</summary>
		public static readonly SourceLocation MinValue = new SourceLocation(0, 1, 1);

		/// <summary>ソースコード内での 0 から始まるインデックスを取得します。</summary>
		public int Index { get; private set; }

		/// <summary>ソースコード内での 1 から始まる行番号を取得します。</summary>
		public int Line { get; private set; }

		/// <summary>ソースコード内での 1 から始まる桁番号を取得します。</summary>
		public int Column { get; private set; }

		/// <summary>この位置が有効かどうかを示す値を取得します。</summary>
		public bool IsValid { get { return Line > 0 && Column > 0; } }

		/// <summary>指定のオブジェクトが現在のオブジェクトと等しいかどうかを判断します。</summary>
		/// <param name="obj">現在のオブジェクトと比較するオブジェクト。</param>
		/// <returns>指定したオブジェクトが現在のオブジェクトと等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public override bool Equals(object obj) { return obj is SourceLocation && Equals((SourceLocation)obj); }

		/// <summary>特定の型のハッシュ関数として機能します。</summary>
		/// <returns>現在のオブジェクトのハッシュ コード。</returns>
		public override int GetHashCode() { return (Line << 16) ^ Column; }

		/// <summary>現在のオブジェクトを表す文字列を返します。</summary>
		/// <returns>現在のオブジェクトを表す文字列。</returns>
		public override string ToString() { return "(" + Line + "," + Column + ")"; }

		/// <summary>現在のオブジェクトのデバッグ用の文字列を返します。</summary>
		/// <returns>現在のオブジェクトのデバッグ用の文字列。</returns>
		internal string ToDebugString() { return string.Format(CultureInfo.CurrentCulture, "({0},{1},{2})", Index, Line, Column); }

		/// <summary>この位置が指定された位置と等しいかどうかを判断します。</summary>
		/// <param name="other">比較する位置を指定します。</param>
		/// <returns>指定された位置が現在の位置と等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool Equals(SourceLocation other) { return CompareTo(other) == 0; }

		/// <summary>この位置を指定された位置と比較します。</summary>
		/// <param name="other">比較する位置を指定します。</param>
		/// <returns>この位置が指定された位置よりも後にある場合は 0 より大きい値。等しい場合は 0。先にある場合は 0 より小さい値。</returns>
		public int CompareTo(SourceLocation other) { return Index.CompareTo(other.Index); }

		/// <summary>このオブジェクトを指定されたオブジェクトと比較します。</summary>
		/// <param name="obj">比較するオブジェクトを指定します。</param>
		/// <returns>このオブジェクトが指定されたオブジェクトよりも後にある場合は 0 より大きい値。等しい場合は 0。先にある場合は 0 より小さい値。</returns>
		public int CompareTo(object obj)
		{
			if (ReferenceEquals(obj, null))
				return 1;
			if (obj is SourceLocation)
				return CompareTo((SourceLocation)obj);
			throw new ArgumentException();
		}

		/// <summary>指定された 2 つの位置を比較して等しいかどうかを判断します。</summary>
		/// <param name="left">比較する 1 つ目の位置。</param>
		/// <param name="right">比較する 2 つ目の位置。</param>
		/// <returns>2 つの位置が等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool operator ==(SourceLocation left, SourceLocation right) { return left.CompareTo(right) == 0; ; }

		/// <summary>指定された 2 つの位置を比較して等しくないかどうかを判断します。</summary>
		/// <param name="left">比較する 1 つ目の位置。</param>
		/// <param name="right">比較する 2 つ目の位置。</param>
		/// <returns>2 つの位置が等しくない場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool operator !=(SourceLocation left, SourceLocation right) { return left.CompareTo(right) != 0; }

		/// <summary>指定された 2 つの位置を比較して 1 つ目の位置が 2 つ目の位置よりも先にあるかどうかを判断します。</summary>
		/// <param name="left">比較する 1 つ目の位置。</param>
		/// <param name="right">比較する 2 つ目の位置。</param>
		/// <returns>1 つ目の位置が 2 つ目の位置よりも先にある場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool operator <(SourceLocation left, SourceLocation right) { return left.CompareTo(right) < 0; }

		/// <summary>指定された 2 つの位置を比較して 1 つ目の位置が 2 つ目の位置よりも後にあるかどうかを判断します。</summary>
		/// <param name="left">比較する 1 つ目の位置。</param>
		/// <param name="right">比較する 2 つ目の位置。</param>
		/// <returns>1 つ目の位置が 2 つ目の位置よりも後にある場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool operator >(SourceLocation left, SourceLocation right) { return left.CompareTo(right) > 0; }

		/// <summary>指定された 2 つの位置を比較して 1 つ目の位置が 2 つ目の位置と等しいか、または、先にあるかどうかを判断します。</summary>
		/// <param name="left">比較する 1 つ目の位置。</param>
		/// <param name="right">比較する 2 つ目の位置。</param>
		/// <returns>1 つ目の位置が 2 つ目の位置よりも等しいか先にある場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool operator <=(SourceLocation left, SourceLocation right) { return left.CompareTo(right) <= 0; }

		/// <summary>指定された 2 つの位置を比較して 1 つ目の位置が 2 つ目の位置と等しいか、または、後にあるかどうかを判断します。</summary>
		/// <param name="left">比較する 1 つ目の位置。</param>
		/// <param name="right">比較する 2 つ目の位置。</param>
		/// <returns>1 つ目の位置が 2 つ目の位置よりも等しいか後にある場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool operator >=(SourceLocation left, SourceLocation right) { return left.CompareTo(right) >= 0; }
	}
}
