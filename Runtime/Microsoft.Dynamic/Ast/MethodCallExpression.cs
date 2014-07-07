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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Ast
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
	public static partial class Utils
	{
		/// <summary>
		/// ���c���[�Ń��\�b�h�Ăяo����\���m�[�h���쐬���܂��B
		/// ���̃��\�b�h�� <see cref="Convert"/> ���g�p���Ĉ����ɑ΂���ϊ����s���܂��B
		/// </summary>
		/// <param name="method">�Ăяo�����\�b�h���w�肵�܂��B</param>
		/// <param name="arguments">���\�b�h�ɓn���������w�肵�܂��B</param>
		/// <returns>�V�����쐬���ꂽ <see cref="MethodCallExpression"/>�B</returns>
		public static MethodCallExpression SimpleCallHelper(MethodInfo method, params Expression[] arguments) { return SimpleCallHelper(null, method, arguments); }

		/// <summary>
		/// ���c���[�Ń��\�b�h�Ăяo����\���m�[�h���쐬���܂��B
		/// ���̃��\�b�h�� <see cref="Convert"/> ���g�p���Ĉ����A�K�v�ł���΃C���X�^���X�ɑ΂���ϊ����s���܂��B
		/// </summary>
		/// <param name="instance">���\�b�h���Ăяo���ۂ̃C���X�^���X���w�肵�܂��B�ÓI���\�b�h�̏ꍇ�� <c>null</c> ���w�肵�܂��B</param>
		/// <param name="method">�Ăяo�����\�b�h���w�肵�܂��B</param>
		/// <param name="arguments">���\�b�h�ɓn���������w�肵�܂��B</param>
		/// <returns>�V�����쐬���ꂽ <see cref="MethodCallExpression"/>�B</returns>
		public static MethodCallExpression SimpleCallHelper(Expression instance, MethodInfo method, params Expression[] arguments)
		{
			ContractUtils.RequiresNotNull(method, "method");
			ContractUtils.Requires(instance != null ^ method.IsStatic, "instance");
			ContractUtils.RequiresNotNullItems(arguments, "arguments");
			var parameters = method.GetParameters();
			ContractUtils.Requires(arguments.Length == parameters.Length, "arguments", "Incorrect number of arguments");
			return Expression.Call(instance != null ? Convert(instance, method.DeclaringType) : null, method, ArgumentConvertHelper(arguments, parameters));
		}

		static IEnumerable<Expression> ArgumentConvertHelper(Expression[] arguments, ParameterInfo[] parameters) { return arguments.Select((x, i) => !CompatibleParameterTypes(parameters[i].ParameterType, x.Type) ? ArgumentConvertHelper(x, parameters[i].ParameterType) : x); }

		static Expression ArgumentConvertHelper(Expression argument, Type type) { return argument.Type != type && (!type.IsByRef || argument.Type != (type = type.GetElementType())) ? Convert(argument, type) : argument; }

		static bool CompatibleParameterTypes(Type parameter, Type argument) { return parameter == argument || !parameter.IsValueType && !argument.IsValueType && parameter.IsAssignableFrom(argument) || parameter.IsByRef && parameter.GetElementType() == argument; }

		/// <summary>
		/// ���c���[�Ń��\�b�h�Ăяo����\���m�[�h���쐬���܂��B
		/// ���̃��\�b�h�� <see cref="Convert"/> ���g�p���Ĉ����ɑ΂���ϊ����s���܂��B
		/// ���̃o�[�W�����ł͂���ɉ������̊���l�Ɣz��������T�|�[�g���܂��B
		/// </summary>
		/// <param name="method">�Ăяo�����\�b�h���w�肵�܂��B</param>
		/// <param name="arguments">���\�b�h�ɓn���������w�肵�܂��B</param>
		/// <returns>�V�����쐬���ꂽ <see cref="MethodCallExpression"/>�B</returns>
		public static MethodCallExpression ComplexCallHelper(MethodInfo method, params Expression[] arguments)
		{
			ContractUtils.RequiresNotNull(method, "method");
			ContractUtils.Requires(method.IsStatic, "method", "Method must be static");
			return ComplexCallHelper(null, method, arguments);
		}

		/// <summary>
		/// ���c���[�Ń��\�b�h�Ăяo����\���m�[�h���쐬���܂��B
		/// ���̃��\�b�h�� <see cref="Convert"/> ���g�p���Ĉ����ɑ΂���ϊ����s���܂��B
		/// ���̃o�[�W�����ł͂���ɉ������̊���l�Ɣz��������T�|�[�g���܂��B
		/// </summary>
		/// <param name="instance">���\�b�h���Ăяo���ۂ̃C���X�^���X���w�肵�܂��B�ÓI���\�b�h�̏ꍇ�� <c>null</c> ���w�肵�܂��B</param>
		/// <param name="method">�Ăяo�����\�b�h���w�肵�܂��B</param>
		/// <param name="arguments">���\�b�h�ɓn���������w�肵�܂��B</param>
		/// <returns>�V�����쐬���ꂽ <see cref="MethodCallExpression"/>�B</returns>
		public static MethodCallExpression ComplexCallHelper(Expression instance, MethodInfo method, params Expression[] arguments)
		{
			ContractUtils.RequiresNotNull(method, "method");
			ContractUtils.RequiresNotNullItems(arguments, "arguments");
			ContractUtils.Requires(instance != null ^ method.IsStatic, "instance");
			var parameters = method.GetParameters();
			Expression[] clone = null;
			int consumed = 0; // ���܂ŏ���ꂽ����
			// �����̔z��������܂��̓N���[���̔z��̓K�؂ȗv�f���쐬���܂��B
			for (int current = 0; current < parameters.Length; current++)
			{
				Expression argument;
				// �Ō�̈����͔z�����?
				if (current == parameters.Length - 1 && parameters[current].IsParamArray())
				{
					var elementType = parameters[current].ParameterType.GetElementType();
					if (consumed >= arguments.Length) // �n�������͂Ȃ���?
						argument = Expression.NewArrayInit(elementType); // �����̂ŁA��̔z����쐬�B
					else if (consumed == arguments.Length - 1 && CompatibleParameterTypes(parameters[current].ParameterType, arguments[consumed].Type))
						// ���m�ɂ��� 1 �̈�����? ���ꂪ�������^�ł���΁A���ړ����B
						argument = arguments[consumed++];
					else
					{
						Expression[] paramArray = new Expression[arguments.Length - consumed];
						for (int i = 0; consumed < arguments.Length; i++, consumed++)
							paramArray[i] = Convert(arguments[consumed], elementType);
						argument = Expression.NewArrayInit(elementType, paramArray);
					}
				}
				else if (consumed < arguments.Length)
					argument = arguments[consumed++]; // ����������B
				else
				{
					// ���݂��Ȃ������̂��߁A����l�������B
					ContractUtils.Requires(!parameters[current].IsMandatory(), "arguments", "Argument not provided for a mandatory parameter");
					argument = CreateDefaultValueExpression(parameters[current]);
				}
				// �K�v�ł���Εϊ���ǉ�
				argument = ArgumentConvertHelper(argument, parameters[current].ParameterType);
				// �z��̃N���[�������K�v�����邩?
				if (clone == null && (current >= arguments.Length || argument != arguments[current]))
				{
					clone = new Expression[parameters.Length];
					for (int i = 0; i < current; i++)
						clone[i] = arguments[i];
				}
				if (clone != null)
					clone[current] = argument;
			}
			ContractUtils.Requires(consumed == arguments.Length, "arguments", "Incorrect number of arguments");
			return Expression.Call(instance != null ? Convert(instance, method.DeclaringType) : null, method, clone != null ? clone : arguments);
		}

		static Expression CreateDefaultValueExpression(ParameterInfo parameter)
		{
			if (parameter.HasDefaultValue())
				return AstUtils.Constant(parameter.DefaultValue, parameter.ParameterType);
			throw new NotSupportedException("missing parameter value not supported");
		}
	}
}
