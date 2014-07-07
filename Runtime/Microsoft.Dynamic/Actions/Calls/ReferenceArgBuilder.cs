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
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using AstUtils = Microsoft.Scripting.Ast.Utils;
using RuntimeHelpers = Microsoft.Scripting.Runtime.ScriptingRuntimeHelpers;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>
	/// ���[�U�[�������I�ɎQ�Ƃ��g�p���� (copy-in �܂��� copy-out �Z�}���e�B�N�X��) �n�����Ƃ���]���Ă��������\���܂��B
	/// ���[�U�[�͌Ăяo�������������ۂɒl���X�V����� <see cref="StrongBox&lt;T&gt;"/> �I�u�W�F�N�g��n���܂��B
	/// </summary>
	sealed class ReferenceArgBuilder : SimpleArgBuilder
	{
		readonly Type _elementType;
		ParameterExpression _tmp;

		/// <summary>�������Ɋւ�����A�v�f�^�A�����̈ʒu���w�肵�āA<see cref="Microsoft.Scripting.Actions.Calls.ReferenceArgBuilder"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="info">�������̏���\�� <see cref="ParameterInfo"/> ���w�肵�܂��B</param>
		/// <param name="elementType">�����̗v�f�^���w�肵�܂��B</param>
		/// <param name="index">�������̈ʒu�������C���f�b�N�X���w�肵�܂��B</param>
		public ReferenceArgBuilder(ParameterInfo info, Type elementType, int index) : base(info, typeof(StrongBox<>).MakeGenericType(elementType), index, false, false) { _elementType = elementType; }

		/// <summary>���� <see cref="SimpleArgBuilder"/> �̈����̈ʒu���w�肳�ꂽ�ʒu�ɒu���������V���� <see cref="SimpleArgBuilder"/> ���쐬���܂��B</summary>
		/// <param name="newIndex">�쐬���� <see cref="SimpleArgBuilder"/> �̈����̈ʒu�������C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>���� <see cref="SimpleArgBuilder"/> �̈����̈ʒu���w�肳�ꂽ�ʒu�ɒu���������V���� <see cref="SimpleArgBuilder"/>�B</returns>
		protected override SimpleArgBuilder Copy(int newIndex) { return new ReferenceArgBuilder(ParameterInfo, _elementType, newIndex); }

		/// <summary>�w�肳�ꂽ�����ɑ΂��邱�� <see cref="ArgBuilder"/> �̃R�s�[�𐶐����܂��B</summary>
		/// <param name="newType">�R�s�[����ɂ��鉼�������w�肵�܂��B</param>
		/// <returns>�R�s�[���ꂽ <see cref="ArgBuilder"/>�B</returns>
		public override ArgBuilder Clone(ParameterInfo newType)
		{
			var elementType = newType.ParameterType.GetElementType();
			return new ReferenceArgBuilder(newType, elementType, Index);
		}

		/// <summary>���̈����̗D�揇�ʂ��擾���܂��B</summary>
		public override int Priority { get { return 5; } }

		/// <summary>�����ɓn�����l��񋟂��� <see cref="Expression"/> ��Ԃ��܂��B</summary>
		/// <param name="resolver">���\�b�h�ɑ΂���I�[�o�[���[�h���������邽�߂Ɏg�p����� <see cref="OverloadResolver"/> ���w�肵�܂��B</param>
		/// <param name="args">���񂳂ꂽ�������w�肵�܂��B</param>
		/// <param name="hasBeenUsed">�Ăяo������������Ǝg�p���ꂽ�����ɑΉ�����ʒu�� <c>true</c> ���i�[����܂��B</param>
		/// <returns>�����ɓn�����l��񋟂��� <see cref="Expression"/>�B�������X�L�b�v���ꂽ�ꍇ (�܂�A�Ăяo����ɓn����Ȃ��ꍇ) <c>null</c> ��Ԃ��܂��B</returns>
		protected internal override Expression ToExpression(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed)
		{
			Debug.Assert(!hasBeenUsed[Index]);
			hasBeenUsed[Index] = true;
			return Expression.Condition(Expression.TypeIs(args.Objects[Index].Expression, Type),
				Expression.Assign(_tmp ?? (_tmp = resolver.GetTemporary(_elementType, "outParam")), Expression.Field(AstUtils.Convert(args.Objects[Index].Expression, Type), Type.GetField("Value"))),
				Expression.Throw(
					Expression.Call(
						new Func<Type, object, Exception>(RuntimeHelpers.MakeIncorrectBoxTypeError).Method,
						AstUtils.Constant(_elementType),
						AstUtils.Convert(args.Objects[Index].Expression, typeof(object))
					), _elementType
				)
			);
		}

		/// <summary>��������������ɓn�����l��񋟂���f���Q�[�g��Ԃ��܂��B</summary>
		/// <param name="resolver">���\�b�h�ɑ΂���I�[�o�[���[�h���������邽�߂Ɏg�p����� <see cref="OverloadResolver"/> ���w�肵�܂��B</param>
		/// <param name="args">���񂳂ꂽ�������w�肵�܂��B</param>
		/// <param name="hasBeenUsed">�Ăяo������������Ǝg�p���ꂽ�����ɑΉ�����ʒu�� <c>true</c> ���i�[����܂��B</param>
		/// <returns>��������������ɓn�����l��񋟂���f���Q�[�g�B�������X�L�b�v���ꂽ�ꍇ (�܂�A�Ăяo����ɓn����Ȃ��ꍇ) <c>null</c> ��Ԃ��܂��B</returns>
		protected internal override Func<object[], object> ToDelegate(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed) { return null; }

		/// <summary>���\�b�h�Ăяo���̌�ɒ񋟂��ꂽ�l���X�V���� <see cref="Expression"/> ��Ԃ��܂��B</summary>
		/// <param name="resolver">���\�b�h�ɑ΂���I�[�o�[���[�h���������邽�߂Ɏg�p����� <see cref="OverloadResolver"/> ���w�肵�܂��B</param>
		/// <param name="args">���񂳂ꂽ�������w�肵�܂��B</param>
		/// <returns>�񋟂��ꂽ�l���X�V���� <see cref="Expression"/>�B�X�V���s�v�ȏꍇ�� <c>null</c> ��Ԃ��܂��B</returns>
		internal override Expression UpdateFromReturn(OverloadResolver resolver, RestrictedArguments args)
		{
			return Expression.Assign(Expression.Field(Expression.Convert(args.Objects[Index].Expression, Type), Type.GetField("Value")), _tmp);
		}

		/// <summary>�Q�Ɠn���̈����ɂ���ēn��������\�Ȓl���擾���܂��B�Ăяo����͍X�V���ꂽ�l���i�[����܂��B</summary>
		internal override Expression ByRefArgument { get { return _tmp; } }
	}
}
