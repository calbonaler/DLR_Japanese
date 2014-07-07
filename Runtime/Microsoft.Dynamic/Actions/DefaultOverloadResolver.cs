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
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions
{
	using Ast = Expression;

	/// <summary><see cref="OverloadResolverFactory"/> �̊���̎�����\���܂��B</summary>
	sealed class DefaultOverloadResolverFactory : OverloadResolverFactory
	{
		DefaultBinder _binder;

		/// <summary>�w�肳�ꂽ <see cref="DefaultBinder"/> ���g�p���āA<see cref="Microsoft.Scripting.Actions.DefaultOverloadResolverFactory"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="binder">�쐬����� <see cref="DefaultOverloadResolver"/> �ɓK�p����� <see cref="DefaultBinder"/> ���w�肵�܂��B</param>
		public DefaultOverloadResolverFactory(DefaultBinder binder)
		{
			Assert.NotNull(binder);
			_binder = binder;
		}

		/// <summary>�w�肳�ꂽ��������ьĂяo���V�O�l�`�����g�p���ĐV���� <see cref="Microsoft.Scripting.Actions.DefaultOverloadResolver"/> ���쐬���܂��B</summary>
		/// <param name="args">�I�[�o�[���[�h�����̑ΏۂƂȂ�����̃��X�g���w�肵�܂��B</param>
		/// <param name="signature">�I�[�o�[���[�h���Ăяo���V�O�l�`�����w�肵�܂��B</param>
		/// <param name="callType">�I�[�o�[���[�h���Ăяo�����@���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ��������уV�O�l�`���ɑ΂���I�[�o�[���[�h���������� <see cref="DefaultOverloadResolver"/>�B</returns>
		public override DefaultOverloadResolver CreateOverloadResolver(IList<DynamicMetaObject> args, CallSignature signature, CallTypes callType) { return new DefaultOverloadResolver(_binder, args, signature, callType); }
	}

	/// <summary><see cref="OverloadResolver"/> �ɑ΂������̎�����\���܂��B</summary>
	public class DefaultOverloadResolver : OverloadResolver
	{
		// the first argument is "self" if CallType is ImplicitInstance
		// (TODO: it might be better to change the signature)
		DynamicMetaObject _invalidSplattee;
		static readonly DefaultOverloadResolverFactory _factory = new DefaultOverloadResolverFactory(DefaultBinder.Instance);

		// instance method call:
		/// <summary>�C���X�^���X���\�b�h�Ăяo���ɑ΂��� <see cref="Microsoft.Scripting.Actions.DefaultOverloadResolver"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="binder">�o�C���f�B���O�����s���� <see cref="ActionBinder"/> ���w�肵�܂��B</param>
		/// <param name="instance">���\�b�h�Ăяo���̃C���X�^���X��\�� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <param name="args">���\�b�h�Ăяo���̎�������\�� <see cref="DynamicMetaObject"/> �̃��X�g���w�肵�܂��B</param>
		/// <param name="signature">�I�[�o�[���[�h�̃V�O�l�`�����w�肵�܂��B</param>
		public DefaultOverloadResolver(ActionBinder binder, DynamicMetaObject instance, IList<DynamicMetaObject> args, CallSignature signature) : this(binder, ArrayUtils.Insert(instance, args), signature, CallTypes.ImplicitInstance) { }

		// method call:
		/// <summary>�ÓI���\�b�h�Ăяo���ɑ΂��� <see cref="Microsoft.Scripting.Actions.DefaultOverloadResolver"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="binder">�o�C���f�B���O�����s���� <see cref="ActionBinder"/> ���w�肵�܂��B</param>
		/// <param name="args">���\�b�h�Ăяo���̎�������\�� <see cref="DynamicMetaObject"/> �̃��X�g���w�肵�܂��B</param>
		/// <param name="signature">�I�[�o�[���[�h�̃V�O�l�`�����w�肵�܂��B</param>
		public DefaultOverloadResolver(ActionBinder binder, IList<DynamicMetaObject> args, CallSignature signature) : this(binder, args, signature, CallTypes.None) { }

		/// <summary>��ʂ̃��\�b�h�Ăяo���ɑ΂��� <see cref="Microsoft.Scripting.Actions.DefaultOverloadResolver"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="binder">�o�C���f�B���O�����s���� <see cref="ActionBinder"/> ���w�肵�܂��B</param>
		/// <param name="args">���\�b�h�Ăяo���̎�������\�� <see cref="DynamicMetaObject"/> �̃��X�g���w�肵�܂��B</param>
		/// <param name="signature">�I�[�o�[���[�h�̃V�O�l�`�����w�肵�܂��B</param>
		/// <param name="callType">�I�[�o�[���[�h���Ăяo�����@���w�肵�܂��B</param>
		public DefaultOverloadResolver(ActionBinder binder, IList<DynamicMetaObject> args, CallSignature signature, CallTypes callType) : base(binder)
		{
			ContractUtils.RequiresNotNullItems(args, "args");
			Debug.Assert((callType == CallTypes.ImplicitInstance ? 1 : 0) + signature.ArgumentCount == args.Count);
			Arguments = args;
			Signature = signature;
			CallType = callType;
		}

		/// <summary><see cref="DefaultOverloadResolver"/> ���쐬������@��\�� <see cref="OverloadResolverFactory"/> ���擾���܂��B</summary>
		public static OverloadResolverFactory Factory { get { return _factory; } }

		/// <summary>���� <see cref="DefaultOverloadResolver"/> �̑ΏۂƂȂ�I�[�o�[���[�h�̃V�O�l�`�����擾���܂��B</summary>
		public CallSignature Signature { get; private set; }

		/// <summary>���� <see cref="DefaultOverloadResolver"/> ���Ăяo���I�[�o�[���[�h�ɓn���������̃��X�g���擾���܂��B</summary>
		public IList<DynamicMetaObject> Arguments { get; private set; }

		/// <summary>���� <see cref="DefaultOverloadResolver"/> ���I�[�o�[���[�h���Ăяo�����@���擾���܂��B</summary>
		public CallTypes CallType { get; private set; }

		/// <summary>�����̃o�C���f�B���O�̑O�ɌĂяo����܂��B</summary>
		/// <param name="mapping">�}�b�s���O�̑ΏۂƂȂ� <see cref="ParameterMapping"/> �I�u�W�F�N�g�B</param>
		/// <returns>
		/// �����������̃��\�b�h�ɂ���ă}�b�s���O���ꂽ���ǂ����������r�b�g�}�X�N�B
		/// ����̃r�b�g�}�X�N�͎c��̉������ɑ΂��č\�z����܂��B(�r�b�g�̓N���A����Ă��܂��B)
		/// </returns>
		protected internal override BitArray MapSpecialParameters(ParameterMapping mapping)
		{
			//  CallType        call-site   m static                  m instance         m operator/extension
			//  implicit inst.  T.m(a,b)    Ast.Call(null, [a, b])    Ast.Call(a, [b])   Ast.Call(null, [a, b])   
			//  none            a.m(b)      Ast.Call(null, [b])       Ast.Call(a, [b])   Ast.Call(null, [a, b])
			if (!mapping.Overload.IsStatic)
			{
				mapping.AddParameter(new ParameterWrapper(null, mapping.Overload.DeclaringType, null, ParameterBindingFlags.ProhibitNull | (CallType == CallTypes.ImplicitInstance ? ParameterBindingFlags.IsHidden : 0)));
				mapping.AddInstanceBuilder(new InstanceBuilder(mapping.ArgIndex));
			}
			return null;
		}

		/// <summary>�������������ł��� 2 �̌����r���܂��B</summary>
		/// <param name="one">��r���� 1 �Ԗڂ̓K�p�\�Ȍ����w�肵�܂��B</param>
		/// <param name="two">��r���� 2 �Ԗڂ̓K�p�\�Ȍ����w�肵�܂��B</param>
		/// <returns>�ǂ���̌�₪�I�����ꂽ���A���邢�͊��S�ɓ����������� <see cref="Candidate"/>�B</returns>
		protected override Candidate CompareEquivalentCandidates(ApplicableCandidate one, ApplicableCandidate two)
		{
			var result = base.CompareEquivalentCandidates(one, two);
			if (result.Chosen())
				return result;
			if (one.Method.Overload.IsStatic && !two.Method.Overload.IsStatic)
				return CallType == CallTypes.ImplicitInstance ? Candidate.Two : Candidate.One;
			else if (!one.Method.Overload.IsStatic && two.Method.Overload.IsStatic)
				return CallType == CallTypes.ImplicitInstance ? Candidate.One : Candidate.Two;
			return Candidate.Equivalent;
		}

		#region Actual Arguments

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�ɂ���������̒l���擾���܂��B�C���X�^���X���\�b�h�Ăяo���ɑ΂���Öق̈������l������܂��B</summary>
		/// <param name="index">�擾����������̈ʒu������ 0 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�C���f�b�N�X�ɂ���������̒l�B</returns>
		DynamicMetaObject GetArgument(int index) { return Arguments[(CallType == CallTypes.ImplicitInstance ? 1 : 0) + index]; }

		/// <summary>���� <see cref="OverloadResolver"/> �ɓn����閼�O�t���������擾���܂��B</summary>
		/// <param name="namedArgs">���O�t�������̒l���i�[���郊�X�g�B</param>
		/// <param name="argNames">���O�t�������̖��O���i�[���郊�X�g�B</param>
		protected override void GetNamedArguments(out IList<DynamicMetaObject> namedArgs, out IList<string> argNames)
		{
			if (Signature.HasNamedArgument() || Signature.HasDictionaryArgument())
			{
				var objects = new List<DynamicMetaObject>();
				var names = new List<string>();
				for (int i = 0; i < Signature.ArgumentCount; i++)
				{
					if (Signature.GetArgumentKind(i) == ArgumentType.Named)
					{
						objects.Add(GetArgument(i));
						names.Add(Signature.GetArgumentName(i));
					}
				}
				if (Signature.HasDictionaryArgument())
				{
					if (objects == null)
					{
						objects = new List<DynamicMetaObject>();
						names = new List<string>();
					}
					SplatDictionaryArgument(names, objects);
				}
				names.TrimExcess();
				objects.TrimExcess();
				argNames = names;
				namedArgs = objects;
			}
			else
			{
				argNames = ArrayUtils.EmptyStrings;
				namedArgs = DynamicMetaObject.EmptyMetaObjects;
			}
		}

		/// <summary>�w�肳�ꂽ���O�t�������ƓW�J���ꂽ�����Ɋւ����񂩂� <see cref="ActualArguments"/> ���쐬���܂��B</summary>
		/// <param name="namedArgs">���O�t�������̒l���i�[���郊�X�g���w�肵�܂��B</param>
		/// <param name="argNames">���O�t�������̖��O���i�[���郊�X�g���w�肵�܂��B</param>
		/// <param name="preSplatLimit">���ۂ̈������œW�J�L���ɐ�s���đ��݂��Ȃ���΂Ȃ�Ȃ������̍ŏ������w�肵�܂��B</param>
		/// <param name="postSplatLimit">���ۂ̈������œW�J�L���Ɍ㑱���đ��݂��Ȃ���΂Ȃ�Ȃ������̍ŏ������w�肵�܂��B</param>
		/// <returns>�쐬���ꂽ <see cref="ActualArguments"/>�B�������\�z����Ȃ����I�[�o�[���[�h�������G���[�𐶐������ꍇ�� <c>null</c> ���Ԃ���܂��B</returns>
		protected override ActualArguments CreateActualArguments(IList<DynamicMetaObject> namedArgs, IList<string> argNames, int preSplatLimit, int postSplatLimit)
		{
			var res = new List<DynamicMetaObject>();
			if (CallType == CallTypes.ImplicitInstance)
				res.Add(Arguments[0]);
			for (int i = 0; i < Signature.ArgumentCount; i++)
			{
				var arg = GetArgument(i);
				switch (Signature.GetArgumentKind(i))
				{
					case ArgumentType.Simple:
					case ArgumentType.Instance:
						res.Add(arg);
						break;
					case ArgumentType.List:
						// TODO: lazy splat
						IList<object> list = arg.Value as IList<object>;
						if (list == null)
						{
							_invalidSplattee = arg;
							return null;
						}
						for (int j = 0; j < list.Count; j++)
							res.Add(DynamicMetaObject.Create(list[j],
								Ast.Call(
									Ast.Convert(
										arg.Expression,
										typeof(IList<object>)
									),
									typeof(IList<object>).GetMethod("get_Item"),
									AstUtils.Constant(j)
								)
							));
						break;
					case ArgumentType.Named:
					case ArgumentType.Dictionary:
						// already processed
						break;
					default:
						throw new NotImplementedException();
				}
			}
			res.TrimExcess();
			return new ActualArguments(res, namedArgs, argNames, CallType == CallTypes.ImplicitInstance ? 1 : 0, 0, -1, -1);
		}

		void SplatDictionaryArgument(IList<string> splattedNames, IList<DynamicMetaObject> splattedArgs)
		{
			Assert.NotNull(splattedNames, splattedArgs);
			DynamicMetaObject dictMo = GetArgument(Signature.ArgumentCount - 1);
			foreach (DictionaryEntry de in (IDictionary)dictMo.Value)
			{
				if (de.Key is string)
				{
					splattedNames.Add((string)de.Key);
					splattedArgs.Add(
						DynamicMetaObject.Create(de.Value,
							Ast.Call(
								AstUtils.Convert(dictMo.Expression, typeof(IDictionary)),
								typeof(IDictionary).GetMethod("get_Item"),
								AstUtils.Constant((string)de.Key)
							)
						)
					);
				}
			}
		}

		/// <summary>�x���W�J���ꂽ���������� <see cref="Expression"/> ���擾���܂��B</summary>
		/// <returns>�x���W�J���ꂽ���������� <see cref="Expression"/>�B</returns>
		protected override Expression GetSplattedExpression() { throw Assert.Unreachable; } // lazy splatting not used:

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�ɂ���x���W�J���ꂽ�����̒l���擾���܂��B</summary>
		/// <param name="index">�擾����x���W�J���ꂽ�����̈ʒu������ 0 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�C���f�b�N�X�ɂ���x���W�J���ꂽ�����̒l�B</returns>
		protected override object GetSplattedItem(int index) { throw Assert.Unreachable; } // lazy splatting not used:

		#endregion

		/// <summary>�w�肳�ꂽ <see cref="BindingTarget"/> ���琳�����Ȃ������Ɋւ���G���[��\�� <see cref="ErrorInfo"/> ���쐬���܂��B</summary>
		/// <param name="target">���s�����o�C���f�B���O��\�� <see cref="BindingTarget"/> ���w�肵�܂��B</param>
		/// <returns>�������Ȃ������Ɋւ���G���[��\�� <see cref="ErrorInfo"/>�B</returns>
		public override ErrorInfo MakeInvalidParametersError(BindingTarget target)
		{
			if (target.Result == BindingResult.InvalidArguments && _invalidSplattee != null)
				return ErrorInfo.FromException(Ast.Call(new Func<string, string, ArgumentTypeException>(BinderOps.InvalidSplatteeError).Method,
					AstUtils.Constant(target.Name),
					AstUtils.Constant(Binder.GetTypeName(_invalidSplattee.GetLimitType()))
				));
			return base.MakeInvalidParametersError(target);
		}
	}
}
