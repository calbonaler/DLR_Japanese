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

namespace Microsoft.Scripting.Hosting.Shell
{
	/// <summary>
	/// 単一の対話コマンドをディスパッチするために使用されます。
	/// このインターフェイスはコマンドが実行されるスレッド、コマンドに許可される実行時間などを制御するために使用されます。
	/// </summary>
	public interface ICommandDispatcher
	{
		/// <summary>指定されたコードを指定されたスコープで実行されるようにディスパッチして、結果を返します。</summary>
		/// <param name="compiledCode">実行するコードを指定します。</param>
		/// <param name="scope">コードを実行するスコープを指定します。</param>
		/// <returns>コードが実行された結果を返します。</returns>
		object Execute(CompiledCode compiledCode, ScriptScope scope);
	}
}
