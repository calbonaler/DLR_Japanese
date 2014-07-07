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
	/// <summary>���l�^���m�̏��Z���߂�\���܂��B</summary>
	abstract class DivInstruction : Instruction
	{
		static Instruction _Int16, _Int32, _Int64, _UInt16, _UInt32, _UInt64, _Single, _Double;

		/// <summary>���̖��߂ŏ�����X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ConsumedStack { get { return 2; } }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 1; } }

		DivInstruction() { }

		sealed class DivInt32 : DivInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var r = frame.Pop();
				var l = frame.Pop();
				frame.Push((int)l / (int)r);
				return 1;
			}
		}

		sealed class DivInt16 : DivInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var r = frame.Pop();
				var l = frame.Pop();
				frame.Push((object)(short)((short)l / (short)r));
				return 1;
			}
		}

		sealed class DivInt64 : DivInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var r = frame.Pop();
				var l = frame.Pop();
				frame.Push((object)(long)((long)l / (long)r));
				return 1;
			}
		}

		sealed class DivUInt16 : DivInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var r = frame.Pop();
				var l = frame.Pop();
				frame.Push((object)(ushort)((ushort)l / (ushort)r));
				return 1;
			}
		}

		sealed class DivUInt32 : DivInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var r = frame.Pop();
				var l = frame.Pop();
				frame.Push((object)(uint)((uint)l / (uint)r));
				return 1;
			}
		}

		sealed class DivUInt64 : DivInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var r = frame.Pop();
				var l = frame.Pop();
				frame.Push((object)(ulong)((ulong)l / (ulong)r));
				return 1;
			}
		}

		sealed class DivSingle : DivInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var r = frame.Pop();
				var l = frame.Pop();
				frame.Push((object)(float)((float)l / (float)r));
				return 1;
			}
		}

		sealed class DivDouble : DivInstruction
		{
			public override int Run(InterpretedFrame frame)
			{
				var r = frame.Pop();
				var l = frame.Pop();
				frame.Push((object)(double)((double)l / (double)r));
				return 1;
			}
		}

		/// <summary>�w�肳�ꂽ���l�^�ɑ΂���K�؂ȏ��Z���߂��쐬���܂��B</summary>
		/// <param name="type">���Z�Ώۂ̐��l�̌^���w�肵�܂��B</param>
		/// <returns>���l�^�ɑ΂���K�؂ȏ��Z���߁B</returns>
		public static Instruction Create(Type type)
		{
			Debug.Assert(!type.IsEnum);
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Int16: return _Int16 ?? (_Int16 = new DivInt16());
				case TypeCode.Int32: return _Int32 ?? (_Int32 = new DivInt32());
				case TypeCode.Int64: return _Int64 ?? (_Int64 = new DivInt64());
				case TypeCode.UInt16: return _UInt16 ?? (_UInt16 = new DivUInt16());
				case TypeCode.UInt32: return _UInt32 ?? (_UInt32 = new DivUInt32());
				case TypeCode.UInt64: return _UInt64 ?? (_UInt64 = new DivUInt64());
				case TypeCode.Single: return _Single ?? (_Single = new DivSingle());
				case TypeCode.Double: return _Double ?? (_Double = new DivDouble());
				default: throw Assert.Unreachable;
			}
		}

		/// <summary>���̃I�u�W�F�N�g�̕�����\�����擾���܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̕�����\���B</returns>
		public override string ToString() { return "Div()"; }
	}
}
