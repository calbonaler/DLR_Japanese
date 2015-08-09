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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace Microsoft.Scripting.ComInterop
{
	/// <summary>タイプライブラリに定義されている COM の型記述を表します。</summary>
	public class ComTypeDesc : ComTypeLibMemberDesc
	{
		string _typeName;
		string _documentation;
		ComMethodDesc _getItem;
		ComMethodDesc _setItem;
		static readonly Dictionary<string, ComEventDesc> _EmptyEventsDict = new Dictionary<string, ComEventDesc>();

		internal ComTypeDesc(ITypeInfo typeInfo, ComType memberType, ComTypeLibDesc typeLibDesc) : base(memberType)
		{
			if (typeInfo != null)
				ComRuntimeHelpers.GetInfoFromType(typeInfo, out _typeName, out _documentation);
			TypeLib = typeLibDesc;
		}

		internal static ComTypeDesc FromITypeInfo(ComTypes.ITypeInfo typeInfo, ComTypes.TYPEATTR typeAttr)
		{
			if (typeAttr.typekind == ComTypes.TYPEKIND.TKIND_COCLASS)
				return new ComTypeClassDesc(typeInfo, null);
			else if (typeAttr.typekind == ComTypes.TYPEKIND.TKIND_ENUM)
				return new ComTypeEnumDesc(typeInfo, null);
			else if (typeAttr.typekind == ComTypes.TYPEKIND.TKIND_DISPATCH || typeAttr.typekind == ComTypes.TYPEKIND.TKIND_INTERFACE)
				return new ComTypeDesc(typeInfo, ComType.Interface, null);
			else
				throw new InvalidOperationException("サポートされていない列挙体型をラップしようとしました。");
		}

		internal static ComTypeDesc CreateEmptyTypeDesc()
		{
			ComTypeDesc typeDesc = new ComTypeDesc(null, ComType.Interface, null);
			typeDesc.Funcs = new Dictionary<string, ComMethodDesc>();
			typeDesc.Puts = new Dictionary<string, ComMethodDesc>();
			typeDesc.PutRefs = new Dictionary<string, ComMethodDesc>();
			typeDesc.Events = _EmptyEventsDict;
			return typeDesc;
		}

		internal static Dictionary<string, ComEventDesc> EmptyEvents { get { return _EmptyEventsDict; } }

		internal Dictionary<string, ComMethodDesc> Funcs { get; set; }

		internal Dictionary<string, ComMethodDesc> Puts { get; set; }

		internal Dictionary<string, ComMethodDesc> PutRefs { get; set; }

		internal Dictionary<string, ComEventDesc> Events { get; set; }

		internal bool TryGetFunc(string name, out ComMethodDesc method) { return Funcs.TryGetValue(name.ToUpper(System.Globalization.CultureInfo.InvariantCulture), out method); }

		internal void AddFunc(string name, ComMethodDesc method)
		{
			lock (Funcs)
				Funcs[name.ToUpper(System.Globalization.CultureInfo.InvariantCulture)] = method;
		}

		internal bool TryGetPut(string name, out ComMethodDesc method) { return Puts.TryGetValue(name.ToUpper(System.Globalization.CultureInfo.InvariantCulture), out method); }

		internal void AddPut(string name, ComMethodDesc method)
		{
			lock (Puts)
				Puts[name.ToUpper(System.Globalization.CultureInfo.InvariantCulture)] = method;
		}

		internal bool TryGetPutRef(string name, out ComMethodDesc method) { return PutRefs.TryGetValue(name.ToUpper(System.Globalization.CultureInfo.InvariantCulture), out method); }

		internal void AddPutRef(string name, ComMethodDesc method)
		{
			lock (PutRefs)
				PutRefs[name.ToUpper(System.Globalization.CultureInfo.InvariantCulture)] = method;
		}

		internal bool TryGetEvent(string name, out ComEventDesc @event) { return Events.TryGetValue(name.ToUpper(System.Globalization.CultureInfo.InvariantCulture), out @event); }

		internal string[] GetMemberNames(bool dataOnly)
		{
			var names = new HashSet<string>();
			lock (Funcs)
			{
				foreach (var func in Funcs.Values)
				{
					if (!dataOnly || func.IsDataMember)
						names.Add(func.Name);
				}
			}
			if (!dataOnly)
			{
				lock (Puts)
				{
					foreach (var func in Puts.Values)
						names.Add(func.Name);
				}
				lock (PutRefs)
				{
					foreach (var func in PutRefs.Values)
						names.Add(func.Name);
				}
				if (Events != null && Events.Count > 0)
				{
					foreach (var name in Events.Keys)
						names.Add(name);
				}
			}
			string[] result = new string[names.Count];
			names.CopyTo(result, 0);
			return result;
		}

		// this property is public - accessed by an AST
		/// <summary>この型の名前を取得します。</summary>
		public string TypeName { get { return _typeName; } }

		internal string Documentation { get { return _documentation; } }

		// this property is public - accessed by an AST
		/// <summary>この型が格納されているタイプライブラリを表す <see cref="ComTypeLibDesc"/> を取得します。</summary>
		public ComTypeLibDesc TypeLib { get; private set; }

		internal Guid Guid { get; set; }

		internal ComMethodDesc GetItem { get { return _getItem; } }

		internal void EnsureGetItem(ComMethodDesc candidate) { Interlocked.CompareExchange(ref _getItem, candidate, null); }

		internal ComMethodDesc SetItem { get { return _setItem; } }

		internal void EnsureSetItem(ComMethodDesc candidate) { Interlocked.CompareExchange(ref _setItem, candidate, null); }
	}
}