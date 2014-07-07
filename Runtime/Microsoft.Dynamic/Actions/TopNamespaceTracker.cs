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
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions
{
	/// <summary>ロードされたすべてのアセンブリや組み込みモジュールなどの追加の情報を含む最上位名前空間を表します。</summary>
	public class TopNamespaceTracker : NamespaceTracker
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")] // TODO: fix
		int _lastDiscovery = 0;
		internal readonly object HierarchyLock = new object();
		static Dictionary<Guid, Type> _comTypeCache = new Dictionary<Guid, Type>();

		/// <summary>指定された <see cref="ScriptDomainManager"/> を使用して、<see cref="Microsoft.Scripting.Actions.TopNamespaceTracker"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="manager">この最上位名前空間に関連付けられる <see cref="ScriptDomainManager"/> を指定します。</param>
		public TopNamespaceTracker(ScriptDomainManager manager) : base(null)
		{
			ContractUtils.RequiresNotNull(manager, "manager");
			SetTopPackage(this);
			DomainManager = manager;
		}

		/// <summary>指定された名前空間に関連付けられたパッケージを取得し、関連付けられたモジュールをパッケージがインポートされたとしてマークします。</summary>
		/// <param name="name">インポートする名前空間の名前を指定します。</param>
		/// <returns>指定された名前空間を表す <see cref="NamespaceTracker"/>。</returns>
		public NamespaceTracker TryGetPackage(string name) { return TryGetPackage(SymbolTable.StringToId(name)); }

		/// <summary>指定された名前空間に関連付けられたパッケージを取得し、関連付けられたモジュールをパッケージがインポートされたとしてマークします。</summary>
		/// <param name="name">インポートする名前空間の名前を <see cref="SymbolId"/> として指定します。</param>
		/// <returns>指定された名前空間を表す <see cref="NamespaceTracker"/>。</returns>
		public NamespaceTracker TryGetPackage(SymbolId name) { return TryGetPackageAny(name) as NamespaceTracker; }

		/// <summary>
		/// 指定された名前のパッケージを取得し、関連付けられたモジュールをパッケージがインポートされたとしてマークします。
		/// 取得されるパッケージは型である可能性もあります。
		/// </summary>
		/// <param name="name">インポートするパッケージの名前を指定します。</param>
		/// <returns>指定されたパッケージ。</returns>
		public MemberTracker TryGetPackageAny(string name) { return TryGetPackageAny(SymbolTable.StringToId(name)); }

		/// <summary>
		/// 指定された名前のパッケージを取得し、関連付けられたモジュールをパッケージがインポートされたとしてマークします。
		/// 取得されるパッケージは型である可能性もあります。
		/// </summary>
		/// <param name="name">インポートするパッケージの名前を <see cref="SymbolId"/> として指定します。</param>
		/// <returns>指定されたパッケージ。</returns>
		public MemberTracker TryGetPackageAny(SymbolId name)
		{
			MemberTracker ret;
			if (TryGetValue(name, out ret))
				return ret;
			return null;
		}

		/// <summary>指定された名前のパッケージを取得します。型のロードなどは行いません。</summary>
		/// <param name="name">取得するパッケージの名前を <see cref="SymbolId"/> として指定します。</param>
		/// <returns>取得されたパッケージ。</returns>
		public MemberTracker TryGetPackageLazy(SymbolId name)
		{
			lock (HierarchyLock)
			{
				MemberTracker ret;
				if (_dict.TryGetValue(SymbolTable.IdToString(name), out ret))
					return ret;
				return null;
			}
		}

		/// <summary>指定されたアセンブリをロードします。</summary>
		/// <param name="assem">ロードするアセンブリを指定します。</param>
		/// <returns>初めてアセンブリがロードされた場合は <c>true</c>。以前にロードされていた場合は <c>false</c>。</returns>
		public bool LoadAssembly(Assembly assem)
		{
			ContractUtils.RequiresNotNull(assem, "assem");
			lock (HierarchyLock)
			{
				if (_packageAssemblies.Contains(assem))
					return false; // アセンブリは既にロードされていたのでもう何もすることはない。
				_packageAssemblies.Add(assem);
				UpdateSubtreeIds();
				PublishComTypes(assem);
			}
			return true;
		}

		/// <summary>アセンブリをスキャンして、COM オブジェクトの型定義を COM インターフェイスの GUID と関連付けます。</summary>
		/// <param name="interopAssembly">スキャンする (相互運用) アセンブリを指定します。</param>
		/// <remarks>読み込んだすべてのアセンブリのスキャンは不経済なため、将来、COM 型のスキャンをトリガするより明示的なユーザーバインダーを考案する可能性があります。</remarks>
		public static void PublishComTypes(Assembly interopAssembly)
		{
			lock (_comTypeCache)
			{
				// 矛盾のないビューを発行できるように操作全体をロックします。
				foreach (var type in LoadTypesFromAssembly(interopAssembly, false).Where(x => x.IsImport && x.IsInterface))
				{
					Type existing;
					if (!_comTypeCache.TryGetValue(type.GUID, out existing))
						_comTypeCache[type.GUID] = type;
					else if (!existing.IsDefined(typeof(CoClassAttribute), false))
						// CoClassAttributeのある型を優先します。例:
						// MS.Office.Interop.Excel.Worksheet vs MS.Office.Interop.Excel._Worksheet
						// Worksheet は型がサポートするすべてのインターフェイスを定義していて、CoClassAttribute があります。
						// _Worksheet はワークシートのインターフェイスしかありません。
						// しかし両方とも同じ GUID をもっています。
						_comTypeCache[type.GUID] = type;
				}
			}
		}

		/// <summary>最上位名前空間に関連付けられたすべてのアセンブリ内の型を適切な名前空間に配置します。</summary>
		protected override void LoadNamespaces()
		{
			lock (HierarchyLock)
			{
				for (int i = _lastDiscovery; i < _packageAssemblies.Count; i++)
					DiscoverAllTypes(_packageAssemblies[i]);
				_lastDiscovery = _packageAssemblies.Count;
			}
		}

		/// <summary>最上位名前空間に関連付けられているドメイン管理を行う <see cref="ScriptDomainManager"/> を取得します。</summary>
		public ScriptDomainManager DomainManager { get; private set; }
	}
}
