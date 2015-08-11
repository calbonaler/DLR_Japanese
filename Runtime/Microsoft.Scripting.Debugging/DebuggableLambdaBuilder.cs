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
using System.Linq;
using Microsoft.Scripting.Ast;
using Microsoft.Scripting.Debugging.CompilerServices;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;
using MSAst = System.Linq.Expressions;

namespace Microsoft.Scripting.Debugging
{
	using Ast = MSAst.Expression;

	/// <summary>DLR ���c���[���f�o�b�O�\�ȃ����_���ɕό`���邽�߂Ɏg�p�����N���X��\���܂��B</summary>
	class DebuggableLambdaBuilder
	{
		readonly DebugContext _debugContext;                                    // DebugContext
		readonly DebugLambdaInfo _lambdaInfo;                                   // Labmda info that's passed to us by the compiler
		int _lambdaId;

		static readonly MSAst.ParameterExpression _frame = Ast.Variable(typeof(DebugFrame), "$frame");
		static readonly MSAst.ParameterExpression _thread = Ast.Variable(typeof(DebugThread), "$thread");
		static readonly MSAst.ParameterExpression _debugMarker = Ast.Variable(typeof(int), "$debugMarker");
		static readonly MSAst.ParameterExpression _framePushed = Ast.Variable(typeof(bool), "$framePushed");
		static readonly MSAst.ParameterExpression _funcInfo = Ast.Parameter(typeof(FunctionInfo), "$funcInfo");
		static readonly MSAst.ParameterExpression _traceLocations = Ast.Parameter(typeof(bool[]), "$traceLocations");
		static readonly MSAst.ParameterExpression _retValAsObject = Ast.Variable(typeof(object), "$retVal");
		static readonly MSAst.ParameterExpression _retValFromGeneratorLoop = Ast.Variable(typeof(object), "$retValFromGen");
		static readonly MSAst.ParameterExpression _frameExitException = Ast.Parameter(typeof(bool), "$frameExitException");

		internal DebuggableLambdaBuilder(DebugContext debugContext, DebugLambdaInfo lambdaInfo)
		{
			_debugContext = debugContext;
			_lambdaInfo = lambdaInfo;
		}

