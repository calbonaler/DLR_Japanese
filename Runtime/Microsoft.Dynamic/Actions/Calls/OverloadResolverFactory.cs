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

using System.Collections.Generic;
using System.Dynamic;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary><see cref="DefaultOverloadResolver"/> ���쐬������@�𒊏ۉ����܂��B</summary>
	public abstract class OverloadResolverFactory
	{
		/// <summary>�w�肳�ꂽ��������ьĂяo���V�O�l�`�����g�p���ĐV���� <see cref="Microsoft.Scripting.Actions.DefaultOverloadResolver"/> ���쐬���܂��B</summary>
		/// <param name="args">�I�[�o�[���[�h�����̑ΏۂƂȂ�����̃��X�g���w�肵�܂��B</param>
		/// <param name="signature">�I�[�o�[���[�h���Ăяo���V�O�l�`�����w�肵�܂��B</param>
		/// <param name="callType">�I�[�o�[���[�h���Ăяo�����@���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ��������уV�O�l�`���ɑ΂���I�[�o�[���[�h���������� <see cref="DefaultOverloadResolver"/>�B</returns>
		public abstract DefaultOverloadResolver CreateOverloadResolver(IList<DynamicMetaObject> args, CallSignature signature, CallTypes callType);
	}
}
