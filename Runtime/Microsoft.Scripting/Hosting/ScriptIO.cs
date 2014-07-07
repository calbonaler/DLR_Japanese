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
	/// <summary>DLR ����ɂ���Ċ���̓��o�͂Ƃ��Ďg�p�����z�X�g�Ƀ��_�C���N�g�\�� I/O �X�g���[����񋟂��܂��B</summary>
	public sealed class ScriptIO : MarshalByRefObject
	{
		/// <summary>���̓X�g���[�����擾���܂��B</summary>
		public Stream InputStream { get { return SharedIO.InputStream; } }
		/// <summary>�o�̓X�g���[�����擾���܂��B</summary>
		public Stream OutputStream { get { return SharedIO.OutputStream; } }
		/// <summary>�G���[�o�̓X�g���[�����擾���܂��B</summary>
		public Stream ErrorStream { get { return SharedIO.ErrorStream; } }

		/// <summary>���͂��當����ǂݎ�� <see cref="System.IO.TextReader"/> ���擾���܂��B</summary>
		public TextReader InputReader { get { return SharedIO.InputReader; } }
		/// <summary>�o�͂ɕ������������� <see cref="System.IO.TextWriter"/> ���擾���܂��B</summary>
		public TextWriter OutputWriter { get { return SharedIO.OutputWriter; } }
		/// <summary>�G���[�o�͂ɕ������������� <see cref="System.IO.TextWriter"/> ���擾���܂��B</summary>
		public TextWriter ErrorWriter { get { return SharedIO.ErrorWriter; } }

		/// <summary>���͂̃G���R�[�f�B���O���擾���܂��B</summary>
		public Encoding InputEncoding { get { return SharedIO.InputEncoding; } }
		/// <summary>�o�͂̃G���R�[�f�B���O���擾���܂��B</summary>
		public Encoding OutputEncoding { get { return SharedIO.OutputEncoding; } }
		/// <summary>�G���[�o�͂̃G���R�[�f�B���O���擾���܂��B</summary>
		public Encoding ErrorEncoding { get { return SharedIO.ErrorEncoding; } }

		/// <summary>��ɂȂ� <see cref="Microsoft.Scripting.Runtime.SharedIO"/> �I�u�W�F�N�g���擾���܂��B</summary>
		internal SharedIO SharedIO { get; private set; }

		/// <summary>
		/// ��ɂȂ� <see cref="Microsoft.Scripting.Runtime.SharedIO"/> ���g�p���āA<see cref="Microsoft.Scripting.Hosting.ScriptIO"/>
		/// �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="io">��ɂȂ� <see cref="Microsoft.Scripting.Runtime.SharedIO"/> ���w�肵�܂��B</param>
		internal ScriptIO(SharedIO io)
		{
			Assert.NotNull(io);
			SharedIO = io;
		}

		/// <summary>�X�g���[������уG���R�[�f�B���O���g�p���āA�o�͂�ݒ肵�܂��B</summary>
		/// <param name="stream">�o�̓f�[�^���������܂��X�g���[�����w�肵�܂��B</param>
		/// <param name="encoding">�X�N���v�g�ɂ���ďo�͂ɏ������܂ꂽ�f�[�^��ϊ�����̂Ɏg�p�����G���R�[�f�B���O���w�肵�܂��B</param>
		public void SetOutput(Stream stream, Encoding encoding)
		{
			ContractUtils.RequiresNotNull(stream, "stream");
			ContractUtils.RequiresNotNull(encoding, "encoding");
			SharedIO.SetOutput(stream, new StreamWriter(stream, encoding));
		}

		/// <summary>�X�g���[������� <see cref="System.IO.TextWriter"/> ���g�p���āA�o�͂�ݒ肵�܂��B</summary>
		/// <param name="stream">�o�̓f�[�^���������܂��X�g���[�����w�肵�܂��B</param>
		/// <param name="writer">�o�̓f�[�^�̏������݂Ɏg�p���� <see cref="System.IO.TextWriter"/> ���w�肵�܂��B</param>
		public void SetOutput(Stream stream, TextWriter writer)
		{
			ContractUtils.RequiresNotNull(stream, "stream");
			ContractUtils.RequiresNotNull(writer, "writer");
			SharedIO.SetOutput(stream, writer);
		}

		/// <summary>�X�g���[������уG���R�[�f�B���O���g�p���āA�G���[�o�͂�ݒ肵�܂��B</summary>
		/// <param name="stream">�G���[�o�̓f�[�^���������܂��X�g���[�����w�肵�܂��B</param>
		/// <param name="encoding">�X�N���v�g�ɂ���ăG���[�o�͂ɏ������܂ꂽ�f�[�^��ϊ�����̂Ɏg�p�����G���R�[�f�B���O���w�肵�܂��B</param>
		public void SetErrorOutput(Stream stream, Encoding encoding)
		{
			ContractUtils.RequiresNotNull(stream, "stream");
			ContractUtils.RequiresNotNull(encoding, "encoding");
			SharedIO.SetErrorOutput(stream, new StreamWriter(stream, encoding));
		}

		/// <summary>�X�g���[������� <see cref="System.IO.TextWriter"/> ���g�p���āA�G���[�o�͂�ݒ肵�܂��B</summary>
		/// <param name="stream">�G���[�o�̓f�[�^���������܂��X�g���[�����w�肵�܂��B</param>
		/// <param name="writer">�G���[�o�̓f�[�^�̏������݂Ɏg�p���� <see cref="System.IO.TextWriter"/> ���w�肵�܂��B</param>
		public void SetErrorOutput(Stream stream, TextWriter writer)
		{
			ContractUtils.RequiresNotNull(stream, "stream");
			ContractUtils.RequiresNotNull(writer, "writer");
			SharedIO.SetErrorOutput(stream, writer);
		}

		/// <summary>�X�g���[������уG���R�[�f�B���O���g�p���āA���͂�ݒ肵�܂��B</summary>
		/// <param name="stream">���̓f�[�^���ǂݍ��܂��X�g���[�����w�肵�܂��B</param>
		/// <param name="encoding">�X�N���v�g�ɂ���ē��͂���ǂݍ��܂ꂽ�f�[�^��ϊ�����̂Ɏg�p�����G���R�[�f�B���O���w�肵�܂��B</param>
		public void SetInput(Stream stream, Encoding encoding)
		{
			ContractUtils.RequiresNotNull(stream, "stream");
			ContractUtils.RequiresNotNull(encoding, "encoding");
			SharedIO.SetInput(stream, new StreamReader(stream, encoding), encoding);
		}

		/// <summary>�X�g���[���A<see cref="System.IO.TextReader"/> ����уG���R�[�f�B���O���g�p���āA���͂�ݒ肵�܂��B</summary>
		/// <param name="stream">���̓f�[�^���ǂݍ��܂��X�g���[�����w�肵�܂��B</param>
		/// <param name="reader">���̓f�[�^�̓ǂݍ��݂Ɏg�p���� <see cref="System.IO.TextReader"/> ���w�肵�܂��B</param>
		/// <param name="encoding">�X�N���v�g�ɂ���ē��͂���ǂݍ��܂ꂽ�f�[�^��ϊ�����̂Ɏg�p�����G���R�[�f�B���O���w�肵�܂��B</param>
		public void SetInput(Stream stream, TextReader reader, Encoding encoding)
		{
			ContractUtils.RequiresNotNull(stream, "stream");
			ContractUtils.RequiresNotNull(reader, "writer");
			ContractUtils.RequiresNotNull(encoding, "encoding");
			SharedIO.SetInput(stream, reader, encoding);
		}

		/// <summary>�o�͂��R���\�[���Ƀ��_�C���N�g���܂��B</summary>
		public void RedirectToConsole() { SharedIO.RedirectToConsole(); }

		// TODO: Figure out what is the right lifetime
		/// <summary>�Ώۂ̃C���X�^���X�̗L�����ԃ|���V�[�𐧌䂷��A�L�����ԃT�[�r�X �I�u�W�F�N�g���擾���܂��B</summary>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
		public override object InitializeLifetimeService() { return null; }
	}
}
