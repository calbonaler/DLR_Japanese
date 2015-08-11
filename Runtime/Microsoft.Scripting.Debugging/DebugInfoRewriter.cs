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
using System.Linq;
using System.Globalization;
using Microsoft.Scripting.Debugging.CompilerServices;
using AstUtils = Microsoft.Scripting.Ast.Utils;
using MSAst = System.Linq.Expressions;

namespace Microsoft.Scripting.Debugging
{
	using Ast = MSAst.Expression;

	/// <summary><see cref="System.Linq.Expressions.DebugInfoExpression"/> を含む式をリライトするために使用されます。</summary>
	class DebugInfoRewriter : MSAst.ExpressionVisitor
	{
		readonly DebugContext _debugContext;
		readonly bool _transformToGenerator;
		readonly Ast _thread;
		readonly Ast _frame;
		readonly Ast _debugMarker;
		readonly Ast _traceLocations;
		readonly IReadOnlyDictionary<MSAst.ParameterExpression, MSAst.ParameterExpression> _replacedLocals;
		readonly IReadOnlyDictionary<MSAst.ParameterExpression, VariableInfo> _localsToVarInfos;
		readonly Stack<MSAst.BlockExpression> _currentLocals;
		readonly Dictionary<int, DebugSourceSpan> _markerLocationMap;
		readonly Dictionary<int, VariableInfo[]> _variableScopeMap;
		readonly Dictionary<MSAst.BlockExpression, VariableInfo[]> _variableScopeMapCache;
		readonly IReadOnlyDictionary<DebugSourceFile, MSAst.ParameterExpression> _sourceFilesToVariablesMap;
		readonly Ast _globalDebugMode;
		readonly MSAst.LabelTarget _generatorLabelTarget;
		readonly MSAst.ConstantExpression _debugYieldValue;
		readonly Ast _pushFrame;
		readonly DebugLambdaInfo _lambdaInfo;
		int _locationCookie;
		bool _insideConditionalBlock;

		internal DebugInfoRewriter(
			DebugContext debugContext,
			bool transformToGenerator,
			Ast traceLocations,
			Ast thread,
			Ast frame,
			Ast pushFrame,
			Ast debugMarker,
			Ast globalDebugMode,
			IReadOnlyDictionary<DebugSourceFile, MSAst.ParameterExpression> sourceFilesToVariablesMap,
			MSAst.LabelTarget generatorLabel,
			IReadOnlyDictionary<MSAst.ParameterExpression, MSAst.ParameterExpression> replacedLocals,
			IReadOnlyDictionary<MSAst.ParameterExpression, VariableInfo> localsToVarInfos,
			DebugLambdaInfo lambdaInfo)
		{

			_debugContext = debugContext;
			_transformToGenerator = transformToGenerator;
			_traceLocations = traceLocations;
			_thread = thread;
			_frame = frame;
			_pushFrame = pushFrame;

			if (_transformToGenerator)
			{
				_debugYieldValue = Ast.Constant(DebugContext.DebugYieldValue);

				// ジェネレータの変形中にマーカー位置および位置ハンドラマップも作成する
				_markerLocationMap = new Dictionary<int, DebugSourceSpan>();
				_variableScopeMap = new Dictionary<int, VariableInfo[]>();
				_currentLocals = new Stack<MSAst.BlockExpression>();
				_variableScopeMapCache = new Dictionary<MSAst.BlockExpression, VariableInfo[]>();
			}

			_debugMarker = debugMarker;
			_globalDebugMode = globalDebugMode;
			_sourceFilesToVariablesMap = sourceFilesToVariablesMap;
			_generatorLabelTarget = generatorLabel;
			_replacedLocals = replacedLocals;
			_localsToVarInfos = localsToVarInfos;
			_lambdaInfo = lambdaInfo;
		}

		internal DebugSourceSpan[] DebugMarkerLocationMap
		{
			get
			{
				return Enumerable.Range(0, _locationCookie).Select(i =>
				{
					DebugSourceSpan location;
					return _markerLocationMap.TryGetValue(i, out location) ? location : null;
				}).ToArray();
			}
		}

		internal bool HasUnconditionalFunctionCalls { get; private set; }

		internal VariableInfo[][] VariableScopeMap
		{
			get
			{
				return Enumerable.Range(0, _locationCookie).Select(i =>
				{
					VariableInfo[] scope;
					return _variableScopeMap.TryGetValue(i, out scope) ? scope : null;
				}).ToArray();
			}
		}

		// 明示的にラムダを走査しない。すでに変形されているはず
		protected override MSAst.Expression VisitLambda<T>(MSAst.Expression<T> node) { return node; }

