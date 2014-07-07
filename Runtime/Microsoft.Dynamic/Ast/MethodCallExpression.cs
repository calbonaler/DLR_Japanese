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
		/// 式ツリーでメソッド呼び出しを表すノードを作成します。
		/// このメソッドは <see cref="Convert"/> を使用して引数に対する変換を行います。
		/// </summary>
		/// <param name="method">呼び出すメソッドを指定します。</param>
		/// <param name="arguments">メソッドに渡す引数を指定します。</param>
		/// <returns>新しく作成された <see cref="MethodCallExpression"/>。</returns>
		public static MethodCallExpression SimpleCallHelper(MethodInfo method, params Expression[] arguments) { return SimpleCallHelper(null, method, arguments); }

		/// <summary>
		/// 式ツリーでメソッド呼び出しを表すノードを作成します。
		/// このメソッドは <see cref="Convert"/> を使用して引数、必要であればインスタンスに対する変換を行います。
		/// </summary>
		/// <param name="instance">メソッドを呼び出す際のインスタンスを指定します。静的メソッドの場合は <c>null</c> を指定します。</param>
		/// <param name="method">呼び出すメソッドを指定します。</param>
		/// <param name="arguments">メソッドに渡す引数を指定します。</param>
		/// <returns>新しく作成された <see cref="MethodCallExpression"/>。</returns>
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
		/// 式ツリーでメソッド呼び出しを表すノードを作成します。
		/// このメソッドは <see cref="Convert"/> を使用して引数に対する変換を行います。
		/// このバージョンではさらに仮引数の既定値と配列引数をサポートします。
		/// </summary>
		/// <param name="method">呼び出すメソッドを指定します。</param>
		/// <param name="arguments">メソッドに渡す引数を指定します。</param>
		/// <returns>新しく作成された <see cref="MethodCallExpression"/>。</returns>
		public static MethodCallExpression ComplexCallHelper(MethodInfo method, params Expression[] arguments)
		{
			ContractUtils.RequiresNotNull(method, "method");
			ContractUtils.Requires(method.IsStatic, "method", "Method must be static");
			return ComplexCallHelper(null, method, arguments);
		}

		/// <summary>
		/// 式ツリーでメソッド呼び出しを表すノードを作成します。
		/// このメソッドは <see cref="Convert"/> を使用して引数に対する変換を行います。
		/// このバージョンではさらに仮引数の既定値と配列引数をサポートします。
		/// </summary>
		/// <param name="instance">メソッドを呼び出す際のインスタンスを指定します。静的メソッドの場合は <c>null</c> を指定します。</param>
		/// <param name="method">呼び出すメソッドを指定します。</param>
		/// <param name="arguments">メソッドに渡す引数を指定します。</param>
		/// <returns>新しく作成された <see cref="MethodCallExpression"/>。</returns>
		public static MethodCallExpression ComplexCallHelper(Expression instance, MethodInfo method, params Expression[] arguments)
		{
			ContractUtils.RequiresNotNull(method, "method");
			ContractUtils.RequiresNotNullItems(arguments, "arguments");
			ContractUtils.Requires(instance != null ^ method.IsStatic, "instance");
			var parameters = method.GetParameters();
			Expression[] clone = null;
			int consumed = 0; // 今まで消費された引数
			// 引数の配列を検査またはクローンの配列の適切な要素を作成します。
			for (int current = 0; current < parameters.Length; current++)
			{
				Expression argument;
				// 最後の引数は配列引数?
				if (current == parameters.Length - 1 && parameters[current].IsParamArray())
				{
					var elementType = parameters[current].ParameterType.GetElementType();
					if (consumed >= arguments.Length) // 渡す引数はないか?
						argument = Expression.NewArrayInit(elementType); // 無いので、空の配列を作成。
					else if (consumed == arguments.Length - 1 && CompatibleParameterTypes(parameters[current].ParameterType, arguments[consumed].Type))
						// 正確にただ 1 つの引数か? それが正しい型であれば、直接入れる。
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
					argument = arguments[consumed++]; // 引数がある。
				else
				{
					// 存在しない引数のため、既定値を試す。
					ContractUtils.Requires(!parameters[current].IsMandatory(), "arguments", "Argument not provided for a mandatory parameter");
					argument = CreateDefaultValueExpression(parameters[current]);
				}
				// 必要であれば変換を追加
				argument = ArgumentConvertHelper(argument, parameters[current].ParameterType);
				// 配列のクローンを作る必要があるか?
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
