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

namespace Microsoft.Scripting.Runtime
{
	/// <summary>
	/// DLR �͂�����z�X�e�B���O API �v���o�C�_�ɂ��̃N���X���������A���̃C���X�^���X�������^�C���̏������Œ񋟂��邱�Ƃ�v�����܂��B
	/// DLR �͊�{�I�ȃz�X�g/�V�X�e���ˑ��̓�������̃N���X��p���ČĂяo���܂��B
	/// </summary>
	[Serializable]
	public abstract class DynamicRuntimeHostingProvider
	{
		/// <summary>�z�X�g�Ɋ֘A�t����ꂽ <see cref="Microsoft.Scripting.PlatformAdaptationLayer"/> ���擾���܂��B</summary>
		public abstract PlatformAdaptationLayer PlatformAdaptationLayer { get; }
	}
}
