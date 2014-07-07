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
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>���\�b�h�r���_�[�ɂ���Ďg�p�����A�߂�l��\�������\�z������@��񋟂��܂��B</summary>
	class ReturnBuilder
	{
		/// <summary>�߂�l�̌^���g�p���āA<see cref="Microsoft.Scripting.Actions.Calls.ReturnBuilder"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="returnType"><see cref="ReturnBuilder"/> ���X�^�b�N�ɒu���l�̌^���w�肵�܂��B</param>
		public ReturnBuilder(Type returnType)
		{
			Debug.Assert(returnType != null);
			ReturnType = returnType;
		}

		/// <summary>���\�b�h�Ăяo���̌��ʂ�Ԃ� <see cref="Expression"/> ��Ԃ��܂��B</summary>
		/// <param name="resolver">���\�b�h�ɑ΂���I�[�o�[���[�h���������邽�߂Ɏg�p����� <see cref="OverloadResolver"/> ���w�肵�܂��B</param>
		/// <param name="builders">���\�b�h�ɓn���ꂽ���ꂼ��̎������ɑ΂��� <see cref="ArgBuilder"/> �̃��X�g���w�肵�܂��B</param>
		/// <param name="args">���񂳂ꂽ�������w�肵�܂��B</param>
		/// <param name="ret">���\�b�h�Ăяo���̌��݂̌��ʂ�\�� <see cref="Expression"/> ���w�肵�܂��B</param>
		/// <returns>���\�b�h�Ăяo���̌��ʂ�\�� <see cref="Expression"/>�B</returns>
		internal virtual Expression ToExpression(OverloadResolver resolver, IList<ArgBuilder> builders, RestrictedArguments args, Expression ret) { return ret; }

		/// <summary>�߂�l�𐶐�����悤�Ȉ����̐����擾���܂��B</summary>
		public virtual int CountOutParams { get { return 0; } }

		/// <summary>���̃r���_�[���\���߂�l�̌^���擾���܂��B</summary>
		public Type ReturnType { get; private set; }
	}
}
