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
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Debugging.CompilerServices;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Debugging
{
	public sealed class DebugFrame
	{
		Exception _thrownException;
		IRuntimeVariables _liftedLocals;
		Dictionary<IList<VariableInfo>, ScopeData> _variables;

		// Symbol used to set "$exception" variable when exceptions are thrown
		static readonly SymbolId _exceptionVariableSymbol = SymbolTable.StringToId("$exception");

		internal DebugFrame(DebugThread thread, FunctionInfo funcInfo)
		{
			LastKnownGeneratorYieldMarker = Int32.MaxValue;
			Thread = thread;
			FunctionInfo = funcInfo;
			_variables = new Dictionary<IList<VariableInfo>, ScopeData>();
		}

		internal DebugFrame(DebugThread thread, FunctionInfo funcInfo, IRuntimeVariables liftedLocals, int frameOrder) : this(thread, funcInfo)
		{
			_liftedLocals = liftedLocals;
			StackDepth = frameOrder;
		}

		/// <summary>スレッドを取得します。</summary>
		internal DebugThread Thread { get; private set; }

		/// <summary>スタックの深さ (フレームの順序) を取得または設定します。</summary>
		internal int StackDepth { get; set; }

		/// <summary>変数を取得します。</summary>
		internal VariableInfo[] Variables
		{
			get
			{
				var locals = LocalsInCurrentScope;
				var scopeData = GetScopeDataForLocals(locals);
				if (_thrownException == null ? scopeData.VarInfos == null : scopeData.VarInfosWithException == null)
				{
					scopeData.VarInfos = FunctionInfo.Variables.Where(x => x.IsParameter && !x.IsHidden).Concat(locals.Where(x => !x.IsHidden)).ToArray();
					scopeData.VarInfosWithException = Utils.ArrayUtils.Append(scopeData.VarInfos, new VariableInfo(_exceptionVariableSymbol, typeof(Exception), false, false, false));
				}
				return _thrownException == null ? scopeData.VarInfos : scopeData.VarInfosWithException;
			}
		}

		/// <summary>現在のシーケンスポイントのインデックスを取得または設定します。</summary>
		internal int CurrentSequencePointIndex
		{
			get
			{
				int debugMarker = CurrentLocationCookie;
				if (debugMarker >= FunctionInfo.SequencePoints.Length)
				{
					Debug.Fail("DebugMarker がどの位置にも一致しません。");
					debugMarker = 0;
				}
				return debugMarker;
			}
			set
			{
				if (value < 0 || value >= FunctionInfo.SequencePoints.Length)
					throw new ArgumentOutOfRangeException("value");

				// 位置はトレースイベント内のリーフフレーム内でのみ変更可能
				if (!IsInTraceback)
				{
					Debug.Fail("フレームはトレースイベントにありません。");
					throw new InvalidOperationException(ErrorStrings.JumpNotAllowedInNonLeafFrames);
				}

				var needsGenerator = (value != CurrentLocationCookie || _thrownException != null);

				// 異なる位置へ変更しようとしているかスローされた例外がある場合はジェネレータを再割り当て
				if (Generator == null && needsGenerator)
				{
					RemapToGenerator(FunctionInfo.Version);
					Debug.Assert(Generator != null);
				}

				// 本当に必要な場合のみ位置を変更する
				if (value != CurrentLocationCookie)
				{
					Debug.Assert(Generator != null);
					Generator.YieldMarkerLocation = value;
				}

				// 位置が変更されたかどうかに関わらず保留中の例外はキャンセルする必要がある
				ThrownException = null;

				// 現在のイベントがジェネレータループから来ていない場合、ジェネレータループへ行くようにする
				if (!InGeneratorLoop && needsGenerator)
					ForceSwitchToGeneratorLoop = true;
			}
		}

		internal void RemapToLatestVersion()
		{
			RemapToGenerator(Int32.MaxValue);
			// ジェネレータループへ強制する
			if (!InGeneratorLoop)
				ForceSwitchToGeneratorLoop = true;
		}

		internal FunctionInfo FunctionInfo { get; private set; }

		internal Exception ThrownException
		{
			get { return _thrownException; }
			set
			{
				if (_thrownException != null && value == null)
				{
					_thrownException = null;
					GetLocalsScope().Remove(_exceptionVariableSymbol);
				}
				else if (value != null && !GetLocalsScope().ContainsKey(_exceptionVariableSymbol))
				{
					_thrownException = value;
					GetLocalsScope()[_exceptionVariableSymbol] = _thrownException;
				}
			}
		}

		internal IDebuggableGenerator Generator { get; private set; }

		internal bool IsInTraceback { get; set; }

		internal bool InGeneratorLoop { get; set; }

		internal bool ForceSwitchToGeneratorLoop { get; set; }

		internal DebugContext DebugContext { get { return Thread.DebugContext; } }

		internal int CurrentLocationCookie
		{
			get
			{
				Debug.Assert(Generator != null || _liftedLocals is IDebugRuntimeVariables);
				return (Generator == null ? ((IDebugRuntimeVariables)_liftedLocals).DebugMarker :
					(Generator.YieldMarkerLocation != Int32.MaxValue ? Generator.YieldMarkerLocation : LastKnownGeneratorYieldMarker));
			}
		}

		internal int LastKnownGeneratorYieldMarker { get; set; }

		/// <summary>
		/// ジェネレータからジェネレータのローカル変数を使用してフレームを更新するために呼ばれます。
		/// </summary>
		internal void ReplaceLiftedLocals(IRuntimeVariables liftedLocals)
		{
			Debug.Assert(_liftedLocals == null || liftedLocals.Count >= _liftedLocals.Count);

			var oldLiftecLocals = _liftedLocals;

			// IStrongBox のリストを新しいリストで置き換える
			_liftedLocals = liftedLocals;

			if (oldLiftecLocals != null)
			{
				for (int i = 0; i < oldLiftecLocals.Count; i++)
				{
					if (!FunctionInfo.Variables[i].IsParameter && i < _liftedLocals.Count)
						_liftedLocals[i] = oldLiftecLocals[i];
				}
			}

			// スコープや変数の状態をクリアして新しい状態の作成を強制する
			_variables.Clear();
		}

		/// <summary>フレームの状態をジェネレータが実行のために使用できるように再割り当てします。</summary>
		/// <param name="version"><see cref="Int32.MaxValue"/> を指定すると最新のバージョンを割り当てます。</param>
		internal void RemapToGenerator(int version)
		{
			Debug.Assert(Generator == null || FunctionInfo.Version != version);

			// 指定されたバージョンに対して対象となる FunctionInfo の検索を試みる
			var targetFuncInfo = GetFunctionInfo(version);
			if (targetFuncInfo == null)
				throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, ErrorStrings.InvalidFunctionVersion, version));

			// 新しいジェネレータを作成
			Generator = (IDebuggableGenerator)targetFuncInfo.GeneratorFactory.DynamicInvoke(Enumerable.Repeat(this, 1).Concat(FunctionInfo.Variables.Where(x => x.IsParameter).Select((x, i) => _liftedLocals[i])).ToArray());
			// FunctionInfo を新しいバージョンに更新
			if (FunctionInfo != targetFuncInfo)
				FunctionInfo = targetFuncInfo;

			// 最初の yield 位置へ移動し実行
			((IEnumerator)Generator).MoveNext();
		}

		internal IAttributesCollection GetLocalsScope()
		{
			var locals = LocalsInCurrentScope;
			var scopeData = GetScopeDataForLocals(locals);
			if (scopeData.Scope == null)
			{
				Debug.Assert(_liftedLocals != null);
				var visibleLocals = FunctionInfo.Variables.Where(x => x.IsParameter && !x.IsHidden).Concat(locals.Where(x => !x.IsHidden)).ToArray();
				scopeData.Scope = new LocalsDictionary(new ScopedRuntimeVariables(visibleLocals, _liftedLocals), visibleLocals.Select(x => x.Symbol));
			}
			return scopeData.Scope;
		}

		FunctionInfo GetFunctionInfo(int version)
		{
			if (version == FunctionInfo.Version)
				return FunctionInfo;

			FunctionInfo lastFuncInfo = null;
			for (var funcInfo = FunctionInfo; funcInfo != null; )
			{
				if (funcInfo.Version == version)
					return funcInfo;
				lastFuncInfo = funcInfo;
				if (version > funcInfo.Version)
					funcInfo = funcInfo.NextVersion;
				else
					funcInfo = funcInfo.PreviousVersion;
			}

			// バージョンが Int32.MaxValue ならば最新のファクトリを返す
			if (version == Int32.MaxValue)
				return lastFuncInfo;

			return null;
		}

		ScopeData GetScopeDataForLocals(IList<VariableInfo> locals)
		{
			ScopeData scopeData;
			if (!_variables.TryGetValue(locals, out scopeData))
				_variables[locals] = scopeData = new ScopeData();
			return scopeData;
		}

		IList<VariableInfo> LocalsInCurrentScope
		{
			get
			{
				var locals = CurrentLocationCookie < FunctionInfo.VariableScopeMap.Length ? FunctionInfo.VariableScopeMap[CurrentLocationCookie] : null;
				if (locals == null)
				{
					Debug.Fail("DebugMarker はどのスコープにも一致しませんでした。");
					// "無効" な位置に対する変数を保持する組に対するキーとして null を使う
					locals = FunctionInfo.VariableScopeMap[0];
				}

				return locals;
			}
		}

		class ScopeData
		{
			public VariableInfo[] VarInfos;
			public VariableInfo[] VarInfosWithException;
			public IAttributesCollection Scope;
		}
	}
}
