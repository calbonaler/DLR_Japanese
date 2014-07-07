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
using System.Text;
using System.Threading;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	/// <summary>DLR 言語によって既定の入出力として使用される I/O ストリームを提供します。</summary>
	public sealed class SharedIO
	{
		readonly object _mutex = new object(); // このオブジェクトが矛盾した状態に遷移するのを防ぎます。入出力は同期しません。

		sealed class StreamProxy : Stream
		{
			readonly ConsoleStreamType _type;
			readonly SharedIO _io;

			public StreamProxy(SharedIO io, ConsoleStreamType type)
			{
				Assert.NotNull(io);
				_io = io;
				_type = type;
			}

			public override bool CanRead { get { return _type == ConsoleStreamType.Input; } }

			public override bool CanSeek { get { return false; } }

			public override bool CanWrite { get { return !CanRead; } }

			public override void Flush() { _io.GetStream(_type).Flush(); }

			public override int Read(byte[] buffer, int offset, int count) { return _io.GetStream(_type).Read(buffer, offset, count); }

			public override void Write(byte[] buffer, int offset, int count) { _io.GetStream(_type).Write(buffer, offset, count); }

			public override long Length { get { throw new NotSupportedException(); } }

			public override long Position
			{
				get { throw new NotSupportedException(); }
				set { throw new NotSupportedException(); }
			}

			public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }

			public override void SetLength(long value) { throw new NotSupportedException(); }
		}

		// 既定でコンソールに遅延初期化されます。
		Stream _inputStream;
		Stream _outputStream;
		Stream _errorStream;

		TextReader _inputReader;
		TextWriter _outputWriter;
		TextWriter _errorWriter;

		Encoding _inputEncoding;

		/// <summary>入力ストリームを取得します。</summary>
		public Stream InputStream { get { InitializeInput(); return _inputStream; } }
		/// <summary>出力ストリームを取得します。</summary>
		public Stream OutputStream { get { InitializeOutput(); return _outputStream; } }
		/// <summary>エラー出力ストリームを取得します。</summary>
		public Stream ErrorStream { get { InitializeErrorOutput(); return _errorStream; } }

		/// <summary>入力から文字を読み取る <see cref="System.IO.TextReader"/> を取得します。</summary>
		public TextReader InputReader { get { InitializeInput(); return _inputReader; } }
		/// <summary>出力に文字を書き込む <see cref="System.IO.TextWriter"/> を取得します。</summary>
		public TextWriter OutputWriter { get { InitializeOutput(); return _outputWriter; } }
		/// <summary>エラー出力に文字を書き込む <see cref="System.IO.TextWriter"/> を取得します。</summary>
		public TextWriter ErrorWriter { get { InitializeErrorOutput(); return _errorWriter; } }

		/// <summary>入力のエンコーディングを取得します。</summary>
		public Encoding InputEncoding { get { InitializeInput(); return _inputEncoding; } }
		/// <summary>出力のエンコーディングを取得します。</summary>
		public Encoding OutputEncoding { get { InitializeOutput(); return _outputWriter.Encoding; } }
		/// <summary>エラー出力のエンコーディングを取得します。</summary>
		public Encoding ErrorEncoding { get { InitializeErrorOutput(); return _errorWriter.Encoding; } }

		/// <summary>この <see cref="Microsoft.Scripting.Runtime.SharedIO"/> クラスの新しいインスタンスを初期化します。</summary>
		internal SharedIO() { }

		void InitializeInput()
		{
			if (_inputStream == null)
			{
				lock (_mutex)
				{
					if (_inputStream == null)
					{
						_inputStream = ConsoleInputStream.Instance;
						_inputEncoding = Console.InputEncoding;
						_inputReader = Console.In;
					}
				}
			}
		}

		void InitializeOutput()
		{
			if (_outputStream == null)
			{
				lock (_mutex)
				{
					if (_outputStream == null)
					{
						_outputStream = Console.OpenStandardOutput();
						_outputWriter = Console.Out;
					}
				}
			}
		}

		void InitializeErrorOutput()
		{
			if (_errorStream == null)
			{
				var errorStream = Console.OpenStandardError();
				Interlocked.CompareExchange(ref _errorStream, errorStream, null);
				Interlocked.CompareExchange(ref _errorWriter, Console.Error, null);
			}
		}

		/// <summary>ストリームおよび <see cref="System.IO.TextWriter"/> を使用して、出力を設定します。</summary>
		/// <param name="stream">出力データが書き込まれるストリームを指定します。</param>
		/// <param name="writer">出力データの書き込みに使用する <see cref="System.IO.TextWriter"/> を指定します。</param>
		public void SetOutput(Stream stream, TextWriter writer)
		{
			Assert.NotNull(stream, writer);
			lock (_mutex)
			{
				_outputStream = stream;
				_outputWriter = writer;
			}
		}

		/// <summary>ストリームおよび <see cref="System.IO.TextWriter"/> を使用して、エラー出力を設定します。</summary>
		/// <param name="stream">エラー出力データが書き込まれるストリームを指定します。</param>
		/// <param name="writer">エラー出力データの書き込みに使用する <see cref="System.IO.TextWriter"/> を指定します。</param>
		public void SetErrorOutput(Stream stream, TextWriter writer)
		{
			Assert.NotNull(stream, writer);
			lock (_mutex)
			{
				_errorStream = stream;
				_errorWriter = writer;
			}
		}

		/// <summary>ストリーム、<see cref="System.IO.TextReader"/> およびエンコーディングを使用して、入力を設定します。</summary>
		/// <param name="stream">入力データが読み込まれるストリームを指定します。</param>
		/// <param name="reader">入力データの読み込みに使用する <see cref="System.IO.TextReader"/> を指定します。</param>
		/// <param name="encoding">スクリプトによって入力から読み込まれたデータを変換するのに使用されるエンコーディングを指定します。</param>
		public void SetInput(Stream stream, TextReader reader, Encoding encoding)
		{
			Assert.NotNull(stream, reader, encoding);
			lock (_mutex)
			{
				_inputStream = stream;
				_inputReader = reader;
				_inputEncoding = encoding;
			}
		}

		/// <summary>出力をコンソールにリダイレクトします。</summary>
		public void RedirectToConsole()
		{
			lock (_mutex)
			{
				_inputEncoding = null;
				_inputStream = null;
				_outputStream = null;
				_errorStream = null;
				_inputReader = null;
				_outputWriter = null;
				_errorWriter = null;
			}
		}

		/// <summary>指定されたコンソールストリームに対するストリームを取得します。</summary>
		/// <param name="type">取得するストリームが対応するコンソールストリームを指定します。</param>
		public Stream GetStream(ConsoleStreamType type)
		{
			switch (type)
			{
				case ConsoleStreamType.Input: return InputStream;
				case ConsoleStreamType.Output: return OutputStream;
				case ConsoleStreamType.ErrorOutput: return ErrorStream;
			}
			throw Error.InvalidStreamType(type);
		}

		/// <summary>指定されたコンソールストリームに対する <see cref="TextWriter"/> を取得します。</summary>
		/// <param name="type">取得する <see cref="TextWriter"/> が対応するコンソールストリームを指定します。</param>
		public TextWriter GetWriter(ConsoleStreamType type)
		{
			switch (type)
			{
				case ConsoleStreamType.Output: return OutputWriter;
				case ConsoleStreamType.ErrorOutput: return ErrorWriter;
			}
			throw Error.InvalidStreamType(type);
		}

		/// <summary>指定されたコンソールストリームのエンコーディングを取得します。</summary>
		/// <param name="type">取得するエンコーディングの基になるストリームが対応するコンソールストリームを指定します。</param>
		public Encoding GetEncoding(ConsoleStreamType type)
		{
			switch (type)
			{
				case ConsoleStreamType.Input: return InputEncoding;
				case ConsoleStreamType.Output: return OutputEncoding;
				case ConsoleStreamType.ErrorOutput: return ErrorEncoding;
			}
			throw Error.InvalidStreamType(type);
		}

		/// <summary>入力ストリームに対応する <see cref="TextReader"/> およびエンコーディングを取得します。</summary>
		/// <param name="encoding">取得したエンコーディングを格納する変数を指定します。</param>
		public TextReader GetReader(out Encoding encoding)
		{
			TextReader reader;
			lock (_mutex)
			{
				reader = InputReader;
				encoding = InputEncoding;
			}
			return reader;
		}

		/// <summary>指定されたコンソールストリームに対するストリームプロキシを取得します。</summary>
		/// <param name="type">取得するストリームプロキシが対応するコンソールストリームを指定します。</param>
		public Stream GetStreamProxy(ConsoleStreamType type) { return new StreamProxy(this, type); }
	}
}
