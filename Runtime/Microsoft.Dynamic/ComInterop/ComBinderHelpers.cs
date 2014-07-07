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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.Scripting.ComInterop
{
	static class ComBinderHelpers
	{
		internal static bool PreferPut(Type type, bool holdsNull)
		{
			Debug.Assert(type != null);
			return type.IsValueType || type.IsArray || type == typeof(String) || type == typeof(DBNull) || holdsNull || type == typeof(System.Reflection.Missing) || type == typeof(CurrencyWrapper);
		}

		internal static bool IsByRef(DynamicMetaObject mo)
		{
			var pe = mo.Expression as ParameterExpression;
			return pe != null && pe.IsByRef;
		}

		internal static bool IsStrongBoxArg(DynamicMetaObject o) { return o.LimitType.IsGenericType && o.LimitType.GetGenericTypeDefinition() == typeof(StrongBox<>); }

		/// <summary>���̃w���p�[�� ByVal StrongBox ���������̈����� Value �t�B�[���h��\�� ByRef ���ɕϊ����邱�Ƃ� COM �o�C���f�B���O�̈������������܂��B</summary>
		internal static bool[] ProcessArgumentsForCom(ref DynamicMetaObject[] args)
		{
			Debug.Assert(args != null);
			DynamicMetaObject[] newArgs = new DynamicMetaObject[args.Length];
			bool[] isByRefArg = new bool[args.Length];
			for (int i = 0; i < args.Length; i++)
			{
				// ���̒l�ɐV������������ݒ肷�邩�A����̒l��ݒ肵�܂��B
				// ���̏C�����s�����ƂŁACOM �o�C���_�[�ɏ�Ɉ�����񂪑��݂��邱�Ƃ�����ł��܂��B
				if (IsByRef(args[i]))
				{
					newArgs[i] = args[i];
					isByRefArg[i] = true;
				}
				else if (IsStrongBoxArg(args[i]))
				{
					// ���̈����� LimitType �Ő��񂵂��̂ŕϊ��ł��A�ϊ��͊ȒP�ȃL���X�g�ōςށB
					var value = args[i].Value as IStrongBox;
					newArgs[i] = new DynamicMetaObject(
						Expression.Field(Ast.Utils.Convert(args[i].Expression, args[i].LimitType), args[i].LimitType.GetField("Value")),
						args[i].Restrictions.Merge(GetTypeRestrictionForDynamicMetaObject(args[i])),
						value != null ? value.Value : null
					);
					isByRefArg[i] = true;
				}
				else
				{
					newArgs[i] = args[i];
					isByRefArg[i] = false;
				}
			}
			args = newArgs;
			return isByRefArg;
		}

		internal static BindingRestrictions GetTypeRestrictionForDynamicMetaObject(DynamicMetaObject obj)
		{
			// DynamicMetaObject �� null �������Ă���΁Anull ���`�F�b�N����C���X�^���X������쐬����B
			return obj.Value == null && obj.HasValue ?
				BindingRestrictions.GetInstanceRestriction(obj.Expression, null) :
				BindingRestrictions.GetTypeRestriction(obj.Expression, obj.LimitType);
		}
	}
}