		internal MSAst.LambdaExpression Transform(MSAst.LambdaExpression lambda)
		{
			string lambdaName = _lambdaInfo.LambdaAlias;

			if (lambdaName == null && (lambdaName = lambda.Name) == null)
				lambdaName = "$lambda" + ++_lambdaId;

			Ast body;
			var generator = lambda.Body as GeneratorExpression;
			MSAst.LabelTarget generatorLabelTarget = null;
			var lambdaPh = new ParametersHolder();
			if (generator != null)
			{
				body = generator.Body;
				generatorLabelTarget = generator.Target;
				// TODO: ���x���̌^�� typeof(object) �łȂ����Ƃ����o���A�V�������x�����쐬����悤�ɂ���
				Debug.Assert(generatorLabelTarget.Type == typeof(object));
			}
			else
			{
				body = lambda.Body;
				lambdaPh.Variables.AddRange(new[] { _thread, _framePushed, _funcInfo, _traceLocations, _debugMarker, _frameExitException });
			}

			var generatorPs = new ParametersHolder();
			generatorPs.Parameters.Add(_frame);

			MSAst.ParameterExpression retVal = null;
			if (generator == null)
			{
				var returnType = lambda.Type.GetMethod("Invoke").ReturnType;
				// �߂�l�̌^�� void �łȂ��ꍇ�̂� $retVal �ϐ����쐬"
				if (returnType == typeof(object))
					retVal = _retValAsObject;
				else if (returnType != typeof(void))
					retVal = Ast.Variable(returnType, "$retVal");
				if (retVal != null)
				{
					lambdaPh.Variables.Add(retVal);
					generatorPs.Variables.Add(retVal);
				}
				lambdaPh.Variables.Add(_retValFromGeneratorLoop);
			}

			var pendingLocals = new List<MSAst.ParameterExpression>();
			var parameters = new HashSet<MSAst.ParameterExpression>();
			foreach (var parameter in lambda.Parameters)
			{
				parameters.Add(parameter);
				pendingLocals.Add(parameter); // ������ۗ����X�g�ɒǉ�
			}
			// �S���[�J���ϐ����ʂ̂��߂� 1 ��ڂ̃c���[���������s
			var lambdaWalker = new LambdaWalker();
			if (generator != null)
				lambdaWalker.Visit(body);
			else
				body = lambdaWalker.Visit(body);
			pendingLocals.AddRange(lambdaWalker.Locals); // ���ɑS���[�J���ϐ���ۗ����X�g�ɒǉ�

			var layout = LayoutVariables(generator != null, pendingLocals, lambdaWalker.StrongBoxedLocals, parameters, lambdaPh, generatorPs); // �ϐ�������

			DebugSourceSpan[] debugMarkerLocationMap;
			VariableInfo[][] variableScopeMap;
			Dictionary<DebugSourceFile, MSAst.ParameterExpression> sourceFilesMap;
			bool noPushFrameOptimization;
			var generatorBody = TransformToGeneratorBody(body, layout.PendingToVariableInfosMap, ref generatorLabelTarget, out debugMarkerLocationMap, out variableScopeMap, out sourceFilesMap, out noPushFrameOptimization); // �W�F�l���[�^�ɑ΂��ă����C�g

			Ast debuggableBody = null;
			PushFrameExpression pushFrame = null;
			if (generator == null)
			{
				lambdaPh.Variables.AddRange(sourceFilesMap.Values); // ���Ƀ\�[�X�t�@�C���̕ϐ�
				pushFrame = new PushFrameExpression(_debugContext, layout.VerifiedLocals, layout.VariableInfos); // �t���[���̃v�b�V���ɑ΂��鎮���쐬
				// �f�o�b�O�\�Ȗ{�̂ɑ΂��ă����C�g
				debuggableBody = CreateDebugInfoRewriter(false, noPushFrameOptimization ? null : pushFrame, sourceFilesMap, null, layout.PendingToVerifiedLocalsMap, null).Visit(body);
			}

			var generatorFactoryLambda = CreateGeneratorFactoryLambda(generatorBody, lambdaName, pendingLocals, layout.VariableInfos, generatorLabelTarget, generatorPs, retVal); // �W�F�l���[�^�t�@�N�g���̃����_���擾
			var functionInfoInitializer = CreateFunctionInfo(generatorFactoryLambda, lambdaName, layout.VariableInfos, debugMarkerLocationMap, variableScopeMap); // FunctionInfo �I�u�W�F�N�g���쐬

			if (generator != null)
				return CreateOuterGeneratorFactory(lambda.Type, lambdaName, generatorLabelTarget.Type, lambdaPh, functionInfoInitializer); // �O���̃W�F�l���[�^�����_���쐬
			else
				return CreateOuterLambda(lambda.Type, debuggableBody, lambdaName, debugMarkerLocationMap, sourceFilesMap,
					lambdaPh, retVal, pushFrame, noPushFrameOptimization, functionInfoInitializer); // �O���̃����_���쐬
		}

