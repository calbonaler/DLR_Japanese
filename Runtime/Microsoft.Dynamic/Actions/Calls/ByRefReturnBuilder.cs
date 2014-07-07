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
using System.Linq.Expressions;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions.Calls
{
	using Ast = Expression;

	/// <summary>�Q�Ɠn�����ꂽ�����̍X�V�ɑ΂��� <see cref="ReturnBuilder"/> ��\���܂��B</summary>
	sealed class ByRefReturnBuilder : ReturnBuilder
	{
		IList<int> _returnArgs;

		/// <summary>�Q�Ɠn�����ꂽ�����̈ʒu�̃��X�g���g�p���āA<see cref="Microsoft.Scripting.Actions.Calls.ByRefReturnBuilder"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="returnArgs">�Q�Ɠn�����ꂽ�����̈ʒu������ 0 ����n�܂�C���f�b�N�X�̃��X�g���w�肵�܂��B</param>
		public ByRefReturnBuilder(IList<int> returnArgs) : base(typeof(object)) { _returnArgs = returnArgs; }

		/// <summary>���\�b�h�Ăяo���̌��ʂ�Ԃ� <see cref="Expression"/> ��Ԃ��܂��B</summary>
		/// <param name="resolver">���\�b�h�ɑ΂���I�[�o�[���[�h���������邽�߂Ɏg�p����� <see cref="OverloadResolver"/> ���w�肵�܂��B</param>
		/// <param name="builders">���\�b�h�ɓn���ꂽ���ꂼ��̎������ɑ΂��� <see cref="ArgBuilder"/> �̃��X�g���w�肵�܂��B</param>
		/// <param name="args">���񂳂ꂽ�������w�肵�܂��B</param>
		/// <param name="ret">���\�b�h�Ăяo���̌��݂̌��ʂ�\�� <see cref="Expression"/> ���w�肵�܂��B</param>
		/// <returns>���\�b�h�Ăяo���̌��ʂ�\�� <see cref="Expression"/>�B</returns>
		internal override Expression ToExpression(OverloadResolver resolver, IList<ArgBuilder> builders, RestrictedArguments args, Expression ret)
		{
			if (_returnArgs.Count == 1)
			{
				if (_returnArgs[0] == -1)
					return ret;
				return Ast.Block(ret, builders[_returnArgs[0]].ToReturnExpression(resolver));
			}
			Expression[] retValues = new Expression[_returnArgs.Count];
			int rIndex = 0;
			bool usesRet = false;
			foreach (int index in _returnArgs)
			{
				if (index == -1)
				{
					usesRet = true;
					retValues[rIndex++] = ret;
				}
				else
					retValues[rIndex++] = builders[index].ToReturnExpression(resolver);
			}
			Expression retArray = AstUtils.NewArrayHelper(typeof(object), retValues);
			if (!usesRet)
				retArray = Ast.Block(ret, retArray);
			return resolver.GetByRefArrayExpression(retArray);
		}

		/// <summary>�߂�l�𐶐�����悤�Ȉ����̐����擾���܂��B</summary>
		public override int CountOutParams { get { return _returnArgs.Count; } }
	}
}
