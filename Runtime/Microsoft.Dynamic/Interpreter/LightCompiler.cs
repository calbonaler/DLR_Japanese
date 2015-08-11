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
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;
using AstUtils = Microsoft.Scripting.Ast.Utils;

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>例外を捕捉するハンドラを表します。</summary>
	public sealed class ExceptionHandler
	{
		/// <summary>捕捉できる例外の型を取得します。</summary>
		public Type ExceptionType { get; private set; }

		/// <summary>例外を捕捉できる最初の命令を示すインデックスを取得します。</summary>
		public int StartIndex { get; private set; }

		/// <summary>例外を捕捉できる最後の命令の次の命令を示すインデックスを取得します。</summary>
		public int EndIndex { get; private set; }

		/// <summary>例外ハンドラの開始点を示すラベルのインデックスを取得します。</summary>
		public int LabelIndex { get; private set; }

		/// <summary>例外ハンドラ開始後の最初の命令を示すインデックスを取得します。</summary>
		public int HandlerStartIndex { get; private set; }

		/// <summary>この例外ハンドラが fault 節を表すかどうかを示す値を取得します。</summary>
		public bool IsFault { get { return ExceptionType == null; } }

		/// <summary>指定された引数を使用して、<see cref="Microsoft.Scripting.Interpreter.ExceptionHandler"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="start">例外を捕捉できる最初の命令を示すインデックスを指定します。</param>
		/// <param name="end">例外を捕捉できる最後の命令の次の命令を示すインデックスを指定します。</param>
		/// <param name="labelIndex">例外ハンドラの開始点を示すラベルのインデックスを指定します。</param>
		/// <param name="handlerStartIndex">例外ハンドラ開始後の最初の命令を示すインデックスを指定します。</param>
		/// <param name="exceptionType">捕捉できる例外の型を指定します。fault 節の場合は null を指定します。</param>
		internal ExceptionHandler(int start, int end, int labelIndex, int handlerStartIndex, Type exceptionType)
		{
			StartIndex = start;
			EndIndex = end;
			LabelIndex = labelIndex;
			ExceptionType = exceptionType;
			HandlerStartIndex = handlerStartIndex;
		}

		/// <summary>この例外ハンドラが指定された場所で発生した指定された型の例外を捕捉できるかどうかを判断します。</summary>
		/// <param name="exceptionType">発生した例外の型を指定します。</param>
		/// <param name="index">例外が発生した命令を示すインデックスを指定します。</param>
		/// <returns>指定された例外をこのハンドラが捕捉できる場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool Matches(Type exceptionType, int index) { return IsInside(index) && (ExceptionType == null || ExceptionType.IsAssignableFrom(exceptionType)); }

		/// <summary>この例外ハンドラが指定された例外ハンドラよりも捕捉に適しているかどうかを判断します。</summary>
		/// <param name="other">比較する例外ハンドラを指定します。</param>
		/// <returns>この例外ハンドラが指定された例外ハンドラよりも捕捉に適している場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		public bool IsBetterThan(ExceptionHandler other) { return other == null || (StartIndex == other.StartIndex && EndIndex == other.EndIndex ? HandlerStartIndex < other.HandlerStartIndex : StartIndex > other.StartIndex || EndIndex < other.EndIndex); }

		/// <summary>指定された命令インデックスがこの例外ハンドラが例外を捕捉できる場所かどうかを判断します。</summary>
		/// <param name="index">調べる命令インデックスを指定します。</param>
		/// <returns>指定された命令インデックスがこの例外ハンドラが例外を捕捉できる場所の場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		internal bool IsInside(int index) { return index >= StartIndex && index < EndIndex; }

		/// <summary>このオブジェクトの文字列表現を返します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		public override string ToString() { return string.Format("{0} [{1}-{2}] [{3}->]", IsFault ? "fault" : "catch(" + ExceptionType.Name + ")", StartIndex, EndIndex, HandlerStartIndex); }
	}

	/// <summary>コードに付加されたデバッグ情報を表します。</summary>
	[Serializable]
	public class DebugInfo
	{
		/// <summary>ソースコード内の開始行を取得します。</summary>
		public int StartLine { get; private set; }

		/// <summary>ソースコード内の終了行を取得します。</summary>
		public int EndLine { get; private set; }

		/// <summary>デバッグ情報が付加された次の命令を示すインデックスを取得します。</summary>
		public int Index { get; private set; }

		/// <summary>デバッグ情報が示すソースファイルのファイル名を取得します。</summary>
		public string FileName { get; private set; }

		/// <summary>デバッグ情報がシーケンスポイントをクリアするために使用されるかどうかを示す値を取得します。</summary>
		public bool IsClear { get; private set; }

		static readonly Comparer<DebugInfo> _debugComparer = Comparer<DebugInfo>.Create((x, y) => x.Index.CompareTo(y.Index));

		/// <summary>指定された情報を使用して、<see cref="Microsoft.Scripting.Interpreter.DebugInfo"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="startLine">ソースコード内の開始行を指定します。</param>
		/// <param name="endLine">ソースコード内の終了行を指定します。</param>
		/// <param name="index">デバッグ情報が付加された次の命令を示すインデックスを指定します。</param>
		/// <param name="fileName">デバッグ情報が示すソースファイルのファイル名を指定します。</param>
		/// <param name="clear">デバッグ情報がシーケンスポイントをクリアするために使用されるかどうかを示す値を指定します。</param>
		public DebugInfo(int startLine, int endLine, int index, string fileName, bool clear)
		{
			StartLine = startLine;
			EndLine = endLine;
			Index = index;
			FileName = fileName;
			IsClear = clear;
		}

		/// <summary>指定された <see cref="DebugInfo"/> の配列の中から指定された命令インデックス以下の命令インデックスをもつ最後の要素を返します。</summary>
		/// <param name="debugInfos">指定された命令インデックス以下の最後の要素を検索する <see cref="DebugInfo"/> の配列を指定します。</param>
		/// <param name="index">検索する命令インデックスを指定します。</param>
		/// <returns>指定された命令インデックス以下の命令インデックスをもつ最後の要素。そのような要素が存在しない場合は <c>null</c> を返します。</returns>
		public static DebugInfo GetMatchingDebugInfo(DebugInfo[] debugInfos, int index)
		{
			// 検索に使用するための DebugInfo を作成し、現在のインデックスの前の最も近い DebugInfo を検索
			int i = Array.BinarySearch(debugInfos, new DebugInfo(0, 0, index, null, false), _debugComparer);
			if (i < 0)
			{
				// ~i は最初の大きな要素に対するインデックスで、大きな要素が存在しない場合、~i は配列の長さ
				i = ~i;
				if (i == 0)
					return null;
				// 小さな最後の要素を返す
				i = i - 1;
			}
			return debugInfos[i];
		}

		/// <summary>このオブジェクトの文字列表現を返します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		public override string ToString() { return IsClear ? string.Format("{0}: clear", Index) : string.Format("{0}: [{1}-{2}] '{3}'", Index, StartLine, EndLine, FileName); }
	}

	/// <summary>インタプリタのスタックフレームに関する情報を表します。</summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]　// TODO:
	[Serializable]
	public struct InterpretedFrameInfo
	{
		/// <summary>対象のスタックフレームが実行してるメソッドの名前を取得します。</summary>
		public string MethodName { get; private set; }

		/// <summary>対象のスタックフレームで現在実行している命令の近くにあるデバッグ情報を取得します。</summary>
		public DebugInfo DebugInfo { get; private set; }

		/// <summary>実行しているメソッド名と実行している命令の近くにあるデバッグ情報を使用して、<see cref="Microsoft.Scripting.Interpreter.InterpretedFrameInfo"/> 構造体の新しいインスタンスを初期化します。</summary>
		/// <param name="methodName">対象のスタックフレームが現在実行しているメソッドの名前を指定します。</param>
		/// <param name="info">対象のスタックフレームで現在実行している命令の近くにあるデバッグ情報を指定します。</param>
		public InterpretedFrameInfo(string methodName, DebugInfo info)
			: this()
		{
			MethodName = methodName;
			DebugInfo = info;
		}

		/// <summary>このオブジェクトの文字列表現を返します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		public override string ToString() { return MethodName + (DebugInfo != null ? ": " + DebugInfo.ToString() : null); }
	}

	/// <summary>式ツリーをインタプリタで実行可能な命令列にコンパイルする軽量コンパイラを表します。</summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
	public sealed class LightCompiler
	{
		static readonly MethodInfo _RunMethod = typeof(Interpreter).GetMethod("Run");
		static readonly MethodInfo _GetCurrentMethod = typeof(MethodBase).GetMethod("GetCurrentMethod");

#if DEBUG
		static LightCompiler() { Debug.Assert(_GetCurrentMethod != null && _RunMethod != null); }
#endif

		readonly int _compilationThreshold; // zero: sync compilation
		readonly List<ExceptionHandler> _handlers = new List<ExceptionHandler>();
		readonly List<DebugInfo> _debugInfos = new List<DebugInfo>();
		readonly List<UpdateStackTraceInstruction> _stackTraceUpdates = new List<UpdateStackTraceInstruction>();
		readonly Dictionary<LabelTarget, BranchLabel> _treeLabels = new Dictionary<LabelTarget, BranchLabel>();
		readonly Stack<ParameterExpression> _exceptionForRethrowStack = new Stack<ParameterExpression>();

		// Set to true to force compiliation of this lambda.
		// This disables the interpreter for this lambda.
		// We still need to walk it, however, to resolve variables closed over from the parent lambdas (because they may be interpreted).
		bool _forceCompile;

		readonly LightCompiler _parent;

		/// <summary>コンパイルに必要なコード実行回数を指定して、<see cref="Microsoft.Scripting.Interpreter.LightCompiler"/> クラスの新しいスタンスを初期化します。</summary>
		/// <param name="compilationThreshold">JIT コードへのコンパイルまでに実行されるべき実行回数を指定します。</param>
		internal LightCompiler(int compilationThreshold)
		{
			Instructions = new InstructionList();
			Locals = new LocalVariables();
			_compilationThreshold = compilationThreshold < 0 ? 32 : compilationThreshold;
		}

		LightCompiler(LightCompiler parent) : this(parent._compilationThreshold) { _parent = parent; }

		/// <summary>コンパイラが生成する疑似命令のリストを取得します。</summary>
		public InstructionList Instructions { get; private set; }

		/// <summary>コンパイラが生成するローカル変数のリストを取得します。</summary>
		public LocalVariables Locals { get; private set; }

		/// <summary>指定された <see cref="StrongBox&lt;Object&gt;"/> を表す式から参照する値を取得する式を返します。</summary>
		/// <param name="strongBoxExpression">参照する値を取得する <see cref="StrongBox&lt;Object&gt;"/> を表す式を指定します。</param>
		/// <returns>指定された式から取得された値を表す式。</returns>
		internal static Expression Unbox(Expression strongBoxExpression) { return Expression.Field(strongBoxExpression, typeof(StrongBox<object>).GetField("Value")); }

		/// <summary>指定されたラムダ式をコンパイルすることで、インタプリタを用いるデリゲートを作成できる <see cref="LightDelegateCreator"/> を返します。</summary>
		/// <param name="node">コンパイルするラムダ式を指定します。</param>
		/// <returns>インタプリタを用いるデリゲートを作成できる <see cref="LightDelegateCreator"/>。</returns>
		internal LightDelegateCreator CompileTop(LambdaExpression node)
		{
			foreach (var p in node.Parameters)
				Instructions.EmitInitializeParameter(Locals.DefineLocal(p, 0).Index);
			Compile(node.Body);
			// pop the result of the last expression:
			if (node.Body.Type != typeof(void) && node.ReturnType == typeof(void))
				Instructions.EmitPop();
			Debug.Assert(Instructions.CurrentStackDepth == (node.ReturnType != typeof(void) ? 1 : 0));
			return new LightDelegateCreator(MakeInterpreter(node), node);
		}

		Interpreter MakeInterpreter(LambdaExpression lambda)
		{
			if (_forceCompile)
				return null;
			var debugInfos = _debugInfos.ToArray();
			foreach (var stackTraceUpdate in _stackTraceUpdates)
				stackTraceUpdate._debugInfos = debugInfos;
			return new Interpreter(lambda, Locals, _treeLabels, Instructions.ToArray(), _handlers.ToArray(), debugInfos, _compilationThreshold);
		}

		BranchLabel MapLabel(LabelTarget target)
		{
			BranchLabel label;
			if (!_treeLabels.TryGetValue(target, out label))
				_treeLabels[target] = label = Instructions.MakeLabel();
			return label;
		}

		void CompileConstantExpression(ConstantExpression expr) { Instructions.EmitLoad(expr.Value, expr.Type); }

		void CompileDefaultExpression(Expression expr) { CompileDefaultExpression(expr.Type); }

		void CompileDefaultExpression(Type type)
		{
			if (type != typeof(void))
			{
				if (type.IsValueType)
				{
					var value = ScriptingRuntimeHelpers.GetPrimitiveDefaultValue(type);
					if (value != null)
						Instructions.EmitLoad(value);
					else
						Instructions.EmitDefaultValue(type);
				}
				else
					Instructions.EmitLoad(null);
			}
		}

		LocalVariable EnsureAvailableForClosure(ParameterExpression expr)
		{
			LocalVariable local;
			if (Locals.TryGetLocalOrClosure(expr, out local))
			{
				if (!local.InClosure && !local.IsBoxed)
					Locals.Box(expr, Instructions);
				return local;
			}
			else if (_parent != null)
			{
				_parent.EnsureAvailableForClosure(expr);
				return Locals.AddClosureVariable(expr);
			}
			else
				throw new InvalidOperationException("束縛されていない変数: " + expr);
		}

		void EnsureVariable(ParameterExpression variable)
		{
			if (!Locals.ContainsVariable(variable))
				EnsureAvailableForClosure(variable);
		}

		LocalVariable ResolveLocal(ParameterExpression variable)
		{
			LocalVariable local;
			if (!Locals.TryGetLocalOrClosure(variable, out local))
				local = EnsureAvailableForClosure(variable);
			return local;
		}

		/// <summary>指定された <see cref="ParameterExpression"/> に対するローカル変数の値を取得する命令を命令リストに追加します。</summary>
		/// <param name="variable">取得される値を格納するローカル変数に対応する <see cref="ParameterExpression"/> を指定します。</param>
		public void CompileGetVariable(ParameterExpression variable)
		{
			var local = ResolveLocal(variable);
			if (local.InClosure)
				Instructions.EmitLoadLocalFromClosure(local.Index);
			else if (local.IsBoxed)
				Instructions.EmitLoadLocalBoxed(local.Index);
			else
				Instructions.EmitLoadLocal(local.Index);
			Instructions.SetDebugCookie(variable.Name);
		}

		/// <summary>指定されたボックス化された <see cref="ParameterExpression"/> に対するローカル変数の値を取得する命令リストに追加します。</summary>
		/// <param name="variable">取得される値を格納するローカル変数に対応する <see cref="ParameterExpression"/> を指定します。</param>
		public void CompileGetBoxedVariable(ParameterExpression variable)
		{
			var local = ResolveLocal(variable);
			if (local.InClosure)
				Instructions.EmitLoadLocalFromClosureBoxed(local.Index);
			else
			{
				Debug.Assert(local.IsBoxed);
				Instructions.EmitLoadLocal(local.Index);
			}
			Instructions.SetDebugCookie(variable.Name);
		}

		/// <summary>指定された <see cref="ParameterExpression"/> に対するローカル変数に値を格納する命令をリストに追加します。</summary>
		/// <param name="variable">値を格納するローカル変数に対応する <see cref="ParameterExpression"/> を指定します。</param>
		/// <param name="isVoid">格納された値を評価スタックからポップするかどうかを示す値を指定します。</param>
		public void CompileSetVariable(ParameterExpression variable, bool isVoid)
		{
			LocalVariable local = ResolveLocal(variable);
			if (local.InClosure)
			{
				if (isVoid)
					Instructions.EmitStoreLocalToClosure(local.Index);
				else
					Instructions.EmitAssignLocalToClosure(local.Index);
			}
			else if (local.IsBoxed)
			{
				if (isVoid)
					Instructions.EmitStoreLocalBoxed(local.Index);
				else
					Instructions.EmitAssignLocalBoxed(local.Index);
			}
			else if (isVoid)
				Instructions.EmitStoreLocal(local.Index);
			else
				Instructions.EmitAssignLocal(local.Index);
			Instructions.SetDebugCookie(variable.Name);
		}

		void CompileParameterExpression(ParameterExpression expr) { CompileGetVariable(expr); }

		void CompileBlockExpression(BlockExpression expr, bool asVoid)
		{
			var end = CompileBlockStart(expr);
			if (asVoid)
				CompileAsVoid(expr.Expressions[expr.Expressions.Count - 1]);
			else
				Compile(expr.Expressions[expr.Expressions.Count - 1]);
			CompileBlockEnd(end);
		}

		LocalDefinition[] CompileBlockStart(BlockExpression node)
		{
			var start = Instructions.Count;
			// TODO: pop these off a stack when exiting
			// TODO: basic flow analysis so we don't have to initialize all variables.
			var locals = new LocalDefinition[node.Variables.Count];
			int localCnt = 0;
			foreach (var variable in node.Variables)
			{
				var local = Locals.DefineLocal(variable, start);
				locals[localCnt++] = local;
				Instructions.EmitInitializeLocal(local.Index, variable.Type);
				Instructions.SetDebugCookie(variable.Name);
			}
			for (int i = 0; i < node.Expressions.Count - 1; i++)
				CompileAsVoid(node.Expressions[i]);
			return locals;
		}

		void CompileBlockEnd(LocalDefinition[] locals)
		{
			foreach (var local in locals)
				Locals.UndefineLocal(local, Instructions.Count);
		}

		void CompileIndexExpression(IndexExpression expr)
		{
			// instance:
			if (expr.Object != null)
				Compile(expr.Object);
			// indexes, byref args not allowed.
			foreach (var arg in expr.Arguments)
				Compile(arg);
			if (expr.Indexer != null)
				Instructions.EmitCall(expr.Indexer.GetGetMethod(true));
			else if (expr.Arguments.Count != 1)
				Instructions.EmitCall(expr.Object.Type.GetMethod("Get", BindingFlags.Public | BindingFlags.Instance));
			else
				Instructions.EmitGetArrayItem(expr.Object.Type);
		}

		void CompileIndexAssignment(BinaryExpression node, bool asVoid)
		{
			var index = (IndexExpression)node.Left;
			if (!asVoid)
				throw new NotImplementedException();
			// instance:
			if (index.Object != null)
				Compile(index.Object);
			// indexes, byref args not allowed.
			foreach (var arg in index.Arguments)
				Compile(arg);
			// value:
			Compile(node.Right);
			if (index.Indexer != null)
				Instructions.EmitCall(index.Indexer.GetSetMethod(true));
			else if (index.Arguments.Count != 1)
				Instructions.EmitCall(index.Object.Type.GetMethod("Set", BindingFlags.Public | BindingFlags.Instance));
			else
				Instructions.EmitSetArrayItem(index.Object.Type);
		}

		void CompileMemberAssignment(BinaryExpression node, bool asVoid)
		{
			var member = (MemberExpression)node.Left;
			var pi = member.Member as PropertyInfo;
			if (pi != null)
			{
				var method = pi.GetSetMethod(true);
				Compile(member.Expression);
				Compile(node.Right);
				if (!asVoid)
				{
					var local = Locals.DefineLocal(Expression.Parameter(node.Right.Type), Instructions.Count);
					Instructions.EmitAssignLocal(local.Index);
					Instructions.EmitCall(method);
					Instructions.EmitLoadLocal(local.Index);
					Locals.UndefineLocal(local, Instructions.Count);
				}
				else
					Instructions.EmitCall(method);
				return;
			}
			var fi = member.Member as FieldInfo;
			if (fi != null)
			{
				if (member.Expression != null)
					Compile(member.Expression);
				Compile(node.Right);
				if (!asVoid)
				{
					var local = Locals.DefineLocal(Expression.Parameter(node.Right.Type), Instructions.Count);
					Instructions.EmitAssignLocal(local.Index);
					Instructions.EmitStoreField(fi);
					Instructions.EmitLoadLocal(local.Index);
					Locals.UndefineLocal(local, Instructions.Count);
				}
				else
					Instructions.EmitStoreField(fi);
				return;
			}
			throw new NotImplementedException();
		}

		void CompileVariableAssignment(BinaryExpression node, bool asVoid)
		{
			Compile(node.Right);
			CompileSetVariable((ParameterExpression)node.Left, asVoid);
		}

		void CompileAssignBinaryExpression(BinaryExpression expr, bool asVoid)
		{
			switch (expr.Left.NodeType)
			{
				case ExpressionType.Index:
					CompileIndexAssignment(expr, asVoid);
					break;
				case ExpressionType.MemberAccess:
					CompileMemberAssignment(expr, asVoid);
					break;
				case ExpressionType.Parameter:
				case ExpressionType.Extension:
					CompileVariableAssignment(expr, asVoid);
					break;
				default:
					throw new InvalidOperationException("代入の無効な左辺値: " + expr.Left.NodeType);
			}
		}

		void CompileBinaryExpression(BinaryExpression expr)
		{
			if (expr.Method != null)
			{
				Compile(expr.Left);
				Compile(expr.Right);
				Instructions.EmitCall(expr.Method);
			}
			else
			{
				switch (expr.NodeType)
				{
					case ExpressionType.ArrayIndex:
						Debug.Assert(expr.Right.Type == typeof(int));
						Compile(expr.Left);
						Compile(expr.Right);
						Instructions.EmitGetArrayItem(expr.Left.Type);
						return;
					case ExpressionType.Add:
					case ExpressionType.AddChecked:
					case ExpressionType.Subtract:
					case ExpressionType.SubtractChecked:
					case ExpressionType.Multiply:
					case ExpressionType.MultiplyChecked:
					case ExpressionType.Divide:
						CompileArithmetic(expr.NodeType, expr.Left, expr.Right);
						return;
					case ExpressionType.Equal:
						CompileEqual(expr.Left, expr.Right);
						return;
					case ExpressionType.NotEqual:
						CompileNotEqual(expr.Left, expr.Right);
						return;
					case ExpressionType.LessThan:
					case ExpressionType.LessThanOrEqual:
					case ExpressionType.GreaterThan:
					case ExpressionType.GreaterThanOrEqual:
						CompileComparison(expr.NodeType, expr.Left, expr.Right);
						return;
					default:
						throw new NotImplementedException(expr.NodeType.ToString());
				}
			}
		}

		void CompileEqual(Expression left, Expression right)
		{
			Debug.Assert(left.Type == right.Type || !left.Type.IsValueType && !right.Type.IsValueType);
			Compile(left);
			Compile(right);
			Instructions.EmitEqual(left.Type);
		}

		void CompileNotEqual(Expression left, Expression right)
		{
			Debug.Assert(left.Type == right.Type || !left.Type.IsValueType && !right.Type.IsValueType);
			Compile(left);
			Compile(right);
			Instructions.EmitNotEqual(left.Type);
		}

		void CompileComparison(ExpressionType nodeType, Expression left, Expression right)
		{
			Debug.Assert(left.Type == right.Type && TypeUtils.IsNumeric(left.Type));
			// TODO:
			// if (TypeUtils.IsNullableType(left.Type) && liftToNull) ...
			Compile(left);
			Compile(right);
			switch (nodeType)
			{
				case ExpressionType.LessThan: Instructions.EmitLessThan(left.Type); break;
				case ExpressionType.LessThanOrEqual: Instructions.EmitLessThanOrEqual(left.Type); break;
				case ExpressionType.GreaterThan: Instructions.EmitGreaterThan(left.Type); break;
				case ExpressionType.GreaterThanOrEqual: Instructions.EmitGreaterThanOrEqual(left.Type); break;
				default: throw Assert.Unreachable;
			}
		}

		void CompileArithmetic(ExpressionType nodeType, Expression left, Expression right)
		{
			Debug.Assert(left.Type == right.Type && TypeUtils.IsArithmetic(left.Type));
			Compile(left);
			Compile(right);
			switch (nodeType)
			{
				case ExpressionType.Add: Instructions.EmitAdd(left.Type, false); break;
				case ExpressionType.AddChecked: Instructions.EmitAdd(left.Type, true); break;
				case ExpressionType.Subtract: Instructions.EmitSub(left.Type, false); break;
				case ExpressionType.SubtractChecked: Instructions.EmitSub(left.Type, true); break;
				case ExpressionType.Multiply: Instructions.EmitMul(left.Type, false); break;
				case ExpressionType.MultiplyChecked: Instructions.EmitMul(left.Type, true); break;
				case ExpressionType.Divide: Instructions.EmitDiv(left.Type); break;
				default: throw Assert.Unreachable;
			}
		}

		void CompileConvertUnaryExpression(UnaryExpression expr)
		{
			if (expr.Method != null)
			{
				Compile(expr.Operand);
				// We should be able to ignore Int32ToObject
				if (expr.Method != Runtime.ScriptingRuntimeHelpers.Int32ToObjectMethod)
					Instructions.EmitCall(expr.Method);
			}
			else if (expr.Type == typeof(void))
				CompileAsVoid(expr.Operand);
			else
			{
				Compile(expr.Operand);
				CompileConvertToType(expr.Operand.Type, expr.Type, expr.NodeType == ExpressionType.ConvertChecked);
			}
		}

		void CompileConvertToType(Type typeFrom, Type typeTo, bool isChecked)
		{
			Debug.Assert(typeFrom != typeof(void) && typeTo != typeof(void));
			if (TypeUtils.AreEquivalent(typeTo, typeFrom))
				return;
			var from = Type.GetTypeCode(typeFrom);
			var to = Type.GetTypeCode(typeTo);
			if (TypeUtils.IsNumeric(from) && TypeUtils.IsNumeric(to))
			{
				if (isChecked)
					Instructions.EmitNumericConvertChecked(from, to);
				else
					Instructions.EmitNumericConvertUnchecked(from, to);
				return;
			}
			// TODO: Conversions to a super-class or implemented interfaces are no-op. A conversion to a non-implemented interface or an unrelated class, etc. should fail.
		}

		void CompileNotExpression(UnaryExpression node)
		{
			if (node.Operand.Type == typeof(bool))
			{
				Compile(node.Operand);
				Instructions.EmitNot();
			}
			else
				throw new NotImplementedException();
		}

		void CompileUnaryExpression(UnaryExpression expr)
		{
			if (expr.Method != null)
			{
				Compile(expr.Operand);
				Instructions.EmitCall(expr.Method);
			}
			else
			{
				switch (expr.NodeType)
				{
					case ExpressionType.Not:
						CompileNotExpression(expr);
						return;
					case ExpressionType.TypeAs:
						CompileTypeAsExpression(expr);
						return;
					default:
						throw new NotImplementedException(expr.NodeType.ToString());
				}
			}
		}

		void CompileAndAlsoBinaryExpression(BinaryExpression expr) { CompileLogicalBinaryExpression(expr, true); }

		void CompileOrElseBinaryExpression(BinaryExpression expr) { CompileLogicalBinaryExpression(expr, false); }

		void CompileLogicalBinaryExpression(BinaryExpression expr, bool andAlso)
		{
			if (expr.Method != null)
				throw new NotImplementedException();
			Debug.Assert(expr.Left.Type == expr.Right.Type);
			if (expr.Left.Type == typeof(bool))
			{
				var elseLabel = Instructions.MakeLabel();
				var endLabel = Instructions.MakeLabel();
				Compile(expr.Left);
				if (andAlso)
					Instructions.EmitBranchFalse(elseLabel);
				else
					Instructions.EmitBranchTrue(elseLabel);
				Compile(expr.Right);
				Instructions.EmitBranch(endLabel, false, true);
				Instructions.MarkLabel(elseLabel);
				Instructions.EmitLoad(!andAlso);
				Instructions.MarkLabel(endLabel);
				return;
			}
			Debug.Assert(expr.Left.Type == typeof(bool?));
			throw new NotImplementedException();
		}

		void CompileConditionalExpression(ConditionalExpression expr, bool asVoid)
		{
			Compile(expr.Test);
			if (expr.IfTrue == AstUtils.Empty())
			{
				var endOfFalse = Instructions.MakeLabel();
				Instructions.EmitBranchTrue(endOfFalse);
				Compile(expr.IfFalse, asVoid);
				Instructions.MarkLabel(endOfFalse);
			}
			else
			{
				var endOfTrue = Instructions.MakeLabel();
				Instructions.EmitBranchFalse(endOfTrue);
				Compile(expr.IfTrue, asVoid);
				if (expr.IfFalse != AstUtils.Empty())
				{
					var endOfFalse = Instructions.MakeLabel();
					Instructions.EmitBranch(endOfFalse, false, !asVoid);
					Instructions.MarkLabel(endOfTrue);
					Compile(expr.IfFalse, asVoid);
					Instructions.MarkLabel(endOfFalse);
				}
				else
					Instructions.MarkLabel(endOfTrue);
			}
		}

		void CompileLoopExpression(LoopExpression expr)
		{
			var enterLoop = new EnterLoopInstruction(expr, Locals, _compilationThreshold, Instructions.Count);
			var continueLabel = expr.ContinueLabel == null ? Instructions.MakeLabel() : MapLabel(expr.ContinueLabel);
			Instructions.MarkLabel(continueLabel);
			// emit loop body:
			Instructions.Emit(enterLoop);
			CompileAsVoid(expr.Body);
			// emit loop branch:
			Instructions.EmitBranch(continueLabel, expr.Type != typeof(void), false);
			if (expr.BreakLabel != null)
				Instructions.MarkLabel(MapLabel(expr.BreakLabel));
			enterLoop.FinishLoop(Instructions.Count);
		}

		void CompileSwitchExpression(SwitchExpression expr)
		{
			// Currently only supports int test values, with no method
			if (expr.SwitchValue.Type != typeof(int) || expr.Comparison != null)
				throw new NotImplementedException();
			// Test values must be constant
			if (!expr.Cases.All(c => c.TestValues.All(t => t is ConstantExpression)))
				throw new NotImplementedException();
			var end = Instructions.MakeLabel();
			var hasValue = expr.Type != typeof(void);
			Compile(expr.SwitchValue);
			var caseDict = new Dictionary<int, int>();
			var switchIndex = Instructions.Count;
			Instructions.EmitSwitch(caseDict);
			if (expr.DefaultBody != null)
				Compile(expr.DefaultBody);
			else
				Debug.Assert(!hasValue);
			Instructions.EmitBranch(end, false, hasValue);
			for (int i = 0; i < expr.Cases.Count; i++)
			{
				var caseOffset = Instructions.Count - switchIndex;
				foreach (ConstantExpression testValue in expr.Cases[i].TestValues)
					caseDict[(int)testValue.Value] = caseOffset;
				Compile(expr.Cases[i].Body);
				if (i < expr.Cases.Count - 1)
					Instructions.EmitBranch(end, false, hasValue);
			}
			Instructions.MarkLabel(end);
		}

		void CompileLabelExpression(LabelExpression expr)
		{
			if (expr.DefaultValue != null)
				Compile(expr.DefaultValue);
			Instructions.MarkLabel(MapLabel(expr.Target));
		}

		void CompileGotoExpression(GotoExpression expr)
		{
			if (expr.Value != null)
				Compile(expr.Value);
			Instructions.EmitGoto(MapLabel(expr.Target), expr.Type != typeof(void), expr.Value != null && expr.Value.Type != typeof(void));
		}

		void CompileThrowUnaryExpression(UnaryExpression expr, bool asVoid)
		{
			if (expr.Operand == null)
			{
				CompileParameterExpression(_exceptionForRethrowStack.Peek());
				if (asVoid)
					Instructions.EmitRethrowVoid();
				else
					Instructions.EmitRethrow();
			}
			else
			{
				Compile(expr.Operand);
				if (asVoid)
					Instructions.EmitThrowVoid();
				else
					Instructions.EmitThrow();
			}
		}

		// TODO: remove (replace by true fault support)
		bool EndsWithRethrow(Expression expr)
		{
			if (expr.NodeType == ExpressionType.Throw)
				return ((UnaryExpression)expr).Operand == null;
			var block = expr as BlockExpression;
			if (block != null)
				return EndsWithRethrow(block.Expressions[block.Expressions.Count - 1]);
			return false;
		}

		// TODO: remove (replace by true fault support)
		void CompileAsVoidRemoveRethrow(Expression expr)
		{
			var stackDepth = Instructions.CurrentStackDepth;
			if (expr.NodeType == ExpressionType.Throw)
			{
				Debug.Assert(((UnaryExpression)expr).Operand == null);
				return;
			}
			var node = (BlockExpression)expr;
			var end = CompileBlockStart(node);
			CompileAsVoidRemoveRethrow(node.Expressions[node.Expressions.Count - 1]);
			Debug.Assert(stackDepth == Instructions.CurrentStackDepth);
			CompileBlockEnd(end);
		}

		void CompileTryExpression(TryExpression expr)
		{
			var tryStart = Instructions.Count;
			BranchLabel startOfFinally = null;
			if (expr.Finally != null)
			{
				startOfFinally = Instructions.MakeLabel();
				Instructions.EmitEnterTryFinally(startOfFinally);
			}
			Compile(expr.Body);
			var tryEnd = Instructions.Count;
			// handlers jump here:
			var gotoEnd = Instructions.MakeLabel();
			Instructions.MarkLabel(gotoEnd);
			var end = Instructions.MakeLabel();
			var hasValue = expr.Body.Type != typeof(void);
			Instructions.EmitGoto(end, hasValue, hasValue);
			// keep the result on the stack:     
			if (expr.Handlers.Count > 0)
			{
				var handlerContinuationDepth = Instructions.CurrentContinuationsDepth;
				// TODO: emulates faults (replace by true fault support)
				if (expr.Finally == null && expr.Handlers.Count == 1)
				{
					if (expr.Handlers[0].Filter == null && expr.Handlers[0].Test == typeof(Exception) && expr.Handlers[0].Variable == null)
					{
						if (EndsWithRethrow(expr.Handlers[0].Body))
						{
							if (hasValue)
								Instructions.EmitEnterExceptionHandlerNonVoid();
							else
								Instructions.EmitEnterExceptionHandlerVoid();
							// at this point the stack balance is prepared for the hidden exception variable:
							var handlerLabel = Instructions.MarkRuntimeLabel();
							var handlerStart = Instructions.Count;
							CompileAsVoidRemoveRethrow(expr.Handlers[0].Body);
							Instructions.EmitLeaveFault(hasValue);
							Instructions.MarkLabel(end);
							_handlers.Add(new ExceptionHandler(tryStart, tryEnd, handlerLabel, handlerStart, null));
							return;
						}
					}
				}
				foreach (var handler in expr.Handlers)
				{
					if (handler.Filter != null)
						throw new NotImplementedException();
					var parameter = handler.Variable ?? Expression.Parameter(handler.Test);
					var local = Locals.DefineLocal(parameter, Instructions.Count);
					_exceptionForRethrowStack.Push(parameter);
					// add a stack balancing nop instruction (exception handling pushes the current exception):
					if (hasValue)
						Instructions.EmitEnterExceptionHandlerNonVoid();
					else
						Instructions.EmitEnterExceptionHandlerVoid();
					// at this point the stack balance is prepared for the hidden exception variable:
					var handlerLabel = Instructions.MarkRuntimeLabel();
					var handlerStart = Instructions.Count;
					CompileSetVariable(parameter, true);
					Compile(handler.Body);
					_exceptionForRethrowStack.Pop();
					// keep the value of the body on the stack:
					Debug.Assert(hasValue == (handler.Body.Type != typeof(void)));
					Instructions.EmitLeaveExceptionHandler(hasValue, gotoEnd);
					_handlers.Add(new ExceptionHandler(tryStart, tryEnd, handlerLabel, handlerStart, handler.Test));
					Locals.UndefineLocal(local, Instructions.Count);
				}
				if (expr.Fault != null)
					throw new NotImplementedException();
			}
			if (expr.Finally != null)
			{
				Instructions.MarkLabel(startOfFinally);
				Instructions.EmitEnterFinally();
				CompileAsVoid(expr.Finally);
				Instructions.EmitLeaveFinally();
			}
			Instructions.MarkLabel(end);
		}

		void CompileDynamicExpression(DynamicExpression expr)
		{
			foreach (var arg in expr.Arguments)
				Compile(arg);
			Instructions.EmitDynamic(expr.DelegateType, expr.Binder);
		}

		void CompileMethodCallExpression(MethodCallExpression expr)
		{
			if (expr.Method == _GetCurrentMethod && expr.Object == null && expr.Arguments.Count == 0)
			{
				// If we call GetCurrentMethod, it will expose details of the interpreter's CallInstruction.
				// Instead, we use Interpreter.Run, which logically represents the running method, and will appear in the stack trace of an exception.
				Instructions.EmitLoad(_RunMethod);
				return;
			}
			var parameters = expr.Method.GetParameters();
			// TODO: Support pass by reference. Note that LoopCompiler needs to be updated too. force compilation for now:
			if (parameters.Any(p => p.ParameterType.IsByRef))
				_forceCompile = true;
			if (!expr.Method.IsStatic)
				Compile(expr.Object);
			foreach (var arg in expr.Arguments)
				Compile(arg);
			Instructions.EmitCall(expr.Method, parameters);
		}

		void CompileNewExpression(NewExpression expr)
		{
			if (expr.Constructor != null)
			{
				foreach (var arg in expr.Arguments)
					Compile(arg);
				Instructions.EmitNew(expr.Constructor);
			}
			else
			{
				Debug.Assert(expr.Type.IsValueType);
				Instructions.EmitDefaultValue(expr.Type);
			}
		}

		void CompileMemberExpression(MemberExpression expr)
		{
			var fi = expr.Member as FieldInfo;
			if (fi != null)
			{
				if (fi.IsLiteral)
					Instructions.EmitLoad(fi.GetRawConstantValue(), fi.FieldType);
				else if (fi.IsStatic)
				{
					if (fi.IsInitOnly)
						Instructions.EmitLoad(fi.GetValue(null), fi.FieldType);
					else
						Instructions.EmitLoadField(fi);
				}
				else
				{
					Compile(expr.Expression);
					Instructions.EmitLoadField(fi);
				}
				return;
			}
			var pi = expr.Member as PropertyInfo;
			if (pi != null)
			{
				if (expr.Expression != null)
					Compile(expr.Expression);
				Instructions.EmitCall(pi.GetGetMethod(true));
				return;
			}
			throw new NotImplementedException();
		}

		void CompileNewArrayExpression(NewArrayExpression expr)
		{
			foreach (var arg in expr.Expressions)
				Compile(arg);
			var elementType = expr.Type.GetElementType();
			var rank = expr.Expressions.Count;
			if (expr.NodeType == ExpressionType.NewArrayInit)
				Instructions.EmitNewArrayInit(elementType, rank);
			else if (expr.NodeType == ExpressionType.NewArrayBounds)
			{
				if (rank == 1)
					Instructions.EmitNewArray(elementType);
				else
					Instructions.EmitNewArrayBounds(elementType, rank);
			}
			else
				throw new NotImplementedException();
		}

		class ParameterVisitor : ExpressionVisitor
		{
			readonly LightCompiler _compiler;

			/// <summary>
			/// A stack of variables that are defined in nested scopes.
			/// We search this first when resolving a variable in case a nested scope shadows one of our variable instances.
			/// </summary>
			readonly Stack<HashSet<ParameterExpression>> _shadowedVars = new Stack<HashSet<ParameterExpression>>();

			public ParameterVisitor(LightCompiler compiler) { _compiler = compiler; }

			protected override Expression VisitLambda<T>(Expression<T> node)
			{
				_shadowedVars.Push(new HashSet<ParameterExpression>(node.Parameters));
				try
				{
					Visit(node.Body);
					return node;
				}
				finally { _shadowedVars.Pop(); }
			}

			protected override Expression VisitBlock(BlockExpression node)
			{
				if (node.Variables.Count > 0)
					_shadowedVars.Push(new HashSet<ParameterExpression>(node.Variables));
				try
				{
					Visit(node.Expressions);
					return node;
				}
				finally
				{
					if (node.Variables.Count > 0)
						_shadowedVars.Pop();
				}
			}

			protected override CatchBlock VisitCatchBlock(CatchBlock node)
			{
				if (node.Variable != null)
					_shadowedVars.Push(new HashSet<ParameterExpression>(new[] { node.Variable }));
				try
				{
					Visit(node.Filter);
					Visit(node.Body);
					return node;
				}
				finally
				{
					if (node.Variable != null)
						_shadowedVars.Pop();
				}
			}

			protected override Expression VisitParameter(ParameterExpression node)
			{
				// Skip variables that are shadowed by a nested scope/lambda
				if (_shadowedVars.Any(x => x.Contains(node)))
					return node;
				// If we didn't find it, it must exist at a higher level scope
				_compiler.EnsureVariable(node);
				return node;
			}
		}

		void CompileExtensionExpression(Expression expr)
		{
			var instructionProvider = expr as IInstructionProvider;
			if (instructionProvider != null)
			{
				instructionProvider.AddInstructions(this);
				return;
			}
			var skip = expr as Ast.SkipInterpretExpression;
			if (skip != null)
			{
				new ParameterVisitor(this).Visit(skip);
				return;
			}
			var node = expr as Ast.SymbolConstantExpression;
			if (node != null)
			{
				Instructions.EmitLoad(node.Value);
				return;
			}
			var updateStack = expr as LastFaultingLineExpression;
			if (updateStack != null)
			{
				var updateStackInstr = new UpdateStackTraceInstruction();
				Instructions.Emit(updateStackInstr);
				_stackTraceUpdates.Add(updateStackInstr);
				return;
			}
			if (expr.CanReduce)
				Compile(expr.Reduce());
			else
				throw new NotImplementedException();
		}

		void CompileDebugInfoExpression(DebugInfoExpression expr) { _debugInfos.Add(new DebugInfo(expr.StartLine, expr.EndLine, Instructions.Count, expr.Document.FileName, expr.IsClear)); }

		void CompileRuntimeVariablesExpression(RuntimeVariablesExpression expr)
		{
			// Generates IRuntimeVariables for all requested variables
			foreach (var variable in expr.Variables)
			{
				EnsureAvailableForClosure(variable);
				CompileGetBoxedVariable(variable);
			}
			Instructions.EmitNewRuntimeVariables(expr.Variables.Count);
		}

		void CompileLambdaExpression(LambdaExpression expr)
		{
			var compiler = new LightCompiler(this);
			var creator = compiler.CompileTop(expr);
			if (compiler.Locals.ClosureVariables != null)
			{
				foreach (var variable in compiler.Locals.ClosureVariables.Keys)
					CompileGetBoxedVariable(variable);
			}
			Instructions.EmitCreateDelegate(creator);
		}

		void CompileCoalesceBinaryExpression(BinaryExpression expr)
		{
			if (TypeUtils.IsNullableType(expr.Left.Type))
				throw new NotImplementedException();
			else if (expr.Conversion != null)
				throw new NotImplementedException();
			else
			{
				var leftNotNull = Instructions.MakeLabel();
				Compile(expr.Left);
				Instructions.EmitCoalescingBranch(leftNotNull);
				Instructions.EmitPop();
				Compile(expr.Right);
				Instructions.MarkLabel(leftNotNull);
			}
		}

		void CompileInvocationExpression(InvocationExpression expr)
		{
			// TODO: LambdaOperand optimization (see compiler)
			if (typeof(LambdaExpression).IsAssignableFrom(expr.Expression.Type))
				throw new NotImplementedException();
			// TODO: do not create a new Call Expression
			if (PlatformAdaptationLayer.IsCompactFramework)
				// Workaround for a bug in Compact Framework
				Compile(
					AstUtils.Convert(
						Expression.Call(expr.Expression, expr.Expression.Type.GetMethod("DynamicInvoke"), Expression.NewArrayInit(typeof(object), expr.Arguments.Select(e => AstUtils.Convert(e, typeof(object))))),
						expr.Type
					)
				);
			else
				CompileMethodCallExpression(Expression.Call(expr.Expression, expr.Expression.Type.GetMethod("Invoke"), expr.Arguments));
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "expr")]
		void CompileListInitExpression(ListInitExpression expr) { throw new NotImplementedException(); }

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "expr")]
		void CompileMemberInitExpression(MemberInitExpression expr) { throw new NotImplementedException(); }

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "expr")]
		void CompileQuoteUnaryExpression(UnaryExpression expr) { throw new NotImplementedException(); }

		void CompileUnboxUnaryExpression(UnaryExpression expr) { Compile(expr.Operand); } // unboxing is a nop:

		void CompileTypeEqualExpression(TypeBinaryExpression expr)
		{
			Debug.Assert(expr.NodeType == ExpressionType.TypeEqual);
			Compile(expr.Expression);
			Instructions.EmitLoad(expr.TypeOperand);
			Instructions.EmitTypeEquals();
		}

		void CompileTypeAsExpression(UnaryExpression node)
		{
			Compile(node.Operand);
			Instructions.EmitTypeAs(node.Type);
		}

		void CompileTypeIsExpression(TypeBinaryExpression expr)
		{
			Debug.Assert(expr.NodeType == ExpressionType.TypeIs);
			Compile(expr.Expression);
			// use TypeEqual for sealed types:
			if (expr.TypeOperand.IsSealed)
			{
				Instructions.EmitLoad(expr.TypeOperand);
				Instructions.EmitTypeEquals();
			}
			else
				Instructions.EmitTypeIs(expr.TypeOperand);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "expr")]
		void CompileReducibleExpression(Expression expr) { throw new NotImplementedException(); }

		void Compile(Expression expr, bool asVoid)
		{
			if (asVoid)
				CompileAsVoid(expr);
			else
				Compile(expr);
		}

		void CompileAsVoid(Expression expr)
		{
			int startingStackDepth = Instructions.CurrentStackDepth;
			switch (expr.NodeType)
			{
				case ExpressionType.Assign: CompileAssignBinaryExpression((BinaryExpression)expr, true); break;
				case ExpressionType.Block: CompileBlockExpression((BlockExpression)expr, true); break;
				case ExpressionType.Throw: CompileThrowUnaryExpression((UnaryExpression)expr, true); break;
				case ExpressionType.Constant:
				case ExpressionType.Default:
				case ExpressionType.Parameter: break;
				default:
					Compile(expr);
					if (expr.Type != typeof(void))
						Instructions.EmitPop();
					break;
			}
			Debug.Assert(Instructions.CurrentStackDepth == startingStackDepth);
		}

		/// <summary>指定された式をコンパイルして、命令リストに対応する命令を追加します。</summary>
		/// <param name="expr">コンパイルする式を指定します。</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		public void Compile(Expression expr)
		{
			int startingStackDepth = Instructions.CurrentStackDepth;
			switch (expr.NodeType)
			{
				case ExpressionType.Add:
				case ExpressionType.AddChecked:
				case ExpressionType.And:
				case ExpressionType.ArrayIndex:
				case ExpressionType.Divide:
				case ExpressionType.Equal:
				case ExpressionType.ExclusiveOr:
				case ExpressionType.GreaterThan:
				case ExpressionType.GreaterThanOrEqual:
				case ExpressionType.LeftShift:
				case ExpressionType.LessThan:
				case ExpressionType.LessThanOrEqual:
				case ExpressionType.Modulo:
				case ExpressionType.Multiply:
				case ExpressionType.MultiplyChecked:
				case ExpressionType.NotEqual:
				case ExpressionType.Or:
				case ExpressionType.Power:
				case ExpressionType.RightShift:
				case ExpressionType.Subtract:
				case ExpressionType.SubtractChecked: CompileBinaryExpression((BinaryExpression)expr); break;
				case ExpressionType.AndAlso: CompileAndAlsoBinaryExpression((BinaryExpression)expr); break;
				case ExpressionType.ArrayLength:
				case ExpressionType.Negate:
				case ExpressionType.UnaryPlus:
				case ExpressionType.NegateChecked:
				case ExpressionType.Not:
				case ExpressionType.TypeAs:
				case ExpressionType.Decrement:
				case ExpressionType.Increment:
				case ExpressionType.OnesComplement:
				case ExpressionType.IsTrue:
				case ExpressionType.IsFalse: CompileUnaryExpression((UnaryExpression)expr); break;
				case ExpressionType.Call: CompileMethodCallExpression((MethodCallExpression)expr); break;
				case ExpressionType.Coalesce: CompileCoalesceBinaryExpression((BinaryExpression)expr); break;
				case ExpressionType.Conditional: CompileConditionalExpression((ConditionalExpression)expr, expr.Type == typeof(void)); break;
				case ExpressionType.Constant: CompileConstantExpression((ConstantExpression)expr); break;
				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked: CompileConvertUnaryExpression((UnaryExpression)expr); break;
				case ExpressionType.Invoke: CompileInvocationExpression((InvocationExpression)expr); break;
				case ExpressionType.Lambda: CompileLambdaExpression((LambdaExpression)expr); break;
				case ExpressionType.ListInit: CompileListInitExpression((ListInitExpression)expr); break;
				case ExpressionType.MemberAccess: CompileMemberExpression((MemberExpression)expr); break;
				case ExpressionType.MemberInit: CompileMemberInitExpression((MemberInitExpression)expr); break;
				case ExpressionType.New: CompileNewExpression((NewExpression)expr); break;
				case ExpressionType.NewArrayInit:
				case ExpressionType.NewArrayBounds: CompileNewArrayExpression((NewArrayExpression)expr); break;
				case ExpressionType.OrElse: CompileOrElseBinaryExpression((BinaryExpression)expr); break;
				case ExpressionType.Parameter: CompileParameterExpression((ParameterExpression)expr); break;
				case ExpressionType.Quote: CompileQuoteUnaryExpression((UnaryExpression)expr); break;
				case ExpressionType.TypeIs: CompileTypeIsExpression((TypeBinaryExpression)expr); break;
				case ExpressionType.Assign: CompileAssignBinaryExpression((BinaryExpression)expr, expr.Type == typeof(void)); break;
				case ExpressionType.Block: CompileBlockExpression((BlockExpression)expr, expr.Type == typeof(void)); break;
				case ExpressionType.DebugInfo: CompileDebugInfoExpression((DebugInfoExpression)expr); break;
				case ExpressionType.Dynamic: CompileDynamicExpression((DynamicExpression)expr); break;
				case ExpressionType.Default: CompileDefaultExpression(expr); break;
				case ExpressionType.Extension: CompileExtensionExpression(expr); break;
				case ExpressionType.Goto: CompileGotoExpression((GotoExpression)expr); break;
				case ExpressionType.Index: CompileIndexExpression((IndexExpression)expr); break;
				case ExpressionType.Label: CompileLabelExpression((LabelExpression)expr); break;
				case ExpressionType.RuntimeVariables: CompileRuntimeVariablesExpression((RuntimeVariablesExpression)expr); break;
				case ExpressionType.Loop: CompileLoopExpression((LoopExpression)expr); break;
				case ExpressionType.Switch: CompileSwitchExpression((SwitchExpression)expr); break;
				case ExpressionType.Throw: CompileThrowUnaryExpression((UnaryExpression)expr, expr.Type == typeof(void)); break;
				case ExpressionType.Try: CompileTryExpression((TryExpression)expr); break;
				case ExpressionType.Unbox: CompileUnboxUnaryExpression((UnaryExpression)expr); break;
				case ExpressionType.TypeEqual: CompileTypeEqualExpression((TypeBinaryExpression)expr); break;
				default:
					if (Ast.Utils.IsReadWriteAssignment(expr.NodeType))
					{
						CompileReducibleExpression(expr);
						break;
					}
					throw Assert.Unreachable;
			}
			Debug.Assert(Instructions.CurrentStackDepth == startingStackDepth + (expr.Type == typeof(void) ? 0 : 1));
		}
	}
}
