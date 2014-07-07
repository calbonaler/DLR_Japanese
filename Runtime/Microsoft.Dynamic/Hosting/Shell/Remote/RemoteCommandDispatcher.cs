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
using System.Diagnostics;
using System.Security.Permissions;
using System.Threading;

namespace Microsoft.Scripting.Hosting.Shell.Remote
{
	/// <summary>
	/// <see cref="RemoteConsoleHost"/> �������Ԏ��s����Ă��鑀��𒆎~�ł���悤�ɂ��܂��B
	/// <see cref="RemoteConsoleHost"/> ���̂͂ǂ̃X���b�h�v�[���̃X���b�h�������[�g�Ăяo�����������Ă���̂���m��Ȃ����߁A����ɂ̓����[�g�����^�C���T�[�o�[����̋��������K�v�Ƃ��܂��B
	/// </summary>
	public class RemoteCommandDispatcher : MarshalByRefObject, ICommandDispatcher
	{
		/// <summary>�����[�g �R���\�[�������݂̃R�}���h����̂��ׂĂ̏o�͂������������Ƃ��m�F�ł���悤�ȏo�͂̏I���������}�[�J�[��\���܂��B</summary>
		internal const string OutputCompleteMarker = "{7FF032BB-DB03-4255-89DE-641CA195E5FA}";
		Thread _executingThread;

		/// <summary>�w�肳�ꂽ�X�R�[�v���g�p���āA<see cref="Microsoft.Scripting.Hosting.Shell.Remote.RemoteCommandDispatcher"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="scope">���̃f�B�X�p�b�`���Ɋ֘A�t����X�R�[�v���w�肵�܂��B</param>
		public RemoteCommandDispatcher(ScriptScope scope) { ScriptScope = scope; }

		/// <summary>���̃f�B�X�p�b�`���Ɋ֘A�t����ꂽ�X�R�[�v���擾���܂��B</summary>
		public ScriptScope ScriptScope { get; private set; }

		/// <summary>�w�肳�ꂽ�����Ԏ��s�����\���̂���R�[�h���w�肳�ꂽ�X�R�[�v�Ŏ��s���A���ʂ�Ԃ��܂��B</summary>
		/// <param name="compiledCode">���s����R�[�h���w�肵�܂��B</param>
		/// <param name="scope">�R�[�h�����s����X�R�[�v���w�肵�܂��B</param>
		/// <returns>�R�[�h���s�̌��ʁB</returns>
		public object Execute(CompiledCode compiledCode, ScriptScope scope)
		{
			Debug.Assert(_executingThread == null);
			_executingThread = Thread.CurrentThread;
			try
			{
				object result = compiledCode.Execute(scope);
				Console.WriteLine(RemoteCommandDispatcher.OutputCompleteMarker);
				return result;
			}
			catch (ThreadAbortException tae)
			{
				var pki = tae.ExceptionState as KeyboardInterruptException;
				if (pki != null)
				{
					// �قƂ�ǂ̗�O�̓N���C�A���g�ɋt�`�d����܂��B
					// �������AThreadAbortException �̓����[�e�B���O �C���t���X�g���N�`���ɂ���ĈقȂ��ď�������ARemotingException �Ƀ��b�v����܂��B
					// ("�T�[�o�[�ł̗v���̏������ɃG���[���������܂����B")
					// ���̂��߁A�t�B���^���đ���� KeyboardInterruptException �𔭐������܂��B
					Thread.ResetAbort();
					throw pki;
				}
				else
					throw;
			}
			finally { _executingThread = null; }
		}

		/// <summary>���ݎ��s���� <see cref="Execute"/> �ւ̌Ăяo���� <see cref="Thread.Abort(object)"/> �ɂ�蒆�~���܂��B</summary>
		/// <returns>���ۂ� <see cref="Thread.Abort(object)"/> ���Ă΂ꂽ�ꍇ�� <c>true</c>�B���s���� <see cref="Execute"/> �ւ̌Ăяo�������݂��Ȃ��ꍇ�� <c>false</c>�B</returns>
		public bool AbortCommand()
		{
			var executingThread = _executingThread;
			if (executingThread == null)
				return false;
			executingThread.Abort(new KeyboardInterruptException(""));
			return true;
		}

		// TODO: �ǂꂪ�������������ԂȂ̂����v�Z����
		/// <summary>�Ώۂ̃C���X�^���X�̗L�����ԃ|���V�[�𐧌䂷��A�L�����ԃT�[�r�X �I�u�W�F�N�g���擾���܂��B</summary>
		/// <returns>
		/// �Ώۂ̃C���X�^���X�̗L�����ԃ|���V�[�𐧌䂷��Ƃ��Ɏg�p����A<see cref="System.Runtime.Remoting.Lifetime.ILease"/> �^�̃I�u�W�F�N�g�B
		/// ���݂���ꍇ�́A���̃C���X�^���X�̌��݂̗L�����ԃT�[�r�X �I�u�W�F�N�g�ł��B����ȊO�̏ꍇ�́A<see cref="System.Runtime.Remoting.Lifetime.LifetimeServices.LeaseManagerPollTime"/>
		/// �v���p�e�B�̒l�ɏ��������ꂽ�V�����L�����ԃT�[�r�X �I�u�W�F�N�g�ł��B
		/// </returns>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
		public override object InitializeLifetimeService() { return null; }
	}
}