		// ブロック内で宣言されたすべてのネストされた変数を取り除く
		protected override MSAst.Expression VisitBlock(MSAst.BlockExpression node)
		{
			if (_transformToGenerator)
				_currentLocals.Push(node);
			try { return base.VisitBlock(Ast.Block(node.Type, node.Expressions)); }
			finally
			{
				if (_transformToGenerator)
				{
#if DEBUG
					var poppedBlock =
#endif
					_currentLocals.Pop();
#if DEBUG
					Debug.Assert(Type.ReferenceEquals(node, poppedBlock));
#endif
				}
			}
		}

		protected override MSAst.Expression VisitTry(MSAst.TryExpression node)
		{
			var body = Visit(node.Body);
			var handlers = Visit(node.Handlers, VisitCatchBlock);
			var @finally = Visit(node.Finally);
			MSAst.Expression fault;

			_insideConditionalBlock = true;
			try { fault = Visit(node.Fault); }
			finally { _insideConditionalBlock = false; }

			node = Ast.MakeTry(node.Type, body, @finally, fault, handlers);

			ReadOnlyCollection<MSAst.CatchBlock> newHandlers = null;
			MSAst.Expression newFinally = null;

			// TryStatement に任意 catch ブロックがある場合、初回例外の通知がされるようにするために最初の文として例外イベントを挿入する必要がある。
			if (node.Handlers != null && node.Handlers.Count > 0)
			{
				var newHandlersMutable = new List<MSAst.CatchBlock>();
				foreach (var catchBlock in node.Handlers)
				{
					var exceptionVar = catchBlock.Variable ?? Ast.Parameter(catchBlock.Test, null);
					newHandlersMutable.Add(Ast.MakeCatchBlock(
						catchBlock.Test,
						exceptionVar,
						Ast.Block(
							// ForceToGeneratorLoopException を再スロー
							AstUtils.If(Ast.TypeIs(exceptionVar, typeof(ForceToGeneratorLoopException)),
								Ast.Throw(exceptionVar)
							),
							AstUtils.If(Ast.Equal(_globalDebugMode, AstUtils.Constant((int)DebugMode.FullyEnabled)),
								_pushFrame != null ? _pushFrame : Ast.Empty(),
								Ast.Call(
									new Action<DebugThread, int, Exception>(RuntimeOps.OnTraceEvent).Method,
									_transformToGenerator ? Ast.Call(new Func<DebugFrame, DebugThread>(RuntimeOps.GetThread).Method, _frame) : _thread,
									_transformToGenerator ? Ast.Call(new Func<DebugFrame, int>(RuntimeOps.GetCurrentSequencePointForGeneratorFrame).Method, _frame) : _debugMarker,
									exceptionVar
								)
							),
							catchBlock.Body
						),
						catchBlock.Filter
					));
				}
				newHandlers = Utils.CollectionUtils.ToReadOnly(newHandlersMutable);
			}

			if (!_transformToGenerator && node.Finally != null)
				// フレームが現在ジェネレータに再割り当てされている場合、ユーザーの finally ブロックが実行するのを防ぐ
				newFinally = AstUtils.If(Ast.Not(Ast.Call(new Func<DebugThread, bool>(RuntimeOps.IsCurrentLeafFrameRemappingToGenerator).Method, _thread)),
					node.Finally
				);

			if (newHandlers != null || newFinally != null)
			{
				node = Ast.MakeTry(
					node.Type,
					node.Body,
					newFinally ?? node.Finally,
					node.Fault,
					newHandlers ?? node.Handlers
				);
			}

			return node;
		}

		protected override MSAst.CatchBlock VisitCatchBlock(MSAst.CatchBlock node)
		{
			var variable = VisitAndConvert(node.Variable, "VisitCatchBlock");

			MSAst.Expression filter;
			MSAst.Expression body;

			_insideConditionalBlock = true;
			try
			{
				filter = Visit(node.Filter);
				body = Visit(node.Body);
			}
			finally { _insideConditionalBlock = false; }

			if (variable == node.Variable && body == node.Body && filter == node.Filter)
				return node;
			return Ast.MakeCatchBlock(node.Test, variable, body, filter);
		}

		protected override MSAst.Expression VisitConditional(MSAst.ConditionalExpression node)
		{
			MSAst.Expression test = Visit(node.Test);

			MSAst.Expression left;
			MSAst.Expression right;

			_insideConditionalBlock = true;
			try
			{
				left = Visit(node.IfTrue);
				right = Visit(node.IfFalse);
			}
			finally { _insideConditionalBlock = false; }

			if (test == node.Test && left == node.IfTrue && right == node.IfFalse)
				return node;
			return Ast.Condition(test, left, right, node.Type);
		}

		protected override MSAst.SwitchCase VisitSwitchCase(MSAst.SwitchCase node)
		{
			_insideConditionalBlock = true;
			try { return base.VisitSwitchCase(node); }
			finally { _insideConditionalBlock = false; }
		}

