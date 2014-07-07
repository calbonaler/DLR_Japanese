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
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Interpreter
{
	/// <summary><see cref="OverflowException"/> を発生させない数値型同士の加算命令を表します。</summary>
	abstract class AddInstruction : Instruction
	{
		static Instruction _Int16, _Int32, _Int64, _UInt16, _UInt32, _UInt64, _Single, _Double;

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return 2; } }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		AddInstruction() { }

		sealed class AddInt32 : AddInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var r = frame.Pop();
				var l = frame.Pop();
				frame.Push(unchecked((int)l + (int)r));
				return +1;
			}
		}

		sealed class AddInt16 : AddInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var r = frame.Pop();
				var l = frame.Pop();
				frame.Push((object)(short)(unchecked((short)l + (short)r)));
				return +1;
			}
		}

		sealed class AddInt64 : AddInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var r = frame.Pop();
				var l = frame.Pop();
				frame.Push((object)(long)(unchecked((long)l + (long)r)));
				return +1;
			}
		}

		sealed class AddUInt16 : AddInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var r = frame.Pop();
				var l = frame.Pop();
				frame.Push((object)(ushort)(unchecked((ushort)l + (ushort)r)));
				return +1;
			}
		}

		sealed class AddUInt32 : AddInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var r = frame.Pop();
				var l = frame.Pop();
				frame.Push((object)(uint)(unchecked((uint)l + (uint)r)));
				return +1;
			}
		}

		sealed class AddUInt64 : AddInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var r = frame.Pop();
				var l = frame.Pop();
				frame.Push((object)(ulong)(unchecked((ulong)l + (ulong)r)));
				return +1;
			}
		}

		sealed class AddSingle : AddInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var r = frame.Pop();
				var l = frame.Pop();
				frame.Push((object)(float)((float)l + (float)r));
				return +1;
			}
		}

		sealed class AddDouble : AddInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var r = frame.Pop();
				var l = frame.Pop();
				frame.Push((object)((double)l + (double)r));
				return +1;
			}
		}

		/// <summary>指定された数値型に対する適切な加算命令を作成します。</summary>
		/// <param name="type">加算対象の数値の型を指定します。</param>
		/// <returns>数値型に対する適切な加算命令。</returns>
		public static Instruction Create(Type type)
		{
			Debug.Assert(!type.IsEnum);
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Int16: return _Int16 ?? (_Int16 = new AddInt16());
				case TypeCode.Int32: return _Int32 ?? (_Int32 = new AddInt32());
				case TypeCode.Int64: return _Int64 ?? (_Int64 = new AddInt64());
				case TypeCode.UInt16: return _UInt16 ?? (_UInt16 = new AddUInt16());
				case TypeCode.UInt32: return _UInt32 ?? (_UInt32 = new AddUInt32());
				case TypeCode.UInt64: return _UInt64 ?? (_UInt64 = new AddUInt64());
				case TypeCode.Single: return _Single ?? (_Single = new AddSingle());
				case TypeCode.Double: return _Double ?? (_Double = new AddDouble());
				default:
					throw Assert.Unreachable;
			}
		}

		/// <summary>このオブジェクトの文字列表現を取得します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		public override string ToString() { return "Add()"; }
	}

	/// <summary><see cref="OverflowException"/> を発生させる数値型同士の加算命令を表します。</summary>
	abstract class AddOvfInstruction : Instruction
	{
		static Instruction _Int16, _Int32, _Int64, _UInt16, _UInt32, _UInt64, _Single, _Double;

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return 2; } }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		AddOvfInstruction() { }

		sealed class AddOvfInt32 : AddOvfInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var r = frame.Pop();
				var l = frame.Pop();
				frame.Push(checked((int)l + (int)r));
				return +1;
			}
		}

		sealed class AddOvfInt16 : AddOvfInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var r = frame.Pop();
				var l = frame.Pop();
				frame.Push((object)(short)checked((short)l + (short)r));
				return +1;
			}
		}

		sealed class AddOvfInt64 : AddOvfInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var r = frame.Pop();
				var l = frame.Pop();
				frame.Push((object)(long)checked((long)l + (long)r));
				return +1;
			}
		}

		sealed class AddOvfUInt16 : AddOvfInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var r = frame.Pop();
				var l = frame.Pop();
				frame.Push((object)(ushort)checked((ushort)l + (ushort)r));
				return +1;
			}
		}

		sealed class AddOvfUInt32 : AddOvfInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var r = frame.Pop();
				var l = frame.Pop();
				frame.Push((object)(uint)checked((uint)l + (uint)r));
				return +1;
			}
		}

		sealed class AddOvfUInt64 : AddOvfInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var r = frame.Pop();
				var l = frame.Pop();
				frame.Push((object)(ulong)checked((ulong)l + (ulong)r));
				return +1;
			}
		}

		sealed class AddOvfSingle : AddOvfInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var r = frame.Pop();
				var l = frame.Pop();
				frame.Push((object)(float)((float)l + (float)r));
				return +1;
			}
		}

		sealed class AddOvfDouble : AddOvfInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var r = frame.Pop();
				var l = frame.Pop();
				frame.Push((object)((double)l + (double)r));
				return +1;
			}
		}

		/// <summary>指定された数値型に対する適切な加算命令を作成します。</summary>
		/// <param name="type">加算対象の数値の型を指定します。</param>
		/// <returns>数値型に対する適切な加算命令。</returns>
		public static Instruction Create(Type type)
		{
			Debug.Assert(!type.IsEnum);
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Int16: return _Int16 ?? (_Int16 = new AddOvfInt16());
				case TypeCode.Int32: return _Int32 ?? (_Int32 = new AddOvfInt32());
				case TypeCode.Int64: return _Int64 ?? (_Int64 = new AddOvfInt64());
				case TypeCode.UInt16: return _UInt16 ?? (_UInt16 = new AddOvfUInt16());
				case TypeCode.UInt32: return _UInt32 ?? (_UInt32 = new AddOvfUInt32());
				case TypeCode.UInt64: return _UInt64 ?? (_UInt64 = new AddOvfUInt64());
				case TypeCode.Single: return _Single ?? (_Single = new AddOvfSingle());
				case TypeCode.Double: return _Double ?? (_Double = new AddOvfDouble());
				default:
					throw Assert.Unreachable;
			}
		}

		/// <summary>このオブジェクトの文字列表現を取得します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		public override string ToString() { return "AddOvf()"; }
	}
}
