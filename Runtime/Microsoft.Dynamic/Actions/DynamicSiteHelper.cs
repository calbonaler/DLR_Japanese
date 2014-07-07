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
using System.Runtime.CompilerServices;

namespace Microsoft.Scripting.Actions
{
	/// <summary>動的サイトに対するヘルパーメソッドを提供します。</summary>
	public static class DynamicSiteHelpers
	{
		/// <summary>メソッドがスタックフレームに表示されるべきではないかどうかを判断します。</summary>
		/// <param name="mb">判断するメソッドを指定します。</param>
		/// <returns>指定されたメソッドがスタックフレームに表示されるべきではない場合 <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool IsInvisibleDlrStackFrame(MethodBase mb)
		{
			// このメソッド名はデリゲート型シグネチャに対して作成された動的メソッドに対して使用される。
			// Microsoft.Scripting 名前空間のメソッドをフィルタする。
			// DLR 規則に対して生成されるか、DLR 規則で使用されるすべてのメソッドをフィルタする。
			return mb.Name == "_Scripting_" ||
				mb.DeclaringType != null && mb.DeclaringType.Namespace != null && mb.DeclaringType.Namespace.StartsWith("Microsoft.Scripting", StringComparison.Ordinal) ||
				CallSiteHelpers.IsInternalFrame(mb);
		}
	}
}
