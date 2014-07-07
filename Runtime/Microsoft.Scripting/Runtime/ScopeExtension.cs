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

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Runtime
{
	// TODO: this class should be abstract
	/// <summary>�X�R�[�v�ɕt������錾�ꂲ�Ƃ̏����i�[����X�R�[�v�g���q��\���܂��B</summary>
	public abstract class ScopeExtension
	{
		/// <summary><see cref="ScopeExtension"/> �̋�̔z����擾���܂��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
		public static readonly ScopeExtension[] EmptyArray = new ScopeExtension[0];

		/// <summary>���̃X�R�[�v�g���q���֘A�t�����Ă���X�R�[�v���擾���܂��B</summary>
		public Scope Scope { get; private set; }

		/// <summary>�֘A�Â���X�R�[�v���g�p���āA<see cref="Microsoft.Scripting.Runtime.ScopeExtension"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="scope">���̃X�R�[�v�g���q���֘A�t����X�R�[�v���w�肵�܂��B</param>
		protected ScopeExtension(Scope scope)
		{
			ContractUtils.RequiresNotNull(scope, "scope");
			Scope = scope;
		}
	}
}
