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

using System.Diagnostics;
using System.Linq.Expressions;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Generation
{
	/// <summary>式ツリーに関する定数比較を行うメソッドを公開します。</summary>
	public static class ConstantCheck
	{
		/// <summary>式が指定された値の定数であるかどうかを調べます。</summary>
		/// <param name="expression">調べる式を指定します。</param>
		/// <param name="value">比較する定数値を指定します。</param>
		/// <returns>指定された式が指定された値の定数である場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool Check(Expression expression, object value)
		{
			ContractUtils.RequiresNotNull(expression, "expression");
			return IsConstant(expression, value);
		}

		/// <summary>式が指定された値の定数であるかどうかを調べます。</summary>
		/// <param name="e">調べる式を指定します。</param>
		/// <param name="value">比較する定数値を指定します。</param>
		/// <returns>指定された式が指定された値の定数である場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
		internal static bool IsConstant(Expression e, object value)
		{
			switch (e.NodeType)
			{
				case ExpressionType.AndAlso:
					return CheckAndAlso((BinaryExpression)e, value);
				case ExpressionType.OrElse:
					return CheckOrElse((BinaryExpression)e, value);
				case ExpressionType.Constant:
					return CheckConstant((ConstantExpression)e, value);
				case ExpressionType.TypeIs:
					return CheckTypeIs((TypeBinaryExpression)e, value);
				default:
					return false;
			}
		}

		//CONFORMING
		/// <summary>式が定数 <c>null</c> であるかどうかを調べます。</summary>
		/// <param name="e">調べる式を指定します。</param>
		/// <returns>指定された式が定数値 <c>null</c> である場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		internal static bool IsNull(Expression e) { return IsConstant(e, null); }

		static bool CheckAndAlso(BinaryExpression node, object value)
		{
			Debug.Assert(node.NodeType == ExpressionType.AndAlso);
			// TODO: 変換を通して伝搬させることができますが、価値はないでしょう
			if (node.Method != null || node.Conversion != null)
				return false;
			if (value is bool)
			{
				if ((bool)value)
					return IsConstant(node.Left, true) && IsConstant(node.Right, true);
				else
					return IsConstant(node.Left, false); // 左辺が定数でなければ評価される必要がある
			}
			return false;
		}

		static bool CheckOrElse(BinaryExpression node, object value)
		{
			Debug.Assert(node.NodeType == ExpressionType.OrElse);
			if (node.Method != null)
				return false;
			if (value is bool)
			{
				if ((bool)value)
					return IsConstant(node.Left, true);
				else
					return IsConstant(node.Left, false) && IsConstant(node.Right, false);
			}
			return false;
		}

		static bool CheckConstant(ConstantExpression node, object value) { return Equals(node.Value, value); }

		static bool CheckTypeIs(TypeBinaryExpression node, object value)
		{
			// allow constant TypeIs expressions to be optimized away
			return Equals(value, true) && node.TypeOperand.IsAssignableFrom(node.Expression.Type);
		}
	}
}
