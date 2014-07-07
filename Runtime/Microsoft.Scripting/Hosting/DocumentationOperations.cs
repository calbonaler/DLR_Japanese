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
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Security.Permissions;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Hosting
{
	/// <summary>�������Ԓ��̃I�u�W�F�N�g�ɑ΂��� REPL �E�B���h�E�Ŏg�p�����h�L�������g��񋟂��܂��B</summary>
	public sealed class DocumentationOperations : MarshalByRefObject
	{
		readonly DocumentationProvider _provider;

		/// <summary>
		/// �w�肳�ꂽ <see cref="Microsoft.Scripting.Runtime.DocumentationProvider"/> ���g�p���āA
		/// <see cref="Microsoft.Scripting.Hosting.DocumentationOperations"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="provider">�h�L�������g��񋟂��� <see cref="Microsoft.Scripting.Runtime.DocumentationProvider"/> ���w�肵�܂��B</param>
		internal DocumentationOperations(DocumentationProvider provider) { _provider = provider; }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g�ɒ�`����Ă��闘�p�\�ȃ����o���擾���܂��B</summary>
		/// <param name="value">�����o���擾����I�u�W�F�N�g���w�肵�܂��B</param>
		public ICollection<MemberDoc> GetMembers(object value) { return _provider.GetMembers(value); }

		/// <summary>�w�肳�ꂽ�I�u�W�F�N�g���Ăяo���\�ł���΁A���p�\�ȃI�[�o�[���[�h���擾���܂��B</summary>
		/// <param name="value">�I�[�o�[���[�h���擾����I�u�W�F�N�g���w�肵�܂��B</param>
		public ICollection<OverloadDoc> GetOverloads(object value) { return _provider.GetOverloads(value); }

		/// <summary>�w�肳�ꂽ�����[�g�I�u�W�F�N�g�ɒ�`����Ă��闘�p�\�ȃ����o���擾���܂��B</summary>
		/// <param name="value">�����o���擾���郊���[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		public ICollection<MemberDoc> GetMembers(ObjectHandle value) { return _provider.GetMembers(value.Unwrap()); }

		/// <summary>�w�肳�ꂽ�����[�g�I�u�W�F�N�g���Ăяo���\�ł���΁A���p�\�ȃI�[�o�[���[�h���擾���܂��B</summary>
		/// <param name="value">�I�[�o�[���[�h���擾���郊���[�g�I�u�W�F�N�g���w�肵�܂��B</param>
		public ICollection<OverloadDoc> GetOverloads(ObjectHandle value) { return _provider.GetOverloads(value.Unwrap()); }

		// TODO: Figure out what is the right lifetime
		/// <summary>�Ώۂ̃C���X�^���X�̗L�����ԃ|���V�[�𐧌䂷��A�L�����ԃT�[�r�X �I�u�W�F�N�g���擾���܂��B</summary>
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
		public override object InitializeLifetimeService() { return null; }
	}
}
