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

using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Ast
{
	public static partial class Utils
	{
		/// <summary>
		/// �w�肵���������g�p���Ďw�肳�ꂽ�R���X�g���N�^���Ăяo���܂��B
		/// ���̃��\�b�h�͕K�v�ł���� <see cref="Convert"/> ���g�p����ϊ��������ɑ΂��čs���܂��B
		/// </summary>
		/// <param name="constructor">�Ăяo���R���X�g���N�^���w�肵�܂��B</param>
		/// <param name="arguments">�R���X�g���N�^�̌Ăяo���ɕK�v�Ȉ������w�肵�܂��B</param>
		/// <returns>�R���X�g���N�^�̌Ăяo����\�� <see cref="NewExpression"/>�B</returns>
		public static NewExpression SimpleNewHelper(ConstructorInfo constructor, params Expression[] arguments)
		{
			ContractUtils.RequiresNotNull(constructor, "constructor");
			ContractUtils.RequiresNotNullItems(arguments, "arguments");
			ParameterInfo[] parameters = constructor.GetParameters();
			ContractUtils.Requires(arguments.Length == parameters.Length, "arguments", "�������̐�������������܂���B");
			return Expression.New(constructor, ArgumentConvertHelper(arguments, parameters));
		}
	}
}
