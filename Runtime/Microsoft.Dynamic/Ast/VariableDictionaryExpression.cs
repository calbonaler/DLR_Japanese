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
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Runtime;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Ast
{
	public partial class Utils
	{
		/// <summary>�w�肳�ꂽ���[�J���ϐ����i�[����f�B�N�V���i�����쐬���� <see cref="Expression"/> ��Ԃ��܂��B</summary>
		/// <param name="variables">�f�B�N�V���i���Ɋi�[���郍�[�J���ϐ����w�肵�܂��B</param>
		/// <returns>���[�J���ϐ����i�[����f�B�N�V���i�����쐬���� <see cref="Expression"/>�B</returns>
		public static Expression VariableDictionary(params ParameterExpression[] variables) { return VariableDictionary(variables.AsEnumerable()); }

		/// <summary>�w�肳�ꂽ���[�J���ϐ����i�[����f�B�N�V���i�����쐬���� <see cref="Expression"/> ��Ԃ��܂��B</summary>
		/// <param name="variables">�f�B�N�V���i���Ɋi�[���郍�[�J���ϐ����w�肵�܂��B</param>
		/// <returns>���[�J���ϐ����i�[����f�B�N�V���i�����쐬���� <see cref="Expression"/>�B</returns>
		public static Expression VariableDictionary(IEnumerable<ParameterExpression> variables)
		{
			return Expression.New(
				typeof(LocalsDictionary).GetConstructor(new[] { typeof(IRuntimeVariables), typeof(SymbolId[]) }),
				Expression.RuntimeVariables(variables),
				AstUtils.Constant(variables.Select(v => SymbolTable.StringToIdOrEmpty(v.Name)).ToArray())
			);
		}
	}
}
