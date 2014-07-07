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
	/// <summary>構文解析器の状態が通知されるオブジェクトです。</summary>
	public class ParserSink
	{
		/// <summary>状態が通知されても何もしない <see cref="ParserSink"/> オブジェクトです。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly ParserSink Null = new ParserSink();

		/// <summary>二つ組としての一致をこのオブジェクトに通知します。</summary>
		/// <param name="opening">開始トークンのソースコード上での範囲を指定します。</param>
		/// <param name="closing">終了トークンのソースコード上での範囲を指定します。</param>
		/// <param name="priority">優先度を指定します。</param>
		public virtual void MatchPair(SourceSpan opening, SourceSpan closing, int priority) { }

		/// <summary>三つ組としての一致をこのオブジェクトに通知します。</summary>
		/// <param name="opening">開始トークンのソースコード上での範囲を指定します。</param>
		/// <param name="middle">中央トークンのソースコード上での範囲を指定します。</param>
		/// <param name="closing">終了トークンのソースコード上での範囲を指定します。</param>
		/// <param name="priority">優先度を指定します。</param>
		public virtual void MatchTriple(SourceSpan opening, SourceSpan middle, SourceSpan closing, int priority) { }

		/// <summary>引数リストの終了をこのオブジェクトに通知します。</summary>
		/// <param name="span">引数リストの終了トークンのソースコード上での範囲を指定します。</param>
		public virtual void EndParameters(SourceSpan span) { }

		/// <summary>次の引数の解析をこのオブジェクトに通知します。</summary>
		/// <param name="span">引数トークンのソースコード上での範囲を指定します。</param>
		public virtual void NextParameter(SourceSpan span) { }

		/// <summary>名前の修飾をこのオブジェクトに通知します。</summary>
		/// <param name="selector">セレクタのソースコード上での範囲を指定します。</param>
		/// <param name="span">修飾される名前のソースコード上での範囲を指定します。</param>
		/// <param name="name">名前を指定します。</param>
		public virtual void QualifyName(SourceSpan selector, SourceSpan span, string name) { }

		/// <summary>名前の開始をこのオブジェクトに通知します。</summary>
		/// <param name="span">名前のソースコード上での範囲を指定します。</param>
		/// <param name="name">名前を指定します。</param>
		public virtual void StartName(SourceSpan span, string name) { }

		/// <summary>引数リストの開始をこのオブジェクトに通知します。</summary>
		/// <param name="context">引数リストの開始トークンのソースコード上での範囲を指定します。</param>
		public virtual void StartParameters(SourceSpan context) { }
	}
}
