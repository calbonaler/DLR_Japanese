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
using System.Globalization;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace Microsoft.Scripting.ComInterop
{
	/// <summary>�^�C�v���C�u�����ɒ�`����Ă��� COM �񋓌^�L�q��\���܂��B</summary>
	public sealed class ComTypeEnumDesc : ComTypeDesc, IDynamicMetaObjectProvider
	{
		readonly KeyValuePair<string, object>[] _members;

		/// <summary>���̃I�u�W�F�N�g�̕�����\�����擾���܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̕�����\���B</returns>
		public override string ToString() { return string.Format(CultureInfo.CurrentCulture, "<enum '{0}'>", TypeName); }

		internal ComTypeEnumDesc(ComTypes.ITypeInfo typeInfo, ComTypeLibDesc typeLibDesc) : base(typeInfo, ComType.Enum, typeLibDesc)
		{
			ComTypes.TYPEATTR typeAttr = ComRuntimeHelpers.GetTypeAttrForTypeInfo(typeInfo);
			KeyValuePair<string, object>[] members = new KeyValuePair<string, object>[typeAttr.cVars];
			IntPtr p = IntPtr.Zero;
			// For each enum member get name and value.
			for (int i = 0; i < typeAttr.cVars; i++)
			{
				typeInfo.GetVarDesc(i, out p);
				// Get the enum member value (as object).
				ComTypes.VARDESC varDesc;
				object value = null;
				try
				{
					varDesc = (ComTypes.VARDESC)Marshal.PtrToStructure(p, typeof(ComTypes.VARDESC));
					if (varDesc.varkind == ComTypes.VARKIND.VAR_CONST)
						value = Marshal.GetObjectForNativeVariant(varDesc.desc.lpvarValue);
				}
				finally { typeInfo.ReleaseVarDesc(p); }
				// Get the enum member name
				members[i] = new KeyValuePair<string, object>(ComRuntimeHelpers.GetNameOfMethod(typeInfo, varDesc.memid), value);
			}
			_members = members;
		}

		DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) { return new TypeEnumMetaObject(this, parameter); }

		/// <summary>�w�肳�ꂽ���O�ɑ΂���񋓌^�̒l���擾���܂��B</summary>
		/// <param name="name">�l���擾���閼�O���w�肵�܂��B</param>
		/// <returns>���O�Ɋ֘A�t����ꂽ�l�B</returns>
		public object GetValue(string name)
		{
			var index = Array.FindIndex(_members, x => x.Key == name);
			if (index >= 0)
				return _members[index].Value;
			throw new MissingMemberException(name);
		}

		internal bool HasMember(string name) { return Array.Exists(_members, x => x.Key == name); }

		// TODO: internal
		internal string[] GetMemberNames() { return Array.ConvertAll(_members, x => x.Key); }
	}
}