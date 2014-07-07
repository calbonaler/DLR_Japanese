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
using System.Dynamic;
using System.Linq.Expressions;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace Microsoft.Scripting.ComInterop
{
	/// <summary>�^�C�v���C�u�����ɒ�`����Ă��� COM �R�N���X�L�q��\���܂��B</summary>
	public class ComTypeClassDesc : ComTypeDesc, IDynamicMetaObjectProvider
	{
		LinkedList<string> _itfs; // implemented interfaces
		LinkedList<string> _sourceItfs; // source interfaces supported by this coclass
		Type _typeObj;

		/// <summary>���� COM �R�N���X�̃C���X�^���X���쐬���܂��B</summary>
		/// <returns>�쐬���ꂽ�C���X�^���X�B</returns>
		public object CreateInstance() { return Activator.CreateInstance(_typeObj ?? (_typeObj = Type.GetTypeFromCLSID(Guid))); }

		internal ComTypeClassDesc(ComTypes.ITypeInfo typeInfo, ComTypeLibDesc typeLibDesc) : base(typeInfo, ComType.Class, typeLibDesc)
		{
			var typeAttr = ComRuntimeHelpers.GetTypeAttrForTypeInfo(typeInfo);
			Guid = typeAttr.guid;
			for (int i = 0; i < typeAttr.cImplTypes; i++)
			{
				int hRefType;
				typeInfo.GetRefTypeOfImplType(i, out hRefType);
				ComTypes.ITypeInfo currentTypeInfo;
				typeInfo.GetRefTypeInfo(hRefType, out currentTypeInfo);
				ComTypes.IMPLTYPEFLAGS implTypeFlags;
				typeInfo.GetImplTypeFlags(i, out implTypeFlags);
				AddInterface(currentTypeInfo, (implTypeFlags & ComTypes.IMPLTYPEFLAGS.IMPLTYPEFLAG_FSOURCE) != 0);
			}
		}

		void AddInterface(ComTypes.ITypeInfo itfTypeInfo, bool isSourceItf)
		{
			string itfName = ComRuntimeHelpers.GetNameOfType(itfTypeInfo);
			if (isSourceItf)
				(_sourceItfs ?? (_sourceItfs = new LinkedList<string>())).AddLast(itfName);
			else
				(_itfs ?? (_itfs = new LinkedList<string>())).AddLast(itfName);
		}

		internal bool Implements(string itfName, bool isSourceItf) { return isSourceItf ? _sourceItfs.Contains(itfName) : _itfs.Contains(itfName); }

		/// <summary>���̃I�u�W�F�N�g�ɑ΂��Ď��s����鑀����o�C���h���� <see cref="System.Dynamic.DynamicMetaObject"/> ��Ԃ��܂��B</summary>
		/// <param name="parameter">�����^�C���l�̎��c���[�\���B</param>
		/// <returns>���̃I�u�W�F�N�g���o�C���h���� <see cref="System.Dynamic.DynamicMetaObject"/>�B</returns>
		public DynamicMetaObject GetMetaObject(Expression parameter) { return new ComClassMetaObject(parameter, this); }
	}
}