		protected override MSAst.Expression VisitDynamic(MSAst.DynamicExpression node) { return VisitCall(base.VisitDynamic(node)); }

		protected override MSAst.Expression VisitMethodCall(MSAst.MethodCallExpression node) { return VisitCall(base.VisitMethodCall(node)); }

		protected override MSAst.Expression VisitInvocation(MSAst.InvocationExpression node) { return VisitCall(base.VisitInvocation(node)); }

		protected override MSAst.Expression VisitNew(MSAst.NewExpression node) { return VisitCall(base.VisitNew(node)); }

		// このメソッドは次の 2 つを行う:
		//  1. 呼び出し式が条件ブロック内に存在するかどうかの記録
		//  2. 呼び出し式の前のフレームプッシュ式の挿入
		//     これは 2 回目のツリー走査で、無条件関数呼び出しが存在しない場合にのみ行われる。
		internal MSAst.Expression VisitCall(MSAst.Expression node)
		{
			if (_lambdaInfo.OptimizeForLeafFrames && (_lambdaInfo.CompilerSupport == null || _lambdaInfo.CompilerSupport.IsCallToDebuggableLambda(node)))
			{
				// 条件ブロック内でない場合、無条件関数呼び出しが存在することを記録
				if (!_insideConditionalBlock)
					HasUnconditionalFunctionCalls = true;
				// プッシュフレーム式の挿入
				if (!_transformToGenerator && _pushFrame != null)
					return Ast.Block(_pushFrame, node);
			}
			return node;
		}

		protected override MSAst.Expression VisitParameter(MSAst.ParameterExpression node)
		{
			if (_replacedLocals == null)
				return base.VisitParameter(node);
			MSAst.ParameterExpression replacement;
			if (_replacedLocals.TryGetValue(node, out replacement))
				return replacement;
			else
				return base.VisitParameter(node);
		}

		protected override MSAst.Expression VisitDebugInfo(MSAst.DebugInfoExpression node)
		{
			if (node.IsClear)
				return Ast.Empty();
			MSAst.Expression transformedExpression;
			// DebugInfoExpression に有効な SymbolDocumentInfo があるかを検証
			if (node.Document == null || String.IsNullOrEmpty(node.Document.FileName))
				throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, ErrorStrings.DebugInfoWithoutSymbolDocumentInfo, _locationCookie));
			var sourceFile = _debugContext.GetDebugSourceFile(node.Document.FileName);
			// 位置クッキーを更新
			int locationCookie = _locationCookie++;
			if (!_transformToGenerator)
				transformedExpression = Ast.Block(
					Ast.Assign(_debugMarker, AstUtils.Constant(locationCookie)),
					Ast.IfThen(Ast.GreaterThan(Ast.Property(_sourceFilesToVariablesMap[sourceFile], "Mode"), Ast.Constant((int)DebugMode.ExceptionsOnly)),
						AstUtils.IfThen(
							Ast.OrElse(
								Ast.Equal(Ast.Property(_sourceFilesToVariablesMap[sourceFile], "Mode"), Ast.Constant((int)DebugMode.FullyEnabled)),
								Ast.ArrayIndex(_traceLocations, AstUtils.Constant(locationCookie))
							),
							_pushFrame != null ? _pushFrame : Ast.Empty(),
							locationCookie == 0 ?
								(Ast)Ast.Empty() :
								Ast.Call(new Action<DebugThread, int, Exception>(RuntimeOps.OnTraceEvent).Method, _thread, AstUtils.Constant(locationCookie), Ast.Convert(Ast.Constant(null), typeof(Exception)))
						)
					)
				);
			else
			{
				Debug.Assert(_generatorLabelTarget != null);

				transformedExpression = Ast.Block(
					AstUtils.YieldReturn(
						_generatorLabelTarget,
						_debugYieldValue,
						locationCookie
					)
				);

				// 変数スコープマップを更新
				if (_currentLocals.Count > 0)
				{
					VariableInfo[] scopedVariables;
					var currentBlock = _currentLocals.Peek();
					if (!_variableScopeMapCache.TryGetValue(currentBlock, out scopedVariables))
						_variableScopeMapCache.Add(currentBlock, scopedVariables = _currentLocals.Reverse().SelectMany(x => x.Variables.Select(y => _localsToVarInfos[y])).ToArray());
					_variableScopeMap.Add(locationCookie, scopedVariables);
				}

				var span = new DebugSourceSpan(sourceFile, node.StartLine, node.StartColumn, node.EndLine, node.EndColumn);

				// 位置-区間マップを更新
				_markerLocationMap.Add(locationCookie, span);
			}

			return transformedExpression;
		}
	}
}
