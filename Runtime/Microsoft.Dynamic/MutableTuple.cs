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
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting
{
	/// <summary>値を変更可能な組オブジェクトの抽象基本クラスです。</summary>
	public abstract class MutableTuple
	{
		/// <summary>組オブジェクトがネストせずに保持することができる要素の最大数を示します。</summary>
		public const int MaxSize = 128;
		static readonly ConcurrentDictionary<Type, int> _sizeDict = new ConcurrentDictionary<Type, int>();

		/// <summary>この組オブジェクトの指定されたインデックスにある値を取得します。</summary>
		/// <param name="index">値を取得するインデックスを指定します。</param>
		/// <returns>取得された値。</returns>
		public abstract object GetValue(int index);

		/// <summary>この組オブジェクトの指定されたインデックスに値を設定します。</summary>
		/// <param name="index">値を設定する場所を示すインデックスを指定します。</param>
		/// <param name="value">設定する値を指定します。</param>
		public abstract void SetValue(int index, object value);

		/// <summary>
		/// 指定されたサイズの組オブジェクトの指定されたインデックスにある値を取得します。
		/// このメソッドはネストされた組オブジェクトを走査して正確なインデックスの値を取得できます。
		/// </summary>
		/// <param name="size">ネストされている組オブジェクトに含まれている実際の要素数を指定します。</param>
		/// <param name="index">値を取得するインデックスを指定します。</param>
		/// <returns>取得された値。</returns>
		public object GetNestedValue(int size, int index)
		{
			if (size < MaxSize)
				return GetValue(index); // fast path
			else
			{
				object res = this; // slow path
				foreach (var i in GetAccessPath(size, index))
					res = ((MutableTuple)res).GetValue(i);
				return res;
			}
		}

		/// <summary>
		/// 指定されたサイズの組オブジェクトの指定されたインデックスに値を設定します。
		/// このメソッドはネストされた組オブジェクトを走査して正確なインデックスに値を設定できます。
		/// </summary>
		/// <param name="size">ネストされている組オブジェクトに含まれている実際の要素数を指定します。</param>
		/// <param name="index">値を設定する場所を示すインデックスを指定します。</param>
		/// <param name="value">設定する値を指定します。</param>
		public void SetNestedValue(int size, int index, object value)
		{
			if (size < MaxSize)
				SetValue(index, value); // fast path
			else
			{
				var res = this; // slow path
				var lastAccess = -1;
				foreach (var i in GetAccessPath(size, index))
				{
					if (lastAccess != -1)
						res = (MutableTuple)res.GetValue(lastAccess);
					lastAccess = i;
				}
				res.SetValue(lastAccess, value);
			}
		}

		/// <summary>指定された数の要素を格納するのに十分な数のスロットを持つ組オブジェクトの型を返します。</summary>
		/// <param name="size">組オブジェクトに格納する要素の数を指定します。</param>
		/// <returns>指定された数の要素を格納するのに十分な大きさの組オブジェクト。十分な大きさの型がないときは <c>null</c>。</returns>
		public static Type GetTupleType(int size)
		{
			if (size <= MaxSize)
			{
				if (size <= 1)
					return typeof(MutableTuple<>);
				else if (size <= 2)
					return typeof(MutableTuple<,>);
				else if (size <= 4)
					return typeof(MutableTuple<,,,>);
				else if (size <= 8)
					return typeof(MutableTuple<,,,,,,,>);
				else if (size <= 16)
					return typeof(MutableTuple<,,,,,,,,,,,,,,,>);
				else if (size <= 32)
					return typeof(MutableTuple<,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,>);
				else if (size <= 64)
					return typeof(MutableTuple<,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,>);
				else
					return typeof(MutableTuple<,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,>);
			}
			return null;
		}

		/// <summary>
		/// 要素ごとの型を使用してジェネリックな組オブジェクトの型を作成します。
		/// 要素数が <see cref="MaxSize"/> 以下であれば単純に単一の型を作成します。
		/// それ以外の場合はネストされた組オブジェクトの型を返します。
		/// </summary>
		/// <param name="types">組オブジェクトに格納する各要素の型を指定します。</param>
		/// <returns>各要素が指定された型であるジェネリックな組オブジェクトの型。</returns>
		/// <exception cref="System.ArgumentNullException"><paramref name="types"/> は <c>null</c> です。</exception>
		/// <remarks>(例: 136個の要素数を持つ組オブジェクトを作成する場合、Tuple`128 および Tuple`8 を含む Tuple`2 を返します。)</remarks>
		public static Type MakeTupleType(params Type[] types)
		{
			ContractUtils.RequiresNotNull(types, "types");
			return MakeTupleType(types, 0, types.Length);
		}
		
		static Type MakeTupleType(Type[] types, int start, int end)
		{
			var size = end - start;
			var multiplier = 1;
			while (size > MaxSize)
			{
				size = (size + MaxSize - 1) / MaxSize; // RoundUp(size / MaxSize)
				multiplier *= MaxSize;
			}
			var type = GetTupleType(size);
			Debug.Assert(type != null);
			var typeArr = new Type[type.GetGenericArguments().Length];
			for (int i = 0; i < size; i++)
				typeArr[i] = multiplier > 1 ? MakeTupleType(types, start + i * multiplier, Math.Min(end, start + (i + 1) * multiplier)) : types[start + i];
			for (int i = size; i < typeArr.Length; i++)
				typeArr[i] = typeof(DynamicNull);
			return type.MakeGenericType(typeArr);
		}

		/// <summary>ネストされた組オブジェクトも含む指定された組オブジェクトの型で利用可能なスロット数を返します。</summary>
		/// <param name="tupleType">利用可能なスロット数を返す組オブジェクトの型を指定します。</param>
		/// <returns>指定された型に対する利用可能なスロット数。</returns>
		/// <exception cref="ArgumentNullException"><paramref name="tupleType"/> は <c>null</c> です。</exception>
		/// <exception cref="ArgumentException"><paramref name="tupleType"/> は <see cref="Microsoft.Scripting.MutableTuple"/> の派生型ではありません。</exception>
		public static int GetSize(Type tupleType)
		{
			ContractUtils.RequiresNotNull(tupleType, "tupleType");
			ContractUtils.Requires(typeof(MutableTuple).IsAssignableFrom(tupleType), "tupleType");
			return _sizeDict.GetOrAdd(tupleType, key =>
			{
				int count = 0;
				for (var types = new Stack<Type>(key.GetGenericArguments()); types.Count > 0; )
				{
					var t = types.Pop();
					if (typeof(MutableTuple).IsAssignableFrom(t))
					{
						foreach (var subtype in t.GetGenericArguments())
							types.Push(subtype);
					}
					else if (t != typeof(DynamicNull))
						count++;
				}
				return count;
			});
		}

		/// <summary>
		/// 指定された型の組オブジェクトを指定された引数を使用して作成します。
		/// 組オブジェクトがネストされていた場合は、ネストされたそれぞれのスロットに対して値が格納されます。
		/// </summary>
		/// <param name="tupleType">作成する組オブジェクトの型を指定します。</param>
		/// <param name="args">作成された組オブジェクトに設定される値を指定します。</param>
		/// <returns>値が設定された新しい組オブジェクト。</returns>
		/// <exception cref="ArgumentNullException"><paramref name="tupleType"/> または <paramref name="args"/> は <c>null</c> です。</exception>
		public static MutableTuple MakeTuple(Type tupleType, params object[] args)
		{
			ContractUtils.RequiresNotNull(tupleType, "tupleType");
			ContractUtils.RequiresNotNull(args, "args");
			var size = args.Length;
			var res = (MutableTuple)Activator.CreateInstance(tupleType);
			var multiplier = 1;
			while (size > MaxSize)
			{
				size = (size + MaxSize - 1) / MaxSize;
				multiplier *= MaxSize;
			}
			for (int i = 0; i < size; i++)
			{
				if (multiplier > 1)
				{
					var creating = tupleType.GetProperty(string.Format("Item{0:D3}", i)).PropertyType;
					var realArgs = new object[creating.GetGenericArguments().Length];
					Array.Copy(args, i * multiplier, realArgs, 0, Math.Min(args.Length, (i + 1) * multiplier) - i * multiplier);
					res.SetValue(i, Activator.CreateInstance(creating, realArgs));
				}
				else
					res.SetValue(i, args[i]);
			}
			return res;
		}

		/// <summary>ネストされた組オブジェクトも含むこの組オブジェクトに格納されているすべての値を取得します。</summary>
		/// <returns>組オブジェクトに格納されているすべての値のシーケンス。</returns>
		public IEnumerable<object> GetTupleValues()
		{
			return GetType().GetGenericArguments().SelectMany((x, i) =>
				typeof(MutableTuple).IsAssignableFrom(x) ? ((MutableTuple)GetValue(i)).GetTupleValues() : Enumerable.Repeat(GetValue(i), 1)
			);
		}

		/// <summary>ネストされている可能性のある組オブジェクト内の特定の論理要素にアクセスするために使用するプロパティのシーケンスを返します。</summary>
		/// <param name="tupleType">プロパティのシーケンスを取得する組オブジェクトの型を指定します。</param>
		/// <param name="index">アクセスするためにプロパティのシーケンスを返す論理要素のインデックスを指定します。</param>
		/// <returns>特定の論理要素にアクセスするために使用するプロパティのシーケンス。</returns>
		/// <exception cref="ArgumentNullException"><paramref name="tupleType"/> は <c>null</c> です。</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> は 0 未満または型で許容できる最大要素数以上の値です。</exception>
		public static IEnumerable<PropertyInfo> GetAccessPath(Type tupleType, int index)
		{
			ContractUtils.RequiresNotNull(tupleType, "tupleType");
			int size = GetSize(tupleType);
			if (index < 0 || index >= size)
				throw new ArgumentOutOfRangeException("index");
			foreach (int curIndex in GetAccessPath(size, index))
			{
				var pi = tupleType.GetProperty(string.Format("Item{0:D3}", curIndex));
				Debug.Assert(pi != null);
				yield return pi;
				tupleType = pi.PropertyType;
			}
		}

		static IEnumerable<int> GetAccessPath(int size, int index)
		{
			// We get the final index by breaking the index into groups of bits.
			// The more significant bits represent the indexes into the outermost tuples and the least significant bits index into the inner most tuples.
			// The mask is initialized to mask the upper bits and adjust is initialized and adjust is the value we need to divide by to get the index in the least significant bits.
			// As we go through we shift the mask and adjust down each loop to pull out the inner slot.
			// Logically everything in here is shifting bits (not multiplying or dividing) because NewTuple.MaxSize is a power of 2.
			int mask = MaxSize - 1;
			int adjust = 1;
			while (size > MaxSize)
			{
				size /= MaxSize;
				mask *= MaxSize;
				adjust *= MaxSize;
			}
			while (adjust > 0)
			{
				Debug.Assert(mask != 0);
				yield return (index & mask) / adjust;
				mask /= MaxSize;
				adjust /= MaxSize;
			}
		}

		/// <summary>この組オブジェクトが格納できる最大の要素数を取得します。</summary>
		public abstract int Capacity { get; }

		/// <summary>指定された値を用いて組オブジェクトを作成する式を作成します。</summary>
		/// <param name="values">組オブジェクトに設定する値を指定します。</param>
		/// <returns>組オブジェクトを作成する式。</returns>
		public static Expression Create(params Expression[] values) { return Create(MakeTupleType(Array.ConvertAll(values, x => x.Type)), 0, values.Length, values); }

		static int PowerOfTwoRound(int value)
		{
			var n1 = value - 1;
			var n2 = n1 | (n1 >>  1);
			var n3 = n2 | (n2 >>  2);
			var n4 = n3 | (n3 >>  4);
			var n5 = n4 | (n4 >>  8);
			var n6 = n5 | (n5 >> 16);
			var n7 = n6 | (n6 >> 32);
			return n7 + 1;
		}

		static Expression Create(Type type, int start, int end, Expression[] values)
		{
			var size = end - start;
			Debug.Assert(type.IsSubclassOf(typeof(MutableTuple)));
			var multiplier = 1;
			while (size > MaxSize)
			{
				size = (size + MaxSize - 1) / MaxSize;
				multiplier *= MaxSize;
			}
			var newValues = new Expression[PowerOfTwoRound(size)];
			for (int i = 0; i < size; i++)
				newValues[i] = multiplier > 1 ?
					Create(type.GetProperty(string.Format("Item{0:D3}", i)).PropertyType, start + i * multiplier, Math.Min(end, start + (i + 1) * multiplier), values) :
					values[i + start];
			for (int i = size; i < newValues.Length; i++)
				newValues[i] = Expression.Constant(null, typeof(DynamicNull));
			return Expression.New(type.GetConstructor(Array.ConvertAll(newValues, x => x.Type)), newValues);
		}
	}

	/// <summary>1 個の要素からなる変更可能な組を表します。</summary>
	/// <typeparam name="T0">組オブジェクトの 1 番目の要素の型を指定します。</typeparam>
	[GeneratedCode("DLR", "2.0")]
	public class MutableTuple<T0> : MutableTuple
	{
		/// <summary><see cref="Microsoft.Scripting.MutableTuple&lt;T0&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		public MutableTuple() { }

		/// <summary>要素を使用して、<see cref="Microsoft.Scripting.MutableTuple&lt;T0&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="item0">組オブジェクトの 1 番目の要素を指定します。</param>
		public MutableTuple(T0 item0) : base() { Item000 = item0; }

		/// <summary>組オブジェクトの 1 番目の要素を取得または設定します。</summary>
		public T0 Item000 { get; set; }

		/// <summary>この組オブジェクトの指定されたインデックスにある値を取得します。</summary>
		/// <param name="index">値を取得するインデックスを指定します。</param>
		/// <returns>取得された値。</returns>
		public override object GetValue(int index)
		{
			switch (index)
			{
				case 0: return Item000;
				default: throw new ArgumentOutOfRangeException("index");
			}
		}

		/// <summary>この組オブジェクトの指定されたインデックスに値を設定します。</summary>
		/// <param name="index">値を設定する場所を示すインデックスを指定します。</param>
		/// <param name="value">設定する値を指定します。</param>
		public override void SetValue(int index, object value)
		{
			switch (index)
			{
				case 0: Item000 = (T0)value; break;
				default: throw new ArgumentOutOfRangeException("index");
			}
		}

		/// <summary>この組オブジェクトが格納できる最大の要素数を取得します。</summary>
		public override int Capacity { get { return 1; } }
	}

	/// <summary>2 個の要素からなる変更可能な組を表します。</summary>
	/// <typeparam name="T0">組オブジェクトの 1 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T1">組オブジェクトの 2 番目の要素の型を指定します。</typeparam>
	[GeneratedCode("DLR", "2.0")]
	public class MutableTuple<T0, T1> : MutableTuple<T0>
	{
		/// <summary><see cref="Microsoft.Scripting.MutableTuple&lt;T0, T1&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		public MutableTuple() { }

		/// <summary>要素を使用して、<see cref="Microsoft.Scripting.MutableTuple&lt;T0, T1&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="item0">組オブジェクトの 1 番目の要素を指定します。</param>
		/// <param name="item1">組オブジェクトの 2 番目の要素を指定します。</param>
		public MutableTuple(T0 item0, T1 item1) : base(item0) { Item001 = item1; }

		/// <summary>組オブジェクトの 2 番目の要素を取得または設定します。</summary>
		public T1 Item001 { get; set; }

		/// <summary>この組オブジェクトの指定されたインデックスにある値を取得します。</summary>
		/// <param name="index">値を取得するインデックスを指定します。</param>
		/// <returns>取得された値。</returns>
		public override object GetValue(int index)
		{
			switch (index)
			{
				case 0: return Item000;
				case 1: return Item001;
				default: throw new ArgumentOutOfRangeException("index");
			}
		}

		/// <summary>この組オブジェクトの指定されたインデックスに値を設定します。</summary>
		/// <param name="index">値を設定する場所を示すインデックスを指定します。</param>
		/// <param name="value">設定する値を指定します。</param>
		public override void SetValue(int index, object value)
		{
			switch (index)
			{
				case 0: Item000 = (T0)value; break;
				case 1: Item001 = (T1)value; break;
				default: throw new ArgumentOutOfRangeException("index");
			}
		}

		/// <summary>この組オブジェクトが格納できる最大の要素数を取得します。</summary>
		public override int Capacity { get { return 2; } }
	}

	/// <summary>4 個の要素からなる変更可能な組を表します。</summary>
	/// <typeparam name="T0">組オブジェクトの 1 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T1">組オブジェクトの 2 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T2">組オブジェクトの 3 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T3">組オブジェクトの 4 番目の要素の型を指定します。</typeparam>
	[GeneratedCode("DLR", "2.0")]
	public class MutableTuple<T0, T1, T2, T3> : MutableTuple<T0, T1>
	{
		/// <summary><see cref="Microsoft.Scripting.MutableTuple&lt;T0, T1, T2, T3&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		public MutableTuple() { }

		/// <summary>要素を使用して、<see cref="Microsoft.Scripting.MutableTuple&lt;T0, T1, T2, T3&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="item0">組オブジェクトの 1 番目の要素を指定します。</param>
		/// <param name="item1">組オブジェクトの 2 番目の要素を指定します。</param>
		/// <param name="item2">組オブジェクトの 3 番目の要素を指定します。</param>
		/// <param name="item3">組オブジェクトの 4 番目の要素を指定します。</param>
		public MutableTuple(T0 item0, T1 item1, T2 item2, T3 item3) : base(item0, item1)
		{
			Item002 = item2;
			Item003 = item3;
		}

		/// <summary>組オブジェクトの 3 番目の要素を取得または設定します。</summary>
		public T2 Item002 { get; set; }
		/// <summary>組オブジェクトの 4 番目の要素を取得または設定します。</summary>
		public T3 Item003 { get; set; }

		/// <summary>この組オブジェクトの指定されたインデックスにある値を取得します。</summary>
		/// <param name="index">値を取得するインデックスを指定します。</param>
		/// <returns>取得された値。</returns>
		public override object GetValue(int index)
		{
			switch (index)
			{
				case 0: return Item000;
				case 1: return Item001;
				case 2: return Item002;
				case 3: return Item003;
				default: throw new ArgumentOutOfRangeException("index");
			}
		}

		/// <summary>この組オブジェクトの指定されたインデックスに値を設定します。</summary>
		/// <param name="index">値を設定する場所を示すインデックスを指定します。</param>
		/// <param name="value">設定する値を指定します。</param>
		public override void SetValue(int index, object value)
		{
			switch (index)
			{
				case 0: Item000 = (T0)value; break;
				case 1: Item001 = (T1)value; break;
				case 2: Item002 = (T2)value; break;
				case 3: Item003 = (T3)value; break;
				default: throw new ArgumentOutOfRangeException("index");
			}
		}

		/// <summary>この組オブジェクトが格納できる最大の要素数を取得します。</summary>
		public override int Capacity { get { return 4; } }
	}

	/// <summary>8 個の要素からなる変更可能な組を表します。</summary>
	/// <typeparam name="T0">組オブジェクトの 1 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T1">組オブジェクトの 2 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T2">組オブジェクトの 3 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T3">組オブジェクトの 4 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T4">組オブジェクトの 5 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T5">組オブジェクトの 6 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T6">組オブジェクトの 7 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T7">組オブジェクトの 8 番目の要素の型を指定します。</typeparam>
	[GeneratedCode("DLR", "2.0")]
	public class MutableTuple<T0, T1, T2, T3, T4, T5, T6, T7> : MutableTuple<T0, T1, T2, T3>
	{
		/// <summary><see cref="Microsoft.Scripting.MutableTuple&lt;T0, T1, T2, T3, T4, T5, T6, T7&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		public MutableTuple() { }

		/// <summary>要素を使用して、<see cref="Microsoft.Scripting.MutableTuple&lt;T0, T1, T2, T3, T4, T5, T6, T7&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="item0">組オブジェクトの 1 番目の要素を指定します。</param>
		/// <param name="item1">組オブジェクトの 2 番目の要素を指定します。</param>
		/// <param name="item2">組オブジェクトの 3 番目の要素を指定します。</param>
		/// <param name="item3">組オブジェクトの 4 番目の要素を指定します。</param>
		/// <param name="item4">組オブジェクトの 5 番目の要素を指定します。</param>
		/// <param name="item5">組オブジェクトの 6 番目の要素を指定します。</param>
		/// <param name="item6">組オブジェクトの 7 番目の要素を指定します。</param>
		/// <param name="item7">組オブジェクトの 8 番目の要素を指定します。</param>
		public MutableTuple(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7) : base(item0, item1, item2, item3)
		{
			Item004 = item4;
			Item005 = item5;
			Item006 = item6;
			Item007 = item7;
		}

		/// <summary>組オブジェクトの 5 番目の要素を取得または設定します。</summary>
		public T4 Item004 { get; set; }
		/// <summary>組オブジェクトの 6 番目の要素を取得または設定します。</summary>
		public T5 Item005 { get; set; }
		/// <summary>組オブジェクトの 7 番目の要素を取得または設定します。</summary>
		public T6 Item006 { get; set; }
		/// <summary>組オブジェクトの 8 番目の要素を取得または設定します。</summary>
		public T7 Item007 { get; set; }

		/// <summary>この組オブジェクトの指定されたインデックスにある値を取得します。</summary>
		/// <param name="index">値を取得するインデックスを指定します。</param>
		/// <returns>取得された値。</returns>
		public override object GetValue(int index)
		{
			switch (index)
			{
				case 0: return Item000;
				case 1: return Item001;
				case 2: return Item002;
				case 3: return Item003;
				case 4: return Item004;
				case 5: return Item005;
				case 6: return Item006;
				case 7: return Item007;
				default: throw new ArgumentOutOfRangeException("index");
			}
		}

		/// <summary>この組オブジェクトの指定されたインデックスに値を設定します。</summary>
		/// <param name="index">値を設定する場所を示すインデックスを指定します。</param>
		/// <param name="value">設定する値を指定します。</param>
		public override void SetValue(int index, object value)
		{
			switch (index)
			{
				case 0: Item000 = (T0)value; break;
				case 1: Item001 = (T1)value; break;
				case 2: Item002 = (T2)value; break;
				case 3: Item003 = (T3)value; break;
				case 4: Item004 = (T4)value; break;
				case 5: Item005 = (T5)value; break;
				case 6: Item006 = (T6)value; break;
				case 7: Item007 = (T7)value; break;
				default: throw new ArgumentOutOfRangeException("index");
			}
		}

		/// <summary>この組オブジェクトが格納できる最大の要素数を取得します。</summary>
		public override int Capacity { get { return 8; } }
	}

	/// <summary>16 個の要素からなる変更可能な組を表します。</summary>
	/// <typeparam name="T0">組オブジェクトの 1 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T1">組オブジェクトの 2 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T2">組オブジェクトの 3 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T3">組オブジェクトの 4 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T4">組オブジェクトの 5 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T5">組オブジェクトの 6 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T6">組オブジェクトの 7 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T7">組オブジェクトの 8 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T8">組オブジェクトの 9 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T9">組オブジェクトの 10 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T10">組オブジェクトの 11 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T11">組オブジェクトの 12 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T12">組オブジェクトの 13 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T13">組オブジェクトの 14 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T14">組オブジェクトの 15 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T15">組オブジェクトの 16 番目の要素の型を指定します。</typeparam>
	[GeneratedCode("DLR", "2.0")]
	public class MutableTuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> : MutableTuple<T0, T1, T2, T3, T4, T5, T6, T7>
	{
		/// <summary><see cref="Microsoft.Scripting.MutableTuple&lt;T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		public MutableTuple() { }

		/// <summary>要素を使用して、<see cref="Microsoft.Scripting.MutableTuple&lt;T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="item0">組オブジェクトの 1 番目の要素を指定します。</param>
		/// <param name="item1">組オブジェクトの 2 番目の要素を指定します。</param>
		/// <param name="item2">組オブジェクトの 3 番目の要素を指定します。</param>
		/// <param name="item3">組オブジェクトの 4 番目の要素を指定します。</param>
		/// <param name="item4">組オブジェクトの 5 番目の要素を指定します。</param>
		/// <param name="item5">組オブジェクトの 6 番目の要素を指定します。</param>
		/// <param name="item6">組オブジェクトの 7 番目の要素を指定します。</param>
		/// <param name="item7">組オブジェクトの 8 番目の要素を指定します。</param>
		/// <param name="item8">組オブジェクトの 9 番目の要素を指定します。</param>
		/// <param name="item9">組オブジェクトの 10 番目の要素を指定します。</param>
		/// <param name="item10">組オブジェクトの 11 番目の要素を指定します。</param>
		/// <param name="item11">組オブジェクトの 12 番目の要素を指定します。</param>
		/// <param name="item12">組オブジェクトの 13 番目の要素を指定します。</param>
		/// <param name="item13">組オブジェクトの 14 番目の要素を指定します。</param>
		/// <param name="item14">組オブジェクトの 15 番目の要素を指定します。</param>
		/// <param name="item15">組オブジェクトの 16 番目の要素を指定します。</param>
		public MutableTuple(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12, T13 item13, T14 item14, T15 item15) : base(item0, item1, item2, item3, item4, item5, item6, item7)
		{
			Item008 = item8;
			Item009 = item9;
			Item010 = item10;
			Item011 = item11;
			Item012 = item12;
			Item013 = item13;
			Item014 = item14;
			Item015 = item15;
		}

		/// <summary>組オブジェクトの 9 番目の要素を取得または設定します。</summary>
		public T8 Item008 { get; set; }
		/// <summary>組オブジェクトの 10 番目の要素を取得または設定します。</summary>
		public T9 Item009 { get; set; }
		/// <summary>組オブジェクトの 11 番目の要素を取得または設定します。</summary>
		public T10 Item010 { get; set; }
		/// <summary>組オブジェクトの 12 番目の要素を取得または設定します。</summary>
		public T11 Item011 { get; set; }
		/// <summary>組オブジェクトの 13 番目の要素を取得または設定します。</summary>
		public T12 Item012 { get; set; }
		/// <summary>組オブジェクトの 14 番目の要素を取得または設定します。</summary>
		public T13 Item013 { get; set; }
		/// <summary>組オブジェクトの 15 番目の要素を取得または設定します。</summary>
		public T14 Item014 { get; set; }
		/// <summary>組オブジェクトの 16 番目の要素を取得または設定します。</summary>
		public T15 Item015 { get; set; }

		/// <summary>この組オブジェクトの指定されたインデックスにある値を取得します。</summary>
		/// <param name="index">値を取得するインデックスを指定します。</param>
		/// <returns>取得された値。</returns>
		public override object GetValue(int index)
		{
			switch (index)
			{
				case 0: return Item000;
				case 1: return Item001;
				case 2: return Item002;
				case 3: return Item003;
				case 4: return Item004;
				case 5: return Item005;
				case 6: return Item006;
				case 7: return Item007;
				case 8: return Item008;
				case 9: return Item009;
				case 10: return Item010;
				case 11: return Item011;
				case 12: return Item012;
				case 13: return Item013;
				case 14: return Item014;
				case 15: return Item015;
				default: throw new ArgumentOutOfRangeException("index");
			}
		}

		/// <summary>この組オブジェクトの指定されたインデックスに値を設定します。</summary>
		/// <param name="index">値を設定する場所を示すインデックスを指定します。</param>
		/// <param name="value">設定する値を指定します。</param>
		public override void SetValue(int index, object value)
		{
			switch (index)
			{
				case 0: Item000 = (T0)value; break;
				case 1: Item001 = (T1)value; break;
				case 2: Item002 = (T2)value; break;
				case 3: Item003 = (T3)value; break;
				case 4: Item004 = (T4)value; break;
				case 5: Item005 = (T5)value; break;
				case 6: Item006 = (T6)value; break;
				case 7: Item007 = (T7)value; break;
				case 8: Item008 = (T8)value; break;
				case 9: Item009 = (T9)value; break;
				case 10: Item010 = (T10)value; break;
				case 11: Item011 = (T11)value; break;
				case 12: Item012 = (T12)value; break;
				case 13: Item013 = (T13)value; break;
				case 14: Item014 = (T14)value; break;
				case 15: Item015 = (T15)value; break;
				default: throw new ArgumentOutOfRangeException("index");
			}
		}

		/// <summary>この組オブジェクトが格納できる最大の要素数を取得します。</summary>
		public override int Capacity { get { return 16; } }
	}

	/// <summary>32 個の要素からなる変更可能な組を表します。</summary>
	/// <typeparam name="T0">組オブジェクトの 1 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T1">組オブジェクトの 2 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T2">組オブジェクトの 3 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T3">組オブジェクトの 4 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T4">組オブジェクトの 5 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T5">組オブジェクトの 6 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T6">組オブジェクトの 7 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T7">組オブジェクトの 8 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T8">組オブジェクトの 9 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T9">組オブジェクトの 10 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T10">組オブジェクトの 11 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T11">組オブジェクトの 12 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T12">組オブジェクトの 13 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T13">組オブジェクトの 14 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T14">組オブジェクトの 15 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T15">組オブジェクトの 16 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T16">組オブジェクトの 17 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T17">組オブジェクトの 18 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T18">組オブジェクトの 19 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T19">組オブジェクトの 20 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T20">組オブジェクトの 21 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T21">組オブジェクトの 22 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T22">組オブジェクトの 23 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T23">組オブジェクトの 24 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T24">組オブジェクトの 25 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T25">組オブジェクトの 26 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T26">組オブジェクトの 27 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T27">組オブジェクトの 28 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T28">組オブジェクトの 29 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T29">組オブジェクトの 30 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T30">組オブジェクトの 31 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T31">組オブジェクトの 32 番目の要素の型を指定します。</typeparam>
	[GeneratedCode("DLR", "2.0")]
	public class MutableTuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31> : MutableTuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
	{
		/// <summary><see cref="Microsoft.Scripting.MutableTuple&lt;T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		public MutableTuple() { }

		/// <summary>要素を使用して、<see cref="Microsoft.Scripting.MutableTuple&lt;T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="item0">組オブジェクトの 1 番目の要素を指定します。</param>
		/// <param name="item1">組オブジェクトの 2 番目の要素を指定します。</param>
		/// <param name="item2">組オブジェクトの 3 番目の要素を指定します。</param>
		/// <param name="item3">組オブジェクトの 4 番目の要素を指定します。</param>
		/// <param name="item4">組オブジェクトの 5 番目の要素を指定します。</param>
		/// <param name="item5">組オブジェクトの 6 番目の要素を指定します。</param>
		/// <param name="item6">組オブジェクトの 7 番目の要素を指定します。</param>
		/// <param name="item7">組オブジェクトの 8 番目の要素を指定します。</param>
		/// <param name="item8">組オブジェクトの 9 番目の要素を指定します。</param>
		/// <param name="item9">組オブジェクトの 10 番目の要素を指定します。</param>
		/// <param name="item10">組オブジェクトの 11 番目の要素を指定します。</param>
		/// <param name="item11">組オブジェクトの 12 番目の要素を指定します。</param>
		/// <param name="item12">組オブジェクトの 13 番目の要素を指定します。</param>
		/// <param name="item13">組オブジェクトの 14 番目の要素を指定します。</param>
		/// <param name="item14">組オブジェクトの 15 番目の要素を指定します。</param>
		/// <param name="item15">組オブジェクトの 16 番目の要素を指定します。</param>
		/// <param name="item16">組オブジェクトの 17 番目の要素を指定します。</param>
		/// <param name="item17">組オブジェクトの 18 番目の要素を指定します。</param>
		/// <param name="item18">組オブジェクトの 19 番目の要素を指定します。</param>
		/// <param name="item19">組オブジェクトの 20 番目の要素を指定します。</param>
		/// <param name="item20">組オブジェクトの 21 番目の要素を指定します。</param>
		/// <param name="item21">組オブジェクトの 22 番目の要素を指定します。</param>
		/// <param name="item22">組オブジェクトの 23 番目の要素を指定します。</param>
		/// <param name="item23">組オブジェクトの 24 番目の要素を指定します。</param>
		/// <param name="item24">組オブジェクトの 25 番目の要素を指定します。</param>
		/// <param name="item25">組オブジェクトの 26 番目の要素を指定します。</param>
		/// <param name="item26">組オブジェクトの 27 番目の要素を指定します。</param>
		/// <param name="item27">組オブジェクトの 28 番目の要素を指定します。</param>
		/// <param name="item28">組オブジェクトの 29 番目の要素を指定します。</param>
		/// <param name="item29">組オブジェクトの 30 番目の要素を指定します。</param>
		/// <param name="item30">組オブジェクトの 31 番目の要素を指定します。</param>
		/// <param name="item31">組オブジェクトの 32 番目の要素を指定します。</param>
		public MutableTuple(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12, T13 item13, T14 item14, T15 item15, T16 item16, T17 item17, T18 item18, T19 item19, T20 item20, T21 item21, T22 item22, T23 item23, T24 item24, T25 item25, T26 item26, T27 item27, T28 item28, T29 item29, T30 item30, T31 item31)
			: base(item0, item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11, item12, item13, item14, item15)
		{
			Item016 = item16;
			Item017 = item17;
			Item018 = item18;
			Item019 = item19;
			Item020 = item20;
			Item021 = item21;
			Item022 = item22;
			Item023 = item23;
			Item024 = item24;
			Item025 = item25;
			Item026 = item26;
			Item027 = item27;
			Item028 = item28;
			Item029 = item29;
			Item030 = item30;
			Item031 = item31;
		}

		/// <summary>組オブジェクトの 17 番目の要素を取得または設定します。</summary>
		public T16 Item016 { get; set; }
		/// <summary>組オブジェクトの 18 番目の要素を取得または設定します。</summary>
		public T17 Item017 { get; set; }
		/// <summary>組オブジェクトの 19 番目の要素を取得または設定します。</summary>
		public T18 Item018 { get; set; }
		/// <summary>組オブジェクトの 20 番目の要素を取得または設定します。</summary>
		public T19 Item019 { get; set; }
		/// <summary>組オブジェクトの 21 番目の要素を取得または設定します。</summary>
		public T20 Item020 { get; set; }
		/// <summary>組オブジェクトの 22 番目の要素を取得または設定します。</summary>
		public T21 Item021 { get; set; }
		/// <summary>組オブジェクトの 23 番目の要素を取得または設定します。</summary>
		public T22 Item022 { get; set; }
		/// <summary>組オブジェクトの 24 番目の要素を取得または設定します。</summary>
		public T23 Item023 { get; set; }
		/// <summary>組オブジェクトの 25 番目の要素を取得または設定します。</summary>
		public T24 Item024 { get; set; }
		/// <summary>組オブジェクトの 26 番目の要素を取得または設定します。</summary>
		public T25 Item025 { get; set; }
		/// <summary>組オブジェクトの 27 番目の要素を取得または設定します。</summary>
		public T26 Item026 { get; set; }
		/// <summary>組オブジェクトの 28 番目の要素を取得または設定します。</summary>
		public T27 Item027 { get; set; }
		/// <summary>組オブジェクトの 29 番目の要素を取得または設定します。</summary>
		public T28 Item028 { get; set; }
		/// <summary>組オブジェクトの 30 番目の要素を取得または設定します。</summary>
		public T29 Item029 { get; set; }
		/// <summary>組オブジェクトの 31 番目の要素を取得または設定します。</summary>
		public T30 Item030 { get; set; }
		/// <summary>組オブジェクトの 32 番目の要素を取得または設定します。</summary>
		public T31 Item031 { get; set; }

		/// <summary>この組オブジェクトの指定されたインデックスにある値を取得します。</summary>
		/// <param name="index">値を取得するインデックスを指定します。</param>
		/// <returns>取得された値。</returns>
		public override object GetValue(int index)
		{
			switch (index)
			{
				case 0: return Item000;
				case 1: return Item001;
				case 2: return Item002;
				case 3: return Item003;
				case 4: return Item004;
				case 5: return Item005;
				case 6: return Item006;
				case 7: return Item007;
				case 8: return Item008;
				case 9: return Item009;
				case 10: return Item010;
				case 11: return Item011;
				case 12: return Item012;
				case 13: return Item013;
				case 14: return Item014;
				case 15: return Item015;
				case 16: return Item016;
				case 17: return Item017;
				case 18: return Item018;
				case 19: return Item019;
				case 20: return Item020;
				case 21: return Item021;
				case 22: return Item022;
				case 23: return Item023;
				case 24: return Item024;
				case 25: return Item025;
				case 26: return Item026;
				case 27: return Item027;
				case 28: return Item028;
				case 29: return Item029;
				case 30: return Item030;
				case 31: return Item031;
				default: throw new ArgumentOutOfRangeException("index");
			}
		}

		/// <summary>この組オブジェクトの指定されたインデックスに値を設定します。</summary>
		/// <param name="index">値を設定する場所を示すインデックスを指定します。</param>
		/// <param name="value">設定する値を指定します。</param>
		public override void SetValue(int index, object value)
		{
			switch (index)
			{
				case 0: Item000 = (T0)value; break;
				case 1: Item001 = (T1)value; break;
				case 2: Item002 = (T2)value; break;
				case 3: Item003 = (T3)value; break;
				case 4: Item004 = (T4)value; break;
				case 5: Item005 = (T5)value; break;
				case 6: Item006 = (T6)value; break;
				case 7: Item007 = (T7)value; break;
				case 8: Item008 = (T8)value; break;
				case 9: Item009 = (T9)value; break;
				case 10: Item010 = (T10)value; break;
				case 11: Item011 = (T11)value; break;
				case 12: Item012 = (T12)value; break;
				case 13: Item013 = (T13)value; break;
				case 14: Item014 = (T14)value; break;
				case 15: Item015 = (T15)value; break;
				case 16: Item016 = (T16)value; break;
				case 17: Item017 = (T17)value; break;
				case 18: Item018 = (T18)value; break;
				case 19: Item019 = (T19)value; break;
				case 20: Item020 = (T20)value; break;
				case 21: Item021 = (T21)value; break;
				case 22: Item022 = (T22)value; break;
				case 23: Item023 = (T23)value; break;
				case 24: Item024 = (T24)value; break;
				case 25: Item025 = (T25)value; break;
				case 26: Item026 = (T26)value; break;
				case 27: Item027 = (T27)value; break;
				case 28: Item028 = (T28)value; break;
				case 29: Item029 = (T29)value; break;
				case 30: Item030 = (T30)value; break;
				case 31: Item031 = (T31)value; break;
				default: throw new ArgumentOutOfRangeException("index");
			}
		}

		/// <summary>この組オブジェクトが格納できる最大の要素数を取得します。</summary>
		public override int Capacity { get { return 32; } }
	}

	/// <summary>64 個の要素からなる変更可能な組を表します。</summary>
	/// <typeparam name="T0">組オブジェクトの 1 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T1">組オブジェクトの 2 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T2">組オブジェクトの 3 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T3">組オブジェクトの 4 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T4">組オブジェクトの 5 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T5">組オブジェクトの 6 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T6">組オブジェクトの 7 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T7">組オブジェクトの 8 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T8">組オブジェクトの 9 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T9">組オブジェクトの 10 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T10">組オブジェクトの 11 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T11">組オブジェクトの 12 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T12">組オブジェクトの 13 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T13">組オブジェクトの 14 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T14">組オブジェクトの 15 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T15">組オブジェクトの 16 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T16">組オブジェクトの 17 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T17">組オブジェクトの 18 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T18">組オブジェクトの 19 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T19">組オブジェクトの 20 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T20">組オブジェクトの 21 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T21">組オブジェクトの 22 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T22">組オブジェクトの 23 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T23">組オブジェクトの 24 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T24">組オブジェクトの 25 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T25">組オブジェクトの 26 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T26">組オブジェクトの 27 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T27">組オブジェクトの 28 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T28">組オブジェクトの 29 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T29">組オブジェクトの 30 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T30">組オブジェクトの 31 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T31">組オブジェクトの 32 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T32">組オブジェクトの 33 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T33">組オブジェクトの 34 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T34">組オブジェクトの 35 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T35">組オブジェクトの 36 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T36">組オブジェクトの 37 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T37">組オブジェクトの 38 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T38">組オブジェクトの 39 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T39">組オブジェクトの 40 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T40">組オブジェクトの 41 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T41">組オブジェクトの 42 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T42">組オブジェクトの 43 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T43">組オブジェクトの 44 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T44">組オブジェクトの 45 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T45">組オブジェクトの 46 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T46">組オブジェクトの 47 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T47">組オブジェクトの 48 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T48">組オブジェクトの 49 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T49">組オブジェクトの 50 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T50">組オブジェクトの 51 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T51">組オブジェクトの 52 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T52">組オブジェクトの 53 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T53">組オブジェクトの 54 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T54">組オブジェクトの 55 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T55">組オブジェクトの 56 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T56">組オブジェクトの 57 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T57">組オブジェクトの 58 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T58">組オブジェクトの 59 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T59">組オブジェクトの 60 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T60">組オブジェクトの 61 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T61">組オブジェクトの 62 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T62">組オブジェクトの 63 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T63">組オブジェクトの 64 番目の要素の型を指定します。</typeparam>
	[GeneratedCode("DLR", "2.0")]
	public class MutableTuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32, T33, T34, T35, T36, T37, T38, T39, T40, T41, T42, T43, T44, T45, T46, T47, T48, T49, T50, T51, T52, T53, T54, T55, T56, T57, T58, T59, T60, T61, T62, T63> : MutableTuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31>
	{
		/// <summary><see cref="Microsoft.Scripting.MutableTuple&lt;T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32, T33, T34, T35, T36, T37, T38, T39, T40, T41, T42, T43, T44, T45, T46, T47, T48, T49, T50, T51, T52, T53, T54, T55, T56, T57, T58, T59, T60, T61, T62, T63&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		public MutableTuple() { }

		/// <summary>要素を使用して、<see cref="Microsoft.Scripting.MutableTuple&lt;T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32, T33, T34, T35, T36, T37, T38, T39, T40, T41, T42, T43, T44, T45, T46, T47, T48, T49, T50, T51, T52, T53, T54, T55, T56, T57, T58, T59, T60, T61, T62, T63&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="item0">組オブジェクトの 1 番目の要素を指定します。</param>
		/// <param name="item1">組オブジェクトの 2 番目の要素を指定します。</param>
		/// <param name="item2">組オブジェクトの 3 番目の要素を指定します。</param>
		/// <param name="item3">組オブジェクトの 4 番目の要素を指定します。</param>
		/// <param name="item4">組オブジェクトの 5 番目の要素を指定します。</param>
		/// <param name="item5">組オブジェクトの 6 番目の要素を指定します。</param>
		/// <param name="item6">組オブジェクトの 7 番目の要素を指定します。</param>
		/// <param name="item7">組オブジェクトの 8 番目の要素を指定します。</param>
		/// <param name="item8">組オブジェクトの 9 番目の要素を指定します。</param>
		/// <param name="item9">組オブジェクトの 10 番目の要素を指定します。</param>
		/// <param name="item10">組オブジェクトの 11 番目の要素を指定します。</param>
		/// <param name="item11">組オブジェクトの 12 番目の要素を指定します。</param>
		/// <param name="item12">組オブジェクトの 13 番目の要素を指定します。</param>
		/// <param name="item13">組オブジェクトの 14 番目の要素を指定します。</param>
		/// <param name="item14">組オブジェクトの 15 番目の要素を指定します。</param>
		/// <param name="item15">組オブジェクトの 16 番目の要素を指定します。</param>
		/// <param name="item16">組オブジェクトの 17 番目の要素を指定します。</param>
		/// <param name="item17">組オブジェクトの 18 番目の要素を指定します。</param>
		/// <param name="item18">組オブジェクトの 19 番目の要素を指定します。</param>
		/// <param name="item19">組オブジェクトの 20 番目の要素を指定します。</param>
		/// <param name="item20">組オブジェクトの 21 番目の要素を指定します。</param>
		/// <param name="item21">組オブジェクトの 22 番目の要素を指定します。</param>
		/// <param name="item22">組オブジェクトの 23 番目の要素を指定します。</param>
		/// <param name="item23">組オブジェクトの 24 番目の要素を指定します。</param>
		/// <param name="item24">組オブジェクトの 25 番目の要素を指定します。</param>
		/// <param name="item25">組オブジェクトの 26 番目の要素を指定します。</param>
		/// <param name="item26">組オブジェクトの 27 番目の要素を指定します。</param>
		/// <param name="item27">組オブジェクトの 28 番目の要素を指定します。</param>
		/// <param name="item28">組オブジェクトの 29 番目の要素を指定します。</param>
		/// <param name="item29">組オブジェクトの 30 番目の要素を指定します。</param>
		/// <param name="item30">組オブジェクトの 31 番目の要素を指定します。</param>
		/// <param name="item31">組オブジェクトの 32 番目の要素を指定します。</param>
		/// <param name="item32">組オブジェクトの 33 番目の要素を指定します。</param>
		/// <param name="item33">組オブジェクトの 34 番目の要素を指定します。</param>
		/// <param name="item34">組オブジェクトの 35 番目の要素を指定します。</param>
		/// <param name="item35">組オブジェクトの 36 番目の要素を指定します。</param>
		/// <param name="item36">組オブジェクトの 37 番目の要素を指定します。</param>
		/// <param name="item37">組オブジェクトの 38 番目の要素を指定します。</param>
		/// <param name="item38">組オブジェクトの 39 番目の要素を指定します。</param>
		/// <param name="item39">組オブジェクトの 40 番目の要素を指定します。</param>
		/// <param name="item40">組オブジェクトの 41 番目の要素を指定します。</param>
		/// <param name="item41">組オブジェクトの 42 番目の要素を指定します。</param>
		/// <param name="item42">組オブジェクトの 43 番目の要素を指定します。</param>
		/// <param name="item43">組オブジェクトの 44 番目の要素を指定します。</param>
		/// <param name="item44">組オブジェクトの 45 番目の要素を指定します。</param>
		/// <param name="item45">組オブジェクトの 46 番目の要素を指定します。</param>
		/// <param name="item46">組オブジェクトの 47 番目の要素を指定します。</param>
		/// <param name="item47">組オブジェクトの 48 番目の要素を指定します。</param>
		/// <param name="item48">組オブジェクトの 49 番目の要素を指定します。</param>
		/// <param name="item49">組オブジェクトの 50 番目の要素を指定します。</param>
		/// <param name="item50">組オブジェクトの 51 番目の要素を指定します。</param>
		/// <param name="item51">組オブジェクトの 52 番目の要素を指定します。</param>
		/// <param name="item52">組オブジェクトの 53 番目の要素を指定します。</param>
		/// <param name="item53">組オブジェクトの 54 番目の要素を指定します。</param>
		/// <param name="item54">組オブジェクトの 55 番目の要素を指定します。</param>
		/// <param name="item55">組オブジェクトの 56 番目の要素を指定します。</param>
		/// <param name="item56">組オブジェクトの 57 番目の要素を指定します。</param>
		/// <param name="item57">組オブジェクトの 58 番目の要素を指定します。</param>
		/// <param name="item58">組オブジェクトの 59 番目の要素を指定します。</param>
		/// <param name="item59">組オブジェクトの 60 番目の要素を指定します。</param>
		/// <param name="item60">組オブジェクトの 61 番目の要素を指定します。</param>
		/// <param name="item61">組オブジェクトの 62 番目の要素を指定します。</param>
		/// <param name="item62">組オブジェクトの 63 番目の要素を指定します。</param>
		/// <param name="item63">組オブジェクトの 64 番目の要素を指定します。</param>
		public MutableTuple(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12, T13 item13, T14 item14, T15 item15, T16 item16, T17 item17, T18 item18, T19 item19, T20 item20, T21 item21, T22 item22, T23 item23, T24 item24, T25 item25, T26 item26, T27 item27, T28 item28, T29 item29, T30 item30, T31 item31, T32 item32, T33 item33, T34 item34, T35 item35, T36 item36, T37 item37, T38 item38, T39 item39, T40 item40, T41 item41, T42 item42, T43 item43, T44 item44, T45 item45, T46 item46, T47 item47, T48 item48, T49 item49, T50 item50, T51 item51, T52 item52, T53 item53, T54 item54, T55 item55, T56 item56, T57 item57, T58 item58, T59 item59, T60 item60, T61 item61, T62 item62, T63 item63)
			: base(item0, item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11, item12, item13, item14, item15, item16, item17, item18, item19, item20, item21, item22, item23, item24, item25, item26, item27, item28, item29, item30, item31)
		{
			Item032 = item32;
			Item033 = item33;
			Item034 = item34;
			Item035 = item35;
			Item036 = item36;
			Item037 = item37;
			Item038 = item38;
			Item039 = item39;
			Item040 = item40;
			Item041 = item41;
			Item042 = item42;
			Item043 = item43;
			Item044 = item44;
			Item045 = item45;
			Item046 = item46;
			Item047 = item47;
			Item048 = item48;
			Item049 = item49;
			Item050 = item50;
			Item051 = item51;
			Item052 = item52;
			Item053 = item53;
			Item054 = item54;
			Item055 = item55;
			Item056 = item56;
			Item057 = item57;
			Item058 = item58;
			Item059 = item59;
			Item060 = item60;
			Item061 = item61;
			Item062 = item62;
			Item063 = item63;
		}

		/// <summary>組オブジェクトの 33 番目の要素を取得または設定します。</summary>
		public T32 Item032 { get; set; }
		/// <summary>組オブジェクトの 34 番目の要素を取得または設定します。</summary>
		public T33 Item033 { get; set; }
		/// <summary>組オブジェクトの 35 番目の要素を取得または設定します。</summary>
		public T34 Item034 { get; set; }
		/// <summary>組オブジェクトの 36 番目の要素を取得または設定します。</summary>
		public T35 Item035 { get; set; }
		/// <summary>組オブジェクトの 37 番目の要素を取得または設定します。</summary>
		public T36 Item036 { get; set; }
		/// <summary>組オブジェクトの 38 番目の要素を取得または設定します。</summary>
		public T37 Item037 { get; set; }
		/// <summary>組オブジェクトの 39 番目の要素を取得または設定します。</summary>
		public T38 Item038 { get; set; }
		/// <summary>組オブジェクトの 40 番目の要素を取得または設定します。</summary>
		public T39 Item039 { get; set; }
		/// <summary>組オブジェクトの 41 番目の要素を取得または設定します。</summary>
		public T40 Item040 { get; set; }
		/// <summary>組オブジェクトの 42 番目の要素を取得または設定します。</summary>
		public T41 Item041 { get; set; }
		/// <summary>組オブジェクトの 43 番目の要素を取得または設定します。</summary>
		public T42 Item042 { get; set; }
		/// <summary>組オブジェクトの 44 番目の要素を取得または設定します。</summary>
		public T43 Item043 { get; set; }
		/// <summary>組オブジェクトの 45 番目の要素を取得または設定します。</summary>
		public T44 Item044 { get; set; }
		/// <summary>組オブジェクトの 46 番目の要素を取得または設定します。</summary>
		public T45 Item045 { get; set; }
		/// <summary>組オブジェクトの 47 番目の要素を取得または設定します。</summary>
		public T46 Item046 { get; set; }
		/// <summary>組オブジェクトの 48 番目の要素を取得または設定します。</summary>
		public T47 Item047 { get; set; }
		/// <summary>組オブジェクトの 49 番目の要素を取得または設定します。</summary>
		public T48 Item048 { get; set; }
		/// <summary>組オブジェクトの 50 番目の要素を取得または設定します。</summary>
		public T49 Item049 { get; set; }
		/// <summary>組オブジェクトの 51 番目の要素を取得または設定します。</summary>
		public T50 Item050 { get; set; }
		/// <summary>組オブジェクトの 52 番目の要素を取得または設定します。</summary>
		public T51 Item051 { get; set; }
		/// <summary>組オブジェクトの 53 番目の要素を取得または設定します。</summary>
		public T52 Item052 { get; set; }
		/// <summary>組オブジェクトの 54 番目の要素を取得または設定します。</summary>
		public T53 Item053 { get; set; }
		/// <summary>組オブジェクトの 55 番目の要素を取得または設定します。</summary>
		public T54 Item054 { get; set; }
		/// <summary>組オブジェクトの 56 番目の要素を取得または設定します。</summary>
		public T55 Item055 { get; set; }
		/// <summary>組オブジェクトの 57 番目の要素を取得または設定します。</summary>
		public T56 Item056 { get; set; }
		/// <summary>組オブジェクトの 58 番目の要素を取得または設定します。</summary>
		public T57 Item057 { get; set; }
		/// <summary>組オブジェクトの 59 番目の要素を取得または設定します。</summary>
		public T58 Item058 { get; set; }
		/// <summary>組オブジェクトの 60 番目の要素を取得または設定します。</summary>
		public T59 Item059 { get; set; }
		/// <summary>組オブジェクトの 61 番目の要素を取得または設定します。</summary>
		public T60 Item060 { get; set; }
		/// <summary>組オブジェクトの 62 番目の要素を取得または設定します。</summary>
		public T61 Item061 { get; set; }
		/// <summary>組オブジェクトの 63 番目の要素を取得または設定します。</summary>
		public T62 Item062 { get; set; }
		/// <summary>組オブジェクトの 64 番目の要素を取得または設定します。</summary>
		public T63 Item063 { get; set; }

		/// <summary>この組オブジェクトの指定されたインデックスにある値を取得します。</summary>
		/// <param name="index">値を取得するインデックスを指定します。</param>
		/// <returns>取得された値。</returns>
		public override object GetValue(int index)
		{
			switch (index)
			{
				case 0: return Item000;
				case 1: return Item001;
				case 2: return Item002;
				case 3: return Item003;
				case 4: return Item004;
				case 5: return Item005;
				case 6: return Item006;
				case 7: return Item007;
				case 8: return Item008;
				case 9: return Item009;
				case 10: return Item010;
				case 11: return Item011;
				case 12: return Item012;
				case 13: return Item013;
				case 14: return Item014;
				case 15: return Item015;
				case 16: return Item016;
				case 17: return Item017;
				case 18: return Item018;
				case 19: return Item019;
				case 20: return Item020;
				case 21: return Item021;
				case 22: return Item022;
				case 23: return Item023;
				case 24: return Item024;
				case 25: return Item025;
				case 26: return Item026;
				case 27: return Item027;
				case 28: return Item028;
				case 29: return Item029;
				case 30: return Item030;
				case 31: return Item031;
				case 32: return Item032;
				case 33: return Item033;
				case 34: return Item034;
				case 35: return Item035;
				case 36: return Item036;
				case 37: return Item037;
				case 38: return Item038;
				case 39: return Item039;
				case 40: return Item040;
				case 41: return Item041;
				case 42: return Item042;
				case 43: return Item043;
				case 44: return Item044;
				case 45: return Item045;
				case 46: return Item046;
				case 47: return Item047;
				case 48: return Item048;
				case 49: return Item049;
				case 50: return Item050;
				case 51: return Item051;
				case 52: return Item052;
				case 53: return Item053;
				case 54: return Item054;
				case 55: return Item055;
				case 56: return Item056;
				case 57: return Item057;
				case 58: return Item058;
				case 59: return Item059;
				case 60: return Item060;
				case 61: return Item061;
				case 62: return Item062;
				case 63: return Item063;
				default: throw new ArgumentOutOfRangeException("index");
			}
		}

		/// <summary>この組オブジェクトの指定されたインデックスに値を設定します。</summary>
		/// <param name="index">値を設定する場所を示すインデックスを指定します。</param>
		/// <param name="value">設定する値を指定します。</param>
		public override void SetValue(int index, object value)
		{
			switch (index)
			{
				case 0: Item000 = (T0)value; break;
				case 1: Item001 = (T1)value; break;
				case 2: Item002 = (T2)value; break;
				case 3: Item003 = (T3)value; break;
				case 4: Item004 = (T4)value; break;
				case 5: Item005 = (T5)value; break;
				case 6: Item006 = (T6)value; break;
				case 7: Item007 = (T7)value; break;
				case 8: Item008 = (T8)value; break;
				case 9: Item009 = (T9)value; break;
				case 10: Item010 = (T10)value; break;
				case 11: Item011 = (T11)value; break;
				case 12: Item012 = (T12)value; break;
				case 13: Item013 = (T13)value; break;
				case 14: Item014 = (T14)value; break;
				case 15: Item015 = (T15)value; break;
				case 16: Item016 = (T16)value; break;
				case 17: Item017 = (T17)value; break;
				case 18: Item018 = (T18)value; break;
				case 19: Item019 = (T19)value; break;
				case 20: Item020 = (T20)value; break;
				case 21: Item021 = (T21)value; break;
				case 22: Item022 = (T22)value; break;
				case 23: Item023 = (T23)value; break;
				case 24: Item024 = (T24)value; break;
				case 25: Item025 = (T25)value; break;
				case 26: Item026 = (T26)value; break;
				case 27: Item027 = (T27)value; break;
				case 28: Item028 = (T28)value; break;
				case 29: Item029 = (T29)value; break;
				case 30: Item030 = (T30)value; break;
				case 31: Item031 = (T31)value; break;
				case 32: Item032 = (T32)value; break;
				case 33: Item033 = (T33)value; break;
				case 34: Item034 = (T34)value; break;
				case 35: Item035 = (T35)value; break;
				case 36: Item036 = (T36)value; break;
				case 37: Item037 = (T37)value; break;
				case 38: Item038 = (T38)value; break;
				case 39: Item039 = (T39)value; break;
				case 40: Item040 = (T40)value; break;
				case 41: Item041 = (T41)value; break;
				case 42: Item042 = (T42)value; break;
				case 43: Item043 = (T43)value; break;
				case 44: Item044 = (T44)value; break;
				case 45: Item045 = (T45)value; break;
				case 46: Item046 = (T46)value; break;
				case 47: Item047 = (T47)value; break;
				case 48: Item048 = (T48)value; break;
				case 49: Item049 = (T49)value; break;
				case 50: Item050 = (T50)value; break;
				case 51: Item051 = (T51)value; break;
				case 52: Item052 = (T52)value; break;
				case 53: Item053 = (T53)value; break;
				case 54: Item054 = (T54)value; break;
				case 55: Item055 = (T55)value; break;
				case 56: Item056 = (T56)value; break;
				case 57: Item057 = (T57)value; break;
				case 58: Item058 = (T58)value; break;
				case 59: Item059 = (T59)value; break;
				case 60: Item060 = (T60)value; break;
				case 61: Item061 = (T61)value; break;
				case 62: Item062 = (T62)value; break;
				case 63: Item063 = (T63)value; break;
				default: throw new ArgumentOutOfRangeException("index");
			}
		}

		/// <summary>この組オブジェクトが格納できる最大の要素数を取得します。</summary>
		public override int Capacity { get { return 64; } }
	}

	/// <summary>128 個の要素からなる変更可能な組を表します。</summary>
	/// <typeparam name="T0">組オブジェクトの 1 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T1">組オブジェクトの 2 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T2">組オブジェクトの 3 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T3">組オブジェクトの 4 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T4">組オブジェクトの 5 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T5">組オブジェクトの 6 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T6">組オブジェクトの 7 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T7">組オブジェクトの 8 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T8">組オブジェクトの 9 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T9">組オブジェクトの 10 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T10">組オブジェクトの 11 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T11">組オブジェクトの 12 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T12">組オブジェクトの 13 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T13">組オブジェクトの 14 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T14">組オブジェクトの 15 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T15">組オブジェクトの 16 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T16">組オブジェクトの 17 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T17">組オブジェクトの 18 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T18">組オブジェクトの 19 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T19">組オブジェクトの 20 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T20">組オブジェクトの 21 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T21">組オブジェクトの 22 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T22">組オブジェクトの 23 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T23">組オブジェクトの 24 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T24">組オブジェクトの 25 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T25">組オブジェクトの 26 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T26">組オブジェクトの 27 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T27">組オブジェクトの 28 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T28">組オブジェクトの 29 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T29">組オブジェクトの 30 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T30">組オブジェクトの 31 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T31">組オブジェクトの 32 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T32">組オブジェクトの 33 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T33">組オブジェクトの 34 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T34">組オブジェクトの 35 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T35">組オブジェクトの 36 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T36">組オブジェクトの 37 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T37">組オブジェクトの 38 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T38">組オブジェクトの 39 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T39">組オブジェクトの 40 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T40">組オブジェクトの 41 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T41">組オブジェクトの 42 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T42">組オブジェクトの 43 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T43">組オブジェクトの 44 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T44">組オブジェクトの 45 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T45">組オブジェクトの 46 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T46">組オブジェクトの 47 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T47">組オブジェクトの 48 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T48">組オブジェクトの 49 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T49">組オブジェクトの 50 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T50">組オブジェクトの 51 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T51">組オブジェクトの 52 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T52">組オブジェクトの 53 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T53">組オブジェクトの 54 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T54">組オブジェクトの 55 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T55">組オブジェクトの 56 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T56">組オブジェクトの 57 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T57">組オブジェクトの 58 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T58">組オブジェクトの 59 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T59">組オブジェクトの 60 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T60">組オブジェクトの 61 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T61">組オブジェクトの 62 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T62">組オブジェクトの 63 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T63">組オブジェクトの 64 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T64">組オブジェクトの 65 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T65">組オブジェクトの 66 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T66">組オブジェクトの 67 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T67">組オブジェクトの 68 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T68">組オブジェクトの 69 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T69">組オブジェクトの 70 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T70">組オブジェクトの 71 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T71">組オブジェクトの 72 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T72">組オブジェクトの 73 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T73">組オブジェクトの 74 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T74">組オブジェクトの 75 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T75">組オブジェクトの 76 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T76">組オブジェクトの 77 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T77">組オブジェクトの 78 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T78">組オブジェクトの 79 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T79">組オブジェクトの 80 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T80">組オブジェクトの 81 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T81">組オブジェクトの 82 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T82">組オブジェクトの 83 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T83">組オブジェクトの 84 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T84">組オブジェクトの 85 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T85">組オブジェクトの 86 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T86">組オブジェクトの 87 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T87">組オブジェクトの 88 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T88">組オブジェクトの 89 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T89">組オブジェクトの 90 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T90">組オブジェクトの 91 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T91">組オブジェクトの 92 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T92">組オブジェクトの 93 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T93">組オブジェクトの 94 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T94">組オブジェクトの 95 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T95">組オブジェクトの 96 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T96">組オブジェクトの 97 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T97">組オブジェクトの 98 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T98">組オブジェクトの 99 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T99">組オブジェクトの 100 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T100">組オブジェクトの 101 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T101">組オブジェクトの 102 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T102">組オブジェクトの 103 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T103">組オブジェクトの 104 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T104">組オブジェクトの 105 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T105">組オブジェクトの 106 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T106">組オブジェクトの 107 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T107">組オブジェクトの 108 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T108">組オブジェクトの 109 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T109">組オブジェクトの 110 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T110">組オブジェクトの 111 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T111">組オブジェクトの 112 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T112">組オブジェクトの 113 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T113">組オブジェクトの 114 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T114">組オブジェクトの 115 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T115">組オブジェクトの 116 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T116">組オブジェクトの 117 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T117">組オブジェクトの 118 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T118">組オブジェクトの 119 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T119">組オブジェクトの 120 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T120">組オブジェクトの 121 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T121">組オブジェクトの 122 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T122">組オブジェクトの 123 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T123">組オブジェクトの 124 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T124">組オブジェクトの 125 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T125">組オブジェクトの 126 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T126">組オブジェクトの 127 番目の要素の型を指定します。</typeparam>
	/// <typeparam name="T127">組オブジェクトの 128 番目の要素の型を指定します。</typeparam>
	[GeneratedCode("DLR", "2.0")]
	public class MutableTuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32, T33, T34, T35, T36, T37, T38, T39, T40, T41, T42, T43, T44, T45, T46, T47, T48, T49, T50, T51, T52, T53, T54, T55, T56, T57, T58, T59, T60, T61, T62, T63, T64, T65, T66, T67, T68, T69, T70, T71, T72, T73, T74, T75, T76, T77, T78, T79, T80, T81, T82, T83, T84, T85, T86, T87, T88, T89, T90, T91, T92, T93, T94, T95, T96, T97, T98, T99, T100, T101, T102, T103, T104, T105, T106, T107, T108, T109, T110, T111, T112, T113, T114, T115, T116, T117, T118, T119, T120, T121, T122, T123, T124, T125, T126, T127> : MutableTuple<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32, T33, T34, T35, T36, T37, T38, T39, T40, T41, T42, T43, T44, T45, T46, T47, T48, T49, T50, T51, T52, T53, T54, T55, T56, T57, T58, T59, T60, T61, T62, T63>
	{
		/// <summary><see cref="Microsoft.Scripting.MutableTuple&lt;T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32, T33, T34, T35, T36, T37, T38, T39, T40, T41, T42, T43, T44, T45, T46, T47, T48, T49, T50, T51, T52, T53, T54, T55, T56, T57, T58, T59, T60, T61, T62, T63, T64, T65, T66, T67, T68, T69, T70, T71, T72, T73, T74, T75, T76, T77, T78, T79, T80, T81, T82, T83, T84, T85, T86, T87, T88, T89, T90, T91, T92, T93, T94, T95, T96, T97, T98, T99, T100, T101, T102, T103, T104, T105, T106, T107, T108, T109, T110, T111, T112, T113, T114, T115, T116, T117, T118, T119, T120, T121, T122, T123, T124, T125, T126, T127&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		public MutableTuple() { }

		/// <summary>要素を使用して、<see cref="Microsoft.Scripting.MutableTuple&lt;T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32, T33, T34, T35, T36, T37, T38, T39, T40, T41, T42, T43, T44, T45, T46, T47, T48, T49, T50, T51, T52, T53, T54, T55, T56, T57, T58, T59, T60, T61, T62, T63, T64, T65, T66, T67, T68, T69, T70, T71, T72, T73, T74, T75, T76, T77, T78, T79, T80, T81, T82, T83, T84, T85, T86, T87, T88, T89, T90, T91, T92, T93, T94, T95, T96, T97, T98, T99, T100, T101, T102, T103, T104, T105, T106, T107, T108, T109, T110, T111, T112, T113, T114, T115, T116, T117, T118, T119, T120, T121, T122, T123, T124, T125, T126, T127&gt;"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="item0">組オブジェクトの 1 番目の要素を指定します。</param>
		/// <param name="item1">組オブジェクトの 2 番目の要素を指定します。</param>
		/// <param name="item2">組オブジェクトの 3 番目の要素を指定します。</param>
		/// <param name="item3">組オブジェクトの 4 番目の要素を指定します。</param>
		/// <param name="item4">組オブジェクトの 5 番目の要素を指定します。</param>
		/// <param name="item5">組オブジェクトの 6 番目の要素を指定します。</param>
		/// <param name="item6">組オブジェクトの 7 番目の要素を指定します。</param>
		/// <param name="item7">組オブジェクトの 8 番目の要素を指定します。</param>
		/// <param name="item8">組オブジェクトの 9 番目の要素を指定します。</param>
		/// <param name="item9">組オブジェクトの 10 番目の要素を指定します。</param>
		/// <param name="item10">組オブジェクトの 11 番目の要素を指定します。</param>
		/// <param name="item11">組オブジェクトの 12 番目の要素を指定します。</param>
		/// <param name="item12">組オブジェクトの 13 番目の要素を指定します。</param>
		/// <param name="item13">組オブジェクトの 14 番目の要素を指定します。</param>
		/// <param name="item14">組オブジェクトの 15 番目の要素を指定します。</param>
		/// <param name="item15">組オブジェクトの 16 番目の要素を指定します。</param>
		/// <param name="item16">組オブジェクトの 17 番目の要素を指定します。</param>
		/// <param name="item17">組オブジェクトの 18 番目の要素を指定します。</param>
		/// <param name="item18">組オブジェクトの 19 番目の要素を指定します。</param>
		/// <param name="item19">組オブジェクトの 20 番目の要素を指定します。</param>
		/// <param name="item20">組オブジェクトの 21 番目の要素を指定します。</param>
		/// <param name="item21">組オブジェクトの 22 番目の要素を指定します。</param>
		/// <param name="item22">組オブジェクトの 23 番目の要素を指定します。</param>
		/// <param name="item23">組オブジェクトの 24 番目の要素を指定します。</param>
		/// <param name="item24">組オブジェクトの 25 番目の要素を指定します。</param>
		/// <param name="item25">組オブジェクトの 26 番目の要素を指定します。</param>
		/// <param name="item26">組オブジェクトの 27 番目の要素を指定します。</param>
		/// <param name="item27">組オブジェクトの 28 番目の要素を指定します。</param>
		/// <param name="item28">組オブジェクトの 29 番目の要素を指定します。</param>
		/// <param name="item29">組オブジェクトの 30 番目の要素を指定します。</param>
		/// <param name="item30">組オブジェクトの 31 番目の要素を指定します。</param>
		/// <param name="item31">組オブジェクトの 32 番目の要素を指定します。</param>
		/// <param name="item32">組オブジェクトの 33 番目の要素を指定します。</param>
		/// <param name="item33">組オブジェクトの 34 番目の要素を指定します。</param>
		/// <param name="item34">組オブジェクトの 35 番目の要素を指定します。</param>
		/// <param name="item35">組オブジェクトの 36 番目の要素を指定します。</param>
		/// <param name="item36">組オブジェクトの 37 番目の要素を指定します。</param>
		/// <param name="item37">組オブジェクトの 38 番目の要素を指定します。</param>
		/// <param name="item38">組オブジェクトの 39 番目の要素を指定します。</param>
		/// <param name="item39">組オブジェクトの 40 番目の要素を指定します。</param>
		/// <param name="item40">組オブジェクトの 41 番目の要素を指定します。</param>
		/// <param name="item41">組オブジェクトの 42 番目の要素を指定します。</param>
		/// <param name="item42">組オブジェクトの 43 番目の要素を指定します。</param>
		/// <param name="item43">組オブジェクトの 44 番目の要素を指定します。</param>
		/// <param name="item44">組オブジェクトの 45 番目の要素を指定します。</param>
		/// <param name="item45">組オブジェクトの 46 番目の要素を指定します。</param>
		/// <param name="item46">組オブジェクトの 47 番目の要素を指定します。</param>
		/// <param name="item47">組オブジェクトの 48 番目の要素を指定します。</param>
		/// <param name="item48">組オブジェクトの 49 番目の要素を指定します。</param>
		/// <param name="item49">組オブジェクトの 50 番目の要素を指定します。</param>
		/// <param name="item50">組オブジェクトの 51 番目の要素を指定します。</param>
		/// <param name="item51">組オブジェクトの 52 番目の要素を指定します。</param>
		/// <param name="item52">組オブジェクトの 53 番目の要素を指定します。</param>
		/// <param name="item53">組オブジェクトの 54 番目の要素を指定します。</param>
		/// <param name="item54">組オブジェクトの 55 番目の要素を指定します。</param>
		/// <param name="item55">組オブジェクトの 56 番目の要素を指定します。</param>
		/// <param name="item56">組オブジェクトの 57 番目の要素を指定します。</param>
		/// <param name="item57">組オブジェクトの 58 番目の要素を指定します。</param>
		/// <param name="item58">組オブジェクトの 59 番目の要素を指定します。</param>
		/// <param name="item59">組オブジェクトの 60 番目の要素を指定します。</param>
		/// <param name="item60">組オブジェクトの 61 番目の要素を指定します。</param>
		/// <param name="item61">組オブジェクトの 62 番目の要素を指定します。</param>
		/// <param name="item62">組オブジェクトの 63 番目の要素を指定します。</param>
		/// <param name="item63">組オブジェクトの 64 番目の要素を指定します。</param>
		/// <param name="item64">組オブジェクトの 65 番目の要素を指定します。</param>
		/// <param name="item65">組オブジェクトの 66 番目の要素を指定します。</param>
		/// <param name="item66">組オブジェクトの 67 番目の要素を指定します。</param>
		/// <param name="item67">組オブジェクトの 68 番目の要素を指定します。</param>
		/// <param name="item68">組オブジェクトの 69 番目の要素を指定します。</param>
		/// <param name="item69">組オブジェクトの 70 番目の要素を指定します。</param>
		/// <param name="item70">組オブジェクトの 71 番目の要素を指定します。</param>
		/// <param name="item71">組オブジェクトの 72 番目の要素を指定します。</param>
		/// <param name="item72">組オブジェクトの 73 番目の要素を指定します。</param>
		/// <param name="item73">組オブジェクトの 74 番目の要素を指定します。</param>
		/// <param name="item74">組オブジェクトの 75 番目の要素を指定します。</param>
		/// <param name="item75">組オブジェクトの 76 番目の要素を指定します。</param>
		/// <param name="item76">組オブジェクトの 77 番目の要素を指定します。</param>
		/// <param name="item77">組オブジェクトの 78 番目の要素を指定します。</param>
		/// <param name="item78">組オブジェクトの 79 番目の要素を指定します。</param>
		/// <param name="item79">組オブジェクトの 80 番目の要素を指定します。</param>
		/// <param name="item80">組オブジェクトの 81 番目の要素を指定します。</param>
		/// <param name="item81">組オブジェクトの 82 番目の要素を指定します。</param>
		/// <param name="item82">組オブジェクトの 83 番目の要素を指定します。</param>
		/// <param name="item83">組オブジェクトの 84 番目の要素を指定します。</param>
		/// <param name="item84">組オブジェクトの 85 番目の要素を指定します。</param>
		/// <param name="item85">組オブジェクトの 86 番目の要素を指定します。</param>
		/// <param name="item86">組オブジェクトの 87 番目の要素を指定します。</param>
		/// <param name="item87">組オブジェクトの 88 番目の要素を指定します。</param>
		/// <param name="item88">組オブジェクトの 89 番目の要素を指定します。</param>
		/// <param name="item89">組オブジェクトの 90 番目の要素を指定します。</param>
		/// <param name="item90">組オブジェクトの 91 番目の要素を指定します。</param>
		/// <param name="item91">組オブジェクトの 92 番目の要素を指定します。</param>
		/// <param name="item92">組オブジェクトの 93 番目の要素を指定します。</param>
		/// <param name="item93">組オブジェクトの 94 番目の要素を指定します。</param>
		/// <param name="item94">組オブジェクトの 95 番目の要素を指定します。</param>
		/// <param name="item95">組オブジェクトの 96 番目の要素を指定します。</param>
		/// <param name="item96">組オブジェクトの 97 番目の要素を指定します。</param>
		/// <param name="item97">組オブジェクトの 98 番目の要素を指定します。</param>
		/// <param name="item98">組オブジェクトの 99 番目の要素を指定します。</param>
		/// <param name="item99">組オブジェクトの 100 番目の要素を指定します。</param>
		/// <param name="item100">組オブジェクトの 101 番目の要素を指定します。</param>
		/// <param name="item101">組オブジェクトの 102 番目の要素を指定します。</param>
		/// <param name="item102">組オブジェクトの 103 番目の要素を指定します。</param>
		/// <param name="item103">組オブジェクトの 104 番目の要素を指定します。</param>
		/// <param name="item104">組オブジェクトの 105 番目の要素を指定します。</param>
		/// <param name="item105">組オブジェクトの 106 番目の要素を指定します。</param>
		/// <param name="item106">組オブジェクトの 107 番目の要素を指定します。</param>
		/// <param name="item107">組オブジェクトの 108 番目の要素を指定します。</param>
		/// <param name="item108">組オブジェクトの 109 番目の要素を指定します。</param>
		/// <param name="item109">組オブジェクトの 110 番目の要素を指定します。</param>
		/// <param name="item110">組オブジェクトの 111 番目の要素を指定します。</param>
		/// <param name="item111">組オブジェクトの 112 番目の要素を指定します。</param>
		/// <param name="item112">組オブジェクトの 113 番目の要素を指定します。</param>
		/// <param name="item113">組オブジェクトの 114 番目の要素を指定します。</param>
		/// <param name="item114">組オブジェクトの 115 番目の要素を指定します。</param>
		/// <param name="item115">組オブジェクトの 116 番目の要素を指定します。</param>
		/// <param name="item116">組オブジェクトの 117 番目の要素を指定します。</param>
		/// <param name="item117">組オブジェクトの 118 番目の要素を指定します。</param>
		/// <param name="item118">組オブジェクトの 119 番目の要素を指定します。</param>
		/// <param name="item119">組オブジェクトの 120 番目の要素を指定します。</param>
		/// <param name="item120">組オブジェクトの 121 番目の要素を指定します。</param>
		/// <param name="item121">組オブジェクトの 122 番目の要素を指定します。</param>
		/// <param name="item122">組オブジェクトの 123 番目の要素を指定します。</param>
		/// <param name="item123">組オブジェクトの 124 番目の要素を指定します。</param>
		/// <param name="item124">組オブジェクトの 125 番目の要素を指定します。</param>
		/// <param name="item125">組オブジェクトの 126 番目の要素を指定します。</param>
		/// <param name="item126">組オブジェクトの 127 番目の要素を指定します。</param>
		/// <param name="item127">組オブジェクトの 128 番目の要素を指定します。</param>
		public MutableTuple(T0 item0, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8, T9 item9, T10 item10, T11 item11, T12 item12, T13 item13, T14 item14, T15 item15, T16 item16, T17 item17, T18 item18, T19 item19, T20 item20, T21 item21, T22 item22, T23 item23, T24 item24, T25 item25, T26 item26, T27 item27, T28 item28, T29 item29, T30 item30, T31 item31, T32 item32, T33 item33, T34 item34, T35 item35, T36 item36, T37 item37, T38 item38, T39 item39, T40 item40, T41 item41, T42 item42, T43 item43, T44 item44, T45 item45, T46 item46, T47 item47, T48 item48, T49 item49, T50 item50, T51 item51, T52 item52, T53 item53, T54 item54, T55 item55, T56 item56, T57 item57, T58 item58, T59 item59, T60 item60, T61 item61, T62 item62, T63 item63, T64 item64, T65 item65, T66 item66, T67 item67, T68 item68, T69 item69, T70 item70, T71 item71, T72 item72, T73 item73, T74 item74, T75 item75, T76 item76, T77 item77, T78 item78, T79 item79, T80 item80, T81 item81, T82 item82, T83 item83, T84 item84, T85 item85, T86 item86, T87 item87, T88 item88, T89 item89, T90 item90, T91 item91, T92 item92, T93 item93, T94 item94, T95 item95, T96 item96, T97 item97, T98 item98, T99 item99, T100 item100, T101 item101, T102 item102, T103 item103, T104 item104, T105 item105, T106 item106, T107 item107, T108 item108, T109 item109, T110 item110, T111 item111, T112 item112, T113 item113, T114 item114, T115 item115, T116 item116, T117 item117, T118 item118, T119 item119, T120 item120, T121 item121, T122 item122, T123 item123, T124 item124, T125 item125, T126 item126, T127 item127)
			: base(item0, item1, item2, item3, item4, item5, item6, item7, item8, item9, item10, item11, item12, item13, item14, item15, item16, item17, item18, item19, item20, item21, item22, item23, item24, item25, item26, item27, item28, item29, item30, item31, item32, item33, item34, item35, item36, item37, item38, item39, item40, item41, item42, item43, item44, item45, item46, item47, item48, item49, item50, item51, item52, item53, item54, item55, item56, item57, item58, item59, item60, item61, item62, item63)
		{
			Item064 = item64;
			Item065 = item65;
			Item066 = item66;
			Item067 = item67;
			Item068 = item68;
			Item069 = item69;
			Item070 = item70;
			Item071 = item71;
			Item072 = item72;
			Item073 = item73;
			Item074 = item74;
			Item075 = item75;
			Item076 = item76;
			Item077 = item77;
			Item078 = item78;
			Item079 = item79;
			Item080 = item80;
			Item081 = item81;
			Item082 = item82;
			Item083 = item83;
			Item084 = item84;
			Item085 = item85;
			Item086 = item86;
			Item087 = item87;
			Item088 = item88;
			Item089 = item89;
			Item090 = item90;
			Item091 = item91;
			Item092 = item92;
			Item093 = item93;
			Item094 = item94;
			Item095 = item95;
			Item096 = item96;
			Item097 = item97;
			Item098 = item98;
			Item099 = item99;
			Item100 = item100;
			Item101 = item101;
			Item102 = item102;
			Item103 = item103;
			Item104 = item104;
			Item105 = item105;
			Item106 = item106;
			Item107 = item107;
			Item108 = item108;
			Item109 = item109;
			Item110 = item110;
			Item111 = item111;
			Item112 = item112;
			Item113 = item113;
			Item114 = item114;
			Item115 = item115;
			Item116 = item116;
			Item117 = item117;
			Item118 = item118;
			Item119 = item119;
			Item120 = item120;
			Item121 = item121;
			Item122 = item122;
			Item123 = item123;
			Item124 = item124;
			Item125 = item125;
			Item126 = item126;
			Item127 = item127;
		}

		/// <summary>組オブジェクトの 65 番目の要素を取得または設定します。</summary>
		public T64 Item064 { get; set; }
		/// <summary>組オブジェクトの 66 番目の要素を取得または設定します。</summary>
		public T65 Item065 { get; set; }
		/// <summary>組オブジェクトの 67 番目の要素を取得または設定します。</summary>
		public T66 Item066 { get; set; }
		/// <summary>組オブジェクトの 68 番目の要素を取得または設定します。</summary>
		public T67 Item067 { get; set; }
		/// <summary>組オブジェクトの 69 番目の要素を取得または設定します。</summary>
		public T68 Item068 { get; set; }
		/// <summary>組オブジェクトの 70 番目の要素を取得または設定します。</summary>
		public T69 Item069 { get; set; }
		/// <summary>組オブジェクトの 71 番目の要素を取得または設定します。</summary>
		public T70 Item070 { get; set; }
		/// <summary>組オブジェクトの 72 番目の要素を取得または設定します。</summary>
		public T71 Item071 { get; set; }
		/// <summary>組オブジェクトの 73 番目の要素を取得または設定します。</summary>
		public T72 Item072 { get; set; }
		/// <summary>組オブジェクトの 74 番目の要素を取得または設定します。</summary>
		public T73 Item073 { get; set; }
		/// <summary>組オブジェクトの 75 番目の要素を取得または設定します。</summary>
		public T74 Item074 { get; set; }
		/// <summary>組オブジェクトの 76 番目の要素を取得または設定します。</summary>
		public T75 Item075 { get; set; }
		/// <summary>組オブジェクトの 77 番目の要素を取得または設定します。</summary>
		public T76 Item076 { get; set; }
		/// <summary>組オブジェクトの 78 番目の要素を取得または設定します。</summary>
		public T77 Item077 { get; set; }
		/// <summary>組オブジェクトの 79 番目の要素を取得または設定します。</summary>
		public T78 Item078 { get; set; }
		/// <summary>組オブジェクトの 80 番目の要素を取得または設定します。</summary>
		public T79 Item079 { get; set; }
		/// <summary>組オブジェクトの 81 番目の要素を取得または設定します。</summary>
		public T80 Item080 { get; set; }
		/// <summary>組オブジェクトの 82 番目の要素を取得または設定します。</summary>
		public T81 Item081 { get; set; }
		/// <summary>組オブジェクトの 83 番目の要素を取得または設定します。</summary>
		public T82 Item082 { get; set; }
		/// <summary>組オブジェクトの 84 番目の要素を取得または設定します。</summary>
		public T83 Item083 { get; set; }
		/// <summary>組オブジェクトの 85 番目の要素を取得または設定します。</summary>
		public T84 Item084 { get; set; }
		/// <summary>組オブジェクトの 86 番目の要素を取得または設定します。</summary>
		public T85 Item085 { get; set; }
		/// <summary>組オブジェクトの 87 番目の要素を取得または設定します。</summary>
		public T86 Item086 { get; set; }
		/// <summary>組オブジェクトの 88 番目の要素を取得または設定します。</summary>
		public T87 Item087 { get; set; }
		/// <summary>組オブジェクトの 89 番目の要素を取得または設定します。</summary>
		public T88 Item088 { get; set; }
		/// <summary>組オブジェクトの 90 番目の要素を取得または設定します。</summary>
		public T89 Item089 { get; set; }
		/// <summary>組オブジェクトの 91 番目の要素を取得または設定します。</summary>
		public T90 Item090 { get; set; }
		/// <summary>組オブジェクトの 92 番目の要素を取得または設定します。</summary>
		public T91 Item091 { get; set; }
		/// <summary>組オブジェクトの 93 番目の要素を取得または設定します。</summary>
		public T92 Item092 { get; set; }
		/// <summary>組オブジェクトの 94 番目の要素を取得または設定します。</summary>
		public T93 Item093 { get; set; }
		/// <summary>組オブジェクトの 95 番目の要素を取得または設定します。</summary>
		public T94 Item094 { get; set; }
		/// <summary>組オブジェクトの 96 番目の要素を取得または設定します。</summary>
		public T95 Item095 { get; set; }
		/// <summary>組オブジェクトの 97 番目の要素を取得または設定します。</summary>
		public T96 Item096 { get; set; }
		/// <summary>組オブジェクトの 98 番目の要素を取得または設定します。</summary>
		public T97 Item097 { get; set; }
		/// <summary>組オブジェクトの 99 番目の要素を取得または設定します。</summary>
		public T98 Item098 { get; set; }
		/// <summary>組オブジェクトの 100 番目の要素を取得または設定します。</summary>
		public T99 Item099 { get; set; }
		/// <summary>組オブジェクトの 101 番目の要素を取得または設定します。</summary>
		public T100 Item100 { get; set; }
		/// <summary>組オブジェクトの 102 番目の要素を取得または設定します。</summary>
		public T101 Item101 { get; set; }
		/// <summary>組オブジェクトの 103 番目の要素を取得または設定します。</summary>
		public T102 Item102 { get; set; }
		/// <summary>組オブジェクトの 104 番目の要素を取得または設定します。</summary>
		public T103 Item103 { get; set; }
		/// <summary>組オブジェクトの 105 番目の要素を取得または設定します。</summary>
		public T104 Item104 { get; set; }
		/// <summary>組オブジェクトの 106 番目の要素を取得または設定します。</summary>
		public T105 Item105 { get; set; }
		/// <summary>組オブジェクトの 107 番目の要素を取得または設定します。</summary>
		public T106 Item106 { get; set; }
		/// <summary>組オブジェクトの 108 番目の要素を取得または設定します。</summary>
		public T107 Item107 { get; set; }
		/// <summary>組オブジェクトの 109 番目の要素を取得または設定します。</summary>
		public T108 Item108 { get; set; }
		/// <summary>組オブジェクトの 110 番目の要素を取得または設定します。</summary>
		public T109 Item109 { get; set; }
		/// <summary>組オブジェクトの 111 番目の要素を取得または設定します。</summary>
		public T110 Item110 { get; set; }
		/// <summary>組オブジェクトの 112 番目の要素を取得または設定します。</summary>
		public T111 Item111 { get; set; }
		/// <summary>組オブジェクトの 113 番目の要素を取得または設定します。</summary>
		public T112 Item112 { get; set; }
		/// <summary>組オブジェクトの 114 番目の要素を取得または設定します。</summary>
		public T113 Item113 { get; set; }
		/// <summary>組オブジェクトの 115 番目の要素を取得または設定します。</summary>
		public T114 Item114 { get; set; }
		/// <summary>組オブジェクトの 116 番目の要素を取得または設定します。</summary>
		public T115 Item115 { get; set; }
		/// <summary>組オブジェクトの 117 番目の要素を取得または設定します。</summary>
		public T116 Item116 { get; set; }
		/// <summary>組オブジェクトの 118 番目の要素を取得または設定します。</summary>
		public T117 Item117 { get; set; }
		/// <summary>組オブジェクトの 119 番目の要素を取得または設定します。</summary>
		public T118 Item118 { get; set; }
		/// <summary>組オブジェクトの 120 番目の要素を取得または設定します。</summary>
		public T119 Item119 { get; set; }
		/// <summary>組オブジェクトの 121 番目の要素を取得または設定します。</summary>
		public T120 Item120 { get; set; }
		/// <summary>組オブジェクトの 122 番目の要素を取得または設定します。</summary>
		public T121 Item121 { get; set; }
		/// <summary>組オブジェクトの 123 番目の要素を取得または設定します。</summary>
		public T122 Item122 { get; set; }
		/// <summary>組オブジェクトの 124 番目の要素を取得または設定します。</summary>
		public T123 Item123 { get; set; }
		/// <summary>組オブジェクトの 125 番目の要素を取得または設定します。</summary>
		public T124 Item124 { get; set; }
		/// <summary>組オブジェクトの 126 番目の要素を取得または設定します。</summary>
		public T125 Item125 { get; set; }
		/// <summary>組オブジェクトの 127 番目の要素を取得または設定します。</summary>
		public T126 Item126 { get; set; }
		/// <summary>組オブジェクトの 128 番目の要素を取得または設定します。</summary>
		public T127 Item127 { get; set; }

		/// <summary>この組オブジェクトの指定されたインデックスにある値を取得します。</summary>
		/// <param name="index">値を取得するインデックスを指定します。</param>
		/// <returns>取得された値。</returns>
		public override object GetValue(int index)
		{
			switch (index)
			{
				case 0: return Item000;
				case 1: return Item001;
				case 2: return Item002;
				case 3: return Item003;
				case 4: return Item004;
				case 5: return Item005;
				case 6: return Item006;
				case 7: return Item007;
				case 8: return Item008;
				case 9: return Item009;
				case 10: return Item010;
				case 11: return Item011;
				case 12: return Item012;
				case 13: return Item013;
				case 14: return Item014;
				case 15: return Item015;
				case 16: return Item016;
				case 17: return Item017;
				case 18: return Item018;
				case 19: return Item019;
				case 20: return Item020;
				case 21: return Item021;
				case 22: return Item022;
				case 23: return Item023;
				case 24: return Item024;
				case 25: return Item025;
				case 26: return Item026;
				case 27: return Item027;
				case 28: return Item028;
				case 29: return Item029;
				case 30: return Item030;
				case 31: return Item031;
				case 32: return Item032;
				case 33: return Item033;
				case 34: return Item034;
				case 35: return Item035;
				case 36: return Item036;
				case 37: return Item037;
				case 38: return Item038;
				case 39: return Item039;
				case 40: return Item040;
				case 41: return Item041;
				case 42: return Item042;
				case 43: return Item043;
				case 44: return Item044;
				case 45: return Item045;
				case 46: return Item046;
				case 47: return Item047;
				case 48: return Item048;
				case 49: return Item049;
				case 50: return Item050;
				case 51: return Item051;
				case 52: return Item052;
				case 53: return Item053;
				case 54: return Item054;
				case 55: return Item055;
				case 56: return Item056;
				case 57: return Item057;
				case 58: return Item058;
				case 59: return Item059;
				case 60: return Item060;
				case 61: return Item061;
				case 62: return Item062;
				case 63: return Item063;
				case 64: return Item064;
				case 65: return Item065;
				case 66: return Item066;
				case 67: return Item067;
				case 68: return Item068;
				case 69: return Item069;
				case 70: return Item070;
				case 71: return Item071;
				case 72: return Item072;
				case 73: return Item073;
				case 74: return Item074;
				case 75: return Item075;
				case 76: return Item076;
				case 77: return Item077;
				case 78: return Item078;
				case 79: return Item079;
				case 80: return Item080;
				case 81: return Item081;
				case 82: return Item082;
				case 83: return Item083;
				case 84: return Item084;
				case 85: return Item085;
				case 86: return Item086;
				case 87: return Item087;
				case 88: return Item088;
				case 89: return Item089;
				case 90: return Item090;
				case 91: return Item091;
				case 92: return Item092;
				case 93: return Item093;
				case 94: return Item094;
				case 95: return Item095;
				case 96: return Item096;
				case 97: return Item097;
				case 98: return Item098;
				case 99: return Item099;
				case 100: return Item100;
				case 101: return Item101;
				case 102: return Item102;
				case 103: return Item103;
				case 104: return Item104;
				case 105: return Item105;
				case 106: return Item106;
				case 107: return Item107;
				case 108: return Item108;
				case 109: return Item109;
				case 110: return Item110;
				case 111: return Item111;
				case 112: return Item112;
				case 113: return Item113;
				case 114: return Item114;
				case 115: return Item115;
				case 116: return Item116;
				case 117: return Item117;
				case 118: return Item118;
				case 119: return Item119;
				case 120: return Item120;
				case 121: return Item121;
				case 122: return Item122;
				case 123: return Item123;
				case 124: return Item124;
				case 125: return Item125;
				case 126: return Item126;
				case 127: return Item127;
				default: throw new ArgumentOutOfRangeException("index");
			}
		}

		/// <summary>この組オブジェクトの指定されたインデックスに値を設定します。</summary>
		/// <param name="index">値を設定する場所を示すインデックスを指定します。</param>
		/// <param name="value">設定する値を指定します。</param>
		public override void SetValue(int index, object value)
		{
			switch (index)
			{
				case 0: Item000 = (T0)value; break;
				case 1: Item001 = (T1)value; break;
				case 2: Item002 = (T2)value; break;
				case 3: Item003 = (T3)value; break;
				case 4: Item004 = (T4)value; break;
				case 5: Item005 = (T5)value; break;
				case 6: Item006 = (T6)value; break;
				case 7: Item007 = (T7)value; break;
				case 8: Item008 = (T8)value; break;
				case 9: Item009 = (T9)value; break;
				case 10: Item010 = (T10)value; break;
				case 11: Item011 = (T11)value; break;
				case 12: Item012 = (T12)value; break;
				case 13: Item013 = (T13)value; break;
				case 14: Item014 = (T14)value; break;
				case 15: Item015 = (T15)value; break;
				case 16: Item016 = (T16)value; break;
				case 17: Item017 = (T17)value; break;
				case 18: Item018 = (T18)value; break;
				case 19: Item019 = (T19)value; break;
				case 20: Item020 = (T20)value; break;
				case 21: Item021 = (T21)value; break;
				case 22: Item022 = (T22)value; break;
				case 23: Item023 = (T23)value; break;
				case 24: Item024 = (T24)value; break;
				case 25: Item025 = (T25)value; break;
				case 26: Item026 = (T26)value; break;
				case 27: Item027 = (T27)value; break;
				case 28: Item028 = (T28)value; break;
				case 29: Item029 = (T29)value; break;
				case 30: Item030 = (T30)value; break;
				case 31: Item031 = (T31)value; break;
				case 32: Item032 = (T32)value; break;
				case 33: Item033 = (T33)value; break;
				case 34: Item034 = (T34)value; break;
				case 35: Item035 = (T35)value; break;
				case 36: Item036 = (T36)value; break;
				case 37: Item037 = (T37)value; break;
				case 38: Item038 = (T38)value; break;
				case 39: Item039 = (T39)value; break;
				case 40: Item040 = (T40)value; break;
				case 41: Item041 = (T41)value; break;
				case 42: Item042 = (T42)value; break;
				case 43: Item043 = (T43)value; break;
				case 44: Item044 = (T44)value; break;
				case 45: Item045 = (T45)value; break;
				case 46: Item046 = (T46)value; break;
				case 47: Item047 = (T47)value; break;
				case 48: Item048 = (T48)value; break;
				case 49: Item049 = (T49)value; break;
				case 50: Item050 = (T50)value; break;
				case 51: Item051 = (T51)value; break;
				case 52: Item052 = (T52)value; break;
				case 53: Item053 = (T53)value; break;
				case 54: Item054 = (T54)value; break;
				case 55: Item055 = (T55)value; break;
				case 56: Item056 = (T56)value; break;
				case 57: Item057 = (T57)value; break;
				case 58: Item058 = (T58)value; break;
				case 59: Item059 = (T59)value; break;
				case 60: Item060 = (T60)value; break;
				case 61: Item061 = (T61)value; break;
				case 62: Item062 = (T62)value; break;
				case 63: Item063 = (T63)value; break;
				case 64: Item064 = (T64)value; break;
				case 65: Item065 = (T65)value; break;
				case 66: Item066 = (T66)value; break;
				case 67: Item067 = (T67)value; break;
				case 68: Item068 = (T68)value; break;
				case 69: Item069 = (T69)value; break;
				case 70: Item070 = (T70)value; break;
				case 71: Item071 = (T71)value; break;
				case 72: Item072 = (T72)value; break;
				case 73: Item073 = (T73)value; break;
				case 74: Item074 = (T74)value; break;
				case 75: Item075 = (T75)value; break;
				case 76: Item076 = (T76)value; break;
				case 77: Item077 = (T77)value; break;
				case 78: Item078 = (T78)value; break;
				case 79: Item079 = (T79)value; break;
				case 80: Item080 = (T80)value; break;
				case 81: Item081 = (T81)value; break;
				case 82: Item082 = (T82)value; break;
				case 83: Item083 = (T83)value; break;
				case 84: Item084 = (T84)value; break;
				case 85: Item085 = (T85)value; break;
				case 86: Item086 = (T86)value; break;
				case 87: Item087 = (T87)value; break;
				case 88: Item088 = (T88)value; break;
				case 89: Item089 = (T89)value; break;
				case 90: Item090 = (T90)value; break;
				case 91: Item091 = (T91)value; break;
				case 92: Item092 = (T92)value; break;
				case 93: Item093 = (T93)value; break;
				case 94: Item094 = (T94)value; break;
				case 95: Item095 = (T95)value; break;
				case 96: Item096 = (T96)value; break;
				case 97: Item097 = (T97)value; break;
				case 98: Item098 = (T98)value; break;
				case 99: Item099 = (T99)value; break;
				case 100: Item100 = (T100)value; break;
				case 101: Item101 = (T101)value; break;
				case 102: Item102 = (T102)value; break;
				case 103: Item103 = (T103)value; break;
				case 104: Item104 = (T104)value; break;
				case 105: Item105 = (T105)value; break;
				case 106: Item106 = (T106)value; break;
				case 107: Item107 = (T107)value; break;
				case 108: Item108 = (T108)value; break;
				case 109: Item109 = (T109)value; break;
				case 110: Item110 = (T110)value; break;
				case 111: Item111 = (T111)value; break;
				case 112: Item112 = (T112)value; break;
				case 113: Item113 = (T113)value; break;
				case 114: Item114 = (T114)value; break;
				case 115: Item115 = (T115)value; break;
				case 116: Item116 = (T116)value; break;
				case 117: Item117 = (T117)value; break;
				case 118: Item118 = (T118)value; break;
				case 119: Item119 = (T119)value; break;
				case 120: Item120 = (T120)value; break;
				case 121: Item121 = (T121)value; break;
				case 122: Item122 = (T122)value; break;
				case 123: Item123 = (T123)value; break;
				case 124: Item124 = (T124)value; break;
				case 125: Item125 = (T125)value; break;
				case 126: Item126 = (T126)value; break;
				case 127: Item127 = (T127)value; break;
				default: throw new ArgumentOutOfRangeException("index");
			}
		}

		/// <summary>この組オブジェクトが格納できる最大の要素数を取得します。</summary>
		public override int Capacity { get { return 128; } }
	}
}
