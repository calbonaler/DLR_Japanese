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
using System.Linq.Expressions;

namespace Microsoft.Scripting.Ast
{
	/// <summary>ノードのオペランドに対するアクセス権を指定します。</summary>
	[Flags]
	public enum ExpressionAccess
	{
		/// <summary>ノードはオペランドに対して読み取りも書き込みもできません。</summary>
		None = 0,
		/// <summary>ノードはオペランドの読み取りのみを行うことができます。</summary>
		Read = 1,
		/// <summary>ノードはオペランドの書き込みのみを行うことができます。</summary>
		Write = 2,
		/// <summary>ノードはオペランドに対して読み取りと書き込みの両方を行うことができます。</summary>
		ReadWrite = Read | Write,
	}

	/// <summary>標準の式ツリーに含まれないさまざまなノードを作成するファクトリ メソッドを公開します。</summary>
	public static partial class Utils
	{
		/// <summary>指定された <see cref="ExpressionType"/> のノードがオペランドを書き換えられるかどうかを判断します。</summary>
		/// <param name="type">判断する <see cref="ExpressionType"/> を指定します。</param>
		/// <returns><see cref="ExpressionType"/> のノードがオペランドを書き換えられる場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		/// <remarks>
		/// 他のノードにおいても変数、メンバ、配列要素の代入が発生する場合があります:
		/// MemberInit、NewArrayInit、参照渡し引数のある Call、参照渡し引数のある New、参照渡し引数のある Dynamic.
		/// </remarks>
		public static bool IsAssignment(this ExpressionType type) { return IsWriteOnlyAssignment(type) || IsReadWriteAssignment(type); }

		/// <summary>指定された <see cref="ExpressionType"/> のノードがオペランドを読み取らずに書き換えられるかどうかを判断します。</summary>
		/// <param name="type">判断する <see cref="ExpressionType"/> を指定します。</param>
		/// <returns><see cref="ExpressionType"/> のノードがオペランドを読み取らずに書き換えられる場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool IsWriteOnlyAssignment(this ExpressionType type) { return type == ExpressionType.Assign; }

		/// <summary>指定された <see cref="ExpressionType"/> のノードがオペランドを読み取った上で書き換えられるかどうかを判断します。</summary>
		/// <param name="type">判断する <see cref="ExpressionType"/> を指定します。</param>
		/// <returns><see cref="ExpressionType"/> のノードがオペランドを読み取った上で書き換えられる場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool IsReadWriteAssignment(this ExpressionType type)
		{
			switch (type)
			{
				// unary:
				case ExpressionType.PostDecrementAssign:
				case ExpressionType.PostIncrementAssign:
				case ExpressionType.PreDecrementAssign:
				case ExpressionType.PreIncrementAssign:
				// binary - compound:
				case ExpressionType.AddAssign:
				case ExpressionType.AddAssignChecked:
				case ExpressionType.AndAssign:
				case ExpressionType.DivideAssign:
				case ExpressionType.ExclusiveOrAssign:
				case ExpressionType.LeftShiftAssign:
				case ExpressionType.ModuloAssign:
				case ExpressionType.MultiplyAssign:
				case ExpressionType.MultiplyAssignChecked:
				case ExpressionType.OrAssign:
				case ExpressionType.PowerAssign:
				case ExpressionType.RightShiftAssign:
				case ExpressionType.SubtractAssign:
				case ExpressionType.SubtractAssignChecked:
					return true;
			}
			return false;
		}

		/// <summary>指定された <see cref="ExpressionType"/> のノードのオペランドに対する権限を取得します。</summary>
		/// <param name="type">アクセス権を取得する <see cref="ExpressionType"/> を指定します。</param>
		/// <returns>指定された <see cref="ExpressionType"/> のノードのオペランドに対する権限。</returns>
		public static ExpressionAccess GetLValueAccess(this ExpressionType type)
		{
			if (type.IsReadWriteAssignment())
				return ExpressionAccess.ReadWrite;
			if (type.IsWriteOnlyAssignment())
				return ExpressionAccess.Write;
			return ExpressionAccess.Read;
		}

		/// <summary>指定された <see cref="ExpressionType"/> のノードが左辺値として利用可能かどうかを判断します。</summary>
		/// <param name="type">判断する <see cref="ExpressionType"/> を指定します。</param>
		/// <returns>指定された <see cref="ExpressionType"/> のノードが左辺値として利用できれば <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool IsLValue(this ExpressionType type)
		{
			switch (type)
			{
				case ExpressionType.Index:
				case ExpressionType.MemberAccess:
				case ExpressionType.Parameter:
					return true;
			}
			return false;
		}
	}
}
