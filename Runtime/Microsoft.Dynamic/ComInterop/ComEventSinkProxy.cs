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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.ComInterop
{
	/// <summary>
	/// <see cref="ComEventSink"/> �̃C���X�^���X�� sourceIid �ɑ΂��� QueryInterface �̃n���h���ɐӔC�𕉂��܂��B
	/// 
	/// �w�i: COM �C�x���g �V���N���R�l�N�V���� �|�C���g�� Advise ����ꍇ�Adispinterface ���n����邱�Ƃ����肳��܂��B
	/// ���݁ACOM �N���C�A���g���������|�C���^��n���ƐM�����Ă���z�X�g������܂����A�����łȂ����̂�����܂��B
	/// �Ⴆ�΁AExcel �̃R�l�N�V�����|�C���g�̎����͓n���ꂽ�|�C���^�ɑ΂��� QueryInterface ���Ăяo���܂��񂪁AWord �͌Ăяo���܂��B
	/// 
	/// <see cref="ComEventSink"/> �͋��������΁A������v������Ă���C���^�[�t�F�C�X���������܂���B<see cref="IReflect"/> ���g�p���āu��₁v���Ă��邾���ł��B
	/// ���̂��߁AIConnectionPoint.Advise �ɓn���ꂽ�|�C���^�ɑ΂��� Word �� QueryInterface �͎��s���܂��B
	/// �����h�����߂ɁA���̃N���X�̂悤�Ɂu������v���Ƃ��ł��� <see cref="RealProxy"/> �̗��_�𗘗p���āA���ۂ̓T�|�[�g���Ȃ��C���^�[�t�F�C�X�ɑ΂��� QueryInterface �𐬌������܂��B
	/// (�C�x���g �V���N�ւ̌Ăяo���̏ꍇ�A���ʂ̃v���N�e�B�X�� IDispatch.Invoke ���g�p���邱�ƂȂ̂Łu���͂��̃C���^�[�t�F�C�X���������܂��v�ƌ�����Ώ\���B)
	/// </summary>
	sealed class ComEventSinkProxy : RealProxy
	{
		Guid _sinkIid;
		ComEventSink _sink;
		static readonly MethodInfo _methodInfoInvokeMember = typeof(ComEventSink).GetMethod("InvokeMember", BindingFlags.Instance | BindingFlags.Public);

		public ComEventSinkProxy(ComEventSink sink, Guid sinkIid) : base(typeof(ComEventSink))
		{
			_sink = sink;
			_sinkIid = sinkIid;
		}

		// iid ���V���N�̂��̂ł���ꍇ�A���N���X�� IDispatch ���� RCW �����߂�
		public override IntPtr SupportsInterface(ref Guid iid) { return iid == _sinkIid ? Marshal.GetIDispatchForObject(_sink) : base.SupportsInterface(ref iid); }

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public override IMessage Invoke(IMessage msg)
		{
			ContractUtils.RequiresNotNull(msg, "msg");
			// ���\�b�h�Ăяo�����n���h��������@�����m���Ă��� (�v���p�e�B��t�B�[���h�A�N�Z�T�̓��\�b�h�Ƃ݂Ȃ����)
			var mcm = msg as IMethodCallMessage;
			if (mcm == null)
				throw new NotSupportedException();
			// ComEventSink.InvokeMember �͓��ʂɃn���h�����܂��B
			// ���̕K�v�����闝�R�́ARealProxy.Invoke ��ʂ��ČĂ΂ꂽ�ꍇ�ɁA
			// �ǂ̂悤�� namedParameters ���� (IMethodCallMessage.Args �z��� 7 �Ԗڂ̗v�f) ���}�[�V�������O����邩�ɂ�邽�߂ł��B
			// RealProxy.Invoke �ł� namedParameters �� object[] �^�ł����AInvokeMember �� string[] �^�����҂��Ă��܂��B
			// �P���ɂ��̌Ăяo���� (RemotingServices.ExecuteMessage ���g�p����) ���̂܂ܓn���ꍇ�A
			// �����[�g�� namedParameter (object[]) �� (namedParameters �Ƃ��� string[] ��\������) InvokeMember �ɓn�����Ƃ����ꍇ�� InvalidCastException �𓾂邱�ƂɂȂ�܂��B
			// ���̂��߁AComEventSink.InvokeMember �ł� namedParameters �͎g�p���܂���B�܂�A�P���Ɉ����𖳎����� null ��n���܂��B
			if (((MethodInfo)mcm.MethodBase) == _methodInfoInvokeMember)
			{
				object retVal = null;
				try
				{
					retVal = ((IReflect)_sink).InvokeMember(mcm.Args[0] as string, (BindingFlags)mcm.Args[1], mcm.Args[2] as Binder, null, mcm.Args[4] as object[], mcm.Args[5] as ParameterModifier[], mcm.Args[6] as CultureInfo, null);
				}
				catch (Exception ex) { return new ReturnMessage(ex.InnerException, mcm); }
				return new ReturnMessage(retVal, mcm.Args, mcm.ArgCount, null, mcm);
			}
			return RemotingServices.ExecuteMessage(_sink, mcm);
		}
	}
}