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

using System.Dynamic;
using System.Linq.Expressions;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions
{
	/// <summary>追加の単項演算についてのバインディングを実行できる <see cref="UnaryOperationBinder"/> を表します。</summary>
	public abstract class ExtensionUnaryOperationBinder : UnaryOperationBinder
	{
		/// <summary>演算の種類を表す文字列を使用して、<see cref="Microsoft.Scripting.Actions.ExtensionUnaryOperationBinder"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="operation">演算の種類を表す文字列を指定します。</param>
		protected ExtensionUnaryOperationBinder(string operation) : base(ExpressionType.Extension)
		{
			ContractUtils.RequiresNotNull(operation, "operation");
			ExtensionOperation = operation;
		}

		/// <summary>演算の種類を表す文字列を取得します。</summary>
		public string ExtensionOperation { get; private set; }

		/// <summary>このオブジェクトについてのハッシュ値を計算します。</summary>
		/// <returns>オブジェクトのハッシュ値。</returns>
		public override int GetHashCode() { return base.GetHashCode() ^ ExtensionOperation.GetHashCode(); }

		/// <summary>このオブジェクトが指定されたオブジェクトと等しいかどうかを判断します。</summary>
		/// <param name="obj">比較するオブジェクトを指定します。</param>
		/// <returns>このオブジェクトが指定されたオブジェクトと等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public override bool Equals(object obj)
		{
			var euob = obj as ExtensionUnaryOperationBinder;
			return euob != null && base.Equals(obj) && ExtensionOperation == euob.ExtensionOperation;
		}
	}
}
