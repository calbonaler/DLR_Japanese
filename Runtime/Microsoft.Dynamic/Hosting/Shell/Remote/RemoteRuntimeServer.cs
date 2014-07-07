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
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting.Lifetime;

namespace Microsoft.Scripting.Hosting.Shell.Remote
{
	/// <summary>�����[�g�����^�C���T�[�o�[�����������ꂽ <see cref="ScriptEngine"/> �� <see cref="ScriptRuntime"/> �������[�e�B���O�`�����l����ʂ��Č��J����ۂɎg�p����܂��B</summary>
	static class RemoteRuntimeServer
	{
		internal const string CommandDispatcherUri = "CommandDispatcherUri";
		internal const string RemoteRuntimeArg = "-X:RemoteRuntimeChannel";

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2116:AptcaMethodsShouldOnlyCallAptcaMethods")] // TODO: Microsoft.Scripting does not need to be APTCA
		internal static IpcChannel CreateChannel(string channelName, string portName)
		{
			// �`�����l�����쐬����
			return new IpcChannel(
				new System.Collections.Hashtable()
				{
					{ "name", channelName },
					{ "portName", portName },
					// exclusiveAddressUse �� CreateNamedPipe �� FILE_FLAG_FIRST_PIPE_INSTANCE �t���O�ɑΉ����Ă��܂�
					// ����� true �ɐݒ肷��� "IPC �|�[�g���쐬�ł��܂���: �A�N�Z�X�����ۂ���܂���" �Ƃ����G���[���Ƃ��ǂ��������܂�
					// TODO: ����� false �ɐݒ肷�邱�Ƃ� ACL ���g�p���������S�ł�
					{ "exclusiveAddressUse", false },
				},
				null,
				// The Hosting API classes require TypeFilterLevel.Full to be remoted 
				new BinaryServerFormatterSinkProvider() { TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full }
			);
		}

		/// <summary>�z�X�g���g�p�ł���悤�ɃI�u�W�F�N�g�����J���āA(���̓X�g���[�����I�[�v�������܂�) �������Ƀu���b�N���܂��B</summary>
		/// <param name="remoteRuntimeChannelName">�����[�g�R���\�[���� <see cref="ScriptEngine"/> �Ƃ̑��ݒʐM�Ɏg�p����Ɨ\������� IPC �`�����l�����w�肵�܂��B</param>
		/// <param name="scope">�X�N���v�g�R�}���h�����̊J�n�������ł��Ă��鏉�����ς݂� <see cref="ScriptScope"/> ���w�肵�܂��B</param>
		/// <remarks>
		/// ����: 1 �̃I�u�W�F�N�g�݂̂����J����悤�ɂ��āA���̃I�u�W�F�N�g�͂��ꂩ��A�N�Z�X�ł���悤�ɂ��Ă��������B
		/// �����[�e�B���O�͗����̃v���L�V�ɑ΂���T�[�o�[�I�u�W�F�N�g�������T�[�o�[�ɂ��邩�ǂ�����m����@���Ȃ����߁A
		/// �����̃I�u�W�F�N�g�̔��s�̓N���C�A���g�� "remoteProxy1(remoteProxy2)" �̂悤�ȌĂяo��������ꍇ�ɁA���𔭐������鋰�ꂪ����܂��B
		/// </remarks>
		internal static void StartServer(string remoteRuntimeChannelName, ScriptScope scope)
		{
			Debug.Assert(ChannelServices.GetChannel(remoteRuntimeChannelName) == null);
			var channel = CreateChannel("ipc", remoteRuntimeChannelName);
			LifetimeServices.LeaseTime = TimeSpan.FromDays(7);
			LifetimeServices.LeaseManagerPollTime = TimeSpan.FromDays(7);
			LifetimeServices.RenewOnCallTime = TimeSpan.FromDays(7);
			LifetimeServices.SponsorshipTimeout = TimeSpan.FromDays(7);
			ChannelServices.RegisterChannel(channel, false);
			try
			{
				RemotingServices.Marshal(new RemoteCommandDispatcher(scope), CommandDispatcherUri);
				// �����[�g�R���\�[���� (���݂����) �N�����o�͂������������Ƃ�m�点�܂��B
				// ���ׂĂ̋N�����o�͂����s�O�Ƀ����[�g�R���\�[���ɓ������邱�Ƃ�K�v�Ƃ��Ă���̂ŁA���O�t���C�x���g�̑���ɂ�����g�p���܂��B
				Console.WriteLine(RemoteCommandDispatcher.OutputCompleteMarker);
				// Console.In �Ńu���b�N���܂��B
				// ����̓z�X�g���I�������������Ƃ��� ReadLine �� null ��Ԃ��̂ŁA�I���𔻒f���邽�߂Ɏg�p����܂��B
				var input = Console.ReadLine();
				Debug.Assert(input == null);
			}
			finally { ChannelServices.UnregisterChannel(channel); }
		}
	}
}