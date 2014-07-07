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
using System.Runtime.InteropServices;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace Microsoft.Scripting.ComInterop
{
	/// <summary>ComTypes.EXCEPINFO �Ɏ��Ă��܂����A�Ǝ��̃}�[�V�������O���s���Ă��܂��B</summary>
	[StructLayout(LayoutKind.Sequential)]
	struct ExcepInfo
	{
		short wCode;
		short wReserved;
		IntPtr bstrSource;
		IntPtr bstrDescription;
		IntPtr bstrHelpFile;
		int dwHelpContext;
		IntPtr pvReserved;
		IntPtr pfnDeferredFillIn;
		int scode;
#if DEBUG
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2207:InitializeValueTypeStaticFieldsInline")]
		static ExcepInfo() { Debug.Assert(Marshal.SizeOf(typeof(ExcepInfo)) == Marshal.SizeOf(typeof(ComTypes.EXCEPINFO))); }
#endif
		static string ConvertAndFreeBstr(ref IntPtr bstr)
		{
			if (bstr == IntPtr.Zero)
				return null;
			var result = Marshal.PtrToStringBSTR(bstr);
			Marshal.FreeBSTR(bstr);
			bstr = IntPtr.Zero;
			return result;
		}

		internal void Dummy()
		{
			wCode = 0;
			wReserved = 0; wReserved++;
			bstrSource = IntPtr.Zero;
			bstrDescription = IntPtr.Zero;
			bstrHelpFile = IntPtr.Zero;
			dwHelpContext = 0;
			pfnDeferredFillIn = IntPtr.Zero;
			pvReserved = IntPtr.Zero;
			scode = 0;
			throw Error.MethodShouldNotBeCalled();
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
		internal Exception GetException()
		{
			Debug.Assert(pfnDeferredFillIn == IntPtr.Zero);
#if DEBUG
			System.Diagnostics.Debug.Assert(wReserved != -1);
			wReserved = -1; // ���\�b�h�� 1 �x�����Ă΂�Ă��Ȃ����Ƃ��m�F����
#endif
			var errorCode = scode != 0 ? scode : wCode;
			var exception = Marshal.GetExceptionForHR(errorCode);
			var message = ConvertAndFreeBstr(ref bstrDescription);
			if (message != null)
			{
				// �J�X�^�����b�Z�[�W������΁A�V���� Exception �I�u�W�F�N�g�����b�Z�[�W��K�؂ɐݒ肷�邱�Ƃō쐬����
				// "exception.Message" �͓ǂݎ���p�̃v���p�e�B�Ȃ̂ŐV�����I�u�W�F�N�g���쐬����K�v������
				if (exception is COMException)
					exception = new COMException(message, errorCode);
				else
				{
					var ctor = exception.GetType().GetConstructor(new[] { typeof(string) });
					if (ctor != null)
						exception = (Exception)ctor.Invoke(new object[] { message });
				}
			}
			exception.Source = ConvertAndFreeBstr(ref bstrSource);
			var helpLink = ConvertAndFreeBstr(ref bstrHelpFile);
			if (helpLink != null && dwHelpContext != 0)
				helpLink += "#" + dwHelpContext;
			exception.HelpLink = helpLink;
			return exception;
		}
	}
}