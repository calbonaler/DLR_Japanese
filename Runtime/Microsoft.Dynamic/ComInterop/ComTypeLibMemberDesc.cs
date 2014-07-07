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

namespace Microsoft.Scripting.ComInterop
{
	/// <summary>�^�C�v���C�u�����ɒ�`����Ă��� COM �̃����o��\���܂��B</summary>
	public abstract class ComTypeLibMemberDesc
	{
		internal ComTypeLibMemberDesc(ComType kind) { Kind = kind; }

		/// <summary>���̃����o�̎�ނ��擾���܂��B</summary>
		public ComType Kind { get; private set; }
	}
}