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
	/// <summary>タイプライブラリに定義されている COM 列挙型記述を表します。</summary>
	public sealed class ComTypeEnumDesc : ComTypeDesc, IDynamicMetaObjectProvider
	{
		readonly KeyValuePair<string, object>[] _members;

		/// <summary>このオブジェクトの文字列表現を取得します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
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

		/// <summary>指定された名前に対する列挙型の値を取得します。</summary>
		/// <param name="name">値を取得する名前を指定します。</param>
		/// <returns>名前に関連付けられた値。</returns>
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