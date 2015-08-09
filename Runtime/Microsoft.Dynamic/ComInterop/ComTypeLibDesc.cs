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
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace Microsoft.Scripting.ComInterop
{
	/// <summary>
	/// タイプライブラリのキャッシュされた情報を表します。
	/// 要求された情報のみが保存されます。
	/// コクラスはイベント フックアップに使用されます。
	/// 列挙体はスクリプトからシンボル名でアクセスするために格納されます。
	/// </summary>
	public sealed class ComTypeLibDesc : IDynamicMetaObjectProvider
	{
		// 通常タイプライブラリは非常に少数のコクラスしか含まないため、要素数が少ない場合にパフォーマンスがよいリンクリストを使用します。
		LinkedList<ComTypeClassDesc> _classes = new LinkedList<ComTypeClassDesc>();
		Dictionary<string, ComTypeEnumDesc> _enums = new Dictionary<string, ComTypeEnumDesc>();
		ComTypes.TYPELIBATTR _typeLibAttributes;

		readonly static Dictionary<Guid, ComTypeLibDesc> _CachedTypeLibDesc = new Dictionary<Guid, ComTypeLibDesc>();

		ComTypeLibDesc() { }

		/// <summary>このオブジェクトの文字列表現を取得します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		public override string ToString() { return string.Format(CultureInfo.CurrentCulture, "<type library {0}>", Name); }

		/// <summary>このオブジェクトのドキュメントを取得します。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
		public string Documentation { get { return string.Empty; } }

		DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) { return new TypeLibMetaObject(parameter, this); }

		/// <summary>指定された GUID に対応する登録されている最新のタイプライブラリとそれに含まれているコクラスおよび列挙体を読み取り、コクラスのインスタンス化と列挙体の実際の値を取得できるようにする <see cref="IDynamicMetaObjectProvider"/> を作成します。</summary>
		/// <param name="typeLibGuid">タイプライブラリを識別する GUID (グローバル一意識別子) を指定します。</param>
		/// <returns>タイプライブラリに関する情報が格納された <see cref="ComTypeLibInfo"/> オブジェクト。</returns>
		[System.Runtime.Versioning.ResourceExposure(System.Runtime.Versioning.ResourceScope.Machine)]
		[System.Runtime.Versioning.ResourceConsumption(System.Runtime.Versioning.ResourceScope.Machine, System.Runtime.Versioning.ResourceScope.Machine)]
		public static ComTypeLibInfo CreateFromGuid(Guid typeLibGuid) { return new ComTypeLibInfo(GetFromTypeLib(UnsafeMethods.LoadRegTypeLib(ref typeLibGuid, -1, -1, 0))); } // majorVersion = -1, minorVersion = -1 は常に最新のタイプライブラリをロードする

		/// <summary>OLE オートメーション互換の RCW から ITypeLib オブジェクトとそれに含まれているコクラスおよび列挙体を読み取り、コクラスのインスタンス化と列挙体の実際の値を取得できるようにする <see cref="IDynamicMetaObjectProvider"/> を作成します。</summary>
		/// <param name="rcw">タイプライブラリを取得する OLE オートメーション互換の RCW を指定します。</param>
		/// <returns>タイプライブラリに関する情報が格納された <see cref="ComTypeLibInfo"/> オブジェクト。</returns>
		public static ComTypeLibInfo CreateFromObject(object rcw)
		{
			if (!Marshal.IsComObject(rcw))
				throw new ArgumentException("COM object である必要があります。");
			ComTypes.ITypeLib typeLib;
			int typeInfoIndex;
			ComRuntimeHelpers.GetITypeInfoFromIDispatch(rcw as IDispatch, true).GetContainingTypeLib(out typeLib, out typeInfoIndex);
			return new ComTypeLibInfo(GetFromTypeLib(typeLib));
		}

		internal static ComTypeLibDesc GetFromTypeLib(ComTypes.ITypeLib typeLib)
		{
			// check whether we have already loaded this type library
			ComTypes.TYPELIBATTR typeLibAttr = ComRuntimeHelpers.GetTypeAttrForTypeLib(typeLib);
			ComTypeLibDesc typeLibDesc;
			lock (_CachedTypeLibDesc)
			{
				if (_CachedTypeLibDesc.TryGetValue(typeLibAttr.guid, out typeLibDesc))
					return typeLibDesc;
			}
			typeLibDesc = new ComTypeLibDesc();
			typeLibDesc.Name = ComRuntimeHelpers.GetNameOfLib(typeLib);
			typeLibDesc._typeLibAttributes = typeLibAttr;
			int countTypes = typeLib.GetTypeInfoCount();
			for (int i = 0; i < countTypes; i++)
			{
				ComTypes.TYPEKIND typeKind;
				typeLib.GetTypeInfoType(i, out typeKind);
				ComTypes.ITypeInfo typeInfo;
				typeLib.GetTypeInfo(i, out typeInfo);
				if (typeKind == ComTypes.TYPEKIND.TKIND_COCLASS)
					typeLibDesc._classes.AddLast(new ComTypeClassDesc(typeInfo, typeLibDesc));
				else if (typeKind == ComTypes.TYPEKIND.TKIND_ENUM)
				{
					ComTypeEnumDesc enumDesc = new ComTypeEnumDesc(typeInfo, typeLibDesc);
					typeLibDesc._enums.Add(enumDesc.TypeName, enumDesc);
				}
				else if (typeKind == ComTypes.TYPEKIND.TKIND_ALIAS)
				{
					var typeAttr = ComRuntimeHelpers.GetTypeAttrForTypeInfo(typeInfo);
					if (typeAttr.tdescAlias.vt == (short)VarEnum.VT_USERDEFINED)
					{
						string aliasName, documentation;
						ComRuntimeHelpers.GetInfoFromType(typeInfo, out aliasName, out documentation);
						ComTypes.ITypeInfo referencedTypeInfo;
						typeInfo.GetRefTypeInfo(typeAttr.tdescAlias.lpValue.ToInt32(), out referencedTypeInfo);
						if (ComRuntimeHelpers.GetTypeAttrForTypeInfo(referencedTypeInfo).typekind == ComTypes.TYPEKIND.TKIND_ENUM)
							typeLibDesc._enums.Add(aliasName, new ComTypeEnumDesc(referencedTypeInfo, typeLibDesc));
					}
				}
			}
			// cached the typelib using the guid as the dictionary key
			lock (_CachedTypeLibDesc)
				_CachedTypeLibDesc.Add(typeLibAttr.guid, typeLibDesc);
			return typeLibDesc;
		}

		/// <summary>指定された名前を持つ型の記述をタイプライブラリから検索します。</summary>
		/// <param name="member">検索する名前を指定します。</param>
		/// <returns>見つかった型の記述。型が見つからなかった場合は <c>null</c> を返します。</returns>
		public ComTypeDesc GetTypeLibObjectDesc(string member)
		{
			ComTypeEnumDesc enumDesc;
			if (_enums != null && _enums.TryGetValue(member, out enumDesc))
				return enumDesc;
			return _classes.FirstOrDefault(x => member == x.TypeName);
		}

		internal string[] GetMemberNames() { return _classes.Select(x => x.TypeName).Concat(_enums.Select(x => x.Key)).ToArray(); }

		internal bool HasMember(string member) { return _classes.Any(x => member == x.TypeName) || _enums.ContainsKey(member); }

		/// <summary>このタイプライブラリのグローバル一意ライブラリ識別子を取得します。</summary>
		public Guid Guid { get { return _typeLibAttributes.guid; } }

		/// <summary>このタイプライブラリのメジャーバージョン番号を取得します。</summary>
		public short VersionMajor { get { return _typeLibAttributes.wMajorVerNum; } }

		/// <summary>このタイプライブラリのマイナーバージョン番号を取得します。</summary>
		public short VersionMinor { get { return _typeLibAttributes.wMinorVerNum; } }

		/// <summary>このタイプライブラリの名前を取得します。</summary>
		public string Name { get; private set; }

		internal ComTypeClassDesc GetCoClassForInterface(string itfName) { return _classes.FirstOrDefault(x => x.Implements(itfName, false)); }
	}
}