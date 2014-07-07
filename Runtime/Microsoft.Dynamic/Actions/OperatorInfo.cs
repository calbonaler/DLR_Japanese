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

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.Scripting.Actions
{
	/// <summary>DLR <see cref="ExpressionType"/> から関連付けられた .NET メソッドに対するマッピングを提供します。</summary>
	internal class OperatorInfo
	{
		static Dictionary<ExpressionType, OperatorInfo> _infos = MakeOperatorTable(); // table of ExpressionType, names, and alt names for looking up methods.

		OperatorInfo(ExpressionType op, string name, string altName)
		{
			Operator = op;
			Name = name;
			AlternateName = altName;
		}

		/// <summary>指定された演算子に関連付けられた <see cref="OperatorInfo"/> または <c>null</c> を返します。</summary>
		/// <param name="op">関連付けられた <see cref="OperatorInfo"/> を返す演算子を指定します。</param>
		/// <returns>演算子に関連付けられた <see cref="OperatorInfo"/>。対応する <see cref="OperatorInfo"/> が存在しない場合は <c>null</c> を返します。</returns>
		public static OperatorInfo GetOperatorInfo(ExpressionType op)
		{
			OperatorInfo res;
			_infos.TryGetValue(op, out res);
			return res;
		}

		/// <summary>指定された演算子の名前から対応する <see cref="OperatorInfo"/> を取得します。</summary>
		/// <param name="name">対応する <see cref="OperatorInfo"/> を検索する演算子の名前または代替名を指定します。</param>
		/// <returns>名前に関連付けられた <see cref="OperatorInfo"/>。対応する <see cref="OperatorInfo"/> が存在しない場合は <c>null</c> を返します。</returns>
		public static OperatorInfo GetOperatorInfo(string name) { return _infos.Values.FirstOrDefault(x => x.Name == name || x.AlternateName == name); }

		/// <summary><see cref="OperatorInfo"/> が情報を提供する演算子を取得します。</summary>
		public ExpressionType Operator { get; private set; }

		/// <summary>
		/// この演算子に関連付けられたプライマリメソッド名を取得します。
		/// メソッド名は通常 op_Operator のような形式をとります。(例: op_Addition)
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// この演算子に関連付けられたセカンダリメソッド名を取得します。
		/// メソッド名は通常パスカルケースの標準的な .NET メソッド名になります。(例: Add)
		/// </summary>
		public string AlternateName { get; private set; }

		static Dictionary<ExpressionType, OperatorInfo> MakeOperatorTable()
		{
			var res = new Dictionary<ExpressionType, OperatorInfo>();
			// alternate names from: http://msdn2.microsoft.com/en-us/library/2sk3x8a7(vs.71).aspx
			//   different in:
			//   comparisons all support alternative names, Xor is "ExclusiveOr" not "Xor"
			// unary ExpressionType as defined in Partition I Architecture 9.3.1:
			res[ExpressionType.Decrement] = new OperatorInfo(ExpressionType.Decrement, "op_Decrement", "Decrement");          // --
			res[ExpressionType.Increment] = new OperatorInfo(ExpressionType.Increment, "op_Increment", "Increment");          // ++
			res[ExpressionType.Negate] = new OperatorInfo(ExpressionType.Negate, "op_UnaryNegation", "Negate");             // - (unary)
			res[ExpressionType.UnaryPlus] = new OperatorInfo(ExpressionType.UnaryPlus, "op_UnaryPlus", "Plus");               // + (unary)
			res[ExpressionType.Not] = new OperatorInfo(ExpressionType.Not, "op_LogicalNot", null);                 // !
			res[ExpressionType.IsTrue] = new OperatorInfo(ExpressionType.IsTrue, "op_True", null);                 // not defined
			res[ExpressionType.IsFalse] = new OperatorInfo(ExpressionType.IsFalse, "op_False", null);                 // not defined
			res[ExpressionType.OnesComplement] = new OperatorInfo(ExpressionType.OnesComplement, "op_OnesComplement", "OnesComplement");     // ~
			// binary ExpressionType as defined in Partition I Architecture 9.3.2:
			res[ExpressionType.Add] = new OperatorInfo(ExpressionType.Add, "op_Addition", "Add");                // +
			res[ExpressionType.Subtract] = new OperatorInfo(ExpressionType.Subtract, "op_Subtraction", "Subtract");           // -
			res[ExpressionType.Multiply] = new OperatorInfo(ExpressionType.Multiply, "op_Multiply", "Multiply");           // *
			res[ExpressionType.Divide] = new OperatorInfo(ExpressionType.Divide, "op_Division", "Divide");             // /
			res[ExpressionType.Modulo] = new OperatorInfo(ExpressionType.Modulo, "op_Modulus", "Mod");                // %
			res[ExpressionType.ExclusiveOr] = new OperatorInfo(ExpressionType.ExclusiveOr, "op_ExclusiveOr", "ExclusiveOr");        // ^
			res[ExpressionType.And] = new OperatorInfo(ExpressionType.And, "op_BitwiseAnd", "BitwiseAnd");         // &
			res[ExpressionType.Or] = new OperatorInfo(ExpressionType.Or, "op_BitwiseOr", "BitwiseOr");          // |
			res[ExpressionType.And] = new OperatorInfo(ExpressionType.And, "op_LogicalAnd", "And");                // &&
			res[ExpressionType.Or] = new OperatorInfo(ExpressionType.Or, "op_LogicalOr", "Or");                 // ||
			res[ExpressionType.LeftShift] = new OperatorInfo(ExpressionType.LeftShift, "op_LeftShift", "LeftShift");          // <<
			res[ExpressionType.RightShift] = new OperatorInfo(ExpressionType.RightShift, "op_RightShift", "RightShift");         // >>
			res[ExpressionType.Equal] = new OperatorInfo(ExpressionType.Equal, "op_Equality", "Equals");             // ==   
			res[ExpressionType.GreaterThan] = new OperatorInfo(ExpressionType.GreaterThan, "op_GreaterThan", "GreaterThan");        // >
			res[ExpressionType.LessThan] = new OperatorInfo(ExpressionType.LessThan, "op_LessThan", "LessThan");           // <
			res[ExpressionType.NotEqual] = new OperatorInfo(ExpressionType.NotEqual, "op_Inequality", "NotEquals");          // != 
			res[ExpressionType.GreaterThanOrEqual] = new OperatorInfo(ExpressionType.GreaterThanOrEqual, "op_GreaterThanOrEqual", "GreaterThanOrEqual"); // >=
			res[ExpressionType.LessThanOrEqual] = new OperatorInfo(ExpressionType.LessThanOrEqual, "op_LessThanOrEqual", "LessThanOrEqual");    // <=
			res[ExpressionType.MultiplyAssign] = new OperatorInfo(ExpressionType.MultiplyAssign, "op_MultiplicationAssignment", "InPlaceMultiply");    // *=
			res[ExpressionType.SubtractAssign] = new OperatorInfo(ExpressionType.SubtractAssign, "op_SubtractionAssignment", "InPlaceSubtract");    // -=
			res[ExpressionType.ExclusiveOrAssign] = new OperatorInfo(ExpressionType.ExclusiveOrAssign, "op_ExclusiveOrAssignment", "InPlaceExclusiveOr"); // ^=
			res[ExpressionType.LeftShiftAssign] = new OperatorInfo(ExpressionType.LeftShiftAssign, "op_LeftShiftAssignment", "InPlaceLeftShift");   // <<=
			res[ExpressionType.RightShiftAssign] = new OperatorInfo(ExpressionType.RightShiftAssign, "op_RightShiftAssignment", "InPlaceRightShift");  // >>=
			res[ExpressionType.ModuloAssign] = new OperatorInfo(ExpressionType.ModuloAssign, "op_ModulusAssignment", "InPlaceMod");         // %=
			res[ExpressionType.AddAssign] = new OperatorInfo(ExpressionType.AddAssign, "op_AdditionAssignment", "InPlaceAdd");         // += 
			res[ExpressionType.AndAssign] = new OperatorInfo(ExpressionType.AndAssign, "op_BitwiseAndAssignment", "InPlaceBitwiseAnd");  // &=
			res[ExpressionType.OrAssign] = new OperatorInfo(ExpressionType.OrAssign, "op_BitwiseOrAssignment", "InPlaceBitwiseOr");   // |=
			res[ExpressionType.DivideAssign] = new OperatorInfo(ExpressionType.DivideAssign, "op_DivisionAssignment", "InPlaceDivide");      // /=
			return res;
		}
	}
}
