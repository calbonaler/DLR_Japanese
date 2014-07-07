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
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions.Calls
{
	class TypeInferer
	{
		static void EnsureAndAddInput(Dictionary<Type, ArgumentInputs> inputs, Type type, DynamicMetaObject arg, ParameterInferer inferer)
		{
			ArgumentInputs res;
			if (!inputs.TryGetValue(type, out res))
				inputs[type] = res = new ArgumentInputs(type);
			res.AddInput(arg, inferer);
		}

		static Tuple<TValue, bool> ForceGet<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key)
		{
			TValue value;
			var result = dictionary.TryGetValue(key, out value);
			return Tuple.Create(value, result);
		}

		/// <summary>�w�肳�ꂽ�W�F�l���b�N���\�b�h�ɑ΂���W�F�l���b�N�^�����𐄘_���܂��B</summary>
		/// <param name="candidate">���_�̑ΏۂƂȂ郁�\�b�h��\�� <see cref="ApplicableCandidate"/> ���w�肵�܂��B</param>
		/// <param name="actualArgs">�W�F�l���b�N���\�b�h�ɓK�p���ꂽ��������\�� <see cref="ActualArguments"/> ���w�肵�܂��B</param>
		/// <returns>���_����ăW�F�l���b�N�^���������肳�ꂽ���\�b�h��\�� <see cref="MethodCandidate"/>�B</returns>
		internal static MethodCandidate InferGenericMethod(ApplicableCandidate/*!*/ candidate, ActualArguments/*!*/ actualArgs)
		{
			var target = candidate.Method.Overload;
			Assert.NotNull(target);
			Debug.Assert(target.IsGenericMethodDefinition);
			Debug.Assert(target.IsGenericMethod && target.ContainsGenericParameters);
			var inputs = GetArgumentToInputMapping(candidate.Method.Parameters, i => actualArgs[candidate.ArgumentBinding.ArgumentToParameter(i)]);
			// ���͂�����
			var constraints = new Dictionary<Type, Type>();
			var restrictions = new Dictionary<DynamicMetaObject, BindingRestrictions>();
			// �\���̂��鐧�񂪏Փ˂��Ă���
			if (GetSortedGenericArguments(target.GenericArguments).Select(x => ForceGet(inputs, x)).Where(x => x.Item2)
				.Select(x => x.Item1.GetBestType(candidate.Method.Resolver, constraints, restrictions)).All(x => x != null))
			{
				// �Ō�ɃW�F�l���b�N���\�b�h�ɑ΂���V���� MethodCandidate ���\�z����
				var genericArgs = target.GenericArguments.Select(x => ForceGet(constraints, x));
				if (genericArgs.Any(x => !x.Item2))
					return null; // �^�����ɑ΂���ǂ̌^�������炸�A���_�ł��Ȃ��^�����݂���
				var newMethod = target.MakeGenericMethod(genericArgs.Select(x => x.Item1).ToArray());
				var builders = candidate.Method.ArgBuilders.Select(x =>
					x.ParameterInfo != null && (x.ParameterInfo.ParameterType.IsGenericParameter || x.ParameterInfo.ParameterType.ContainsGenericParameters) ?
					Tuple.Create(true, x.Clone(newMethod.Parameters[x.ParameterInfo.Position])) :
					Tuple.Create(false, x));
				if (builders.Any(x => x.Item1 && x.Item2 == null))
					return null; // 1 �ȏ�� ArgBuilder ���^���_���T�|�[�g���Ă��Ȃ�
				// �V���� MethodCandidate ���쐬
				return candidate.Method.ReplaceMethod(newMethod, CreateNewWrappers(candidate.Method.Parameters, newMethod.Parameters, target.Parameters), builders.Select(x => x.Item2).ToList(), restrictions.Count == 0 ? null : restrictions);
			}
			return null;
		}

		/// <summary>�Â�������V�������̂Œu��������W�F�l���b�N���\�b�h�ɑ΂��� <see cref="ParameterWrapper"/> �̐V�������X�g���쐬���܂��B</summary>
		static List<ParameterWrapper> CreateNewWrappers(IEnumerable<ParameterWrapper> candidateParams, IList<ParameterInfo> newOverloadParams, IList<ParameterInfo> oldOverloadParams)
		{
			List<ParameterWrapper> newWrappers = new List<ParameterWrapper>();
			foreach (var oldWrap in candidateParams)
			{
				var pi = oldWrap.ParameterInfo != null ? newOverloadParams[oldWrap.ParameterInfo.Position] : null;
				var newType = oldWrap.Type;
				if (pi != null)
				{
					if (oldOverloadParams[oldWrap.ParameterInfo.Position].ParameterType == oldWrap.Type)
						newType = pi.ParameterType;
					else if (pi.ParameterType.IsByRef)
					{
						newType = pi.ParameterType.GetElementType();
						if (oldOverloadParams[oldWrap.ParameterInfo.Position].ParameterType.GetElementType() != oldWrap.Type)
						{
							Debug.Assert(CompilerHelpers.IsStrongBox(oldWrap.Type));
							newType = typeof(StrongBox<>).MakeGenericType(newType);
						}
					}
					else
					{
						Debug.Assert(oldOverloadParams[oldWrap.ParameterInfo.Position].ParameterType.GetElementType() == oldWrap.Type);
						newType = pi.ParameterType.GetElementType();
					}
				}
				newWrappers.Add(new ParameterWrapper(pi, newType, oldWrap.Name, oldWrap.Flags));
			}
			return newWrappers;
		}

		/// <summary>���̌^��������ˑ�����Ă���^����������Ɉˑ����Ă���^�����̑O�ɂȂ�悤�Ƀ\�[�g���ꂽ�\�[�g�ς݂̃W�F�l���b�N�^�������擾���܂��B</summary>
		static IEnumerable<Type> GetSortedGenericArguments(IEnumerable<Type> genericArguments)
		{
			var dependencies = genericArguments.Select(x => Tuple.Create(x, GetNestedDependencies(x.GetGenericParameterConstraints()))).Where(x => x.Item2.Any()).ToDictionary(x => x.Item1, x => x.Item2);
			// �ˑ��֌W�ɂ��������Č^�������\�[�g����
			return genericArguments.OrderBy(x => x, Comparer<Type>.Create((x, y) =>
			{
				if (ReferenceEquals(x, y))
					return 0;
				if (IsDependentConstraint(dependencies, x, y))
					return 1;
				if (IsDependentConstraint(dependencies, y, x))
					return -1;
				int cmp = x.GetHashCode().CompareTo(y.GetHashCode());
				if (cmp != 0)
					return cmp;
				return IdDispenser.GetId(x).CompareTo(IdDispenser.GetId(y));
			}));
		}

		/// <summary>�^���� <paramref name="x"/> ���^���� <paramref name="y"/> �Ɉˑ����Ă��邩�ǂ����𔻒f���܂��B</summary>
		static bool IsDependentConstraint(IDictionary<Type, IEnumerable<Type>> dependencies, Type x, Type y)
		{
			IEnumerable<Type> deps;
			return dependencies.TryGetValue(x, out deps) && deps.Any(t => t == y || IsDependentConstraint(dependencies, t, y));
		}

		static IEnumerable<Type> GetNestedDependencies(IEnumerable<Type> types)
		{
			foreach (var type in types)
			{
				if (type.IsGenericParameter)
					yield return type;
				else if (type.ContainsGenericParameters)
				{
					foreach (var child in GetNestedDependencies(type.GetGenericArguments()))
						yield return child;
				}
			}
		}

		/// <summary>�W�F�l���b�N�^����������͂��ꂽ <see cref="DynamicMetaObject"/> �ւ̊֌W��Ԃ��܂��B</summary>
		static Dictionary<Type/*!*/, ArgumentInputs/*!*/>/*!*/ GetArgumentToInputMapping(IList<ParameterWrapper> candidateParams, Func<int, DynamicMetaObject> indexer)
		{
			Dictionary<Type, ArgumentInputs> inputs = new Dictionary<Type, ArgumentInputs>();
			for (int i = 0; i < candidateParams.Count; i++)
			{
				Type paramType;
				if (candidateParams[i].IsParamArray)
					paramType = candidateParams[i].Type.GetElementType();
				else if (candidateParams[i].IsByRef)
					paramType = candidateParams[i].ParameterInfo.ParameterType;
				else
					paramType = candidateParams[i].Type;
				if (paramType.IsGenericParameter)
					EnsureAndAddInput(inputs, paramType, indexer(i), new GenericParameterInferer(paramType));
				else if (paramType.ContainsGenericParameters)
				{
					List<Type> containedGenArgs = new List<Type>();
					CollectGenericParameters(paramType, containedGenArgs);
					foreach (var type in containedGenArgs)
						EnsureAndAddInput(inputs, type, indexer(i), new ConstructedParameterInferer(paramType));
				}
			}
			return inputs;
		}

		/// <summary>
		/// ���̌^�ɂ���ĎQ�Ƃ���Ă��邷�ׂẴW�F�l���b�N�^�������\�z���邽�߂Ƀl�X�g���ꂽ�W�F�l���b�N�K�w��T�����܂��B
		/// </summary>
		/// <remarks>
		/// �Ⴆ�΁A���̂悤�ȃ��\�b�h�̈��� x �ɑ΂���W�F�l���b�N�^�������擾���邱�Ƃ��l���܂�:
		/// void Foo{T0, T1}(Dictionary{T0, T1} x);
		/// ���̂Ƃ��A���̃��\�b�h�̓W�F�l���b�N�^�����̃��X�g�� typeof(T0) �� typeof(T1) �̗�����ǉ����܂��B
		/// </remarks>
		static void CollectGenericParameters(Type type, ICollection<Type> containedGenArgs)
		{
			if (type.IsGenericParameter)
			{
				if (!containedGenArgs.Contains(type))
					containedGenArgs.Add(type);
			}
			else if (type.ContainsGenericParameters)
			{
				if (type.IsArray || type.IsByRef)
					CollectGenericParameters(type.GetElementType(), containedGenArgs);
				else
				{
					foreach (var genArg in type.GetGenericArguments())
						CollectGenericParameters(genArg, containedGenArgs);
				}
			}
		}

		/// <summary>�P��̌^�����Ɛ��_�̊�ɂȂ� <see cref="DynamicMetaObject"/> ���\���̂�������Ɋ��蓖�Ă܂��B</summary>
		/// <remarks>
		/// ���Ƃ��΁A���̂悤�ȃV�O�l�`�����l���܂��B
		/// 
		/// void Foo{T0, T1}(T0 x, T1 y, IList{T1} z);
		/// 
		/// �܂��Ax �ɑ΂���������l��ێ����� T0 �ɑ΂��� <see cref="ArgumentInputs"/> �� 1 ����܂��B
		/// ����ɁA y ����� z �ɑ΂��� <see cref="DynamicMetaObject"/> ��ێ����� T1 �ɑ΂��� <see cref="ArgumentInputs"/> �� 1 ���݂��܂��B
		/// y �Ɋ֘A�t����ꂽ���̂� <see cref="GenericParameterInferer"/> �ɁAz �Ɋ֘A�t����ꂽ���̂� <see cref="ConstructedParameterInferer"/> �ɂȂ�܂��B
		/// </remarks>
		class ArgumentInputs
		{
			readonly List<Tuple<DynamicMetaObject, ParameterInferer>> _mappings = new List<Tuple<DynamicMetaObject, ParameterInferer>>();
			readonly Type _genericParam;

			public ArgumentInputs(Type genericParam) { _genericParam = genericParam; }

			public void AddInput(DynamicMetaObject value, ParameterInferer inferer) { _mappings.Add(Tuple.Create(value, inferer)); }

			public Type GetBestType(OverloadResolver resolver, Dictionary<Type, Type> prevConstraints, Dictionary<DynamicMetaObject, BindingRestrictions> restrictions)
			{
				Type curType = null;
				foreach (var mapping in _mappings)
				{
					var nextType = mapping.Item2.GetInferedType(resolver, _genericParam, mapping.Item1, prevConstraints, restrictions);
					if (nextType == null)
						return null; // �ǂ̊��蓖�Ă����p�ł��Ȃ�
					else if (curType == null || curType.IsAssignableFrom(nextType))
						curType = nextType;
					else if (!nextType.IsAssignableFrom(curType))
						return null; // �����������񂪂���
					else
						curType = nextType;
				}
				return curType;
			}
		}

		/// <summary>�P��̈����ɑ΂���W�F�l���b�N�^�����̐��_�@�\��񋟂��܂��B</summary>
		abstract class ParameterInferer
		{
			public ParameterInferer(Type parameterType) { ParameterType = parameterType; }

			public abstract Type GetInferedType(OverloadResolver resolver, Type genericParameter, DynamicMetaObject input, IDictionary<Type, Type> prevConstraints, IDictionary<DynamicMetaObject, BindingRestrictions> restrictions);

			/// <summary>
			/// ���_��������������̌^���擾���܂��B
			/// ����̓W�F�l���b�N�^�����ł͂Ȃ��������̌^��\���܂��B
			/// ���̒l�� typeof(IList&lt;T&gt;) �܂��� typeof(T) �̂悤�ɂ��Ȃ�܂��B
			/// </summary>
			public Type ParameterType { get; private set; }

			/// <summary>
			/// �w�肳�ꂽ�W�F�l���b�N�^����������Ɉᔽ���Ă��邩�ǂ����𔻒f���܂��B
			/// ���̃��\�b�h�ɂ͂��̃��\�b�h�����񂳂�Ă���ˑ����邠����W�F�l���b�N�^�����ɑ΂���}�b�s���O���w�肳���K�v������܂��B
			/// �Ⴆ�΁A�V�O�l�`���� "void Foo{T0, T1}(T0 x, T1 y) where T0 : T1" �̂悤�ȏꍇ�A
			/// T1 ���ǂ̂悤�Ȍ^�ł��邩���������Ȃ���΁A����Ɉᔽ���Ă��邩�ǂ������f�ł��܂���B
			/// </summary>
			protected static bool ConstraintsViolated(Type inputType, Type genericMethodParameterType, IDictionary<Type, Type> prevConstraints)
			{
				return
					// �^�������N���X�ɐ��񂳂�Ă���̂ɒl�^�����͂��ꂽ
					(genericMethodParameterType.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0 && inputType.IsValueType ||
					// �^�������l�^�ɐ��񂳂�Ă���̂� Nullable<T> �܂��̓N���X���邢�̓C���^�[�t�F�C�X�����͂��ꂽ
					(genericMethodParameterType.GenericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0 && (!inputType.IsValueType || TypeUtils.IsNullableType(inputType)) ||
					// �^������ new() ����ł���̂Ɋ���̃R���X�g���N�^�������Ȃ��Q�ƌ^�����͂��ꂽ
					(genericMethodParameterType.GenericParameterAttributes & GenericParameterAttributes.DefaultConstructorConstraint) != 0 && !inputType.IsValueType && inputType.GetConstructor(Type.EmptyTypes) == null ||
					genericMethodParameterType.GetGenericParameterConstraints().Any(x => x.ContainsGenericParameters && (x = ReplaceTypes(x, prevConstraints)) == null || !x.IsAssignableFrom(inputType));
			}

			static Type ReplaceTypes(Type t, IDictionary<Type, Type> prevConstraints)
			{
				Type res;
				if (prevConstraints.TryGetValue(t, out res))
					return res;
				else if (t.IsGenericParameter)
					return null;
				else if (t.ContainsGenericParameters)
				{
					var result = t.GetGenericArguments().Select(x => ReplaceTypes(x, prevConstraints));
					if (result.Any(x => x == null))
						return null;
					return t.GetGenericTypeDefinition().MakeGenericType(result.ToArray());
				}
				return t;
			}
		}

		/// <summary>�^�����\�b�h�^�����̌^�ł�������ɑ΂���^���_�@�\��񋟂��܂��B</summary>
		/// <example>M{T}(T x)</example>
		class GenericParameterInferer : ParameterInferer
		{
			public GenericParameterInferer(Type type) : base(type) { }

			public override Type GetInferedType(OverloadResolver resolver, Type genericParameter, DynamicMetaObject input, IDictionary<Type, Type> prevConstraints, IDictionary<DynamicMetaObject, BindingRestrictions> restrictions)
			{
				var inputType = resolver.GetGenericInferenceType(input);
				if (inputType != null)
				{
					prevConstraints[genericParameter] = inputType;
					if (ConstraintsViolated(inputType, genericParameter, prevConstraints))
						return null;
				}
				return inputType;
			}
		}

		/// <summary>�^�����\�b�h�^��������\�z�����^�ł�������ɑ΂���^���_�@�\��񋟂��܂��B</summary>
		/// <example>
		/// M{T}(IList{T} x)
		/// M{T}(ref T x)
		/// M{T}(T[] x)
		/// M{T}(ref Dictionary{T,T}[] x)
		/// </example>
		class ConstructedParameterInferer : ParameterInferer
		{
			/// <summary>�w�肳�ꂽ�����̌^���g�p���āA<see cref="Microsoft.Scripting.Actions.Calls.TypeInferer.ConstructedParameterInferer"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
			/// <param name="parameterType">�W�F�l���b�N�^�������܂ވ����̌^���w�肵�܂��B�^���̂̓W�F�l���b�N�^�����ł͂���܂���B</param>
			public ConstructedParameterInferer(Type parameterType) : base(parameterType)
			{
				Debug.Assert(!parameterType.IsGenericParameter);
				Debug.Assert(parameterType.ContainsGenericParameters);
			}

			public override Type GetInferedType(OverloadResolver resolver, Type genericParameter, DynamicMetaObject input, IDictionary<Type, Type> prevConstraints, IDictionary<DynamicMetaObject, BindingRestrictions> restrictions)
			{
				var inputType = resolver.GetGenericInferenceType(input);
				if (ParameterType.IsInterface)
				{
					// The argument can implement multiple instantiations of the same generic interface definition, e.g.
					// ArgType : I<C<X>>, I<D<Y>>
					// ParamType == I<C<T>>
					// Unless X == Y we can't infer T.
					Type match = null;
					Type genTypeDef = ParameterType.GetGenericTypeDefinition();
					if (inputType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == genTypeDef && !MatchGenericParameter(genericParameter, x, ParameterType, prevConstraints, ref match)))
						return null;
					prevConstraints[genericParameter] = match;
					return match;
				}
				else if (ParameterType.IsArray)
					return prevConstraints[genericParameter] = MatchGenericParameter(genericParameter, input.LimitType, ParameterType, prevConstraints);
				else if (ParameterType.IsByRef)
					return prevConstraints[genericParameter] = MatchGenericParameter(genericParameter, CompilerHelpers.IsStrongBox(input.LimitType) ? input.LimitType.GetGenericArguments()[0] : input.LimitType, ParameterType.GetElementType(), prevConstraints);
				else if (ParameterType.IsSubclassOf(typeof(Delegate)))
				{
					// see if we have an invokable object which can be used to infer into this delegate
					var invokeInfer = input as IInferableInvokable;
					InferenceResult inference;
					if (invokeInfer != null && (inference = invokeInfer.GetInferredType(ParameterType, genericParameter)) != null)
					{
						if (inference.Restrictions != BindingRestrictions.Empty)
							restrictions[input] = inference.Restrictions;
						prevConstraints[genericParameter] = inference.Type;
						return ConstraintsViolated(inference.Type, genericParameter, prevConstraints) ? null : inference.Type;
					}
				}
				// see if we're anywhere in our base class hierarchy
				var genType = ParameterType.GetGenericTypeDefinition();
				for (var curType = input.LimitType; curType != typeof(object); curType = curType.BaseType)
				{
					if (curType.IsGenericType && curType.GetGenericTypeDefinition() == genType) // TODO: Merge w/ the interface logic above
						return prevConstraints[genericParameter] = MatchGenericParameter(genericParameter, curType, ParameterType, prevConstraints);
				}
				return null;
			}

			static Type MatchGenericParameter(Type genericParameter, Type closedType, Type openType, IDictionary<Type, Type> constraints)
			{
				Type match = null;
				return MatchGenericParameter(genericParameter, closedType, openType, constraints, ref match) ? match : null;
			}

			/// <summary>
			/// <paramref name="openType"/> �� <paramref name="closedType"/> ���̑Ή������ی^���炷�ׂĂ� <paramref name="genericParameter"/> �̏o�����������܂��B
			/// <paramref name="openType"/> ���� <paramref name="genericParameter"/> �̂��ׂĂ̏o���� <paramref name="closedType"/> ���̓�����ی^�ɑΉ����Ă��āA
			/// ���̌^�� <paramref name="constraints"/> ���[�����Ă���� <c>true</c> ��Ԃ��܂��B
			/// ����ɂ��̏ꍇ�́A<paramref name="match"/> �ɋ�ی^��Ԃ��܂��B
			/// </summary>
			static bool MatchGenericParameter(Type genericParameter, Type closedType, Type openType, IDictionary<Type, Type> constraints, ref Type match)
			{
				if (openType.IsGenericParameter)
				{
					if (openType == genericParameter)
					{
						if (match != null)
							return match == closedType;
						if (ConstraintsViolated(closedType, genericParameter, constraints))
							return false;
						match = closedType;
					}
					return true;
				}
				if (openType.IsArray)
				{
					if (!closedType.IsArray)
						return false;
					return MatchGenericParameter(genericParameter, closedType.GetElementType(), openType.GetElementType(), constraints, ref match);
				}
				if (!openType.IsGenericType || !closedType.IsGenericType)
					return openType == closedType;
				if (openType.GetGenericTypeDefinition() != closedType.GetGenericTypeDefinition())
					return false;
				var closedArgs = closedType.GetGenericArguments();
				var openArgs = openType.GetGenericArguments();
				for (int i = 0; i < openArgs.Length; i++)
				{
					if (!MatchGenericParameter(genericParameter, closedArgs[i], openArgs[i], constraints, ref match))
						return false;
				}
				return true;
			}
		}
	}

	/// <summary>
	/// �֘A�t����ꂽ�I�u�W�F�N�g���W�F�l���b�N�^���_�ɎQ���ł���悤�ɂ��܂��B
	/// ���̃C���^�[�t�F�C�X�͌^���_�@�\���f���Q�[�g�^�̈����ɑ΂���^���_�����݂�ۂɎg�p����܂��B
	/// </summary>
	public interface IInferableInvokable
	{
		/// <summary>�f���Q�[�g�^�ւ̕ϊ��ɑ΂��鐄�_�����s����ۂɁA�w�肳�ꂽ�W�F�l���b�N�^�����ɑ΂��鐄�_���ꂽ�^��Ԃ��܂��B</summary>
		/// <param name="delegateType">���������ϊ������f���Q�[�g�^�B</param>
		/// <param name="parameterType">���_�̑ΏۂƂȂ�W�F�l���b�N�^�����B</param>
		/// <returns>�^���_�̌��ʂ��i�[���� <see cref="InferenceResult"/>�B</returns>
		InferenceResult GetInferredType(Type delegateType, Type parameterType);
	}

	/// <summary>
	/// ���I�Ɋ�ɂȂ�^�𐄘_����J�X�^���I�u�W�F�N�g�̌��ʂɊւ������񋟂��܂��B
	/// ���݂͌Ăяo���\�I�u�W�F�N�g���f���Q�[�g�^�ɑ΂���^���t�B�[�h�o�b�N����ړI�ł̂ݎg�p����Ă��܂��B
	/// </summary>
	public class InferenceResult
	{
		/// <summary>���_���ꂽ�^�ƃo�C���f�B���O������g�p���āA<see cref="Microsoft.Scripting.Actions.Calls.InferenceResult"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="type">���_���ꂽ�^��\�� <see cref="Type"/> �I�u�W�F�N�g���w�肵�܂��B</param>
		/// <param name="restrictions">���_���s������������\�� <see cref="DynamicMetaObject"/> �ɒǉ�����o�C���f�B���O������w�肵�܂��B</param>
		public InferenceResult(Type type, BindingRestrictions restrictions)
		{
			ContractUtils.RequiresNotNull(type, "type");
			ContractUtils.RequiresNotNull(restrictions, "restrictions");
			Type = type;
			Restrictions = restrictions;
		}

		/// <summary>���_���ꂽ�^��\�� <see cref="Type"/> �I�u�W�F�N�g���擾���܂��B</summary>
		public Type Type { get; private set; }

		/// <summary>���_���s������������\�� <see cref="DynamicMetaObject"/> �ɒǉ�����o�C���f�B���O������擾���܂��B</summary>
		public BindingRestrictions Restrictions { get; private set; }
	}
}
