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
using System.Diagnostics;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions
{
	using Ast = Expression;

	public partial class DefaultBinder : ActionBinder
	{
		/// <summary>
		/// �w�肳�ꂽ�������g�p���āA�I�[�o�[���[�h���ꂽ���\�b�h �Z�b�g�ɑ΂���o�C���f�B���O�����s���܂��B
		/// ������ <see cref="CallSignature"/> �I�u�W�F�N�g�ɂ���Ďw�肳�ꂽ�ʂ�ɏ����܂��B
		/// </summary>
		/// <param name="resolver">�I�[�o�[���[�h�̉����ƃ��\�b�h�o�C���f�B���O�Ɏg�p����� <see cref="DefaultOverloadResolver"/> ���w�肵�܂��B</param>
		/// <param name="targets">�Ăяo����郁�\�b�h �Z�b�g���w�肵�܂��B</param>
		/// <returns>�Ăяo���̌��ʂ�\�� <see cref="DynamicMetaObject"/>�B</returns>
		public DynamicMetaObject CallMethod(DefaultOverloadResolver resolver, IList<MethodBase> targets) { return CallMethod(resolver, targets, BindingRestrictions.Empty, null); }

		/// <summary>
		/// �w�肳�ꂽ�������g�p���āA�I�[�o�[���[�h���ꂽ���\�b�h �Z�b�g�ɑ΂���o�C���f�B���O�����s���܂��B
		/// ������ <see cref="CallSignature"/> �I�u�W�F�N�g�ɂ���Ďw�肳�ꂽ�ʂ�ɏ����܂��B
		/// </summary>
		/// <param name="resolver">�I�[�o�[���[�h�̉����ƃ��\�b�h�o�C���f�B���O�Ɏg�p����� <see cref="DefaultOverloadResolver"/> ���w�肵�܂��B</param>
		/// <param name="targets">�Ăяo����郁�\�b�h �Z�b�g���w�肵�܂��B</param>
		/// <param name="name">�^�[�Q�b�g����̖��O�Ɏg�p���郁�\�b�h�̖��O�܂��� <c>null</c> ���w�肵�܂��B</param>
		/// <returns>�Ăяo���̌��ʂ�\�� <see cref="DynamicMetaObject"/>�B</returns>
		public DynamicMetaObject CallMethod(DefaultOverloadResolver resolver, IList<MethodBase> targets, string name) { return CallMethod(resolver, targets, BindingRestrictions.Empty, name); }

		/// <summary>
		/// �w�肳�ꂽ�������g�p���āA�I�[�o�[���[�h���ꂽ���\�b�h �Z�b�g�ɑ΂���o�C���f�B���O�����s���܂��B
		/// ������ <see cref="CallSignature"/> �I�u�W�F�N�g�ɂ���Ďw�肳�ꂽ�ʂ�ɏ����܂��B
		/// </summary>
		/// <param name="resolver">�I�[�o�[���[�h�̉����ƃ��\�b�h�o�C���f�B���O�Ɏg�p����� <see cref="DefaultOverloadResolver"/> ���w�肵�܂��B</param>
		/// <param name="targets">�Ăяo����郁�\�b�h �Z�b�g���w�肵�܂��B</param>
		/// <param name="restrictions">��������� <see cref="DynamicMetaObject"/> �ɑ΂��ēK�p�����ǉ��̃o�C���f�B���O������w�肵�܂��B</param>
		/// <returns>�Ăяo���̌��ʂ�\�� <see cref="DynamicMetaObject"/>�B</returns>
		public DynamicMetaObject CallMethod(DefaultOverloadResolver resolver, IList<MethodBase> targets, BindingRestrictions restrictions) { return CallMethod(resolver, targets, restrictions, null); }

		/// <summary>
		/// �w�肳�ꂽ�������g�p���āA�I�[�o�[���[�h���ꂽ���\�b�h �Z�b�g�ɑ΂���o�C���f�B���O�����s���܂��B
		/// ������ <see cref="CallSignature"/> �I�u�W�F�N�g�ɂ���Ďw�肳�ꂽ�ʂ�ɏ����܂��B
		/// </summary>
		/// <param name="resolver">�I�[�o�[���[�h�̉����ƃ��\�b�h�o�C���f�B���O�Ɏg�p����� <see cref="DefaultOverloadResolver"/> ���w�肵�܂��B</param>
		/// <param name="targets">�Ăяo����郁�\�b�h �Z�b�g���w�肵�܂��B</param>
		/// <param name="restrictions">��������� <see cref="DynamicMetaObject"/> �ɑ΂��ēK�p�����ǉ��̃o�C���f�B���O������w�肵�܂��B</param>
		/// <param name="name">�^�[�Q�b�g����̖��O�Ɏg�p���郁�\�b�h�̖��O�܂��� <c>null</c> ���w�肵�܂��B</param>
		/// <returns>�Ăяo���̌��ʂ�\�� <see cref="DynamicMetaObject"/>�B</returns>
		public DynamicMetaObject CallMethod(DefaultOverloadResolver resolver, IList<MethodBase> targets, BindingRestrictions restrictions, string name)
		{
			BindingTarget target;
			return CallMethod(resolver, targets, restrictions, name, NarrowingLevel.None, NarrowingLevel.All, out target);
		}

		/// <summary>
		/// �w�肳�ꂽ�������g�p���āA�I�[�o�[���[�h���ꂽ���\�b�h �Z�b�g�ɑ΂���o�C���f�B���O�����s���܂��B
		/// ������ <see cref="CallSignature"/> �I�u�W�F�N�g�ɂ���Ďw�肳�ꂽ�ʂ�ɏ����܂��B
		/// </summary>
		/// <param name="minLevel">�I�[�o�[���[�h�̉����Ɏg�p����ŏ��̏k���ϊ����x�����w�肵�܂��B</param>
		/// <param name="maxLevel">�I�[�o�[���[�h�̉����Ɏg�p����ő�̏k���ϊ����x�����w�肵�܂��B</param>
		/// <param name="resolver">�I�[�o�[���[�h�̉����ƃ��\�b�h�o�C���f�B���O�Ɏg�p����� <see cref="DefaultOverloadResolver"/> ���w�肵�܂��B</param>
		/// <param name="targets">�Ăяo����郁�\�b�h �Z�b�g���w�肵�܂��B</param>
		/// <param name="restrictions">��������� <see cref="DynamicMetaObject"/> �ɑ΂��ēK�p�����ǉ��̃o�C���f�B���O������w�肵�܂��B</param>
		/// <param name="target">�G���[���̐����Ɏg�p�ł��錋�ʂƂ��ē�����o�C���f�B���O �^�[�Q�b�g���i�[����ϐ����w�肵�܂��B</param>
		/// <param name="name">�^�[�Q�b�g����̖��O�Ɏg�p���郁�\�b�h�̖��O�܂��� <c>null</c> ���w�肵�܂��B</param>
		/// <returns>�Ăяo���̌��ʂ�\�� <see cref="DynamicMetaObject"/>�B</returns>
		public DynamicMetaObject CallMethod(DefaultOverloadResolver resolver, IList<MethodBase> targets, BindingRestrictions restrictions, string name, NarrowingLevel minLevel, NarrowingLevel maxLevel, out BindingTarget target)
		{
			ContractUtils.RequiresNotNull(resolver, "resolver");
			ContractUtils.RequiresNotNullItems(targets, "targets");
			ContractUtils.RequiresNotNull(restrictions, "restrictions");
			// attempt to bind to an individual method
			if ((target = resolver.ResolveOverload(name ?? (targets[0].IsConstructor ? targets[0].DeclaringType.Name : targets[0].Name), targets, minLevel, maxLevel)).Success)
				return new DynamicMetaObject(
					target.MakeExpression(),
					restrictions.Merge(
						MakeSplatTests(resolver.CallType, resolver.Signature, false, resolver.Arguments).Merge(target.RestrictedArguments.GetAllRestrictions())
					)
				); // if we succeed make the target for the rule
			// make an error rule
			var restriction = MakeSplatTests(resolver.CallType, resolver.Signature, true, resolver.Arguments);
			// restrict to the exact type of all parameters for errors
			for (int i = 0; i < resolver.Arguments.Count; i++)
				resolver.Arguments[i] = resolver.Arguments[i].Restrict(resolver.Arguments[i].GetLimitType());
			return MakeError(
				resolver.MakeInvalidParametersError(target),
				restrictions.Merge(BindingRestrictions.Combine(resolver.Arguments).Merge(restriction)),
				typeof(object)
			);
		}

		/// <summary>�z���������ю��������ɑ΂���e�X�g�𐶐����܂��B</summary>
		static BindingRestrictions MakeSplatTests(CallTypes callType, CallSignature signature, bool testTypes, IList<DynamicMetaObject> args)
		{
			var res = BindingRestrictions.Empty;
			if (signature.HasListArgument())
				res = MakeParamsArrayTest(callType, signature, testTypes, args);
			if (signature.HasDictionaryArgument())
				res = res.Merge(MakeParamsDictionaryTest(args, testTypes));
			return res;
		}

		/// <summary>splat �e�X�g���\�z���鐳�����������擾���܂��B<see cref="MakeParamsTest"/> �����ۂ̃e�X�g���쐬���܂��B</summary>
		static BindingRestrictions MakeParamsArrayTest(CallTypes callType, CallSignature signature, bool testTypes, IList<DynamicMetaObject> args)
		{
			int listIndex = signature.IndexOf(ArgumentType.List);
			Debug.Assert(listIndex != -1);
			if (callType == CallTypes.ImplicitInstance)
				listIndex++;
			return MakeParamsTest(args[listIndex], testTypes);
		}

		/// <summary>
		/// �U�J�������̂���Ăяo���ɑ΂��鐧����\�z���܂��B
		/// �������܂��I�u�W�F�N�g�̃R���N�V�����ł��蓯�����̈����������Ă��邱�Ƃ��m�F���܂��B
		/// </summary>
		static BindingRestrictions MakeParamsTest(DynamicMetaObject splattee, bool testTypes)
		{
			var list = splattee.Value as IList<object>;
			if (list == null)
			{
				if (splattee.Value == null)
					return BindingRestrictions.GetExpressionRestriction(Ast.Equal(splattee.Expression, AstUtils.Constant(null)));
				else
					return BindingRestrictions.GetTypeRestriction(splattee.Expression, splattee.Value.GetType());
			}
			var res = BindingRestrictions.GetExpressionRestriction(
				Ast.AndAlso(
					Ast.TypeIs(splattee.Expression, typeof(IList<object>)),
					Ast.Equal(
						Ast.Property(
							Ast.Convert(splattee.Expression, typeof(IList<object>)),
							typeof(ICollection<object>).GetProperty("Count")
						),
						AstUtils.Constant(list.Count)
					)
				)
			);
			if (testTypes)
			{
				for (int i = 0; i < list.Count; i++)
				{
					res = res.Merge(
						BindingRestrictionsHelpers.GetRuntimeTypeRestriction(
							Ast.Call(
								AstUtils.Convert(splattee.Expression, typeof(IList<object>)),
								typeof(IList<object>).GetMethod("get_Item"),
								AstUtils.Constant(i)
							),
							CompilerHelpers.GetType(list[i])
						)
					);
				}
			}
			return res;
		}

		/// <summary>
		/// �L�[���[�h�����̂���Ăяo���ɑ΂��鐧����\�z���܂��B
		/// ����͎����̌ʂ̃L�[�ɂ��Ă���炪�������O�������Ă��邱�Ƃ��m�F����e�X�g���܂݂܂��B
		/// </summary>
		static BindingRestrictions MakeParamsDictionaryTest(IList<DynamicMetaObject> args, bool testTypes)
		{
			var dict = (IDictionary)args[args.Count - 1].Value;
			// verify the dictionary has the same count and arguments.
			string[] names = new string[dict.Count];
			Type[] types = testTypes ? new Type[dict.Count] : null;
			int index = 0;
			foreach (DictionaryEntry entry in dict)
			{
				var name = entry.Key as string;
				if (name == null)
					throw ScriptingRuntimeHelpers.SimpleTypeError(string.Format("���������ɂ͕����񂪗\������܂����� {0} ���n����܂����B", entry.Key));
				names[index] = name;
				if (types != null)
					types[index] = CompilerHelpers.GetType(entry.Value);
				index++;
			}
			return BindingRestrictions.GetExpressionRestriction(
				Ast.AndAlso(
					Ast.TypeIs(args[args.Count - 1].Expression, typeof(IDictionary)),
					Ast.Call(
						new Func<IDictionary, string[], Type[], bool>(BinderOps.CheckDictionaryMembers).Method,
						Ast.Convert(args[args.Count - 1].Expression, typeof(IDictionary)),
						AstUtils.Constant(names),
						testTypes ? AstUtils.Constant(types) : AstUtils.Constant(null, typeof(Type[]))
					)
				)
			);
		}
	}
}
