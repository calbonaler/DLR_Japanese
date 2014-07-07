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

using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Actions.Calls
{
	/// <summary>メソッドに渡される実引数のセットを表します。</summary>
	public sealed class ActualArguments
	{
		// Index into _args array indicating the first post-splat argument or -1 of there are no splatted arguments.
		// For call site f(a,b,*c,d) and preSplatLimit == 1 and postSplatLimit == 2
		// args would be (a,b,c[0],c[n-2],c[n-1],d) with splat index 3, where n = c.Count.
		/// <summary>実引数に関する情報を使用して、<see cref="Microsoft.Scripting.Actions.Calls.ActualArguments"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="args">実引数を指定します。</param>
		/// <param name="namedArgs">名前付き実引数を指定します。</param>
		/// <param name="argNames">名前付き実引数の名前を指定します。</param>
		/// <param name="hiddenCount">エラー報告に使用される隠された実引数の数を指定します。</param>
		/// <param name="collapsedCount">折りたたまれた実引数の数を指定します。</param>
		/// <param name="firstSplattedArg">展開された実引数の先頭の引数リスト内での位置を指定します。</param>
		/// <param name="splatIndex">省略された展開された実引数の先頭の引数リスト内での位置を指定します。</param>
		public ActualArguments(IList<DynamicMetaObject> args, IList<DynamicMetaObject> namedArgs, IList<string> argNames, int hiddenCount, int collapsedCount, int firstSplattedArg, int splatIndex)
		{
			ContractUtils.RequiresNotNullItems(args, "args");
			ContractUtils.RequiresNotNullItems(namedArgs, "namedArgs");
			ContractUtils.RequiresNotNullItems(argNames, "argNames");
			ContractUtils.Requires(namedArgs.Count == argNames.Count);

			ContractUtils.Requires(splatIndex == -1 || firstSplattedArg == -1 || firstSplattedArg >= 0 && firstSplattedArg <= splatIndex);
			ContractUtils.Requires(splatIndex == -1 || splatIndex >= 0);
			ContractUtils.Requires(collapsedCount >= 0);
			ContractUtils.Requires(hiddenCount >= 0);

			Arguments = args;
			NamedArguments = namedArgs;
			ArgNames = argNames;
			CollapsedCount = collapsedCount;
			SplatIndex = collapsedCount > 0 ? splatIndex : -1;
			FirstSplattedArg = firstSplattedArg;
			HiddenCount = hiddenCount;
		}

		/// <summary>折りたたまれた引数の数を取得します。</summary>
		public int CollapsedCount { get; private set; }

		/// <summary>省略された展開された実引数の先頭の引数リスト内での位置を取得します。</summary>
		public int SplatIndex { get; private set; }

		/// <summary>展開された実引数の先頭の引数リスト内での位置を取得します。</summary>
		public int FirstSplattedArg { get; private set; }

		/// <summary>名前付き実引数の名前を取得します。</summary>
		public IList<string> ArgNames { get; private set; }

		/// <summary>名前付き実引数を取得します。</summary>
		public IList<DynamicMetaObject> NamedArguments { get; private set; }

		/// <summary>実引数を取得します。</summary>
		public IList<DynamicMetaObject> Arguments { get; private set; }

		internal int ToSplattedItemIndex(int collapsedArgIndex) { return SplatIndex - FirstSplattedArg + collapsedArgIndex; }

		/// <summary>折りたたまれた実引数を含まない実引数の数を取得します。</summary>
		public int Count { get { return Arguments.Count + NamedArguments.Count; } }

		/// <summary>エラー報告に使用される隠された実引数の数を取得します。</summary>
		public int HiddenCount { get; private set; }

		/// <summary>コールサイトに渡された折りたたまれた実引数を含む可視である実引数の総数を取得します。</summary>
		public int VisibleCount { get { return Count + CollapsedCount - HiddenCount; } }

		/// <summary>指定されたインデックスの実引数を取得します。</summary>
		/// <param name="index">インデックスを指定します。</param>
		/// <returns>指定されたインデックスの実引数。</returns>
		public DynamicMetaObject this[int index] { get { return index < Arguments.Count ? Arguments[index] : NamedArguments[index - Arguments.Count]; } }

		/// <summary>
		/// 名前付き実引数を仮引数に関連付け、名前付き実引数と対応する仮引数の間の関係を示すインデックスの置換を返します。
		/// このメソッドは重複および関連付けられていない名前付き引数を確認します。
		/// </summary>
		/// <param name="method">関連付ける仮引数を持つメソッドを指定します。</param>
		/// <param name="binding">関連付けの結果として得られる置換を格納する変数を指定します。</param>
		/// <param name="failure">関連付けが失敗した際に <see cref="CallFailure"/> オブジェクトが格納される変数を指定します。</param>
		/// <returns>関連付けが成功した場合は <c>true</c>。それ以外の場合は <c>false</c>。</returns>
		/// <remarks>すべての i に対して、namedArgs[i] は parameters[args.Length + bindingPermutation[i]] に関連付けられていることを保証します。</remarks>
		internal bool TryBindNamedArguments(MethodCandidate method, out ArgumentBinding binding, out CallFailure failure)
		{
			if (NamedArguments.Count == 0)
			{
				binding = new ArgumentBinding(Arguments.Count);
				failure = null;
				return true;
			}
			var permutation = new int[NamedArguments.Count];
			var boundParameters = new BitArray(NamedArguments.Count);
			for (int i = 0; i < permutation.Length; i++)
				permutation[i] = -1;
			List<string> unboundNames = null;
			List<string> duppedNames = null;
			int positionalArgCount = Arguments.Count;
			for (int i = 0; i < ArgNames.Count; i++)
			{
				int paramIndex = method.IndexOfParameter(ArgNames[i]);
				if (paramIndex >= 0)
				{
					int nameIndex = paramIndex - positionalArgCount;
					// argument maps to already bound parameter:
					if (paramIndex < positionalArgCount || boundParameters[nameIndex])
					{
						if (duppedNames == null)
							duppedNames = new List<string>();
						duppedNames.Add(ArgNames[i]);
					}
					else
					{
						permutation[i] = nameIndex;
						boundParameters[nameIndex] = true;
					}
				}
				else
				{
					if (unboundNames == null)
						unboundNames = new List<string>();
					unboundNames.Add(ArgNames[i]);
				}
			}
			binding = new ArgumentBinding(positionalArgCount, permutation);
			if (unboundNames != null)
			{
				failure = new CallFailure(method, unboundNames.ToArray(), true);
				return false;
			}
			if (duppedNames != null)
			{
				failure = new CallFailure(method, duppedNames.ToArray(), false);
				return false;
			}
			failure = null;
			return true;
		}
	}
}
