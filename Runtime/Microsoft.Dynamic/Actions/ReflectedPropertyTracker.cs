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
using System.Reflection;
using Microsoft.Contracts;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Actions
{
	/// <summary>型に定義されている実際のプロパティを表します。</summary>
	public class ReflectedPropertyTracker : PropertyTracker
	{
		/// <summary>基になる <see cref="PropertyInfo"/> を使用して、<see cref="Microsoft.Scripting.Actions.ReflectedPropertyTracker"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="property">基になるプロパティを表す <see cref="PropertyInfo"/> を指定します。</param>
		public ReflectedPropertyTracker(PropertyInfo property) { Property = property; }

		/// <summary>メンバの名前を取得します。</summary>
		public override string Name { get { return Property.Name; } }

		/// <summary>メンバを論理的に宣言する型を取得します。</summary>
		public override Type DeclaringType { get { return Property.DeclaringType; } }

		/// <summary>このプロパティが静的であるかどうかを示す値を取得します。</summary>
		public override bool IsStatic { get { return (GetGetMethod(true) ?? GetSetMethod(true)).IsStatic; } }

		/// <summary>このプロパティの型を取得します。</summary>
		public override Type PropertyType { get { return Property.PropertyType; } }

		/// <summary>このプロパティのパブリックまたは非パブリックな get アクセサーを返します。</summary>
		/// <param name="privateMembers">非パブリックな get アクセサーを返すかどうかを示します。非パブリック アクセサーを返す場合は <c>true</c>。それ以外の場合は <c>false</c>。</param>
		/// <returns>
		/// <paramref name="privateMembers"/> が <c>true</c> の場合は、このプロパティの get アクセサーを表す <see cref="MethodInfo"/> オブジェクト。
		/// <paramref name="privateMembers"/> が <c>false</c> で get アクセサーが非パブリックの場合、または <paramref name="privateMembers"/> が <c>true</c> でも get アクセサーがない場合は、<c>null</c> を返します。
		/// </returns>
		public override MethodInfo GetGetMethod(bool privateMembers) { return Property.GetGetMethod(privateMembers); }

		/// <summary>このプロパティのパブリックまたは非パブリックな set アクセサーを返します。</summary>
		/// <param name="privateMembers">非パブリックな set アクセサーを返すかどうかを示します。非パブリック アクセサーを返す場合は <c>true</c>。それ以外の場合は <c>false</c>。</param>
		/// <returns>
		/// <paramref name="privateMembers"/> が <c>true</c> の場合は、このプロパティの set アクセサーを表す <see cref="MethodInfo"/> オブジェクト。
		/// <paramref name="privateMembers"/> が <c>false</c> で set アクセサーが非パブリックの場合、または <paramref name="privateMembers"/> が <c>true</c> でも set アクセサーがない場合は、<c>null</c> を返します。
		/// </returns>
		public override MethodInfo GetSetMethod(bool privateMembers) { return Property.GetSetMethod(privateMembers); }

		/// <summary>このプロパティのパブリックまたは非パブリックな delete アクセサーを返します。</summary>
		/// <param name="privateMembers">非パブリックな delete アクセサーを返すかどうかを示します。非パブリック アクセサーを返す場合は <c>true</c>。それ以外の場合は <c>false</c>。</param>
		/// <returns>
		/// <paramref name="privateMembers"/> が <c>true</c> の場合は、このプロパティの delete アクセサーを表す <see cref="MethodInfo"/> オブジェクト。
		/// <paramref name="privateMembers"/> が <c>false</c> で delete アクセサーが非パブリックの場合、または <paramref name="privateMembers"/> が <c>true</c> でも delete アクセサーがない場合は、<c>null</c> を返します。
		/// </returns>
		public override MethodInfo GetDeleteMethod(bool privateMembers)
		{
			var res = Property.DeclaringType.GetMethod("Delete" + Property.Name, (privateMembers ? BindingFlags.NonPublic : 0) | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);
			return res != null && res.IsSpecialName && res.IsDefined(typeof(PropertyMethodAttribute), true) ? res : null;
		}

		/// <summary>派生クラスでオーバーライドされた場合に、プロパティのすべてのインデックス パラメータの配列を返します。</summary>
		/// <returns>インデックスのパラメーターを格納している <see cref="ParameterInfo"/> 型の配列。プロパティがインデックス付けされていない場合、配列の要素はゼロ (0) です。</returns>
		public override ParameterInfo[] GetIndexParameters() { return Property.GetIndexParameters(); }

		/// <summary>基になるプロパティを表す <see cref="PropertyInfo"/> を取得します。</summary>
		public PropertyInfo Property { get; private set; }

		/// <summary>このオブジェクトの文字列表現を取得します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		[Confined]
		public override string ToString() { return Property.ToString(); }
	}
}
