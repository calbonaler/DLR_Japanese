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

namespace Microsoft.Scripting.Runtime
{
	/// <summary>モジュールの内容が変更された場合に発生するイベントのデータを表します。</summary>
	public class ModuleChangeEventArgs : EventArgs
	{
		/// <summary>指定された名前および型を使用して、<see cref="Microsoft.Scripting.Runtime.ModuleChangeEventArgs"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="name">変更が発生したシンボルの名前を指定します。</param>
		/// <param name="changeType">モジュールに発生した変更を示す <see cref="ModuleChangeType"/> を指定します。</param>
		public ModuleChangeEventArgs(string name, ModuleChangeType changeType)
		{
			Name = name;
			ChangeType = changeType;
		}

		/// <summary>指定された名前、型および変更された値を使用して、<see cref="Microsoft.Scripting.Runtime.ModuleChangeEventArgs"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="name">変更が発生したシンボルの名前を指定します。</param>
		/// <param name="changeType">モジュールに発生した変更を示す <see cref="ModuleChangeType"/> を指定します。</param>
		/// <param name="value">シンボルに新しく設定された値を指定します。</param>
		public ModuleChangeEventArgs(string name, ModuleChangeType changeType, object value)
		{
			Name = name;
			ChangeType = changeType;
			Value = value;
		}

		/// <summary>変更が発生したシンボルの名前を指定します。</summary>
		public string Name { get; private set; }

		/// <summary>シンボルがどのように変更されたかを示す <see cref="ModuleChangeType"/> を取得します。</summary>
		public ModuleChangeType ChangeType { get; private set; }

		/// <summary>シンボルに新しく設定された値を取得します。</summary>
		public object Value { get; private set; }
	}
}
