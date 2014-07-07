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
	/// <summary>タイプライブラリに定義されているメソッド記述を表します。</summary>
	public class ComMethodDesc
	{
		readonly int _memid;  // FUNCDESC.memid より抽出されたメンバーID
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

		/// <summary>メソッドの名前を取得します。</summary>
		public string Name { get; private set; }

		/// <summary>メソッドの DispID を取得します。</summary>
		public int DispId { get { return _memid; } }

		/// <summary>メソッドが通常のプロパティ アクセスの構文を使用して呼び出されるかどうかを示す値を取得します。</summary>
		public bool IsPropertyGet { get { return (InvokeKind & INVOKEKIND.INVOKE_PROPERTYGET) != 0; } }

		/// <summary>メソッドがデータメンバであるかどうかを示す値を取得します。</summary>
		public bool IsDataMember { get { return IsPropertyGet && DispId != ComDispIds.DISPID_NEWENUM && ParamCount == 0; } } // 通常の get であり引数がない

		/// <summary>メソッドが通常のプロパティ割り当ての構文を使用して呼び出されるかどうかを示す値を取得します。</summary>
		public bool IsPropertyPut { get { return (InvokeKind & (INVOKEKIND.INVOKE_PROPERTYPUT | INVOKEKIND.INVOKE_PROPERTYPUTREF)) != 0; } }

		/// <summary>メソッドがプロパティ参照割り当ての構文を使用して呼び出されるかどうかを示す値を取得します。</summary>
		public bool IsPropertyPutRef { get { return (InvokeKind & INVOKEKIND.INVOKE_PROPERTYPUTREF) != 0; } }

		/// <summary>メソッドの引数の個数を取得します。</summary>
		internal int ParamCount { get; private set; }
	}
}