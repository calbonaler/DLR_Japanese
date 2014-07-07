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

using Microsoft.Contracts;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting
{
	/// <summary>
	/// 特定の <see cref="LanguageContext"/> に関連付けられたコンパイルされたコードのインスタンスを表します。
	/// コードは異なるスコープで複数回実行できます。
	/// このクラスに対するもう 1 つのホスティング API は <see cref="Microsoft.Scripting.Hosting.CompiledCode"/> です。
	/// </summary>
	public abstract class ScriptCode
	{
		/// <summary>翻訳単位を使用して、<see cref="Microsoft.Scripting.ScriptCode"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="sourceUnit">このクラスに関連づけられる<see cref="LanguageContext"/> を保持している <see cref="SourceUnit"/> オブジェクトを指定します。</param>
		protected ScriptCode(SourceUnit sourceUnit)
		{
			ContractUtils.RequiresNotNull(sourceUnit, "sourceUnit");
			SourceUnit = sourceUnit;
		}

		/// <summary>このクラスに関連付けられている <see cref="LanguageContext"/> を取得します。</summary>
		public LanguageContext LanguageContext { get { return SourceUnit.LanguageContext; } }

		/// <summary>このクラスのコードを保持している <see cref="SourceUnit"/> を取得します。</summary>
		public SourceUnit SourceUnit { get; private set; }

		/// <summary>新しい <see cref="Scope"/> オブジェクトを作成します。</summary>
		/// <returns>新しい <see cref="Scope"/> オブジェクト。</returns>
		public virtual Scope CreateScope() { return new Scope(); }

		/// <summary>新しいスコープでこのコードを実行します。</summary>
		/// <returns>このコードの実行結果。</returns>
		public virtual object Run() { return Run(CreateScope()); }

		/// <summary>指定されたスコープでこのコードを実行します。</summary>
		/// <param name="scope">このコードを実行するスコープを指定します。</param>
		/// <returns>このコードの実行結果。</returns>
		public abstract object Run(Scope scope);

		/// <summary>このオブジェクトの文字列表現を返します。</summary>
		/// <returns>文字列表現。</returns>
		[Confined]
		public override string ToString() { return string.Format("ScriptCode '{0}' from {1}", SourceUnit.Path, LanguageContext.GetType().Name); }
	}
}
