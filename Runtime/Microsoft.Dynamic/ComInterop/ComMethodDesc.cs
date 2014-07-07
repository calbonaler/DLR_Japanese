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

using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;

namespace Microsoft.Scripting.ComInterop
{
	/// <summary>�^�C�v���C�u�����ɒ�`����Ă��郁�\�b�h�L�q��\���܂��B</summary>
	public class ComMethodDesc
	{
		readonly int _memid;  // FUNCDESC.memid ��蒊�o���ꂽ�����o�[ID
		internal readonly INVOKEKIND InvokeKind;

		ComMethodDesc(int dispId) { _memid = dispId; }

		internal ComMethodDesc(string name, int dispId) : this(dispId) { Name = name; } // no ITypeInfo constructor

		internal ComMethodDesc(string name, int dispId, INVOKEKIND invkind) : this(name, dispId) { InvokeKind = invkind; }

		internal ComMethodDesc(ITypeInfo typeInfo, FUNCDESC funcDesc) : this(funcDesc.memid)
		{
			InvokeKind = funcDesc.invkind;
			int cNames;
			var rgNames = new string[1 + funcDesc.cParams];
			typeInfo.GetNames(_memid, rgNames, rgNames.Length, out cNames);
			if (IsPropertyPut && rgNames[rgNames.Length - 1] == null)
			{
				rgNames[rgNames.Length - 1] = "value";
				cNames++;
			}
			Debug.Assert(cNames == rgNames.Length);
			Name = rgNames[0];
			ParamCount = funcDesc.cParams;
		}

		/// <summary>���\�b�h�̖��O���擾���܂��B</summary>
		public string Name { get; private set; }

		/// <summary>���\�b�h�� DispID ���擾���܂��B</summary>
		public int DispId { get { return _memid; } }

		/// <summary>���\�b�h���ʏ�̃v���p�e�B �A�N�Z�X�̍\�����g�p���ČĂяo����邩�ǂ����������l���擾���܂��B</summary>
		public bool IsPropertyGet { get { return (InvokeKind & INVOKEKIND.INVOKE_PROPERTYGET) != 0; } }

		/// <summary>���\�b�h���f�[�^�����o�ł��邩�ǂ����������l���擾���܂��B</summary>
		public bool IsDataMember { get { return IsPropertyGet && DispId != ComDispIds.DISPID_NEWENUM && ParamCount == 0; } } // �ʏ�� get �ł���������Ȃ�

		/// <summary>���\�b�h���ʏ�̃v���p�e�B���蓖�Ă̍\�����g�p���ČĂяo����邩�ǂ����������l���擾���܂��B</summary>
		public bool IsPropertyPut { get { return (InvokeKind & (INVOKEKIND.INVOKE_PROPERTYPUT | INVOKEKIND.INVOKE_PROPERTYPUTREF)) != 0; } }

		/// <summary>���\�b�h���v���p�e�B�Q�Ɗ��蓖�Ă̍\�����g�p���ČĂяo����邩�ǂ����������l���擾���܂��B</summary>
		public bool IsPropertyPutRef { get { return (InvokeKind & INVOKEKIND.INVOKE_PROPERTYPUTREF) != 0; } }

		/// <summary>���\�b�h�̈����̌����擾���܂��B</summary>
		internal int ParamCount { get; private set; }
	}
}