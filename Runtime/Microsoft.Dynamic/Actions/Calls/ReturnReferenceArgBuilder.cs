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

namespace Microsoft.Scripting.Actions.Calls
{
	using Ast = Expression;

	/// <summary>
	/// <see cref="System.Runtime.CompilerServices.StrongBox&lt;T&gt;"/> ���񋟂���Ȃ��Ƃ��ɁA�Q�ƈ����ɑ΂�����������\�z���܂��B
	/// �X�V���ꂽ�l�͖߂�l�̈ꕔ�Ƃ��ĕԂ���܂��B
	/// </summary>
	sealed class ReturnReferenceArgBuilder : SimpleArgBuilder
	{
		ParameterExpression _tmp;

		/// <summary>�������̏�񂨂�ю������̈ʒu���g�p���āA<see cref="Microsoft.Scripting.Actions.Calls.ReturnReferenceArgBuilder"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="info">�������̏���\�� <see cref="ParameterInfo"/> ���w�肵�܂��B</param>
		/// <param name="index">�������̈ʒu�������C���f�b�N�X���w�肵�܂��B</param>
		public ReturnReferenceArgBuilder(ParameterInfo info, int index) : base(info, info.ParameterType.GetElementType(), index, false, false) { }

		/// <summary>���� <see cref="SimpleArgBuilder"/> �̈����̈ʒu���w�肳�ꂽ�ʒu�ɒu���������V���� <see cref="SimpleArgBuilder"/> ���쐬���܂��B</summary>
		/// <param name="newIndex">�쐬���� <see cref="SimpleArgBuilder"/> �̈����̈ʒu�������C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>���� <see cref="SimpleArgBuilder"/> �̈����̈ʒu���w�肳�ꂽ�ʒu�ɒu���������V���� <see cref="SimpleArgBuilder"/>�B</returns>
		protected override SimpleArgBuilder Copy(int newIndex) { return new ReturnReferenceArgBuilder(ParameterInfo, newIndex); }

		/// <summary>�w�肳�ꂽ�����ɑ΂��邱�� <see cref="ArgBuilder"/> �̃R�s�[�𐶐����܂��B</summary>
		/// <param name="newType">�R�s�[����ɂ��鉼�������w�肵�܂��B</param>
		/// <returns>�R�s�[���ꂽ <see cref="ArgBuilder"/>�B</returns>
		public override ArgBuilder Clone(ParameterInfo newType) { return new ReturnReferenceArgBuilder(newType, Index); }

		/// <summary>�����ɓn�����l��񋟂��� <see cref="Expression"/> ��Ԃ��܂��B</summary>
		/// <param name="resolver">���\�b�h�ɑ΂���I�[�o�[���[�h���������邽�߂Ɏg�p����� <see cref="OverloadResolver"/> ���w�肵�܂��B</param>
		/// <param name="args">���񂳂ꂽ�������w�肵�܂��B</param>
		/// <param name="hasBeenUsed">�Ăяo������������Ǝg�p���ꂽ�����ɑΉ�����ʒu�� <c>true</c> ���i�[����܂��B</param>
		/// <returns>�����ɓn�����l��񋟂��� <see cref="Expression"/>�B�������X�L�b�v���ꂽ�ꍇ (�܂�A�Ăяo����ɓn����Ȃ��ꍇ) <c>null</c> ��Ԃ��܂��B</returns>
		protected internal override Expression ToExpression(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed)
		{
			_tmp = _tmp ?? resolver.GetTemporary(Type, "outParam");
			return Ast.Block(Ast.Assign(_tmp, base.ToExpression(resolver, args, hasBeenUsed)), _tmp);
		}

		/// <summary>�������Ԗߒl�𐶐����� (ref ���邢�� out �̂悤��) �ꍇ�A�Ăяo�����ɒǉ��ŕԂ����l��񋟂��܂��B</summary>
		/// <param name="resolver">���\�b�h�ɑ΂���I�[�o�[���[�h���������邽�߂Ɏg�p����� <see cref="OverloadResolver"/> ���w�肵�܂��B</param>
		/// <returns>�Ăяo����Œǉ��ŕԂ����l��񋟂��� <see cref="Expression"/>�B</returns>
		internal override Expression ToReturnExpression(OverloadResolver resolver) { return _tmp; }

		/// <summary>�Q�Ɠn���̈����ɂ���ēn��������\�Ȓl���擾���܂��B�Ăяo����͍X�V���ꂽ�l���i�[����܂��B</summary>
		internal override Expression ByRefArgument { get { return _tmp; } }

		/// <summary>���̈����̗D�揇�ʂ��擾���܂��B</summary>
		public override int Priority { get { return 5; } }
	}
}
