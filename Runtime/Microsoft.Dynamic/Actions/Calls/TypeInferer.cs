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

		/// <summary>指定されたジェネリックメソッドに対するジェネリック型引数を推論します。</summary>
		/// <param name="candidate">推論の対象となるメソッドを表す <see cref="ApplicableCandidate"/> を指定します。</param>
		/// <param name="actualArgs">ジェネリックメソッドに適用された実引数を表す <see cref="ActualArguments"/> を指定します。</param>
		/// <returns>推論されてジェネリック型引数が決定されたメソッドを表す <see cref="MethodCandidate"/>。</returns>
		internal static MethodCandidate InferGenericMethod(ApplicableCandidate/*!*/ candidate, ActualArguments/*!*/ actualArgs)
		{
			var target = candidate.Method.Overload;
			Assert.NotNull(target);
			Debug.Assert(target.IsGenericMethodDefinition);
			Debug.Assert(target.IsGenericMethod && target.ContainsGenericParameters);
			var inputs = GetArgumentToInputMapping(candidate.Method.Parameters, i => actualArgs[candidate.ArgumentBinding.ArgumentToParameter(i)]);
			// 入力を処理
			var constraints = new Dictionary<Type, Type>();
			var restrictions = new Dictionary<DynamicMetaObject, BindingRestrictions>();
			// 可能性のある制約が衝突している
			if (GetSortedGenericArguments(target.GenericArguments).Select(x => ForceGet(inputs, x)).Where(x => x.Item2)
				.Select(x => x.Item1.GetBestType(candidate.Method.Resolver, constraints, restrictions)).All(x => x != null))
			{
				// 最後にジェネリックメソッドに対する新しい MethodCandidate を構築する
				var genericArgs = target.GenericArguments.Select(x => ForceGet(constraints, x));
				if (genericArgs.Any(x => !x.Item2))
					return null; // 型引数に対するどの型も見つからず、推論できない型が存在する
				var newMethod = target.MakeGenericMethod(genericArgs.Select(x => x.Item1).ToArray());
				var builders = candidate.Method.ArgBuilders.Select(x =>
					x.ParameterInfo != null && (x.ParameterInfo.ParameterType.IsGenericParameter || x.ParameterInfo.ParameterType.ContainsGenericParameters) ?
					Tuple.Create(true, x.Clone(newMethod.Parameters[x.ParameterInfo.Position])) :
					Tuple.Create(false, x));
				if (builders.Any(x => x.Item1 && x.Item2 == null))
					return null; // 1 つ以上の ArgBuilder が型推論をサポートしていない
				// 新しい MethodCandidate を作成
				return candidate.Method.ReplaceMethod(newMethod, CreateNewWrappers(candidate.Method.Parameters, newMethod.Parameters, target.Parameters), builders.Select(x => x.Item2).ToList(), restrictions.Count == 0 ? null : restrictions);
			}
			return null;
		}

		/// <summary>古い引数を新しいもので置き換えるジェネリックメソッドに対する <see cref="ParameterWrapper"/> の新しいリストを作成します。</summary>
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

		/// <summary>他の型引数から依存されている型引数がそれに依存している型引数の前になるようにソートされたソート済みのジェネリック型引数を取得します。</summary>
		static IEnumerable<Type> GetSortedGenericArguments(IEnumerable<Type> genericArguments)
		{
			var dependencies = genericArguments.Select(x => Tuple.Create(x, GetNestedDependencies(x.GetGenericParameterConstraints()))).Where(x => x.Item2.Any()).ToDictionary(x => x.Item1, x => x.Item2);
			// 依存関係にしたがって型引数をソートする
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

		/// <summary>型引数 <paramref name="x"/> が型引数 <paramref name="y"/> に依存しているかどうかを判断します。</summary>
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

		/// <summary>ジェネリック型引数から入力された <see cref="DynamicMetaObject"/> への関係を返します。</summary>
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
		/// この型によって参照されているすべてのジェネリック型引数を構築するためにネストされたジェネリック階層を探索します。
		/// </summary>
		/// <remarks>
		/// 例えば、次のようなメソッドの引数 x に対するジェネリック型引数を取得することを考えます:
		/// void Foo{T0, T1}(Dictionary{T0, T1} x);
		/// このとき、このメソッドはジェネリック型引数のリストに typeof(T0) と typeof(T1) の両方を追加します。
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

		/// <summary>単一の型引数と推論の基になる <see cref="DynamicMetaObject"/> を可能性のある引数に割り当てます。</summary>
		/// <remarks>
		/// たとえば、次のようなシグネチャを考えます。
		/// 
		/// void Foo{T0, T1}(T0 x, T1 y, IList{T1} z);
		/// 
		/// まず、x に対する実引数値を保持する T0 に対する <see cref="ArgumentInputs"/> が 1 つあります。
		/// さらに、 y および z に対する <see cref="DynamicMetaObject"/> を保持する T1 に対する <see cref="ArgumentInputs"/> も 1 つ存在します。
		/// y に関連付けられたものは <see cref="GenericParameterInferer"/> に、z に関連付けられたものは <see cref="ConstructedParameterInferer"/> になります。
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
						return null; // どの割り当ても利用できない
					else if (curType == null || curType.IsAssignableFrom(nextType))
						curType = nextType;
					else if (!nextType.IsAssignableFrom(curType))
						return null; // 矛盾した制約がある
					else
						curType = nextType;
				}
				return curType;
			}
		}

		/// <summary>単一の引数に対するジェネリック型引数の推論機構を提供します。</summary>
		abstract class ParameterInferer
		{
			public ParameterInferer(Type parameterType) { ParameterType = parameterType; }

			public abstract Type GetInferedType(OverloadResolver resolver, Type genericParameter, DynamicMetaObject input, IDictionary<Type, Type> prevConstraints, IDictionary<DynamicMetaObject, BindingRestrictions> restrictions);

			/// <summary>
			/// 推論が発生する引数の型を取得します。
			/// これはジェネリック型引数ではなく実引数の型を表します。
			/// この値は typeof(IList&lt;T&gt;) または typeof(T) のようにもなります。
			/// </summary>
			public Type ParameterType { get; private set; }

			/// <summary>
			/// 指定されたジェネリック型引数が制約に違反しているかどうかを判断します。
			/// このメソッドにはこのメソッドが制約されている依存するあらゆるジェネリック型引数に対するマッピングが指定される必要があります。
			/// 例えば、シグネチャが "void Foo{T0, T1}(T0 x, T1 y) where T0 : T1" のような場合、
			/// T1 がどのような型であるかが判明しなければ、制約に違反しているかどうか判断できません。
			/// </summary>
			protected static bool ConstraintsViolated(Type inputType, Type genericMethodParameterType, IDictionary<Type, Type> prevConstraints)
			{
				return
					// 型引数がクラスに制約されているのに値型が入力された
					(genericMethodParameterType.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0 && inputType.IsValueType ||
					// 型引数が値型に制約されているのに Nullable<T> またはクラスあるいはインターフェイスが入力された
					(genericMethodParameterType.GenericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0 && (!inputType.IsValueType || TypeUtils.IsNullableType(inputType)) ||
					// 型引数が new() 制約であるのに既定のコンストラクタを持たない参照型が入力された
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

		/// <summary>型がメソッド型引数の型である引数に対する型推論機構を提供します。</summary>
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

		/// <summary>型がメソッド型引数から構築される型である引数に対する型推論機構を提供します。</summary>
		/// <example>
		/// M{T}(IList{T} x)
		/// M{T}(ref T x)
		/// M{T}(T[] x)
		/// M{T}(ref Dictionary{T,T}[] x)
		/// </example>
		class ConstructedParameterInferer : ParameterInferer
		{
			/// <summary>指定された引数の型を使用して、<see cref="Microsoft.Scripting.Actions.Calls.TypeInferer.ConstructedParameterInferer"/> クラスの新しいインスタンスを初期化します。</summary>
			/// <param name="parameterType">ジェネリック型引数を含む引数の型を指定します。型自体はジェネリック型引数ではありません。</param>
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
			/// <paramref name="openType"/> と <paramref name="closedType"/> 内の対応する具象型からすべての <paramref name="genericParameter"/> の出現を検索します。
			/// <paramref name="openType"/> 内の <paramref name="genericParameter"/> のすべての出現が <paramref name="closedType"/> 内の同じ具象型に対応していて、
			/// この型が <paramref name="constraints"/> を充足していれば <c>true</c> を返します。
			/// さらにその場合は、<paramref name="match"/> に具象型を返します。
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
	/// 関連付けられたオブジェクトがジェネリック型推論に参加できるようにします。
	/// このインターフェイスは型推論機構がデリゲート型の引数に対する型推論を試みる際に使用されます。
	/// </summary>
	public interface IInferableInvokable
	{
		/// <summary>デリゲート型への変換に対する推論を実行する際に、指定されたジェネリック型引数に対する推論された型を返します。</summary>
		/// <param name="delegateType">実引数が変換されるデリゲート型。</param>
		/// <param name="parameterType">推論の対象となるジェネリック型引数。</param>
		/// <returns>型推論の結果を格納する <see cref="InferenceResult"/>。</returns>
		InferenceResult GetInferredType(Type delegateType, Type parameterType);
	}

	/// <summary>
	/// 動的に基になる型を推論するカスタムオブジェクトの結果に関する情報を提供します。
	/// 現在は呼び出し可能オブジェクトがデリゲート型に対する型をフィードバックする目的でのみ使用されています。
	/// </summary>
	public class InferenceResult
	{
		/// <summary>推論された型とバインディング制約を使用して、<see cref="Microsoft.Scripting.Actions.Calls.InferenceResult"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="type">推論された型を表す <see cref="Type"/> オブジェクトを指定します。</param>
		/// <param name="restrictions">推論を行った実引数を表す <see cref="DynamicMetaObject"/> に追加するバインディング制約を指定します。</param>
		public InferenceResult(Type type, BindingRestrictions restrictions)
		{
			ContractUtils.RequiresNotNull(type, "type");
			ContractUtils.RequiresNotNull(restrictions, "restrictions");
			Type = type;
			Restrictions = restrictions;
		}

		/// <summary>推論された型を表す <see cref="Type"/> オブジェクトを取得します。</summary>
		public Type Type { get; private set; }

		/// <summary>推論を行った実引数を表す <see cref="DynamicMetaObject"/> に追加するバインディング制約を取得します。</summary>
		public BindingRestrictions Restrictions { get; private set; }
	}
}
