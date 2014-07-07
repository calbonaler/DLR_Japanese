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
	/// メソッドまたはメソッドオーバーロードを呼び出すありうるさまざまな方法を表します。
	/// それぞれの <see cref="MethodCandidate"/> は <see cref="ParameterWrapper"/> を使用して候補に対する引数の型を表します。
	/// </summary>
	/// <remarks>
	/// 単一のメソッドでも複数の <see cref="MethodCandidate"/> を生成する場合があります。次に理由を挙げます。
	/// - それぞれの省略可能な引数または既定値のある引数が候補になります。
	/// - ref または out 引数の存在は言語に対して返戻値として更新された値を返す候補を追加します。
	/// - <see cref="ArgumentType.List"/> または <see cref="ArgumentType.Dictionary"/> はリストが毎回異なる場合、新しい候補になります。
	/// </remarks>
	public sealed class MethodCandidate
	{
		ParameterWrapper _paramsDict;
		InstanceBuilder _instanceBuilder;

		/// <summary>指定された引数を使用して、<see cref="Microsoft.Scripting.Actions.Calls.MethodCandidate"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="resolver">メソッド呼び出しの生成時にオーバーロードの解決に使用する <see cref="OverloadResolver"/> を指定します。</param>
		/// <param name="method">メソッドを表す <see cref="OverloadInfo"/> を指定します。</param>
		/// <param name="parameters">メソッドの仮引数のリストを指定します。</param>
		/// <param name="paramsDict">メソッドの辞書引数を指定します。</param>
		/// <param name="returnBuilder">返戻値の生成に使用する <see cref="Microsoft.Scripting.Actions.Calls.ReturnBuilder"/> を指定します。</param>
		/// <param name="instanceBuilder">メソッド呼び出しの対象のインスタンスの生成に使用する <see cref="InstanceBuilder"/> を指定します。</param>
		/// <param name="argBuilders">引数の生成に使用する <see cref="Microsoft.Scripting.Actions.Calls.ArgBuilder"/> のリストを指定します。</param>
		/// <param name="restrictions">オーバーロード解決時に使用する特定の <see cref="DynamicMetaObject"/> に対する追加のバインディング制約を保持するディクショナリを指定します。</param>
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

		/// <summary>このメソッド候補が対象とするメソッドを指定されたメソッドに置き換えた新しい <see cref="MethodCandidate"/> を作成します。</summary>
		/// <param name="newMethod">新しいメソッドを表す <see cref="OverloadInfo"/> を指定します。</param>
		/// <param name="parameters">新しいメソッドに対する仮引数のリストを指定します。</param>
		/// <param name="argBuilders">メソッド呼び出しの生成に使用する <see cref="Microsoft.Scripting.Actions.Calls.ArgBuilder"/> のリストを指定します。</param>
		/// <param name="restrictions">オーバーロード解決時に使用する特定の <see cref="DynamicMetaObject"/> に対する追加のバインディング制約を保持するディクショナリを指定します。</param>
		/// <returns>このメソッド候補の対象メソッドを指定されたメソッドに置き換えた新しい <see cref="MethodCandidate"/>。</returns>
		internal MethodCandidate ReplaceMethod(OverloadInfo newMethod, IList<ParameterWrapper> parameters, IList<ArgBuilder> argBuilders, Dictionary<DynamicMetaObject, BindingRestrictions> restrictions)
		{
			return new MethodCandidate(Resolver, newMethod, parameters, _paramsDict, ReturnBuilder, _instanceBuilder, argBuilders, restrictions);
		}

		/// <summary>返戻値の生成に使用する <see cref="Microsoft.Scripting.Actions.Calls.ReturnBuilder"/> を取得します。</summary>
		internal ReturnBuilder ReturnBuilder { get; private set; }

		/// <summary>メソッド呼び出しの引数の生成に使用する <see cref="Microsoft.Scripting.Actions.Calls.ArgBuilder"/> のリストを取得します。</summary>
		internal IList<ArgBuilder> ArgBuilders { get; private set; }

		/// <summary>メソッド呼び出しの生成時にオーバーロードの解決に使用する <see cref="OverloadResolver"/> を取得します。</summary>
		public OverloadResolver Resolver { get; private set; }

		/// <summary>対象となるオーバーロードを表す <see cref="OverloadInfo"/> を取得します。</summary>
		public OverloadInfo Overload { get; private set; }

		/// <summary>オーバーロード解決時に使用する特定の <see cref="DynamicMetaObject"/> に対する追加のバインディング制約を保持するディクショナリを取得します。</summary>
		internal Dictionary<DynamicMetaObject, BindingRestrictions> Restrictions { get; private set; }

		/// <summary>このメソッド候補の返戻値の型を取得します。</summary>
		public Type ReturnType { get { return ReturnBuilder.ReturnType; } }

		/// <summary>このメソッド候補の引数リスト内での配列引数の位置を示すインデックスを取得します。</summary>
		public int ParamsArrayIndex { get; private set; }

		/// <summary>このメソッド候補の引数リスト内に配列引数が存在するかどうかを示す値を取得します。</summary>
		public bool HasParamsArray { get { return ParamsArrayIndex != -1; } }

		/// <summary>このメソッド候補の引数リスト内に辞書引数が存在するかどうかを示す値を取得します。</summary>
		public bool HasParamsDictionary { get { return _paramsDict != null; } }

		/// <summary>指定された名前の引数に対する引数リスト内での位置を示すインデックスを返します。</summary>
		/// <param name="name">引数の名前を指定します。</param>
		/// <returns>引数リスト内での引数の位置を示すインデックス。</returns>
		internal int IndexOfParameter(string name) { return Parameters.FindIndex(x => x.Name == name); }

		/// <summary>可視である引数の数を取得します。</summary>
		public int VisibleParameterCount { get { return Parameters.Count(x => !x.IsHidden); } }

		/// <summary>引数の読み取り専用のリストを取得します。</summary>
		public ReadOnlyCollection<ParameterWrapper> Parameters { get; private set; }

		/// <summary>
		/// 指定された数の引数とキーワード引数をとる新しい <see cref="MethodCandidate"/> を作成します。
		/// 基本的な考えはどの引数が通常の引数または辞書引数に割り当てられるかを計算し、それらの場所を余分な <see cref="ParameterWrapper"/> で埋めることです。
		/// </summary>
		/// <param name="count">引数の数を指定します。</param>
		/// <param name="names">キーワード引数の名前を指定します。</param>
		/// <returns>指定された数の引数とキーワード引数をとる新しい <see cref="MethodCandidate"/>。</returns>
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

		/// <summary>引数の型を配列として取得します。</summary>
		/// <returns>それぞれの要素に引数の型が格納された <see cref="System.Type"/> 型の配列。</returns>
		public Type[] GetParameterTypes() { return ArgBuilders.Select(x => x.Type).Where(x => x != null).ToArray(); }

		#region MakeDelegate

		/// <summary>制限された引数を使用してメソッド呼び出しを表すデリゲートを作成します。</summary>
		/// <param name="restrictedArgs">制限された引数を指定します。</param>
		/// <returns>メソッド呼び出しを表すデリゲート。</returns>
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

		/// <summary>制限された引数を使用してメソッド呼び出しを表す <see cref="Expression"/> を作成します。</summary>
		/// <param name="restrictedArgs">制限された引数を指定します。</param>
		/// <returns>メソッド呼び出しを表す <see cref="Expression"/>。</returns>
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

		/// <summary>このインスタンスの文字列表現を返します。</summary>
		/// <returns>インスタンスの文字列表現。</returns>
		[Confined]
		public override string ToString() { return string.Format("MethodCandidate({0} on {1})", Overload.ReflectionInfo, Overload.DeclaringType.FullName); }
	}
}