		/// <summary>
		/// �ϐ������t�g����鏇���Ń��C�A�E�g���܂��B
		/// ���O���Փ˂���C�ӂ̕ϐ��͐V�����ϐ��Œu���������܂��B
		/// StrongBox ����Ă���ϐ��͂����ł̓��t�g����܂���B
		/// </summary>
		VariableLayout LayoutVariables(bool forGenerator, IEnumerable<MSAst.ParameterExpression> pendingLocals, HashSet<MSAst.ParameterExpression> strongBoxedLocals, HashSet<MSAst.ParameterExpression> parameters, ParametersHolder lambdaPs, ParametersHolder generatorPs)
		{
			int byrefIndex = 0;
			int strongBoxIndex = 0;
			var verifiedLocalNames = new HashSet<string>();
			var layout = new VariableLayout();

			foreach (var pendingLocal in pendingLocals)
			{
				// ���[�J���ϐ��ɑ΂���ʖ������邩�𒲂ׂ�
				string name;
				if (_lambdaInfo.VariableAliases == null || !_lambdaInfo.VariableAliases.TryGetValue(pendingLocal, out name))
					name = pendingLocal.Name;
				var isHidden = _lambdaInfo.HiddenVariables != null && _lambdaInfo.HiddenVariables.Contains(pendingLocal);
				if (name == null)
				{
					name = "local";
					isHidden = true;
				}

				var verifiedLocal = pendingLocal;

				// ���[�J���ϐ��𖼑O�Փ˂̂��߂ɒu��������K�v�����邩�𒲂ׂ�
				if (verifiedLocalNames.Contains(name))
				{
					// ��ӂȖ��O�𓾂�
					int count = 0;
					while (verifiedLocalNames.Contains(name + ++count)) { }
					verifiedLocal = Ast.Parameter(verifiedLocal.Type, name += count);
					layout.PendingToVerifiedLocalsMap.Add(pendingLocal, verifiedLocal);
				}

				layout.VerifiedLocals.Add(verifiedLocal);
				verifiedLocalNames.Add(name);

				var isParameter = parameters.Contains(pendingLocal);
				var isStrongBoxed = forGenerator || strongBoxedLocals.Contains(pendingLocal);
				var varInfo = new VariableInfo(SymbolTable.StringToId(name), pendingLocal.Type, isParameter, isHidden, isStrongBoxed, isStrongBoxed ? strongBoxIndex++ : byrefIndex++, layout.VariableInfos.Count);
				layout.VariableInfos.Add(varInfo);
				layout.PendingToVariableInfosMap.Add(pendingLocal, varInfo);

				// �ϐ����r���_�[�ɒǉ�
				if (isParameter)
				{
					lambdaPs.Parameters.Add(pendingLocal);
					generatorPs.Parameters.Add(pendingLocal);
				}
				else
				{
					if (!forGenerator)
						lambdaPs.Variables.Add(verifiedLocal);
					generatorPs.Variables.Add(pendingLocal);
				}
			}

			return layout;
		}

		Ast CreateFunctionInfo(MSAst.LambdaExpression generatorFactoryLambda, string lambdaName, IList<VariableInfo> variableInfos, DebugSourceSpan[] debugMarkerLocationMap, VariableInfo[][] variableScopeMap)
		{
			if (_lambdaInfo.CompilerSupport != null && _lambdaInfo.CompilerSupport.DoesExpressionNeedReduction(generatorFactoryLambda))
			{
				return _lambdaInfo.CompilerSupport.QueueExpressionForReduction(
					Ast.Call(
						new Func<Delegate, string, DebugSourceSpan[], VariableInfo[][], IList<VariableInfo>, object, FunctionInfo>(DebugContext.CreateFunctionInfo).Method,
						generatorFactoryLambda,
						AstUtils.Constant(lambdaName),
						AstUtils.Constant(debugMarkerLocationMap),
						AstUtils.Constant(variableScopeMap),
						AstUtils.Constant(variableInfos),
						Ast.Constant(_lambdaInfo.CustomPayload, typeof(object))
					)
				);
			}
			else
			{
				return Ast.Constant(
					DebugContext.CreateFunctionInfo(generatorFactoryLambda.Compile(), lambdaName, debugMarkerLocationMap, variableScopeMap, variableInfos, _lambdaInfo.CustomPayload),
					typeof(FunctionInfo)
				);
			}
		}

		Ast TransformToGeneratorBody(Ast body, Dictionary<MSAst.ParameterExpression, VariableInfo> pendingToVariableInfosMap,
			ref MSAst.LabelTarget generatorLabelTarget, out DebugSourceSpan[] debugMarkerLocationMap, out VariableInfo[][] variableScopeMap, out Dictionary<DebugSourceFile, MSAst.ParameterExpression> sourceFilesMap,
			out bool noPushFrameOptimization)
		{
			if (generatorLabelTarget == null)
				generatorLabelTarget = Ast.Label(typeof(object));

			var debugInfoToYieldRewriter = CreateDebugInfoRewriter(true, null, null, generatorLabelTarget, null, pendingToVariableInfosMap);

			var transformedBody = debugInfoToYieldRewriter.Visit(body);
			debugMarkerLocationMap = debugInfoToYieldRewriter.DebugMarkerLocationMap;
			variableScopeMap = debugInfoToYieldRewriter.VariableScopeMap;

			// DebugSourceFile-�ϐ��ԃ}�b�s���O���쐬
			sourceFilesMap = new Dictionary<DebugSourceFile, MSAst.ParameterExpression>();
			foreach (var sourceSpan in debugMarkerLocationMap)
			{
				if (!sourceFilesMap.ContainsKey(sourceSpan.SourceFile))
					sourceFilesMap.Add(sourceSpan.SourceFile, Ast.Parameter(typeof(DebugSourceFile)));
			}

			// �R���p�C�������[�t�t���[���œK����v�����Ȃ��������A���̃f�o�b�O�\�ȃ����_�ւ̖������Ăяo�������������ꍇ�́A�œK�����s��Ȃ�
			noPushFrameOptimization = !_lambdaInfo.OptimizeForLeafFrames || debugInfoToYieldRewriter.HasUnconditionalFunctionCalls;
			return transformedBody;
		}

