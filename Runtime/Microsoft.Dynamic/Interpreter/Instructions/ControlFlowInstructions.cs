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
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Interpreter
{
	using LoopFunc = Func<object[], StrongBox<object>[], InterpretedFrame, int>;

	/// <summary>この命令からのオフセットにジャンプする命令の基底クラスを表します。</summary>
	abstract class OffsetInstruction : Instruction
	{
		/// <summary><see cref="Microsoft.Scripting.Interpreter.OffsetInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		protected OffsetInstruction() { Offset = Unknown; }

		internal const int Unknown = int.MinValue;
		internal const int CacheSize = 32;

		/// <summary>この命令に相対的なジャンプ先のオフセットを取得します。</summary>
		public int Offset { get; private set; }

		/// <summary>オフセットに対する命令のキャッシュを取得します。</summary>
		public abstract Instruction[] Cache { get; }

		/// <summary>この命令のジャンプ先のオフセットが指定された値に書き換えられた <see cref="Instruction"/> を返します。</summary>
		/// <param name="offset">オフセットを書き換える値を指定します。</param>
		/// <param name="targetContinuationDepth">ジャンプ先の継続の深さを指定します。</param>
		/// <param name="targetStackDepth">ジャンプ先のスタックの深さを指定します。</param>
		/// <returns>オフセットが置き換えられた新しい <see cref="Instruction"/>。</returns>
		public virtual Instruction Fixup(int offset, int targetContinuationDepth, int targetStackDepth)
		{
			Debug.Assert(Offset == Unknown && offset != Unknown);
			Offset = offset;
			if (Cache != null && offset >= 0 && offset < Cache.Length)
				return Cache[offset] ?? (Cache[offset] = this);
			return this;
		}

		/// <summary>このオブジェクトのデバッグ用文字列表現を取得します。</summary>
		/// <param name="instructionIndex">この命令の命令インデックスを指定します。</param>
		/// <param name="cookie">デバッグ用 Cookie を指定します。</param>
		/// <param name="labelIndexer">ラベルを表すインデックスからラベルの遷移先インデックスを取得するデリゲートを指定します。</param>
		/// <param name="objects">デバッグ用 Cookie のリストを指定します。</param>
		/// <returns>デバッグ用文字列表現。</returns>
		public override string ToDebugString(int instructionIndex, object cookie, Func<int, int> labelIndexer, IList<object> objects) { return ToString() + (Offset != Unknown ? " -> " + (instructionIndex + Offset) : ""); }

		/// <summary>このオブジェクトの文字列表現を取得します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		public override string ToString() { return InstructionName + (Offset == Unknown ? "(?)" : "(" + Offset + ")"); }
	}

	/// <summary>スタックトップの値が <c>false</c> であればジャンプする分岐命令を表します。</summary>
	sealed class BranchFalseInstruction : OffsetInstruction
	{
		static Instruction[] _cache;

		/// <summary>オフセットに対する命令のキャッシュを取得します。</summary>
		public override Instruction[] Cache { get { return _cache ?? (_cache = new Instruction[CacheSize]); } }

		/// <summary><see cref="Microsoft.Scripting.Interpreter.BranchFalseInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		internal BranchFalseInstruction() { }

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			Debug.Assert(Offset != Unknown);
			if (!(bool)frame.Pop())
				return Offset;
			return +1;
		}
	}

	/// <summary>スタックトップの値が <c>true</c> であればジャンプする分岐命令を表します。</summary>
	sealed class BranchTrueInstruction : OffsetInstruction
	{
		static Instruction[] _cache;

		/// <summary>オフセットに対する命令のキャッシュを取得します。</summary>
		public override Instruction[] Cache { get { return _cache ?? (_cache = new Instruction[CacheSize]); } }

		/// <summary><see cref="Microsoft.Scripting.Interpreter.BranchTrueInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		internal BranchTrueInstruction() { }

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			Debug.Assert(Offset != Unknown);
			if ((bool)frame.Pop())
				return Offset;
			return +1;
		}
	}

	/// <summary>スタックトップの値が <c>null</c> でなければジャンプするがスタックは消費しない分岐命令を表します。</summary>
	sealed class CoalescingBranchInstruction : OffsetInstruction
	{
		static Instruction[] _cache;

		/// <summary>オフセットに対する命令のキャッシュを取得します。</summary>
		public override Instruction[] Cache { get { return _cache ?? (_cache = new Instruction[CacheSize]); } }

		/// <summary><see cref="Microsoft.Scripting.Interpreter.CoalescingBranchInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		internal CoalescingBranchInstruction() { }

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			Debug.Assert(Offset != Unknown);
			if (frame.Peek() != null)
				return Offset;
			return +1;
		}
	}

	/// <summary>常に指定されたオフセットにジャンプする分岐命令を表します。</summary>
	class BranchInstruction : OffsetInstruction
	{
		static Instruction[][][] _caches;

		/// <summary>オフセットに対する命令のキャッシュを取得します。</summary>
		public override Instruction[] Cache
		{
			get
			{
				if (_caches == null)
					_caches = new Instruction[2][][] { new Instruction[2][], new Instruction[2][] };
				return _caches[ConsumedStack][ProducedStack] ?? (_caches[ConsumedStack][ProducedStack] = new Instruction[CacheSize]);
			}
		}

		readonly bool _hasResult;
		readonly bool _hasValue;

		/// <summary><see cref="Microsoft.Scripting.Interpreter.CoalescingBranchInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		internal BranchInstruction() : this(false, false) { }

		/// <summary>指定されたオプションを使用して、<see cref="Microsoft.Scripting.Interpreter.CoalescingBranchInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="hasResult">この分岐が結果をもつかどうかを示す値を指定します。</param>
		/// <param name="hasValue">この分岐に値が存在するかどうかを示す値を指定します。</param>
		public BranchInstruction(bool hasResult, bool hasValue)
		{
			_hasResult = hasResult;
			_hasValue = hasValue;
		}

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return _hasValue ? 1 : 0; } }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return _hasResult ? 1 : 0; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			Debug.Assert(Offset != Unknown);
			return Offset;
		}
	}

	/// <summary>指定されたインデックスのラベルへのジャンプを表す分岐命令の基底クラスを表します。</summary>
	abstract class IndexedBranchInstruction : Instruction
	{
		protected const int CacheSize = 32;

		/// <summary>ジャンプ先のラベルのインデックスを指定して、<see cref="Microsoft.Scripting.Interpreter.IndexedBranchInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="labelIndex">ジャンプ先のラベルのインデックスを指定します。</param>
		protected IndexedBranchInstruction(int labelIndex) { LabelIndex = labelIndex; }

		/// <summary>ジャンプ先のラベルのインデックスを取得します。</summary>
		protected int LabelIndex { get; private set; }

		/// <summary>指定されたフレームに対するこの命令のジャンプ先の <see cref="RuntimeLabel"/> を取得します。</summary>
		/// <param name="frame">ラベルを取得するフレームを指定します。</param>
		/// <returns>この命令がジャンプするラベルを表す <see cref="RuntimeLabel"/>。</returns>
		public RuntimeLabel GetLabel(InterpretedFrame frame) { return frame.Interpreter._labels[LabelIndex]; }

		/// <summary>このオブジェクトのデバッグ用文字列表現を取得します。</summary>
		/// <param name="instructionIndex">この命令の命令インデックスを指定します。</param>
		/// <param name="cookie">デバッグ用 Cookie を指定します。</param>
		/// <param name="labelIndexer">ラベルを表すインデックスからラベルの遷移先インデックスを取得するデリゲートを指定します。</param>
		/// <param name="objects">デバッグ用 Cookie のリストを指定します。</param>
		/// <returns>デバッグ用文字列表現。</returns>
		public override string ToDebugString(int instructionIndex, object cookie, Func<int, int> labelIndexer, IList<object> objects)
		{
			var targetIndex = labelIndexer(LabelIndex);
			return ToString() + (targetIndex != BranchLabel.UnknownIndex ? " -> " + targetIndex.ToString() : "");
		}

		/// <summary>このオブジェクトの文字列表現を取得します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		public override string ToString() { return InstructionName + "[" + LabelIndex + "]"; }
	}

	/// <summary>
	/// あらゆる式からジャンプできる goto 式を実装する命令を表します。
	/// これは goto 式とジャンプ先ラベルの間にあるノードが、プッシュされてまだ消費されていない評価スタックから値 (引数) をポップします。
	/// この命令は値を転送し、(値は最初の引数として使用されるので) 最初の引数のすぐ後にジャンプする限り、引数を評価するノードにジャンプできます。
	/// また、ブロックが子ノードを評価するように評価スタック上の値を集積しない場合、<see cref="BlockExpression"/> の任意の子ノードにジャンプできます。
	/// </summary>
	/// <remarks>
	/// goto はジャンプ先ラベルまでに存在するあらゆる finally ブロックを実行する必要があります。
	/// <code>
	/// { 
	///     f(1, 2, try { g(3, 4, try { goto L } finally { ... }, 6) } finally { ... }, 7, 8)
	///     L: ... 
	/// }
	/// </code>
	/// この goto 式は 4 個の要素 (1, 2, 3, 4) を評価スタックに残したまま、ラベル L にジャンプします。
	/// ジャンプは両方の finally ブロックを実行する必要があります。1 番目はスタックレベル 4、2 番目はスタックレベル 2 にあります。
	/// そのため、命令はまず最初の finaly ブロックにジャンプして 2 個の要素をスタックからポップし、
	/// 2 番目の finally ブロックを実行して別の 2 個の要素をスタックからポップしてから、命令ポインタをラベル L に設定します。
	/// 
	/// goto はまた catch ハンドラからのジャンプで現在のスレッドが "中止を要求された状態" のときおよびその場合に限り <see cref="ThreadAbortException"/> を再スローする必要があります。
	/// </remarks>
	sealed class GotoInstruction : IndexedBranchInstruction
	{
		const int Variants = 4;
		static readonly GotoInstruction[] Cache = new GotoInstruction[Variants * CacheSize];

		readonly bool _hasResult;
		// TODO: ラベルで hasValue を記憶してスタックバランスを計算するときに検索するようにできる。少しキャッシュを保存する
		readonly bool _hasValue;

		// この値は現在の継続の深さと異なる深さをもつラベルに対する goto では技術的には Consumed = 1, Produced = 1 であるべきです。
		// しかし、前方の goto の場合、ラベルが発行されるまでわかりません。
		// その時までには、継続の深さの情報は役に立たなくなっています。重要なのは増減が 0 であるということだけです。
		/// <summary>この命令で消費される継続の数を取得します。</summary>
		public override int ConsumedContinuations { get { return 0; } }

		/// <summary>この命令で生成される継続の数を取得します。</summary>
		public override int ProducedContinuations { get { return 0; } }

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return _hasValue ? 1 : 0; } }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return _hasResult ? 1 : 0; } }

		GotoInstruction(int targetIndex, bool hasResult, bool hasValue) : base(targetIndex)
		{
			_hasResult = hasResult;
			_hasValue = hasValue;
		}

		/// <summary>指定されたラベルへジャンプする <see cref="GotoInstruction"/> を作成します。</summary>
		/// <param name="labelIndex">ジャンプ先のラベルのインデックスを指定します。</param>
		/// <param name="hasResult">この分岐が結果をもつかどうかを示す値を指定します。</param>
		/// <param name="hasValue">この分岐が値を転送するかどうかを示す値を指定します。</param>
		/// <returns>作成された <see cref="GotoInstruction"/>。</returns>
		internal static GotoInstruction Create(int labelIndex, bool hasResult, bool hasValue)
		{
			if (labelIndex < CacheSize)
			{
				var index = Variants * labelIndex | (hasResult ? 2 : 0) | (hasValue ? 1 : 0);
				return Cache[index] ?? (Cache[index] = new GotoInstruction(labelIndex, hasResult, hasValue));
			}
			return new GotoInstruction(labelIndex, hasResult, hasValue);
		}

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			// 現在のスレッドを中止しながら、catch/finally からジャンプ?
			Interpreter.AbortThreadIfRequested(frame, LabelIndex);
			// ターゲットラベルまたは現在の finally 継続に goto
			return frame.Goto(LabelIndex, _hasValue ? frame.Pop() : Interpreter.NoValue);
		}
	}

	/// <summary>try-finally ブロックの開始を示す命令を表します。</summary>
	sealed class EnterTryFinallyInstruction : IndexedBranchInstruction
	{
		readonly static EnterTryFinallyInstruction[] Cache = new EnterTryFinallyInstruction[CacheSize];

		/// <summary>この命令で生成される継続の数を取得します。</summary>
		public override int ProducedContinuations { get { return 1; } }

		EnterTryFinallyInstruction(int targetIndex) : base(targetIndex) { }

		/// <summary>指定された finally ラベルのインデックスを使用して、<see cref="EnterTryFinallyInstruction"/> を作成します。</summary>
		/// <param name="labelIndex">finally ブロックを示すラベルのインデックスを指定します。</param>
		/// <returns>作成された <see cref="EnterTryFinallyInstruction"/>。</returns>
		internal static EnterTryFinallyInstruction Create(int labelIndex)
		{
			if (labelIndex < CacheSize)
				return Cache[labelIndex] ?? (Cache[labelIndex] = new EnterTryFinallyInstruction(labelIndex));
			return new EnterTryFinallyInstruction(labelIndex);
		}

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			// finally をプッシュ。
			frame.PushContinuation(LabelIndex);
			return 1;
		}
	}

	/// <summary>finally ブロックの開始を示す命令を表します。</summary>
	sealed class EnterFinallyInstruction : Instruction
	{
		/// <summary>この命令の唯一のインスタンスを示します。</summary>
		internal static readonly Instruction Instance = new EnterFinallyInstruction();

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 2; } }

		/// <summary>この命令で消費される継続の数を取得します。</summary>
		public override int ConsumedContinuations { get { return 1; } }

		EnterFinallyInstruction() { }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.PushPendingContinuation();
			frame.RemoveContinuation();
			return 1;
		}
	}

	/// <summary>finally ブロックの終了を示す命令を表します。</summary>
	sealed class LeaveFinallyInstruction : Instruction
	{
		/// <summary>この命令の唯一のインスタンスを示します。</summary>
		internal static readonly Instruction Instance = new LeaveFinallyInstruction();

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return 2; } }

		LeaveFinallyInstruction() { }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			frame.PopPendingContinuation();
			// jump to goto target or to the next finally:
			return frame.YieldToPendingContinuation();
		}
	}

	// no-op: we need this just to balance the stack depth.
	/// <summary>例外ハンドラの開始を示す命令を表します。</summary>
	sealed class EnterExceptionHandlerInstruction : Instruction
	{
		/// <summary>try ブロック本体が値をもたない場合のこの命令のインスタンスを示します。</summary>
		internal static readonly EnterExceptionHandlerInstruction Void = new EnterExceptionHandlerInstruction(false);

		/// <summary>try ブロック本体が値をもつ場合のこの命令のインスタンスを示します。</summary>
		internal static readonly EnterExceptionHandlerInstruction NonVoid = new EnterExceptionHandlerInstruction(true);

		// True if try-expression is non-void.
		readonly bool _hasValue;

		EnterExceptionHandlerInstruction(bool hasValue) { _hasValue = hasValue; }

		// 例外が try 本体でスローされた場合、try 本体の式の結果は評価されずスタックにも積まれません。
		// そのため、ハンドラの実行を開始する際には、スタックは try 本体の値を含みません。
		// しかし、命令を発行すると同時に try ブロックはスタック上に値がある catch ブロックを流れ落ちます。
		// ハンドラの開始でのスタック状態がスローされてこの catch ブロックにジャンプした後の本当のスタックの深さと対応するように、消費されたと宣言する必要があります。
		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return _hasValue ? 1 : 0; } }

		// 現在の例外を格納する変数が例外ハンドリングによってスタックにプッシュされます。
		// Catch ハンドラ: 値はすぐにポップされ、ローカル変数に格納されます。
		// Fault ハンドラ: 値は Fault ハンドラの評価の間、スタック上に維持されます。
		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame) { return 1; } // nop (the exception value is pushed by the interpreter in HandleCatch)
	}

	/// <summary>例外ハンドラの終了を示す命令を表します。</summary>
	sealed class LeaveExceptionHandlerInstruction : IndexedBranchInstruction
	{
		static LeaveExceptionHandlerInstruction[] Cache = new LeaveExceptionHandlerInstruction[2 * CacheSize];

		readonly bool _hasValue;

		// catch ブロックは本体が void でない場合、値を譲ります。この値はスタックに残されます。
		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return _hasValue ? 1 : 0; } }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return _hasValue ? 1 : 0; } }

		LeaveExceptionHandlerInstruction(int labelIndex, bool hasValue) : base(labelIndex) { _hasValue = hasValue; }

		/// <summary>指定された try ブロックの終了ラベルを使用して、<see cref="LeaveExceptionHandlerInstruction"/> を作成します。</summary>
		/// <param name="labelIndex">例外ハンドラを抜けた後に移動する try ブロックの終了位置を示すラベルのインデックスを指定します。</param>
		/// <param name="hasValue">例外ハンドラ本体が値をもつかどうかを示す値を指定します。</param>
		/// <returns>作成された <see cref="LeaveExceptionHandlerInstruction"/>。</returns>
		internal static LeaveExceptionHandlerInstruction Create(int labelIndex, bool hasValue)
		{
			if (labelIndex < CacheSize)
			{
				var index = (2 * labelIndex) | (hasValue ? 1 : 0);
				return Cache[index] ?? (Cache[index] = new LeaveExceptionHandlerInstruction(labelIndex, hasValue));
			}
			return new LeaveExceptionHandlerInstruction(labelIndex, hasValue);
		}

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			// CLR は現在のスレッドが中止を要求されていた場合、catch ハンドラを抜けると ThreadAbortException を再スローします。
			Interpreter.AbortThreadIfRequested(frame, LabelIndex);
			return GetLabel(frame).Index - frame.InstructionIndex;
		}
	}

	/// <summary>fault 例外ハンドラの終了を示す命令を表します。</summary>
	sealed class LeaveFaultInstruction : Instruction
	{
		/// <summary>try ブロック本体が値をもつ場合のこの命令のインスタンスを示します。</summary>
		internal static readonly Instruction NonVoid = new LeaveFaultInstruction(true);

		/// <summary>try ブロック本体が値をもたない場合のこの命令のインスタンスを示します。</summary>
		internal static readonly Instruction Void = new LeaveFaultInstruction(false);

		readonly bool _hasValue;

		// fault ブロックは本体が void でない場合にも値をもちますが、値は使用されることはありません。
		// fault ブロックの本体を void としてコンパイルします。
		// しかし、fault ブロックの開始時にスタックにプッシュされた例外オブジェクトをブロックの実行中保持して、最後にポップします。
		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return 1; } }

		// 命令の発行と同時に void でない try-fault 式は値を生成すると予期されます。
		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return _hasValue ? 1 : 0; } }

		LeaveFaultInstruction(bool hasValue) { _hasValue = hasValue; }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			// TODO: ThreadAbortException ?
			ExceptionHandler handler;
			return frame.Interpreter.GotoHandler(frame, frame.Pop(), out handler);
		}
	}

	/// <summary>例外をスローする命令を表します。</summary>
	sealed class ThrowInstruction : Instruction
	{
		/// <summary>値をもち単純なスローを表す命令のインスタンスを示します。</summary>
		internal static readonly ThrowInstruction Throw = new ThrowInstruction(true, false);

		/// <summary>値をもたない単純なスローを表す命令のインスタンスを示します。</summary>
		internal static readonly ThrowInstruction VoidThrow = new ThrowInstruction(false, false);

		/// <summary>値をもち再スローを表す命令のインスタンスを示します。</summary>
		internal static readonly ThrowInstruction Rethrow = new ThrowInstruction(true, true);

		/// <summary>値をもたない再スローを表す命令のインスタンスを示します。</summary>
		internal static readonly ThrowInstruction VoidRethrow = new ThrowInstruction(false, true);

		readonly bool _hasResult, _rethrow;

		ThrowInstruction(bool hasResult, bool isRethrow)
		{
			_hasResult = hasResult;
			_rethrow = isRethrow;
		}

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return _hasResult ? 1 : 0; } }

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			var ex = frame.Pop();
			if (_rethrow)
			{
				ExceptionHandler handler;
				return frame.Interpreter.GotoHandler(frame, ex, out handler);
			}
			throw (Exception)ex;
		}
	}

	/// <summary>switch 分岐命令を表します。</summary>
	sealed class SwitchInstruction : Instruction
	{
		readonly Dictionary<int, int> _cases;

		/// <summary>値と移動先オフセットのマッピングを使用して、<see cref="Microsoft.Scripting.Interpreter.SwitchInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="cases">値から移動先オフセットへのマッピングを指定します。</param>
		internal SwitchInstruction(Dictionary<int, int> cases)
		{
			Assert.NotNull(cases);
			_cases = cases;
		}

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 0; } }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			int target;
			return _cases.TryGetValue((int)frame.Pop(), out target) ? target : 1;
		}
	}

	/// <summary>ループの開始を示す命令を表します。</summary>
	sealed class EnterLoopInstruction : Instruction
	{
		readonly int _instructionIndex;
		Dictionary<ParameterExpression, LocalVariable> _variables;
		Dictionary<ParameterExpression, LocalVariable> _closureVariables;
		LoopExpression _loop;
		int _loopEnd;
		int _compilationThreshold;

		/// <summary>ループを表すノード、ローカル変数、コンパイル閾値、命令インデックスを使用して、<see cref="Microsoft.Scripting.Interpreter.EnterLoopInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="loop">開始されるループを表すノードを指定します。</param>
		/// <param name="locals">ローカル変数のセットを指定します。</param>
		/// <param name="compilationThreshold">インタプリタとして実行される最大実行回数を指定します。</param>
		/// <param name="instructionIndex">この命令の命令インデックスを指定します。</param>
		internal EnterLoopInstruction(LoopExpression loop, LocalVariables locals, int compilationThreshold, int instructionIndex)
		{
			_loop = loop;
			_variables = locals.CopyLocals();
			_closureVariables = locals.ClosureVariables;
			_compilationThreshold = compilationThreshold;
			_instructionIndex = instructionIndex;
		}

		/// <summary>ループ終了時点の命令インデックスを指定してループの範囲を通知します。</summary>
		/// <param name="loopEnd">ループ終了時点の命令インデックスを指定します。</param>
		internal void FinishLoop(int loopEnd) { _loopEnd = loopEnd; }

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame)
		{
			// 頻繁に実行されるヒットパスなので、ここではロックしません。
			//
			// 複数スレッドによる競合が発生する可能性がありますが問題ありません。
			// 2 つの問題が発生する可能性があります。
			//   * デクリメントを逃す。(先にカウンタを設定するスレッドがある)
			//   * "if" 分岐に 2 回以上入る
			//
			// 1 番目は問題ありません。単純にコンパイルまでに長くかかるだけです。
			// 2 番目は Compile() 内で明示的に防いでいます。
			// 
			// 0 を逃がすことはありません。-1 を書き込んだ最初のスレッドは 0 を読み取る必要があり、このためコンパイルは開始されるからです。
			if (unchecked(_compilationThreshold--) == 0)
			{
				if (frame.Interpreter.CompileSynchronously)
					Compile(frame);
				else
					ThreadPool.QueueUserWorkItem(Compile, frame); // 別のスレッドでコンパイルするため、実行を継続できます。
			}
			return 1;
		}

		bool Compiled { get { return _loop == null; } }

		void Compile(object frameObj)
		{
			if (Compiled)
				return;
			lock (this)
			{
				if (Compiled)
					return;
				PerfTrack.NoteEvent(PerfTrack.Category.Compiler, "Interpreted loop compiled");
				var frame = (InterpretedFrame)frameObj;
				// この命令を最適化されたものに置き換える
				frame.Interpreter.Instructions.Instructions[_instructionIndex] = new CompiledLoopInstruction(new LoopCompiler(_loop, frame.Interpreter.LabelMapping, _variables, _closureVariables, _instructionIndex, _loopEnd).CreateDelegate());
				// この命令を無効化する。これをもっているスレッドがあるかもしれない
				_loop = null;
				_variables = null;
				_closureVariables = null;
			}
		}
	}

	/// <summary>コンパイルされたループ命令を表します。</summary>
	sealed class CompiledLoopInstruction : Instruction
	{
		readonly LoopFunc _compiledLoop;

		/// <summary>指定されたループを表すデリゲートを使用して、<see cref="Microsoft.Scripting.Interpreter.CompiledLoopInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="compiledLoop">実際にループを実行するデリゲートを指定します。</param>
		public CompiledLoopInstruction(LoopFunc compiledLoop)
		{
			Assert.NotNull(compiledLoop);
			_compiledLoop = compiledLoop;
		}

		/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
		/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
		/// <returns>次に実行する命令へのオフセット。</returns>
		public override int Run(InterpretedFrame frame) { return _compiledLoop(frame.Data, frame.Closure, frame); }
	}
}
