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
using System.Runtime.CompilerServices;

namespace Microsoft.Scripting.ComInterop
{
	sealed class SplatCallSite
	{
		// �Ăяo���\�ȃf���Q�[�g�܂��� IDynamicMetaObjectProvider ���i�[����܂�
		internal readonly object _callable;

		// �^����ꂽ�C�x���g�ɓn�������̐��͌Ăяo�����ƂɈقȂ�\��������?
		// �����łȂ��Ȃ�A���̃��x���̊Ԑډ��͗v��Ȃ��B�W�J���s���f���Q�[�g���L���b�V���ł��邩��B
		internal CallSite<Func<CallSite, object, object[], object>> _site;

		internal SplatCallSite(object callable)
		{
			Debug.Assert(callable != null);
			_callable = callable;
		}

		internal object Invoke(object[] args)
		{
			Debug.Assert(args != null);
			// �f���Q�[�g�Ȃ�ADynamicInvoke �Ƀo�C���f�B���O���s�킹��B
			var d = _callable as Delegate;
			if (d != null)
				return d.DynamicInvoke(args);
			// �����łȂ��Ȃ�΁A�R�[���T�C�g���쐬���Ăяo���B
			if (_site == null)
				_site = CallSite<Func<CallSite, object, object[], object>>.Create(SplatInvokeBinder.Instance);
			return _site.Target(_site, _callable, args);
		}
	}
}