		DebugInfoRewriter CreateDebugInfoRewriter(bool forGenerator, PushFrameExpression pushFrame, IReadOnlyDictionary<DebugSourceFile, MSAst.ParameterExpression> sourceFilesMap, MSAst.LabelTarget generatorLabelTarget, IReadOnlyDictionary<MSAst.ParameterExpression, MSAst.ParameterExpression> pendingToVerifiedLocalsMap, IReadOnlyDictionary<MSAst.ParameterExpression, VariableInfo> pendingToVariableInfosMap)
		{
			return new DebugInfoRewriter(
				_debugContext,
				forGenerator,
				_traceLocations,
				_thread,
				_frame,
				pushFrame != null ? pushFrame.ConditionalExecutor : null,
				forGenerator ? null : _debugMarker,
				Ast.Property(Ast.Constant(_debugContext), "Mode"),
				sourceFilesMap,
				generatorLabelTarget,
				pendingToVerifiedLocalsMap,
				pendingToVariableInfosMap,
				_lambdaInfo
			);
		}

		MSAst.LambdaExpression CreateOuterLambda(Type delegateType, Ast debuggableBody, string name, IEnumerable<DebugSourceSpan> debugMarkerLocationMap,
			IReadOnlyDictionary<DebugSourceFile, MSAst.ParameterExpression> sourceFilesMap, ParametersHolder parameters,
			Ast retVal, PushFrameExpression pushFrame, bool noPushFrameOptimization, Ast functionInfoInitializer)
		{
			var globalDebugModeExpression = Ast.Property(Ast.Constant(_debugContext), "Mode");

			var returnType = delegateType.GetMethod("Invoke").ReturnType;
			var returnLabelTarget = Ast.Label(returnType);

			var tryExpressions = new List<MSAst.Expression>();

			// $funcInfo ��������
			tryExpressions.Add(Ast.Assign(_funcInfo, Ast.Convert(functionInfoInitializer, typeof(FunctionInfo))));
			// $traceLocations ��������
			// TODO: TracePoints ���[�h���ł݂̂��������悤��
			tryExpressions.Add(Ast.Assign(_traceLocations, Ast.Call(new Func<FunctionInfo, bool[]>(RuntimeOps.GetTraceLocations).Method, _funcInfo)));
			// DebugSourceFile ���[�J���ϐ���������
			foreach (var entry in sourceFilesMap)
				tryExpressions.Add(Ast.Assign(entry.Value, Ast.Constant(entry.Key, typeof(DebugSourceFile))));
			if (noPushFrameOptimization)
				tryExpressions.Add(pushFrame.Executor);
			tryExpressions.Add(Ast.Call(new Action<DebugThread>(RuntimeOps.OnFrameEnterTraceEvent).Method, _thread));

			// �ʏ�̏I��
			tryExpressions.Add(retVal != null ? Ast.Assign(retVal, debuggableBody) : debuggableBody);
			tryExpressions.Add(Ast.Assign(_frameExitException, Ast.Constant(true)));
			var debugSourceFile = debugMarkerLocationMap.Select(x => x.SourceFile).FirstOrDefault();
			var frameExit = AstUtils.If(
				Ast.Equal(
					debugSourceFile != null ? Ast.Property(sourceFilesMap[debugSourceFile], "Mode") : globalDebugModeExpression,
					AstUtils.Constant((int)DebugMode.FullyEnabled)
				),
				Ast.Call(new Action<DebugThread, int, object>(RuntimeOps.OnFrameExitTraceEvent).Method, _thread, _debugMarker, retVal != null ? (MSAst.Expression)Ast.Convert(retVal, typeof(object)) : Ast.Constant(null))
			);
			tryExpressions.Add(frameExit);
			tryExpressions.Add(retVal != null ? (MSAst.Expression)Ast.Return(returnLabelTarget, retVal) : Ast.Empty());

			var popFrameCondition = Ast.AndAlso(
				Ast.Call(new Func<DebugThread, bool>(RuntimeOps.PopFrame).Method, _thread),
				Ast.Equal(globalDebugModeExpression, AstUtils.Constant((int)DebugMode.FullyEnabled))
			);
			if (!noPushFrameOptimization)
				popFrameCondition = Ast.AndAlso(_framePushed, popFrameCondition);

			// �֐��{�̂����s
			MSAst.ParameterExpression caughtException;
			var body = AstUtils.Try(
				AstUtils.Try(
					ArrayUtils.Append(tryExpressions, Ast.Default(returnType))
				).Catch(caughtException = Ast.Variable(typeof(Exception), "$caughtException"),
				// �ȉ��̎��͏�ɃX���[���܂��B
				// ��O���L�����Z�������K�v���������ꍇ OnTraceEvent �� ForceToGeneratorLoopException ���X���[���܂��B
				// ��O���L�����Z������Ă���ꍇ catch �u���b�N�̏I�[�ŒP�ɍăX���[���܂��B
					AstUtils.If(Ast.Not(Ast.TypeIs(caughtException, typeof(ForceToGeneratorLoopException))),
						AstUtils.If(Ast.NotEqual(globalDebugModeExpression, AstUtils.Constant((int)DebugMode.Disabled)),
							noPushFrameOptimization ? Ast.Empty() : pushFrame.ConditionalExecutor,
							Ast.Call(new Action<DebugThread, int, Exception>(RuntimeOps.OnTraceEventUnwind).Method, _thread, _debugMarker, caughtException)
						),
				// ��O�I��
						AstUtils.If(Ast.Not(_frameExitException), frameExit)
					),
					Ast.Rethrow(returnType)
				)
			).Catch(typeof(ForceToGeneratorLoopException),
				AstUtils.Try(
				// ForceToGeneratorLoopException �̃n���h��
					returnType != typeof(void) ? AstUtils.If(Ast.NotEqual(Ast.Assign(_retValFromGeneratorLoop, Ast.Call(new Func<DebugThread, object>(RuntimeOps.GeneratorLoopProc).Method, _thread)), Ast.Constant(null)),
						Ast.Return(returnLabelTarget, Ast.Assign(retVal, Ast.Convert(_retValFromGeneratorLoop, returnType)))
					).Else(
						Ast.Return(returnLabelTarget, Ast.Assign(retVal, Ast.Default(returnType)))
					) : Ast.Block(
						Ast.Call(new Func<DebugThread, object>(RuntimeOps.GeneratorLoopProc).Method, _thread),
						Ast.Return(returnLabelTarget)
					),
				// catch �u���b�N�� try �u���b�N�Ɠ����^�ł��邱�Ƃ�ۏ؂���
					Ast.Default(returnType)
				).Finally(
				// debugMarker ���W�F�l���[�^���[�v�̌�ɍŐV�ł��邱�Ƃ�ۏ؂���
					Ast.Assign(_debugMarker, Ast.Call(new Func<DebugThread, int>(RuntimeOps.GetCurrentSequencePointForLeafGeneratorFrame).Method, _thread))
				)
			).Finally(
				AstUtils.If(popFrameCondition,
				// PopFrame �� true ��Ԃ�����A�X���b�h�I���C�x���g�𔭍s
					Ast.Call(new Action<DebugThread>(RuntimeOps.OnThreadExitEvent).Method, _thread)
				)
			).ToExpression();

			if (body.Type == typeof(void) && returnType != typeof(void))
				body = Ast.Block(parameters.Variables, body, Ast.Default(returnType));
			else
				body = Ast.Block(parameters.Variables, body);

			return Ast.Lambda(delegateType, Ast.Label(returnLabelTarget, body), name, parameters.Parameters);
		}

