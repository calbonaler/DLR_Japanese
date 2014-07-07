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
using System.Linq;
using System.Reflection;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Actions
{
	/// <summary>拡張プロパティを表します。</summary>
	public class ExtensionPropertyTracker : PropertyTracker
	{
		string _name;
		Type _declaringType;
		MethodInfo _getter, _setter, _deleter;

		/// <summary>名前、get アクセサ、set アクセサ、delete アクセサおよび宣言する型を使用して、<see cref="Microsoft.Scripting.Actions.ExtensionPropertyTracker"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="name">拡張プロパティの名前を指定します。</param>
		/// <param name="getter">get アクセサを表す <see cref="MethodInfo"/> を指定します。</param>
		/// <param name="setter">set アクセサを表す <see cref="MethodInfo"/> を指定します。</param>
		/// <param name="deleter">delete アクセサを表す <see cref="MethodInfo"/> を指定します。</param>
		/// <param name="declaringType">拡張プロパティを宣言する型を指定します。この値は拡張プロパティが拡張する型と等しくなります。</param>
		public ExtensionPropertyTracker(string name, MethodInfo getter, MethodInfo setter, MethodInfo deleter, Type declaringType)
		{
			_name = name;
			_getter = getter;
			_setter = setter;
			_deleter = deleter;
			_declaringType = declaringType;
		}

		/// <summary>メンバの名前を取得します。</summary>
		public override string Name { get { return _name; } }

		/// <summary>メンバを論理的に宣言する型を取得します。</summary>
		public override Type DeclaringType { get { return _declaringType; } }

		/// <summary>このプロパティが静的であるかどうかを示す値を取得します。</summary>
		public override bool IsStatic { get { return (GetGetMethod(true) ?? GetSetMethod(true)).IsDefined(typeof(StaticExtensionMethodAttribute), false); } }

		/// <summary>このプロパティのパブリックまたは非パブリックな get アクセサーを返します。</summary>
		/// <param name="privateMembers">非パブリックな get アクセサーを返すかどうかを示します。非パブリック アクセサーを返す場合は <c>true</c>。それ以外の場合は <c>false</c>。</param>
		/// <returns>
		/// <paramref name="privateMembers"/> が <c>true</c> の場合は、このプロパティの get アクセサーを表す <see cref="MethodInfo"/> オブジェクト。
		/// <paramref name="privateMembers"/> が <c>false</c> で get アクセサーが非パブリックの場合、または <paramref name="privateMembers"/> が <c>true</c> でも get アクセサーがない場合は、<c>null</c> を返します。
		/// </returns>
		public override MethodInfo GetGetMethod(bool privateMembers) { return privateMembers || _getter == null || !_getter.IsPrivate ? _getter : null; }

		/// <summary>このプロパティのパブリックまたは非パブリックな set アクセサーを返します。</summary>
		/// <param name="privateMembers">非パブリックな set アクセサーを返すかどうかを示します。非パブリック アクセサーを返す場合は <c>true</c>。それ以外の場合は <c>false</c>。</param>
		/// <returns>
		/// <paramref name="privateMembers"/> が <c>true</c> の場合は、このプロパティの set アクセサーを表す <see cref="MethodInfo"/> オブジェクト。
		/// <paramref name="privateMembers"/> が <c>false</c> で set アクセサーが非パブリックの場合、または <paramref name="privateMembers"/> が <c>true</c> でも set アクセサーがない場合は、<c>null</c> を返します。
		/// </returns>
		public override MethodInfo GetSetMethod(bool privateMembers) { return privateMembers || _setter == null || !_setter.IsPrivate ? _setter : null; }

		/// <summary>このプロパティのパブリックまたは非パブリックな delete アクセサーを返します。</summary>
		/// <param name="privateMembers">非パブリックな delete アクセサーを返すかどうかを示します。非パブリック アクセサーを返す場合は <c>true</c>。それ以外の場合は <c>false</c>。</param>
		/// <returns>
		/// <paramref name="privateMembers"/> が <c>true</c> の場合は、このプロパティの delete アクセサーを表す <see cref="MethodInfo"/> オブジェクト。
		/// <paramref name="privateMembers"/> が <c>false</c> で delete アクセサーが非パブリックの場合、または <paramref name="privateMembers"/> が <c>true</c> でも delete アクセサーがない場合は、<c>null</c> を返します。
		/// </returns>
		public override MethodInfo GetDeleteMethod(bool privateMembers) { return privateMembers || _deleter == null || !_deleter.IsPrivate ? _deleter : null; }

		/// <summary>プロパティのすべてのインデックス パラメータの配列を返します。</summary>
		/// <returns>インデックスのパラメーターを格納している <see cref="ParameterInfo"/> 型の配列。プロパティがインデックス付けされていない場合、配列の要素はゼロ (0) です。</returns>
		public override ParameterInfo[] GetIndexParameters() { return new ParameterInfo[0]; }

		/// <summary>このプロパティの型を取得します。</summary>
		public override Type PropertyType { get { return _getter != null ? _getter.ReturnType : _setter.GetParameters().Last().ParameterType; } }
	}
}
