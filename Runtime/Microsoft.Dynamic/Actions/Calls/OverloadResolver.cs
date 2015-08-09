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
	/// .NET ���\�b�h�ɑ΂���o�C���f�B���O�ƃI�[�o�[���[�h������񋟂��܂��B
	/// ���̃N���X�𗘗p���邱�ƂŁA���\�b�h�̌Ăяo���ɑ΂���V�������ۍ\���؂̐����A���s���̃��t���N�V������ʂ������\�b�h�Ăяo���A�������ł������یĂяo�������s�ł��܂��B
	/// ���̃N���X�͊���l���������A�ȗ��\�Ȉ����A�Q�Ɠn�� (in ����� out)�A����уL�[���[�h�������T�|�[�g���܂��B
	/// </summary>
	/// <remarks>
	/// �����̏ڍ�:
	/// 
	/// ���̃N���X�̓I�[�o�[���[�h�Z�b�g�ɓn����邻�ꂼ��̗L���Ȉ����̐��ɑ΂��� <see cref="CandidateSet"/> ���\�z���邱�Ƃɂ�蓮�삵�܂��B
	/// �Ⴆ�΁A�I�[�o�[���[�h�Z�b�g�����̂悤�Ȃ��̂ł���Ƃ��܂�:
	///     foo(object a, object b, object c)
	///     foo(int a, int b)
	/// ��̃Z�b�g�ł� 2 �̃^�[�Q�b�g�Z�b�g�����݂��܂��B1 �� 3 �̈������Ƃ�A���� 1 �� 2 �̈������Ƃ�܂��B
	/// ���̃N���X�͈����z��ɑ΂��āA�K�v�ɉ����ēK�؂ȑ傫���� <see cref="CandidateSet"/> ���t�H�[���o�b�N���쐬���܂��B
	/// 
	/// ���ꂼ��� <see cref="CandidateSet"/> �� <see cref="MethodCandidate"/> �̏W����ێ����Ă��܂��B
	/// ���ꂼ��� <see cref="MethodCandidate"/> �͎󂯎�邱�Ƃ��ł��镽�R�����ꂽ������m���Ă��܂��B
	/// �Ⴆ�΁A���̂悤�Ȋ֐�������Ƃ��܂�:
	///	    foo(params int[] args)
	/// ���̃��\�b�h���傫���� 3 �� <see cref="CandidateSet"/> ���ɑ��݂��Ă���Ƃ��A<see cref="MethodCandidate"/> �� 3 �̈������Ƃ�܂��B�����Ă���͂��ׂ� int �^�ł��B
	/// �����āA�����傫�� 4 �� <see cref="CandidateSet"/> ���ɑ��݂��Ă���΁A���l�� 4 �̈������Ƃ�܂��B
	/// ������ <see cref="MethodCandidate"/> �͕K�{�̈ʒu����ς݈����Ƃ��Ĉ����邷�ׂĂ̈��������e����P�������ꂽ�r���[�ł��B
	/// 
	/// ���ꂼ��� <see cref="MethodCandidate"/> �͓��l�Ƀ��\�b�h�^�[�Q�b�g���Q�Ƃ��܂��B
	/// ���\�b�h�^�[�Q�b�g�͈ʒu����ς݈������ǂ̂悤�ɏ���邩�A�����Ăǂ̂悤�ɑΏۃ��\�b�h�̓K�؂Ȉ����ɓn������m���Ă���
	/// <see cref="ArgBuilder"/> �� <see cref="ReturnBuilder"/> �̏W���ō\������Ă��܂��B
	/// ����̓L�[���[�h�����̓K�؂Ȉʒu�̌����A�ȗ��\�Ȉ����̊���l�̒񋟂Ȃǂ��܂�ł��܂��B
	/// 
	/// �o�C���f�B���O�̊������ <see cref="MethodCandidate"/> �͔j������A<see cref="BindingTarget"/> ���Ԃ���܂��B
	/// <see cref="BindingTarget"/> �̓o�C���f�B���O�������������������A�����łȂ���΁A���[�U�[�ɕ񍐂����ׂ����s�����o�C���f�B���O�Ɋւ��邠����ǉ�����񋟂��܂��B
	/// ����͂���Ƀ��[�U�[�ɌĂяo���ɕK�{�Ȉ����̕��R�����ꂽ���X�g���擾���邱�Ƃ��\�ɂ��郁�\�b�h�^�[�Q�b�g�����J���܂��B
	/// <see cref="MethodCandidate"/> �͌��J���ꂸ�A�܂�����̓��\�b�h�o�C���_�[�Ɋւ�����������̏ڍׂƂȂ�܂��B
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

		/// <summary>�o�C���f�B���O�����s���� <see cref="ActionBinder"/> ���g�p���āA<see cref="Microsoft.Scripting.Actions.Calls.OverloadResolver"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="binder">�o�C���f�B���O�����s���� <see cref="ActionBinder"/> ���w�肵�܂��B</param>
		protected OverloadResolver(ActionBinder binder)
		{
			ContractUtils.RequiresNotNull(binder, "binder");
			Binder = binder;
			MaxAccessedCollapsedArg = -1;
		}

		/// <summary>�o�C���f�B���O�����s���� <see cref="ActionBinder"/> ���擾�܂��͐ݒ肵�܂��B</summary>
		public ActionBinder Binder { get; private set; }

		/// <summary>���� <see cref="OverloadResolver"/> ���ێ�����ꎞ�ϐ����擾���܂��B</summary>
		internal List<ParameterExpression> Temps { get; private set; }

		/// <summary>�w�肳�ꂽ�^����і��O���g�p���āA���� <see cref="OverloadResolver"/> �Ɋ֘A�t����ꂽ�ꎞ�ϐ����쐬���܂��B</summary>
		/// <param name="type">�ꎞ�ϐ��̌^���w�肵�܂��B</param>
		/// <param name="name">�ꎞ�ϐ��̖��O���w�肵�܂��B<c>null</c> ���w��ł��܂��B</param>
		/// <returns>�ꎞ�ϐ���\�� <see cref="ParameterExpression"/>�B</returns>
		internal ParameterExpression GetTemporary(Type type, string name)
		{
			Assert.NotNull(type);
			var res = Expression.Variable(type, name);
			(Temps ?? (Temps = new List<ParameterExpression>())).Add(res);
			return res;
		}

		#region ResolveOverload

		/// <summary>
		/// ���\�b�h�I�[�o�[���[�h���������܂��B
		/// �o�C���f�B���O�ɐ��������ꍇ�A<see cref="BindingTarget"/> �͓���̃I�[�o�[���[�h�̑I����ۏ؂���ǉ��̐��񂪕t�����ꂽ�����ɑ΂��� <see cref="DynamicMetaObject"/> ���i�[����Ă��܂��B
		/// </summary>
		/// <param name="methodName">��������I�[�o�[���[�h�������Ă��閼�O���w�肵�܂��B</param>
		/// <param name="methods">��������I�[�o�[���[�h��\�� <see cref="MethodBase"/> �̃��X�g���w�肵�܂��B</param>
		/// <param name="minLevel">�I�[�o�[���[�h�̉����Ɏg�p����ŏ��̏k���ϊ����x�����w�肵�܂��B</param>
		/// <param name="maxLevel">�I�[�o�[���[�h�̉����Ɏg�p����ő�̏k���ϊ����x�����w�肵�܂��B</param>
		/// <returns></returns>
		public BindingTarget ResolveOverload(string methodName, IEnumerable<MethodBase> methods, NarrowingLevel minLevel, NarrowingLevel maxLevel) { return ResolveOverload(methodName, methods.Select(m => new ReflectionOverloadInfo(m)), minLevel, maxLevel); }

		/// <summary>
		/// ���\�b�h�I�[�o�[���[�h���������܂��B
		/// �o�C���f�B���O�ɐ��������ꍇ�A<see cref="BindingTarget"/> �͓���̃I�[�o�[���[�h�̑I����ۏ؂���ǉ��̐��񂪕t�����ꂽ�����ɑ΂��� <see cref="DynamicMetaObject"/> ���i�[����Ă��܂��B
		/// </summary>
		/// <param name="methodName">��������I�[�o�[���[�h�������Ă��閼�O���w�肵�܂��B</param>
		/// <param name="methods">��������I�[�o�[���[�h��\�� <see cref="OverloadInfo"/> �̃��X�g���w�肵�܂��B</param>
		/// <param name="minLevel">�I�[�o�[���[�h�̉����Ɏg�p����ŏ��̏k���ϊ����x�����w�肵�܂��B</param>
		/// <param name="maxLevel">�I�[�o�[���[�h�̉����Ɏg�p����ő�̏k���ϊ����x�����w�肵�܂��B</param>
		/// <returns></returns>
		public BindingTarget ResolveOverload(string methodName, IEnumerable<OverloadInfo> methods, NarrowingLevel minLevel, NarrowingLevel maxLevel)
		{
			ContractUtils.RequiresNotNullItems(methods, "methods");
			ContractUtils.Requires(minLevel <= maxLevel);
			if (_candidateSets != null)
				throw new InvalidOperationException("�I�[�o�[���[�h������͍ė��p�ł��܂���");
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

		/// <summary>���ꂪ���O�t���������C���X�^���X�t�B�[���h�܂��̓v���p�e�B�Ɋ֘A�t���Z�b�^�[�ɂł��邩�ǂ����𔻒f���܂��B����ł͂���̓R���X�g���N�^�ɂ̂݋��e����܂��B</summary>
		/// <param name="method">���f�̑ΏۂƂȂ郁�\�b�h�̏���\�� <see cref="OverloadInfo"/> ���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ���\�b�h�ɑ΂��閼�O�t���������C���X�^���X�t�B�[���h�܂��̓v���p�e�B�Ɋ֘A�t������ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		protected internal virtual bool AllowMemberInitialization(OverloadInfo method) { return CompilerHelpers.IsConstructor(method.ReflectionInfo); }

		/// <summary>GetByRefArray ����̌��ʂ�]������ <see cref="Expression"/> ���擾���܂��B</summary>
		/// <param name="argumentArrayExpression">����̌��ʂ�\�� <see cref="Expression"/> ���w�肵�܂��B</param>
		/// <returns>GetByRefArray ����̌��ʂ�]������ <see cref="Expression"/>�B</returns>
		protected internal virtual Expression GetByRefArrayExpression(Expression argumentArrayExpression) { return argumentArrayExpression; }

		/// <summary>�z��܂��̓f�B�N�V���i���̃C���X�^���X�܂��� <c>null</c> �Q�Ƃ�z������܂��͎��������Ɋ֘A�t�����邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="candidate">�֘A�t���̑ΏۂƂȂ郁�\�b�h���w�肵�܂��B</param>
		/// <returns>�z��܂��̓f�B�N�V���i���̃C���X�^���X�܂��� <c>null</c> �Q�Ƃ�z������܂��͎��������Ɋ֘A�Â�����ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		protected virtual bool BindToUnexpandedParams(MethodCandidate candidate) { return true; }

		/// <summary>�����̃o�C���f�B���O�̑O�ɌĂяo����܂��B</summary>
		/// <param name="mapping">�}�b�s���O�̑ΏۂƂȂ� <see cref="ParameterMapping"/> �I�u�W�F�N�g�B</param>
		/// <returns>
		/// �����������̃��\�b�h�ɂ���ă}�b�s���O���ꂽ���ǂ����������r�b�g�}�X�N�B
		/// ����̃r�b�g�}�X�N�͎c��̉������ɑ΂��č\�z����܂��B(�r�b�g�̓N���A����Ă��܂��B)
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

		/// <summary>���� <see cref="OverloadResolver"/> �ɓn�����������̃Z�b�g���擾���܂��B</summary>
		public ActualArguments ActualArguments
		{
			get
			{
				if (_actualArguments == null)
					throw new InvalidOperationException("�������Z�b�g�͂܂��\�z����Ă��܂���");
				return _actualArguments;
			}
		}

		/// <summary>���� <see cref="OverloadResolver"/> �ɓn����閼�O�t���������擾���܂��B</summary>
		/// <param name="namedArgs">���O�t�������̒l���i�[���郊�X�g�B</param>
		/// <param name="argNames">���O�t�������̖��O���i�[���郊�X�g�B</param>
		protected virtual void GetNamedArguments(out IList<DynamicMetaObject> namedArgs, out IList<string> argNames)
		{
			// language doesn't support named arguments:
			argNames = ArrayUtils.EmptyStrings;
			namedArgs = DynamicMetaObject.EmptyMetaObjects;
		}

		/// <summary>�w�肳�ꂽ���O�t�������ƓW�J���ꂽ�����Ɋւ����񂩂� <see cref="ActualArguments"/> ���쐬���܂��B</summary>
		/// <param name="namedArgs">���O�t�������̒l���i�[���郊�X�g���w�肵�܂��B</param>
		/// <param name="argNames">���O�t�������̖��O���i�[���郊�X�g���w�肵�܂��B</param>
		/// <param name="preSplatLimit">���ۂ̈������œW�J�L���ɐ�s���đ��݂��Ȃ���΂Ȃ�Ȃ������̍ŏ������w�肵�܂��B</param>
		/// <param name="postSplatLimit">���ۂ̈������œW�J�L���Ɍ㑱���đ��݂��Ȃ���΂Ȃ�Ȃ������̍ŏ������w�肵�܂��B</param>
		/// <returns>�쐬���ꂽ <see cref="ActualArguments"/>�B�������\�z����Ȃ����I�[�o�[���[�h�������G���[�𐶐������ꍇ�� <c>null</c> ���Ԃ���܂��B</returns>
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

		/// <summary>�w�肳�ꂽ�I�[�o�[���[�h���w�肳�ꂽ�C���f�b�N�X�̈����ŃI�[�o�[���[�h����Ă��邩 (�w�肳�ꂽ�C���f�b�N�X�̈����̌^��������) �𔻒f���܂��B</summary>
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

		/// <summary>�������������ł��� 2 �̌����r���܂��B</summary>
		/// <param name="one">��r���� 1 �Ԗڂ̓K�p�\�Ȍ����w�肵�܂��B</param>
		/// <param name="two">��r���� 2 �Ԗڂ̓K�p�\�Ȍ����w�肵�܂��B</param>
		/// <returns>�ǂ���̌�₪�I�����ꂽ���A���邢�͊��S�ɓ����������� <see cref="Candidate"/>�B</returns>
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
					Debug.Fail("�e�������͑Ή����鉼�����ɕϊ��\�ł���K�v������܂�");
					break;
				}
			}
			NarrowingLevel levelTwo;
			for (levelTwo = NarrowingLevel.None; levelTwo < level && !CanConvertFrom(argType, arg, candidateTwo, levelTwo); levelTwo++)
			{
				if (levelTwo == NarrowingLevel.All)
				{
					Debug.Fail("�e�������͑Ή����鉼�����ɕϊ��\�ł���K�v������܂�");
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

		/// <summary>�w�肳�ꂽ 2 �̉��������������ǂ����𔻒f���܂��B</summary>
		/// <param name="parameter1">��r���� 1 �Ԗڂ̉��������w�肵�܂��B</param>
		/// <param name="parameter2">��r���� 2 �Ԗڂ̉��������w�肵�܂��B</param>
		/// <returns>2 �̉������������ł���� <c>true</c>�B�����łȂ���� <c>false</c>�B</returns>
		public virtual bool ParametersEquivalent(ParameterWrapper parameter1, ParameterWrapper parameter2) { return parameter1.Type == parameter2.Type && parameter1.ProhibitNull == parameter2.ProhibitNull; }

		/// <summary><see cref="NarrowingLevel.None"/> �̏ꍇ�ɁA<paramref name="parameter1"/> ���� <paramref name="parameter2"/> �̊ԂŌ^��ϊ��ł��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="parameter1">�ϊ����ƂȂ鉼�������w�肵�܂��B</param>
		/// <param name="parameter2">�ϊ���ƂȂ鉼�������w�肵�܂��B</param>
		/// <returns><paramref name="parameter1"/> ���� <paramref name="parameter2"/> �̊ԂŌ^��ϊ��ł���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
		public virtual bool CanConvertFrom(ParameterWrapper parameter1, ParameterWrapper parameter2) { return CanConvertFrom(parameter1.Type, null, parameter2, NarrowingLevel.None); }

		/// <summary>�w�肳�ꂽ�k���ϊ����x���ŁA�w�肳�ꂽ�^�������������w�肳�ꂽ�������̌^�ɕϊ��ł��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="fromType">�ϊ����̎������̌^���w�肵�܂��B</param>
		/// <param name="fromArgument">�ϊ����̎������̒l���w�肵�܂��B</param>
		/// <param name="toParameter">�ϊ���̉��������w�肵�܂��B</param>
		/// <param name="level">�ϊ������s����k�����x�����w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�k���ϊ����x���ŁA�w�肳�ꂽ�^�������������w�肳�ꂽ�������̌^�ɕϊ��ł���ꍇ�� <c>true</c>�B����ȊO�̏ꍇ�� <c>false</c>�B</returns>
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

		/// <summary>�w�肳�ꂽ�k���ϊ����x���ŁA�w�肳�ꂽ���������� 2 �̎w�肳�ꂽ�������̊Ԃłǂ���ɓK�؂ɕϊ��ł��邩�ǂ����𔻒f���܂��B</summary>
		/// <param name="arg">�������̒l���w�肵�܂��B</param>
		/// <param name="candidateOne">1 �Ԗڂ̉��������w�肵�܂��B</param>
		/// <param name="candidateTwo">2 �Ԗڂ̉��������w�肵�܂��B</param>
		/// <param name="level">�ϊ������s����k���ϊ����x�����w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�k���ϊ����x���łǂ���̌��ɓK�؂ɕϊ��ł��邩�ǂ��������� <see cref="Candidate"/>�B</returns>
		public virtual Candidate SelectBestConversionFor(DynamicMetaObject arg, ParameterWrapper candidateOne, ParameterWrapper candidateTwo, NarrowingLevel level) { return Candidate.Equivalent; }

		/// <summary>2 �̉������̌^�̊Ԃɕϊ������݂��Ȃ��ꍇ�ɁA2 �̉������̌^�̊Ԃ̏��������肵�܂��B</summary>
		/// <param name="t1">1 �Ԗڂ̉������̌^���w�肵�܂��B</param>
		/// <param name="t2">2 �Ԗڂ̉������̌^���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ 2 �̉������̌^�̊Ԃłǂ���ɕϊ����邩������ <see cref="Candidate"/>�B</returns>
		public virtual Candidate PreferConvert(Type t1, Type t2) { return Binder.PreferConvert(t1, t2); }

		// TODO: revisit
		/// <summary>�w�肳�ꂽ <see cref="DynamicMetaObject"/> ���w�肳�ꂽ�^�ɕϊ����� <see cref="Expression"/> ��Ԃ��܂��B</summary>
		/// <param name="metaObject">�ϊ����� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <param name="restrictedType">�ϊ����� <see cref="DynamicMetaObject"/> �̐����^���w�肵�܂��B</param>
		/// <param name="info">�ϊ����鉼�����ɂ��Ă̏����i�[���� <see cref="ParameterInfo"/> ���w�肵�܂��B</param>
		/// <param name="toType">�ϊ���̌^���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ <see cref="DynamicMetaObject"/> ���w�肳�ꂽ�^�ɕϊ����� <see cref="Expression"/>�B</returns>
		public virtual Expression Convert(DynamicMetaObject metaObject, Type restrictedType, ParameterInfo info, Type toType)
		{
			ContractUtils.RequiresNotNull(metaObject, "metaObject");
			ContractUtils.RequiresNotNull(toType, "toType");
			return Binder.ConvertExpression(metaObject.Expression, toType, ConversionResultKind.ExplicitCast, null);
		}

		// TODO: revisit
		/// <summary>�������X�g�̎w�肳�ꂽ�C���f�b�N�X�ɑ��݂���������w�肳�ꂽ�^�ɕϊ�����f���Q�[�g���擾���܂��B</summary>
		/// <param name="index">�ϊ���������������������X�g���̃C���f�b�N�X���w�肵�܂��B</param>
		/// <param name="metaObject">�ϊ���������̒l��\�� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <param name="info">�ϊ����鉼�����Ɋւ�������i�[���� <see cref="ParameterInfo"/> ���w�肵�܂��B</param>
		/// <param name="toType">�ϊ���̌^���w�肵�܂��B</param>
		/// <returns>�������X�g�̎w�肳�ꂽ�C���f�b�N�X�ɑ��݂���������w�肳�ꂽ�^�ɕϊ�����f���Q�[�g�B</returns>
		public virtual Func<object[], object> GetConvertor(int index, DynamicMetaObject metaObject, ParameterInfo info, Type toType) { throw new NotImplementedException(); }

		// TODO: revisit
		/// <summary>�w�肳�ꂽ <see cref="Expression"/> ���w�肳�ꂽ�^�ɕϊ����� <see cref="Expression"/> ���擾���܂��B</summary>
		/// <param name="value">�^��ϊ����� <see cref="Expression"/> ���w�肵�܂��B</param>
		/// <param name="type">�ϊ���̌^���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ <see cref="Expression"/> ���w�肳�ꂽ�^�ɕϊ����� <see cref="Expression"/>�B</returns>
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

		/// <summary>�w�肳�ꂽ <see cref="BindingTarget"/> ���琳�����Ȃ������Ɋւ���G���[��\�� <see cref="ErrorInfo"/> ���쐬���܂��B</summary>
		/// <param name="target">���s�����o�C���f�B���O��\�� <see cref="BindingTarget"/> ���w�肵�܂��B</param>
		/// <returns>�������Ȃ������Ɋւ���G���[��\�� <see cref="ErrorInfo"/>�B</returns>
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
							AstUtils.Constant("�����̃^�[�Q�b�g����v���܂���: " + string.Join(", ", target.AmbiguousMatches.Select(x => string.Concat(target.Name, "(", string.Join(", ", x.GetParameterTypes().Select(y => Binder.GetTypeName(y))), ")"))), typeof(string))
						)
					);
				case BindingResult.IncorrectArgumentCount:
					return MakeIncorrectArgumentCountError(target);
				case BindingResult.InvalidArguments:
					return ErrorInfo.FromException(Ast.Call(new Func<string, ArgumentTypeException>(BinderOps.SimpleTypeError).Method, AstUtils.Constant("�����Ȉ����ł��B")));
				case BindingResult.NoCallableMethod:
					return ErrorInfo.FromException(Ast.New(typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) }), AstUtils.Constant("�Ăяo���\�ȃ��\�b�h������܂���B")));
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
									AstUtils.Constant(String.Format("{0} ���\������܂����� {1} ���n����܂����B", Binder.GetTypeName(cr.To), cr.GetArgumentTypeName(Binder)))
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

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�ɂ���x���W�J���ꂽ�����̒l������ <see cref="Expression"/> ���擾���܂��B</summary>
		/// <param name="indexExpression">�擾����x���W�J���ꂽ�����̈ʒu�������C���f�b�N�X��\�� <see cref="Expression"/> ���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�C���f�b�N�X�ɂ���x���W�J���ꂽ�����̒l������ <see cref="Expression"/>�B</returns>
		internal Expression GetSplattedItemExpression(Expression indexExpression) { return Expression.Call(GetSplattedExpression(), typeof(IList).GetMethod("get_Item"), indexExpression); } // TODO: move up?

		/// <summary>�x���W�J���ꂽ���������� <see cref="Expression"/> ���擾���܂��B</summary>
		/// <returns>�x���W�J���ꂽ���������� <see cref="Expression"/>�B</returns>
		protected abstract Expression GetSplattedExpression();

		/// <summary>�w�肳�ꂽ�C���f�b�N�X�ɂ���x���W�J���ꂽ�����̒l���擾���܂��B</summary>
		/// <param name="index">�擾����x���W�J���ꂽ�����̈ʒu������ 0 ����n�܂�C���f�b�N�X���w�肵�܂��B</param>
		/// <returns>�w�肳�ꂽ�C���f�b�N�X�ɂ���x���W�J���ꂽ�����̒l�B</returns>
		protected abstract object GetSplattedItem(int index);

		/// <summary>�܂肽���܂ꂽ�����̒��ŃA�N�Z�X���ꂽ�C���f�b�N�X���ő�̈������擾���܂��B</summary>
		public int MaxAccessedCollapsedArg { get; private set; }

		// TODO: move up?
		/// <summary>�܂肽���܂ꂽ�����Ɋւ���������擾���܂��B</summary>
		/// <returns>�܂肽���܂ꂽ�����Ɋւ������������ <see cref="Expression"/>�B</returns>
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

		/// <summary>�w�肳�ꂽ <see cref="DynamicMetaObject"/> �ɑ΂���W�F�l���b�N�^�����𐄘_���܂��B</summary>
		/// <param name="dynamicObject">�W�F�l���b�N�^�����𐄘_���� <see cref="DynamicMetaObject"/> ���w�肵�܂��B</param>
		/// <returns>���_���ꂽ�W�F�l���b�N�^�����B</returns>
		public virtual Type GetGenericInferenceType(DynamicMetaObject dynamicObject) { return dynamicObject.LimitType; }

		/// <summary>���̃I�u�W�F�N�g�̕�����\����Ԃ��܂��B</summary>
		/// <returns>���̃I�u�W�F�N�g�̕�����\���B</returns>
		[Confined]
		public override string ToString() { return string.Join(Environment.NewLine, _candidateSets.Values); }
	}
}
