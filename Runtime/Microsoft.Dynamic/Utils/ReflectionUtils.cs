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
	/// <summary>���t���N�V�����Ɋւ��郆�[�e�B���e�B ���\�b�h�����J���܂��B</summary>
	public static class ReflectionUtils
	{
		/// <summary>
		/// �^�̃}���O�����ꂽ���O�ɂ����āA�W�F�l���b�N�^�̖��O�ƌ^�p�����[�^�̐�����؂�f���~�^��\���܂��B
		/// �}���O�����ꂽ���O�̓W�F�l���b�N�^�̖��̂ɑ����āA"`" ���}������Ō�ɂ��̃W�F�l���b�N�^�̌^�p�����[�^�̌����L�q����܂��B
		/// �Ⴆ�΁A<see cref="System.Collections.Generic.List&lt;T&gt;"/> �̏ꍇ�A�}���O�����ꂽ���O�� "List`1" �ƂȂ�܂��B
		/// </summary>
		public const char GenericArityDelimiter = '`';

		/// <summary>���\�b�h�܂��̓R���X�g���N�^�̃V�O�l�`����l�Ԃ����ʉ\�ȕ�����`���Ƀt�H�[�}�b�g���܂��B</summary>
		/// <param name="method">�t�H�[�}�b�g���ꂽ�V�O�l�`�����擾���郁�\�b�h�܂��̓R���X�g���N�^���w�肵�܂��B</param>
		/// <returns>�t�H�[�}�b�g���ꂽ�V�O�l�`���B</returns>4
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

		/// <summary>�w�肳�ꂽ�^�̖��O��l�Ԃ����ʉ\�ȕ�����`���Ƀt�H�[�}�b�g���܂��B���̃��\�b�h�ł̓W�F�l���b�N�^�p�����[�^�܂��͌^�������o�͂���܂��B</summary>
		/// <param name="type">�t�H�[�}�b�g���ꂽ���O���擾����^���w�肵�܂��B</param>
		/// <param name="nameDispenser">�^�ɑ΂��閼�O���擾����f���Q�[�g���w�肵�܂��B</param>
		/// <returns>�t�H�[�}�b�g���ꂽ�^�̖��O�B</returns>
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

		/// <summary>�w�肳�ꂽ�W�F�l���b�N�^�p�����[�^�܂��͌^������l�Ԃ����ʉ\�ȕ�����`���Ƀt�H�[�}�b�g���܂��B</summary>
		/// <param name="types">�t�H�[�}�b�g���ꂽ��������擾����W�F�l���b�N�^�p�����[�^�܂��͌^�������w�肵�܂��B</param>
		/// <param name="nameDispenser">�^�ɑ΂��閼�O���擾����f���Q�[�g���w�肵�܂��B</param>
		/// <returns>�t�H�[�}�b�g���ꂽ�^�p�����[�^�܂��͌^�����B</returns>
		public static string FormatTypeArgs(Type[] types, Func<Type, string> nameDispenser)
		{
			ContractUtils.RequiresNotNullItems(types, "types");
			ContractUtils.RequiresNotNull(nameDispenser, "nameDispenser");
			return types.Length > 0 ? "<" + string.Join(", ", types.Select(x => FormatTypeName(x, nameDispenser))) + ">" : string.Empty;
		}
		
		/// <summary>���\�b�h�܂��̓R���X�g���N�^�̖߂�l�̌^���擾���܂��B</summary>
		/// <param name="methodBase">�߂�l�̌^���擾���郁�\�b�h�܂��̓R���X�g���N�^�B</param>
		/// <returns>���\�b�h�̏ꍇ�͖߂�l�̌^�B�R���X�g���N�^�̏ꍇ�͍\�z�����I�u�W�F�N�g�̌^�B</returns>
		public static Type GetReturnType(this MethodBase methodBase) { return methodBase.IsConstructor ? methodBase.DeclaringType : ((MethodInfo)methodBase).ReturnType; }

		/// <summary>�w�肳�ꂽ���\�b�h�̃V�O�l�`�����w�肳�ꂽ���̂Ɠ��������ǂ����𔻒f���܂��B</summary>
		/// <param name="method">���f���郁�\�b�h���w�肵�܂��B</param>
		/// <param name="requiredSignature">���������ǂ������m�F�����ƂȂ�V�O�l�`�����w�肵�܂��B�����̌^�ɑ����Ė߂�l�̌^���w�肵�܂��B</param>
		/// <returns>���\�b�h���w�肳�ꂽ�V�O�l�`���Ɠ������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool SignatureEquals(MethodInfo method, params Type[] requiredSignature)
		{
			ContractUtils.RequiresNotNull(method, "method");
			var actualTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();
			return actualTypes.Length == requiredSignature.Length - 1 && actualTypes.Concat(Enumerable.Repeat(method.ReturnType, 1)).SequenceEqual(requiredSignature);
		}

		/// <summary>�����o���g�����\�b�h�ł��邩�ǂ����A�܂��͌^�Ɋg�����\�b�h���܂܂�Ă��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="member">�g�����\�b�h�ł��邩�A�܂��͊g�����\�b�h���܂܂�Ă��邩�𔻒f���郁�\�b�h�܂��͌^���w�肵�܂��B</param>
		/// <returns>�����o���g�����\�b�h�ł��邩�A�^�Ɋg�����\�b�h���܂܂�Ă���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool IsExtension(this MemberInfo member) { return member.IsDefined(typeof(ExtensionAttribute), false); }
		
		// not using IsIn/IsOut properties as they are not available in Silverlight:
		/// <summary>�p�����[�^�� out �p�����[�^�ł��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="parameter">out �p�����[�^���ǂ����𒲂ׂ�p�����[�^���w�肵�܂��B</param>
		/// <returns>�p�����[�^�� out �p�����[�^�̏ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool IsOutParameter(this ParameterInfo parameter) { return parameter.ParameterType.IsByRef && (parameter.Attributes & (ParameterAttributes.Out | ParameterAttributes.In)) == ParameterAttributes.Out; }

		/// <summary>�p�����[�^���K�{�ł��邩�ǂ����𔻒f���܂��B�܂�A�ȗ��\�ł͂Ȃ��A����l�����݂��Ȃ��p�����[�^�ł��邩�ǂ����𒲂ׂ܂��B</summary>
		/// <param name="parameter">�K�{�ł��邩�ǂ����𒲂ׂ�p�����[�^���w�肵�܂��B</param>
		/// <returns>�p�����[�^�̎w�肪�K�{�ł���ꍇ�ɂ� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool IsMandatory(this ParameterInfo parameter) { return (parameter.Attributes & (ParameterAttributes.Optional | ParameterAttributes.HasDefault)) == 0; }

		/// <summary>�p�����[�^�Ɋ���l�����݂��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="parameter">����l�����݂��邩�ǂ����𒲂ׂ�p�����[�^���w�肵�܂��B</param>
		/// <returns>�p�����[�^�Ɋ���l������ꍇ�ɂ�<c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool HasDefaultValue(this ParameterInfo parameter) { return (parameter.Attributes & ParameterAttributes.HasDefault) != 0; }

		/// <summary>�p�����[�^�� null �񋖗e�ł��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="parameter">null �񋖗e�ł��邩�ǂ����𒲂ׂ�p�����[�^���w�肵�܂��B</param>
		/// <returns>�p�����[�^�� null �񋖗e�̏ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool ProhibitsNull(this ParameterInfo parameter) { return parameter.IsDefined(typeof(NotNullAttribute), false); }

		/// <summary>�p�����[�^�ɓn�����R���N�V�����̗v�f�� null ���܂߂邱�Ƃ��ł��Ȃ����ǂ����𔻒f���܂��B</summary>
		/// <param name="parameter">�n�����R���N�V�����̗v�f�� null ���܂߂邱�Ƃ��ł��Ȃ����ǂ����𒲂ׂ�p�����[�^���w�肵�܂��B</param>
		/// <returns>�p�����[�^�ɓn�����R���N�V�����̗v�f�� null ���܂߂邱�Ƃ��ł��Ȃ��ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool ProhibitsNullItems(this ParameterInfo parameter) { return parameter.IsDefined(typeof(NotNullItemsAttribute), false); }

		/// <summary>�p�����[�^�ɔC�ӂ̐��̈������w��ł��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="parameter">�C�ӂ̐��̈������w��ł��邩�ǂ����𒲂ׂ�p�����[�^���w�肵�܂��B</param>
		/// <returns>�p�����[�^�ɔC�ӂ̐��̈������w��ł���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool IsParamArray(this ParameterInfo parameter) { return parameter.IsDefined(typeof(ParamArrayAttribute), false); }

		/// <summary>�ʏ�̈����ɑ�������Ȃ�������L�[���[�h�������p�����[�^���󂯕t���邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="parameter">�ʏ�̈����ɑ�������Ȃ�������L�[���[�h�������󂯕t���邩�ǂ����𒲂ׂ�p�����[�^���w�肵�܂��B</param>
		/// <returns>�ʏ�̈����ɑ�������Ȃ�������L�[���[�h�������p�����[�^���󂯕t����ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public static bool IsParamDictionary(this ParameterInfo parameter) { return parameter.IsDefined(typeof(ParamDictionaryAttribute), false); }

		/// <summary>�w�肳�ꂽ�^�ɂ���Ď��������C���^�[�t�F�C�X���擾���܂��B���̌^���p������^�Ŏ������ꂽ�C���^�[�t�F�C�X�͊܂܂�܂���B</summary>
		/// <param name="type">�������ꂽ�C���^�[�t�F�C�X���擾����^���w�肵�܂��B</param>
		/// <returns>�p���K�w���� <paramref name="type"/> �ŏ��߂Ď������ꂽ�C���^�[�t�F�C�X�̔z��B</returns>
		public static Type[] GetDeclaredInterfaces(Type type) { return type.BaseType != null ? type.GetInterfaces().Except(type.BaseType.GetInterfaces()).ToArray() : type.GetInterfaces(); }

		/// <summary>�w�肳�ꂽ�^�̃W�F�l���b�N�^�p�����[�^�̐����܂܂Ȃ����O���擾���܂��B</summary>
		/// <param name="type">�W�F�l���b�N�^�p�����[�^�̐����܂܂Ȃ����O���擾����^���w�肵�܂��B</param>
		/// <returns>�W�F�l���b�N�^�p�����[�^�̐����܂܂Ȃ����O�B</returns>
		public static string GetNormalizedTypeName(Type type) { return type.IsGenericType ? GetNormalizedTypeName(type.Name) : type.Name; }

		/// <summary>�w�肳�ꂽ���O����W�F�l���b�N�^�p�����[�^�̐��Ɋւ��������菜���āA�^�̏����Ȗ��O���擾���܂��B</summary>
		/// <param name="typeName">�����Ȗ��O���擾���錳�̌^�����w�肵�܂��B</param>
		/// <returns>���O����W�F�l���b�N�^�p�����[�^�̐��Ɋւ����񂪏����ꂽ���O�B</returns>
		public static string GetNormalizedTypeName(string typeName)
		{
			ContractUtils.Requires(typeName.IndexOf(Type.Delimiter) == -1, "typeName", "typeName must be simple name.");
			var backtick = typeName.IndexOf(GenericArityDelimiter);
			return backtick != -1 ? typeName.Substring(0, backtick) : typeName;
		}

		const MethodAttributes MethodAttributesToEraseInOveride = MethodAttributes.Abstract | MethodAttributes.ReservedMask;

		/// <summary>�w�肳�ꂽ���\�b�h���I�[�o�[���C�h���郁�\�b�h���w�肳�ꂽ <see cref="TypeBuilder"/> �ɒ�`���āA���\�b�h�𐶐����� <see cref="MethodBuilder"/> ��Ԃ��܂��B</summary>
		/// <param name="typeBuilder">�I�[�o�[���C�h���`����^������ <see cref="TypeBuilder"/> ���w�肵�܂��B</param>
		/// <param name="additionalAttribute">���\�b�h �I�[�o�[���C�h�ɗ^����ǉ��̃��\�b�h�������w�肵�܂��B���̑�����u�����鑮�������݂��܂��B</param>
		/// <param name="baseMethod">��`���郁�\�b�h�ɂ���ăI�[�o�[���C�h����郁�\�b�h���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ���\�b�h���I�[�o�[���C�h���郁�\�b�h�𐶐����� <see cref="MethodBuilder"/>�B</returns>
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

		/// <summary>�w�肳�ꂽ���\�b�h�̃V�O�l�`�����w�肳�ꂽ <see cref="MethodBuilder"/> �ɃR�s�[���܂��B</summary>
		/// <param name="from">�V�O�l�`���̃R�s�[�����\�b�h���w�肵�܂��B</param>
		/// <param name="to">�V�O�l�`���̃R�s�[�惁�\�b�h�𐶐����� <see cref="MethodBuilder"/> ���w�肵�܂��B</param>
		/// <param name="replaceDeclaringType">�V�O�l�`�����R�s�[����ۂ� <see cref="P:System.Reflection.MethodInfo.DeclaringType"/> ��u�������邩�ǂ����������l���w�肵�܂��B</param>
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
