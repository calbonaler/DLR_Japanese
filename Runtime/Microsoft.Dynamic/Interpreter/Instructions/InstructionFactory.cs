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
using Microsoft.Scripting.Numerics;
using BigInt = System.Numerics.BigInteger;

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>型に対応する命令の作成を支援します。</summary>
	public abstract class InstructionFactory
	{
		// TODO: weak table for types in a collectible assembly?
		static Dictionary<Type, InstructionFactory> _factories;

		/// <summary>指定された型に基づく命令を作成できる <see cref="InstructionFactory"/> を返します。</summary>
		/// <param name="type">命令に使用する型を指定します。</param>
		/// <returns>型に基づく命令を作成できる <see cref="InstructionFactory"/>。</returns>
		internal static InstructionFactory GetFactory(Type type)
		{
			if (_factories == null)
			{
				_factories = new Dictionary<Type, InstructionFactory>() {
                    { typeof(object), InstructionFactory<object>.Factory },
                    { typeof(bool), InstructionFactory<bool>.Factory },
                    { typeof(byte), InstructionFactory<byte>.Factory },
                    { typeof(sbyte), InstructionFactory<sbyte>.Factory },
                    { typeof(short), InstructionFactory<short>.Factory },
                    { typeof(ushort), InstructionFactory<ushort>.Factory },
                    { typeof(int), InstructionFactory<int>.Factory },
                    { typeof(uint), InstructionFactory<uint>.Factory },
                    { typeof(long), InstructionFactory<long>.Factory },
                    { typeof(ulong), InstructionFactory<ulong>.Factory },
                    { typeof(float), InstructionFactory<float>.Factory },
                    { typeof(double), InstructionFactory<double>.Factory },
                    { typeof(char), InstructionFactory<char>.Factory },
                    { typeof(string), InstructionFactory<string>.Factory },
                    { typeof(BigInt), InstructionFactory<BigInt>.Factory },
                    { typeof(BigInteger), InstructionFactory<BigInteger>.Factory },
                    { typeof(SymbolId), InstructionFactory<SymbolId>.Factory },     
                };
			}
			lock (_factories)
			{
				InstructionFactory factory;
				if (!_factories.TryGetValue(type, out factory))
					_factories[type] = factory = (InstructionFactory)typeof(InstructionFactory<>).MakeGenericType(type).GetField("Factory").GetValue(null);
				return factory;
			}
		}

		/// <summary>配列の要素を取得する命令を作成します。</summary>
		/// <returns>作成された命令を表す <see cref="Instruction"/>。</returns>
		protected internal abstract Instruction GetArrayItem();

		/// <summary>配列の要素を設定する命令を作成します。</summary>
		/// <returns>作成された命令を表す <see cref="Instruction"/>。</returns>
		protected internal abstract Instruction SetArrayItem();

		/// <summary>オブジェクトが指定された型に変換可能であるかどうかを判断する命令を作成します。</summary>
		/// <returns>作成された命令を表す <see cref="Instruction"/>。</returns>
		protected internal abstract Instruction TypeIs();

		/// <summary>オブジェクトの指定された型への変換を試み、失敗した場合は <c>null</c> を返す命令を作成します。</summary>
		/// <returns>作成された命令を表す <see cref="Instruction"/>。</returns>
		protected internal abstract Instruction TypeAs();

		/// <summary>型の既定値を取得する命令を作成します。</summary>
		/// <returns>作成された命令を表す <see cref="Instruction"/>。</returns>
		protected internal abstract Instruction DefaultValue();

		/// <summary>指定された長さをもつ配列を作成する命令を作成します。</summary>
		/// <returns>作成された命令を表す <see cref="Instruction"/>。</returns>
		protected internal abstract Instruction NewArray();

		/// <summary>指定された数の要素によって新しく作成された配列を初期化する命令を作成します。</summary>
		/// <param name="elementCount">配列の初期化に使用する要素の数を指定します。</param>
		/// <returns>作成された命令を表す <see cref="Instruction"/>。</returns>
		protected internal abstract Instruction NewArrayInit(int elementCount);
	}

	/// <summary>型に対応する命令の作成を支援します。<see cref="InstructionFactory"/> クラスの既定の実装を提供します。</summary>
	/// <typeparam name="T">命令に使用される型を指定します。</typeparam>
	public sealed class InstructionFactory<T> : InstructionFactory
	{
		/// <summary>このクラスのインスタンスを示します。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly InstructionFactory Factory = new InstructionFactory<T>();

		Instruction _getArrayItem;
		Instruction _setArrayItem;
		Instruction _typeIs;
		Instruction _defaultValue;
		Instruction _newArray;
		Instruction _typeAs;

		InstructionFactory() { }

		/// <summary>配列の要素を取得する命令を作成します。</summary>
		/// <returns>作成された命令を表す <see cref="Instruction"/>。</returns>
		protected internal override Instruction GetArrayItem() { return _getArrayItem ?? (_getArrayItem = new GetArrayItemInstruction<T>()); }

		/// <summary>配列の要素を設定する命令を作成します。</summary>
		/// <returns>作成された命令を表す <see cref="Instruction"/>。</returns>
		protected internal override Instruction SetArrayItem() { return _setArrayItem ?? (_setArrayItem = new SetArrayItemInstruction<T>()); }

		/// <summary>オブジェクトが指定された型に変換可能であるかどうかを判断する命令を作成します。</summary>
		/// <returns>作成された命令を表す <see cref="Instruction"/>。</returns>
		protected internal override Instruction TypeIs() { return _typeIs ?? (_typeIs = new TypeIsInstruction<T>()); }

		/// <summary>オブジェクトの指定された型への変換を試み、失敗した場合は <c>null</c> を返す命令を作成します。</summary>
		/// <returns>作成された命令を表す <see cref="Instruction"/>。</returns>
		protected internal override Instruction TypeAs() { return _typeAs ?? (_typeAs = new TypeAsInstruction<T>()); }

		/// <summary>型の既定値を取得する命令を作成します。</summary>
		/// <returns>作成された命令を表す <see cref="Instruction"/>。</returns>
		protected internal override Instruction DefaultValue() { return _defaultValue ?? (_defaultValue = new DefaultValueInstruction<T>()); }

		/// <summary>指定された長さをもつ配列を作成する命令を作成します。</summary>
		/// <returns>作成された命令を表す <see cref="Instruction"/>。</returns>
		protected internal override Instruction NewArray() { return _newArray ?? (_newArray = new NewArrayInstruction<T>()); }

		/// <summary>指定された数の要素によって新しく作成された配列を初期化する命令を作成します。</summary>
		/// <param name="elementCount">配列の初期化に使用する要素の数を指定します。</param>
		/// <returns>作成された命令を表す <see cref="Instruction"/>。</returns>
		protected internal override Instruction NewArrayInit(int elementCount) { return new NewArrayInitInstruction<T>(elementCount); }
	}
}
