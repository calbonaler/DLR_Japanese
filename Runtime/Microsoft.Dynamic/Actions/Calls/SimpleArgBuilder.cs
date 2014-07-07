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
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>
	/// ���[�U�[�ɂ���Ď������̒l�Ƃ��Đ������ꂽ�l�𐶐����܂��B
	/// ���̃N���X�͂���Ɍ��̉������Ɋւ������ǐՂ��A�z������⎫�����������֐��ɑ΂���g�����\�b�h���쐬���邽�߂Ɏg�p����܂��B
	/// </summary>
	public class SimpleArgBuilder : ArgBuilder
	{
		readonly Type _parameterType;

		/// <summary>�������ɑ΂��鉼�����̏�񂪗��p�ł��Ȃ��ꍇ�ɁA<see cref="Microsoft.Scripting.Actions.Calls.SimpleArgBuilder"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="parameterType">�ΏۂƂȂ鉼�����̌^���w�肵�܂��B</param>
		/// <param name="index">�������̈ʒu�������C���f�b�N�X���w�肵�܂��B</param>
		/// <param name="isParams">���̈������z��������ǂ����������l���w�肵�܂��B</param>
		/// <param name="isParamsDict">���̈����������������ǂ����������l���w�肵�܂��B</param>
		public SimpleArgBuilder(Type parameterType, int index, bool isParams, bool isParamsDict) : this(null, parameterType, index, isParams, isParamsDict) { }

		/// <summary>�������ɑ΂��鉼�����̏�񂪗��p�ł��Ȃ��ꍇ�ɁA<see cref="Microsoft.Scripting.Actions.Calls.SimpleArgBuilder"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="info">�������̏���\�� <see cref="ParameterInfo"/> ���w�肵�܂��B</param>
		/// <param name="parameterType">�ΏۂƂȂ鉼�����̌^���w�肵�܂��B</param>
		/// <param name="index">�������̈ʒu�������C���f�b�N�X���w�肵�܂��B</param>
		/// <param name="isParams">���̈������z��������ǂ����������l���w�肵�܂��B</param>
		/// <param name="isParamsDict">���̈����������������ǂ����������l���w�肵�܂��B</param>
		public SimpleArgBuilder(ParameterInfo info, Type parameterType, int index, bool isParams, bool isParamsDict) : base(info)
		{
			ContractUtils.Requires(index >= 0, "index");
			ContractUtils.RequiresNotNull(parameterType, "parameterType");
			Index = index;
			_parameterType = parameterType;
			IsParamsArray = isParams;
			IsParamsDict = isParamsDict;
		}

		/// <summary>���� <see cref="SimpleArgBuilder"/> �̈����̈ʒu���w�肳�ꂽ�ʒu�ɒu���������V���� <see cref="SimpleArgBuilder"/> ���쐬���܂��B</summary>
		/// <param name="newIndex">�쐬���� <see cref="SimpleArgBuilder"/> �̈����̈ʒu�������C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>���� <see cref="SimpleArgBuilder"/> �̈����̈ʒu���w�肳�ꂽ�ʒu�ɒu���������V���� <see cref="SimpleArgBuilder"/>�B</returns>
		internal SimpleArgBuilder MakeCopy(int newIndex)
		{
			var result = Copy(newIndex);
			// Copy() must be overriden in derived classes and return an instance of the derived class:
			Debug.Assert(result.GetType() == GetType());
			return result;
		}

		/// <summary>���� <see cref="SimpleArgBuilder"/> �̈����̈ʒu���w�肳�ꂽ�ʒu�ɒu���������V���� <see cref="SimpleArgBuilder"/> ���쐬���܂��B</summary>
		/// <param name="newIndex">�쐬���� <see cref="SimpleArgBuilder"/> �̈����̈ʒu�������C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>���� <see cref="SimpleArgBuilder"/> �̈����̈ʒu���w�肳�ꂽ�ʒu�ɒu���������V���� <see cref="SimpleArgBuilder"/>�B</returns>
		protected virtual SimpleArgBuilder Copy(int newIndex) { return new SimpleArgBuilder(ParameterInfo, _parameterType, newIndex, IsParamsArray, IsParamsDict); }

		/// <summary>���̃r���_�ɂ���ď�������ۂ̈����̐����擾���܂��B</summary>
		public override int ConsumedArgumentCount { get { return 1; } }

		/// <summary>���̈����̗D�揇�ʂ��擾���܂��B</summary>
		public override int Priority { get { return 0; } }

		/// <summary>���̈������z������ł��邩�ǂ����������l���擾���܂��B</summary>
		public bool IsParamsArray { get; private set; }

		/// <summary>���̈��������������ł��邩�ǂ����������l���擾���܂��B</summary>
		public bool IsParamsDict { get; private set; }

		/// <summary>�����ɓn�����l��񋟂��� <see cref="Expression"/> ��Ԃ��܂��B</summary>
		/// <param name="resolver">���\�b�h�ɑ΂���I�[�o�[���[�h���������邽�߂Ɏg�p����� <see cref="OverloadResolver"/> ���w�肵�܂��B</param>
		/// <param name="args">���񂳂ꂽ�������w�肵�܂��B</param>
		/// <param name="hasBeenUsed">�Ăяo������������Ǝg�p���ꂽ�����ɑΉ�����ʒu�� <c>true</c> ���i�[����܂��B</param>
		/// <returns>�����ɓn�����l��񋟂��� <see cref="Expression"/>�B�������X�L�b�v���ꂽ�ꍇ (�܂�A�Ăяo����ɓn����Ȃ��ꍇ) <c>null</c> ��Ԃ��܂��B</returns>
		protected internal override Expression ToExpression(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed)
		{
			ContractUtils.Requires(hasBeenUsed.Length == args.Length, "hasBeenUsed");
			ContractUtils.RequiresArrayIndex(args.Length, Index, "args");
			ContractUtils.Requires(!hasBeenUsed[Index], "hasBeenUsed");
			hasBeenUsed[Index] = true;
			return resolver.Convert(args.Objects[Index], args.Types[Index], ParameterInfo, _parameterType);
		}

		/// <summary>��������������ɓn�����l��񋟂���f���Q�[�g��Ԃ��܂��B</summary>
		/// <param name="resolver">���\�b�h�ɑ΂���I�[�o�[���[�h���������邽�߂Ɏg�p����� <see cref="OverloadResolver"/> ���w�肵�܂��B</param>
		/// <param name="args">���񂳂ꂽ�������w�肵�܂��B</param>
		/// <param name="hasBeenUsed">�Ăяo������������Ǝg�p���ꂽ�����ɑΉ�����ʒu�� <c>true</c> ���i�[����܂��B</param>
		/// <returns>��������������ɓn�����l��񋟂���f���Q�[�g�B�������X�L�b�v���ꂽ�ꍇ (�܂�A�Ăяo����ɓn����Ȃ��ꍇ) <c>null</c> ��Ԃ��܂��B</returns>
		protected internal override Func<object[], object> ToDelegate(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed)
		{
			var conv = resolver.GetConvertor(Index + 1, args.Objects[Index], ParameterInfo, _parameterType);
			if (conv != null)
				return conv;
			return (Func<object[], object>)Delegate.CreateDelegate(typeof(Func<object[], object>), Index + 1, new Func<object, object[], object>(ArgBuilder.ArgumentRead).Method);
		}

		// Index of actual argument expression.
		/// <summary>�������̈ʒu�������C���f�b�N�X���擾���܂��B</summary>
		public int Index { get; private set; }

		/// <summary>�����ɑ΂��ėv�������^���擾���܂��B<see cref="ArgBuilder"/> ������������Ȃ��ꍇ�� <c>null</c> ���Ԃ���܂��B</summary>
		public override Type Type { get { return _parameterType; } }

		/// <summary>�w�肳�ꂽ�����ɑ΂��邱�� <see cref="ArgBuilder"/> �̃R�s�[�𐶐����܂��B</summary>
		/// <param name="newType">�R�s�[����ɂ��鉼�������w�肵�܂��B</param>
		/// <returns>�R�s�[���ꂽ <see cref="ArgBuilder"/>�B</returns>
		public override ArgBuilder Clone(ParameterInfo newType) { return new SimpleArgBuilder(newType, newType.ParameterType, Index, IsParamsArray, IsParamsDict); }
	}
}
