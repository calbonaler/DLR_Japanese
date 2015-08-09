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
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting
{
	/// <summary>複数のファイル名があいまいである場合にスローされる例外を表します。</summary>
	[Serializable]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
	public class AmbiguousFileNameException : Exception
	{
		/// <summary>あいまいなファイル名に対する 1 番目のファイルパスを取得します。</summary>
		public string FirstPath { get; private set; }

		/// <summary>あいまいなファイル名に対する 2 番目のファイルパスを取得します。</summary>
		public string SecondPath { get; private set; }

		/// <summary>あいまいであるパスを使用して、<see cref="Microsoft.Scripting.AmbiguousFileNameException"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="firstPath">あいまいである 1 番目のパスを指定します。</param>
		/// <param name="secondPath">あいまいである 2 番目のパスを指定します。</param>
		public AmbiguousFileNameException(string firstPath, string secondPath) : this(firstPath, secondPath, null, null) { }

		/// <summary>あいまいであるパスと例外を説明するメッセージを使用して、<see cref="Microsoft.Scripting.AmbiguousFileNameException"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="firstPath">あいまいである 1 番目のパスを指定します。</param>
		/// <param name="secondPath">あいまいである 2 番目のパスを指定します。</param>
		/// <param name="message">例外を説明するメッセージを指定します。</param>
		public AmbiguousFileNameException(string firstPath, string secondPath, string message) : this(firstPath, secondPath, message, null) { }

		/// <summary>あいまいであるパス、例外を説明するメッセージ、この例外の原因となった例外を使用して、<see cref="Microsoft.Scripting.AmbiguousFileNameException"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="firstPath">あいまいである 1 番目のパスを指定します。</param>
		/// <param name="secondPath">あいまいである 2 番目のパスを指定します。</param>
		/// <param name="message">例外を説明するメッセージを指定します。</param>
		/// <param name="innerException">この例外の原因となった例外を指定します。</param>
		public AmbiguousFileNameException(string firstPath, string secondPath, string message, Exception innerException) : base(message ?? string.Format("ファイル名があいまいです。2 つ以上のファイルが同じ名前にマッチしました ('{0}', '{1}')", firstPath, secondPath), innerException)
		{
			ContractUtils.RequiresNotNull(firstPath, "firstPath");
			ContractUtils.RequiresNotNull(secondPath, "secondPath");
			FirstPath = firstPath;
			SecondPath = secondPath;
		}

		/// <summary>シリアル化したデータを使用して、<see cref="Microsoft.Scripting.AmbiguousFileNameException"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="info">スローされている例外に関するシリアル化済みオブジェクト データを保持している <see cref="SerializationInfo"/>。</param>
		/// <param name="context">転送元または転送先に関するコンテキスト情報を含んでいる <see cref="StreamingContext"/>。</param>
		/// <exception cref="ArgumentNullException"><paramref name="info"/> パラメーターが <c>null</c> です。</exception>
		/// <exception cref="SerializationException">クラス名が <c>null</c> であるか、または <see cref="P:Microsoft.Scripting.AmbiguousFileNameException.HResult"/> が 0 です。</exception>
		protected AmbiguousFileNameException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			FirstPath = info.GetString("firstPath");
			SecondPath = info.GetString("secondPath");
		}

		/// <summary>例外に関する情報を使用して <see cref="SerializationInfo"/> を設定します。</summary>
		/// <param name="info">スローされている例外に関するシリアル化済みオブジェクト データを保持している <see cref="SerializationInfo"/>。</param>
		/// <param name="context">転送元または転送先に関するコンテキスト情報を含んでいる <see cref="StreamingContext"/>。</param>
		/// <exception cref="ArgumentNullException"><paramref name="info"/> パラメーターが <c>null</c> 参照 (Visual Basic の場合は <c>Nothing</c>) です。</exception>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("firstPath", FirstPath);
			info.AddValue("secondPath", SecondPath);
			base.GetObjectData(info, context);
		}
	}
}
