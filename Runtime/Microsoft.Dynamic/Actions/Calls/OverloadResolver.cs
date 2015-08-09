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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Contracts;
using Microsoft.Scripting.Generation;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Actions.Calls
{
	using Ast = Expression;

	/// <summary>
	/// .NET メソッドに対するバインディングとオーバーロード解決を提供します。
	/// このクラスを利用することで、メソッドの呼び出しに対する新しい抽象構文木の生成、実行時のリフレクションを通したメソッド呼び出し、未実装ですが抽象呼び出しを実行できます。
	/// このクラスは既定値を持つ引数、省略可能な引数、参照渡し (in および out)、およびキーワード引数をサポートします。
	/// </summary>
	/// <remarks>
	/// 実装の詳細:
	/// 
	/// このクラスはオーバーロードセットに渡されるそれぞれの有効な引数の数に対する <see cref="CandidateSet"/> を構築することにより動作します。
	/// 例えば、オーバーロードセットが次のようなものであるとします:
	///     foo(object a, object b, object c)
	///     foo(int a, int b)
	/// 上のセットでは 2 個のターゲットセットが存在します。1 つは 3 個の引数をとり、もう 1 つは 2 個の引数をとります。
	/// このクラスは引数配列に対して、必要に応じて適切な大きさの <see cref="CandidateSet"/> をフォールバックし作成します。
	/// 
	/// それぞれの <see cref="CandidateSet"/> は <see cref="MethodCandidate"/> の集合を保持しています。
	/// それぞれの <see cref="MethodCandidate"/> は受け取ることができる平坦化された引数を知っています。
	/// 例えば、次のような関数があるとします:
	///	    foo(params int[] args)
	/// このメソッドが大きさが 3 の <see cref="CandidateSet"/> 内に存在しているとき、<see cref="MethodCandidate"/> は 3 個の引数をとります。そしてそれはすべて int 型です。
	/// そして、もし大きさ 4 の <see cref="CandidateSet"/> 内に存在していれば、同様に 4 個の引数をとります。
	/// 事実上 <see cref="MethodCandidate"/> は必須の位置決定済み引数として扱われるすべての引数を許容する単純化されたビューです。
	/// 
	/// それぞれの <see cref="MethodCandidate"/> は同様にメソッドターゲットを参照します。
	/// メソッドターゲットは位置決定済み引数をどのように消費するか、そしてどのように対象メソッドの適切な引数に渡すかを知っている
	/// <see cref="ArgBuilder"/> と <see cref="ReturnBuilder"/> の集合で構成されています。
	/// これはキーワード引数の適切な位置の決定や、省略可能な引数の既定値の提供などを含んでいます。
	/// 
	/// バインディングの完了後は <see cref="MethodCandidate"/> は破棄され、<see cref="BindingTarget"/> が返されます。
	/// <see cref="BindingTarget"/> はバインディングが成功したかを示し、そうでなければ、ユーザーに報告されるべき失敗したバインディングに関するあらゆる追加情報を提供します。
	/// これはさらにユーザーに呼び出しに必須な引数の平坦化されたリストを取得することを可能にするメソッドターゲットも公開します。
	/// <see cref="MethodCandidate"/> は公開されず、またそれはメソッドバインダーに関する内部実装の詳細となります。
	/// </remarks>
	public abstract partial class OverloadResolver
	{
		// built as target sets are built:
		string _methodName;
		NarrowingLevel _minLevel, _maxLevel;             // specifies the minimum and maximum narrowing levels for conversions during binding
		IList<string> _argNames;
		Dictionary<int, CandidateSet> _candidateSets;    // the methods as they map from # of arguments -> the possible CandidateSet's.
		List<MethodCandidate> _paramsCandidates;         // the methods which are params methods which need special treatment because they don't have fixed # of args

		// built as arguments are processed:
		ActualArguments _actualArguments;

		/// <summary>バインディングを実行する <see cref="ActionBinder"/> を使用して、<see cref="Microsoft.Scripting.Actions.Calls.OverloadResolver"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="binder">バインディングを実行する <see cref="ActionBinder"/> を指定します。</param>
		protected OverloadResolver(ActionBinder binder)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			Binder = binder;
			MaxAccessedCollapsedArg = -1;
		}

		/// <summary>バインディングを実行する <see cref="ActionBinder"/> を取得または設定します。</summary>
		public ActionBinder Binder { get; private set; }

		/// <summary>この <see cref="OverloadResolver"/> が保持する一時変数を取得します。</summary>
		internal List<ParameterExpression> Temps { get; private set; }

		/// <summary>指定された型および名前を使用して、この <see cref="OverloadResolver"/> に関連付けられた一時変数を作成します。</summary>
		/// <param name="type">一時変数の型を指定します。</param>
		/// <param name="name">一時変数の名前を指定します。<c>null</c> を指定できます。</param>
		/// <returns>一時変数を表す <see cref="ParameterExpression"/>。</returns>
		internal ParameterExpression GetTemporary(Type type, string name)
		{
			Assert.NotNull(type);
			var res = Expression.Variable(type, name);
			(Temps ?? (Temps = new List<ParameterExpression>())).Add(res);
			return res;
		}

		#region ResolveOverload

		/// <summary>
		/// メソッドオーバーロードを解決します。
		/// バインディングに成功した場合、<see cref="BindingTarget"/> は特定のオーバーロードの選択を保証する追加の制約が付加された引数に対する <see cref="DynamicMetaObject"/> が格納されています。
		/// </summary>
		/// <param name="methodName">解決するオーバーロードがもっている名前を指定します。</param>
		/// <param name="methods">解決するオーバーロードを表す <see cref="MethodBase"/> のリストを指定します。</param>
		/// <param name="minLevel">オーバーロードの解決に使用する最小の縮小変換レベルを指定します。</param>
		/// <param name="maxLevel">オーバーロードの解決に使用する最大の縮小変換レベルを指定します。</param>
		/// <returns></returns>
		public BindingTarget ResolveOverload(string methodName, IEnumerable<MethodBase> methods, NarrowingLevel minLevel, NarrowingLevel maxLevel) { return ResolveOverload(methodName, methods.Select(m => new ReflectionOverloadInfo(m)), minLevel, maxLevel); }

		/// <summary>
		/// メソッドオーバーロードを解決します。
		/// バインディングに成功した場合、<see cref="BindingTarget"/> は特定のオーバーロードの選択を保証する追加の制約が付加された引数に対する <see cref="DynamicMetaObject"/> が格納されています。
		/// </summary>
		/// <param name="methodName">解決するオーバーロードがもっている名前を指定します。</param>
		/// <param name="methods">解決するオーバーロードを表す <see cref="OverloadInfo"/> のリストを指定します。</param>
		/// <param name="minLevel">オーバーロードの解決に使用する最小の縮小変換レベルを指定します。</param>
		/// <param name="maxLevel">オーバーロードの解決に使用する最大の縮小変換レベルを指定します。</param>
		/// <returns></returns>
		public BindingTarget ResolveOverload(string methodName, IEnumerable<OverloadInfo> methods, NarrowingLevel minLevel, NarrowingLevel maxLevel)
		{
			ContractUtils.RequiresNotNullItems(methods, "methods");
			ContractUtils.Requires(minLevel <= maxLevel);
			if (_candidateSets != null)
				throw new InvalidOperationException("オーバーロード解決器は再利用できません");
			_methodName = methodName;
			_minLevel = minLevel;
			_maxLevel = maxLevel;
			// step 1:
			IList<DynamicMetaObject> namedArgs;
			GetNamedArguments(out namedArgs, out _argNames);
			// uses arg names:
			BuildCandidateSets(methods);
			// uses target sets:
			int preSplatLimit, postSplatLimit;
			GetSplatLimits(out preSplatLimit, out postSplatLimit);
			// step 2:
			if ((_actualArguments = CreateActualArguments(namedArgs, _argNames, preSplatLimit, postSplatLimit)) == null)
				return new BindingTarget(methodName, BindingResult.InvalidArguments);
			// steps 3, 4:
			var candidateSet = GetCandidateSet();
			if (candidateSet != null && !candidateSet.IsParamsDictionaryOnly())
				return MakeBindingTarget(candidateSet);
			// step 5:
			return new BindingTarget(methodName, _actualArguments.VisibleCount, GetExpectedArgCounts());
		}

		#endregion

		#region Step 1: TargetSet construction, custom special parameters handling

		/// <summary>言語が名前付き引数をインスタンスフィールドまたはプロパティに関連付けセッターにできるかどうかを判断します。既定ではこれはコンストラクタにのみ許容されます。</summary>
		/// <param name="method">判断の対象となるメソッドの情報を表す <see cref="OverloadInfo"/> を指定します。</param>
		/// <returns>指定されたメソッドに対する名前付き引数をインスタンスフィールドまたはプロパティに関連付けられる場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		protected internal virtual bool AllowMemberInitialization(OverloadInfo method) { return CompilerHelpers.IsConstructor(method.ReflectionInfo); }

		/// <summary>GetByRefArray 操作の結果を評価する <see cref="Expression"/> を取得します。</summary>
		/// <param name="argumentArrayExpression">操作の結果を表す <see cref="Expression"/> を指定します。</param>
		/// <returns>GetByRefArray 操作の結果を評価する <see cref="Expression"/>。</returns>
		protected internal virtual Expression GetByRefArrayExpression(Expression argumentArrayExpression) { return argumentArrayExpression; }

		/// <summary>配列またはディクショナリのインスタンスまたは <c>null</c> 参照を配列引数または辞書引数に関連付けられるかどうかを判断します。</summary>
		/// <param name="candidate">関連付けの対象となるメソッドを指定します。</param>
		/// <returns>配列またはディクショナリのインスタンスまたは <c>null</c> 参照を配列引数または辞書引数に関連づけられる場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		protected virtual bool BindToUnexpandedParams(MethodCandidate candidate) { return true; }

		/// <summary>引数のバインディングの前に呼び出されます。</summary>
		/// <param name="mapping">マッピングの対象となる <see cref="ParameterMapping"/> オブジェクト。</param>
		/// <returns>
		/// 仮引数がこのメソッドによってマッピングされたかどうかを示すビットマスク。
		/// 既定のビットマスクは残りの仮引数に対して構築されます。(ビットはクリアされています。)
		/// </returns>
		protected internal virtual BitArray MapSpecialParameters(ParameterMapping mapping)
		{
			if (!mapping.Overload.IsStatic)
			{
				mapping.AddParameter(new ParameterWrapper(null, mapping.Overload.DeclaringType, null, ParameterBindingFlags.ProhibitNull));
				mapping.AddInstanceBuilder(new InstanceBuilder(mapping.ArgIndex));
			}
			return null;
		}

		void BuildCandidateSets(IEnumerable<OverloadInfo> methods)
		{
			Debug.Assert(_candidateSets == null);
			Debug.Assert(_argNames != null);
			_candidateSets = new Dictionary<int, CandidateSet>();
			foreach (var method in methods.Where(x => (x.CallingConvention & CallingConventions.VarArgs) == 0)) // skip unsupported method.
				AddBasicMethodTargets(method);
			if (_paramsCandidates != null)
			{
				// For all the methods that take a params array, create MethodCandidates that clash with the other overloads of the method
				foreach (var target in _paramsCandidates.SelectMany(x => _candidateSets.Keys.Select(y => x.MakeParamsExtended(y, _argNames))).Where(x => x != null))
					AddTarget(target);
			}
		}

		CandidateSet GetCandidateSet()
		{
			Debug.Assert(_candidateSets != null && _actualArguments != null);
			CandidateSet result;
			// use precomputed set if arguments are fully expanded and we have one: - or - build a new target set specific to the number of arguments we have:
			if (_actualArguments.CollapsedCount == 0 && _candidateSets.TryGetValue(_actualArguments.Count, out result) || BuildExpandedTargetSet(_actualArguments.Count, out result))
				return result;
			return null;
		}

		bool BuildExpandedTargetSet(int count, out CandidateSet result)
		{
			result = new CandidateSet(count);
			if (_paramsCandidates != null)
			{
				foreach (var item in _paramsCandidates.Select(x => x.MakeParamsExtended(count, _argNames)).Where(x => x != null))
					result.Add(item);
			}
			return result.Count > 0;
		}

		void AddTarget(MethodCandidate target)
		{
			CandidateSet set;
			if (!_candidateSets.TryGetValue(target.Parameters.Count, out set))
				_candidateSets[target.Parameters.Count] = set = new CandidateSet(target.Parameters.Count);
			set.Add(target);
		}

		void AddSimpleTarget(MethodCandidate target)
		{
			if (target.HasParamsArray || target.HasParamsDictionary)
			{
				if (BindToUnexpandedParams(target))
					AddTarget(target);
				(_paramsCandidates ?? (_paramsCandidates = new List<MethodCandidate>())).Add(target);
			}
			else
				AddTarget(target);
		}

		void AddBasicMethodTargets(OverloadInfo method)
		{
			Assert.NotNull(method);
			var mapping = new ParameterMapping(this, method, _argNames);
			mapping.MapParameters(false);
			foreach (var defaultCandidate in mapping.CreateDefaultCandidates())
				AddSimpleTarget(defaultCandidate);
			// TODO: We reduce out/ref parameters only for the main overload.
			// We should rather treat all out params as optional (either a StrongBox is provided or not).
			var byRefReducedCandidate = mapping.CreateByRefReducedCandidate();
			if (byRefReducedCandidate != null)
				AddSimpleTarget(byRefReducedCandidate);
			AddSimpleTarget(mapping.CreateCandidate());
		}

		#endregion

		#region Step 2: Actual Arguments

		/// <summary>この <see cref="OverloadResolver"/> に渡される実引数のセットを取得します。</summary>
		public ActualArguments ActualArguments
		{
			get
			{
				if (_actualArguments == null)
					throw new InvalidOperationException("実引数セットはまだ構築されていません");
				return _actualArguments;
			}
		}

		/// <summary>この <see cref="OverloadResolver"/> に渡される名前付き引数を取得します。</summary>
		/// <param name="namedArgs">名前付き引数の値を格納するリスト。</param>
		/// <param name="argNames">名前付き引数の名前を格納するリスト。</param>
		protected virtual void GetNamedArguments(out IList<DynamicMetaObject> namedArgs, out IList<string> argNames)
		{
			// language doesn't support named arguments:
			argNames = ArrayUtils.EmptyStrings;
			namedArgs = DynamicMetaObject.EmptyMetaObjects;
		}

		/// <summary>指定された名前付き引数と展開された引数に関する情報から <see cref="ActualArguments"/> を作成します。</summary>
		/// <param name="namedArgs">名前付き引数の値を格納するリストを指定します。</param>
		/// <param name="argNames">名前付き引数の名前を格納するリストを指定します。</param>
		/// <param name="preSplatLimit">実際の引数内で展開記号に先行して存在しなければならない引数の最小数を指定します。</param>
		/// <param name="postSplatLimit">実際の引数内で展開記号に後続して存在しなければならない引数の最小数を指定します。</param>
		/// <returns>作成された <see cref="ActualArguments"/>。引数が構築されないかオーバーロード解決がエラーを生成した場合は <c>null</c> が返されます。</returns>
		protected abstract ActualArguments CreateActualArguments(IList<DynamicMetaObject> namedArgs, IList<string> argNames, int preSplatLimit, int postSplatLimit);

		#endregion

		#region Step 3: Resolution

		BindingTarget MakeBindingTarget(CandidateSet targetSet)
		{
			// get candidates whose named arguments can be bind to the parameters:
			List<CallFailure> nameBindingFailures = null;
			var potential = EnsureMatchingNamedArgs(targetSet, ref nameBindingFailures);
			if (!potential.Any())
				return new BindingTarget(_methodName, _actualArguments.VisibleCount, nameBindingFailures.ToArray());
			// go through all available narrowing levels selecting candidates.
			List<CallFailure> failures = null;
			for (var level = _minLevel; level <= _maxLevel; level++)
			{
				if (failures != null)
					failures.Clear();
				// only allow candidates whose non-collapsed arguments are convertible to the parameter types:
				var applicable = SelectCandidatesWithConvertibleArgs(potential, level, ref failures);
				if (applicable.Any())
				{
					if (!applicable.Skip(1).Any())
						return MakeSuccessfulBindingTarget(applicable.Single(), potential, level, targetSet);
					// see if collapsed arguments be converted to the corresponding element types:
					if ((applicable = SelectCandidatesWithConvertibleCollapsedArgs(applicable, level, ref failures)).Any())
					{
						if (!applicable.Skip(1).Any())
							return MakeSuccessfulBindingTarget(applicable.Single(), potential, level, targetSet);
						var bestCandidate = applicable.FirstOrDefault(x => applicable.Where(y => x != y).All(y => GetPreferredCandidate(x, y, level) == Candidate.One));
						if (bestCandidate != null)
							return MakeSuccessfulBindingTarget(bestCandidate, potential, level, targetSet);
						return new BindingTarget(_methodName, _actualArguments.VisibleCount, applicable.Select(x => x.Method).ToArray());
					}
				}
			}
			if (failures == null) // this can happen if there is no callable method:
				return new BindingTarget(_methodName, BindingResult.NoCallableMethod);
			if (nameBindingFailures != null)
				failures.AddRange(nameBindingFailures);
			return new BindingTarget(_methodName, _actualArguments.VisibleCount, failures.ToArray());
		}

		IEnumerable<ApplicableCandidate> EnsureMatchingNamedArgs(IEnumerable<MethodCandidate> candidates, ref List<CallFailure> failures)
		{
			var result = new List<ApplicableCandidate>();
			// skip params dictionaries - we want to only pick up the methods normalized
			// to have argument names (which we created because the MethodBinder gets created w/ keyword arguments).
			foreach (var candidate in candidates.Where(x => !x.HasParamsDictionary))
			{
				CallFailure callFailure;
				ArgumentBinding namesBinding;
				if (_actualArguments.TryBindNamedArguments(candidate, out namesBinding, out callFailure))
					result.Add(new ApplicableCandidate(candidate, namesBinding));
				else
					AddFailure(ref failures, callFailure);
			}
			return result;
		}

		IEnumerable<ApplicableCandidate> SelectCandidatesWithConvertibleArgs(IEnumerable<ApplicableCandidate> candidates, NarrowingLevel level, ref List<CallFailure> failures)
		{
			var result = new List<ApplicableCandidate>();
			foreach (var candidate in candidates.Where(x => !x.Method.Overload.ContainsGenericParameters))
			{
				CallFailure callFailure;
				if (TryConvertArguments(candidate.Method, candidate.ArgumentBinding, level, out callFailure))
					result.Add(candidate);
				else
					AddFailure(ref failures, callFailure);
			}
			if (result.Count == 0)
			{
				// attempt generic method type inference
				foreach (var candidate in candidates.Where(x => x.Method.Overload.IsGenericMethodDefinition))
				{
					var newCandidate = TypeInferer.InferGenericMethod(candidate, _actualArguments);
					if (newCandidate != null)
					{
						CallFailure callFailure;
						if (TryConvertArguments(newCandidate, candidate.ArgumentBinding, level, out callFailure))
							result.Add(new ApplicableCandidate(newCandidate, candidate.ArgumentBinding));
						else
							AddFailure(ref failures, callFailure);
					}
					else
						AddFailure(ref failures, new CallFailure(candidate.Method, CallFailureReason.TypeInference));
				}
			}
			return result;
		}

		IEnumerable<ApplicableCandidate> SelectCandidatesWithConvertibleCollapsedArgs(IEnumerable<ApplicableCandidate> candidates, NarrowingLevel level, ref List<CallFailure> failures)
		{
			if (_actualArguments.CollapsedCount == 0)
				return candidates;
			var result = new List<ApplicableCandidate>();
			foreach (var candidate in candidates)
			{
				CallFailure callFailure;
				if (TryConvertCollapsedArguments(candidate.Method, level, out callFailure))
					result.Add(candidate);
				else
					AddFailure(ref failures, callFailure);
			}
			return result;
		}

		static void AddFailure(ref List<CallFailure> failures, CallFailure failure) { (failures = failures ?? new List<CallFailure>()).Add(failure); }

		bool TryConvertArguments(MethodCandidate candidate, ArgumentBinding namesBinding, NarrowingLevel narrowingLevel, out CallFailure failure)
		{
			Debug.Assert(_actualArguments.Count == candidate.Parameters.Count);
			var hasConversion = Enumerable.Range(0, _actualArguments.Count).Select(i => CanConvertFrom(_actualArguments[i].GetLimitType(), _actualArguments[i], candidate.Parameters[namesBinding.ArgumentToParameter(i)], narrowingLevel)).ToArray();
			failure = Array.TrueForAll(hasConversion, x => x) ? null : new CallFailure(candidate, Enumerable.Range(0, _actualArguments.Count).Select(i => new ConversionResult(_actualArguments[i].Value, _actualArguments[i].GetLimitType(), candidate.Parameters[namesBinding.ArgumentToParameter(i)].Type, !hasConversion[i])).ToArray());
			return failure == null;
		}

		bool TryConvertCollapsedArguments(MethodCandidate candidate, NarrowingLevel narrowingLevel, out CallFailure failure)
		{
			Debug.Assert(_actualArguments.CollapsedCount > 0);
			// There must be at least one expanded parameter preceding splat index (see MethodBinder.GetSplatLimits):
			ParameterWrapper parameter = candidate.Parameters[_actualArguments.SplatIndex - 1];
			Debug.Assert(parameter.ParameterInfo != null && candidate.Overload.IsParamArray(parameter.ParameterInfo.Position));
			for (int i = 0; i < _actualArguments.CollapsedCount; i++)
			{
				var value = GetSplattedItem(_actualArguments.ToSplattedItemIndex(i));
				MaxAccessedCollapsedArg = System.Math.Max(MaxAccessedCollapsedArg, i);
				var argType = CompilerHelpers.GetType(value);
				if (!CanConvertFrom(argType, null, parameter, narrowingLevel))
				{
					failure = new CallFailure(candidate, new[] { new ConversionResult(value, argType, parameter.Type, false) });
					return false;
				}
			}
			failure = null;
			return true;
		}

		RestrictedArguments GetRestrictedArgs(ApplicableCandidate selectedCandidate, IEnumerable<ApplicableCandidate> candidates, int targetSetSize)
		{
			Debug.Assert(selectedCandidate.Method.Parameters.Count == _actualArguments.Count);
			var restrictedArgs = new DynamicMetaObject[_actualArguments.Count];
			var types = new Type[_actualArguments.Count];
			bool hasAdditionalRestrictions = false;
			for (int i = 0; i < _actualArguments.Count; i++)
			{
				if (targetSetSize > 0 && IsOverloadedOnParameter(i, _actualArguments.Count, candidates) || !selectedCandidate.GetParameter(i).Type.IsAssignableFrom(_actualArguments[i].Expression.Type))
				{
					restrictedArgs[i] = RestrictArgument(_actualArguments[i], selectedCandidate.GetParameter(i));
					types[i] = _actualArguments[i].GetLimitType();
				}
				else
					restrictedArgs[i] = _actualArguments[i];
				BindingRestrictions additionalRestrictions;
				if (selectedCandidate.Method.Restrictions != null && selectedCandidate.Method.Restrictions.TryGetValue(_actualArguments[i], out additionalRestrictions))
				{
					hasAdditionalRestrictions = true;
					restrictedArgs[i] = new DynamicMetaObject(restrictedArgs[i].Expression, restrictedArgs[i].Restrictions.Merge(additionalRestrictions));
				}
			}
			return new RestrictedArguments(restrictedArgs, types, hasAdditionalRestrictions);
		}

		DynamicMetaObject RestrictArgument(DynamicMetaObject arg, ParameterWrapper parameter)
		{
			// don't use Restrict as it'll box & unbox.
			return parameter.Type == typeof(object) ? new DynamicMetaObject(arg.Expression, BindingRestrictionsHelpers.GetRuntimeTypeRestriction(arg.Expression, arg.GetLimitType())) : arg.Restrict(arg.GetLimitType());
		}

		/// <summary>指定されたオーバーロードが指定されたインデックスの引数でオーバーロードされているか (指定されたインデックスの引数の型が同じか) を判断します。</summary>
		static bool IsOverloadedOnParameter(int argIndex, int argCount, IEnumerable<ApplicableCandidate> overloads)
		{
			Debug.Assert(argIndex >= 0);
			Type seenParametersType = null;
			foreach (var overload in overloads.Where(x => x.Method.Parameters.Count != 0))
			{
				var lastParameter = overload.Method.Parameters[overload.Method.Parameters.Count - 1];
				Type parameterType;
				if (argIndex < overload.Method.Parameters.Count)
				{
					if (overload.GetParameter(argIndex).IsParamArray)
					{
						if (overload.Method.Parameters.Count == argCount)
							// We're the params array argument and a single value is being passed directly to it.
							// The params array could be in the middle for a params setter.
							// So pis.Count - readIndex is usually 1 for the params at the end,
							// and therefore types.Length - 1 is usually if we're the last argument.
							// We always have to check this type to disambiguate between passing an object which is compatible with the arg array and
							// passing an object which goes into the arg array.
							// Maybe we could do better sometimes.
							return true;
						parameterType = lastParameter.Type.GetElementType();
					}
					else if (overload.GetParameter(argIndex).Type.ContainsGenericParameters)
						return true;
					else
						parameterType = overload.GetParameter(argIndex).Type;
				}
				else if (lastParameter.IsParamArray)
					parameterType = lastParameter.Type.GetElementType();
				else
					continue;
				if (seenParametersType == null)
					seenParametersType = parameterType;
				else if (seenParametersType != parameterType)
					return true;
			}
			return false;
		}

		Candidate GetPreferredCandidate(ApplicableCandidate one, ApplicableCandidate two, NarrowingLevel level)
		{
			var cmpParams = GetPreferredParameters(one, two, level);
			if (cmpParams.Chosen())
				return cmpParams;
			return CompareEquivalentCandidates(one, two);
		}

		/// <summary>仮引数が等価である 2 つの候補を比較します。</summary>
		/// <param name="one">比較する 1 番目の適用可能な候補を指定します。</param>
		/// <param name="two">比較する 2 番目の適用可能な候補を指定します。</param>
		/// <returns>どちらの候補が選択されたか、あるいは完全に等価かを示す <see cref="Candidate"/>。</returns>
		protected virtual Candidate CompareEquivalentCandidates(ApplicableCandidate one, ApplicableCandidate two)
		{
			// Prefer normal methods over explicit interface implementations
			if (two.Method.Overload.IsPrivate && !one.Method.Overload.IsPrivate)
				return Candidate.One;
			if (one.Method.Overload.IsPrivate && !two.Method.Overload.IsPrivate)
				return Candidate.Two;
			// Prefer non-generic methods over generic methods
			if (one.Method.Overload.IsGenericMethod)
			{
				if (!two.Method.Overload.IsGenericMethod)
					return Candidate.Two;
				else
					return Candidate.Equivalent; //!!! Need to support selecting least generic method here
			}
			else if (two.Method.Overload.IsGenericMethod)
				return Candidate.One;
			// prefer methods without out params over those with them
			if (one.Method.ReturnBuilder.CountOutParams < two.Method.ReturnBuilder.CountOutParams)
				return Candidate.One;
			else if (one.Method.ReturnBuilder.CountOutParams > two.Method.ReturnBuilder.CountOutParams)
				return Candidate.Two;
			// prefer methods using earlier conversions rules to later ones            
			for (int i = int.MaxValue; i >= 0; )
			{
				int maxPriorityThis = one.Method.ArgBuilders.Where(x => x.Priority <= i).Aggregate(0, (x, y) => System.Math.Max(x, y.Priority));
				int maxPriorityOther = two.Method.ArgBuilders.Where(x => x.Priority <= i).Aggregate(0, (x, y) => System.Math.Max(x, y.Priority));
				if (maxPriorityThis < maxPriorityOther)
					return Candidate.One;
				if (maxPriorityOther < maxPriorityThis)
					return Candidate.Two;
				i = maxPriorityThis - 1;
			}
			// prefer methods whose name exactly matches the call site name:
			if (one.Method.Overload.Name != two.Method.Overload.Name)
			{
				if (one.Method.Overload.Name == _methodName)
					return Candidate.One;
				if (two.Method.Overload.Name == _methodName)
					return Candidate.Two;
			}
			return Candidate.Equivalent;
		}

		Candidate GetPreferredParameters(ApplicableCandidate one, ApplicableCandidate two, NarrowingLevel level)
		{
			Debug.Assert(one.Method.Parameters.Count == two.Method.Parameters.Count);
			Candidate result = Candidate.Equivalent;
			for (int i = 0; i < ActualArguments.Count; i++)
			{
				var preferred = GetPreferredParameter(one.GetParameter(i), two.GetParameter(i), ActualArguments[i], level);
				switch (result)
				{
					case Candidate.Equivalent:
						result = preferred;
						break;
					case Candidate.One:
						if (preferred == Candidate.Two)
							return Candidate.Ambiguous;
						break;
					case Candidate.Two:
						if (preferred == Candidate.One)
							return Candidate.Ambiguous;
						break;
					case Candidate.Ambiguous:
						if (preferred != Candidate.Equivalent)
							result = preferred;
						break;
					default:
						throw new InvalidOperationException();
				}
			}

			// TODO: process collapsed arguments:

			return result;
		}

		Candidate GetPreferredParameter(ParameterWrapper candidateOne, ParameterWrapper candidateTwo, DynamicMetaObject arg, NarrowingLevel level)
		{
			Assert.NotNull(candidateOne, candidateTwo);
			if (ParametersEquivalent(candidateOne, candidateTwo))
				return Candidate.Equivalent;
			var candidate = SelectBestConversionFor(arg, candidateOne, candidateTwo, level);
			if (candidate.Chosen())
				return candidate;
			if (CanConvertFrom(candidateTwo, candidateOne))
			{
				if (CanConvertFrom(candidateOne, candidateTwo))
					return Candidate.Ambiguous;
				else
					return Candidate.Two;
			}
			else if (CanConvertFrom(candidateOne, candidateTwo))
				return Candidate.One;
			// Special additional rules to order numeric value types
			var preferred = PreferConvert(candidateOne.Type, candidateTwo.Type);
			if (preferred.Chosen())
				return preferred;
			preferred = PreferConvert(candidateTwo.Type, candidateOne.Type).TheOther();
			if (preferred.Chosen())
				return preferred;
			// consider the actual argument type:
			var argType = arg.GetLimitType();
			NarrowingLevel levelOne;
			for (levelOne = NarrowingLevel.None; levelOne < level && !CanConvertFrom(argType, arg, candidateOne, levelOne); levelOne++)
			{
				if (levelOne == NarrowingLevel.All)
				{
					Debug.Fail("各実引数は対応する仮引数に変換可能である必要があります");
					break;
				}
			}
			NarrowingLevel levelTwo;
			for (levelTwo = NarrowingLevel.None; levelTwo < level && !CanConvertFrom(argType, arg, candidateTwo, levelTwo); levelTwo++)
			{
				if (levelTwo == NarrowingLevel.All)
				{
					Debug.Fail("各実引数は対応する仮引数に変換可能である必要があります");
					break;
				}
			}
			if (levelOne < levelTwo)
				return Candidate.One;
			else if (levelOne > levelTwo)
				return Candidate.Two;
			else
				return Candidate.Ambiguous;
		}

		BindingTarget MakeSuccessfulBindingTarget(ApplicableCandidate result, IEnumerable<ApplicableCandidate> potentialCandidates, NarrowingLevel level, CandidateSet targetSet)
		{
			return new BindingTarget(_methodName, _actualArguments.VisibleCount, result.Method, level, GetRestrictedArgs(result, potentialCandidates, targetSet.Arity));
		}

		#endregion

		#region Step 4: Argument Building, Conversions

		/// <summary>指定された 2 つの仮引数が等価かどうかを判断します。</summary>
		/// <param name="parameter1">比較する 1 番目の仮引数を指定します。</param>
		/// <param name="parameter2">比較する 2 番目の仮引数を指定します。</param>
		/// <returns>2 つの仮引数が等価であれば <c>true</c>。そうでなければ <c>false</c>。</returns>
		public virtual bool ParametersEquivalent(ParameterWrapper parameter1, ParameterWrapper parameter2) { return parameter1.Type == parameter2.Type && parameter1.ProhibitNull == parameter2.ProhibitNull; }

		/// <summary><see cref="NarrowingLevel.None"/> の場合に、<paramref name="parameter1"/> から <paramref name="parameter2"/> の間で型を変換できるかどうかを判断します。</summary>
		/// <param name="parameter1">変換元となる仮引数を指定します。</param>
		/// <param name="parameter2">変換先となる仮引数を指定します。</param>
		/// <returns><paramref name="parameter1"/> から <paramref name="parameter2"/> の間で型を変換できる場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public virtual bool CanConvertFrom(ParameterWrapper parameter1, ParameterWrapper parameter2) { return CanConvertFrom(parameter1.Type, null, parameter2, NarrowingLevel.None); }

		/// <summary>指定された縮小変換レベルで、指定された型を持つ実引数を指定された仮引数の型に変換できるかどうかを判断します。</summary>
		/// <param name="fromType">変換元の実引数の型を指定します。</param>
		/// <param name="fromArgument">変換元の実引数の値を指定します。</param>
		/// <param name="toParameter">変換先の仮引数を指定します。</param>
		/// <param name="level">変換を実行する縮小レベルを指定します。</param>
		/// <returns>指定された縮小変換レベルで、指定された型を持つ実引数を指定された仮引数の型に変換できる場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public virtual bool CanConvertFrom(Type fromType, DynamicMetaObject fromArgument, ParameterWrapper toParameter, NarrowingLevel level)
		{
			ContractUtils.RequiresNotNull(fromType, "fromType");
			ContractUtils.RequiresNotNull(toParameter, "toParameter");
			Type toType = toParameter.Type;
			if (fromType == typeof(DynamicNull))
			{
				if (toParameter.ProhibitNull)
					return false;
				if (TypeUtils.IsNullableType(toType) || !toType.IsValueType)
					return true;
			}
			if (fromType == toType)
				return true;
			return Binder.CanConvertFrom(fromType, toType, toParameter.ProhibitNull, level);
		}

		/// <summary>指定された縮小変換レベルで、指定された実引数から 2 つの指定された仮引数の間でどちらに適切に変換できるかどうかを判断します。</summary>
		/// <param name="arg">実引数の値を指定します。</param>
		/// <param name="candidateOne">1 番目の仮引数を指定します。</param>
		/// <param name="candidateTwo">2 番目の仮引数を指定します。</param>
		/// <param name="level">変換を実行する縮小変換レベルを指定します。</param>
		/// <returns>指定された縮小変換レベルでどちらの候補に適切に変換できるかどうかを示す <see cref="Candidate"/>。</returns>
		public virtual Candidate SelectBestConversionFor(DynamicMetaObject arg, ParameterWrapper candidateOne, ParameterWrapper candidateTwo, NarrowingLevel level) { return Candidate.Equivalent; }

		/// <summary>2 つの仮引数の型の間に変換が存在しない場合に、2 つの仮引数の型の間の順序を決定します。</summary>
		/// <param name="t1">1 番目の仮引数の型を指定します。</param>
		/// <param name="t2">2 番目の仮引数の型を指定します。</param>
		/// <returns>指定された 2 つの仮引数の型の間でどちらに変換するかを示す <see cref="Candidate"/>。</returns>
		public virtual Candidate PreferConvert(Type t1, Type t2) { return Binder.PreferConvert(t1, t2); }

		// TODO: revisit
		/// <summary>指定された <see cref="DynamicMetaObject"/> を指定された型に変換する <see cref="Expression"/> を返します。</summary>
		/// <param name="metaObject">変換する <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <param name="restrictedType">変換する <see cref="DynamicMetaObject"/> の制限型を指定します。</param>
		/// <param name="info">変換する仮引数についての情報を格納する <see cref="ParameterInfo"/> を指定します。</param>
		/// <param name="toType">変換先の型を指定します。</param>
		/// <returns>指定された <see cref="DynamicMetaObject"/> を指定された型に変換する <see cref="Expression"/>。</returns>
		public virtual Expression Convert(DynamicMetaObject metaObject, Type restrictedType, ParameterInfo info, Type toType)
		{
			ContractUtils.RequiresNotNull(metaObject, "metaObject");
			ContractUtils.RequiresNotNull(toType, "toType");
			return Binder.ConvertExpression(metaObject.Expression, toType, ConversionResultKind.ExplicitCast, null);
		}

		// TODO: revisit
		/// <summary>引数リストの指定されたインデックスに存在する引数を指定された型に変換するデリゲートを取得します。</summary>
		/// <param name="index">変換する引数を示す引数リスト内のインデックスを指定します。</param>
		/// <param name="metaObject">変換する引数の値を表す <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <param name="info">変換する仮引数に関する情報を格納する <see cref="ParameterInfo"/> を指定します。</param>
		/// <param name="toType">変換先の型を指定します。</param>
		/// <returns>引数リストの指定されたインデックスに存在する引数を指定された型に変換するデリゲート。</returns>
		public virtual Func<object[], object> GetConvertor(int index, DynamicMetaObject metaObject, ParameterInfo info, Type toType) { throw new NotImplementedException(); }

		// TODO: revisit
		/// <summary>指定された <see cref="Expression"/> を指定された型に変換する <see cref="Expression"/> を取得します。</summary>
		/// <param name="value">型を変換する <see cref="Expression"/> を指定します。</param>
		/// <param name="type">変換先の型を指定します。</param>
		/// <returns>指定された <see cref="Expression"/> を指定された型に変換する <see cref="Expression"/>。</returns>
		public virtual Expression GetDynamicConversion(Expression value, Type type) { return Expression.Convert(value, type); }

		#endregion

		#region Step 5: Results, Errors

		int[] GetExpectedArgCounts()
		{
			if (_candidateSets.Count == 0 && _paramsCandidates == null)
				return new int[0];
			int minParamsArray = _paramsCandidates != null ? _paramsCandidates.Where(x => x.HasParamsArray).Aggregate(int.MaxValue, (x, y) => System.Math.Min(x, y.VisibleParameterCount - 1)) : int.MaxValue;
			var result = new List<int>();
			if (_candidateSets.Count > 0)
			{
				var arities = new BitArray(System.Math.Min(_candidateSets.Keys.Aggregate(int.MinValue, System.Math.Max), minParamsArray) + 1);
				foreach (var candidate in _candidateSets.Values.SelectMany(x => x).Where(x => !x.HasParamsArray && x.VisibleParameterCount < arities.Count))
					arities[candidate.VisibleParameterCount] = true;
				for (int i = 0; i < arities.Count; i++)
				{
					if (arities[i] || i == minParamsArray)
						result.Add(i);
				}
			}
			else if (minParamsArray < int.MaxValue)
				result.Add(minParamsArray);
			// all arities starting from minParamsArray are available:
			if (minParamsArray < int.MaxValue)
				result.Add(int.MaxValue);
			return result.ToArray();
		}

		/// <summary>指定された <see cref="BindingTarget"/> から正しくない引数に関するエラーを表す <see cref="ErrorInfo"/> を作成します。</summary>
		/// <param name="target">失敗したバインディングを表す <see cref="BindingTarget"/> を指定します。</param>
		/// <returns>正しくない引数に関するエラーを表す <see cref="ErrorInfo"/>。</returns>
		public virtual ErrorInfo MakeInvalidParametersError(BindingTarget target)
		{
			switch (target.Result)
			{
				case BindingResult.CallFailure:
					return MakeCallFailureError(target);
				case BindingResult.AmbiguousMatch:
					return ErrorInfo.FromException(
						Ast.Call(
							new Func<string, ArgumentTypeException>(BinderOps.SimpleTypeError).Method,
							AstUtils.Constant("複数のターゲットが一致しました: " + string.Join(", ", target.AmbiguousMatches.Select(x => string.Concat(target.Name, "(", string.Join(", ", x.GetParameterTypes().Select(y => Binder.GetTypeName(y))), ")"))), typeof(string))
						)
					);
				case BindingResult.IncorrectArgumentCount:
					return MakeIncorrectArgumentCountError(target);
				case BindingResult.InvalidArguments:
					return ErrorInfo.FromException(Ast.Call(new Func<string, ArgumentTypeException>(BinderOps.SimpleTypeError).Method, AstUtils.Constant("無効な引数です。")));
				case BindingResult.NoCallableMethod:
					return ErrorInfo.FromException(Ast.New(typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) }), AstUtils.Constant("呼び出し可能なメソッドがありません。")));
				default:
					throw new InvalidOperationException();
			}
		}

		static ErrorInfo MakeIncorrectArgumentCountError(BindingTarget target)
		{
			int minArgs = int.MaxValue;
			int maxArgs = int.MinValue;
			foreach (int argCnt in target.ExpectedArgumentCount)
			{
				minArgs = System.Math.Min(minArgs, argCnt);
				maxArgs = System.Math.Max(maxArgs, argCnt);
			}
			return ErrorInfo.FromException(
				Ast.Call(
					new Func<string, int, int, int, int, bool, bool, ArgumentTypeException>(BinderOps.TypeErrorForIncorrectArgumentCount).Method,
					AstUtils.Constant(target.Name, typeof(string)),  // name
					AstUtils.Constant(minArgs),                      // min formal normal arg cnt
					AstUtils.Constant(maxArgs),                      // max formal normal arg cnt
					AstUtils.Constant(0),                            // default cnt
					AstUtils.Constant(target.ActualArgumentCount),   // args provided
					AstUtils.Constant(false),                        // hasArgList
					AstUtils.Constant(false)                         // kwargs provided
				)
			);
		}

		ErrorInfo MakeCallFailureError(BindingTarget target)
		{
			foreach (var cf in target.CallFailures)
			{
				switch (cf.Reason)
				{
					case CallFailureReason.ConversionFailure:
						var cr = cf.ConversionResults.FirstOrDefault(x => x.Failed);
						if (cr != null)
							return ErrorInfo.FromException(
								Ast.Call(
									new Func<string, ArgumentTypeException>(BinderOps.SimpleTypeError).Method,
									AstUtils.Constant(String.Format("{0} が予期されましたが {1} が渡されました。", Binder.GetTypeName(cr.To), cr.GetArgumentTypeName(Binder)))
								)
							);
						break;
					case CallFailureReason.DuplicateKeyword:
						return ErrorInfo.FromException(
								Ast.Call(
									new Func<string, string, ArgumentTypeException>(BinderOps.TypeErrorForDuplicateKeywordArgument).Method,
									AstUtils.Constant(target.Name, typeof(string)),
									AstUtils.Constant(cf.KeywordArguments[0], typeof(string))    // TODO: Report all bad arguments?
							)
						);
					case CallFailureReason.UnassignableKeyword:
						return ErrorInfo.FromException(
								Ast.Call(
									new Func<string, string, ArgumentTypeException>(BinderOps.TypeErrorForExtraKeywordArgument).Method,
									AstUtils.Constant(target.Name, typeof(string)),
									AstUtils.Constant(cf.KeywordArguments[0], typeof(string))    // TODO: Report all bad arguments?
							)
						);
					case CallFailureReason.TypeInference:
						return ErrorInfo.FromException(
								Ast.Call(
									new Func<string, ArgumentTypeException>(BinderOps.TypeErrorForNonInferrableMethod).Method,
									AstUtils.Constant(target.Name, typeof(string))
							)
						);
					default: throw new InvalidOperationException();
				}
			}
			throw new InvalidOperationException();
		}

		#endregion

		#region Splatting

		// Get minimal number of arguments that must precede/follow splat mark in actual arguments.
		void GetSplatLimits(out int preSplatLimit, out int postSplatLimit)
		{
			Debug.Assert(_candidateSets != null);
			if (_paramsCandidates != null)
			{
				int preCount = -1;
				int postCount = -1;
				// For all the methods that take a params array, create MethodCandidates that clash with the other overloads of the method
				foreach (var candidate in _paramsCandidates)
				{
					preCount = System.Math.Max(preCount, candidate.ParamsArrayIndex);
					postCount = System.Math.Max(postCount, candidate.Parameters.Count - candidate.ParamsArrayIndex - 1);
				}
				int maxArity = _candidateSets.Keys.Aggregate(int.MinValue, System.Math.Max);
				if (preCount + postCount < maxArity)
					preCount = maxArity - postCount;
				// +1 ensures that there is at least one expanded parameter before splatIndex (see MethodCandidate.TryConvertCollapsedArguments):
				preSplatLimit = preCount + 1;
				postSplatLimit = postCount;
			}
			else
				preSplatLimit = postSplatLimit = int.MaxValue; // no limits, expand splatted arg fully:
		}

		/// <summary>指定されたインデックスにある遅延展開された引数の値を示す <see cref="Expression"/> を取得します。</summary>
		/// <param name="indexExpression">取得する遅延展開された引数の位置を示すインデックスを表す <see cref="Expression"/> を指定します。</param>
		/// <returns>指定されたインデックスにある遅延展開された引数の値を示す <see cref="Expression"/>。</returns>
		internal Expression GetSplattedItemExpression(Expression indexExpression) { return Expression.Call(GetSplattedExpression(), typeof(IList).GetMethod("get_Item"), indexExpression); } // TODO: move up?

		/// <summary>遅延展開された引数を示す <see cref="Expression"/> を取得します。</summary>
		/// <returns>遅延展開された引数を示す <see cref="Expression"/>。</returns>
		protected abstract Expression GetSplattedExpression();

		/// <summary>指定されたインデックスにある遅延展開された引数の値を取得します。</summary>
		/// <param name="index">取得する遅延展開された引数の位置を示す 0 から始まるインデックスを指定します。</param>
		/// <returns>指定されたインデックスにある遅延展開された引数の値。</returns>
		protected abstract object GetSplattedItem(int index);

		/// <summary>折りたたまれた引数の中でアクセスされたインデックスが最大の引数を取得します。</summary>
		public int MaxAccessedCollapsedArg { get; private set; }

		// TODO: move up?
		/// <summary>折りたたまれた引数に関する条件を取得します。</summary>
		/// <returns>折りたたまれた引数に関する条件を示す <see cref="Expression"/>。</returns>
		public Expression GetCollapsedArgsCondition()
		{
			// collapsed args:
			if (MaxAccessedCollapsedArg >= 0)
			{
				return Ast.Call(null, new Func<IList, int, Type[], bool>(CompilerHelpers.TypesEqual).Method,
					GetSplattedExpression(),
					AstUtils.Constant(_actualArguments.ToSplattedItemIndex(0)),
					Ast.Constant(Enumerable.Range(0, MaxAccessedCollapsedArg + 1).Select(i => GetSplattedItem(_actualArguments.ToSplattedItemIndex(i))).Select(x => x != null ? x.GetType() : null).ToArray())
				);
			}
			else
				return null;
		}

		#endregion

		/// <summary>指定された <see cref="DynamicMetaObject"/> に対するジェネリック型引数を推論します。</summary>
		/// <param name="dynamicObject">ジェネリック型引数を推論する <see cref="DynamicMetaObject"/> を指定します。</param>
		/// <returns>推論されたジェネリック型引数。</returns>
		public virtual Type GetGenericInferenceType(DynamicMetaObject dynamicObject) { return dynamicObject.LimitType; }

		/// <summary>このオブジェクトの文字列表現を返します。</summary>
		/// <returns>このオブジェクトの文字列表現。</returns>
		[Confined]
		public override string ToString() { return string.Join(Environment.NewLine, _candidateSets.Values); }
	}
}
