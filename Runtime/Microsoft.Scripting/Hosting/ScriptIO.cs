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
using System.IO;
using System.Security.Permissions;
using System.Text;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting
{
	/// <summary>DLR 言語によって既定の入出力として使用されるホストにリダイレクト可能な I/O ストリームを提供します。</summary>
	public sealed class ScriptIO : MarshalByRefObject
	{
		/// <summary>入力ストリームを取得します。</summary>
		public Stream InputStream { get { return SharedIO.InputStream; } }
		/// <summary>出力ストリームを取得します。</summary>
		public Stream OutputStream { get { return SharedIO.OutputStream; } }
		/// <summary>エラー出力ストリームを取得します。</summary>
		public Stream ErrorStream { get { return SharedIO.ErrorStream; } }

		/// <summary>入力から文字を読み取る <see cref="System.IO.TextReader"/> を取得します。</summary>
		public TextReader InputReader { get { return SharedIO.InputReader; } }
		/// <summary>出力に文字を書き込む <see cref="System.IO.TextWriter"/> を取得します。</summary>
		public TextWriter OutputWriter { get { return SharedIO.OutputWriter; } }
		/// <summary>エラー出力に文字を書き込む <see cref="System.IO.TextWriter"/> を取得します。</summary>
		public TextWriter ErrorWriter { get { return SharedIO.ErrorWriter; } }

		/// <summary>入力のエンコーディングを取得します。</summary>
		public Encoding InputEncoding { get { return SharedIO.InputEncoding; } }
		/// <summary>出力のエンコーディングを取得します。</summary>
		public Encoding OutputEncoding { get { return SharedIO.OutputEncoding; } }
		/// <summary>エラー出力のエンコーディングを取得します。</summary>
		public Encoding ErrorEncoding { get { return SharedIO.ErrorEncoding; } }

		/// <summary>基になる <see cref="Microsoft.Scripting.Runtime.SharedIO"/> オブジェクトを取得します。</summary>
		internal SharedIO SharedIO { get; private set; }

		/// <summary>
		/// 基になる <see cref="Microsoft.Scripting.Runtime.SharedIO"/> を使用して、<see cref="Microsoft.Scripting.Hosting.ScriptIO"/>
		/// クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="io">基になる <see cref="Microsoft.Scripting.Runtime.SharedIO"/> を指定します。</param>
		internal ScriptIO(SharedIO io)
		{
			Assert.NotNull(io);
			SharedIO = io;
		}

		/// <summary>ストリームおよびエンコーディングを使用して、出力を設定します。</summary>
		/// <param name="stream">出力データが書き込まれるストリームを指定します。</param>
		/// <param name="encoding">スクリプトによって出力に書き込まれたデータを変換するのに使用されるエンコーディングを指定します。</param>
		public void SetOutput(Stream stream, Encoding encoding)
		{
			ContractUtils.RequiresNotNull(stream, "stream");
			ContractUtils.RequiresNotNull(encoding, "encoding");
			SharedIO.SetOutput(stream, new StreamWriter(stream, encoding));
		}

		/// <summary>ストリームおよび <see cref="System.IO.TextWriter"/> を使用して、出力を設定します。</summary>
		/// <param name="stream">出力データが書き込まれるストリームを指定します。</param>
		/// <param name="writer">出力データの書き込みに使用する <see cref="System.IO.TextWriter"/> を指定します。</param>
		public void SetOutput(Stream stream, TextWriter writer)
		{
			ContractUtils.RequiresNotNull(stream, "stream");
			ContractUtils.RequiresNotNull(writer, "writer");
			SharedIO.SetOutput(stream, writer);
		}

		/// <summary>ストリームおよびエンコーディングを使用して、エラー出力を設定します。</summary>
		/// <param name="stream">エラー出力データが書き込まれるストリームを指定します。</param>
		/// <param name="encoding">スクリプトによってエラー出力に書き込まれたデータを変換するのに使用されるエンコーディングを指定します。</param>
		public void SetErrorOutput(Stream stream, Encoding encoding)
		{
			ContractUtils.RequiresNotNull(stream, "stream");
			ContractUtils.RequiresNotNull(encoding, "encoding");
			SharedIO.SetErrorOutput(stream, new StreamWriter(stream, encoding));
		}

		/// <summary>ストリームおよび <see cref="System.IO.TextWriter"/> を使用して、エラー出力を設定します。</summary>
		/// <param name="stream">エラー出力データが書き込まれるストリームを指定します。</param>
		/// <param name="writer">エラー出力データの書き込みに使用する <see cref="System.IO.TextWriter"/> を指定します。</param>
		public void SetErrorOutput(Stream stream, TextWriter writer)
		{
			ContractUtils.RequiresNotNull(stream, "stream");
			ContractUtils.RequiresNotNull(writer, "writer");
			SharedIO.SetErrorOutput(stream, writer);
		}

		/// <summary>ストリームおよびエンコーディングを使用して、入力を設定します。</summary>
		/// <param name="stream">入力データが読み込まれるストリームを指定します。</param>
		/// <param name="encoding">スクリプトによって入力から読み込まれたデータを変換するのに使用されるエンコーディングを指定します。</param>
		public void SetInput(Stream stream, Encoding encoding)
		{
			ContractUtils.RequiresNotNull(stream, "stream");
			ContractUtils.RequiresNotNull(encoding, "encoding");
			SharedIO.SetInput(stream, new StreamReader(stream, encoding), encoding);
		}

		/// <summary>ストリーム、<see cref="System.IO.TextReader"/> およびエンコーディングを使用して、入力を設定します。</summary>
		/// <param name="stream">入力データが読み込まれるストリームを指定します。</param>
		/// <param name="reader">入力データの読み込みに使用する <see cref="System.IO.TextReader"/> を指定します。</param>
		/// <param name="encoding">スクリプトによって入力から読み込まれたデータを変換するのに使用されるエンコーディングを指定します。</param>
		public void SetInput(Stream stream, TextReader reader, Encoding encoding)
		{
			ContractUtils.RequiresNotNull(stream, "stream");
			ContractUtils.RequiresNotNull(reader, "writer");
			ContractUtils.RequiresNotNull(encoding, "encoding");
			SharedIO.SetInput(stream, reader, encoding);
		}

		/// <summary>出力をコンソールにリダイレクトします。</summary>
		public void RedirectToConsole() { SharedIO.RedirectToConsole(); }

		// TODO: Figure out what is the right lifetime
		/// <summary>対象のインスタンスの有効期間ポリシーを制御する、有効期間サービス オブジェクトを取得します。</summary>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
		public override object InitializeLifetimeService() { return null; }
	}
}
