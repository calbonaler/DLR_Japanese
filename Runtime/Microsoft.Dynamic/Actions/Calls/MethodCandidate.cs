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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Contracts;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Interpreter;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions.Calls
{
	using Ast = Expression;

	/// <summary>
	/// ���\�b�h�܂��̓��\�b�h�I�[�o�[���[�h���Ăяo�����肤�邳�܂��܂ȕ��@��\���܂��B
	/// ���ꂼ��� <see cref="MethodCandidate"/> �� <see cref="ParameterWrapper"/> ���g�p���Č��ɑ΂�������̌^��\���܂��B
	/// </summary>
	/// <remarks>
	/// �P��̃��\�b�h�ł������� <see cref="MethodCandidate"/> �𐶐�����ꍇ������܂��B���ɗ��R�������܂��B
	/// - ���ꂼ��̏ȗ��\�Ȉ����܂��͊���l�̂�����������ɂȂ�܂��B
	/// - ref �܂��� out �����̑��݂͌���ɑ΂��ĕԖߒl�Ƃ��čX�V���ꂽ�l��Ԃ�����ǉ����܂��B
	/// - <see cref="ArgumentType.List"/> �܂��� <see cref="ArgumentType.Dictionary"/> �̓��X�g������قȂ�ꍇ�A�V�������ɂȂ�܂��B
	/// </remarks>
	public sealed class MethodCandidate
	{
		ParameterWrapper _paramsDict;
		InstanceBuilder _instanceBuilder;

		/// <summary>�w�肳�ꂽ�������g�p���āA<see cref="Microsoft.Scripting.Actions.Calls.MethodCandidate"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="resolver">���\�b�h�Ăяo���̐������ɃI�[�o�[���[�h�̉����Ɏg�p���� <see cref="OverloadResolver"/> ���w�肵�܂��B</param>
		/// <param name="method">���\�b�h��\�� <see cref="OverloadInfo"/> ���w�肵�܂��B</param>
		/// <param name="parameters">���\�b�h�̉������̃��X�g���w�肵�܂��B</param>
		/// <param name="paramsDict">���\�b�h�̎����������w�肵�܂��B</param>
		/// <param name="returnBuilder">�Ԗߒl�̐����Ɏg�p���� <see cref="Microsoft.Scripting.Actions.Calls.ReturnBuilder"/> ���w�肵�܂��B</param>
		/// <param name="instanceBuilder">���\�b�h�Ăяo���̑Ώۂ̃C���X�^���X�̐����Ɏg�p���� <see cref="InstanceBuilder"/> ���w�肵�܂��B</param>
		/// <param name="argBuilders">�����̐����Ɏg�p���� <see cref="Microsoft.Scripting.Actions.Calls.ArgBuilder"/> �̃��X�g���w�肵�܂��B</param>
		/// <param name="restrictions">�I�[�o�[���[�h�������Ɏg�p�������� <see cref="DynamicMetaObject"/> �ɑ΂���ǉ��̃o�C���f�B���O�����ێ�����f�B�N�V���i�����w�肵�܂��B</param>
		internal MethodCandidate(OverloadResolver resolver, OverloadInfo method, IList<ParameterWrapper> parameters, ParameterWrapper paramsDict, ReturnBuilder returnBuilder, InstanceBuilder instanceBuilder, IList<ArgBuilder> argBuilders, Dictionary<DynamicMetaObject, BindingRestrictions> restrictions)
		{
			Assert.NotNull(resolver, method, instanceBuilder, returnBuilder);
			Assert.NotNullItems(parameters);
			Assert.NotNullItems(argBuilders);
			Resolver = resolver;
			Overload = method;
			_instanceBuilder = instanceBuilder;
			ArgBuilders = argBuilders;
			ReturnBuilder = returnBuilder;
			Parameters = new ReadOnlyCollection<ParameterWrapper>(parameters);
			_paramsDict = paramsDict;
			Restrictions = restrictions;
			ParamsArrayIndex = parameters.FindIndex(x => x.IsParamArray);
		}

		/// <summary>���̃��\�b�h��₪�ΏۂƂ��郁�\�b�h���w�肳�ꂽ���\�b�h�ɒu���������V���� <see cref="MethodCandidate"/> ���쐬���܂��B</summary>
		/// <param name="newMethod">�V�������\�b�h��\�� <see cref="OverloadInfo"/> ���w�肵�܂��B</param>
		/// <param name="parameters">�V�������\�b�h�ɑ΂��鉼�����̃��X�g���w�肵�܂��B</param>
		/// <param name="argBuilders">���\�b�h�Ăяo���̐����Ɏg�p���� <see cref="Microsoft.Scripting.Actions.Calls.ArgBuilder"/> �̃��X�g���w�肵�܂��B</param>
		/// <param name="restrictions">�I�[�o�[���[�h�������Ɏg�p�������� <see cref="DynamicMetaObject"/> �ɑ΂���ǉ��̃o�C���f�B���O�����ێ�����f�B�N�V���i�����w�肵�܂��B</param>
		/// <returns>���̃��\�b�h���̑Ώۃ��\�b�h���w�肳�ꂽ���\�b�h�ɒu���������V���� <see cref="MethodCandidate"/>�B</returns>
		internal MethodCandidate ReplaceMethod(OverloadInfo newMethod, IList<ParameterWrapper> parameters, IList<ArgBuilder> argBuilders, Dictionary<DynamicMetaObject, BindingRestrictions> restrictions)
		{
			return new MethodCandidate(Resolver, newMethod, parameters, _paramsDict, ReturnBuilder, _instanceBuilder, argBuilders, restrictions);
		}

		/// <summary>�Ԗߒl�̐����Ɏg�p���� <see cref="Microsoft.Scripting.Actions.Calls.ReturnBuilder"/> ���擾���܂��B</summary>
		internal ReturnBuilder ReturnBuilder { get; private set; }

		/// <summary>���\�b�h�Ăяo���̈����̐����Ɏg�p���� <see cref="Microsoft.Scripting.Actions.Calls.ArgBuilder"/> �̃��X�g���擾���܂��B</summary>
		internal IList<ArgBuilder> ArgBuilders { get; private set; }

		/// <summary>���\�b�h�Ăяo���̐������ɃI�[�o�[���[�h�̉����Ɏg�p���� <see cref="OverloadResolver"/> ���擾���܂��B</summary>
		public OverloadResolver Resolver { get; private set; }

		/// <summary>�ΏۂƂȂ�I�[�o�[���[�h��\�� <see cref="OverloadInfo"/> ���擾���܂��B</summary>
		public OverloadInfo Overload { get; private set; }

		/// <summary>�I�[�o�[���[�h�������Ɏg�p�������� <see cref="DynamicMetaObject"/> �ɑ΂���ǉ��̃o�C���f�B���O�����ێ�����f�B�N�V���i�����擾���܂��B</summary>
		internal Dictionary<DynamicMetaObject, BindingRestrictions> Restrictions { get; private set; }

		/// <summary>���̃��\�b�h���̕Ԗߒl�̌^���擾���܂��B</summary>
		public Type ReturnType { get { return ReturnBuilder.ReturnType; } }

		/// <summary>���̃��\�b�h���̈������X�g���ł̔z������̈ʒu�������C���f�b�N�X���擾���܂��B</summary>
		public int ParamsArrayIndex { get; private set; }

		/// <summary>���̃��\�b�h���̈������X�g���ɔz����������݂��邩�ǂ����������l���擾���܂��B</summary>
		public bool HasParamsArray { get { return ParamsArrayIndex != -1; } }

		/// <summary>���̃��\�b�h���̈������X�g���Ɏ������������݂��邩�ǂ����������l���擾���܂��B</summary>
		public bool HasParamsDictionary { get { return _paramsDict != null; } }

		/// <summary>�w�肳�ꂽ���O�̈����ɑ΂���������X�g���ł̈ʒu�������C���f�b�N�X��Ԃ��܂��B</summary>
		/// <param name="name">�����̖��O���w�肵�܂��B</param>
		/// <returns>�������X�g���ł̈����̈ʒu�������C���f�b�N�X�B</returns>
		internal int IndexOfParameter(string name) { return Parameters.FindIndex(x => x.Name == name); }

		/// <summary>���ł�������̐����擾���܂��B</summary>
		public int VisibleParameterCount { get { return Parameters.Count(x => !x.IsHidden); } }

		/// <summary>�����̓ǂݎ���p�̃��X�g���擾���܂��B</summary>
		public ReadOnlyCollection<ParameterWrapper> Parameters { get; private set; }

		/// <summary>
		/// �w�肳�ꂽ���̈����ƃL�[���[�h�������Ƃ�V���� <see cref="MethodCandidate"/> ���쐬���܂��B
		/// ��{�I�ȍl���͂ǂ̈������ʏ�̈����܂��͎��������Ɋ��蓖�Ă��邩���v�Z���A�����̏ꏊ��]���� <see cref="ParameterWrapper"/> �Ŗ��߂邱�Ƃł��B
		/// </summary>
		/// <param name="count">�����̐����w�肵�܂��B</param>
		/// <param name="names">�L�[���[�h�����̖��O���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ���̈����ƃL�[���[�h�������Ƃ�V���� <see cref="MethodCandidate"/>�B</returns>
		internal MethodCandidate MakeParamsExtended(int count, IEnumerable<string> names)
		{
			Debug.Assert(Overload.IsVariadic);

			List<ParameterWrapper> newParameters = new List<ParameterWrapper>(count);

			// keep track of which named args map to a real argument, and which ones map to the params dictionary.
			var unusedNames = names.Select((x, i) => Tuple.Create(x, i)).ToList();

			// if we don't have a param array we'll have a param dict which is type object
			ParameterWrapper paramsArrayParameter = null;
			int paramsArrayIndex = -1;

			for (int i = 0; i < Parameters.Count; i++)
			{
				if (Parameters[i].IsParamArray)
				{
					paramsArrayParameter = Parameters[i];
					paramsArrayIndex = i;
				}
				else
				{
					int j = unusedNames.FindIndex(x => x.Item1 == Parameters[i].Name);
					if (j != -1)
						unusedNames.RemoveAt(j);
					newParameters.Add(Parameters[i]);
				}
			}

			if (paramsArrayIndex != -1)
			{
				ParameterWrapper expanded = paramsArrayParameter.Expand();
				while (newParameters.Count < count - unusedNames.Count)
					newParameters.Insert(System.Math.Min(paramsArrayIndex, newParameters.Count), expanded);
			}

			if (_paramsDict != null)
				newParameters.AddRange(unusedNames.Select(x => new ParameterWrapper(_paramsDict.ParameterInfo, typeof(object), x.Item1, (Overload.ProhibitsNullItems(_paramsDict.ParameterInfo.Position) ? ParameterBindingFlags.ProhibitNull : 0) | (_paramsDict.IsHidden ? ParameterBindingFlags.IsHidden : 0))));
			else if (unusedNames.Count != 0)
				// unbound kw args and no where to put them, can't call...
				// TODO: We could do better here because this results in an incorrect arg # error message.
				return null;
			// if we have too many or too few args we also can't call
			if (count != newParameters.Count)
				return null;
			return MakeParamsExtended(unusedNames.Select(x => x.Item1).ToArray(), unusedNames.Select(x => x.Item2).ToArray(), newParameters);
		}

		MethodCandidate MakeParamsExtended(string[] names, int[] nameIndices, List<ParameterWrapper> parameters)
		{
			Debug.Assert(Overload.IsVariadic);
			List<ArgBuilder> newArgBuilders = new List<ArgBuilder>(ArgBuilders.Count);
			// current argument that we consume, initially skip this if we have it.
			int curArg = Overload.IsStatic ? 0 : 1;
			int kwIndex = -1;
			ArgBuilder paramsDictBuilder = null;
			foreach (var ab in ArgBuilders)
			{
				// TODO: define a virtual method on ArgBuilder implementing this functionality:
				SimpleArgBuilder sab = ab as SimpleArgBuilder;
				if (sab != null)
				{
					// we consume one or more incoming argument(s)
					if (sab.IsParamsArray)
					{
						// consume all the extra arguments
						var paramsUsed = parameters.Count - GetConsumedArguments() - names.Length + (Overload.IsStatic ? 1 : 0);
						newArgBuilders.Add(new ParamsArgBuilder(sab.ParameterInfo, sab.Type.GetElementType(), curArg, paramsUsed));
						curArg += paramsUsed;
					}
					else if (sab.IsParamsDict)
					{
						// consume all the kw arguments
						kwIndex = newArgBuilders.Count;
						paramsDictBuilder = sab;
					}
					else
						newArgBuilders.Add(sab.MakeCopy(curArg++)); // consume the argument, adjust its position:
				}
				else if (ab is KeywordArgBuilder)
				{
					newArgBuilders.Add(ab);
					curArg++;
				}
				else
					newArgBuilders.Add(ab); // CodeContext, null, default, etc...  we don't consume an  actual incoming argument.
			}
			if (kwIndex != -1)
				newArgBuilders.Insert(kwIndex, new ParamsDictArgBuilder(paramsDictBuilder.ParameterInfo, curArg, names, nameIndices));
			return new MethodCandidate(Resolver, Overload, parameters, null, ReturnBuilder, _instanceBuilder, newArgBuilders, null);
		}

		int GetConsumedArguments() { return ArgBuilders.Count(x => x is SimpleArgBuilder && !((SimpleArgBuilder)x).IsParamsDict || x is KeywordArgBuilder); }

		/// <summary>�����̌^��z��Ƃ��Ď擾���܂��B</summary>
		/// <returns>���ꂼ��̗v�f�Ɉ����̌^���i�[���ꂽ <see cref="System.Type"/> �^�̔z��B</returns>
		public Type[] GetParameterTypes() { return ArgBuilders.Select(x => x.Type).Where(x => x != null).ToArray(); }

		#region MakeDelegate

		/// <summary>�������ꂽ�������g�p���ă��\�b�h�Ăяo����\���f���Q�[�g���쐬���܂��B</summary>
		/// <param name="restrictedArgs">�������ꂽ�������w�肵�܂��B</param>
		/// <returns>���\�b�h�Ăяo����\���f���Q�[�g�B</returns>
		internal OptimizingCallDelegate MakeDelegate(RestrictedArguments restrictedArgs)
		{
			if (restrictedArgs.HasUntypedRestrictions)
				return null;
			MethodInfo mi = Overload.ReflectionInfo as MethodInfo;
			if (mi == null)
				return null;
			if (IsRestrictedType(mi.GetBaseDefinition().DeclaringType))
				return null; // members of reflection are off limits via reflection in partial trust
			if (ReturnBuilder.CountOutParams > 0)
				return null;
			bool[] hasBeenUsed = new bool[restrictedArgs.Length];
			var builders = ArgBuilders.Select(x => x.ToDelegate(Resolver, restrictedArgs, hasBeenUsed)).ToArray();
			if (builders.Any(x => x == null))
				return null;
			if (_instanceBuilder.HasValue)
				return new Caller(mi, builders, _instanceBuilder.ToDelegate(ref mi, Resolver, restrictedArgs, hasBeenUsed)).CallWithInstance;
			else
				return new Caller(mi, builders, null).Call;
		}

		static bool IsRestrictedType(Type declType) { return declType != null && declType.Assembly == typeof(string).Assembly && (declType.IsSubclassOf(typeof(MemberInfo)) || declType == typeof(IsolatedStorageFile)); }

		sealed class Caller
		{
			readonly Func<object[], object>[] _argBuilders;
			readonly Func<object[], object> _instanceBuilder;
			readonly MethodInfo _mi;
			CallInstruction _caller;
			int _hitCount;

			public Caller(MethodInfo mi, Func<object[], object>[] argBuilders, Func<object[], object> instanceBuilder)
			{
				_mi = mi;
				_argBuilders = argBuilders;
				_instanceBuilder = instanceBuilder;
			}

			public object Call(object[] args, out bool shouldOptimize)
			{
				shouldOptimize = TrackUsage(args);
				try
				{
					if (_caller != null)
						return _caller.Invoke(GetArguments(args));
					return _mi.Invoke(null, GetArguments(args));
				}
				catch (TargetInvocationException tie)
				{
					ExceptionHelpers.UpdateForRethrow(tie.InnerException);
					throw tie.InnerException;
				}
			}

			public object CallWithInstance(object[] args, out bool shouldOptimize)
			{
				shouldOptimize = TrackUsage(args);
				try
				{
					if (_caller != null)
						return _caller.InvokeInstance(_instanceBuilder(args), GetArguments(args));
					return _mi.Invoke(_instanceBuilder(args), GetArguments(args));
				}
				catch (TargetInvocationException tie)
				{
					ExceptionHelpers.UpdateForRethrow(tie.InnerException);
					throw tie.InnerException;
				}
			}

			object[] GetArguments(object[] args) { return Array.ConvertAll(_argBuilders, x => x(args)); }

			bool TrackUsage(object[] args)
			{
				bool shouldOptimize = ++_hitCount > 100;
				if (_caller == null && _hitCount <= 100 && (_hitCount > 5 || Array.IndexOf(args, Missing.Value) >= 0))
					_caller = CallInstruction.Create(_mi);
				return shouldOptimize;
			}
		}

		#endregion

		#region MakeExpression

		/// <summary>�������ꂽ�������g�p���ă��\�b�h�Ăяo����\�� <see cref="Expression"/> ���쐬���܂��B</summary>
		/// <param name="restrictedArgs">�������ꂽ�������w�肵�܂��B</param>
		/// <returns>���\�b�h�Ăяo����\�� <see cref="Expression"/>�B</returns>
		internal Expression MakeExpression(RestrictedArguments restrictedArgs)
		{
			bool[] usageMarkers;
			Expression[] spilledArgs;
			var callArgs = GetArgumentExpressions(restrictedArgs, out usageMarkers, out spilledArgs);
			Expression call;
			var mb = Overload.ReflectionInfo;
			// TODO: make MakeExpression virtual on OverloadInfo?
			if (mb == null)
				throw new InvalidOperationException("Cannot generate an expression for an overload w/o MethodBase");
			var mi = mb as MethodInfo;
			if (mi != null)
			{
				Expression instance = null;
				if (!mi.IsStatic)
				{
					instance = _instanceBuilder.ToExpression(ref mi, Resolver, restrictedArgs, usageMarkers);
					Debug.Assert(instance != null, "Can't skip instance expression");
				}
				if (CompilerHelpers.IsVisible(mi))
					call = AstUtils.SimpleCallHelper(instance, mi, callArgs);
				else
					call = Ast.Call(new Func<MethodBase, object, object[], object>(BinderOps.InvokeMethod).Method, AstUtils.Constant(mi),
						instance != null ? AstUtils.Convert(instance, typeof(object)) : AstUtils.Constant(null),
						AstUtils.NewArrayHelper(typeof(object), callArgs)
					);
			}
			else
			{
				ConstructorInfo ci = (ConstructorInfo)mb;
				if (CompilerHelpers.IsVisible(ci))
					call = AstUtils.SimpleNewHelper(ci, callArgs);
				else
					call = Ast.Call(new Func<ConstructorInfo, object[], object>(BinderOps.InvokeConstructor).Method, AstUtils.Constant(ci), AstUtils.NewArrayHelper(typeof(object), callArgs));
			}
			if (spilledArgs != null)
				call = Expression.Block(ArrayUtils.Append(spilledArgs, call));
			var ret = ReturnBuilder.ToExpression(Resolver, ArgBuilders, restrictedArgs, call);
			var updates = ArgBuilders.Select(x => x.UpdateFromReturn(Resolver, restrictedArgs)).Where(x => x != null);
			if (updates.Any())
			{
				if (ret.Type != typeof(void))
				{
					ParameterExpression temp = Ast.Variable(ret.Type, "$ret");
					ret = Ast.Block(new[] { temp }, Enumerable.Repeat(Ast.Assign(temp, ret), 1).Concat(updates).Concat(Enumerable.Repeat(temp, 1)));
				}
				else
					ret = Ast.Block(typeof(void), Enumerable.Repeat(ret, 1).Concat(updates));
			}
			if (Resolver.Temps != null)
				ret = Ast.Block(Resolver.Temps, ret);
			return ret;
		}

		Expression[] GetArgumentExpressions(RestrictedArguments restrictedArgs, out bool[] usageMarkers, out Expression[] spilledArgs)
		{
			int minPriority = int.MaxValue;
			int maxPriority = int.MinValue;
			foreach (ArgBuilder ab in ArgBuilders)
			{
				minPriority = System.Math.Min(minPriority, ab.Priority);
				maxPriority = System.Math.Max(maxPriority, ab.Priority);
			}
			var args = new Expression[ArgBuilders.Count];
			Expression[] actualArgs = null;
			usageMarkers = new bool[restrictedArgs.Length];
			for (int priority = minPriority; priority <= maxPriority; priority++)
			{
				for (int i = 0; i < ArgBuilders.Count; i++)
				{
					if (ArgBuilders[i].Priority == priority)
					{
						args[i] = ArgBuilders[i].ToExpression(Resolver, restrictedArgs, usageMarkers);
						// see if this has a temp that needs to be passed as the actual argument
						Expression byref = ArgBuilders[i].ByRefArgument;
						if (byref != null)
						{
							if (actualArgs == null)
								actualArgs = new Expression[ArgBuilders.Count];
							actualArgs[i] = byref;
						}
					}
				}
			}
			if (actualArgs != null)
			{
				for (int i = 0; i < args.Length; i++)
				{
					if (args[i] != null && actualArgs[i] == null)
						args[i] = Expression.Assign(actualArgs[i] = Resolver.GetTemporary(args[i].Type, null), args[i]);
				}
				spilledArgs = args.Where(x => x != null).ToArray();
				return actualArgs.Where(x => x != null).ToArray();
			}
			spilledArgs = null;
			return args.Where(x => x != null).ToArray();
		}

		#endregion

		/// <summary>���̃C���X�^���X�̕�����\����Ԃ��܂��B</summary>
		/// <returns>�C���X�^���X�̕�����\���B</returns>
		[Confined]
		public override string ToString() { return string.Format("MethodCandidate({0} on {1})", Overload.ReflectionInfo, Overload.DeclaringType.FullName); }
	}
}