		static MSAst.LambdaExpression CreateGeneratorFactoryLambda(Ast body, string name, IEnumerable<MSAst.ParameterExpression> locals, IEnumerable<VariableInfo> variableInfos, MSAst.LabelTarget label, ParametersHolder parameters, Ast retVal)
		{
			body = Ast.Block(
				Ast.Call(new Action<DebugFrame, System.Runtime.CompilerServices.IRuntimeVariables>(RuntimeOps.ReplaceLiftedLocals).Method, _frame, Ast.RuntimeVariables(locals)),
				body
			);
			if (retVal != null)
				body = Ast.Block(
					Ast.Assign(retVal, body),
					AstUtils.YieldReturn(label, Ast.Convert(retVal, typeof(object)))
				);
			if (body.Type != typeof(void))
				body = AstUtils.Void(body);

			return AstUtils.GeneratorLambda(
				Ast.GetDelegateType(Enumerable.Repeat(typeof(DebugFrame), 1).Concat(variableInfos.Where(x => x.IsParameter).Select(x => x.VariableType)).Concat(Enumerable.Repeat(typeof(System.Collections.IEnumerable), 1)).ToArray()),
				label,
				Ast.Block(parameters.Variables, body),
				name,
				parameters.Parameters
			);
		}

		MSAst.LambdaExpression CreateOuterGeneratorFactory(Type delegateType, string name, Type generatorLabelType, ParametersHolder lambdaPs, Ast functionInfoInitializer)
		{
			var returnLabelTarget = Ast.Label(delegateType.GetMethod("Invoke").ReturnType);

			Ast body = Ast.Return(
				returnLabelTarget,
				Ast.Call(
					new Func<DebugFrame, IEnumerator<int>>(RuntimeOps.CreateDebugGenerator<int>).Method.GetGenericMethodDefinition().MakeGenericMethod(generatorLabelType),
					Ast.Call(
						new Func<DebugContext, FunctionInfo, DebugFrame>(RuntimeOps.CreateFrameForGenerator).Method,
						Ast.Constant(_debugContext),
						functionInfoInitializer
					)
				)
			);

			MSAst.LabelExpression returnLabel = null;
			if (returnLabelTarget.Type == typeof(void))
				returnLabel = Ast.Label(returnLabelTarget, Ast.Block(lambdaPs.Variables, body, Ast.Empty()));
			else
				returnLabel = Ast.Label(returnLabelTarget, Ast.Block(lambdaPs.Variables, AstUtils.Convert(body, returnLabelTarget.Type)));

			return Ast.Lambda(delegateType, returnLabel, name, lambdaPs.Parameters);
		}

