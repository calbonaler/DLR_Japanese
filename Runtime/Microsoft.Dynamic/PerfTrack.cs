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
using System.IO;
using System.Linq;
using System.Security;

namespace Microsoft.Scripting
{
	/// <summary>
	/// 時間のかかる操作に対するパフォーマンスカウントを高速に収集する際に使用されるメソッド群を提供します。
	/// 通常このような操作はリフレクションまたはコード生成に関係する操作を意味します。
	/// 長期にわたってこの処理が通常のパフォーマンスカウンターアーキテクチャに含まれるかを確かめる必要があります。
	/// </summary>
	public static class PerfTrack
	{
		/// <summary>パフォーマンスイベントのカテゴリを表します。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")] // TODO: fix
		public enum Category
		{
			/// <summary>迅速な調査のための一時的なカテゴリを表します。</summary>
			Temporary,
			/// <summary>型オブジェクトに対する処理を表します。</summary>
			ReflectedTypes,
			/// <summary>例外のスローを表します。</summary>
			Exceptions,
			/// <summary>プロパティの取得または設定を表します。</summary>
			Properties,
			/// <summary>フィールドの取得または設定を表します。</summary>
			Fields,
			/// <summary>MethodBase.Invoke を通したメソッド呼び出しを表します。</summary>
			Methods,
			/// <summary>ReflectOptimizer を通じてコンパイルされたメソッドを表します。</summary>
			Compiler,
			/// <summary>デリゲートに対する新しいメソッドを作成したことを表します。</summary>
			DelegateCreate,
			/// <summary>辞書によるアクセスを表します。</summary>
			DictInvoke,
			/// <summary>型に対する演算子呼び出しを表します。</summary>
			OperatorInvoke,
			/// <summary>必要以上に割り当てをしなければならない理想的ではないアルゴリズムの存在している場所を表します。</summary>
			OverAllocate,
			/// <summary>規則またはアクションに関連する操作を表します。</summary>
			Rules,
			/// <summary>規則が評価されたことを表します。</summary>
			RuleEvaluation,
			/// <summary>規則がバインドされたことを表します。</summary>
			Binding,
			/// <summary>低速なバインディングを表します。</summary>
			BindingSlow,
			/// <summary>高速なバインディングを表します。</summary>
			BindingFast,
			/// <summary>規則が特定の型のターゲットに対してバインドされたことを表します。</summary>
			BindingTarget,
			/// <summary>任意のパフォーマンスカウントイベントを表します。</summary>
			Count
		}

		[MultiRuntimeAware]
		static int totalEvents;
		static readonly Dictionary<Category, Dictionary<string, int>> _events = MakeEventsDictionary();
		static readonly Dictionary<Category, int> summaryStats = new Dictionary<Category, int>();

		static Dictionary<Category, Dictionary<string, int>> MakeEventsDictionary()
		{
			var result = new Dictionary<Category, Dictionary<string, int>>();
			for (int i = 0; i <= (int)Category.Count; i++)
				result[(Category)i] = new Dictionary<string, int>();
			return result;
		}

		/// <summary>指定されたヒストグラムの内容を標準出力に表示します。ヒストグラムは値によって昇順に並び替えられます。</summary>
		/// <typeparam name="TKey">ヒストグラムのキーの型を指定します。</typeparam>
		/// <param name="histogram">出力するヒストグラムを指定します。</param>
		public static void DumpHistogram<TKey>(IDictionary<TKey, int> histogram) { DumpHistogram(histogram, Console.Out); }

		/// <summary>指定されたヒストグラムの内容を指定された <see cref="TextWriter"/> に書き込みます。ヒストグラムは値によって昇順に並び替えられます。</summary>
		/// <typeparam name="TKey">ヒストグラムのキーの型を指定します。</typeparam>
		/// <param name="histogram">出力するヒストグラムを指定します。</param>
		/// <param name="output">ヒストグラムを出力する <see cref="TextWriter"/> を指定します。</param>
		public static void DumpHistogram<TKey>(IDictionary<TKey, int> histogram, TextWriter output)
		{
			foreach (var kvp in histogram.OrderBy(x => x.Value))
				output.WriteLine("{0} {1}", kvp.Key, kvp.Value);
		}

		/// <summary>2 個のヒストグラムの内容を 1 つに統合します。</summary>
		/// <typeparam name="TKey">ヒストグラムのキーの型を指定します。</typeparam>
		/// <param name="result">統合された結果が書き込まれるヒストグラムを指定します。</param>
		/// <param name="addend"><paramref name="result"/> に統合するヒストグラムを指定します。</param>
		public static void AddHistograms<TKey>(IDictionary<TKey, int> result, IDictionary<TKey, int> addend)
		{
			int value;
			foreach (var entry in addend)
				result[entry.Key] = entry.Value + (result.TryGetValue(entry.Key, out value) ? value : 0);
		}

