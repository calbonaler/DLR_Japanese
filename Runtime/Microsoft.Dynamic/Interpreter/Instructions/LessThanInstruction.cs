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
using System.Diagnostics;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>数値型同士の小なり比較命令を表します。</summary>
	public abstract class LessThanInstruction : Instruction
	{
		static Instruction _SByte, _Int16, _Char, _Int32, _Int64, _Byte, _UInt16, _UInt32, _UInt64, _Single, _Double;

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return 2; } }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		LessThanInstruction() { }

		sealed class LessThanSByte : LessThanInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var right = (sbyte)frame.Pop();
				frame.Push(((sbyte)frame.Pop()) < right);
				return +1;
			}
		}

		sealed class LessThanInt16 : LessThanInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var right = (short)frame.Pop();
				frame.Push(((short)frame.Pop()) < right);
				return +1;
			}
		}

		sealed class LessThanChar : LessThanInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var right = (char)frame.Pop();
				frame.Push(((char)frame.Pop()) < right);
				return +1;
			}
		}

		sealed class LessThanInt32 : LessThanInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var right = (int)frame.Pop();
				frame.Push(((int)frame.Pop()) < right);
				return +1;
			}
		}

		sealed class LessThanInt64 : LessThanInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var right = (long)frame.Pop();
				frame.Push(((long)frame.Pop()) < right);
				return +1;
			}
		}

		sealed class LessThanByte : LessThanInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var right = (byte)frame.Pop();
				frame.Push(((byte)frame.Pop()) < right);
				return +1;
			}
		}

		sealed class LessThanUInt16 : LessThanInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var right = (ushort)frame.Pop();
				frame.Push(((ushort)frame.Pop()) < right);
				return +1;
			}
		}

		sealed class LessThanUInt32 : LessThanInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var right = (uint)frame.Pop();
				frame.Push(((uint)frame.Pop()) < right);
				return +1;
			}
		}

		sealed class LessThanUInt64 : LessThanInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var right = (ulong)frame.Pop();
				frame.Push(((ulong)frame.Pop()) < right);
				return +1;
			}
		}

		sealed class LessThanSingle : LessThanInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var right = (float)frame.Pop();
				frame.Push(((float)frame.Pop()) < right);
				return +1;
			}
		}

		sealed class LessThanDouble : LessThanInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var right = (double)frame.Pop();
				frame.Push(((double)frame.Pop()) < right);
				return +1;
			}
		}

		/// <summary>指定された数値型に対する適切な小なり比較命令を作成します。</summary>
		/// <param name="type">比較対象の数値の型を指定します。</param>
		/// <returns>数値型に対する適切な小なり比較命令。</returns>
		public static Instruction Create(Type type)
		{
			Debug.Assert(!type.IsEnum);
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.SByte: return _SByte ?? (_SByte = new LessThanSByte());
				case TypeCode.Byte: return _Byte ?? (_Byte = new LessThanByte());
				case TypeCode.Char: return _Char ?? (_Char = new LessThanChar());
				case TypeCode.Int16: return _Int16 ?? (_Int16 = new LessThanInt16());
				case TypeCode.Int32: return _Int32 ?? (_Int32 = new LessThanInt32());
				case TypeCode.Int64: return _Int64 ?? (_Int64 = new LessThanInt64());
				case TypeCode.UInt16: return _UInt16 ?? (_UInt16 = new LessThanUInt16());
				case TypeCode.UInt32: return _UInt32 ?? (_UInt32 = new LessThanUInt32());
				case TypeCode.UInt64: return _UInt64 ?? (_UInt64 = new LessThanUInt64());
				case TypeCode.Single: return _Single ?? (_Single = new LessThanSingle());
				case TypeCode.Double: return _Double ?? (_Double = new LessThanDouble());
				default: throw Assert.Unreachable;
			}
		}

		/// <summary>このオブジェクトの文字列表現を取得します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		public override string ToString() { return "LessThan()"; }
	}
}
