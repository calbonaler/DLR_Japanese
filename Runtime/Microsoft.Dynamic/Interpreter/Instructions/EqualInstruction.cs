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
	/// <summary>�v���~�e�B�u�^�̓��l��r����юQ�ƌ^�̎Q�Ɣ�r���s�����߂�\���܂��B</summary>
	abstract class EqualInstruction : Instruction
	{
		// Perf: EqualityComparer<T> but is 1.5 to 2 times slower.
		static Instruction _Reference, _Boolean, _SByte, _Int16, _Char, _Int32, _Int64, _Byte, _UInt16, _UInt32, _UInt64, _Single, _Double;

		/// <summary>���̖��߂ŏ�����X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ConsumedStack { get { return 2; } }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 1; } }

		EqualInstruction() { }

		sealed class EqualBoolean : EqualInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				frame.Push(((bool)frame.Pop()) == ((bool)frame.Pop()));
				return +1;
			}
		}

		sealed class EqualSByte : EqualInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				frame.Push(((sbyte)frame.Pop()) == ((sbyte)frame.Pop()));
				return +1;
			}
		}

		sealed class EqualInt16 : EqualInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				frame.Push(((short)frame.Pop()) == ((short)frame.Pop()));
				return +1;
			}
		}

		sealed class EqualChar : EqualInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				frame.Push(((char)frame.Pop()) == ((char)frame.Pop()));
				return +1;
			}
		}

		sealed class EqualInt32 : EqualInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				frame.Push(((int)frame.Pop()) == ((int)frame.Pop()));
				return +1;
			}
		}

		sealed class EqualInt64 : EqualInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				frame.Push(((long)frame.Pop()) == ((long)frame.Pop()));
				return +1;
			}
		}

		sealed class EqualByte : EqualInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				frame.Push(((byte)frame.Pop()) == ((byte)frame.Pop()));
				return +1;
			}
		}

		sealed class EqualUInt16 : EqualInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				frame.Push(((ushort)frame.Pop()) == ((ushort)frame.Pop()));
				return +1;
			}
		}

		sealed class EqualUInt32 : EqualInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				frame.Push(((uint)frame.Pop()) == ((uint)frame.Pop()));
				return +1;
			}
		}

		sealed class EqualUInt64 : EqualInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				frame.Push(((ulong)frame.Pop()) == ((ulong)frame.Pop()));
				return +1;
			}
		}

		sealed class EqualSingle : EqualInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				frame.Push(((float)frame.Pop()) == ((float)frame.Pop()));
				return +1;
			}
		}

		sealed class EqualDouble : EqualInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				frame.Push(((double)frame.Pop()) == ((double)frame.Pop()));
				return +1;
			}
		}

		sealed class EqualReference : EqualInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				frame.Push(frame.Pop() == frame.Pop());
				return +1;
			}
		}

		/// <summary>�w�肳�ꂽ�v���~�e�B�u�^�܂��͎Q�ƌ^�ɑ΂���K�؂ȓ��l����юQ�Ɣ�r���߂��쐬���܂��B</summary>
		/// <param name="type">��r�Ώۂ̃v���~�e�B�u�^�܂��͎Q�ƌ^���w�肵�܂��B</param>
		/// <returns>�v���~�e�B�u�^�܂��͎Q�ƌ^�ɑ΂���K�؂ȓ��l����юQ�Ɣ�r���߁B</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		public static Instruction Create(Type type)
		{
			// Boxed enums can be unboxed as their underlying types:
			switch (Type.GetTypeCode(type.IsEnum ? Enum.GetUnderlyingType(type) : type))
			{
				case TypeCode.Boolean: return _Boolean ?? (_Boolean = new EqualBoolean());
				case TypeCode.SByte: return _SByte ?? (_SByte = new EqualSByte());
				case TypeCode.Byte: return _Byte ?? (_Byte = new EqualByte());
				case TypeCode.Char: return _Char ?? (_Char = new EqualChar());
				case TypeCode.Int16: return _Int16 ?? (_Int16 = new EqualInt16());
				case TypeCode.Int32: return _Int32 ?? (_Int32 = new EqualInt32());
				case TypeCode.Int64: return _Int64 ?? (_Int64 = new EqualInt64());
				case TypeCode.UInt16: return _UInt16 ?? (_UInt16 = new EqualUInt16());
				case TypeCode.UInt32: return _UInt32 ?? (_UInt32 = new EqualUInt32());
				case TypeCode.UInt64: return _UInt64 ?? (_UInt64 = new EqualUInt64());
				case TypeCode.Single: return _Single ?? (_Single = new EqualSingle());
				case TypeCode.Double: return _Double ?? (_Double = new EqualDouble());
				case TypeCode.Object:
					if (!type.IsValueType)
						return _Reference ?? (_Reference = new EqualReference());
					// TODO: Nullable<T>
					throw new NotImplementedException();
				default:
					throw new NotImplementedException();
			}
		}

		/// <summary>���̃I�u�W�F�N�g�̕�����\�����擾���܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̕�����\���B</returns>
		public override string ToString() { return "Equal()"; }
	}
}