		/// <summary>ヒストグラムの指定されたキーに対する値を 1 増加させます。</summary>
		/// <typeparam name="TKey">ヒストグラムのキーの型を指定します。</typeparam>
		/// <param name="histogram">値を増加させるヒストグラムを指定します。</param>
		/// <param name="key">値を増加させるエントリを示すキーを指定します。</param>
		public static void IncrementEntry<TKey>(IDictionary<TKey, int> histogram, TKey key)
		{
			int value;
			histogram[key] = histogram.TryGetValue(key, out value) ? value + 1 : 1;
		}

		/// <summary>このクラスで収集したパフォーマンス統計情報を標準出力に表示します。</summary>
		public static void DumpStats() { DumpStats(Console.Out); }

		/// <summary>このクラスで収集したパフォーマンス統計情報を指定された <see cref="TextWriter"/> に書き込みます。</summary>
		/// <param name="output">統計情報を書き込む <see cref="TextWriter"/> を指定します。</param>
		public static void DumpStats(TextWriter output)
		{
			if (totalEvents == 0)
				return;
			// numbers from AMD Opteron 244 1.8 Ghz, 2.00GB of ram, running on IronPython 1.0 Beta 4 against Whidbey RTM.
			const double CALL_TIME = 0.0000051442355;
			const double THROW_TIME = 0.000025365656;
			const double FIELD_TIME = 0.0000018080093;
			output.WriteLine();
			output.WriteLine("---- 性能の詳細 ----");
			output.WriteLine();
			foreach (var kvp in _events)
			{
				if (kvp.Value.Count > 0)
				{
					output.WriteLine("カテゴリ : " + kvp.Key);
					DumpHistogram(kvp.Value, output);
					output.WriteLine();
				}
			}
			output.WriteLine();
			output.WriteLine("---- 性能の概要 ----");
			output.WriteLine();
			double knownTimes = 0;
			foreach (var kvp in summaryStats)
			{
				switch (kvp.Key)
				{
					case Category.Exceptions:
						output.WriteLine("全例外 ({0}) = {1}  (スロー時間 = ~{2} secs)", kvp.Key, kvp.Value, kvp.Value * THROW_TIME);
						knownTimes += kvp.Value * THROW_TIME;
						break;
					case Category.Fields:
						output.WriteLine("全フィールド = {0} (時間 = ~{1} secs)", kvp.Value, kvp.Value * FIELD_TIME);
						knownTimes += kvp.Value * FIELD_TIME;
						break;
					case Category.Methods:
						output.WriteLine("全呼び出し = {0} (呼び出し時間 = ~{1} secs)", kvp.Value, kvp.Value * CALL_TIME);
						knownTimes += kvp.Value * CALL_TIME;
						break;
					//case Categories.Properties:
					default:
						output.WriteLine("全体 {1} = {0}", kvp.Value, kvp.Key);
						break;
				}
			}
			output.WriteLine();
			output.WriteLine("全体の既知の時間: {0}", knownTimes);
		}

		/// <summary>指定されたカテゴリのイベントの発生を記録します。</summary>
		/// <param name="category">発生したイベントのカテゴリを指定します。</param>
		/// <param name="key">発生したイベントの詳細を示すキーを指定します。</param>
		[Conditional("DEBUG")]
		public static void NoteEvent(Category category, object key)
		{
			if (!DebugOptions.TrackPerformance)
				return;
			var categoryEvents = _events[category];
			totalEvents++;
			lock (categoryEvents)
			{
				var ex = key as Exception;
				var name = ex != null ? ex.GetType().ToString() : key.ToString();
				int v;
				categoryEvents[name] = !categoryEvents.TryGetValue(name, out v) ? 1 : v + 1;
				summaryStats[category] = !summaryStats.TryGetValue(category, out v) ? 1 : v + 1;
			}
		}
	}

	/// <summary>
	/// このクラスはこのアセンブリ内で使用される内部的なデバッグオプションを保持します。
	/// これらのオプションは環境変数 DLR_{option-name} を通して設定することができます。
	/// ブール値のオプションは "true" は true、それ以外は false とみなされます。
	/// 
	/// これらのオプションは内部的なデバッグのためにのみ存在し、どのようなパブリック API を通しても公開するべきではありません。
	/// </summary>
	static class DebugOptions
	{
		static bool ReadOption(string name) { return "true".Equals(ReadString(name), StringComparison.OrdinalIgnoreCase); }

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "name")]
		static bool ReadDebugOption(string name)
		{
#if DEBUG
			return ReadOption(name);
#else
            return false;
#endif
		}

		static string ReadString(string name)
		{
			try { return Environment.GetEnvironmentVariable("DLR_" + name); }
			catch (SecurityException) { return null; }
		}

		readonly static bool _trackPerformance = ReadDebugOption("TrackPerformance");

		internal static bool TrackPerformance { get { return _trackPerformance; } }
	}
}
