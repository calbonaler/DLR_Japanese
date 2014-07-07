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
	/// <summary>���l�^���m�̌^�ϊ����s�����߂̊�{�N���X��\���܂��B</summary>
	abstract class NumericConvertInstruction : Instruction
	{
		internal readonly TypeCode _from, _to;

		/// <summary>�ϊ�������ѕϊ���̌^���g�p���āA<see cref="Microsoft.Scripting.Interpreter.NumericConvertInstruction"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
		/// <param name="from">�ϊ����̌^���w�肵�܂��B</param>
		/// <param name="to">�ϊ���̌^���w�肵�܂��B</param>
		protected NumericConvertInstruction(TypeCode from, TypeCode to)
		{
			_from = from;
			_to = to;
		}

		/// <summary>���̖��߂ŏ�����X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ConsumedStack { get { return 1; } }

		/// <summary>���̖��߂Ő��������X�^�b�N���̗v�f�̐����擾���܂��B</summary>
		public override int ProducedStack { get { return 1; } }

		/// <summary>���̃I�u�W�F�N�g�̕�����\�����擾���܂��B</summary>
		/// <returns>�I�u�W�F�N�g�̕�����\���B</returns>
		public override string ToString() { return InstructionName + "(" + _from + "->" + _to + ")"; }

		/// <summary>���l�^���m�̃I�[�o�[�t���[���`�F�b�N���Ȃ��^�ϊ����߂�\���܂��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
		public sealed class Unchecked : NumericConvertInstruction
		{
			/// <summary>���̖��߂̖��O���擾���܂��B</summary>
			public override string InstructionName { get { return "UncheckedConvert"; } }

			/// <summary>�ϊ�������ѕϊ���̌^���g�p���āA<see cref="Microsoft.Scripting.Interpreter.NumericConvertInstruction.Unchecked"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
			/// <param name="from">�ϊ����̌^���w�肵�܂��B</param>
			/// <param name="to">�ϊ���̌^���w�肵�܂��B</param>
			public Unchecked(TypeCode from, TypeCode to) : base(from, to) { }

			/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
			/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
			/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
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

		/// <summary>���l�^���m�̃I�[�o�[�t���[���`�F�b�N����^�ϊ����߂�\���܂��B</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
		public sealed class Checked : NumericConvertInstruction
		{
			/// <summary>���̖��߂̖��O���擾���܂��B</summary>
			public override string InstructionName { get { return "CheckedConvert"; } }

			/// <summary>�ϊ�������ѕϊ���̌^���g�p���āA<see cref="Microsoft.Scripting.Interpreter.NumericConvertInstruction.Checked"/> �N���X�̐V�����C���X�^���X�����������܂��B</summary>
			/// <param name="from">�ϊ����̌^���w�肵�܂��B</param>
			/// <param name="to">�ϊ���̌^���w�肵�܂��B</param>
			public Checked(TypeCode from, TypeCode to) : base(from, to) { }

			/// <summary>�w�肳�ꂽ�t���[�����g�p���Ă��̖��߂����s���A���Ɏ��s���閽�߂ւ̃I�t�Z�b�g��Ԃ��܂��B</summary>
			/// <param name="frame">���߂ɂ���Ďg�p�����񂪊܂܂�Ă���t���[�����w�肵�܂��B</param>
			/// <returns>���Ɏ��s���閽�߂ւ̃I�t�Z�b�g�B</returns>
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
