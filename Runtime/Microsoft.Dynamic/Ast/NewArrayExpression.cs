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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Ast
{
	public static partial class Utils
	{
		/// <summary>
		/// �v�f�̃��X�g����� 1 �����z��̏�������\���m�[�h���쐬���܂��B
		/// ���̃��\�b�h�͏��������X�g�̂��ꂼ��̎��ɑ΂��ĕK�v�ł���� <see cref="Convert"/> �܂��� <see cref="Expression.Quote"/> ���g�p�����ϊ����s���܂��B
		/// </summary>
		/// <param name="type">�쐬���� 1 �����z��̗v�f�̌^���w�肵�܂��B</param>
		/// <param name="initializers">�z��̏��������X�g���w�肵�܂��B</param>
		/// <returns>�V�����z��̏�������\�� <see cref="NewArrayExpression"/>�B</returns>
		public static NewArrayExpression NewArrayHelper(Type type, IEnumerable<Expression> initializers)
		{
			ContractUtils.RequiresNotNull(type, "type");
			ContractUtils.RequiresNotNullItems(initializers, "initializers");
			ContractUtils.Requires(type != typeof(void), "type", "�^�� void �ɂ��邱�Ƃ͂ł��܂���B");
			return Expression.NewArrayInit(
				type,
				initializers.Select(x => !TypeUtils.AreReferenceAssignable(type, x.Type) ?
					(type.IsSubclassOf(typeof(Expression)) && TypeUtils.AreAssignable(type, x.GetType()) ?
						Expression.Quote(x) :
						Convert(x, type)
					) : x
				)
			);
		}
	}
}