		class PushFrameExpression
		{
			public PushFrameExpression(DebugContext debugContext, IEnumerable<MSAst.ParameterExpression> verifiedLocals, IEnumerable<VariableInfo> variableInfos)
			{
				Executor = Ast.Block(
					Ast.Assign(_framePushed, Ast.Constant(true)),
					Ast.Assign(_thread, AstUtils.SimpleCallHelper(new Func<DebugContext, DebugThread>(RuntimeOps.GetCurrentThread).Method, Ast.Constant(debugContext))), // �X���b�h���擾
					debugContext.ThreadFactory.CreatePushFrameExpression(_funcInfo, _debugMarker, verifiedLocals, variableInfos, _thread)
				);
				ConditionalExecutor = AstUtils.If(Ast.Not(_framePushed), Executor);
			}

			public readonly Ast Executor;
			public readonly Ast ConditionalExecutor;
		}

		class VariableLayout
		{
			public readonly List<MSAst.ParameterExpression> VerifiedLocals = new List<MSAst.ParameterExpression>();
			public readonly List<VariableInfo> VariableInfos = new List<VariableInfo>();
			public readonly Dictionary<MSAst.ParameterExpression, VariableInfo> PendingToVariableInfosMap = new Dictionary<MSAst.ParameterExpression, VariableInfo>();
			public readonly Dictionary<MSAst.ParameterExpression, MSAst.ParameterExpression> PendingToVerifiedLocalsMap = new Dictionary<MSAst.ParameterExpression, MSAst.ParameterExpression>();
		}

		class ParametersHolder
		{
			public readonly List<MSAst.ParameterExpression> Parameters = new List<MSAst.ParameterExpression>();
			public readonly List<MSAst.ParameterExpression> Variables = new List<MSAst.ParameterExpression>();
		}
	}
}
