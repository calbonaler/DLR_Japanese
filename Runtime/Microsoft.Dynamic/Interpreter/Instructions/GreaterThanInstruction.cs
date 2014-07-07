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
	/// <summary>数値型同士の大なり比較命令を表します。</summary>
	abstract class GreaterThanInstruction : Instruction
	{
		static Instruction _SByte, _Int16, _Char, _Int32, _Int64, _Byte, _UInt16, _UInt32, _UInt64, _Single, _Double;

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return 2; } }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		GreaterThanInstruction() { }

		sealed class GreaterThanSByte : GreaterThanInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var right = (sbyte)frame.Pop();
				frame.Push(((sbyte)frame.Pop()) > right);
				return +1;
			}
		}

		sealed class GreaterThanInt16 : GreaterThanInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var right = (short)frame.Pop();
				frame.Push(((short)frame.Pop()) > right);
				return +1;
			}
		}

		sealed class GreaterThanChar : GreaterThanInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var right = (char)frame.Pop();
				frame.Push(((char)frame.Pop()) > right);
				return +1;
			}
		}

		sealed class GreaterThanInt32 : GreaterThanInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var right = (int)frame.Pop();
				frame.Push(((int)frame.Pop()) > right);
				return +1;
			}
		}

		sealed class GreaterThanInt64 : GreaterThanInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var right = (long)frame.Pop();
				frame.Push(((long)frame.Pop()) > right);
				return +1;
			}
		}

		sealed class GreaterThanByte : GreaterThanInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var right = (byte)frame.Pop();
				frame.Push(((byte)frame.Pop()) > right);
				return +1;
			}
		}

		sealed class GreaterThanUInt16 : GreaterThanInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var right = (ushort)frame.Pop();
				frame.Push(((ushort)frame.Pop()) > right);
				return +1;
			}
		}

		sealed class GreaterThanUInt32 : GreaterThanInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var right = (uint)frame.Pop();
				frame.Push(((uint)frame.Pop()) > right);
				return +1;
			}
		}

		sealed class GreaterThanUInt64 : GreaterThanInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var right = (ulong)frame.Pop();
				frame.Push(((ulong)frame.Pop()) > right);
				return +1;
			}
		}

		sealed class GreaterThanSingle : GreaterThanInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var right = (float)frame.Pop();
				frame.Push(((float)frame.Pop()) > right);
				return +1;
			}
		}

		sealed class GreaterThanDouble : GreaterThanInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var right = (double)frame.Pop();
				frame.Push(((double)frame.Pop()) > right);
				return +1;
			}
		}

		/// <summary>指定された数値型に対する適切な大なり比較命令を作成します。</summary>
		/// <param name="type">比較対象の数値の型を指定します。</param>
		/// <returns>数値型に対する適切な大なり比較命令。</returns>
		public static Instruction Create(Type type)
		{
			Debug.Assert(!type.IsEnum);
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.SByte: return _SByte ?? (_SByte = new GreaterThanSByte());
				case TypeCode.Byte: return _Byte ?? (_Byte = new GreaterThanByte());
				case TypeCode.Char: return _Char ?? (_Char = new GreaterThanChar());
				case TypeCode.Int16: return _Int16 ?? (_Int16 = new GreaterThanInt16());
				case TypeCode.Int32: return _Int32 ?? (_Int32 = new GreaterThanInt32());
				case TypeCode.Int64: return _Int64 ?? (_Int64 = new GreaterThanInt64());
				case TypeCode.UInt16: return _UInt16 ?? (_UInt16 = new GreaterThanUInt16());
				case TypeCode.UInt32: return _UInt32 ?? (_UInt32 = new GreaterThanUInt32());
				case TypeCode.UInt64: return _UInt64 ?? (_UInt64 = new GreaterThanUInt64());
				case TypeCode.Single: return _Single ?? (_Single = new GreaterThanSingle());
				case TypeCode.Double: return _Double ?? (_Double = new GreaterThanDouble());
				default: throw Assert.Unreachable;
			}
		}

		/// <summary>このオブジェクトの文字列表現を取得します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		public override string ToString() { return "GreaterThan()"; }
	}
}
