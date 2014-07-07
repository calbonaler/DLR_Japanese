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
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Scripting.Runtime;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions.Calls
{
	using Ast = Expression;

	/// <summary>�Ԗߒl�̃t�B�[���h�܂��̓v���p�e�B���g�p����Ȃ��L�[���[�h�������g�p���čX�V���܂��B</summary>
	sealed class KeywordConstructorReturnBuilder : ReturnBuilder
	{
		ReturnBuilder _builder;
		int _kwArgCount;
		int[] _indexesUsed;
		MemberInfo[] _membersSet;
		bool _privateBinding;

		/// <summary>��ɂȂ� <see cref="ReturnBuilder"/>�A�L�[���[�h�����̐��A�ʒu�A�ݒ肷�郁���o�ACLR �����`�F�b�N���s�����ǂ������g�p���āA<see cref="Microsoft.Scripting.Actions.Calls.KeywordConstructorReturnBuilder"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="builder">��ɂȂ� <see cref="ReturnBuilder"/> ���w�肵�܂��B</param>
		/// <param name="kwArgCount">�L�[���[�h�����̐����w�肵�܂��B</param>
		/// <param name="indexesUsed">�g�p����L�[���[�h�����̈ʒu������ 0 ����n�܂�C���f�b�N�X�̔z����w�肵�܂��B</param>
		/// <param name="membersSet">�ݒ肷�郁���o��\�� <see cref="MemberInfo"/> ���w�肵�܂��B</param>
		/// <param name="privateBinding">CLR �����`�F�b�N�𖳎����邩�ǂ����������l���w�肵�܂��B</param>
		public KeywordConstructorReturnBuilder(ReturnBuilder builder, int kwArgCount, int[] indexesUsed, MemberInfo[] membersSet, bool privateBinding) : base(builder.ReturnType)
		{
			_builder = builder;
			_kwArgCount = kwArgCount;
			_indexesUsed = indexesUsed;
			_membersSet = membersSet;
			_privateBinding = privateBinding;
		}

		/// <summary>���\�b�h�Ăяo���̌��ʂ�Ԃ� <see cref="Expression"/> ��Ԃ��܂��B</summary>
		/// <param name="resolver">���\�b�h�ɑ΂���I�[�o�[���[�h���������邽�߂Ɏg�p����� <see cref="OverloadResolver"/> ���w�肵�܂��B</param>
		/// <param name="builders">���\�b�h�ɓn���ꂽ���ꂼ��̎������ɑ΂��� <see cref="ArgBuilder"/> �̃��X�g���w�肵�܂��B</param>
		/// <param name="args">���񂳂ꂽ�������w�肵�܂��B</param>
		/// <param name="ret">���\�b�h�Ăяo���̌��݂̌��ʂ�\�� <see cref="Expression"/> ���w�肵�܂��B</param>
		/// <returns>���\�b�h�Ăяo���̌��ʂ�\�� <see cref="Expression"/>�B</returns>
		internal override Expression ToExpression(OverloadResolver resolver, IList<ArgBuilder> builders, RestrictedArguments args, Expression ret)
		{
			List<Expression> sets = new List<Expression>();
			ParameterExpression tmp = resolver.GetTemporary(ret.Type, "val");
			sets.Add(Ast.Assign(tmp, ret));
			for (int i = 0; i < _indexesUsed.Length; i++)
			{
				Expression value = args.Objects[args.Length - _kwArgCount + _indexesUsed[i]].Expression;
				switch (_membersSet[i].MemberType)
				{
					case MemberTypes.Field:
						FieldInfo fi = (FieldInfo)_membersSet[i];
						if (!fi.IsLiteral && !fi.IsInitOnly)
							sets.Add(Ast.Assign(Ast.Field(tmp, fi), ConvertToHelper(resolver, value, fi.FieldType)));
						else
							// call a helper which throws the error but "returns object"
							sets.Add(
								Ast.Convert(
									Ast.Call(
										new Func<bool, string, object>(ScriptingRuntimeHelpers.ReadOnlyAssignError).Method,
										AstUtils.Constant(true),
										AstUtils.Constant(fi.Name)
									),
									fi.FieldType
								)
							);
						break;
					case MemberTypes.Property:
						PropertyInfo pi = (PropertyInfo)_membersSet[i];
						if (pi.GetSetMethod(_privateBinding) != null)
							sets.Add(Ast.Assign(Ast.Property(tmp, pi), ConvertToHelper(resolver, value, pi.PropertyType)));
						else
							// call a helper which throws the error but "returns object"
							sets.Add(
								Ast.Convert(
									Ast.Call(
										new Func<bool, string, object>(ScriptingRuntimeHelpers.ReadOnlyAssignError).Method,
										AstUtils.Constant(false),
										AstUtils.Constant(pi.Name)
									),
									pi.PropertyType
								)
							);
						break;
				}
			}
			sets.Add(tmp);
			return _builder.ToExpression(resolver, builders, args, Ast.Block(sets.ToArray()));
		}

		// TODO: revisit
		static Expression ConvertToHelper(OverloadResolver resolver, Expression value, Type type)
		{
			if (type == value.Type)
				return value;
			if (type.IsAssignableFrom(value.Type))
				return AstUtils.Convert(value, type);
			return resolver.GetDynamicConversion(value, type);
		}
	}
}
