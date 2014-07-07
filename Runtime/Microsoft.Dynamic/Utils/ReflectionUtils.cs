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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;

using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Utils
{
	/// <summary>リフレクションに関するユーティリティ メソッドを公開します。</summary>
	public static class ReflectionUtils
	{
		/// <summary>
		/// 型のマングルされた名前において、ジェネリック型の名前と型パラメータの数を区切るデリミタを表します。
		/// マングルされた名前はジェネリック型の名称に続いて、"`" が挿入され最後にそのジェネリック型の型パラメータの個数が記述されます。
		/// 例えば、<see cref="System.Collections.Generic.List&lt;T&gt;"/> の場合、マングルされた名前は "List`1" となります。
		/// </summary>
		public const char GenericArityDelimiter = '`';

		/// <summary>メソッドまたはコンストラクタのシグネチャを人間が判別可能な文字列形式にフォーマットします。</summary>
		/// <param name="method">フォーマットされたシグネチャを取得するメソッドまたはコンストラクタを指定します。</param>
		/// <returns>フォーマットされたシグネチャ。</returns>4
		public static string FormatSignature(MethodBase method)
		{
			ContractUtils.RequiresNotNull(method, "method");
			StringBuilder result = new StringBuilder();
			var methodInfo = method as MethodInfo;
			if (methodInfo != null)
				result.Append(FormatTypeName(methodInfo.ReturnType, x => x.FullName) + " ");
			var builder = method as MethodBuilder;
			if (builder != null)
				return result.Append(builder.Signature).ToString();
			var cb = method as ConstructorBuilder;
			if (cb != null)
				return result.Append(cb.Signature).ToString();
			result.Append(FormatTypeName(method.DeclaringType, x => x.FullName) + "::" + method.Name);
			if (!method.IsConstructor)
				result.Append(FormatTypeArgs(method.GetGenericArguments(), x => x.FullName));
			return result.Append("(" + (
				method.ContainsGenericParameters ? "?" :
				string.Join(", ", method.GetParameters().Select(x => FormatTypeName(x.ParameterType, t => t.FullName) + (!string.IsNullOrEmpty(x.Name) ? " " + x.Name : "")))
			) + ")").ToString();
		}

		/// <summary>指定された型の名前を人間が判別可能な文字列形式にフォーマットします。このメソッドではジェネリック型パラメータまたは型引数も出力されます。</summary>
		/// <param name="type">フォーマットされた名前を取得する型を指定します。</param>
		/// <param name="nameDispenser">型に対する名前を取得するデリゲートを指定します。</param>
		/// <returns>フォーマットされた型の名前。</returns>
		public static string FormatTypeName(Type type, Func<Type, string> nameDispenser)
		{
			ContractUtils.RequiresNotNull(type, "type");
			ContractUtils.RequiresNotNull(nameDispenser, "nameDispenser");
			if (type.IsGenericType)
			{
				var genericName = nameDispenser(type.GetGenericTypeDefinition()).Replace('+', '.');
				var tickIndex = genericName.IndexOf(GenericArityDelimiter);
				var typeArgs = type.GetGenericArguments();
				return (tickIndex >= 0 ? genericName.Substring(0, tickIndex) : genericName) +
					(type.IsGenericTypeDefinition ? "<" + new string(',', typeArgs.Length - 1) + ">" : FormatTypeArgs(typeArgs, nameDispenser));
			}
			else
				return type.IsGenericParameter ? type.Name : nameDispenser(type).Replace('+', '.');
		}

		/// <summary>指定されたジェネリック型パラメータまたは型引数を人間が判別可能な文字列形式にフォーマットします。</summary>
		/// <param name="types">フォーマットされた文字列を取得するジェネリック型パラメータまたは型引数を指定します。</param>
		/// <param name="nameDispenser">型に対する名前を取得するデリゲートを指定します。</param>
		/// <returns>フォーマットされた型パラメータまたは型引数。</returns>
		public static string FormatTypeArgs(Type[] types, Func<Type, string> nameDispenser)
		{
			ContractUtils.RequiresNotNullItems(types, "types");
			ContractUtils.RequiresNotNull(nameDispenser, "nameDispenser");
			return types.Length > 0 ? "<" + string.Join(", ", types.Select(x => FormatTypeName(x, nameDispenser))) + ">" : string.Empty;
		}
		
		/// <summary>メソッドまたはコンストラクタの戻り値の型を取得します。</summary>
		/// <param name="methodBase">戻り値の型を取得するメソッドまたはコンストラクタ。</param>
		/// <returns>メソッドの場合は戻り値の型。コンストラクタの場合は構築されるオブジェクトの型。</returns>
		public static Type GetReturnType(this MethodBase methodBase) { return methodBase.IsConstructor ? methodBase.DeclaringType : ((MethodInfo)methodBase).ReturnType; }

		/// <summary>指定されたメソッドのシグネチャが指定されたものと等しいかどうかを判断します。</summary>
		/// <param name="method">判断するメソッドを指定します。</param>
		/// <param name="requiredSignature">等しいかどうかを確認する基準となるシグネチャを指定します。引数の型に続いて戻り値の型を指定します。</param>
		/// <returns>メソッドが指定されたシグネチャと等しい場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool SignatureEquals(MethodInfo method, params Type[] requiredSignature)
		{
			ContractUtils.RequiresNotNull(method, "method");
			var actualTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();
			return actualTypes.Length == requiredSignature.Length - 1 && actualTypes.Concat(Enumerable.Repeat(method.ReturnType, 1)).SequenceEqual(requiredSignature);
		}

		/// <summary>メンバが拡張メソッドであるかどうか、または型に拡張メソッドが含まれているかどうかを判断します。</summary>
		/// <param name="member">拡張メソッドであるか、または拡張メソッドが含まれているかを判断するメソッドまたは型を指定します。</param>
		/// <returns>メンバが拡張メソッドであるか、型に拡張メソッドが含まれている場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool IsExtension(this MemberInfo member) { return member.IsDefined(typeof(ExtensionAttribute), false); }
		
		// not using IsIn/IsOut properties as they are not available in Silverlight:
		/// <summary>パラメータが out パラメータであるかどうかを判断します。</summary>
		/// <param name="parameter">out パラメータかどうかを調べるパラメータを指定します。</param>
		/// <returns>パラメータが out パラメータの場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool IsOutParameter(this ParameterInfo parameter) { return parameter.ParameterType.IsByRef && (parameter.Attributes & (ParameterAttributes.Out | ParameterAttributes.In)) == ParameterAttributes.Out; }

		/// <summary>パラメータが必須であるかどうかを判断します。つまり、省略可能ではなく、既定値も存在しないパラメータであるかどうかを調べます。</summary>
		/// <param name="parameter">必須であるかどうかを調べるパラメータを指定します。</param>
		/// <returns>パラメータの指定が必須である場合には <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool IsMandatory(this ParameterInfo parameter) { return (parameter.Attributes & (ParameterAttributes.Optional | ParameterAttributes.HasDefault)) == 0; }

		/// <summary>パラメータに既定値が存在するかどうかを判断します。</summary>
		/// <param name="parameter">既定値が存在するかどうかを調べるパラメータを指定します。</param>
		/// <returns>パラメータに既定値がある場合には<c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool HasDefaultValue(this ParameterInfo parameter) { return (parameter.Attributes & ParameterAttributes.HasDefault) != 0; }

		/// <summary>パラメータが null 非許容であるかどうかを判断します。</summary>
		/// <param name="parameter">null 非許容であるかどうかを調べるパラメータを指定します。</param>
		/// <returns>パラメータが null 非許容の場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool ProhibitsNull(this ParameterInfo parameter) { return parameter.IsDefined(typeof(NotNullAttribute), false); }

		/// <summary>パラメータに渡されるコレクションの要素に null を含めることができないかどうかを判断します。</summary>
		/// <param name="parameter">渡されるコレクションの要素に null を含めることができないかどうかを調べるパラメータを指定します。</param>
		/// <returns>パラメータに渡されるコレクションの要素に null を含めることができない場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool ProhibitsNullItems(this ParameterInfo parameter) { return parameter.IsDefined(typeof(NotNullItemsAttribute), false); }

		/// <summary>パラメータに任意の数の引数を指定できるかどうかを判断します。</summary>
		/// <param name="parameter">任意の数の引数を指定できるかどうかを調べるパラメータを指定します。</param>
		/// <returns>パラメータに任意の数の引数を指定できる場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool IsParamArray(this ParameterInfo parameter) { return parameter.IsDefined(typeof(ParamArrayAttribute), false); }

		/// <summary>通常の引数に束縛されないあらゆるキーワード引数をパラメータが受け付けるかどうかを判断します。</summary>
		/// <param name="parameter">通常の引数に束縛されないあらゆるキーワード引数を受け付けるかどうかを調べるパラメータを指定します。</param>
		/// <returns>通常の引数に束縛されないあらゆるキーワード引数をパラメータが受け付ける場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public static bool IsParamDictionary(this ParameterInfo parameter) { return parameter.IsDefined(typeof(ParamDictionaryAttribute), false); }

		/// <summary>指定された型によって実装されるインターフェイスを取得します。この型が継承する型で実装されたインターフェイスは含まれません。</summary>
		/// <param name="type">実装されたインターフェイスを取得する型を指定します。</param>
		/// <returns>継承階層中で <paramref name="type"/> で初めて実装されたインターフェイスの配列。</returns>
		public static Type[] GetDeclaredInterfaces(Type type) { return type.BaseType != null ? type.GetInterfaces().Except(type.BaseType.GetInterfaces()).ToArray() : type.GetInterfaces(); }

		/// <summary>指定された型のジェネリック型パラメータの数を含まない名前を取得します。</summary>
		/// <param name="type">ジェネリック型パラメータの数を含まない名前を取得する型を指定します。</param>
		/// <returns>ジェネリック型パラメータの数を含まない名前。</returns>
		public static string GetNormalizedTypeName(Type type) { return type.IsGenericType ? GetNormalizedTypeName(type.Name) : type.Name; }

		/// <summary>指定された名前からジェネリック型パラメータの数に関する情報を取り除いて、型の純粋な名前を取得します。</summary>
		/// <param name="typeName">純粋な名前を取得する元の型名を指定します。</param>
		/// <returns>名前からジェネリック型パラメータの数に関する情報が除かれた名前。</returns>
		public static string GetNormalizedTypeName(string typeName)
		{
			ContractUtils.Requires(typeName.IndexOf(Type.Delimiter) == -1, "typeName", "typeName must be simple name.");
			var backtick = typeName.IndexOf(GenericArityDelimiter);
			return backtick != -1 ? typeName.Substring(0, backtick) : typeName;
		}

		const MethodAttributes MethodAttributesToEraseInOveride = MethodAttributes.Abstract | MethodAttributes.ReservedMask;

		/// <summary>指定されたメソッドをオーバーライドするメソッドを指定された <see cref="TypeBuilder"/> に定義して、メソッドを生成する <see cref="MethodBuilder"/> を返します。</summary>
		/// <param name="typeBuilder">オーバーライドを定義する型を示す <see cref="TypeBuilder"/> を指定します。</param>
		/// <param name="additionalAttribute">メソッド オーバーライドに与える追加のメソッド属性を指定します。元の属性を置換する属性も存在します。</param>
		/// <param name="baseMethod">定義するメソッドによってオーバーライドされるメソッドを指定します。</param>
		/// <returns>指定されたメソッドをオーバーライドするメソッドを生成する <see cref="MethodBuilder"/>。</returns>
		public static MethodBuilder DefineMethodOverride(TypeBuilder typeBuilder, MethodAttributes additionalAttribute, MethodInfo baseMethod)
		{
			var finalAttrs = (baseMethod.Attributes & ~MethodAttributesToEraseInOveride) | additionalAttribute;
			if (!baseMethod.DeclaringType.IsInterface)
				finalAttrs &= ~MethodAttributes.NewSlot;
			if ((additionalAttribute & MethodAttributes.MemberAccessMask) != 0)
			{
				// remove existing member access, add new member access
				finalAttrs &= ~MethodAttributes.MemberAccessMask;
				finalAttrs |= additionalAttribute;
			}
			var builder = typeBuilder.DefineMethod(baseMethod.Name, finalAttrs, baseMethod.CallingConvention);
			CopyMethodSignature(baseMethod, builder, false);
			return builder;
		}

		/// <summary>指定されたメソッドのシグネチャを指定された <see cref="MethodBuilder"/> にコピーします。</summary>
		/// <param name="from">シグネチャのコピー元メソッドを指定します。</param>
		/// <param name="to">シグネチャのコピー先メソッドを生成する <see cref="MethodBuilder"/> を指定します。</param>
		/// <param name="replaceDeclaringType">シグネチャをコピーする際に <see cref="P:System.Reflection.MethodInfo.DeclaringType"/> を置き換えるかどうかを示す値を指定します。</param>
		public static void CopyMethodSignature(MethodInfo from, MethodBuilder to, bool replaceDeclaringType)
		{
			var paramInfos = from.GetParameters();
			var parameterTypes = new Type[paramInfos.Length];
			Type[][] parameterRequiredModifiers = null, parameterOptionalModifiers = null;
			for (int i = 0; i < paramInfos.Length; i++)
			{
				parameterTypes[i] = replaceDeclaringType && paramInfos[i].ParameterType == from.DeclaringType ? to.DeclaringType : paramInfos[i].ParameterType;
				var modifiers = paramInfos[i].GetRequiredCustomModifiers();
				if (modifiers.Length > 0)
					(parameterRequiredModifiers ?? (parameterRequiredModifiers = new Type[paramInfos.Length][]))[i] = modifiers;
				if ((modifiers = paramInfos[i].GetOptionalCustomModifiers()).Length > 0)
					(parameterOptionalModifiers ?? (parameterOptionalModifiers = new Type[paramInfos.Length][]))[i] = modifiers;
			}
			to.SetSignature(from.ReturnType, from.ReturnParameter.GetRequiredCustomModifiers(), from.ReturnParameter.GetOptionalCustomModifiers(), parameterTypes, parameterRequiredModifiers, parameterOptionalModifiers);
			CopyGenericMethodAttributes(from, to);
			for (int i = 0; i < paramInfos.Length; i++)
				to.DefineParameter(i + 1, paramInfos[i].Attributes, paramInfos[i].Name);
		}

		static void CopyGenericMethodAttributes(MethodInfo from, MethodBuilder to)
		{
			if (from.IsGenericMethodDefinition)
			{
				var args = from.GetGenericArguments();
				var builders = to.DefineGenericParameters(args.Select(x => x.Name).ToArray());
				for (int i = 0; i < args.Length; i++)
				{
					// Copy template parameter attributes
					builders[i].SetGenericParameterAttributes(args[i].GenericParameterAttributes);
					// Copy template parameter constraints
					List<Type> interfaces = new List<Type>();
					foreach (var constraint in args[i].GetGenericParameterConstraints())
					{
						if (constraint.IsInterface)
							interfaces.Add(constraint);
						else
							builders[i].SetBaseTypeConstraint(constraint);
					}
					if (interfaces.Count > 0)
						builders[i].SetInterfaceConstraints(interfaces.ToArray());
				}
			}
		}
	}
}
