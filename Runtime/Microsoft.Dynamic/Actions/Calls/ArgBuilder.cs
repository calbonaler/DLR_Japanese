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
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>
	/// ���\�b�h�r���_�[�ɂ���Ďg�p�����������̒l��񋟂��܂��B
	/// ���\�b�h�ɓn����邻�ꂼ��̕��������ɑ΂��� 1 �� <see cref="ArgBuilder"/> �����݂��܂��B
	/// ���\�b�h�ɒ�`���ꂽ�_��������\�� <see cref="ParameterWrapper"/> �Ƃ͑ΏƓI�ł��B
	/// </summary>
	public abstract class ArgBuilder
	{
		internal const int AllArguments = -1;

		/// <summary>�w�肳�ꂽ���������g�p���āA<see cref="Microsoft.Scripting.Actions.Calls.ArgBuilder"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="info">���� <see cref="ArgBuilder"/> ���Ή����鉼�������w�肵�܂��B</param>
		protected ArgBuilder(ParameterInfo info) { ParameterInfo = info; }

		/// <summary>���̈����̗D�揇�ʂ��擾���܂��B</summary>
		public abstract int Priority { get; }

		// can be null, e.g. for ctor return value builder or custom arg builders
		/// <summary>��ɂȂ鉼�������擾���܂��B�R���X�g���N�^�̕Ԗߒl�ɑ΂��� <see cref="ArgBuilder"/> �Ȃǂł� <c>null</c> �ɂȂ邱�Ƃ�����܂��B</summary>
		public ParameterInfo ParameterInfo { get; private set; }

		/// <summary>���̃r���_�ɂ���ď�������ۂ̈����̐����擾���܂��B</summary>
		public abstract int ConsumedArgumentCount { get; }

		/// <summary>�����ɓn�����l��񋟂��� <see cref="Expression"/> ��Ԃ��܂��B</summary>
		/// <param name="resolver">���\�b�h�ɑ΂���I�[�o�[���[�h���������邽�߂Ɏg�p����� <see cref="OverloadResolver"/> ���w�肵�܂��B</param>
		/// <param name="args">���񂳂ꂽ�������w�肵�܂��B</param>
		/// <param name="hasBeenUsed">�Ăяo������������Ǝg�p���ꂽ�����ɑΉ�����ʒu�� <c>true</c> ���i�[����܂��B</param>
		/// <returns>�����ɓn�����l��񋟂��� <see cref="Expression"/>�B�������X�L�b�v���ꂽ�ꍇ (�܂�A�Ăяo����ɓn����Ȃ��ꍇ) <c>null</c> ��Ԃ��܂��B</returns>
		protected internal abstract Expression ToExpression(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed);

		/// <summary>��������������ɓn�����l��񋟂���f���Q�[�g��Ԃ��܂��B</summary>
		/// <param name="resolver">���\�b�h�ɑ΂���I�[�o�[���[�h���������邽�߂Ɏg�p����� <see cref="OverloadResolver"/> ���w�肵�܂��B</param>
		/// <param name="args">���񂳂ꂽ�������w�肵�܂��B</param>
		/// <param name="hasBeenUsed">�Ăяo������������Ǝg�p���ꂽ�����ɑΉ�����ʒu�� <c>true</c> ���i�[����܂��B</param>
		/// <returns>��������������ɓn�����l��񋟂���f���Q�[�g�B�������X�L�b�v���ꂽ�ꍇ (�܂�A�Ăяo����ɓn����Ȃ��ꍇ) <c>null</c> ��Ԃ��܂��B</returns>
		protected internal virtual Func<object[], object> ToDelegate(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed) { return null; }

		/// <summary>
		/// �w�肳�ꂽ�C���f�b�N�X�ɑ΂���������ɃA�N�Z�X����֐���\���܂��B
		/// ToDelegate ����Ԃ����Ƃ��A�N���[�Y�I�[�o�[���ꂽ�l�͈������œK�����������f���Q�[�g�Ăяo�����\�ɂ��܂��B
		/// ���̊֐��̓��t���N�V������p���ĎQ�Ƃ���邽�߁A���O��ύX����ꍇ�͌Ăяo�����̍X�V���K�v�ɂȂ�܂��B
		/// </summary>
		/// <param name="value">�����ɑΉ�����C���f�b�N�X���w�肵�܂��B</param>
		/// <param name="args">���������w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�C���f�b�N�X�ɑΉ�����������B</returns>
		public static object ArgumentRead(object value, object[] args) { return args[(int)value]; }

		/// <summary>�����ɑ΂��ėv�������^���擾���܂��B<see cref="ArgBuilder"/> ������������Ȃ��ꍇ�� <c>null</c> ���Ԃ���܂��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
		public virtual Type Type { get { return null; } }

		/// <summary>���\�b�h�Ăяo���̌�ɒ񋟂��ꂽ�l���X�V���� <see cref="Expression"/> ��Ԃ��܂��B</summary>
		/// <param name="resolver">���\�b�h�ɑ΂���I�[�o�[���[�h���������邽�߂Ɏg�p����� <see cref="OverloadResolver"/> ���w�肵�܂��B</param>
		/// <param name="args">���񂳂ꂽ�������w�肵�܂��B</param>
		/// <returns>�񋟂��ꂽ�l���X�V���� <see cref="Expression"/>�B�X�V���s�v�ȏꍇ�� <c>null</c> ��Ԃ��܂��B</returns>
		internal virtual Expression UpdateFromReturn(OverloadResolver resolver, RestrictedArguments args) { return null; }

		/// <summary>�������Ԗߒl�𐶐����� (ref ���邢�� out �̂悤��) �ꍇ�A�Ăяo�����ɒǉ��ŕԂ����l��񋟂��܂��B</summary>
		/// <param name="resolver">���\�b�h�ɑ΂���I�[�o�[���[�h���������邽�߂Ɏg�p����� <see cref="OverloadResolver"/> ���w�肵�܂��B</param>
		/// <returns>�Ăяo����Œǉ��ŕԂ����l��񋟂��� <see cref="Expression"/>�B</returns>
		internal virtual Expression ToReturnExpression(OverloadResolver resolver) { throw new InvalidOperationException(); }

		/// <summary>�Q�Ɠn���̈����ɂ���ēn��������\�Ȓl���擾���܂��B�Ăяo����͍X�V���ꂽ�l���i�[����܂��B</summary>
		internal virtual Expression ByRefArgument { get { return null; } }

		/// <summary>�w�肳�ꂽ�����ɑ΂��邱�� <see cref="ArgBuilder"/> �̃R�s�[�𐶐����܂��B</summary>
		/// <param name="newType">�R�s�[����ɂ��鉼�������w�肵�܂��B</param>
		/// <returns>�R�s�[���ꂽ <see cref="ArgBuilder"/>�B</returns>
		public virtual ArgBuilder Clone(ParameterInfo newType) { return null; }
	}
}
