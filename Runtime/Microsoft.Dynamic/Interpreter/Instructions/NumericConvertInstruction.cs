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
using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>数値型同士の型変換を行う命令の基本クラスを表します。</summary>
	abstract class NumericConvertInstruction : Instruction
	{
		internal readonly TypeCode _from, _to;

		/// <summary>変換元および変換先の型を使用して、<see cref="Microsoft.Scripting.Interpreter.NumericConvertInstruction"/> クラスの新しいインスタンスを初期化します。</summary>
		/// <param name="from">変換元の型を指定します。</param>
		/// <param name="to">変換先の型を指定します。</param>
		protected NumericConvertInstruction(TypeCode from, TypeCode to)
		{
			_from = from;
			_to = to;
		}

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>このオブジェクトの文字列表現を取得します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		public override string ToString() { return InstructionName + "(" + _from + "->" + _to + ")"; }

		/// <summary>数値型同士のオーバーフローをチェックしない型変換命令を表します。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
		public sealed class Unchecked : NumericConvertInstruction
		{
			/// <summary>この命令の名前を取得します。</summary>
			public override string InstructionName { get { return "UncheckedConvert"; } }

			/// <summary>変換元および変換先の型を使用して、<see cref="Microsoft.Scripting.Interpreter.NumericConvertInstruction.Unchecked"/> クラスの新しいインスタンスを初期化します。</summary>
			/// <param name="from">変換元の型を指定します。</param>
			/// <param name="to">変換先の型を指定します。</param>
			public Unchecked(TypeCode from, TypeCode to) : base(from, to) { }

			/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
			/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
			/// <returns>次に実行する命令へのオフセット。</returns>
			public override int Run(InterpretedFrame frame)
			{
				frame.Push(Convert(frame.Pop()));
				return +1;
			}

			object Convert(object obj)
			{
				switch (_from)
				{
					case TypeCode.Byte: return ConvertInt32((byte)obj);
					case TypeCode.SByte: return ConvertInt32((sbyte)obj);
					case TypeCode.Int16: return ConvertInt32((short)obj);
					case TypeCode.Char: return ConvertInt32((char)obj);
					case TypeCode.Int32: return ConvertInt32((int)obj);
					case TypeCode.Int64: return ConvertInt64((long)obj);
					case TypeCode.UInt16: return ConvertInt32((ushort)obj);
					case TypeCode.UInt32: return ConvertInt64((uint)obj);
					case TypeCode.UInt64: return ConvertUInt64((ulong)obj);
					case TypeCode.Single: return ConvertDouble((float)obj);
					case TypeCode.Double: return ConvertDouble((double)obj);
					default: throw Assert.Unreachable;
				}
			}

			object ConvertInt32(int obj)
			{
				unchecked
				{
					switch (_to)
					{
						case TypeCode.Byte: return (byte)obj;
						case TypeCode.SByte: return (sbyte)obj;
						case TypeCode.Int16: return (short)obj;
						case TypeCode.Char: return (char)obj;
						case TypeCode.Int32: return (int)obj;
						case TypeCode.Int64: return (long)obj;
						case TypeCode.UInt16: return (ushort)obj;
						case TypeCode.UInt32: return (uint)obj;
						case TypeCode.UInt64: return (ulong)obj;
						case TypeCode.Single: return (float)obj;
						case TypeCode.Double: return (double)obj;
						default: throw Assert.Unreachable;
					}
				}
			}

			object ConvertInt64(long obj)
			{
				unchecked
				{
					switch (_to)
					{
						case TypeCode.Byte: return (byte)obj;
						case TypeCode.SByte: return (sbyte)obj;
						case TypeCode.Int16: return (short)obj;
						case TypeCode.Char: return (char)obj;
						case TypeCode.Int32: return (int)obj;
						case TypeCode.Int64: return (long)obj;
						case TypeCode.UInt16: return (ushort)obj;
						case TypeCode.UInt32: return (uint)obj;
						case TypeCode.UInt64: return (ulong)obj;
						case TypeCode.Single: return (float)obj;
						case TypeCode.Double: return (double)obj;
						default: throw Assert.Unreachable;
					}
				}
			}

			object ConvertUInt64(ulong obj)
			{
				unchecked
				{
					switch (_to)
					{
						case TypeCode.Byte: return (byte)obj;
						case TypeCode.SByte: return (sbyte)obj;
						case TypeCode.Int16: return (short)obj;
						case TypeCode.Char: return (char)obj;
						case TypeCode.Int32: return (int)obj;
						case TypeCode.Int64: return (long)obj;
						case TypeCode.UInt16: return (ushort)obj;
						case TypeCode.UInt32: return (uint)obj;
						case TypeCode.UInt64: return (ulong)obj;
						case TypeCode.Single: return (float)obj;
						case TypeCode.Double: return (double)obj;
						default: throw Assert.Unreachable;
					}
				}
			}

			object ConvertDouble(double obj)
			{
				unchecked
				{
					switch (_to)
					{
						case TypeCode.Byte: return (byte)obj;
						case TypeCode.SByte: return (sbyte)obj;
						case TypeCode.Int16: return (short)obj;
						case TypeCode.Char: return (char)obj;
						case TypeCode.Int32: return (int)obj;
						case TypeCode.Int64: return (long)obj;
						case TypeCode.UInt16: return (ushort)obj;
						case TypeCode.UInt32: return (uint)obj;
						case TypeCode.UInt64: return (ulong)obj;
						case TypeCode.Single: return (float)obj;
						case TypeCode.Double: return (double)obj;
						default: throw Assert.Unreachable;
					}
				}
			}
		}

		/// <summary>数値型同士のオーバーフローをチェックする型変換命令を表します。</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
		public sealed class Checked : NumericConvertInstruction
		{
			/// <summary>この命令の名前を取得します。</summary>
			public override string InstructionName { get { return "CheckedConvert"; } }

			/// <summary>変換元および変換先の型を使用して、<see cref="Microsoft.Scripting.Interpreter.NumericConvertInstruction.Checked"/> クラスの新しいインスタンスを初期化します。</summary>
			/// <param name="from">変換元の型を指定します。</param>
			/// <param name="to">変換先の型を指定します。</param>
			public Checked(TypeCode from, TypeCode to) : base(from, to) { }

			/// <summary>指定されたフレームを使用してこの命令を実行し、次に実行する命令へのオフセットを返します。</summary>
			/// <param name="frame">命令によって使用する情報が含まれているフレームを指定します。</param>
			/// <returns>次に実行する命令へのオフセット。</returns>
			public override int Run(InterpretedFrame frame)
			{
				frame.Push(Convert(frame.Pop()));
				return +1;
			}

			object Convert(object obj)
			{
				switch (_from)
				{
					case TypeCode.Byte: return ConvertInt32((byte)obj);
					case TypeCode.SByte: return ConvertInt32((sbyte)obj);
					case TypeCode.Int16: return ConvertInt32((short)obj);
					case TypeCode.Char: return ConvertInt32((char)obj);
					case TypeCode.Int32: return ConvertInt32((int)obj);
					case TypeCode.Int64: return ConvertInt64((long)obj);
					case TypeCode.UInt16: return ConvertInt32((ushort)obj);
					case TypeCode.UInt32: return ConvertInt64((uint)obj);
					case TypeCode.UInt64: return ConvertUInt64((ulong)obj);
					case TypeCode.Single: return ConvertDouble((float)obj);
					case TypeCode.Double: return ConvertDouble((double)obj);
					default: throw Assert.Unreachable;
				}
			}

			object ConvertInt32(int obj)
			{
				checked
				{
					switch (_to)
					{
						case TypeCode.Byte: return (byte)obj;
						case TypeCode.SByte: return (sbyte)obj;
						case TypeCode.Int16: return (short)obj;
						case TypeCode.Char: return (char)obj;
						case TypeCode.Int32: return (int)obj;
						case TypeCode.Int64: return (long)obj;
						case TypeCode.UInt16: return (ushort)obj;
						case TypeCode.UInt32: return (uint)obj;
						case TypeCode.UInt64: return (ulong)obj;
						case TypeCode.Single: return (float)obj;
						case TypeCode.Double: return (double)obj;
						default: throw Assert.Unreachable;
					}
				}
			}

			object ConvertInt64(long obj)
			{
				checked
				{
					switch (_to)
					{
						case TypeCode.Byte: return (byte)obj;
						case TypeCode.SByte: return (sbyte)obj;
						case TypeCode.Int16: return (short)obj;
						case TypeCode.Char: return (char)obj;
						case TypeCode.Int32: return (int)obj;
						case TypeCode.Int64: return (long)obj;
						case TypeCode.UInt16: return (ushort)obj;
						case TypeCode.UInt32: return (uint)obj;
						case TypeCode.UInt64: return (ulong)obj;
						case TypeCode.Single: return (float)obj;
						case TypeCode.Double: return (double)obj;
						default: throw Assert.Unreachable;
					}
				}
			}

			object ConvertUInt64(ulong obj)
			{
				checked
				{
					switch (_to)
					{
						case TypeCode.Byte: return (byte)obj;
						case TypeCode.SByte: return (sbyte)obj;
						case TypeCode.Int16: return (short)obj;
						case TypeCode.Char: return (char)obj;
						case TypeCode.Int32: return (int)obj;
						case TypeCode.Int64: return (long)obj;
						case TypeCode.UInt16: return (ushort)obj;
						case TypeCode.UInt32: return (uint)obj;
						case TypeCode.UInt64: return (ulong)obj;
						case TypeCode.Single: return (float)obj;
						case TypeCode.Double: return (double)obj;
						default: throw Assert.Unreachable;
					}
				}
			}

			object ConvertDouble(double obj)
			{
				checked
				{
					switch (_to)
					{
						case TypeCode.Byte: return (byte)obj;
						case TypeCode.SByte: return (sbyte)obj;
						case TypeCode.Int16: return (short)obj;
						case TypeCode.Char: return (char)obj;
						case TypeCode.Int32: return (int)obj;
						case TypeCode.Int64: return (long)obj;
						case TypeCode.UInt16: return (ushort)obj;
						case TypeCode.UInt32: return (uint)obj;
						case TypeCode.UInt64: return (ulong)obj;
						case TypeCode.Single: return (float)obj;
						case TypeCode.Double: return (double)obj;
						default: throw Assert.Unreachable;
					}
				}
			}
		}
	}
}
