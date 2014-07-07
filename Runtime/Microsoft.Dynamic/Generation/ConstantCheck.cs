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
	/// <summary>���c���[�Ɋւ���萔��r���s�����\�b�h�����J���܂��B</summary>
	public static class ConstantCheck
	{
		/// <summary>�����w�肳�ꂽ�l�̒萔�ł��邩�ǂ����𒲂ׂ܂��B</summary>
		/// <param name="expression">���ׂ鎮���w�肵�܂��B</param>
		/// <param name="value">��r����萔�l���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�����w�肳�ꂽ�l�̒萔�ł���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool Check(Expression expression, object value)
		{
			ContractUtils.RequiresNotNull(expression, "expression");
			return IsConstant(expression, value);
		}

		/// <summary>�����w�肳�ꂽ�l�̒萔�ł��邩�ǂ����𒲂ׂ܂��B</summary>
		/// <param name="e">���ׂ鎮���w�肵�܂��B</param>
		/// <param name="value">��r����萔�l���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�����w�肳�ꂽ�l�̒萔�ł���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
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
		/// <summary>�����萔 <c>null</c> �ł��邩�ǂ����𒲂ׂ܂��B</summary>
		/// <param name="e">���ׂ鎮���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�����萔�l <c>null</c> �ł���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		internal static bool IsNull(Expression e) { return IsConstant(e, null); }

		static bool CheckAndAlso(BinaryExpression node, object value)
		{
			Debug.Assert(node.NodeType == ExpressionType.AndAlso);
			// TODO: �ϊ���ʂ��ē`�������邱�Ƃ��ł��܂����A���l�͂Ȃ��ł��傤
			if (node.Method != null || node.Conversion != null)
				return false;
			if (value is bool)
			{
				if ((bool)value)
					return IsConstant(node.Left, true) && IsConstant(node.Right, true);
				else
					return IsConstant(node.Left, false); // ���ӂ��萔�łȂ���Ε]�������K�v������
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
