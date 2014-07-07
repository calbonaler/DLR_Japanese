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
	/// <summary>ソースコード内の範囲を表します。</summary>
	[Serializable]
	public struct SourceSpan : IEquatable<SourceSpan>
	{
		/// <summary>開始位置と終了位置を使用して、<see cref="Microsoft.Scripting.SourceSpan"/> 構造体の新しいインスタンスを初期化します。</summary>
		/// <param name="start">この範囲の開始位置を指定します。</param>
		/// <param name="end">この範囲の終了位置を指定します。</param>
		public SourceSpan(SourceLocation start, SourceLocation end) : this()
		{
			ValidateLocations(start, end);
			Start = start;
			End = end;
		}

		static void ValidateLocations(SourceLocation start, SourceLocation end)
		{
			if (start.IsValid && end.IsValid)
			{
				if (start > end)
					throw new ArgumentException("Start and End must be well ordered");
			}
			else if (start.IsValid || end.IsValid)
				throw new ArgumentException("Start and End must both be valid or both invalid");
		}

		/// <summary>どの位置も示さない有効な範囲を表します。</summary>
		public static readonly SourceSpan None = new SourceSpan(SourceLocation.None, SourceLocation.None);

		/// <summary>無効な範囲を表します。</summary>
		public static readonly SourceSpan Invalid = new SourceSpan(SourceLocation.Invalid, SourceLocation.Invalid);

		/// <summary>この範囲の開始位置を取得します。</summary>
		public SourceLocation Start { get; private set; }

		/// <summary>この範囲の終了位置を取得します。範囲の後の最初の文字位置を表します。</summary>
		public SourceLocation End { get; private set; }

		/// <summary>この範囲の長さ (範囲に含まれている文字数) を取得します。</summary>
		public int Length { get { return End.Index - Start.Index; } }

		/// <summary>この範囲に含まれている位置が有効かどうかを示す値を取得します。</summary>
		public bool IsValid { get { return Start.IsValid && End.IsValid; } }

		/// <summary>指定のオブジェクトが現在のオブジェクトと等しいかどうかを判断します。</summary>
		/// <param name="obj">現在のオブジェクトと比較するオブジェクト。</param>
		/// <returns>指定したオブジェクトが現在のオブジェクトと等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public override bool Equals(object obj) { return obj is SourceSpan && Equals((SourceSpan)obj); }

		/// <summary>現在のオブジェクトを表す文字列を返します。</summary>
		/// <returns>現在のオブジェクトを表す文字列。</returns>
		public override string ToString() { return Start.ToString() + " - " + End.ToString(); }

		// それぞれの列に 7 bit (0 - 128), それぞれの行に 9 bit (0 - 512)、XOR は大きなファイルを扱う時に助けになる。
		/// <summary>特定の型のハッシュ関数として機能します。</summary>
		/// <returns>現在のオブジェクトのハッシュ コード。</returns>
		public override int GetHashCode() { return (Start.Column) ^ (End.Column << 7) ^ (Start.Line << 14) ^ (End.Line << 23); }

		/// <summary>現在のオブジェクトのデバッグ用の文字列を返します。</summary>
		/// <returns>現在のオブジェクトのデバッグ用の文字列。</returns>
		internal string ToDebugString() { return string.Format(CultureInfo.CurrentCulture, "{0}-{1}", Start.ToDebugString(), End.ToDebugString()); }

		/// <summary>この範囲が指定された範囲と等しいかどうかを判断します。</summary>
		/// <param name="other">比較する範囲を指定します。</param>
		/// <returns>指定された範囲が現在の範囲と等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool Equals(SourceSpan other) { return Start == other.Start && End == other.End; }

		/// <summary>指定された 2 つの範囲を比較して等しいかどうかを判断します。</summary>
		/// <param name="left">比較する 1 つ目の範囲。</param>
		/// <param name="right">比較する 2 つ目の範囲。</param>
		/// <returns>2 つの範囲が等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool operator ==(SourceSpan left, SourceSpan right) { return left.Equals(right); }

		/// <summary>指定された 2 つの範囲を比較して等しくないかどうかを判断します。</summary>
		/// <param name="left">比較する 1 つ目の範囲。</param>
		/// <param name="right">比較する 2 つ目の範囲。</param>
		/// <returns>2 つの範囲が等しくない場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool operator !=(SourceSpan left, SourceSpan right) { return !left.Equals(right); }
	}
}
