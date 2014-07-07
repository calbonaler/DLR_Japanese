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
using System.Dynamic;
using System.Linq.Expressions;

namespace Microsoft.Scripting.ComInterop
{
	/// <summary>COM �̃^�C�v���C�u�����Ɋւ���������J���܂��B</summary>
	public sealed class ComTypeLibInfo : IDynamicMetaObjectProvider
	{
		internal ComTypeLibInfo(ComTypeLibDesc typeLibDesc) { TypeLibDesc = typeLibDesc; }

		/// <summary>�^�C�v���C�u�����̖��O���擾���܂��B</summary>
		public string Name { get { return TypeLibDesc.Name; } }

		/// <summary>�^�C�v���C�u�����̃O���[�o����Ӄ��C�u�������ʎq���擾���܂��B</summary>
		public Guid Guid { get { return TypeLibDesc.Guid; } }

		/// <summary>�^�C�v���C�u�����̃��W���[�o�[�W�����ԍ����擾���܂��B</summary>
		public short VersionMajor { get { return TypeLibDesc.VersionMajor; } }

		/// <summary>�^�C�v���C�u�����̃}�C�i�[�o�[�W�����ԍ����擾���܂��B</summary>
		public short VersionMinor { get { return TypeLibDesc.VersionMinor; } }

		/// <summary>�^�C�v���C�u������\�� <see cref="ComTypeLibDesc"/> ���擾���܂��B</summary>
		public ComTypeLibDesc TypeLibDesc { get; private set; }

		internal string[] GetMemberNames() { return new string[] { this.Name, "Guid", "Name", "VersionMajor", "VersionMinor" }; }

		DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) { return new TypeLibInfoMetaObject(parameter, this); }
	}
}