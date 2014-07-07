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
	/// <summary>COM のタイプライブラリに関する情報を公開します。</summary>
	public sealed class ComTypeLibInfo : IDynamicMetaObjectProvider
	{
		internal ComTypeLibInfo(ComTypeLibDesc typeLibDesc) { TypeLibDesc = typeLibDesc; }

		/// <summary>タイプライブラリの名前を取得します。</summary>
		public string Name { get { return TypeLibDesc.Name; } }

		/// <summary>タイプライブラリのグローバル一意ライブラリ識別子を取得します。</summary>
		public Guid Guid { get { return TypeLibDesc.Guid; } }

		/// <summary>タイプライブラリのメジャーバージョン番号を取得します。</summary>
		public short VersionMajor { get { return TypeLibDesc.VersionMajor; } }

		/// <summary>タイプライブラリのマイナーバージョン番号を取得します。</summary>
		public short VersionMinor { get { return TypeLibDesc.VersionMinor; } }

		/// <summary>タイプライブラリを表す <see cref="ComTypeLibDesc"/> を取得します。</summary>
		public ComTypeLibDesc TypeLibDesc { get; private set; }

		internal string[] GetMemberNames() { return new string[] { this.Name, "Guid", "Name", "VersionMajor", "VersionMinor" }; }

		DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) { return new TypeLibInfoMetaObject(parameter, this); }
	}
}