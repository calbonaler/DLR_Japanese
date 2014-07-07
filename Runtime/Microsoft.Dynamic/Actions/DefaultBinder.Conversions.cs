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
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions
{
	using Ast = Expression;
	using AstUtils = Microsoft.Scripting.Ast.Utils;

	public partial class DefaultBinder : ActionBinder
	{
		/// <summary>�w�肳�ꂽ <see cref="DynamicMetaObject"/> ���\���l���w�肳�ꂽ�^�ɕϊ����܂��B</summary>
		/// <param name="toType">�ϊ���̌^���w�肵�܂��B</param>
		/// <param name="kind">�ϊ��̎�ނ����� <see cref="ConversionResultKind"/> ���w�肵�܂��B</param>
		/// <param name="arg">�ϊ����̒l��\�� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <returns>�ϊ����ꂽ�l��\�� <see cref="DynamicMetaObject"/>�B��O���X���[���鎮���܂ނ��Ƃ�����܂��B</returns>
		public DynamicMetaObject ConvertTo(Type toType, ConversionResultKind kind, DynamicMetaObject arg) { return ConvertTo(toType, kind, arg, new DefaultOverloadResolverFactory(this)); }

		/// <summary>�w�肳�ꂽ <see cref="DynamicMetaObject"/> ���\���l���w�肳�ꂽ�^�ɕϊ����܂��B</summary>
		/// <param name="toType">�ϊ���̌^���w�肵�܂��B</param>
		/// <param name="kind">�ϊ��̎�ނ����� <see cref="ConversionResultKind"/> ���w�肵�܂��B</param>
		/// <param name="arg">�ϊ����̒l��\�� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <param name="resolverFactory">���̕ϊ�����Ɏg�p����� <see cref="OverloadResolverFactory"/> ���w�肵�܂��B</param>
		/// <returns>�ϊ����ꂽ�l��\�� <see cref="DynamicMetaObject"/>�B��O���X���[���鎮���܂ނ��Ƃ�����܂��B</returns>
		public DynamicMetaObject ConvertTo(Type toType, ConversionResultKind kind, DynamicMetaObject arg, OverloadResolverFactory resolverFactory) { return ConvertTo(toType, kind, arg, resolverFactory, null); }

		/// <summary>�w�肳�ꂽ <see cref="DynamicMetaObject"/> ���\���l���w�肳�ꂽ�^�ɕϊ����܂��B</summary>
		/// <param name="toType">�ϊ���̌^���w�肵�܂��B</param>
		/// <param name="kind">�ϊ��̎�ނ����� <see cref="ConversionResultKind"/> ���w�肵�܂��B</param>
		/// <param name="arg">�ϊ����̒l��\�� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <param name="resolverFactory">���̕ϊ�����Ɏg�p����� <see cref="OverloadResolverFactory"/> ���w�肵�܂��B</param>
		/// <param name="errorSuggestion">�ϊ������s�����ۂɎg�p����� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <returns>�ϊ����ꂽ�l��\�� <see cref="DynamicMetaObject"/>�B��O���X���[���鎮���܂ނ��Ƃ�����܂��B</returns>
		public DynamicMetaObject ConvertTo(Type toType, ConversionResultKind kind, DynamicMetaObject arg, OverloadResolverFactory resolverFactory, DynamicMetaObject errorSuggestion)
		{
			ContractUtils.RequiresNotNull(toType, "toType");
			ContractUtils.RequiresNotNull(arg, "arg");
			var knownType = arg.GetLimitType();
			// try all the conversions - first look for conversions against the expression type,
			// these can be done w/o any additional tests.  Then look for conversions against the restricted type.
			var typeRestrictions = arg.Restrictions.Merge(BindingRestrictionsHelpers.GetRuntimeTypeRestriction(arg.Expression, arg.GetLimitType()));
			DynamicMetaObject res = null;
			if (toType == typeof(object))
				res = arg.Expression.Type.IsValueType ? MakeSimpleConversionTarget(typeof(object), typeRestrictions, arg) : new DynamicMetaObject(arg.Expression, typeRestrictions);
			res = res ?? TryAllConversions(resolverFactory, toType, kind, arg.Expression.Type, typeRestrictions, arg) ??
				TryAllConversions(resolverFactory, toType, kind, arg.GetLimitType(), typeRestrictions, arg) ??
				errorSuggestion ?? MakeErrorTarget(toType, kind, typeRestrictions, arg);
			if ((kind == ConversionResultKind.ExplicitTry || kind == ConversionResultKind.ImplicitTry) && toType.IsValueType)
				res = new DynamicMetaObject(AstUtils.Convert(res.Expression, typeof(object)), res.Restrictions);
			return res;
		}

		#region Conversion attempt helpers

		/// <summary>�����ꂩ�̕ϊ������p�\���ǂ����𒲂ׁA�\�ł���Εϊ��̃^�[�Q�b�g���\�z���܂��B</summary>
		DynamicMetaObject TryAllConversions(OverloadResolverFactory factory, Type toType, ConversionResultKind kind, Type knownType, BindingRestrictions restrictions, DynamicMetaObject arg)
		{
			DynamicMetaObject result = null;
			// known type -> known type
			// MakeSimpleConversionTarget �� ConversionResultKind �̔��f���s���܂�
			if ((toType.IsAssignableFrom(knownType) || knownType == typeof(DynamicNull) && (toType.IsClass || toType.IsInterface)) &&
				(result = MakeSimpleConversionTarget(toType, restrictions, arg)) != null)
				return result;
			// Extensible<T> -> Extensible<T>.Value
			var extensibleType = typeof(Extensible<>).MakeGenericType(toType);
			if (extensibleType.IsAssignableFrom(knownType) &&
				(result = new DynamicMetaObject(Ast.Property(Ast.Convert(arg.Expression, extensibleType), extensibleType.GetProperty("Value")), restrictions)) != null)
				return result;
			result = TryUserDefinedConversion(kind, toType, knownType, restrictions, arg) ??         // op_Implicit
				TryImplicitNumericConversion(toType, knownType, restrictions, arg);          // op_Implicit
			if (result != null)
				return result;
			// null -> Nullable<T> �܂��� T -> Nullable<T>
			if (TypeUtils.IsNullableType(toType))
			{
				if (knownType == typeof(DynamicNull)) // null -> Nullable<T>
					result = new DynamicMetaObject(Ast.Default(toType), restrictions);
				else if (knownType == toType.GetGenericArguments()[0]) // T -> Nullable<T>
					result = new DynamicMetaObject(Ast.New(toType.GetConstructor(new[] { knownType }), AstUtils.Convert(arg.Expression, knownType)), restrictions);
				else if ((kind == ConversionResultKind.ExplicitCast || kind == ConversionResultKind.ExplicitTry) && knownType != typeof(object))
					// �����I�ϊ��̎��s���� int -> Nullable<float> �̂悤�Ȃ��Ƃ��s���܂��B
					result = MakeConvertingToTToNullableOfTTarget(factory, toType, kind, restrictions, arg);
				if (result != null)
					return result;
			}
			// null -> �Q�ƌ^
			if (knownType == typeof(DynamicNull) && !toType.IsValueType)
				return new DynamicMetaObject(AstUtils.Constant(null, toType), restrictions);
			return null;
		}

		/// <summary>�ϊ������[�U�[��`�̕ϊ����\�b�h�ɂ���ăn���h������邩�ǂ����𔻒f���܂��B</summary>
		DynamicMetaObject TryUserDefinedConversion(ConversionResultKind kind, Type toType, Type type, BindingRestrictions restrictions, DynamicMetaObject arg)
		{
			var fromType = GetUnderlyingType(type);
			var res = TryOneConversion(kind, toType, type, fromType, "op_Implicit", true, restrictions, arg) ??
				   TryOneConversion(kind, toType, type, fromType, "ConvertTo" + toType.Name, true, restrictions, arg);
			if (res != null)
				return res;
			if (kind == ConversionResultKind.ExplicitCast || kind == ConversionResultKind.ExplicitTry)
				// finally try explicit conversions
				res = TryOneConversion(kind, toType, type, fromType, "op_Explicit", false, restrictions, arg) ??
					TryOneConversion(kind, toType, type, fromType, "ConvertTo" + toType.Name, false, restrictions, arg);
			return res;
		}

		/// <summary>�ǂꂩ���w�肳�ꂽ�ϊ����\�b�h���`���Ă��邩�𒲂ׂ邽�߂ɗ����̌^�𒲍�����w���p�[���\�b�h�ł��B</summary>
		DynamicMetaObject TryOneConversion(ConversionResultKind kind, Type toType, Type type, Type fromType, string methodName, bool isImplicit, BindingRestrictions restrictions, DynamicMetaObject arg)
		{
			var checkType = GetUnderlyingType(type);
			var item = GetUserDefinedConversion(checkType, toType, GetMember(MemberRequestKind.Convert, fromType, methodName), isImplicit) ??
				GetUserDefinedConversion(checkType, toType, GetMember(MemberRequestKind.Convert, toType, methodName), isImplicit);
			return item != null ? new DynamicMetaObject(
				WrapForThrowingTry(kind, isImplicit, AstUtils.SimpleCallHelper(item, type == checkType ? AstUtils.Convert(arg.Expression, type) : GetExtensibleValue(type, arg)), item.ReturnType),
				restrictions
			) : null;
		}

		static System.Reflection.MethodInfo GetUserDefinedConversion(Type checkType, Type toType, MemberGroup conversions, bool isImplicit)
		{
			var result = conversions.Where(x => x.MemberType == TrackerTypes.Method).Cast<MethodTracker>()
				.Where(x => (!isImplicit || !x.Method.IsDefined(typeof(ExplicitConversionMethodAttribute), true)) && x.Method.ReturnType == toType)
				.Select(x => Tuple.Create(x.Method, x.Method.GetParameters()))
				.FirstOrDefault(x => x.Item2.Length == 1 && x.Item2[0].ParameterType.IsAssignableFrom(checkType));
			return result != null ? result.Item1 : null;
		}

		/// <summary>�v���~�e�B�u�f�[�^�^�ɈÖق̐��l�ϊ������݂��邩�ǂ����𒲂ׂ܂��B</summary>
		static DynamicMetaObject TryImplicitNumericConversion(Type toType, Type type, BindingRestrictions restrictions, DynamicMetaObject arg)
		{
			var checkType = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Extensible<>) ? type.GetGenericArguments()[0] : type;
			// �����I�ȕϊ��𒲂ׂ܂�
			if (TypeUtils.IsNumeric(toType) && TypeUtils.IsNumeric(checkType) && TypeUtils.IsNumericImplicitlyConvertible(Type.GetTypeCode(checkType), Type.GetTypeCode(toType)))
				return type == checkType ? MakeSimpleConversionTarget(toType, restrictions, arg) :
					new DynamicMetaObject(AstUtils.Convert(GetExtensibleValue(typeof(Extensible<>).MakeGenericType(toType), arg), toType), restrictions);
			return null;
		}

		#endregion

		#region Rule production helpers

		/// <summary>�ϊ����N����Ȃ������Ƃ��ɃG���[�𐶐�����w���p�[���\�b�h�ł��B</summary>
		DynamicMetaObject MakeErrorTarget(Type toType, ConversionResultKind kind, BindingRestrictions restrictions, DynamicMetaObject arg)
		{
			switch (kind)
			{
				case ConversionResultKind.ImplicitCast:
				case ConversionResultKind.ExplicitCast:
					return MakeError(MakeConversionError(toType, arg.Expression), restrictions, toType);
				case ConversionResultKind.ImplicitTry:
				case ConversionResultKind.ExplicitTry:
					return new DynamicMetaObject(GetTryConvertReturnValue(toType), restrictions);
				default:
					throw new InvalidOperationException(kind.ToString());
			}
		}

		/// <summary>�����I�ϊ����삪��O���X���[����ꍇ�ɂ���� try/catch �ň݂͂܂��B��O���X���[���ꂽ�ꍇ�͊���l���Ԃ���܂��B</summary>
		static Expression WrapForThrowingTry(ConversionResultKind kind, bool isImplicit, Expression ret, Type retType)
		{
			if (!isImplicit && kind == ConversionResultKind.ExplicitTry)
			{
				var convFailed = GetTryConvertReturnValue(retType);
				var tmp = Ast.Variable(convFailed.Type == typeof(object) ? typeof(object) : ret.Type, "tmp");
				ret = Ast.Block(new[] { tmp },
					AstUtils.Try(Ast.Assign(tmp, AstUtils.Convert(ret, tmp.Type)))
					.Catch(typeof(Exception), Ast.Assign(tmp, convFailed)),
					tmp
				);
			}
			return ret;
		}

		/// <summary>
		/// �ǂ̕ϊ����v������Ȃ����ɋK���𐶐�����w���p�[���\�b�h�ł��B
		/// (�����͂̋����^�͕ϊ�����^�������� IL ���x���ňÖٓI�ȕϊ��������Ă���^�Ɉ�v���܂��B)
		/// </summary>
		static DynamicMetaObject MakeSimpleConversionTarget(Type toType, BindingRestrictions restrictions, DynamicMetaObject arg)
		{
			return new DynamicMetaObject(AstUtils.Convert(arg.Expression, CompilerHelpers.GetVisibleType(toType)), restrictions);
			/*
			if (toType.IsValueType && _rule.ReturnType == typeof(object) && Expression.Type == typeof(object)) {
				// boxed value type is being converted back to object.  We've done 
				// the type check, there's no need to unbox & rebox the value.  infact 
				// it breaks calls on instance methods so we need to avoid it.
				_rule.Target =
					_rule.MakeReturn(
						Binder,
						Expression
					);
			} 
			*/
		}

		/// <summary>T �� Nullable(T) �ɕϊ�����K���𐶐�����w���p�[���\�b�h�ł��B</summary>
		DynamicMetaObject MakeConvertingToTToNullableOfTTarget(OverloadResolverFactory resolverFactory, Type toType, ConversionResultKind kind, BindingRestrictions restrictions, DynamicMetaObject arg)
		{
			var valueType = toType.GetGenericArguments()[0];
			// ConvertSelfToT -> Nullable<T>
			if (kind == ConversionResultKind.ExplicitCast)
				// T �ւ̕ϊ������s����΃X���[���邾��
				return new DynamicMetaObject(Ast.New(toType.GetConstructor(new[] { valueType }), ConvertExpression(arg.Expression, valueType, kind, resolverFactory)), restrictions);
			else
			{
				// T �ւ̕ϊ������������ Nullable<T> �𐶐����A�����łȂ���� default(retType) ��Ԃ��B
				var tmp = Ast.Variable(typeof(object), "tmp");
				return new DynamicMetaObject(
					Ast.Block(new[] { tmp },
						Ast.Condition(Ast.NotEqual(Ast.Assign(tmp, ConvertExpression(arg.Expression, valueType, kind, resolverFactory)), AstUtils.Constant(null)),
							Ast.New(toType.GetConstructor(new Type[] { valueType }), Ast.Convert(tmp, valueType)),
							GetTryConvertReturnValue(toType)
						)
					), restrictions
				);
			}
		}

		/// <summary><see cref="ConversionResultKind.ImplicitTry"/> �܂��� <see cref="ConversionResultKind.ExplicitTry"/> �̂Ƃ��ϊ������s�����ۂɕԂ����l��\�� <see cref="Expression"/> ���擾���܂��B</summary>
		/// <param name="type">�Ԃ����l���\���^���w�肵�܂��B</param>
		/// <returns>�^���N���X�܂��̓C���^�[�t�F�C�X�̏ꍇ�͂��̌^�� <c>null</c> ���A����ȊO�̏ꍇ�� <c>(object)null</c> ��\�� <see cref="Expression"/> ��Ԃ��܂��B</returns>
		public static Expression GetTryConvertReturnValue(Type type) { return type.IsInterface || type.IsClass ? AstUtils.Constant(null, type) : AstUtils.Constant(null); }

		/// <summary>�ϊ�����鎮���� Extensible(T) �̒l�����o���܂��B</summary>
		static Expression GetExtensibleValue(Type extType, DynamicMetaObject arg) { return Ast.Property(AstUtils.Convert(arg.Expression, extType), extType.GetProperty("Value")); }

		/// <summary>
		/// <paramref name="fromType"/> �� Extensible(T) �܂��� Extensible(T) �̃T�u�N���X�ł���� T ��Ԃ��܂��B����ȊO�̏ꍇ�� fromType ��Ԃ��܂��B
		/// ����͊g���^�����̂��ƂɂȂ�^�Ɠ���Ɉ��������ꍇ�Ɏg�p����܂��B
		/// </summary>
		static Type GetUnderlyingType(Type fromType)
		{
			var curType = fromType;
			do
			{
				if (curType.IsGenericType && curType.GetGenericTypeDefinition() == typeof(Extensible<>))
					fromType = curType.GetGenericArguments()[0];
			} while ((curType = curType.BaseType) != null);
			return fromType;
		}

		#endregion
	}
}
