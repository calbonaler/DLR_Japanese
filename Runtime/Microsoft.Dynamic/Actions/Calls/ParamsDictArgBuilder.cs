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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions.Calls
{
	using Ast = Expression;

	/// <summary>���������ł�����������������ɒ񋟂��܂��B����͊֐��ɒ񋟂���邷�ׂĂ̗]���Ȗ��O/�l�y�A���֐��ɓn�����V���{���f�B�N�V���i���Ɏ��W���܂��B</summary>
	sealed class ParamsDictArgBuilder : ArgBuilder
	{
		readonly string[] _names;
		readonly int[] _nameIndexes;
		readonly int _argIndex;

		/// <summary>�������Ɋւ��郁�^�f�[�^�A���������X�g���ł̎��������̊J�n�ʒu�A���O����ёΉ�����C���f�b�N�X�̃��X�g���g�p���āA<see cref="Microsoft.Scripting.Actions.Calls.ParamsDictArgBuilder"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="info">�������Ɋւ��郁�^�f�[�^���w�肵�܂��B</param>
		/// <param name="argIndex">���������X�g���ł̎��������̊J�n�ʒu������ 0 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		/// <param name="names">���������̖��O�̃��X�g���w�肵�܂��B</param>
		/// <param name="nameIndexes">���������̃C���f�b�N�X�̃��X�g���w�肵�܂��B</param>
		public ParamsDictArgBuilder(ParameterInfo info, int argIndex, string[] names, int[] nameIndexes) : base(info)
		{
			Assert.NotNull(info, names, nameIndexes);
			_argIndex = argIndex;
			_names = names;
			_nameIndexes = nameIndexes;
		}

		/// <summary>���̃r���_�ɂ���ď�������ۂ̈����̐����擾���܂��B<see cref="ParamsDictArgBuilder"/> �ł͎c��̂��ׂĂ̈����������܂��B</summary>
		public override int ConsumedArgumentCount { get { return AllArguments; } }

		/// <summary>���̈����̗D�揇�ʂ��擾���܂��B</summary>
		public override int Priority { get { return 3; } }

		/// <summary>�����ɓn�����l��񋟂��� <see cref="Expression"/> ��Ԃ��܂��B</summary>
		/// <param name="resolver">���\�b�h�ɑ΂���I�[�o�[���[�h���������邽�߂Ɏg�p����� <see cref="OverloadResolver"/> ���w�肵�܂��B</param>
		/// <param name="args">���񂳂ꂽ�������w�肵�܂��B</param>
		/// <param name="hasBeenUsed">�Ăяo������������Ǝg�p���ꂽ�����ɑΉ�����ʒu�� <c>true</c> ���i�[����܂��B</param>
		/// <returns>�����ɓn�����l��񋟂��� <see cref="Expression"/>�B�������X�L�b�v���ꂽ�ꍇ (�܂�A�Ăяo����ɓn����Ȃ��ꍇ) <c>null</c> ��Ԃ��܂��B</returns>
		protected internal override Expression ToExpression(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed)
		{
			Type dictType = ParameterInfo.ParameterType;
			return Ast.Call(
				GetCreationDelegate(dictType).Method,
				Ast.NewArrayInit(typeof(string), _names.Select(x => AstUtils.Constant(x))),
				AstUtils.NewArrayHelper(typeof(object), GetParameters(hasBeenUsed).Select(x => args.Objects[x].Expression))
			);
		}

		/// <summary>�����ɑ΂��ėv�������^���擾���܂��B</summary>
		public override Type Type { get { return typeof(IAttributesCollection); } }

		IEnumerable<int> GetParameters(bool[] hasBeenUsed)
		{
			var result = _nameIndexes.Select(x => x + _argIndex).Where(x => !hasBeenUsed[x]);
			foreach (var index in result)
				hasBeenUsed[index] = true;
			return result;
		}

		/// <summary>��������������ɓn�����l��񋟂���f���Q�[�g��Ԃ��܂��B</summary>
		/// <param name="resolver">���\�b�h�ɑ΂���I�[�o�[���[�h���������邽�߂Ɏg�p����� <see cref="OverloadResolver"/> ���w�肵�܂��B</param>
		/// <param name="args">���񂳂ꂽ�������w�肵�܂��B</param>
		/// <param name="hasBeenUsed">�Ăяo������������Ǝg�p���ꂽ�����ɑΉ�����ʒu�� <c>true</c> ���i�[����܂��B</param>
		/// <returns>��������������ɓn�����l��񋟂���f���Q�[�g�B�������X�L�b�v���ꂽ�ꍇ (�܂�A�Ăяo����ɓn����Ȃ��ꍇ) <c>null</c> ��Ԃ��܂��B</returns>
		protected internal override Func<object[], object> ToDelegate(OverloadResolver resolver, RestrictedArguments args, bool[] hasBeenUsed)
		{
			var indexes = GetParameters(hasBeenUsed).ToArray();
			var func = GetCreationDelegate(ParameterInfo.ParameterType);
			return actualArgs => func(_names, indexes.Select(i => actualArgs[i + 1]).ToArray());
		}

		Func<string[], object[], object> GetCreationDelegate(Type dictType)
		{
			Func<string[], object[], object> func = null;
			if (dictType == typeof(IDictionary))
				func = BinderOps.MakeDictionary<object, object>;
			else if (dictType == typeof(IAttributesCollection))
				func = BinderOps.MakeSymbolDictionary;
			else if (dictType.IsGenericType)
			{
				Type[] genArgs = dictType.GetGenericArguments();
				if ((dictType.GetGenericTypeDefinition() == typeof(IDictionary<,>) || dictType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) && (genArgs[0] == typeof(string) || genArgs[0] == typeof(object)))
				{
					var method = new Func<string[], object[], IDictionary<string, object>>(BinderOps.MakeDictionary<string, object>).Method.GetGenericMethodDefinition();
					func = (Func<string[], object[], object>)method.MakeGenericMethod(genArgs).CreateDelegate(typeof(Func<string[], object[], object>));
				}
			}
			if (func == null)
				throw new InvalidOperationException(string.Format("�T�|�[�g����Ă��Ȃ����������^: {0}", dictType.FullName));
			return func;
		}
	}
}
