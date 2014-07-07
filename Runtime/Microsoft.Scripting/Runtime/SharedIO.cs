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
	/// <summary>DLR ����ɂ���Ċ���̓��o�͂Ƃ��Ďg�p����� I/O �X�g���[����񋟂��܂��B</summary>
	public sealed class SharedIO
	{
		readonly object _mutex = new object(); // ���̃I�u�W�F�N�g������������ԂɑJ�ڂ���̂�h���܂��B���o�͓͂������܂���B

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

		// ����ŃR���\�[���ɒx������������܂��B
		Stream _inputStream;
		Stream _outputStream;
		Stream _errorStream;

		TextReader _inputReader;
		TextWriter _outputWriter;
		TextWriter _errorWriter;

		Encoding _inputEncoding;

		/// <summary>���̓X�g���[�����擾���܂��B</summary>
		public Stream InputStream { get { InitializeInput(); return _inputStream; } }
		/// <summary>�o�̓X�g���[�����擾���܂��B</summary>
		public Stream OutputStream { get { InitializeOutput(); return _outputStream; } }
		/// <summary>�G���[�o�̓X�g���[�����擾���܂��B</summary>
		public Stream ErrorStream { get { InitializeErrorOutput(); return _errorStream; } }

		/// <summary>���͂��當����ǂݎ�� <see cref="System.IO.TextReader"/> ���擾���܂��B</summary>
		public TextReader InputReader { get { InitializeInput(); return _inputReader; } }
		/// <summary>�o�͂ɕ������������� <see cref="System.IO.TextWriter"/> ���擾���܂��B</summary>
		public TextWriter OutputWriter { get { InitializeOutput(); return _outputWriter; } }
		/// <summary>�G���[�o�͂ɕ������������� <see cref="System.IO.TextWriter"/> ���擾���܂��B</summary>
		public TextWriter ErrorWriter { get { InitializeErrorOutput(); return _errorWriter; } }

		/// <summary>���͂̃G���R�[�f�B���O���擾���܂��B</summary>
		public Encoding InputEncoding { get { InitializeInput(); return _inputEncoding; } }
		/// <summary>�o�͂̃G���R�[�f�B���O���擾���܂��B</summary>
		public Encoding OutputEncoding { get { InitializeOutput(); return _outputWriter.Encoding; } }
		/// <summary>�G���[�o�͂̃G���R�[�f�B���O���擾���܂��B</summary>
		public Encoding ErrorEncoding { get { InitializeErrorOutput(); return _errorWriter.Encoding; } }

		/// <summary>���� <see cref="Microsoft.Scripting.Runtime.SharedIO"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
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

		/// <summary>�X�g���[������� <see cref="System.IO.TextWriter"/> ���g�p���āA�o�͂�ݒ肵�܂��B</summary>
		/// <param name="stream">�o�̓f�[�^���������܂��X�g���[�����w�肵�܂��B</param>
		/// <param name="writer">�o�̓f�[�^�̏������݂Ɏg�p���� <see cref="System.IO.TextWriter"/> ���w�肵�܂��B</param>
		public void SetOutput(Stream stream, TextWriter writer)
		{
			Assert.NotNull(stream, writer);
			lock (_mutex)
			{
				_outputStream = stream;
				_outputWriter = writer;
			}
		}

		/// <summary>�X�g���[������� <see cref="System.IO.TextWriter"/> ���g�p���āA�G���[�o�͂�ݒ肵�܂��B</summary>
		/// <param name="stream">�G���[�o�̓f�[�^���������܂��X�g���[�����w�肵�܂��B</param>
		/// <param name="writer">�G���[�o�̓f�[�^�̏������݂Ɏg�p���� <see cref="System.IO.TextWriter"/> ���w�肵�܂��B</param>
		public void SetErrorOutput(Stream stream, TextWriter writer)
		{
			Assert.NotNull(stream, writer);
			lock (_mutex)
			{
				_errorStream = stream;
				_errorWriter = writer;
			}
		}

		/// <summary>�X�g���[���A<see cref="System.IO.TextReader"/> ����уG���R�[�f�B���O���g�p���āA���͂�ݒ肵�܂��B</summary>
		/// <param name="stream">���̓f�[�^���ǂݍ��܂��X�g���[�����w�肵�܂��B</param>
		/// <param name="reader">���̓f�[�^�̓ǂݍ��݂Ɏg�p���� <see cref="System.IO.TextReader"/> ���w�肵�܂��B</param>
		/// <param name="encoding">�X�N���v�g�ɂ���ē��͂���ǂݍ��܂ꂽ�f�[�^��ϊ�����̂Ɏg�p�����G���R�[�f�B���O���w�肵�܂��B</param>
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

		/// <summary>�o�͂��R���\�[���Ƀ��_�C���N�g���܂��B</summary>
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

		/// <summary>�w�肳�ꂽ�R���\�[���X�g���[���ɑ΂���X�g���[�����擾���܂��B</summary>
		/// <param name="type">�擾����X�g���[�����Ή�����R���\�[���X�g���[�����w�肵�܂��B</param>
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

		/// <summary>�w�肳�ꂽ�R���\�[���X�g���[���ɑ΂��� <see cref="TextWriter"/> ���擾���܂��B</summary>
		/// <param name="type">�擾���� <see cref="TextWriter"/> ���Ή�����R���\�[���X�g���[�����w�肵�܂��B</param>
		public TextWriter GetWriter(ConsoleStreamType type)
		{
			switch (type)
			{
				case ConsoleStreamType.Output: return OutputWriter;
				case ConsoleStreamType.ErrorOutput: return ErrorWriter;
			}
			throw Error.InvalidStreamType(type);
		}

		/// <summary>�w�肳�ꂽ�R���\�[���X�g���[���̃G���R�[�f�B���O���擾���܂��B</summary>
		/// <param name="type">�擾����G���R�[�f�B���O�̊�ɂȂ�X�g���[�����Ή�����R���\�[���X�g���[�����w�肵�܂��B</param>
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

		/// <summary>���̓X�g���[���ɑΉ����� <see cref="TextReader"/> ����уG���R�[�f�B���O���擾���܂��B</summary>
		/// <param name="encoding">�擾�����G���R�[�f�B���O���i�[����ϐ����w�肵�܂��B</param>
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

		/// <summary>�w�肳�ꂽ�R���\�[���X�g���[���ɑ΂���X�g���[���v���L�V���擾���܂��B</summary>
		/// <param name="type">�擾����X�g���[���v���L�V���Ή�����R���\�[���X�g���[�����w�肵�܂��B</param>
		public Stream GetStreamProxy(ConsoleStreamType type) { return new StreamProxy(this, type); }
	}
}
