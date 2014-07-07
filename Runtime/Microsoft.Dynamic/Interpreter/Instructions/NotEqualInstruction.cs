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

namespace Microsoft.Scripting.Interpreter
{
	/// <summary>プリミティブ型の不等値比較および参照型の参照比較を行う命令を表します。</summary>
	abstract class NotEqualInstruction : Instruction
	{
		// Perf: EqualityComparer<T> but is 3/2 to 2 times slower.
		static Instruction _Reference, _Boolean, _SByte, _Int16, _Char, _Int32, _Int64, _Byte, _UInt16, _UInt32, _UInt64, _Single, _Double;

		/// <summary>この命令で消費されるスタック中の要素の数を取得します。</summary>
		public override int ConsumedStack { get { return 2; } }

		/// <summary>この命令で生成されるスタック中の要素の数を取得します。</summary>
		public override int ProducedStack { get { return 1; } }

		NotEqualInstruction() { }

		sealed class NotEqualBoolean : NotEqualInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				frame.Push(((bool)frame.Pop()) != ((bool)frame.Pop()));
				return +1;
			}
		}

		sealed class NotEqualSByte : NotEqualInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				frame.Push(((sbyte)frame.Pop()) != ((sbyte)frame.Pop()));
				return +1;
			}
		}

		sealed class NotEqualInt16 : NotEqualInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				frame.Push(((short)frame.Pop()) != ((short)frame.Pop()));
				return +1;
			}
		}

		sealed class NotEqualChar : NotEqualInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				frame.Push(((char)frame.Pop()) != ((char)frame.Pop()));
				return +1;
			}
		}

		sealed class NotEqualInt32 : NotEqualInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				frame.Push(((int)frame.Pop()) != ((int)frame.Pop()));
				return +1;
			}
		}

		sealed class NotEqualInt64 : NotEqualInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				frame.Push(((long)frame.Pop()) != ((long)frame.Pop()));
				return +1;
			}
		}

		sealed class NotEqualByte : NotEqualInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				frame.Push(((byte)frame.Pop()) != ((byte)frame.Pop()));
				return +1;
			}
		}

		sealed class NotEqualUInt16 : NotEqualInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				frame.Push(((ushort)frame.Pop()) != ((ushort)frame.Pop()));
				return +1;
			}
		}

		sealed class NotEqualUInt32 : NotEqualInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				frame.Push(((uint)frame.Pop()) != ((uint)frame.Pop()));
				return +1;
			}
		}

		sealed class NotEqualUInt64 : NotEqualInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				frame.Push(((ulong)frame.Pop()) != ((ulong)frame.Pop()));
				return +1;
			}
		}

		sealed class NotEqualSingle : NotEqualInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				frame.Push(((float)frame.Pop()) != ((float)frame.Pop()));
				return +1;
			}
		}

		sealed class NotEqualDouble : NotEqualInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				frame.Push(((double)frame.Pop()) != ((double)frame.Pop()));
				return +1;
			}
		}

		sealed class NotEqualReference : NotEqualInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				frame.Push(frame.Pop() != frame.Pop());
				return +1;
			}
		}

		/// <summary>指定されたプリミティブ型または参照型に対する適切な不等値および参照比較命令を作成します。</summary>
		/// <param name="type">比較対象のプリミティブ型または参照型を指定します。</param>
		/// <returns>プリミティブ型または参照型に対する適切な不等値および参照比較命令。</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		public static Instruction Create(Type type)
		{
			// Boxed enums can be unboxed as their underlying types:
			switch (Type.GetTypeCode(type.IsEnum ? Enum.GetUnderlyingType(type) : type))
			{
				case TypeCode.Boolean: return _Boolean ?? (_Boolean = new NotEqualBoolean());
				case TypeCode.SByte: return _SByte ?? (_SByte = new NotEqualSByte());
				case TypeCode.Byte: return _Byte ?? (_Byte = new NotEqualByte());
				case TypeCode.Char: return _Char ?? (_Char = new NotEqualChar());
				case TypeCode.Int16: return _Int16 ?? (_Int16 = new NotEqualInt16());
				case TypeCode.Int32: return _Int32 ?? (_Int32 = new NotEqualInt32());
				case TypeCode.Int64: return _Int64 ?? (_Int64 = new NotEqualInt64());
				case TypeCode.UInt16: return _UInt16 ?? (_UInt16 = new NotEqualUInt16());
				case TypeCode.UInt32: return _UInt32 ?? (_UInt32 = new NotEqualUInt32());
				case TypeCode.UInt64: return _UInt64 ?? (_UInt64 = new NotEqualUInt64());
				case TypeCode.Single: return _Single ?? (_Single = new NotEqualSingle());
				case TypeCode.Double: return _Double ?? (_Double = new NotEqualDouble());
				case TypeCode.Object:
					if (!type.IsValueType)
						return _Reference ?? (_Reference = new NotEqualReference());
					// TODO: Nullable<T>
					throw new NotImplementedException();

				default:
					throw new NotImplementedException();
			}
		}

		/// <summary>このオブジェクトの文字列表現を取得します。</summary>
		/// <returns>オブジェクトの文字列表現。</returns>
		public override string ToString() { return "NotEqual()"; }
	}
}

