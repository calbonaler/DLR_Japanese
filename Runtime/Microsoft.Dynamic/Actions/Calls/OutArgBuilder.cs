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

#if !CLR2
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions.Calls
{
	using Ast = Expression;

	/// <summary>StrongBox �Ƃ��ēn����Ȃ������ꍇ�Aout �����ɑ΂���������𐶐����܂��Bout �����͒ǉ��̕Ԗߒl�Ƃ��ĕԂ���܂��B</summary>
	sealed class OutArgBuilder : ArgBuilder
	{
		readonly Type _parameterType;
		readonly bool _isRef;
		ParameterExpression _tmp;

		/// <summary>�w�肳�ꂽ���������g�p���āA<see cref="Microsoft.Scripting.Actions.Calls.OutArgBuilder"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="info">���� <see cref="OutArgBuilder"/> ���Ή����鉼�������w�肵�܂��B</param>
		public OutArgBuilder(ParameterInfo info) : base(info)
		{
			_parameterType = info.ParameterType.IsByRef ? info.ParameterType.GetElementType() : info.ParameterType;
			_isRef = info.ParameterType.IsByRef;
		}

		/// <summary>���̃r���_�ɂ���ď�������ۂ̈����̐����擾���܂��B</summary>
		public override int ConsumedArgumentCount { get { return 0; } }

		/// <summary>���̈����̗D�揇�ʂ��擾���܂��B</summary>
		public override int Priority { get { return 5; } }

		/// <summary>�����ɓn�����l��񋟂��� <see cref="Expression"/> ��Ԃ��܂��B</summary>
		/// <param name="resolver">���\�b�h�ɑ΂���I�[�o�[���[�h���������邽�߂Ɏg�p����� <see cref="OverloadResolver"/> ���w�肵�܂��B</param>
		/// <param name="args">���񂳂ꂽ�������w�肵�܂��B</param>
		/// <param name="hasBeenUsed">�Ăяo������������Ǝg�p���ꂽ�����ɑΉ�����ʒu�� <c>true</c> ���i�[����܂��B</param>
		/// <returns>�����ɓn�����l��񋟂��� <see cref="Expression"/>�B�������X�L�b�v���ꂽ�ꍇ (�܂�A�Ăяo����ɓn����Ȃ��ꍇ) <c>null</c> ��Ԃ��܂��B</returns>
		protected internal override Expression ToExpression(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed)
		{
			if (_isRef)
			{
				if (_tmp == null)
					_tmp = resolver.GetTemporary(_parameterType, "outParam");
				return _tmp;
			}
			return GetDefaultValue();
		}

		/// <summary>�������Ԗߒl�𐶐����� (ref ���邢�� out �̂悤��) �ꍇ�A�Ăяo�����ɒǉ��ŕԂ����l��񋟂��܂��B</summary>
		/// <param name="resolver">���\�b�h�ɑ΂���I�[�o�[���[�h���������邽�߂Ɏg�p����� <see cref="OverloadResolver"/> ���w�肵�܂��B</param>
		/// <returns>�Ăяo����Œǉ��ŕԂ����l��񋟂��� <see cref="Expression"/>�B</returns>
		internal override Expression ToReturnExpression(OverloadResolver resolver)
		{
			if (_isRef)
				return _tmp;
			return GetDefaultValue();
		}

		/// <summary>�Q�Ǝ��̈����ɂ���ēn��������\�Ȓl���擾���܂��B�Ăяo����͍X�V���ꂽ�l���i�[����܂��B</summary>
		internal override Expression ByRefArgument { get { return _isRef ? _tmp : null; } }

		Expression GetDefaultValue()
		{
			if (_parameterType.IsValueType)
				// default(T)                
				return AstUtils.Constant(Activator.CreateInstance(_parameterType));
			return AstUtils.Constant(null);
		}
	}
}
