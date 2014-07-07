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
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>���\�b�h�Ăяo���ɑ΂��Ĉ����̊���l��񋟂��� <see cref="ArgBuilder"/> �ł��B</summary>
	sealed class DefaultArgBuilder : ArgBuilder
	{
		/// <summary>�w�肳�ꂽ���������g�p���āA<see cref="Microsoft.Scripting.Actions.Calls.ArgBuilder"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="info">���� <see cref="ArgBuilder"/> ���Ή����鉼�������w�肵�܂��B</param>
		public DefaultArgBuilder(ParameterInfo info) : base(info) { Assert.NotNull(info); }

		/// <summary>���̈����̗D�揇�ʂ��擾���܂��B</summary>
		public override int Priority { get { return 2; } }

		/// <summary>���̃r���_�ɂ���ď�������ۂ̈����̐����擾���܂��B</summary>
		public override int ConsumedArgumentCount { get { return 0; } }

		/// <summary>�����ɓn�����l��񋟂��� <see cref="Expression"/> ��Ԃ��܂��B</summary>
		/// <param name="resolver">���\�b�h�ɑ΂���I�[�o�[���[�h���������邽�߂Ɏg�p����� <see cref="OverloadResolver"/> ���w�肵�܂��B</param>
		/// <param name="args">���񂳂ꂽ�������w�肵�܂��B</param>
		/// <param name="hasBeenUsed">�Ăяo������������Ǝg�p���ꂽ�����ɑΉ�����ʒu�� <c>true</c> ���i�[����܂��B</param>
		/// <returns>�����ɓn�����l��񋟂��� <see cref="Expression"/>�B�������X�L�b�v���ꂽ�ꍇ (�܂�A�Ăяo����ɓn����Ȃ��ꍇ) <c>null</c> ��Ԃ��܂��B</returns>
		protected internal override Expression ToExpression(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed)
		{
			object value = ParameterInfo.DefaultValue;
			if (value is Missing)
				value = CompilerHelpers.GetMissingValue(ParameterInfo.ParameterType);
			if (ParameterInfo.ParameterType.IsByRef)
				return AstUtils.Constant(value, ParameterInfo.ParameterType.GetElementType());
			return resolver.Convert(new DynamicMetaObject(AstUtils.Constant(value), BindingRestrictions.Empty, value), CompilerHelpers.GetType(value), ParameterInfo, ParameterInfo.ParameterType);
		}

		/// <summary>��������������ɓn�����l��񋟂���f���Q�[�g��Ԃ��܂��B</summary>
		/// <param name="resolver">���\�b�h�ɑ΂���I�[�o�[���[�h���������邽�߂Ɏg�p����� <see cref="OverloadResolver"/> ���w�肵�܂��B</param>
		/// <param name="args">���񂳂ꂽ�������w�肵�܂��B</param>
		/// <param name="hasBeenUsed">�Ăяo������������Ǝg�p���ꂽ�����ɑΉ�����ʒu�� <c>true</c> ���i�[����܂��B</param>
		/// <returns>��������������ɓn�����l��񋟂���f���Q�[�g�B�������X�L�b�v���ꂽ�ꍇ (�܂�A�Ăяo����ɓn����Ȃ��ꍇ) <c>null</c> ��Ԃ��܂��B</returns>
		protected internal override Func<object[], object> ToDelegate(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed)
		{
			if (ParameterInfo.ParameterType.IsByRef)
				return null;
			else if (ParameterInfo.DefaultValue is Missing && CompilerHelpers.GetMissingValue(ParameterInfo.ParameterType) is Missing)
				return null;  // reflection throws when we do this
			object val = ParameterInfo.DefaultValue;
			if (val is Missing)
				val = CompilerHelpers.GetMissingValue(ParameterInfo.ParameterType);
			Debug.Assert(val != Missing.Value);
			return _ => val;
		}
	}
}
