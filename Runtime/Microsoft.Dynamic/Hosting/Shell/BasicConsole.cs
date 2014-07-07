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
using System.Threading;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Hosting.Shell
{
	/// <summary><see cref="IConsole"/> �����������{�I�ȃR���\�[����\���܂��B</summary>
	public class BasicConsole : IConsole, IDisposable
	{
		TextWriter _output;
		TextWriter _errorOutput;

		/// <summary>���̃R���\�[���̏o�͂��������� <see cref="TextWriter"/> ���擾�܂��͐ݒ肵�܂��B</summary>
		public TextWriter Output
		{
			get { return _output; }
			set
			{
				ContractUtils.RequiresNotNull(value, "value");
				_output = value;
			}
		}

		/// <summary>���̃R���\�[���̃G���[�o�͂��������� <see cref="TextWriter"/> ���擾�܂��͐ݒ肵�܂��B</summary>
		public TextWriter ErrorOutput
		{
			get { return _errorOutput; }
			set
			{
				ContractUtils.RequiresNotNull(value, "value");
				_errorOutput = value;
			}
		}

		/// <summary>���̃R���\�[���� Ctrl-C ����͂��ꂽ�ۂɏI���ł���悤�ɂ��� <see cref="AutoResetEvent"/> ���擾�܂��͐ݒ肵�܂��B</summary>
		protected AutoResetEvent CtrlCEvent { get; set; }

		/// <summary>���̃R���\�[�����쐬���ꂽ�X���b�h���擾�܂��͐ݒ肵�܂��B</summary>
		protected Thread CreatingThread { get; set; }

		/// <summary>��ɂȂ�R���\�[���ŃL�����Z���L�[�������ꂽ���ɌĂ΂��f���Q�[�g���擾�܂��͐ݒ肵�܂��B</summary>
		public ConsoleCancelEventHandler ConsoleCancelEventHandler { get; set; }

		ConsoleColor _promptColor;
		ConsoleColor _outColor;
		ConsoleColor _errorColor;
		ConsoleColor _warningColor;

		/// <summary>�e�X�^�C���ňقȂ�F���g�p���邩�ǂ����������l���g�p���āA<see cref="Microsoft.Scripting.Hosting.Shell.BasicConsole"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="colorful">���̃R���\�[���� <see cref="Style"/> �񋓑̂̊e�l�ɉ����ĈقȂ�F���g�p���邩�ǂ����������l���w�肵�܂��B</param>
		public BasicConsole(bool colorful)
		{
			_output = System.Console.Out;
			_errorOutput = System.Console.Error;
			SetupColors(colorful);
			CreatingThread = Thread.CurrentThread;
			// ����̃n���h�����쐬
			ConsoleCancelEventHandler = (sender, e) =>
			{
				if (e.SpecialKey == ConsoleSpecialKey.ControlC)
				{
					e.Cancel = true;
					CtrlCEvent.Set();
					CreatingThread.Abort(new KeyboardInterruptException(""));
				}
			};
			Console.CancelKeyPress += (sender, e) =>
			{
				// �o�^���ꂽ�n���h���Ƀf�B�X�p�b�`
				if (ConsoleCancelEventHandler != null)
					ConsoleCancelEventHandler(sender, e);
			};
			CtrlCEvent = new AutoResetEvent(false);
		}

		void SetupColors(bool colorful)
		{
			if (colorful)
			{
				_promptColor = PickColor(ConsoleColor.Gray, ConsoleColor.White);
				_outColor = PickColor(ConsoleColor.Cyan, ConsoleColor.White);
				_errorColor = PickColor(ConsoleColor.Red, ConsoleColor.White);
				_warningColor = PickColor(ConsoleColor.Yellow, ConsoleColor.White);
			}
			else
				_promptColor = _outColor = _errorColor = _warningColor = Console.ForegroundColor;
		}

		static ConsoleColor PickColor(ConsoleColor best, ConsoleColor other)
		{
			if (Console.BackgroundColor != best)
				return best;
			return other;
		}

		/// <summary>�w�肳�ꂽ <see cref="TextWriter"/> �Ɏw�肳�ꂽ��������w�肳�ꂽ�F�ŏ������݂܂��B</summary>
		/// <param name="output">�����񂪏������܂�� <see cref="TextWriter"/> ���w�肵�܂��B</param>
		/// <param name="str">�������܂�镶������w�肵�܂��B</param>
		/// <param name="c">�����񂪕\�������F���w�肵�܂��B</param>
		protected void WriteColor(TextWriter output, string str, ConsoleColor c)
		{
			var origColor = Console.ForegroundColor;
			Console.ForegroundColor = c;
			output.Write(str);
			output.Flush();
			Console.ForegroundColor = origColor;
		}

		/// <summary>�w�肳�ꂽ�C���f���g���ŃR���\�[������ 1 �s��ǂݎ��܂��B</summary>
		/// <param name="autoIndentSize">�s�̍��[�ɑ}�������C���f���g�̕����w�肵�܂��B</param>
		/// <returns>�ǂݎ��ꂽ������B������ɂ͎����C���f���g�̕����܂܂�܂��B</returns>
		public virtual string ReadLine(int autoIndentSize)
		{
			Write("".PadLeft(autoIndentSize), Style.Prompt);
			var res = Console.In.ReadLine();
			if (res == null)
			{
				// ����������܂� - Ctrl-C �C�x���g�� ReadLine ���琧�䂪�߂�����ɓ������܂��B
				// �ǂ��炪�����������ǂ�����m��ɂ͂������҂K�v������܂��B
				// ����� Ctrl-Z ��ʂ����V���b�g�_�E�������ɂ킸���Ȓx���𐶂������܂����A�{���ɋC�Â��قǂł͂���܂���B
				// Ctrl-C �̏ꍇ�C�x���g���V�O�i����ԂɂȂ�΁A�����ɖ߂�܂��B
				if (CtrlCEvent != null && CtrlCEvent.WaitOne(100, false))
					return ""; // ctrl-C
				else
					return null; // ctrl-Z
			}
			return "".PadLeft(autoIndentSize) + res;
		}

		/// <summary>�w�肳�ꂽ��������w�肳�ꂽ�X�^�C���œK�؂ȃX�g���[���ɏ������݂܂��B</summary>
		/// <param name="text">�������܂�镶������w�肵�܂��B</param>
		/// <param name="style">�������܂�镶����̃X�^�C�����w�肵�܂��B</param>
		public virtual void Write(string text, Style style)
		{
			switch (style)
			{
				case Style.Prompt: WriteColor(_output, text, _promptColor); break;
				case Style.Out: WriteColor(_output, text, _outColor); break;
				case Style.Error: WriteColor(_errorOutput, text, _errorColor); break;
				case Style.Warning: WriteColor(_errorOutput, text, _warningColor); break;
			}
		}

		/// <summary>�w�肳�ꂽ��������w�肳�ꂽ�X�^�C���œK�؂ȃX�g���[���ɏ������݁A�����čs�I�[�L�����o�͂��܂��B</summary>
		/// <param name="text">�������܂�镶������w�肵�܂��B</param>
		/// <param name="style">�������܂�镶����̃X�^�C�����w�肵�܂��B</param>
		public void WriteLine(string text, Style style) { Write(text + Environment.NewLine, style); }

		/// <summary>�W���o�͂ɍs�I�[�L�����o�͂��܂��B</summary>
		public void WriteLine() { Write(Environment.NewLine, Style.Out); }

		/// <summary>���̃N���X�Ŏg�p����Ă��邷�ׂẴ��\�[�X��j�����܂��B</summary>
		public void Dispose()
		{
			if (CtrlCEvent != null)
				CtrlCEvent.Close();
			GC.SuppressFinalize(this);
		}
	}
}

