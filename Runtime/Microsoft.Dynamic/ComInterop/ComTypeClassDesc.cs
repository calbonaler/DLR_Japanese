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
	/// <summary>タイプライブラリに定義されている COM コクラス記述を表します。</summary>
	public class ComTypeClassDesc : ComTypeDesc, IDynamicMetaObjectProvider
	{
		LinkedList<string> _itfs; // implemented interfaces
		LinkedList<string> _sourceItfs; // source interfaces supported by this coclass
		Type _typeObj;

		/// <summary>この COM コクラスのインスタンスを作成します。</summary>
		/// <returns>作成されたインスタンス。</returns>
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

		/// <summary>このオブジェクトに対して実行される操作をバインドする <see cref="System.Dynamic.DynamicMetaObject"/> を返します。</summary>
		/// <param name="parameter">ランタイム値の式ツリー表現。</param>
		/// <returns>このオブジェクトをバインドする <see cref="System.Dynamic.DynamicMetaObject"/>。</returns>
		public DynamicMetaObject GetMetaObject(Expression parameter) { return new ComClassMetaObject(parameter, this); }
	}
}