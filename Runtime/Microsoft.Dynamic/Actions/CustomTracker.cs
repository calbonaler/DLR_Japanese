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

namespace Microsoft.Scripting.Actions
{
	/// <summary>���ꂪ�C�ӂ̃����o�������v���Z�X�ɎQ�������邱�Ƃ��ł���悤�ɂ���J�X�^�������o�g���b�J�[��\���܂��B</summary>
	public abstract class CustomTracker : MemberTracker
	{
		/// <summary><see cref="Microsoft.Scripting.Actions.CustomTracker"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		protected CustomTracker() { }

		/// <summary><see cref="MemberTracker"/> �̎�ނ��擾���܂��B</summary>
		public sealed override TrackerTypes MemberType { get { return TrackerTypes.Custom; } }
	}
}
